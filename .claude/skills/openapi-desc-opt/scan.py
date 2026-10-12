"""Build the queue of controllers whose published endpoint texts are not optimized yet.

Usage (from the repository root):

    python .claude/skills/openapi-desc-opt/scan.py                    # write the list, print a summary
    python .claude/skills/openapi-desc-opt/scan.py --batch            # ... and commit to the next batch
    python .claude/skills/openapi-desc-opt/scan.py --verify-batch     # did the batch close what it declared?
    python .claude/skills/openapi-desc-opt/scan.py --diag             # counts only, nothing written
    python .claude/skills/openapi-desc-opt/scan.py --scope api,files  # narrow the run to some documents

The procedure this implements is `references/list-rules.md`; section numbers in the comments below
point at it. Two things it deliberately does NOT do: it never works from a pre-built surface
snapshot (a snapshot ages, the sources do not), and it never runs the schema-level checks, whose unit is a DTO and not a
controller (list rules §1).
"""

import argparse
import collections
import datetime
import difflib
import json
import os
import re
import sys

# --- what is in scope -------------------------------------------------------------------------
# `scope` is the default membership. `ai` and `apisystem` sit outside the public bundle (rule 5.5);
# they stay switchable with --scope so that taking them in costs a flag, not an edit.
PROJECTS = {
    "api":       dict(project="web/ASC.Web.Api",                 doc="api_2.0.json",          asm="ASC.Web.Api",     scope=True),
    "files":     dict(project="products/ASC.Files/Server",       doc="files_2.0.json",        asm="ASC.Files",       scope=True),
    "people":    dict(project="products/ASC.People/Server",      doc="people_2.0.json",       asm="ASC.People",      scope=True),
    "backup":    dict(project="common/services/ASC.Data.Backup", doc="backup_2.0.json",       asm="ASC.Data.Backup", scope=True),
    "ai":        dict(project="products/ASC.AI/Server",          doc="ai_2.0.json",           asm="ASC.AI",          scope=False),
    "apisystem": dict(project="common/services/ASC.ApiSystem",   doc="apisystem_common.json", asm="ASC.ApiSystem",   scope=False),
}

# The size of the published surface, per document: controllers, actions in the sources, operations in
# the document. These move only when somebody adds or removes an action, so a mismatch here is the
# index breaking, not the API changing — and a controller the index quietly lost takes its findings
# with it, which reads exactly like a pass that fixed them. That is why the numbers live here and are
# checked on every run instead of sitting in a document somebody has to remember to compare against.
# Update a row in the same commit that adds or removes the actions, and say so in the message.
EXPECTED_SURFACE = {
    "api":       dict(controllers=26, actions=254, operations=254),
    "files":     dict(controllers=29, actions=318, operations=215),
    "people":    dict(controllers=13, actions=91,  operations=82),
    "backup":    dict(controllers=1,  actions=14,  operations=14),
    "ai":        dict(controllers=3,  actions=14,  operations=14),
    "apisystem": dict(controllers=2,  actions=14,  operations=14),
}

JSON_DIR = "common/Tools/ASC.Api.Documentation/ASC.Api.Documentation/json"
LIST_PATH = ".claude/skills/openapi-desc-opt/unoptimized-controllers.md"

VERBS = ("get", "post", "put", "delete", "patch", "head", "options", "trace")

STOP_WORDS = set("""a an the of to for in on by with and or is are was were this that it its be as from at all
if when which what current specified given new used use uses set get gets returns return""".split())

# Response texts the generator prints for every operation. No [SwaggerResponse] on an action writes
# them, so no controller edit can improve them (rule 5.1). Matched on the normalised text, because
# the spelling drifts between generator versions ("Too many requests." vs "Too Many Requests.").
GENERATOR_RESPONSE_TEXTS = {
    "bad request", "unauthorized", "too many requests", "internal server error",
    "bad gateway", "service unavailable",
}

# Tier A is "there is no text", B is "there is text and it says nothing", C is "the text is there and
# says something, the form could be better". `tautological-param` is tier C (list-rules.md §4):
# standing on its own it means a parameter whose operation is already fully written, so it is an
# improvement to a finished text and must not outrank a controller whose descriptions are still
# empty. The same finding at a thin operation never
# reaches a tier at all — it is folded into the operation's own row (§4.2).
TIERS = {
    "no-description": "A", "no-summary": "A", "no-param-description": "A",
    "thin-description": "B", "empty-response-text": "B",
    "tautological-param": "C", "duplicate-summary": "C", "long-summary": "C",
}

# --- what a pass can hold ---------------------------------------------------------------------
# A row of the queue is a controller, because that is the unit of the surface report — but it is
# neither the unit of work nor the unit of verification, and the two numbers disagree in both
# directions. Over the four documents of the public bundle, 35 findings in 12 rows came to 17 places
# where something is actually typed. Fourteen `tautological-param` findings in `files` were TWO of
# them (the properties `fileId` and `folderId`), so four passes counted by rows would have edited one
# DTO four times; fourteen `long-summary` findings in the same document were twelve separate titles,
# where the pass really is per action. So the batch is budgeted in edit
# sites, and the weights below say what a site costs in attention rather than in build time — the
# limiting resource of this loop is careful prose, not MSBuild.
SITE_WEIGHTS = {
    # The whole description written from scratch: read the handler end to end, then 150-200 words.
    # Four points means two of them fill a pass and a third does not fit, which is the point.
    "no-description": 4, "thin-description": 4,
    # One DTO property. Cheap to type and expensive to get right, because the sentence has to be true
    # of every operation that binds it — hence the per-consumer surcharge in edit_sites().
    "no-param-description": 2, "tautological-param": 2,
    # One attribute or one title. The handler is read to confirm a fact, not to be retold.
    "no-summary": 1, "empty-response-text": 1, "long-summary": 1, "duplicate-summary": 1,
}
# The band a pass is budgeted in. The ceiling is what one pass may carry; the floor is what
# keeps a split from leaving a two-point remainder that wastes a whole pass on one title.
BUDGET_LOW, BUDGET_HIGH = 8, 10
DOC_ORDER = {key: i for i, key in enumerate(PROJECTS)}
MANIFEST_PATH = ".claude/skills/openapi-desc-opt/batch-manifest.json"


# --- text helpers -----------------------------------------------------------------------------

def norm(text):
    """Lower-case, every non-alphanumeric run collapsed to one space (rule 4)."""
    return re.sub(r"[^a-z0-9]+", " ", (text or "").lower()).strip()


def content_words(text):
    return {w for w in norm(text).split() if len(w) > 2 and w not in STOP_WORDS}


def split_camel(name):
    return re.sub(r"(?<!^)(?=[A-Z])", " ", name or "")


def sentences(text):
    """Split the way rule 4 does: code spans masked first, then split, then fragments dropped.

    Back-ticked code is masked before splitting because `GET api/2.0/files/fileops` carries dots that
    are not sentence ends, and a description naming three endpoints would otherwise look twice as
    thorough as it is.
    """
    if not text:
        return []
    masked = re.sub(r"`[^`]*`", "CODE", text).replace("\r", " ").replace("\n", " ")
    return [p for p in re.split(r"(?<=[.!?])\s+", masked) if len(p.split()) >= 2]


