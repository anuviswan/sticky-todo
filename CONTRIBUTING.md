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
3. Runs dead code analysis (see below).
4. Runs all MSTest test projects.

It runs on `windows-latest` because `StickyDo.Widget` and `StickyDo.Widget.Controls` target
`net10.0-windows` (WPF) and cannot build on Linux/macOS runners. A failed restore, build, dead
code analysis, or test step fails the workflow, which is a required status check on `main` —
Pull Requests cannot be merged until it passes.

Test results are uploaded as a workflow artifact (`test-results`, `.trx` format) for inspection
regardless of pass/fail.

The workflow is intentionally a single `build-and-test` job so future steps — code coverage,
security scanning, or a separate packaging/release workflow — can be added as new steps or jobs
without restructuring it.

### Dead code analysis

The pipeline detects unused and unreachable code using the Roslyn analyzers built into the .NET
SDK — no separate tool to install. Severities are configured once in the root
[`.editorconfig`](.editorconfig) and apply both locally (IDE / `dotnet build`) and in CI, via
[`EnforceCodeStyleInBuild`](src/Directory.Build.props). Two CI steps enforce a curated,
high-confidence subset as build failures; everything else stays a visible warning
("informational findings" per the rule of thumb):

| Step | Catches | Diagnostic IDs |
| --- | --- | --- |
| `Dead code analysis (compiler diagnostics)` | Unreachable code, unused local variables, unused local functions, fields assigned but never read | `CS0162`, `CS0168`, `CS0219`, `CS0414`, `CS8321` |
| `Dead code analysis (unused members and usings)` | Unused private methods/fields/properties/events/types, write-only private members, unused `using` directives, redundant assignments | `IDE0051`, `IDE0052`, `IDE0005`, `IDE0059` |

The second step runs `dotnet format analyzers --verify-no-changes` rather than `dotnet build`,
because `IDE0005` (unused usings) cannot be evaluated during a normal build without also turning
on `GenerateDocumentationFile` — which would separately require XML doc comments on every public
member. `dotnet format` doesn't have that limitation, doesn't modify any files in
`--verify-no-changes` mode, and exits non-zero when it finds a diagnostic at or above `warn`.

**Known limitations** — these analyzers work per-member within a file's semantic model, not via
whole-solution reachability analysis, so they do **not** catch an entirely unused `public`/
`internal` class, unused method parameters (`IDE0060`, deliberately left at its default silent
severity — it fires constantly on interface implementations and event handlers), or redundant
logic that doesn't reduce to an unused symbol. For that class of dead code, use the
[`dead-code-scan`](.claude/skills/dead-code-scan/SKILL.md) Claude Code skill for a manual/AI-assisted
review of changed files.

**Resolving a finding:**

- If the code is genuinely unused, delete it.
- If it looks unused but must be retained (e.g. required to satisfy an interface, invoked via
  reflection, DI, XAML binding, or serialization), suppress the specific diagnostic ID inline
  with `#pragma warning disable <ID> ... #pragma warning restore <ID>` and a one-line comment
  explaining why. See `FakePersistenceService.NoteSaveStateChanged` in
  [`StickyNoteWindowViewModelTests.cs`](src/StickyDo.Widget.Tests/ViewModels/StickyNoteWindowViewModelTests.cs)
  for an example (an interface-mandated event a test fake never needs to raise).
