namespace PdfFormPublisher.Interfaces;

/// <summary>
/// Represents one row of data in a tabular PDF form.
/// </summary>
/// <remarks>
/// Implement this interface on row models used by <see cref="TabularPdfForm.Items"/>.
/// Public properties on the row model are matched to row fields in the PDF.
/// </remarks>
public interface IDataLine : IPdfFormPublisher
{
    /// <summary>
    /// Whether this row should be skipped when line numbers are assigned.
    /// </summary>
    /// <remarks>
    /// This is useful for separator rows or intentionally blank rows that should not consume
    /// a line number.
    /// </remarks>
    bool SkipLineNumber { get; set; }
}
