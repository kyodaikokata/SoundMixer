using System;
using System.Collections.Generic;
using System.Linq;
using DotNet.Globbing;
using SoundMixer.Api;

namespace SoundMixer;

public class VolumeCalculator
{
    internal const string BuiltinUiGroupId = "a8a8dba3-a7ed-4d79-b85a-e3f600e23b59";

    private Configuration Config { get; }
    private Dictionary<string, float> VolumeCache { get; } = new();
    private EffectiveSnapshot? _effectiveSnapshot;
    private Dictionary<string, Glob>? _effectiveGlobs;
    private bool? _requiresAnyVolumeScaling;

    public VolumeCalculator(Configuration config)
    {
        Config = config;
    }

    internal void SetEffectiveSnapshot(EffectiveSnapshot snapshot)
    {
        _effectiveSnapshot = snapshot;
        _effectiveGlobs = BuildGlobs(snapshot.Groups);
        ClearCache();
    }

    public void ClearCache()
    {
        VolumeCache.Clear();
        _requiresAnyVolumeScaling = null;
    }

    /// <summary>
    /// True when any effective group or per-sound volume differs from 100%.
    /// Used to skip list scans and volume hooks when the plugin is enabled but neutral.
    /// </summary>
    internal bool RequiresAnyVolumeScaling()
    {
        _requiresAnyVolumeScaling ??= ComputeRequiresAnyVolumeScaling();
        return _requiresAnyVolumeScaling.Value;
    }

    private bool ComputeRequiresAnyVolumeScaling()
    {
        const float unityEpsilon = 0.001f;

        foreach (var volume in IndividualVolumes.Values)
        {
            if (Math.Abs(volume - 1.0f) > unityEpsilon)
            {
                return true;
            }
        }

        foreach (var group in Groups)
        {
            if (Math.Abs(group.GroupVolume - 1.0f) > unityEpsilon)
            {
                return true;
            }
        }

        return false;
    }

    private IReadOnlyList<SoundGroup> Groups => _effectiveSnapshot?.Groups ?? Config.Groups;

    private IReadOnlyDictionary<string, string> SoundToGroup =>
        _effectiveSnapshot?.SoundToGroup ?? Config.SoundToGroup;

    private IReadOnlyDictionary<string, float> IndividualVolumes =>
        _effectiveSnapshot?.IndividualVolumes ?? Config.IndividualVolumes;

    private IReadOnlyDictionary<string, string> PathAliases =>
        _effectiveSnapshot?.PathAliases ?? Config.PathAliases;

    public string GetDisplayCategory(string soundPath)
    {
        var groupId = ResolveGroupId(soundPath);
        if (groupId != null)
        {
            var groupName = Groups.FirstOrDefault(g => g.Id == groupId)?.Name;
            if (!string.IsNullOrWhiteSpace(groupName))
            {
                return groupName;
            }
        }

        return SoundClassifier.Classify(GetScdPath(soundPath.ToLowerInvariant())).Category;
    }

    public string? GetGroupNameForSound(string soundPath)
    {
        var groupId = ResolveGroupId(soundPath);
        if (groupId == null)
        {
            return null;
        }

        return Groups.FirstOrDefault(g => g.Id == groupId)?.Name;
    }

    public string? GetMatchedGroupId(string soundPath)
    {
        return ResolveGroupId(soundPath);
    }

