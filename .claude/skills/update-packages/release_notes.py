"""Print the GitHub release notes of a NuGet package between two versions.

Usage (from the repo root):
  python -I .claude/skills/update-packages/release_notes.py PACKAGE FROM TO [--repo OWNER/NAME]
         [--tag-prefix PREFIX] [--changelog PATH] [--out FILE] [--keep-noise] [--max-pages N]

  PACKAGE   NuGet id; its nuspec <repository>/<projectUrl> names the GitHub repo unless --repo is given.
  FROM TO   Releases with FROM < version <= TO are printed, newest first (as GitHub lists them).
            Versions compare per SemVer 2: prerelease identifiers numerically where numeric
            (beta.9 < beta.10).

  --tag-prefix  Only tags starting with it, for monorepos that tag per component:
                opentelemetry-dotnet core "core-", contrib "Instrumentation.AspNetCore-".
  --changelog   Print the section of a CHANGELOG.md (path in the repo, default branch) between the two
                versions instead of releases; for repos whose releases are empty or shared
                (Scalar: integrations/dotnet/aspnetcore/CHANGELOG.md).
  --keep-noise  Keep dependency-bump / CI lines, which are dropped by default. Lines that mention
                breaking / drop / remove / obsolete / minimum / security / CVE / GHSA are never dropped.
  --max-pages   Pages of 100 releases to read at most (default 20). A WARNING is printed when the
                limit stopped the scan before FROM was reached - the output is then incomplete.

GitHub API: unauthenticated 60 requests per hour per IP, one request per 100 releases. When the
GITHUB_TOKEN environment variable is set it is sent as a Bearer token (5000 requests per hour).
"""

import argparse
import json
import os
import re
import sys
import urllib.error
import urllib.request

sys.stdout.reconfigure(encoding="utf-8")
sys.stderr.reconfigure(encoding="utf-8")

NOISE = re.compile(r"dependabot|renovate|chore\(deps|update dependency|^\s*[*-]\s*(chore|build|ci)\b|bump |"
                   r"new contributors|made their first contribution", re.I)
# A line that matches NOISE but says one of these is kept: "Breaking change: bump minimum .NET",
# "build: drop .NET Framework support", "bump X to fix CVE-...".
KEEP = re.compile(r"breaking|\bdrop|\bremov|obsolete|deprecat|minimum|security|vulnerab|\bCVE-|GHSA-", re.I)


def get(url, raw=False):
    headers = {"User-Agent": "docspace-update-packages"}
    token = os.environ.get("GITHUB_TOKEN")
    if token and url.startswith("https://api.github.com/"):
        headers["Authorization"] = f"Bearer {token}"
    req = urllib.request.Request(url, headers=headers)
    with urllib.request.urlopen(req, timeout=30) as r:
        data = r.read().decode("utf-8-sig")
    return data if raw else json.loads(data)


def parse_version(text):
    """Sortable SemVer 2 key: numeric core, then prerelease identifiers (stable sorts last)."""
    text = text.split("+")[0]
    core, _, pre = text.partition("-")
    nums = tuple(int(p) if p.isdigit() else 0 for p in core.split("."))
    nums = nums + (0,) * (4 - len(nums))
    if not pre:
        return nums, (1,)
    # Numeric identifiers sort numerically and before alphanumeric ones; a shorter prefix sorts first.
    parts = tuple((0, int(p), "") if p.isdigit() else (1, 0, p.lower()) for p in pre.split("."))
    return nums, (0,) + parts


def version_in(tag, prefix):
    if prefix and not tag.startswith(prefix):
        return None
    m = re.search(r"(\d+(?:\.\d+){1,3}(?:-[0-9A-Za-z.\-]+)?)$", tag[len(prefix or ""):])
    return m.group(1) if m else None


def repo_of(package, version):
    nuspec = get(f"https://api.nuget.org/v3-flatcontainer/{package.lower()}/{version.lower()}/{package.lower()}.nuspec", raw=True)
    # Repository names may contain dots (dlemstra/Magick.NET), so only a trailing ".git" is cut.
    for pattern in (r'<repository[^>]*url="https?://github\.com/([^/"]+/[^/"#?]+)', r"<projectUrl>https?://github\.com/([^/<]+/[^/<#?]+)"):
        m = re.search(pattern, nuspec)
        if m:
            return re.sub(r"\.git$", "", m.group(1).rstrip("/"))
    return None


def denoise(body):
    """Drop dependency-bump / CI lines unless they carry a KEEP word; collapse runs of blank lines."""
    out = []
    for line in body.splitlines():
        if NOISE.search(line) and not KEEP.search(line):
            continue
        if not line.strip() and (not out or not out[-1].strip()):
            continue
        out.append(line.rstrip())
    while out and not out[-1].strip():
        out.pop()
    return "\n".join(out)


