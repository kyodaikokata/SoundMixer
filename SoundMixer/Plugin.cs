using System;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using SoundMixer.Api;
using SoundMixer.Localization;
using static SoundMixer.Localization.Loc.Keys;

namespace SoundMixer;

public class Plugin : IDalamudPlugin
{
    public string Name => "SoundMixer";

    internal Configuration Config { get; private set; }
    internal Filter Filter { get; private set; }
    internal VolumeCalculator VolumeCalculator { get; private set; }
    internal PluginUI UI { get; private set; }
    internal SoundMixerApi Api { get; private set; } = null!;

    internal bool IsEffectivelyEnabled => Api?.EffectiveSnapshot.Enabled ?? Config.Enabled;
    internal bool HasTemporaryOverrides => Api?.TemporaryOverrides.HasAnyOverrides ?? false;

    private SoundMixerIpcProviders? _ipcProviders;

    private const string CommandName = "/soundmixer";
    private const string CommandNameShort = "/smix";

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Services>();

        Config = Services.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        if (GroupHierarchy.RepairConfiguration(Config))
        {
            Services.PluginLog.Warning("SoundMixer: repaired corrupted group hierarchy in saved configuration");
            try
            {
                Config.Save();
            }
            catch (Exception ex)
            {
                Services.PluginLog.Error(ex, "SoundMixer: failed to save repaired configuration");
            }
        }

        Loc.Bind(Config);

        VolumeCalculator = new VolumeCalculator(Config);
        Filter = new Filter(this);
        
        // Hook state is applied after Api initialization via RefreshEffectiveState.

        UI = new PluginUI(this);

