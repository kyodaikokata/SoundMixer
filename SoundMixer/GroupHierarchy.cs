using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dalamud.Bindings.ImGui;

namespace SoundMixer;

internal static class GroupDragState
{
    internal static string? DraggingGroupId { get; set; }
}

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
        while (!string.IsNullOrWhiteSpace(current))
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

        group.ParentId = parentId;
        MoveNearParent(config, group);
        return true;
    }

    internal static void RemoveFromParent(Configuration config, string groupId)
    {
        var group = FindById(config, groupId);
        if (group == null)
        {
            return;
        }

        group.ParentId = null;
        MoveToRootEnd(config, group);
    }

    internal static void MoveBefore(Configuration config, string draggedId, string targetId)
    {
        if (draggedId == targetId)
        {
            return;
        }

        var dragged = FindById(config, draggedId);
        var target = FindById(config, targetId);
        if (dragged == null || target == null)
        {
            return;
        }

        dragged.ParentId = target.ParentId;
        Reinsert(config, draggedId, config.Groups.FindIndex(g => g.Id == targetId));
    }

    internal static void MoveAfter(Configuration config, string draggedId, string targetId)
    {
        if (draggedId == targetId)
        {
            return;
        }

        var dragged = FindById(config, draggedId);
        var target = FindById(config, targetId);
        if (dragged == null || target == null)
        {
            return;
        }

        dragged.ParentId = target.ParentId;
        var targetIndex = config.Groups.FindIndex(g => g.Id == targetId);
        Reinsert(config, draggedId, targetIndex + 1);
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
            child.ParentId = parentId;
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
            }
        }

        return group;
    }

    internal static bool HasMatchingDescendant(Configuration config, SoundGroup group, Func<string, bool> matches)
    {
        if (matches(group.Name))
        {
            return true;
        }

        return GetChildren(config, group.Id).Any(child => HasMatchingDescendant(config, child, matches));
    }

    internal static void BeginDragSource(string groupId, string displayName)
    {
        if (!ImGui.BeginDragDropSource())
        {
            return;
        }

        GroupDragState.DraggingGroupId = groupId;
        ImGui.SetDragDropPayload(DragDropType, Encoding.UTF8.GetBytes(groupId));
        ImGui.TextUnformatted($"移动: {displayName}");
        ImGui.EndDragDropSource();
    }

    internal static string? ReadDragPayload()
    {
        ImGui.AcceptDragDropPayload(DragDropType);
        return GroupDragState.DraggingGroupId;
    }

    internal static GroupDropIntent GetDropIntentForItem()
    {
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var mouseY = ImGui.GetMousePos().Y;
        var height = Math.Max(max.Y - min.Y, 1f);
        var relative = (mouseY - min.Y) / height;

        if (relative < 0.25f)
        {
            return GroupDropIntent.Before;
        }

        if (relative > 0.75f)
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
                MoveBefore(config, draggedId, targetId);
                return true;
            case GroupDropIntent.After when !string.IsNullOrWhiteSpace(targetId):
                MoveAfter(config, draggedId, targetId);
                return true;
            default:
                return false;
        }
    }

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
        var current = FindById(config, groupId)?.ParentId;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (current == ancestorId)
            {
                return true;
            }

            current = FindById(config, current)?.ParentId;
        }

        return false;
    }
}
