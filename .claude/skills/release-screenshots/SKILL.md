---
name: release-screenshots
description: Recaptures the 10 Microsoft Store listing screenshots in docs/screenshots/ from a live run of the Widget app, seeded with the fixed demo dataset in docs/dummy notes/. Use this before a release, whenever the Widget UI has changed visibly, or whenever the user asks to "update the screenshots," "refresh the Store screenshots," or "regenerate docs/screenshots." Windows-only — drives the actual WPF app via window automation, not a mock.
---

# Release Screenshots

Regenerates `docs/screenshots/*.png` by actually building and running
`StickyDo.Widget`, seeding it with the fixed demo dataset in
`docs/dummy notes/`, and driving it through window automation to reproduce
each of the 10 marketing screenshots pixel-for-pixel against the current
UI. This exists because the shipped screenshots need to stay visually
truthful to the real app — the point isn't a mockup, it's proof the actual
build looks like this.

## Why this shape

The first time these screenshots were regenerated, it took a full session
of trial and error to work out: which app data actually produced the
original screenshots (recovered from `%LocalAppData%` — the seed set is now
preserved in `docs/dummy notes/` specifically so this never has to be
re-derived), the exact click coordinates for each view, and — the biggest
time sink — that Win32 window automation loses foreground/z-order the
moment control returns to *this* host app between tool calls, so a
newly-opened floating note window silently ends up capturing whatever's
behind it instead. The bundled script encodes all of that as one
continuous, uninterrupted PowerShell run so the timing issue can't recur.

## Workflow

### 1. Confirm `docs/dummy notes/` is still the source of truth

This folder holds 8 JSON files (IDs `11111111-...-101` through `108`) that
are a byte-for-byte copy of what the Widget app persists per-note. If the
user has hand-edited any of them (new title, different color, a task
added), that's intentional — just be aware the resulting screenshots will
reflect whatever's in there now, not what's currently committed under
`docs/screenshots/`.

### 2. Run the capture script

```bash
pwsh -File ".claude/skills/release-screenshots/scripts/Capture-Screenshots.ps1"
```

(or plain `powershell -File ...` on Windows PowerShell 5.1). This one
script does the entire flow start to finish:

1. Stops any running `StickyDo.Widget` process.
2. Backs up the real `%LocalAppData%\DefineStack\StickyDo.Debug\Data`.
3. Seeds that folder from `docs/dummy notes/` (forcing every item's
   `IsOpened` to `false` first, so the app boots to the main list view
   instead of auto-restoring floating notes).
4. Runs `dotnet build` on the Widget project.
5. Launches the app and, via raw Win32 calls (no UI Automation framework
   needed for this app), clicks through Todos → Notes → Favorites →
   Search → Settings, opens the three notes used in the floating-window
   and desktop-hero shots, and captures all 10 PNGs into
   `docs/screenshots/`.
6. Restores the user's original debug data — in a `finally` block, so this
   happens even if a step above throws.

It aborts early and loudly if display scaling isn't 100% (96 DPI), since
every click coordinate in the script is a hardcoded pixel offset measured
at that scale.

### 3. Verify before trusting it

Read each of the 10 output files and sanity-check them — don't just assume
the run succeeded because the script exited 0. Specifically look for:

- A screenshot that's just this terminal/host app's own window instead of
  the expected app view (the z-order failure mode described above — if you
  see this, it means something interrupted the script mid-run rather than
  letting it finish uninterrupted).
- Card colors or star/favorite states in `01`, `02`, `03` that don't match
  what `docs/dummy notes/*.json` actually specifies — a mismatch means the
  dummy notes were edited without updating a color/flag consistently, not
  a script bug.
- `10-desktop-hero.png` showing real desktop icons or a visible taskbar —
  the script hides desktop icons for the capture and restores them
  immediately after; if they're visible, the capture region was probably
  occluded by another window and needs a rerun.

Diff file sizes or open a couple side-by-side against the previously
committed versions in `git diff` / `git show HEAD:docs/screenshots/...` —
large, unexplained differences (not just anti-aliasing noise) are worth a
second look before committing.

### 4. Show your work, then ask before committing

Report which of the 10 files changed meaningfully and why (UI change,
content change, or neither — i.e. a clean re-run with no visible diff).
Per this repo's commit rules, **always ask before committing** — don't
stage or commit `docs/screenshots/` changes automatically.

## Notes

- If the app's XAML layout changes (sidebar icon positions, the card grid,
  the note footer toolbar), the hardcoded click coordinates in
  `scripts/Capture-Screenshots.ps1` will drift out of sync and need
  updating to match — the script's header comment documents what each
  coordinate assumes.
- The script only ever touches the **Debug** app data folder
  (`StickyDo.Debug`), never the Release one — screenshots are captured
  from a Debug build on purpose, matching how they were originally taken.
- If `docs/dummy notes/` is ever regenerated from scratch, keep note IDs
  `101`-`108` and the `WindowLeft`/`WindowTop` values on the Wi-Fi/Weekly
  Groceries/Sprint Bug Fixes notes (`80,120` / `440,120` / `800,120`) —
  the hero shot's crop region and the floating-window captures both depend
  on those exact coordinates.
