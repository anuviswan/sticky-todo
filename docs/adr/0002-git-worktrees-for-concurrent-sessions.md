# ADR 0002: Use a separate git worktree for each concurrent development session

## Status

Accepted

## Context

Two Claude Code sessions ran against the same local clone (`D:\Source\sticky-todo`) at the
same time: an interactive session preparing a versioning-format change on
`chore/release-versioning-windows-store-format`, and a background run of this repo's
changelog-updater skill on `docs/update-changelog-unreleased-3`.

A single git working directory has exactly one working tree and one `HEAD`. Both sessions
shared it. The changelog-updater session's own housekeeping — branch checkouts and a
`git reset` to get a clean tree before committing — ran against that same shared working
tree and incidentally discarded the interactive session's uncommitted edits to four files
mid-flight. The files' original content came back later, but only because nothing had been
committed yet and the diffs were still available in the conversation transcript; a
concurrent session that reset *after* different, unsaved edits could destroy them outright
with no recovery path.

This isn't specific to Claude Code sessions — any two processes (a human editing in an IDE,
a CI-adjacent script, a second terminal) operating on one shared working directory can
stomp each other's uncommitted state the same way, since `checkout`/`reset`/`stash` all
mutate the one tree in place.

`git worktree` exists specifically to give each concurrent line of work its own working
directory while sharing the same underlying object store and refs, so branches, commits,
and pushes are still visible across worktrees without the working-tree collisions.

## Decision

- Each concurrent development session against this repo (multiple Claude Code sessions, or
  a session running alongside manual work) gets its own `git worktree`, created off the
  primary clone:

  ```bash
  git worktree add ../sticky-todo-<purpose> <branch-name>
  ```

- Only one active session may use the primary clone's working directory at a time. Before
  starting a second concurrent session, check `git worktree list` and add a new worktree
  rather than reusing the primary checkout.
- Remove a worktree once its branch is merged or abandoned:

  ```bash
  git worktree remove ../sticky-todo-<purpose>
  ```

## Consequences

- Eliminates the specific failure observed here: one session's branch switches, resets, or
  stashes can no longer clobber another session's uncommitted edits, since each has its own
  working tree.
- Costs some extra disk space per worktree (a full checkout, not the `.git` object store,
  which is still shared) and adds a cleanup step after a branch is done with.
- Does not eliminate all shared-state risk — two worktrees can still race on pushing to the
  same remote branch, or both open PRs against `main` that conflict with each other. Ordinary
  git conflict resolution still applies there; this ADR only addresses working-tree
  corruption from shared local state.
- Requires remembering to set this up *before* starting concurrent work — there's no
  automatic enforcement, so this is a process convention rather than a technical guarantee.
