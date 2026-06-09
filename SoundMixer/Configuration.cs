using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using DotNet.Globbing;
using SoundMixer.Localization;

namespace SoundMixer;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 8;

    public bool Enabled { get; set; } = true;
    public bool ExpertMode { get; set; } = false;

    public bool EnableMonitoring { get; set; } = true;

    /// <summary>Per-hook manual enable/disable for CTD tracing (Debug tab).</summary>
    public HookDebugSettings HookDebug { get; set; } = new();
    public const int MonitoringHistorySize = 100;
    public const int MonitoringDisplayCount = 20;
    public bool ShowRecentSounds { get; set; } = true;
    public bool RecentSoundsPanelExpanded { get; set; } = true;
    public bool IpcOverridesPanelExpanded { get; set; } = true;
    public bool HideMatchedMonitoringLogs { get; set; } = false;
    public string MonitoringHideKeywords { get; set; } = "";
    /// <summary>When non-empty, only show recent sounds intercepted by these hook ids.</summary>
    public List<string> MonitoringHookFilter { get; set; } = new();
    public LanguageMode UiLanguage { get; set; } = LanguageMode.System;

    public float? MainWindowX { get; set; }
    public float? MainWindowY { get; set; }
    public float? MainWindowWidth { get; set; }
    public float? MainWindowHeight { get; set; }

    public List<SoundPreset> Presets { get; set; } = new();
    public string? ActivePresetId { get; set; }

    public List<SoundGroup> Groups { get; set; } = new();
    public Dictionary<string, string> SoundToGroup { get; set; } = new();
    public Dictionary<string, float> IndividualVolumes { get; set; } = new();
    public Dictionary<string, string> PathAliases { get; set; } = new();

    /// <summary>Player-managed blacklist entries (editable in the Blacklist tab).</summary>
    public List<UserSoundBlacklistEntry> UserSoundBlacklist { get; set; } = new();

    /// <summary>Legacy one-line rules migrated into <see cref="UserSoundBlacklist"/>.</summary>
    public List<string> CustomSoundBlacklist { get; set; } = new();

    /// <summary>Last synced revision from OfficialSoundBlacklist.json on GitHub.</summary>
    public int OfficialBlacklistRevision { get; set; }

    /// <summary>User-defined action guards (trigger → disabled hooks).</summary>
    public List<UserHookGuardEntry> UserHookGuards { get; set; } = new();

    /// <summary>Last synced revision from OfficialHookGuards.json on GitHub.</summary>
    public int OfficialHookGuardsRevision { get; set; }

    [NonSerialized]
    private Dictionary<string, Glob>? _cachedGlobs;

    [NonSerialized]
    internal static Action? OnSaved;

    public void Save(bool runSavedHandlers = true)
    {
        try
        {
            PresetManager.SyncActivePreset(this);
            Services.PluginInterface.SavePluginConfig(this);
            if (runSavedHandlers)
            {
                OnSaved?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Services.PluginLog.Warning(ex, "SoundMixer: failed to save plugin configuration");
        }
    }

    public const float NormalMaxVolume = 2.0f;

    /// <summary>实测引擎听感上限约 300%–350%，取 350% 作为默认应用钳制值。</summary>
    public const float EngineAudibleCap = 3.5f;

    /// <summary>Debug tab extreme volume mode: up to 10000% linear gain.</summary>
    public const float DebugExtremeMaxVolume = 100f;

    public const float ExpertMaxVolume = EngineAudibleCap;

    public float ResolveEngineCap() =>
        HookDebug.DebugExtremeVolume ? DebugExtremeMaxVolume : EngineAudibleCap;

    public float GetMaxVolume()
    {
        if (HookDebug.DebugExtremeVolume)
        {
            return DebugExtremeMaxVolume;
        }

        return ExpertMode ? ExpertMaxVolume : NormalMaxVolume;
    }

    public static float ClampToUiRange(float volume, float maxVolume) => Math.Clamp(volume, 0f, maxVolume);

    public float ClampVolumeToEngineCap(float volume) => Math.Clamp(volume, 0f, ResolveEngineCap());

    internal Dictionary<string, Glob> GetCachedGlobs()
    {
        if (_cachedGlobs == null)
        {
            _cachedGlobs = new Dictionary<string, Glob>();
            foreach (var group in Groups)
            {
                foreach (var pattern in group.PathPatterns)
                {
                    if (!_cachedGlobs.ContainsKey(pattern))
                    {
                        _cachedGlobs[pattern] = Glob.Parse(pattern);
                    }
                }
            }
        }
        return _cachedGlobs;
    }

    public void InvalidateGlobCache()
    {
        _cachedGlobs = null;
    }
}

[Serializable]
public class SoundGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "New Group";
    /// <summary>Optional note shown in group details (e.g. built-in glob explanations).</summary>
    public string Description { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public float GroupVolume { get; set; } = 1.0f;
    public bool ApplyToChildren { get; set; } = true;
    public List<string> SoundPaths { get; set; } = new();
    public List<string> PathPatterns { get; set; } = new();
    public string Icon { get; set; } = "";
    public bool IsBuiltIn { get; set; } = false;
    public bool IsExpanded { get; set; } = true;

    /// <summary>ARGB label color for root groups. 0 = default theme text color.</summary>
    public uint LabelColorArgb { get; set; }

    /// <summary>Display color for nested groups (tree + monitor log). Set once from parent LabelColorArgb when nested; editable afterward.</summary>
    public uint OverrideColorArgb { get; set; }

    /// <summary>Hide sounds matching this group (and sub-groups) from the live monitor log.</summary>
    public bool HideFromMonitorLog { get; set; }
}

public class SoundInfo
{
    public string Path { get; set; } = "";
    public int Index { get; set; }
    public string Category { get; set; } = "未分类";
    public float Volume { get; set; } = 1.0f;
    public int PlayCount { get; set; }
    public DateTime LastPlayed { get; set; }
    public bool IsPathResolutionFailure { get; set; }
    public string FailureDetail { get; set; } = "";
    public string HookSourceId { get; set; } = "";

    public string FullPath => IsPathResolutionFailure ? FailureDetail : $"{Path}/{Index}";
}
