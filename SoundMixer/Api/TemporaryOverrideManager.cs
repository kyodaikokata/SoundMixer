using System.Collections.Generic;
using System.Linq;

namespace SoundMixer.Api;

/// <summary>Session-only overrides keyed by caller tag (Penumbra TempModManager pattern).</summary>
internal sealed class TemporaryOverrideManager
{
    private readonly Dictionary<string, TemporaryOverrideEntry> _entries = new(StringComparer.Ordinal);

    internal bool HasAnyOverrides => _entries.Count > 0;

    internal IReadOnlyList<TemporaryOverrideEntry> GetOrderedEntries()
    {
        return _entries.Values.OrderBy(e => e.Priority).ThenBy(e => e.Tag, StringComparer.Ordinal).ToList();
    }

    internal void SetEnabled(string tag, int priority, bool enabled)
    {
        var entry = GetOrCreate(tag, priority);
        entry.Enabled = enabled;
    }

    internal void ClearEnabled(string tag)
    {
        if (_entries.TryGetValue(tag, out var entry))
        {
            entry.Enabled = null;
            PruneIfEmpty(tag, entry);
        }
    }

    internal void SetPresetId(string tag, int priority, string presetId)
    {
        var entry = GetOrCreate(tag, priority);
        entry.PresetId = presetId;
    }

    internal void ClearPreset(string tag)
    {
        if (_entries.TryGetValue(tag, out var entry))
        {
            entry.PresetId = null;
            PruneIfEmpty(tag, entry);
        }
    }

    internal void SetGroupVolume(string tag, int priority, string groupId, float volume)
    {
        var entry = GetOrCreate(tag, priority);
        entry.GroupVolumes[groupId] = volume;
    }

    internal void ClearGroupVolume(string tag, string groupId)
    {
        if (_entries.TryGetValue(tag, out var entry))
        {
            entry.GroupVolumes.Remove(groupId);
            PruneIfEmpty(tag, entry);
        }
    }

    internal void RemoveTag(string tag)
    {
        _entries.Remove(tag);
    }

    internal void ClearAll()
    {
        _entries.Clear();
    }

    private TemporaryOverrideEntry GetOrCreate(string tag, int priority)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new ArgumentException("IPC tag must not be empty.", nameof(tag));
        }

        if (!_entries.TryGetValue(tag, out var entry))
        {
            entry = new TemporaryOverrideEntry { Tag = tag, Priority = priority };
            _entries[tag] = entry;
        }
        else if (priority > entry.Priority)
        {
            entry.Priority = priority;
        }

        return entry;
    }

    private void PruneIfEmpty(string tag, TemporaryOverrideEntry entry)
    {
        if (entry.Enabled == null && entry.PresetId == null && entry.GroupVolumes.Count == 0)
        {
            _entries.Remove(tag);
        }
    }
}

internal sealed class TemporaryOverrideEntry
{
    internal string Tag { get; init; } = "";
    internal int Priority { get; set; }
    internal bool? Enabled { get; set; }
    internal string? PresetId { get; set; }
    internal Dictionary<string, float> GroupVolumes { get; } = new();
}
