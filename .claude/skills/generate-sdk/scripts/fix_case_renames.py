"""Find (and optionally fix) generated files whose tracked name differs from the expected one only by case.

When a schema is renamed by case alone (IPRestrictionArrayWrapper -> IpRestrictionArrayWrapper) and the
generator runs on a case-insensitive file system (Windows, default macOS) with core.ignorecase=true, the
generator rewrites the old file in place: the content carries the new name, the file keeps the old one,
and git sees only a content change. Java then fails on Linux CI with "class X is public, should be
declared in a file named X.java", PHP's PSR-4 autoloader cannot find the class, and docs links break.

Two signals decide the expected name:
  * the primary type declared in a source file (.java, .kt, .cs, .swift, .php) must match its file name;
  * a relative Markdown link (README.md, docs/*.md) must match the file it points to exactly.

Usage:
  python fix_case_renames.py <sdk dir> [<sdk dir> ...]          # report only
  python fix_case_renames.py --apply <sdk dir> [<sdk dir> ...]  # git mv to the expected case

Each <sdk dir> must be the root of its own git repository (a submodule); anything else is skipped.
Exit code: 0 when nothing is left to fix, 1 when mismatches remain (report mode), 2 on a conflict.
"""
import os
import re
import subprocess
import sys

TYPE_RE = {
    ".java": re.compile(r"^public\s+(?:final\s+|abstract\s+)*(?:class|interface|enum|record)\s+(\w+)", re.M),
    ".kt": re.compile(r"^(?:data\s+|enum\s+|open\s+|sealed\s+)*(?:class|interface|object)\s+(\w+)", re.M),
    ".cs": re.compile(r"^\s*public\s+(?:partial\s+|abstract\s+|sealed\s+|static\s+)*(?:class|interface|enum|struct|record)\s+(\w+)", re.M),
    ".swift": re.compile(r"^(?:public\s+|open\s+)?(?:final\s+)?(?:class|struct|enum)\s+(\w+)", re.M),
    ".php": re.compile(r"^(?:final\s+|abstract\s+)?(?:class|interface|enum)\s+(\w+)", re.M),
}
LINK_RE = re.compile(r"\]\(([^)\s#]+\.md)(?:#[^)]*)?\)")


def git(repo, *args):
    return subprocess.run(["git", "-C", repo, *args], capture_output=True, text=True, encoding="utf-8")


def read(path):
    with open(path, encoding="utf-8", errors="replace") as fh:
        return fh.read()


def scan(repo):
    files = git(repo, "ls-files").stdout.splitlines()
    exact = set(files)
    by_lower = {}
    for f in files:
        by_lower.setdefault(f.lower(), []).append(f)

    renames, conflicts = {}, []

    def propose(src, dst):
        prev = renames.get(src)
        if prev and prev != dst:
            conflicts.append(f"{src}: {prev} vs {dst}")
        renames[src] = dst

    for f in files:
        ext = os.path.splitext(f)[1]
        full = os.path.join(repo, f)
        if not os.path.isfile(full):
            continue
        if ext in TYPE_RE:
            m = TYPE_RE[ext].search(read(full))
            base = os.path.splitext(os.path.basename(f))[0]
            if m and m.group(1) != base and m.group(1).lower() == base.lower():
                propose(f, f"{os.path.dirname(f)}/{m.group(1)}{ext}".lstrip("/"))
        elif ext == ".md":
            for link in LINK_RE.findall(read(full)):
                if "://" in link:
                    continue
                target = os.path.normpath(os.path.join(os.path.dirname(f), link)).replace("\\", "/")
                if target in exact:
                    continue
                candidates = by_lower.get(target.lower(), [])
                if len(candidates) == 1:
                    propose(candidates[0], target)

    return renames, conflicts


def main(argv):
    apply = "--apply" in argv
    dirs = [a for a in argv if a != "--apply"]
    if not dirs:
        print(__doc__)
        return 2

    left, conflicted = 0, False
    for d in dirs:
        top = git(d, "rev-parse", "--show-toplevel").stdout.strip()
        if not top or os.path.normcase(os.path.realpath(top)) != os.path.normcase(os.path.realpath(d)):
            print(f"== {d}: skipped, not a repository root")
            continue

        renames, conflicts = scan(d)
        print(f"== {d}: {len(renames)} case mismatch(es)")
        for c in conflicts:
            print(f"  CONFLICT {c}")
        if conflicts:
            conflicted = True
            continue
        for src, dst in sorted(renames.items()):
            print(f"  {src} -> {dst}")
            if apply:
                r = git(d, "mv", "-f", src, dst)
                if r.returncode != 0:
                    print(f"  git mv failed: {r.stderr.strip()}")
                    left += 1
            else:
                left += 1

    if conflicted:
        return 2
    return 1 if left else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
