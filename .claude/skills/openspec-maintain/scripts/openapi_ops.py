"""Operations of the published OpenAPI document, shared by op_coverage.py and api_test_coverage.py.

The document is read line by line instead of with a YAML library, so the scripts need nothing beyond the standard
library: a path is a two-space key under `paths:`, a method a four-space key under it, and `operationId` and the
first entry of `tags` are six-space keys of the method.
"""
import os
import re

YAML = "sdk/docspace-api-spec/docspace-backend.yaml"


def read_operations(yaml_path=YAML):
    """Return [(operationId, METHOD, path, tag)] in document order."""
    if not os.path.exists(yaml_path):
        raise SystemExit(f"{yaml_path} not found: run from the repository root with the submodules initialised")
    lines = open(yaml_path, encoding="utf-8").read().split("\n")
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
    return ops
