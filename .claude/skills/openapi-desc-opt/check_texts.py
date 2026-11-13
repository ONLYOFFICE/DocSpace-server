"""Form and readability check of rewritten operation descriptions (references/description-rules.md §6).

What it checks, per operation:
- its own checks, always: sentence length and count, the word order and plain words of §4 and §5.3,
  the place of access and effect, the repeat statement, the operations the text names (do they
  exist in a document), and the status codes against the old text, the declared responses and the
  parameters (§1.5). A code the text gives to another operation (`GET api/2.0/x` returns 404, every
  save call returns 402) is not a code of this one and takes no part in that comparison;
- textstat, Vale and vacuum, each only when it is found on this machine. A tool that is missing is
  skipped and named on the last line, `MISSING TOOLS:`.

Which operations: the description sites of the batch in batch-manifest.json, or the ones named with
--op. Where the new text comes from:
- --from source (default): the `<remarks>` in the C# sources, and the declared codes from the
  [SwaggerResponse] attributes of the action; the old text is the description in the document, so
  run it after the edit and before the document is regenerated;
- --from document: the published description, webhook sentence included; run it after step 6.

Finding the tools: --vale / VALE_BIN, --vacuum / VACUUM_BIN, else PATH; textstat is imported by this
interpreter, else by --textstat-python / TEXTSTAT_PYTHON.

--check-tools runs every tool it finds on two texts with a known verdict and says whether each one
works with the settings of readability/ (references/tools-setup.md, step 4); exit code 1 when one
does not.
"""
import argparse
import html
import json
import os
import re
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.dont_write_bytecode = True  # no __pycache__ inside the skill
import scan  # noqa: E402  (the same index, matching and paths as the queue)
import tools as readability  # noqa: E402  (Vale, vacuum and their stand-in, shared with scan.py)

MANIFEST = os.path.join(HERE, "batch-manifest.json")
RULES = os.path.join(HERE, "references", "description-rules.md")

DESCRIPTION_CHECKS = {"no-description", "thin-description", "off-form-description", "heavy-description"}
WRITE_VERBS = {"post", "put", "delete", "patch"}
# §1.5: the usual reasons to leave a code out of the text.
PIPELINE_CODES = {"401", "500", "502", "503"}

CODE_SPAN = re.compile(r"`[^`]*`")
STATUS = re.compile(r"(?<![\w/{.-])([45]\d\d)(?![\w}])(?!\.\d)")
ACCESS = re.compile(r"\b(Requires|can call it|Anyone can|Only (the|a|an)\b|Sign the request)")
# Open access refuses nobody, so no refusal code follows it; "except a guest" is a refusal.
OPEN_ACCESS = re.compile(r"^(Anyone|Any signed-in user)\b(?![^.]*\bexcept\b)[^.]*?\bcan call it\b")
# A refusal that is no error: the sentence after the access phrase says what other callers get instead.
REFUSAL_WITHOUT_ERROR = re.compile(r"\b(instead of an error|not an error|(gets|returns) 200)\b")
# The call itself refuses: "The call refuses …", "It also rejects …" — not "a license the service rejects".
REFUSAL_BY_CALL = re.compile(r"^(The call|This call|It)\s+(also\s+)?(refuses|rejects|denies)\b")
SWAGGER_RESPONSE = re.compile(r'\[SwaggerResponse\(\s*(\d{3})\s*(?:,\s*"((?:[^"\\]|\\.)*)")?')
# The subject of `returns N` / `gets N` when the code belongs to other operations, not to this one:
# a named operation (OPREF after masking) or "every branding save call", "other calls".
OTHER_CALLS = re.compile(r"\bOPREF\b|\b(every|other|those|these)\s+"
                         r"(?!(repeated|second|next|later|further|following)\b)([\w-]+\s+){0,3}calls?\b", re.I)
