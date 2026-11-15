"""Which API operations of the published OpenAPI document the integration tests call.

Usage (from the repository root):
    python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py [--out <file.md>] [--spec <name>]

For every operation of sdk/docspace-api-spec/docspace-backend.yaml it looks at the source of the test projects of
ASC.Tests.slnx (every project whose name contains ".Tests", ASC.Tests.Common included) and decides:

- sdk:  a test calls the generated SDK method, `.<OperationId>Async(` or `.<OperationId>WithHttpInfoAsync(`
        (the C# SDK names its methods after the operationId in PascalCase);
- raw:  no SDK call, but a string literal in a test holds the route (`api/2.0/...`, path parameters matched as
        any segment or an interpolation); the HTTP method is not checked, so a raw hit is a lead, not proof;
- none: neither.

It also tells, per operation, whether the method exists in the SDK source at all (sdk/docspace-api-sdk-csharp):
an operation the SDK lacks can only be tested raw. And it names the spec whose spec.md mentions the operationId,
so the gaps can be queued per spec (mode B of the openspec-maintain skill).

This is a text scan, not code navigation: a helper method of the same name called with a receiver counts as a
call, and a call only in a helper that no test uses counts too. Read-only over the repository.
"""
import argparse
import collections
import glob
import os
import re

parser = argparse.ArgumentParser()
parser.add_argument("--out", help="write the per-spec report as Markdown to this file")
parser.add_argument("--spec", help="report only the operations this spec names")
args = parser.parse_args()

YAML = "sdk/docspace-api-spec/docspace-backend.yaml"
SDK = "sdk/docspace-api-sdk-csharp/src/DocSpace.API.SDK/Api"
SLNX = "ASC.Tests.slnx"
for p in (YAML, SDK, SLNX):
    if not os.path.exists(p):
        raise SystemExit(f"{p} not found: run from the repository root with the submodules initialised")

# --- operations of the published document: (operationId, METHOD, path, tag)
lines = open(YAML, encoding="utf-8").read().split("\n")
ops = []
in_paths, path = False, None
for i, line in enumerate(lines):
    if line.startswith("paths:"):
        in_paths = True
        continue
    if in_paths and re.match(r"^[a-zA-Z]", line):
        in_paths = False
    if not in_paths:
        continue
    m = re.match(r"^  (/\S*):\s*$", line)
    if m:
        path = m.group(1)
        continue
    m = re.match(r"^    (get|post|put|delete|patch):\s*$", line)
    if not m:
        continue
    op = tag = None
    j = i + 1
    while j < len(lines) and not re.match(r"^ {0,4}\S", lines[j]):
        mo = re.match(r"^      operationId:\s*(\S+)", lines[j])
        if mo:
            op = mo.group(1)
        if re.match(r"^      tags:\s*$", lines[j]) and j + 1 < len(lines):
            tag = lines[j + 1].strip().lstrip("- ").strip("'\"")
        j += 1
    if op:
        ops.append((op, m.group(1).upper(), path, tag or "?"))


def pascal(name):
    return name[:1].upper() + name[1:]


# --- methods the SDK source declares
sdk_methods = set()
for f in glob.glob(os.path.join(SDK, "**", "*.cs"), recursive=True):
    for m in re.finditer(r"\b(?:public|internal)\s+async\s+[^(]*?\s(\w+?)(?:WithHttpInfo)?Async\s*\(", open(f, encoding="utf-8", errors="replace").read()):
        sdk_methods.add(m.group(1))

# --- test sources
test_dirs = []
for m in re.finditer(r'Path="([^"]+\.csproj)"', open(SLNX, encoding="utf-8").read()):
    proj = m.group(1)
    if ".Tests" in os.path.basename(proj):
        test_dirs.append(os.path.dirname(proj))
files = {}
for d in test_dirs:
    for f in glob.glob(os.path.join(d, "**", "*.cs"), recursive=True):
        p = f.replace("\\", "/")
        if "/bin/" in p or "/obj/" in p:
            continue
        files[p] = open(f, encoding="utf-8", errors="replace").read()

