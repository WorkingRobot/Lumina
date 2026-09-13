using System.Runtime.InteropServices;

namespace Lumina.Data.Structs.SqPack;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct SqPackStandardBlockDescriptor
{
    public uint Offset;
    public ushort CompressedSize;
    public ushort UncompressedSize;
}
