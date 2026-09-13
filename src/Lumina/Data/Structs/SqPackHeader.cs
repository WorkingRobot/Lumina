using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Lumina.Data.Structs
{
    [SuppressMessage( "ReSharper", "InconsistentNaming" )]
    public enum PlatformId : byte
    {
        Win32,
        PS3,
        PS4
    }
    
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public unsafe struct SqPackHeader
    {
        public fixed byte magic[8];
        public PlatformId platformId;
        public fixed byte __unknown[3];
        public uint size;
        public uint version;
        public uint type;
    }
}