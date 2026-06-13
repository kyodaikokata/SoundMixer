using Dalamud.Game.ClientState.Conditions;

namespace SoundMixer;

/// <summary>
/// Clears BGM/streaming tracking when combat or mount context changes so stale nodes are not re-enforced.
/// </summary>
internal static class BgmTransitionGuard
{
    private static bool? s_lastInCombat;
    private static bool s_lastMountedOrMounting;

    internal static void Update(Filter filter)
    {
        if (ZoneTransitionGuard.ShouldSkipSoundDataMaintenance())
        {
            return;
        }

        var inCombat = Services.Condition[ConditionFlag.InCombat];
        if (s_lastInCombat.HasValue && s_lastInCombat.Value != inCombat)
        {
            filter.ClearBgmTransitionTracking();
            Services.PluginLog.Debug(
                $"SoundMixer: cleared BGM tracking after combat state -> {(inCombat ? "in combat" : "out of combat")}"
            );
        }

        s_lastInCombat = inCombat;

        var mountedOrMounting = IsMountedOrMounting();
        if (s_lastMountedOrMounting != mountedOrMounting)
        {
            filter.ClearBgmTransitionTracking();
            Services.PluginLog.Debug(
                $"SoundMixer: cleared BGM tracking after mount state -> {(mountedOrMounting ? "mounted/mounting" : "on foot")}"
            );
        }

        s_lastMountedOrMounting = mountedOrMounting;
    }

    internal static void Reset()
    {
        s_lastInCombat = null;
        s_lastMountedOrMounting = false;
    }

    private static bool IsMountedOrMounting()
    {
        return Services.Condition[ConditionFlag.Mounted]
            || Services.Condition[ConditionFlag.RidingPillion]
            || Services.Condition[ConditionFlag.Mounting]
            || Services.Condition[ConditionFlag.Mounting71];
    }
}
