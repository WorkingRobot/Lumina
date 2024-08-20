using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lumina.Data;
using Lumina.Data.Files.Excel;
using Lumina.Data.Structs.Excel;
using Lumina.Excel;
using Lumina.Excel.Rows;

namespace Lumina.Tests;

public class ExcelBenchmark
{
    private const int Iteration = 100000;

    [Sheet( "Addon" )]
    public readonly struct Addon( RawExcelRow row ) : IExcelRow< Addon >
    {
        public RawExcelRow RawRow => row;

        static Addon IExcelRow< Addon >.Create( in RawExcelRow row ) => new( row );
    }

    [Sheet( "Item" )]
    public readonly struct Item( RawExcelRow row ) : IExcelRow< Item >
    {
        public RawExcelRow RawRow => row;

        static Item IExcelRow< Item >.Create( in RawExcelRow row ) => new( row );
    }

    [Sheet( "QuestLinkMarker" )]
    public readonly struct QuestLinkMarker( RawExcelRow row ) : IExcelRow< QuestLinkMarker >
    {
        public RawExcelRow RawRow => row;

        static QuestLinkMarker IExcelRow< QuestLinkMarker >.Create( in RawExcelRow row ) => new( row );
    }

    [RequiresGameInstallationFact]
    public void TestAllSheets()
    {
        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
        var gameData = new GameData( @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack", new()
        {
            PanicOnSheetChecksumMismatch = false,
        } );

        var gaps = new List< uint >();
        foreach( var sheetName in gameData.Excel.SheetNames )
        {
            if( gameData.GetFile< ExcelHeaderFile >( $"exd/{sheetName}.exh" ) is not { } headerFile )
                continue;
            var lang = headerFile.Languages.Contains( Language.English ) ? Language.English : Language.None;
            switch( headerFile.Header.Variant )
            {
                case ExcelVariant.Default:
                {
                    var sheet = gameData.Excel.GetSheet< Addon >( sheetName, lang );
                    gaps.Add(
                        sheet.Count == 0 ? 0 : sheet.GetRowAt( sheet.Count - 1 ).RawRow.RowId - sheet.GetRowAt( 0 ).RawRow.RowId + 1 - (uint) sheet.Count );
                    break;
                }
                case ExcelVariant.Subrows:
                {
                    var sheet = gameData.Excel.GetSubrowSheet< QuestLinkMarker >( sheetName, lang );
                    gaps.Add( sheet.Count == 0 ? 0 : sheet.GetRowAt( sheet.Count - 1 ).RowId - sheet.GetRowAt( 0 ).RowId + 1 - (uint) sheet.Count );
                    break;
                }
            }
        }

        gaps.Sort();
        var countAcc = 0;
        var wasteAcc = 0;
        var test = string.Join(
            "\n",
            gaps
                .GroupBy( static x => x / 1024, static x => x )
                .Select( x =>
                    $"{( x.Key + 1 ) * 1024,8}, {( countAcc += x.Count() ) * 100f / gaps.Count,6:00.00}%, {( wasteAcc += x.Sum( static y => (int) y ) * 4 ) / 1024,5}KB" ) );
    }

    [RequiresGameInstallationFact]
    public void BenchmarkAddonRowAccessor()
    {
        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
        var gameData = new GameData( @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack" );

        var sheet = gameData.GetExcelSheet< Addon >() ?? throw new();
        var keys = sheet.Select( static x => x.RawRow.RowId ).ToArray();

        var k = Stopwatch.StartNew();
        for( var i = 0; i < Iteration; i++ )
        {
            foreach( var x in keys )
                _ = sheet[ x ];
        }

        k.Stop();
        throw new( $"Took {k.Elapsed.TotalMilliseconds / Iteration}ms" );
    }

    [RequiresGameInstallationFact]
    public void BenchmarkAddonRowEnumerator()
    {
        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
        var gameData = new GameData( @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack" );

        var sheet = gameData.GetExcelSheet< Addon >() ?? throw new();

        var k = Stopwatch.StartNew();
        for( var i = 0; i < Iteration; i++ )
        {
            foreach( var x in sheet )
                _ = x.RawRow.RowId;
        }

        k.Stop();
        throw new( $"Took {k.Elapsed.TotalMilliseconds / Iteration}ms" );
    }

    [RequiresGameInstallationFact]
    public void BenchmarkItemRowAccessor()
    {
        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
        var gameData = new GameData( @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack" );

        var sheet = gameData.GetExcelSheet< Item >() ?? throw new();
        var keys = sheet.Select( static x => x.RawRow.RowId ).ToArray();

        var k = Stopwatch.StartNew();
        for( var i = 0; i < Iteration; i++ )
        {
            foreach( var x in keys )
                _ = sheet[ x ];
        }

        k.Stop();
        throw new( $"Took {k.Elapsed.TotalMilliseconds / Iteration}ms" );
    }

    [RequiresGameInstallationFact]
    public void BenchmarkItemRowEnumerator()
    {
        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
        var gameData = new GameData( @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack" );

        var sheet = gameData.GetExcelSheet< Item >() ?? throw new();

        var k = Stopwatch.StartNew();
        for( var i = 0; i < Iteration; i++ )
        {
            foreach( var x in sheet )
                _ = x.RawRow.RowId;
        }

        k.Stop();
        throw new( $"Took {k.Elapsed.TotalMilliseconds / Iteration}ms" );
    }

    [RequiresGameInstallationFact]
    public void BenchmarkSubrowAccessor()
    {
        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
        var gameData = new GameData( @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack" );

        var sheet = gameData.GetSubrowExcelSheet< QuestLinkMarker >() ?? throw new();
        var keys = sheet.Select( static x => x.RowId ).ToArray();

        var k = Stopwatch.StartNew();
        for( var i = 0; i < Iteration; i++ )
        {
            foreach( var x in keys )
            {
                var sc = sheet.GetSubrowCount( x );
                for( ushort j = 0; j < sc; j++ )
                    _ = sheet[ x, j ];
            }
        }

        k.Stop();
        throw new( $"Took {k.Elapsed.TotalMilliseconds / Iteration}ms" );
    }

    [RequiresGameInstallationFact]
    public void BenchmarkSubrowEnumerator()
    {
        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
        var gameData = new GameData( @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack" );

        var sheet = gameData.GetSubrowExcelSheet< QuestLinkMarker >() ?? throw new();

        var k = Stopwatch.StartNew();
        for( var i = 0; i < Iteration; i++ )
        {
            foreach( var x in sheet.Flatten() )
                _ = x.RawRow.RowId;
        }

        k.Stop();
        throw new( $"Took {k.Elapsed.TotalMilliseconds / Iteration}ms" );
    }
}