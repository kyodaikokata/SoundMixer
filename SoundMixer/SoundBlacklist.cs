using System.Collections.Concurrent;
using DotNet.Globbing;
using FFXIVClientStructs.FFXIV.Client.Sound;

namespace SoundMixer;

/// <summary>
/// User and official blacklists are stored and shown separately, but both block hook/scan paths.
/// </summary>
internal static unsafe class SoundBlacklist
{
    private static readonly ConcurrentDictionary<nint, byte> BlockedPointers = new();

    private static List<CompiledBlacklistRule> s_userRules = new();
    private static List<CompiledBlacklistRule> s_officialRules = new();
    private static List<CompiledBlacklistRule> s_mountLoopRules = new();
    private static int s_officialRevision;

    internal static int OfficialRevision => s_officialRevision;
    internal static int UserRuleCount => s_userRules.Count;
    internal static int OfficialRuleCount => s_officialRules.Count;
    internal static int ActiveRuleCount => UserRuleCount + OfficialRuleCount;

    internal static IReadOnlyList<UserSoundBlacklistEntry> UserEntries { get; private set; } =
        Array.Empty<UserSoundBlacklistEntry>();

    internal static IReadOnlyList<OfficialSoundBlacklistEntryDto> OfficialEntries { get; private set; } =
        Array.Empty<OfficialSoundBlacklistEntryDto>();

    internal static void Rebuild(Configuration config, OfficialSoundBlacklistDto? official)
    {
        UserEntries = config.UserSoundBlacklist.ToList();
        OfficialEntries = ExpandOfficialEntries(official);
        s_officialRevision = official?.Revision ?? 0;

        s_userRules = CompileRules(UserEntries.Select(e => (e.MatchKind, e.Match)));
        s_officialRules = CompileRules(
            OfficialEntries.Select(e => (ParseOfficialKind(e.Kind), e.Match))
        );
        s_mountLoopRules = s_userRules
            .Where(IsMountLoopRule)
            .Concat(s_officialRules.Where(IsMountLoopRule))
            .ToList();

        BlockedPointers.Clear();
    }

    /// <summary>BGM/ride BGM may match **/guideroid** but must remain volume-controllable.</summary>
    internal static bool IsExcludedFromMountLoopBlacklist(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalized = path.ToLowerInvariant();
        return StreamingBgmTracker.IsBgmOrMusicPath(normalized)
            || MountTransitionGuard.IsRideBgmPath(normalized);
    }

    internal static bool IsMountLoopBlockedPath(string? path)
    {
        if (IsExcludedFromMountLoopBlacklist(path))
        {
            return false;
        }

        return IsPathBlockedByRules(path, s_mountLoopRules);
    }

    internal static bool IsPlayHookBlockedPath(string? path)
    {
        if (IsExcludedFromMountLoopBlacklist(path))
        {
            return false;
        }

        return IsPathBlocked(path);
    }

    internal static bool IsPathBlocked(string? path)
    {
        return IsPathBlockedByRules(path, s_userRules) || IsPathBlockedByRules(path, s_officialRules);
    }

    internal static bool HasMountLoopBlockRules()
    {
        return s_mountLoopRules.Count > 0;
    }

    internal static bool IsPointerBlocked(SoundData* soundData)
    {
        return soundData != null && BlockedPointers.ContainsKey((nint)soundData);
    }

    internal static bool ShouldBypassSoundData(SoundData* soundData)
    {
        if (soundData == null)
        {
            return true;
        }

        // Never call GetFileName / TryGetPathFromSoundData here — that can crash on mount
        // loop nodes (e.g. Guideroid). Path-based blocking is resolved at play time via
        // RegisterPointerForPath; this cache is the only safe active-list bypass signal.
        return IsPointerBlocked(soundData);
    }

    internal static void RegisterBlockedPlayResult(SoundData* soundData, string? path)
    {
        RegisterPointerForPath(soundData, path);
        if (IsMountLoopBlockedPath(path))
        {
            MountTransitionGuard.NotifyGuideroidLoopSound(path);
        }
    }

    [ThreadStatic]
    private static int s_playBypassDepth;

    internal static bool IsPlayBypassActive => s_playBypassDepth > 0;

    internal static PlayBypassScope EnterPlayBypass()
    {
        s_playBypassDepth++;
        return new PlayBypassScope();
    }

    internal readonly struct PlayBypassScope : IDisposable
    {
        public void Dispose()
        {
            if (s_playBypassDepth > 0)
            {
                s_playBypassDepth--;
            }
        }
    }

    internal static void RegisterPointer(SoundData* soundData)
    {
        if (soundData == null)
        {
            return;
        }

        BlockedPointers.TryAdd((nint)soundData, 0);
    }

    internal static void RegisterPointerForPath(SoundData* soundData, string? path)
    {
        if (soundData == null)
        {
            return;
        }

        if (IsPathBlocked(path))
        {
            RegisterPointer(soundData);
        }
    }

    internal static void PruneInactivePointers()
    {
        foreach (var ptr in BlockedPointers.Keys)
        {
            var soundData = (SoundData*)ptr;
            if (!SoundDataSafety.TryReadSoundData(soundData, out var isActive, out _, out _)
                || !isActive)
            {
                BlockedPointers.TryRemove(ptr, out _);
            }
        }
    }

