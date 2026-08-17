using PdfFormPublisher.Attributes;
using PdfFormPublisher.Interfaces;
using iText.Forms;
using iText.Kernel.Pdf;

namespace PdfFormPublisher;

/// <summary>
/// Base class for filling one existing fillable PDF form from a C# model.
/// </summary>
/// <remarks>
/// Inherit from this class and add public properties whose names match the PDF field names.
/// Use <see cref="Attributes.FormFieldAttribute"/> when a property needs a different
/// PDF field name, a display format, or should be ignored.
/// </remarks>
public class PdfForm : IPdfFormPublisher, IPublish
{
    /// <summary>
    /// Creates a PDF form model that can publish with a template stream supplied at publish time.
    /// </summary>
    protected PdfForm()
    {
    }

    /// <summary>
    /// Creates a PDF form model for the PDF template at the supplied file path.
    /// </summary>
    /// <param name="filePath">The path to the existing PDF form template.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is blank.</exception>
    public PdfForm(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = filePath;
    }

    /// <summary>
    /// The path to the existing PDF form template used by path-based publishing.
    /// </summary>
    [FormField(false)]
    public string FilePath { get; protected set; } = string.Empty;

    /// <summary>
    /// Fills the PDF template with this model's public property values.
    /// </summary>
    /// <returns>The filled PDF as a byte array.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the template path is missing or the PDF does not contain an AcroForm.
    /// </exception>
    /// <exception cref="FileNotFoundException">Thrown when the PDF template file does not exist.</exception>
    public byte[] Publish()
    {
        var templateSource = PdfTemplateSource.FromFilePath(FilePath, nameof(FilePath));

        return Publish(templateSource);
    }

    /// <summary>
    /// Fills the file-path PDF template with this model's public property values and writes the result to a stream.
    /// </summary>
    /// <param name="outputStream">The writable stream that receives the filled PDF. The stream is left open.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="outputStream"/> is not writable.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="outputStream"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the template path is missing or the PDF does not contain an AcroForm.
    /// </exception>
    /// <exception cref="FileNotFoundException">Thrown when the PDF template file does not exist.</exception>
    public void PublishTo(Stream outputStream)
    {
        ValidateOutputStream(outputStream, nameof(outputStream));
        WriteToOutput(Publish(), outputStream);
    }

    /// <summary>
    /// Fills a PDF template stream with this model's public property values.
    /// </summary>
    /// <param name="templateStream">The readable stream containing the PDF template. The stream is left open.</param>
    /// <returns>The filled PDF as a byte array.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="templateStream"/> is not readable.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="templateStream"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the PDF does not contain an AcroForm.</exception>
    public byte[] Publish(Stream templateStream)
    {
        var templateSource = PdfTemplateSource.FromStream(templateStream, nameof(templateStream), "template stream");

        return Publish(templateSource);
    }

    /// <summary>
    /// Fills a PDF template stream with this model's public property values and writes the result to a stream.
    /// </summary>
    /// <param name="templateStream">The readable stream containing the PDF template. The stream is left open.</param>
    /// <param name="outputStream">The writable stream that receives the filled PDF. The stream is left open.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="templateStream"/> is not readable or <paramref name="outputStream"/> is not writable.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="templateStream"/> or <paramref name="outputStream"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the PDF does not contain an AcroForm.</exception>
    public void Publish(Stream templateStream, Stream outputStream)
    {
        ValidateOutputStream(outputStream, nameof(outputStream));
        WriteToOutput(Publish(templateStream), outputStream);
    }

    private byte[] Publish(PdfTemplateSource templateSource)
    {
        // Get property information of this form.
        var fields = this.GetFormFields().ToList();

        using (var ms = new MemoryStream())
        {
            using (var reader = templateSource.CreateReader())
            {
                using (var document = new PdfDocument(reader, new PdfWriter(ms)))
                {
                    var acroForm = PdfAcroForm.GetAcroForm(document, false)
                        ?? throw new InvalidOperationException($"The PDF template {templateSource.Description} does not contain an AcroForm.");

                    foreach (var field in fields)
                    {
                        field.SetField(acroForm);
                    }
                }
            }

            return ms.ToArray();
        }
    }

    private static void ValidateOutputStream(Stream outputStream, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(outputStream, parameterName);

        if (!outputStream.CanWrite)
        {
            throw new ArgumentException("Output stream must be writable.", parameterName);
        }
    }

    private static void WriteToOutput(byte[] pdfBytes, Stream outputStream)
    {
        outputStream.Write(pdfBytes, 0, pdfBytes.Length);
    }
}
