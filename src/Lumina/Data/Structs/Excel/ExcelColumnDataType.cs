namespace Lumina.Data.Structs.Excel;

/// <summary>Type of column.</summary>
public enum ExcelColumnDataType : ushort
{
    /// <summary>Column contains a SeString.</summary>
    String = 0x0,

    /// <summary>Column contains a <see cref="bool"/> value.</summary>
    Bool = 0x1,

    /// <summary>Column contains a <see cref="sbyte"/> value.</summary>
    Int8 = 0x2,

    /// <summary>Column contains a <see cref="byte"/> value.</summary>
    UInt8 = 0x3,

    /// <summary>Column contains a <see cref="short"/> value.</summary>
    Int16 = 0x4,

    /// <summary>Column contains a <see cref="ushort"/> value.</summary>
    UInt16 = 0x5,

    /// <summary>Column contains a <see cref="int"/> value.</summary>
    Int32 = 0x6,

    /// <summary>Column contains a <see cref="uint"/> value.</summary>
    UInt32 = 0x7,

    /// <summary>Column contains a value that is unknown to Lumina (1).</summary>
    // unused?
    Unk = 0x8,

    /// <summary>Column contains a <see cref="float"/> value.</summary>
    Float32 = 0x9,

    /// <summary>Column contains a <see cref="long"/> value.</summary>
    Int64 = 0xA,

    /// <summary>Column contains a <see cref="ulong"/> value.</summary>
    UInt64 = 0xB,

    /// <summary>Column contains a value that is unknown to Lumina (2).</summary>
    // unused?
    Unk2 = 0xC,

    // 0 is read like data & 1, 1 is like data & 2, 2 = data & 4, etc...
    /// <summary>Column contains a <see cref="bool"/> value in a <see cref="byte"/> flag value at the least significant bit.</summary>
    PackedBool0 = 0x19,

    /// <summary>Column contains a <see cref="bool"/> value in a <see cref="byte"/> flag value at the second least significant bit.</summary>
    PackedBool1 = 0x1A,

    /// <summary>Column contains a <see cref="bool"/> value in a <see cref="byte"/> flag value at the third least significant bit.</summary>
    PackedBool2 = 0x1B,

    /// <summary>Column contains a <see cref="bool"/> value in a <see cref="byte"/> flag value at the fourth least significant bit.</summary>
    PackedBool3 = 0x1C,

    /// <summary>Column contains a <see cref="bool"/> value in a <see cref="byte"/> flag value at the fifth least significant bit.</summary>
    PackedBool4 = 0x1D,

    /// <summary>Column contains a <see cref="bool"/> value in a <see cref="byte"/> flag value at the sixth least significant bit.</summary>
    PackedBool5 = 0x1E,

    /// <summary>Column contains a <see cref="bool"/> value in a <see cref="byte"/> flag value at the seventh least significant bit.</summary>
    PackedBool6 = 0x1F,

    /// <summary>Column contains a <see cref="bool"/> value in a <see cref="byte"/> flag value at the most significant bit.</summary>
    PackedBool7 = 0x20,
}