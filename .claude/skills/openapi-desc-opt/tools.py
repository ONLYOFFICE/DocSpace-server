"""The readability tools of the skill and their stand-in, shared by scan.py and check_texts.py.

Vale and vacuum are optional. When one is on this machine it gives the verdict on the form of a
description with the settings in readability/; when it is not, the same thresholds are computed here
(`form_metrics`, a port of readability/vacuum/functions/description_form.js). So the queue follows the
tools where they exist and still works where they do not.

Finding the tools: a flag, else VALE_BIN / VACUUM_BIN, else PATH.
"""
import json
import os
import re
import shutil
import subprocess

HERE = os.path.dirname(os.path.abspath(__file__))
VALE_DIR = os.path.join(HERE, "readability", "vale")
VACUUM_DIR = os.path.join(HERE, "readability", "vacuum")

def _ruleset_limits():
    """The thresholds of readability/vacuum/ruleset.yaml (the numbers of description-rules.md §4),
    read from it so that the stand-in and the tool can never disagree on them."""
    limits = {}
    with open(os.path.join(VACUUM_DIR, "ruleset.yaml"), encoding="utf-8") as f:
        for check, value in re.findall(r"check: (\w+), (?:max|min): (\d+)", f.read()):
            limits[check] = int(value)
    return limits


def _check_vale_limits(limits):
    """The Docs style of Vale carries two of the same thresholds; a drift would make the tools disagree."""
    styles = os.path.join(VALE_DIR, "styles", "Docs")
    for file, pattern, check in (("SentenceLength.yml", r"(?m)^max: (\d+)", "sentenceLength"),
                                 ("Grade.yml", r"(?m)^condition: \"> (\d+)\"", "grade")):
        with open(os.path.join(styles, file), encoding="utf-8") as f:
            found = re.search(pattern, f.read())
        if not found or int(found.group(1)) != limits.get(check):
            raise SystemExit("readability/vale/styles/Docs/{0} disagrees with readability/vacuum/ruleset.yaml "
                             "on {1}: make the two the same".format(file, check))


_LIMITS = _ruleset_limits()
_check_vale_limits(_LIMITS)
MAX_SENTENCE_WORDS = _LIMITS["sentenceLength"]
MAX_AVERAGE_WORDS = _LIMITS["averageSentence"]
MIN_SENTENCES = _LIMITS["minSentences"]
MAX_GRADE = _LIMITS["grade"]


def find_binary(flag, env, name):
    for candidate in (flag, os.environ.get(env)):
        if candidate and os.path.isfile(candidate):
            return candidate
    return shutil.which(name)


def discover(vale=None, vacuum=None, use_tools=True):
    """{'vale': path or None, 'vacuum': path or None}."""
    if not use_tools:
        return dict(vale=None, vacuum=None)
    return dict(vale=find_binary(vale, "VALE_BIN", "vale"), vacuum=find_binary(vacuum, "VACUUM_BIN", "vacuum"))


def source_name(found):
    names = [n for n, key in (("Vale", "vale"), ("vacuum", "vacuum")) if found.get(key)]
    return " + ".join(names) if names else "own checks"


def published(raw):
    """The description as a reader gets it: the webhook sentence the generator appends as a paragraph
    of its own joins the text, single line breaks of the XML wrap stay (they are no paragraphs)."""
    return re.sub(r"\s*\n\s*\n\s*(?=Raises the )", " ", raw or "")


# --- the stand-in: description_form.js in Python ---------------------------------------------------

def _clean(text):
    text = re.sub(r"`[^`]*`", " Code ", str(text))
    text = re.sub(r"https?://\S+", " URL ", text)
    text = re.sub(r"[*_#>|]", " ", text)
    return re.sub(r"\s+", " ", text).strip()


def _sentences(text):
    parts = re.sub(r"([.!?])\s+(?=[A-Z(0-9])", "\\1\0", text).split("\0")
    return [s for s in parts if re.search(r"[A-Za-z]", s)]


def _syllables(word):
    word = word.lower()
    if len(word) <= 3:
        return 1
    word = re.sub(r"(?:[^laeiouy]es|ed|[^laeiouy]e)$", "", word)
    word = re.sub(r"^y", "", word)
    return max(1, len(re.findall(r"[aeiouy]{1,2}", word)))


