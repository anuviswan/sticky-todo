# Microsoft Store Listing — StickyDo

> Draft product copy for the Partner Center listing. Character counts noted per Microsoft Store
> field limits. Update this file if the listing copy changes so it stays in sync with what's
> actually submitted.

## Short description / subtitle (max 100 chars)

```
Sticky notes and to-dos, together on your desktop.
```
(51 chars)

## Description (max 10,000 chars)

```
StickyDo brings the familiar feel of Windows Sticky Notes together with real to-do lists — so
the things you jot down can also be the things you check off.

Create a note, and decide what it is: a free-form sticky note for quick thoughts, or a to-do
list you can actually complete. Pin the ones that matter to your desktop as floating sticky
notes, right where you're used to seeing them. Everything else lives in a clean, searchable list
so your desktop never gets buried in windows.

FEATURES

• To-dos and notes in one app — capture a quick thought or a task list, your choice
• Floating desktop sticky notes — pin any note to your desktop, sized and styled like the
  Sticky Notes you already know
• Color-coded notes — organize at a glance with a full color palette
• Favorites — keep your most important notes and to-dos one click away
• Fast search — find any note or to-do instantly as you type
• System tray access — StickyDo stays out of your way and is always a click away
• Launch at startup — have your notes ready the moment you sign in

PRIVATE BY DESIGN

StickyDo is fully local and offline. Your notes are stored only on your device — there's no
account to create, no sign-in, and nothing is ever sent over the network. No telemetry, no
analytics, no ads.

Whether you're tracking a project, jotting down a reminder, or replacing a desktop full of
loose notes, StickyDo keeps it simple: write it down, pin what matters, check off what's done.
```
(~1,480 chars)

## What's new (release notes template)

```
Initial release of StickyDo:
- Create to-dos and free-form sticky notes
- Pin notes to your desktop
- Color-code, favorite, and search your notes
- Launch at startup for quick access
```

## Features list (bullet field, if used separately from description)

- To-dos and sticky notes in one app
- Pin notes to your desktop
- Color-coded organization
- Favorites for quick access
- Instant search
- System tray access
- Fully offline — no account, no telemetry

## Category / suggestions

- **Category:** Productivity
- **Age rating:** Everyone (no data collection, no network access — see `PRIVACY_POLICY.md`)
- **Privacy policy URL:** link to `PRIVACY_POLICY.md` (or its hosted equivalent)
- **Support contact:** GitHub issues — https://github.com/anuviswan/sticky-todo/issues

## Restricted capability justification — `runFullTrust`

Partner Center asks why the app needs each restricted capability. Answer for `runFullTrust`:

```
StickyDo's Windows app is a classic Win32/WPF desktop application packaged as MSIX (not a
UWP/sandboxed app) — the manifest declares EntryPoint="Windows.FullTrustApplication". Any
traditional desktop executable packaged this way must request runFullTrust; it is a
requirement of being a Win32 process at all, independent of which specific APIs are called.

StickyDo is built as a WPF app rather than a sandboxed UWP/WinUI app because it needs
desktop-shell integration that UWP does not support:

- Borderless, always-on-top desktop widget windows (the floating sticky notes) that sit
  outside normal app window management
- A system tray icon with a context menu for quick access and background operation
- An optional startup task so the app can launch at Windows sign-in

The app's local JSON-based note storage would work identically in a sandboxed process and
is not a driver of this requirement.
```

## Notes / things to verify before submitting

- Copy deliberately does **not** mention cloud sync or the Android app — those are Phase 2/3
  and not shipped yet (see `README.md`); adding them now would misrepresent the current release
  to Store reviewers and users.
- Screenshots and the Store icon set (`Square44x44Logo`, `Square150x150Logo`, `Wide310x150Logo`,
  splash screen) still need final Store-quality assets per `README.md`'s MSIX packaging notes.
- Confirm the publisher display name (`DefineStack`) and app name (`StickyDo`) match what's
  registered in Partner Center before submission.
