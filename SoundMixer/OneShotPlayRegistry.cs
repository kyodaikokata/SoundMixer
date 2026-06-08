using System.Collections.Concurrent;
using FFXIVClientStructs.FFXIV.Client.Sound;

namespace SoundMixer;

/// <summary>
/// UI/menu one-shots: play hooks bypass volume detours; scaling is applied once via native SetVolume.
/// </summary>
internal static unsafe class OneShotPlayRegistry
{
    private const double EntryLifetimeSeconds = 30;
    private const double PendingLifetimeSeconds = 5;

    private static readonly ConcurrentDictionary<nint, Entry> Entries = new();
    private static readonly ConcurrentDictionary<string, PendingEntry> Pending = new();

    internal delegate bool ApplyVolumeDelegate(SoundData* soundData, float multiplier);

    internal delegate bool FindActiveSoundDelegate(uint soundIndex, out SoundData* soundData);

    private sealed class Entry
    {
        internal string Path { get; init; } = string.Empty;
        internal float Multiplier { get; init; } = 1f;
        internal bool VolumeApplied { get; set; }
        internal DateTime RegisteredAt { get; init; }
    }

    private sealed class PendingEntry
    {
        internal string Path { get; init; } = string.Empty;
        internal float Multiplier { get; init; } = 1f;
        internal DateTime RegisteredAt { get; init; }
    }

    internal static void NotePending(string path, uint soundIndex, float multiplier)
    {
        if (string.IsNullOrWhiteSpace(path) || Math.Abs(multiplier - 1.0f) < 0.001f)
        {
            return;
        }

        Pending[MakeKey(path, soundIndex)] = new PendingEntry
        {
            Path = path.ToLowerInvariant(),
            Multiplier = multiplier,
            RegisteredAt = DateTime.UtcNow,
        };
    }

    internal static void Register(
        SoundData* soundData,
        string path,
        float multiplier = 1f,
        bool volumeAlreadyApplied = false
    )
    {
        if (soundData == null || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        Entries[(nint)soundData] = new Entry
        {
            Path = path.ToLowerInvariant(),
            Multiplier = multiplier,
            VolumeApplied = volumeAlreadyApplied,
            RegisteredAt = DateTime.UtcNow,
        };
    }

    internal static bool ShouldPassthroughVolumeHooks(SoundData* soundData)
    {
        if (soundData == null)
        {
            return false;
        }

        if (Entries.ContainsKey((nint)soundData))
        {
            return true;
        }

        if (!SoundDataSafety.TryReadSoundData(soundData, out _, out var soundNumber, out _))
        {
            return false;
        }

        return HasPendingForSoundNumber(soundNumber);
    }

    internal static void ProcessDeferredApplies(
        ApplyVolumeDelegate applyVolume,
        FindActiveSoundDelegate findActiveSound
    )
    {
        var now = DateTime.UtcNow;

        foreach (var (key, pending) in Pending)
        {
            if ((now - pending.RegisteredAt).TotalSeconds > PendingLifetimeSeconds)
            {
                Pending.TryRemove(key, out _);
                continue;
            }

            if (!TryParseKey(key, out _, out var soundIndex))
            {
                continue;
            }

            if (!findActiveSound(soundIndex, out var soundData))
            {
                continue;
            }

            Register(soundData, pending.Path, pending.Multiplier);
            Pending.TryRemove(key, out _);
            TryApplyEntry(soundData, applyVolume);
        }

        foreach (var (ptr, entry) in Entries)
        {
            if ((now - entry.RegisteredAt).TotalSeconds > EntryLifetimeSeconds)
            {
                Entries.TryRemove(ptr, out _);
                continue;
            }

            if (entry.VolumeApplied)
            {
                continue;
            }

            TryApplyEntry((SoundData*)ptr, applyVolume);
        }
    }

    internal static void Prune()
    {
        var now = DateTime.UtcNow;
        foreach (var (ptr, entry) in Entries)
        {
            if ((now - entry.RegisteredAt).TotalSeconds > EntryLifetimeSeconds)
            {
                Entries.TryRemove(ptr, out _);
            }
        }

        foreach (var (key, pending) in Pending)
        {
            if ((now - pending.RegisteredAt).TotalSeconds > PendingLifetimeSeconds)
            {
                Pending.TryRemove(key, out _);
            }
        }
    }

    internal static void NotifyReleased(SoundData* soundData)
    {
        if (soundData == null)
        {
            return;
        }

        Entries.TryRemove((nint)soundData, out _);
    }

    internal static void Clear()
    {
        Entries.Clear();
        Pending.Clear();
    }

    private static void TryApplyEntry(SoundData* soundData, ApplyVolumeDelegate applyVolume)
    {
        if (soundData == null || !Entries.TryGetValue((nint)soundData, out var entry))
        {
            return;
        }

        if (entry.VolumeApplied || Math.Abs(entry.Multiplier - 1.0f) < 0.001f)
        {
            return;
        }

        if (applyVolume(soundData, entry.Multiplier))
        {
            entry.VolumeApplied = true;
        }
    }

    private static bool HasPendingForSoundNumber(uint soundNumber)
    {
        var now = DateTime.UtcNow;
        foreach (var (key, pending) in Pending)
        {
            if ((now - pending.RegisteredAt).TotalSeconds > PendingLifetimeSeconds)
            {
                continue;
            }

            if (TryParseKey(key, out _, out var pendingIndex) && pendingIndex == soundNumber)
            {
                return true;
            }
        }

        return false;
    }

    private static string MakeKey(string path, uint soundIndex) =>
        $"{path.ToLowerInvariant()}:{soundIndex}";

    private static bool TryParseKey(string key, out string path, out uint soundIndex)
    {
        path = string.Empty;
        soundIndex = 0;

        var colon = key.LastIndexOf(':');
        if (colon <= 0 || colon >= key.Length - 1)
        {
            return false;
        }

        if (!uint.TryParse(key[(colon + 1)..], out soundIndex))
        {
            return false;
        }

        path = key[..colon];
        return !string.IsNullOrWhiteSpace(path);
    }
}