# --- C# source index --------------------------------------------------------------------------

CLASS_RE = re.compile(
    r"^[ \t]*(?P<mods>(?:public|internal|protected|private|abstract|sealed|static|partial|file|\s)*)"
    r"\bclass\s+(?P<name>\w+)\s*(?P<generic><[^<>{()]*>)?",
    re.M)
HTTP_ATTR_RE = re.compile(r"\[Http(?P<verb>Get|Post|Put|Delete|Patch|Head)(?:\(\s*\"(?P<route>[^\"]*)\"[^)]*\))?\]")
IGNORE_RE = re.compile(r"ApiExplorerSettings\s*\(\s*IgnoreApi\s*=\s*true", re.I)
METHOD_RE = re.compile(r"^\s*(?:public|protected|internal|private)\b[^;{=]*?\b(?P<name>\w+)\s*(?:<[^>()]*>)?\s*\(", re.M)


def read_text(path):
    with open(path, encoding="utf-8-sig", errors="replace") as f:
        return f.read()


def mask_comments(text):
    """Blank out ordinary comments and string literals, keeping `///` doc comments and all offsets.

    Commented-out actions are not rare in these controllers, and the parser cannot tell one from a
    live one: `BackupController` carries a disabled copy of `GetBackupProgress` right above the real
    one, and reading the dead attribute block first cost that operation its `<remarks>` and made a
    fully documented endpoint look like a document gone stale. Characters are replaced one for one so
    that every span and line number still points where it did.
    """
    out = list(text)
    i, n = 0, len(text)
    while i < n:
        ch = text[i]
        if text.startswith("///", i):
            while i < n and text[i] != "\n":
                i += 1
        elif text.startswith("//", i):
            # The two slashes stay: a `//` note is allowed to sit between the doc block and the
            # action (`HasTagLinks` has one), and the walk that collects that block reads a blank
            # line as the end of it. Blanking the whole line would cut the action off from its text.
            i += 2
            while i < n and text[i] != "\n":
                out[i] = " "
                i += 1
        elif text.startswith("/*", i):
            while i < n and not text.startswith("*/", i):
                if text[i] != "\n":
                    out[i] = " "
                i += 1
            for k in range(i, min(i + 2, n)):
                out[k] = " "
            i += 2
        elif ch == '"':
            verbatim = i > 0 and text[i - 1] == "@"
            i += 1
            while i < n:
                if text[i] == "\\" and not verbatim:
                    i += 2
                    continue
                if text[i] == '"':
                    if verbatim and i + 1 < n and text[i + 1] == '"':
                        i += 2
                        continue
                    break
                if text[i] == "\n" and not verbatim:
                    break            # an unbalanced quote must not swallow the rest of the file
                i += 1
            i += 1
        else:
            i += 1
    return "".join(out)


def compile_removes(project_dir):
    removed = set()
    for entry in os.listdir(project_dir):
        if entry.endswith(".csproj"):
            for m in re.finditer(r"<Compile\s+Remove=\"([^\"]+)\"", read_text(os.path.join(project_dir, entry))):
                removed.add(m.group(1).replace("\\", "/").lower())
    return removed


def cs_files(project_dir):
    removed = compile_removes(project_dir)
    for root, dirs, files in os.walk(project_dir):
        dirs[:] = [d for d in dirs if d not in ("obj", "bin", "node_modules")]
        for name in sorted(files):
            if not name.endswith(".cs"):
                continue
            path = os.path.join(root, name)
            rel = os.path.relpath(path, project_dir).replace("\\", "/").lower()
            if rel in removed:
                continue
            yield path


def block_above(text, position):
    """The attribute and XML-doc block sitting directly above `position`.

    Walked backwards line by line while the lines still look like documentation or attributes, which
    is what separates one action from the member above it: these files put a blank line between
    members and never inside the block.
    """
    kept = []
    head = text[:position].splitlines()
    if head and not head[-1].strip():
        # the indentation in front of `position` itself, not a blank separator line
        head.pop()
    for line in reversed(head):
        stripped = line.strip()
        if not stripped:
            break
        if stripped.startswith(("///", "[", "]", "//")) or (kept and not stripped.endswith(("{", "}", ";"))):
            kept.append(line)
            continue
        break
    return "\n".join(reversed(kept))


def xml_tag(block, tag):
    """One XML doc tag out of a `///` block, its inner text flattened to a single line."""
    body = "\n".join(re.sub(r"^\s*///\s?", "", line) for line in block.splitlines() if line.strip().startswith("///"))
    m = re.search(r"<{0}>(.*?)</{0}>".format(tag), body, re.S)
    if not m:
        return ""
    return re.sub(r"\s+", " ", re.sub(r"<[^>]+>", " ", m.group(1))).strip()


def type_key(reference):
    """`EditorController<int>` and `EditorController` are two different classes, so key on both.

    `EditorController.cs` declares an abstract `EditorController<T>` and a concrete
    `EditorController` side by side. Keyed by bare name, the concrete one disappears behind the
    generic one and takes three rows of the list with it, so the arity travels with the name.
    """
    reference = reference.strip()
    name = re.match(r"[\w.]+", reference)
    name = name.group(0).split(".")[-1] if name else reference
    arity, depth = 0, 0
    generic = reference[len(name):].lstrip()
    if generic.startswith("<"):
        arity = 1
        for ch in generic[1:]:
            if ch in "<([":
                depth += 1
            elif ch == ">" and depth == 0:
                break
            elif ch in ")]>":
                depth -= 1
            elif ch == "," and depth == 0:
                arity += 1
    return (name, arity)


def class_spans(text):
    """Every class declaration in a file, with the source span of its body."""
    out = []
    for m in CLASS_RE.finditer(text):
        tail_start = m.end()
        stop = len(text)
        for ch in "{;":
            k = text.find(ch, tail_start)
            if k != -1:
                stop = min(stop, k)
        bases, tail = [], text[tail_start:stop]
        if ":" in tail:
            depth, current = 0, ""
            for ch in tail.split(":", 1)[1]:
                if ch in "<([":
                    depth += 1
                elif ch in ">)]":
                    depth -= 1
                if ch == "," and depth == 0:
                    bases.append(current)
                    current = ""
                else:
                    current += ch
            bases.append(current)
        body = (0, 0)
        if stop < len(text) and text[stop] == "{":
            depth, j = 0, stop
            while j < len(text):
                if text[j] == "{":
                    depth += 1
                elif text[j] == "}":
                    depth -= 1
                    if depth == 0:
                        body = (stop + 1, j)
                        break
                j += 1
        out.append(dict(
            name=m.group("name"),
            key=type_key(m.group("name") + (m.group("generic") or "")),
            abstract="abstract" in (m.group("mods") or ""),
            generic=bool(m.group("generic")),
            # A base call spans lines here (`: FilesController<int>(\n  helper,\n  ...)`), so the
            # argument list has to be cut with DOTALL or the whole call ends up inside the name.
            bases=[type_key(b) for b in bases if b.strip()],
            attrs=block_above(text, m.start()),
            body=body,
        ))
    return out


