using System.IO;
using System.Runtime.CompilerServices;
using Lumina.Data.Attributes;
using Lumina.Data.Structs.Excel;

namespace Lumina.Data.Files.Excel;

/// <summary>Represents an Excel sheet data file.</summary>
[FileExtension( ".exd" )]
public class ExcelDataFile : FileResource
{
    /// <summary>Gets the header of this Excel data file.</summary>
    public ExcelDataHeader Header { get; protected set; }

    /// <summary>Gets the offsets to rows.</summary>
    public ExcelDataOffset[] RowOffsets { get; protected set; }= [];

    /// <inheritdoc/>
    public override void LoadFile()
    {
        // exd data is always in big endian
        Reader.IsLittleEndian = false;

        Header = ExcelDataHeader.Read( Reader );

        if( Header.Magic!= ExcelDataHeader.ExpectedMagic )
            throw new InvalidDataException( "fucked exd file :(((((" );

        // read offsets
        var offsetSize = Unsafe.SizeOf< ExcelDataOffset >();
        var count = Header.IndexSize / offsetSize;

        RowOffsets = new ExcelDataOffset[count];
        for( var i = 0; i < count; i++ )
            RowOffsets[i] = ExcelDataOffset.Read( Reader );
    }

    /// <summary>Gets the row header at the given offset.</summary>
    /// <param name="offset">Offset to retrieve from.</param>
    /// <returns>Row header.</returns>
    public ExcelDataRowHeader GetRowHeaderAt( uint offset ) => ExcelDataRowHeader.FromSpan( DataSpan[ (int)offset.. ] );
}