CODE_VERB = re.compile(r"\b(returns?|gets?)\b")
EFFECT = re.compile(r"\b(read-only|changes data|cannot be undone|spends|queues)\b")
# The effect is said of the call; "queues" in a sentence about something else is no effect statement.
CALL_SUBJECT = re.compile(r"^(The call|This call|It)\b")
REPEAT = re.compile(r"\brepeat", re.I)
OPERATION_REF = re.compile(r"\b(GET|POST|PUT|DELETE|PATCH) /?((?:api/\d+\.\d+|internal)/[^\s`,;)]*[^\s`,;).])")
WORD_ORDER = [
    (re.compile(r",\s+or (the call|it) (returns|gets|fails)"), "`or` in the sense of otherwise"),
    (re.compile(r"\bunless\b", re.I), "`unless`"),
    (re.compile(r"^So (does|do|is|are|did|can)\b"), "inversion"),
    (re.compile(r"\bidempotent\b", re.I), "`idempotent` (§2.4 uses the repeat phrases)"),
]


# --- inputs ------------------------------------------------------------------------------------

def plain_words():
    """The "Instead of" column of §5.3, read from the rules themselves so the two never drift apart."""
    with open(RULES, encoding="utf-8") as f:
        text = f.read()
    section = text.split("### 5.3", 1)[-1].split("\n### ", 1)[0]
    out = []
    for line in section.splitlines():
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) >= 2 and cells[0].startswith("`"):
            out.extend(re.findall(r"`([^`]+)`", cells[0]))
    return out


def published_routes(root):
    """`verb path` of every operation in every document of the json folder, placeholders blanked."""
    folder = os.path.join(root, scan.JSON_DIR)
    routes = set()
    for name in sorted(os.listdir(folder)) if os.path.isdir(folder) else []:
        if not name.endswith(".json"):
            continue
        try:
            with open(os.path.join(folder, name), encoding="utf-8-sig") as f:
                paths = json.load(f).get("paths") or {}
        except (ValueError, OSError, AttributeError):
            continue
        for path, item in paths.items():
            for verb in item:
                routes.add(route_ref(verb, path))
    return routes


def route_ref(verb, path):
    return "{0} {1}".format(verb.lower(), re.sub(r"\{[^}]*\}", "{}", path.strip("/").lower()))


# --- text --------------------------------------------------------------------------------------

def flatten(text):
    """One paragraph, code kept: the generator joins wrapped lines, and the webhook sentence that it
    appends as a paragraph of its own is part of the text a reader sees."""
    return re.sub(r"\s+", " ", html.unescape(text or "")).strip()


def sentences(text):
    """Sentences of the flattened text, each as written (code kept)."""
    masked = CODE_SPAN.sub(lambda m: "\0" * len(m.group(0)), text)
    cuts = [m.end() for m in re.finditer(r"[.!?](?=\s+[A-Z(0-9\0])", masked)]
    out, start = [], 0
    for cut in cuts + [len(text)]:
        piece = text[start:cut].strip()
        if re.search(r"[A-Za-z]", CODE_SPAN.sub("", piece)) or "`" in piece:
            out.append(piece)
        start = cut
    return out


def words(sentence):
    """Vale's count (`\\w+`, so a hyphenated word is two), except that code in backticks is one word."""
    return len(re.findall(r"\w+", CODE_SPAN.sub(" Code ", sentence)))


def codes(text):
    return set(STATUS.findall(CODE_SPAN.sub(lambda m: m.group(0) if re.match(r"`[45]\d\d`", m.group(0)) else " ", text or "")))


def split_codes(text, own_route):
    """(codes of this operation, codes the text gives to other operations).

    A code is another operation's when the subject of the `returns` or `gets` before it, in its own
    clause, names an operation other than `own_route` or other calls: "Also, `GET api/2.0/migration/logs`
    returns 404", "every branding save call returns 402". Everything else is this operation's."""
    def other(verb, path):
        return " OPREF " if route_ref(verb, path) != own_route else " "

    def mask(m):
        span = m.group(0)
        if re.match(r"`[45]\d\d`$", span):
            return span
        ref = OPERATION_REF.search(span)
        return other(*ref.groups()) if ref else " "

    own, others = set(), set()
    for sentence in sentences(text or ""):
        masked = OPERATION_REF.sub(lambda r: other(*r.groups()), CODE_SPAN.sub(mask, sentence))
        for m in STATUS.finditer(masked):
            clause = re.split(r"[,;:]", masked[:m.start()])[-1]
            verbs = list(CODE_VERB.finditer(clause))
            subject = clause[verbs[-2].end() if len(verbs) > 1 else 0:verbs[-1].start()] if verbs else ""
            (others if OTHER_CALLS.search(subject) else own).add(m.group(1))
    return own, others


