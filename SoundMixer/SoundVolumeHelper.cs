using System.Collections.Concurrent;
using FFXIVClientStructs.FFXIV.Client.Sound;

namespace SoundMixer;

internal readonly struct PlayContext
{
    internal string Path { get; init; }
    internal uint SoundNumber { get; init; }
    internal float Multiplier { get; init; }
    /// <summary>Native play volume before plugin scaling (usually 1.0).</summary>
    internal float GameVolume { get; init; }
}

internal static unsafe class SoundVolumeHelper
{
    private const double RecentPlaySpecificSeconds = 4.0;

    private sealed class RecentPlaySpecificEntry
    {
        internal string MaterialPath { get; init; } = string.Empty;
        internal DateTime RecordedUtc { get; init; }
    }

    private static readonly ConcurrentDictionary<string, float> PendingMultipliers = new();
    private static readonly ConcurrentDictionary<string, RecentPlaySpecificEntry> RecentPlaySpecificByKey = new();

    [ThreadStatic]
    private static PlayContext? s_playContext;

    [ThreadStatic]
    private static bool s_setVolumeCalledThisPlay;

    internal static void BeginPlay(string path, uint soundNumber, float multiplier, float gameVolume = 1.0f)
    {
        s_setVolumeCalledThisPlay = false;
        gameVolume = gameVolume <= 0.001f ? 1.0f : gameVolume;

        s_playContext = new PlayContext
        {
            Path = path,
            SoundNumber = soundNumber,
            Multiplier = multiplier,
            GameVolume = gameVolume,
        };

        if (multiplier is > 0.999f and < 1.001f)
        {
            return;
        }

        PendingMultipliers[MakeKey(path, soundNumber)] = multiplier;
    }

    internal static bool TryGetActivePlayBaseVolume(out float gameVolume)
    {
        if (s_playContext is { } ctx)
        {
            gameVolume = ctx.GameVolume > 0.001f ? ctx.GameVolume : 1.0f;
            return true;
        }

        gameVolume = 1.0f;
        return false;
    }

    internal static void EndPlay()
    {
        if (s_playContext is { } ctx)
        {
            PendingMultipliers.TryRemove(MakeKey(ctx.Path, ctx.SoundNumber), out _);
        }

        s_playContext = null;
    }

    internal static void MarkSetVolumeCalled()
    {
        s_setVolumeCalledThisPlay = true;
    }

    internal static bool WasSetVolumeCalledThisPlay => s_setVolumeCalledThisPlay;

    /// <summary>
    /// PlaySpecificSound may fire on setup indices (e.g. /27) while PlaySound plays the audible index (e.g. /1).
    /// Remember the material path so sibling nodes and foot containers can resolve child-group volume.
    /// </summary>
    internal static void NoteRecentPlaySpecific(string path, uint soundNumber, float multiplier)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        path = path.ToLowerInvariant().Trim();
        var now = DateTime.UtcNow;
        var entry = new RecentPlaySpecificEntry
        {
            MaterialPath = path,
            RecordedUtc = now,
        };

        var scdBase = PathResolver.GetScdBasePath(path);
        RecentPlaySpecificByKey[scdBase] = entry;

        if (TryGetFootContainerPath(scdBase, out var containerPath))
        {
            RecentPlaySpecificByKey[containerPath] = entry;
        }

        if (multiplier is > 0.999f and < 1.001f)
        {
            return;
        }

