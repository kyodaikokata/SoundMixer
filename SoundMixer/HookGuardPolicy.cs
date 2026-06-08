namespace SoundMixer;

internal static class HookGuardPolicy
{
    private static List<CompiledHookGuardRule> s_rules = new();
    private static OfficialHookGuardsDto? s_official;

    internal static int OfficialRevision => s_official?.Revision ?? 0;
    internal static int OfficialRuleCount => s_official?.Entries.Count ?? 0;
    internal static int UserRuleCount { get; private set; }

    internal static IReadOnlyList<OfficialHookGuardEntryDto> OfficialHandbook =>
        (IReadOnlyList<OfficialHookGuardEntryDto>?)s_official?.Entries ?? Array.Empty<OfficialHookGuardEntryDto>();

    internal static void Rebuild(Configuration config, OfficialHookGuardsDto? official)
    {
        s_official = official;
        var rules = new List<CompiledHookGuardRule>();

        if (official != null)
        {
            foreach (var entry in official.Entries)
            {
                if (TryCompile(entry.ResolveTrigger(), entry.DisabledHooks, entry.SkipActiveListScan, true, out var rule))
                {
                    rules.Add(rule);
                }
            }
        }

        UserRuleCount = 0;
        foreach (var entry in config.UserHookGuards)
        {
            if (!entry.Enabled)
            {
                continue;
            }

            UserRuleCount++;
            if (TryCompile(entry.Trigger, entry.DisabledHooks, entry.SkipActiveListScan, false, out var rule))
            {
                rules.Add(rule);
            }
        }

        s_rules = rules;
    }

    internal static bool IsTriggerActive(HookGuardTrigger trigger) =>
        HookGuardTriggerEvaluator.IsActive(trigger);

