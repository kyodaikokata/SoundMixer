using System.Runtime.InteropServices;

namespace SoundMixer.GameTypes;

[StructLayout(LayoutKind.Explicit)]
public struct GetResourceParameters
{
    [FieldOffset(16)]
    public uint SegmentOffset;

    [FieldOffset(20)]
    public uint SegmentLength;

    public bool IsPartialRead => SegmentLength != 0;
}
