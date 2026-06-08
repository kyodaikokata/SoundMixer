using Newtonsoft.Json;
using System.Linq;
using SoundMixer.Localization;

namespace SoundMixer.Api;

internal sealed class SoundMixerApi
{
    private readonly Plugin _plugin;
    private readonly TemporaryOverrideManager _temporaryOverrides = new();
    private readonly EffectiveRuntimeState _effectiveState = new();

    internal TemporaryOverrideManager TemporaryOverrides => _temporaryOverrides;
    internal EffectiveSnapshot EffectiveSnapshot => _effectiveState.Snapshot;

    internal event Action<string>? StateChanged;

    internal SoundMixerApi(Plugin plugin)
    {
        _plugin = plugin;
        RefreshEffectiveState(notify: false);
    }

    internal void RefreshEffectiveState(bool notify = true, string? reason = null)
    {
        ApplyLiveEffectiveState();
        _plugin.ApplyEffectiveHookState();

        if (notify)
        {
            StateChanged?.Invoke(reason ?? "state");
        }
    }

    /// <summary>
    /// Rebuild runtime volume snapshot from in-memory config without writing disk or touching hooks.
    /// Used while dragging volume sliders for live preview.
    /// </summary>
    internal void ApplyLiveEffectiveState()
    {
        _effectiveState.Rebuild(_plugin.Config, _temporaryOverrides);
        _plugin.VolumeCalculator.SetEffectiveSnapshot(_effectiveState.Snapshot);
    }

    internal int GetApiVersion() => SoundMixerIpcGates.ApiVersion;

    internal bool GetEnabled() => _effectiveState.Snapshot.Enabled;

    internal bool GetSavedEnabled() => _plugin.Config.Enabled;

    internal string[] GetPresetNames()
    {
        return _plugin.Config.Presets.Select(p => p.Name).ToArray();
    }

    internal string GetActivePresetName()
    {
        return _effectiveState.Snapshot.ActivePresetName ?? string.Empty;
    }

    internal string GetSavedActivePresetName()
    {
        return PresetManager.FindPreset(_plugin.Config, _plugin.Config.ActivePresetId)?.Name ?? string.Empty;
    }

    internal string GetGroupsJson()
    {
        var groups = _effectiveState.Snapshot.Groups.Select(group => new IpcGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            ParentId = group.ParentId,
            LabelColorArgb = group.LabelColorArgb,
            Volume = group.GroupVolume,
            EffectiveVolume = ResolveEffectiveGroupVolume(group.Id),
        }).ToList();

