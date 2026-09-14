using System;
using System.IO;

namespace Lumina.Data.Parsing.Tex
{
    // From DotSquish
    public static class Squish
    {
        public static int GetStorageRequirements( int width, int height, SquishOptions flags )
        {
            var blockCount = ( ( width + 3 ) / 4 ) * ( ( height + 3 ) / 4 );
            var blockSize = ( flags & SquishOptions.DXT1 ) != 0 ? 8 : 16;
            return blockCount * blockSize;
        }

        #region On buffer

        public static byte[] CompressBlock( byte[] rgba, SquishOptions flags )
        {
            return CompressBlockMasked( rgba, 0xFFFF, flags );
        }

        public static byte[] CompressBlockMasked( byte[] rgba, int mask, SquishOptions flags )
        {
            throw new NotImplementedException();
        }

        public static byte[] DecompressBlock( byte[] block, int blockOffset, SquishOptions flags )
        {
            var rgba = new byte[ 4 * 16 ];
            DecompressBlock( block, blockOffset, flags, rgba );
            return rgba;
        }

        private static void DecompressBlock( byte[] block, int blockOffset, SquishOptions flags, Span< byte > rgba )
        {
            // Get the block locations
            var colOff = blockOffset;
            var alphaOff = blockOffset;
            if( ( flags & ( SquishOptions.DXT3 | SquishOptions.DXT5 ) ) != 0 )
                colOff += 8;

            // Decompress colour.
            ColourBlock.DecompressColour( block, colOff, ( flags & SquishOptions.DXT1 ) != 0, rgba );

            // Decompress alpha seperately if necessary.
            if( ( flags & SquishOptions.DXT3 ) != 0 )
                Alpha.DecompressAlphaDxt3( block, alphaOff, rgba, 0 );
            else if( ( flags & SquishOptions.DXT5 ) != 0 )
                Alpha.DecompressAlphaDxt5( block, alphaOff, rgba, 0 );
        }

        public static byte[] CompressImage( byte[] rgba, int width, int height, SquishOptions flags )
        {
            throw new NotImplementedException();
        }

        public static byte[] DecompressImage( byte[] blocks, int width, int height, SquishOptions flags )
        {
            return DecompressImage( blocks, 0, width, height, flags );
        }

        public static byte[] DecompressImage( byte[] blocks, int offset, int width, int height, SquishOptions flags )
        {
            var argb = new byte[ 4 * width * height ];
            DecompressImage( blocks, offset, width, height, flags, argb );
            return argb;
        }

        public static void DecompressImage( byte[] blocks, int offset, int width, int height, SquishOptions flags,
            Span< byte > argb )
        {
            var bytesPerBlock = ( flags & SquishOptions.DXT1 ) != 0 ? 8 : 16;

            Span< byte > targetRgba = stackalloc byte[ 4 * 16 ];
            var blockOffset = offset;
            // Loop over blocks.
            for( int y = 0; y < height; y += 4 )
            {
                for( int x = 0; x < width; x += 4 )
                {
                    // Decompress the block.
                    DecompressBlock( blocks, blockOffset, flags, targetRgba );

                    // Write the decompressed pixels to the correct image locations.
                    var sourcePixelOffset = 0;
                    for( int py = 0; py < 4; ++py )
                    {
                        for( int px = 0; px < 4; ++px )
                        {
                            // Get the target location.
                            var sx = x + px;
                            var sy = y + py;
                            if( sx < width && sy < height )
                            {
                                var targetPixelOffset = 4 * ( ( width * sy ) + sx );
                                // Copy the rgba value
                                argb[ targetPixelOffset + 0 ] = targetRgba[ sourcePixelOffset + 2 ];
                                argb[ targetPixelOffset + 1 ] = targetRgba[ sourcePixelOffset + 1 ];
                                argb[ targetPixelOffset + 2 ] = targetRgba[ sourcePixelOffset + 0 ];
                                argb[ targetPixelOffset + 3 ] = targetRgba[ sourcePixelOffset + 3 ];
                            }

                            sourcePixelOffset += 4;
                        }
                    }

                    // advance
                    blockOffset += bytesPerBlock;
                }
            }
        }

        #endregion

        #region On stream

        public static void CompressBlock( Stream input, Stream output, SquishOptions flags )
        {
            CompressBlockMasked( input, output, 0xFFFF, flags );
        }

        public static void CompressBlockMasked( Stream input, Stream output, int mask, SquishOptions flags )
        {
            throw new NotImplementedException();
        }

        public static void DecompressBlock( Stream input, Stream output, SquishOptions flags )
        {
            throw new NotImplementedException();
        }

        public static void CompressImage( Stream input, Stream output, int width, int height, SquishOptions flags )
        {
            throw new NotImplementedException();
        }

        public static void DecompressImage( Stream input, Stream output, int width, int height, SquishOptions flags )
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}