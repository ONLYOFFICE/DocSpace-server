#!/usr/bin/env python3
"""Mechanical pre-pass for a branch review: what the diff touches and which repo rules it trips.

Everything here is a cheap, deterministic signal read from `git diff` — a HINT list for the
reviewer, never a verdict. Each hit still has to be read in context before it becomes a finding.

Usage (from the repo root, server/):
    python .claude/skills/review-branch/scan.py [--base <ref>] [--head <ref>] [--worktree] [--since-review <sha>] [--full] [--gate]

--base      ref to compare against; default: the candidate with the fewest commits ahead of it
            (origin/release/* newest first, origin/develop, origin/master)
--head      default HEAD
--since-review  also list file changes from the previous reviewed commit to the scanned tree that are in
            the branch's own diff against the merge-base, and the files the branch changed at the review and
            has undone since; what a merge of the base brought after the review is counted, not listed. The
            commit must be an ancestor of the selected head (or the head itself).
--worktree  compare the base against the working tree (uncommitted changes included); only for the
            checked-out branch, so with a --head that names another ref (another branch, even at the same
            commit) it exits 2; with a detached HEAD a --head at that commit is accepted
--full      print the long hint lists (claims with numbers, stale verification.md rows, the texts the diff
            writes, removed response codes, fix commits without a proof) whole instead of their first rows
            and a count
--gate      exit with 1 when the "Gate" section lists a hard failure: AI attribution in a commit; a
            change of the base, since the merge-base, to rules or review tooling that apply to the
            branch; an untracked file (--worktree; the branch's own saved review at the root
            excepted); a requirement the branch adds at 500 characters or more, brings to 500 or
            more, or makes longer while over; a whole-file line-ending flip; a conflict marker; a new source file
            without a license header (generated files and thirdparty/ excepted); a verification.md line
            citation of a changed C# file that the branch wrote wrong or moved; a FORM, STYLE or CODES finding
            of openapi-desc-opt/check_texts.py on a changed operation that the merge-base does not have (also
            for an operation not in the emitted document yet), and a run of that check that gave no answer
            on the scanned tree (not next to the scan, an exit code other than 0 and 1, a crash, a report
            that stops before its end or has no answer for an operation; an action it answers it cannot
            check - its project emits no document, or no published action of that name is in the file - is
            listed, not gated). Exit 0 otherwise; 2 when the scan itself cannot run (git, the arguments, an
            error of the script)

With --worktree the scan reads a commit of what `git commit -a` would record (clean filters applied,
intent-to-add files and moved submodule pointers included), built in a temporary copy of the index; the
header prints its tree id, which a Hand-over block records.
"""

from __future__ import annotations

import argparse
import functools
import fnmatch
import os
import re
import subprocess
import sys
from collections import Counter, defaultdict
from pathlib import Path
from typing import NamedTuple

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
# The scan reads the repository; `git status` would otherwise refresh and rewrite the real index under its lock,
# and an IDE's git call running at the same moment would fail on that lock.
os.environ["GIT_OPTIONAL_LOCKS"] = "0"

RULES_DIR = Path(".claude/rules")
# Hard failures of --gate, collected while the sections are printed.
GATE: list[str] = []
# What a review runs: a base change to one of these that applies to the branch makes a check on the branch's older
# copy see less. CLAUDE.md is left out: its frequent edits are lists, and the rules it points to are covered here.
# The text check of --gate is check_texts.py with what it reads: its index (scan.py), the limits (tools.py and the
# readability/ settings, which it checks against each other) and the plain-word table of description-rules.md §5.3.
CHECK_TEXTS_INPUTS = tuple(f".claude/skills/openapi-desc-opt/{p}" for p in (
    "check_texts.py", "scan.py", "tools.py", "readability/", "references/description-rules.md"))
REVIEW_TOOLING = (".claude/rules", ".claude/skills/review-branch", ".claude/scripts/prove-red.sh",
                  "openspec/config.yaml", *CHECK_TEXTS_INPUTS)
# Rules whose `paths:` say less than where they apply: CLAUDE.md (Git Workflow) has every branch follow
# fixing-findings.md §6 and §9-§12, whatever files the branch changes.
ALWAYS_APPLIES = {".claude/rules/fixing-findings.md"}
# Rules with no `paths:` that govern how the agent works, not what a branch must hold (SKILL.md §3 A): a base change
# to them leaves nothing for a branch to bring in.
NOT_A_REVIEW_CRITERION = {".claude/rules/csharp-lsp.md"}
# The header forms in use: the full AGPL header, the short "(c) Copyright" one, and the SPDX line.
LICENSE_MARKS = ("Copyright (C) Ascensio System SIA", "(c) Copyright Ascensio System SIA", "SPDX-License-Identifier: AGPL")


def tooling_applies(path: str, ref: str, branch_paths: list[str]) -> bool:
    """Whether a rule or review-tooling file, as `ref` holds it, applies to a branch that changes `branch_paths`:
    a rule whose `paths:` match a file of the branch or that has none, fixing-findings.md always, this skill and its
    scan, prove-red.sh, the spec rules when the branch touches openspec/, and the text check of --gate with its
    inputs (CHECK_TEXTS_INPUTS) when the branch changes a controller."""
    if path in NOT_A_REVIEW_CRITERION:
        return False
    if path in ALWAYS_APPLIES:
        return True
    if path.startswith(".claude/rules/") and path.endswith(".md"):
        text = (git_bytes("show", f"{ref}:{path}") or b"").decode("utf-8", "replace")
        m = re.match(r"---\s*\n(.*?)\n---", text, re.S)
        globs = [glob_to_regex(g) for g in re.findall(r'-\s*"([^"]+)"', m.group(1))] if m else []
        return not text or not globs or any(g.match(p) for g in globs for p in branch_paths)
    if path == "openspec/config.yaml":
        return any(p.startswith("openspec/") for p in branch_paths)
    if path.startswith(CHECK_TEXTS_INPUTS):
        # --gate runs it on the changed actions, which live in controllers
        return any(p.endswith("Controller.cs") for p in branch_paths)
    return path.startswith(REVIEW_TOOLING)


def fail(message: str) -> None:
    """Exit with 2, so that --gate's 1 always means a hard failure and never a scan that could not run."""
    print(message, file=sys.stderr)
    raise SystemExit(2)


def git(*args: str, check: bool = True) -> str:
    res = subprocess.run(["git", *args], capture_output=True, text=True, encoding="utf-8", errors="replace")
    if check and res.returncode != 0:
        fail(f"git {' '.join(args)} failed: {res.stderr.strip()}")
    return res.stdout


def worktree_snapshot() -> str:
    """A commit of what `git commit -a` would record from the working tree: tracked changes, intent-to-add
    entries and moved submodule pointers, with git's clean filters applied. It is built in a temporary copy of
    the index, so the real index, its lock and the stash are never touched; it leaves only unreachable objects,
    which `git gc` prunes."""
    import shutil
    import tempfile

    index = git("rev-parse", "--git-path", "index").strip()
    with tempfile.TemporaryDirectory() as tmp:
        copy = os.path.join(tmp, "index")
        if os.path.exists(index):
            shutil.copyfile(index, copy)
        env = dict(os.environ, GIT_INDEX_FILE=copy, GIT_AUTHOR_NAME="scan.py", GIT_AUTHOR_EMAIL="scan@localhost",
                   GIT_COMMITTER_NAME="scan.py", GIT_COMMITTER_EMAIL="scan@localhost")

        def run(*args: str) -> str:
            res = subprocess.run(["git", *args], env=env, capture_output=True, text=True, encoding="utf-8",
                                 errors="replace")
            if res.returncode != 0:
                fail(f"git {' '.join(args)} failed: {res.stderr.strip()}")
            return res.stdout.strip()

        run("add", "-u")
        tree = run("write-tree")
        return run("commit-tree", tree, "-p", "HEAD", "-m", "scan.py snapshot of the working tree")


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
        fail("cannot detect a base branch; pass --base")
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


def load_rules(ref: str) -> dict[str, list[re.Pattern[str]]]:
    """The rules of the reviewed tree `ref` (not of the checkout on disk) with their `paths:` globs."""
    rules: dict[str, list[re.Pattern[str]]] = {}
    for path in sorted(git("ls-tree", "--name-only", f"{ref}", f"{RULES_DIR.as_posix()}/", check=False).splitlines()):
        if not path.endswith(".md"):
            continue
        text = (git_bytes("show", f"{ref}:{path}") or b"").decode("utf-8", "replace").replace("\r\n", "\n")
        m = re.match(r"---\s*\n(.*?)\n---", text, re.S)
        globs = re.findall(r'-\s*"([^"]+)"', m.group(1)) if m else []
        rules[path.rsplit("/", 1)[-1]] = [glob_to_regex(g) for g in globs]  # [] = always-on rule
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
# (rule file, label, regex, path filter); the filter "paths:" is the rule's own `paths:` (C# outside the tests)

def is_cs(p: str) -> bool:
    return p.endswith(".cs") and not p.startswith(("migrations/", "sdk/"))


def is_test(p: str) -> bool:
    return bool(re.match(r"(products/[^/]+/(Tests|ASC\.\w+\.Tests)/|common/Tests/|web/ASC\.Web\.Api(\.Billing)?\.Tests/)", p))


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
    ("logging.md", "[LoggerMessage] outside a dedicated *Logger class file",
     re.compile(r"\[LoggerMessage\b"), lambda p: is_cs(p) and not p.endswith("Logger.cs")),
    ("logging.md", "exception message logged instead of the exception (stack trace and inner exceptions lost)",
     re.compile(r"\b_?[lL]ogger\??\.\w+\((?!.*\b(?:e|ex|exc|exception|err|error)\s*[,)])"
                r".*\b(?:e|ex|exc|exception|err|error)\.Message\b"), is_cs),
    # TimeSpan reads "24:00:00" as 24 days (configuration.md §3); a value of 24 hours or more is written d.hh:mm:ss
    ("configuration.md", "interval of 24 hours or more without a day part (\"24:00:00\" is 24 days) - write d.hh:mm:ss",
     re.compile(r'"(?:2[4-9]|[3-9]\d|\d{3,}):[0-5]\d(?::[0-5]\d)?"'),
     lambda p: p.endswith((".cs", ".json")) and not p.startswith(("sdk/", "migrations/")) and not p.endswith("_2.0.json")),
    ("dto-mapping.md", "* DTO built inline in a controller - a Mapperly mapper, or only server-owned fields set here?",
     re.compile(r"\bnew\s+\w+Dto\s*(?:<[^>]*>)?\s*[({]"),
     lambda p: p.endswith("Controller.cs") and is_cs(p) and not is_test(p)),
    ("configuration.md", "* configuration value read - through ConfigurationValueExtensions (a list or a bound section: configuration.md §1), both bounds, 0 / negative / huge?",
     re.compile(r"\.GetValue<\s*(int|long|double|bool|TimeSpan|int\[\])|configuration\[\s*\"|\.GetSection\(\s*\"[^\"]*\"\s*\)\s*\.Get<"),
     lambda p: is_cs(p) and not is_test(p)),
    ("configuration.md", "`Parse` of a configuration value (throws on a typo; use ConfigurationValueExtensions)",
     re.compile(r"\b(TimeSpan|int|long|double|bool)\.Parse\s*\(\s*_?(configuration|config)\b"),
     lambda p: is_cs(p) and not is_test(p)),
    ("http-clients.md", "`new HttpClient(` (IHttpClientFactory required)",
     re.compile(r"new\s+HttpClient\s*\("), is_cs),
    ("http-clients.md", "new named HttpClient registered",
     re.compile(r"\.AddHttpClient\s*\("), is_cs),
    ("http-clients.md", "* body from outside read - byte limit, deadline, prefix first? (bullet 5)",
     re.compile(r"\.ReadAs(?:ByteArray|String|Stream)Async\s*\(|HttpCompletionOption\.ResponseHeadersRead"
                r"|CopyToAsync\s*\(\s*(?:ms|memoryStream|new\s+MemoryStream)\b"),
     lambda p: is_cs(p) and not is_test(p)),
    ("access-checks.md", "* decrypted value - does it authorize an action? keyed and expiring? (§5)",
     re.compile(r"\.Decrypt(?:Async)?\s*\("), lambda p: is_cs(p) and not is_test(p)),
    ("row-states.md", "* sentinel date - a state in a column of its own? (§1)",
     re.compile(r"DateTime\.(?:MinValue|UnixEpoch)|new\s+DateTime\(\s*19\d\d\b"),
     "paths:"),
    ("caching.md", "hand-rolled cache / IMemoryCache / IDistributedCache",
     re.compile(r"\bIMemoryCache\b|\bIDistributedCache\b|static\s+(readonly\s+)?ConcurrentDictionary<|\bLazy<"), is_cs),
    ("caching.md", "bare RemoveAsync/RemoveByTagAsync - on the memory cache it clears the local node only",
     re.compile(r"\.Remove(ByTag)?Async\s*\("), is_cs),
    ("caching.md", "* GetOrSet/Set - check explicit duration and tenantId in key/tag",
     re.compile(r"\.(GetOrSet|Set)Async\s*[<(]"), is_cs),
    ("caching.md", "cache tag built inline (tags come from CacheExtention only)",
     re.compile(r'[Tt]ags?\s*[:=].*\$"'), is_cs),
    # a field named *Format holds a format string, not a key (caching.md "Keys")
    ("caching.md", "cache key in a field (keys come from a static Get*CacheKey method)",
     re.compile(r"\b(?:private|protected|internal|public)\s+(?:static\s+)?(?:readonly\s+)?(?:const\s+)?string\s+"
                r"_?(?!\w*Format\s*=)\w*[Cc]ache[Kk]ey\w*\s*=(?!=)"),
     lambda p: is_cs(p) and not is_test(p)),
    ("caching.md", "* memory cache - obtained and read by the same process, no decision falls back on it? (\"Which instance\")",
     re.compile(r"\bGetMemoryCache\s*\("), lambda p: is_cs(p) and not is_test(p)),
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
    ("tests.md", "`ErrorContent?.` - read it with `!`: with `?.` an empty body skips the assertion",
     re.compile(r"\bErrorContent\s*\?\."), lambda p: is_cs(p) and is_test(p)),
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


