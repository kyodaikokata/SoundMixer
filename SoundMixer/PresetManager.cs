using System;
using System.Collections.Generic;
using System.Linq;
using SoundMixer.Localization;
using static SoundMixer.Localization.Loc.Keys;

namespace SoundMixer;

[Serializable]
public class SoundPreset
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";

    public List<SoundGroup> Groups { get; set; } = new();
    public Dictionary<string, string> SoundToGroup { get; set; } = new();
    public Dictionary<string, float> IndividualVolumes { get; set; } = new();
    public Dictionary<string, string> PathAliases { get; set; } = new();
}

internal static class PresetManager
{
    internal static void Initialize(Configuration config)
    {
        if (config.Presets.Count == 0)
        {
            Loc.Bind(config);
            var preset = CaptureFromConfig(config, Loc.Get(PresetDefaultName));
            config.Presets.Add(preset);
            config.ActivePresetId = preset.Id;
            config.Save();
            return;
        }

        if (FindPreset(config, config.ActivePresetId) == null)
        {
            config.ActivePresetId = config.Presets[0].Id;
            ApplyToConfig(config, config.Presets[0]);
            config.Save();
        }
        else if (config.Groups.Count == 0)
        {
            ApplyToConfig(config, FindPreset(config, config.ActivePresetId)!);
            config.Save();
        }
    }

    internal static SoundPreset? FindPreset(Configuration config, string? presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
        {
            return null;
        }

        return config.Presets.FirstOrDefault(p => p.Id == presetId);
    }

    internal static SoundPreset CaptureFromConfig(Configuration config, string name)
    {
        EnsureAtLeastOneGroup(config);

        return new SoundPreset
        {
            Name = name,
            Groups = config.Groups.Select(CloneGroup).ToList(),
            SoundToGroup = new Dictionary<string, string>(config.SoundToGroup),
            IndividualVolumes = new Dictionary<string, float>(config.IndividualVolumes),
            PathAliases = new Dictionary<string, string>(config.PathAliases),
        };
    }

    internal static void ApplyToConfig(Configuration config, SoundPreset preset, Filter? filter = null)
    {
        config.Groups = preset.Groups.Select(CloneGroup).ToList();
        config.SoundToGroup = new Dictionary<string, string>(preset.SoundToGroup);
        config.IndividualVolumes = new Dictionary<string, float>(preset.IndividualVolumes);
        config.PathAliases = new Dictionary<string, string>(preset.PathAliases);
        EnsureAtLeastOneGroup(config);
        config.InvalidateGlobCache();
        filter?.ReloadPathAliases();
    }

    internal static void SyncActivePreset(Configuration config)
    {
        var preset = FindPreset(config, config.ActivePresetId);
        if (preset == null)
        {
            return;
        }

        EnsureAtLeastOneGroup(config);
        preset.Groups = config.Groups.Select(CloneGroup).ToList();
        preset.SoundToGroup = new Dictionary<string, string>(config.SoundToGroup);
        preset.IndividualVolumes = new Dictionary<string, float>(config.IndividualVolumes);
        preset.PathAliases = new Dictionary<string, string>(config.PathAliases);
    }

    internal static SoundPreset CreateNew(string name)
    {
        var group = new SoundGroup { Name = Loc.Get(GroupNewDefault) };
        return new SoundPreset
        {
            Name = name,
            Groups = new List<SoundGroup> { group },
        };
    }

    internal static SoundPreset ClonePreset(SoundPreset source, string name)
    {
        return new SoundPreset
        {
            Name = name,
            Groups = source.Groups.Select(CloneGroup).ToList(),
            SoundToGroup = new Dictionary<string, string>(source.SoundToGroup),
            IndividualVolumes = new Dictionary<string, float>(source.IndividualVolumes),
            PathAliases = new Dictionary<string, string>(source.PathAliases),
        };
    }

    internal static bool CanDeletePreset(Configuration config)
    {
        return config.Presets.Count > 1;
    }

    internal static bool TryDeletePreset(Configuration config, string presetId)
    {
        if (!CanDeletePreset(config))
        {
            return false;
        }

        var index = config.Presets.FindIndex(p => p.Id == presetId);
        if (index < 0)
        {
            return false;
        }

        config.Presets.RemoveAt(index);

        if (config.ActivePresetId == presetId)
        {
            config.ActivePresetId = config.Presets[0].Id;
            ApplyToConfig(config, config.Presets[0]);
        }

        return true;
    }

    internal static void SwitchPreset(Configuration config, string presetId, Filter filter)
    {
        var preset = FindPreset(config, presetId);
        if (preset == null)
        {
            return;
        }

        SyncActivePreset(config);
        config.ActivePresetId = presetId;
        ApplyToConfig(config, preset, filter);
        config.Save();
    }

    internal static bool IsNameAvailable(Configuration config, string name, string? excludePresetId = null)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return !config.Presets.Any(
            p => p.Id != excludePresetId
                 && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)
        );
    }

    internal static void EnsureAtLeastOneGroup(Configuration config)
    {
        if (config.Groups.Count > 0)
        {
            return;
        }

        config.Groups.Add(new SoundGroup { Name = Loc.Get(GroupNewDefault) });
    }

    internal static bool CanDeleteGroup(Configuration config)
    {
        return config.Groups.Count > 1;
    }

    private static SoundGroup CloneGroup(SoundGroup group)
    {
        return new SoundGroup
        {
            Id = group.Id,
            Name = group.Name,
            ParentId = group.ParentId,
            GroupVolume = group.GroupVolume,
            ApplyToChildren = group.ApplyToChildren,
            SoundPaths = new List<string>(group.SoundPaths),
            PathPatterns = new List<string>(group.PathPatterns),
            Icon = group.Icon,
            IsBuiltIn = group.IsBuiltIn,
            IsExpanded = group.IsExpanded,
            LabelColorArgb = group.LabelColorArgb,
            HideFromMonitorLog = group.HideFromMonitorLog,
        };
    }
}
