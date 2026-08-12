#!/usr/bin/env python3
"""
Gather merged PRs that are on `head` but not yet on `base`, classify them by
conventional-commit type, and check which ones are already represented in the
CHANGELOG.md [Unreleased] section.

This does the deterministic legwork (git log parsing, gh API calls, regex
classification, dedup lookup) and hands back structured JSON. Writing the
actual user-friendly changelog prose is a judgment call left to the caller.

Usage:
  python gather_prs.py --base origin/release --head origin/main [--changelog CHANGELOG.md]
"""
import argparse
import json
import re
import subprocess
import sys

CONVENTIONAL_RE = re.compile(
    r"^(?P<type>feat|fix|chore|docs|ci|test|refactor|style|build|perf)"
    r"(?:\((?P<scope>[^)]*)\))?"
    r"(?P<bang>!)?:\s*(?P<desc>.+)$",
    re.IGNORECASE,
)
OPT_IN_RE = re.compile(r"^\s*changelog:\s*(.+)$", re.IGNORECASE | re.MULTILINE)
BREAKING_FOOTER_RE = re.compile(r"breaking[ -]change:?\s*(.+)", re.IGNORECASE)
MERGE_PR_RE = re.compile(r"Merge pull request #(\d+) from")
SQUASH_PR_RE = re.compile(r"\(#(\d+)\)\s*$")

DEFAULT_INCLUDED_TYPES = {"feat", "fix"}
TYPE_TO_SECTION = {
    "feat": "Added",
    "fix": "Fixed",
    "perf": "Changed",
}


def run(cmd):
    result = subprocess.run(
        cmd, capture_output=True, text=True, encoding="utf-8", errors="replace"
    )
    if result.returncode != 0:
        raise RuntimeError(f"Command failed: {' '.join(cmd)}\n{result.stderr}")
    return result.stdout


def find_pr_numbers(base, head):
    """Find PR numbers for commits in head..base, via merge commits first,
    falling back to squash-merge '(#123)' suffixes for any commits a merge
    commit didn't already account for."""
    log = run(["git", "log", f"{base}..{head}", "--pretty=format:%s"])
    numbers = []
    seen = set()
    for line in log.splitlines():
        m = MERGE_PR_RE.search(line)
        if not m:
            m = SQUASH_PR_RE.search(line)
        if m:
            n = int(m.group(1))
            if n not in seen:
                seen.add(n)
                numbers.append(n)
    numbers.reverse()  # oldest first, so changelog entries land in merge order
    return numbers


def fetch_pr(number):
    out = run([
        "gh", "pr", "view", str(number),
        "--json", "number,title,body,url,state,closingIssuesReferences",
    ])
    return json.loads(out)


def fetch_issue(number):
    out = run(["gh", "issue", "view", str(number), "--json", "number,title,body"])
    return json.loads(out)


def classify(pr):
    title = pr["title"].strip()
    body = pr.get("body") or ""
    m = CONVENTIONAL_RE.match(title)
    commit_type = m.group("type").lower() if m else None
    desc = m.group("desc") if m else title

    breaking = bool(m and m.group("bang")) or bool(BREAKING_FOOTER_RE.search(body))
    opt_in_match = OPT_IN_RE.search(body)
    opt_in = bool(opt_in_match)
    opt_in_text = opt_in_match.group(1).strip() if opt_in_match else None

    if breaking:
        include, reason = True, "marked breaking (! or BREAKING CHANGE footer)"
    elif opt_in:
        include, reason = True, "opted in via 'Changelog:' marker in PR body"
    elif commit_type in DEFAULT_INCLUDED_TYPES:
        include, reason = True, f"type '{commit_type}' is user-facing by default"
    elif commit_type is None:
        include, reason = False, "title doesn't follow conventional-commit format; skipped by default"
    else:
        include, reason = False, f"type '{commit_type}' is excluded by default and has no 'Changelog:' opt-in"

    if breaking:
        section = "Changed"
    else:
        section = TYPE_TO_SECTION.get(commit_type, "Changed")

    return {
        "type": commit_type,
        "description_from_title": desc.strip(),
        "breaking": breaking,
        "opt_in": opt_in,
        "opt_in_text": opt_in_text,
        "include": include,
        "reason": reason,
        "suggested_section": section,
    }


def find_existing_pr_refs(changelog_text):
    """Pull every PR number already referenced in the Unreleased section,
    so we never add a duplicate entry for the same PR."""
    m = re.search(r"##\s*\[Unreleased\]", changelog_text, re.IGNORECASE)
    if not m:
        return set(), ""
    rest = changelog_text[m.end():]
    next_heading = re.search(r"\n##\s+\[", rest)
    section = rest[: next_heading.start()] if next_heading else rest
    refs = set(int(n) for n in re.findall(r"#(\d+)", section))
    refs |= set(int(n) for n in re.findall(r"/pull/(\d+)", section))
    return refs, section.strip()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="origin/release")
    ap.add_argument("--head", default="origin/main")
    ap.add_argument("--changelog", default="CHANGELOG.md")
    args = ap.parse_args()

    try:
        with open(args.changelog, encoding="utf-8") as f:
            changelog_text = f.read()
        changelog_exists = True
    except FileNotFoundError:
        changelog_text = ""
        changelog_exists = False

    existing_refs, unreleased_section_text = find_existing_pr_refs(changelog_text)

    pr_numbers = find_pr_numbers(args.base, args.head)

    candidates = []
    for number in pr_numbers:
        try:
            pr = fetch_pr(number)
        except RuntimeError as e:
            candidates.append({"number": number, "error": str(e)})
            continue

        if pr.get("state") != "MERGED":
            continue

        entry = classify(pr)
        entry["number"] = number
        entry["url"] = pr["url"]
        entry["title"] = pr["title"]
        entry["body"] = pr.get("body") or ""

        is_duplicate = number in existing_refs
        if is_duplicate:
            entry["include"] = False
            entry["reason"] = "already present in [Unreleased] section"
        entry["duplicate"] = is_duplicate

        linked_issues = []
        for ref in pr.get("closingIssuesReferences") or []:
            try:
                issue = fetch_issue(ref["number"])
                linked_issues.append(issue)
            except RuntimeError:
                pass
        entry["linked_issues"] = linked_issues

        candidates.append(entry)

    result = {
        "changelog_exists": changelog_exists,
        "changelog_path": args.changelog,
        "existing_pr_refs_in_unreleased": sorted(existing_refs),
        "unreleased_section_text": unreleased_section_text,
        "candidates": candidates,
        "to_add": [c for c in candidates if c.get("include")],
        "skipped": [c for c in candidates if not c.get("include")],
    }
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    sys.exit(main())
