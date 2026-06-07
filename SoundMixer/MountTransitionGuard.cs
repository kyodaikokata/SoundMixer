using Dalamud.Game.ClientState.Conditions;

namespace SoundMixer;

/// <summary>
/// Mount / dismount triggers bursty SetVolume traffic on transitional SoundData nodes.
/// While active we physically disable SoundData vtable hooks (not just detour passthrough).
/// </summary>
internal static class MountTransitionGuard
{
    private const double GraceSeconds = 5.0;

    private static bool s_cachedActive;
    private static bool s_lastMountedOrMounting;
    private static DateTime s_graceUntilUtc = DateTime.MinValue;
    private static bool s_loggedActive;
    private static bool s_loggedInactive;

    internal static bool IsActive => s_cachedActive;

    internal static bool IsEngaged(bool safeMode) => safeMode && IsActive;

    internal static void Update()
    {
        try
        {
            var active = Services.Condition[ConditionFlag.Mounted]
                || Services.Condition[ConditionFlag.RidingPillion]
                || Services.Condition[ConditionFlag.Mounting]
                || Services.Condition[ConditionFlag.Mounting71];

            if (active != s_lastMountedOrMounting)
            {
                s_graceUntilUtc = DateTime.UtcNow.AddSeconds(GraceSeconds);
            }
            else if (active)
            {
                s_graceUntilUtc = DateTime.UtcNow.AddSeconds(GraceSeconds);
            }

            s_lastMountedOrMounting = active;
            s_cachedActive = active || DateTime.UtcNow < s_graceUntilUtc;
        }
        catch
        {
            s_cachedActive = DateTime.UtcNow < s_graceUntilUtc;
        }

        if (s_cachedActive && !s_loggedActive)
        {
            s_loggedActive = true;
            s_loggedInactive = false;
            Services.PluginLog.Info("SoundMixer: mount transition guard active");
        }
        else if (!s_cachedActive && !s_loggedInactive)
        {
            s_loggedInactive = true;
            s_loggedActive = false;
            Services.PluginLog.Info("SoundMixer: mount transition guard inactive");
        }
    }

    /// <summary>
    /// Mount call SFX often fires before Mounted/Mounting condition flags flip.
    /// </summary>
    internal static void NotifyMountSound(string? path, bool safeMode)
    {
        if (!safeMode || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var normalized = path.ToLowerInvariant();
        if (!IsMountRelatedPath(normalized))
        {
            return;
        }

        s_graceUntilUtc = DateTime.UtcNow.AddSeconds(GraceSeconds);
        s_cachedActive = true;
        s_loggedActive = false;
    }

    private static bool IsMountRelatedPath(string normalized)
    {
        return normalized.Contains("/mount/", StringComparison.Ordinal)
            || normalized.Contains("se_bt_etc_mount", StringComparison.Ordinal)
            || normalized.Contains("guideroid", StringComparison.Ordinal)
            || normalized.Contains("/riding/", StringComparison.Ordinal)
            || normalized.Contains("/mech/", StringComparison.Ordinal);
    }
}
