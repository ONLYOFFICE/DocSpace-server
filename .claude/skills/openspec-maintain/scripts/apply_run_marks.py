"""Write the outcomes of a full test run onto the TEST-CONFIRMED rows of openspec/specs/*/verification.md.

Usage (from the repository root):
    python -I -X utf8 .claude/skills/openspec-maintain/scripts/apply_run_marks.py <out-dir> [--ru <dir>] [--dry-run]

<out-dir> is the folder run_full.sh wrote: results/*.trx (the full run), optional rerun/**/*.trx (failed classes
re-run alone), commit.txt and date.txt. Each TEST-CONFIRMED row gets one mark from the tests its
`File.cs::Method` citations (or, in older rows, `File.cs:line`) resolve to; the run legend under the title is
replaced. Idempotent.

--ru <dir>: also mark translated copies at <dir>/<spec>/verification.md when they hold the same number of
TEST-CONFIRMED rows. The legend text comes from <dir>/run-legend.txt ({commit} and {date} are filled in); the
text before {commit} identifies the old legend line to replace.
"""
import argparse
import collections
import glob
import os
import re
import xml.etree.ElementTree as ET

NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
TAG = {
    "pass": "PASS",
    "rerun": "PASS-ON-RERUN",
    "bug": "FAIL-BUG",
    "fail": "FAIL",
    "skip": "SKIP",
    "notrun": "NOT-IN-RUN",
    "nolink": "NO-LINK",
}
LEGEND_EN = (
    "> **Test run {commit}** (`dotnet test ASC.Tests.slnx --max-parallel-test-modules 1`, {date}), written by script. "
    "After TEST-CONFIRMED: `[run: PASS]` the cited tests passed; `[run: PASS-ON-RERUN]` failed in the full run and passed alone; "
    "`[run: FAIL-BUG]` a cited test failed and carries a bug mark, so the row may describe a defect; `[run: FAIL]` failed without a bug mark, repeated alone; "
    "`[run: SKIP]` only skipped; `[run: NOT-IN-RUN]` cited test not in the results; `[run: NO-LINK]` no test citation the script could resolve. "
    "PASS means the cited test passed, not that it proves the claim. Line ranges map to test methods approximately."
)
MARK = re.compile(r" \[run: [A-Z-]+\]")
# A test is cited by name, `File.cs::Method` (several: `File.cs::A, B`), which survives every edit of the file, or,
# in older rows, by lines, `File.cs:12-40`, which moves with every edit above it.
# A further name after a comma starts with a capital or an underscore, as test methods do, is not the file of the
# next citation (`B.cs::X`) and ends the item (a comma, a backtick, `;`, `)`, `|` or the end follows), so that the
# prose after a citation is not read as a name - the same as NAME_CITATION in review-branch/scan.py.
CITE = re.compile(r"([A-Za-z_0-9]+\.cs)(?:::([A-Za-z_]\w*(?:\s*,\s*[A-Z_]\w*(?![\w.]*::|\.\w)(?=\s*(?:[,`;)|]|$)))*)"
                  r"|:([0-9][0-9,\s\-]*))?")
METHOD = re.compile(r"^\s*(?:public|internal)\s+(?:async\s+)?(?:Task|ValueTask|void)\s+(\w+)\s*\(")

parser = argparse.ArgumentParser()
parser.add_argument("out")
parser.add_argument("--ru")
parser.add_argument("--dry-run", action="store_true")
args = parser.parse_args()


def load(pattern):
    res = collections.defaultdict(lambda: collections.defaultdict(list))
    for trx in glob.glob(pattern, recursive=True):
        root = ET.parse(trx).getroot()
        ids = {}
        for ut in root.iter(NS + "UnitTest"):
            tm = ut.find(NS + "TestMethod")
            if tm is not None:
                ids[ut.get("id")] = (tm.get("className") or "", tm.get("name") or "")
        for r in root.iter(NS + "UnitTestResult"):
            cls, meth = ids.get(r.get("testId"), ("", ""))
            name = (meth or r.get("testName") or "").split("(")[0].split(".")[-1]
            res[cls.split(",")[0].split(".")[-1]][name].append(r.get("outcome"))
    return res


first = load(os.path.join(args.out, "results", "*.trx"))
if not first:
    raise SystemExit(f"no TRX files in {args.out}/results")
rerun = load(os.path.join(args.out, "rerun", "**", "*.trx"))
commit = open(os.path.join(args.out, "commit.txt")).read().strip()[:10]
date_file = os.path.join(args.out, "date.txt")
date = open(date_file).read().strip() if os.path.exists(date_file) else "date unknown"

# test methods per file: (line, method, bug-marked)
sources = collections.defaultdict(list)
for path in glob.glob("**/*Tests*/**/*.cs", recursive=True):
    p = path.replace("\\", "/")
    if "/bin/" in p or "/obj/" in p or "worktrees" in p:
        continue
    lines = open(path, encoding="utf-8-sig", errors="replace").read().split("\n")
    for i, line in enumerate(lines, 1):
        m = METHOD.match(line)
        if not m:
            continue
        window = "\n".join(lines[max(0, i - 15) : i - 1]).split("}\n")[-1]
        if not re.search(r"\[(Fact|Theory|SkipFact|Retry)", window):
            continue
        bug = bool(re.search(r'Trait\("Bug",|Trait\("Category",\s*"Bug"\)', window))
        sources[os.path.basename(path)].append((i, m.group(1), bug))