        PendingMultipliers[MakeKey(path, soundNumber)] = multiplier;
        PendingMultipliers[MakeKey(scdBase, soundNumber)] = multiplier;
    }

    internal static string UpgradeEnforcementPath(string path, uint soundNumber)
    {
        if (TryResolveSiblingMaterialPath(path, out var materialPath))
        {
            return PathResolver.GetScdBasePath(materialPath);
        }

        return path;
    }

    /// <summary>
    /// SoundData.FileName is often the foot container; Scds cache may hold the material file.
    /// </summary>
    internal static string ChooseNodeEnforcementPath(string fileName, string scdCached, uint soundNumber)
    {
        fileName = string.IsNullOrWhiteSpace(fileName) ? string.Empty : fileName.ToLowerInvariant().Trim();
        scdCached = string.IsNullOrWhiteSpace(scdCached) ? string.Empty : scdCached.ToLowerInvariant().Trim();

        if (soundNumber == FootstepPlaybackBridge.PlaybackSoundIndex)
        {
            if (FootstepPlaybackBridge.IsFootMaterialPath(scdCached))
            {
                return PathResolver.GetScdBasePath(scdCached);
            }

            if (FootstepPlaybackBridge.IsFootMaterialPath(fileName))
            {
                return PathResolver.GetScdBasePath(fileName);
            }

            if (TryResolveSiblingMaterialPath(fileName, out var materialPath)
                || TryResolveSiblingMaterialPath(scdCached, out materialPath))
            {
                return PathResolver.GetScdBasePath(materialPath);
            }
        }

        var best = ChooseAuthoritativePath(scdCached, fileName);
        if (string.IsNullOrWhiteSpace(best))
        {
            return string.Empty;
        }

        return UpgradeEnforcementPath(best, soundNumber);
    }

    internal static bool TryResolveSiblingMaterialPath(string nodePath, out string materialPath)
    {
        materialPath = nodePath.ToLowerInvariant().Trim();
        PruneStaleRecentPlaySpecific();

        var nodeScd = PathResolver.GetScdBasePath(materialPath);
        if (RecentPlaySpecificByKey.TryGetValue(nodeScd, out var entry) && IsRecentPlaySpecificFresh(entry))
        {
            materialPath = PathResolver.GetScdBasePath(entry.MaterialPath);
            return true;
        }

        if (IsFootContainerPath(nodeScd))
        {
            foreach (var (key, candidate) in RecentPlaySpecificByKey)
            {
                if (!IsRecentPlaySpecificFresh(candidate))
                {
                    continue;
                }

                if (key == nodeScd
                    || PathResolver.ShareScdFile(key, candidate.MaterialPath)
                       && TryGetFootContainerPath(PathResolver.GetScdBasePath(candidate.MaterialPath), out var container)
                       && container == nodeScd)
                {
                    materialPath = PathResolver.GetScdBasePath(candidate.MaterialPath);
                    return true;
                }
            }
        }

        return false;
    }

    internal static bool IsPlaySiblingTarget(
        string nodePath,
        uint nodeSoundNumber,
        string playSpecificPath,
        uint playSpecificIndex
    )
    {
        if (nodeSoundNumber == playSpecificIndex)
        {
            return true;
        }

        if (PathResolver.ShareScdFile(nodePath, playSpecificPath))
        {
            return true;
        }

        return TryResolveSiblingMaterialPath(nodePath, out var materialPath)
               && PathResolver.ShareScdFile(materialPath, playSpecificPath);
    }

    /// <summary>
    /// Single enforcement rule: same stacked group math as monitor / refresh.
    /// </summary>
    internal static float GetEnforcementMultiplier(
        VolumeCalculator calculator,
        string path,
        uint soundNumber
    )
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return 1.0f;
        }

        return calculator.GetVolumeForSound(
            PathResolver.BuildSpecificPath(path.ToLowerInvariant().Trim(), (int)soundNumber)
        );
    }

    internal static float GetMultiplier(VolumeCalculator calculator, string path, uint soundNumber)
    {
        return GetEnforcementMultiplier(calculator, path, soundNumber);
    }

    /// <summary>
    /// Prefer the path read from the playing SoundData node over a PlaySpecificSound container path.
    /// </summary>
    internal static string ChooseAuthoritativePath(string livePath, string storedPath)
    {
        livePath = string.IsNullOrWhiteSpace(livePath) ? string.Empty : livePath.ToLowerInvariant();
        storedPath = string.IsNullOrWhiteSpace(storedPath) ? string.Empty : storedPath.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(livePath))
        {
            return storedPath;
        }

        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return livePath;
        }

        if (livePath == storedPath)
        {
            return livePath;
        }

        return livePath.Length > storedPath.Length ? livePath : storedPath;
    }

    internal static float ResolveMultiplier(
        VolumeCalculator calculator,
        string path,
        uint soundNumber
    )
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            return GetEnforcementMultiplier(calculator, path, soundNumber);
        }

        if (s_playContext is { } ctx)
        {
            if (!string.IsNullOrWhiteSpace(ctx.Path))
            {
                return GetEnforcementMultiplier(calculator, ctx.Path, soundNumber);
            }

            if (ctx.SoundNumber == soundNumber)
            {
                return ctx.Multiplier;
            }
        }

        foreach (var (key, multiplier) in PendingMultipliers)
        {
            if (key.EndsWith($":{soundNumber}", StringComparison.Ordinal))
            {
                return multiplier;
            }
        }

        return 1.0f;
    }

    internal static float ScaleVolume(float volume, float multiplier)
    {
        if (multiplier <= 0.001f)
        {
            return 0f;
        }

        return volume * multiplier;
    }

    internal static unsafe string NormalizePath(byte* path)
    {
        return Util.ReadTerminatedString(path).ToLowerInvariant();
    }

    internal static bool TryGetPathFromSoundData(SoundData* soundData, out string path)
    {
        path = GetPathFromSoundData(soundData);
        return !string.IsNullOrWhiteSpace(path);
    }

    internal static string GetPathFromSoundData(SoundData* soundData)
    {
        if (soundData == null
            || ZoneTransitionGuard.ShouldSkipSoundDataListAccess()
            || !SoundDataSafety.IsReadable((nint)soundData))
        {
            return string.Empty;
        }

        if (SoundBlacklist.ShouldBypassSoundData(soundData))
        {
            return string.Empty;
        }

        try
        {
            var handle = soundData->SoundResourceHandle;
            if (handle != null && SoundDataSafety.IsReadable((nint)handle, 0x40))
            {
                var name = handle->FileName.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name.ToLowerInvariant();
                }
            }
        }
        catch (Exception ex)
        {
            Services.PluginLog.Verbose(ex, "SoundMixer: failed to read safe path from SoundData handle");
        }

        return string.Empty;
    }

    private static string MakeKey(string path, uint soundNumber)
    {
        return $"{path.ToLowerInvariant()}:{soundNumber}";
    }

    private static bool IsRecentPlaySpecificFresh(RecentPlaySpecificEntry entry)
    {
        return (DateTime.UtcNow - entry.RecordedUtc).TotalSeconds <= RecentPlaySpecificSeconds;
    }

    private static void PruneStaleRecentPlaySpecific()
    {
        var now = DateTime.UtcNow;
        foreach (var (key, entry) in RecentPlaySpecificByKey)
        {
            if ((now - entry.RecordedUtc).TotalSeconds > RecentPlaySpecificSeconds)
            {
                RecentPlaySpecificByKey.TryRemove(key, out _);
            }
        }
    }

    private static bool TryGetFootContainerPath(string materialScdPath, out string containerPath)
    {
        containerPath = string.Empty;
        const string marker = "/foot/foot/";
        var idx = materialScdPath.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0)
        {
            return false;
        }

        containerPath = $"{materialScdPath[..(idx + marker.Length - 1)]}.scd";
        return true;
    }

    internal static bool IsFootContainerPath(string scdPath)
    {
        scdPath = scdPath.ToLowerInvariant();
        if (!scdPath.Contains("/foot/foot", StringComparison.Ordinal))
        {
            return false;
        }

        return scdPath.EndsWith("/foot/foot.scd", StringComparison.Ordinal)
               || scdPath.EndsWith("foot/foot.scd", StringComparison.Ordinal);
    }
}