SOURCE_FILES = {}


def source_responses(root, action):
    """{code: text} of the [SwaggerResponse] attributes of the action, read from its C# file: the
    attribute block above the route attribute and the attributes between it and the method."""
    path = os.path.join(root, action["file"])
    if path not in SOURCE_FILES:
        text = scan.read_text(path)
        SOURCE_FILES[path] = (text, scan.mask_comments(text))
    text, masked = SOURCE_FILES[path]
    start = sum(len(line) + 1 for line in text.split("\n")[:action["line"] - 1])
    decl = scan.METHOD_RE.search(masked, start)
    region = scan.block_above(text, start) + "\n" + text[start:decl.start() if decl else start]
    region = "\n".join(line for line in region.splitlines() if not re.match(r"\s*//(?!/)", line))
    return {m.group(1): (m.group(2) or "").replace('\\"', '"') for m in SWAGGER_RESPONSE.finditer(region)}


# --- operations --------------------------------------------------------------------------------

def parse_op(spec):
    verb, _, path = spec.strip().partition(" ")
    return verb.lower(), path.strip()


def targets(args):
    if args.op:
        known = scan.projects(args.root)
        if args.doc not in known:
            sys.exit("--op needs --doc, one of: " + ", ".join(known))
        return args.doc, [parse_op(o) for o in args.op]
    if not os.path.exists(MANIFEST):
        sys.exit("no batch-manifest.json and no --op: nothing to check")
    with open(MANIFEST, encoding="utf-8") as f:
        manifest = json.load(f)
    ops = sorted({o for s in manifest["sites"] if s["check"] in DESCRIPTION_CHECKS for o in s["ops"]})
    if not ops:
        print("the batch ({0}) rewrites no operation description: nothing for this check".format(manifest["check"]))
        sys.exit(0)
    if manifest["doc"] not in scan.projects(args.root):
        sys.exit("the manifest names the document `{0}`, which no C# project emits now: run scan.py --batch again"
                 .format(manifest["doc"]))
    return manifest["doc"], [parse_op(o) for o in ops]


def param_texts(op, components):
    """Every description an agent sees next to the operation: parameters and the body, nested."""
    out = [p.get("description", "") for p in op.get("parameters", []) if isinstance(p, dict)]

    def walk(schema, depth=0):
        if depth > 4 or not isinstance(schema, dict):
            return
        if "$ref" in schema:
            walk(components.get(schema["$ref"].split("/")[-1], {}), depth + 1)
            return
        out.append(schema.get("description", ""))
        for value in (schema.get("properties") or {}).values():
            walk(value, depth + 1)
        walk(schema.get("items"), depth + 1)
        for key in ("allOf", "oneOf", "anyOf"):
            for value in schema.get(key, []):
                walk(value, depth + 1)

    for media in ((op.get("requestBody") or {}).get("content") or {}).values():
        walk(media.get("schema"))
    return " ".join(t for t in out if t)