    internal static void ClearPointerCache()
    {
        BlockedPointers.Clear();
    }

    internal static bool TryAddUserEntry(
        Configuration config,
        SoundBlacklistMatchKind kind,
        string match,
        string note
    )
    {
        match = match.Trim();
        if (string.IsNullOrWhiteSpace(match))
        {
            return false;
        }

        if (config.UserSoundBlacklist.Any(
                e => e.MatchKind == kind
                     && string.Equals(e.Match, match, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        config.UserSoundBlacklist.Add(
            new UserSoundBlacklistEntry
            {
                MatchKind = kind,
                Match = match,
                Note = note?.Trim() ?? string.Empty,
            }
        );
        return true;
    }

    internal static SoundBlacklistMatchKind InferMatchKind(string match)
    {
        match = match.Trim();
        if (LooksLikeGlob(match))
        {
            return SoundBlacklistMatchKind.Glob;
        }

        if (match.Contains('/', StringComparison.Ordinal) || match.EndsWith(".scd", StringComparison.OrdinalIgnoreCase))
        {
            return SoundBlacklistMatchKind.Path;
        }

        return SoundBlacklistMatchKind.Keyword;
    }

    private static List<OfficialSoundBlacklistEntryDto> ExpandOfficialEntries(OfficialSoundBlacklistDto? official)
    {
        if (official == null)
        {
            return new List<OfficialSoundBlacklistEntryDto>();
        }

        if (official.Entries.Count > 0)
        {
            return official.Entries;
        }

        var legacy = new List<OfficialSoundBlacklistEntryDto>();
        foreach (var pattern in official.Patterns)
        {
            legacy.Add(
                new OfficialSoundBlacklistEntryDto
                {
                    Kind = "glob",
                    Match = pattern,
                }
            );
        }

        foreach (var keyword in official.Keywords)
        {
            legacy.Add(
                new OfficialSoundBlacklistEntryDto
                {
                    Kind = "keyword",
                    Match = keyword,
                }
            );
        }

        return legacy;
    }

    private static List<CompiledBlacklistRule> CompileRules(
        IEnumerable<(SoundBlacklistMatchKind Kind, string Match)> entries
    )
    {
        var rules = new List<CompiledBlacklistRule>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (kind, rawMatch) in entries)
        {
            if (string.IsNullOrWhiteSpace(rawMatch))
            {
                continue;
            }

            var match = rawMatch.Trim().ToLowerInvariant();
            var key = $"{kind}:{match}";
            if (!seen.Add(key))
            {
                continue;
            }

            rules.Add(
                new CompiledBlacklistRule
                {
                    Kind = kind,
                    Match = match,
                    Glob = kind == SoundBlacklistMatchKind.Glob ? Glob.Parse(match) : null,
                }
            );
        }

        return rules;
    }

    private static bool IsPathBlockedByRules(string? path, IReadOnlyList<CompiledBlacklistRule> rules)
    {
        if (string.IsNullOrWhiteSpace(path) || rules.Count == 0)
        {
            return false;
        }

        path = path.ToLowerInvariant();
        var scdPath = path;
        var slash = path.LastIndexOf('/');
        if (slash > 0 && int.TryParse(path[(slash + 1)..], out _))
        {
            scdPath = path[..slash];
        }

        foreach (var rule in rules)
        {
            switch (rule.Kind)
            {
                case SoundBlacklistMatchKind.Keyword:
                    if (path.Contains(rule.Match, StringComparison.Ordinal))
                    {
                        return true;
                    }

                    break;
                case SoundBlacklistMatchKind.Path:
                    if (path == rule.Match
                        || scdPath == rule.Match
                        || path.StartsWith(rule.Match + "/", StringComparison.Ordinal))
                    {
                        return true;
                    }

                    break;
                case SoundBlacklistMatchKind.Glob:
                    if (rule.Glob != null && rule.Glob.IsMatch(path))
                    {
                        return true;
                    }

                    break;
            }
        }

        return false;
    }

    private static SoundBlacklistMatchKind ParseOfficialKind(string? kind)
    {
        return kind?.Trim().ToLowerInvariant() switch
        {
            "path" => SoundBlacklistMatchKind.Path,
            "glob" => SoundBlacklistMatchKind.Glob,
            _ => SoundBlacklistMatchKind.Keyword,
        };
    }

    private static bool LooksLikeGlob(string pattern)
    {
        return pattern.Contains('*', StringComparison.Ordinal)
            || pattern.Contains('?', StringComparison.Ordinal)
            || pattern.Contains('[', StringComparison.Ordinal);
    }

    private static bool IsMountLoopRule(CompiledBlacklistRule rule)
    {
        if (rule.Kind == SoundBlacklistMatchKind.Keyword
            && (rule.Match.Contains("guideroid", StringComparison.Ordinal)
                || rule.Match.Contains("se_bt_etc_mount_guideroid", StringComparison.Ordinal)))
        {
            return true;
        }

        if (rule.Kind == SoundBlacklistMatchKind.Glob
            && rule.Match.Contains("guideroid", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
}
