using System;
using System.Collections.Concurrent;
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

    internal bool PlaySoundHookActive => PlaySoundHook != null;
    internal bool PlaySystemSoundHookActive => PlaySystemSoundHook != null;
    internal bool PlayClipSoundHookActive => PlayClipSoundHook != null;
    internal bool PlayMovieSoundHookActive => PlayMovieSoundHook != null;
    internal bool PlayBgmSoundHookActive => PlayBGMSoundHook != null;
    internal bool PlayWeatherSoundHookActive => PlayWeatherSoundHook != null;
    internal bool SetVolumeHookActive => SetVolumeHook != null;
    internal bool GetVolumeHookActive => GetVolumeHook != null;

    private bool SoundDataHooksInstalled { get; set; }

    private readonly ConcurrentDictionary<nint, string> KnownActiveMonitoredSounds = new();
    private DateTime _lastActiveScanUtc;

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
        if (
            PlaySpecificSoundHook == null
            && Services.SigScanner.TryScanText(Signatures.PlaySpecificSound, out var playPtr)
        )
        {
            PlaySpecificSoundHook =
                Services.GameInteropProvider.HookFromAddress<PlaySpecificSoundDelegate>(
                    playPtr,
                    PlaySpecificSoundDetour
                );
        }

        TryCreateCallHook(Signatures.PlaySound, PlaySoundDetour, ref PlaySoundHook);
        TryCreateCallHook(Signatures.PlaySystemSound, PlaySystemSoundDetour, ref PlaySystemSoundHook);
        TryCreateCallHook(Signatures.PlayClipSound, PlayClipSoundDetour, ref PlayClipSoundHook);
        TryCreateCallHook(Signatures.PlayMovieSound, PlayMovieSoundDetour, ref PlayMovieSoundHook);
        TryCreateCallHook(Signatures.PlayBGMSound, PlayBGMSoundDetour, ref PlayBGMSoundHook);
        TryCreateCallHook(Signatures.PlayWeatherSound, PlayWeatherSoundDetour, ref PlayWeatherSoundHook);

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

        PlaySpecificSoundHook?.Enable();
        PlaySoundHook?.Enable();
        PlaySystemSoundHook?.Enable();
        PlayClipSoundHook?.Enable();
        PlayMovieSoundHook?.Enable();
        PlayBGMSoundHook?.Enable();
        PlayWeatherSoundHook?.Enable();
        LoadSoundFileHook?.Enable();
        GetResourceSyncHook?.Enable();
        GetResourceAsyncHook?.Enable();
        TryInstallSetVolumeHook();
        SetVolumeHook?.Enable();
        GetVolumeHook?.Enable();

        Services.PluginLog.Info(
            $"SoundMixer: hooks enabled (PlaySpecific={PlaySpecificSoundHook != null}, "
                + $"PlaySound={PlaySoundHook != null}, PlaySystem={PlaySystemSoundHook != null}, "
                + $"PlayClip={PlayClipSoundHook != null}, PlayMovie={PlayMovieSoundHook != null}, "
                + $"PlayBGM={PlayBGMSoundHook != null}, PlayWeather={PlayWeatherSoundHook != null}, "
                + $"SetVolume={SetVolumeHook != null}, GetVolume={GetVolumeHook != null})"
        );
    }

    internal void EnforceTrackedVolumes()
    {
        var soundManager = SoundManager.Instance();
        if (soundManager == null)
        {
            return;
        }

        if (Plugin.Config.Enabled)
        {
            EnforceTrackedVolumesInList(soundManager->ActiveSoundDataListHead);
            EnforceTrackedSound(soundManager->WeatherSoundData);
        }

        ScanActiveSoundsForMonitoring(soundManager);
        SoundVolumeTracker.PruneInactive();
        PruneInactiveMonitoredSounds(soundManager);
    }

    private void EnforceTrackedSound(SoundData* soundData)
    {
        if (soundData == null)
        {
            return;
        }

        SoundVolumeTracker.EnforceActiveSound(soundData, Plugin.VolumeCalculator);
    }

    private void EnforceTrackedVolumesInList(SoundData* listHead)
    {
        for (
            ISoundData* node = (ISoundData*)listHead;
            node != null;
            node = node->Next
        )
        {
            SoundVolumeTracker.EnforceActiveSound((SoundData*)node, Plugin.VolumeCalculator);
        }
    }

    internal int RefreshAllActiveSounds()
    {
        if (!Plugin.Config.Enabled)
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
        refreshed += RefreshSoundsInList(soundManager->ActiveSoundDataListHead);
        refreshed += RefreshSoundsInList(soundManager->InactiveSoundDataListHead);

        if (soundManager->WeatherSoundData != null
            && SoundVolumeTracker.ForceRefreshActiveSound(
                soundManager->WeatherSoundData,
                Plugin.VolumeCalculator,
                ApplyRefreshedVolume
            ))
        {
            refreshed++;
        }

        Services.PluginLog.Info($"SoundMixer: refreshed {refreshed} active sounds");
        return refreshed;
    }

    internal int RefreshGroupSounds(string groupId)
    {
        if (!Plugin.Config.Enabled || string.IsNullOrWhiteSpace(groupId))
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
        refreshed += RefreshGroupSoundsInList(soundManager->ActiveSoundDataListHead, groupId);
        refreshed += RefreshGroupSoundsInList(soundManager->InactiveSoundDataListHead, groupId);

        if (soundManager->WeatherSoundData != null
            && SoundBelongsToGroup(soundManager->WeatherSoundData, groupId)
            && SoundVolumeTracker.ForceRefreshActiveSound(
                soundManager->WeatherSoundData,
                Plugin.VolumeCalculator,
                ApplyRefreshedVolume
            ))
        {
            refreshed++;
        }

        Services.PluginLog.Info($"SoundMixer: refreshed {refreshed} sounds for group {groupId}");
        return refreshed;
    }

    private int RefreshSoundsInList(SoundData* listHead)
    {
        var refreshed = 0;

        for (
            ISoundData* node = (ISoundData*)listHead;
            node != null;
            node = node->Next
        )
        {
            if (SoundVolumeTracker.ForceRefreshActiveSound(
                    (SoundData*)node,
                    Plugin.VolumeCalculator,
                    ApplyRefreshedVolume
                ))
            {
                refreshed++;
            }
        }

        return refreshed;
    }

    private int RefreshGroupSoundsInList(SoundData* listHead, string groupId)
    {
        var refreshed = 0;

        for (
            ISoundData* node = (ISoundData*)listHead;
            node != null;
            node = node->Next
        )
        {
            var soundData = (SoundData*)node;
            if (!SoundBelongsToGroup(soundData, groupId))
            {
                continue;
            }

            if (SoundVolumeTracker.ForceRefreshActiveSound(
                    soundData,
                    Plugin.VolumeCalculator,
                    ApplyRefreshedVolume
                ))
            {
                refreshed++;
            }
        }

        return refreshed;
    }

    private bool SoundBelongsToGroup(SoundData* soundData, string groupId)
    {
        if (soundData == null || !soundData->IsActive)
        {
            return false;
        }

        var path = SoundVolumeHelper.GetPathFromSoundData(soundData);
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var resolvedPath = PathResolver.ResolveScdPath(Plugin.Config, path);
        var specificPath = PathResolver.BuildSpecificPath(resolvedPath, (int)soundData->SoundNumber);
        return Plugin.VolumeCalculator.SoundBelongsToGroupTree(specificPath, groupId)
               || Plugin.VolumeCalculator.SoundBelongsToGroupTree(resolvedPath, groupId);
    }

    private void ApplyRefreshedVolume(SoundData* soundData, float effectiveVolume)
    {
        if (SoundDataHooksInstalled && SetVolumeHook != null)
        {
            SetVolumeHook.Original(soundData, effectiveVolume, 0);
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
        Services.PluginLog.Info("SoundMixer: Filter hooks disabled");
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

    private void TryCreateCallHook<T>(
        string callSignature,
        T detour,
        ref Hook<T>? hookField
    )
        where T : Delegate
    {
        if (hookField != null)
        {
            return;
        }

        if (!Services.SigScanner.TryResolveCallTarget(callSignature, out var target))
        {
            Services.PluginLog.Warning($"SoundMixer: failed to resolve call target for {callSignature}");
            return;
        }

        hookField = Services.GameInteropProvider.HookFromAddress(target, detour);
    }

    private void TryInstallSetVolumeHook()
    {
        if (SoundDataHooksInstalled)
        {
            SetVolumeHook?.Enable();
            GetVolumeHook?.Enable();
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
        SetVolumeHook.Enable();

        if (getVolumeAddr != nint.Zero)
        {
            GetVolumeHook = Services.GameInteropProvider.HookFromAddress<GetVolumeDelegate>(
                getVolumeAddr,
                GetVolumeDetour
            );
            GetVolumeHook.Enable();
        }
        else
        {
            Services.PluginLog.Warning("SoundMixer: failed to resolve SoundData.GetVolume");
        }

        SoundDataHooksInstalled = true;
        Services.PluginLog.Info(
            $"SoundMixer: SoundData hooks at GetVolume={getVolumeAddr:X}, SetVolume={setVolumeAddr:X}"
        );
    }

    private void SetVolumeDetour(SoundData* self, float volume, uint fadeDuration)
    {
        if (Plugin.Config.Enabled)
        {
            try
            {
                var gameVolume = volume;
                var multiplier = SoundVolumeTracker.GetMultiplier(self, Plugin.VolumeCalculator);
                volume = Configuration.ClampToEngineCap(
                    SoundVolumeHelper.ScaleVolume(volume, multiplier)
                );
                SoundVolumeTracker.Register(
                    self,
                    Plugin.VolumeCalculator,
                    gameVolume,
                    volume
                );
                SoundVolumeHelper.MarkSetVolumeCalled();
            }
            catch (Exception ex)
            {
                Services.PluginLog.Error(ex, "Error in SetVolumeDetour");
            }
        }

        SetVolumeHook!.Original(self, volume, fadeDuration);

        if (Plugin.Config.Enabled)
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

    private float GetVolumeDetour(SoundData* self)
    {
        var rawVolume = GetVolumeHook!.Original(self);
        if (!Plugin.Config.Enabled)
        {
            return rawVolume;
        }

        try
        {
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
        var multiplier = 1.0f;
        var path = string.Empty;

        try
        {
            multiplier = PlaySpecificSoundDetourInner(a1, idx, out path);
            if (Plugin.Config.Enabled)
            {
                SoundVolumeHelper.BeginPlay(path, (uint)idx, multiplier);
            }
        }
        catch (Exception ex)
        {
            Services.PluginLog.Error(ex, "Error in PlaySpecificSoundDetour");
        }

        TryInstallSetVolumeHook();

        try
        {
            var result = PlaySpecificSoundHook!.Original(a1, idx);

            if (Plugin.Config.Enabled && Math.Abs(multiplier - 1.0f) > 0.001f)
            {
                if (!SoundVolumeHelper.WasSetVolumeCalledThisPlay)
                {
                    ApplyVolumeToActiveSounds((uint)idx, path, multiplier);
                }
            }

            return result;
        }
        finally
        {
            SoundVolumeHelper.EndPlay();
        }
    }

    private void ApplyVolumeToActiveSounds(uint scdSoundIndex, string path, float multiplier)
    {
        var soundManager = SoundManager.Instance();
        if (soundManager == null)
        {
            return;
        }

        if (TryApplyVolumeInList(soundManager->ActiveSoundDataListHead, scdSoundIndex, path, multiplier))
        {
            return;
        }

        TryApplyVolumeInList(soundManager->InactiveSoundDataListHead, scdSoundIndex, path, multiplier);
    }

    private bool TryApplyVolumeInList(
        SoundData* listHead,
        uint scdSoundIndex,
        string path,
        float multiplier
    )
    {
        SoundData* fallbackMatch = null;

        for (
            ISoundData* node = (ISoundData*)listHead;
            node != null;
            node = node->Next
        )
        {
            var current = (SoundData*)node;
            var dataPath = SoundVolumeHelper.GetPathFromSoundData(current);

            if (!string.IsNullOrWhiteSpace(path))
            {
                if (string.IsNullOrWhiteSpace(dataPath) || !PathsMatch(path, dataPath))
                {
                    continue;
                }

                if (current->SoundNumber == scdSoundIndex)
                {
                    ApplyMultiplierToSoundData(current, multiplier);
                    return true;
                }

                fallbackMatch = current;
                continue;
            }

            if (current->SoundNumber == scdSoundIndex)
            {
                ApplyMultiplierToSoundData(current, multiplier);
                return true;
            }
        }

        if (fallbackMatch != null)
        {
            ApplyMultiplierToSoundData(fallbackMatch, multiplier);
            return true;
        }

        return false;
    }

    private void ApplyMultiplierToSoundData(SoundData* soundData, float multiplier)
    {
        var gameVolume = soundData->Volume;
        var effectiveVolume = Configuration.ClampToEngineCap(
            SoundVolumeHelper.ScaleVolume(gameVolume, multiplier)
        );
        SoundVolumeTracker.Register(
            soundData,
            Plugin.VolumeCalculator,
            gameVolume,
            effectiveVolume
        );

        if (SoundDataHooksInstalled)
        {
            SetVolumeHook!.Original(soundData, effectiveVolume, 0);
        }

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
        if (a1 == 0)
        {
            return 1.0f;
        }

        var scdData = *(byte**)(a1 + 8);
        if (scdData == null)
        {
            return 1.0f;
        }

        var scdPtr = (nint)scdData;
        if (!Scds.TryGetValue(scdPtr, out path))
        {
            path = $"unknown/{scdPtr:X}";
        }

        path = PathResolver.ResolveScdPath(Plugin.Config, path, scdPtr);
        var specificPath = PathResolver.BuildSpecificPath(path, idx);
        var multiplier = Plugin.VolumeCalculator.GetVolumeForSound(specificPath);

        if (Plugin.Config.EnableMonitoring)
        {
            EnqueueMonitoredSound(path, idx, specificPath, multiplier);
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
                normalizedPath = PathResolver.ResolveScdPath(
                    Plugin.Config,
                    SoundVolumeHelper.NormalizePath(path)
                );
                multiplier = ResolveScdPathMultiplier(normalizedPath);

                if (Plugin.Config.Enabled)
                {
                    SoundVolumeHelper.BeginPlay(normalizedPath, 0, multiplier);
                }
            }

            TryInstallSetVolumeHook();
            var result = PlayBGMSoundHook!.Original(self, path);

            if (
                result != null
                && Plugin.Config.Enabled
                && Math.Abs(multiplier - 1.0f) > 0.001f
                && !SoundVolumeHelper.WasSetVolumeCalledThisPlay
            )
            {
                ApplyMultiplierToSoundData(result, multiplier);
            }

            if (Plugin.Config.EnableMonitoring && !string.IsNullOrEmpty(normalizedPath))
            {
                var soundNumber = result != null ? (int)result->SoundNumber : 0;
                var specificPath = PathResolver.BuildSpecificPath(normalizedPath, soundNumber);
                EnqueueMonitoredSound(normalizedPath, soundNumber, specificPath, multiplier);
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
                normalizedPath = PathResolver.ResolveScdPath(
                    Plugin.Config,
                    SoundVolumeHelper.NormalizePath(path)
                );
            }

            PlayWeatherSoundHook!.Original(self, path, fadeDuration);

            if (string.IsNullOrEmpty(normalizedPath))
            {
                return;
            }

            var weatherSound = self->WeatherSoundData;
            if (weatherSound != null && Plugin.Config.Enabled)
            {
                var multiplier = ResolveScdPathMultiplier(normalizedPath);
                if (Math.Abs(multiplier - 1.0f) > 0.001f)
                {
                    ApplyMultiplierToSoundData(weatherSound, multiplier);
                }
            }

            if (Plugin.Config.EnableMonitoring && weatherSound != null)
            {
                var soundNumber = (int)weatherSound->SoundNumber;
                var specificPath = PathResolver.BuildSpecificPath(normalizedPath, soundNumber);
                var multiplier = Plugin.VolumeCalculator.GetVolumeForSound(specificPath);
                EnqueueMonitoredSound(normalizedPath, soundNumber, specificPath, multiplier);
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
        PlayOriginalWithVolumeDelegate playOriginal
    )
    {
        var normalizedPath = string.Empty;
        var multiplier = 1.0f;

        try
        {
            if (path != null)
            {
                normalizedPath = PathResolver.ResolveScdPath(
                    Plugin.Config,
                    SoundVolumeHelper.NormalizePath(path)
                );
                var specificPath = PathResolver.BuildSpecificPath(normalizedPath, (int)soundNumber);
                multiplier = Plugin.VolumeCalculator.GetVolumeForSound(specificPath);

                if (Plugin.Config.EnableMonitoring)
                {
                    EnqueueMonitoredSound(normalizedPath, (int)soundNumber, specificPath, multiplier);
                }

                if (Plugin.Config.Enabled)
                {
                    SoundVolumeHelper.BeginPlay(normalizedPath, soundNumber, multiplier);
                }
            }

            TryInstallSetVolumeHook();
            var result = playOriginal(volume);

            if (
                result != null
                && Plugin.Config.Enabled
                && Math.Abs(multiplier - 1.0f) > 0.001f
                && !SoundVolumeHelper.WasSetVolumeCalledThisPlay
            )
            {
                ApplyMultiplierToSoundData(result, multiplier);
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

        var seen = new HashSet<nint>();
        ScanSoundListForMonitoring(soundManager->ActiveSoundDataListHead, seen);
        ScanSingleSoundForMonitoring(soundManager->WeatherSoundData, seen);

        foreach (var ptr in KnownActiveMonitoredSounds.Keys)
        {
            if (!seen.Contains(ptr))
            {
                KnownActiveMonitoredSounds.TryRemove(ptr, out _);
            }
        }
    }

    private void PruneInactiveMonitoredSounds(SoundManager* soundManager)
    {
        foreach (var ptr in KnownActiveMonitoredSounds.Keys)
        {
            var soundData = (SoundData*)ptr;
            if (soundData == null || !soundData->IsActive)
            {
                KnownActiveMonitoredSounds.TryRemove(ptr, out _);
            }
        }
    }

    private void ScanSoundListForMonitoring(SoundData* listHead, HashSet<nint> seen)
    {
        for (
            ISoundData* node = (ISoundData*)listHead;
            node != null;
            node = node->Next
        )
        {
            ScanSingleSoundForMonitoring((SoundData*)node, seen);
        }
    }

    private void ScanSingleSoundForMonitoring(SoundData* soundData, HashSet<nint> seen)
    {
        if (soundData == null || !soundData->IsActive)
        {
            return;
        }

        var ptr = (nint)soundData;
        seen.Add(ptr);

        var path = SoundVolumeHelper.GetPathFromSoundData(soundData);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var resolvedPath = PathResolver.ResolveScdPath(Plugin.Config, path);
        var soundNumber = (int)soundData->SoundNumber;
        var specificPath = PathResolver.BuildSpecificPath(resolvedPath, soundNumber);
        var fingerprint = $"{specificPath}#{soundNumber}";

        if (KnownActiveMonitoredSounds.TryGetValue(ptr, out var previous) && previous == fingerprint)
        {
            return;
        }

        KnownActiveMonitoredSounds[ptr] = fingerprint;
        var multiplier = Plugin.VolumeCalculator.GetVolumeForSound(specificPath);
        EnqueueMonitoredSound(resolvedPath, soundNumber, specificPath, multiplier, isStreaming: true);
    }

    private void EnqueueMonitoredSound(
        string path,
        int idx,
        string specificPath,
        float multiplier,
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
        if (ret != null && strPath.EndsWith(".scd", StringComparison.OrdinalIgnoreCase))
        {
            var scdData = Marshal.ReadIntPtr((nint)ret + ResourceDataPointerOffset);
            if (scdData != nint.Zero)
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
            var handle = (ResourceHandle*)resourceHandle;
            var name = handle->FileName.ToString();
            if (name.EndsWith(".scd", StringComparison.OrdinalIgnoreCase))
            {
                var dataPtr = Marshal.ReadIntPtr(resourceHandle + ResourceDataPointerOffset);
                Scds[dataPtr] = name;
            }
        }
        catch (Exception ex)
        {
            Services.PluginLog.Error(ex, "Error in LoadSoundFileDetour");
        }

        return ret;
    }
}
