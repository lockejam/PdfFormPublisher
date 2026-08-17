using PdfFormPublisher;
using PdfFormPublisher.Attributes;
using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;

var templatePath = System.IO.Path.Combine(
    System.IO.Path.GetTempPath(),
    $"pdf-form-publisher-smoke-{Guid.NewGuid():N}.pdf");

CreateTemplate(templatePath);

var form = new SmokeForm(templatePath)
{
    Title = "Package smoke test",
    RequestId = "PKG-001"
};

var fields = ReadFieldValues(form.Publish());

if (fields["Title"] != "Package smoke test")
{
    throw new InvalidOperationException($"Expected Title field to be filled, but found '{fields["Title"]}'.");
}

if (fields["REQUEST_ID"] != "PKG-001")
{
    throw new InvalidOperationException($"Expected REQUEST_ID field to be filled, but found '{fields["REQUEST_ID"]}'.");
}

using (var templateStream = File.OpenRead(templatePath))
{
    using (var outputStream = new MemoryStream())
    {
        var streamForm = new SmokeForm
        {
            Title = "Package stream smoke test",
            RequestId = "PKG-STREAM"
        };

        streamForm.Publish(templateStream, outputStream);

        var streamFields = ReadFieldValues(outputStream.ToArray());

        if (streamFields["Title"] != "Package stream smoke test")
        {
            throw new InvalidOperationException($"Expected stream Title field to be filled, but found '{streamFields["Title"]}'.");
        }

        if (streamFields["REQUEST_ID"] != "PKG-STREAM")
        {
            throw new InvalidOperationException($"Expected stream REQUEST_ID field to be filled, but found '{streamFields["REQUEST_ID"]}'.");
        }
    }
}

Console.WriteLine("PdfFormPublisher package smoke test passed.");

static void CreateTemplate(string filePath)
{
    using (var writer = new PdfWriter(filePath))
    {
        using (var document = new PdfDocument(writer))
        {
            var page = document.AddNewPage();
            var acroForm = PdfAcroForm.GetAcroForm(document, true);

            acroForm.AddField(CreateTextField(document, page, "Title", 0), page);
            acroForm.AddField(CreateTextField(document, page, "REQUEST_ID", 1), page);
        }
    }
}

static IReadOnlyDictionary<string, string> ReadFieldValues(byte[] pdfBytes)
{
    using (var stream = new MemoryStream(pdfBytes))
    {
        using (var reader = new PdfReader(stream))
        {
            using (var document = new PdfDocument(reader))
            {
                var acroForm = PdfAcroForm.GetAcroForm(document, false)
                    ?? throw new InvalidOperationException("The generated PDF does not contain an AcroForm.");

                return acroForm.GetAllFormFields()
                               .ToDictionary(field => field.Key, field => field.Value.GetValueAsString());
            }
        }
    }
}

static PdfTextFormField CreateTextField(PdfDocument document, PdfPage page, string fieldName, int index)
{
    return new TextFormFieldBuilder(document, fieldName)
        .SetWidgetRectangle(new Rectangle(36, 760 - (index * 24), 200, 18))
        .SetPage(page)
        .CreateText();
}

internal sealed class SmokeForm : PdfForm
{
    public SmokeForm()
    {
    }

    public SmokeForm(string filePath)
        : base(filePath)
    {
    }

    public string Title { get; init; } = string.Empty;

    [FormField(FieldName = "REQUEST_ID")]
    public string RequestId { get; init; } = string.Empty;
}
