"""List the operations of the published OpenAPI document that no openspec/specs/*/spec.md names.

Usage: python -I -X utf8 .claude/skills/openspec-maintain/scripts/op_coverage.py [--out <file>]
Run from the repository root. Read-only over the repository.
"""
import argparse
import collections
import glob
import os
import re
import sys

sys.dont_write_bytecode = True  # no __pycache__ in the skill folder
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))  # -I leaves the script folder off the path
from openapi_ops import read_operations  # noqa: E402

parser = argparse.ArgumentParser()
parser.add_argument("--out", help="write the missing operations grouped by tag to this file")
args = parser.parse_args()

ops = read_operations()  # (operationId, method, path, tag)

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