EMPTY_CATCH = re.compile(r"catch\s*(?:\([^)]*\))?\s*(?:when\s*\((?:[^()]|\([^()]*\))*\)\s*)?\{\s*(?://[^\n]*\s*)*\}")


# --- localized resources (.resx) -----------------------------------------------------------------
# A resource family is the neutral file plus its culture siblings: Foo.resx, Foo.ru.resx, Foo.pt-BR.resx.

RESX_CULTURE = re.compile(r"^(?P<stem>.+?)\.(?P<culture>[a-z]{2,3}(?:-[A-Za-z]{2,4})*(?:-[A-Z]{2})?)\.resx$")
# what must survive translation verbatim: $Tags of notify patterns, {0} format items, HTML tags.
# A tag name is ASCII-only, exactly as NVelocityPatternFormatter.DefaultPattern reads it, so a suffix glued
# to it in the translation ("$UserName님" in Korean) still leaves the tag intact - Python's \w would not.
PLACEHOLDER = re.compile(r"\$\{?[A-Za-z0-9_]+|\{\d+(?::[^}]*)?\}|</?[A-Za-z][A-Za-z0-9]*")


def placeholders(text: str) -> list[str]:
    return sorted(p.replace("${", "$") for p in PLACEHOLDER.findall(text))


def resx_family(path: str) -> tuple[str, str]:
    """path -> (neutral path, culture or '' for the neutral file)."""
    folder, _, name = path.rpartition("/")
    m = RESX_CULTURE.match(name)
    if m:
        return f"{folder}/{m.group('stem')}.resx" if folder else f"{m.group('stem')}.resx", m.group("culture")
    return path, ""


@functools.lru_cache(maxsize=None)
def resx_values(ref: str | None, path: str) -> dict[str, str]:
    """data name -> value at a git ref (None = working tree); {} when the file does not exist there."""
    import xml.etree.ElementTree as ET

    if ref is None:
        p = Path(path)
        raw = p.read_bytes() if p.exists() else None
    else:
        raw = git_bytes("show", f"{ref}:{path}")
    if not raw:
        return {}
    try:
        root = ET.fromstring(raw)
    except ET.ParseError as e:
        return {"!! parse error": str(e)}
    return {d.get("name"): (d.findtext("value") or "") for d in root.iter("data") if d.get("name")}


def print_resx_section(files: list[tuple[str, str]], base: str, head: str | None) -> None:
    changed = [p for st, p in files if p.endswith(".resx") and not st.startswith("D")]
    print("## Localized resources (.resx)")
    if not changed:
        print("  none")
        print()
        return

    families: dict[str, set[str]] = defaultdict(set)
    for p in changed:
        neutral, _ = resx_family(p)
        families[neutral].add(p)

    for neutral in sorted(families):
        # every culture file of the family that exists at head, changed or not
        folder = str(Path(neutral).parent).replace("\\", "/")
        stem = Path(neutral).name[:-len(".resx")]
        listing = git("ls-files", folder) if head is None else git("ls-tree", "--name-only", head, f"{folder}/")
        siblings = sorted(p for p in listing.splitlines()
                          if resx_family(p)[0] == neutral and Path(p).name.startswith(stem + "."))

        old_n, new_n = resx_values(base, neutral), resx_values(head, neutral)
        keys = sorted(k for k in new_n if old_n.get(k) != new_n[k]) + sorted(k for k in old_n if k not in new_n)
        print(f"  {neutral}: {len(families[neutral])} file(s) changed of {len(siblings)}; neutral keys changed: {len(keys)}")

        # keys changed in a culture file but not in the neutral one: a translation edited on its own
        for p in sorted(families[neutral] - {neutral}):
            old_c, new_c = resx_values(base, p), resx_values(head, p)
            own = [k for k in new_c if old_c.get(k) != new_c[k] and k not in keys]
            if own:
                print(f"    {resx_family(p)[1]}: changed without the neutral text: {', '.join(own[:10])}")

        for key in keys[:20]:
            if key not in new_n:
                stale = [resx_family(p)[1] for p in siblings if p != neutral and key in resx_values(head, p)]
                print(f"    - {key}: removed from neutral" + (f"; still in {', '.join(stale)}" if stale else ""))
                continue
            expected = placeholders(new_n[key])
            untouched, missing, broken = [], [], []
            for p in siblings:
                if p == neutral:
                    continue
                culture = resx_family(p)[1]
                old_c, new_c = resx_values(base, p), resx_values(head, p)
                if key not in new_c:
                    missing.append(culture)
                    continue
                if old_c.get(key) == new_c[key] and key in old_n:
                    untouched.append(culture)
                got = placeholders(new_c[key])
                if got != expected:
                    lost = sorted(set(expected) - set(got))
                    extra = sorted(set(got) - set(expected))
                    broken.append(f"{culture}(" + "; ".join(
                        x for x in (f"lost {' '.join(lost)}" if lost else "", f"extra {' '.join(extra)}" if extra else "") if x) + ")")
            print(f"    - {key}: placeholders {' '.join(expected) or '-'}")
            if untouched:
                print(f"      neutral changed, translation untouched: {', '.join(untouched)}")
            if missing:
                print(f"      no translation (falls back to neutral): {', '.join(missing)}")
            if broken:
                print(f"      !! placeholder mismatch: {', '.join(broken)}")
        if len(keys) > 20:
            print(f"    ... and {len(keys) - 20} more keys")
    print()


# --- changed endpoints, stateful code, size ------------------------------------------------------
# These sections feed the cross-cutting matrix (SKILL.md §3 A0), reviewer G and the chunking of
# reviewers (SKILL.md §3 "Coverage"): a reviewer handed a large area samples it, and these lists make
# the work enumerable.

HTTP_ATTR = re.compile(r'^\s*\[Http(Get|Post|Put|Delete|Patch)(?:\(\s*"([^"]*)")?')
ACTION = re.compile(r"^\s*public\s+(?:async\s+)?[\w<>\[\],.? ]+?\s+(\w+)\s*\(")
# the controllers here name their route with [ApiEndpoint("...")]; [Route] is the plain ASP.NET form
CLASS_ROUTE = re.compile(r'^\s*\[(?:Route|ApiEndpoint)\(\s*"([^"]*)"')


def method_end(lines: list[str], start: int) -> int:
    """The 1-based line of the brace that closes the member declared on line `start`: the first `}` at the
    member's own indentation (or the end of an expression-bodied member)."""
    indent = len(lines[start - 1]) - len(lines[start - 1].lstrip())
    for i in range(start, len(lines)):
        text = lines[i]
        if text.strip() == "}" and len(text) - len(text.lstrip()) == indent:
            return i + 1
        if i == start - 1 and text.rstrip().endswith(";"):
            return i + 1
    return len(lines)


# (controller file, action) of each changed action, for the texts check
CHANGED_ACTIONS: list[tuple[str, str]] = []


def print_endpoints_section(files, added, read_head) -> list[tuple[str, str, int]]:
    """Prints the changed actions and returns (name, file, first line of its doc comment and attributes) of each,
    for the OpenSpec section and the texts the diff reaches."""
    print("## Changed endpoints (one row each in the cross-cutting matrix, SKILL.md §3 A0)")
    rows, names = [], []
    CHANGED_ACTIONS.clear()
    for st, p in files:
        if not p.endswith("Controller.cs") or not is_cs(p) or is_test(p) or st.startswith("D"):
            continue
        changed = {n for n, _ in added.get(p, [])}
        blob = read_head(p) if changed else None
        if not blob:
            continue
        lines = blob.decode("utf-8", "replace").splitlines()
        prefix, actions, pending = "", [], None
        for i, line in enumerate(lines, 1):
            if not prefix and (m := CLASS_ROUTE.match(line)):
                prefix = m.group(1)
            if m := HTTP_ATTR.match(line):
                pending = (i, m.group(1).upper(), m.group(2) or "")
            elif pending and (m := ACTION.match(line)):
                lo = pending[0]
                while lo > 1 and lines[lo - 2].strip().startswith(("///", "[")):
                    lo -= 1
                actions.append([lo, pending[0], pending[1], pending[2], m.group(1), method_end(lines, i)])
                pending = None
        for k, (lo, attr, verb, route, name, end) in enumerate(actions):
            # the action ends at its own closing brace: the private helpers and any class below it are not its lines
            hi = min(end, actions[k + 1][0] - 1) if k + 1 < len(actions) else end
            n = sum(1 for x in changed if lo <= x <= hi)
            if n:
                mark = "new" if attr in changed else f"{n} changed line(s)"
                rows.append(f"  {verb:6} {prefix}/{route}".rstrip("/") + f"  {name}  [{p}:{attr}]  ({mark})")
                names.append((name, p, lo))
                CHANGED_ACTIONS.append((p, name))
    print("\n".join(rows) if rows else "  none")
    print()
    return names


# --- OpenSpec -------------------------------------------------------------------------------------
# CLAUDE.md "Specs (OpenSpec)": a change to what an endpoint does is checked against the requirements that
# govern it, and one that contradicts a requirement goes through openspec-propose. The specs name operations
# by operationId (the action name, camelCase), and each verification.md names the product files it was
# checked against, so both point from the diff to the requirements to read (SKILL.md §3 S).

REQUIREMENT = re.compile(r"^###\s+Requirement:\s*(.+)$")


