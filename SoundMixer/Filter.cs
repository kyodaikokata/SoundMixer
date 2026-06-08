using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Sound;
using FFXIVClientStructs.FFXIV.Client.System.Resource;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using SoundMixer.GameTypes;
using SoundMixer.Localization;

namespace SoundMixer;

internal unsafe class Filter : IDisposable
{
    private static class Signatures
    {
        internal const string PlaySpecificSound =
            "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 33 F6 8B DA 48 8B F9 0F BA E2 0F";

        internal const string GetResourceSync = "E8 ?? ?? ?? ?? 48 8B C8 8B C3 F0 0F C0 81";
        internal const string GetResourceAsync =
            "E8 ?? ?? ?? 00 48 8B D8 EB ?? F0 FF 83 ?? ?? 00 00";
        internal const string LoadSoundFile = "E8 ?? ?? ?? ?? 48 85 C0 75 12 B0 F6";

        internal const string PlaySound = "E8 ?? ?? ?? ?? 83 FB ?? 41 BF";
        internal const string PlaySystemSound = "E8 ?? ?? ?? ?? 48 0F BE 46 ?? 41 B1";
        internal const string PlayClipSound =
            "E8 ?? ?? ?? ?? 48 85 C0 74 ?? 48 89 43 ?? 48 8B 4B ?? 48 85 C9 74 ?? ?? ?? ?? FF 50 ?? 84 C0 75";
        internal const string PlayMovieSound = "E8 ?? ?? ?? ?? 48 89 43 ?? 48 8D 8D";

        internal const string PlayBGMSound =
            "E8 ?? ?? ?? ?? 48 89 43 ?? 48 8B C8 48 85 C0 74 ?? 80 7B ?? 00";

        internal const string PlayWeatherSound =
            "E8 ?? ?? ?? ?? 48 8D 5F ?? BE ?? ?? ?? ?? ?? ?? ?? 48 85 C9 74 ?? 4C 39 77";
    }

    private const int ResourceDataPointerOffset = 0xB0;

    #region Delegates

    private delegate void* PlaySpecificSoundDelegate(long a1, int idx);

    private delegate SoundData* PlaySoundDelegate(
        SoundManager* self,
        byte* path,
        float volume,
        uint fadeInDuration,
        float posX,
        float posY,
        float posZ,
        float speed,
        int a9,
        uint soundNumber,
        bool autoRelease,
        SoundVolumeCategory volumeCategory,
        bool a13,
        int midiNote,
        bool a15,
        bool defaultFadeOut,
        bool isPositional,
        bool a18
    );

    private delegate SoundData* PlaySystemSoundDelegate(
        SoundManager* self,
        byte* path,
        float volume,
        uint soundNumber,
        uint fadeInDuration,
        bool autoRelease,
        SoundVolumeCategory volumeCategory
    );

    private delegate SoundData* PlayClipSoundDelegate(
        SoundManager* self,
        byte* path,
        float volume,
        uint fadeInDuration,
        float posX,
        float posY,
        float posZ,
        float playbackSpeed,
        int a9,
        uint soundNumber,
        bool autoRelease,
        bool a12
    );

    private delegate SoundData* PlayMovieSoundDelegate(
        SoundManager* self,
        byte* path,
        float volume,
        uint soundNumber,
        uint fadeInDuration,
        bool autoRelease
    );

    private delegate SoundData* PlayBGMSoundDelegate(SoundManager* self, byte* path);

    private delegate void PlayWeatherSoundDelegate(
        SoundManager* self,
        byte* path,
        uint fadeDuration
    );

    private delegate ResourceHandle* GetResourceSyncPrototype(
        ResourceManager* resourceManager,
        ResourceCategory* pCategoryId,
        ResourceType* pResourceType,
        int* pResourceHash,
        byte* pPath,
        GetResourceParameters* pGetResParams,
        nint unk7,
        uint unk8
    );

    private delegate ResourceHandle* GetResourceAsyncPrototype(
        ResourceManager* resourceManager,
        ResourceCategory* pCategoryId,
        ResourceType* pResourceType,
        int* pResourceHash,
        byte* pPath,
        GetResourceParameters* pGetResParams,
        byte isUnknown,
        nint unk8,
        uint unk9
    );

    private delegate nint LoadSoundFileDelegate(nint resourceHandle, uint a2);

    private delegate SoundData* PlayOriginalWithVolumeDelegate(float scaledVolume);

    private delegate void SetVolumeDelegate(SoundData* self, float volume, uint fadeDuration);

    private delegate float GetVolumeDelegate(SoundData* self);

    #endregion

    #region Hooks

    private Hook<PlaySpecificSoundDelegate>? PlaySpecificSoundHook;
    private Hook<PlaySoundDelegate>? PlaySoundHook;
    private Hook<PlaySystemSoundDelegate>? PlaySystemSoundHook;
    private Hook<PlayClipSoundDelegate>? PlayClipSoundHook;
    private Hook<PlayMovieSoundDelegate>? PlayMovieSoundHook;
    private Hook<PlayBGMSoundDelegate>? PlayBGMSoundHook;
    private Hook<PlayWeatherSoundDelegate>? PlayWeatherSoundHook;
    private Hook<SetVolumeDelegate>? SetVolumeHook;
    private Hook<GetVolumeDelegate>? GetVolumeHook;
    private Hook<GetResourceSyncPrototype>? GetResourceSyncHook;
    private Hook<GetResourceAsyncPrototype>? GetResourceAsyncHook;
    private Hook<LoadSoundFileDelegate>? LoadSoundFileHook;

    #endregion

    private Plugin Plugin { get; }
    private ConcurrentDictionary<nint, string> Scds { get; } = new();
    internal ConcurrentQueue<SoundInfo> RecentSounds { get; } = new();
    internal int TrackedScdCount => Scds.Count;

    internal bool PlaySpecificSoundHookActive => PlaySpecificSoundHook != null;
    internal bool PlaySystemSoundHookActive => PlaySystemSoundHook != null;
    internal bool PlayClipSoundHookActive => PlayClipSoundHook != null;
    internal bool PlayMovieSoundHookActive => PlayMovieSoundHook != null;
    internal bool PlayBgmSoundHookActive => PlayBGMSoundHook != null;
    internal bool PlayWeatherSoundHookActive => PlayWeatherSoundHook != null;
    internal bool SetVolumeHookActive => SetVolumeHook != null;
    internal bool GetVolumeHookActive => GetVolumeHook != null;
    internal bool LoadSoundFileHookActive => LoadSoundFileHook != null;
    internal bool GetResourceSyncHookActive => GetResourceSyncHook != null;
    internal bool GetResourceAsyncHookActive => GetResourceAsyncHook != null;

    internal HookRuntimeStatus GetHookRuntimeStatus(HookDebugId id) =>
        id switch
        {
            HookDebugId.PlaySpecificSound => StatusOf(PlaySpecificSoundHook),
            HookDebugId.PlaySound => StatusOf(PlaySoundHook),
            HookDebugId.PlaySystemSound => StatusOf(PlaySystemSoundHook),
            HookDebugId.PlayClipSound => StatusOf(PlayClipSoundHook),
            HookDebugId.PlayMovieSound => StatusOf(PlayMovieSoundHook),
            HookDebugId.PlayBgmSound => StatusOf(PlayBGMSoundHook),
            HookDebugId.PlayWeatherSound => StatusOf(PlayWeatherSoundHook),
            HookDebugId.SetVolume => StatusOf(SetVolumeHook),
            HookDebugId.GetVolume => StatusOf(GetVolumeHook),
            HookDebugId.LoadSoundFile => StatusOf(LoadSoundFileHook),
            HookDebugId.GetResourceSync => StatusOf(GetResourceSyncHook),
            HookDebugId.GetResourceAsync => StatusOf(GetResourceAsyncHook),
            _ => default,
        };

    internal void ApplyHookDebugSettings()
    {
        if (!Plugin.IsEffectivelyEnabled)
        {
            return;
        }

        EnsurePlaySoundHookLoaded();
        ApplyDesiredHookStates();
    }

    private bool ShouldLoadPlaySoundHook()
    {
        var dbg = Plugin.Config.HookDebug;
        return dbg.ManualControl && dbg.PlaySound;
    }

    private void EnsurePlaySoundHookLoaded(List<string>? unavailableHooks = null)
    {
        if (ShouldLoadPlaySoundHook())
        {
            var unavailable = unavailableHooks ?? new List<string>();
            TryCreateSoundManagerHook(
                nameof(SoundManager.PlaySound),
                Signatures.PlaySound,
                PlaySoundDetour,
                ref PlaySoundHook,
                unavailable
            );
            return;
        }

        if (PlaySoundHook == null)
        {
            return;
        }

        PlaySoundHook.Dispose();
        PlaySoundHook = null;
    }

    private bool SoundDataHooksInstalled { get; set; }
    private bool HooksSuppressedForMount { get; set; }
    private bool HooksSuppressedVolumeForGuideroid { get; set; }

    internal bool IsMountLoopSafetyActive => ShouldSkipActiveSoundListScanning();

    internal bool IsHookGuardBlockingActiveListScan => ShouldSkipActiveSoundListScanning();

    [ThreadStatic]
    private static int s_setVolumeDetourDepth;

    [ThreadStatic]
    private static bool s_directEffectiveVolumeApply;

    private readonly ConcurrentDictionary<nint, string> KnownActiveMonitoredSounds = new();
    private readonly ConcurrentDictionary<nint, DateTime> _pathFailureThrottle = new();
    private DateTime _lastActiveScanUtc;

    private const double PathFailureThrottleSeconds = 8.0;

    internal Filter(Plugin plugin)
    {
        Plugin = plugin;
        ApplySavedPathAliases();
    }

    internal void RegisterPathAlias(string fromPath, string toPath)
    {
        fromPath = fromPath.ToLowerInvariant().Trim();
        toPath = toPath.ToLowerInvariant().Trim();
        if (string.IsNullOrWhiteSpace(fromPath) || string.IsNullOrWhiteSpace(toPath))
        {
            return;
        }

        Plugin.Config.PathAliases[fromPath] = toPath;
        ApplyPathAliasToRuntimeCache(fromPath, toPath);

        Plugin.Config.Save();
        Plugin.VolumeCalculator.ClearCache();
        Services.PluginLog.Info($"SoundMixer: path alias {fromPath} -> {toPath}");
    }

