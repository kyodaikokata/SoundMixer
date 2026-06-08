using System.Collections.Concurrent;
using FFXIVClientStructs.FFXIV.Client.Sound;

namespace SoundMixer;

internal sealed class TrackedSoundVolume
{
    internal string ScdPath { get; set; } = string.Empty;
    internal uint SoundNumber { get; set; }
    internal float LastGameVolume { get; set; }
    internal float LastEffectiveVolume { get; set; }
    internal float LastAppliedMultiplier { get; set; } = 1.0f;
    internal bool LastWriteByPlugin { get; set; }
    /// <summary>Play returned Volume=0; wait for fade-in to finish before forcing scaled volume.</summary>
    internal bool AwaitVolumeApply { get; set; }
}

internal static unsafe class SoundVolumeTracker
{
    private const int VolumeFadeTargetOffset = 0x64;

    private static readonly ConcurrentDictionary<nint, TrackedSoundVolume> Tracked = new();

    internal static float GetMultiplier(SoundData* soundData, VolumeCalculator calculator)
    {
        if (SoundBlacklist.IsPlayBypassActive || SoundBlacklist.ShouldBypassSoundData(soundData))
        {
            return 1.0f;
        }

        if (SoundEnforcement.TryResolve(soundData, calculator, out var resolved))
        {
            var ptr = (nint)soundData;
            if (Tracked.TryGetValue(ptr, out var tracked))
            {
                tracked.ScdPath = resolved.ResolvedPath;
                tracked.SoundNumber = resolved.SoundNumber;
            }

            return resolved.Multiplier;
        }

        return 1.0f;
    }

    /// <summary>
    /// Reset tracking when a SoundData node begins a new play (pooled one-shots such as UI SFX reuse pointers).
    /// </summary>
    internal static void PrepareTrackedForPlay(SoundData* soundData, string scdPath, uint soundNumber)
    {
        if (soundData == null || string.IsNullOrWhiteSpace(scdPath))
        {
            return;
        }

        if (ZoneTransitionGuard.ShouldSkipSoundDataTracking())
        {
            return;
        }

        if (IsUnsafeForVolumeEnforcement(scdPath))
        {
            return;
        }

        if (!SoundDataSafety.TryReadSoundData(soundData, out _, out var readNumber, out var fieldVolume))
        {
            return;
        }

        if (soundNumber == 0 && readNumber != 0)
        {
            soundNumber = readNumber;
        }

        scdPath = SoundVolumeHelper.ChooseAuthoritativePath(
            SoundVolumeHelper.GetPathFromSoundData(soundData),
            scdPath
        );

        SanitizeReusedPoolNode(soundData, fieldVolume);

        Tracked[(nint)soundData] = new TrackedSoundVolume
        {
            ScdPath = scdPath.ToLowerInvariant(),
            SoundNumber = soundNumber,
            LastGameVolume = fieldVolume > 0.001f ? fieldVolume : 1.0f,
            AwaitVolumeApply = fieldVolume <= 0.001f,
            LastWriteByPlugin = false,
            LastEffectiveVolume = 0f,
            LastAppliedMultiplier = 1.0f,
        };
    }

