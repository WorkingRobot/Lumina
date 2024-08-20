using Lumina.Data;
using Lumina.Data.Files.Excel;
using Lumina.Text.ReadOnly;
using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Lumina.Data.Structs.Excel;
using Lumina.Excel.Exceptions;
using Lumina.Excel.Rows;
using Lumina.Excel.Sheets;

namespace Lumina.Excel;

/// <summary>
/// Represents a module for working with Excel sheets for a <see cref="Lumina.GameData"/> instance.
/// </summary>
public class ExcelModule
{
    /// <summary>Sheets that are known to exist via <c>root.exl</c> file.</summary>
    private readonly FrozenDictionary< string, SheetSet > _definedSheets;

    /// <summary>Sheets that do not exist in <see cref="_definedSheets"/> but turned out existing.</summary>
    private readonly ConcurrentDictionary< string, SheetSet > _adhocSheets;

    /// <summary>Lookup table for <see cref="SheetAttribute"/>, as looking this up via reflection is costly.</summary>
    private readonly ConcurrentDictionary< Type, SheetAttribute? > _sheetAttributeCache = [];

    internal GameData GameData { get; }

    internal ResolveRsvDelegate? RsvResolver => GameData.Options.RsvResolver;

    /// <summary>
    /// A delegate provided by the user to resolve RSV strings.
    /// </summary>
    /// <param name="rsvString">The string to resolve. It is guaranteed that this string it begins with <c>_rsv_</c>.</param>
    /// <param name="resolvedString">The output resolved string.</param>
    /// <returns><see langword="true"/> if resolved and <paramref name="resolvedString"/> is written to and <see langword="false"/> otherwise.</returns>
    public delegate bool ResolveRsvDelegate( ReadOnlySeString rsvString, out ReadOnlySeString resolvedString );

    /// <summary>
    /// Get the names of all available sheets, parsed from root.exl.
    /// </summary>
    public IReadOnlyCollection< string > SheetNames { get; }

    /// <summary>
    /// Create a new ExcelModule. This will do all the initial discovery of sheets from the EXL but not load any sheets.
    /// </summary>
    /// <param name="gameData">The <see cref="Lumina.GameData"/> instance to load sheets from</param>
    /// <exception cref="FileNotFoundException">Thrown when the root.exl file cannot be found - make sure that a 0a dat is available.</exception>
    public ExcelModule( GameData gameData )
    {
        GameData = gameData;

        var files = GameData.GetFile< ExcelListFile >( "exd/root.exl" ) ??
            throw new FileNotFoundException( "Unable to load exd/root.exl!" );

        GameData.Logger?.Information( "got {ExltEntryCount} exlt entries", files.ExdMap.Count );

        _definedSheets =
            files.ExdMap
                .Select( x => gameData.GetFile< ExcelHeaderFile >( $"exd/{x.Key}.exh" ) )
                .Where( x => x is not null )
                .ToFrozenDictionary(
                    x => x!.FilePath.Path[ 4..^4 ],
                    x => {
                        var numLanguageSlots = x!.Languages.Prepend( Language.None ).Max( l => (int) l ) + 1;
                        return new SheetSet(
                            x,
                            new Lazy< RawExcelSheet >?[numLanguageSlots],
                            new ConcurrentDictionary< Type, Array >?[numLanguageSlots] );
                    },
                    StringComparer.InvariantCultureIgnoreCase );

        foreach( var (headerFile, sheets, rowCaches) in _definedSheets.Values )
        {
            foreach( var language in headerFile.Languages )
            {
                sheets[ (int) language ] = new( () => headerFile.Header.Variant switch
                {
                    ExcelVariant.Default => new RawExcelSheet( this, language, headerFile ),
                    ExcelVariant.Subrows => new RawSubrowExcelSheet( this, language, headerFile ),
                    var x => throw new NotSupportedException( $"Sheet variant {x} is not supported." ),
                } );
                rowCaches[ (int) language ] = gameData.Options.CacheReferenceTypeRowInstances ? new() : null;
            }
        }

        _adhocSheets = new( StringComparer.InvariantCultureIgnoreCase );
        SheetNames = [.. files.ExdMap.Keys];
    }