def collect(root, doc_key, wanted, source):
    meta = scan.projects(root)[doc_key]
    doc_path = os.path.join(root, scan.JSON_DIR, meta["doc"])
    with open(doc_path, encoding="utf-8-sig") as f:
        doc = json.load(f)
    components = (doc.get("components") or {}).get("schemas") or {}
    operations = scan.load_operations(doc_path)
    defaults = scan.default_response_texts(operations, scan.generator_response_texts(root))
    matched = {}
    if source:
        controllers = scan.index_sources(root, meta["project"])
        matched, _ = scan.match(operations, controllers)

    by_key = {scan.route_key(o["verb"], o["path"]): o for o in operations}
    items = []
    for verb, path in wanted:
        op = by_key.get(scan.route_key(verb, path))
        if not op:
            items.append(dict(name="{0} {1}".format(verb.upper(), path), error="not in " + meta["doc"]))
            continue
        raw = doc["paths"][op["path"]][op["verb"]]
        declared = sorted(c for c, text in op["responses"].items()
                          if c[:1] in "45" and (c, scan.norm(text)) not in defaults)
        item = dict(name="{0} {1}".format(op["verb"].upper(), op["path"].lstrip("/")), verb=op["verb"],
                    path=op["path"], operation_id=op["operation_id"], declared=declared,
                    declared_all=sorted(c for c in op["responses"] if c[:1] in "45"),
                    params=param_texts(raw, components), raw=op["description"])
        if source:
            candidates = matched.get((op["verb"], op["path"]))
            if not candidates:
                item["error"] = "no C# action matched this operation"
                items.append(item)
                continue
            action = candidates[0][1]
            # The attributes, not the document: a [SwaggerResponse] added with the new text counts before
            # the document is regenerated. The generator's own responses still come from the document.
            attrs = source_responses(root, action)
            item.update(text=flatten(action["remarks"]), old=flatten(op["description"]),
                        where="{0}:{1} {2}.{3}".format(action["file"], action["line"], action["cls"], action["method"]),
                        declared=sorted(c for c, text in attrs.items()
                                        if c[:1] in "45" and (c, scan.norm(text)) not in defaults),
                        declared_all=sorted({c for c in attrs if c[:1] in "45"}
                                            | {c for c, text in op["responses"].items()
                                               if c[:1] in "45" and (c, scan.norm(text)) in defaults}))
        else:
            item.update(text=flatten(op["description"]), old=None, where=meta["doc"])
        items.append(item)
    return items


# --- own checks --------------------------------------------------------------------------------

