using System.Runtime.InteropServices;

namespace Lumina.Data.Structs.SqPack;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public unsafe struct SqPackHeader
{
    public ulong Magic;
    public PlatformType Platform;
    public uint Size;
    public uint Version;
    public uint Type;
}