def changelog_section(repo, path, lo, hi):
    meta = get(f"https://api.github.com/repos/{repo}")
    text = get(f"https://raw.githubusercontent.com/{repo}/{meta['default_branch']}/{path}", raw=True)
    out, keep = [], False
    for line in text.splitlines():
        m = re.match(r"^#{1,3}\s*\[?v?(\d+(?:\.\d+){1,3}(?:-[0-9A-Za-z.\-]+)?)", line)
        if m:
            v = parse_version(m.group(1))
            keep = parse_version(lo) < v <= parse_version(hi)
        if keep:
            out.append(line)
    return "\n".join(out)


def releases_between(repo, prefix, lo, hi, keep_noise, max_pages):
    chunks, warnings = [], []
    lo_key, hi_key = parse_version(lo), parse_version(hi)
    reached, oldest_seen, page = False, None, 0
    for page in range(1, max_pages + 1):
        releases = get(f"https://api.github.com/repos/{repo}/releases?per_page=100&page={page}")
        for r in releases:
            v = version_in(r["tag_name"], prefix)
            if not v:
                continue
            key = parse_version(v)
            if oldest_seen is None or key < parse_version(oldest_seen):
                oldest_seen = v
            if key <= lo_key:
                reached = True
            if lo_key < key <= hi_key:
                body = r.get("body") or ""
                if not keep_noise:
                    body = denoise(body)
                chunks.append(f"===== {r['tag_name']} ({(r.get('published_at') or '')[:10]})\n{body}")
        if len(releases) < 100 or reached:
            break
    else:
        if not reached:
            warnings.append(f"WARNING: stopped after {max_pages} pages ({max_pages * 100} releases) without reaching "
                            f"{lo} (oldest matching tag read: {oldest_seen}). The notes below are INCOMPLETE - "
                            f"rerun with a larger --max-pages, a --tag-prefix, or --changelog.")
    if not reached and not warnings and chunks:
        warnings.append(f"Note: no release at or below {lo} exists in {repo}"
                        + (f" with tag prefix {prefix!r}" if prefix else "")
                        + f"; the oldest matching tag is {oldest_seen}. Older notes may live elsewhere.")
    return chunks, warnings


def main(argv):
    ap = argparse.ArgumentParser(description="Print the GitHub release notes of a NuGet package between two versions.")
    ap.add_argument("package")
    ap.add_argument("lo", metavar="FROM")
    ap.add_argument("hi", metavar="TO")
    ap.add_argument("--repo")
    ap.add_argument("--tag-prefix")
    ap.add_argument("--changelog")
    ap.add_argument("--out")
    ap.add_argument("--keep-noise", action="store_true")
    ap.add_argument("--max-pages", type=int, default=20)
    args = ap.parse_args(argv)

    repo = args.repo or repo_of(args.package, args.hi)
    if not repo:
        print(f"No GitHub repository in the nuspec of {args.package} {args.hi}; pass --repo.")
        return 1

    warnings = []
    if args.changelog:
        text = changelog_section(repo, args.changelog, args.lo, args.hi)
        if not args.keep_noise:
            text = denoise(text)
    else:
        prefix = args.tag_prefix
        chunks, warnings = releases_between(repo, prefix, args.lo, args.hi, args.keep_noise, args.max_pages)
        text = "\n\n".join(chunks) or (f"No releases of {repo} between {args.lo} and {args.hi}"
                                        + (f" with tag prefix {prefix!r}" if prefix else "")
                                        + ". Try --tag-prefix or --changelog.")
    header = f"# {args.package} {args.lo} -> {args.hi}  (https://github.com/{repo})\n"
    if warnings:
        header += "\n".join(warnings) + "\n\n"
    for w in warnings:
        print(w, file=sys.stderr)
    if args.out:
        with open(args.out, "w", encoding="utf-8") as f:
            f.write(header + text + "\n")
        print(f"written {args.out} ({len(text)} chars)")
    else:
        print(header + text)
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main(sys.argv[1:]))
    except urllib.error.HTTPError as e:
        limited = "" if os.environ.get("GITHUB_TOKEN") else " (60/hour unauthenticated - set GITHUB_TOKEN for 5000/hour)"
        hint = {403: f"GitHub rate limit{limited} - wait or read the release page in a browser",
                401: "GITHUB_TOKEN rejected - check or unset it",
                404: "wrong repository or path - pass --repo OWNER/NAME or check --changelog"}.get(e.code, "")
        print(f"HTTP {e.code} for {e.url}. {hint}")
        sys.exit(1)