def parse_actions(text, span, file_rel, class_name):
    """Every action declared inside one class body."""
    body = text[span[0]:span[1]]
    actions, seen = [], set()
    for m in HTTP_ATTR_RE.finditer(body):
        decl = METHOD_RE.search(body, m.end())
        if not decl:
            continue
        method = decl.group("name")
        if method in seen:
            continue
        seen.add(method)
        block = block_above(body, m.start())
        # Every route attribute of this method, not only the one that matched: two [Http*] attributes
        # on one action are one operation in the document but two rows in the surface report (rule 3).
        head = body[max(0, m.start() - 1500):decl.start()]
        verbs, routes = [], []
        for a in HTTP_ATTR_RE.finditer(head):
            verbs.append(a.group("verb").lower())
            routes.append((a.group("route") or "").strip("/"))
        actions.append(dict(
            cls=class_name,
            method=method,
            file=file_rel,
            line=text[:span[0] + m.start()].count("\n") + 1,
            verbs=verbs or [m.group("verb").lower()],
            routes=routes,
            summary=xml_tag(block, "summary"),
            remarks=xml_tag(block, "remarks"),
            path=xml_tag(block, "path"),
            ignored=bool(IGNORE_RE.search(block + text[span[0] + m.start():span[0] + decl.start()])),
        ))
    return actions


def index_sources(root, project_dir):
    """Concrete controller classes of one project, each with the actions it publishes.

    A class is a controller when it publishes actions, declared on itself or inherited from a base in
    the same project. That is the population the surface report walks (a `ControllerBase` descendant
    carrying route attributes), reached without compiling anything, and it keeps the unit of the list
    a class: `TagsControllerInternal` and `TagsControllerThirdparty` are two rows sharing one
    declaration site in `TagsController<T>` (rule 1).
    """
    declared = collections.defaultdict(list)
    classes = {}
    for path in cs_files(os.path.join(root, project_dir)):
        text = mask_comments(read_text(path))
        rel = os.path.relpath(path, root).replace("\\", "/")
        for cls in class_spans(text):
            classes.setdefault(cls["key"], dict(cls, file=rel))
            if cls["body"] != (0, 0):
                declared[cls["key"]].extend(parse_actions(text, cls["body"], rel, cls["name"]))

    def inherited(key, seen=None):
        seen = set() if seen is None else seen
        if key in seen or key not in classes:
            return []
        seen.add(key)
        out = list(declared.get(key, []))
        for base in classes[key]["bases"]:
            out.extend(inherited(base, seen))
        return out

    controllers = {}
    for key, cls in classes.items():
        name = cls["name"]
        if cls["abstract"] or cls["generic"]:
            continue
        if IGNORE_RE.search(cls["attrs"]):                     # class-level IgnoreApi (rule 5.4)
            continue
        actions = [a for a in inherited(key) if not a["ignored"]]
        if not actions:
            continue
        controllers[name] = dict(name=name, file=cls["file"], actions=actions)
    return controllers


# --- OpenAPI document -------------------------------------------------------------------------

def load_operations(doc_path):
    with open(doc_path, encoding="utf-8-sig") as f:
        doc = json.load(f)
    operations = []
    for path, item in (doc.get("paths") or {}).items():
        for verb, op in item.items():
            if verb.lower() not in VERBS:
                continue
            operations.append(dict(
                path=path, verb=verb.lower(),
                summary=op.get("summary") or "",
                description=op.get("description") or "",
                operation_id=op.get("operationId") or "",
                parameters=[p for p in (op.get("parameters") or []) if isinstance(p, dict)],
                responses={code: (r or {}).get("description", "") for code, r in (op.get("responses") or {}).items()},
            ))
    return operations


def route_key(verb, path):
    """`verb path`, route constraints dropped and case folded (rule 3).

    The document publishes `/api/2.0/files/file/{fileId}`, never `{fileId:int}`, and it does not keep
    the casing of the attribute, so both have to go before the two sides can be compared.
    """
    path = re.sub(r"\{(\w+)\s*:[^}]*\}", r"{\1}", path or "")
    return "{0} {1}".format(verb.lower(), path.strip("/").lower())


def match(operations, controllers):
    """Operation to declared action, by normalised summary, falling back to the route.

    The summary is the working key because the rule requires titles to be unique inside a document;
    `operationId` is not, since it is the C# method name and `*Internal` and `*Thirdparty` share it.
    """
    by_summary = collections.defaultdict(list)
    by_route = collections.defaultdict(list)
    for ctrl in controllers.values():
        for action in ctrl["actions"]:
            entry = ((action["cls"], action["method"]), action, ctrl)
            by_summary[norm(action["summary"])].append(entry)
            keys = set()
            for verb in action["verbs"]:
                if action["path"]:
                    keys.add(route_key(verb, action["path"]))
                for route in action["routes"]:
                    if route:
                        keys.add(route_key(verb, route))
            for key in keys:
                by_route[key].append(entry)

    matched, unmatched = {}, []
    for op in operations:
        candidates = by_summary.get(norm(op["summary"]), []) if op["summary"].strip() else []
        if not candidates or len({c[0] for c in candidates}) > 1:
            key = route_key(op["verb"], op["path"])
            by_path = by_route.get(key, [])
            if not by_path:                    # attribute routes are relative; match on the suffix
                verb, path = key.split(" ", 1)
                by_path = [c for k, group in by_route.items() if k.startswith(verb + " ")
                           and path.endswith(k.split(" ", 1)[1]) for c in group]
            candidates = by_path or candidates
        if not candidates:
            unmatched.append(op)
            continue
        matched[(op["verb"], op["path"])] = candidates
    return matched, unmatched


# --- checks -----------------------------------------------------------------------------------