def print_openspec_section(files, endpoint_names, head: str, merge_base: str, old_paths=(), untracked=(),
                           full: bool = False, renames: dict[str, str] | None = None) -> None:
    print("## OpenSpec (requirements that govern the changed code, SKILL.md §3 S)")
    spec_paths = sorted(p for p in git("ls-tree", "-r", "--name-only", head, "openspec/specs", check=False).splitlines()
                        if p.endswith(".md"))
    read = lambda p: git_bytes("show", f"{head}:{p}")
    if not spec_paths:
        print("  no openspec/specs in the reviewed tree - skip")
        print()
        return

    texts = {p: (read(p) or b"").decode("utf-8", "replace").splitlines() for p in spec_paths}
    hits = []
    for name in dict.fromkeys(endpoint_names):
        op = name[:1].lower() + name[1:]
        word = re.compile(rf"\b{re.escape(op)}\b")
        for p in spec_paths:
            if not p.endswith("/spec.md"):
                continue
            req, req_line, seen = None, 0, set()
            for i, line in enumerate(texts[p], 1):
                if m := REQUIREMENT.match(line):
                    req, req_line = m.group(1).strip(), i
                elif req and req not in seen and word.search(line):
                    seen.add(req)
                    hits.append(f"  {op}: {p}:{req_line}  \"{req}\"")
    print("  operations of the changed endpoints named by a requirement:")
    print("\n".join(hits) if hits else "    none")

    product = [p for st, p in files if not st.startswith("D") and not is_test(p) and not p.startswith(("openspec/", "sdk/"))
               and p.endswith((".cs", ".js", ".ts", ".json", ".conf"))]
    named = []
    for p in spec_paths:
        if not p.endswith("/verification.md"):
            continue
        text = "\n".join(texts[p])
        found = [f for f in product if f in text]
        if found:
            named.append(f"  {p.split('/')[-2]}: {', '.join(found[:6])}" + (f" (+{len(found) - 6})" if len(found) > 6 else ""))
    print("  capabilities whose verification.md was checked against a changed file:")
    print("\n".join(named) if named else "    none")

    # claims.md §4: a row cites lines of a file, and a branch that moves those lines leaves the row pointing elsewhere.
    # Rows cite a file by its full path, an alias the file defines (`Lic` is `web/.../LicenseController.cs`, or a
    # target elided as `.../Api/X.cs`), the alias with an extension, its bare file name, or a folder alias and the
    # rest (`Unit/` is `common/Tests/...`). A file name that several tracked files share is marked as such: the row
    # may cite another of them.
    changed = sorted({p for _, p in files if not p.startswith("sdk/")
                      and (not p.startswith("openspec/") or p.endswith("/spec.md"))} | set(old_paths))
    tracked = git("ls-tree", "-r", "--name-only", head, check=False)
    name_count: dict[str, int] = defaultdict(int)
    for t in tracked.splitlines():
        name_count[t.rsplit("/", 1)[-1]] += 1
    stale = []
    # A row that cites a member by name (`File.cs::Method`) survives a moved line: it is stale when the file in the
    # reviewed tree no longer has that name in its code (comments left out; a renamed file is read at its new path),
    # and otherwise listed to re-read, since the member's behaviour may have changed (claims.md §4).
    renamed_to = {old: new for new, old in (renames or {}).items()}
    members: dict[str, str] = {}

    def has_member(f: str, member: str) -> bool:
        if f not in members:
            text = (git_bytes("show", f"{head}:{renamed_to.get(f, f)}") or b"").decode("utf-8", "replace")
            code = []
            for line in text.splitlines():
                comment = comment_part(line)
                stripped = line.strip()
                code.append(stripped[:len(stripped) - len(comment)] if comment else stripped)
            members[f] = "\n".join(code)
        return re.search(rf"\b{re.escape(member)}\b", members[f]) is not None

    def shown_rows(rows) -> str:
        keys = sorted(rows)
        listed = keys if full else keys[:8]
        text = "; ".join(f"{i}: {', '.join(rows[i])}" for i in listed) if isinstance(rows, dict) \
            else ", ".join(str(i) for i in listed)
        return text + (f" (+{len(keys) - len(listed)})" if len(keys) > len(listed) else "")

    for p in spec_paths:
        if not p.endswith("/verification.md"):
            continue
        lines = texts[p]
        cited: dict[str, set[int]] = defaultdict(set)
        by_name: dict[str, list[tuple[int, list[str]]]] = defaultdict(list)
        for i, line in enumerate(lines, 1):
            for m in CITATION.finditer(line):
                cited[m.group(1)].add(i)
            for m in NAME_CITATION.finditer(line):
                by_name[m.group(1)].append((i, [n.strip() for n in m.group(2).split(",")]))
        aliases = re.findall(r"`([A-Za-z][\w./-]*)`\s+(?:is|=)\s+`([^`]+)`", "\n".join(lines))
        for f in changed:
            name = f.rsplit("/", 1)[-1]
            forms = {f: f, name: f"{name} (file name shared by {name_count[name]} files)" if name_count[name] > 1 else name}
            for alias, target in aliases:
                elided = target.startswith("...")
                tail = target[3:] if elided else target
                if tail.endswith("/"):
                    rooted = "/" + f
                    if not elided and f.startswith(tail):
                        form = alias.rstrip("/") + "/" + f[len(tail):]
                        forms[form] = form
                    elif elided and tail in rooted:
                        form = alias.rstrip("/") + "/" + rooted.split(tail, 1)[1]
                        forms[form] = form
                elif target == f or (elided and f.endswith(tail)):
                    forms[alias] = alias
                    forms[alias + Path(f).suffix] = alias + Path(f).suffix
            matched = [t for t in forms if t in cited]
            rows = sorted({r for t in matched for r in cited[t]})
            if rows:
                stale.append(f"    {p}: {len(rows)} row(s) cite {f} (as {', '.join(sorted(forms[t] for t in matched))}) "
                             f"- lines {shown_rows(rows)}")
            missing: dict[int, list[str]] = {}
            present: set[int] = set()
            gone_as: set[str] = set()
            read_as: set[str] = set()
            for t in forms:
                for i, names in by_name.get(t, []):
                    absent = [n for n in names if not has_member(f, n) and n not in missing.get(i, [])]
                    if absent:
                        missing.setdefault(i, []).extend(absent)
                        gone_as.add(forms[t])
                    elif names:
                        present.add(i)
                        read_as.add(forms[t])
            if missing:
                stale.append(f"    {p}: {len(missing)} row(s) cite {f} (as {', '.join(sorted(gone_as))}) by a name "
                             f"its code no longer has - stale - lines {shown_rows(missing)}")
            present -= set(missing)
            if present:
                stale.append(f"    {p}: {len(present)} row(s) cite {f} (as {', '.join(sorted(read_as))}) by a name it "
                             f"still has - re-read if its behaviour changed - lines {shown_rows(sorted(present))}")
    print("  verification.md rows citing changed files (by line: a moved line or a changed behaviour makes the row")
    print("  stale; by `::Name`: stale when the code no longer has the name, otherwise to re-read; a citation in")
    print("  another form than path, alias, file name or folder alias is not found):")
    print_capped(stale, STALE_ROWS_SHOWN, full, "    none")

    # An untracked proposal is the branch's change to openspec/ too (--worktree).
    touched = [p for st, p in files if p.startswith("openspec/")] + \
              [f"{p} (untracked)" for p in untracked if p.startswith("openspec/")]
    print(f"  openspec/ files changed by the branch: {len(touched)}")
    for p in touched[:10]:
        print(f"    {p}")
    if (hits or named) and not touched:
        print("  !! the branch changes specified behaviour and leaves openspec/ untouched - check each requirement above")

    # openspec/config.yaml and claims.md §4, measured on what the branch writes: a requirement whose body is new or
    # differs from the same-named requirement at the merge-base (in the same file, its old path, or - for a delta
    # under openspec/changes/<change>/specs/<capability>/ - the capability's spec), outside a REMOVED section; and the
    # task lists it changes. With --worktree also the untracked files, since openspec-propose writes a change before
    # anyone adds it: they are not in the snapshot, so they are read from disk.
    untracked_set = set(untracked)
    read_any = lambda q: (Path(q).read_bytes() if Path(q).exists() else None) if q in untracked_set \
        else git_bytes("show", f"{head}:{q}")
    format_hits = []
    candidates = [p for st, p in files if not st.startswith("D")] + list(untracked)
    for p in dict.fromkeys(candidates):
        if not p.startswith("openspec/") or not p.endswith(".md"):
            continue
        lines = (read_any(p) or b"").decode("utf-8", "replace").splitlines()
        if p.endswith("/spec.md"):
            delta = re.match(r"openspec/changes/(?:archive/)?[^/]+/specs/([^/]+)/spec\.md$", p)
            # Every body each requirement had at the merge-base, by name, read in a fixed order - the same file, its
            # old path, the capability's spec - with REMOVED entries left out, so that the verdict never depends on
            # which source comes first. A requirement the branch renamed is also looked up by its old name (a
            # RENAMED FROM/TO pair). A requirement with no old body at all whose body equals that of a requirement
            # gone from the same file (or its old path) is a rename without a pair, so unchanged. A requirement moved
            # to another spec file is measured as new.
            own = [p] + ([renames[p]] if renames and p in renames else [])
            ordered = own + ([f"openspec/specs/{delta.group(1)}/spec.md"] if delta else [])
            old_bodies: dict[str, list[str]] = defaultdict(list)
            own_names: set[str] = set()
            for q in dict.fromkeys(ordered):
                for _, name, body, section in requirement_bodies(
                        (git_bytes("show", f"{merge_base}:{q}") or b"").decode("utf-8", "replace").splitlines()):
                    if not section.startswith("REMOVED"):
                        old_bodies[name].append(body)
                        if q in own:
                            own_names.add(name)
            current = requirement_bodies(lines)
            renamed_from = renamed_requirements(lines)
            names_now = {name for _, name, _, _ in current}
            gone = {b for name in own_names - names_now for b in old_bodies[name]}
            for n, name, body, section in current:
                if section.startswith("REMOVED") or len(body) < REQUIREMENT_LIMIT:
                    continue
                olds = old_bodies.get(name, []) + old_bodies.get(renamed_from.get(name, ""), [])
                if body in olds or (not olds and body in gone):
                    continue
                long_olds = [len(o) for o in olds if len(o) >= REQUIREMENT_LIMIT]
                if long_olds and len(body) <= max(long_olds):
                    # already over the limit at the merge-base and not made longer: an edit of such a requirement
                    # must not be blocked, only reminded
                    format_hits.append(f"  {p}:{n}  requirement \"{name}\" is {len(body)} characters; it was "
                                       f"{max(long_olds)} before the branch and did not grow - shorten it when you can")
                    continue
                format_hits.append(f"  !! {p}:{n}  requirement \"{name}\" is {len(body)} characters (under {REQUIREMENT_LIMIT} required, openspec/config.yaml)")
                GATE.append(f"{p}:{n} requirement \"{name}\" is {len(body)} characters (openspec/config.yaml: under {REQUIREMENT_LIMIT})")
        if p.endswith("/tasks.md"):
            for i, line in enumerate(lines, 1):
                if says_not_done(line):
                    format_hits.append(f"  !! {p}:{i}  ticked, but says it is not done (claims.md §4): {line.strip()[:110]}")
    for line in format_hits:
        print(line)
    print()


def print_capped(rows: list[str], limit: int, full: bool, empty: str) -> None:
    """Every row with --full; otherwise the first `limit` and a count of the rest, so a release-sized diff stays readable."""
    if not rows:
        print(empty)
        return
    shown = rows if full else rows[:limit]
    for r in shown:
        print(r)
    if len(rows) > len(shown):
        print(f"    ... {len(rows) - len(shown)} more not shown - run with --full to list them")


REQUIREMENT_LIMIT = 500
STALE_ROWS_SHOWN = 100
CLAIMS_SHOWN = 200
TICKED = re.compile(r"^\s*[-*]\s+\[[xX]\]\s*")
# Whether a ticked task says it is not done (claims.md §4). A status, not a domain word:
# - a status word, with at most one qualifier, at the start of a clause - the task text, or after ". ", ":", ";",
#   "(", a dash or a comma - and followed by a break (".", ":", ";", ",", a bracket, a dash, the end) or by "on",
#   "by", "because", "until", "due", "here", "yet", "for now", "to a follow-up", "to <version>" (a dotted number
#   or v<n>): "...: blocked, because", "Done. Blocked on CI.", "(still blocked)", "Not covered, needs a stand",
#   "Port it, skipped on CI"; "not done", "not covered", "not run" and "cannot run" at a clause start whatever
#   follows ("...: not done for PostgreSQL", "...: cannot run without the stand");
# - a status word right before a colon ("Tests skipped: no stand"), or followed anywhere by "for now", "to a
#   follow-up" or "to <version>" ("Tests deferred to a follow-up", "postponed to 4.2"), and "cannot be run here"
#   anywhere;
# - TODO or TBD as a mark, unless it is the direct object of remove/delete/drop/resolve/address/fix ("Remove the
#   TODO from X") or a file name ("TODO.md").
# Not a status: a domain word inside a clause ("the login is blocked after 5 attempts", "Deferred loading of the
# list", "blocked for guests", "postponed to 30 seconds after a failure"), an item of a list of one-word states
# ("active, terminated, blocked", "copied, skipped, failed"), a value after "status:" or "state:".
# It is a hint for the reader, not a gate: it misses forms such as "**blocked**", "tests—blocked" or "[blocked]",
# and it can flag a domain line ("Return 403 - blocked by the firewall rule").
_STATUS = r"(?:blocked|not done|not yet done|not run|cannot (?:be )?run|not covered|skipped|deferred|postponed)"
_STATUS_AT = re.compile(rf"(?:(?:still|partially|currently)\s+)?{_STATUS}\b", re.I)
_CLAUSE_START = re.compile(r"^|[.:;(]\s*|\s[-–—]\s+|,\s*")
_VERSION = r"v?\d+(?:\.\d+)+\b|v\d+\b"
_STATUS_END = re.compile(
    r"\s*(?:[.:;,()]|[-–—]\s|$)"
    rf"|\s+(?:(?:on|by|because|until|due|here|yet|for now|to (?:a )?follow-up)\b|to (?:{_VERSION}))", re.I)
_STATUS_COLON = re.compile(rf"(?<!state )(?<!status )\b{_STATUS}:", re.I)
_NOT_HERE = re.compile(r"\b(?:cannot (?:be )?run|not run|not covered) here\b", re.I)
_STATUS_LATER = re.compile(rf"\b{_STATUS}\s+(?:for now\b|to (?:a )?follow-up\b|to (?:{_VERSION}))", re.I)
NOT_DONE_MARK = re.compile(r"\b(?:TBD|TODO)\b(?!\.\w)")
_REMOVED_MARK = re.compile(r"\b(?:remove|delete|drop|resolve|address|fix)\s+(?:(?:the|a|all|every|this)\s+)?$", re.I)


