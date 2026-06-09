using FFXIVClientStructs.FFXIV.Client.Sound;

namespace SoundMixer;

/// <summary>
/// Battle/field/mount BGM often loads asynchronously: PlayBGMSound may return null while the
/// SoundData node appears later on the active list without a resolvable FileName.
/// </summary>
internal static unsafe class StreamingBgmTracker
{
    private static readonly object Gate = new();

    private static string s_pendingPath = string.Empty;
    private static float s_pendingMultiplier = 1f;
    private static DateTime s_pendingAt = DateTime.MinValue;
    private static nint s_claimedPtr;

    private const double ClaimWindowSeconds = 12;

    internal static bool IsBgmOrMusicPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalized = path.ToLowerInvariant();
        return normalized.Contains("/bgm/", StringComparison.Ordinal)
            || normalized.Contains("/music/", StringComparison.Ordinal)
            || normalized.StartsWith("music/", StringComparison.Ordinal)
            || MountTransitionGuard.IsRideBgmPath(normalized);
    }

    internal static void NotePlay(string scdPath, float multiplier, SoundData* result, uint soundNumber = 0)
    {
        if (string.IsNullOrWhiteSpace(scdPath) || !IsBgmOrMusicPath(scdPath))
        {
            return;
        }

        scdPath = scdPath.ToLowerInvariant();
        lock (Gate)
        {
            s_pendingPath = scdPath;
            s_pendingMultiplier = multiplier;
            s_pendingAt = DateTime.UtcNow;
            s_claimedPtr = result != null ? (nint)result : 0;
        }

        if (result != null)
        {
            SoundVolumeTracker.PrepareTrackedForPlay(result, scdPath, soundNumber);
            return;
        }

        TryClaimStreamingNodeForPendingBgm();
    }

    internal static bool TryResolvePendingPath(SoundData* soundData, out string path)
    {
        path = string.Empty;
        if (soundData == null || ZoneTransitionGuard.ShouldSkipSoundDataListAccess())
        {
            return false;
        }

        var ptr = (nint)soundData;
        lock (Gate)
        {
            if (string.IsNullOrWhiteSpace(s_pendingPath) || !IsBgmOrMusicPath(s_pendingPath))
            {
                return false;
            }

            if ((DateTime.UtcNow - s_pendingAt).TotalSeconds > ClaimWindowSeconds)
            {
                ClearLocked();
                return false;
            }

            if (s_claimedPtr != 0)
            {
                if (ptr != s_claimedPtr)
                {
                    return false;
                }

                path = s_pendingPath;
                return true;
            }

            if (!IsEligibleStreamingBgmNode(soundData))
            {
                return false;
            }

            s_claimedPtr = ptr;
            path = s_pendingPath;
        }

        SoundVolumeTracker.PrepareTrackedForPlay(soundData, path, 0);
        return true;
    }

    internal static float GetPendingMultiplier(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return 1f;
        }

        path = path.ToLowerInvariant();
        lock (Gate)
        {
            if (path == s_pendingPath)
            {
                return s_pendingMultiplier;
            }
        }

        return 1f;
    }

    internal static void Clear()
    {
        lock (Gate)
        {
            ClearLocked();
        }
    }

    internal static void NotifySoundReleased(SoundData* soundData)
    {
        if (soundData == null)
        {
            return;
        }

        var ptr = (nint)soundData;
        lock (Gate)
        {
            if (s_claimedPtr == ptr)
            {
                ClearLocked();
            }
        }
    }

    private static void TryClaimStreamingNodeForPendingBgm()
    {
        if (ZoneTransitionGuard.ShouldSkipSoundDataListAccess())
        {
            return;
        }

        string pendingPath;
        lock (Gate)
        {
            if (string.IsNullOrWhiteSpace(s_pendingPath) || s_claimedPtr != 0)
            {
                return;
            }

            pendingPath = s_pendingPath;
        }

        var soundManager = SoundManager.Instance();
        if (soundManager == null)
        {
            return;
        }

        SoundDataSafety.VisitSoundList(
            soundManager->ActiveSoundDataListHead,
            soundData =>
            {
                if (!IsEligibleStreamingBgmNode(soundData))
                {
                    return true;
                }

                lock (Gate)
                {
                    if (s_claimedPtr != 0 || s_pendingPath != pendingPath)
                    {
                        return false;
                    }

                    s_claimedPtr = (nint)soundData;
                }

                SoundVolumeTracker.PrepareTrackedForPlay(soundData, pendingPath, 0);
                return false;
            },
            listName: "bgm-claim"
        );
    }

    private static bool IsEligibleStreamingBgmNode(SoundData* soundData)
    {
        if (SoundBlacklist.IsPlayBypassActive || SoundBlacklist.ShouldBypassSoundData(soundData))
        {
            return false;
        }

        if (!SoundDataSafety.TryReadSoundData(soundData, out var isActive, out _, out _)
            || !isActive)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(SoundVolumeHelper.GetPathFromSoundData(soundData)))
        {
            return false;
        }

        if (SoundVolumeTracker.TryGetTrackedPath(soundData, out var existingPath)
            && !string.IsNullOrWhiteSpace(existingPath))
        {
            return false;
        }

        return true;
    }

    private static void ClearLocked()
    {
        s_pendingPath = string.Empty;
        s_pendingMultiplier = 1f;
        s_pendingAt = DateTime.MinValue;
        s_claimedPtr = 0;
    }
}