def form_metrics(raw):
    """Sentence lengths, average, paragraphs and Flesch-Kincaid grade, counted the way vacuum does."""
    text = _clean(raw)
    sentences = _sentences(text)
    lengths = [len(re.findall(r"\w+", s)) for s in sentences]
    letters = re.findall(r"[A-Za-z][A-Za-z'-]*", text)
    n_sent, n_words = max(1, len(sentences)), max(1, len(letters))
    grade = 0.39 * (len(letters) / n_sent) + 11.8 * (sum(_syllables(w) for w in letters) / n_words) - 15.59
    return dict(
        lengths=lengths,
        average=round(sum(lengths) / n_sent, 1),
        paragraphs=bool(re.search(r"\n\s*\n|^\s*[-*] |^\s*\d+\. ", str(raw), re.M)),
        grade=round(grade, 1))


# --- the tools ---------------------------------------------------------------------------------------

def vale_config(work):
    with open(os.path.join(VALE_DIR, ".vale.ini"), encoding="utf-8") as f:
        ini = f.read()
    styles = os.path.join(VALE_DIR, "styles")
    has_microsoft = os.path.isdir(os.path.join(styles, "Microsoft"))
    ini = re.sub(r"(?m)^StylesPath = .*$", "StylesPath = " + styles.replace("\\", "/"), ini)
    if not has_microsoft:
        ini = re.sub(r"(?m)^Packages = .*\n", "", ini)
        ini = ini.replace("BasedOnStyles = Docs, Microsoft", "BasedOnStyles = Docs")
        ini = re.sub(r"(?m)^Microsoft\..*\n", "", ini)
    path = os.path.join(work, ".vale.ini")
    with open(path, "w", encoding="utf-8") as f:
        f.write(ini)
    return path, has_microsoft


def run_vale(binary, texts, work, docs_only=False):
    """{key: [(check, severity, match, message)]} for {key: text}, and a note on the styles used.

    `docs_only` keeps the run to the Docs styles, which carry the thresholds — the Microsoft
    suggestions are for reading one text, not for sorting hundreds.
    """
    folder = os.path.join(work, "vale")
    os.makedirs(folder, exist_ok=True)
    names = {}
    for n, (key, text) in enumerate(texts.items()):
        name = "{0:04d}.md".format(n)
        with open(os.path.join(folder, name), "w", encoding="utf-8") as f:
            f.write(re.sub(r"\s+", " ", text).strip() + "\n")
        names[name] = key
    config, has_microsoft = vale_config(work)
    if docs_only and has_microsoft:
        with open(config, encoding="utf-8") as f:
            ini = f.read()
        ini = re.sub(r"(?m)^Packages = .*\n", "", ini).replace("BasedOnStyles = Docs, Microsoft", "BasedOnStyles = Docs")
        with open(config, "w", encoding="utf-8") as f:
            f.write(re.sub(r"(?m)^Microsoft\..*\n", "", ini))
    proc = subprocess.run([binary, "--config", config, "--output=JSON", "."], cwd=folder,
                          capture_output=True, text=True, encoding="utf-8")
    try:
        data = json.loads(proc.stdout or "{}")
    except ValueError:
        return None, "Vale failed: " + (proc.stderr or proc.stdout).strip()[:300]
    out = {}
    for file, alerts in data.items():
        key = names.get(os.path.basename(file))
        if key is not None:
            out[key] = [(a.get("Check"), a.get("Severity"), a.get("Match"), a.get("Message")) for a in alerts]
    if docs_only or has_microsoft:
        note = "styles Docs" + (", Microsoft" if has_microsoft and not docs_only else "")
    else:
        note = "styles Docs only — Microsoft not synced: vale --config {0} sync".format(
            os.path.relpath(os.path.join(VALE_DIR, ".vale.ini")).replace("\\", "/"))
    return out, note