def says_not_done(line: str) -> bool:
    """A ticked task line that says it is not done (see the comment above)."""
    m = TICKED.match(line)
    if not m:
        return False
    text = re.sub(r"^[\d.]+\s+", "", line[m.end():]).rstrip()
    if _NOT_HERE.search(text) or _STATUS_COLON.search(text):
        return True
    # a TODO is a mark unless it is what the task removes; every occurrence counts
    if any(not _REMOVED_MARK.search(text[:mark.start()]) for mark in NOT_DONE_MARK.finditer(text)):
        return True
    for start in _CLAUSE_START.finditer(text):
        status = _STATUS_AT.match(text, start.end())
        # "not done", "not covered", "cannot run" at the start of a clause are a status whatever follows them
        if not status or not (_STATUS_END.match(text, status.end())
                              or re.search(r"(?:\b|can)not\b", status.group(0), re.I)):
            continue
        before = text[:start.start()]
        separator = start.group(0).strip()
        if separator in (":", "(") and re.search(r"\b(?:status|state)(?:es|s)?\s*$", before, re.I):
            continue
        # An item of a list of one-word states: it ends at a comma or at the end, and the next item is one word,
        # or it is the last item and the one before it is one word.
        rest = text[status.end():]
        if re.match(r"\s*(?:,|$)", rest):
            following = re.match(r",\s*([^,.;:()]*)", rest)
            previous = re.split(r"[.:;,(]", before)[-1].strip() if separator == "," else ""
            if following is not None and following.group(1).strip() and " " not in following.group(1).strip():
                continue
            if following is None and previous and " " not in previous:
                continue
        return True
    return bool(_STATUS_LATER.search(text))
# A citation `token:line`: a full path (also one starting with a dot, `.claude/...`), an alias, a file name.
CITATION = re.compile(r"(?<![\w/.-])(\.?\w[\w./-]*):\d")
# A citation by name, `token::Member` or `token::A, B` (claims.md §4). A further name after a comma starts with a
# capital or an underscore, as C# members do, is not the file of the next citation (`B.cs::X`), and ends the item
# (a comma, a backtick, `;`, `)`, `|` or the end follows), so that the prose after a citation is not read as a name.
NAME_CITATION = re.compile(r"(?<![\w/.-])(\.?\w[\w./-]*)::([A-Za-z_]\w*"
                           r"(?:\s*,\s*[A-Z_]\w*(?![\w.]*::|\.\w)(?=\s*(?:,\s*[A-Z_]|[`;)|(:]|\.(?!\w)|$)|\s+(?:and|or)\b))*)")


def renamed_requirements(lines: list[str]) -> dict[str, str]:
    """New name -> old name for each pair of a delta's `## RENAMED Requirements` section (`- FROM: ...`, `- TO: ...`,
    with or without the backticks and the `### Requirement:` prefix)."""
    pairs, section, old = {}, "", None
    for line in lines:
        if line.startswith("## "):
            section, old = line[3:].strip().upper(), None
            continue
        if not section.startswith("RENAMED"):
            continue
        m = re.match(r"\s*[-*+]?\s*(FROM|TO)\s*:\s*`?(?:###\s*Requirement:\s*)?(.+?)`?\s*$", line, re.I)
        if not m:
            continue
        if m.group(1).upper() == "FROM":
            old = m.group(2).strip()
        elif old is not None:
            pairs[m.group(2).strip()] = old
            old = None
    return pairs


def requirement_bodies(lines: list[str]) -> list[tuple[int, str, str, str]]:
    """(line, name, body, section) per requirement: its text up to the first scenario or heading, lines trimmed, and
    the `##` heading it sits under ("ADDED Requirements", "MODIFIED Requirements", "REMOVED Requirements", ...)."""
    result, name, start, body, section = [], None, 0, [], ""

    def close() -> None:
        if name is not None:
            result.append((start, name, "\n".join(body).strip(), section))

    for i, line in enumerate(lines, 1):
        if m := REQUIREMENT.match(line):
            close()
            name, start, body = m.group(1).strip(), i, []
        elif line.startswith("#"):
            close()
            name = None
            if line.startswith("## "):
                section = line[3:].strip().upper()
        elif name is not None:
            body.append(line.strip())
    close()
    return result


# claims.md §1: a number with a unit, a worded duration, a limit or a date word in a text is checked against the
# source that makes it true. Worded forms outside these lists stay to the reader.
_UNITS = (r"(?:ms|milliseconds?|s|secs?|seconds?|mins?|minutes?|h|hrs?|hours?|d|days?|weeks?|months?|years?|"
          r"bytes?|KB|MB|GB|%|characters?|chars|times|attempts?|retries)")
_NUMERALS = r"(?:one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|fifteen|twenty|thirty|sixty|ninety|hundred)"
NUMBER_CLAIM = re.compile(
    rf"(?<![§\w.])(?<![A-Za-z]-)\d+(?:\.\d+)?\s?-?\s?{_UNITS}(?![\w%])|(?<![§\w.])(?<![A-Za-z]-)\d+m\b"
    rf"|\b{_NUMERALS}\s+(?:milliseconds?|seconds?|minutes?|hours?|days?|weeks?|months?|years?)\b"
    r"|\ban?\s+(?:minute|hour|day|week|month|year)\b"
    r"|\b(?:at most|at least|up to|no more than|no fewer than|maximum of|limit of)\s+\d+"
    r"|\b(?:yesterday|today|tomorrow)\b", re.I)
# The result lines prove-red.sh writes, pasted into a commit message, are measurements, not claims about the code.
PROVE_RED_LINE = re.compile(r"^\s*(?:prove-red\b|RED as expected|STILL GREEN)")


def comment_spans(s: str, backtick: bool = False) -> list[tuple[int, int]]:
    """(start, end) of each comment of one line outside string and char literals: a `//` runs to the end of the line,
    a `/*` to its `*/` or the end. A backslash escapes the next character in a string or char literal, except in a C#
    verbatim string (`@"..."`, `$@"..."`, `@$"..."`), where `""` is the quote. A backtick opens a literal only with
    `backtick` (TypeScript's template literal; in C# it is a character of a string). Interpolation holes and raw
    string literals are not parsed."""
    spans, quote, verbatim, i = [], None, False, 0
    quotes = "\"'`" if backtick else "\"'"
    while i < len(s):
        c = s[i]
        if quote:
            if verbatim:
                if c == '"':
                    if s.startswith('""', i):
                        i += 2
                        continue
                    quote = None
            elif c == "\\":
                i += 2
                continue
            elif c == quote:
                quote = None
        elif c in quotes:
            quote = c
            verbatim = c == '"' and not backtick and (s[i - 1:i] == "@" or s[max(0, i - 2):i] == "@$")
        elif s.startswith("//", i):
            spans.append((i, len(s)))
            break
        elif s.startswith("/*", i):
            end = s.find("*/", i + 2)
            end = len(s) if end < 0 else end + 2
            spans.append((i, end))
            i = end
            continue
        i += 1
    return spans


def comment_part(text: str, backtick: bool = False) -> str | None:
    """The comment of a C# or TypeScript line, from its first `//` or `/*` outside a literal (`comment_spans`) to the
    end of the line; a whole-line `//`, `///`, `/*` or `*` line is all comment. The inner lines of a block comment
    that do not start with `*` are not recognized."""
    s = text.strip()
    if s.startswith(("//", "/*", "*")):
        return s
    spans = comment_spans(s, backtick)
    return s[spans[0][0]:] if spans else None


def shell_comment(line: str) -> str | None:
    """The `#` comment of a shell line, outside quotes; `$#` and `${#x}` are no comment."""
    quote, i = None, 0
    while i < len(line):
        c = line[i]
        if quote:
            if c == "\\" and quote == '"':
                i += 2
                continue
            if c == quote:
                quote = None
        elif c == "\\":
            i += 2
            continue
        elif c in "\"'":
            quote = c
        elif c == "#" and (i == 0 or line[i - 1] in " \t;|&(") :
            return line[i:]
        i += 1
    return None


def script_text_lines(path: str, source: str) -> tuple[set[int], set[int]]:
    """(comment lines, docstring lines) of a .py or .sh file; the shebang is neither."""
    comments, docs = set(), set()
    if path.endswith(".py"):
        import ast
        import io
        import tokenize
        try:
            for tok in tokenize.generate_tokens(io.StringIO(source).readline):
                if tok.type == tokenize.COMMENT:
                    comments.add(tok.start[0])
        except (tokenize.TokenError, SyntaxError):
            pass
        try:
            tree = ast.parse(source)
        except SyntaxError:
            tree = None
        for node in ast.walk(tree) if tree else ():
            body = getattr(node, "body", None)
            if isinstance(node, (ast.Module, ast.ClassDef, ast.FunctionDef, ast.AsyncFunctionDef)) and body \
                    and isinstance(body[0], ast.Expr) and isinstance(body[0].value, ast.Constant) \
                    and isinstance(body[0].value.value, str):
                docs.update(range(body[0].lineno, body[0].end_lineno + 1))
    else:
        comments = {n for n, line in enumerate(source.splitlines(), 1) if shell_comment(line) is not None}
    if source.startswith("#!"):
        comments.discard(1)
    return comments, docs


def print_claims_section(added, commit_bodies: list[tuple[str, str]], full: bool) -> None:
    rows = []
    for sha, body in commit_bodies:
        for line in body.splitlines():
            if NUMBER_CLAIM.search(line) and not PROVE_RED_LINE.match(line):
                rows.append(f"    commit {sha}: {line.strip()[:130]}")
    for path, lines in sorted(added.items()):
        spec = path.startswith("openspec/") and path.endswith(".md")
        if not spec and not is_cs(path):
            continue
        for n, text in lines:
            part = text.strip() if spec else comment_part(text)
            if part and NUMBER_CLAIM.search(part):
                rows.append(f"    {path}:{n}  {part[:130]}")
    print("## Claims with numbers (claims.md §1, SKILL.md §3 B) - each against the constant, timeout or test behind it")
    if rows:
        print(f"  {len(rows)} line(s) in commit messages, C# comments and spec texts")
    print_capped(rows, CLAIMS_SHOWN, full, "  none")
    print()


def openapi_responses(ref: str, path: str) -> dict[str, set[str]] | None:
    """`METHOD /path` -> its documented response codes, from an OpenAPI document as `ref` holds it; None when the file
    is missing, not JSON, or not an object with a `paths` object. A path item or a `responses` value of another shape is
    left out."""
    import json

    blob = git_bytes("show", f"{ref}:{path}")
    try:
        doc = json.loads(blob.decode("utf-8-sig")) if blob else None
    except ValueError:
        return None
    if not isinstance(doc, dict) or not isinstance(doc.get("paths"), dict):
        return None
    result = {}
    for route, item in doc["paths"].items():
        if not isinstance(item, dict):
            continue
        for method, op in item.items():
            if isinstance(op, dict) and isinstance(op.get("responses"), dict):
                result[f"{method.upper()} {route}"] = set(op["responses"].keys())
    return result


TEXTS_SHOWN = 60


def print_texts_section(added, endpoints, files, untracked, commit_bodies, unpushed: set[str], full: bool,
                        view: str) -> None:
    """claims.md §3: the texts a fix batch wrote or reached, for the author to re-read against the code after it
    (fixing-findings.md §6, the batch check). A text the diff does not touch and whose behaviour changed - the remarks
    of an action in an unchanged controller that calls a changed service - is still the author's to find."""
    print("## Texts the diff writes or reaches (claims.md §3 - re-read each against the code after every fix batch)")
    rows = []
    for path, lines in sorted(added.items()):
        if not path.endswith((".cs", ".ts", ".py", ".sh")) or path.startswith(("sdk/", "migrations/")) \
                or path.endswith((".Designer.cs", "ModelSnapshot.cs")):
            continue
        if path.endswith((".py", ".sh")):
            # a comment at the end of a line and a docstring are texts too: the whole file tells them apart
            comments, docs = script_text_lines(path, (git_bytes("show", f"{view}:{path}") or b"").decode("utf-8", "replace"))
            is_text = lambda n, t: n in comments or n in docs
            is_doc = lambda n, t: n in docs
        else:
            ts = path.endswith(".ts")
            is_text = lambda n, t: comment_part(t, backtick=ts) is not None
            is_doc = lambda n, t: t.strip().startswith("///")
        # the license header of a new file is no claim about the code
        header, k = set(), 0
        while k < len(lines) and lines[k][0] == k + 1 and lines[k][1].strip() and is_text(*lines[k]):
            k += 1
        if any(mark in t for _, t in lines[:k] for mark in LICENSE_MARKS):
            header = {n for n, _ in lines[:k]}
        nums = [n for n, t in lines if is_text(n, t) and n not in header]
        if nums:
            doc = sum(1 for n, t in lines if is_doc(n, t) and n not in header)
            listed = nums if full else nums[:8]
            shown = ", ".join(map(str, listed)) + (f" (+{len(nums) - len(listed)})" if len(nums) > len(listed) else "")
            rows.append(f"    {path}: {len(nums)} comment line(s), {doc} of them doc comments - lines {shown}")
    print("  comments and doc comments the diff writes (.cs, .ts, .py and .sh outside sdk/ and migrations/, generated "
          "files and a new file's license header left out):")
    print_capped(rows, TEXTS_SHOWN, full, "    none")
    print("  doc comments of the actions whose lines the diff changes (<summary>, <remarks>, [SwaggerResponse]); an "
          "action that reaches changed code from elsewhere is not listed:")
    print_capped([f"    {name}  [{path}:{lo}]" for name, path, lo in endpoints], TEXTS_SHOWN, full, "    none")
    document = lambda p: (p.endswith(".md") and not p.startswith("sdk/")) or p == "openspec/config.yaml"
    specs = [f"    {p}  (+{len(added.get(p, []))} line(s))" for st, p in files if document(p) and not st.startswith("D")]
    specs += [f"    {p}  (untracked)" for p in untracked if document(p)]
    print("  documents the diff writes - specs, proposals, task lists, verification.md, rules, skills, any other "
          "Markdown outside sdk/, openspec/config.yaml (files, with the added line count):")
    print_capped(specs, TEXTS_SHOWN, full, "    none")
    print("  verification.md rows that cite changed files: the OpenSpec section above (by line, and by a name that is "
          "gone or still there)")
    pushed = [sha for sha, _ in commit_bodies if sha not in unpushed]
    still = [sha for sha, _ in commit_bodies if sha in unpushed]
    print(f"  commit messages: {len(commit_bodies)}; not pushed yet, still editable: {len(still)}"
          + (f" ({', '.join(still if full else still[:12])}{' ...' if len(still) > 12 and not full else ''})"
             if still else "")
          + f"; pushed: {len(pushed)} - a wrong claim in a pushed one is corrected in the PR description (claims.md §4)")
    print("  the PR description, and the Hand-over block in it and in the last commit's body: re-read them too")
    print()


