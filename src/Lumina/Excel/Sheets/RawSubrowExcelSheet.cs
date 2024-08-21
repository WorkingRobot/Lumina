using System;
using System.Runtime.CompilerServices;
using Lumina.Data;
using Lumina.Data.Files.Excel;
using Lumina.Data.Structs.Excel;
using Lumina.Excel.Exceptions;

namespace Lumina.Excel.Sheets;

/// <summary>An Excel sheet of <see cref="ExcelVariant.Subrows"/> variant.</summary>
public class RawSubrowExcelSheet : RawExcelSheet, ISubrowExcelSheet
{
    /// <summary>Creates a new instance of <see cref="RawSubrowExcelSheet"/>.</summary>
    /// <param name="module">The <see cref="ExcelModule"/> to access sheet data from.</param>
    /// <param name="language">The language to use for this sheet.</param>
    /// <param name="headerFile">Instance of <see cref="ExcelHeaderFile"/> that defines this sheet.</param>
    /// <exception cref="UnsupportedLanguageException"><paramref name="headerFile"/> defined that the sheet does not support this language.</exception>
    public RawSubrowExcelSheet( ExcelModule module, Language language, ExcelHeaderFile headerFile )
        : base(
            module,
            language,
            headerFile.Header.Variant == ExcelVariant.Subrows
                ? headerFile
                : throw new NotSupportedException( $"Sheet is not of {nameof( ExcelVariant.Subrows )} variant." ) )
    {
        foreach( var f in RawRows )
            TotalSubrowCount += f.SubrowCount;
    }

    /// <inheritdoc/>
    public int TotalSubrowCount { get; }

    /// <inheritdoc/>
    public bool HasSubrow( uint rowId, ushort subrowId )
    {
        ref readonly var lookup = ref GetRawRowOrNullRef( rowId, out _ );
        return !Unsafe.IsNullRef( in lookup ) && subrowId < lookup.SubrowCount;
    }

    /// <inheritdoc/>
    public bool TryGetSubrowCount( uint rowId, out ushort subrowCount )
    {
        ref readonly var lookup = ref GetRawRowOrNullRef( rowId, out _ );
        if( Unsafe.IsNullRef( in lookup ) )
        {
            subrowCount = 0;
            return false;
        }

        subrowCount = lookup.SubrowCount;
        return true;
    }

    /// <inheritdoc/>
    public ushort GetSubrowCount( uint rowId )
    {
        ref readonly var lookup = ref GetRawRowOrNullRef( rowId, out _ );
        return Unsafe.IsNullRef( in lookup ) ? throw new ArgumentOutOfRangeException( nameof( rowId ), rowId, null ) : lookup.SubrowCount;
    }
}