def own_checks(item, source, words_to_replace, routes):
    text, notes = item["text"], []
    sents = sentences(text)
    counts = [words(s) for s in sents]
    total = sum(counts)
    item["stats"] = dict(words=total, sentences=len(sents), average=round(total / max(1, len(sents)), 1),
                         longest=max(counts or [0]))
    if len(sents) < readability.MIN_SENTENCES:
        notes.append(("FORM", "{0} sentences, fewer than {1} (§4)".format(len(sents), readability.MIN_SENTENCES)))
    for n, (s, c) in enumerate(zip(sents, counts), 1):
        if c > readability.MAX_SENTENCE_WORDS:
            notes.append(("FORM", "sentence {0} has {1} words (§4): {2}".format(n, c, s)))
    if item["stats"]["average"] > readability.MAX_AVERAGE_WORDS:
        notes.append(("FORM", "average {0} words per sentence, more than {1} (§4)".format(
            item["stats"]["average"], readability.MAX_AVERAGE_WORDS)))
    if not source and readability.has_paragraphs(readability.published(item["raw"])):
        notes.append(("FORM", "more than one paragraph or a list (§4)"))

    first = CODE_SPAN.sub("", sents[0]).split() if sents else []
    if first and not (first[0][0].isupper() and first[0].endswith("s")) and first[0] != "Deprecated":
        notes.append(("ORDER", "the first sentence does not start with a present-tense verb (§2.1)"))
    access = [n for n, s in enumerate(sents) if ACCESS.search(s)]
    effect = [n for n, s in enumerate(sents) if EFFECT.search(s)]
    said_of_call = [n for n in effect if CALL_SUBJECT.match(sents[n])]
    if not access:
        notes.append(("ORDER", "no access phrase of §5.1 (§2.3)"))
    else:
        a = access[0]
        if not OPEN_ACCESS.match(sents[a]) and not any(codes(s) or REFUSAL_WITHOUT_ERROR.search(s)
                                                       for s in sents[a:a + 2]):
            notes.append(("ORDER", "the access phrase is not followed by the refusal code (§2.3). What does the "
                                   "action return to other callers? Take it from its code"))
    # "The call refuses a locked file." names a refusal but not the answer that signals it.
    for n, s in enumerate(sents, 1):
        if REFUSAL_BY_CALL.match(s) and not codes(s):
            notes.append(("ORDER", "sentence {0} names a refusal without its code (§2.3): {1}. Take the code from "
                                   "the code of the action and write `returns N`; a 500 is left out, then say the "
                                   "call does not work".format(n, s)))
    if not effect:
        notes.append(("ORDER", "no effect statement: read-only, changes data, cannot be undone, spends, queues (§2.4)"))
    if access and said_of_call and said_of_call[0] < access[0]:
        notes.append(("ORDER", "the effect comes before the access (§2 order)"))
    if item["verb"] in WRITE_VERBS and not REPEAT.search(text):
        notes.append(("ORDER", "nothing says what a second call does (§2.4). Take the answer from the code, for "
                               "every state the object can be in; `Do not repeat it.` is an answer too"))
    if source and re.search(r"\bwebhook", text, re.I):
        notes.append(("CONTENT", "mentions a webhook; the generator writes that sentence from [RaisesWebhook] (§3)"))

    for n, s in enumerate(sents, 1):
        bare = CODE_SPAN.sub("", s)
        if s.startswith("`"):
            notes.append(("STYLE", "sentence {0} starts with code or a path (§4)".format(n)))
        if re.search(r":\s", bare):
            notes.append(("STYLE", "sentence {0} has a colon; only before a list (§4): {1}".format(n, s)))
        for pattern, label in WORD_ORDER:
            if pattern.search(bare):
                notes.append(("STYLE", "sentence {0}: {1} (§4)".format(n, label)))
        hits = [w for w in words_to_replace if re.search(r"\b{0}\b".format(re.escape(w)), bare, re.I)]
        if hits:
            notes.append(("STYLE", "sentence {0}: {1} (§5.3)".format(n, ", ".join(hits))))

    if routes:
        for verb, path in OPERATION_REF.findall(text):
            if route_ref(verb, path) not in routes:
                notes.append(("REFS", "{0} {1} is in no document of the json folder; name the operation "
                                      "as the document publishes it (§4 Notation)".format(verb, path)))

    # Only the codes of this operation: a code the text gives to another operation is neither named nor lost.
    own_route = route_ref(item["verb"], item["path"])
    new, others = split_codes(text, own_route)
    params = codes(item["params"])
    old = split_codes(item["old"], own_route)[0] if item["old"] is not None else set()
    lost = sorted(c for c in old - new - params)
    unstated = sorted(c for c in item["declared"] if c not in new and c not in params and c not in PIPELINE_CODES)
    undeclared = sorted(c for c in new if c not in item["declared_all"])
    item["codes"] = dict(text=sorted(new), others=sorted(others), declared=item["declared"],
                         old=sorted(old) if item["old"] is not None else None)
    if lost:
        notes.append(("CODES", "in the old text, not in the new one or the parameters: {0}. Does the action still "
                               "return it? Keep it if so, give the reason if not (§1.5)"
                      .format(", ".join(c + (" (usually left out)" if c in PIPELINE_CODES else "") for c in lost))))
    if unstated:
        notes.append(("CODES", "declared, not in the text or the parameters: {0}. Can this operation return it? Find "
                               "the path in the code of the action, not in the [SwaggerResponse] text. Reachable: "
                               "name it in the text. Unreachable: remove the attribute (§1.5)".format(", ".join(unstated))))
    if undeclared:
        notes.append(("CODES", "named in the text, no response declares it: {0}. Can this operation return it? "
                               "Reachable: declare it. Unreachable: take it out of the text (§1.5)"
                      .format(", ".join(undeclared))))
    return notes


# --- tools -------------------------------------------------------------------------------------

TEXTSTAT_KEYS = ("flesch_kincaid_grade", "flesch_reading_ease", "gunning_fog", "difficult_words")


def textstat_metrics(texts):
    import textstat
    textstat.set_lang("en_US")
    out = {}
    for key, text in texts.items():
        t = re.sub(r"\s+", " ", CODE_SPAN.sub("Code", text)).strip()
        out[key] = dict(
            flesch_kincaid_grade=round(textstat.flesch_kincaid_grade(t), 1),
            flesch_reading_ease=round(textstat.flesch_reading_ease(t), 1),
            gunning_fog=round(textstat.gunning_fog(t), 1),
            difficult_words=sorted({w.lower() for w in textstat.difficult_words_list(t)}),
        )
    return out


