using System;
using System.IO;
using System.Runtime.CompilerServices;
using Lumina.Data.Attributes;
using Lumina.Data.Structs.Excel;
using Lumina.Extensions;
using Lumina.Misc;

namespace Lumina.Data.Files.Excel;

/// <summary>Represents an Excel sheet header file.</summary>
[FileExtension( ".exh" )]
public class ExcelHeaderFile : FileResource
{
    private Lazy< uint > _columnHashLazy = null!;

    /// <summary>Expected magic value for <see cref="ExcelHeaderHeader.Magic"/> in <see cref="string"/>.</summary>
    public const string Magic = "EXHF";

    /// <summary>Gets the file header.</summary>
    public ExcelHeaderHeader Header { get; private set; }

    /// <summary>Gets the column definitions.</summary>
    public ExcelColumnDefinition[] ColumnDefinitions { get; private set; } = [];

    /// <summary>Gets the pagination definitions.</summary>
    public ExcelDataPagination[] DataPages { get; private set; } = [];

    /// <summary>Gets the supported languages.</summary>
    public Language[] Languages { get; private set; } = [];

    /// <inheritdoc/>
    public override void LoadFile()
    {
        // exd data is always in big endian
        Reader.IsLittleEndian = false;

        Header = ExcelHeaderHeader.Read( Reader );

        if( Header.Magic != ExcelHeaderHeader.ExpectedMagic )
            throw new InvalidDataException( "fucked exh file :(((((" );

        ColumnDefinitions = new ExcelColumnDefinition[Header.ColumnCount];
        DataPages = new ExcelDataPagination[Header.PageCount];
        for( var i = 0; i < Header.ColumnCount; i++ ) ColumnDefinitions[ i ] = ExcelColumnDefinition.Read( Reader );
        for( var i = 0; i < Header.PageCount; i++ ) DataPages[ i ] = ExcelDataPagination.Read( Reader );

        Languages = new Language[ Header.LanguageCount ];

        for( var i = 0; i < Header.LanguageCount; i++ )
        {
            Languages[ i ] = (Language)Reader.ReadByte();

            // optional parameter string (unused?)
            Reader.ReadStringData();
        }

        _columnHashLazy = new( () =>
            Crc32.Get( DataSpan.Slice( Unsafe.SizeOf< ExcelHeaderHeader >(), Unsafe.SizeOf< ExcelColumnDefinition >() * Header.ColumnCount ) ) );
    }

    /// <summary>Calculates the hash of column definitions.</summary>
    /// <returns>Calculated hash of column definitions.</returns>
    /// <remarks>
    /// Column hash is calculated using <see cref="FileResource.DataSpan"/>, which does not change anything w.r.t. system endianness.
    /// Calculation using <see cref="ColumnDefinitions"/>, which contains values transformed for the system endianness, will result in an unusable value.
    /// </remarks>
    public uint GetColumnsHash() => _columnHashLazy.Value;

    /// <summary>Gets <see cref="GetColumnsHash"/> in a hex string form.</summary>
    /// <returns>Hex string representation of <see cref="GetColumnsHash"/>.</returns>
    public string GetColumnsHashString() => GetColumnsHash().ToString( "x8" );
}