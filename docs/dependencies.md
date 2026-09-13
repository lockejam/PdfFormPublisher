# Dependency Notes

This project currently depends on `itext` for PDF form reading, field assignment, and PDF merging.

## Repository license

PdfFormPublisher is licensed under the GNU Affero General Public License v3.0. See the root `LICENSE` file.

The repository license does not remove the need to understand the licenses of its dependencies. The current implementation uses iText, which has its own AGPL/commercial licensing model.

## Current package set

- `itext` `9.6.0`
- `itext.bouncy-castle-adapter` `9.6.0`

`ClosedXML` was removed because the repository does not currently contain any code paths that read or write Excel files.

## Build dependency

- `Microsoft.SourceLink.GitHub` `10.0.401` provides source metadata for package debugging.
  It is a private build dependency (`PrivateAssets="All"`).

## iText strategy

`PdfFormPublisher` currently uses `itext` `9.6.0` with `itext.bouncy-castle-adapter` `9.6.0`.

Before implementing signature support, review the active iText package line and confirm that the chosen version is still the right fit for:

- visible signature placement
- certificate-based digital signing
- package maintenance status
- any API changes that would affect `PdfFormPublisher`

## Licensing note

iText uses a dual-licensing model:

- AGPL for open-source use
- commercial licensing for use cases that cannot comply with AGPL obligations

That matters for `PdfFormPublisher` because any application that uses this library also uses iText through it. Closed-source, internal business, or commercial applications should review whether they can comply with iText's AGPL terms or need a commercial iText license.

Before shipping NuGet packages or signature examples, confirm that the intended distribution model is compatible with PdfFormPublisher's repository license and iText's license terms.

References:

- NuGet Gallery for `itext`: [https://www.nuget.org/packages/itext/9.6.0](https://www.nuget.org/packages/itext/9.6.0)
- NuGet Gallery for `itext.bouncy-castle-adapter`: [https://www.nuget.org/packages/itext.bouncy-castle-adapter/9.6.0](https://www.nuget.org/packages/itext.bouncy-castle-adapter/9.6.0)
- iText AGPL licensing overview: [https://itextpdf.com/how-buy/AGPLv3-license](https://itextpdf.com/how-buy/AGPLv3-license)
- iText licensing overview: [https://itextpdf.com/how-buy](https://itextpdf.com/how-buy)