        return JsonConvert.SerializeObject(groups);
    }

    internal (SoundMixerApiEc, float) GetGroupVolume(string groupIdOrName)
    {
        var group = ApiResolver.FindGroup(_effectiveState.Snapshot, groupIdOrName);
        if (group == null)
        {
            return (SoundMixerApiEc.NotFound, 1.0f);
        }

        return (SoundMixerApiEc.Success, ResolveEffectiveGroupVolume(group.Id));
    }

    internal SoundMixerApiEc SetEnabled(bool enabled)
    {
        _plugin.SetSavedEnabled(enabled, refreshSounds: true);
        RefreshEffectiveState(reason: "set-enabled");
        return SoundMixerApiEc.Success;
    }

    internal SoundMixerApiEc SwitchPreset(string presetNameOrId)
    {
        var preset = ApiResolver.FindPreset(_plugin.Config, presetNameOrId);
        if (preset == null)
        {
            return SoundMixerApiEc.NotFound;
        }

        PresetManager.SwitchPreset(_plugin.Config, preset.Id, _plugin.Filter);
        _plugin.ApplyPresetRuntimeState();
        return SoundMixerApiEc.Success;
    }

    internal SoundMixerApiEc SetGroupVolume(string groupIdOrName, float volume)
    {
        var group = ApiResolver.FindGroup(_plugin.Config, groupIdOrName);
        if (group == null)
        {
            return SoundMixerApiEc.NotFound;
        }

        var max = _plugin.Config.GetMaxVolume();
        group.GroupVolume = Configuration.ClampToUiRange(volume, max);
        _plugin.Config.Save();
        _plugin.Config.InvalidateGlobCache();
        _plugin.VolumeCalculator.ClearCache();
        if (_plugin.IsEffectivelyEnabled && _plugin.Filter.CanSafelyRefreshActiveSounds())
        {
            _plugin.Filter.RefreshGroupSounds(group.Id);
        }

        RefreshEffectiveState(reason: "set-group-volume");
        return SoundMixerApiEc.Success;
    }

    internal SoundMixerApiEc SetTemporaryEnabled(string tag, int priority, bool enabled)
    {
        if (!ValidateTag(tag, out var ec))
        {
            return ec;
        }

        _temporaryOverrides.SetEnabled(tag, priority, enabled);
        RefreshEffectiveState(reason: $"temp-enabled:{tag}");
        return SoundMixerApiEc.Success;
    }

    internal SoundMixerApiEc ClearTemporaryEnabled(string tag)
    {
        if (!ValidateTag(tag, out var ec))
        {
            return ec;
        }

        _temporaryOverrides.ClearEnabled(tag);
        RefreshEffectiveState(reason: $"temp-enabled-clear:{tag}");
        return SoundMixerApiEc.Success;
    }

    internal SoundMixerApiEc SetTemporaryPreset(string tag, int priority, string presetNameOrId)
    {
        if (!ValidateTag(tag, out var ec))
        {
            return ec;
        }

        var preset = ApiResolver.FindPreset(_plugin.Config, presetNameOrId);
        if (preset == null)
        {
            return SoundMixerApiEc.NotFound;
        }

        _temporaryOverrides.SetPresetId(tag, priority, preset.Id);
        RefreshEffectiveState(reason: $"temp-preset:{tag}");
        return SoundMixerApiEc.Success;
    }

    internal SoundMixerApiEc ClearTemporaryPreset(string tag)
    {
        if (!ValidateTag(tag, out var ec))
        {
            return ec;
        }

        _temporaryOverrides.ClearPreset(tag);
        RefreshEffectiveState(reason: $"temp-preset-clear:{tag}");
        return SoundMixerApiEc.Success;
    }

    internal SoundMixerApiEc SetTemporaryGroupVolume(string tag, int priority, string groupIdOrName, float volume)
    {
        if (!ValidateTag(tag, out var ec))
        {
            return ec;
        }

        var group = ApiResolver.FindGroup(_plugin.Config, groupIdOrName)
                    ?? ApiResolver.FindGroup(_effectiveState.Snapshot, groupIdOrName);
        if (group == null)
        {
            return SoundMixerApiEc.NotFound;
        }

        var max = _plugin.Config.GetMaxVolume();
        _temporaryOverrides.SetGroupVolume(
            tag,
            priority,
            group.Id,
            Configuration.ClampToUiRange(volume, max)
        );
        RefreshEffectiveState(reason: $"temp-group-volume:{tag}");
        return SoundMixerApiEc.Success;
    }

    internal SoundMixerApiEc ClearTemporaryGroupVolume(string tag, string groupIdOrName)
    {
        if (!ValidateTag(tag, out var ec))
        {
            return ec;
        }

        var group = ApiResolver.FindGroup(_plugin.Config, groupIdOrName)
                    ?? ApiResolver.FindGroup(_effectiveState.Snapshot, groupIdOrName);
        if (group == null)
        {
            return SoundMixerApiEc.NotFound;
        }

        _temporaryOverrides.ClearGroupVolume(tag, group.Id);
        RefreshEffectiveState(reason: $"temp-group-volume-clear:{tag}");
        return SoundMixerApiEc.Success;
    }

    internal SoundMixerApiEc RemoveTemporaryOverrides(string tag)
    {
        if (!ValidateTag(tag, out var ec))
        {
            return ec;
        }

        _temporaryOverrides.RemoveTag(tag);
        RefreshEffectiveState(reason: $"temp-remove:{tag}");
        return SoundMixerApiEc.Success;
    }

    internal SoundMixerApiEc RemoveAllTemporaryOverrides()
    {
        _temporaryOverrides.ClearAll();
        RefreshEffectiveState(reason: "temp-remove-all");
        return SoundMixerApiEc.Success;
    }

    internal IReadOnlyList<IpcOverrideSummary> GetTemporaryOverrideSummaries()
    {
        var summaries = new List<IpcOverrideSummary>();
        foreach (var entry in _temporaryOverrides.GetOrderedEntries())
        {
            var summary = new IpcOverrideSummary
            {
                Tag = entry.Tag,
                Priority = entry.Priority,
            };

            if (entry.Enabled.HasValue)
            {
                summary.DetailLines.Add(
                    entry.Enabled.Value
                        ? Loc.Get(Loc.Keys.IpcOverrideEnabledOn)
                        : Loc.Get(Loc.Keys.IpcOverrideEnabledOff)
                );
            }

            if (!string.IsNullOrWhiteSpace(entry.PresetId))
            {
                var presetName = PresetManager.FindPreset(_plugin.Config, entry.PresetId)?.Name ?? entry.PresetId;
                summary.DetailLines.Add(Loc.Format(Loc.Keys.IpcOverridePreset, presetName));
            }

            foreach (var groupId in entry.GroupVolumes.Keys.OrderBy(
                         id => ApiResolver.FindGroup(_plugin.Config, id)?.Name ?? id,
                         StringComparer.OrdinalIgnoreCase
                     ))
            {
                var groupName = ApiResolver.FindGroup(_plugin.Config, groupId)?.Name ?? groupId;
                summary.GroupVolumeLines.Add(new IpcOverrideGroupVolumeLine
                {
                    GroupName = groupName,
                    EffectivePercent = (int)(ResolveEffectiveGroupVolume(groupId) * 100),
                });
            }

            summaries.Add(summary);
        }

        return summaries;
    }

    internal void OnLogout()
    {
        if (!_temporaryOverrides.HasAnyOverrides)
        {
            return;
        }

        _temporaryOverrides.ClearAll();
        RefreshEffectiveState(reason: "logout");
    }

    private static bool ValidateTag(string tag, out SoundMixerApiEc ec)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            ec = SoundMixerApiEc.InvalidTag;
            return false;
        }

        ec = SoundMixerApiEc.Success;
        return true;
    }

    internal bool HasTemporaryGroupVolume(string groupId)
    {
        return _temporaryOverrides.GetOrderedEntries().Any(e => e.GroupVolumes.ContainsKey(groupId));
    }

    internal float GetEffectiveGroupVolume(string groupId) => ResolveEffectiveGroupVolume(groupId);

    private float ResolveEffectiveGroupVolume(string groupId)
    {
        var group = _effectiveState.Snapshot.Groups.FirstOrDefault(g => g.Id == groupId);
        if (group == null)
        {
            return 1.0f;
        }

        var volume = group.GroupVolume;
        if (group.ApplyToChildren && group.ParentId != null)
        {
            volume *= ResolveEffectiveGroupVolume(group.ParentId);
        }

        return Configuration.ClampToEngineCap(volume);
    }
}
