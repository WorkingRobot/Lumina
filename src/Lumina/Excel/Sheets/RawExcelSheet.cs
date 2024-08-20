using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Lumina.Data;
using Lumina.Data.Files.Excel;
using Lumina.Data.Structs.Excel;
using Lumina.Excel.Exceptions;
using Lumina.Excel.Rows;
using Lumina.Extensions;

namespace Lumina.Excel.Sheets;

/// <summary>A wrapper around an Excel sheet.</summary>
public class RawExcelSheet : IExcelSheet
{
    /// <summary>Number of items in <see cref="_rowIndexLookupArray"/> that may resolve to no entry.</summary>
    // 7.05h: across 7292 sheets that exist and are referenced from exlt file, following ratio can be represented solely using lookup array of certain sizes.
    //  Max Gap, Coverage, Net Wasted
    //     1024,   99.15%,       38KB
    //     2048,   99.25%,       82KB
    //     3072,   99.29%,      109KB
    //     4096,   99.36%,      183KB
    //     5120,   99.40%,      239KB
    //     6144,   99.41%,      259KB
    //     9216,   99.42%,      295KB
    //    10240,   99.47%,      410KB
    //    14336,   99.48%,      463KB
    //    16384,   99.49%,      525KB
    //    19456,   99.51%,      599KB
    //    24576,   99.52%,      692KB
    //    26624,   99.53%,      793KB
    //    28672,   99.56%,     1011KB
    //    29696,   99.57%,     1127KB
    //    30720,   99.59%,     1244KB
    //    33792,   99.63%,     1633KB
    //    34816,   99.64%,     1765KB
    //    41984,   99.67%,     2089KB
    //    43008,   99.68%,     2255KB
    //    44032,   99.71%,     2594KB
    //    50176,   99.73%,     2789KB
    //    64512,   99.74%,     3041KB
    //    65536,   99.75%,     3293KB
    //    70656,   99.84%,     4941KB
    //    71680,   99.88%,     5773KB
    //    89088,   99.89%,     6118KB
    //   720896,   99.90%,     8934KB
    //   721920,   99.92%,    11754KB
    //  1049600,   99.93%,    15853KB
    //  1507328,   99.95%,    21741KB
    //  2001920,   99.96%,    29559KB
    //  2990080,   99.97%,    41236KB
    //  9832448,   99.99%,    79643KB
    // 10146816,  100.00%,   119276KB
    // We're allowing up to 65536 lookup items in _RawExcelRowTable, at cost of up to 3293KB of lookup items that resolve to nonexistence per language.
    private const int MaxUnusedLookupItemCount = 65536;

    private readonly ExcelPage[] _pages;
    private readonly RawExcelRow[] _rawExcelRows;
    private readonly ushort _subrowDataOffset;

    // RowLookup must use int as the key because it benefits from a fast path that removes indirections.
    // https://github.com/dotnet/runtime/blob/release/8.0/src/libraries/System.Collections.Immutable/src/System/Collections/Frozen/FrozenDictionary.cs#L140
    private readonly FrozenDictionary< int, int > _rowIndexLookupDict;

    private readonly int[] _rowIndexLookupArray;
    private readonly uint _rowIndexLookupArrayOffset;

    /// <inheritdoc/>
    public ExcelModule Module { get; }

    /// <inheritdoc/>
    public Language Language { get; }

    /// <inheritdoc/>
    public ExcelVariant Variant { get; }

    /// <inheritdoc/>
    public IReadOnlyList< ExcelColumnDefinition > Columns { get; }

    /// <inheritdoc/>
    public uint ColumnHash { get; }

