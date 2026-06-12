using System.Collections.Concurrent;

namespace SoundMixer;

/// <summary>
/// Per-active-node cache to skip repeated path resolve and stable ForceRefresh work each frame.
/// Invalidated on territory change, runtime clear, and group volume refresh.
/// </summary>
internal sealed class ActiveSoundEnforceCache
{
    private sealed class Entry
    {
        internal int Generation;
        internal string ResolvedPath = string.Empty;
        internal uint SoundNumber;
        internal float Multiplier;
        internal float LastEffectiveVolume;
    }

    private int _generation;
    private readonly ConcurrentDictionary<nint, Entry> _entries = new();

    internal void Invalidate()
    {
        _generation++;
        _entries.Clear();
    }

    internal void Remove(nint ptr)
    {
        _entries.TryRemove(ptr, out _);
    }

    /// <summary>
    /// True when configured enforcement can skip path resolve and ForceRefresh for this frame.
    /// </summary>
    internal bool IsConfiguredEnforcementStable(nint ptr, float fieldVolume)
    {
        if (!_entries.TryGetValue(ptr, out var entry) || entry.Generation != _generation)
        {
            return false;
        }

        if (Math.Abs(entry.Multiplier - 1.0f) < 0.001f)
        {
            return true;
        }

        return entry.LastEffectiveVolume > 0.001f
            && Math.Abs(fieldVolume - entry.LastEffectiveVolume) < 0.05f;
    }

    internal bool TryGetResolved(
        nint ptr,
        out string resolvedPath,
        out uint soundNumber,
        out float multiplier
    )
    {
        resolvedPath = string.Empty;
        soundNumber = 0;
        multiplier = 1.0f;

        if (!_entries.TryGetValue(ptr, out var entry) || entry.Generation != _generation)
        {
            return false;
        }

        resolvedPath = entry.ResolvedPath;
        soundNumber = entry.SoundNumber;
        multiplier = entry.Multiplier;
        return true;
    }

    internal void RememberResolved(
        nint ptr,
        string resolvedPath,
        uint soundNumber,
        float multiplier
    )
    {
        if (ptr == nint.Zero || string.IsNullOrWhiteSpace(resolvedPath))
        {
            return;
        }

        resolvedPath = resolvedPath.ToLowerInvariant();
        var keepEffective = 0f;
        if (_entries.TryGetValue(ptr, out var existing)
            && existing.Generation == _generation
            && existing.ResolvedPath == resolvedPath
            && existing.SoundNumber == soundNumber
            && Math.Abs(existing.Multiplier - multiplier) < 0.001f)
        {
            keepEffective = existing.LastEffectiveVolume;
        }

        _entries[ptr] = new Entry
        {
            Generation = _generation,
            ResolvedPath = resolvedPath,
            SoundNumber = soundNumber,
            Multiplier = multiplier,
            LastEffectiveVolume = keepEffective,
        };
    }

    internal void NoteEffectiveVolume(nint ptr, float effectiveVolume)
    {
        if (_entries.TryGetValue(ptr, out var entry) && entry.Generation == _generation)
        {
            entry.LastEffectiveVolume = effectiveVolume;
        }
    }
}
