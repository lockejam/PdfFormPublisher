# Contributing

PdfFormPublisher is still early and is not published to a public NuGet feed yet. Contributions should keep the project small, focused, and easy for newer C# developers to understand.

## Local Setup

Use the solution file for build and test commands:

```powershell
dotnet restore PdfFormPublisher.slnx
dotnet build PdfFormPublisher.slnx
dotnet test PdfFormPublisher.slnx
```

The test project creates small PDF templates at runtime, so no external PDF test assets are required for the current test suite.

The packageable library project lives under `src/PdfFormPublisher`, and tests live under `tests/PdfFormPublisher.Tests`.

## Branch Workflow

Create issue branches from an up-to-date `main` and open pull requests back into
`main`. Use GitHub issues and milestones to track scope and progress. Milestones
do not have their own integration branches.

Use a descriptive branch name, such as `issue-61-template-inspection`:

```powershell
git switch main
git pull --ff-only origin main
git switch -c issue-61-template-inspection
```

Keep each issue branch focused. Merge it after review and passing build, test,
package, and smoke-test checks, then delete the issue branch. Start the next
issue from the updated `main`.

## Pull Requests

- Keep each PR focused on one issue or one small group of closely related issues.
- Reference the issue in the PR body.
- Run build and tests before opening the PR.
- Update `docs/dependencies.md` when dependency versions or licensing notes change.
- Keep README examples friendly and practical.

## Style Notes

The repository uses `.editorconfig` for shared style preferences.

- Prefer block-style `using` statements over simple using declarations.
- Avoid primary constructors unless there is a strong reason to use one.
- Keep XML documentation helpful and plain, especially on public APIs.
- Do not add broad refactors while making narrow behavior or documentation changes.
