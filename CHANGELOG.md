# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Notes can now be resized by dragging any edge or corner, not just the bottom-left one. ([#151](https://github.com/anuviswan/sticky-todo/pull/151))
- You can now maximize the notes list window from a button in its title bar, or by double-clicking the title bar. ([#165](https://github.com/anuviswan/sticky-todo/pull/165))

### Fixed

- Deleting a note no longer occasionally shows a false "Save Error" message right after. ([#135](https://github.com/anuviswan/sticky-todo/pull/135))
- Favouriting a note from the notes list is now saved right away, instead of sometimes losing another note's favourite status. ([#142](https://github.com/anuviswan/sticky-todo/pull/142))
- The notes list no longer pops open by itself every time Windows restarts — your open notes are restored instead. ([#146](https://github.com/anuviswan/sticky-todo/pull/146))
- Marking a note as a favourite or pinning it no longer changes its "last modified" time. ([#147](https://github.com/anuviswan/sticky-todo/pull/147))
- Toggling one note's favourite status no longer briefly disables the favourite button on every other note. ([#147](https://github.com/anuviswan/sticky-todo/pull/147))
- The "First Task" sample task now only appears in your very first note, instead of every new note you create — and it won't reappear if you delete it. ([#150](https://github.com/anuviswan/sticky-todo/pull/150))
- Relaunching StickyDo while it's already running no longer shows a confusing "Unexpected Error" message after the "already running" notice. ([#154](https://github.com/anuviswan/sticky-todo/pull/154))
- Deleting a note now reliably shows the confirmation prompt, instead of occasionally doing nothing on slower PCs. ([#157](https://github.com/anuviswan/sticky-todo/pull/157))
- The app icon no longer has a faint white box baked into its background — it now blends in properly against the tray, title bar, and dark surfaces. ([#166](https://github.com/anuviswan/sticky-todo/pull/166))
- The search box placeholder no longer mentions searching by "labels," a feature that doesn't exist. ([#167](https://github.com/anuviswan/sticky-todo/pull/167))

### Removed

- The sync status icon in a note's footer, since sync isn't available yet and the icon was only showing a confusing "not synced" message. ([#144](https://github.com/anuviswan/sticky-todo/pull/144))
