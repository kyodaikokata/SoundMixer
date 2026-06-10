using System.Collections.Generic;
using System.Linq;

namespace SoundMixer.Api;

/// <summary>Resolved runtime state: saved config + temporary IPC overrides (higher priority wins).</summary>
internal sealed class EffectiveRuntimeState
{
    internal EffectiveSnapshot Snapshot { get; private set; } = new();

    internal void Rebuild(Configuration config, TemporaryOverrideManager overrides)
    {
        var enabled = config.Enabled;
        var preset = PresetManager.FindPreset(config, config.ActivePresetId);

        foreach (var entry in overrides.GetOrderedEntries())
        {
            if (entry.Enabled.HasValue)
            {
                enabled = entry.Enabled.Value;
            }

            if (!string.IsNullOrWhiteSpace(entry.PresetId))
            {
                preset = PresetManager.FindPreset(config, entry.PresetId) ?? preset;
            }
        }

        Snapshot = BuildSnapshot(config, preset, enabled);

        foreach (var entry in overrides.GetOrderedEntries())
        {
            foreach (var (groupId, volume) in entry.GroupVolumes)
            {
                var group = Snapshot.Groups.FirstOrDefault(g => g.Id == groupId);
                if (group != null)
                {
                    group.GroupVolume = config.ClampVolumeToEngineCap(volume);
                }
            }
        }
    }

    private static EffectiveSnapshot BuildSnapshot(Configuration config, SoundPreset? preset, bool enabled)
    {
        if (preset == null)
        {
            return BuildSnapshotFromConfig(config, enabled);
        }

        if (preset.Id == config.ActivePresetId)
        {
            return new EffectiveSnapshot
            {
                Enabled = enabled,
                ActivePresetId = preset.Id,
                ActivePresetName = preset.Name,
                Groups = config.Groups.Select(CloneGroup).ToList(),
                SoundToGroup = new Dictionary<string, string>(config.SoundToGroup),
                IndividualVolumes = new Dictionary<string, float>(config.IndividualVolumes),
                PathAliases = new Dictionary<string, string>(config.PathAliases),
            };
        }

        return new EffectiveSnapshot
        {
            Enabled = enabled,
            ActivePresetId = preset.Id,
            ActivePresetName = preset.Name,
            Groups = preset.Groups.Select(CloneGroup).ToList(),
            SoundToGroup = new Dictionary<string, string>(preset.SoundToGroup),
            IndividualVolumes = new Dictionary<string, float>(preset.IndividualVolumes),
            PathAliases = new Dictionary<string, string>(preset.PathAliases),
        };
    }

    private static EffectiveSnapshot BuildSnapshotFromConfig(Configuration config, bool enabled)
    {
        return new EffectiveSnapshot
        {
            Enabled = enabled,
            ActivePresetId = config.ActivePresetId,
            ActivePresetName = PresetManager.FindPreset(config, config.ActivePresetId)?.Name,
            Groups = config.Groups.Select(CloneGroup).ToList(),
            SoundToGroup = new Dictionary<string, string>(config.SoundToGroup),
            IndividualVolumes = new Dictionary<string, float>(config.IndividualVolumes),
            PathAliases = new Dictionary<string, string>(config.PathAliases),
        };
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
            ScaleByFather = group.ScaleByFather,
            SoundPaths = new List<string>(group.SoundPaths),
            PathPatterns = new List<string>(group.PathPatterns),
            Icon = group.Icon,
            IsBuiltIn = group.IsBuiltIn,
            IsExpanded = group.IsExpanded,
            LabelColorArgb = group.LabelColorArgb,
            OverrideColorArgb = group.OverrideColorArgb,
            HideFromMonitorLog = group.HideFromMonitorLog,
        };
    }
}

internal sealed class EffectiveSnapshot
{
    internal bool Enabled { get; init; }
    internal string? ActivePresetId { get; init; }
    internal string? ActivePresetName { get; init; }
    internal List<SoundGroup> Groups { get; init; } = new();
    internal Dictionary<string, string> SoundToGroup { get; init; } = new();
    internal Dictionary<string, float> IndividualVolumes { get; init; } = new();
    internal Dictionary<string, string> PathAliases { get; init; } = new();
}