    /// <summary>Loads an <see cref="ExcelSheet{T}"/>.</summary>
    /// <param name="name">The requested explicit sheet name. Leave <see langword="null"/> to use <typeparamref name="T"/>'s sheet name.
    ///     Explicit names are necessary for quest/dungeon/cutscene sheets.</param>
    /// <param name="language">The requested sheet language. Leave <see langword="null"/> or empty to use the default language.</param>
    /// <returns>An Excel sheet corresponding to <typeparamref name="T"/>, <paramref name="language"/>, and <paramref name="name"/>
    /// that may be created anew or reused from a previous invocation of this method.</returns>
    /// <remarks/>
    /// <exception cref="NotSupportedException">Sheet was not a <see cref="ExcelVariant.Default"/>.</exception>
    public ExcelSheet< T > GetSheet< T >( string? name = null, Language? language = null ) where T : IExcelRow< T >
    {
        var attribute = GetSheetAttributes< T >();
        name ??= attribute?.Name ?? throw new SheetNameEmptyException( "Sheet name must be specified via parameter or sheet attributes.", nameof( name ) );
        if( GetRawSheetAndRowCache( name, language, out var rowCache ) is not { Variant: ExcelVariant.Default } rawSheet )
            throw new NotSupportedException( $"Sheet \"{name}\" is not of {nameof( ExcelVariant.Default )} variant." );
        return new(
            rawSheet,
            GameData.Options.PanicOnSheetChecksumMismatch ? attribute?.ColumnHash : null,
            typeof( T ).IsValueType
                ? null
                : (object?[]) rowCache?.GetOrAdd( typeof( T ), static ( _, context ) => new object?[context], rawSheet.Count ) );
    }

    /// <summary>Loads a <see cref="SubrowExcelSheet{T}"/>.</summary>
    /// <param name="name">The requested explicit sheet name. Leave <see langword="null"/> to use <typeparamref name="T"/>'s sheet name.
    ///     Explicit names are necessary for quest/dungeon/cutscene sheets.</param>
    /// <param name="language">The requested sheet language. Leave <see langword="null"/> or empty to use the default language.</param>
    /// <returns>An Excel sheet corresponding to <typeparamref name="T"/>, <paramref name="language"/>, and <paramref name="name"/>
    /// that may be created anew or reused from a previous invocation of this method.</returns>
    /// <remarks/>
    /// <exception cref="NotSupportedException">Sheet was not a <see cref="ExcelVariant.Subrows"/>.</exception>
    public SubrowExcelSheet< T > GetSubrowSheet< T >( string? name = null, Language? language = null ) where T : IExcelRow< T >
    {
        var attribute = GetSheetAttributes< T >();
        name ??= attribute?.Name ?? throw new SheetNameEmptyException( "Sheet name must be specified via parameter or sheet attributes.", nameof( name ) );
        if( GetRawSheetAndRowCache( name, language, out var rowCache ) is not RawSubrowExcelSheet { Variant: ExcelVariant.Subrows } rawSheet )
            throw new NotSupportedException( $"Sheet \"{name}\" is not of {nameof( ExcelVariant.Subrows )} variant." );
        return new(
            rawSheet,
            GameData.Options.PanicOnSheetChecksumMismatch ? attribute?.ColumnHash : null,
            typeof( T ).IsValueType
                ? null
                : (object?[]?[]?) rowCache?.GetOrAdd( typeof( T ), static ( _, context ) => new object?[]?[context], rawSheet.Count ) );
    }

    /// <summary>Loads a <see cref="RawExcelSheet"/> that might also be a <see cref="RawSubrowExcelSheet"/>.</summary>
    /// <param name="name">The requested explicit sheet name.</param>
    /// <param name="language">The requested sheet language. Leave <see langword="null"/> or empty to use the default language.</param>
    /// <returns>An Excel sheet corresponding to <paramref name="name"/> and <paramref name="language"/>,
    /// that may be created anew or reused from a previous invocation of this method.</returns>
    /// <exception cref="SheetNotFoundException">Sheet does not exist.</exception>
    /// <exception cref="UnsupportedLanguageException">Sheet does not support <paramref name="language" /> nor <see cref="Language.None"/>.</exception>
    /// <exception cref="NotSupportedException">Sheet had an unsupported <see cref="ExcelVariant"/>.</exception>
    [RequiresDynamicCode( "Creating a generic sheet from a type requires reflection and dynamic code." )]
    [EditorBrowsable( EditorBrowsableState.Advanced )]
    public RawExcelSheet GetRawSheet( string name, Language? language = null ) => GetRawSheetAndRowCache( name, language, out _ );

