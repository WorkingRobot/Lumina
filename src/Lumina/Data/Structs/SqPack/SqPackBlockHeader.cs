using System.Runtime.InteropServices;

namespace Lumina.Data.Structs.SqPack;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct SqPackBlockHeader
{
    public uint HeaderSize;
    public uint Unknown;
    public uint CompressedSize;
    public uint DataSize;
}
