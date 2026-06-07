using System;
using System.Collections.Generic;
using System.Linq;
using DotNet.Globbing;
using SoundMixer.Api;

namespace SoundMixer;

public class VolumeCalculator
{
    private Configuration Config { get; }
    private Dictionary<string, float> VolumeCache { get; } = new();
    private EffectiveSnapshot? _effectiveSnapshot;
    private Dictionary<string, Glob>? _effectiveGlobs;

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

        return ResolveGroupId(soundPath) != null;
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

    public bool SoundBelongsToGroupTree(string soundPath, string groupId)
    {
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

    private static bool IsDescendantGroup(Configuration config, string childGroupId, string ancestorGroupId)
    {
        var groups = config.Groups;
        var current = groups.FirstOrDefault(g => g.Id == childGroupId);
        while (current?.ParentId != null)
        {
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
        return IsDescendantGroup(Config, childGroupId, ancestorGroupId);
    }

    private string? ResolveGroupId(string soundPath)
    {
        soundPath = ResolveScdPath(soundPath.ToLowerInvariant());

        var groupId = FindGroupForSound(soundPath);
        if (groupId != null)
        {
            return groupId;
        }

        var scdPath = GetScdPath(soundPath);
        if (scdPath != soundPath)
        {
            return FindGroupForSound(scdPath);
        }

        return null;
    }

    public float GetVolumeForSound(string soundPath)
    {
        soundPath = soundPath.ToLowerInvariant();
        if (VolumeCache.TryGetValue(soundPath, out var cachedVolume))
        {
            return cachedVolume;
        }

        var volume = CalculateVolume(soundPath);

        if (Math.Abs(volume - 1.0f) < 0.001f)
        {
            var scdPath = GetScdPath(soundPath);
            if (scdPath != soundPath)
            {
                volume = CalculateVolume(scdPath);
            }
        }

        volume = Configuration.ClampToEngineCap(volume);
        VolumeCache[soundPath] = volume;
        return volume;
    }

    private float CalculateVolume(string soundPath)
    {
        soundPath = soundPath.ToLowerInvariant();

        if (IndividualVolumes.TryGetValue(soundPath, out var individualVolume))
        {
            return individualVolume;
        }

        var groupId = ResolveGroupId(soundPath);
        if (groupId != null)
        {
            return GetGroupVolume(groupId);
        }

        var resolvedPath = ResolveScdPath(soundPath);
        if (resolvedPath != soundPath && IndividualVolumes.TryGetValue(resolvedPath, out individualVolume))
        {
            return individualVolume;
        }

        return 1.0f;
    }

    private string? FindGroupForSound(string soundPath)
    {
        soundPath = soundPath.ToLowerInvariant();

        if (SoundToGroup.TryGetValue(soundPath, out var explicitGroupId))
        {
            return explicitGroupId;
        }

        var candidates = new List<string>();

        foreach (var group in Groups)
        {
            if (group.SoundPaths.Contains(soundPath))
            {
                candidates.Add(group.Id);
            }
        }

        var globs = GetGlobs();
        foreach (var group in Groups)
        {
            foreach (var pattern in group.PathPatterns)
            {
                if (globs.TryGetValue(pattern, out var glob) && glob.IsMatch(soundPath))
                {
                    candidates.Add(group.Id);
                    break;
                }
            }
        }

        return SelectDeepestMatchingGroup(candidates);
    }

    /// <summary>
    /// When multiple groups match the same path (e.g. parent and child Glob patterns),
    /// prefer the deepest node in the group tree.
    /// </summary>
    private string? SelectDeepestMatchingGroup(List<string> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        string? bestId = null;
        var bestDepth = -1;
        var bestIndex = -1;

        foreach (var groupId in candidates)
        {
            var depth = GetGroupDepth(groupId);
            var index = IndexOfGroup(groupId);

            if (depth > bestDepth || (depth == bestDepth && index > bestIndex))
            {
                bestDepth = depth;
                bestIndex = index;
                bestId = groupId;
            }
        }

        return bestId;
    }

    private int GetGroupDepth(string groupId)
    {
        var depth = 0;
        var current = Groups.FirstOrDefault(g => g.Id == groupId);
        while (current?.ParentId != null)
        {
            depth++;
            current = Groups.FirstOrDefault(g => g.Id == current.ParentId);
        }

        return depth;
    }

    private int IndexOfGroup(string groupId)
    {
        for (var i = 0; i < Groups.Count; i++)
        {
            if (Groups[i].Id == groupId)
            {
                return i;
            }
        }

        return -1;
    }

    private float GetGroupVolume(string groupId)
    {
        var group = Groups.FirstOrDefault(g => g.Id == groupId);
        if (group == null)
        {
            return 1.0f;
        }

        var volume = group.GroupVolume;

        if (group.ApplyToChildren && group.ParentId != null)
        {
            volume *= GetGroupVolume(group.ParentId);
        }

        return volume;
    }

    private string ResolveScdPath(string rawPath)
    {
        return PathResolver.ResolveScdPath(Config, rawPath, PathAliases);
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
