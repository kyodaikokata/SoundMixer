using Dalamud.Game.ClientState.Conditions;

namespace SoundMixer;

/// <summary>
/// Safe Mode: broad mount guard (all mounts). Official Guideroid blacklist uses a separate
/// sliding grace window refreshed only by Guideroid path activity — never latched to "any mount".
/// </summary>
internal static class MountTransitionGuard
{
    private const double GraceSeconds = 5.0;
    private const double GuideroidLoopGraceSeconds = 45.0;

    private static bool s_cachedActive;
    private static bool s_lastMountedOrMounting;
    private static DateTime s_graceUntilUtc = DateTime.MinValue;
    private static bool s_loggedActive;
    private static bool s_loggedInactive;

    private static DateTime s_guideroidLoopUntilUtc = DateTime.MinValue;
    private static bool s_loggedGuideroidActive;
    private static bool s_loggedGuideroidInactive;

    internal static bool IsActive => s_cachedActive;

    internal static bool IsEngaged(bool safeMode) => safeMode && IsActive;

    internal static bool IsGuideroidLoopSafetyActive =>
        SoundBlacklist.HasMountLoopBlockRules()
        && DateTime.UtcNow < s_guideroidLoopUntilUtc
        && IsMountedOrMounting();

    internal static void Update()
    {
        try
        {
            var active = IsMountedOrMounting();

            if (active != s_lastMountedOrMounting)
            {
                if (active)
                {
                    s_graceUntilUtc = DateTime.UtcNow.AddSeconds(GraceSeconds);
                }
                else
                {
                    ClearGuideroidLoopGrace();
                }
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

        if (IsGuideroidLoopSafetyActive && !s_loggedGuideroidActive)
        {
            s_loggedGuideroidActive = true;
            s_loggedGuideroidInactive = false;
            Services.PluginLog.Info("SoundMixer: Guideroid mount-loop blacklist safety active");
        }
        else if (!IsGuideroidLoopSafetyActive && !s_loggedGuideroidInactive)
        {
            s_loggedGuideroidInactive = true;
            s_loggedGuideroidActive = false;
            if (s_guideroidLoopUntilUtc != DateTime.MinValue)
            {
                SoundBlacklist.ClearPointerCache();
            }

            s_guideroidLoopUntilUtc = DateTime.MinValue;
            Services.PluginLog.Info("SoundMixer: Guideroid mount-loop blacklist safety inactive");
        }
    }

    internal static void NotifyMountSound(string? path, bool safeMode)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var normalized = path.ToLowerInvariant();

        if (safeMode && IsMountRelatedPath(normalized))
        {
            s_graceUntilUtc = DateTime.UtcNow.AddSeconds(GraceSeconds);
            s_cachedActive = true;
            s_loggedActive = false;
            return;
        }

        if (SoundBlacklist.IsMountLoopBlockedPath(normalized))
        {
            NotifyGuideroidLoopSound(normalized);
            return;
        }

        if (IsNonGuideroidMountPath(normalized))
        {
            ClearGuideroidLoopGrace();
        }
    }

    internal static void NotifyGuideroidLoopSound(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !SoundBlacklist.IsMountLoopBlockedPath(path))
        {
            return;
        }

        s_guideroidLoopUntilUtc = DateTime.UtcNow.AddSeconds(GuideroidLoopGraceSeconds);
        s_loggedGuideroidActive = false;
    }

    internal static void ClearGuideroidLoopGrace()
    {
        s_guideroidLoopUntilUtc = DateTime.MinValue;
        s_loggedGuideroidActive = false;
        s_loggedGuideroidInactive = false;
    }

    internal static void ClearGuideroidSession()
    {
        ClearGuideroidLoopGrace();
        SoundBlacklist.ClearPointerCache();
    }

    private static bool IsMountedOrMounting()
    {
        return Services.Condition[ConditionFlag.Mounted]
            || Services.Condition[ConditionFlag.RidingPillion]
            || Services.Condition[ConditionFlag.Mounting]
            || Services.Condition[ConditionFlag.Mounting71];
    }

    /// <summary>Mount BGM lives under ride (not mount), e.g. sound/bgm/ride/...</summary>
    internal static bool IsRideBgmPath(string normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        if (!normalized.Contains("ride", StringComparison.Ordinal))
        {
            return false;
        }

        return normalized.Contains("/bgm/", StringComparison.Ordinal)
            || normalized.Contains("/music/", StringComparison.Ordinal)
            || normalized.StartsWith("music/", StringComparison.Ordinal)
            || normalized.Contains("bgm/ride", StringComparison.Ordinal)
            || normalized.Contains("music/ride", StringComparison.Ordinal);
    }

    internal static bool IsMountRelatedPath(string normalized)
    {
        if (IsRideBgmPath(normalized))
        {
            return false;
        }

        return normalized.Contains("/ride/", StringComparison.Ordinal)
            || normalized.Contains("/mount/", StringComparison.Ordinal)
            || normalized.Contains("se_bt_etc_mount", StringComparison.Ordinal)
            || normalized.Contains("guideroid", StringComparison.Ordinal)
            || normalized.Contains("/riding/", StringComparison.Ordinal)
            || normalized.Contains("/mech/", StringComparison.Ordinal);
    }

    private static bool IsNonGuideroidMountPath(string normalized)
    {
        if (!IsMountRelatedPath(normalized))
        {
            return false;
        }

        return !SoundBlacklist.IsMountLoopBlockedPath(normalized);
    }
}