    /// <summary>Unloads cached data that references an assembly.</summary>
    /// <param name="assembly">Assembly to look for in the cached sheets.</param>
    public void UnloadAssemblyTypedCache( Assembly assembly )
    {
        foreach( var k in _definedSheets.Values.Concat( _adhocSheets.Values ) )
        {
            foreach( var ri in k.RowCaches )
            {
                if( ri is null )
                    continue;

                foreach( var c in ri.Keys )
                {
                    if( c.Assembly == assembly )
                        _ = ri.TryRemove( c, out _ );
                }
            }
        }

        foreach( var c in _sheetAttributeCache.Keys )
        {
            if( c.Assembly == assembly )
                _ = _sheetAttributeCache.TryRemove( c, out _ );
        }
    }

    /// <summary>Gets the sheet attributes for <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">Type of the row.</typeparam>
    /// <returns>Sheet attributes, if any.</returns>
    internal SheetAttribute? GetSheetAttributes< T >() => GetSheetAttributes( typeof( T ) );

    /// <summary>Gets the sheet attributes for <paramref name="rowType"/>.</summary>
    /// <param name="rowType">Type of the row.</param>
    /// <returns>Sheet attributes, if any.</returns>
    internal SheetAttribute? GetSheetAttributes( Type rowType ) =>
        _sheetAttributeCache.GetOrAdd( rowType, static rowType => rowType.GetCustomAttribute< SheetAttribute >( false ) );

    private RawExcelSheet GetRawSheetAndRowCache( string name, Language? language, out ConcurrentDictionary< Type, Array >? rowCache )
    {
        ArgumentNullException.ThrowIfNull( name );
        var definedSheetSet = _definedSheets.GetValueRefOrNullRef( name );
        var sheetSet = Unsafe.IsNullRef( ref definedSheetSet )
            ? _adhocSheets.GetOrAdd( name, static ( key, context ) => {
                var headerFile = context.GameData.GetFile< ExcelHeaderFile >( $"exd/{key}.exh" )
                    ?? throw new SheetNotFoundException( null, nameof( key ) );
                var numLanguageSlots = headerFile.Languages.Prepend( Language.None ).Max( l => (int) l ) + 1;
                var sheets = new Lazy< RawExcelSheet >?[numLanguageSlots];
                var rowCaches = new ConcurrentDictionary< Type, Array >?[numLanguageSlots];
                foreach( var language in headerFile.Languages )
                {
                    sheets[ (int) language ] = new( () => headerFile.Header.Variant switch
                    {
                        ExcelVariant.Default => new RawExcelSheet( context, language, headerFile ),
                        ExcelVariant.Subrows => new RawSubrowExcelSheet( context, language, headerFile ),
                        var x => throw new NotSupportedException( $"Sheet variant {x} is not supported." ),
                    } );
                    rowCaches[ (int) language ] = context.GameData.Options.CacheReferenceTypeRowInstances ? new() : null;
                }

                return new( headerFile, sheets, rowCaches );
            }, this )
            : definedSheetSet;

        // Return the language-neutral sheet, if it exists, which also implies that language-specified sheets do not exist for this sheet.
        if( sheetSet.Sheets[ (int) Language.None ] is { } neutralLanguageSheet )
        {
            rowCache = sheetSet.RowCaches[ (int) Language.None ];
            return neutralLanguageSheet.Value;
        }

        var languageNumber = (int) ( language ?? GameData.Options.DefaultExcelLanguage );
        if( languageNumber < 0
           || languageNumber >= sheetSet.Sheets.Length
           || sheetSet.Sheets[ languageNumber ] is not { } languageSheet )
            throw new UnsupportedLanguageException( nameof( language ), language, null );
        rowCache = sheetSet.RowCaches[ languageNumber ];
        return languageSheet.Value;
    }

    private readonly record struct SheetSet(
        ExcelHeaderFile HeaderFile,
        Lazy< RawExcelSheet >?[] Sheets,
        ConcurrentDictionary< Type, Array >?[] RowCaches );
}