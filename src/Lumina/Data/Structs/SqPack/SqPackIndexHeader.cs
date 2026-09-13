using System.Runtime.InteropServices;

namespace Lumina.Data.Structs.SqPack;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct SqPackIndexHeader
{
    public uint Size;
    public uint Version;
    public SqPackIndexBufferDescriptor IndexData;
    public uint DataFileCount;
    public SqPackIndexBufferDescriptor SynonymData;
    public SqPackIndexBufferDescriptor EmptyBlockData;
    public SqPackIndexBufferDescriptor DirIndexData;
    public uint IndexType;
    public fixed byte Reserved[0x290];
    public fixed byte SelfHash[0x40];
}
