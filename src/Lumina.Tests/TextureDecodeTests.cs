using System;
using Lumina.Data.Parsing.Tex;
using Xunit;

namespace Lumina.Tests;

// Claude wrote this one
public class TextureDecodeTests
{
    // a DXT1 block with distinct endpoints, so every index selects a different colour
    private static readonly byte[] Dxt1Block =
    [
        0x00, 0xF8, // c0 = pure red   (565)
        0x1F, 0x00, // c1 = pure blue  (565)
        0b11_10_01_00, 0b11_10_01_00, 0b11_10_01_00, 0b11_10_01_00,
    ];

    [Fact]
    public void DecodingIntoACallerBufferMatchesTheAllocatingOverload()
    {
        var expected = Squish.DecompressImage( Dxt1Block, 0, 4, 4, SquishOptions.DXT1 );

        var actual = new byte[4 * 4 * 4];
        Squish.DecompressImage( Dxt1Block, 0, 4, 4, SquishOptions.DXT1, actual );

        Assert.Equal( expected, actual );
    }

    // endpoints are pure red and pure blue, so output channel order is pinned independently
    // of the decoder: index 0 must be red, index 1 must be blue, in B8G8R8A8 byte order
    [Fact]
    public void EndpointsDecodeToTheExpectedChannels()
    {
        var pixels = Squish.DecompressImage( Dxt1Block, 0, 4, 4, SquishOptions.DXT1 );

        // pixel 0 selects c0 (red): B=0 G=0 R=255 A=255
        Assert.Equal( 0, pixels[ 0 ] );
        Assert.Equal( 0, pixels[ 1 ] );
        Assert.Equal( 255, pixels[ 2 ] );
        Assert.Equal( 255, pixels[ 3 ] );

        // pixel 1 selects c1 (blue): B=255 G=0 R=0 A=255
        Assert.Equal( 255, pixels[ 4 ] );
        Assert.Equal( 0, pixels[ 5 ] );
        Assert.Equal( 0, pixels[ 6 ] );
        Assert.Equal( 255, pixels[ 7 ] );

        // the two interpolants must differ from both endpoints and from each other
        var c2 = BitConverter.ToInt32( pixels, 8 );
        var c3 = BitConverter.ToInt32( pixels, 12 );
        Assert.NotEqual( c2, c3 );
        Assert.NotEqual( BitConverter.ToInt32( pixels, 0 ), c2 );
        Assert.NotEqual( BitConverter.ToInt32( pixels, 4 ), c3 );
    }

    [Fact]
    public void DecodingIsDeterministicAcrossRepeatedCalls()
    {
        var first = new byte[4 * 4 * 4];
        var second = new byte[4 * 4 * 4];

        Squish.DecompressImage( Dxt1Block, 0, 4, 4, SquishOptions.DXT1, first );
        Squish.DecompressImage( Dxt1Block, 0, 4, 4, SquishOptions.DXT1, second );

        Assert.Equal( first, second );
    }
}
