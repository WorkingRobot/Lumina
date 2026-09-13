using System.Runtime.InteropServices;

namespace Lumina.Data.Structs.SqPack;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct SqPackIndexBufferDescriptor
{
    public uint Offset;
    public uint Size;
    public fixed byte Hash[0x40];
}
