using PdfFormPublisher.Attributes;
using PdfFormPublisher.Interfaces;
using iText.Forms;
using iText.Kernel.Pdf;

namespace PdfFormPublisher;

/// <summary>
/// Base class for filling existing PDF forms that contain repeating rows.
/// </summary>
/// <remarks>
/// Inherit from this class for forms that have a first-page template and optional continuation
/// pages. Put row data in <see cref="Items"/> and configure row counts and optional template paths with
/// <see cref="TabularPdfFormSettings"/>.
/// </remarks>
public class TabularPdfForm : IPdfFormPublisher, IPublish
{
    /// <summary>
    /// Creates a tabular PDF form model with the PDF template and row settings to use.
    /// </summary>
    /// <param name="settings">The row counts and optional template paths used while publishing.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
    public TabularPdfForm(TabularPdfFormSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Settings = settings;
    }

    /// <summary>
    /// The row counts and optional template paths used while publishing.
    /// </summary>
    [FormField(false)]
    public TabularPdfFormSettings Settings { get; protected set; }

    /// <summary>
    /// The rows that should be written to the PDF form.
    /// </summary>
    /// <remarks>
    /// Each item represents one row. The item's public properties are matched to row field names
    /// in the PDF, such as <c>Description.0</c>, <c>Description.1</c>, and so on.
    /// </remarks>
    [FormField(false)]
    public IEnumerable<IDataLine>? Items { get; set; }

    /// <summary>
    /// Fills the configured PDF templates with the model values and row data.
    /// </summary>
    /// <returns>The filled PDF as a byte array.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required settings are missing, row counts are invalid, or a template does not contain an AcroForm.
    /// </exception>
    /// <exception cref="FileNotFoundException">Thrown when a required PDF template file does not exist.</exception>
    public byte[] Publish()
    {
        var itemFields = GetValidatedItemFields();
        ValidatePublishState(itemFields.Count);

        var firstPageTemplate = PdfTemplateSource.FromFilePath(Settings.FirstPageFilePath, nameof(Settings.FirstPageFilePath));
        PdfTemplateSource? continuationPageTemplate = null;

        if (RequiresContinuationTemplate(itemFields.Count))
        {
            continuationPageTemplate = PdfTemplateSource.FromFilePath(Settings.ContinuationPageFilePath, nameof(Settings.ContinuationPageFilePath));
        }

        return Publish(itemFields, firstPageTemplate, continuationPageTemplate);
    }

    /// <summary>
    /// Fills the configured file-path PDF templates and writes the result to a stream.
    /// </summary>
    /// <param name="outputStream">The writable stream that receives the filled PDF. The stream is left open.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="outputStream"/> is not writable.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="outputStream"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required settings are missing, row counts are invalid, or a template does not contain an AcroForm.
    /// </exception>
    /// <exception cref="FileNotFoundException">Thrown when a required PDF template file does not exist.</exception>
    public void PublishTo(Stream outputStream)
    {
        ValidateOutputStream(outputStream, nameof(outputStream));
        WriteToOutput(Publish(), outputStream);
    }

    /// <summary>
    /// Fills PDF template streams with the model values and row data.
    /// </summary>
    /// <param name="firstPageTemplateStream">The readable stream containing the first-page PDF template. The stream is left open.</param>
    /// <param name="continuationPageTemplateStream">
    /// The readable stream containing the continuation-page PDF template. Required when rows overflow the first page.
    /// The stream is left open.
    /// </param>
    /// <returns>The filled PDF as a byte array.</returns>
    /// <exception cref="ArgumentException">Thrown when a supplied template stream is not readable.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="firstPageTemplateStream"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required settings are missing, row counts are invalid, a continuation stream is required, or a template does not contain an AcroForm.
    /// </exception>
    public byte[] Publish(Stream firstPageTemplateStream, Stream? continuationPageTemplateStream = null)
    {
        var itemFields = GetValidatedItemFields();
        ValidatePublishState(itemFields.Count);

        var firstPageTemplate = PdfTemplateSource.FromStream(firstPageTemplateStream, nameof(firstPageTemplateStream), "first page template stream");
        PdfTemplateSource? continuationPageTemplate = null;

        if (RequiresContinuationTemplate(itemFields.Count))
        {
            if (continuationPageTemplateStream is null)
            {
                throw new InvalidOperationException($"{nameof(continuationPageTemplateStream)} must be provided when items exceed the first-page row count.");
            }

            continuationPageTemplate = PdfTemplateSource.FromStream(continuationPageTemplateStream, nameof(continuationPageTemplateStream), "continuation page template stream");
        }

        return Publish(itemFields, firstPageTemplate, continuationPageTemplate);
    }

    /// <summary>
    /// Fills PDF template streams with the model values and row data and writes the result to a stream.
    /// </summary>
    /// <param name="firstPageTemplateStream">The readable stream containing the first-page PDF template. The stream is left open.</param>
    /// <param name="continuationPageTemplateStream">
    /// The readable stream containing the continuation-page PDF template. Required when rows overflow the first page.
    /// The stream is left open.
    /// </param>
    /// <param name="outputStream">The writable stream that receives the filled PDF. The stream is left open.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when a supplied template stream is not readable or <paramref name="outputStream"/> is not writable.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="firstPageTemplateStream"/> or <paramref name="outputStream"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required settings are missing, row counts are invalid, a continuation stream is required, or a template does not contain an AcroForm.
    /// </exception>
    public void Publish(Stream firstPageTemplateStream, Stream? continuationPageTemplateStream, Stream outputStream)
    {
        ValidateOutputStream(outputStream, nameof(outputStream));
        WriteToOutput(Publish(firstPageTemplateStream, continuationPageTemplateStream), outputStream);
    }