def run_textstat(texts, python):
    try:
        import textstat  # noqa: F401
        return textstat_metrics(texts), sys.executable
    except ImportError:
        pass
    python = python or os.environ.get("TEXTSTAT_PYTHON")
    if not python or not os.path.isfile(python):
        return None, None
    proc = subprocess.run([python, "-I", os.path.abspath(__file__), "--textstat-worker"],
                          input=json.dumps(texts), capture_output=True, text=True, encoding="utf-8")
    if proc.returncode != 0:
        return None, None
    return json.loads(proc.stdout), python


# --- the installation check --------------------------------------------------------------------

# Two texts with a known verdict (references/tools-setup.md, step 4). The first is one sentence of 26
# words: it breaks the sentence length, the sentence count and the average of description-rules.md §4.
# The second keeps all of them.
SAMPLE_LONG = ("Returns the list of all the files and folders that the current user can open in the room "
               "with the given ID and the given filter.")
SAMPLE_SHORT = "Returns the files of a room. The room is set by its ID. A missing room returns 404."
VACUUM_RULES = {"desc-sentence-length", "desc-average-sentence", "desc-min-sentences", "desc-one-paragraph",
                "desc-grade"}
VACUUM_EXPECTED = {"desc-sentence-length", "desc-min-sentences"}
# The paragraph check: an XML comment wraps its lines, and a wrapped line that begins with a status code is
# no numbered list; a blank line followed by bullets is a list.
SAMPLE_WRAPPED = "Returns the files of a room. If the room does not exist, the call returns\n404. A guest gets 403."
SAMPLE_LIST = "Returns the files of a room. Each entry has two fields.\n\n- `id`, the file ID.\n- `title`, its name."
PARAGRAPH_SAMPLES = {"wrapped": (SAMPLE_WRAPPED, False), "list": (SAMPLE_LIST, True)}


def tool_version(binary, *variants):
    """The first non-empty output line of the first variant of the version flags that succeeds."""
    env = dict(os.environ, VACUUM_NO_UPDATE_CHECK="true")
    for flags in variants:
        try:
            proc = subprocess.run([binary] + list(flags), capture_output=True, text=True, encoding="utf-8",
                                  env=env, timeout=60)
        except (OSError, subprocess.SubprocessError):
            continue
        line = next((x.strip() for x in (proc.stdout or "").splitlines() if x.strip()), "")
        if proc.returncode == 0 and line:
            return line
    return "version unknown"


