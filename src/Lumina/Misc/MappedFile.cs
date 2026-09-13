using DotNext.IO.MemoryMappedFiles;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lumina.Misc;

public sealed unsafe partial class MappedFile : IDisposable
{
    public FileInfo File { get; }

    private MemoryMappedFile Map { get; }

    private MemoryMappedDirectAccessor Accessor { get; }

    private byte* Pointer => Accessor.Pointer;

    public long Size => Accessor.Size;

    public MappedFile( string path ) : this( new FileInfo( path ) )
    {

    }

    public MappedFile( FileInfo fileInfo )
    {
        File = fileInfo;

        Map = MemoryMappedFile.CreateFromFile( File.FullName, FileMode.Open, null, 0, MemoryMappedFileAccess.Read );

        Accessor = Map.CreateDirectAccessor( access: MemoryMappedFileAccess.Read );
    }

    public Segment<T> GetSpanAt<T>( long byteOffset, int length ) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegative( byteOffset );
        ArgumentOutOfRangeException.ThrowIfGreaterThan( byteOffset + ( length * sizeof( T ) ), Size );
        return new( this, byteOffset, length );
    }

    public Segment<T> GetByteSpanAt<T>( long byteOffset, long byteSize ) where T : unmanaged
    {
        return GetSpanAt<T>( byteOffset, checked((int)( byteSize / sizeof( T ) )) );
    }

    public ref readonly T GetAt<T>( long byteOffset ) where T : unmanaged
    {
        return ref MemoryMarshal.GetReference( GetSpanAt<T>( byteOffset, 1 ).Span );
    }

    public void Prefetch( long offset, long size )
    {
        if( OperatingSystem.IsWindows() )
        {
            MemoryRangeEntry* entries = stackalloc MemoryRangeEntry[1];
            entries[0] = new MemoryRangeEntry { VirtualAddress = Pointer + offset, NumberOfBytes = (nuint)size };
            if( !PrefetchVirtualMemory( Process.GetCurrentProcess().Handle, 1, entries, 0 ) )
                throw new Win32Exception();
        }
    }

    [StructLayout( LayoutKind.Sequential )]
    private struct MemoryRangeEntry
    {
        public void* VirtualAddress;
        public nuint NumberOfBytes;
    }

    [LibraryImport( "kernel32.dll", SetLastError = true )]
    [return: MarshalAs( UnmanagedType.Bool )]
    private static unsafe partial bool PrefetchVirtualMemory(
        IntPtr hProcess,
        UIntPtr numberOfEntries,
        MemoryRangeEntry* memoryRanges,
        uint flags );

    public void Dispose()
    {
        Accessor.Dispose();

        Map?.Dispose();
    }

    public readonly record struct Segment<T>( MappedFile File, long ByteOffset, int Length ) where T : unmanaged
    {
        public int ByteLength => Length * sizeof( T );

        public ReadOnlySpan<T> Span =>
            new( File.Pointer + ByteOffset, Length );

        public UnmanagedMemoryStream Stream =>
            new( File.Pointer + ByteOffset, ByteLength );

        public static implicit operator ReadOnlySpan<T>( Segment<T> segment )
        {
            return segment.Span;
        }
    }
}
