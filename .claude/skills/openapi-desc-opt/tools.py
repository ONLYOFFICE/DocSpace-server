"""The readability tools of the skill and their stand-in, shared by scan.py and check_texts.py.

Vale and vacuum are optional. The same thresholds are always computed here as well (`form_metrics`, a
port of readability/vacuum/functions/description_form.js), and the verdict on the form is the worst of
the tools that ran and this count: a tool can add a finding, never take one away. So the queue is
never milder with a tool than without it.

Every run of a tool carries a canary, a description it has to flag. A tool that ran but left the canary
unflagged checked nothing; its verdict is dropped and the note says so (`FAILED: …`).

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


# The canary: one sentence of 28 plain words and a code span. Vale skips the code span, so the plain
# words alone are over the limit for it too. It never reaches a caller: the run_* functions drop it.
CANARY = ("This canary sentence is long on purpose, because every working form check has to flag a sentence "
          "of more than twenty words like this one with `code` in it.")
# vacuum reports a path with a dot as one quoted name and a path without one as separate segments;
# a canary of each kind proves both are read back.
CANARY_ROUTES = (("/internal/desc-opt-canary", "get"), ("/api/2.0/desc-opt-canary", "get"))
_CANARY = object()

# Seconds a Vale or vacuum run may take before it counts as failed; a full queue takes well under a minute.
TOOL_TIMEOUT = 300


def _json_array(text):
    """The first `[` of `text` that opens a JSON array, or None. vacuum may print a banner or an ANSI
    escape before its report, so the first `[` of the output is not always the report."""
    decoder = json.JSONDecoder()
    for m in re.finditer(r"\[", text):
        try:
            value, _ = decoder.raw_decode(text, m.start())
        except ValueError:
            continue
        if isinstance(value, list):
            return value
    return None


def _canary_failure(rule, where=""):
    return ("FAILED: the canary, a {0}-word sentence{1}, got no {2} — the tool checked nothing, so its "
            "verdict is dropped and the own checks judge").format(max(form_metrics(CANARY)["lengths"]), where, rule)


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
        paragraphs=has_paragraphs(raw),
        grade=round(grade, 1))


# A blank line, a bullet at the start of a line, or a numbered item where CommonMark starts a list: at
# the start of the text, or at the start of a line only when the number is 1. A wrapped line of the XML
# comment that begins with a status code ("…the call returns\n403. A…") continues the paragraph and is
# no list. readability/vacuum/functions/description_form.js holds the same two expressions.
_PARAGRAPH_OR_LIST = re.compile(r"\n\s*\n|^\s*[-*+] |^\s*1[.)] ", re.M)
_LIST_AT_START = re.compile(r"\s*\d{1,9}[.)] ")


def has_paragraphs(raw):
    text = str(raw)
    return bool(_PARAGRAPH_OR_LIST.search(text) or _LIST_AT_START.match(text))


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
    suggestions are for reading one text, not for sorting hundreds. Returns None in place of the
    alerts when Vale failed or left the canary unflagged.
    """
    folder = os.path.join(work, "vale")
    os.makedirs(folder, exist_ok=True)
    names = {}
    for n, (key, text) in enumerate(list(texts.items()) + [(_CANARY, CANARY)]):
        name = "canary.md" if key is _CANARY else "{0:04d}.md".format(n)
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
    try:
        proc = subprocess.run([binary, "--config", config, "--output=JSON", "."], cwd=folder,
                              capture_output=True, text=True, encoding="utf-8", timeout=TOOL_TIMEOUT)
    except (OSError, subprocess.SubprocessError) as e:
        return None, "Vale failed: " + str(e)[:300]
    try:
        data = json.loads(proc.stdout or "{}")
    except ValueError:
        return None, "Vale failed: " + (proc.stderr or proc.stdout).strip()[:300]
    out = {}
    for file, alerts in data.items():
        key = names.get(os.path.basename(file))
        if key is not None:
            out[key] = [(a.get("Check"), a.get("Severity"), a.get("Match"), a.get("Message")) for a in alerts]
    canary = out.pop(_CANARY, [])
    if not any(a[0] == "Docs.SentenceLength" for a in canary):
        return None, _canary_failure("Docs.SentenceLength")
    if docs_only or has_microsoft:
        note = "styles Docs" + (", Microsoft" if has_microsoft and not docs_only else "")
    else:
        note = "styles Docs only — Microsoft not synced: vale --config {0} sync".format(
            os.path.relpath(os.path.join(VALE_DIR, ".vale.ini")).replace("\\", "/"))
    return out, note


# The path of a vacuum finding, joined back on the dots vacuum split it on. A route with a dot comes as
# one quoted name, `paths['/api/2.0/files'].get.description` (`"` when the route holds a `'`); a route
# without one as separate segments, `paths./internal/ai/agents/agentquota.put.description`.
_VACUUM_PATH = re.compile(r"""^paths(?:\['(?P<single>.*)'\]|\["(?P<double>.*)"\]|\.(?P<bare>[^.'"\[]*))"""
                          r"""\.(?P<verb>\w+)\.description$""")


def _vacuum_route(segments):
    m = _VACUUM_PATH.match(".".join(segments or []))
    if not m:
        return None
    return next(g for g in (m.group("single"), m.group("double"), m.group("bare")) if g is not None), m.group("verb")