    private byte[] Publish(List<DataLine> itemFields, PdfTemplateSource firstPageTemplate, PdfTemplateSource? continuationPageTemplate)
    {
        // Get property information of this form and of the form's items.
        var fields = this.GetFormFields().ToList();

        List<IEnumerable<DataLine>> chunks =
        [
            itemFields.Take(Settings.FirstPageRowCount)
        ];

        if (itemFields.Count > Settings.FirstPageRowCount)
        {
            chunks.AddRange(itemFields.Skip(Settings.FirstPageRowCount)
                                      .Chunk(Settings.ContinuationPageRowCount));
        }

        var sheetNumber = 1;
        var firstPass = true;
        using (var ms = new MemoryStream())
        {
            using (var document = new PdfDocument(new PdfWriter(ms)))
            {
                var formCopier = new PdfPageFormCopier();

                foreach (var chunk in chunks)
                {
                    var templateSource = firstPass
                        ? firstPageTemplate
                        : continuationPageTemplate ?? throw new InvalidOperationException("A continuation page template is required for continuation pages.");

                    var bytes = CreateForm(templateSource, fields, chunk, sheetNumber, chunks.Count, firstPass);

                    using (var byteStream = new MemoryStream(bytes))
                    {
                        using (var byteDoc = new PdfDocument(new PdfReader(byteStream)))
                        {
                            byteDoc.CopyPagesTo(1, byteDoc.GetNumberOfPages(), document, formCopier);
                        }
                    }

                    firstPass = false;
                    sheetNumber++;
                }
            }

            return ms.ToArray();
        }
    }

    private static byte[] CreateForm(PdfTemplateSource templateSource, IEnumerable<FormField> fields, IEnumerable<DataLine> dataLines, int sheetNumber, int numberOfSheets, bool firstPass)
    {
        using (var ms = new MemoryStream())
        {
            using (var reader = templateSource.CreateReader())
            {
                using (var document = new PdfDocument(reader, new PdfWriter(ms)))
                {
                    var acroForm = PdfAcroForm.GetAcroForm(document, false)
                        ?? throw new InvalidOperationException($"The PDF template {templateSource.Description} does not contain an AcroForm.");

                    // iterate of form fields
                    foreach (var field in fields.Where(f => firstPass || (firstPass == f.IsInitial)))
                    {
                        if (field.IsNumberOfPages)
                        {
                            field.Value = numberOfSheets;
                        }

                        if (field.IsPageNumber)
                        {
                            field.Value = sheetNumber;
                        }

                        if (field.SumOf is string sumOfFieldName && sumOfFieldName.Length > 0)
                        {
                            field.Value = CalculateSum(dataLines, sumOfFieldName);
                        }

                        field.SetField(acroForm);
                    }

                    // iterate over data fields
                    var index = 0;
                    var lineNumber = 1;
                    foreach (var line in dataLines)
                    {
                        foreach (var field in line.FormFields)
                        {
                            if (field.IsLineNumber && !line.SkipLineNumber)
                            {
                                field.Value = lineNumber;
                            }

                            field.SetField(acroForm, index);
                        }

                        if (!line.SkipLineNumber)
                        {
                            lineNumber++;
                        }

                        index++;
                    }

                    foreach (var field in acroForm.GetAllFormFields())
                    {
                        field.Value.SetFieldName($"{field.Key}_sheet({sheetNumber})");
                    }
                }
            }

            return ms.ToArray();
        }
    }

    private List<DataLine> GetValidatedItemFields()
    {
        if (Items is null)
        {
            throw new InvalidOperationException("Items must be set before publishing.");
        }

        return Items.GetFormFields().ToList();
    }

    private void ValidatePublishState(int itemCount)
    {
        if (Settings is null)
        {
            throw new InvalidOperationException("Settings must be set before publishing.");
        }

        if (Settings.FirstPageRowCount < 0)
        {
            throw new InvalidOperationException("Settings.FirstPageRowCount cannot be negative.");
        }

        if (Settings.ContinuationPageRowCount < 0)
        {
            throw new InvalidOperationException("Settings.ContinuationPageRowCount cannot be negative.");
        }

        if (RequiresContinuationTemplate(itemCount))
        {
            if (Settings.ContinuationPageRowCount <= 0)
            {
                throw new InvalidOperationException("Settings.ContinuationPageRowCount must be greater than zero when items exceed the first-page row count.");
            }
        }
    }

    private bool RequiresContinuationTemplate(int itemCount)
    {
        return itemCount > Settings.FirstPageRowCount;
    }

    private static decimal CalculateSum(IEnumerable<DataLine> dataLines, string sumOfFieldName)
    {
        decimal total = 0;

        foreach (var formField in dataLines.SelectMany(line => line.FormFields).Where(f => f.Name == sumOfFieldName))
        {
            if (formField.Value is null)
            {
                continue;
            }

            if (formField.Value is not decimal value)
            {
                throw new InvalidOperationException($"SumOf field '{sumOfFieldName}' must contain decimal values.");
            }

            total += value;
        }

        return total;
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
