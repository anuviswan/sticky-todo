# Contributing

## Versioning

StickyDo follows [Semantic Versioning](https://semver.org/) (`MAJOR.MINOR.PATCH`).

The version is defined in one place, [`src/Directory.Build.props`](src/Directory.Build.props), and applies
to every project under `src/` via `Version`, `AssemblyVersion`, and `FileVersion`. Individual `.csproj`
files should not set these properties.

- **MAJOR** — breaking changes to the sync protocol, on-disk note format, or public API.
- **MINOR** — new user-facing functionality that's backward compatible (e.g. a new note type, a new widget feature).
- **PATCH** — bug fixes and other backward-compatible changes with no new functionality.

Before `1.0.0`, the app is under active development and its API/format is not considered stable; expect more
frequent `MINOR` bumps for breaking changes during this period, per SemVer's pre-1.0 convention.

### Bumping the version

As part of preparing a release:

1. Update `Version`, `AssemblyVersion`, and `FileVersion` in `src/Directory.Build.props`.
2. Tag the release commit (e.g. `git tag v0.2.0`).

The version is readable at runtime via standard assembly metadata (e.g.
`Assembly.GetExecutingAssembly().GetName().Version` or
`FileVersionInfo.GetVersionInfo(...).FileVersion`), for use anywhere the build needs to be identified —
such as a future About dialog.

## Continuous Integration

Every Pull Request targeting `main` is validated by the
[`PR Validation`](.github/workflows/pr-validation.yml) GitHub Actions workflow, which:

1. Restores NuGet dependencies for [`StickyDo.Widget.sln`](src/StickyDo.Widget/StickyDo.Widget.sln)
   (the solution covering every project: `StickyDo.Domain`, `StickyDo.Domain.Tests`,
   `StickyDo.Widget`, `StickyDo.Widget.Controls`, `StickyDo.Widget.Tests`).
2. Builds the solution in `Release` configuration.
3. Runs all MSTest test projects.

It runs on `windows-latest` because `StickyDo.Widget` and `StickyDo.Widget.Controls` target
`net10.0-windows` (WPF) and cannot build on Linux/macOS runners. A failed restore, build, or test
step fails the workflow, which is a required status check on `main` — Pull Requests cannot be
merged until it passes.

Test results are uploaded as a workflow artifact (`test-results`, `.trx` format) for inspection
regardless of pass/fail.

The workflow is intentionally a single `build-and-test` job so future steps — code coverage,
static analysis/Roslyn analyzers, formatting checks (`dotnet format`), security scanning, or a
separate packaging/release workflow — can be added as new steps or jobs without restructuring it.
