namespace PdfFormPublisher.Interfaces;

/// <summary>
/// Marks a model whose public properties can be read by PdfFormPublisher.
/// </summary>
/// <remarks>
/// Most users get this interface automatically by inheriting from <see cref="PdfForm"/> or
/// <see cref="TabularPdfForm"/>. Row models implement it through <see cref="IDataLine"/>.
/// </remarks>
public interface IPdfFormPublisher
{
}
