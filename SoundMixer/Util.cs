using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SoundMixer;

internal static class Util
{
    internal static bool TryScanText(this Dalamud.Plugin.Services.ISigScanner scanner, string sig, out nint result)
    {
        result = nint.Zero;
        try
        {
            result = scanner.ScanText(sig);
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    internal static bool TryResolveCallTarget(
        this Dalamud.Plugin.Services.ISigScanner scanner,
        string callSignature,
        out nint target
    )
    {
        target = nint.Zero;
        if (!scanner.TryScanText(callSignature, out var callSite))
        {
            return false;
        }

        if (Marshal.ReadByte(callSite) != 0xE8)
        {
            return false;
        }

        var relative = Marshal.ReadInt32(callSite + 1);
        target = callSite + 5 + relative;
        return target != nint.Zero;
    }

    internal static nint GetVirtualFunctionAddress(nint obj, int index)
    {
        var vtable = Marshal.ReadIntPtr(obj);
        return Marshal.ReadIntPtr(vtable + (index * IntPtr.Size));
    }

    private static unsafe byte[] ReadTerminatedBytes(byte* ptr)
    {
        if (ptr == null)
        {
            return [];
        }

        var bytes = new List<byte>();
        while (*ptr != 0)
        {
            bytes.Add(*ptr);
            ptr += 1;
        }

        return [.. bytes];
    }

    internal static unsafe string ReadTerminatedString(byte* ptr)
    {
        return Encoding.UTF8.GetString(ReadTerminatedBytes(ptr));
    }
}
