#!/usr/bin/env python3
"""Mechanical pre-pass for a branch review: what the diff touches and which repo rules it trips.

Everything here is a cheap, deterministic signal read from `git diff` — a HINT list for the
reviewer, never a verdict. Each hit still has to be read in context before it becomes a finding.

Usage (from the repo root, server/):
    python .claude/skills/review-branch/scan.py [--base <ref>] [--head <ref>] [--worktree]

--base      ref to compare against; default: the candidate with the fewest commits ahead of it
            (origin/release/* newest first, origin/develop, origin/master)
--head      default HEAD
--worktree  compare the base against the working tree (uncommitted changes included)
"""

from __future__ import annotations

import argparse
import fnmatch
import re
import subprocess
import sys
from collections import defaultdict
from pathlib import Path

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

RULES_DIR = Path(".claude/rules")
LICENSE_MARK = "Copyright (C) Ascensio System SIA"


def git(*args: str, check: bool = True) -> str:
    res = subprocess.run(["git", *args], capture_output=True, text=True, encoding="utf-8", errors="replace")
    if check and res.returncode != 0:
        raise SystemExit(f"git {' '.join(args)} failed: {res.stderr.strip()}")
    return res.stdout


def git_bytes(*args: str) -> bytes | None:
    res = subprocess.run(["git", *args], capture_output=True)
    return res.stdout if res.returncode == 0 else None


# --- base detection ------------------------------------------------------------------------------

def pick_base(head: str) -> str:
    remotes = git("branch", "-r", "--format=%(refname:short)").split()
    releases = sorted((r for r in remotes if r.startswith("origin/release/")), reverse=True)
    candidates = releases + [r for r in ("origin/develop", "origin/master") if r in remotes]
    best, best_count = None, None
    for c in candidates:
        out = git("rev-list", "--count", f"{c}..{head}", check=False).strip()
        if not out.isdigit():
            continue
        n = int(out)
        if best_count is None or n < best_count:
            best, best_count = c, n
    if best is None:
        raise SystemExit("cannot detect a base branch; pass --base")
    return best


# --- rule applicability (frontmatter `paths:` globs) ---------------------------------------------

def glob_to_regex(glob: str) -> re.Pattern[str]:
    out, i = [], 0
    while i < len(glob):
        if glob.startswith("**/", i):
            out.append("(?:.*/)?")
            i += 3
        elif glob.startswith("**", i):
            out.append(".*")
            i += 2
        elif glob[i] == "*":
            out.append("[^/]*")
            i += 1
        else:
            out.append(re.escape(glob[i]))
            i += 1
    return re.compile("^" + "".join(out) + "$")


def load_rules() -> dict[str, list[re.Pattern[str]]]:
    rules: dict[str, list[re.Pattern[str]]] = {}
    for f in sorted(RULES_DIR.glob("*.md")):
        text = f.read_text(encoding="utf-8")
        m = re.match(r"---\s*\n(.*?)\n---", text, re.S)
        globs = re.findall(r'-\s*"([^"]+)"', m.group(1)) if m else []
        rules[f.name] = [glob_to_regex(g) for g in globs]  # [] = always-on rule
    return rules


# --- diff parsing --------------------------------------------------------------------------------

def added_lines(diff_args: list[str]) -> dict[str, list[tuple[int, str]]]:
    """path -> [(new line number, text)] for every added line."""
    out = git("diff", "-U0", "--no-color", "--no-ext-diff", *diff_args)
    result: dict[str, list[tuple[int, str]]] = defaultdict(list)
    path, line_no = None, 0
    for raw in out.splitlines():
        if raw.startswith("+++ "):
            p = raw[4:]
            path = None if p == "/dev/null" else p[2:] if p.startswith("b/") else p
        elif raw.startswith("@@"):
            m = re.search(r"\+(\d+)", raw)
            line_no = int(m.group(1)) if m else 0
        elif raw.startswith("+") and path:
            result[path].append((line_no, raw[1:].rstrip("\r")))
            line_no += 1
    return result


