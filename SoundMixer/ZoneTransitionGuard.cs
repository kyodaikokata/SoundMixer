using Dalamud.Game.ClientState.Conditions;

namespace SoundMixer;

/// <summary>
/// Suppresses proactive SoundData access during zone loads (housing, teleports, etc.).
/// Stale pointers often still pass memory checks while the audio engine tears down nodes.
/// Reactive hook passthrough is handled separately via <see cref="ShouldPassthroughVolumeHooks"/>.
/// </summary>
internal static class ZoneTransitionGuard
{
    private const double PostTerritoryGraceSeconds = 3.0;

    private static DateTime s_postTerritoryGraceUntilUtc = DateTime.MinValue;

    internal static void NotifyTerritoryChanged()
    {
        s_postTerritoryGraceUntilUtc = DateTime.UtcNow.AddSeconds(PostTerritoryGraceSeconds);
    }

    /// <summary>Skip per-frame enforcement, list scans, monitoring, and deferred one-shot applies.</summary>
    internal static bool ShouldSkipSoundDataMaintenance() => IsInTransitionWindow();

    /// <summary>Skip SetVolume/GetVolume detour logic; call native implementations only.</summary>
    internal static bool ShouldPassthroughVolumeHooks() => IsInTransitionWindow();

    /// <summary>Skip linked-list walks and path probing on live SoundData nodes.</summary>
    internal static bool ShouldSkipSoundDataListAccess() => IsInTransitionWindow();

    /// <summary>Skip plugin-initiated volume field writes and native SetVolume from maintenance paths.</summary>
    internal static bool ShouldSkipSoundDataWrites() => IsInTransitionWindow();

    /// <summary>Skip new tracking registration while nodes are being torn down/reused.</summary>
    internal static bool ShouldSkipSoundDataTracking() => IsInTransitionWindow();

    private static bool IsInTransitionWindow()
    {
        if (IsBetweenAreas())
        {
            return true;
        }

        return DateTime.UtcNow < s_postTerritoryGraceUntilUtc;
    }

    private static bool IsBetweenAreas()
    {
        try
        {
            return Services.Condition[ConditionFlag.BetweenAreas]
                || Services.Condition[ConditionFlag.BetweenAreas51];
        }
        catch
        {
            return false;
        }
    }
}