# The section is headed exactly `## Checklist`; a longer heading ("## Checklist for any change that ...") is a part of
# the rule's body, not its short list.
CHECKLIST = re.compile(r"^## Checklist[ \t]*\n(.*?)(?=^## |\Z)", re.S | re.M)


def print_checklists_section(view: str, names: list[str]) -> None:
    """The `## Checklist` section of each applicable rule, as the tree `view` holds it: the short list the author walks
    through the batch diff (fixing-findings.md §6, the batch check, point 3)."""
    print("## Checklists of the applicable rules (fixing-findings.md §6, the batch check - walk each over the batch diff)")
    without = []
    for name in names:
        text = (git_bytes("show", f"{view}:{RULES_DIR.as_posix()}/{name}") or b"").decode("utf-8", "replace")
        m = CHECKLIST.search(text.replace("\r\n", "\n"))
        if not m or not m.group(1).strip():
            without.append(name)
            continue
        print(f"  [{name}]")
        for line in m.group(1).strip().splitlines():
            print(f"    {line}" if line.strip() else "")
    if without:
        print(f"  no checklist: {', '.join(without)}")
    if not names:
        print("  none")
    print()


# --- texts of the changed operations ----------------------------------------------------------------
# openapi-endpoint-docs.md: the `<remarks>` and the declared codes of a changed action are checked by the
# openapi-desc-opt check, from the source, so an action not in the emitted document yet is checked too. It runs on a
# copy of the scanned tree (and of the merge-base): what fails the same way at the merge-base is base debt, outside
# the gate. Only FORM, STYLE and CODES fail the gate; the other kinds and the readability tools are hints. A check
# that gave no answer for an operation on the scanned tree fails the gate too: a gate that passes what it did not
# check is open. An action the check answers it cannot check (its project emits no document, or it is not published)
# is listed for a reading by hand, outside the gate.

TEXT_GATE_KINDS = ("FORM", "STYLE", "CODES")
DOC_TOOL_DIR = "common/Tools/ASC.Api.Documentation/ASC.Api.Documentation"


class TextRun(NamedTuple):
    notes: dict[str, list[str]]  # "operationId (NAME)" -> ["KIND note", ...], the gate kinds only
    checked: int                 # operations check_texts.py checked
    unchecked: list[str]         # "FILE:METHOD — reason" of an action it reports it cannot check: its project emits
                                 # no document, or no published action of that name is in the file (IgnoreApi)
    failures: list[str]          # why the run gave no answer for some of the actions, or none at all


# Windows limits a command line to 32 767 characters; the actions of one run of check_texts.py stay well under it.
ACTION_ARGS_PER_RUN = 24_000


def one_line(text: str, limit: int = 300) -> str:
    """The last `limit` characters of a tool's output on one line, for a gate line that stays one line."""
    return " ".join(text.split())[-limit:]


def check_texts_script() -> Path | None:
    """The openapi-desc-opt check of the same copy as this scan. A copy of the scan saved without it gets none: the
    checked-out tree's check_texts.py can be older or newer than the scan and gate differently (SKILL.md §1 "The
    rules are the base's" saves both folders)."""
    candidate = Path(__file__).resolve().parent.parent / "openapi-desc-opt" / "check_texts.py"
    return candidate if candidate.is_file() else None


def text_notes(script: Path, ref: str, actions: list[tuple[str, str]]) -> TextRun:
    """check_texts.py run on `ref`'s copy of the files it reads, for the actions whose file `ref` has."""
    import tarfile
    import tempfile

    present = set(git("ls-tree", "-r", "--name-only", ref, check=False).splitlines())
    wanted = [(f, m) for f, m in actions if f in present]
    if not wanted:
        return TextRun({}, 0, [], [])
    with tempfile.TemporaryDirectory() as tmp:
        project_dirs = {p.rsplit("/", 1)[0] for p in present if p.endswith(".csproj") and "/" in p}
        projects = set()
        for f, _ in wanted:
            d = f.rsplit("/", 1)[0]
            while d and d not in project_dirs:
                d = d.rsplit("/", 1)[0] if "/" in d else ""
            if d:
                projects.add(d)
        # what check_texts.py reads: every project file (to find the projects that emit a document), the
        # documentation tool's folder (the documents, the bundle settings) and the projects of the actions
        paths = [":(glob)**/*.csproj", DOC_TOOL_DIR, *sorted(projects)]
        tar_path = os.path.join(tmp, "tree.tar")
        with open(tar_path, "wb") as out:
            res = subprocess.run(["git", "archive", "--format=tar", ref, "--", *paths], stdout=out, stderr=subprocess.PIPE)
        if res.returncode != 0:
            return TextRun({}, 0, [], [f"git archive {ref} failed: {res.stderr.decode('utf-8', 'replace').strip()[:200]}"])
        root = os.path.join(tmp, "tree")
        with tarfile.open(tar_path) as tar:
            tar.extractall(root, filter="data") if hasattr(tarfile, "data_filter") else tar.extractall(root)
        chunks, size = [[]], 0
        for f, m in wanted:
            if chunks[-1] and size + len(f) + len(m) + 12 > ACTION_ARGS_PER_RUN:
                chunks.append([])
                size = 0
            chunks[-1].append((f, m))
            size += len(f) + len(m) + 12
        notes: dict[str, list[str]] = defaultdict(list)
        checked, unchecked, failures = 0, [], []
        for chunk in chunks:
            args = [sys.executable, str(script), "--root", root, "--work", os.path.join(tmp, "work"), "--no-tools"]
            for f, m in chunk:
                args += ["--action", f"{f}:{m}"]
            res = subprocess.run(args, capture_output=True, text=True, encoding="utf-8", errors="replace",
                                 env=dict(os.environ, PYTHONUTF8="1"))
            # An operation is answered by its own head, matched by file and method: "NAME  (operationId) — FILE:LINE
            # Class.Method" heads its notes, "FILE:METHOD — error" says why it was not checked. Any other line (a
            # warning) answers nothing.
            answered, current = set(), None
            for line in res.stdout.splitlines():
                if not line.strip() or line.startswith(" "):
                    if current and (m := re.match(r"\s+([A-Z]+)\s+(.*)", line)) and m.group(1) in TEXT_GATE_KINDS:
                        notes[current].append(f"{m.group(1)} {m.group(2)}")
                    continue
                current = None
                if m := re.match(r"(.*?)  \((\w+)\) — (.+):\d+ \S*?(\w+)$", line):
                    if (m.group(3), m.group(4)) in chunk:
                        current = f"{m.group(2)} ({m.group(1)})"
                        answered.add((m.group(3), m.group(4)))
                        checked += 1
                elif (m := re.match(r"(.+):(\w+) — (.+)$", line)) and (m.group(1), m.group(2)) in chunk:
                    answered.add((m.group(1), m.group(2)))
                    unchecked.append(line)
            # check_texts.py exits with 1 when it could not check an item, which is its normal answer for an action
            # it cannot check; a crash exits with 1 too, so a run counts only when it reached its last lines
            if res.returncode not in (0, 1):
                why = f"exited with {res.returncode}"
            elif "Traceback (most recent call last)" in (res.stderr or ""):
                why = f"crashed (exit {res.returncode}, a traceback on stderr)"
            elif "CHECK:" not in res.stdout:
                why = "printed no report"
            elif not re.search(r"^TOOLS: ", res.stdout, re.M):
                why = "stopped before the end of its report"
            else:
                why = None
            if why:
                failures.append(f"check_texts.py on {ref} {why}: {one_line(res.stderr or res.stdout)}")
            elif missing := [f"{f}:{m}" for f, m in chunk if (f, m) not in answered]:
                failures.append(f"check_texts.py on {ref} gave no answer for {len(missing)} of {len(chunk)} "
                                f"operation(s): {', '.join(missing[:5])}" + (" ..." if len(missing) > 5 else ""))
    return TextRun(notes, checked, unchecked, failures)


# What a note of check_texts.py is about, to match it with the merge-base's notes of the same operation (base debt):
# the codes of a CODES note and the words of a §5.3 note one by one, the average and the sentence count by value.
CODES_NOTE = re.compile(r"CODES (in the old text|declared|named in the text)[^:]*: (.*?)\.(?:\s|$)")
WORDS_NOTE = re.compile(r"STYLE sentence \d+: (.+) \(§5\.3\)$")
AVERAGE_NOTE = re.compile(r"FORM average ([\d.]+) words per sentence")
FEW_NOTE = re.compile(r"FORM (\d+) sentences, fewer than")
STATUS_CODE = re.compile(r"\b[1-5]\d\d\b")


def note_units(note: str) -> list[tuple[str, ...]]:
    if (m := CODES_NOTE.match(note)) and (found := STATUS_CODE.findall(m.group(2))):
        return [("CODES", m.group(1), c) for c in found]
    if m := WORDS_NOTE.match(note):
        return [("WORD", w.strip().lower()) for w in m.group(1).split(",")]
    # a sentence added above a long one renumbers it: the note is compared without its sentence number
    return [(re.sub(r"\bsentence \d+", "sentence", note),)]


def split_base_debt(old: list[str], new: list[str]) -> tuple[list[str], list[str]]:
    """(the branch's notes, base debt) of one operation. A note is base debt when the merge-base had what it is about:
    the same sentence, each of its codes or replaced words, an average or a sentence count no worse than the
    merge-base's (a sentence the branch adds may lower the average or name a code the merge-base left out, which
    changes the text of the note, not what it is about). Each unit the merge-base had - a sentence, a code, a word -
    covers one unit of the branch: a second sentence that starts with code is the branch's. The notes with the most
    units take theirs first, and only a note that is covered whole takes any, so a new note cannot use up the units of
    an old one; which of two notes with the same units is the old one is not known, and either is reported."""
    def value(pattern: re.Pattern, notes: list[str]) -> float | None:
        return next((float(m.group(1)) for n in notes if (m := pattern.match(n))), None)

    base_average, base_count = value(AVERAGE_NOTE, old), value(FEW_NOTE, old)
    pool = Counter(u for n in old if not AVERAGE_NOTE.match(n) and not FEW_NOTE.match(n) for u in note_units(n))
    verdict: dict[int, str] = {}
    rest = []
    for i, n in enumerate(new):
        if m := AVERAGE_NOTE.match(n):
            verdict[i] = "debt" if base_average is not None and float(m.group(1)) <= base_average else "branch"
        elif m := FEW_NOTE.match(n):
            verdict[i] = "debt" if base_count is not None and float(m.group(1)) >= base_count else "branch"
        else:
            rest.append(i)
    needs = {i: Counter(note_units(new[i])) for i in rest}
    for i in sorted(rest, key=lambda k: -sum(needs[k].values())):
        if all(pool[u] >= k for u, k in needs[i].items()):
            pool.subtract(needs[i])
            verdict[i] = "debt"
    branch, debt = [], []
    for i, n in enumerate(new):
        if verdict.get(i) == "debt":
            debt.append(n)
            continue
        need = needs.get(i)
        added = [u for u in need if pool[u] < need[u]] if need else []
        if need and len(need) > 1 and len(added) < len(need):
            branch.append(f"{n} [not at the merge-base: {', '.join(u[-1] for u in added)}]")
        else:
            branch.append(n)
    return branch, debt