# --- checks on added lines -----------------------------------------------------------------------
# (rule file, label, regex, path filter)

def is_cs(p: str) -> bool:
    return p.endswith(".cs") and not p.startswith(("migrations/", "sdk/"))


def is_test(p: str) -> bool:
    return bool(re.match(r"(products/[^/]+/(Tests|ASC\.\w+\.Tests)/|common/Tests/|web/ASC\.Web\.Api\.Tests/)", p))


LINE_CHECKS = [
    ("csharp-style.md", "using directive outside GlobalUsings.cs",
     re.compile(r"^using\s+(static\s+)?[A-Za-z_][\w.]*(\s*=\s*[\w.<>, ]+)?;\s*$"),
     lambda p: is_cs(p) and not p.endswith("GlobalUsings.cs")),
    ("csharp-style.md", "block-scoped namespace (file-scoped `namespace X;` required)",
     re.compile(r"^namespace\s+[\w.]+\s*(\{.*)?$"), is_cs),
    ("csharp-style.md", "`default(T)` instead of `default`",
     re.compile(r"\bdefault\(\s*[A-Za-z_][\w.<>,? ]*\)"), is_cs),
    ("csharp-style.md", "`ReferenceEquals` instead of `is null`",
     re.compile(r"ReferenceEquals\("), is_cs),
    ("csharp-style.md", "`new JsonSerializerOptions` outside a static readonly field",
     re.compile(r"new\s+JsonSerializerOptions\b"), is_cs),
    ("csharp-style.md", "sync-over-async",
     re.compile(r"\.GetAwaiter\(\)\.GetResult\(\)|\.Wait\(\)|\.Result\b(?!s)"), is_cs),
    ("csharp-style.md", "`async void`",
     re.compile(r"\basync\s+void\b"), is_cs),
    ("csharp-style.md", "route segment not camelCase (snake/kebab)",
     re.compile(r'\[(Http(Get|Post|Put|Delete|Patch)|Route)\("[^"]*[a-z0-9][_-][a-z][^"]*"'), is_cs),
    ("logging.md", "direct ILogger.Log* call ([LoggerMessage] required)",
     re.compile(r"\.Log(Trace|Debug|Information|Warning|Error|Critical)\s*\(|\b_?logger\.Log\s*\("), is_cs),
    ("logging.md", "string interpolation in a log call",
     re.compile(r"\.Log\w*\(\s*\$\""), is_cs),
    ("http-clients.md", "`new HttpClient(` (IHttpClientFactory required)",
     re.compile(r"new\s+HttpClient\s*\("), is_cs),
    ("http-clients.md", "new named HttpClient registered",
     re.compile(r"\.AddHttpClient\s*\("), is_cs),
    ("caching.md", "hand-rolled cache / IMemoryCache / IDistributedCache",
     re.compile(r"\bIMemoryCache\b|\bIDistributedCache\b|static\s+(readonly\s+)?ConcurrentDictionary<|\bLazy<"), is_cs),
    ("caching.md", "bare RemoveAsync/RemoveByTagAsync - on the memory cache it clears the local node only",
     re.compile(r"\.Remove(ByTag)?Async\s*\("), is_cs),
    ("caching.md", "* GetOrSet/Set - check explicit duration and tenantId in key/tag",
     re.compile(r"\.(GetOrSet|Set)Async\s*[<(]"), is_cs),
    ("caching.md", "cache tag built inline (tags come from CacheExtention only)",
     re.compile(r'[Tt]ags?\s*[:=].*\$"'), is_cs),
    ("tests.md", "Skip in a test (open bug = [Trait(\"Bug\")] + red test, not Skip)",
     re.compile(r"\[(Fact|Theory)\([^)]*\bSkip\s*=|Assert\.Skip|SkipWhen|SkipUnless"), lambda p: is_cs(p) and is_test(p)),
    ("tests.md", "Thread.Sleep in a test",
     re.compile(r"Thread\.Sleep\("), lambda p: is_cs(p) and is_test(p)),
    ("tests.md", "* Task.Delay in a test - fixed pause or deadline loop?",
     re.compile(r"Task\.Delay\("), lambda p: is_cs(p) and is_test(p)),
    ("tests.md", "* raw HTTP in a test - allowed only under a carve-out",
     re.compile(r"RawApiClient|\.(Get|Post|Put|Delete|Send)Async\(\s*\$?\"api/"), lambda p: is_cs(p) and is_test(p)),
    ("tests.md", "CancellationToken.None in a test (TestContext.Current.CancellationToken required)",
     re.compile(r"CancellationToken\.None"), lambda p: is_cs(p) and is_test(p)),
    ("openapi-endpoint-docs.md", "* <summary>/<remarks> on actions - check against the rule",
     re.compile(r"///\s*<(summary|remarks)>"), lambda p: p.endswith("Controller.cs")),
    ("openapi-endpoint-docs.md", "requiresAuthorization=false - only together with [AllowAnonymous]",
     re.compile(r"<requiresAuthorization>\s*false"), lambda p: p.endswith(".cs")),
    ("openapi-dto-docs.md", "placeholder <example>",
     re.compile(r"<example>\s*(string|item1|test|example|value)?\s*</example>", re.I),
     lambda p: p.endswith(".cs")),
    ("openapi-dto-docs.md", "unknown/misspelled XML doc tag",
     re.compile(r"^\s*///\s*</?(?!(?:summary|remarks|example|param|returns|see|seealso|c|code|para|paramref|typeparam|typeparamref|inheritdoc|list|item|term|description|value|exception|path|collection|requiresAuthorization|br|b|i|a|short)\b)\w+[\s/>]"),
     lambda p: p.endswith(".cs")),
    ("csharp-style.md", "EnforceCodeStyleInBuild committed to a project file",
     re.compile(r"EnforceCodeStyleInBuild"), lambda p: p.endswith((".csproj", ".props", ".targets"))),
    ("CLAUDE.md", "TargetFramework in csproj (TFM lives in Directory.Packages.props)",
     re.compile(r"<TargetFrameworks?>"), lambda p: p.endswith(".csproj")),
    ("CLAUDE.md", "package version in csproj (versions live in Directory.Packages.props)",
     re.compile(r"<PackageReference[^>]*\bVersion="), lambda p: p.endswith(".csproj")),
    # security / performance: not rule files - hints for reviewers E and D, product code only
    ("security", "[AllowAnonymous] - is anonymous access intended?",
     re.compile(r"\[AllowAnonymous\]"), lambda p: is_cs(p) and not is_test(p)),
    ("security", "raw SQL - check for interpolated user input",
     re.compile(r"FromSqlRaw|ExecuteSqlRaw|SqlQueryRaw|FromSqlInterpolated|ExecuteSqlInterpolated"),
     lambda p: is_cs(p) and not is_test(p)),
    ("security", "unsafe deserialization",
     re.compile(r"TypeNameHandling\.(All|Auto|Objects|Arrays)|BinaryFormatter|NetDataContractSerializer"),
     lambda p: is_cs(p) and not is_test(p)),
    ("security", "certificate validation overridden",
     re.compile(r"ServerCertificateCustomValidationCallback|DangerousAcceptAnyServerCertificateValidator|RemoteCertificateValidationCallback"),
     lambda p: is_cs(p) and not is_test(p)),
    ("security", "weak crypto / non-crypto random - fine only if not security-relevant",
     re.compile(r"\bMD5\b|\bSHA1\b|new\s+Random\s*\(|Random\.Shared"), lambda p: is_cs(p) and not is_test(p)),
    ("security", "secret-looking value in a log template",
     re.compile(r"LoggerMessage\(.*\{[^}]*(password|passwd|token|secret|apikey|api_key|privatekey)[^}]*\}", re.I),
     lambda p: is_cs(p) and not is_test(p)),
    ("security", "* Path.Combine / file name from input - path traversal?",
     re.compile(r"Path\.(Combine|Join|GetFullPath)\s*\(|ZipArchive|ExtractToDirectory"),
     lambda p: is_cs(p) and not is_test(p)),
    ("security", "process start / raw HTML output",
     re.compile(r"Process\.Start\s*\(|Html\.Raw\s*\(|\.innerHTML\s*="), lambda p: (is_cs(p) or p.endswith(".ts")) and not is_test(p)),
    ("security", "outbound request - user-supplied URL must go through UrlValidator.PinnedHttpClient",
     re.compile(r"\.(GetAsync|PostAsync|SendAsync|GetStreamAsync|GetStringAsync)\s*\(\s*(url|uri|link|address|endpoint)\b", re.I),
     lambda p: is_cs(p) and not is_test(p)),
    ("performance", "materialized then filtered / Count() for existence",
     re.compile(r"\.ToList(Async)?\(\)\s*\.\s*(Where|Count|Any|First|Select)\b|\.Count\(\)\s*(>|!=|==)\s*0"),
     lambda p: is_cs(p) and not is_test(p)),
    ("performance", "per-call Regex / Enum.GetValues - hoist into a static",
     re.compile(r"new\s+Regex\s*\(|Enum\.GetValues"), lambda p: is_cs(p) and not is_test(p)),
    ("performance", "* reflection - is it per item?",
     re.compile(r"\.GetProperty\(|\.GetProperties\(|PropertyInfo|\.GetValue\(|Activator\.CreateInstance"),
     lambda p: is_cs(p) and not is_test(p)),
    ("ts-ai-style.md", "`any` / console.* in ASC.AI.Chat",
     re.compile(r":\s*any\b|\bas\s+any\b|console\.(log|error|warn|info)"), lambda p: p.endswith(".ts")),
]


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--base")
    ap.add_argument("--head", default="HEAD")
    ap.add_argument("--worktree", action="store_true")
    a = ap.parse_args()

    base = a.base or pick_base(a.head)
    merge_base = git("merge-base", base, a.head).strip()
    diff_args = [merge_base] if a.worktree else [f"{merge_base}..{a.head}"]
    head_label = "working tree" if a.worktree else a.head

    branch = git("rev-parse", "--abbrev-ref", "HEAD").strip()
    commits = git("log", "--oneline", "--no-merges", f"{merge_base}..{a.head}").splitlines()
    merges = git("log", "--oneline", "--merges", f"{merge_base}..{a.head}").splitlines()
    status = git("status", "--porcelain", "--untracked-files=no").splitlines()

    print(f"# Branch scan: {branch}")
    print(f"base: {base}  merge-base: {merge_base[:10]}  head: {head_label}")
    print(f"commits: {len(commits)} (+{len(merges)} merge)")
    for c in commits[:40]:
        print(f"  {c}")
    if len(commits) > 40:
        print(f"  ... and {len(commits) - 40} more")
    if status and not a.worktree:
        print(f"!! working tree is dirty ({len(status)} files) - uncommitted changes are NOT scanned; pass --worktree to include them")
    print()

    # --- files ---------------------------------------------------------------------------------
    name_status = [l.split("\t") for l in git("diff", "--name-status", "-M", *diff_args).splitlines() if l]
    numstat = {}
    for l in git("diff", "--numstat", "-M", *diff_args).splitlines():
        parts = l.split("\t")
        if len(parts) == 3:
            numstat[parts[2]] = (parts[0], parts[1])
    files = [(ns[0], ns[-1]) for ns in name_status]

    areas: dict[str, list[str]] = defaultdict(list)
    for st, p in files:
        top = "/".join(p.split("/")[:2]) if p.count("/") >= 1 else p
        areas[top].append(f"{st[0]} {p}")
    print(f"## Files: {len(files)}")
    for top in sorted(areas):
        print(f"  [{top}] {len(areas[top])}")
    print()

    # --- applicable rules ------------------------------------------------------------------------
    rules = load_rules()
    applies: dict[str, int] = defaultdict(int)
    for _, p in files:
        for name, pats in rules.items():
            if pats and any(pt.match(p) for pt in pats):
                applies[name] += 1
    print("## Applicable rules (by `paths:` in their frontmatter)")
    for name, pats in rules.items():
        if not pats:
            print(f"  {name}: always")
        elif applies.get(name):
            print(f"  {name}: {applies[name]} file(s)")
    print()

    # --- line checks ---------------------------------------------------------------------------
    added = added_lines(diff_args)
    hits: dict[tuple[str, str], list[str]] = defaultdict(list)
    for path, lines in added.items():
        for rule, label, rx, flt in LINE_CHECKS:
            if not flt(path):
                continue
            for n, text in lines:
                s = text.strip()
                if s.startswith("//") and not s.startswith("///"):
                    continue
                if rx.search(text):
                    if label.startswith("`new JsonSerializerOptions`") and "static readonly" in text:
                        continue
                    hits[(rule, label)].append(f"{path}:{n}  {s[:140]}")

    # await inside a loop body (N+1 candidate): a loop header among the added lines, then an
    # `await` on a contiguous added line indented deeper than the header
    for path, lines in added.items():
        if not is_cs(path) or is_test(path):
            continue
        loop_indent, loop_line, prev_n = None, 0, -2
        for n, text in lines:
            if n != prev_n + 1:
                loop_indent = None
            prev_n = n
            if not text.strip() or text.strip() == "{":
                continue
            indent = len(text) - len(text.lstrip())
            if loop_indent is not None and indent <= loop_indent:
                loop_indent = None
            if re.match(r"\s*(await\s+)?(foreach|for|while)\s*\(", text):
                loop_indent, loop_line = indent, n
                continue
            if loop_indent is not None and re.search(r"\bawait\b", text):
                hits[("performance", "* await inside a loop - N+1 round-trips?")].append(f"{path}:{loop_line}")
                loop_indent = None

    # license header on new source files
    for st, p in files:
        if st.startswith("A") and p.endswith((".cs", ".ts")) and not p.startswith(("migrations/", "sdk/")):
            blob = git_bytes("show", f"{a.head}:{p}") if not a.worktree else (Path(p).read_bytes() if Path(p).exists() else None)
            if blob is not None and LICENSE_MARK not in blob[:600].decode("utf-8", "replace"):
                hits[("csharp-style.md", "new file without the AGPL header")].append(p)

    print("## Rule signals (hints - verify each in context)")
    if not hits:
        print("  none")
    for (rule, label), items in sorted(hits.items()):
        print(f"  [{rule}] {label.removeprefix('* ')}: {len(items)}")
        if label.startswith("* "):
            per_file = defaultdict(int)
            for it in items:
                per_file[it.split(":")[0]] += 1
            items = [f"{f}  ({n})" for f, n in sorted(per_file.items())]
        for it in items[:15]:
            print(f"    {it}")
        if len(items) > 15:
            print(f"    ... and {len(items) - 15} more")
    print()

    # --- whole-file EOL flips and whitespace --------------------------------------------------
    print("## Line-ending flips (whole file switched LF<->CRLF)")
    flips = []
    for st, p in files:
        if not st.startswith(("M", "R")):
            continue
        old_path = p
        if st.startswith("R"):
            old_path = next(ns[1] for ns in name_status if ns[-1] == p)
        old = git_bytes("show", f"{merge_base}:{old_path}")
        new = Path(p).read_bytes() if a.worktree and Path(p).exists() else git_bytes("show", f"{a.head}:{p}")
        if not old or not new or b"\0" in old[:8000]:
            continue

        def style(b: bytes) -> str:
            crlf, lf = b.count(b"\r\n"), b.count(b"\n")
            if lf == 0:
                return "-"
            return "CRLF" if crlf / lf > 0.9 else "LF" if crlf / lf < 0.1 else "mixed"

        so, sn = style(old), style(new)
        if so != sn and "-" not in (so, sn):
            add, rem = numstat.get(p, ("?", "?"))
            flips.append(f"  {p}: {so} -> {sn}  (+{add}/-{rem} in the diff)")
    print("\n".join(flips) if flips else "  none")
    print()

    check = git("diff", "--check", *diff_args, check=False).splitlines()
    ws = [l for l in check if not l.startswith(("+", "-"))]
    print(f"## git diff --check (trailing whitespace, conflict markers): {len(ws)}")
    for l in ws[:15]:
        print(f"  {l}")
    print()

    # --- migrations ----------------------------------------------------------------------------
    mig = [p for st, p in files if p.startswith("migrations/")]
    print("## Migrations")
    if not mig:
        print("  none")
    else:
        variants = defaultdict(list)
        for p in mig:
            parts = p.split("/")
            variants["/".join(parts[1:3])].append(p)
        for v in ("mysql/SaaS", "mysql/Standalone", "postgre/SaaS", "postgre/Standalone"):
            new = [x for x in variants.get(v, []) if re.search(r"\d{14}_.*\.cs$", x) and not x.endswith(".Designer.cs")]
            print(f"  {v}: {len(variants.get(v, []))} file(s); new migrations: {', '.join(Path(x).name for x in new) or '-'}")
        renamed = [ns for ns in name_status if ns[0].startswith(("R", "D")) and ns[1].startswith("migrations/")]
        for ns in renamed:
            print(f"  !! {ns[0]} {' -> '.join(ns[1:])}  (migration renamed/deleted)")
    print()

    # --- contract / SDK / submodules -------------------------------------------------------------
    contract = [p for _, p in files if re.search(r"(Controller\.cs|Dto\.cs|/ApiModels/)", p)]
    sdk_touched = [p for _, p in files if p.startswith("sdk/")]
    print("## API contract and SDK")
    print(f"  contract files (controllers/DTOs/ApiModels): {len(contract)}")
    print(f"  sdk/ submodule pointers in the branch: {', '.join(sdk_touched) or 'unchanged'}")
    sub = [l for l in git("submodule", "status").splitlines() if l[:1] in "+-U"]
    for l in sub:
        print(f"  !! submodule checkout differs from the recorded pointer: {l.strip()}")
    print()

    # --- tests ---------------------------------------------------------------------------------
    test_files = [p for _, p in files if is_test(p)]
    prod_cs = [p for _, p in files if is_cs(p) and not is_test(p)]
    bugs = sorted({m for lines in added.values() for _, t in lines for m in re.findall(r'Trait\("Bug",\s*"(\d+)"', t)})
    print("## Tests")
    print(f"  product .cs: {len(prod_cs)}, test files: {len(test_files)}")
    if bugs:
        print(f"  new [Trait(\"Bug\")]: {', '.join(bugs)}")
    for p in test_files[:20]:
        print(f"    {p}")
    print()

    # --- junk ----------------------------------------------------------------------------------
    junk = [p for st, p in files if st.startswith("A") and (
        re.search(r"(\.binlog|\.log|\.env|\.user|\.orig|\.rej|launchSettings\.local\.json)$", p)
        or ("/" not in p and p.endswith(".md") and p not in ("README.md", "CLAUDE.md")))]
    print("## Possibly committed by accident")
    print("\n".join(f"  {p}" for p in junk) if junk else "  none")


if __name__ == "__main__":
    main()
