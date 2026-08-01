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
