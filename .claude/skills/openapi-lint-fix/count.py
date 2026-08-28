"""Aggregate a Spectral markdown report into the counts lint-memory.md section 2 is built from.

Usage: python .claude/skills/openapi-lint-fix/count.py [lint/report.md]
"""

import collections
import re
import sys

BS = chr(92)


def rows(path):
    with open(path, encoding="utf-8") as f:
        for i, line in enumerate(f):
            if i < 2 or not line.startswith("|"):
                continue
            cells = [c.strip() for c in line.strip().strip("|").split("|")]
            if len(cells) < 7:
                continue
            code, jsonpath, message, severity, start, _end, source = cells[:7]
            # `code` is a markdown link for spectral:oas rules, bare text for custom ones.
            code = re.sub(r"^\[([^\]]+)\].*", r"\1", code)
            document = source.replace(BS, "").split("/")[-1]
            yield code, jsonpath.replace(BS, ""), message, severity, start, document


def main():
    path = sys.argv[1] if len(sys.argv) > 1 else "lint/report.md"
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
