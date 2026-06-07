using System.Numerics;

namespace SoundMixer;

internal static class GroupColorHelper
{
    internal static bool TryGetColor(uint argb, out Vector4 color)
    {
        if (argb == 0)
        {
            color = default;
            return false;
        }

        color = new Vector4(
            ((argb >> 16) & 0xFF) / 255f,
            ((argb >> 8) & 0xFF) / 255f,
            (argb & 0xFF) / 255f,
            ((argb >> 24) & 0xFF) / 255f
        );
        return true;
    }

    internal static Vector4 GetPickerColor(uint argb)
    {
        return TryGetColor(argb, out var color)
            ? color
            : new Vector4(0.45f, 0.72f, 1f, 1f);
    }

    internal static uint FromVector4(Vector4 color)
    {
        var a = (uint)Math.Clamp((int)(color.W * 255f + 0.5f), 1, 255);
        var r = (uint)Math.Clamp((int)(color.X * 255f + 0.5f), 0, 255);
        var g = (uint)Math.Clamp((int)(color.Y * 255f + 0.5f), 0, 255);
        var b = (uint)Math.Clamp((int)(color.Z * 255f + 0.5f), 0, 255);
        return (a << 24) | (r << 16) | (g << 8) | b;
    }

    internal static bool IsRootGroup(SoundGroup group)
    {
        return string.IsNullOrWhiteSpace(group.ParentId);
    }

    /// <summary>Root groups use LabelColorArgb; nested groups use OverrideColorArgb only.</summary>
    internal static bool TryGetDisplayColor(Configuration config, SoundGroup group, out Vector4 color)
    {
        var argb = IsRootGroup(group) ? group.LabelColorArgb : group.OverrideColorArgb;
        return TryGetColor(argb, out color);
    }

    internal static bool TryGetDisplayColorForGroupId(Configuration config, string? groupId, out Vector4 color)
    {
        var group = GroupHierarchy.FindById(config, groupId);
        if (group == null)
        {
            color = default;
            return false;
        }

        return TryGetDisplayColor(config, group, out color);
    }

    /// <summary>Sets OverrideColorArgb from the immediate parent's LabelColorArgb (color field).</summary>
    internal static void SyncOverrideColorFromParent(Configuration config, SoundGroup group)
    {
        if (string.IsNullOrWhiteSpace(group.ParentId))
        {
            return;
        }

        var parent = GroupHierarchy.FindById(config, group.ParentId);
        if (parent == null)
        {
            return;
        }

        group.OverrideColorArgb = parent.LabelColorArgb;
    }

    internal static void InheritOverrideColorFromParent(Configuration config, SoundGroup group)
    {
        SyncOverrideColorFromParent(config, group);
    }
}