    internal static bool IsPooledOneShotPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || StreamingBgmTracker.IsBgmOrMusicPath(path))
        {
            return false;
        }

        var normalized = path.ToLowerInvariant();
        return normalized.Contains("/ui/", StringComparison.Ordinal)
            || normalized.Contains("/menu/", StringComparison.Ordinal)
            || normalized.Contains("se_ui", StringComparison.Ordinal)
            || normalized.Contains("system/se_ui", StringComparison.Ordinal);
    }

    /// <summary>
    /// Reset a reused pooled node before applying one-shot scaling for a new play.
    /// </summary>
    internal static void ResetPoolNodeForOneShotPlay(SoundData* soundData)
    {
        if (soundData == null || !SoundDataSafety.IsValidForVolumeWrite(soundData))
        {
            return;
        }

        if (!SoundDataSafety.TryReadSoundData(soundData, out _, out _, out var fieldVolume))
        {
            return;
        }

        SanitizeReusedPoolNode(soundData, fieldVolume);

        if (Tracked.ContainsKey((nint)soundData))
        {
            return;
        }

        if (fieldVolume > 1.02f || (fieldVolume > 0.001f && IsNativeFadeActive(soundData, fieldVolume)))
        {
            ApplyFieldVolume(soundData, 1.0f);
        }
    }

    /// <summary>
    /// Scale a one-shot node once without adding it to Tracked (pool-safe).
    /// </summary>
    internal static void ApplyOneShotScaledVolume(
        SoundData* soundData,
        float multiplier,
        string? scdPath = null
    )
    {
        if (soundData == null
            || Math.Abs(multiplier - 1.0f) < 0.001f
            || SoundBlacklist.IsPlayBypassActive
            || SoundBlacklist.ShouldBypassSoundData(soundData))
        {
            return;
        }

        if (!SoundDataSafety.TryReadSoundData(soundData, out _, out _, out var gameVolume))
        {
            return;
        }

        ResetPoolNodeForOneShotPlay(soundData);
        if (!SoundDataSafety.TryReadSoundData(soundData, out _, out _, out gameVolume))
        {
            return;
        }

        var effectiveVolume = Configuration.ClampToEngineCap(
            SoundVolumeHelper.ScaleVolume(gameVolume > 0.001f ? gameVolume : 1.0f, multiplier)
        );

        if (gameVolume <= 0.001f)
        {
            WriteFadeTarget(soundData, effectiveVolume);
            return;
        }

        ApplyFieldVolume(soundData, effectiveVolume);
    }

    internal static int ReleaseAllTracked(bool restoreVolumes = true)
    {
        var released = 0;
        var toRemove = new List<nint>();

        var canRestore = restoreVolumes && !ZoneTransitionGuard.ShouldSkipSoundDataWrites();
        foreach (var (ptr, tracked) in Tracked)
        {
            var soundData = (SoundData*)ptr;
            if (canRestore && SoundDataSafety.IsValidForVolumeWrite(soundData))
            {
                var restore = tracked.LastGameVolume > 0.001f ? tracked.LastGameVolume : 1.0f;
                ApplyFieldVolume(soundData, restore);
            }

            OneShotPlayRegistry.NotifyReleased(soundData);
            toRemove.Add(ptr);
            released++;
        }

        foreach (var ptr in toRemove)
        {
            Tracked.TryRemove(ptr, out _);
        }

        return released;
    }

    internal static int ReleaseTrackedForGroup(
        string groupId,
        VolumeCalculator calculator,
        Func<string, int, string, bool> belongsToGroup
    )
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            return 0;
        }

        var released = 0;
        var toRemove = new List<nint>();

        foreach (var (ptr, tracked) in Tracked)
        {
            if (string.IsNullOrWhiteSpace(tracked.ScdPath))
            {
                continue;
            }

            if (!belongsToGroup(tracked.ScdPath, (int)tracked.SoundNumber, groupId))
            {
                continue;
            }

            var soundData = (SoundData*)ptr;
            if (SoundDataSafety.IsValidForVolumeWrite(soundData))
            {
                var restore = tracked.LastGameVolume > 0.001f ? tracked.LastGameVolume : 1.0f;
                ApplyFieldVolume(soundData, restore);
            }

            toRemove.Add(ptr);
            released++;
        }

        foreach (var ptr in toRemove)
        {
            Tracked.TryRemove(ptr, out _);
        }

        calculator.ClearCache();
        return released;
    }

    private static void SanitizeReusedPoolNode(SoundData* soundData, float fieldVolume)
    {
        if (!SoundDataSafety.IsValidForVolumeWrite(soundData))
        {
            return;
        }

        var fadeTarget = ReadFadeTarget(soundData);
        if (fieldVolume <= 0.001f && fadeTarget > 0.001f)
        {
            WriteFadeTarget(soundData, 0f);
            return;
        }

        if (fieldVolume > 0.001f && Math.Abs(fadeTarget - fieldVolume) > 0.05f)
        {
            WriteFadeTarget(soundData, fieldVolume);
        }
    }

    internal static void UntrackForOneShot(SoundData* soundData)
    {
        if (soundData == null)
        {
            return;
        }

        Tracked.TryRemove((nint)soundData, out _);
    }

    internal static int RestoreInactivePooledOneShots(SoundData* listHead, VolumeCalculator calculator)
    {
        if (ZoneTransitionGuard.ShouldSkipSoundDataListAccess())
        {
            return 0;
        }

        var restored = 0;

        SoundDataSafety.VisitSoundList(
            listHead,
            soundData =>
            {
                if (!SoundDataSafety.TryReadSoundData(soundData, out var isActive, out _, out var fieldVolume)
                    || isActive)
                {
                    return true;
                }

                var path = SoundVolumeHelper.GetPathFromSoundData(soundData);
                if (string.IsNullOrWhiteSpace(path) || !calculator.IsLikelyOneShotPath(path))
                {
                    return true;
                }

                if (SoundDataSafety.IsValidForVolumeWrite(soundData)
                    && (fieldVolume > 1.02f || fieldVolume <= 0.001f))
                {
                    ApplyFieldVolume(soundData, 1.0f);
                    restored++;
                }

                return true;
            },
            listName: "restore-inactive-oneshot"
        );

        return restored;
    }

    internal static bool TryGetTrackedPath(SoundData* soundData, out string path)
    {
        path = string.Empty;
        if (soundData == null)
        {
            return false;
        }

        if (Tracked.TryGetValue((nint)soundData, out var tracked)
            && !string.IsNullOrWhiteSpace(tracked.ScdPath))
        {
            path = tracked.ScdPath;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Mount-loop and blacklist paths must not receive per-frame enforcement or native SetVolume.
    /// </summary>
    internal static bool IsUnsafeForVolumeEnforcement(string? scdPath)
    {
        return SoundBlacklist.IsPlayHookBlockedPath(scdPath);
    }

    internal static bool ShouldSkipVolumeEnforcement(SoundData* soundData)
    {
        if (soundData == null)
        {
            return true;
        }

        if (SoundBlacklist.ShouldBypassSoundData(soundData))
        {
            return true;
        }

        return TryGetTrackedPath(soundData, out var path) && IsUnsafeForVolumeEnforcement(path);
    }

    /// <summary>
    /// Remember the SCD path for a playing SoundData node without applying volume.
    /// Required for streaming BGM/music that cannot resolve paths later via GetFileName.
    /// </summary>
    internal static void TrackPlayPath(SoundData* soundData, string scdPath, uint soundNumber = 0)
    {
        if (soundData == null || string.IsNullOrWhiteSpace(scdPath))
        {
            return;
        }

        if (ZoneTransitionGuard.ShouldSkipSoundDataTracking())
        {
            return;
        }

        if (IsUnsafeForVolumeEnforcement(scdPath))
        {
            return;
        }

        if (!SoundDataSafety.TryReadSoundData(soundData, out _, out var readNumber, out _))
        {
            return;
        }

        if (soundNumber == 0 && readNumber != 0)
        {
            soundNumber = readNumber;
        }

        var ptr = (nint)soundData;
        scdPath = SoundVolumeHelper.ChooseAuthoritativePath(
            SoundVolumeHelper.GetPathFromSoundData(soundData),
            scdPath
        ).ToLowerInvariant();
        var fieldVolume = 0f;
        SoundDataSafety.TryReadSoundData(soundData, out _, out _, out fieldVolume);

        if (!Tracked.TryGetValue(ptr, out var tracked))
        {
            Tracked[ptr] = new TrackedSoundVolume
            {
                ScdPath = scdPath,
                SoundNumber = soundNumber,
                LastGameVolume = fieldVolume > 0.001f ? fieldVolume : 1.0f,
                AwaitVolumeApply = fieldVolume <= 0.001f,
            };
            return;
        }

        tracked.ScdPath = scdPath;
        tracked.SoundNumber = soundNumber;
        if (fieldVolume > 0.001f && !tracked.LastWriteByPlugin)
        {
            tracked.LastGameVolume = fieldVolume;
            tracked.AwaitVolumeApply = false;
        }
        else if (fieldVolume <= 0.001f)
        {
            tracked.LastGameVolume = 1.0f;
            if (!(tracked.LastWriteByPlugin && tracked.LastEffectiveVolume > 0.001f))
            {
                tracked.AwaitVolumeApply = true;
            }

            if (tracked.LastWriteByPlugin && tracked.LastEffectiveVolume <= 0.001f)
            {
                tracked.LastWriteByPlugin = false;
                tracked.LastEffectiveVolume = 0f;
                tracked.LastAppliedMultiplier = 1.0f;
            }
        }
    }

    /// <summary>
    /// Cast-loop SFX use SetVolume(0) as fade-in setup; only release tracking for genuine silencing.
    /// </summary>
    internal static bool ShouldTreatSetVolumeZeroAsSilencing(SoundData* soundData)
    {
        if (soundData == null)
        {
            return false;
        }

        if (!Tracked.TryGetValue((nint)soundData, out var tracked))
        {
            return false;
        }

        if (tracked.AwaitVolumeApply)
        {
            return false;
        }

        if (!SoundDataSafety.TryReadSoundData(soundData, out _, out _, out var fieldVolume))
        {
            return false;
        }

        if (tracked.LastEffectiveVolume > 0.05f || fieldVolume > 0.05f)
        {
            return true;
        }

        var fadeTarget = ReadFadeTarget(soundData);
        return fadeTarget > 0.05f && fieldVolume > fadeTarget * 0.5f;
    }

    internal static void NotifyGameSilencing(SoundData* soundData)
    {
        if (soundData == null)
        {
            return;
        }

        Tracked.TryRemove((nint)soundData, out _);
        StreamingBgmTracker.NotifySoundReleased(soundData);
        OneShotPlayRegistry.NotifyReleased(soundData);
    }

    internal static bool ShouldPassthroughScaledVolume(SoundData* soundData, float fieldVolume)
    {
        if (soundData == null)
        {
            return true;
        }

        if (Tracked.TryGetValue((nint)soundData, out var tracked) && tracked.AwaitVolumeApply)
        {
            return true;
        }

        return IsNativeFadeActive(soundData, fieldVolume);
    }

    internal static bool ShouldAllowForceRefresh(SoundData* soundData)
    {
        if (soundData == null)
        {
            return false;
        }

        if (!SoundDataSafety.TryReadSoundData(soundData, out var isActive, out _, out var fieldVolume)
            || !isActive)
        {
            return false;
        }

        return !ShouldPassthroughScaledVolume(soundData, fieldVolume);
    }

    private static void ClearAwaitVolumeApplyIfStable(TrackedSoundVolume tracked, SoundData* soundData, float fieldVolume)
    {
        if (!tracked.AwaitVolumeApply)
        {
            return;
        }

        if (IsNativeFadeActive(soundData, fieldVolume))
        {
            return;
        }

        if (fieldVolume > 0.05f)
        {
            tracked.AwaitVolumeApply = false;
        }
    }

    internal static void EnforceAllTracked(
        VolumeCalculator calculator,
        ApplyVolumeDelegate? applyVolume = null
    )
    {
        foreach (var (ptr, tracked) in Tracked)
        {
            var soundData = (SoundData*)ptr;
            if (!SoundDataSafety.IsValidForVolumeWrite(soundData))
            {
                continue;
            }

            if (IsUnsafeForVolumeEnforcement(tracked.ScdPath) || SoundBlacklist.ShouldBypassSoundData(soundData))
            {
                Tracked.TryRemove(ptr, out _);
                continue;
            }

            TryEnforceFieldVolume(soundData, calculator, applyVolume);
        }
    }

    internal static int RefreshAllTracked(
        VolumeCalculator calculator,
        ApplyVolumeDelegate applyVolume
    )
    {
        var refreshed = 0;

        foreach (var (ptr, tracked) in Tracked)
        {
            if (string.IsNullOrWhiteSpace(tracked.ScdPath))
            {
                continue;
            }

            var soundData = (SoundData*)ptr;
            if (!SoundDataSafety.IsReadable(ptr))
            {
                continue;
            }

            if (ForceRefreshActiveSound(soundData, calculator, applyVolume, tracked.ScdPath))
            {
                refreshed++;
            }
        }

        return refreshed;
    }

    internal static int RefreshTrackedSoundsForGroup(
        string groupId,
        VolumeCalculator calculator,
        Func<string, int, string, bool> belongsToGroup,
        ApplyVolumeDelegate applyVolume
    )
    {
        var refreshed = 0;

        foreach (var (ptr, tracked) in Tracked)
        {
            if (string.IsNullOrWhiteSpace(tracked.ScdPath))
            {
                continue;
            }

            if (!belongsToGroup(tracked.ScdPath, (int)tracked.SoundNumber, groupId))
            {
                continue;
            }

            var soundData = (SoundData*)ptr;
            if (!SoundDataSafety.IsReadable(ptr))
            {
                continue;
            }

            if (ForceRefreshActiveSound(
                    soundData,
                    calculator,
                    applyVolume,
                    tracked.ScdPath
                ))
            {
                refreshed++;
            }
        }

        return refreshed;
    }

    internal static void Register(
        SoundData* soundData,
        VolumeCalculator calculator,
        float gameVolume,
        float effectiveVolume,
        string? scdPath = null
    )
    {
        if (!SoundDataSafety.TryReadSoundData(soundData, out _, out var soundNumber, out _))
        {
            return;
        }

        var ptr = (nint)soundData;
        if (!Tracked.TryGetValue(ptr, out var tracked))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(scdPath))
        {
            tracked.ScdPath = scdPath.ToLowerInvariant();
            tracked.SoundNumber = soundNumber;
        }

        var multiplier = GetMultiplier(soundData, calculator);
        if (Math.Abs(multiplier - 1.0f) < 0.001f || gameVolume <= 0.001f)
        {
            return;
        }

        var entry = Tracked[ptr];
        entry.LastGameVolume = gameVolume;
        entry.LastEffectiveVolume = effectiveVolume;
        entry.LastAppliedMultiplier = multiplier;
        entry.LastWriteByPlugin = true;
        entry.AwaitVolumeApply = false;
    }

    internal static float GetEffectiveVolume(SoundData* soundData, float fieldVolume, float multiplier)
    {
        if (Math.Abs(multiplier - 1.0f) < 0.001f)
        {
            return fieldVolume;
        }

        if (Tracked.TryGetValue((nint)soundData, out var tracked) && tracked.LastGameVolume > 0.001f)
        {
            return Configuration.ClampToEngineCap(
                SoundVolumeHelper.ScaleVolume(tracked.LastGameVolume, multiplier)
            );
        }

        return Configuration.ClampToEngineCap(
            SoundVolumeHelper.ScaleVolume(InferGameVolume(fieldVolume, multiplier), multiplier)
        );
    }

    internal static void ApplyFieldVolume(SoundData* soundData, float effectiveVolume)
    {
        if (ZoneTransitionGuard.ShouldSkipSoundDataWrites()
            || !SoundDataSafety.IsValidForVolumeWrite(soundData))
        {
            return;
        }

        try
        {
            var engineVolume = Configuration.ClampToEngineCap(effectiveVolume);
            soundData->Volume = engineVolume;
            WriteFadeTarget(soundData, engineVolume);
        }
        catch (Exception ex)
        {
            Services.PluginLog.Verbose(ex, "SoundMixer: failed to apply SoundData volume");
        }
    }

    internal static void EnforceActiveSound(
        SoundData* soundData,
        VolumeCalculator calculator,
        ApplyVolumeDelegate? applyVolume = null
    )
    {
        if (SoundBlacklist.IsPlayBypassActive || SoundBlacklist.ShouldBypassSoundData(soundData))
        {
            return;
        }

        if (!SoundDataSafety.TryReadSoundData(soundData, out var isActive, out _, out _))
        {
            return;
        }

        if (!isActive)
        {
            return;
        }

        var ptr = (nint)soundData;
        if (Tracked.TryGetValue(ptr, out var trackedEntry))
        {
            if (IsUnsafeForVolumeEnforcement(trackedEntry.ScdPath)
                || calculator.IsLikelyOneShotPath(trackedEntry.ScdPath))
            {
                Tracked.TryRemove(ptr, out _);
                return;
            }
        }

        if (!Tracked.ContainsKey(ptr))
        {
            StreamingBgmTracker.TryResolvePendingPath(soundData, out _);
        }

        if (!Tracked.ContainsKey(ptr))
        {
            return;
        }

        TryEnforceFieldVolume(soundData, calculator, applyVolume);
    }

    internal static bool TryEnforceFieldVolume(
        SoundData* soundData,
        VolumeCalculator calculator,
        ApplyVolumeDelegate? applyVolume = null
    )
    {
        if (!SoundDataSafety.TryReadSoundData(soundData, out var isActive, out _, out var fieldVolume))
        {
            return false;
        }

        if (!isActive)
        {
            return false;
        }

        var ptr = (nint)soundData;
        if (!Tracked.TryGetValue(ptr, out var tracked))
        {
            return false;
        }

        if (IsUnsafeForVolumeEnforcement(tracked.ScdPath))
        {
            Tracked.TryRemove(ptr, out _);
            return false;
        }

        if (calculator.IsLikelyOneShotPath(tracked.ScdPath))
        {
            Tracked.TryRemove(ptr, out _);
            return false;
        }

        float multiplier;
        if (SoundEnforcement.TryResolve(soundData, calculator, out var resolved))
        {
            tracked.ScdPath = resolved.ResolvedPath;
            tracked.SoundNumber = resolved.SoundNumber;
            multiplier = resolved.Multiplier;
        }
        else if (StreamingBgmTracker.IsBgmOrMusicPath(tracked.ScdPath))
        {
            multiplier = SoundVolumeHelper.GetEnforcementMultiplier(
                calculator,
                tracked.ScdPath,
                tracked.SoundNumber
            );
        }
        else
        {
            return false;
        }
        if (TryApplyMuteMultiplier(soundData, tracked, fieldVolume, multiplier, applyVolume))
        {
            return true;
        }

        if (Math.Abs(multiplier - 1.0f) < 0.001f)
        {
            if (!tracked.LastWriteByPlugin)
            {
                return false;
            }

            var restoreVolume = tracked.LastGameVolume > 0.001f ? tracked.LastGameVolume : fieldVolume;
            if (applyVolume != null)
            {
                applyVolume(soundData, restoreVolume);
            }
            else
            {
                ApplyFieldVolume(soundData, restoreVolume);
            }

            Tracked.TryRemove(ptr, out _);
            return true;
        }

        if (ShouldPassthroughScaledVolume(soundData, fieldVolume))
        {
            ClearAwaitVolumeApplyIfStable(tracked, soundData, fieldVolume);
            return false;
        }

        var gameVolume = ResolveBaseGameVolume(tracked, fieldVolume, multiplier);
        if (gameVolume <= 0.001f)
        {
            return false;
        }

        var effective = Configuration.ClampToEngineCap(
            SoundVolumeHelper.ScaleVolume(gameVolume, multiplier)
        );

        UpdateTrackedVolumeState(tracked, gameVolume, effective, multiplier);

        if (Math.Abs(fieldVolume - effective) < 0.002f
            && Math.Abs(ReadFadeTarget(soundData) - effective) < 0.002f)
        {
            return true;
        }

        if (applyVolume != null)
        {
            applyVolume(soundData, effective);
        }
        else
        {
            ApplyFieldVolume(soundData, effective);
        }

        return true;
    }

    internal static bool IsNativeFadeActive(SoundData* soundData, float fieldVolume)
    {
        var fadeTarget = ReadFadeTarget(soundData);
        if (Math.Abs(fadeTarget - fieldVolume) <= 0.02f)
        {
            return false;
        }

        // SetVolume(0) leaves fadeTarget at 0 while cast-loop SFX fade in to full volume.
        if (fadeTarget <= 0.02f && fieldVolume > 0.3f)
        {
            return false;
        }

        return true;
    }

    internal static void PruneInactive()
    {
        foreach (var (ptr, tracked) in Tracked)
        {
            var soundData = (SoundData*)ptr;
            if (!SoundDataSafety.TryReadSoundData(soundData, out var isActive, out _, out _)
                || !isActive)
            {
                if (!IsUnsafeForVolumeEnforcement(tracked.ScdPath)
                    && SoundDataSafety.IsValidForVolumeWrite(soundData)
                    && (tracked.LastWriteByPlugin
                        || tracked.LastEffectiveVolume > 0.001f
                        || IsPooledOneShotPath(tracked.ScdPath)))
                {
                    var restore = tracked.LastGameVolume > 0.001f ? tracked.LastGameVolume : 1.0f;
                    ApplyFieldVolume(soundData, restore);
                }

                OneShotPlayRegistry.NotifyReleased(soundData);
                Tracked.TryRemove(ptr, out _);
            }
        }
    }

    internal static void Clear()
    {
        Tracked.Clear();
    }

    internal delegate void ApplyVolumeDelegate(SoundData* soundData, float effectiveVolume);

    internal static bool ForceRefreshActiveSound(
        SoundData* soundData,
        VolumeCalculator calculator,
        ApplyVolumeDelegate? applyVolume,
        string? knownScdPath = null
    )
    {
        if (SoundBlacklist.IsPlayBypassActive || SoundBlacklist.ShouldBypassSoundData(soundData))
        {
            return false;
        }

        if (!SoundDataSafety.TryReadSoundData(soundData, out var isActive, out var soundNumber, out var fieldVolume))
        {
            return false;
        }

        if (!isActive)
        {
            return false;
        }

        var ptr = (nint)soundData;
        var path = string.Empty;
        var multiplier = 1.0f;

        if (SoundEnforcement.TryResolve(soundData, calculator, out var resolved))
        {
            path = resolved.ResolvedPath;
            soundNumber = resolved.SoundNumber;
            multiplier = resolved.Multiplier;
        }
        else if (StreamingBgmTracker.TryResolvePendingPath(soundData, out var pendingBgmPath))
        {
            path = pendingBgmPath.ToLowerInvariant();
            multiplier = SoundVolumeHelper.GetEnforcementMultiplier(calculator, path, soundNumber);
            var pendingMultiplier = StreamingBgmTracker.GetPendingMultiplier(path);
            if (Math.Abs(pendingMultiplier - 1.0f) > 0.001f)
            {
                multiplier = pendingMultiplier;
            }
        }
        else
        {
            return false;
        }

        if (IsUnsafeForVolumeEnforcement(path))
        {
            return false;
        }

        if (calculator.IsLikelyOneShotPath(path))
        {
            if (Tracked.TryRemove(ptr, out var stale)
                && stale.LastWriteByPlugin
                && SoundDataSafety.IsValidForVolumeWrite(soundData))
            {
                var restore = stale.LastGameVolume > 0.001f ? stale.LastGameVolume : 1.0f;
                ApplyFieldVolume(soundData, restore);
            }

            OneShotPlayRegistry.NotifyReleased(soundData);
            return false;
        }

        if (!Tracked.TryGetValue(ptr, out var tracked))
        {
            PrepareTrackedForPlay(soundData, path, soundNumber);
            if (!Tracked.TryGetValue(ptr, out tracked))
            {
                return false;
            }
        }
        else
        {
            tracked.ScdPath = path;
            tracked.SoundNumber = soundNumber;
        }

        if (TryApplyMuteMultiplier(soundData, tracked, fieldVolume, multiplier, applyVolume))
        {
            return true;
        }

        if (ShouldPassthroughScaledVolume(soundData, fieldVolume))
        {
            ClearAwaitVolumeApplyIfStable(tracked, soundData, fieldVolume);
            return false;
        }

        var gameVolume = ResolveBaseGameVolume(tracked, fieldVolume, multiplier);
        if (gameVolume <= 0.001f)
        {
            return false;
        }

        if (Math.Abs(multiplier - 1.0f) < 0.001f)
        {
            if (!Tracked.TryGetValue(ptr, out tracked) || !tracked.LastWriteByPlugin)
            {
                return false;
            }

            var restoreVolume = tracked.LastGameVolume > 0.001f ? tracked.LastGameVolume : fieldVolume;
            if (applyVolume != null)
            {
                applyVolume(soundData, restoreVolume);
            }
            else
            {
                ApplyFieldVolume(soundData, restoreVolume);
            }

            Tracked.TryRemove(ptr, out _);
            return true;
        }

        var effectiveVolume = Configuration.ClampToEngineCap(
            SoundVolumeHelper.ScaleVolume(gameVolume, multiplier)
        );
        UpdateTrackedVolumeState(tracked, gameVolume, effectiveVolume, multiplier);

        if (applyVolume != null)
        {
            applyVolume(soundData, effectiveVolume);
        }
        else
        {
            ApplyFieldVolume(soundData, effectiveVolume);
        }

        return true;
    }

    private static bool TryApplyMuteMultiplier(
        SoundData* soundData,
        TrackedSoundVolume tracked,
        float fieldVolume,
        float multiplier,
        ApplyVolumeDelegate? applyVolume
    )
    {
        if (multiplier > 0.001f)
        {
            return false;
        }

        PreserveBaseGameVolumeForMute(tracked, fieldVolume);
        tracked.LastEffectiveVolume = 0f;
        tracked.LastAppliedMultiplier = 0f;
        tracked.LastWriteByPlugin = true;
        tracked.AwaitVolumeApply = false;

        if (applyVolume != null)
        {
            applyVolume(soundData, 0f);
        }
        else
        {
            ApplyFieldVolume(soundData, 0f);
        }

        return true;
    }

    private static void PreserveBaseGameVolumeForMute(TrackedSoundVolume tracked, float fieldVolume)
    {
        if (tracked.LastGameVolume > 0.001f)
        {
            return;
        }

        if (fieldVolume > 0.001f)
        {
            tracked.LastGameVolume = fieldVolume;
            return;
        }

        tracked.LastGameVolume = 1.0f;
    }

    private static void UpdateTrackedVolumeState(
        TrackedSoundVolume tracked,
        float gameVolume,
        float effectiveVolume,
        float multiplier
    )
    {
        if (gameVolume > 0.001f)
        {
            tracked.LastGameVolume = gameVolume;
        }

        tracked.LastEffectiveVolume = effectiveVolume;
        tracked.LastAppliedMultiplier = multiplier;
        tracked.LastWriteByPlugin = true;
        tracked.AwaitVolumeApply = false;
    }

    private static float ResolveBaseGameVolume(
        TrackedSoundVolume tracked,
        float fieldVolume,
        float multiplier
    )
    {
        if (tracked.LastGameVolume > 0.001f)
        {
            return tracked.LastGameVolume;
        }

        if (tracked.LastWriteByPlugin
            && tracked.LastEffectiveVolume > 0.001f
            && tracked.LastAppliedMultiplier > 0.001f)
        {
            return tracked.LastEffectiveVolume / tracked.LastAppliedMultiplier;
        }

        var inferred = InferGameVolume(fieldVolume, multiplier);
        if (inferred > 0.001f)
        {
            return inferred;
        }

        if (tracked.LastWriteByPlugin
            && fieldVolume <= 0.001f
            && multiplier > 0.001f)
        {
            return 1.0f;
        }

        return inferred;
    }

    private static float InferGameVolume(float fieldVolume, float multiplier)
    {
        if (multiplier < 1.0f - 0.001f && fieldVolume <= multiplier + 0.02f)
        {
            return fieldVolume / multiplier;
        }

        if (multiplier > 1.0f + 0.001f && fieldVolume > multiplier + 0.02f)
        {
            return fieldVolume / multiplier;
        }

        return fieldVolume;
    }

    private static void WriteFadeTarget(SoundData* soundData, float effectiveVolume)
    {
        var fadeTargetPtr = (nint)soundData + VolumeFadeTargetOffset;
        if (!SoundDataSafety.IsReadable(fadeTargetPtr, sizeof(float)))
        {
            return;
        }

        *(float*)fadeTargetPtr = effectiveVolume;
    }

    private static float ReadFadeTarget(SoundData* soundData)
    {
        return ReadFadeTargetPublic(soundData);
    }

    internal static float ReadFadeTargetPublic(SoundData* soundData)
    {
        var fadeTargetPtr = (nint)soundData + VolumeFadeTargetOffset;
        if (!SoundDataSafety.IsReadable(fadeTargetPtr, sizeof(float)))
        {
            return 0f;
        }

        return *(float*)fadeTargetPtr;
    }

    internal static int RestoreAllInactivePoolVolumes(SoundData* listHead)
    {
        if (ZoneTransitionGuard.ShouldSkipSoundDataListAccess())
        {
            return 0;
        }

        var restored = 0;

        SoundDataSafety.VisitSoundList(
            listHead,
            soundData =>
            {
                if (!SoundDataSafety.TryReadSoundData(soundData, out var isActive, out _, out var fieldVolume)
                    || isActive)
                {
                    return true;
                }

                if (!SoundDataSafety.IsValidForVolumeWrite(soundData))
                {
                    return true;
                }

                if (fieldVolume <= 0.001f || Math.Abs(fieldVolume - 1.0f) > 0.02f)
                {
                    ApplyFieldVolume(soundData, 1.0f);
                    restored++;
                }

                return true;
            },
            listName: "restore-inactive-pool"
        );

        return restored;
    }
}
