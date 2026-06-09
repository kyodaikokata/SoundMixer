using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Sound;

namespace SoundMixer;

/// <summary>
/// Validates game-process memory before dereferencing SoundData / ISoundData pointers.
/// Prevents crashes when lists contain freed or corrupt nodes during zone loads and teardown.
/// </summary>
internal static unsafe class SoundDataSafety
{
    private const int MinSoundDataReadableBytes = 0x80;
    /// <summary>Covers SoundData.Volume (0x60) through VolumeCategory (0xB4) for boost writes.</summary>
    private const int MinSoundDataVolumeFieldBytes = 0xB8;
    private static readonly int PointerSize = IntPtr.Size;
    private const uint MemCommit = 0x1000;
    private const uint PageNoAccess = 0x01;
    private const uint PageGuard = 0x100;

    internal delegate void SoundDataVisitor(SoundData* soundData);

    internal delegate bool SoundDataVisitorWithBreak(SoundData* soundData);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nuint VirtualQuery(nint lpAddress, out MemoryBasicInformation lpBuffer, nuint dwLength);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryBasicInformation
    {
        public nint BaseAddress;
        public nint AllocationBase;
        public uint AllocationProtect;
        public nuint RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    internal static bool IsValidForHook(SoundData* soundData)
    {
        return TryReadSoundData(soundData, out _, out _, out _);
    }

    internal static bool IsValidForVolumeWrite(SoundData* soundData)
    {
        return TryReadSoundData(soundData, out var isActive, out _, out _) && isActive;
    }

    internal static bool IsValidForExtendedVolumeWrite(SoundData* soundData)
    {
        if (soundData == null)
        {
            return false;
        }

        return IsReadable((nint)soundData, MinSoundDataVolumeFieldBytes)
            && TryReadSoundData(soundData, out var isActive, out _, out _)
            && isActive;
    }

    internal static bool IsReadablePointer(nint address, int size = MinSoundDataReadableBytes)
    {
        return IsReadable(address, size);
    }

    internal static bool IsReadable(nint address, int size = MinSoundDataReadableBytes)
    {
        if (address == 0 || size <= 0)
        {
            return false;
        }

        if (address % PointerSize != 0)
        {
            return false;
        }

        var remaining = size;
        var current = address;

        while (remaining > 0)
        {
            if (VirtualQuery(current, out var info, (nuint)Marshal.SizeOf<MemoryBasicInformation>()) == 0)
            {
                return false;
            }

            if (info.State != MemCommit)
            {
                return false;
            }

            if ((info.Protect & PageNoAccess) != 0 || (info.Protect & PageGuard) != 0)
            {
                return false;
            }

            var regionEnd = info.BaseAddress + (nint)(long)info.RegionSize;
            var readableInRegion = (int)(regionEnd - current);
            if (readableInRegion <= 0)
            {
                return false;
            }

            var chunk = Math.Min(remaining, readableInRegion);
            remaining -= chunk;
            current += chunk;
        }

        return true;
    }

    internal static bool TryReadSoundData(
        SoundData* soundData,
        out bool isActive,
        out uint soundNumber,
        out float volume
    )
    {
        isActive = false;
        soundNumber = 0;
        volume = 0f;

        if (soundData == null)
        {
            return false;
        }

        var ptr = (nint)soundData;
        if (!IsReadable(ptr))
        {
            return false;
        }

        try
        {
            isActive = soundData->IsActive;
            soundNumber = soundData->SoundNumber;
            volume = soundData->Volume;
            return true;
        }
        catch (Exception ex)
        {
            Services.PluginLog.Verbose(ex, "SoundMixer: failed to read SoundData fields");
            return false;
        }
    }

    internal static bool TryGetNext(ISoundData* node, out ISoundData* next)
    {
        next = null;
        if (node == null)
        {
            return false;
        }

        var nodePtr = (nint)node;
        if (!IsReadable(nodePtr, PointerSize * 4))
        {
            return false;
        }

        try
        {
            next = node->Next;
        }
        catch (Exception ex)
        {
            Services.PluginLog.Verbose(ex, "SoundMixer: failed to read ISoundData.Next");
            return false;
        }

        if (next == null)
        {
            return true;
        }

        var nextPtr = (nint)next;
        if (!IsReadable(nextPtr))
        {
            next = null;
            return false;
        }

        return true;
    }

    internal static void VisitSoundList(
        SoundData* listHead,
        SoundDataVisitor visitor,
        int maxNodes = 4096,
        string? listName = null
    )
    {
        VisitSoundList(listHead, soundData =>
        {
            visitor(soundData);
            return true;
        }, maxNodes, listName);
    }

    internal static void VisitSoundList(
        SoundData* listHead,
        SoundDataVisitorWithBreak visitor,
        int maxNodes = 4096,
        string? listName = null
    )
    {
        if (listHead == null)
        {
            return;
        }

        if (!IsReadable((nint)listHead))
        {
            return;
        }

        var visited = new HashSet<nint>();
        var count = 0;
        ISoundData* node = (ISoundData*)listHead;

        while (node != null && count < maxNodes)
        {
            var nodePtr = (nint)node;
            if (!visited.Add(nodePtr))
            {
                Services.PluginLog.Warning(
                    $"SoundMixer: cycle detected in sound list ({listName ?? "unknown"})"
                );
                break;
            }

            if (!IsReadable(nodePtr))
            {
                break;
            }

            try
            {
                if (!visitor((SoundData*)node))
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                Services.PluginLog.Verbose(ex, "SoundMixer: skipped invalid SoundData node");
            }

            count++;
            if (!TryGetNext(node, out var next))
            {
                break;
            }

            node = next;
        }

        if (count >= maxNodes)
        {
            Services.PluginLog.Warning(
                $"SoundMixer: sound list visit hit node limit ({maxNodes}, {listName ?? "unknown"})"
            );
        }
    }
}