    internal void RemovePathAlias(string fromPath)
    {
        fromPath = fromPath.ToLowerInvariant().Trim();
        if (Plugin.Config.PathAliases.Remove(fromPath))
        {
            Plugin.Config.Save();
            Plugin.VolumeCalculator.ClearCache();
        }
    }

    private void ApplySavedPathAliases()
    {
        foreach (var (fromPath, toPath) in Plugin.Config.PathAliases)
        {
            ApplyPathAliasToRuntimeCache(fromPath, toPath);
        }
    }

    internal void ReloadPathAliases()
    {
        foreach (var fromPath in Plugin.Config.PathAliases.Keys)
        {
            if (PathResolver.TryParseUnknownPointer(fromPath, out var pointer))
            {
                Scds.TryRemove(pointer, out _);
                continue;
            }

            var scdPath = GetScdPathFromFullPath(fromPath);
            if (PathResolver.TryParseUnknownPointer(scdPath, out pointer))
            {
                Scds.TryRemove(pointer, out _);
            }
        }

        ApplySavedPathAliases();
        Plugin.VolumeCalculator.ClearCache();
    }

    private void ApplyPathAliasToRuntimeCache(string fromPath, string toPath)
    {
        if (PathResolver.TryParseUnknownPointer(fromPath, out var pointer))
        {
            Scds[pointer] = toPath.ToLowerInvariant();
            return;
        }

        var scdPath = GetScdPathFromFullPath(fromPath);
        if (PathResolver.TryParseUnknownPointer(scdPath, out pointer))
        {
            Scds[pointer] = toPath.ToLowerInvariant();
        }
    }

    private static string GetScdPathFromFullPath(string fullPath)
    {
        var slash = fullPath.LastIndexOf('/');
        return slash > 0 ? fullPath[..slash] : fullPath;
    }

    internal void Enable()
    {
        MountTransitionGuard.Update();

        if (PlaySpecificSoundHook != null)
        {
            ApplyMountSafeHookState();
            return;
        }

        var unavailableHooks = new List<string>();

        if (Services.SigScanner.TryScanText(Signatures.PlaySpecificSound, out var playPtr))
        {
            PlaySpecificSoundHook =
                Services.GameInteropProvider.HookFromAddress<PlaySpecificSoundDelegate>(
                    playPtr,
                    PlaySpecificSoundDetour
                );
        }

        EnsurePlaySoundHookLoaded(unavailableHooks);
        TryCreateSoundManagerHook(nameof(SoundManager.PlaySystemSound), Signatures.PlaySystemSound, PlaySystemSoundDetour, ref PlaySystemSoundHook, unavailableHooks);
        TryCreateSoundManagerHook(nameof(SoundManager.PlayClipSound), Signatures.PlayClipSound, PlayClipSoundDetour, ref PlayClipSoundHook, unavailableHooks);
        TryCreateSoundManagerHook(nameof(SoundManager.PlayMovieSound), Signatures.PlayMovieSound, PlayMovieSoundDetour, ref PlayMovieSoundHook, unavailableHooks);
        TryCreateSoundManagerHook(nameof(SoundManager.PlayBGMSound), Signatures.PlayBGMSound, PlayBGMSoundDetour, ref PlayBGMSoundHook, unavailableHooks);
        TryCreateSoundManagerHook(nameof(SoundManager.PlayWeatherSound), Signatures.PlayWeatherSound, PlayWeatherSoundDetour, ref PlayWeatherSoundHook, unavailableHooks);

        if (unavailableHooks.Count > 0)
        {
            Services.PluginLog.Info(
                "SoundMixer: "
                    + $"{unavailableHooks.Count} SoundManager play hook(s) unavailable on this client build "
                    + $"({string.Join(", ", unavailableHooks)}). "
                    + "Per-path volume still works via PlaySpecificSound and SetVolume enforcement; "
                    + "BGM/weather rely on active-sound scanning when play hooks are missing."
            );
        }

        if (
            GetResourceSyncHook == null
            && Services.SigScanner.TryScanText(Signatures.GetResourceSync, out var syncPtr)
        )
        {
            GetResourceSyncHook =
                Services.GameInteropProvider.HookFromAddress<GetResourceSyncPrototype>(
                    syncPtr,
                    GetResourceSyncDetour
                );
        }

        if (
            GetResourceAsyncHook == null
            && Services.SigScanner.TryScanText(Signatures.GetResourceAsync, out var asyncPtr)
        )
        {
            GetResourceAsyncHook =
                Services.GameInteropProvider.HookFromAddress<GetResourceAsyncPrototype>(
                    asyncPtr,
                    GetResourceAsyncDetour
                );
        }

        if (
            LoadSoundFileHook == null
            && Services.SigScanner.TryScanText(Signatures.LoadSoundFile, out var soundPtr)
        )
        {
            LoadSoundFileHook = Services.GameInteropProvider.HookFromAddress<LoadSoundFileDelegate>(
                soundPtr,
                LoadSoundFileDetour
            );
        }

        TryInstallSetVolumeHook();
        ApplyMountSafeHookState();

        Services.PluginLog.Debug(
            $"SoundMixer: hooks installed (PlaySpecific={PlaySpecificSoundHook != null}, "
                + $"PlaySound={PlaySoundHook != null}, PlaySystem={PlaySystemSoundHook != null}, "
                + $"PlayClip={PlayClipSoundHook != null}, PlayMovie={PlayMovieSoundHook != null}, "
                + $"PlayBGM={PlayBGMSoundHook != null}, PlayWeather={PlayWeatherSoundHook != null}, "
                + $"SetVolume={SetVolumeHook != null}, GetVolume={GetVolumeHook != null})"
        );
    }

    private bool ShouldBypassVolumeInterception()
    {
        return !Plugin.IsEffectivelyEnabled;
    }

    private bool ShouldBypassSoundDataVolumeHooks()
    {
        return ShouldBypassVolumeInterception();
    }

    /// <summary>
    /// Skip walking ActiveSoundData linked lists when official/user hook guards require it.
    /// </summary>
    private bool ShouldSkipActiveSoundListScanning()
    {
        return HookGuardPolicy.ShouldSkipActiveListScan(Plugin.Config);
    }

    private bool ShouldSkipActiveSoundListProcessing() => ShouldSkipActiveSoundListScanning();

    internal bool CanSafelyRefreshActiveSounds()
    {
        return !ShouldSkipActiveSoundListScanning();
    }

    private static bool ShouldBypassPath(string? path)
    {
        return SoundBlacklist.IsPlayHookBlockedPath(path);
    }

    private static bool ShouldBypassSoundData(SoundData* soundData)
    {
        return SoundBlacklist.ShouldBypassSoundData(soundData);
    }

    internal void ApplyMountSafeHookState()
    {
        if (!Plugin.IsEffectivelyEnabled)
        {
            ClearMountHookSuppression();
            return;
        }

        if (HooksSuppressedForMount)
        {
            HooksSuppressedForMount = false;
            ResumeAllHooksRuntime();
            Services.PluginLog.Info("SoundMixer: audio hooks resumed after mount transition");
        }

        var volumeHooksBlocked = HookGuardPolicy.ShouldDisableHook(HookDebugId.SetVolume, Plugin.Config)
            || HookGuardPolicy.ShouldDisableHook(HookDebugId.GetVolume, Plugin.Config);

        if (volumeHooksBlocked)
        {
            if (!HooksSuppressedVolumeForGuideroid)
            {
                HooksSuppressedVolumeForGuideroid = true;
                SetHookEnabled(SetVolumeHook, false);
                SetHookEnabled(GetVolumeHook, false);
                Services.PluginLog.Info("SoundMixer: SetVolume/GetVolume hooks suspended by action guard");
            }

            EnsureRuntimeHooksEnabled();
            return;
        }

        if (HooksSuppressedVolumeForGuideroid)
        {
            HooksSuppressedVolumeForGuideroid = false;
            Services.PluginLog.Info("SoundMixer: SetVolume/GetVolume hooks resumed after action guard");
        }

        EnsureRuntimeHooksEnabled();
    }

    private void EnsureRuntimeHooksEnabled()
    {
        if (HooksSuppressedForMount)
        {
            return;
        }

        ApplyDesiredHookStates();
    }

    private void ApplyDesiredHookStates()
    {
        var dbg = Plugin.Config.HookDebug;
        var manual = dbg.ManualControl && Plugin.IsEffectivelyEnabled;

        bool Desired(HookDebugId id, bool autoDefault = true)
        {
            if (manual)
            {
                return dbg.GetDesired(id);
            }

            if (HookGuardPolicy.ShouldDisableHook(id, Plugin.Config))
            {
                return false;
            }

            return autoDefault;
        }

        SetHookEnabled(PlaySpecificSoundHook, Desired(HookDebugId.PlaySpecificSound));
        SetHookEnabled(PlaySoundHook, Desired(HookDebugId.PlaySound, autoDefault: false));
        SetHookEnabled(PlaySystemSoundHook, Desired(HookDebugId.PlaySystemSound));
        SetHookEnabled(PlayClipSoundHook, Desired(HookDebugId.PlayClipSound));
        SetHookEnabled(PlayMovieSoundHook, Desired(HookDebugId.PlayMovieSound));
        SetHookEnabled(PlayBGMSoundHook, Desired(HookDebugId.PlayBgmSound));
        SetHookEnabled(PlayWeatherSoundHook, Desired(HookDebugId.PlayWeatherSound));
        SetHookEnabled(LoadSoundFileHook, Desired(HookDebugId.LoadSoundFile));
        SetHookEnabled(GetResourceSyncHook, Desired(HookDebugId.GetResourceSync));
        SetHookEnabled(GetResourceAsyncHook, Desired(HookDebugId.GetResourceAsync));
        SetHookEnabled(SetVolumeHook, Desired(HookDebugId.SetVolume));
        SetHookEnabled(GetVolumeHook, Desired(HookDebugId.GetVolume));
    }