    internal static bool ShouldDisableHook(HookDebugId hookId, Configuration config)
    {
        if (config.HookDebug.ManualControl)
        {
            return false;
        }

        foreach (var rule in s_rules)
        {
            if (!IsTriggerActive(rule.Trigger))
            {
                continue;
            }

            if (rule.DisabledHooks.Contains(hookId))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool ShouldSkipActiveListScan(Configuration config)
    {
        if (config.HookDebug.ManualControl)
        {
            return false;
        }

        foreach (var rule in s_rules)
        {
            if (rule.SkipActiveListScan && IsTriggerActive(rule.Trigger))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool TryAddUserEntry(
        Configuration config,
        HookGuardTrigger trigger,
        IEnumerable<HookDebugId> disabledHooks,
        bool skipActiveListScan,
        string note
    )
    {
        var hooks = disabledHooks.Select(HookGuardIds.ToId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (hooks.Count == 0)
        {
            return false;
        }

        config.UserHookGuards.Add(
            new UserHookGuardEntry
            {
                Trigger = trigger,
                DisabledHooks = hooks,
                SkipActiveListScan = skipActiveListScan,
                Note = note?.Trim() ?? string.Empty,
            }
        );
        return true;
    }

    private static bool TryCompile(
        HookGuardTrigger trigger,
        IEnumerable<string>? hookIds,
        bool skipActiveListScan,
        bool fromOfficial,
        out CompiledHookGuardRule rule
    )
    {
        rule = default;
        if (hookIds == null)
        {
            return skipActiveListScan;
        }

        var hooks = new HashSet<HookDebugId>();
        foreach (var id in hookIds)
        {
            if (HookGuardIds.TryParse(id, out var hookId))
            {
                hooks.Add(hookId);
            }
        }

        if (hooks.Count == 0 && !skipActiveListScan)
        {
            return false;
        }

        rule = new CompiledHookGuardRule
        {
            Trigger = trigger,
            DisabledHooks = hooks,
            SkipActiveListScan = skipActiveListScan,
            FromOfficial = fromOfficial,
        };
        return true;
    }
}

internal static class HookGuardIds
{
    internal static string ToId(HookDebugId id) =>
        id switch
        {
            HookDebugId.PlaySpecificSound => "PlaySpecificSound",
            HookDebugId.PlaySound => "PlaySound",
            HookDebugId.PlaySystemSound => "PlaySystemSound",
            HookDebugId.PlayClipSound => "PlayClipSound",
            HookDebugId.PlayMovieSound => "PlayMovieSound",
            HookDebugId.PlayBgmSound => "PlayBGMSound",
            HookDebugId.PlayWeatherSound => "PlayWeatherSound",
            HookDebugId.SetVolume => "SetVolume",
            HookDebugId.GetVolume => "GetVolume",
            HookDebugId.LoadSoundFile => "LoadSoundFile",
            HookDebugId.GetResourceSync => "GetResourceSync",
            HookDebugId.GetResourceAsync => "GetResourceAsync",
            _ => string.Empty,
        };

    internal static bool TryParse(string? id, out HookDebugId hookId)
    {
        hookId = default;
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        switch (id.Trim())
        {
            case "PlaySpecificSound":
                hookId = HookDebugId.PlaySpecificSound;
                return true;
            case "PlaySound":
                hookId = HookDebugId.PlaySound;
                return true;
            case "PlaySystemSound":
                hookId = HookDebugId.PlaySystemSound;
                return true;
            case "PlayClipSound":
                hookId = HookDebugId.PlayClipSound;
                return true;
            case "PlayMovieSound":
                hookId = HookDebugId.PlayMovieSound;
                return true;
            case "PlayBGMSound":
            case "PlayBgmSound":
                hookId = HookDebugId.PlayBgmSound;
                return true;
            case "PlayWeatherSound":
                hookId = HookDebugId.PlayWeatherSound;
                return true;
            case "SetVolume":
                hookId = HookDebugId.SetVolume;
                return true;
            case "GetVolume":
                hookId = HookDebugId.GetVolume;
                return true;
            case "LoadSoundFile":
                hookId = HookDebugId.LoadSoundFile;
                return true;
            case "GetResourceSync":
                hookId = HookDebugId.GetResourceSync;
                return true;
            case "GetResourceAsync":
                hookId = HookDebugId.GetResourceAsync;
                return true;
            default:
                return false;
        }
    }

    internal static string? TryGetMonitorLabelKey(string? hookSourceId)
    {
        if (string.IsNullOrWhiteSpace(hookSourceId))
        {
            return null;
        }

        if (TryParse(hookSourceId, out var hookId))
        {
            return GetDebugNameKey(hookId);
        }

        return hookSourceId switch
        {
            SoundMonitorHookIds.ActiveScan => Localization.Loc.Keys.MonitorHookActiveScan,
            SoundMonitorHookIds.PathResolve => Localization.Loc.Keys.MonitorHookPathResolve,
            _ => null,
        };
    }

    internal static string GetDebugNameKey(HookDebugId id) =>
        id switch
        {
            HookDebugId.PlaySpecificSound => Localization.Loc.Keys.DebugHookPlaySpecificSound,
            HookDebugId.PlaySound => Localization.Loc.Keys.DebugHookPlaySound,
            HookDebugId.PlaySystemSound => Localization.Loc.Keys.DebugHookPlaySystemSound,
            HookDebugId.PlayClipSound => Localization.Loc.Keys.DebugHookPlayClipSound,
            HookDebugId.PlayMovieSound => Localization.Loc.Keys.DebugHookPlayMovieSound,
            HookDebugId.PlayBgmSound => Localization.Loc.Keys.DebugHookPlayBgmSound,
            HookDebugId.PlayWeatherSound => Localization.Loc.Keys.DebugHookPlayWeatherSound,
            HookDebugId.SetVolume => Localization.Loc.Keys.DebugHookSetVolume,
            HookDebugId.GetVolume => Localization.Loc.Keys.DebugHookGetVolume,
            HookDebugId.LoadSoundFile => Localization.Loc.Keys.DebugHookLoadSoundFile,
            HookDebugId.GetResourceSync => Localization.Loc.Keys.DebugHookGetResourceSync,
            HookDebugId.GetResourceAsync => Localization.Loc.Keys.DebugHookGetResourceAsync,
            _ => Localization.Loc.Keys.DebugHookPlaySpecificSound,
        };

    internal static HookDebugId[] AllHooks { get; } =
    [
        HookDebugId.PlaySpecificSound,
        HookDebugId.PlaySound,
        HookDebugId.PlaySystemSound,
        HookDebugId.PlayClipSound,
        HookDebugId.PlayMovieSound,
        HookDebugId.PlayBgmSound,
        HookDebugId.PlayWeatherSound,
        HookDebugId.SetVolume,
        HookDebugId.GetVolume,
        HookDebugId.LoadSoundFile,
        HookDebugId.GetResourceSync,
        HookDebugId.GetResourceAsync,
    ];
}
