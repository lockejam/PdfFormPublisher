# Repository Layout

Reviewed for the M3 packaging-readiness milestone.

## Decision

PdfFormPublisher uses a conventional source/test layout before NuGet packaging:

```text
src/
  PdfFormPublisher/
tests/
  PdfFormPublisher.Tests/
docs/
```

The packageable library project lives at `src/PdfFormPublisher/PdfFormPublisher.csproj`.
The xUnit test project lives at `tests/PdfFormPublisher.Tests/PdfFormPublisher.Tests.csproj`
and references the library project directly.
Package identity and versioning are documented in `docs/packaging.md`.

## Rationale

- Keep packageable source separate from tests, docs, CI files, and generated artifacts.
- Give packaging, CI, and future example projects a stable project path before release workflows are added.
- Keep the project path aligned with the `PdfFormPublisher` package identity.

## Deferred

- Future example projects should be added under `examples/` when the M5 example work begins.
- Keep the GitHub repository name aligned with the `PdfFormPublisher` package identity before release publishing.
