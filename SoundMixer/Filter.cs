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
    /// <summary>Native SoundData.SetVolume vfunc; used when hook is not installed yet or for gains above 100%.</summary>
    private SetVolumeDelegate? NativeSetVolumeFallback;
    private Hook<GetResourceSyncPrototype>? GetResourceSyncHook;
    private Hook<GetResourceAsyncPrototype>? GetResourceAsyncHook;
    private Hook<LoadSoundFileDelegate>? LoadSoundFileHook;

    #endregion

    private Plugin Plugin { get; }
    private ConcurrentDictionary<nint, string> Scds { get; } = new();
    /// <summary>Maps vanilla paths like sound/renai.scd to the longer mod path cached in Scds.</summary>
    private readonly ConcurrentDictionary<string, string> ScdBasenameAliases = new(StringComparer.Ordinal);
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

    private readonly ActiveSoundEnforceCache _activeEnforceCache = new();
    private uint _activeListEnforceFrameCounter;

    private readonly ConcurrentDictionary<nint, string> KnownActiveMonitoredSounds = new();
    private readonly ConcurrentDictionary<nint, DateTime> _pathFailureThrottle = new();
    private readonly HashSet<nint> _monitoringSeenBuffer = new();
    private DateTime _lastActiveScanUtc;

    private const double PathFailureThrottleSeconds = 8.0;
    private const int FootstepEnforceIntervalFrames = 10;
    private const int ActiveListEnforceIntervalFrames = 2;
    private const int MonitorPruneIntervalFrames = 15;
    private const int TrackedPruneIntervalFrames = 5;

    internal void InvalidateActiveEnforceCache() => _activeEnforceCache.Invalidate();

    internal Filter(Plugin plugin)
    {
        Plugin = plugin;
        ApplySavedPathAliases();
        SoundEnforcement.SetResolver(ResolveSoundEnforcement);
    }

    /// <summary>
    /// Same path + multiplier as monitor log when safe resolution succeeds:
    /// TryResolveSafeSoundPath → specificPath → GetVolumeForSound.
    /// Never calls ISoundData.GetFileName (unsafe on streaming nodes).
    /// </summary>
    private bool ResolveSoundEnforcement(
        SoundData* soundData,
        VolumeCalculator calculator,
        out ResolvedSoundEnforcement result
    )
    {
        result = default;
        if (soundData == null
            || ZoneTransitionGuard.ShouldSkipSoundDataListAccess()
            || !TryResolveSafeSoundPath(soundData, out var resolvedPath, out _)
            || string.IsNullOrWhiteSpace(resolvedPath))
        {
            return false;
        }

        if (!SoundDataSafety.TryReadSoundData(soundData, out _, out var soundNumber, out _))
        {
            return false;
        }

        resolvedPath = resolvedPath.ToLowerInvariant();
        var specificPath = PathResolver.BuildSpecificPath(resolvedPath, (int)soundNumber);
        result = new ResolvedSoundEnforcement
        {
            ResolvedPath = resolvedPath,
            SoundNumber = soundNumber,
            SpecificPath = specificPath,
            Multiplier = calculator.GetVolumeForSound(specificPath),
        };
        return true;
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
        toPath = toPath.ToLowerInvariant().Trim();
        RegisterScdBasenameAlias(toPath);

        if (PathResolver.TryParseUnknownPointer(fromPath, out var pointer))
        {
            Scds[pointer] = toPath;
            return;
        }

        var scdPath = GetScdPathFromFullPath(fromPath);
        if (PathResolver.TryParseUnknownPointer(scdPath, out pointer))
        {
            Scds[pointer] = toPath;
        }
    }

    internal bool TryResolveScdBasenameAlias(string rawPath, out string fullPath)
    {
        fullPath = string.Empty;
        rawPath = rawPath.ToLowerInvariant().Trim();
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return false;
        }

        if (ScdBasenameAliases.TryGetValue(rawPath, out fullPath!))
        {
            return true;
        }

        if (!PathResolver.TrySplitSoundIndex(rawPath, out var scdPath, out var index))
        {
            return false;
        }

        if (!ScdBasenameAliases.TryGetValue(scdPath, out var aliasedScd))
        {
            return false;
        }

        fullPath = PathResolver.BuildSpecificPath(aliasedScd, index);
        return true;
    }

    private void CacheScdPath(nint scdData, string path)
    {
        if (scdData == nint.Zero || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        path = path.ToLowerInvariant().Trim();
        if (Scds.TryGetValue(scdData, out var existing) && existing.Length > path.Length)
        {
            path = existing;
        }

        Scds[scdData] = path;
        RegisterScdBasenameAlias(path);
    }

    private void RegisterScdBasenameAlias(string fullPath)
    {
        fullPath = fullPath.ToLowerInvariant().Trim();
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return;
        }

        var soundIdx = fullPath.LastIndexOf("/sound/", StringComparison.Ordinal);
        if (soundIdx < 0)
        {
            return;
        }

        var basename = fullPath[(soundIdx + 1)..];
        ScdBasenameAliases.AddOrUpdate(
            basename,
            fullPath,
            (_, existing) => fullPath.Length > existing.Length ? fullPath : existing
        );

        if (PathResolver.TrySplitSoundIndex(basename, out var scdOnly, out _))
        {
            ScdBasenameAliases.AddOrUpdate(
                scdOnly,
                fullPath,
                (_, existing) => fullPath.Length > existing.Length ? fullPath : existing
            );
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
        return ShouldBypassVolumeInterception()
            || ZoneTransitionGuard.ShouldPassthroughVolumeHooks();
    }

    /// <summary>
    /// Skip walking ActiveSoundData linked lists when official/user hook guards require it.
    /// </summary>
    private bool ShouldSkipActiveSoundListScanning()
    {
        return HookGuardPolicy.ShouldSkipActiveListScan(Plugin.Config);
    }

    private bool ShouldSkipActiveSoundListProcessing() =>
        ZoneTransitionGuard.ShouldSkipSoundDataListAccess()
        || ShouldSkipActiveSoundListScanning();

    internal bool CanSafelyRefreshActiveSounds() =>
        !ZoneTransitionGuard.ShouldSkipSoundDataMaintenance()
        && !ShouldSkipActiveSoundListScanning();

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

    internal void ClearTransitionTracking()
    {
        SoundVolumeTracker.Clear();
        OneShotPlayRegistry.Clear();
        StreamingBgmTracker.Clear();
        SoundBlacklist.ClearPointerCache();
        _pathFailureThrottle.Clear();
        KnownActiveMonitoredSounds.Clear();
        InvalidateActiveEnforceCache();
    }

    internal void EnforceTrackedVolumes()
    {
        if (ZoneTransitionGuard.ShouldSkipSoundDataMaintenance())
        {
            return;
        }

        try
        {
            var soundManager = SoundManager.Instance();
            if (soundManager == null)
            {
                return;
            }

            _activeListEnforceFrameCounter++;
            var enforceFootstepsThisFrame =
                _activeListEnforceFrameCounter % FootstepEnforceIntervalFrames == 0;
            var enforceActiveListThisFrame =
                _activeListEnforceFrameCounter % ActiveListEnforceIntervalFrames == 0;
            var requiresScaling = Plugin.VolumeCalculator.RequiresAnyVolumeScaling();

            if (Plugin.IsEffectivelyEnabled && requiresScaling)
            {
                SoundVolumeTracker.EnforceAllTracked(Plugin.VolumeCalculator, ApplyRefreshedVolume);

                if (!ShouldSkipActiveSoundListProcessing())
                {
                    EnforceTrackedSound(soundManager->WeatherSoundData);

                    if (enforceActiveListThisFrame)
                    {
                        EnforceActiveSoundListVolumes(
                            soundManager->ActiveSoundDataListHead,
                            enforceFootstepsThisFrame
                        );
                    }
                }
            }

            if (!ShouldSkipActiveSoundListProcessing())
            {
                ScanActiveSoundsForMonitoring(soundManager);
            }

            if (_activeListEnforceFrameCounter % TrackedPruneIntervalFrames == 0)
            {
                SoundVolumeTracker.PruneInactive();
            }

            if (!ShouldSkipActiveSoundListProcessing()
                && _activeListEnforceFrameCounter % MonitorPruneIntervalFrames == 0)
            {
                PruneInactiveMonitoredSounds(soundManager);
            }
        }
        catch (Exception ex)
        {
            Services.PluginLog.Error(ex, "SoundMixer: EnforceTrackedVolumes failed");
        }
    }

    /// <summary>
    /// Single pass over ActiveSoundData: tracked enforce, configured group enforce (cached), footsteps (throttled).
    /// </summary>
    private void EnforceActiveSoundListVolumes(SoundData* listHead, bool enforceFootstepsThisFrame)
    {
        SoundDataSafety.VisitSoundList(
            listHead,
            soundData =>
            {
                if (ShouldBypassSoundData(soundData))
                {
                    return true;
                }

                if (!SoundDataSafety.TryReadSoundData(
                        soundData,
                        out var isActive,
                        out var soundNumber,
                        out var fieldVolume))
                {
                    _activeEnforceCache.Remove((nint)soundData);
                    return true;
                }

                if (!isActive)
                {
                    _activeEnforceCache.Remove((nint)soundData);
                    return true;
                }

                var ptr = (nint)soundData;

                EnforceConfiguredVolumeForNode(soundData, ptr, soundNumber, fieldVolume);

                if (enforceFootstepsThisFrame
                    && soundNumber == FootstepPlaybackBridge.PlaybackSoundIndex)
                {
                    EnforceFootstepVolumeForNode(soundData);
                }

                return true;
            },
            listName: "active-enforce-merged"
        );
    }

    private void EnforceConfiguredVolumeForNode(
        SoundData* soundData,
        nint ptr,
        uint soundNumber,
        float fieldVolume
    )
    {
        if (_activeEnforceCache.IsConfiguredEnforcementStable(ptr, fieldVolume))
        {
            return;
        }

        var resolvedPath = string.Empty;
        float multiplier;

        if (!_activeEnforceCache.TryGetResolved(ptr, out resolvedPath, out soundNumber, out multiplier))
        {
            if (!TryResolveSafeSoundPath(soundData, out resolvedPath, out soundNumber)
                && !SoundVolumeTracker.TryGetTrackedPath(soundData, out resolvedPath))
            {
                _activeEnforceCache.Remove(ptr);
                return;
            }

            resolvedPath = Plugin.ResolveSoundPath(resolvedPath);
            var specificPath = PathResolver.BuildSpecificPath(resolvedPath, (int)soundNumber);
            multiplier = Plugin.VolumeCalculator.GetVolumeForSound(specificPath);
            _activeEnforceCache.RememberResolved(ptr, resolvedPath, soundNumber, multiplier);
        }

        if (Math.Abs(multiplier - 1.0f) < 0.001f)
        {
            return;
        }

        if (SoundVolumeTracker.ForceRefreshActiveSound(
                soundData,
                Plugin.VolumeCalculator,
                ApplyRefreshedVolume,
                resolvedPath)
            && SoundDataSafety.TryReadSoundData(soundData, out _, out _, out var afterField))
        {
            _activeEnforceCache.NoteEffectiveVolume(ptr, afterField);
        }
    }

    private void EnforceFootstepVolumeForNode(SoundData* soundData)
    {
        if (!TryResolveSafeSoundPath(soundData, out var nodePath, out _))
        {
            return;
        }

        if (!FootstepPlaybackBridge.IsFootMaterialPath(nodePath)
            && !SoundVolumeHelper.IsFootContainerPath(nodePath))
        {
            return;
        }

        SoundVolumeTracker.ForceRefreshActiveSound(
            soundData,
            Plugin.VolumeCalculator,
            ApplyRefreshedVolume
        );
    }

    private void EnforceTrackedSound(SoundData* soundData)
    {
        if (soundData == null)
        {
            return;
        }

        SoundVolumeTracker.EnforceActiveSound(soundData, Plugin.VolumeCalculator, ApplyRefreshedVolume);
    }

    /// <summary>
    /// Drop in-memory tracking/caches and restore pooled SoundData volumes when possible.
    /// Does not change saved config; use after UI/pool glitches instead of restarting the game.
    /// </summary>
    internal (int Released, int Refreshed) ClearRuntimeCache()
    {
        var canTouchNodes = !ZoneTransitionGuard.ShouldSkipSoundDataWrites();
        var released = SoundVolumeTracker.ReleaseAllTracked(restoreVolumes: canTouchNodes);
        OneShotPlayRegistry.Clear();
        StreamingBgmTracker.Clear();
        SoundBlacklist.ClearPointerCache();
        _pathFailureThrottle.Clear();
        KnownActiveMonitoredSounds.Clear();
        Plugin.VolumeCalculator.ClearCache();
        InvalidateActiveEnforceCache();
        Plugin.Api.ApplyLiveEffectiveState();

        var restoredPool = 0;
        var soundManager = SoundManager.Instance();
        if (canTouchNodes
            && soundManager != null
            && !ShouldSkipActiveSoundListScanning())
        {
            restoredPool += SoundVolumeTracker.RestoreAllInactivePoolVolumes(
                soundManager->InactiveSoundDataListHead
            );
        }

        var refreshed = Plugin.IsEffectivelyEnabled && canTouchNodes ? RefreshAllActiveSounds() : 0;
        Services.PluginLog.Info(
            $"SoundMixer: cleared runtime cache (released {released} tracked, restored {restoredPool} pooled UI nodes, refreshed {refreshed} active)"
        );
        return (released, refreshed);
    }

    internal int RefreshAllActiveSounds()
    {
        if (!Plugin.IsEffectivelyEnabled || ZoneTransitionGuard.ShouldSkipSoundDataMaintenance())
        {
            return 0;
        }

        Plugin.VolumeCalculator.ClearCache();
        InvalidateActiveEnforceCache();
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
        if (!Plugin.IsEffectivelyEnabled
            || string.IsNullOrWhiteSpace(groupId)
            || ZoneTransitionGuard.ShouldSkipSoundDataMaintenance())
        {
            return 0;
        }

        Plugin.VolumeCalculator.ClearCache();
        InvalidateActiveEnforceCache();

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
            // Playing nodes only — inactive pool entries can match the path but are not audible.
            refreshed += RefreshGroupSoundsInList(soundManager->ActiveSoundDataListHead, groupId);
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

                if (!TryResolveSafeSoundPath(soundData, out var resolvedPath, out soundNumber))
                {
                    return true;
                }

                if (!SoundBelongsToResolvedPath(resolvedPath, (int)soundNumber, groupId))
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

    private int ApplyScdSiblingVolumeRefresh(string scdPath)
    {
        if (ShouldSkipActiveSoundListProcessing()
            || string.IsNullOrWhiteSpace(scdPath))
        {
            return 0;
        }

        scdPath = Plugin.ResolveSoundPath(scdPath.ToLowerInvariant().Trim());
        var soundManager = SoundManager.Instance();
        if (soundManager == null)
        {
            return 0;
        }

        var applied = 0;
        SoundDataSafety.VisitSoundList(
            soundManager->ActiveSoundDataListHead,
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

                var nodePath = string.Empty;
                if (TryResolveSafeSoundPath(current, out nodePath, out soundNumber)
                    || SoundVolumeTracker.TryGetTrackedPath(current, out nodePath))
                {
                    nodePath = Plugin.ResolveSoundPath(nodePath);
                }

                if (string.IsNullOrWhiteSpace(nodePath)
                    || !PathResolver.ShareScdFile(nodePath, scdPath))
                {
                    return true;
                }

                var multiplier = Plugin.VolumeCalculator.GetVolumeForSound(
                    PathResolver.BuildSpecificPath(nodePath, (int)soundNumber)
                );
                if (Math.Abs(multiplier - 1.0f) < 0.001f)
                {
                    return true;
                }

                if (SoundVolumeTracker.CommitScaledVolume(
                        current,
                        Plugin.VolumeCalculator,
                        multiplier,
                        nodePath,
                        soundNumber,
                        ApplyRefreshedVolume)
                    || SoundVolumeTracker.ForceRefreshActiveSound(
                        current,
                        Plugin.VolumeCalculator,
                        ApplyRefreshedVolume,
                        nodePath))
                {
                    applied++;
                }

                return true;
            },
            listName: "scd-sibling"
        );

        return applied;
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

        if (!TryResolveSafeSoundPath(soundData, out var path, out soundNumber)
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

        if (soundData == null
            || ShouldBypassSoundData(soundData)
            || ZoneTransitionGuard.ShouldSkipSoundDataListAccess())
        {
            return false;
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
                var fileName = handle->FileName.ToString();
                var scdCached = string.Empty;

                if (SoundDataSafety.IsReadable((nint)handle, ResourceDataPointerOffset + IntPtr.Size))
                {
                    var scdData = Marshal.ReadIntPtr((nint)handle + ResourceDataPointerOffset);
                    if (scdData != nint.Zero
                        && Scds.TryGetValue(scdData, out var cachedPath)
                        && !string.IsNullOrWhiteSpace(cachedPath))
                    {
                        scdCached = cachedPath.ToLowerInvariant();
                    }
                }

                path = SoundVolumeHelper.ChooseNodeEnforcementPath(fileName, scdCached, soundNumber);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    ReportNodePathResolution(soundData, fileName, scdCached, path, soundNumber);
                    return true;
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

        if (SoundVolumeTracker.TryGetTrackedPath(soundData, out path))
        {
            return !string.IsNullOrWhiteSpace(path);
        }

        return false;
    }

    /// <summary>
    /// Resolve SCD path via safe reads only (never ISoundData.GetFileName). Failures can be reported to the monitor log.
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

        if (!string.IsNullOrWhiteSpace(reportContext))
        {
            ReportPathResolutionFailure(reportContext, soundData, soundNumber);
        }

        return false;
    }

    private int ApplyFootstepPlaybackVolumes(string materialPath)
    {
        if (ShouldSkipActiveSoundListProcessing()
            || string.IsNullOrWhiteSpace(materialPath)
            || !FootstepPlaybackBridge.IsFootMaterialPath(materialPath))
        {
            return 0;
        }

        var soundManager = SoundManager.Instance();
        if (soundManager == null)
        {
            return 0;
        }

        return ApplyFootstepPlaybackVolumesInList(soundManager->ActiveSoundDataListHead, materialPath);
    }

    private int ApplyFootstepPlaybackVolumesInList(SoundData* listHead, string materialPath)
    {
        var applied = 0;

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

                if (!TryResolveSafeSoundPath(current, out var nodePath, out soundNumber)
                    || !FootstepPlaybackBridge.IsFootPlaybackCandidate(nodePath, soundNumber, materialPath))
                {
                    return true;
                }

                if (SoundVolumeTracker.ForceRefreshActiveSound(
                        current,
                        Plugin.VolumeCalculator,
                        ApplyRefreshedVolume))
                {
                    applied++;
                }

                return true;
            },
            listName: "footstep-playback"
        );

        return applied;
    }

    private void ReportNodePathResolution(
        SoundData* soundData,
        string fileName,
        string scdCached,
        string chosenPath,
        uint soundNumber
    )
    {
        if (!Plugin.Config.EnableMonitoring
            || soundData == null
            || soundNumber != FootstepPlaybackBridge.PlaybackSoundIndex)
        {
            return;
        }

        if (!chosenPath.Contains("/foot/foot", StringComparison.Ordinal)
            && !fileName.Contains("/foot/foot", StringComparison.OrdinalIgnoreCase)
            && !scdCached.Contains("/foot/foot", StringComparison.Ordinal))
        {
            return;
        }

        var ptr = (nint)soundData;
        var now = DateTime.UtcNow;
        if (_pathFailureThrottle.TryGetValue(ptr, out var lastReported)
            && (now - lastReported).TotalSeconds < 2.0)
        {
            return;
        }

        _pathFailureThrottle[ptr] = now;

        var resolvedPath = chosenPath.ToLowerInvariant();
        var specificPath = PathResolver.BuildSpecificPath(resolvedPath, (int)soundNumber);
        var multiplier = Plugin.VolumeCalculator.GetVolumeForSound(specificPath);

        var detail = $"fn={(string.IsNullOrWhiteSpace(fileName) ? "-" : fileName)} | scd={(string.IsNullOrWhiteSpace(scdCached) ? "-" : scdCached)}";

        EnqueueMonitoredSound(
            resolvedPath,
            (int)soundNumber,
            specificPath,
            multiplier,
            SoundMonitorHookIds.PathResolve,
            isStreaming: true
        );

        Services.PluginLog.Verbose($"SoundMixer: enforce path /{soundNumber} -> {chosenPath} ({detail})");
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
            scdCache
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
        if (SoundVolumeTracker.ShouldSkipVolumeEnforcement(soundData)
            || ShouldBypassSoundData(soundData)
            || !SoundDataSafety.IsValidForVolumeWrite(soundData))
        {
            return;
        }

        // Mod / PlaySound nodes often ignore raw field writes; route through native SetVolume
        // (s_directEffectiveVolumeApply avoids detour re-scaling). Mount/blacklist paths still
        // bail out in ApplyDirectEngineVolume / ShouldSkipVolumeEnforcement.
        ApplyDirectEngineVolume(soundData, effectiveVolume);
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
        SoundEnforcement.SetResolver(null);
        PlaySpecificSoundHook?.Dispose();
        PlaySoundHook?.Dispose();
        PlaySystemSoundHook?.Dispose();
        PlayClipSoundHook?.Dispose();
        PlayMovieSoundHook?.Dispose();
        PlayBGMSoundHook?.Dispose();
        PlayWeatherSoundHook?.Dispose();
        SetVolumeHook?.Dispose();
        GetVolumeHook?.Dispose();
        NativeSetVolumeFallback = null;
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

        NativeSetVolumeFallback = Marshal.GetDelegateForFunctionPointer<SetVolumeDelegate>(setVolumeAddr);
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
        if (!Plugin.IsEffectivelyEnabled
            || SetVolumeHook == null
            || ZoneTransitionGuard.ShouldSkipSoundDataMaintenance())
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
        if (soundData == null
            || Math.Abs(multiplier - 1.0f) < 0.001f
            || ZoneTransitionGuard.ShouldSkipSoundDataWrites())
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

        var effectiveVolume = Plugin.Config.ClampVolumeToEngineCap(
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
        if (ZoneTransitionGuard.ShouldSkipSoundDataWrites()
            || SoundVolumeTracker.ShouldSkipVolumeEnforcement(soundData)
            || ShouldBypassSoundData(soundData))
        {
            return;
        }

        if (SetVolumeHook == null && NativeSetVolumeFallback == null)
        {
            TryInstallSetVolumeHook();
        }

        SoundVolumeTracker.ApplyEngineVolume(soundData, effectiveVolume, TryInvokeNativeSetVolume);
    }

    private void TryInvokeNativeSetVolume(SoundData* soundData, float volume)
    {
        if (!SoundDataSafety.IsValidForHook(soundData)
            || ShouldBypassSoundDataVolumeHooks()
            || SoundBlacklist.IsPlayBypassActive
            || (SetVolumeHook == null && NativeSetVolumeFallback == null))
        {
            return;
        }

        s_directEffectiveVolumeApply = true;
        try
        {
            if (SetVolumeHook != null)
            {
                SetVolumeHook.Original(soundData, volume, 0);
            }
            else
            {
                NativeSetVolumeFallback!(soundData, volume, 0);
            }
        }
        catch (Exception ex)
        {
            Services.PluginLog.Verbose(ex, "SoundMixer: direct native SetVolume failed");
        }
        finally
        {
            s_directEffectiveVolumeApply = false;
        }
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
            if (fadeDuration == 0)
            {
                SoundVolumeTracker.ApplyEngineVolume(self, volume);
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
                if (!Plugin.VolumeCalculator.RequiresAnyVolumeScaling())
                {
                    PassthroughSetVolume(self, volume, fadeDuration);
                    return;
                }

                string resolvedPath = string.Empty;
                uint soundNumber = 0;
                var hasResolvedPath = SoundVolumeTracker.TryGetTrackedPath(self, out resolvedPath);
                if (!hasResolvedPath)
                {
                    hasResolvedPath = TryResolveSafeSoundPath(self, out resolvedPath, out soundNumber);
                }
                else
                {
                    SoundDataSafety.TryReadSoundData(self, out _, out soundNumber, out _);
                }

                if (hasResolvedPath)
                {
                    var enforcementMult = SoundVolumeHelper.GetEnforcementMultiplier(
                        Plugin.VolumeCalculator,
                        resolvedPath,
                        soundNumber
                    );
                    if (SoundVolumeTracker.TryGetTrackedPath(self, out _)
                        || StreamingBgmTracker.IsBgmOrMusicPath(resolvedPath)
                        || FootstepPlaybackBridge.IsFootMaterialPath(resolvedPath)
                        || SoundVolumeHelper.IsFootContainerPath(resolvedPath)
                        || Math.Abs(enforcementMult - 1.0f) > 0.001f)
                    {
                        SoundVolumeTracker.TrackPlayPath(self, resolvedPath, soundNumber);
                    }
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
                volume = Plugin.Config.ClampVolumeToEngineCap(
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

            if (fadeDuration == 0)
            {
                try
                {
                    SoundVolumeTracker.ApplyEngineVolume(self, volume);
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

            if (!Plugin.VolumeCalculator.RequiresAnyVolumeScaling())
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
                if (FootstepPlaybackBridge.IsFootMaterialPath(path))
                {
                    ApplyFootstepPlaybackVolumes(path);
                    if (result != null
                        && SoundDataSafety.IsReadable((nint)result)
                        && SoundVolumeTracker.ForceRefreshActiveSound(
                            (SoundData*)result,
                            Plugin.VolumeCalculator,
                            ApplyRefreshedVolume))
                    {
                        return result;
                    }
                }

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

                if (Math.Abs(multiplier - 1.0f) > 0.001f
                    && !FootstepPlaybackBridge.IsFootMaterialPath(path))
                {
                    ApplyScdSiblingVolumeRefresh(path);
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

        if (!SoundVolumeHelper.WasSetVolumeCalledThisPlay
            && SoundVolumeTracker.ForceRefreshActiveSound(
                soundData,
                Plugin.VolumeCalculator,
                ApplyRefreshedVolume))
        {
            return true;
        }

        if (ResolveSoundEnforcement(soundData, Plugin.VolumeCalculator, out var resolved))
        {
            SoundVolumeTracker.PrepareTrackedForPlay(soundData, resolved.ResolvedPath, scdSoundIndex);
        }

        return true;
    }

    /// <summary>
    /// PlaySpecificSound only knows the container SCD pointer; the active node may be playing a child file.
    /// </summary>
    private void ResolvePlaySpecificTarget(
        SoundData* soundData,
        uint scdSoundIndex,
        string playSpecificPath,
        out string effectivePath,
        out float effectiveMultiplier
    )
    {
        effectivePath = playSpecificPath;
        var targetIndex = (int)scdSoundIndex;
        effectiveMultiplier = Plugin.VolumeCalculator.GetVolumeForSound(
            PathResolver.BuildSpecificPath(playSpecificPath, targetIndex)
        );

        if (TryResolveSafeSoundPath(soundData, out var resolvedPath, out var soundNumber))
        {
            effectivePath = Plugin.ResolveSoundPath(resolvedPath);
            targetIndex = soundNumber != 0 ? (int)soundNumber : targetIndex;
        }
        else
        {
            var livePath = SoundVolumeHelper.GetPathFromSoundData(soundData);
            if (!string.IsNullOrWhiteSpace(livePath))
            {
                effectivePath = Plugin.ResolveSoundPath(livePath);
            }
        }

        effectivePath = SoundVolumeHelper.UpgradeEnforcementPath(
            PathResolver.GetScdBasePath(effectivePath),
            (uint)targetIndex
        );
        effectiveMultiplier = SoundVolumeHelper.GetEnforcementMultiplier(
            Plugin.VolumeCalculator,
            effectivePath,
            (uint)targetIndex
        );
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

                ResolvePlaySpecificTarget(current, scdSoundIndex, path, out var effectivePath, out var effectiveMultiplier);
                ApplyMultiplierToSoundData(current, effectiveMultiplier, effectivePath);

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

                if (soundNumber != scdSoundIndex)
                {
                    return true;
                }

                ResolvePlaySpecificTarget(current, scdSoundIndex, path, out var effectivePath, out _);
                SoundVolumeTracker.PrepareTrackedForPlay(current, effectivePath, soundNumber);
                tracked = true;
                return false;
            },
            listName: "track-path"
        );

        return tracked;
    }

    private void ApplyMultiplierToSoundData(SoundData* soundData, float multiplier, string? scdPath = null)
    {
        if (SoundBlacklist.IsPlayBypassActive
            || ShouldBypassSoundData(soundData)
            || ZoneTransitionGuard.ShouldSkipSoundDataWrites())
        {
            return;
        }

        if (!SoundDataSafety.TryReadSoundData(soundData, out _, out var soundNumber, out _))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(scdPath))
        {
            var baseGameVolume = 0f;
            SoundVolumeHelper.TryGetActivePlayBaseVolume(out baseGameVolume);
            if (!SoundVolumeTracker.CommitScaledVolume(
                    soundData,
                    Plugin.VolumeCalculator,
                    multiplier,
                    scdPath,
                    soundNumber,
                    ApplyRefreshedVolume,
                    baseGameVolume)
                && !SoundVolumeTracker.ForceRefreshActiveSound(
                    soundData,
                    Plugin.VolumeCalculator,
                    ApplyRefreshedVolume,
                    scdPath))
            {
                return;
            }

            ApplyScdSiblingVolumeRefresh(scdPath);
            return;
        }

        SoundVolumeTracker.ForceRefreshActiveSound(
            soundData,
            Plugin.VolumeCalculator,
            ApplyRefreshedVolume,
            scdPath
        );
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

        MountTransitionGuard.NotifyMountSound(path);
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

        SoundVolumeHelper.NoteRecentPlaySpecific(path, (uint)idx, multiplier);

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
            {
                var category = scaledVolume > 1.001f && Plugin.IsEffectivelyEnabled
                    ? SoundVolumeCategory.BypassVolumeRules
                    : volumeCategory;
                return PlaySoundHook!.Original(
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
                    category,
                    a13,
                    midiNote,
                    a15,
                    defaultFadeOut,
                    isPositional,
                    a18
                );
            }
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
            {
                var category = scaledVolume > 1.001f && Plugin.IsEffectivelyEnabled
                    ? SoundVolumeCategory.BypassVolumeRules
                    : volumeCategory;
                return PlaySystemSoundHook!.Original(
                    self,
                    path,
                    scaledVolume,
                    soundNumber,
                    fadeInDuration,
                    autoRelease,
                    category
                );
            }
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

                MountTransitionGuard.NotifyMountSound(normalizedPath);
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

                MountTransitionGuard.NotifyMountSound(normalizedPath);
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

    private bool TryUpgradePlayPathFromSoundData(
        SoundData* result,
        ref string normalizedPath,
        ref float multiplier,
        int soundNumber
    )
    {
        if (!TryResolveSafeSoundPath(result, out var resolvedPath, out var resolvedNumber))
        {
            return false;
        }

        resolvedPath = Plugin.ResolveSoundPath(resolvedPath);
        if (string.Equals(resolvedPath, normalizedPath, StringComparison.Ordinal))
        {
            return false;
        }

        normalizedPath = resolvedPath;
        var targetIndex = resolvedNumber != 0 ? (int)resolvedNumber : soundNumber;
        multiplier = Plugin.VolumeCalculator.GetVolumeForSound(
            PathResolver.BuildSpecificPath(normalizedPath, targetIndex)
        );
        return true;
    }

    private void ApplyPostPlayVolume(
        SoundData* result,
        string normalizedPath,
        float multiplier,
        uint soundNumber,
        float gameVolume
    )
    {
        var isMusicPath = StreamingBgmTracker.IsBgmOrMusicPath(normalizedPath);
        if (isMusicPath)
        {
            StreamingBgmTracker.NotePlay(normalizedPath, multiplier, result, soundNumber);
        }

        SoundVolumeTracker.PrepareTrackedForPlay(result, normalizedPath, soundNumber, gameVolume);
        if (Math.Abs(multiplier - 1.0f) <= 0.001f)
        {
            return;
        }

        if (isMusicPath)
        {
            SoundVolumeTracker.ForceRefreshActiveSound(
                result,
                Plugin.VolumeCalculator,
                ApplyRefreshedVolume,
                normalizedPath
            );
        }
        else
        {
            ApplyMultiplierToSoundData(result, multiplier, normalizedPath);
        }

        ApplyScdSiblingVolumeRefresh(normalizedPath);
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

                MountTransitionGuard.NotifyMountSound(normalizedPath);

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
                        oneShotPlayVolume = Plugin.Config.ClampVolumeToEngineCap(
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
                    SoundVolumeHelper.BeginPlay(normalizedPath, soundNumber, multiplier, volume);
                }
            }

            TryInstallSetVolumeHookFromPlayPath();

            var playVolume = volume;
            if (Plugin.IsEffectivelyEnabled
                && !string.IsNullOrWhiteSpace(normalizedPath)
                && multiplier > 1.0f + 0.001f)
            {
                playVolume = Plugin.Config.ClampVolumeToEngineCap(
                    SoundVolumeHelper.ScaleVolume(volume, multiplier)
                );
            }

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
                if (TryUpgradePlayPathFromSoundData(
                        result,
                        ref normalizedPath,
                        ref multiplier,
                        (int)soundNumber)
                    && Math.Abs(multiplier - 1.0f) > 0.001f
                    && multiplier > 1.0f + 0.001f)
                {
                    playVolume = Plugin.Config.ClampVolumeToEngineCap(
                        SoundVolumeHelper.ScaleVolume(volume, multiplier)
                    );
                    TryInvokeNativeSetVolume(result, playVolume);
                }

                ApplyPostPlayVolume(result, normalizedPath, multiplier, soundNumber, volume);
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
            _monitoringSeenBuffer.Clear();
            var seen = _monitoringSeenBuffer;
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
                CacheScdPath(scdData, strPath);
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
                    CacheScdPath(dataPtr, name);
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