def check_tools(args):
    """Runs the tools that are found on the two samples and compares their verdicts with the known one.
    A tool that runs but misses the long sentence would close form findings it never saw."""
    work = args.work or tempfile.mkdtemp(prefix="desc-opt-tools-")
    os.makedirs(work, exist_ok=True)
    samples = {"long": SAMPLE_LONG, "short": SAMPLE_SHORT}
    failed, missing = [], []
    print("TOOL CHECK: a 26-word sentence has to be flagged, three short sentences have to pass")

    own = {k: max(readability.form_metrics(t)["lengths"]) for k, t in samples.items()}
    if own["long"] > readability.MAX_SENTENCE_WORDS >= own["short"]:
        print("  own checks  ok: longest sentence {0} and {1} words".format(own["long"], own["short"]))
    else:
        failed.append("own checks")
        print("  own checks  FAILED: longest sentence {0} and {1} words".format(own["long"], own["short"]))
    wrong = [k for k, (t, expected) in PARAGRAPH_SAMPLES.items() if readability.has_paragraphs(t) != expected]
    if wrong:
        failed.append("own checks")
        print("  own checks  FAILED: the paragraph check is wrong on the {0} sample".format(" and ".join(wrong)))
    else:
        print("  own checks  ok: a wrapped status code is one paragraph, bullets after a blank line are a list")

    stats, python = run_textstat(samples, args.textstat_python)
    if stats is None:
        missing.append("textstat")
        print("  textstat    not found")
    else:
        grades = [stats[k]["flesch_kincaid_grade"] for k in ("long", "short")]
        if grades[0] > grades[1]:
            print("  textstat    ok ({0}): grade {1} and {2}".format(python, *grades))
        else:
            failed.append("textstat")
            print("  textstat    FAILED ({0}): grade {1} and {2}, the long sample has to grade higher".format(
                python, *grades))

    found = readability.discover(args.vale, args.vacuum)
    if not found["vale"]:
        missing.append("Vale")
        print("  Vale        not found")
    else:
        where = "{0}, {1}".format(found["vale"], tool_version(found["vale"], ["--version"]))
        out, note = readability.run_vale(found["vale"], samples, work)
        if out is None:
            failed.append("Vale")
            print("  Vale        FAILED ({0}): {1}".format(where, note))
        else:
            hits = {k: sum(1 for a in out.get(k, []) if a[0] == "Docs.SentenceLength") for k in samples}
            if hits["long"] and not hits["short"]:
                print("  Vale        ok ({0}): {1}".format(where, note))
            else:
                failed.append("Vale")
                print("  Vale        FAILED ({0}): Docs.SentenceLength alerts: {1} on the long sample, {2} on the "
                      "short one; expected at least 1 and 0".format(where, hits["long"], hits["short"]))

    if not found["vacuum"]:
        missing.append("vacuum")
        print("  vacuum      not found")
    else:
        where = "{0}, {1}".format(found["vacuum"], tool_version(found["vacuum"], ["version"], ["--version"]))
        # The dot of `api/2.0` is part of the check: vacuum splits the reported path on it.
        entries = [(k, "/api/2.0/tool-check/" + k, "get", t) for k, t in samples.items()]
        entries += [(k, "/api/2.0/tool-check/" + k, "get", t) for k, (t, _) in PARAGRAPH_SAMPLES.items()]
        out, note = readability.run_vacuum(found["vacuum"], entries, work)
        if out is None:
            failed.append("vacuum")
            print("  vacuum      FAILED ({0}): {1}".format(where, note))
        else:
            codes = {k: {c for c, _ in out.get(k, []) if c in VACUUM_RULES} for k, _, _, _ in entries}
            wrong = [k for k, (_, expected) in PARAGRAPH_SAMPLES.items()
                     if ("desc-one-paragraph" in codes[k]) != expected]
            if wrong:
                failed.append("vacuum")
                print("  vacuum      FAILED ({0}): desc-one-paragraph is wrong on the {1} sample".format(
                    where, " and ".join(wrong)))
            elif VACUUM_EXPECTED <= codes["long"] and not codes["short"]:
                print("  vacuum      ok ({0}): {1}".format(where, ", ".join(sorted(codes["long"]))))
            else:
                failed.append("vacuum")
                print("  vacuum      FAILED ({0}): rules on the long sample: {1}; on the short one: {2}; expected "
                      "at least {3} on the long one and none on the short one".format(
                          where, ", ".join(sorted(codes["long"])) or "none",
                          ", ".join(sorted(codes["short"])) or "none", ", ".join(sorted(VACUUM_EXPECTED))))

    print()
    print(("TOOLS FAILED: " + ", ".join(failed)) if failed else "TOOLS OK")
    print("MISSING TOOLS: " + (", ".join(missing) if missing else "none"))
    print("tool inputs: " + work)
    return 1 if failed else 0


# --- report ------------------------------------------------------------------------------------

