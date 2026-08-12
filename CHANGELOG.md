# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed

- Deleting a note no longer occasionally shows a false "Save Error" message right after. ([#135](https://github.com/anuviswan/sticky-todo/pull/135))
- Favouriting a note from the notes list is now saved right away, instead of sometimes losing another note's favourite status. ([#142](https://github.com/anuviswan/sticky-todo/pull/142))
- The notes list no longer pops open by itself every time Windows restarts — your open notes are restored instead. ([#146](https://github.com/anuviswan/sticky-todo/pull/146))
- Marking a note as a favourite or pinning it no longer changes its "last modified" time. ([#147](https://github.com/anuviswan/sticky-todo/pull/147))
- Toggling one note's favourite status no longer briefly disables the favourite button on every other note. ([#147](https://github.com/anuviswan/sticky-todo/pull/147))

### Removed

- The sync status icon in a note's footer, since sync isn't available yet and the icon was only showing a confusing "not synced" message. ([#144](https://github.com/anuviswan/sticky-todo/pull/144))
