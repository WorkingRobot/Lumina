using DotNext.Collections.Generic;
using Lumina.Data.Structs.SqPack;
using Lumina.Extensions;
using Lumina.Misc;
using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Lumina.Data.RepositoryInfo;

namespace Lumina.Data;

public sealed class SqPackVfs( GameData gameData ) : IDisposable
{
    public GameData GameData { get; } = gameData;

    public DirectoryInfo Path => GameData.DataPath;

    public delegate bool ResolveRsfDelegate( ulong indexHash, out ReadOnlyMemory<byte> resolvedData );

    // (Expansion, Category) + Hash -> DataFile, ChunkIdx, Offset
    private FrozenDictionary<int, FrozenDictionary<int, SqPackValue>>? HashTable { get; set; }
    private FrozenDictionary<string, SqPackValue>? CollisionTable { get; set; }

    private ConcurrentDictionary<(SqPackKey Key, byte ChunkIdx, byte DatFile), MappedFile> OpenedFiles { get; set; } = [];

    public List<RepositoryInfo> Repositories { get; } = [];

    public async Task Initialize()
    {
        var sw = Stopwatch.StartNew();

        var tasks = new List<Task<RepositoryData>>();
        foreach( var dir in Path.GetDirectories() )
            tasks.Add( AddRepository( dir ) );

        var hashTable = new Dictionary<int, FrozenDictionary<int, SqPackValue>>();
        var synonymData = new List<Dictionary<string, SqPackValue>>();
        while( tasks.Count != 0 )
        {
            var finishedTask = await Task.WhenAny( tasks ).ConfigureAwait( false );
            tasks.Remove( finishedTask );
            var (expansionId, hashTables, synonyms) = finishedTask.Result;

            foreach( var (category, table) in hashTables )
                hashTable.Add( (int)new SqPackKey( expansionId, category ), table );
            synonymData.AddRange( synonyms );
        }

        HashTable = hashTable.ToFrozenDictionary();
        CollisionTable = synonymData.SelectMany( x => x ).ToFrozenDictionary();

        sw.Stop();
        GameData.Options.Logger?.Information( $"Initialized {Repositories.Count} repositories in {sw.Elapsed.TotalMilliseconds:0.00}ms" );
    }

    private readonly record struct RepositoryData( byte ExpansionId, Dictionary<CategoryType, FrozenDictionary<int, SqPackValue>> HashTables, List<Dictionary<string, SqPackValue>> Synonyms);

    private async Task<RepositoryData> AddRepository( DirectoryInfo repositoryPath )
    {
        var repo = new RepositoryInfo( repositoryPath );
        Repositories.Add( repo );

        var hashTable = new Dictionary<CategoryType, Dictionary<int, SqPackValue>>();
        var synonymData = new List<Dictionary<string, SqPackValue>>();

        var taskOrder = new Dictionary<CategoryType, Task<IndexData>>();
        foreach( var indexFile in repo.GetIndexFiles( GameData.Options.CurrentPlatform ) )
        {
            var key = indexFile.Category;
            var dict = hashTable.GetOrAdd( key );
            if( taskOrder.TryGetValue( key, out var dependentTask ) )
                taskOrder[key] = dependentTask.ContinueWith( _ => Task.Run( () => AddIndex( indexFile, dict ) ) ).Unwrap();
            else
                taskOrder[key] = Task.Run( () => AddIndex( indexFile, dict ) );
        }
        var tasks = taskOrder.Values.ToList();

        while( tasks.Count != 0 )
        {
            var finishedTask = await Task.WhenAny( tasks ).ConfigureAwait( false );
            tasks.Remove( finishedTask );
            var (_, synonyms, _) = finishedTask.Result;

            if( synonyms.Count != 0 )
                synonymData.Add( synonyms );
        }

        return new( repo.ExpansionId, hashTable.ToDictionary( x => x.Key, x => x.Value.ToFrozenDictionary() ), synonymData );
    }

    private readonly record struct IndexData( Dictionary<int, SqPackValue> Entries, Dictionary<string, SqPackValue> Synonyms, IndexFileInfo IndexFile );

