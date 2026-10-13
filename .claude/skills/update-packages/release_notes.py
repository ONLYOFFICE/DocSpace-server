"""Print the GitHub release notes of a NuGet package between two versions.

Usage (from the repo root):
  python -I .claude/skills/update-packages/release_notes.py PACKAGE FROM TO [--repo OWNER/NAME]
         [--tag-prefix PREFIX] [--changelog PATH] [--out FILE] [--keep-noise]

  PACKAGE   NuGet id; its nuspec <repository>/<projectUrl> names the GitHub repo unless --repo is given.
  FROM TO   Releases with FROM < version <= TO are printed, oldest last (as GitHub lists them).

  --tag-prefix  Only tags starting with it, for monorepos that tag per component:
                opentelemetry-dotnet core "core-", contrib "Instrumentation.AspNetCore-".
  --changelog   Print the section of a CHANGELOG.md (path in the repo, default branch) between the two
                versions instead of releases; for repos whose releases are empty or shared
                (Scalar: integrations/dotnet/aspnetcore/CHANGELOG.md).
  --keep-noise  Keep dependency-bump / CI lines, which are dropped by default.

Unauthenticated GitHub API: 60 requests per hour per IP, one request per 100 releases. `gh` is not
installed on this machine, so there is no token to fall back on; keep the number of calls small.
"""

import json
import re
import sys
import urllib.error
import urllib.request

sys.stdout.reconfigure(encoding="utf-8")

NOISE = re.compile(r"dependabot|renovate|chore\(deps|update dependency|^\s*\*\s*(chore|build|ci)\b|bump |"
                   r"new contributors|made their first contribution|^\s*$", re.I)


def get(url, raw=False):
    req = urllib.request.Request(url, headers={"User-Agent": "docspace-update-packages"})
    with urllib.request.urlopen(req, timeout=30) as r:
        data = r.read().decode("utf-8-sig")
    return data if raw else json.loads(data)


def parse_version(text):
    core, _, pre = text.partition("-")
    nums = tuple(int(p) if p.isdigit() else 0 for p in core.split("."))
    nums = nums + (0,) * (4 - len(nums))
    return nums, (1,) if not pre else (0, pre)


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


def main(argv):
    opts = {}
    for name in ("--repo", "--tag-prefix", "--changelog", "--out"):
        if name in argv:
            i = argv.index(name)
            opts[name] = argv[i + 1]
            del argv[i:i + 2]
    keep_noise = "--keep-noise" in argv
    args = [a for a in argv if not a.startswith("--")]
    if len(args) != 3:
        print(__doc__)
        return 2
    package, lo, hi = args
    repo = opts.get("--repo") or repo_of(package, hi)
    if not repo:
        print(f"No GitHub repository in the nuspec of {package} {hi}; pass --repo.")
        return 1

    if "--changelog" in opts:
        text = changelog_section(repo, opts["--changelog"], lo, hi)
    else:
        prefix = opts.get("--tag-prefix")
        chunks = []
        for page in range(1, 6):
            releases = get(f"https://api.github.com/repos/{repo}/releases?per_page=100&page={page}")
            for r in releases:
                v = version_in(r["tag_name"], prefix)
                if v and parse_version(lo) < parse_version(v) <= parse_version(hi):
                    body = r.get("body") or ""
                    if not keep_noise:
                        body = "\n".join(line for line in body.splitlines() if not NOISE.search(line))
                    chunks.append(f"===== {r['tag_name']} ({(r.get('published_at') or '')[:10]})\n{body}")
            oldest = [version_in(r["tag_name"], prefix) for r in releases]
            oldest = [v for v in oldest if v]
            if len(releases) < 100 or (oldest and min(parse_version(v) for v in oldest) <= parse_version(lo)):
                break
        text = "\n\n".join(chunks) or (f"No releases of {repo} between {lo} and {hi}"
                                        + (f" with tag prefix {prefix!r}" if prefix else "")
                                        + ". Try --tag-prefix or --changelog.")
    header = f"# {package} {lo} -> {hi}  (https://github.com/{repo})\n"
    if "--out" in opts:
        with open(opts["--out"], "w", encoding="utf-8") as f:
            f.write(header + text + "\n")
        print(f"written {opts['--out']} ({len(text)} chars)")
    else:
        print(header + text)
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main(sys.argv[1:]))
    except urllib.error.HTTPError as e:
        hint = {403: "GitHub rate limit (60/hour unauthenticated) - wait or read the release page in a browser",
                404: "wrong repository or path - pass --repo OWNER/NAME or check --changelog"}.get(e.code, "")
        print(f"HTTP {e.code} for {e.url}. {hint}")
        sys.exit(1)
