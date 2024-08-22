using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lumina.Data.Files.Excel;

namespace Lumina.Data.Structs.Excel;

/// <summary>Header of a <see cref="ExcelDataFile"/>.</summary>
[StructLayout( LayoutKind.Sequential )]
public struct ExcelDataHeader
{
    /// <summary>Expected value for <see cref="Magic"/>.</summary>
    public const uint ExpectedMagic = 0x45584446u; // 'EXDF'

    /// <summary>File magic value.</summary>
    public uint Magic;

    /// <summary>Version of the file.</summary>
    public ushort Version;

    /// <summary>Unknown value 1.</summary>
    private ushort _unknown1;

    /// <summary>Byte size of all <see cref="ExcelDataOffset"/>s contained within.</summary>
    public uint IndexSize;

    /// <summary>Byte size of all row data contained within.</summary>
    public uint DataSize;

    /// <summary>Unknown value 2.</summary>
    private ulong _unknown2;

    /// <summary>Unknown value 2.</summary>
    private ulong _unknown3;

    /// <summary>Creates a new instance of <see cref="ExcelDataHeader"/> from a binary-serialized form.</summary>
    /// <param name="reader">Binary reader to read from.</param>
    /// <returns>Read header data.</returns>
    public static ExcelDataHeader Read( LuminaBinaryReader reader ) => new()
    {
        Magic = reader.ReadUInt32(),
        Version = reader.ReadUInt16(),
        _unknown1 = reader.ReadUInt16(),
        IndexSize = reader.ReadUInt32(),
        DataSize = reader.ReadUInt32(),
        _unknown2 = reader.ReadUInt64(),
        _unknown3 = reader.ReadUInt64(),
    };

    /// <inheritdoc/>
    public override string ToString() => $"{nameof( ExcelDataHeader )}(v{Version} with {IndexSize / Unsafe.SizeOf< ExcelDataOffset >()} index items)";
}