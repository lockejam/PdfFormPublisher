# PdfFormPublisher

PdfFormPublisher is a small, focused C# library that helps you fill out existing PDF forms.

You start with a fillable PDF template, create a simple C# model with matching property names, and call `Publish()` to get the finished PDF as a `byte[]`. PdfFormPublisher uses iText under the hood so you can handle common form-filling work without writing directly against the PDF library.

## When To Use It

Use PdfFormPublisher when you already have a fillable PDF and want code to populate its fields from application data. It is meant for form filling, not full PDF editing or designing new PDF documents from scratch.

Good fits:

- generating a filled PDF from a web app
- mapping a C# object to PDF field names
- filling repeated table rows in a PDF
- creating continuation pages when a table has more rows than one page can hold

PdfFormPublisher is still being modernized. The current code targets `.NET 10`.

## Add It To A Project

PdfFormPublisher is not published to a public NuGet feed yet. During development,
you can reference the project directly:

```xml
<ProjectReference Include="..\path\to\src\PdfFormPublisher\PdfFormPublisher.csproj" />
```

The intended NuGet package ID is `PdfFormPublisher`.
Package metadata and versioning are documented in [docs/packaging.md](docs/packaging.md).
Installation, local package consumption, and upgrade guidance are documented in
[docs/package-consumption.md](docs/package-consumption.md).

## Your First Form

Start with a fillable PDF template. The template might have fields named something like:

```text
Title
REQUEST_ID
RequestDate
Category
Expedited
```

Create a class that inherits from `PdfForm`. Each public property is matched to a PDF field.

```csharp
using PdfFormPublisher;
using PdfFormPublisher.Attributes;

public sealed class SupplyRequestForm : PdfForm
{
    public SupplyRequestForm(string templatePath)
        : base(templatePath)
    {
    }

    public string Title { get; init; } = string.Empty;

    [FormField(FieldName = "REQUEST_ID")]
    public string RequestId { get; init; } = string.Empty;

    [FormField(DataFormat = "yyyy-MM-dd")]
    public DateTime RequestDate { get; init; }

    public string[] Category { get; init; } = [];

    public bool Expedited { get; init; }

    [FormField(false)]
    public string InternalNotes { get; init; } = string.Empty;
}
```

Then create the form and publish it:

```csharp
var form = new SupplyRequestForm("templates/supply-request.pdf")
{
    Title = "Station Supply Request",
    RequestId = "DS9-001",
    RequestDate = new DateTime(2375, 5, 3),
    Category = ["Operations", "Engineering", "Security"],
    Expedited = true,
    InternalNotes = "Not written to the PDF"
};

byte[] pdfBytes = form.Publish();
```

In this example, `Category` is a `string[]` because it represents a PDF choice field, such as a list box or dropdown. The PDF might offer choices like `Operations`, `Engineering`, `Medical`, and `Security`. For normal text fields, use `string`.

`Expedited` is a `bool` because it represents a checkbox. When the value is `true`, PdfFormPublisher uses the PDF field's checked value, such as `Yes` or `On`. When the value is `false`, it writes `Off`.

That is the core idea: make a model, fill in its values, and publish the PDF.

## Publishing From Streams

Use stream-based publishing when templates come from ASP.NET uploads, cloud storage, embedded resources, or test fixtures. A stream-based form model does not need a template file path.

```csharp
public sealed class SupplyRequestForm : PdfForm
{
    public string Title { get; init; } = string.Empty;

    [FormField(FieldName = "REQUEST_ID")]
    public string RequestId { get; init; } = string.Empty;
}
```

Publish to a `byte[]`:

```csharp
using var templateStream = File.OpenRead("templates/supply-request.pdf");

var form = new SupplyRequestForm
{
    Title = "Station Supply Request",
    RequestId = "DS9-001"
};

byte[] pdfBytes = form.Publish(templateStream);
```

Or write directly to an output stream:

```csharp
using var templateStream = File.OpenRead("templates/supply-request.pdf");
using var outputStream = new MemoryStream();

form.Publish(templateStream, outputStream);
```

PdfFormPublisher leaves caller-provided streams open. Template streams are read from their current position, and output streams are written at their current position.

## How Field Matching Works

PdfFormPublisher uses property names by default.

```csharp
public string Title { get; init; } = string.Empty;
```

This looks for a PDF field named `Title`.

If the PDF field has a different name, use `FieldName`:

```csharp
[FormField(FieldName = "REQUEST_ID")]
public string RequestId { get; init; } = string.Empty;
```

If a value needs a specific format, use `DataFormat`:

```csharp
[FormField(DataFormat = "yyyy-MM-dd")]
public DateTime RequestDate { get; init; }
```

If a property is useful in C# but should not be written to the PDF, exclude it:

```csharp
[FormField(false)]
public string InternalNotes { get; init; } = string.Empty;
```

For checkbox fields, use a `bool`:

```csharp
public bool Expedited { get; init; }
```

## Forms With Rows

Some PDFs have repeated row fields, like an inventory list or line-item table. Use `TabularPdfForm` for those.