    public bool HasMatchingRule(string soundPath)
    {
        soundPath = soundPath.ToLowerInvariant();

        if (IndividualVolumes.ContainsKey(soundPath))
        {
            return true;
        }

        var resolvedPath = ResolveScdPath(soundPath);
        if (IndividualVolumes.ContainsKey(resolvedPath))
        {
            return true;
        }

        soundPath = soundPath.ToLowerInvariant();
        foreach (var group in Groups)
        {
            if (GroupOwnRulesMatch(soundPath, group.Id))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsHiddenFromMonitorByGroup(string soundPath)
    {
        foreach (var group in Groups)
        {
            if (!group.HideFromMonitorLog)
            {
                continue;
            }

            if (SoundBelongsToGroupTree(soundPath, group.Id))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Short pooled SFX (UI/menu clicks). Volume is scaled only at play-hook entry; never via SetVolume/GetVolume tracking.
    /// </summary>
    public bool IsLikelyOneShotPath(string soundPath)
    {
        if (string.IsNullOrWhiteSpace(soundPath) || StreamingBgmTracker.IsBgmOrMusicPath(soundPath))
        {
            return false;
        }

        soundPath = soundPath.ToLowerInvariant();
        var resolved = ResolveScdPath(soundPath);

        if (SoundBelongsToGroupTree(soundPath, BuiltinUiGroupId)
            || (resolved != soundPath && SoundBelongsToGroupTree(resolved, BuiltinUiGroupId)))
        {
            return true;
        }

        foreach (var group in Groups)
        {
            if (!IsUiLikeGroup(group))
            {
                continue;
            }

            if (SoundBelongsToGroupTree(soundPath, group.Id)
                || (resolved != soundPath && SoundBelongsToGroupTree(resolved, group.Id)))
            {
                return true;
            }
        }

        return soundPath.Contains("/ui/", StringComparison.Ordinal)
            || soundPath.Contains("/menu/", StringComparison.Ordinal)
            || soundPath.Contains("se_ui", StringComparison.Ordinal)
            || soundPath.Contains("se_ui.scd", StringComparison.Ordinal)
            || soundPath.Contains("system/se_ui", StringComparison.Ordinal);
    }

    internal bool IsUiGroup(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            return false;
        }

        if (groupId == BuiltinUiGroupId)
        {
            return true;
        }

        var group = Groups.FirstOrDefault(g => g.Id == groupId);
        return group != null && IsUiLikeGroup(group);
    }

    private static bool IsUiLikeGroup(SoundGroup group)
    {
        if (group.PathPatterns.Count == 0)
        {
            return false;
        }

        return group.PathPatterns.All(pattern =>
        {
            var normalized = pattern.ToLowerInvariant();
            return normalized.Contains("ui", StringComparison.Ordinal)
                || normalized.Contains("menu", StringComparison.Ordinal);
        });
    }

    public bool SoundBelongsToGroupTree(string soundPath, string groupId)
    {
        // Refresh must match child groups by their own globs even when deepest ResolveGroupId
        // falls back to a parent (e.g. PlaySound nodes with a broader resolved SCD path).
        if (GroupOwnRulesMatch(soundPath, groupId))
        {
            return true;
        }

        var resolvedGroupId = ResolveGroupId(soundPath);
        if (resolvedGroupId == null)
        {
            return false;
        }

        if (resolvedGroupId == groupId)
        {
            return true;
        }

        return IsDescendantGroup(resolvedGroupId, groupId);
    }

    private bool GroupOwnRulesMatch(string soundPath, string groupId)
    {
        soundPath = ResolveScdPath(soundPath.ToLowerInvariant());
        var group = Groups.FirstOrDefault(g => g.Id == groupId);
        if (group == null)
        {
            return false;
        }

        if (group.SoundPaths.Contains(soundPath))
        {
            return true;
        }

        var scdPath = GetScdPath(soundPath);
        if (scdPath != soundPath && group.SoundPaths.Contains(scdPath))
        {
            return true;
        }

        var globs = GetGlobs();
        foreach (var pattern in group.PathPatterns)
        {
            if (!globs.TryGetValue(pattern, out var glob))
            {
                continue;
            }

            if (glob.IsMatch(soundPath) || (scdPath != soundPath && glob.IsMatch(scdPath)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDescendantGroup(IReadOnlyList<SoundGroup> groups, string childGroupId, string ancestorGroupId)
    {
        var visited = new HashSet<string>();
        var current = groups.FirstOrDefault(g => g.Id == childGroupId);
        var depth = 0;

        while (current?.ParentId != null && depth++ < 64)
        {
            if (!visited.Add(current.Id))
            {
                return false;
            }

            if (current.ParentId == ancestorGroupId)
            {
                return true;
            }

            current = groups.FirstOrDefault(g => g.Id == current.ParentId);
        }

        return false;
    }

    private bool IsDescendantGroup(string childGroupId, string ancestorGroupId)
    {
        return IsDescendantGroup(Groups, childGroupId, ancestorGroupId);
    }

    private string? ResolveGroupId(string soundPath)
    {
        CalculateStackedGroupVolume(ResolveScdPath(soundPath.ToLowerInvariant()), out _, out var deepestMatchedId);
        return deepestMatchedId;
    }

    public float GetVolumeForSound(string soundPath)
    {
        soundPath = soundPath.ToLowerInvariant();
        if (VolumeCache.TryGetValue(soundPath, out var cachedVolume))
        {
            return cachedVolume;
        }

        var volume = CalculateVolume(soundPath, out _);

        volume = Config.ClampVolumeToEngineCap(volume);
        VolumeCache[soundPath] = volume;
        return volume;
    }

    private float CalculateVolume(string soundPath, out string? matchedGroupId)
    {
        soundPath = soundPath.ToLowerInvariant();
        matchedGroupId = null;

        if (IndividualVolumes.TryGetValue(soundPath, out var individualVolume))
        {
            return individualVolume;
        }

        if (SoundToGroup.TryGetValue(soundPath, out var explicitGroupId))
        {
            matchedGroupId = explicitGroupId;
            var explicitVolume = CalculateStackedGroupVolume(soundPath, out _, out _);
            if (Math.Abs(explicitVolume - 1.0f) > 0.001f)
            {
                return explicitVolume;
            }

            var group = Groups.FirstOrDefault(g => g.Id == explicitGroupId);
            return group?.GroupVolume ?? 1.0f;
        }

        var resolvedPath = ResolveScdPath(soundPath);
        if (resolvedPath != soundPath && IndividualVolumes.TryGetValue(resolvedPath, out individualVolume))
        {
            return individualVolume;
        }

        return CalculateStackedGroupVolume(soundPath, out matchedGroupId, out _);
    }

    /// <summary>
    /// Multiply every group whose own glob/path rules match, then walk ancestors when
    /// <see cref="SoundGroup.ScaleByFather"/> is set (same chain math as UI GetEffectiveGroupVolume).
    /// Parent folders without globs (e.g. 战斗 / 环境) only affect sounds via this ancestor pass.
    /// </summary>
    private float CalculateStackedGroupVolume(
        string soundPath,
        out string? anyMatchedGroupId,
        out string? deepestMatchedGroupId
    )
    {
        anyMatchedGroupId = null;
        deepestMatchedGroupId = null;
        var volume = 1.0f;
        var matchedIds = new List<string>();
        var deepestDepth = -1;

        foreach (var group in Groups)
        {
            if (!GroupOwnRulesMatch(soundPath, group.Id))
            {
                continue;
            }

            volume *= group.GroupVolume;
            matchedIds.Add(group.Id);
            anyMatchedGroupId ??= group.Id;

            var depth = GetGroupDepth(group.Id);
            if (depth >= deepestDepth)
            {
                deepestDepth = depth;
                deepestMatchedGroupId = group.Id;
            }
        }

        if (matchedIds.Count == 0)
        {
            return 1.0f;
        }

        var appliedAncestors = new HashSet<string>(matchedIds);
        foreach (var matchedId in matchedIds)
        {
            volume *= CollectAncestorVolumeMultiplier(matchedId, appliedAncestors);
        }

        return volume;
    }

    /// <summary>
    /// Parent groups with no path rules still scale child-matched sounds when ScaleByFather is enabled.
    /// </summary>
    private float CollectAncestorVolumeMultiplier(string groupId, HashSet<string> appliedAncestors)
    {
        var multiplier = 1.0f;
        var current = Groups.FirstOrDefault(g => g.Id == groupId);

        while (current?.ParentId != null)
        {
            if (!current.ScaleByFather)
            {
                break;
            }

            var parent = Groups.FirstOrDefault(g => g.Id == current.ParentId);
            if (parent == null)
            {
                break;
            }

            if (!appliedAncestors.Contains(parent.Id))
            {
                multiplier *= parent.GroupVolume;
                appliedAncestors.Add(parent.Id);
            }

            current = parent;
        }

        return multiplier;
    }

    private int GetGroupDepth(string groupId)
    {
        var depth = 0;
        var visited = new HashSet<string>();
        var current = Groups.FirstOrDefault(g => g.Id == groupId);

        while (current?.ParentId != null && depth < 64)
        {
            if (!visited.Add(current.Id))
            {
                break;
            }

            depth++;
            current = Groups.FirstOrDefault(g => g.Id == current.ParentId);
        }

        return depth;
    }

    private string ResolveScdPath(string rawPath)
    {
        return PathResolver.ResolveScdPath(Config, rawPath, PathAliases, 0);
    }

    private Dictionary<string, Glob> GetGlobs()
    {
        return _effectiveGlobs ?? Config.GetCachedGlobs();
    }

    private static Dictionary<string, Glob> BuildGlobs(IEnumerable<SoundGroup> groups)
    {
        var globs = new Dictionary<string, Glob>();
        foreach (var group in groups)
        {
            foreach (var pattern in group.PathPatterns)
            {
                if (!globs.ContainsKey(pattern))
                {
                    globs[pattern] = Glob.Parse(pattern);
                }
            }
        }

        return globs;
    }

    private static string GetScdPath(string soundPath)
    {
        var slash = soundPath.LastIndexOf('/');
        if (slash <= 0)
        {
            return soundPath;
        }

        var suffix = soundPath[(slash + 1)..];
        return int.TryParse(suffix, out _) ? soundPath[..slash] : soundPath;
    }
}
