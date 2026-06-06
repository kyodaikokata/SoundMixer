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

    internal static SoundGroup? GetRootGroup(Configuration config, string? groupId)
    {
        var current = GroupHierarchy.FindById(config, groupId);
        while (current != null && !IsRootGroup(current))
        {
            current = GroupHierarchy.FindById(config, current.ParentId);
        }

        return current;
    }

    internal static bool TryGetRootLabelColor(Configuration config, string? groupId, out Vector4 color)
    {
        var root = GetRootGroup(config, groupId);
        if (root == null)
        {
            color = default;
            return false;
        }

        return TryGetColor(root.LabelColorArgb, out color);
    }
}
