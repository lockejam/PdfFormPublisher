# Testing

Use the solution file when running tests locally.

```powershell
dotnet restore PdfFormPublisher.slnx
dotnet build PdfFormPublisher.slnx
dotnet test PdfFormPublisher.slnx
```

The library project is `src/PdfFormPublisher`, and the test project is `tests/PdfFormPublisher.Tests`. The tests use xUnit and create small PDF templates at runtime, so the current test suite does not require checked-in PDF assets or local sample files.

## What The Tests Cover

- simple `PdfForm` publishing
- renamed fields and formatted values
- checkbox checked and unchecked states
- missing template and invalid configuration errors
- `TabularPdfForm` pagination and continuation pages
- line numbering, sheet totals, page numbers, and total page count fields

## Troubleshooting

If restore fails, confirm the configured NuGet sources can reach nuget.org.

If tests fail because a generated PDF file is missing, rerun the tests from the repository root and check that the process can write to the system temporary folder.

If Visual Studio Code shows stale analyzer or using-statement suggestions, reload the window or restart the C# language server before changing code. The repository `.editorconfig` file is the source of truth for shared style preferences.
