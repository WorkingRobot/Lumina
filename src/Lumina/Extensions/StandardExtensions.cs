using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Lumina.Extensions;

#if NETSTANDARD

internal static class StandardExtensions
{
    public static int Read( this Stream stream, byte[] buffer )
    {
        return stream.Read( buffer, 0, buffer.Length );
    }

    public static void Write( this Stream stream, ReadOnlySpan<byte> buffer )
    {
        stream.Write( buffer.ToArray(), 0, buffer.Length );
    }

    public static unsafe int GetByteCount( this System.Text.Encoding encoding, ReadOnlySpan<char> chars )
    {
        fixed( char* ptr = chars )
            return encoding.GetByteCount( ptr, chars.Length );
    }

    public static unsafe int GetBytes( this System.Text.Encoding encoding, ReadOnlySpan<char> chars, Span<byte> bytes )
    {
        fixed( char* charPtr = chars )
        fixed( byte* bytePtr = bytes )
            return encoding.GetBytes( charPtr, chars.Length, bytePtr, bytes.Length );
    }

    public static unsafe string GetString( this System.Text.Encoding encoding, ReadOnlySpan<byte> bytes )
    {
        fixed( byte* ptr = bytes )
            return encoding.GetString( ptr, bytes.Length );
    }

    public static unsafe int GetCharCount( this System.Text.Encoding encoding, ReadOnlySpan<byte> bytes )
    {
        fixed( byte* ptr = bytes )
            return encoding.GetCharCount( ptr, bytes.Length );
    }

    public static unsafe int GetChars( this System.Text.Encoding encoding, ReadOnlySpan<byte> bytes, Span<char> chars )
    {
        fixed( byte* bytePtr = bytes )
        fixed( char* charPtr = chars )
            return encoding.GetChars( bytePtr, bytes.Length, charPtr, chars.Length );
    }

    public static T CreateDelegate<T>( this System.Reflection.MethodInfo methodInfo ) where T : Delegate
    {
        return (T)methodInfo.CreateDelegate( typeof( T ) );
    }

    public static void AddBytes( this System.HashCode hashCode, ReadOnlySpan<byte> bytes )
    {
        foreach( var b in bytes )
            hashCode.Add( b );
    }
}

internal static class EnumExt
{
    public static bool IsDefined<TEnum>(TEnum value) where TEnum : struct
    {
        return Enum.IsDefined( typeof( TEnum ), value );
    }
}

internal static class MathExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp(float value, float min, float max)
    {
        if (min > max)
        {
            throw new ArgumentException($"Min ({min}) is greater than Max ({max})");
        }

        if (value < min)
        {
            return min;
        }
        else if (value > max)
        {
            return max;
        }

        return value;
    }


    private const int ILogB_NaN = 0x7FFFFFFF;
    private const int ILogB_Zero = (-1 - 0x7FFFFFFF);

    public static int ILogB(double x)
        {
            // Implementation based on https://git.musl-libc.org/cgit/musl/tree/src/math/ilogb.c

            if (double.IsNaN(x))
            {
                return ILogB_NaN;
            }

            ulong i = BitConverterExt.DoubleToUInt64Bits(x);
            int e = (int)((i >> 52) & 0x7FF);

            if (e == 0)
            {
                i <<= 12;
                if (i == 0)
                {
                    return ILogB_Zero;
                }

                for (e = -0x3FF; (i >> 63) == 0; e--, i <<= 1) ;
                return e;
            }

            if (e == 0x7FF)
            {
                return (i << 12) != 0 ? ILogB_Zero : int.MaxValue;
            }

            return e - 0x3FF;
        }
}

internal static class BitConverterExt
{
    // Unsafe.BitCast
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    private static unsafe TTo BitCast<TFrom, TTo>( TFrom source )
            where TFrom : unmanaged
            where TTo : unmanaged
    {
        if( sizeof( TFrom ) != sizeof( TTo ) )
            throw new NotSupportedException();
        return Unsafe.ReadUnaligned<TTo>( ref Unsafe.As<TFrom, byte>( ref source ) );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static unsafe ulong DoubleToUInt64Bits(double value) => BitCast<double, ulong>(value);

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static unsafe float Int32BitsToSingle(int value) => BitCast<int, float>(value);

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static unsafe int SingleToInt32Bits(float value) => BitCast<float, int>(value);
}

internal static class MemoryMarshalExt
{
    public static unsafe ReadOnlySpan<byte> CreateReadOnlySpanFromNullTerminated( byte* value ) =>
            value != null ? new ReadOnlySpan<byte>( value, IndexOfNullByte( value ) ) :
            default;

    // https://github.com/dotnet/runtime/blob/c3e5ce97a29317d3dab98d09c310daf11a2260c7/src/libraries/System.Private.CoreLib/src/System/SpanHelpers.Byte.cs#L453
    private static unsafe int IndexOfNullByte( byte* searchSpace )
    {
        byte* currentByte = searchSpace;
        while( *currentByte != 0 )
            currentByte++;

        return (int)( currentByte - searchSpace );
    }
}

#endif