def print_operation_texts_section(actions: list[tuple[str, str]], view: str, merge_base: str, gate: bool,
                                  renames: dict[str, str]) -> None:
    print("## Texts of the changed operations (openapi-endpoint-docs.md, check_texts.py --from source)")
    if not actions:
        print("  no changed operation")
        print()
        return
    if not gate:
        print(f"  {len(actions)} changed operation(s); their texts are checked with --gate")
        print()
        return
    script = check_texts_script()
    if not script:
        reason = (f"the texts of {len(actions)} changed operation(s) were not checked: openapi-desc-opt/check_texts.py "
                  "is not next to this scan (a copy saved without it, SKILL.md §1)")
        print(f"  !! {reason}")
        GATE.append(f"[openapi-endpoint-docs.md] {reason}")
        print()
        return
    new = text_notes(script, view, actions)
    # a file the branch moved is read at the merge-base under its old path
    old = text_notes(script, merge_base, [(renames.get(f, f), m) for f, m in actions])
    # a merge-base run that gave no answer leaves nothing to compare with: every note is the branch's
    old_notes = {} if old.failures else {k.split(" ", 1)[0]: v for k, v in old.notes.items()}
    branch, debt = [], []
    for op, lines in new.notes.items():
        mine, theirs = split_base_debt(old_notes.get(op.split(" ", 1)[0], []), lines)
        branch += [f"{op}: {n}" for n in mine]
        debt += [f"{op}: {n}" for n in theirs]
    print(f"  {len(actions)} changed operation(s): {new.checked} checked, {len(new.unchecked)} not checkable here; "
          "FORM, STYLE and CODES fail the gate")
    for f in new.failures:
        print(f"  !! not run on the scanned tree: {f}")
        GATE.append(f"[openapi-endpoint-docs.md] the texts of the changed operations were not checked: {f}")
    for u in new.unchecked:
        print(f"  !! not checked: {u} - read its texts by hand (§3 A2)")
    for f in old.failures:
        print(f"  !! not run on the merge-base, so base debt cannot be told apart and every note counts as the "
              f"branch's: {f}")
    print("  new on the branch:")
    print_capped([f"    !! {r}" for r in branch], STALE_ROWS_SHOWN, True, "    none")
    print("  already so at the merge-base (base debt, SKILL.md §4; not in the gate):")
    print_capped([f"    {r}" for r in debt], STALE_ROWS_SHOWN, True, "    none")
    GATE.extend(f"[openapi-endpoint-docs.md] {r}" for r in branch)
    print()


# --- verification.md line citations ----------------------------------------------------------------
# claims.md §4, fixing-findings.md §12: a row cites lines of a C# file, and a fix that moves those lines leaves the
# row pointing at something else. Each citation of a changed .cs file is checked on the scanned tree: a range starts
# on a member, an attribute, a comment or a statement, never on a blank line, a lone brace or inside a statement,
# and ends on a closing brace or the end of a statement, or on the comments and attributes of the member below when
# it holds nothing else; a name given in backticks right after it is in the cited lines. A citation the branch did
# not write - a row it left alone, or one it touched that had the citation at the merge-base - is also compared with
# the merge-base: its cited block found elsewhere in the new file has moved, and what fails the same way at the
# merge-base is the base's, listed apart, outside the gate. A row citing the old path of a file the branch moved is
# the branch's.

CITED_FILE = re.compile(r"(?<![\w/.-])(?P<file>\.?\w[\w./-]*\.cs)\b")
CITED_LINES = re.compile(r"(?<!\w)(?P<file>\.?\w[\w./-]*\.cs)?:(?P<nums>\d+(?:-\d+)?(?:,\s*\d+(?:-\d+)?)*)(?![\w.])")
CITED_MEMBER = re.compile(r"(?P<file>\w[\w./-]*\.cs)`?\s+`(?P<member>\w+)`\s*\((?P<n>\d+)\b"
                          r"|`(?P<cls>[A-Z]\w+)\.(?P<member2>[A-Z]\w+)`\s*\((?P<n2>\d+)\b")
CODE_NAME = re.compile(r"`([^`\s]+)`")
CONTINUES = re.compile(r"(?:,|\(|=|=>|\+|-|\*|&&|\|\||\?|\?\?|:)\s*$")
CONTINUED = re.compile(r"^\s*(?:\.|\?\.|\?\?|\?|:|=>|&&|\|\||\+)(?!\s*//)")
DECLARATION = re.compile(r"^\s*(?:\[|///|//|/\*|\*|#|(?:public|private|protected|internal|static|async|override|"
                         r"virtual|abstract|sealed|readonly|partial|const|record|class|interface|enum|struct)\b)")


# the declaration of a member: a modifier first (a method, a property, a delegate field, a local function)
MEMBER_DECLARATION = re.compile(r"^\s*(?:public|private|protected|internal|static|async|override|virtual|sealed|"
                                r"readonly|partial|unsafe|extern)\b")
# `case X:` and `default:` end with a colon and still end their statement
CASE_LABEL = re.compile(r"^\s*(?:case\b.*|default\s*):\s*$")


def _code(line: str) -> str:
    """The code of a C# line with its comments taken out: a trailing `//`, and each `/* ... */` wherever it stands
    (one in a string literal is code); nothing for a comment line or the inner line of a block comment."""
    s = line.rstrip()
    if s.lstrip().startswith("*"):
        if "*/" not in s:
            return ""
        s = s[s.index("*/") + 2:]
    parts, last = [], 0
    for a, b in comment_spans(s):
        parts.append(s[last:a])
        last = b
    parts.append(s[last:])
    return "".join(parts).rstrip()


def _continues(code: str) -> bool:
    """The code line goes on into the next one (a trailing operator, comma or open parenthesis)."""
    return bool(CONTINUES.search(code)) and not CASE_LABEL.match(code)


def _prev_code(lines: list[str], i: int) -> str | None:
    """The last code line above line `i` (1-based), comments and blank lines skipped."""
    for k in range(i - 2, -1, -1):
        if _code(lines[k]).strip():
            return _code(lines[k])
    return None


def _next_code(lines: list[str], i: int) -> str | None:
    """The first code line below line `i` (1-based)."""
    for k in range(i, len(lines)):
        if _code(lines[k]).strip():
            return _code(lines[k])
    return None


def citation_problems(lines: list[str], ranges: list[tuple[int, int]], names: list[str]) -> list[str]:
    """What is wrong with ranges of a C# file cited together, and with the names given for them."""
    out = []
    for a, b in ranges:
        if a < 1 or b > len(lines) or a > b:
            out.append(f"{a}-{b} is outside the file ({len(lines)} lines)")
            continue
        first, last = lines[a - 1].strip(), lines[b - 1].strip()
        span = f"{a}-{b}" if a != b else str(a)
        if not first:
            out.append(f"{span} starts on a blank line")
        elif first in ("{", "}", "};", "});", ")"):
            out.append(f"{span} starts on a lone `{first}`")
        elif a != b and not DECLARATION.match(lines[a - 1]) \
                and (CONTINUED.match(lines[a - 1]) or _continues(_prev_code(lines, a) or ";")):
            out.append(f"{span} starts inside a statement")
        if a == b:
            continue
        if not last:
            out.append(f"{span} ends on a blank line")
        elif last == "{":
            out.append(f"{span} ends on an opening brace")
        elif last.startswith(("///", "[")) and not last.endswith(("]);", "];")):
            # only comments and attributes cite the member below them; code followed by them runs into the next one
            k = b
            while k > a and lines[k - 2].lstrip().startswith(("///", "//", "[")):
                k -= 1
            if any(_code(l).strip() and not l.lstrip().startswith("[") for l in lines[a - 1:k - 1]):
                out.append(f"{span} ends on a comment or attribute of the next member")
        elif _continues(_code(lines[b - 1])) or CONTINUED.match(_next_code(lines, b) or ""):
            out.append(f"{span} ends inside a statement")
    if names and all(1 <= a <= b <= len(lines) for a, b in ranges):
        cited = []
        for a, b in ranges:
            cited.extend(lines[a - 1:b])
            # a range that ends on attributes or comments cites the member declared right below them
            k = b
            while k < len(lines) and lines[k - 1].lstrip().startswith(("[", "/")):
                cited.append(lines[k])
                k += 1
        text = "\n".join(cited)
        # a name in prose keeps the case of the sentence (`taskId` for `TaskId`)
        missing = [n for n in names if n.lower() not in text.lower()]
        if missing:
            out.append(f"{', '.join('`' + n + '`' for n in missing)} not in the cited lines")
    return out


def _named(paren: str) -> list[str]:
    """The names in backticks of a parenthetical that can be looked up in the cited code: identifiers of members or
    types (a capital letter or a leading underscore), and attributes."""
    names = []
    for token in CODE_NAME.findall(paren):
        token = token.rstrip("()")
        if re.fullmatch(r"\[[A-Z][\w(),. \"=]*\]", token):
            names.append(token)
        elif re.fullmatch(r"[A-Za-z_]\w*(?:\.\w+)*", token) and (token[0] == "_" or any(c.isupper() for c in token)):
            names.append(token.rsplit(".", 1)[-1])
    return names


def parse_citations(row: str) -> list[tuple[str, list[tuple[int, int]], list[str], bool]]:
    """(cited file as written, ranges, names to find, exact) for each group of citations in a row. A bare `:N` takes
    the last file named before it; a parenthetical right after a citation names what the citations of that file since
    the last parenthetical point at. `exact` is False for "`Name` (N)", whose number may be a bug number."""
    events = []
    for m in CITED_MEMBER.finditer(row):
        f = m.group("file") or (m.group("cls") + ".cs")
        n = int(m.group("n") or m.group("n2"))
        events.append((m.start(), m.end(), "member", f, [(n, n)], [m.group("member") or m.group("member2")]))
    for m in CITED_LINES.finditer(row):
        if any(s <= m.start() < e for s, e, *_ in events):
            continue
        ranges = []
        for part in re.split(r",\s*", m.group("nums")):
            lo, _, hi = part.partition("-")
            ranges.append((int(lo), int(hi or lo)))
        events.append((m.start(), m.end(), "lines", m.group("file"), ranges, []))
    for m in CITED_FILE.finditer(row):
        if not any(s <= m.start() < e for s, e, *_ in events):
            events.append((m.start(), m.end(), "file", m.group("file"), [], []))
    events.sort()
    groups, pending, last_file = [], [], None
    for _, end, kind, f, ranges, names in events:
        if f:
            last_file = f
        if kind == "file" or not last_file:
            continue
        if kind == "member":
            groups.append((last_file, ranges, names, False))
            continue
        pending.append((last_file, ranges))
        rest = row[end:].lstrip("` ")
        if rest.startswith("("):
            depth, k = 0, 0
            for k, ch in enumerate(rest):
                depth += ch == "("
                depth -= ch == ")"
                if depth == 0:
                    break
            # a parenthetical that cites further lines describes them, not this citation: its names before the
            # first such citation are the ones of this one
            paren = re.split(r"(?<!\w):\d|\w\.cs:\d", rest[:k + 1], maxsplit=1)[0]
            same = [r for pf, rs in pending if pf == last_file for r in rs]
            groups.extend((pf, rs, [], True) for pf, rs in pending if pf != last_file)
            groups.append((last_file, same, _named(paren), True))
            pending = []
        elif rest.startswith((";", "|")) or not rest:
            groups.extend((pf, rs, [], True) for pf, rs in pending)
            pending = []
    groups.extend((pf, rs, [], True) for pf, rs in pending)
    return groups


