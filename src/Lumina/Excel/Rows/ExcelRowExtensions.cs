namespace Lumina.Excel.Rows;

/// <summary>Convenience methods for <see cref="IExcelRow{T}"/> and its implementors.</summary>
public static class ExcelRowExtensions
{
    /// <summary>Checks if a value typed row is empty, and return <see langword="null"/> or <paramref name="row"/> accordingly.</summary>
    /// <param name="row">Row to test.</param>
    /// <typeparam name="T">Type of row.</typeparam>
    /// <returns><see langword="null"/> if <see cref="RawExcelRow.IsEmpty"/> is <see langword="true"/>, or <paramref name="row"/> otherwise.</returns>
    public static T? AsNullable< T >( this T row ) where T : struct, IExcelRow< T > => row.RawRow.IsEmpty ? null : row;
}