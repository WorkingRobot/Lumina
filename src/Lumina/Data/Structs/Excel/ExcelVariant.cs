namespace Lumina.Data.Structs.Excel;

/// <summary>Type of sheet.</summary>
public enum ExcelVariant : ushort
{
    /// <summary>Type of sheet is not known.</summary>
    Unknown,

    /// <summary>Sheet is two-dimensional, mapping one row from a single row ID.</summary>
    Default,

    /// <summary>Sheet is three-dimensional, mapping multiple subrows from a single row ID.</summary>
    Subrows,
}