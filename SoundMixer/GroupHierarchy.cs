using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;

namespace SoundMixer;

internal enum GroupDropIntent
{
    Before,
    Into,
    After,
    ToRoot,
}

internal static class GroupHierarchy
{
    internal const string DragDropType = "SoundMixerGroup";

    internal static SoundGroup? FindById(Configuration config, string? groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            return null;
        }

        return config.Groups.FirstOrDefault(g => g.Id == groupId);
    }

    internal static List<SoundGroup> GetChildren(Configuration config, string? parentId)
    {
        return config.Groups
            .Where(g => string.Equals(g.ParentId, parentId, StringComparison.Ordinal))
            .ToList();
    }

    internal static List<SoundGroup> GetRoots(Configuration config)
    {
        return GetChildren(config, null);
    }

    /// <summary>Fix duplicate ids, broken parents, and cycles left by interrupted drag-drop saves.</summary>
    internal static bool RepairConfiguration(Configuration config)
    {
        var changed = false;
        changed |= RemoveDuplicateGroupIds(config);
        changed |= FixInvalidParentIds(config);
        changed |= BreakParentCycles(config);
        changed |= CleanupSoundToGroupReferences(config);
        return changed;
    }

    internal static string? GetParentName(Configuration config, SoundGroup group)
    {
        return FindById(config, group.ParentId)?.Name;
    }

    internal static bool WouldCreateCycle(Configuration config, string childId, string? newParentId)
    {
        if (string.IsNullOrWhiteSpace(newParentId))
        {
            return false;
        }

        if (childId == newParentId)
        {
            return true;
        }

        var current = newParentId;
        var depth = 0;
        while (!string.IsNullOrWhiteSpace(current) && depth++ < 64)
        {
            if (current == childId)
            {
                return true;
            }

            current = FindById(config, current)?.ParentId;
        }

        return false;
    }

    internal static bool SetParent(Configuration config, string groupId, string? parentId)
    {
        var group = FindById(config, groupId);
        if (group == null)
        {
            return false;
        }

        if (WouldCreateCycle(config, groupId, parentId))
        {
            return false;
        }

        var previousParentId = group.ParentId;
        group.ParentId = parentId;
        MoveNearParent(config, group);
        ApplyOverrideColorAfterParentChangeIfNeeded(config, group, previousParentId);
        return true;
    }

    internal static void RemoveFromParent(Configuration config, string groupId)
    {
        var group = FindById(config, groupId);
        if (group == null)
        {
            return;
        }

        var previousParentId = group.ParentId;
        if (string.IsNullOrWhiteSpace(previousParentId))
        {
            return;
        }

        group.ParentId = null;
        MoveToRootEnd(config, group);
        ApplyOverrideColorAfterParentChangeIfNeeded(config, group, previousParentId);
    }

    internal static bool MoveBefore(Configuration config, string draggedId, string targetId)
    {
        if (draggedId == targetId)
        {
            return false;
        }

        var dragged = FindById(config, draggedId);
        var target = FindById(config, targetId);
        if (dragged == null || target == null)
        {
            return false;
        }

        if (IsDescendantOf(config, targetId, draggedId))
        {
            return false;
        }

        var previousParentId = dragged.ParentId;
        dragged.ParentId = target.ParentId;
        Reinsert(config, draggedId, config.Groups.FindIndex(g => g.Id == targetId));
        ApplyOverrideColorAfterParentChangeIfNeeded(config, dragged, previousParentId);
        return true;
    }

    internal static bool MoveAfter(Configuration config, string draggedId, string targetId)
    {
        if (draggedId == targetId)
        {
            return false;
        }

        var dragged = FindById(config, draggedId);
        var target = FindById(config, targetId);
        if (dragged == null || target == null)
        {
            return false;
        }

        if (IsDescendantOf(config, targetId, draggedId))
        {
            return false;
        }

        var previousParentId = dragged.ParentId;
        dragged.ParentId = target.ParentId;
        var targetIndex = config.Groups.FindIndex(g => g.Id == targetId);
        Reinsert(config, draggedId, targetIndex + 1);
        ApplyOverrideColorAfterParentChangeIfNeeded(config, dragged, previousParentId);
        return true;
    }

    internal static void DeleteGroup(Configuration config, string groupId)
    {
        if (!PresetManager.CanDeleteGroup(config))
        {
            return;
        }

        var group = FindById(config, groupId);
        if (group == null)
        {
            return;
        }

        var parentId = group.ParentId;
        foreach (var child in GetChildren(config, groupId))
        {
            var previousParentId = child.ParentId;
            child.ParentId = parentId;
            ApplyOverrideColorAfterParentChangeIfNeeded(config, child, previousParentId);
        }

        foreach (var path in group.SoundPaths)
        {
            config.SoundToGroup.Remove(path);
        }

        foreach (var key in config.SoundToGroup.Where(kv => kv.Value == groupId).Select(kv => kv.Key).ToList())
        {
            config.SoundToGroup.Remove(key);
        }

        config.Groups.Remove(group);
    }

    internal static SoundGroup CreateGroup(Configuration config, string name, string? parentId = null)
    {
        var group = new SoundGroup
        {
            Name = name,
            ParentId = parentId,
            Icon = string.Empty,
            IsBuiltIn = false,
        };

        if (string.IsNullOrWhiteSpace(parentId))
        {
            config.Groups.Add(group);
        }
        else
        {
            var parent = FindById(config, parentId);
            if (parent == null)
            {
                config.Groups.Add(group);
            }
            else
            {
                var insertIndex = FindInsertIndexAfterSubtree(config, parentId);
                config.Groups.Insert(insertIndex, group);
                ApplyOverrideColorAfterParentChange(config, group);
            }
        }

        return group;
    }

    internal static void ApplyOverrideColorAfterParentChange(Configuration config, SoundGroup group)
    {
        if (string.IsNullOrWhiteSpace(group.ParentId))
        {
            group.OverrideColorArgb = 0;
            return;
        }

        GroupColorHelper.InheritOverrideColorFromParent(config, group);
    }

    internal static void ApplyOverrideColorAfterParentChangeIfNeeded(
        Configuration config,
        SoundGroup group,
        string? previousParentId
    )
    {
        if (ParentIdsEqual(previousParentId, group.ParentId))
        {
            return;
        }

        ApplyOverrideColorAfterParentChange(config, group);
    }

    internal static bool HasPathRules(SoundGroup group) =>
        group.SoundPaths.Count > 0 || group.PathPatterns.Count > 0;

    internal static bool HasMatchingDescendant(Configuration config, SoundGroup group, Func<string, bool> matches)
    {
        if (matches(group.Name))
        {
            return true;
        }

        return GetChildren(config, group.Id).Any(child => HasMatchingDescendant(config, child, matches));
    }

    internal static void BeginDragSource(string groupId, string dragLabel)
    {
        if (!ImGui.BeginDragDropSource())
        {
            return;
        }

        ImGui.SetDragDropPayload(DragDropType, Encoding.UTF8.GetBytes(groupId));
        ImGui.TextUnformatted(dragLabel);
        ImGui.EndDragDropSource();
    }

    internal static void DrawDropIntentOverlay(Vector2 min, Vector2 max, GroupDropIntent intent, string label)
    {
        if (string.IsNullOrEmpty(label))
        {
            return;
        }

        var drawList = ImGui.GetWindowDrawList();
        var accent = ImGui.GetColorU32(new Vector4(0.35f, 0.9f, 0.5f, 1f));
        var accentFill = ImGui.GetColorU32(new Vector4(0.35f, 0.9f, 0.5f, 0.22f));
        const float lineThickness = 2f;

        switch (intent)
        {
            case GroupDropIntent.Before:
                drawList.AddLine(min, new Vector2(max.X, min.Y), accent, lineThickness);
                DrawDropIntentLabel(drawList, min, max, label, min.Y, accent);
                break;
            case GroupDropIntent.After:
                drawList.AddLine(new Vector2(min.X, max.Y), max, accent, lineThickness);
                DrawDropIntentLabel(drawList, min, max, label, max.Y - ImGui.GetTextLineHeight(), accent);
                break;
            case GroupDropIntent.Into:
            case GroupDropIntent.ToRoot:
                drawList.AddRectFilled(min, max, accentFill);
                drawList.AddRect(min, max, accent, 0f, ImDrawFlags.None, 1.5f);
                DrawDropIntentLabel(
                    drawList,
                    min,
                    max,
                    label,
                    min.Y + (max.Y - min.Y - ImGui.GetTextLineHeight()) * 0.5f,
                    accent,
                    centered: true
                );
                break;
        }
    }

    private static void DrawDropIntentLabel(
        ImDrawListPtr drawList,
        Vector2 min,
        Vector2 max,
        string label,
        float y,
        uint accent,
        bool centered = false
    )
    {
        var textSize = ImGui.CalcTextSize(label);
        var x = centered
            ? min.X + (max.X - min.X - textSize.X) * 0.5f
            : max.X - textSize.X - 6f;
        var bgMin = new Vector2(x - 4f, y - 2f);
        var bgMax = new Vector2(x + textSize.X + 4f, y + textSize.Y + 2f);
        drawList.AddRectFilled(bgMin, bgMax, ImGui.GetColorU32(new Vector4(0.08f, 0.08f, 0.08f, 0.9f)));
        drawList.AddText(new Vector2(x, y), accent, label);
    }

    /// <summary>Returns the dragged group id only on the drop frame (not while hovering).</summary>
    internal static unsafe string? TryConsumeDroppedGroupId()
    {
        var payload = ImGui.AcceptDragDropPayload(DragDropType);

        if (payload.IsNull)
        {
            return null;
        }
        if (payload.Data == null || payload.DataSize <= 0 || !payload.IsDelivery())
        {
            return null;
        }

        return Encoding.UTF8.GetString((byte*)payload.Data, payload.DataSize);
    }

    internal static GroupDropIntent GetDropIntentForItem()
    {
        return GetDropIntentForRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
    }

    internal static GroupDropIntent GetDropIntentForRect(Vector2 min, Vector2 max)
    {
        var mouseY = ImGui.GetMousePos().Y;
        var height = Math.Max(max.Y - min.Y, 1f);
        var relative = (mouseY - min.Y) / height;

        // Top/bottom bands are for sibling reorder; keep the middle band narrow so nesting stays deliberate.
        if (relative < 0.35f)
        {
            return GroupDropIntent.Before;
        }

        if (relative > 0.65f)
        {
            return GroupDropIntent.After;
        }

        return GroupDropIntent.Into;
    }

    internal static bool ApplyDrop(
        Configuration config,
        string draggedId,
        string? targetId,
        GroupDropIntent intent
    )
    {
        if (string.IsNullOrWhiteSpace(draggedId))
        {
            return false;
        }

        switch (intent)
        {
            case GroupDropIntent.ToRoot:
                return SetParent(config, draggedId, null);
            case GroupDropIntent.Into when !string.IsNullOrWhiteSpace(targetId):
                return SetParent(config, draggedId, targetId);
            case GroupDropIntent.Before when !string.IsNullOrWhiteSpace(targetId):
                return MoveBefore(config, draggedId, targetId);
            case GroupDropIntent.After when !string.IsNullOrWhiteSpace(targetId):
                return MoveAfter(config, draggedId, targetId);
            default:
                return false;
        }
    }

    private static bool ParentIdsEqual(string? a, string? b) =>
        string.Equals(a, b, StringComparison.Ordinal);

    private static void MoveNearParent(Configuration config, SoundGroup group)
    {
        if (string.IsNullOrWhiteSpace(group.ParentId))
        {
            MoveToRootEnd(config, group);
            return;
        }

        var insertIndex = FindInsertIndexAfterSubtree(config, group.ParentId);
        Reinsert(config, group.Id, insertIndex);
    }

    private static void MoveToRootEnd(Configuration config, SoundGroup group)
    {
        Reinsert(config, group.Id, config.Groups.Count);
    }

    private static void Reinsert(Configuration config, string groupId, int insertIndex)
    {
        var currentIndex = config.Groups.FindIndex(g => g.Id == groupId);
        if (currentIndex < 0)
        {
            return;
        }

        var group = config.Groups[currentIndex];
        config.Groups.RemoveAt(currentIndex);

        if (insertIndex > currentIndex)
        {
            insertIndex--;
        }

        insertIndex = Math.Clamp(insertIndex, 0, config.Groups.Count);
        config.Groups.Insert(insertIndex, group);
    }

    private static int FindInsertIndexAfterSubtree(Configuration config, string parentId)
    {
        var parentIndex = config.Groups.FindIndex(g => g.Id == parentId);
        if (parentIndex < 0)
        {
            return config.Groups.Count;
        }

        var maxIndex = parentIndex;
        foreach (var group in config.Groups)
        {
            if (IsDescendantOf(config, group.Id, parentId))
            {
                var index = config.Groups.FindIndex(g => g.Id == group.Id);
                if (index > maxIndex)
                {
                    maxIndex = index;
                }
            }
        }

        return maxIndex + 1;
    }

    private static bool IsDescendantOf(Configuration config, string groupId, string ancestorId)
    {
        var visited = new HashSet<string>();
        var current = FindById(config, groupId)?.ParentId;
        var depth = 0;

        while (!string.IsNullOrWhiteSpace(current) && depth++ < 64)
        {
            if (current == ancestorId)
            {
                return true;
            }

            if (!visited.Add(current))
            {
                return false;
            }

            current = FindById(config, current)?.ParentId;
        }

        return false;
    }

    private static bool RemoveDuplicateGroupIds(Configuration config)
    {
        var seen = new HashSet<string>();
        var changed = false;

        for (var i = config.Groups.Count - 1; i >= 0; i--)
        {
            if (seen.Add(config.Groups[i].Id))
            {
                continue;
            }

            config.Groups.RemoveAt(i);
            changed = true;
        }

        return changed;
    }

    private static bool FixInvalidParentIds(Configuration config)
    {
        var changed = false;

        foreach (var group in config.Groups)
        {
            if (string.IsNullOrWhiteSpace(group.ParentId))
            {
                continue;
            }

            if (group.ParentId == group.Id || FindById(config, group.ParentId) == null)
            {
                group.ParentId = null;
                changed = true;
            }
        }

        return changed;
    }

    private static bool BreakParentCycles(Configuration config)
    {
        var changed = false;

        foreach (var group in config.Groups)
        {
            if (string.IsNullOrWhiteSpace(group.ParentId))
            {
                continue;
            }

            var visited = new HashSet<string> { group.Id };
            var current = FindById(config, group.ParentId);
            while (current != null && !string.IsNullOrWhiteSpace(current.ParentId))
            {
                if (!visited.Add(current.Id))
                {
                    group.ParentId = null;
                    changed = true;
                    break;
                }

                current = FindById(config, current.ParentId);
            }
        }

        return changed;
    }

    private static bool CleanupSoundToGroupReferences(Configuration config)
    {
        var validIds = config.Groups.Select(g => g.Id).ToHashSet();
        var changed = false;

        foreach (var key in config.SoundToGroup.Keys.ToList())
        {
            if (!validIds.Contains(config.SoundToGroup[key]))
            {
                config.SoundToGroup.Remove(key);
                changed = true;
            }
        }

        return changed;
    }
}
