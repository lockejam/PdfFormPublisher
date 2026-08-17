# Package Installation And Upgrade

Reviewed for the M3 packaging-readiness milestone.

## Current Package State

The package ID is `PdfFormPublisher`, and the current package version is `0.1.0`.
The package is not published to a public feed yet. Until a package version is
published through the release workflow, consume it from a local package output
or from a CI package artifact.

PdfFormPublisher currently targets `.NET 10`. Consumer projects must target a
compatible framework.

Before adopting the package, review the repository license and dependency notes
in [dependencies.md](dependencies.md). PdfFormPublisher and its iText dependency
have AGPL licensing implications for package consumers.

## Install From A Local Package

Pack the library from the repository root:

```powershell
dotnet restore PdfFormPublisher.slnx
dotnet build PdfFormPublisher.slnx --configuration Release
dotnet pack src\PdfFormPublisher\PdfFormPublisher.csproj --configuration Release --no-build --output artifacts\packages
```

In the consumer project, create or update `NuGet.config` so restore can find both
the local package and public dependencies:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="pdf-form-publisher-local" value="C:\path\to\PdfFormPublisher\artifacts\packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

Use the actual path to your local `artifacts\packages` directory. Then add the
package:

```powershell
dotnet add package PdfFormPublisher --version 0.1.0
dotnet restore
```

For repeated local package testing with the same version, force a clean restore:

```powershell
dotnet restore --no-cache --force
```

## Install From A Public Feed

After the release workflow publishes the package to NuGet.org, a consumer project
should install it with the normal NuGet package command:

```powershell
dotnet add package PdfFormPublisher --version 0.1.0
```

Use the exact released version. During the `0.x` phase, avoid floating versions
because public API changes may still occur before `1.0.0`.

## Basic Consumption

Reference the package namespace and model your PDF form with `PdfForm`:

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
}
```

Publish from a template path:

```csharp
var form = new SupplyRequestForm("templates/supply-request.pdf")
{
    Title = "Supply Request",
    RequestId = "REQ-001"
};

byte[] pdfBytes = form.Publish();
```

For ASP.NET, cloud storage, embedded resources, or tests, publish from streams:

```csharp
using var templateStream = File.OpenRead("templates/supply-request.pdf");
using var outputStream = new MemoryStream();

var form = new SupplyRequestForm
{
    Title = "Supply Request",
    RequestId = "REQ-001"
};

form.Publish(templateStream, outputStream);
```

Keep fillable PDF templates available to the consuming app through normal .NET
content files, storage, embedded resources, or another application-specific
template source.

## Upgrade

Upgrade by choosing the target package version, updating the package reference,
and rerunning restore, build, and tests:

```powershell
dotnet add package PdfFormPublisher --version 0.1.1
dotnet restore
dotnet build
dotnet test
```

If editing the project file directly, update the `PackageReference` version:

```xml
<PackageReference Include="PdfFormPublisher" Version="0.1.1" />
```

For local package upgrades, repack the library, update the consumer reference to
the new version, and restore with `--no-cache --force` if NuGet still resolves an
older local package.

Because the package is still pre-`1.0`, check the README and release notes, when
available, for renamed APIs or behavior changes before upgrading. Compile errors
after an upgrade usually indicate a deliberate public API cleanup that should be
updated in the consumer model code.