calls = collections.defaultdict(set)  # PascalName -> files
for p, text in files.items():
    for m in re.finditer(r"\.(\w+?)(?:WithHttpInfo)?Async\s*\(", text):
        calls[m.group(1)].add(p)

literals = []  # (file, literal) of strings that look like routes
for p, text in files.items():
    for m in re.finditer(r'\$?@?"((?:[^"\\\n]|\\.)*api/2\.0/(?:[^"\\\n]|\\.)*)"', text):
        literals.append((p, m.group(1)))


def route_regex(path):
    rest = path.lstrip("/")
    if rest.startswith("api/2.0/"):
        rest = rest[len("api/2.0/"):]
    out = ""
    for part in re.split(r"(\{[^}]+\})", rest):
        if part.startswith("{"):
            out += r"(?:\{[^}]+\}|[^/\"?{}]+)"
        else:
            out += re.escape(part)
    return re.compile(r"api/2\.0/" + out + r"(?:$|[?\"])")


# --- which spec names each operation
spec_text = {os.path.basename(os.path.dirname(f)): open(f, encoding="utf-8").read() for f in glob.glob("openspec/specs/*/spec.md")}
spec_of = {}
for op, *_ in ops:
    hits = [s for s, t in spec_text.items() if re.search(r"\b" + re.escape(op) + r"\b", t)]
    spec_of[op] = hits

rows = []
for op, method, path, tag in ops:
    name = pascal(op)
    in_sdk = name in sdk_methods
    sdk_files = calls.get(name, set()) if in_sdk else set()
    raw_files = set()
    if not sdk_files:
        rx = route_regex(path)
        raw_files = {p for p, lit in literals if rx.search(lit)}
    kind = "sdk" if sdk_files else ("raw" if raw_files else "none")
    rows.append(dict(op=op, method=method, path=path, tag=tag, in_sdk=in_sdk, kind=kind,
                     files=sorted(sdk_files or raw_files), specs=spec_of[op]))

if args.spec:
    rows = [r for r in rows if args.spec in r["specs"]]

total = len(rows)
cnt = collections.Counter(r["kind"] for r in rows)
not_in_sdk = sum(1 for r in rows if not r["in_sdk"])
print(f"operations: {total}; called through the SDK: {cnt['sdk']}; raw route only: {cnt['raw']}; "
      f"not called: {cnt['none']}; missing from the SDK source: {not_in_sdk}")
print(f"test projects scanned: {len(test_dirs)}, files: {len(files)}")

per_spec = collections.defaultdict(collections.Counter)
for r in rows:
    for s in (r["specs"] or ["(no spec)"]):
        per_spec[s][r["kind"]] += 1
print(f"\n{'spec':<28}{'ops':>6}{'sdk':>6}{'raw':>6}{'none':>6}")
for s, c in sorted(per_spec.items(), key=lambda kv: -kv[1]["none"]):
    print(f"{s:<28}{sum(c.values()):>6}{c['sdk']:>6}{c['raw']:>6}{c['none']:>6}")

if args.out:
    out = ["# API operations and the tests that call them", "",
           f"Operations: {total}. Through the SDK: {cnt['sdk']}. Raw route only: {cnt['raw']} (HTTP method not "
           f"checked). Not called: {cnt['none']}. Missing from the SDK source: {not_in_sdk}.", ""]
    for s in sorted(per_spec, key=lambda s: -per_spec[s]["none"]):
        sel = [r for r in rows if s in (r["specs"] or ["(no spec)"]) and r["kind"] != "sdk"]
        if not sel:
            continue
        c = per_spec[s]
        out += [f"## {s} — {c['none']} not called, {c['raw']} raw only", "",
                "| operationId | method | path | tag | in SDK | tests |", "|---|---|---|---|---|---|"]
        for r in sorted(sel, key=lambda r: (r["kind"] != "none", r["tag"], r["op"])):
            where = ", ".join(os.path.basename(f) for f in r["files"][:3]) or "—"
            out.append(f"| `{r['op']}` | {r['method']} | `{r['path']}` | {r['tag']} | {'yes' if r['in_sdk'] else 'no'} | {where} |")
        out.append("")
    open(args.out, "w", encoding="utf-8", newline="\n").write("\n".join(out))
    print(f"\nwritten {args.out}")