    /// <summary>Creates a new instance of <see cref="RawExcelSheet"/>.</summary>
    /// <param name="module">The <see cref="ExcelModule"/> to access sheet data from.</param>
    /// <param name="language">The language to use for this sheet.</param>
    /// <param name="headerFile">Instance of <see cref="ExcelHeaderFile"/> that defines this sheet.</param>
    /// <exception cref="UnsupportedLanguageException"><paramref name="headerFile"/> defined that the sheet does not support this language.</exception>
    public RawExcelSheet( ExcelModule module, Language language, ExcelHeaderFile headerFile )
    {
        ArgumentNullException.ThrowIfNull( module );
        ArgumentNullException.ThrowIfNull( headerFile );

        var name = headerFile.FilePath.Path[ 4..^4 ]; // "exd/" ... ".exh"
        if( !headerFile.Languages.Contains( language ) )
            throw new UnsupportedLanguageException( nameof( language ), language, null );

        var hasSubrows = headerFile.Header.Variant == ExcelVariant.Subrows;

        Module = module;
        Language = headerFile.Languages.Contains( language ) ? language : Language.None;
        Variant = headerFile.Header.Variant;
        Columns = headerFile.ColumnDefinitions;
        ColumnHash = headerFile.GetColumnsHash();
        _subrowDataOffset = hasSubrows ? headerFile.Header.DataOffset : (ushort) 0;
        _pages = new ExcelPage[headerFile.DataPages.Length];
        _rawExcelRows = new RawExcelRow[headerFile.Header.RowCount];

        var i = 0;
        for( ushort pageIdx = 0; pageIdx < headerFile.DataPages.Length; pageIdx++ )
        {
            var pageDef = headerFile.DataPages[ pageIdx ];
            var filePath = Language == Language.None
                ? $"exd/{name}_{pageDef.StartId}.exd"
                : $"exd/{name}_{pageDef.StartId}_{LanguageUtil.GetLanguageStr( Language )}.exd";
            var fileData = module.GameData.GetFile< ExcelDataFile >( filePath );
            if( fileData == null )
                continue;

            var newPage = _pages[ pageIdx ] = new( Module, fileData.Data, headerFile.Header.DataOffset );

            // If row count information from exh file is incorrect, cope with it.
            if( i + fileData.RowData.Count > _rawExcelRows.Length )
                Array.Resize( ref _rawExcelRows, i + fileData.RowData.Count );

            foreach( var rowPtr in fileData.RowData.Values )
            {
                var subrowCount = hasSubrows ? newPage.ReadUInt16( rowPtr.Offset + 4 ) : (ushort) 1;
                var rowOffset = rowPtr.Offset + 6;
                _rawExcelRows[ i++ ] = new(
                    _pages[ pageIdx ],
                    rowPtr.RowId,
                    rowOffset,
                    language,
                    hasSubrows ? headerFile.Header.DataOffset : (ushort) 0,
                    subrowCount,
                    0 );
            }
        }

        // If row count information from exh file is incorrect, cope with it. (2)
        if( i != _rawExcelRows.Length )
            Array.Resize( ref _rawExcelRows, i );

        // A lot of sheets do not have large gap between row IDs. If total number of gaps is less than a threshold, then make a lookup array.
        if( _rawExcelRows.Length > 0 )
        {
            _rowIndexLookupArrayOffset = _rawExcelRows[ 0 ].RowId;
            var numSlots = _rawExcelRows[ ^1 ].RowId - _rowIndexLookupArrayOffset + 1;
            var numUnused = numSlots - headerFile.Header.RowCount;
            if( numUnused <= MaxUnusedLookupItemCount )
            {
                _rowIndexLookupArray = new int[numSlots];
                _rowIndexLookupArray.AsSpan().Fill( -1 );
                for( i = 0; i < _rawExcelRows.Length; i++ )
                    _rowIndexLookupArray[ _rawExcelRows[ i ].RowId - _rowIndexLookupArrayOffset ] = i;

                // All items can be looked up from _rowIndexLookupArray. Dictionary is unnecessary.
                _rowIndexLookupDict = FrozenDictionary< int, int >.Empty;
            }
            else
            {
                _rowIndexLookupArray = new int[MaxUnusedLookupItemCount];
                _rowIndexLookupArray.AsSpan().Fill( -1 );

                var lastLookupArrayRowId = uint.MaxValue;
                for( i = 0; i < _rawExcelRows.Length; i++ )
                {
                    var offsetRowId = _rawExcelRows[ i ].RowId - _rowIndexLookupArrayOffset;
                    if( offsetRowId >= MaxUnusedLookupItemCount )
                    {
                        // Discard the unused entries.
                        Array.Resize( ref _rowIndexLookupArray, unchecked( (int) ( lastLookupArrayRowId + 1 ) ) );
                        break;
                    }

                    _rowIndexLookupArray[ offsetRowId ] = i;
                    lastLookupArrayRowId = offsetRowId;
                }

                // Skip the items that can be looked up from _rowIndexLookupArray.
                _rowIndexLookupDict = _rawExcelRows.Skip( i ).ToFrozenDictionary( static row => (int) row.RowId, _ => i++ );
            }

            Count = _rawExcelRows.Length;
        }
        else
        {
            _rowIndexLookupDict = FrozenDictionary< int, int >.Empty;
            _rowIndexLookupArray = [];
            _rowIndexLookupArrayOffset = 0;
            _rawExcelRows = [];
            Count = 0;
        }
    }

    /// <inheritdoc/>
    public int Count { get; }

    /// <inheritdoc/>
    public ReadOnlySpan< RawExcelRow > OffsetLookupTable => _rawExcelRows;

    /// <inheritdoc/>
    public ushort GetColumnOffset( int columnIdx ) => Columns[ columnIdx ].Offset;

    /// <inheritdoc/>
    public bool HasRow( uint rowId )
    {
        ref readonly var lookup = ref GetRowLookupOrNullRef( rowId );
        return !Unsafe.IsNullRef( in lookup ) && lookup.SubrowCount > 0;
    }

    /// <summary>Gets a row lookup at the given index, if possible.</summary>
    /// <param name="rowId">Index of the desired row.</param>
    /// <returns>Lookup data for the desired row, or a null reference if no corresponding row exists.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
    internal ref readonly RawExcelRow GetRowLookupOrNullRef( uint rowId )
    {
        var lookupArrayIndex = unchecked( rowId - _rowIndexLookupArrayOffset );
        if( lookupArrayIndex < _rowIndexLookupArray.Length )
        {
            var rowIndex = _rowIndexLookupArray.UnsafeAt( (int) lookupArrayIndex );
            if( rowIndex == -1 )
                return ref Unsafe.NullRef< RawExcelRow >();
            return ref UnsafeGetRowLookupAt( rowIndex );
        }

        ref readonly var rowIndexRef = ref _rowIndexLookupDict.GetValueRefOrNullRef( (int) rowId );
        if( Unsafe.IsNullRef( in rowIndexRef ) )
            return ref Unsafe.NullRef< RawExcelRow >();
        return ref UnsafeGetRowLookupAt( rowIndexRef );
    }

    /// <summary>Gets a page at the given index, without checking for bounds or preconditions.</summary>
    /// <param name="pageIndex">Index of the desired page.</param>
    /// <returns>Page at the given index.</returns>
    internal ExcelPage UnsafeGetPageAt( int pageIndex ) => _pages.UnsafeAt( pageIndex );

    /// <summary>Gets a row lookup at the given index, without checking for bounds or preconditions.</summary>
    /// <param name="rowIndex">Index of the desired row.</param>
    /// <returns>Lookup data for the desired row.</returns>
    internal ref readonly RawExcelRow UnsafeGetRowLookupAt( int rowIndex ) =>
        ref _rawExcelRows.UnsafeAt( rowIndex );
}