def run_vacuum(binary, entries, work):
    """{key: [(code, message)]} for [(key, path, verb, text)], and a note."""
    spec = dict(openapi="3.0.1", info=dict(title="description check", version="1"), paths={})
    by_route = {}
    for n, (key, path, verb, text) in enumerate(entries):
        spec["paths"].setdefault(path, {})[verb] = dict(
            operationId="op{0}".format(n), description=text, responses={"200": dict(description="OK")})
        by_route[(path, verb)] = key
    spec_path = os.path.join(work, "vacuum-spec.json")
    with open(spec_path, "w", encoding="utf-8") as f:
        json.dump(spec, f, ensure_ascii=False, indent=1)
    env = dict(os.environ, VACUUM_NO_UPDATE_CHECK="true")
    proc = subprocess.run([binary, "spectral-report", "-q", "-n", "-o",
                           "-r", os.path.join(VACUUM_DIR, "ruleset.yaml"),
                           "-f", os.path.join(VACUUM_DIR, "functions"), spec_path],
                          capture_output=True, text=True, encoding="utf-8", env=env)
    start = (proc.stdout or "").find("[")
    try:
        data = json.loads(proc.stdout[start:]) if start >= 0 else None
    except ValueError:
        data = None
    if data is None:
        return None, "vacuum failed: " + (proc.stderr or proc.stdout or "").strip()[:300]
    out = {}
    for r in data:
        # vacuum splits the path on every dot, the one in `api/2.0` included: join it back.
        m = re.match(r"paths\['(.*?)'\]\.(\w+)\.description$", ".".join(r.get("path") or []))
        key = by_route.get((m.group(1), m.group(2))) if m else None
        if key is not None:
            out.setdefault(key, []).append((r.get("code"), r.get("message")))
    return out, "ruleset readability/vacuum"


# --- the verdict on the form, for the queue ----------------------------------------------------------

def form_verdicts(entries, found, work):
    """Per key, how the description departs from the form: {long, paragraphs, average, grade}.

    `long` is the number of sentences over the limit, `average` and `grade` are set only when over
    theirs. Each value comes from a tool where one is installed and covers it, else from
    `form_metrics`; a description flagged by either tool is flagged. Returns (verdicts, source, notes).
    """
    vale = vacuum = None
    notes = []
    if found.get("vale"):
        vale, note = run_vale(found["vale"], {k: t for k, _, _, t in entries}, work, docs_only=True)
        notes.append("Vale: " + note)
    if found.get("vacuum"):
        vacuum, note = run_vacuum(found["vacuum"], entries, work)
        notes.append("vacuum: " + note)

    verdicts = {}
    for key, _, _, text in entries:
        own = form_metrics(text)
        long_own = sum(1 for n in own["lengths"] if n > MAX_SENTENCE_WORDS)
        v = dict(long=None, paragraphs=None, average=None, grade=None)
        if vacuum is not None:
            alerts = vacuum.get(key, [])
            v["long"] = sum(1 for c, _ in alerts if c == "desc-sentence-length")
            v["paragraphs"] = any(c == "desc-one-paragraph" for c, _ in alerts)
            for c, msg in alerts:
                number = re.search(r"(\d+(?:\.\d+)?)", msg or "")
                if c == "desc-average-sentence" and number:
                    v["average"] = float(number.group(1))
                if c == "desc-grade" and number:
                    v["grade"] = float(number.group(1))
        if vale is not None:
            alerts = vale.get(key, [])
            v["long"] = max(v["long"] or 0, sum(1 for a in alerts if a[0] == "Docs.SentenceLength"))
            for a in alerts:
                number = re.search(r"(\d+(?:\.\d+)?)", a[3] or "")
                if a[0] == "Docs.Grade" and number:
                    v["grade"] = max(v["grade"] or 0, float(number.group(1)))
        if v["long"] is None:
            v["long"] = long_own
        if v["paragraphs"] is None:
            v["paragraphs"] = own["paragraphs"]
        if vacuum is None and own["average"] > MAX_AVERAGE_WORDS:
            v["average"] = own["average"]
        if vacuum is None and vale is None and own["grade"] > MAX_GRADE:
            v["grade"] = own["grade"]
        v["longest"] = max(own["lengths"] or [0])
        verdicts[key] = v
    if vale is None and vacuum is None:
        source = "own checks"
    else:
        source = " + ".join(n for n, t in (("Vale", vale), ("vacuum", vacuum)) if t is not None)
        if vale is None or vacuum is None:
            source += " + own checks"
    return verdicts, source, notes