def default_response_texts(operations):
    """Texts the generator prints everywhere, found twice over (rule 5.1).

    The fixed list catches the ones whose wording is known; the frequency guard catches the rest,
    including the ones a filter in this repository adds, without having to enumerate them.
    """
    counts = collections.Counter()
    for op in operations:
        for code, text in op["responses"].items():
            counts[(code, norm(text))] += 1
    limit = max(5, len(operations) // 5)
    return {key for key, n in counts.items() if n > limit}


def check_operation(op, defaults, summary_counts):
    """Every finding of one operation, before the merges of rules 4.1 and 4.2."""
    found = []
    description, summary = op["description"], op["summary"]

    if not description.strip():
        found.append(dict(check="no-description"))
    if not summary.strip():
        found.append(dict(check="no-summary"))

    novel = len(content_words(description)
                - content_words(summary)
                - content_words(split_camel(op["operation_id"]))
                - content_words(op["path"].replace("/", " ")))
    count = len(sentences(description))
    if description.strip() and count < 3:
        # Rule 4.1: a tautological description is a field of this row, never a row of its own.
        found.append(dict(check="thin-description", detail="sentences {0}, novel words {1}".format(count, novel)))

    thin = any(f["check"] in ("no-description", "thin-description") for f in found)
    for param in op["parameters"]:
        name, text = param.get("name", ""), param.get("description") or ""
        if not text.strip():
            found.append(dict(check="no-param-description", detail=name, param=name))
        elif len(content_words(text) - content_words(split_camel(name))) <= 1:
            # Rule 4.2: at a thin operation this is part of rewriting the operation, not its own row.
            found.append(dict(check="tautological-param", detail=name, param=name, rolled_up=thin))

    for code, text in op["responses"].items():
        if (code, norm(text)) in defaults or norm(text) in GENERATOR_RESPONSE_TEXTS:
            continue
        if len(norm(text).split()) <= 3:
            found.append(dict(check="empty-response-text", detail='{0} "{1}"'.format(code, text)))

    if summary.strip():
        if summary_counts[norm(summary)] > 1:
            found.append(dict(check="duplicate-summary", detail='"{0}"'.format(summary)))
        if len(summary.split()) > 6:
            found.append(dict(check="long-summary", detail='{0} words: "{1}"'.format(len(summary.split()), summary)))

    for f in found:
        f["tier"] = TIERS[f["check"]]
        f["op"] = "{0} {1}".format(op["verb"].upper(), op["path"].strip("/"))
    return found


def staleness(op, action):
    """How far the published description has drifted from the `<remarks>` it came from (rule 5.3).

    A document older than its source makes findings that were already fixed look open, which is the
    one failure mode that wastes a whole pass, so anything below the threshold is reported apart from
    the queue instead of inside it.
    """
    published, source = norm(op["description"]), norm(action["remarks"])
    if not published and not source:
        return 1.0
    return difflib.SequenceMatcher(None, published, source).ratio()


# --- assembly ---------------------------------------------------------------------------------

def git(root, *args):
    import subprocess
    try:
        out = subprocess.run(("git",) + args, cwd=root, capture_output=True, text=True, encoding="utf-8", errors="replace")
    except OSError:
        return ""
    return (out.stdout or "").strip()


def freshness(root, scope):
    """Is each document younger than the sources it was generated from? (SKILL.md step 1.)

    A finding taken off a document older than its controller is a finding about a text that no longer
    exists, and the pass that closes it edits code that is already right. Two signals, because either
    alone lies: the last commit that touched the project against the last commit that touched the
    document, and any uncommitted change in the project — which no commit date can see.
    """
    verdicts = []
    for key in scope:
        meta = PROJECTS[key]
        doc_rel = "{0}/{1}".format(JSON_DIR, meta["doc"])
        doc_commit = git(root, "log", "-1", "--format=%ct", "--", doc_rel)
        src_commit = git(root, "log", "-1", "--format=%ct", "--", meta["project"])
        dirty = [line for line in git(root, "status", "--porcelain", "--", meta["project"]).splitlines()
                 if line.strip().endswith(".cs")]
        doc_dirty = bool(git(root, "status", "--porcelain", "--", doc_rel))
        reasons = []
        if dirty:
            reasons.append("uncommitted edits in {0} {1} of the project"
                           .format(len(dirty), plural(len(dirty), "file", "files")))
        if doc_commit and src_commit and int(src_commit) > int(doc_commit):
            reasons.append("the sources were committed later than the document")
        if not doc_commit:
            reasons.append("the document is not in git")
        verdicts.append(dict(key=key, doc=meta["doc"], stale=bool(reasons), doc_dirty=doc_dirty,
                             reasons=reasons or ["the document is younger than the sources"]))
    return verdicts


def run(root, scope, threshold):
    result = dict(sections=[], diag=collections.OrderedDict(), stale=[], unmatched={})
    for key in scope:
        meta = PROJECTS[key]
        doc_path = os.path.join(root, JSON_DIR, meta["doc"])
        if not os.path.exists(doc_path):
            result["diag"][key] = dict(error="no document " + meta["doc"])
            continue
        controllers = index_sources(root, meta["project"])
        operations = load_operations(doc_path)
        matched, unmatched = match(operations, controllers)
        defaults = default_response_texts(operations)
        summary_counts = collections.Counter(norm(op["summary"]) for op in operations if op["summary"].strip())

        per_controller = collections.defaultdict(list)
        stale_ops = []
        for op in operations:
            candidates = matched.get((op["verb"], op["path"]))
            if not candidates:
                continue
            action = candidates[0][1]
            if staleness(op, action) < 0.9:
                stale_ops.append("{0} {1} ({2}.{3})".format(op["verb"].upper(), op["path"], action["cls"], action["method"]))
                continue
            findings = check_operation(op, defaults, summary_counts)
            if not findings:
                continue
            for ctrl in {c[2]["name"]: c[2] for c in candidates}.values():
                per_controller[ctrl["name"]].append(dict(controller=ctrl, action=action, findings=findings))

        # The split is between findings, not between controllers: the same controller belongs in
        # "Must fix" for its tier A and B findings and in "Recommended" for its tier C ones, and
        # it appears in both with only the findings that section is about. Splitting by controller
        # instead would hide the cosmetics of a controller that also has real gaps, and hide the real
        # gaps of a controller read from the advisory section — the two rows cross-reference each
        # other's counts so that neither is mistaken for the whole story.
        mandatory_tiers = {"A": {"A"}, "AB": {"A", "B"}, "ABC": {"A", "B", "C"}}[threshold]
        must, better = [], []
        for name, entries in per_controller.items():
            counted = [f for e in entries for f in e["findings"] if not f.get("rolled_up")]
            # A tautological parameter at a thin operation has no tier of its own (rule 4.2): it is
            # part of rewriting that operation, so it travels with the operation's own row.
            rolled = [f for e in entries for f in e["findings"] if f.get("rolled_up")]
            groups = {
                True: [f for f in counted if f["tier"] in mandatory_tiers],
                False: [f for f in counted if f["tier"] not in mandatory_tiers],
            }
            for is_mandatory, findings in groups.items():
                if not findings:
                    continue
                tiers = collections.Counter(f["tier"] for f in findings)
                other = collections.Counter(f["tier"] for f in groups[not is_mandatory])
                row = dict(
                    name=name,
                    file=entries[0]["controller"]["file"],
                    operations=len({f["op"] for f in findings}),
                    tiers=tiers,
                    other=other,
                    weight=tiers["A"] * 10000 + tiers["B"] * 100 + tiers["C"],
                    findings=findings,
                    rolled=rolled if is_mandatory or not groups[True] else [],
                )
                (must if is_mandatory else better).append(row)
        for rows in (must, better):
            rows.sort(key=lambda r: (-r["weight"], -r["operations"], r["name"]))
        result["sections"].append(dict(key=key, asm=meta["asm"], rows=must, advisory=better,
                                       controllers=len(controllers), operations=len(operations)))
        result["diag"][key] = collections.OrderedDict(
            controllers=len(controllers), operations=len(operations),
            actions=sum(len(c["actions"]) for c in controllers.values()),
            unmatched=len(unmatched), stale=len(stale_ops),
            must_fix=len(must), advisory=len(better))
        result["stale"].extend(stale_ops)
        if unmatched:
            result["unmatched"][key] = ['{0} {1} — "{2}"'.format(o["verb"].upper(), o["path"], o["summary"]) for o in unmatched]
    result["batches"] = plan_batches(result)
    result["cross_doc_params"] = cross_document_params(result)
    return result


def surface_drift(result):
    """Where the indexed surface disagrees with EXPECTED_SURFACE, one line per document.

    `unmatched` is checked here too: it is not a size but it has the same job — an operation nobody
    could tie to an action carries findings nobody can attribute, so a non-zero value invalidates the
    run just as a lost controller does.
    """
    out = []
    for key, diag in result["diag"].items():
        if diag.get("error"):
            continue
        expected = EXPECTED_SURFACE.get(key)
        if expected:
            for field, want in expected.items():
                got = diag.get(field)
                if got != want:
                    out.append("{0}.{1}: {2}, expected {3}".format(key, field, got, want))
        if diag.get("unmatched"):
            out.append("{0}.unmatched: {1}, expected 0".format(key, diag["unmatched"]))
    return out


def advisory_summary(result):
    """What is left in the advisory section, said in plain words.

    This text is what a person reads when the mandatory section empties, so it is a sentence and not a
    table of rule codes: `tautological-param — 20 findings` tells the reader nothing they can act on,
    while "twenty parameters whose description all but repeats the name, most of them fileId" tells
    them what the remaining work actually is. The examples are grouped the way the fix is grouped —
    parameters by the parameter, because one DTO property closes every operation that binds it
    (rule 4.2), titles by controller, because there the edit is per action.
    """
    findings = [(f, row["name"]) for s in result["sections"] for row in s["advisory"] for f in row["findings"]]
    by_check = collections.defaultdict(list)
    for f, controller in findings:
        by_check[f["check"]].append((f, controller))

    # How each check reads as a phrase: a template, the noun it counts, and what the examples are
    # keyed by. Written out rather than assembled from the check name so the sentence stays a
    # sentence — the reader of this line is deciding whether to spend an evening on it, not looking
    # up a rule code. Every template is a noun phrase, so it reads the same for one finding and for
    # twenty and the sentence never has to agree with a verb.
    WORDING = {
        "tautological-param":   ("{n} {u} whose description all but repeats the name", ("parameter", "parameters"), "param"),
        "long-summary":         ("{n} {u} longer than six words", ("title", "titles"), "controller"),
        "duplicate-summary":    ("{n} {u} that repeat one another", ("title", "titles"), "controller"),
        "empty-response-text":  ("{n} {u} labelled with a word or two", ("response code", "response codes"), "controller"),
        "thin-description":     ("{n} {u} shorter than three sentences", ("description", "descriptions"), "controller"),
        "no-description":       ("{n} {u} with no description at all", ("operation", "operations"), "controller"),
        "no-summary":           ("{n} {u} with no title at all", ("operation", "operations"), "controller"),
        "no-param-description": ("{n} {u} with no description at all", ("parameter", "parameters"), "param"),
    }

    parts = []
    for check in sorted(by_check, key=lambda c: -len(by_check[c])):
        items = by_check[check]
        template, unit, key = WORDING.get(check, ("{n} {u} " + check, ("finding", "findings"), "controller"))
        if key == "param":
            spread = collections.Counter(f.get("param", "?") for f, _ in items)
        else:
            spread = collections.Counter(name for _, name in items)
        examples = ", ".join("{0} — {1}".format(name, n) for name, n in spread.most_common(2))
        parts.append(template.format(n=len(items), u=plural(len(items), *unit)) + " ({0})".format(examples))

    if not parts:
        return ""
    body = ", ".join(parts[:-1]) + " and " + parts[-1] if len(parts) > 1 else parts[0]
    return ("The mandatory findings are closed. What is left are recommendations: {0}. "
            "None of this stops anyone from using the API — I will take it on only if you say so."
            .format(body))


def plural(n, one, many):
    return one if n == 1 else many


# --- batches ----------------------------------------------------------------------------------

def edit_sites(result):
    """The places a pass actually types, which is not the same list as the rows of the queue.

    Two collapses happen here and they are the reason the row count overstates the work. A parameter
    finding names a DTO property, and one edit to that property closes every finding that names it,
    across controllers and across operations (rule 4.2) — so parameters are keyed by their name
    within the document, not by the row they were reported under. An action declared once in a
    generic base is reported twice, under `*Internal` and under `*Thirdparty`; keying the rest by
    file and operation collapses that pair back into the single edit it is.

    Only the mandatory section feeds this. The milestone rule of the skill is unchanged: when
    "Must fix" empties there is no batch, and a tier C row becomes work only when the user says so
    (at which point --threshold ABC makes it mandatory and it arrives here by the same route).
    """
    sites = collections.OrderedDict()
    for section in result["sections"]:
        for row in section["rows"]:
            for f in row["findings"]:
                if f.get("param"):
                    key = (section["key"], f["check"], "param:" + f["param"])
                else:
                    key = (section["key"], f["check"], row["file"] + "|" + f["op"])
                site = sites.setdefault(key, dict(
                    doc=section["key"], asm=section["asm"], check=f["check"], tier=f["tier"],
                    target=f.get("param") or f["op"], param=bool(f.get("param")),
                    file=row["file"], ops=set(), rows=set(), ids=set()))
                site["ops"].add(f["op"])
                site["rows"].add(row["name"])
                site["ids"].add(finding_id(section["key"], row["name"], f))
    for site in sites.values():
        base = SITE_WEIGHTS.get(site["check"], 2)
        # A property's sentence has to hold for every operation that binds it, so the cost sits in the
        # checking rather than in the typing: one point per consuming operation beyond the first.
        site["weight"] = base + (len(site["ops"]) - 1 if site["param"] else 0)
    return sorted(sites.values(),
                  key=lambda s: (s["tier"], DOC_ORDER.get(s["doc"], 99), s["check"], s["file"], s["target"]))


def finding_id(doc, controller, finding):
    """The identity a batch declares and a verification looks for. Stable across runs by construction:
    every part of it is printed in the list, so a mismatch can be read rather than debugged."""
    return "{0}|{1}|{2}|{3}".format(doc, controller, finding["op"], finding["check"])


def finding_ids(result):
    """Every open finding, mandatory and advisory alike.

    Advisory rows are counted deliberately: a tier B finding whose fix only downgraded it to tier C
    has not been closed, and a set built from the mandatory section alone would report it as closed.
    """
    out = set()
    for section in result["sections"]:
        for row in section["rows"] + section["advisory"]:
            for f in row["findings"]:
                out.add(finding_id(section["key"], row["name"], f))
    return out


def weight_of(sites):
    return sum(s["weight"] for s in sites)


def pack(file_groups):
    """Split one (document × check) group into passes of 8-10 points.

    Sites of the same file travel together: a file opened by two passes is one diff reviewed twice,
    and the second pass cannot say which of the two edits closed what. When the group does not fit in
    one pass it is spread over the fewest passes that do, balanced rather than filled to the brim —
    filling greedily leaves a two-point remainder, and a whole pass spent on one title is exactly the
    waste this batching exists to remove.
    """
    total = sum(weight_of(g) for g in file_groups)
    if total <= BUDGET_HIGH:
        return [[s for g in file_groups for s in g]]
    count = max(2, -(-total // BUDGET_HIGH))
    bins = [[] for _ in range(count)]
    for group in sorted(file_groups, key=lambda g: -weight_of(g)):
        bins.sort(key=weight_of)
        bins[0].extend(group)
    return [b for b in sorted(bins, key=lambda b: -weight_of(b)) if b]


def plan_batches(result):
    """Group the edit sites into the passes they should be done in.

    The axis is (document × check). The document, because the build and the regeneration are per
    project and because step 7's guard — controllers, operations and actions unchanged — only reads
    cleanly inside one document. The check, because one check is one shape of edit, one section of
    the rule and one thing for a reviewer to hold: a diff that mixes rewritten titles with rewritten
    DTO properties is the same problem the skill already forbids when it keeps tier C out of a tier B
    pass.
    """
    sites = edit_sites(result)
    groups = collections.OrderedDict()
    for site in sites:
        groups.setdefault((site["tier"], site["doc"], site["check"]), []).append(site)

    batches = []
    for (tier, doc, check), members in groups.items():
        total = weight_of(members)
        by_file = collections.OrderedDict()
        for site in members:
            by_file.setdefault(site["file"], []).append(site)
        for chunk in pack(list(by_file.values())):
            batches.append(make_batch(doc, check, chunk, total, None))
    # Full loads first. A group that holds less than a pass — one stray title in a whole document —
    # is real work and keeps its own pass, but taking it first would spend a pass on one point while
    # loaded batches wait. Inside that, the deterministic order the rest of the script uses.
    batches.sort(key=lambda b: (b["tier"], b["undersized"], DOC_ORDER.get(b["doc"], 99), b["check"],
                                -b["weight"]))
    return batches


def make_batch(doc, check, sites, group_total, note):
    weight = weight_of(sites)
    warnings = []
    if weight > BUDGET_HIGH:
        warnings.append("heavier than the ceiling of {0} points: a file is not split between passes"
                        .format(BUDGET_HIGH))
    elif weight < BUDGET_LOW and group_total > BUDGET_HIGH:
        # Not every split can put both halves above the floor — twelve points in two passes cannot.
        # Said rather than balanced away, so the reader knows the pass is light by arithmetic and not
        # because something was dropped.
        warnings.append("underloaded: the remainder of the group, {0} {1}, does not divide evenly "
                        "into passes of the {2}-{3} band"
                        .format(group_total, plural(group_total, "point", "points"),
                                BUDGET_LOW, BUDGET_HIGH))
    return dict(doc=doc, asm=sites[0]["asm"], check=check, tier=sites[0]["tier"], sites=sites,
                weight=weight, note=note, warnings=warnings,
                undersized=group_total < BUDGET_LOW,
                ids=sorted({i for s in sites for i in s["ids"]}),
                rows=sorted({r for s in sites for r in s["rows"]}))


def cross_document_params(result):
    """Parameter names that carry findings in more than one document.

    Not proof that one property feeds them — two documents may simply have a `userId` each — but the
    one case where an edit's blast radius leaves the document whose counts step 7 checks, so it is
    named and checked rather than assumed either way (rule 3.5 of the docs rule).
    """
    where = collections.defaultdict(set)
    for section in result["sections"]:
        for row in section["rows"]:
            for f in row["findings"]:
                if f.get("param"):
                    where[f["param"]].add(section["key"])
    return {name: sorted(docs) for name, docs in where.items() if len(docs) > 1}


def write_manifest(root, result, batch, threshold, scope):
    """Declare, before the first edit, exactly which findings this pass intends to close.

    This is what makes a batch verifiable at all. Step 7 used to read "the diff touches only the
    expected rows" and check it by reasoning — workable while a pass was one row, hopeless once it is
    six places. With the declared set on disk the check becomes set equality: what closed and was
    declared, what was declared and survived, and what closed without being declared. The last of the
    three is the one a batch would otherwise hide, and it is usually good news — a shared DTO property
    reaching further than expected — which still has to be stated rather than discovered later.
    """
    manifest = dict(
        created=datetime.datetime.now().isoformat(timespec="seconds"),
        threshold=threshold, scope=scope,
        doc=batch["doc"], asm=batch["asm"], check=batch["check"], tier=batch["tier"],
        weight=batch["weight"], budget=[BUDGET_LOW, BUDGET_HIGH],
        note=batch["note"], warnings=batch["warnings"],
        rows=batch["rows"],
        sites=[dict(target=s["target"], param=s["param"], file=s["file"], check=s["check"],
                    weight=s["weight"], ops=sorted(s["ops"]), rows=sorted(s["rows"]))
               for s in batch["sites"]],
        declared=batch["ids"],
        # Everything open at declaration time, so that a finding which closed without being asked for
        # can be told from one that was never open.
        baseline=sorted(finding_ids(result)),
        diag={key: dict(diag) for key, diag in result["diag"].items()},
    )
    path = os.path.join(root, MANIFEST_PATH)
    os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(manifest, f, ensure_ascii=False, indent=2)
    return manifest


def verify_batch(root, result):
    """Compare what is open now against what the manifest declared.

    Prints a verdict rather than a diff, because the verdict is what the pass reports: the counts are
    part of it, since a row that vanished because the parser lost the controller reads exactly like a
    row that was fixed.
    """
    path = os.path.join(root, MANIFEST_PATH)
    if not os.path.exists(path):
        print("no manifest: {0} — no batch was committed, there is nothing to verify".format(MANIFEST_PATH))
        return 1
    with open(path, encoding="utf-8") as f:
        manifest = json.load(f)

    current = finding_ids(result)
    declared = set(manifest["declared"])
    baseline = set(manifest["baseline"])
    closed = sorted(declared - current)
    left = sorted(declared & current)
    unexpected = sorted((baseline - current) - declared)
    appeared = sorted(current - baseline)

    print("BATCH: {0} · {1} · {2} {3}, {4} {5} (declared {6})".format(
        manifest["asm"], manifest["check"], len(manifest["sites"]),
        plural(len(manifest["sites"]), "site", "sites"),
        manifest["weight"], plural(manifest["weight"], "point", "points"),
        manifest["created"]))
    print("closed {0} of {1} declared findings".format(len(closed), len(declared)))

    moved = []
    for key, diag in manifest["diag"].items():
        now = result["diag"].get(key)
        if not now:
            continue
        for field in ("controllers", "operations", "actions", "unmatched"):
            if now.get(field) != diag.get(field):
                moved.append("{0}.{1}: {2} → {3}".format(key, field, diag.get(field), now.get(field)))
        if now.get("stale", 0) > diag.get("stale", 0):
            moved.append("{0}.stale: {1} → {2}".format(key, diag.get("stale"), now.get("stale")))

    for item in left:
        print("  STILL OPEN: {0}".format(item))
    for item in unexpected:
        print("  CLOSED BEYOND THE DECLARATION: {0}".format(item))
    for item in appeared:
        print("  APPEARED: {0}".format(item))
    for item in moved:
        print("  COUNTER MOVED: {0}".format(item))

    if moved:
        print("VERDICT: the counts of the document moved — a row may have gone because of parsing "
              "rather than because of an edit; work it out before reporting")
        return 2
    if left:
        print("VERDICT: the batch closed partly — {0} of {1}; the remainder is the next batch"
              .format(len(closed), len(declared)))
        return 1
    if unexpected or appeared:
        print("VERDICT: what was declared is closed, but the diff is wider than the declaration — name that in the report")
        return 0
    print("VERDICT: the batch closed in full, the diff matched the declaration")
    return 0


def render_site(site, lines):
    if site["param"]:
        # The property itself is in a DTO the document does not name, so what is printed is what the
        # reader needs to find it: the parameter, and every operation whose text it feeds.
        head = "property `{0}` — {1} {2}".format(
            site["target"], len(site["ops"]),
            plural(len(site["ops"]), "operation", "operations"))
    else:
        head = "`{0}`".format(site["target"])
    lines.append("- {0} — {1} {2} ({3})".format(
        head, site["weight"], plural(site["weight"], "point", "points"), site["check"]))
    lines.append("  - file: `{0}`".format(site["file"]))
    if site["param"]:
        for op in sorted(site["ops"]):
            lines.append("    - `{0}`".format(op))
    if len(site["rows"]) > 1:
        lines.append("  - queue rows it closes: {0}".format(", ".join(sorted(site["rows"]))))


def render_batch(batch, lines):
    lines.append("## Batch of the next pass — {0} · {1} · {2} {3}, {4} of {5} points"
                 .format(batch["asm"], batch["check"], len(batch["sites"]),
                         plural(len(batch["sites"]), "site", "sites"),
                         batch["weight"], BUDGET_HIGH))
    lines.append("")
    lines.append("The unit of work here is an edit site, not a row of the queue: one DTO property "
                 "closes every operation that binds it, and an action declared in a shared base "
                 "closes both rows of an `*Internal`/`*Thirdparty` pair. This batch closes **{0}** "
                 "{1} in {2} {3} of the queue."
                 .format(len(batch["ids"]), plural(len(batch["ids"]), "finding", "findings"),
                         len(batch["rows"]), plural(len(batch["rows"]), "row", "rows")))
    lines.append("")
    if batch["note"]:
        lines.append("- **{0}**".format(batch["note"]))
    for warning in batch["warnings"]:
        lines.append("- **warning**: {0}".format(warning))
    if batch["note"] or batch["warnings"]:
        lines.append("")
    for site in batch["sites"]:
        render_site(site, lines)
    lines.append("")
    lines.append("Commit to the batch and write the manifest: "
                 "`python .claude/skills/openapi-desc-opt/scan.py --batch`. "
                 "After the edit and the regeneration of the document — "
                 "`python .claude/skills/openapi-desc-opt/scan.py --verify-batch`.")
    lines.append("")


def render_row(row, lines, mandatory):
    tiers = row["tiers"]
    lines.append("#### {0}".format(row["name"]))
    lines.append("")
    lines.append("- file: `{0}`".format(row["file"]))
    lines.append("- operations with findings: {0}".format(row["operations"]))
    lines.append("- findings here: {0} (A {1} · B {2} · C {3})".format(
        sum(tiers.values()), tiers["A"], tiers["B"], tiers["C"]))
    if row["other"]:
        spread = " · ".join("{0} {1}".format(t, n) for t, n in sorted(row["other"].items()))
        lines.append('- the same controller under "{0}": {1} {2} ({3})'.format(
            "Recommended" if mandatory else "Must fix",
            sum(row["other"].values()),
            plural(sum(row["other"].values()), "finding", "findings"),
            spread))
    by_check = collections.defaultdict(list)
    for f in row["findings"]:
        by_check[f["check"]].append(f)
    for check in sorted(by_check, key=lambda c: (TIERS[c], c)):
        items = by_check[check]
        lines.append("- **[{0}] {1}** ({2}):".format(TIERS[check], check, len(items)))
        for f in items[:12]:
            detail = " — {0}".format(f["detail"]) if f.get("detail") else ""
            lines.append("  - `{0}`{1}".format(f["op"], detail))
        if len(items) > 12:
            lines.append("  - … {0} more".format(len(items) - 12))
    if row["rolled"]:
        names = sorted({f.get("param", "") for f in row["rolled"]})
        lines.append("- folded into the descriptions of the operations (rule 4.2): tautological parameters {0}"
                     .format(", ".join(names)))
    lines.append("")


def render(result, threshold):
    must = sum(len(s["rows"]) for s in result["sections"])
    advisory = sum(len(s["advisory"]) for s in result["sections"])
    lines = ["# Controllers whose descriptions are not optimized yet", ""]
    lines.append("Collected {0} by `.claude/skills/openapi-desc-opt/scan.py` following "
                 "`.claude/skills/openapi-desc-opt/references/list-rules.md`. Mandatory-fix "
                 "threshold: {1}."
                 .format(datetime.date.today().isoformat(), "A + B" if threshold == "AB" else threshold))
    lines.append("")
    must_findings = sum(len(r["findings"]) for s in result["sections"] for r in s["rows"])
    advisory_findings = sum(len(r["findings"]) for s in result["sections"] for r in s["advisory"])
    lines.append("To fix: **{0}** {1} in {2} {3}. To improve: **{4}** {5} in {6} {7}. "
                 "A controller may stand in both sections — each section shows only its own findings."
                 .format(must_findings, plural(must_findings, "finding", "findings"),
                         must, plural(must, "controller", "controllers"),
                         advisory_findings, plural(advisory_findings, "finding", "findings"),
                         advisory, plural(advisory, "controller", "controllers")))
    lines.append("")
    # The one line a reader of this file acts on. When the mandatory section is empty the campaign has
    # reached its milestone, and saying so here keeps the next pass from quietly starting on a tier-C
    # row just because it is the first heading in the file.
    batches = result.get("batches") or []
    if must:
        lines.append("**The target of the next run** is the batch below: {0} {1} worth {2} {3}, "
                     "{4} {5} planned in total."
                     .format(len(batches[0]["sites"]),
                             plural(len(batches[0]["sites"]), "site", "sites"),
                             batches[0]["weight"],
                             plural(batches[0]["weight"], "point", "points"),
                             len(batches), plural(len(batches), "pass", "passes")))
    else:
        lines.append(advisory_summary(result))
    lines.append("")
    if batches:
        render_batch(batches[0], lines)
        if len(batches) > 1:
            lines.append("### Further along the queue of passes")
            lines.append("")
            for i, batch in enumerate(batches[1:], start=2):
                lines.append("{0}. {1} · {2} — {3} {4}, {5} {6}{7}"
                             .format(i, batch["asm"], batch["check"], len(batch["sites"]),
                                     plural(len(batch["sites"]), "site", "sites"),
                                     batch["weight"],
                                     plural(batch["weight"], "point", "points"),
                                     " — " + batch["note"] if batch["note"] else ""))
            lines.append("")
    if result.get("cross_doc_params"):
        lines.append("### Parameters that appear in more than one document")
        lines.append("")
        lines.append("One name in two documents is not yet one property, but it is exactly the case "
                     "where the blast radius of an edit leaves the document whose counts step 7 "
                     "checks. Check it before the edit, do not batch it blind.")
        lines.append("")
        for name, docs in sorted(result["cross_doc_params"].items()):
            lines.append("- `{0}` — {1}".format(name, ", ".join(docs)))
        lines.append("")

    # The headings name the tiers the run actually treats as mandatory: with --threshold ABC the
    # recommendations become the queue, and a heading that still said "A and B" would describe the
    # default rather than this file.
    mandatory_label = {"A": "tier A", "AB": "tiers A and B", "ABC": "tiers A, B and C"}[threshold]
    advisory_label = {"A": " — tiers B and C", "AB": " — tier C", "ABC": ""}[threshold]
    lines.append("## Must fix — {0}".format(mandatory_label))
    lines.append("")
    lines.append("Tier A — there is no text at all; tier B — there is text and it says nothing. "
                 "Either one leaves an agent without a tool definition, so this is work, not a wish.")
    lines.append("")
    if not must:
        lines.append("Empty: the mandatory findings in scope are closed.")
        lines.append("")
    for section in result["sections"]:
        if not section["rows"]:
            continue
        lines.append("### {0} — {1} of {2} controllers, {3} {4} in the document"
                     .format(section["asm"], len(section["rows"]), section["controllers"], section["operations"],
                             plural(section["operations"], "operation", "operations")))
        lines.append("")
        for row in section["rows"]:
            render_row(row, lines, True)

    lines.append("## Recommended{0}".format(advisory_label))
    lines.append("")
    lines.append("Findings of the form: a tautological parameter at an operation that is already "
                 "fully written, a title longer than six words, a repeated title. The text is there "
                 "and says something; an edit makes it better but unblocks nothing. These are taken "
                 "once the \"Must fix\" section is empty, or on a separate request. A controller that "
                 "also has mandatory findings stands here as well — with its tier C findings and a "
                 "pointer to the first section.")
    lines.append("")
    if not advisory:
        lines.append("Empty.")
        lines.append("")
    for section in result["sections"]:
        if not section["advisory"]:
            continue
        lines.append("### {0} — {1} {2}".format(
            section["asm"], len(section["advisory"]),
            plural(len(section["advisory"]), "controller", "controllers")))
        lines.append("")
        for row in section["advisory"]:
            render_row(row, lines, False)
    if result["stale"]:
        lines.append("## Document older than its source — not findings, a reason to regenerate")
        lines.append("")
        lines.extend("- " + item for item in result["stale"])
        lines.append("")
    if result["unmatched"]:
        lines.append("## Operations with no source — the match did not come out, check by hand")
        lines.append("")
        for key, items in result["unmatched"].items():
            lines.extend("- [{0}] {1}".format(key, item) for item in items)
        lines.append("")
    return "\n".join(lines).rstrip() + "\n"


def main():
    # The console code page on this machine is not UTF-8, and this report prints em dashes, middots
    # and quotation marks: without this the summary reaches the terminal as mojibake and nobody can
    # read the verdict.
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8")
        except (AttributeError, ValueError):
            pass

    parser = argparse.ArgumentParser()
    parser.add_argument("--root", default=".")
    parser.add_argument("--scope", default=",".join(k for k, v in PROJECTS.items() if v["scope"]))
    parser.add_argument("--threshold", default="AB", choices=["A", "AB", "ABC"])
    parser.add_argument("--out", default=LIST_PATH)
    parser.add_argument("--diag", action="store_true", help="counts only; write nothing")
    parser.add_argument("--freshness", action="store_true", help="are the documents younger than their sources?")
    parser.add_argument("--batch", action="store_true",
                        help="commit to the next batch: write the list and the manifest step 7 verifies against")
    parser.add_argument("--verify-batch", dest="verify", action="store_true",
                        help="compare what is open now against the manifest the batch declared")
    args = parser.parse_args()

    scope = [k.strip() for k in args.scope.split(",") if k.strip()]
    unknown = [k for k in scope if k not in PROJECTS]
    if unknown:
        sys.exit("unknown documents in --scope: " + ", ".join(unknown))

    if args.freshness:
        stale = False
        for verdict in freshness(args.root, scope):
            stale = stale or verdict["stale"]
            print("{0:10} {1:22} {2} — {3}".format(
                verdict["key"], verdict["doc"], "STALE" if verdict["stale"] else "fresh",
                "; ".join(verdict["reasons"])))
        print("VERDICT: " + ("regenerate the documents before the analysis" if stale
                             else "safe to analyse as it stands"))
        return

    result = run(args.root, scope, args.threshold)

    if args.verify:
        sys.exit(verify_batch(args.root, result))

    for key, diag in result["diag"].items():
        print("{0:10} ".format(key) + "  ".join("{0}={1}".format(k, v) for k, v in diag.items()))
    drifted = surface_drift(result)
    for line in drifted:
        print("SURFACE: " + line)
    if drifted:
        print("SURFACE: the index disagrees with EXPECTED_SURFACE in scan.py. Find the commit that "
              "moved it before trusting any finding below — a controller the index lost takes its "
              "findings with it and looks like a closed batch.")
    must = sum(len(s["rows"]) for s in result["sections"])
    advisory = sum(len(s["advisory"]) for s in result["sections"])
    print("TOTAL: mandatory {0}, recommended {1}".format(must, advisory))
    if result["stale"]:
        print("WARNING: the document is older than the source for {0} operations — regenerate the documents"
              .format(len(result["stale"])))

    if args.diag:
        return
    out_path = os.path.join(args.root, args.out)
    if must + advisory == 0:
        if os.path.exists(out_path):
            os.remove(out_path)
            print("no findings — the list {0} was deleted".format(args.out))
        else:
            print("no findings — the list was never created")
        return
    os.makedirs(os.path.dirname(out_path) or ".", exist_ok=True)
    with open(out_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(render(result, args.threshold))
    print("list written: {0}".format(args.out))

    batches = result["batches"]
    if not batches:
        # The milestone: no mandatory findings, so no batch. Said in the same words as before, because
        # this line is the one a person acts on.
        print(advisory_summary(result))
        manifest_path = os.path.join(args.root, MANIFEST_PATH)
        if args.batch and os.path.exists(manifest_path):
            os.remove(manifest_path)
            print("manifest deleted: there is nothing to batch")
        return

    batch = batches[0]
    print("batch of the next pass: {0} · {1} — {2} {3}, {4} {5}, closes {6} {7} in {8} {9} of the queue"
          .format(batch["asm"], batch["check"], len(batch["sites"]),
                  plural(len(batch["sites"]), "site", "sites"),
                  batch["weight"], plural(batch["weight"], "point", "points"),
                  len(batch["ids"]), plural(len(batch["ids"]), "finding", "findings"),
                  len(batch["rows"]), plural(len(batch["rows"]), "row", "rows")))
    if batch["note"]:
        print("  {0}".format(batch["note"]))
    for warning in batch["warnings"]:
        print("  warning: {0}".format(warning))
    print("passes planned in total: {0}".format(len(batches)))

    if args.batch:
        write_manifest(args.root, result, batch, args.threshold, scope)
        print("manifest written: {0} — {1} {2} declared"
              .format(MANIFEST_PATH, len(batch["ids"]),
                      plural(len(batch["ids"]), "finding", "findings")))
    else:
        print("commit to the batch: python .claude/skills/openapi-desc-opt/scan.py --batch")


if __name__ == "__main__":
    main()
