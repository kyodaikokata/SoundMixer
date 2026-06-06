using System;
using System.Collections.Generic;
using System.Linq;

namespace SoundMixer;

public class VolumeCalculator
{
    private Configuration Config { get; }
    private Dictionary<string, float> VolumeCache { get; } = new();

    public VolumeCalculator(Configuration config)
    {
        Config = config;
    }

    public void ClearCache()
    {
        VolumeCache.Clear();
    }

    public string GetDisplayCategory(string soundPath)
    {
        var groupId = ResolveGroupId(soundPath);
        if (groupId != null)
        {
            var groupName = Config.Groups.FirstOrDefault(g => g.Id == groupId)?.Name;
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

        return Config.Groups.FirstOrDefault(g => g.Id == groupId)?.Name;
    }

    public string? GetMatchedGroupId(string soundPath)
    {
        return ResolveGroupId(soundPath);
    }

    public bool HasMatchingRule(string soundPath)
    {
        soundPath = soundPath.ToLowerInvariant();

        if (Config.IndividualVolumes.ContainsKey(soundPath))
        {
            return true;
        }

        var resolvedPath = PathResolver.ResolveScdPath(Config, soundPath);
        if (Config.IndividualVolumes.ContainsKey(resolvedPath))
        {
            return true;
        }

        return ResolveGroupId(soundPath) != null;
    }

    public bool IsHiddenFromMonitorByGroup(string soundPath)
    {
        foreach (var group in Config.Groups)
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
        var current = config.Groups.FirstOrDefault(g => g.Id == childGroupId);
        while (current?.ParentId != null)
        {
            if (current.ParentId == ancestorGroupId)
            {
                return true;
            }

            current = config.Groups.FirstOrDefault(g => g.Id == current.ParentId);
        }

        return false;
    }

    private bool IsDescendantGroup(string childGroupId, string ancestorGroupId)
    {
        return IsDescendantGroup(Config, childGroupId, ancestorGroupId);
    }

    private string? ResolveGroupId(string soundPath)
    {
        soundPath = PathResolver.ResolveScdPath(Config, soundPath.ToLowerInvariant());

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

        if (Config.IndividualVolumes.TryGetValue(soundPath, out var individualVolume))
        {
            return individualVolume;
        }

        var groupId = ResolveGroupId(soundPath);
        if (groupId != null)
        {
            return GetGroupVolume(groupId);
        }

        var resolvedPath = PathResolver.ResolveScdPath(Config, soundPath);
        if (resolvedPath != soundPath && Config.IndividualVolumes.TryGetValue(resolvedPath, out individualVolume))
        {
            return individualVolume;
        }

        return 1.0f;
    }

    private string? FindGroupForSound(string soundPath)
    {
        soundPath = soundPath.ToLowerInvariant();

        if (Config.SoundToGroup.TryGetValue(soundPath, out var groupId))
        {
            return groupId;
        }

        foreach (var group in Config.Groups)
        {
            if (group.SoundPaths.Contains(soundPath))
            {
                return group.Id;
            }
        }

        var globs = Config.GetCachedGlobs();
        foreach (var group in Config.Groups)
        {
            foreach (var pattern in group.PathPatterns)
            {
                if (globs.TryGetValue(pattern, out var glob) && glob.IsMatch(soundPath))
                {
                    return group.Id;
                }
            }
        }

        return null;
    }

    private float GetGroupVolume(string groupId)
    {
        var group = Config.Groups.FirstOrDefault(g => g.Id == groupId);
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