def print_citation_section(files, added, view: str, merge_base: str, full: bool, renames: dict[str, str]) -> None:
    print("## verification.md line citations of changed C# files (claims.md §4, fixing-findings.md §12)")
    changed_cs = [p for st, p in files if p.endswith(".cs") and not st.startswith("D")]
    specs = [p for p in git("ls-tree", "-r", "--name-only", view, "openspec/specs", check=False).splitlines()
             if p.endswith("/verification.md")]
    if not changed_cs or not specs:
        print("  none to check")
        print()
        return

    def resolve(token: str) -> str | None:
        token = token.lstrip("./")
        if token in changed_cs:
            return token
        hits = [p for p in changed_cs if p.endswith("/" + token)]
        return hits[0] if len(hits) == 1 else None

    moved_from = {old: new for new, old in renames.items() if new.endswith(".cs")}

    def moved_away(token: str) -> str | None:
        """The new path of a file the branch moved, for a token that names its old path (a file name the move kept
        resolves to the new path)."""
        token = token.lstrip("./")
        hits = [o for o in moved_from if o == token or o.endswith("/" + token)]
        return moved_from[hits[0]] if len(hits) == 1 else None

    @functools.lru_cache(maxsize=None)
    def lines_at(ref: str, path: str) -> tuple[str, ...] | None:
        blob = git_bytes("show", f"{ref}:{path}")
        if blob is None:
            return None
        text = blob.decode("utf-8", "replace").replace("\r\n", "\n")
        # the newline that ends the last line starts no line of its own
        return tuple((text[:-1] if text.endswith("\n") else text).split("\n"))

    @functools.lru_cache(maxsize=None)
    def hunks(spec: str) -> tuple[tuple[int, int, int, int], ...]:
        """(new start, new count, old start, old count) of each hunk of the spec's diff from the merge-base."""
        out = []
        for line in git("diff", "-U0", "--no-color", "--no-ext-diff", f"{merge_base}..{view}", "--", spec,
                        check=False).splitlines():
            if m := re.match(r"@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@", line):
                out.append((int(m.group(3)), int(m.group(4) or 1), int(m.group(1)), int(m.group(2) or 1)))
        return tuple(out)

    def replaced_citations(spec: str, n: int) -> frozenset:
        """(token, ranges) of the citations of the merge-base rows that the hunk holding row `n` replaced: a row the
        branch only touched (a run mark, a word) keeps those, a row it inserted has none."""
        rows = lines_at(merge_base, spec) or ()
        for new_start, new_count, old_start, old_count in hunks(spec):
            if new_start <= n < new_start + new_count:
                old_rows = rows[old_start - 1:old_start - 1 + old_count] if old_count else ()
                return frozenset((t, tuple(r)) for row in old_rows if ".cs" in row
                                 for t, r, _, _ in parse_citations(row))
        return frozenset()

    branch_rows, moved, edited, base_debt = [], [], [], []
    checked = 0
    for spec in specs:
        text = lines_at(view, spec)
        if not text:
            continue
        written = {n for n, _ in added.get(spec, [])}
        for n, row in enumerate(text, 1):
            if ".cs" not in row:
                continue
            for token, ranges, names, exact in parse_citations(row):
                path = resolve(token)
                if path is None and ranges and (moved_to := moved_away(token)):
                    branch_rows.append(f"{spec}:{n} cites {token}, which the branch moved to {moved_to}")
                    continue
                new = lines_at(view, path) if path and ranges else None
                if new is None:
                    continue
                old = lines_at(merge_base, renames.get(path, path))
                # "`Name` (81615)" after a test name is a bug number when it is past the end of the file on both sides
                if not exact and all(a > len(new) and (old is None or a > len(old)) for a, _ in ranges):
                    continue
                checked += 1
                problems = citation_problems(list(new), ranges, names)
                where = f"{spec}:{n} cites {path}:" + ", ".join(f"{a}-{b}" if a != b else str(a) for a, b in ranges)
                # a row the branch touched (a run mark, a word) keeps the citations the rows it replaced had: those
                # are compared with the merge-base like the rows it left alone; a citation the branch wrote is its own
                if old is None or (n in written and (token, tuple(ranges)) not in replaced_citations(spec, n)):
                    branch_rows.extend(f"{where}: {p}" for p in problems)
                    continue
                # matched by what is wrong, not by the file's length: a range already past the end at the merge-base
                # stays base debt when the branch changes the length of the file
                same = lambda p: re.sub(r" \(\d+ lines\)$", "", p)
                old_problems = {same(p) for p in citation_problems(list(old), ranges, names)}
                for p in problems:
                    (base_debt if same(p) in old_problems else branch_rows).append(f"{where}: {p}")
                stripped = [l.rstrip() for l in new]
                for a, b in ranges:
                    if a < 1 or a > b or b > len(old):
                        continue
                    block = [l.rstrip() for l in old[a - 1:b]]
                    if stripped[a - 1:b] == block or not any(block):
                        continue
                    found = [k + 1 for k in range(len(stripped) - len(block) + 1) if stripped[k:k + len(block)] == block]
                    span = f"{a}-{b}" if a != b else str(a)
                    if len(found) == 1:
                        c = found[0]
                        moved.append(f"{spec}:{n} cites {path}:{span}: the cited lines are now at "
                                     + (f"{c}-{c + b - a}" if a != b else str(c)))
                    elif not found:
                        edited.append(f"{spec}:{n} cites {path}:{span}: the branch changed the cited lines")
    print(f"  {checked} citation group(s) of changed .cs files checked")
    for title, rows, gate in (("citations the branch wrote or left pointing at a moved file, and new problems of the "
                               "others", branch_rows, True),
                              ("cited blocks that moved under citations the branch did not write", moved, True),
                              ("cited lines the branch edited under citations it did not write (a hint: does the row hold?)",
                               edited, False),
                              ("already so at the merge-base (base debt, SKILL.md §4; not in the gate)", base_debt, False)):
        print(f"  {title}:")
        print_capped([f"    {'!! ' if gate else ''}{r}" for r in rows], STALE_ROWS_SHOWN, full, "    none")
        if gate:
            GATE.extend(f"[claims.md §4] {r}" for r in rows)
    print()



STATEFUL = re.compile(
    r"\b(BackgroundService|IHostedService|PeriodicTimer|Channel<|SemaphoreSlim|IDistributedLockProvider|"
    r"DistributedLock|StopAsync|IIntegrationEventHandler|Interlocked)\b|(\w*(?:lease|nextattempt|retry)\w*)", re.I)


def print_stateful_section(added) -> None:
    print("## Stateful / background processing (reviewer G, SKILL.md §3 G)")
    rows = []
    for path, lines in sorted(added.items()):
        if not is_cs(path) or is_test(path):
            continue
        kinds = sorted({m.group(1) or m.group(2) for _, t in lines for m in STATEFUL.finditer(t)})
        if len(kinds) >= 2:
            rows.append(f"  {path}: {', '.join(kinds[:8])}")
    print("\n".join(rows) if rows else "  none")
    print()


CHUNK_LINES = 1500


