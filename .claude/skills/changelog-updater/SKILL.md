---
name: changelog-updater
description: Compares the main and release branches in this repo, gathers the merged PRs that haven't shipped yet, and updates CHANGELOG.md's "## [Unreleased]" section in Keep a Changelog format with user-friendly wording. Use this whenever the user asks to update the changelog, prep release notes, sync CHANGELOG.md with what's on main, or figure out "what's changed since the last release." Also use it if the user asks to clean up or reword the existing Unreleased section, or to check the changelog for duplicate/missing entries. Trigger even if they just say "update the changelog" without more detail — this skill is the intended workflow for that in this repo.
---

# Changelog Updater

Turns merged PRs that are ahead of the release branch into changelog entries
a *user* would want to read — not a PR-title dump. This is a translation
task as much as a data-gathering one: the hard part isn't finding the PRs,
it's rewriting them.

## Why this shape

This repo's commits follow a `type(issue-N): description` convention
(feat/fix/chore/docs/ci/test/refactor/style/build), which is also how PRs
get merged — as merge commits carrying `Merge pull request #N from ...`.
That prefix is what makes automatic classification possible: it tells you
*what kind* of change something is without reading the diff. But per Keep a
Changelog, only changes a user would actually notice belong in the
changelog — a `refactor` or `chore` is invisible to them by definition, so
those are excluded unless the PR explicitly says otherwise.

## Workflow

### 1. Make sure refs are current

```bash
git fetch origin main release
```

Confirm both branches exist (`git branch -a`). Default to comparing
`origin/main` (head) against `origin/release` (base) — that's this repo's
actual release branch. If the user names different branches, use those
instead of asking; only ask if it's genuinely ambiguous which branches they
mean.

### 2. Gather and classify candidates

Run the bundled script from the repo root:

```bash
python .claude/skills/changelog-updater/scripts/gather_prs.py --base origin/release --head origin/main
```

This does the mechanical part for you:
- Walks the merge commits between the branches and resolves each to a PR via `gh pr view` (also handles squash-merge `(#123)` suffixes, in case this repo's merge style changes later).
- Classifies each PR's type from its conventional-commit title prefix.
- Marks a PR `include: true` by default only if its type is `feat`, `fix`, or it's flagged breaking (`!` after the type, or a `BREAKING CHANGE:` footer in the body). Everything else (`chore`/`docs`/`ci`/`test`/`refactor`/`style`/`build`) is excluded *unless* the PR body contains a line starting with `Changelog:` — that's this repo's explicit opt-in marker, matching the conventional-commit footer style already in use (like `BREAKING CHANGE:`).
- **Checks CHANGELOG.md's existing `[Unreleased]` section for each PR number and marks anything already referenced there as a duplicate** (`include: false`, `duplicate: true`) — this is your dedup guarantee, so you never re-add an entry for a PR that's already listed.
- Follows each PR's linked/closing issue(s) and fetches their title and body too, since issue text (especially any "Story" or "As a user..." framing) is often written closer to the user's perspective than the PR title.

Read the JSON output. `to_add` is what needs new changelog entries;
`skipped` is everything excluded, with a `reason` for each — skim it to
make sure nothing user-facing got miscategorized (e.g. a `docs:` PR that
was actually about in-app help text a user reads, which should probably
have had a `Changelog:` marker and didn't — flag this to the user rather
than silently including or excluding it).

### 3. Write each new entry

For every item in `to_add`, read **[references/wording-guide.md](references/wording-guide.md)**
before writing anything — it has the actual before/after examples from this
repo's own PRs and the rules of thumb for translating developer language
into user language. The short version: use the PR title and body plus any
linked issue text as raw material, but write your own one-sentence
description of what the user will actually notice. Never copy a PR title
or an issue's technical section verbatim.

Format each entry as:

```
- <one user-friendly sentence>. ([#<number>](<pr-url>))
```

The trailing PR link isn't optional decoration — it's what step 2's dedup
check keys off of next time this skill runs, so every entry needs one.

Sort each entry into the section the script suggested (`suggested_section`),
but use judgment: a `fix` that actually removed a broken feature belongs
under `### Removed`, not `### Fixed`. Keep a Changelog's sections, in
order, are: `Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`,
`Security`. Only include the subsections that have content. For a
breaking change, prefix the sentence with `**Breaking:**` and put it under
`### Changed` (or `### Removed` if something was taken away), and say
plainly what the user needs to do differently — not just that something
changed internally.

If `CHANGELOG.md` doesn't exist yet, create it with the standard header:

```markdown
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
```

### 4. Clean up the existing Unreleased section too

Before you finish, read whatever was already under `## [Unreleased]`
(the script gives you this verbatim as `unreleased_section_text`) with the
same eye you just used on the new entries. Existing bullets written before
this skill existed may read like PR titles or contain implementation
detail. Reword any that fail the wording-guide's test — keep the meaning
and the trailing PR link, just fix the phrasing. Don't touch bullets that
already read fine; this is a cleanup pass, not a rewrite-everything pass.

### 5. Show your work

Summarize for the user what got added (grouped by section) and what got
excluded and why, same as any other change — per this repo's CLAUDE.md,
call out assumptions (e.g. "PR #144 removed the sync icon rather than
fixing it, so I filed it under Removed instead of Fixed") and anything you
flagged as ambiguous in step 2.

## Notes

- This skill only ever touches `## [Unreleased]`. Cutting a version and
  moving Unreleased entries under a dated heading is a separate, deliberate
  release step — don't do it as part of this workflow unless asked.
- `gh` must be authenticated (`gh auth status`) since PR/issue bodies come
  from the GitHub API, not local git data.
- The script is read-only against GitHub and only ever *reads* CHANGELOG.md
  — it never writes the file itself. You do the writing, after applying
  judgment the script can't.