The PDF template should name repeated row fields with zero-based suffixes:

```text
LineNumber.0
Description.0
Cost.0
LineNumber.1
Description.1
Cost.1
```

Here is an example of `TabularPdfForm`. Note that the `Title` field is marked with `[DataLine(IsInitial = true)]` because it only exists on the first-page template. That tells PdfFormPublisher to skip `Title` on continuation pages instead of looking for a field that is not there.

```csharp
using PdfFormPublisher;
using PdfFormPublisher.Attributes;
using PdfFormPublisher.Interfaces;

public sealed class InventoryForm : TabularPdfForm
{
    public InventoryForm(TabularPdfFormSettings settings)
        : base(settings)
    {
    }

    [DataLine(IsInitial = true)]
    public string Title { get; init; } = string.Empty;

    [DataLine(SumOf = nameof(InventoryLine.Cost))]
    [FormField(DataFormat = "0.00")]
    public decimal SheetTotal { get; init; }

    [DataLine(IsPageNumber = true)]
    public int PageNumber { get; init; }

    [DataLine(IsNumberOfPages = true)]
    public int NumberOfPages { get; init; }
}

public sealed class InventoryLine : IDataLine
{
    [DataLine(IsLineNumber = true)]
    public int? LineNumber { get; init; }

    public string Description { get; init; } = string.Empty;

    [FormField(DataFormat = "0.00")]
    public decimal Cost { get; init; }

    [FormField(false)]
    public bool SkipLineNumber { get; set; }
}
```

Publish it like this:

```csharp
var settings = new TabularPdfFormSettings
{
    FirstPageFilePath = "templates/inventory-first.pdf",
    ContinuationPageFilePath = "templates/inventory-continuation.pdf",
    FirstPageRowCount = 14,
    ContinuationPageRowCount = 18
};

var form = new InventoryForm(settings)
{
    Title = "Maintenance Supply List",
    Items =
    [
        new InventoryLine { Description = "Self-sealing stem bolt", Cost = 1.25m },
        new InventoryLine { Description = "Cargo bay spacer", Cost = 0.00m, SkipLineNumber = true },
        new InventoryLine { Description = "Power coupling", Cost = 2.75m }
    ]
};

byte[] pdfBytes = form.Publish();
```

If the rows do not fit on the first page of the PDF, PdfFormPublisher uses the continuation-page PDF for the remaining rows. In other words, if your form has overflow pages and `ContinuationPageFilePath` is configured, PdfFormPublisher fills those pages as needed. After publishing, fields are renamed with `_sheet(n)` suffixes so each generated page can keep its own values.

Tabular forms can also publish from template streams. Keep the row counts in `TabularPdfFormSettings`, then pass first-page and continuation-page template streams to `Publish`:

```csharp
using var firstPageTemplateStream = File.OpenRead("templates/inventory-first.pdf");
using var continuationTemplateStream = File.OpenRead("templates/inventory-continuation.pdf");
using var outputStream = new MemoryStream();

form.Publish(firstPageTemplateStream, continuationTemplateStream, outputStream);
```

## Roadmap

PdfFormPublisher is being modernized in small milestones. This README describes how the library works today.

Planned follow-up work includes:

- NuGet packaging and release readiness
- advanced PDF workflow support, including better field diagnostics and template inspection
- runnable example projects, including a basic form example and a DD Form 1149 tabular form example
- signature support, followed by a DD Form 2875 signature workflow example

## Reference

### `PdfForm`

Use this for a PDF that can be filled from one model and one template file.

- Pass the template path to the constructor.
- Add public properties for PDF fields.
- Call `Publish()` to get the completed PDF bytes.
- Call `Publish(templateStream)` or `Publish(templateStream, outputStream)` when the template is already available as a stream.
- Call `PublishTo(outputStream)` to write a path-based publish result to a stream.

### `TabularPdfForm`

Use this for a PDF with repeated rows.

- Pass a `TabularPdfFormSettings` object to the constructor.
- Set `Items` to your row models.
- Call `Publish()` to get the completed PDF bytes.
- Call `Publish(firstPageTemplateStream, continuationTemplateStream)` or `Publish(firstPageTemplateStream, continuationTemplateStream, outputStream)` when templates are already available as streams.
- Call `PublishTo(outputStream)` to write a path-based publish result to a stream.

### `TabularPdfFormSettings`

`TabularPdfFormSettings` tells a tabular form where its templates are and how many rows fit on each page.

| Property | What it means |
| --- | --- |
| `FirstPageFilePath` | Path to the first-page PDF template for path-based publishing. |
| `ContinuationPageFilePath` | Path to the continuation-page PDF template for path-based publishing. Required when rows overflow the first page. |
| `FirstPageRowCount` | Number of rows available on the first page. |
| `ContinuationPageRowCount` | Number of rows available on each continuation page. Must be greater than zero when rows overflow the first page. |

### `IDataLine`

Use `IDataLine` for each row in a tabular form.