def print_size_section(added) -> None:
    prod = {p: len(ls) for p, ls in added.items() if is_cs(p) and not is_test(p)
            and not p.endswith((".Designer.cs", "ModelSnapshot.cs"))}
    total = sum(prod.values())
    chunks = max(1, -(-total // CHUNK_LINES))
    print("## Size (SKILL.md §3 Coverage)")
    print(f"  added lines in product .cs: {total} in {len(prod)} file(s) -> "
          f"{chunks} chunk(s) of ~{CHUNK_LINES} lines for reviewers B, D, E, G")
    for p, n in sorted(prod.items(), key=lambda x: -x[1])[:10]:
        print(f"    {n:5}  {p}")
    print()


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--base")
    ap.add_argument("--head", default="HEAD")
    ap.add_argument("--since-review", metavar="SHA", help="also show files changed since the previous review")
    ap.add_argument("--worktree", action="store_true")
    ap.add_argument("--full", action="store_true")
    ap.add_argument("--gate", action="store_true")
    a = ap.parse_args()

    head_sha = git("rev-parse", "--verify", f"{a.head}^{{commit}}").strip()
    if a.since_review is not None:
        # a value that starts with "-" would be read as an option (older git has no --end-of-options for rev-parse)
        reviewed = git("rev-parse", "--verify", "--quiet", f"{a.since_review}^{{commit}}", check=False).strip() \
            if a.since_review and not a.since_review.startswith("-") else ""
        if not reviewed:
            fail(f"--since-review: unknown reviewed commit {a.since_review!r}")
        if git("merge-base", reviewed, head_sha, check=False).strip() != reviewed:
            fail(f"--since-review: {reviewed} is not an ancestor of {a.head}. "
                 "Check the review boundary after a rebase or switch to a full review.")
    # "Checked out" is decided by the ref, not the commit: another branch at the same commit is not the branch the
    # working tree belongs to. A detached HEAD matches by commit (a sha, a tag).
    head_ref = git("symbolic-ref", "-q", "HEAD", check=False).strip()
    if a.head == "HEAD":
        checked_out = True
    elif head_ref:
        checked_out = git("rev-parse", "--symbolic-full-name", a.head, check=False).strip() == head_ref
    else:
        checked_out = head_sha == git("rev-parse", "--verify", "HEAD^{commit}", check=False).strip()
    current_branch = git("rev-parse", "--abbrev-ref", "HEAD", check=False).strip()
    if a.worktree and not checked_out:
        # the working tree belongs to the checked-out branch; mixing it with another head's history scans neither
        fail(f"--worktree scans the checked-out working tree, but --head {a.head} is not checked out: "
             "check it out, or leave --worktree out")
    base = a.base or pick_base(a.head)
    merge_base = git("merge-base", base, a.head).strip()
    # With --worktree the scan reads what `git commit -a` would record, not the bytes on disk (clean filters
    # applied, a submodule as its pointer); its tree id is what a Hand-over block records (SKILL.md §8).
    view = worktree_snapshot() if a.worktree else a.head
    diff_args = [f"{merge_base}..{view}"]
    head_label = "working tree" if a.worktree else a.head
    tree = git("rev-parse", f"{view}^{{tree}}").strip()

    branch = current_branch if a.head == "HEAD" else a.head
    commits = git("log", "--oneline", "--no-merges", f"{merge_base}..{a.head}").splitlines()
    merges = git("log", "--oneline", "--merges", f"{merge_base}..{a.head}").splitlines()
    # the state of the working tree matters only for the checked-out branch
    status = git("status", "--porcelain", "--untracked-files=no").splitlines() if checked_out else []

    print(f"# Branch scan: {branch}")
    print(f"base: {base}  merge-base: {merge_base[:10]}  head: {head_label}  tree: {tree}")
    print(f"commits: {len(commits)} (+{len(merges)} merge)")
    for c in commits[:40]:
        print(f"  {c}")
    if len(commits) > 40:
        print(f"  ... and {len(commits) - 40} more")
    # git-commits.md: no AI attribution in the branch's own commit messages
    attributed, commit_bodies = [], []
    for block in git("log", "--no-merges", "--format=%h%x1f%B%x1e", f"{merge_base}..{a.head}").split("\x1e"):
        if "\x1f" not in block:
            continue
        sha, body = block.strip().split("\x1f", 1)
        commit_bodies.append((sha, body))
        if re.search(r"co-authored-by:.*(claude|anthropic|copilot|gpt|openai)|generated with \[?claude|noreply@anthropic\.com", body, re.I):
            attributed.append(sha)
    if attributed:
        print(f"!! [git-commits.md] AI attribution in commit messages: {', '.join(attributed)}")
        GATE.append(f"[git-commits.md] AI attribution in commit messages: {', '.join(attributed)}")
    if status and not a.worktree:
        print(f"!! working tree is dirty ({len(status)} files) - uncommitted changes are NOT scanned; pass --worktree to include them")
    untracked: list[str] = []
    if a.worktree:
        # `git diff` of the working tree sees tracked files only: a new file nobody added is reviewed by no one.
        untracked = [l[3:] for l in git("status", "--porcelain", "--untracked-files=all").splitlines() if l.startswith("?? ")]
        if untracked:
            print(f"!! {len(untracked)} untracked file(s) are NOT scanned - offer the branch's own to the user for `git add` (SKILL.md §1):")
            for p in untracked[:20]:
                print(f"  {p}")
            if len(untracked) > 20:
                print(f"  ... and {len(untracked) - 20} more")
            # A hand-over records the tree git would commit: a file left out of it is reviewed by no one. The review
            # of this branch that the skill saved at the repo root stays untracked by design (SKILL.md §8).
            saved = {f"{current_branch.replace('/', '-')}-review.md", f"{current_branch.replace('/', '-')}-self-review.md"}
            left_out = [p for p in untracked if p not in saved]
            if left_out:
                GATE.append(f"{len(left_out)} untracked file(s): `git add` the branch's own, remove or ignore the rest")
    print()

    # --- files ---------------------------------------------------------------------------------
    if a.since_review is not None:
        print("## Changes since the previous review")
        print(f"  reviewed commit: {reviewed}")
        changes = git("diff", "--name-status", "-M", reviewed, view, "--").splitlines()
        # a merge of the base after the review changes files the branch never touched: the batch is what is also in
        # the branch's own diff against the merge-base
        own = set(git("diff", "--name-only", "-M", *diff_args).splitlines())
        # a file in the branch's own diff at the review and gone from it now was undone by the branch: part of the
        # batch, named; the rest came with a merge of the base
        reviewed_base = git("merge-base", base, reviewed, check=False).strip() or merge_base
        own_then = set(git("diff", "--name-only", "-M", reviewed_base, reviewed, "--", check=False).splitlines())
        batch = [c for c in changes if c.split("\t")[-1] in own]
        undone = [c for c in changes if c.split("\t")[-1] not in own and c.split("\t")[-1] in own_then]
        for change in batch:
            print(f"  {change}")
        for change in undone:
            print(f"  {change}  (back as at the merge-base: the branch undid its change)")
        if not batch and not undone:
            print("  none")
        if rest := len(changes) - len(batch) - len(undone):
            print(f"  {rest} more file(s) changed since the review came with a merge of the base, not listed")
        print()

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
    rules = load_rules(view)
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

    # The reviewer judges by the base's rules and skill (SKILL.md §1). What the base changed in them since the
    # merge-base, and what of that applies to this branch: a rule whose `paths:` match a file of the branch (or
    # that has none), this skill and its scan, prove-red.sh, and the spec rules when the branch touches openspec/.
    branch_paths = [p for _, p in files]
    relevant = []
    for t in git("diff", "--name-only", merge_base, base, "--", *REVIEW_TOOLING).splitlines():
        # The branch already has the base's version (a cherry-pick, the same edit): nothing is missing.
        if git("rev-parse", "-q", "--verify", f"{base}:{t}", check=False).strip() == \
                git("rev-parse", "-q", "--verify", f"{view}:{t}", check=False).strip():
            continue
        if tooling_applies(t, base, branch_paths):
            relevant.append(t)
    if relevant:
        print(f"  !! the base changed rules or review tooling that apply to this branch since the merge-base - bring "
              f"the base in before a hand-over check; judge by the base's rules (SKILL.md §1): {', '.join(relevant)}")
        GATE.append(f"the base changed rules or review tooling that apply to this branch: {', '.join(relevant)}")
    print()

    # A merge of the base that brought rules or review tooling into the branch: a check of the branch made before it
    # ran on the older copy and cannot have seen what the newer one checks (SKILL.md §1 "Earlier checks of the
    # branch", fixing-findings.md §6 "The base first"). Not a gate: the author may have run what changed since.
    print("## Review tooling brought in by the branch (SKILL.md §1 \"Earlier checks of the branch\")")
    tooling_merges = 0
    for line in git("log", "--merges", "--first-parent", "--format=%H %ad", "--date=short",
                    f"{merge_base}..{a.head}").splitlines():
        sha, date = line.split(" ", 1)
        brought = [t for t in git("diff", "--name-only", f"{sha}^1", sha, "--", *REVIEW_TOOLING).splitlines()
                   if tooling_applies(t, sha, branch_paths)]
        if not brought:
            continue
        tooling_merges += 1
        # The branch's own commits only: base commits a later merge brought in are ancestors of the merge-base.
        before = int(git("rev-list", "--count", "--no-merges", f"{sha}^1", f"^{merge_base}").strip())
        after = git("log", "--oneline", "--no-merges", a.head, f"^{sha}", f"^{merge_base}").splitlines()
        shown = ", ".join(brought[:8]) + (f" (+{len(brought) - 8})" if len(brought) > 8 else "")
        print(f"  merge {sha[:10]} ({date}): {shown}")
        print(f"    commits of the branch: {before} before it, {len(after)} after it"
              + (f": {', '.join(c.split(' ', 1)[0] for c in after[:12])}" + (" ..." if len(after) > 12 else "")
                 if after else ""))
    if tooling_merges:
        print("  a check of the branch made before one of these merges ran on the older copy: what the merge changed "
              "runs over the whole branch (fixing-findings.md §6 \"The base first\"); the commits after the newest "
              "merge are checked by the next check only")
    else:
        print("  none")
    # The copy of the rules and the skill a review ran with: a saved review records it (SKILL.md §8), so that a later
    # reader can tell a check made on an older copy.
    stamp = git("log", "-1", "--format=%H %ad", "--date=short", view, "--", *REVIEW_TOOLING).strip()
    if a.worktree and stamp.startswith(view):
        stamp = "the working tree (uncommitted changes)"
    print(f"  tooling of the scanned tree: last changed in {stamp[:10] + stamp[40:] if len(stamp) > 40 else stamp or 'none'}")
    print()

    # --- line checks ---------------------------------------------------------------------------
    added = added_lines(diff_args)
    hits: dict[tuple[str, str], list[str]] = defaultdict(list)
    for path, lines in added.items():
        for rule, label, rx, flt in LINE_CHECKS:
            # "paths:" takes the rule's own `paths:`, so the hint and the rule cannot drift apart
            if not (any(pt.match(path) for pt in rules.get(rule, [])) and is_cs(path) and not is_test(path)
                    if flt == "paths:" else flt(path)):
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

    # empty catch / swallowed failure, across lines (the braces are on lines of their own here), with or without a
    # `when` filter; a catch whose body is only a comment counts as empty too
    for path, lines in added.items():
        if not is_cs(path) or is_test(path):
            continue
        blob = git_bytes("show", f"{view}:{path}")
        if not blob:
            continue
        text = blob.decode("utf-8", "replace")
        added_numbers = {n for n, _ in lines}
        for m in EMPTY_CATCH.finditer(text):
            line = text.count("\n", 0, m.start()) + 1
            if added_numbers & set(range(line, line + m.group(0).count("\n") + 1)):
                hits[("correctness", "* empty catch / swallowed failure - logged, and is the primary action still right?")].append(f"{path}:{line}")

    # ef-compiled-queries.md "Exceptions": an ExecuteUpdate cannot be precompiled, and its method says so in a comment
    # that starts "Not precompiled on purpose:". The comment belongs to the member that holds the call: the comments
    # and attributes right above its declaration, or the lines of the member above the call; a comment of the member
    # above it does not count. With no declaration found, the 30 lines above the call stand in.
    for path, lines in added.items():
        if not is_cs(path) or is_test(path):
            continue
        calls = [(n, t) for n, t in lines if re.search(r"\.ExecuteUpdate(?:Async)?\s*\(", t)
                 and not t.strip().startswith(("//", "/*", "*"))]
        if not calls:
            continue
        source = (git_bytes("show", f"{view}:{path}") or b"").decode("utf-8", "replace").splitlines()
        for n, t in calls:
            indent = len(t) - len(t.lstrip())
            decl = next((k for k in range(n, 0, -1) if MEMBER_DECLARATION.match(source[k - 1])
                         and len(source[k - 1]) - len(source[k - 1].lstrip()) <= indent), None)
            if decl is None:
                own = source[max(0, n - 31):n]
            else:
                top = decl
                while top > 1 and source[top - 2].lstrip().startswith(("//", "/*", "*", "[")):
                    top -= 1
                own = source[top - 1:n]
            if "Not precompiled on purpose:" not in "\n".join(own):
                hits[("ef-compiled-queries.md", "ExecuteUpdate with no \"Not precompiled on purpose:\" comment on the "
                      "member that holds it")].append(f"{path}:{n}  {t.strip()[:140]}")

    # license header on new source files - not on generated files (a `*.Designer.cs`, an `<auto-generated>` header)
    # and not on vendored code under thirdparty/, which carry no header of ours by design
    for st, p in files:
        if st.startswith("A") and p.endswith((".cs", ".ts")) and not p.startswith(("migrations/", "sdk/", "thirdparty/")) \
                and not p.endswith(".Designer.cs"):
            blob = git_bytes("show", f"{view}:{p}")
            head_text = blob[:600].decode("utf-8", "replace") if blob is not None else ""
            if blob is not None and "<auto-generated" not in head_text and not any(m in head_text for m in LICENSE_MARKS):
                hits[("csharp-style.md", "new file without the AGPL header")].append(p)
                GATE.append(f"[csharp-style.md] new file without the AGPL header: {p}")

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
        new = git_bytes("show", f"{view}:{p}")
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
            GATE.append(f"{p}: line endings switched {so} -> {sn} for the whole file")
    print("\n".join(flips) if flips else "  none")
    print()

    check = git("diff", "--check", *diff_args, check=False).splitlines()
    ws = [l for l in check if not l.startswith(("+", "-"))]
    GATE.extend(l for l in ws if "conflict marker" in l)
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
    # the submodule checkouts belong to the checked-out branch: for another --head they say nothing about it
    sub = [l for l in git("submodule", "status").splitlines() if l[:1] in "+-U"] if checked_out else []
    for l in sub:
        print(f"  !! submodule checkout differs from the recorded pointer: {l.strip()}")
    # A documented response code or operation the branch removes from a service's OpenAPI document is a change of
    # the contract the SDKs and agents read: is it one the endpoint really cannot return any more?
    removed = []
    for ns in name_status:
        st, p = ns[0], ns[-1]
        if not re.search(r"/json/\w+_2\.0\.json$", p) or st.startswith("A"):
            continue
        old_doc, new_doc = openapi_responses(merge_base, ns[1]), ({} if st.startswith("D") else openapi_responses(view, p))
        if old_doc is None or new_doc is None:
            removed.append(f"    {p}: not compared (not an OpenAPI document with a `paths` object on "
                           f"{'the merge-base' if old_doc is None else 'the reviewed tree'})")
            continue
        for op, codes in sorted(old_doc.items()):
            if op not in new_doc:
                removed.append(f"    {p}: operation {op} removed")
            elif codes - new_doc[op]:
                removed.append(f"    {p}: {op}: {', '.join(sorted(codes - new_doc[op]))} removed")
    print("  response codes and operations removed from the OpenAPI documents (json/*_2.0.json):")
    print_capped(removed, 30, a.full, "    none")
    print()

    read_head = lambda p: git_bytes("show", f"{view}:{p}")
    endpoints = print_endpoints_section(files, added, read_head)
    endpoint_names = [name for name, _, _ in endpoints]
    old_paths = [ns[1] for ns in name_status if ns[0].startswith(("R", "D")) and len(ns) > 1]
    renames = {ns[-1]: ns[1] for ns in name_status if ns[0].startswith("R") and len(ns) > 2}
    print_openspec_section(files, endpoint_names, view, merge_base, old_paths, untracked, a.full, renames)
    print_citation_section(files, added, view, merge_base, a.full, renames)
    print_operation_texts_section(list(dict.fromkeys(CHANGED_ACTIONS)), view, merge_base, a.gate, renames)
    print_claims_section(added, commit_bodies, a.full)
    # A commit no remote-tracking ref reaches is not pushed yet: its message can still be fixed (claims.md §4).
    unpushed = set(git("log", "--no-merges", "--format=%h", f"{merge_base}..{a.head}", "--not", "--remotes",
                       check=False).split())
    print_texts_section(added, endpoints, files, untracked, commit_bodies, unpushed, a.full, view)
    print_stateful_section(added)
    print_size_section(added)

    # --- localized resources -------------------------------------------------------------------
    print_resx_section(files, merge_base, view)

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
    # fixing-findings.md §6, the batch check, point 4: a behavioural fix is proven red, or its commit says it is not
    # proven and why. A fix commit is told by its subject (`fix:`, `fix(area):`, "Fix ..."); a fix committed under
    # another subject is not seen.
    unproven = []
    files_of: dict[str, list[str]] = {}
    for block in git("log", "--no-merges", "--name-only", "--format=%x1e%h", f"{merge_base}..{a.head}").split("\x1e"):
        parts = [x for x in block.strip().splitlines() if x]
        if parts:
            files_of[parts[0]] = parts[1:]
    for sha, body in commit_bodies:
        if not re.match(r"\s*fix\b", body, re.I):
            continue
        if re.search(r"\bRED as expected\b", body) or re.search(r"\bnot proven\b", body, re.I):
            continue
        if any(is_cs(p) and not is_test(p) for p in files_of.get(sha, [])):
            unproven.append(f"    {sha} {body.strip().splitlines()[0][:100]}")
    print("  fix commits that change product .cs with no prove-red result and no \"not proven\" line:")
    print_capped(unproven, 20, a.full, "    none")
    print()

    # --- new projects --------------------------------------------------------------------------
    # A project the solutions do not list is built by no one, and a test project that CLAUDE.md does not
    # name reads, by CLAUDE.md's own wording, as a legacy one that `dotnet test` skips.
    new_projects = [p for st, p in files if st.startswith("A") and p.endswith(".csproj")
                    and not p.startswith(("sdk/", "thirdparty/"))]
    print("## New projects")
    if not new_projects:
        print("  none")
    else:
        solutions = {s: (read_head(s) or b"").decode("utf-8", "replace") for s in ("ASC.Web.slnx", "ASC.Tests.slnx")}
        claude_md = (read_head("CLAUDE.md") or b"").decode("utf-8", "replace")
        for p in new_projects:
            listed = [s for s, text in solutions.items() if f'Path="{p}"' in text]
            notes = []
            if not listed:
                notes.append("in neither ASC.Web.slnx nor ASC.Tests.slnx (fine only for a tool built by a solution of its own)")
            if is_test(p) and not p.startswith("products/") and Path(p).stem not in claude_md:
                notes.append("a test project CLAUDE.md does not name (its Testing section calls the unnamed ones legacy)")
            mark = "!! " if notes else ""
            print(f"  {mark}{p}: {', '.join(listed) or '-'}" + (f" - {'; '.join(notes)}" if notes else ""))
    print()

    # --- junk ----------------------------------------------------------------------------------
    junk = [p for st, p in files if st.startswith("A") and (
        re.search(r"(\.binlog|\.log|\.env|\.user|\.orig|\.rej|launchSettings\.local\.json)$", p)
        or ("/" not in p and p.endswith(".md") and p not in ("README.md", "CLAUDE.md")))]
    print("## Possibly committed by accident")
    print("\n".join(f"  {p}" for p in junk) if junk else "  none")
    print()

    # --- checklists of the applicable rules -------------------------------------------------------
    applicable = [name for name, pats in rules.items()
                  if f"{RULES_DIR.as_posix()}/{name}" not in NOT_A_REVIEW_CRITERION
                  and (not pats or applies.get(name) or f"{RULES_DIR.as_posix()}/{name}" in ALWAYS_APPLIES)]
    print_checklists_section(view, applicable)

    # --- gate ----------------------------------------------------------------------------------
    # Only what stops a hand-over whatever the branch is, so a script stops it, not a reader (SKILL.md §8 "Hand-over
    # block"). The lines are the ones --gate's help lists; every other section above stays a hint.
    print("## Gate (hard failures - `--gate` exits with 1 when there is any)")
    print("\n".join(f"  !! {g}" for g in GATE) if GATE else "  none")
    if a.gate and GATE:
        sys.exit(1)


if __name__ == "__main__":
    try:
        main()
    except SystemExit:
        raise
    except Exception:  # an exit code of 1 is reserved for --gate's hard failures (fail())
        import traceback
        traceback.print_exc()
        sys.exit(2)