def main():
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8")
        except (AttributeError, ValueError):
            pass
    if "--textstat-worker" in sys.argv:
        json.dump(textstat_metrics(json.load(sys.stdin)), sys.stdout)
        return

    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("--root", default=".")
    parser.add_argument("--from", dest="source", choices=["source", "document"], default="source")
    parser.add_argument("--doc", help="document key with --op, as scan.py --diag prints it")
    parser.add_argument("--op", action="append", help='"PUT api/2.0/files/tags"; repeatable')
    parser.add_argument("--vale")
    parser.add_argument("--vacuum")
    parser.add_argument("--textstat-python")
    parser.add_argument("--work", help="folder for the tool inputs (default: a new temp folder)")
    parser.add_argument("--check-tools", action="store_true",
                        help="run the tools that are found on two samples with a known verdict and stop")
    args = parser.parse_args()
    if args.check_tools:
        sys.exit(check_tools(args))

    source = args.source == "source"
    doc_key, wanted = targets(args)
    items = collect(args.root, doc_key, wanted, source)
    work = args.work or tempfile.mkdtemp(prefix="desc-opt-check-")
    os.makedirs(work, exist_ok=True)

    ok = [i for i in items if "error" not in i]
    words_to_replace = plain_words()
    routes = published_routes(args.root)
    for n, item in enumerate(ok):
        item["key"] = n
        item["notes"] = own_checks(item, source, words_to_replace, routes)

    ran, missing = [], []
    texts = {str(i["key"]): i["text"] for i in ok}
    stats, python = run_textstat(texts, args.textstat_python) if ok else (None, None)
    if stats is None:
        missing.append("textstat")
    else:
        ran.append("textstat ({0})".format(python))
    found = readability.discover(args.vale, args.vacuum)
    vale, vale_note = (readability.run_vale(found["vale"], {i["key"]: i["text"] for i in ok}, work)
                       if found["vale"] and ok else (None, None))
    if not found["vale"]:
        missing.append("Vale")
    else:
        ran.append("Vale: " + (vale_note or "not run"))
    entries = [(i["key"], i["path"], i["verb"], readability.published(i["raw"]) if not source else i["text"])
               for i in ok]
    vac, vac_note = (readability.run_vacuum(found["vacuum"], entries, work)
                     if found["vacuum"] and ok else (None, None))
    if not found["vacuum"]:
        missing.append("vacuum")
    else:
        ran.append("vacuum: " + (vac_note or "not run"))

    print("CHECK: {0} · {1} {2} · text from {3}".format(
        doc_key, len(items), "operation" if len(items) == 1 else "operations",
        "<remarks> in the sources (old text: the document)" if source else "the published document"))
    for item in items:
        print()
        if "error" in item:
            print("{0} — {1}".format(item["name"], item["error"]))
            continue
        s = item["stats"]
        old_words = sum(words(x) for x in sentences(item["old"])) if item["old"] else None
        print("{0}  ({1}) — {2}".format(item["name"], item["operation_id"], item["where"]))
        print("  words {0}{1} · sentences {2} · average {3} · longest {4}".format(
            "{0} → ".format(old_words) if old_words is not None else "", s["words"],
            s["sentences"], s["average"], s["longest"]))
        c = item["codes"]
        print("  codes: text {0}{1} · declared by the action {2}{3}".format(
            " ".join(c["text"]) or "none",
            " · of other operations {0}".format(" ".join(c["others"])) if c["others"] else "",
            " ".join(c["declared"]) or "none",
            " · old text {0}".format(" ".join(c["old"]) or "none") if c["old"] is not None else ""))
        for kind, note in item["notes"]:
            print("  {0:7} {1}".format(kind, note))
        if not item["notes"]:
            print("  own     no findings: FORM, ORDER, STYLE, REFS and CODES are clean")
        if stats:
            m = stats[str(item["key"])]
            print("  textstat FKGL {0} · FRE {1} · fog {2} · difficult: {3}".format(
                m["flesch_kincaid_grade"], m["flesch_reading_ease"], m["gunning_fog"],
                ", ".join(m["difficult_words"]) or "-"))
        if vale is not None:
            alerts = vale.get(item["key"], [])
            if not alerts:
                print("  Vale    clean")
            for check, severity, match, message in alerts:
                print("  Vale    {0} ({1}) \"{2}\": {3}".format(check, severity, match, message))
        if vac is not None:
            vac_alerts = vac.get(item["key"], [])
            if not vac_alerts:
                print("  vacuum  clean")
            for code, message in vac_alerts:
                print("  vacuum  {0}: {1}".format(code, message))

    print()
    print("TOOLS: " + ("; ".join(ran) if ran else "none ran"))
    print("MISSING TOOLS: " + (", ".join(missing) if missing else "none"))
    print("tool inputs: " + work)


if __name__ == "__main__":
    main()
