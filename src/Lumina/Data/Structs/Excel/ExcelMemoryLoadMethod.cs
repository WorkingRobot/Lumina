namespace Lumina.Data.Structs.Excel;

/// <summary>Decides how the game will store the loaded sheet in memory.</summary>
public enum ExcelMemoryLoadMethod : ushort
{
    /// <summary>The game will load the full sheet into memory.</summary>
    Full,

    /// <summary>The game will load a chunk at a time in a ring buffer.</summary>
    RingBuffer,
}