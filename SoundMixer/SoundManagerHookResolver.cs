using System.Reflection;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Sound;

namespace SoundMixer;

internal static class SoundManagerHookResolver
{
    internal static nint ResolveMemberFunctionAddress(string memberName)
    {
        var pointersType = typeof(SoundManager).GetNestedType(
            "MemberFunctionPointers",
            BindingFlags.Public | BindingFlags.Static
        );
        if (pointersType == null)
        {
            return nint.Zero;
        }

        var member = pointersType.GetMember(
            memberName,
            BindingFlags.Public | BindingFlags.Static
        ).FirstOrDefault();
        if (member == null)
        {
            return nint.Zero;
        }

        object? value = member switch
        {
            FieldInfo field => field.GetValue(null),
            PropertyInfo property => property.GetValue(null),
            _ => null,
        };

        return ConvertToNativeAddress(value);
    }

    private static nint ConvertToNativeAddress(object? value)
    {
        switch (value)
        {
            case null:
                return nint.Zero;
            case nint ptr:
                return ptr;
            case Delegate function:
                return Marshal.GetFunctionPointerForDelegate(function);
            default:
                break;
        }

        var valueProperty = value.GetType().GetProperty(
            "Value",
            BindingFlags.Public | BindingFlags.Instance
        );
        if (valueProperty?.GetValue(value) is nint nested)
        {
            return nested;
        }

        try
        {
            return (nint)Convert.ToInt64(value);
        }
        catch
        {
            return nint.Zero;
        }
    }
}
