# ADR 0003: Use the Microsoft Store's Major.Minor.Build.Revision format instead of SemVer

## Status

Accepted

## Context

Release versioning touched three places that each cared about the version string
differently:

- `CHANGELOG.md` (Keep a Changelog headings) and `CONTRIBUTING.md` originally documented
  Semantic Versioning (`MAJOR.MINOR.PATCH`, optionally with a `-prerelease` suffix).
- `prepare-release.yml` validated the version dispatched to it as SemVer, matching that
  documentation.
- `msix-build.yml` — the workflow that actually builds and signs the MSIX package for
  Microsoft Store submission — sets the manifest's `Identity/@Version`, which the Store
  requires to be exactly four numeric fields, `Major.Minor.Build.Revision`, each a 16-bit
  unsigned integer (0–65535). This is a hard platform constraint, not a style choice: MSIX
  packaging and Partner Center reject any other shape, and by convention the fourth field
  (`Revision`) is reserved — Partner Center manages it, and submitted manifests should set
  it to `0`.

These two schemes are incompatible: a valid SemVer string like `1.2.3-beta.1` is not a valid
Store identity version, and a Store version like `1.0.1.0` is not valid SemVer. The mismatch
surfaced concretely when `prepare-release.yml` rejected `1.0.1.0` — a version already
expected downstream by `msix-build.yml` — because its validation regex still expected SemVer.

Three options were considered:

1. **Keep SemVer as the source of truth**, entered once in `prepare-release.yml` and
   `CHANGELOG.md`, and separately derive/enter the 4-part Store version by hand when running
   `msix-build.yml` (e.g. `1.2.3` → `1.2.3.0`).
2. **Accept both formats** in `prepare-release.yml` without changing anything else, leaving
   the two conventions to coexist.
3. **Standardize on the Store's 4-part format everywhere** — `CHANGELOG.md`,
   `CONTRIBUTING.md`, `Directory.Build.props`, and both release workflows — so one version
   string flows through the entire pipeline with no conversion step.

Option 1 keeps SemVer's expressiveness (in particular prerelease tags like `-beta.1`, which
the Store format cannot represent) but requires a manual, error-prone translation step at
release time — exactly the kind of mismatch that caused this issue in the first place.
Option 2 avoids an immediate rewrite but leaves two conventions permanently coexisting in the
same pipeline, which is confusing and doesn't fix the root cause. Neither this project's
`AssemblyVersion`/`FileVersion` (already 4-part, e.g. `0.1.0.0`) nor its release process
publishes NuGet packages, so nothing in the existing tooling depends on strict SemVer
semantics.

## Decision

- Adopt the Microsoft Store's `Major.Minor.Build.Revision` format as this project's single
  versioning scheme, replacing SemVer everywhere it appeared:
  - `Directory.Build.props`: `Version`, `AssemblyVersion`, `FileVersion` all use the 4-part
    format (`Version` changed from `0.1.0` to `0.1.0.0` to match the other two).
  - `CHANGELOG.md` headings and `CONTRIBUTING.md`'s versioning policy use
    `Major.Minor.Build.Revision`.
  - `prepare-release.yml` validates the dispatched version against that format instead of
    SemVer.
- Field meaning, adapted from the prior SemVer convention:
  - **MAJOR** — breaking changes to the sync protocol, on-disk note format, or public API.
  - **MINOR** — new user-facing functionality that's backward compatible.
  - **BUILD** — bug fixes and other backward-compatible changes with no new functionality
    (fills the role SemVer's `PATCH` had).
  - **REVISION** — reserved by the Microsoft Store, which assigns it at submission; always
    `0` in this repo's manifests, tags, and CHANGELOG.md headings.
- Before `1.0.0.0`, expect more frequent `MINOR` bumps for breaking changes, mirroring
  SemVer's pre-1.0 convention applied to the MAJOR/MINOR fields.

## Consequences

- One version string now flows unchanged from the `prepare-release.yml` dispatch input,
  through the `CHANGELOG.md` heading and release tag, to `msix-build.yml`'s manifest
  `Identity/@Version` — no manual conversion step, and no risk of the changelog version and
  the shipped package version silently drifting apart.
- Loses SemVer's ability to express prerelease/build-metadata suffixes (e.g. `-beta.1`,
  `+build.5`). If this project ever needs prerelease channels, that will need a different
  mechanism (e.g. a separate prerelease branch or tag suffix convention) rather than encoding
  it in the version string itself.
- Diverges from the common open-source convention of SemVer for `CHANGELOG.md`/tags, which
  may be less immediately familiar to contributors coming from other projects; the versioning
  section of `CONTRIBUTING.md` documents the departure and the reasoning.
- If this project later publishes a NuGet package from any `src/` project, `Version` would
  need reconsidering, since NuGet's package version field has its own SemVer-flavored
  parsing rules that a 4-part `0.0.0.0`-style version does not cleanly satisfy for prerelease
  tagging.