def classify(row):
    found, linked = [], False
    for fn, names, rng in CITE.findall(row):
        tests = sources.get(fn)
        if not tests:
            continue
        chosen = tests
        if names.strip():
            wanted_names = {n.strip() for n in names.split(",")}
            chosen = [t for t in tests if t[1] in wanted_names]
            if not chosen:
                # a name no test method of the file has: the citation does not resolve, the same as no citation
                continue
        linked = True
        if not names.strip() and rng.strip():
            wanted = []
            for part in re.split(r",\s*", rng.strip()):
                p = part.strip().split("-")
                if p[0].isdigit():
                    wanted.append((int(p[0]), int(p[-1]) if p[-1].isdigit() else int(p[0])))
            chosen = []
            for a, b in wanted:
                # a citation may start on the [Fact]/[Trait] lines just above the declaration; a wider window
                # pulls in the next test when a row cites the last assertion of the previous one
                hit = [t for k, t in enumerate(tests)
                       if a <= (tests[k + 1][0] - 1 if k + 1 < len(tests) else 10**9) and b >= t[0] - 3]
                if not hit:
                    # a citation of the doc comment above a test: the first test declared within 12 lines after it
                    hit = [t for t in tests if b < t[0] <= b + 12][:1]
                chosen += [t for t in hit if t not in chosen]
        for t in chosen:
            r = first.get(fn[:-3], {}).get(t[1])
            if r:
                found.append((fn[:-3], t[1], t[2], r))
    if not linked:
        return "nolink"
    if not found:
        return "notrun"
    states = []
    for cls, meth, bug, r in found:
        if "Failed" in r:
            again = rerun.get(cls, {}).get(meth)
            states.append("rerun" if again and "Failed" not in again else ("bug" if bug else "fail"))
        elif all(o in ("NotExecuted", "NotRunnable", "Inconclusive", "Skipped") for o in r):
            states.append("skip")
        else:
            states.append("pass")
    for s in ("fail", "bug", "rerun"):
        if s in states:
            return s
    return "pass" if "pass" in states else "skip"


def rewrite(path, tags, legend, prefix):
    raw = open(path, encoding="utf-8", newline="").read()
    nl = "\r\n" if "\r\n" in raw else "\n"
    lines = [l for l in raw.split(nl) if not l.startswith(prefix)]
    k = 0
    for i, line in enumerate(lines):
        if line.startswith("|") and "TEST-CONFIRMED" in line:
            lines[i] = MARK.sub("", line).replace("TEST-CONFIRMED", f"TEST-CONFIRMED [run: {TAG[tags[k]]}]", 1)
            k += 1
    title = next(i for i, l in enumerate(lines) if l.startswith("#"))
    lines[title + 1 : title + 1] = ["", legend]
    # exactly one blank line after the legend, whatever the old file had there
    after = title + 3
    while after < len(lines) and lines[after] == "":
        del lines[after]
    lines.insert(after, "")
    out = []
    for l in lines:
        if l == "" and len(out) >= 2 and out[-1] == "" and out[-2] == "":
            continue
        out.append(l)
    if not args.dry_run:
        open(path, "w", encoding="utf-8", newline="").write(nl.join(out))


# The old legend line is found by the text before {commit}, which never changes between runs.
legend_en = LEGEND_EN.format(commit=commit, date=date)
prefix_en = LEGEND_EN.split("{commit}")[0]
legend_ru = prefix_ru = None
if args.ru:
    lf = os.path.join(args.ru, "run-legend.txt")
    if not os.path.exists(lf):
        raise SystemExit(f"--ru given but {lf} is missing")
    template_ru = open(lf, encoding="utf-8").read().strip()
    if "{commit}" not in template_ru or not template_ru.split("{commit}")[0].strip():
        raise SystemExit(f"{lf} must start with fixed text followed by {{commit}}")
    legend_ru = template_ru.format(commit=commit, date=date)
    prefix_ru = template_ru.split("{commit}")[0]

total = collections.Counter()
print(f"{'spec':<28}" + "".join(f"{v:>14}" for v in TAG.values()) + ("  copy" if args.ru else ""))
for vf in sorted(glob.glob("openspec/specs/*/verification.md")):
    spec = os.path.basename(os.path.dirname(vf))
    rows = [MARK.sub("", l) for l in open(vf, encoding="utf-8").read().split("\n") if l.startswith("|") and "TEST-CONFIRMED" in l]
    tags = [classify(r) for r in rows]
    rewrite(vf, tags, legend_en, prefix_en)
    c = collections.Counter(tags)
    total.update(c)
    note = ""
    if args.ru:
        cf = os.path.join(args.ru, spec, "verification.md")
        if not os.path.exists(cf):
            note = "none"
        else:
            n = sum(1 for l in open(cf, encoding="utf-8").read().split("\n") if l.startswith("|") and "TEST-CONFIRMED" in l)
            if n == len(tags):
                rewrite(cf, tags, legend_ru, prefix_ru)
                note = "ok"
            else:
                note = f"MISMATCH {n}/{len(tags)}"
    print(f"{spec:<28}" + "".join(f"{c[k]:>14}" for k in TAG) + (f"  {note}" if note else ""))
print(f"{'TOTAL':<28}" + "".join(f"{total[k]:>14}" for k in TAG))
