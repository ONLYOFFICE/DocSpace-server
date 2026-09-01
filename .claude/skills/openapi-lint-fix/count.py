"""Aggregate a Spectral JSON report into the counts lint-memory.md section 2 is built from.

Usage: python .claude/skills/openapi-lint-fix/count.py [lint/report.json]

Reads the `-f json` output, not the markdown one. The markdown table was the original input and
parsing it worked, but every field arrived needing repair: the rule code came wrapped in a markdown
link for `spectral:oas` rules and bare for custom ones, the JSON path came backslash-escaped, and the
severity came as a word whose spelling the formatter owns. The JSON carries all four as data. Emitted
output is unchanged, so the numbers quoted in SKILL.md and the ruleset's `# measured:` comments still
mean the same thing.
"""

import collections
import json
import sys

# Spectral's DiagnosticSeverity, in the spelling the markdown formatter used, so that a count copied
# into a ruleset comment reads the same as it always has.
SEVERITY = {0: "Error", 1: "Warning", 2: "Information", 3: "Hint"}


def json_path(segments):
    """Render Spectral's path array the way the report renders it, so the two can be grepped alike.

    Numeric segments are array indices and the markdown formatter writes them `[0]`, not `.0`.
    Spectral sends them as strings, so test the value rather than the type.
    """
    out = ""
    for segment in segments:
        text = str(segment)
        if text.isdigit():
            out += f"[{text}]"
        else:
            out += f".{text}" if out else text
    return out


def rows(path):
    with open(path, encoding="utf-8") as f:
        findings = json.load(f)

    for finding in findings:
        start = finding.get("range", {}).get("start", {})
        yield (
            finding.get("code", ""),
            json_path(finding.get("path", [])),
            finding.get("message", ""),
            SEVERITY.get(finding.get("severity"), str(finding.get("severity"))),
            f"{start.get('line', '')}:{start.get('character', '')}",
            # `source` is an absolute path whose separator follows the platform Spectral ran on.
            finding.get("source", "").replace(chr(92), "/").split("/")[-1],
        )


def main():
    path = sys.argv[1] if len(sys.argv) > 1 else "lint/report.json"
    by_severity = collections.Counter()
    by_document = collections.Counter()
    by_document_severity = collections.Counter()
    by_rule = collections.Counter()
    rule_documents = collections.defaultdict(collections.Counter)
    errors = []
    total = 0

    for code, jsonpath, message, severity, start, document in rows(path):
        total += 1
        by_severity[severity] += 1
        by_document[document] += 1
        by_document_severity[(document, severity)] += 1
        by_rule[(code, severity)] += 1
        rule_documents[code][document] += 1
        if severity == "Error":
            errors.append((document, code, jsonpath, message, start))

    print(f"TOTAL {total}")
    print("\n== severity ==")
    for k, v in by_severity.most_common():
        print(f"{k:10} {v}")
    print("\n== document ==")
    for k, v in by_document.most_common():
        print(f"{k:22} {v}")
    print("\n== document x severity ==")
    for (d, s), v in sorted(by_document_severity.items()):
        print(f"{d:22} {s:10} {v}")
    print("\n== rule (one lint-memory.md section-2 row each, already in table order) ==")
    for (code, severity), v in by_rule.most_common():
        spread = ", ".join(f"{d} {n}" for d, n in rule_documents[code].most_common())
        print(f"{v:6} {severity:10} {code:38} {spread}")
    print("\n== Error findings ==")
    for document, code, jsonpath, message, start in errors:
        print(f"{document:16} {code:26} {jsonpath}  -> {message}  @{start}")


if __name__ == "__main__":
    main()
