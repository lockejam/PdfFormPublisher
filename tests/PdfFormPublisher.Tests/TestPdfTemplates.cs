using PdfFormPublisher.Attributes;
using PdfFormPublisher.Interfaces;
using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;

namespace PdfFormPublisher.Tests;

internal static class TestPdfTemplates
{
    public static string CreateSimpleTemplate()
    {
        return CreateTemplate("simple", ["Title", "RENAMED", "Date"], choiceFields: ["Choice"]);
    }

    public static string CreateCheckboxTemplate()
    {
        return CreateTemplate(
            "checkbox",
            textFields: [],
            choiceFields: [],
            checkboxFields:
            [
                new CheckboxField("DefaultChecked"),
                new CheckboxField("CustomChecked", "On"),
                new CheckboxField("DefaultUnchecked")
            ]);
    }

    public static string CreateTabularTemplate(string name, int rowCount, bool includeInitialFields)
    {
        var fields = new List<string>
        {
            "SheetTotal",
            "PageNumber",
            "NumberOfPages"
        };

        if (includeInitialFields)
        {
            fields.Add("Title");
        }

        for (var index = 0; index < rowCount; index++)
        {
            fields.Add($"LineNumber.{index}");
            fields.Add($"Description.{index}");
            fields.Add($"Cost.{index}");
        }

        return CreateTemplate(name, fields, choiceFields: []);
    }

    public static IReadOnlyDictionary<string, string> ReadFieldValues(byte[] pdfBytes)
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

    private static string CreateTemplate(string name, IEnumerable<string> textFields, IEnumerable<string> choiceFields)
    {
        return CreateTemplate(name, textFields, choiceFields, checkboxFields: []);
    }

    private static string CreateTemplate(
        string name,
        IEnumerable<string> textFields,
        IEnumerable<string> choiceFields,
        IEnumerable<CheckboxField> checkboxFields)
    {
        var filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{name}-{Guid.NewGuid():N}.pdf");

        using (var writer = new PdfWriter(filePath))
        {
            using (var document = new PdfDocument(writer))
            {
                var page = document.AddNewPage();
                var acroForm = PdfAcroForm.GetAcroForm(document, true);
                var fieldIndex = 0;

                foreach (var fieldName in textFields)
                {
                    acroForm.AddField(CreateTextField(document, page, fieldName, fieldIndex), page);
                    fieldIndex++;
                }

                foreach (var fieldName in choiceFields)
                {
                    acroForm.AddField(CreateChoiceField(document, page, fieldName, fieldIndex), page);
                    fieldIndex++;
                }

                foreach (var field in checkboxFields)
                {
                    acroForm.AddField(CreateCheckboxField(document, page, field.Name, fieldIndex, field.CheckedValue), page);
                    fieldIndex++;
                }
            }
        }

        return filePath;
    }

    private static PdfTextFormField CreateTextField(PdfDocument document, PdfPage page, string fieldName, int index)
    {
        return new TextFormFieldBuilder(document, fieldName)
            .SetWidgetRectangle(CreateFieldRectangle(index))
            .SetPage(page)
            .CreateText();
    }

    private static PdfChoiceFormField CreateChoiceField(PdfDocument document, PdfPage page, string fieldName, int index)
    {
        return new ChoiceFormFieldBuilder(document, fieldName)
            .SetWidgetRectangle(CreateFieldRectangle(index))
            .SetPage(page)
            .SetOptions(["Red", "Blue", "Green"])
            .CreateComboBox();
    }

    private static PdfButtonFormField CreateCheckboxField(PdfDocument document, PdfPage page, string fieldName, int index, string? checkedValue)
    {
        var field = new CheckBoxFormFieldBuilder(document, fieldName)
            .SetWidgetRectangle(CreateFieldRectangle(index))
            .SetPage(page)
            .CreateCheckBox();

        if (!string.IsNullOrWhiteSpace(checkedValue))
        {
            field.GetFirstFormAnnotation().SetCheckBoxAppearanceOnStateName(checkedValue);
        }

        return field;
    }

    private static Rectangle CreateFieldRectangle(int index)
    {
        return new Rectangle(36, 760 - (index * 24), 200, 18);
    }

    private sealed record CheckboxField(string Name, string? CheckedValue = null);
}

internal sealed class SimplePdfFormModel : PdfForm
{
    public SimplePdfFormModel()
    {
    }

    public SimplePdfFormModel(string filePath)
        : base(filePath)
    {
    }

    public string Title { get; init; } = string.Empty;

    [FormField(FieldName = "RENAMED")]
    public string Alias { get; init; } = string.Empty;

    [FormField(DataFormat = "yyyy-MM-dd")]
    public DateTime Date { get; init; }

    public string[] Choice { get; init; } = [];
}

internal sealed class CheckboxPdfFormModel : PdfForm
{
    public CheckboxPdfFormModel(string filePath)
        : base(filePath)
    {
    }

    public bool DefaultChecked { get; init; }

    public bool CustomChecked { get; init; }

    public bool DefaultUnchecked { get; init; }
}

internal sealed class TestInventoryPdfForm : TabularPdfForm
{
    public TestInventoryPdfForm(TabularPdfFormSettings settings)
        : base(settings)
    {
    }

    [DataLine(IsInitial = true)]
    public string Title { get; init; } = string.Empty;

    [DataLine(SumOf = nameof(TestInventoryLine.Cost))]
    [FormField(DataFormat = "0.00")]
    public decimal SheetTotal { get; init; }

    [DataLine(IsPageNumber = true)]
    public int PageNumber { get; init; }

    [DataLine(IsNumberOfPages = true)]
    public int NumberOfPages { get; init; }
}

internal sealed class TestInventoryLine : IDataLine
{
    [DataLine(IsLineNumber = true)]
    public int? LineNumber { get; init; }

    public string Description { get; init; } = string.Empty;

    [FormField(DataFormat = "0.00")]
    public decimal Cost { get; init; }

    [FormField(false)]
    public bool SkipLineNumber { get; set; }
}