        var commandHelp = Loc.Get(CommandHelp);
        Services.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = commandHelp
        });

        Services.CommandManager.AddHandler(CommandNameShort, new CommandInfo(OnCommand)
        {
            HelpMessage = commandHelp
        });

        Services.PluginInterface.UiBuilder.Draw += UI.Draw;
        Services.PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
        Services.PluginInterface.UiBuilder.OpenMainUi += OpenConfigUi;
        Services.Framework.Update += OnFrameworkUpdate;

        ApplyDefaultConfiguration();
        MigrateClearGroupIcons();
        MigrateLegacyBuiltinSoundGroupsOnce();
        MigrateVolumeAboveEngineCap();
        MigrateSoundBlacklistEntries();
        MigratePlaySoundDisabledByDefault();
        MigrateRemoveSafeModeSetting();
        PresetManager.Initialize(Config);
        DefaultGroupLocalization.Apply(Config);

        OfficialBlacklistSync.Initialize(this);
        OfficialHookGuardsSync.Initialize(this);

        Api = new SoundMixerApi(this);
        Configuration.OnSaved = () =>
        {
            OfficialBlacklistSync.RebuildFromConfig(Config);
            OfficialHookGuardsSync.RebuildFromConfig(Config);
            ApplyHookGuardState();
            Api.ApplyLiveEffectiveState();
        };
        _ipcProviders = new SoundMixerIpcProviders(Services.PluginInterface, Api);

        DisableDebugManualHookControl(reapplyHooks: false);

        Services.ClientState.Logout += OnLogout;
        Services.ClientState.TerritoryChanged += OnTerritoryChanged;
    }

    internal void RebuildSoundBlacklist()
    {
        OfficialBlacklistSync.RebuildFromConfig(Config);
    }

    internal void RebuildHookGuards()
    {
        OfficialHookGuardsSync.RebuildFromConfig(Config);
        ApplyHookGuardState();
    }

    internal void ApplyHookGuardState()
    {
        Filter.ApplyMountSafeHookState();
    }

    internal bool AddUserBlacklistEntry(SoundBlacklistMatchKind kind, string match, string note)
    {
        if (!SoundBlacklist.TryAddUserEntry(Config, kind, match, note))
        {
            return false;
        }

        Config.Save();
        return true;
    }

    internal void RemoveUserBlacklistEntry(string entryId)
    {
        var entry = Config.UserSoundBlacklist.Find(e => e.Id == entryId);
        if (entry == null)
        {
            return;
        }

        Config.UserSoundBlacklist.Remove(entry);
        Config.Save();
    }

    internal bool AddUserHookGuardEntry(
        HookGuardTrigger trigger,
        IReadOnlyList<HookDebugId> disabledHooks,
        bool skipActiveListScan,
        string note
    )
    {
        if (!HookGuardPolicy.TryAddUserEntry(Config, trigger, disabledHooks, skipActiveListScan, note))
        {
            return false;
        }

        Config.Save();
        return true;
    }

    internal void RemoveUserHookGuardEntry(string entryId)
    {
        var entry = Config.UserHookGuards.Find(e => e.Id == entryId);
        if (entry == null)
        {
            return;
        }

        Config.UserHookGuards.Remove(entry);
        Config.Save();
    }

    internal void SetUserHookGuardEnabled(string entryId, bool enabled)
    {
        var entry = Config.UserHookGuards.Find(e => e.Id == entryId);
        if (entry == null || entry.Enabled == enabled)
        {
            return;
        }

        entry.Enabled = enabled;
        Config.Save();
    }

    internal void SetSavedEnabled(bool enabled, bool refreshSounds = true)
    {
        if (Config.Enabled == enabled)
        {
            ApplyEffectiveHookState();
            return;
        }

        if (!enabled)
        {
            DisableDebugManualHookControl(reapplyHooks: false);
        }

        Config.Enabled = enabled;
        Config.Save();
        ApplyEffectiveHookState();

        // Do not RefreshAllActiveSounds on enable — probing every active node (incl. mount
        // loops such as Guideroid) can native-crash. Hooks apply rules as the game touches sounds.
        _ = refreshSounds;
    }

    internal void DisableDebugManualHookControl(bool reapplyHooks = true)
    {
        if (!Config.HookDebug.ManualControl)
        {
            return;
        }

        Config.HookDebug.ManualControl = false;
        Config.Save();

        if (!reapplyHooks)
        {
            return;
        }

        Filter.ApplyHookDebugSettings();
        ApplyHookGuardState();
    }

    internal void ApplyEffectiveHookState()
    {
        MountTransitionGuard.Update();
        if (IsEffectivelyEnabled)
        {
            Filter.Enable();
        }
        else
        {
            DisableDebugManualHookControl(reapplyHooks: false);
            Filter.Disable();
        }

        Filter.ApplyMountSafeHookState();
    }

    internal bool SwitchActivePreset(string presetId)
    {
        if (PresetManager.FindPreset(Config, presetId) == null)
        {
            return false;
        }

        if (presetId != Config.ActivePresetId)
        {
            PresetManager.SwitchPreset(Config, presetId, Filter);
        }

        ApplyPresetRuntimeState();
        return true;
    }

    internal void ApplyPresetRuntimeState()
    {
        SoundVolumeTracker.Clear();
        StreamingBgmTracker.Clear();
        Api.RefreshEffectiveState(notify: false);
        if (IsEffectivelyEnabled && Filter.CanSafelyRefreshActiveSounds())
        {
            Filter.RefreshAllActiveSounds();
        }
    }

    internal string ResolveSoundPath(string rawPath, nint scdDataPtr = 0)
    {
        var aliases = Api?.EffectiveSnapshot.PathAliases ?? Config.PathAliases;
        return PathResolver.ResolveScdPath(Config, rawPath, aliases, scdDataPtr);
    }

    private void OnLogout(int type, int code)
    {
        MountTransitionGuard.ClearGuideroidSession();
        ZoneTransitionGuard.NotifyTerritoryChanged();
        Filter.ClearTransitionTracking();
        Api.OnLogout();
    }

    private void MigrateSoundBlacklistEntries()
    {
        if (Config.Version >= 6)
        {
            return;
        }

        if (Config.UserSoundBlacklist.Count == 0 && Config.CustomSoundBlacklist.Count > 0)
        {
            foreach (var legacy in Config.CustomSoundBlacklist)
            {
                if (string.IsNullOrWhiteSpace(legacy))
                {
                    continue;
                }

                Config.UserSoundBlacklist.Add(
                    new UserSoundBlacklistEntry
                    {
                        MatchKind = SoundBlacklist.InferMatchKind(legacy),
                        Match = legacy.Trim(),
                        Note = string.Empty,
                    }
                );
            }
        }

        Config.CustomSoundBlacklist.Clear();
        Config.Version = 6;
        Config.Save();
    }

    private void MigratePlaySoundDisabledByDefault()
    {
        if (Config.Version >= 7)
        {
            return;
        }

        if (!Config.HookDebug.ManualControl)
        {
            Config.HookDebug.PlaySound = false;
        }

        Config.Version = 7;
        Config.Save();
        Services.PluginLog.Info("SoundMixer: PlaySound hook disabled by default (enable via Debug → manual hook control)");
    }

    private void MigrateRemoveSafeModeSetting()
    {
        if (Config.Version >= 8)
        {
            return;
        }

        Config.Version = 8;
        Config.Save();
    }

    private void MigrateVolumeAboveEngineCap()
    {
        var changed = false;

        foreach (var group in Config.Groups)
        {
            var clamped = Configuration.ClampToEngineCap(group.GroupVolume);
            if (Math.Abs(clamped - group.GroupVolume) > 0.001f)
            {
                group.GroupVolume = clamped;
                changed = true;
            }
        }

        foreach (var key in Config.IndividualVolumes.Keys.ToList())
        {
            var clamped = Configuration.ClampToEngineCap(Config.IndividualVolumes[key]);
            if (Math.Abs(clamped - Config.IndividualVolumes[key]) > 0.001f)
            {
                Config.IndividualVolumes[key] = clamped;
                changed = true;
            }
        }

        if (changed)
        {
            Config.Save();
            VolumeCalculator.ClearCache();
            Services.PluginLog.Info(
                $"SoundMixer: clamped saved volumes to engine audible cap ({Configuration.EngineAudibleCap * 100:F0}%)"
            );
        }
    }

    private void OnCommand(string command, string args)
    {
        UI.IsVisible = !UI.IsVisible;
    }

    private void OpenConfigUi()
    {
        UI.IsVisible = true;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        MountTransitionGuard.Update();
        Filter.ApplyMountSafeHookState();

        if (ZoneTransitionGuard.ShouldSkipSoundDataMaintenance())
        {
            return;
        }

        Filter.ProcessOneShotVolumeApplies();
        Filter.EnforceTrackedVolumes();
        SoundBlacklist.PruneInactivePointers();
        OneShotPlayRegistry.Prune();
    }

    private void OnTerritoryChanged(uint territoryId)
    {
        ZoneTransitionGuard.NotifyTerritoryChanged();
        Filter.ClearTransitionTracking();
        Services.PluginLog.Debug($"SoundMixer: territory changed ({territoryId}); cleared audio tracking during transition");
    }

    private void ApplyDefaultConfiguration()
    {
        if (Config.Groups.Count > 0 || Config.Presets.Count > 0)
        {
            return;
        }

        if (!DefaultConfigLoader.TryLoad(out var defaults))
        {
            return;
        }

        DefaultConfigLoader.ApplyGroupsAndPresets(Config, defaults);
        DefaultGroupLocalization.Apply(Config);
        Config.Save();
        Services.PluginLog.Info(
            $"SoundMixer: applied embedded default configuration ({Config.Groups.Count} groups)"
        );
    }

    private void MigrateClearGroupIcons()
    {
        var changed = false;
        foreach (var group in Config.Groups)
        {
            if (!string.IsNullOrEmpty(group.Icon))
            {
                group.Icon = string.Empty;
                changed = true;
            }
        }

        if (changed)
        {
            Config.Save();
        }
    }

    /// <summary>
    /// One-time legacy migration for configs older than v4. Built-in group patterns are not
    /// patched on later startups — defaults come only from embedded config on first install.
    /// </summary>
    private void MigrateLegacyBuiltinSoundGroupsOnce()
    {
        if (Config.Version >= 4)
        {
            return;
        }

        MigrateBuiltinSoundGroupsToVersion4();
    }

    private void MigrateBuiltinSoundGroupsToVersion4()
    {
        var changed = false;

        changed |= RemoveDuplicateBgmBuiltinGroups();

        if (!HasGroupWithAnyPattern("**/bgm/**", "**/music/**"))
        {
            Config.Groups.Insert(
                Math.Min(1, Config.Groups.Count),
                new SoundGroup
                {
                    Name = Loc.Get(BuiltinBgm),
                    IsBuiltIn = true,
                    PathPatterns = new() { "**/music/**", "**/bgm/**" },
                }
            );
            changed = true;
        }

        changed |= EnsureBuiltInPatterns(
            g => g.PathPatterns.Contains("**/env/**") || g.PathPatterns.Contains("**/weather/**"),
            "**/ambient/**",
            "**/stream/**"
        );

        Config.Version = 4;
        Config.InvalidateGlobCache();
        Config.Save();
        VolumeCalculator.ClearCache();
        if (changed)
        {
            Services.PluginLog.Info("SoundMixer: migrated built-in BGM/environment groups");
        }
    }

    private bool HasGroupWithAnyPattern(params string[] patterns)
    {
        return Config.Groups.Any(
            g => patterns.Any(pattern => g.PathPatterns.Contains(pattern))
        );
    }

    private bool RemoveDuplicateBgmBuiltinGroups()
    {
        var duplicates = Config.Groups
            .Where(
                g => g.IsBuiltIn
                     && (g.PathPatterns.Contains("**/bgm/**") || g.PathPatterns.Contains("**/music/**"))
            )
            .Skip(1)
            .ToList();

        if (duplicates.Count == 0)
        {
            return false;
        }

        foreach (var group in duplicates)
        {
            RemoveGroupForMigration(group.Id);
        }

        return true;
    }

    private void RemoveGroupForMigration(string groupId)
    {
        var group = GroupHierarchy.FindById(Config, groupId);
        if (group == null)
        {
            return;
        }

        var parentId = group.ParentId;
        foreach (var child in GroupHierarchy.GetChildren(Config, groupId))
        {
            child.ParentId = parentId;
        }

        foreach (var path in group.SoundPaths)
        {
            Config.SoundToGroup.Remove(path);
        }

        foreach (var key in Config.SoundToGroup.Where(kv => kv.Value == groupId).Select(kv => kv.Key).ToList())
        {
            Config.SoundToGroup.Remove(key);
        }

        Config.Groups.Remove(group);
    }

    private bool EnsureBuiltInPatterns(Func<SoundGroup, bool> predicate, params string[] patterns)
    {
        var group = Config.Groups.Find(g => predicate(g));
        if (group == null)
        {
            return false;
        }

        var changed = false;
        foreach (var pattern in patterns)
        {
            if (!group.PathPatterns.Contains(pattern))
            {
                group.PathPatterns.Add(pattern);
                changed = true;
            }
        }

        return changed;
    }

    public void Dispose()
    {
        DisableDebugManualHookControl(reapplyHooks: false);
        _ipcProviders?.NotifyDisposed(Services.PluginInterface);
        _ipcProviders?.Dispose();
        Configuration.OnSaved = null;

        Services.ClientState.Logout -= OnLogout;
        Services.ClientState.TerritoryChanged -= OnTerritoryChanged;
        Services.Framework.Update -= OnFrameworkUpdate;

        if (UI != null)
        {
            Services.PluginInterface.UiBuilder.Draw -= UI.Draw;
            Services.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
            Services.PluginInterface.UiBuilder.OpenMainUi -= OpenConfigUi;
            UI.Dispose();
        }
        
        Filter?.Dispose();

        Services.CommandManager.RemoveHandler(CommandName);
        Services.CommandManager.RemoveHandler(CommandNameShort);
    }
}