    private static IndexData AddIndex( IndexFileInfo indexFile, Dictionary<int, SqPackValue> entryTable )
    {
        using var index = new MappedFile( indexFile.File );
        index.Prefetch( 0, index.Size );

        ref readonly var fileHeader = ref index.GetAt<SqPackHeader>( 0 );

        ref readonly var header = ref index.GetAt<SqPackIndexHeader>( fileHeader.Size );

        // It can be added trivially, but there's no reason to do so since index2 already exists everywhere.
        if( header.IndexType == 0 )
        {
            var entries = index.GetByteSpanAt<SqPackIndexTablePair<ulong>>( header.IndexData.Offset, header.IndexData.Size );
            throw new NotSupportedException( "Index type 0 is not supported." );
        }
        else if( header.IndexType == 2 )
        {
            var entries = index.GetByteSpanAt<SqPackIndexTablePair<uint>>( header.IndexData.Offset, header.IndexData.Size );
            foreach( var entry in entries.Span )
                entryTable.Add( unchecked((int)entry.Hash), new( entry.Data, indexFile.ChunkIndex ) );

            var synonyms = index.GetByteSpanAt<SqPackIndexSynonymEntry<uint>>( header.SynonymData.Offset, header.SynonymData.Size );
            var synonymTable = new Dictionary<string, SqPackValue>( synonyms.Length );
            foreach( var entry in synonyms.Span )
            {
                if( entry.Hash == uint.MaxValue )
                    break;

                entryTable.Remove( unchecked((int)entry.Hash) );

                var path = entry.Path;
                if( string.IsNullOrWhiteSpace( path ) )
                    throw new InvalidDataException( "Synonym entry has no path." );

                synonymTable.Add( path, new( entry.Data, indexFile.ChunkIndex ) );
            }

            return new( entryTable, synonymTable, indexFile );
        }
        else
            throw new InvalidDataException( $"Unknown index type: {header.IndexType}" );
    }

    internal readonly record struct SqPackKey( byte ExpansionId, CategoryType Category )
    {
        public SqPackKey( IndexFileInfo indexFile ) :
            this(
                indexFile.ExpansionId,
                indexFile.Category
            )
        {
        }

        public static explicit operator int(SqPackKey key) =>
            ( key.ExpansionId << 8 ) | (byte)key.Category;

        public static explicit operator SqPackKey(int key) =>
            new( (byte)( key >> 8 ), (CategoryType)( key & 0xFF ) );
    }

    private readonly record struct SqPackValue( SqPackIndexTableEntry Data, byte ChunkIdx );

    private MappedFile GetMappedFile( SqPackKey key, byte chunkIdx, byte datFile ) =>
        OpenedFiles.GetOrAdd( (key, chunkIdx, datFile), _ => {
            var directory = Repositories.First( x => x.ExpansionId == key.ExpansionId ).Path;
            var baseName = $"{(byte)key.Category:x02}{key.ExpansionId:x02}{chunkIdx:x02}.{GameData.Options.CurrentPlatform.GetName()}.dat{datFile}";
            
            return new( System.IO.Path.Combine( directory.FullName, baseName ) );
        } );

    public bool ContainsFile( string path ) =>
        ContainsFile( HashedFilePath.Create( path, out _ ), path );

    public bool ContainsFile( in HashedFilePath hashedPath, string path )
    {
        if( HashTable is not { } hashTable || CollisionTable is not { } collisionTable )
            throw new InvalidOperationException( "VFS is not initialized." );

        if( hashTable.TryGetValue( (int)hashedPath.PackKey, out var entries ) )
        {
            if( entries.ContainsKey( unchecked((int)hashedPath.Hash) ) )
                return true;
        }

        return collisionTable.ContainsKey( path );
    }

    private SqPackValue? TryGetFileInfo( in HashedFilePath hashedPath, string path )
    {
        if( HashTable is not { } hashTable || CollisionTable is not { } collisionTable )
            throw new InvalidOperationException( "VFS is not initialized." );

        if( hashTable.TryGetValue( (int)hashedPath.PackKey, out var entries ) )
        {
            if( entries.TryGetValue( unchecked((int)hashedPath.Hash), out var entry ) )
                return entry;
        }

        if( collisionTable.TryGetValue( path, out var collisionEntry ) )
            return collisionEntry;

        return null;
    }

    public SqPackStream? TryGetFileStream( string path ) =>
        TryGetFileStream( HashedFilePath.Create(path, out _), path );

    public SqPackStream? TryGetFileStream( in HashedFilePath hashedPath, string path )
    {
        if( TryGetFileInfo( in hashedPath, path ) is not { } value )
            return null;

        var file = GetMappedFile( hashedPath.PackKey, value.ChunkIdx, value.Data.DataFileIdx );
        return SqPackStream.Create( file, value.Data.Offset, GameData, hashedPath.OldHash );
    }

    public SqPackStream GetFileStream( string path ) =>
        GetFileStream( HashedFilePath.Create( path, out _ ), path );

    public SqPackStream GetFileStream( in HashedFilePath hashedPath, string path )
    {
        if( TryGetFileInfo( in hashedPath, path ) is not { } value )
            throw new FileNotFoundException( "File not found", path );

        var file = GetMappedFile( hashedPath.PackKey, value.ChunkIdx, value.Data.DataFileIdx );
        return SqPackStream.Create(file, value.Data.Offset, GameData, hashedPath.OldHash );
    }

    public void Dispose()
    {
        OpenedFiles.ForEach( f => f.Value.Dispose() );
        OpenedFiles.Clear();
    }
}
