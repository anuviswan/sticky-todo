# Writing user-facing changelog entries

The audience for CHANGELOG.md is someone using StickyDo, not someone reading
the code. PR titles and issue text in this repo are written by and for
developers — full of class names, method names, root-cause analysis, and
ticket references. None of that belongs in a changelog entry. Your job is to
translate, not copy.

## The test

Read your entry back and ask: would someone who has never opened Visual
Studio understand what changed for them? If the sentence contains a class
name, a file path, a stack trace detail, an internal component name, or the
word "refactor", it fails the test.

## Translating a real example

PR #147's title: `fix(issue-143): favourite/pin no longer touch Last
Modified, and fix the resulting UI flicker`

Its body is full of implementation detail (`FileBasedRepository.UpdateAsync`,
`AllowConcurrentExecutions = true`, etc.) — none of that is for the
changelog. The linked issue's "Story" section is more useful, because it's
already written from the user's point of view:

> As a user, when I mark a note as Favourite or Pin it, I don't want its
> Last Modified timestamp to change...

Distilled into one changelog line:

> Fixed: Marking a note as a favourite or pinning it no longer changes its
> "last modified" time.

Note what happened: the two separate bugs described in the PR (timestamp
bug + button flicker) collapsed into what the user actually experienced —
they'd only ever have noticed the timestamp being wrong, or the flicker.
If both are independently noticeable, write two short entries instead of
one long one; don't just shorten the developer summary.

## More before/after pairs

| Developer-facing (don't write this) | User-facing (write this) |
|---|---|
| "fix(issue-134): stop spurious Save Error dialog when deleting a note" | "Fixed: Deleting a note while it was still open no longer showed a false 'Save Error' message." |
| "fix(issue-136): persist favourite toggle from Notes List immediately" | "Fixed: Favouriting a note from the notes list now saves right away, instead of only in memory." |
| "fix(issue-131): hide sync status icon until sync feature ships" | "Removed the sync status icon from the note footer — sync isn't available yet, so this was showing an inaccurate status." |
| "fix(issue-141): don't reopen NoteList after Windows restart" | "Fixed: The notes list no longer pops open automatically every time Windows restarts." |
| "refactor: extract NoteCardViewModel base class" | (excluded by default — no user-visible effect) |

## Rules of thumb

- **Lead with the user's action or experience**, not the mechanism: "Pinning a note no longer..." beats "The pin/favourite command no longer stamps...".
- **Say what changed for them**, not how it was fixed. Nobody needs to know which handler was patched.
- **One sentence per entry.** If you need "and" to join two unrelated fixes, split them.
- **Drop ticket/issue numbers from the sentence itself** — the PR reference link at the end already provides that traceability.
- **Keep terminology consistent with the app's UI** (e.g. "favourite", "pin", "notes list") rather than internal names for the same concepts.
- **For breaking changes**, say what the user needs to do differently, not just that something changed internally.
- When a PR fixed something most users would never have noticed (an edge case, a rare race condition), it's fine for the entry to be short and low-key — don't inflate it into something it wasn't.
