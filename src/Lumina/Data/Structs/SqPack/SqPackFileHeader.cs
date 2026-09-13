using System.Runtime.InteropServices;

namespace Lumina.Data.Structs.SqPack;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct SqPackFileHeader
{
    public uint HeaderSize;
    public SqPackFileType Type;
    public uint UncompressedFileSize;
}