def run_vacuum(binary, entries, work):
    """{key: [(code, message)]} for [(key, path, verb, text)], and a note. Returns None in place of
    the findings when vacuum failed, left a canary unflagged or reported a finding on no known route."""
    spec = dict(openapi="3.0.1", info=dict(title="description check", version="1"), paths={})
    by_route = {}
    canaries = [(_CANARY, path, verb, CANARY) for path, verb in CANARY_ROUTES]
    for n, (key, path, verb, text) in enumerate(list(entries) + canaries):
        spec["paths"].setdefault(path, {})[verb] = dict(
            operationId="op{0}".format(n), description=text, responses={"200": dict(description="OK")})
        by_route[(path, verb)] = key
    spec_path = os.path.join(work, "vacuum-spec.json")
    with open(spec_path, "w", encoding="utf-8") as f:
        json.dump(spec, f, ensure_ascii=False, indent=1)
    env = dict(os.environ, VACUUM_NO_UPDATE_CHECK="true")
    argv = [binary, "spectral-report", "-q", "-n", "-o",
            "-r", os.path.join(VACUUM_DIR, "ruleset.yaml"),
            "-f", os.path.join(VACUUM_DIR, "functions"), spec_path]
    # On Windows a .cmd/.bat shim (what npm installs) runs through cmd.exe, which parses the arguments
    # again: a path with one of these characters would be split or run as a command.
    if binary.lower().endswith((".cmd", ".bat")) and any(c in a for a in argv[1:] for c in '&|<>^%"'):
        return None, ("vacuum failed: {0} is a cmd shim and a path holds one of & | < > ^ % \"; use another "
                      "--work folder or point --vacuum at vacuum.exe").format(binary)
    try:
        proc = subprocess.run(argv, capture_output=True, text=True, encoding="utf-8", env=env,
                              timeout=TOOL_TIMEOUT)
    except (OSError, subprocess.SubprocessError) as e:
        return None, "vacuum failed: " + str(e)[:300]
    data = _json_array(proc.stdout or "")
    if data is None:
        return None, "vacuum failed: " + (proc.stderr or proc.stdout or "").strip()[:300]
    out, flagged, lost = {}, set(), []
    for r in data:
        if not str(r.get("code") or "").startswith("desc-"):
            continue  # not a rule of readability/vacuum/ruleset.yaml, whose rules are all named desc-…
        route = _vacuum_route(r.get("path"))
        if route not in by_route:
            lost.append(".".join(r.get("path") or []))
        elif by_route[route] is _CANARY:
            if r.get("code") == "desc-sentence-length":
                flagged.add(route)
        else:
            out.setdefault(by_route[route], []).append((r.get("code"), r.get("message")))
    if lost:
        # A finding no route takes would be dropped in silence and read as a clean description.
        return None, ("FAILED: {0} finding(s) on a path no operation takes, first {1} — the verdict is "
                      "dropped and the own checks judge").format(len(lost), lost[0])
    missed = [path for path, verb in CANARY_ROUTES if (path, verb) not in flagged]
    if missed:
        return None, _canary_failure("desc-sentence-length", " at " + ", ".join(missed))
    return out, "ruleset readability/vacuum"


# --- the verdict on the form, for the queue ----------------------------------------------------------

def form_verdicts(entries, found, work):
    """Per key, how the description departs from the form: {long, paragraphs, average, grade}.

    `long` is the number of sentences over the limit, `average` and `grade` are set only when over
    theirs. Each value is the worst of `form_metrics`, always counted, and the tools that ran: a
    description flagged by any of them is flagged. A tool that failed or left its canary unflagged
    gives no verdict, and its note says so. Returns (verdicts, source, notes); `source` names the tools
    whose verdict was used and adds "own checks" when one is missing or failed — the label of a run
    with both stays "Vale + vacuum", the one the manifests of earlier batches hold.
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
    def worst(current, number):
        return number if current is None or number > current else current

    for key, _, _, text in entries:
        own = form_metrics(text)
        v = dict(long=sum(1 for n in own["lengths"] if n > MAX_SENTENCE_WORDS),
                 paragraphs=own["paragraphs"],
                 average=own["average"] if own["average"] > MAX_AVERAGE_WORDS else None,
                 grade=own["grade"] if own["grade"] > MAX_GRADE else None)
        if vacuum is not None:
            alerts = vacuum.get(key, [])
            v["long"] = max(v["long"], sum(1 for c, _ in alerts if c == "desc-sentence-length"))
            v["paragraphs"] = v["paragraphs"] or any(c == "desc-one-paragraph" for c, _ in alerts)
            for c, msg in alerts:
                number = re.search(r"(\d+(?:\.\d+)?)", msg or "")
                if c == "desc-average-sentence" and number:
                    v["average"] = worst(v["average"], float(number.group(1)))
                if c == "desc-grade" and number:
                    v["grade"] = worst(v["grade"], float(number.group(1)))
        if vale is not None:
            alerts = vale.get(key, [])
            v["long"] = max(v["long"], sum(1 for a in alerts if a[0] == "Docs.SentenceLength"))
            for a in alerts:
                number = re.search(r"(\d+(?:\.\d+)?)", a[3] or "")
                if a[0] == "Docs.Grade" and number:
                    v["grade"] = worst(v["grade"], float(number.group(1)))
        v["longest"] = max(own["lengths"] or [0])
        verdicts[key] = v
    if vale is None and vacuum is None:
        source = "own checks"
    else:
        source = " + ".join(n for n, t in (("Vale", vale), ("vacuum", vacuum)) if t is not None)
        if vale is None or vacuum is None:
            source += " + own checks"
    return verdicts, source, notes