    private static HookRuntimeStatus StatusOf<T>(Hook<T>? hook) where T : Delegate
    {
        if (hook == null)
        {
            return default;
        }

        return new HookRuntimeStatus
        {
            IsResolved = true,
            IsRuntimeEnabled = hook.IsEnabled,
        };
    }

    private static void SetHookEnabled<T>(Hook<T>? hook, bool enabled) where T : Delegate
    {
        if (hook == null)
        {
            return;
        }

        if (enabled)
        {
            hook.Enable();
        }
        else
        {
            hook.Disable();
        }
    }

    private void ClearMountHookSuppression()
    {
        if (HooksSuppressedForMount)
        {
            HooksSuppressedForMount = false;
            ResumeAllHooksRuntime();
        }

        HooksSuppressedVolumeForGuideroid = false;
    }

    private void SuspendAllHooksRuntime()
    {
        PlaySpecificSoundHook?.Disable();
        PlaySoundHook?.Disable();
        PlaySystemSoundHook?.Disable();
        PlayClipSoundHook?.Disable();
        PlayMovieSoundHook?.Disable();
        PlayBGMSoundHook?.Disable();
        PlayWeatherSoundHook?.Disable();
        SetVolumeHook?.Disable();
        GetVolumeHook?.Disable();
        LoadSoundFileHook?.Disable();
        GetResourceSyncHook?.Disable();
        GetResourceAsyncHook?.Disable();
    }

    private void ResumeAllHooksRuntime()
    {
        TryInstallSetVolumeHook();
        EnsureRuntimeHooksEnabled();
    }

    internal void EnforceTrackedVolumes()
    {
        try
        {
            var soundManager = SoundManager.Instance();
            if (soundManager == null)
            {
                return;
            }

            if (Plugin.IsEffectivelyEnabled)
            {
                SoundVolumeTracker.EnforceAllTracked(Plugin.VolumeCalculator, ApplyRefreshedVolume);

                if (!ShouldSkipActiveSoundListProcessing())
                {
                    EnforceTrackedVolumesInList(soundManager->ActiveSoundDataListHead);
                    EnforceTrackedSound(soundManager->WeatherSoundData);
                }
            }

            if (!ShouldSkipActiveSoundListProcessing())
            {
                ScanActiveSoundsForMonitoring(soundManager);
            }

            SoundVolumeTracker.PruneInactive();
            if (!ShouldSkipActiveSoundListProcessing())
            {
                PruneInactiveMonitoredSounds(soundManager);
            }
        }
        catch (Exception ex)
        {
            Services.PluginLog.Error(ex, "SoundMixer: EnforceTrackedVolumes failed");
        }
    }

    private void EnforceTrackedSound(SoundData* soundData)
    {
        if (soundData == null)
        {
            return;
        }

        SoundVolumeTracker.EnforceActiveSound(soundData, Plugin.VolumeCalculator, ApplyRefreshedVolume);
    }

    private void EnforceTrackedVolumesInList(SoundData* listHead)
    {
        SoundDataSafety.VisitSoundList(
            listHead,
            soundData =>
            {
                if (ShouldBypassSoundData(soundData))
                {
                    return true;
                }

                SoundVolumeTracker.EnforceActiveSound(
                    soundData,
                    Plugin.VolumeCalculator,
                    ApplyRefreshedVolume
                );
                return true;
            },
            listName: "active-enforce"
        );
    }

    /// <summary>
    /// Drop in-memory tracking/caches and restore pooled SoundData volumes when possible.
    /// Does not change saved config; use after UI/pool glitches instead of restarting the game.
    /// </summary>
    internal (int Released, int Refreshed) ClearRuntimeCache()
    {
        var released = SoundVolumeTracker.ReleaseAllTracked(restoreVolumes: true);
        OneShotPlayRegistry.Clear();
        StreamingBgmTracker.Clear();
        SoundBlacklist.ClearPointerCache();
        _pathFailureThrottle.Clear();
        Plugin.VolumeCalculator.ClearCache();
        Plugin.Api.ApplyLiveEffectiveState();

        var restoredPool = 0;
        var soundManager = SoundManager.Instance();
        if (soundManager != null && !ShouldSkipActiveSoundListScanning())
        {
            restoredPool += SoundVolumeTracker.RestoreAllInactivePoolVolumes(
                soundManager->InactiveSoundDataListHead
            );
        }

        var refreshed = Plugin.IsEffectivelyEnabled ? RefreshAllActiveSounds() : 0;
        Services.PluginLog.Info(
            $"SoundMixer: cleared runtime cache (released {released} tracked, restored {restoredPool} pooled UI nodes, refreshed {refreshed} active)"
        );
        return (released, refreshed);
    }

    internal int RefreshAllActiveSounds()
    {
        if (!Plugin.IsEffectivelyEnabled)
        {
            return 0;
        }

        Plugin.VolumeCalculator.ClearCache();
        TryInstallSetVolumeHook();

        var soundManager = SoundManager.Instance();
        if (soundManager == null)
        {
            return 0;
        }

        var refreshed = 0;
        if (!ShouldSkipActiveSoundListScanning())
        {
            refreshed += RefreshSoundsInList(soundManager->ActiveSoundDataListHead);
            refreshed += RefreshSoundsInList(soundManager->InactiveSoundDataListHead);
        }

        refreshed += RefreshWeatherSoundIfPresent(soundManager);
        refreshed += SoundVolumeTracker.RefreshAllTracked(Plugin.VolumeCalculator, ApplyRefreshedVolume);

        Services.PluginLog.Info($"SoundMixer: refreshed {refreshed} active sounds");
        return refreshed;
    }

    internal int RefreshGroupSounds(string groupId)
    {
        if (!Plugin.IsEffectivelyEnabled || string.IsNullOrWhiteSpace(groupId))
        {
            return 0;
        }

        Plugin.VolumeCalculator.ClearCache();

        if (Plugin.VolumeCalculator.IsUiGroup(groupId))
        {
            var released = SoundVolumeTracker.ReleaseTrackedForGroup(
                groupId,
                Plugin.VolumeCalculator,
                SoundBelongsToResolvedPath
            );
            Services.PluginLog.Info(
                $"SoundMixer: UI group refresh released {released} tracked nodes (skipped list scan)"
            );
            return released;
        }

        TryInstallSetVolumeHook();

        var soundManager = SoundManager.Instance();
        if (soundManager == null)
        {
            return 0;
        }

        var refreshed = 0;
        if (!ShouldSkipActiveSoundListScanning())
        {
            refreshed += RefreshGroupSoundsInList(soundManager->ActiveSoundDataListHead, groupId);
            refreshed += RefreshGroupSoundsInList(soundManager->InactiveSoundDataListHead, groupId);
        }

        if (soundManager->WeatherSoundData != null
            && SoundDataSafety.IsReadable((nint)soundManager->WeatherSoundData)
            && SoundBelongsToGroup(soundManager->WeatherSoundData, groupId)
            && SoundVolumeTracker.ForceRefreshActiveSound(
                soundManager->WeatherSoundData,
                Plugin.VolumeCalculator,
                ApplyRefreshedVolume
            ))
        {
            refreshed++;
        }

        refreshed += SoundVolumeTracker.RefreshTrackedSoundsForGroup(
            groupId,
            Plugin.VolumeCalculator,
            SoundBelongsToResolvedPath,
            ApplyRefreshedVolume
        );

        Services.PluginLog.Info($"SoundMixer: refreshed {refreshed} sounds for group {groupId}");
        return refreshed;
    }

    private int RefreshWeatherSoundIfPresent(SoundManager* soundManager)
    {
        if (soundManager->WeatherSoundData == null
            || !SoundDataSafety.IsReadable((nint)soundManager->WeatherSoundData))
        {
            return 0;
        }

        return SoundVolumeTracker.ForceRefreshActiveSound(
            soundManager->WeatherSoundData,
            Plugin.VolumeCalculator,
            ApplyRefreshedVolume
        )
            ? 1
            : 0;
    }

    private int RefreshSoundsInList(SoundData* listHead)
    {
        var refreshed = 0;

        SoundDataSafety.VisitSoundList(
            listHead,
            soundData =>
            {
                if (ShouldBypassSoundData(soundData))
                {
                    return true;
                }

                if (SoundVolumeTracker.ForceRefreshActiveSound(
                        soundData,
                        Plugin.VolumeCalculator,
                        ApplyRefreshedVolume
                    ))
                {
                    refreshed++;
                }

                return true;
            },
            listName: "refresh"
        );

        return refreshed;
    }

    private int RefreshGroupSoundsInList(SoundData* listHead, string groupId)
    {
        var refreshed = 0;

        SoundDataSafety.VisitSoundList(
            listHead,
            soundData =>
            {
                if (ShouldBypassSoundData(soundData))
                {
                    return true;
                }

                if (!SoundDataSafety.TryReadSoundData(soundData, out var isActive, out var soundNumber, out _)
                    || !isActive)
                {
                    return true;
                }

                if (!TryResolveSoundPath(soundData, out var resolvedPath, "refresh")
                    || !SoundBelongsToResolvedPath(resolvedPath, (int)soundNumber, groupId))
                {
                    return true;
                }

                if (SoundVolumeTracker.ForceRefreshActiveSound(
                        soundData,
                        Plugin.VolumeCalculator,
                        ApplyRefreshedVolume,
                        resolvedPath
                    ))
                {
                    refreshed++;
                }

                return true;
            },
            listName: "refresh-group"
        );

        return refreshed;
    }

