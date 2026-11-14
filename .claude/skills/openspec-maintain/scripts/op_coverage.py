"""List the operations of the published OpenAPI document that no openspec/specs/*/spec.md names.

Usage: python -I -X utf8 .claude/skills/openspec-maintain/scripts/op_coverage.py [--out <file>]
Run from the repository root. Read-only over the repository.
"""
import argparse
import collections
import glob
import os
import re

parser = argparse.ArgumentParser()
parser.add_argument("--out", help="write the missing operations grouped by tag to this file")
args = parser.parse_args()

yaml_path = "sdk/docspace-api-spec/docspace-backend.yaml"
if not os.path.exists(yaml_path):
    raise SystemExit(f"{yaml_path} not found: run from the repository root with the submodules initialised")

lines = open(yaml_path, encoding="utf-8").read().split("\n")
ops = []  # (operationId, method, path, tag)
in_paths = False
path = None
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

text = "\n".join(open(f, encoding="utf-8").read() for f in glob.glob("openspec/specs/*/spec.md"))
missing = [o for o in ops if not re.search(r"\b" + re.escape(o[0]) + r"\b", text)]
print(f"operations in yaml: {len(ops)}; named in specs: {len(ops) - len(missing)}; missing: {len(missing)}")

total = collections.Counter(o[3] for o in ops)
by_tag = collections.defaultdict(list)
for o in missing:
    by_tag[o[3]].append(o)
report = []
for tag, items in sorted(by_tag.items(), key=lambda kv: -len(kv[1])):
    report.append(f"## {tag} ({len(items)}/{total[tag]})")
    report.extend(f"{o[0]}  {o[1]} {o[2]}" for o in items)
if args.out:
    open(args.out, "w", encoding="utf-8").write("\n".join(report) + "\n")
    print("written:", args.out)
else:
    print("\n".join(report))
