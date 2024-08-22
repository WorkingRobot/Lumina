namespace Lumina.Excel.Rows;

/// <summary>
/// Defines a row type/schema for an Excel sheet.
/// </summary>
/// <typeparam name="T">The type that implements the interface.</typeparam>
public interface IExcelRow< out T >
{
    /// <summary>Gets the raw row backing this row.</summary>
    RawExcelRow RawRow { get; }

    /// <summary>
    /// Creates an instance of the current type. Designed only for use within <see cref="Lumina"/>.
    /// </summary>
    /// <param name="row"></param>
    /// <returns>A newly created row object.</returns>
    abstract static T Create( in RawExcelRow row );
}