| Property | What it means |
| --- | --- |
| `SkipLineNumber` | When `true`, the row does not receive a generated line number and does not advance the line count. This affects the property marked with `[DataLine(IsLineNumber = true)]`. |

### `FormFieldAttribute`

Use `[FormField]` when a property needs special handling.

| Option | What it does |
| --- | --- |
| `FieldName` | Maps a property to a PDF field with a different name. |
| `DataFormat` | Formats values such as dates and decimals before writing them. |
| `false` | Excludes the property from PDF output. |

Most values can be simple strings, numbers, dates, or decimals. Use `DataFormat` when you want a specific date or number format. Use `string[]` only for PDF choice fields, such as list boxes or combo boxes. For checkbox fields, use `bool`; PdfFormPublisher uses the PDF field's checked value for `true` and writes `Off` for `false`.

### `DataLineAttribute`

Use `[DataLine]` for tabular behavior.

| Option | What it does |
| --- | --- |
| `IsInitial` | Writes the field only on the first page. |
| `SumOf` | Sums a decimal row field for each generated page. Use the target PDF field name if the row property uses `FieldName`. |
| `IsLineNumber` | Writes the generated row number. |
| `IsPageNumber` | Writes the current page number. |
| `IsNumberOfPages` | Writes the total page count. |

## Common Errors

PdfFormPublisher tries to fail with clear messages when something is wrong. These are good places to start when publishing does not work.

| Problem | Why it happens | What to try |
| --- | --- | --- |
| The template path is blank. | A `PdfForm` was created with `null`, an empty string, or whitespace for the template path. | Check the path passed to the form constructor. Make sure configuration values are loaded before creating the form. |
| The template file does not exist. | The path points to a file that cannot be found from the running app. | Use an absolute path while debugging, or log the final path before calling `Publish()`. Check that the PDF is copied to the expected output folder. |
| The PDF is not a fillable form. | The PDF does not contain AcroForm fields. A scanned PDF or flat PDF usually has no fields to fill. | Open the PDF in a PDF editor and confirm that the fields are real fillable form fields. |
| A PDF field name does not match your model. | By default, PdfFormPublisher looks for a PDF field with the same name as the C# property. | Check the field name in the PDF. If it differs from the property name, add `[FormField(FieldName = "PDF_FIELD_NAME")]`. |
| A `string[]` value fails during publishing. | `string[]` is only supported for PDF choice fields, such as list boxes or combo boxes. | Use `string` for regular text fields. Use `string[]` only when the target PDF field is a choice field. |
| A checkbox value fails during publishing. | The PDF field may not be a checkbox, or it may not define a checked state. | Confirm that the target field is a checkbox. If the PDF uses unusual field setup, inspect the field's appearance states. |
| A tabular form has no `Items`. | `TabularPdfForm.Publish()` needs row data, even if there is only one row. | Set `Items` before calling `Publish()`. If there are no real rows, pass an empty list only after confirming that empty output is what you want. |
| Rows overflow the first page, but continuation settings are missing. | More rows were provided than `FirstPageRowCount`, so PdfFormPublisher needs a continuation template. | Set `ContinuationPageFilePath` and make sure `ContinuationPageRowCount` is greater than zero. |
| A first-page field is missing on continuation pages. | The field exists only on the first-page template but was not marked as first-page-only. | Add `[DataLine(IsInitial = true)]` to that form property. Future versions may infer this automatically. |
| `SumOf` points to a value that is not a decimal. | Sheet totals currently add decimal row values. | Make sure the row property named by `SumOf` is a `decimal`. If the row field uses `FieldName`, use the target PDF field name in `SumOf`. |

## Dependency And Licensing Notes

PdfFormPublisher is licensed under the GNU Affero General Public License v3.0. See [LICENSE](LICENSE).

PdfFormPublisher currently uses iText 9.6.0 for PDF form reading, field assignment, and PDF merging. iText is dual-licensed under AGPLv3 and commercial terms, so review the dependency licensing notes before using this library in closed-source or commercial applications.

For more detail, see [docs/dependencies.md](docs/dependencies.md).

## Contributing And Security

This project is still early and is not published to a public NuGet feed yet. See [CONTRIBUTING.md](CONTRIBUTING.md) for local setup, test commands, and style expectations.

Please do not open public issues for suspected security vulnerabilities. See [SECURITY.md](SECURITY.md) for reporting guidance.

Public-readiness tracking is documented in [docs/public-readiness.md](docs/public-readiness.md).
Repository layout decisions are documented in [docs/repository-layout.md](docs/repository-layout.md).

## Build And Test

```powershell
dotnet restore PdfFormPublisher.slnx
dotnet build PdfFormPublisher.slnx
dotnet test PdfFormPublisher.slnx
dotnet pack src\PdfFormPublisher\PdfFormPublisher.csproj --configuration Release --no-build --output artifacts\packages
.\scripts\package-smoke-test.ps1 -SkipSolutionBuild -SkipPack
```

The tests and package smoke test create small PDF templates at runtime, so no local PDF assets are required. For more detail, see [docs/testing.md](docs/testing.md) and [docs/packaging.md](docs/packaging.md).