    private bool SoundBelongsToGroup(SoundData* soundData, string groupId)
    {
        if (ShouldBypassSoundData(soundData))
        {
            return false;
        }

        if (!SoundDataSafety.TryReadSoundData(soundData, out var isActive, out var soundNumber, out _))
        {
            return false;
        }

        if (!isActive)
        {
            return false;
        }

        if (!TryResolveSoundPath(soundData, out var path)
            || string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return SoundBelongsToResolvedPath(path, (int)soundNumber, groupId);
    }

    private bool SoundBelongsToResolvedPath(string path, int soundNumber, string groupId)
    {
        var resolvedPath = Plugin.ResolveSoundPath(path);
        var specificPath = PathResolver.BuildSpecificPath(resolvedPath, soundNumber);
        return Plugin.VolumeCalculator.SoundBelongsToGroupTree(specificPath, groupId)
               || Plugin.VolumeCalculator.SoundBelongsToGroupTree(resolvedPath, groupId);
    }

    /// <summary>
    /// Resolve SCD path without calling SoundData.GetFileName (unsafe on streaming nodes).
    /// </summary>
    private bool TryResolveSafeSoundPath(SoundData* soundData, out string path, out uint soundNumber)
    {
        path = string.Empty;
        soundNumber = 0;

        if (soundData == null || ShouldBypassSoundData(soundData))
        {
            return false;
        }

        if (SoundVolumeTracker.TryGetTrackedPath(soundData, out path))
        {
            if (SoundDataSafety.TryReadSoundData(soundData, out _, out soundNumber, out _))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(path);
        }

        if (!SoundDataSafety.IsReadable((nint)soundData))
        {
            return false;
        }

        if (!SoundDataSafety.TryReadSoundData(soundData, out _, out soundNumber, out _))
        {
            return false;
        }

        try
        {
            var handle = soundData->SoundResourceHandle;
            if (handle != null && SoundDataSafety.IsReadable((nint)handle, 0x40))
            {
                var name = handle->FileName.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    path = name.ToLowerInvariant();
                    return true;
                }

                if (SoundDataSafety.IsReadable((nint)handle, ResourceDataPointerOffset + IntPtr.Size))
                {
                    var scdData = Marshal.ReadIntPtr((nint)handle + ResourceDataPointerOffset);
                    if (scdData != nint.Zero
                        && Scds.TryGetValue(scdData, out var cachedPath)
                        && !string.IsNullOrWhiteSpace(cachedPath))
                    {
                        path = cachedPath.ToLowerInvariant();
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Services.PluginLog.Verbose(ex, "SoundMixer: failed to resolve safe path from SoundData");
        }

        if (StreamingBgmTracker.TryResolvePendingPath(soundData, out path))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Resolve SCD path. Safe Mode forbids GetFileName fallback; failures can be reported to the monitor log.
    /// </summary>
    internal bool TryResolveSoundPath(SoundData* soundData, out string path, string? reportContext = null)
    {
        path = string.Empty;
        if (soundData == null || ShouldBypassSoundData(soundData))
        {
            return false;
        }

        if (TryResolveSafeSoundPath(soundData, out path, out var soundNumber))
        {
            return true;
        }

        if (!Plugin.Config.SafeMode && SoundVolumeHelper.TryGetUnsafeFileName(soundData, out path))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(reportContext))
        {
            ReportPathResolutionFailure(reportContext, soundData, soundNumber);
        }

        return false;
    }

    private void ReportPathResolutionFailure(string context, SoundData* soundData, uint soundNumber)
    {
        if (!Plugin.Config.EnableMonitoring || soundData == null)
        {
            return;
        }

        var ptr = (nint)soundData;
        var now = DateTime.UtcNow;
        if (_pathFailureThrottle.TryGetValue(ptr, out var lastReported)
            && (now - lastReported).TotalSeconds < PathFailureThrottleSeconds)
        {
            return;
        }

        _pathFailureThrottle[ptr] = now;

        var hasTracked = SoundVolumeTracker.TryGetTrackedPath(soundData, out var trackedPath);
        var hasHandleName = false;
        var scdCache = "-";
        try
        {
            var handle = soundData->SoundResourceHandle;
            if (handle != null && SoundDataSafety.IsReadable((nint)handle, 0x40))
            {
                hasHandleName = !string.IsNullOrWhiteSpace(handle->FileName.ToString());
                if (SoundDataSafety.IsReadable((nint)handle, ResourceDataPointerOffset + IntPtr.Size))
                {
                    var scdPtr = Marshal.ReadIntPtr((nint)handle + ResourceDataPointerOffset);
                    if (scdPtr != nint.Zero && Scds.TryGetValue(scdPtr, out var cachedPath))
                    {
                        scdCache = $"{scdPtr:X}={cachedPath}";
                    }
                    else if (scdPtr != nint.Zero)
                    {
                        scdCache = $"{scdPtr:X}=miss";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Services.PluginLog.Verbose(ex, "SoundMixer: failed to collect path-resolution diagnostics");
        }

        if (soundNumber == 0)
        {
            SoundDataSafety.TryReadSoundData(soundData, out _, out soundNumber, out _);
        }

        SoundDataSafety.TryReadSoundData(soundData, out var isActive, out _, out _);

        var detail = Loc.Format(
            Loc.Keys.MonitorPathResolveFailedDetail,
            context,
            ptr.ToString("X"),
            soundNumber,
            isActive ? "yes" : "no",
            hasTracked ? trackedPath : "-",
            hasHandleName ? "yes" : "no",
            scdCache,
            Plugin.Config.SafeMode ? "on" : "off"
        );

        RecentSounds.Enqueue(new SoundInfo
        {
            Path = Loc.Get(Loc.Keys.MonitorPathResolveFailed),
            Index = (int)soundNumber,
            Category = detail,
            Volume = 0f,
            LastPlayed = DateTime.Now,
            PlayCount = 0,
            IsPathResolutionFailure = true,
            FailureDetail = detail,
            HookSourceId = SoundMonitorHookIds.PathResolve,
        });

        while (RecentSounds.Count > Configuration.MonitoringHistorySize)
        {
            RecentSounds.TryDequeue(out _);
        }
    }

    private static void RegisterBlockedPlayResultFromSpecificSound(void* bypassResult, string? path)
    {
        if (bypassResult == null)
        {
            return;
        }

        var soundData = (SoundData*)bypassResult;
        if (SoundDataSafety.IsReadable((nint)soundData))
        {
            SoundBlacklist.RegisterBlockedPlayResult(soundData, path);
        }
    }

    private void ApplyRefreshedVolume(SoundData* soundData, float effectiveVolume)
    {
        if (!SoundVolumeTracker.ShouldAllowForceRefresh(soundData))
        {
            return;
        }

        if (SetVolumeHook != null
            && SoundDataSafety.IsValidForHook(soundData)
            && !ShouldBypassSoundDataVolumeHooks()
            && !SoundBlacklist.IsPlayBypassActive
            && !ShouldBypassSoundData(soundData))
        {
            s_directEffectiveVolumeApply = true;
            try
            {
                SetVolumeHook.Original(soundData, effectiveVolume, 0);
            }
            catch (Exception ex)
            {
                Services.PluginLog.Verbose(ex, "SoundMixer: SetVolume refresh failed, falling back to field write");
                SoundVolumeTracker.ApplyFieldVolume(soundData, effectiveVolume);
            }
            finally
            {
                s_directEffectiveVolumeApply = false;
            }

            return;
        }

        SoundVolumeTracker.ApplyFieldVolume(soundData, effectiveVolume);
    }

    internal void Disable()
    {
        PlaySpecificSoundHook?.Disable();
        PlaySoundHook?.Disable();
        PlaySystemSoundHook?.Disable();
        PlayClipSoundHook?.Disable();
        PlayMovieSoundHook?.Disable();
        PlayBGMSoundHook?.Disable();
        PlayWeatherSoundHook?.Disable();
        SetVolumeHook?.Disable();
        GetVolumeHook?.Disable();
        LoadSoundFileHook?.Disable();
        GetResourceSyncHook?.Disable();
        GetResourceAsyncHook?.Disable();
        SoundVolumeTracker.Clear();
        StreamingBgmTracker.Clear();
        HooksSuppressedForMount = false;
        HooksSuppressedVolumeForGuideroid = false;
        Services.PluginLog.Debug("SoundMixer: Filter hooks disabled");
    }

    public void Dispose()
    {
        PlaySpecificSoundHook?.Dispose();
        PlaySoundHook?.Dispose();
        PlaySystemSoundHook?.Dispose();
        PlayClipSoundHook?.Dispose();
        PlayMovieSoundHook?.Dispose();
        PlayBGMSoundHook?.Dispose();
        PlayWeatherSoundHook?.Dispose();
        SetVolumeHook?.Dispose();
        GetVolumeHook?.Dispose();
        LoadSoundFileHook?.Dispose();
        GetResourceSyncHook?.Dispose();
        GetResourceAsyncHook?.Dispose();
    }

    private void TryCreateSoundManagerHook<T>(
        string memberName,
        string callSignature,
        T detour,
        ref Hook<T>? hookField,
        List<string> unavailableHooks
    )
        where T : Delegate
    {
        if (hookField != null)
        {
            return;
        }

        var address = SoundManagerHookResolver.ResolveMemberFunctionAddress(memberName);
        if (address == nint.Zero && Services.SigScanner.TryScanText(callSignature, out var scanned))
        {
            address = scanned;
        }

        if (address == nint.Zero)
        {
            unavailableHooks.Add(memberName);
            return;
        }

        hookField = Services.GameInteropProvider.HookFromAddress(address, detour);
    }

    private bool IsSetVolumeHookDesired()
    {
        if (!Plugin.IsEffectivelyEnabled)
        {
            return false;
        }

        var dbg = Plugin.Config.HookDebug;
        return !dbg.ManualControl || dbg.GetDesired(HookDebugId.SetVolume);
    }

    /// <summary>
    /// Play hooks must not probe ActiveSoundData lists to install SetVolume while mounted or when SetVolume is off.
    /// </summary>
    private bool ShouldDeferSetVolumeInstallFromPlayPath()
    {
        if (!IsSetVolumeHookDesired())
        {
            return true;
        }

        return MountTransitionGuard.IsActive;
    }

    /// <summary>
    /// PlaySound detour only: cast-loop SFX are already handled via PlaySpecificSound + SetVolume.
    /// Hooking both stacks breaks native fade-in (e.g. SAM castlp).
    /// </summary>
    private static bool ShouldPassthroughPlaySoundDetour(string? normalizedPath)
    {
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return false;
        }

        return normalizedPath.Contains("castlp", StringComparison.Ordinal);
    }

    /// <summary>
    /// Passthrough play hooks without TryInstall, post-play SoundData touches, or volume scaling.
    /// </summary>
    private bool ShouldPassthroughPlayHook(string? normalizedPath)
    {
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return false;
        }

        if (StreamingBgmTracker.IsBgmOrMusicPath(normalizedPath)
            || MountTransitionGuard.IsRideBgmPath(normalizedPath))
        {
            return false;
        }

        return SoundBlacklist.IsMountLoopBlockedPath(normalizedPath)
            || MountTransitionGuard.IsMountRelatedPath(normalizedPath);
    }

    private void TryInstallSetVolumeHookFromPlayPath()
    {
        if (ShouldDeferSetVolumeInstallFromPlayPath())
        {
            if (SoundDataHooksInstalled)
            {
                ApplyMountSafeHookState();
            }

            return;
        }

        TryInstallSetVolumeHook();
    }

    private void TryInstallSetVolumeHook()
    {
        if (SoundDataHooksInstalled)
        {
            ApplyMountSafeHookState();
            return;
        }

        var soundManager = SoundManager.Instance();
        if (soundManager == null)
        {
            return;
        }

        var sample = soundManager->InactiveSoundDataListHead;
        if (sample == null)
        {
            sample = soundManager->ActiveSoundDataListHead;
        }

        if (sample == null && soundManager->SoundDataPool != null)
        {
            sample = (SoundData*)soundManager->SoundDataPool;
        }

        if (sample == null)
        {
            Services.PluginLog.Debug("SoundMixer: SoundData hooks deferred (no sample yet)");
            return;
        }

        var getVolumeAddr = Util.GetVirtualFunctionAddress((nint)sample, 9);
        var setVolumeAddr = Util.GetVirtualFunctionAddress((nint)sample, 10);
        if (setVolumeAddr == nint.Zero)
        {
            Services.PluginLog.Warning("SoundMixer: failed to resolve SoundData.SetVolume");
            return;
        }

        SetVolumeHook = Services.GameInteropProvider.HookFromAddress<SetVolumeDelegate>(
            setVolumeAddr,
            SetVolumeDetour
        );

        if (getVolumeAddr != nint.Zero)
        {
            GetVolumeHook = Services.GameInteropProvider.HookFromAddress<GetVolumeDelegate>(
                getVolumeAddr,
                GetVolumeDetour
            );
        }
        else
        {
            Services.PluginLog.Warning("SoundMixer: failed to resolve SoundData.GetVolume");
        }

        SoundDataHooksInstalled = true;
        Services.PluginLog.Info(
            $"SoundMixer: SoundData hooks at GetVolume={getVolumeAddr:X}, SetVolume={setVolumeAddr:X}"
        );
        ApplyMountSafeHookState();
    }

    private void PassthroughSetVolume(SoundData* self, float volume, uint fadeDuration)
    {
        try
        {
            SetVolumeHook!.Original(self, volume, fadeDuration);
        }
        catch (Exception ex)
        {
            Services.PluginLog.Verbose(ex, "SoundMixer: SetVolume passthrough failed");
        }
    }

    internal void ProcessOneShotVolumeApplies()
    {
        if (!Plugin.IsEffectivelyEnabled || SetVolumeHook == null)
        {
            return;
        }

        OneShotPlayRegistry.ProcessDeferredApplies(
            TryApplyOneShotEngineVolume,
            TryFindActiveOneShotByIndex
        );
    }

    private bool TryFindActiveOneShotByIndex(uint soundIndex, out SoundData* soundData)
    {
        soundData = null;
        if (ShouldSkipActiveSoundListProcessing())
        {
            return false;
        }

        var soundManager = SoundManager.Instance();
        if (soundManager == null)
        {
            return false;
        }

        SoundData* found = null;
        SoundDataSafety.VisitSoundList(
            soundManager->ActiveSoundDataListHead,
            current =>
            {
                if (ShouldBypassSoundData(current))
                {
                    return true;
                }

                if (!SoundDataSafety.TryReadSoundData(current, out var isActive, out var readNumber, out _)
                    || !isActive
                    || readNumber != soundIndex)
                {
                    return true;
                }

                found = current;
                return false;
            },
            listName: "oneshot-find"
        );

        if (found == null)
        {
            return false;
        }

        soundData = found;
        return true;
    }

    private bool TryApplyOneShotEngineVolume(SoundData* soundData, float multiplier)
    {
        if (soundData == null || Math.Abs(multiplier - 1.0f) < 0.001f)
        {
            return false;
        }

        if (!SoundDataSafety.TryReadSoundData(soundData, out var isActive, out _, out var fieldVolume)
            || !isActive)
        {
            return false;
        }

        var baseVolume = fieldVolume > 0.001f
            ? fieldVolume
            : SoundVolumeTracker.ReadFadeTargetPublic(soundData);
        if (baseVolume <= 0.001f)
        {
            return false;
        }

        var effectiveVolume = Configuration.ClampToEngineCap(
            SoundVolumeHelper.ScaleVolume(baseVolume, multiplier)
        );

        if (Math.Abs(fieldVolume - effectiveVolume) < 0.02f)
        {
            return true;
        }

        ApplyDirectEngineVolume(soundData, effectiveVolume);
        return true;
    }

    private void ApplyDirectEngineVolume(SoundData* soundData, float effectiveVolume)
    {
        if (SetVolumeHook != null
            && SoundDataSafety.IsValidForHook(soundData)
            && !ShouldBypassSoundDataVolumeHooks()
            && !SoundBlacklist.IsPlayBypassActive
            && !ShouldBypassSoundData(soundData))
        {
            s_directEffectiveVolumeApply = true;
            try
            {
                SetVolumeHook.Original(soundData, effectiveVolume, 0);
            }
            catch (Exception ex)
            {
                Services.PluginLog.Verbose(ex, "SoundMixer: direct one-shot SetVolume failed");
                SoundVolumeTracker.ApplyFieldVolume(soundData, effectiveVolume);
            }
            finally
            {
                s_directEffectiveVolumeApply = false;
            }

            return;
        }

        SoundVolumeTracker.ApplyFieldVolume(soundData, effectiveVolume);
    }

    private void SetVolumeDetour(SoundData* self, float volume, uint fadeDuration)
    {
        if (self == null)
        {
            return;
        }

        if (s_directEffectiveVolumeApply)
        {
            PassthroughSetVolume(self, volume, fadeDuration);
            if (fadeDuration == 0 && SoundDataSafety.IsValidForVolumeWrite(self))
            {
                SoundVolumeTracker.ApplyFieldVolume(self, volume);
            }

            return;
        }

        if (ShouldBypassSoundDataVolumeHooks()
            || SoundBlacklist.IsPlayBypassActive
            || ShouldBypassSoundData(self))
        {
            PassthroughSetVolume(self, volume, fadeDuration);
            return;
        }

        if (!SoundDataSafety.IsValidForHook(self))
        {
            PassthroughSetVolume(self, volume, fadeDuration);
            return;
        }

        if (OneShotPlayRegistry.ShouldPassthroughVolumeHooks(self))
        {
            SoundVolumeTracker.UntrackForOneShot(self);
            PassthroughSetVolume(self, volume, fadeDuration);
            return;
        }

        s_setVolumeDetourDepth++;
        try
        {
            if (s_setVolumeDetourDepth > 1)
            {
                PassthroughSetVolume(self, volume, fadeDuration);
                return;
            }

            try
            {
                string resolvedPath = string.Empty;
                uint soundNumber = 0;
                if (TryResolveSafeSoundPath(self, out resolvedPath, out soundNumber)
                    && (SoundVolumeTracker.TryGetTrackedPath(self, out _)
                        || StreamingBgmTracker.IsBgmOrMusicPath(resolvedPath)))
                {
                    SoundVolumeTracker.TrackPlayPath(self, resolvedPath, soundNumber);
                }

                var gameVolume = volume;

                // Cast-loop sounds often get SetVolume(0) during play before TrackPlayPath.
                // Scaling/registering zero poisons LastGameVolume and forces ApplyFieldVolume(0).
                if (gameVolume <= 0.001f)
                {
                    if (SoundVolumeTracker.ShouldTreatSetVolumeZeroAsSilencing(self))
                    {
                        SoundVolumeTracker.NotifyGameSilencing(self);
                    }

                    PassthroughSetVolume(self, volume, fadeDuration);
                    return;
                }

                var multiplier = SoundVolumeTracker.GetMultiplier(self, Plugin.VolumeCalculator);
                volume = Configuration.ClampToEngineCap(
                    SoundVolumeHelper.ScaleVolume(volume, multiplier)
                );
                SoundVolumeTracker.Register(
                    self,
                    Plugin.VolumeCalculator,
                    gameVolume,
                    volume,
                    string.IsNullOrWhiteSpace(resolvedPath) ? null : resolvedPath
                );
                SoundVolumeHelper.MarkSetVolumeCalled();
            }
            catch (Exception ex)
            {
                Services.PluginLog.Error(ex, "Error in SetVolumeDetour");
            }

            if (!SoundDataSafety.IsValidForHook(self))
            {
                PassthroughSetVolume(self, volume, fadeDuration);
                return;
            }

            PassthroughSetVolume(self, volume, fadeDuration);

            if (fadeDuration == 0 && SoundDataSafety.IsValidForVolumeWrite(self))
            {
                try
                {
                    SoundVolumeTracker.ApplyFieldVolume(self, volume);
                }
                catch (Exception ex)
                {
                    Services.PluginLog.Error(ex, "Error forcing SoundData.Volume after SetVolume");
                }
            }
        }
        finally
        {
            s_setVolumeDetourDepth--;
        }
    }

    private float PassthroughGetVolume(SoundData* self)
    {
        try
        {
            return GetVolumeHook!.Original(self);
        }
        catch (Exception ex)
        {
            Services.PluginLog.Verbose(ex, "SoundMixer: GetVolume passthrough failed");
            return 1.0f;
        }
    }

    private float GetVolumeDetour(SoundData* self)
    {
        if (self == null)
        {
            return 1.0f;
        }

        if (ShouldBypassSoundDataVolumeHooks()
            || SoundBlacklist.IsPlayBypassActive
            || ShouldBypassSoundData(self))
        {
            return PassthroughGetVolume(self);
        }

        if (!SoundDataSafety.IsValidForHook(self))
        {
            return PassthroughGetVolume(self);
        }

        if (OneShotPlayRegistry.ShouldPassthroughVolumeHooks(self))
        {
            return PassthroughGetVolume(self);
        }

        var rawVolume = PassthroughGetVolume(self);

        try
        {
            if (!SoundDataSafety.IsValidForHook(self))
            {
                return rawVolume;
            }

            if (SoundVolumeTracker.ShouldPassthroughScaledVolume(self, rawVolume))
            {
                return rawVolume;
            }

            var multiplier = SoundVolumeTracker.GetMultiplier(self, Plugin.VolumeCalculator);
            return SoundVolumeTracker.GetEffectiveVolume(self, rawVolume, multiplier);
        }
        catch (Exception ex)
        {
            Services.PluginLog.Error(ex, "Error in GetVolumeDetour");
            return rawVolume;
        }
    }

    private void* PlaySpecificSoundDetour(long a1, int idx)
    {
        if (ShouldBypassVolumeInterception())
        {
            return PlaySpecificSoundHook!.Original(a1, idx);
        }

        var multiplier = 1.0f;
        var path = string.Empty;

        try
        {
            multiplier = PlaySpecificSoundDetourInner(a1, idx, out path);
            if (ShouldBypassPath(path))
            {
                using (SoundBlacklist.EnterPlayBypass())
                {
                    var bypassResult = PlaySpecificSoundHook!.Original(a1, idx);
                    RegisterBlockedPlayResultFromSpecificSound(bypassResult, path);
                    return bypassResult;
                }
            }

            if (Plugin.VolumeCalculator.IsLikelyOneShotPath(path))
            {
                void* oneShotResult;
                using (SoundBlacklist.EnterPlayBypass())
                {
                    oneShotResult = PlaySpecificSoundHook!.Original(a1, idx);
                }

                if (Plugin.IsEffectivelyEnabled && Math.Abs(multiplier - 1.0f) > 0.001f)
                {
                    if (oneShotResult != null
                        && SoundDataSafety.IsReadable((nint)oneShotResult))
                    {
                        OneShotPlayRegistry.Register((SoundData*)oneShotResult, path, multiplier);
                    }
                    else
                    {
                        OneShotPlayRegistry.NotePending(path, (uint)idx, multiplier);
                    }
                }

                return oneShotResult;
            }

            if (Plugin.IsEffectivelyEnabled)
            {
                SoundVolumeHelper.BeginPlay(path, (uint)idx, multiplier);
            }
        }
        catch (Exception ex)
        {
            Services.PluginLog.Error(ex, "Error in PlaySpecificSoundDetour");
        }

        TryInstallSetVolumeHookFromPlayPath();

        try
        {
            var result = PlaySpecificSoundHook!.Original(a1, idx);

            if (Plugin.IsEffectivelyEnabled
                && !string.IsNullOrWhiteSpace(path)
                && !ShouldPassthroughPlayHook(path))
            {
                if (!TryApplyVolumeFromPlaySpecificResult(result, (uint)idx, path, multiplier))
                {
                    if (Math.Abs(multiplier - 1.0f) > 0.001f
                        && !SoundVolumeHelper.WasSetVolumeCalledThisPlay)
                    {
                        ApplyVolumeToActiveSounds((uint)idx, path, multiplier);
                    }
                    else
                    {
                        TrackActiveSoundPaths((uint)idx, path);
                    }
                }
            }

            return result;
        }
        finally
        {
            SoundVolumeHelper.EndPlay();
        }
    }

    private bool TryApplyVolumeFromPlaySpecificResult(
        void* result,
        uint scdSoundIndex,
        string path,
        float multiplier
    )
    {
        if (result == null)
        {
            return false;
        }

        var soundData = (SoundData*)result;
        if (!SoundDataSafety.IsReadable((nint)soundData))
        {
            return false;
        }

        if (Math.Abs(multiplier - 1.0f) > 0.001f && !SoundVolumeHelper.WasSetVolumeCalledThisPlay)
        {
            ApplyMultiplierToSoundData(soundData, multiplier, path);
        }
        else
        {
            SoundVolumeTracker.PrepareTrackedForPlay(soundData, path, scdSoundIndex);
        }

        return true;
    }

    private void ApplyVolumeToActiveSounds(
        uint scdSoundIndex,
        string path,
        float multiplier,
        bool statelessOneShot = false
    )
    {
        if (ShouldSkipActiveSoundListProcessing())
        {
            return;
        }

        var soundManager = SoundManager.Instance();
        if (soundManager == null)
        {
            return;
        }

        TryApplyVolumeInList(
            soundManager->ActiveSoundDataListHead,
            scdSoundIndex,
            path,
            multiplier,
            statelessOneShot
        );
    }

    private bool TryApplyVolumeInList(
        SoundData* listHead,
        uint scdSoundIndex,
        string path,
        float multiplier,
        bool statelessOneShot = false
    )
    {
        var found = false;

        SoundDataSafety.VisitSoundList(
            listHead,
            current =>
            {
                if (ShouldBypassSoundData(current))
                {
                    return true;
                }

                if (!SoundDataSafety.TryReadSoundData(current, out var isActive, out var soundNumber, out _)
                    || !isActive)
                {
                    return true;
                }

                if (soundNumber != scdSoundIndex)
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(path))
                {
                    ApplyMultiplierToSoundData(current, multiplier, path);
                }
                else
                {
                    ApplyMultiplierToSoundData(current, multiplier);
                }

                found = true;
                return false;
            },
            listName: "apply-volume"
        );

        return found;
    }

    private void TrackActiveSoundPaths(uint scdSoundIndex, string path)
    {
        if (ShouldSkipActiveSoundListProcessing() || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var soundManager = SoundManager.Instance();
        if (soundManager == null)
        {
            return;
        }

        TryTrackPathsInList(soundManager->ActiveSoundDataListHead, scdSoundIndex, path);
    }

    private bool TryTrackPathsInList(SoundData* listHead, uint scdSoundIndex, string path)
    {
        var tracked = false;

        SoundDataSafety.VisitSoundList(
            listHead,
            current =>
            {
                if (ShouldBypassSoundData(current))
                {
                    return true;
                }

                if (!SoundDataSafety.TryReadSoundData(current, out var isActive, out var soundNumber, out _)
                    || !isActive)
                {
                    return true;
                }

                if (soundNumber == scdSoundIndex)
                {
                    SoundVolumeTracker.PrepareTrackedForPlay(current, path, soundNumber);
                    tracked = true;
                    return false;
                }

                return true;
            },
            listName: "track-path"
        );

        return tracked;
    }

    private void ApplyMultiplierToSoundData(SoundData* soundData, float multiplier, string? scdPath = null)
    {
        if (SoundBlacklist.IsPlayBypassActive
            || ShouldBypassSoundData(soundData))
        {
            return;
        }

        if (!SoundDataSafety.TryReadSoundData(soundData, out _, out _, out var gameVolume))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(scdPath))
        {
            SoundVolumeTracker.PrepareTrackedForPlay(soundData, scdPath, 0);
        }

        // Cast/loop sounds (e.g. *_castlp_*) often return with Volume=0 and fade in via SetVolume later.
        // Scaling zero here would mute them and EnforceTrackedVolumes would keep forcing zero.
        if (gameVolume <= 0.001f)
        {
            return;
        }

        var effectiveVolume = Configuration.ClampToEngineCap(
            SoundVolumeHelper.ScaleVolume(gameVolume, multiplier)
        );
        SoundVolumeTracker.Register(
            soundData,
            Plugin.VolumeCalculator,
            gameVolume,
            effectiveVolume,
            scdPath
        );

        SoundVolumeTracker.ApplyFieldVolume(soundData, effectiveVolume);
    }

    private static bool PathsMatch(string expected, string actual)
    {
        expected = expected.ToLowerInvariant();
        actual = actual.ToLowerInvariant();
        return expected == actual
               || expected.Contains(actual, StringComparison.Ordinal)
               || actual.Contains(expected, StringComparison.Ordinal);
    }

    private float PlaySpecificSoundDetourInner(long a1, int idx, out string path)
    {
        path = string.Empty;
        if (a1 == 0 || !SoundDataSafety.IsReadable((nint)a1, IntPtr.Size * 2))
        {
            return 1.0f;
        }

        byte* scdData;
        try
        {
            scdData = *(byte**)(a1 + 8);
        }
        catch (Exception ex)
        {
            Services.PluginLog.Verbose(ex, "SoundMixer: failed to read SCD pointer in PlaySpecificSound");
            return 1.0f;
        }

        if (scdData == null || !SoundDataSafety.IsReadablePointer((nint)scdData))
        {
            return 1.0f;
        }

        var scdPtr = (nint)scdData;
        if (!Scds.TryGetValue(scdPtr, out var cachedPath))
        {
            path = $"unknown/{scdPtr:X}";
        }
        else
        {
            path = cachedPath;
        }

        path = Plugin.ResolveSoundPath(path, scdPtr);
        if (ShouldBypassPath(path))
        {
            return 1.0f;
        }

        MountTransitionGuard.NotifyMountSound(path, Plugin.Config.SafeMode);
        var specificPath = PathResolver.BuildSpecificPath(path, idx);
        var multiplier = Plugin.VolumeCalculator.GetVolumeForSound(specificPath);

        if (Plugin.Config.EnableMonitoring)
        {
            EnqueueMonitoredSound(
                path,
                idx,
                specificPath,
                multiplier,
                SoundMonitorHookIds.From(HookDebugId.PlaySpecificSound)
            );
        }

        return multiplier;
    }

    private SoundData* PlaySoundDetour(
        SoundManager* self,
        byte* path,
        float volume,
        uint fadeInDuration,
        float posX,
        float posY,
        float posZ,
        float speed,
        int a9,
        uint soundNumber,
        bool autoRelease,
        SoundVolumeCategory volumeCategory,
        bool a13,
        int midiNote,
        bool a15,
        bool defaultFadeOut,
        bool isPositional,
        bool a18
    ) =>
        PlaySoundWithVolume(
            path,
            volume,
            soundNumber,
            HookDebugId.PlaySound,
            (scaledVolume) =>
                PlaySoundHook!.Original(
                    self,
                    path,
                    scaledVolume,
                    fadeInDuration,
                    posX,
                    posY,
                    posZ,
                    speed,
                    a9,
                    soundNumber,
                    autoRelease,
                    volumeCategory,
                    a13,
                    midiNote,
                    a15,
                    defaultFadeOut,
                    isPositional,
                    a18
                )
        );

    private SoundData* PlaySystemSoundDetour(
        SoundManager* self,
        byte* path,
        float volume,
        uint soundNumber,
        uint fadeInDuration,
        bool autoRelease,
        SoundVolumeCategory volumeCategory
    ) =>
        PlaySoundWithVolume(
            path,
            volume,
            soundNumber,
            HookDebugId.PlaySystemSound,
            (scaledVolume) =>
                PlaySystemSoundHook!.Original(
                    self,
                    path,
                    scaledVolume,
                    soundNumber,
                    fadeInDuration,
                    autoRelease,
                    volumeCategory
                )
        );

    private SoundData* PlayClipSoundDetour(
        SoundManager* self,
        byte* path,
        float volume,
        uint fadeInDuration,
        float posX,
        float posY,
        float posZ,
        float playbackSpeed,
        int a9,
        uint soundNumber,
        bool autoRelease,
        bool a12
    ) =>
        PlaySoundWithVolume(
            path,
            volume,
            soundNumber,
            HookDebugId.PlayClipSound,
            (scaledVolume) =>
                PlayClipSoundHook!.Original(
                    self,
                    path,
                    scaledVolume,
                    fadeInDuration,
                    posX,
                    posY,
                    posZ,
                    playbackSpeed,
                    a9,
                    soundNumber,
                    autoRelease,
                    a12
                )
        );

    private SoundData* PlayMovieSoundDetour(
        SoundManager* self,
        byte* path,
        float volume,
        uint soundNumber,
        uint fadeInDuration,
        bool autoRelease
    ) =>
        PlaySoundWithVolume(
            path,
            volume,
            soundNumber,
            HookDebugId.PlayMovieSound,
            (scaledVolume) =>
                PlayMovieSoundHook!.Original(
                    self,
                    path,
                    scaledVolume,
                    soundNumber,
                    fadeInDuration,
                    autoRelease
                )
        );

    private SoundData* PlayBGMSoundDetour(SoundManager* self, byte* path)
    {
        var normalizedPath = string.Empty;
        var multiplier = 1.0f;

        try
        {
            if (path != null)
            {
                normalizedPath = Plugin.ResolveSoundPath(SoundVolumeHelper.NormalizePath(path));
                if (ShouldBypassPath(normalizedPath))
                {
                    using (SoundBlacklist.EnterPlayBypass())
                    {
                        var bypassResult = PlayBGMSoundHook!.Original(self, path);
                        SoundBlacklist.RegisterBlockedPlayResult(bypassResult, normalizedPath);
                        return bypassResult;
                    }
                }

                MountTransitionGuard.NotifyMountSound(normalizedPath, Plugin.Config.SafeMode);
                multiplier = ResolveScdPathMultiplier(normalizedPath);

                if (Plugin.IsEffectivelyEnabled)
                {
                    SoundVolumeHelper.BeginPlay(normalizedPath, 0, multiplier);
                }
            }

            TryInstallSetVolumeHook();
            var result = PlayBGMSoundHook!.Original(self, path);
            if (ShouldBypassPath(normalizedPath))
            {
                SoundBlacklist.RegisterBlockedPlayResult(result, normalizedPath);
            }

            if (Plugin.IsEffectivelyEnabled && !string.IsNullOrEmpty(normalizedPath))
            {
                StreamingBgmTracker.NotePlay(normalizedPath, multiplier, result);

                if (result != null
                    && Math.Abs(multiplier - 1.0f) > 0.001f
                    && !SoundVolumeHelper.WasSetVolumeCalledThisPlay)
                {
                    ApplyMultiplierToSoundData(result, multiplier, normalizedPath);
                }
            }

            if (Plugin.Config.EnableMonitoring && !string.IsNullOrEmpty(normalizedPath))
            {
                var soundNumber = 0;
                if (result != null
                    && SoundDataSafety.TryReadSoundData(result, out _, out var number, out _))
                {
                    soundNumber = (int)number;
                }

                var specificPath = PathResolver.BuildSpecificPath(normalizedPath, soundNumber);
                EnqueueMonitoredSound(
                    normalizedPath,
                    soundNumber,
                    specificPath,
                    multiplier,
                    SoundMonitorHookIds.From(HookDebugId.PlayBgmSound),
                    isStreaming: true
                );
            }

            return result;
        }
        catch (Exception ex)
        {
            Services.PluginLog.Error(ex, "Error in PlayBGMSoundDetour");
            return PlayBGMSoundHook!.Original(self, path);
        }
        finally
        {
            if (!string.IsNullOrEmpty(normalizedPath))
            {
                SoundVolumeHelper.EndPlay();
            }
        }
    }

    private void PlayWeatherSoundDetour(SoundManager* self, byte* path, uint fadeDuration)
    {
        var normalizedPath = string.Empty;

        try
        {
            if (path != null)
            {
                normalizedPath = Plugin.ResolveSoundPath(SoundVolumeHelper.NormalizePath(path));
                if (ShouldBypassPath(normalizedPath))
                {
                    using (SoundBlacklist.EnterPlayBypass())
                    {
                        PlayWeatherSoundHook!.Original(self, path, fadeDuration);
                        SoundBlacklist.RegisterBlockedPlayResult(self->WeatherSoundData, normalizedPath);
                    }

                    return;
                }

                MountTransitionGuard.NotifyMountSound(normalizedPath, Plugin.Config.SafeMode);
            }

            PlayWeatherSoundHook!.Original(self, path, fadeDuration);

            if (string.IsNullOrEmpty(normalizedPath) || ShouldSkipActiveSoundListProcessing())
            {
                return;
            }

            var weatherSound = self->WeatherSoundData;
            var weatherSoundNumber = 0u;
            if (weatherSound != null
                && SoundDataSafety.IsReadable((nint)weatherSound)
                && SoundDataSafety.TryReadSoundData(weatherSound, out _, out weatherSoundNumber, out _))
            {
                if (Plugin.IsEffectivelyEnabled)
                {
                    var multiplier = ResolveScdPathMultiplier(normalizedPath);
                    if (Math.Abs(multiplier - 1.0f) > 0.001f)
                    {
                        ApplyMultiplierToSoundData(weatherSound, multiplier, normalizedPath);
                    }
                    else
                    {
                        SoundVolumeTracker.TrackPlayPath(weatherSound, normalizedPath, weatherSoundNumber);
                    }
                }
            }

            if (Plugin.Config.EnableMonitoring && weatherSound != null)
            {
                var specificPath = PathResolver.BuildSpecificPath(normalizedPath, (int)weatherSoundNumber);
                var multiplier = Plugin.VolumeCalculator.GetVolumeForSound(specificPath);
                EnqueueMonitoredSound(
                    normalizedPath,
                    (int)weatherSoundNumber,
                    specificPath,
                    multiplier,
                    SoundMonitorHookIds.From(HookDebugId.PlayWeatherSound)
                );
            }
        }
        catch (Exception ex)
        {
            Services.PluginLog.Error(ex, "Error in PlayWeatherSoundDetour");
            PlayWeatherSoundHook!.Original(self, path, fadeDuration);
        }
    }

    private float ResolveScdPathMultiplier(string scdPath)
    {
        var multiplier = Plugin.VolumeCalculator.GetVolumeForSound(scdPath);
        if (Math.Abs(multiplier - 1.0f) > 0.001f)
        {
            return multiplier;
        }

        return Plugin.VolumeCalculator.GetVolumeForSound(PathResolver.BuildSpecificPath(scdPath, 0));
    }

    private SoundData* PlaySoundWithVolume(
        byte* path,
        float volume,
        uint soundNumber,
        HookDebugId hookSource,
        PlayOriginalWithVolumeDelegate playOriginal
    )
    {
        if (ShouldBypassVolumeInterception())
        {
            return playOriginal(volume);
        }

        var normalizedPath = string.Empty;
        var multiplier = 1.0f;

        try
        {
            if (path != null)
            {
                normalizedPath = Plugin.ResolveSoundPath(SoundVolumeHelper.NormalizePath(path));
                if (ShouldBypassPath(normalizedPath))
                {
                    using (SoundBlacklist.EnterPlayBypass())
                    {
                        var bypassResult = playOriginal(volume);
                        SoundBlacklist.RegisterBlockedPlayResult(bypassResult, normalizedPath);
                        return bypassResult;
                    }
                }

                MountTransitionGuard.NotifyMountSound(normalizedPath, Plugin.Config.SafeMode);

                if ((hookSource == HookDebugId.PlaySound && ShouldPassthroughPlaySoundDetour(normalizedPath))
                    || ShouldPassthroughPlayHook(normalizedPath))
                {
                    return playOriginal(volume);
                }

                var specificPath = PathResolver.BuildSpecificPath(normalizedPath, (int)soundNumber);
                multiplier = Plugin.VolumeCalculator.GetVolumeForSound(specificPath);

                if (Plugin.Config.EnableMonitoring)
                {
                    EnqueueMonitoredSound(
                        normalizedPath,
                        (int)soundNumber,
                        specificPath,
                        multiplier,
                        SoundMonitorHookIds.From(hookSource)
                    );
                }

                if (Plugin.VolumeCalculator.IsLikelyOneShotPath(normalizedPath))
                {
                    var oneShotPlayVolume = volume;
                    if (Plugin.IsEffectivelyEnabled && Math.Abs(multiplier - 1.0f) > 0.001f)
                    {
                        oneShotPlayVolume = Configuration.ClampToEngineCap(
                            SoundVolumeHelper.ScaleVolume(volume, multiplier)
                        );
                    }

                    SoundData* oneShotResult;
                    using (SoundBlacklist.EnterPlayBypass())
                    {
                        oneShotResult = playOriginal(oneShotPlayVolume);
                    }

                    if (ShouldBypassPath(normalizedPath))
                    {
                        SoundBlacklist.RegisterBlockedPlayResult(oneShotResult, normalizedPath);
                    }

                    if (oneShotResult != null
                        && Plugin.IsEffectivelyEnabled
                        && Math.Abs(multiplier - 1.0f) > 0.001f)
                    {
                        OneShotPlayRegistry.Register(
                            oneShotResult,
                            normalizedPath,
                            multiplier,
                            volumeAlreadyApplied: true
                        );
                    }

                    return oneShotResult;
                }

                if (Plugin.IsEffectivelyEnabled)
                {
                    SoundVolumeHelper.BeginPlay(normalizedPath, soundNumber, multiplier);
                }
            }

            var playVolume = volume;
            if (Plugin.IsEffectivelyEnabled
                && !string.IsNullOrWhiteSpace(normalizedPath)
                && Math.Abs(multiplier - 1.0f) > 0.001f)
            {
                playVolume = Configuration.ClampToEngineCap(
                    SoundVolumeHelper.ScaleVolume(volume, multiplier)
                );
            }

            TryInstallSetVolumeHookFromPlayPath();
            var result = playOriginal(playVolume);
            if (ShouldBypassPath(normalizedPath))
            {
                SoundBlacklist.RegisterBlockedPlayResult(result, normalizedPath);
            }

            if (result != null
                && Plugin.IsEffectivelyEnabled
                && !string.IsNullOrEmpty(normalizedPath)
                && !ShouldPassthroughPlayHook(normalizedPath))
            {
                SoundVolumeTracker.PrepareTrackedForPlay(result, normalizedPath, soundNumber);
                if (Math.Abs(multiplier - 1.0f) > 0.001f && !SoundVolumeHelper.WasSetVolumeCalledThisPlay)
                {
                    ApplyMultiplierToSoundData(result, multiplier, normalizedPath);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            Services.PluginLog.Error(ex, "Error in PlaySoundWithVolume");
            return playOriginal(volume);
        }
        finally
        {
            if (!string.IsNullOrEmpty(normalizedPath))
            {
                SoundVolumeHelper.EndPlay();
            }
        }
    }

    private void ScanActiveSoundsForMonitoring(SoundManager* soundManager)
    {
        if (!Plugin.Config.EnableMonitoring)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if ((now - _lastActiveScanUtc).TotalSeconds < 2.0)
        {
            return;
        }

        _lastActiveScanUtc = now;

        try
        {
            var seen = new HashSet<nint>();
            ScanSoundListForMonitoring(soundManager->ActiveSoundDataListHead, seen);

            if (soundManager->WeatherSoundData != null
                && SoundDataSafety.IsReadable((nint)soundManager->WeatherSoundData))
            {
                ScanSingleSoundForMonitoring(soundManager->WeatherSoundData, seen);
            }

            foreach (var ptr in KnownActiveMonitoredSounds.Keys)
            {
                if (!seen.Contains(ptr))
                {
                    KnownActiveMonitoredSounds.TryRemove(ptr, out _);
                }
            }
        }
        catch (Exception ex)
        {
            Services.PluginLog.Error(ex, "SoundMixer: active sound monitoring scan failed");
        }
    }

    private void PruneInactiveMonitoredSounds(SoundManager* soundManager)
    {
        foreach (var ptr in KnownActiveMonitoredSounds.Keys)
        {
            var soundData = (SoundData*)ptr;
            if (!SoundDataSafety.TryReadSoundData(soundData, out var isActive, out _, out _)
                || !isActive)
            {
                KnownActiveMonitoredSounds.TryRemove(ptr, out _);
            }
        }
    }

    private void ScanSoundListForMonitoring(SoundData* listHead, HashSet<nint> seen)
    {
        SoundDataSafety.VisitSoundList(
            listHead,
            soundData =>
            {
                ScanSingleSoundForMonitoring(soundData, seen);
                return true;
            },
            listName: "monitoring"
        );
    }

    private void ScanSingleSoundForMonitoring(SoundData* soundData, HashSet<nint> seen)
    {
        try
        {
            if (ShouldBypassSoundData(soundData))
            {
                seen.Add((nint)soundData);
                return;
            }

            if (!SoundDataSafety.TryReadSoundData(soundData, out var isActive, out var soundNumber, out _))
            {
                return;
            }

            if (!isActive)
            {
                return;
            }

            var ptr = (nint)soundData;
            seen.Add(ptr);

            if (!TryResolveSoundPath(soundData, out var resolvedPath, "monitor")
                || string.IsNullOrWhiteSpace(resolvedPath))
            {
                return;
            }

            if (ShouldBypassPath(resolvedPath))
            {
                SoundBlacklist.RegisterBlockedPlayResult(soundData, resolvedPath);
                return;
            }

            var specificPath = PathResolver.BuildSpecificPath(resolvedPath, (int)soundNumber);
            var fingerprint = $"{specificPath}#{soundNumber}";

            if (KnownActiveMonitoredSounds.TryGetValue(ptr, out var previous) && previous == fingerprint)
            {
                return;
            }

            KnownActiveMonitoredSounds[ptr] = fingerprint;
            var multiplier = Plugin.VolumeCalculator.GetVolumeForSound(specificPath);
            EnqueueMonitoredSound(
                resolvedPath,
                (int)soundNumber,
                specificPath,
                multiplier,
                SoundMonitorHookIds.ActiveScan,
                isStreaming: true
            );
        }
        catch (Exception ex)
        {
            Services.PluginLog.Verbose(ex, "SoundMixer: skipped one active sound during monitoring scan");
        }
    }

    private void EnqueueMonitoredSound(
        string path,
        int idx,
        string specificPath,
        float multiplier,
        string hookSourceId,
        bool isStreaming = false
    )
    {
        var category = Plugin.VolumeCalculator.GetDisplayCategory(specificPath);
        if (isStreaming)
        {
            category += " " + Loc.PlayingTag();
        }

        RecentSounds.Enqueue(new SoundInfo
        {
            Path = path,
            Index = idx,
            Category = category,
            Volume = multiplier,
            LastPlayed = DateTime.Now,
            PlayCount = 1,
            HookSourceId = hookSourceId,
        });

        while (RecentSounds.Count > Configuration.MonitoringHistorySize)
        {
            RecentSounds.TryDequeue(out _);
        }
    }

    private ResourceHandle* GetResourceSyncDetour(
        ResourceManager* resourceManager,
        ResourceCategory* categoryId,
        ResourceType* resourceType,
        int* resourceHash,
        byte* path,
        GetResourceParameters* pGetResParams,
        nint unk8,
        uint unk9
    ) =>
        GetResourceHandler(
            true,
            resourceManager,
            categoryId,
            resourceType,
            resourceHash,
            path,
            pGetResParams,
            0,
            unk8,
            unk9
        );

    private ResourceHandle* GetResourceAsyncDetour(
        ResourceManager* resourceManager,
        ResourceCategory* categoryId,
        ResourceType* pResourceType,
        int* resourceHash,
        byte* path,
        GetResourceParameters* pGetResParams,
        byte isUnk,
        nint unk8,
        uint unk9
    ) =>
        GetResourceHandler(
            false,
            resourceManager,
            categoryId,
            pResourceType,
            resourceHash,
            path,
            pGetResParams,
            isUnk,
            unk8,
            unk9
        );

    private ResourceHandle* GetResourceHandler(
        bool isSync,
        ResourceManager* resourceManager,
        ResourceCategory* categoryId,
        GameTypes.ResourceType* resourceType,
        int* resourceHash,
        byte* path,
        GetResourceParameters* pGetResParams,
        byte isUnk,
        nint unk8,
        uint unk9
    )
    {
        var ret = isSync
            ? GetResourceSyncHook!.Original(
                resourceManager,
                categoryId,
                resourceType,
                resourceHash,
                path,
                pGetResParams,
                unk8,
                unk9
            )
            : GetResourceAsyncHook!.Original(
                resourceManager,
                categoryId,
                resourceType,
                resourceHash,
                path,
                pGetResParams,
                isUnk,
                unk8,
                unk9
            );

        var strPath = Util.ReadTerminatedString(path);
        if (ret != null
            && strPath.EndsWith(".scd", StringComparison.OrdinalIgnoreCase)
            && SoundDataSafety.IsReadablePointer((nint)ret, ResourceDataPointerOffset + IntPtr.Size))
        {
            var scdData = Marshal.ReadIntPtr((nint)ret + ResourceDataPointerOffset);
            if (scdData != nint.Zero && SoundDataSafety.IsReadablePointer(scdData))
            {
                Scds[scdData] = strPath;
            }
        }

        return ret;
    }

    private nint LoadSoundFileDetour(nint resourceHandle, uint a2)
    {
        var ret = LoadSoundFileHook!.Original(resourceHandle, a2);
        try
        {
            if (resourceHandle == 0
                || !SoundDataSafety.IsReadablePointer(resourceHandle, ResourceDataPointerOffset + IntPtr.Size))
            {
                return ret;
            }

            var handle = (ResourceHandle*)resourceHandle;
            var name = handle->FileName.ToString();
            if (name.EndsWith(".scd", StringComparison.OrdinalIgnoreCase))
            {
                var dataPtr = Marshal.ReadIntPtr(resourceHandle + ResourceDataPointerOffset);
                if (dataPtr != nint.Zero && SoundDataSafety.IsReadablePointer(dataPtr))
                {
                    Scds[dataPtr] = name;
                }
            }
        }
        catch (Exception ex)
        {
            Services.PluginLog.Error(ex, "Error in LoadSoundFileDetour");
        }

        return ret;
    }
}
