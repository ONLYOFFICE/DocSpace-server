#!/usr/bin/env python3
"""Queue of API actions whose code, or a service it calls, produces a status code that no [SwaggerResponse] declares.

The queue keeps no state: it is the set of actions whose evidence codes minus declared codes is not empty,
computed from the sources on every run, so an action added or changed since the last run is in it automatically.
A 500 (an exception no case of the host's handler maps) is never part of the gap: it is printed apart, marked `*`.
The only file it writes is rejected.json, next to this script: "this finding was analysed and is not a response".
Each rejection is keyed on a fingerprint of the code it was decided on and expires by itself when that code changes.

Run from anywhere inside the repository:
  scan.py                       pipeline of every host, queue summary, the next batch (5 actions; --batch N)
  scan.py --all                 the whole queue, one line per action
  scan.py --endpoint ID|FILE    full detail for one action or for every action of a file, hints included
  scan.py --reject ID CODE --reason TEXT       record a gap code that is not a real response
  scan.py --unreject ID [CODE]                 drop a rejection
  scan.py --rejected                           every rejection with its state (valid / expired / orphaned)
  scan.py --prune-orphaned                     drop the rejections whose action or gap code no longer exists
  scan.py --snapshot FILE...    keep a pre-edit copy of each file (in the git directory) for --diff-check
  scan.py --diff-check FILE...  only [SwaggerResponse] lines changed since the snapshot (HEAD if there is none);
                                BOM and line endings intact
  scan.py --verify-doc ID|FILE...              declared codes and texts are in the regenerated document
  scan.py --audit-catches [ID|FILE...]         declared codes every found producer of which is caught on the way,
                                               services followed (all actions when none is named)
  --no-codegraph                without the hints from a CodeGraph index (.codegraph/codegraph.db), when there is one

The exception -> status table and the global MVC filters are read, per host project, from the startup class chain
that registers them (AddExceptionHandler<T>, Filters.Add), so a service with its own handler gets its own table.

Services are followed SERVICE_HOPS calls deep by the declared type of the field or parameter called (every
implementation of an interface). A finding in a service counts like one in the action unless the rules of
Analyzer.service_note (a null guard, the fallback arm of a switch, a framework exception two or more services deep, a
sign-in 401) or the agreement rule (a call that resolves to several bodies) make it a hint. A CodeGraph index, when the
repository has one, adds the calls the text cannot resolve, as hints only: the scan never depends on it.
"""
import argparse
import difflib
import hashlib
import http
import json
import os
import re
import subprocess
import sys
from datetime import date
from pathlib import Path

SKILL_DIR = Path(__file__).resolve().parent
ROOT = SKILL_DIR.parents[2]
REJECTED = SKILL_DIR / "rejected.json"
SOURCE_DIRS = ("common", "products", "web")
SKIP_PARTS = {"bin", "obj", "node_modules", "sdk", "migrations"}
DOCS_DIR = ROOT / "common/Tools/ASC.Api.Documentation/ASC.Api.Documentation/json"
DOCS_SETTINGS = DOCS_DIR.parent / "appsettings.json"

# .NET base classes of the BCL exceptions that DocSpace code throws (framework facts, not codebase data).
BCL_BASE = {
    "ArgumentNullException": "ArgumentException", "ArgumentOutOfRangeException": "ArgumentException",
    "ArgumentException": "SystemException", "InvalidOperationException": "SystemException",
    "ObjectDisposedException": "InvalidOperationException", "WebException": "InvalidOperationException",
    "NotSupportedException": "SystemException", "PlatformNotSupportedException": "NotSupportedException",
    "NotImplementedException": "SystemException", "UnauthorizedAccessException": "SystemException",
    "SecurityException": "SystemException", "AuthenticationException": "SystemException",
    "InvalidCredentialException": "AuthenticationException", "IOException": "SystemException",
    "FileNotFoundException": "IOException", "DirectoryNotFoundException": "IOException",
    "PathTooLongException": "IOException", "EndOfStreamException": "IOException",
    "KeyNotFoundException": "SystemException", "FormatException": "SystemException",
    "UriFormatException": "FormatException", "NullReferenceException": "SystemException",
    "IndexOutOfRangeException": "SystemException", "InvalidCastException": "SystemException",
    "ArithmeticException": "SystemException", "OverflowException": "ArithmeticException",
    "DivideByZeroException": "ArithmeticException", "TimeoutException": "SystemException",
    "OperationCanceledException": "SystemException", "TaskCanceledException": "OperationCanceledException",
    "InvalidDataException": "SystemException", "MemberAccessException": "SystemException",
    "MethodAccessException": "MemberAccessException", "DataException": "SystemException",
    "HttpRequestException": "Exception", "JsonException": "Exception", "AggregateException": "Exception",
    "ApplicationException": "Exception", "SystemException": "Exception", "XmlException": "SystemException",
}
BCL_VALIDATION = {"Required", "Range", "StringLength", "MinLength", "MaxLength", "Length", "EmailAddress", "Url",
                  "Phone", "RegularExpression", "Compare", "CreditCard", "AllowedValues", "DeniedValues",
                  "Base64String", "FileExtensions"}
RESULT_HELPERS = {"NotFound": 404, "BadRequest": 400, "Unauthorized": 401, "Forbid": 403, "Conflict": 409,
                  "UnprocessableEntity": 422, "NoContent": 204}
PRIMITIVES = {"int", "long", "short", "byte", "uint", "ulong", "bool", "string", "Guid", "DateTime", "decimal",
              "double", "float", "object", "T", "IFormFile", "IFormCollection", "CancellationToken",
              "DateTimeOffset", "TimeSpan", "char", "dynamic", "JsonElement", "Stream"}
MODIFIERS = r"(?:(?:public|private|protected|internal|static|async|override|virtual|abstract|sealed|new|extern|unsafe|partial|readonly)\s+)+"


# ---------------------------------------------------------------- lexing

def blank(src, strings=True):
    """Same-length copy of a C# source with comments and string/char literal contents replaced by spaces.
    With strings=False only the comments go: what the compiler sees, attribute texts included."""
    out = list(src)
    i, n = 0, len(src)

    def wipe(a, b, literal=True):
        if literal and not strings:
            return
        for k in range(a, b):
            if out[k] not in "\r\n":
                out[k] = " "

    while i < n:
        c = src[i]
        if c == "/" and src.startswith("//", i) or c == "#" and not src[src.rfind("\n", 0, i) + 1:i].strip():
            # a comment, or a preprocessor line (#region, #endregion, #if ...): between an action's attributes
            # and the previous declaration, a #region line would hide the action from the method parser
            j = src.find("\n", i)
            j = n if j < 0 else j
            wipe(i, j, literal=False)
            i = j
        elif c == "/" and src.startswith("/*", i):
            j = src.find("*/", i + 2)
            j = n if j < 0 else j + 2
            wipe(i, j, literal=False)
            i = j
        elif c == "'":
            j = i + 1
            while j < n and src[j] != "'" and src[j] != "\n":
                j += 2 if src[j] == "\\" else 1
            wipe(i + 1, min(j, n))
            i = j + 1
        elif c == '"' or (c in "@$" and i + 1 < n and src[i + 1] in '"@$'):
            j = i
            verbatim = False
            while j < n and src[j] in "@$":
                verbatim |= src[j] == "@"
                j += 1
            if j >= n or src[j] != '"':
                i += 1
                continue
            q = j
            while q < n and src[q] == '"':
                q += 1
            quotes = q - j
            if quotes >= 3:                      # raw string literal
                end = src.find('"' * quotes, q)
                end = n if end < 0 else end
                wipe(q, end)
                i = end + quotes
                continue
            k = j + 1
            while k < n:
                ch = src[k]
                if verbatim and ch == '"' and k + 1 < n and src[k + 1] == '"':
                    k += 2
                elif not verbatim and ch == "\\":
                    k += 2
                elif ch == '"' or (not verbatim and ch == "\n"):
                    break
                else:
                    k += 1
            wipe(j + 1, min(k, n))
            i = k + 1
        else:
            i += 1
    return "".join(out)


def match_close(text, start, open_ch, close_ch):
    """Index of the bracket closing the one at `start`."""
    depth = 0
    for k in range(start, len(text)):
        ch = text[k]
        if ch == open_ch:
            depth += 1
        elif ch == close_ch:
            depth -= 1
            if depth == 0:
                return k
    return len(text) - 1


def split_top(text, sep=","):
    parts, depth, cur = [], 0, []
    for ch in text:
        if ch in "<([{":
            depth += 1
        elif ch in ">)]}":
            depth -= 1
        if ch == sep and depth == 0:
            parts.append("".join(cur))
            cur = []
        else:
            cur.append(ch)
    if "".join(cur).strip():
        parts.append("".join(cur))
    return parts


def simple_name(type_text):
    t = re.sub(r"<.*", "", type_text.strip()).rstrip("?[] ")
    return t.split(".")[-1]


def generic_args(type_text):
    m = re.search(r"<(.*)>", type_text)
    return [a.strip() for a in split_top(m.group(1))] if m else []


def arity(method):
    """(required, maximum) number of arguments a method accepts; `params` makes the maximum unbounded."""
    if method._arity is None:
        params = [p for p in split_top(method.params) if p.strip()]
        if any(re.match(r"\s*(?:\[[^\]]*\]\s*)*params\b", p) for p in params):
            method._arity = (len(params) - 1, 10 ** 6)
        else:
            required = sum(1 for p in params if "=" not in p and not re.match(r"\s*this\b", p))
            method._arity = (required, len(params))
    return method._arity


def norm(text):
    return re.sub(r"\s+", " ", text).strip()


# ---------------------------------------------------------------- source model

class SourceFile:
    def __init__(self, path):
        self.path = path
        self.rel = path.relative_to(ROOT).as_posix()
        self.src = path.read_text(encoding="utf-8-sig", errors="replace")
        self.bl = blank(self.src)
        self.nc = blank(self.src, strings=False)      # comments gone, string literals kept
        self._lines = None

    def line(self, pos):
        if self._lines is None:
            self._lines = [m.start() for m in re.finditer("\n", self.src)]
        lo, hi = 0, len(self._lines)
        while lo < hi:
            mid = (lo + hi) // 2
            if self._lines[mid] < pos:
                lo = mid + 1
            else:
                hi = mid
        return lo + 1

    def line_start(self, line):
        """Position where 1-based `line` starts."""
        self.line(0)
        return 0 if line <= 1 else self._lines[min(line, len(self._lines) + 1) - 2] + 1

    def line_text(self, pos):
        a = self.src.rfind("\n", 0, pos) + 1
        b = self.src.find("\n", pos)
        return self.src[a:b if b >= 0 else len(self.src)].strip()


def attr_block_start(bl, pos):
    """Start of the attribute block preceding a declaration at `pos` (after the previous ; { or })."""
    return max(bl.rfind(";", 0, pos), bl.rfind("{", 0, pos), bl.rfind("}", 0, pos)) + 1


class TypeDecl:
    def __init__(self, f, kind, name, start, attrs, bases, body):
        self.f, self.kind, self.name, self.start = f, kind, name, start
        self.attrs, self.bases, self.body = attrs, bases, body   # body = (open, close) in f.bl
        self.methods = []
        self.props = None


class Method:
    def __init__(self, f, cls, name, decl_start, sig_pos, params, body, attrs, modifiers=""):
        self.f, self.cls, self.name = f, cls, name
        self.modifiers = modifiers      # the declaration between its attributes and its name
        self.decl_start, self.sig_pos, self.params, self.body, self.attrs = decl_start, sig_pos, params, body, attrs
        self._arity = None

    @property
    def key(self):
        return f"{self.f.rel}::{self.cls.name}.{self.name}@{self.f.line(self.sig_pos)}"

    def body_text(self):
        return self.f.bl[self.body[0]:self.body[1] + 1] if self.body else ""


TYPE_RE = re.compile(r"\b(class|record|struct|interface|enum)\s+(?:class\s+|struct\s+)?([A-Za-z_]\w*)")
# Type arguments of a generic method or call: whitespace only after a comma. With any whitespace allowed,
# `Task<File<T>> GetFileAsync<T>(` read as a method named `Task`: the group swallowed the real name.
GENERIC = r"<[^(){};=\s]*(?:,\s*[^(){};=\s]*)*>"
METHOD_RE = re.compile(r"\b([A-Za-z_]\w*)\s*(" + GENERIC + r")?\s*\(")


def parse_types(f):
    types = []
    bl = f.bl
    for m in TYPE_RE.finditer(bl):
        if bl[max(0, m.start() - 1)] in ".":
            continue
        kind, name = m.group(1), m.group(2)
        k = m.end()
        if k < len(bl) and bl[k] == "<":
            k = match_close(bl, k, "<", ">") + 1
        while k < len(bl) and bl[k].isspace():
            k += 1
        if k < len(bl) and bl[k] == "(":                       # primary constructor
            k = match_close(bl, k, "(", ")") + 1
        brace = bl.find("{", k)
        semi = bl.find(";", k)
        if brace < 0 or (0 <= semi < brace and kind != "enum"):
            body = None
            head_end = semi if semi >= 0 else k
        else:
            body = (brace, match_close(bl, brace, "{", "}"))
            head_end = brace
        head = bl[k:head_end]
        bases = []
        hm = re.match(r"\s*:\s*(.*?)(?:\bwhere\b|$)", head, re.S)
        if hm:
            bases = [simple_name(b) for b in split_top(re.sub(r"\(.*?\)", "", hm.group(1), flags=re.S))]
            bases = [b for b in bases if b]
        attrs = f.nc[attr_block_start(bl, m.start()):m.start()]
        types.append(TypeDecl(f, kind, name, m.start(), attrs, bases, body))
    return types


def parse_methods(f, types):
    """Method declarations with bodies, attached to the innermost enclosing type."""
    bl = f.bl
    for m in METHOD_RE.finditer(bl):
        name = m.group(1)
        if name in {"if", "for", "foreach", "while", "switch", "catch", "using", "lock", "return", "nameof",
                    "typeof", "sizeof", "default", "when", "new", "base", "this", "await", "throw"}:
            continue
        stmt = attr_block_start(bl, m.start())
        prefix = bl[stmt:m.start()]
        prefix_noattr = re.sub(r"^\s*(?:\[[^\[\]]*(?:\[[^\[\]]*\][^\[\]]*)*\]\s*)*", "", prefix)
        # parentheses are allowed when balanced: a tuple return type, Task<(int Id, string Name)>, has them
        if not re.match(MODIFIERS + r"[^=;{}]*$", prefix_noattr, re.S) or \
                prefix_noattr.count("(") != prefix_noattr.count(")"):
            continue
        if re.search(r"\b(class|record|struct|interface|enum|delegate|event|operator)\b", prefix_noattr):
            continue
        if prefix_noattr.split() == ["new"]:      # `new Foo(...)` as an expression statement, not a declaration
            continue
        close = match_close(bl, m.end() - 1, "(", ")")
        k = close + 1
        rest = bl[k:k + 400]
        wm = re.match(r"\s*(?:where\b[^{=;]*)?", rest)
        k += wm.end()
        if bl.startswith("{", k):
            body = (k, match_close(bl, k, "{", "}"))
        elif bl.startswith("=>", k):
            j, depth = k, 0
            while j < len(bl):
                ch = bl[j]
                if ch in "([{":
                    depth += 1
                elif ch in ")]}":
                    depth -= 1
                elif ch == ";" and depth == 0:
                    break
                j += 1
            body = (k, j)
        else:
            body = None
        owner = None
        for t in types:
            if t.body and t.body[0] < m.start() < t.body[1] and (owner is None or t.body[0] > owner.body[0]):
                owner = t
        if owner is None or owner.kind == "enum":
            continue
        params = f.nc[m.end():close]
        attrs = f.nc[stmt:m.start()]
        owner.methods.append(Method(f, owner, name, stmt, m.start(), params, body, attrs, prefix_noattr))


USE_CODEGRAPH = True     # --no-codegraph turns the supplement off
CODEGRAPH_DB = ROOT / ".codegraph" / "codegraph.db"


class CodeGraph:
    """The call edges of a CodeGraph index (`.codegraph/codegraph.db`), when the repository has one: an optional
    supplement for calls the text cannot resolve. The scan never depends on it."""

    def __init__(self, ix, nodes, edges):
        self.ix = ix
        self.nodes, self.edges = nodes, edges     # id -> (name, file, start, end); source id -> [(target id, line)]
        self.by_file = {}
        for nid, (name, fp, sl, el) in nodes.items():
            self.by_file.setdefault((fp, name), []).append((sl, el, nid))
        self._method = {}

    @classmethod
    def load(cls, ix):
        if not CODEGRAPH_DB.exists():
            return None
        try:
            import sqlite3
            db = sqlite3.connect(f"file:{CODEGRAPH_DB.as_posix()}?mode=ro", uri=True)
            nodes = {nid: (name, fp, sl, el) for nid, name, fp, sl, el in db.execute(
                "select id, name, file_path, start_line, end_line from nodes "
                "where language = 'csharp' and kind in ('method', 'function')")}
            edges = {}
            for src, tgt, line in db.execute("select source, target, line from edges where kind = 'calls'"):
                if src in nodes and tgt in nodes:
                    edges.setdefault(src, []).append((tgt, line))
            db.close()
        except Exception as e:      # a locked, old or foreign database: the scan goes on without it
            print(f"CodeGraph index not read ({type(e).__name__}: {e}); going on without it", file=sys.stderr)
            return None
        return cls(ix, nodes, edges)

    def node_of(self, method):
        line = method.f.line(method.sig_pos)
        best = None
        for sl, el, nid in self.by_file.get((method.f.rel, method.name), ()):
            if sl <= line <= el and (best is None or sl > best[0]):
                best = (sl, nid)
        return best[1] if best else None

    def method_of(self, nid):
        if nid not in self._method:
            name, fp, sl, el = self.nodes[nid]
            cands = [m for m in self.ix.methods.get(name, []) if m.f.rel == fp and sl <= m.f.line(m.sig_pos) <= el]
            m = max(cands, key=lambda x: x.sig_pos) if cands else None
            if m is not None and (m.cls.kind == "interface" or not m.body):
                # an interface or abstract member: only when exactly one body implements it
                impls = [m2 for c in self.ix.implementations(m.cls.name) for c2 in self.ix.class_chain(c)
                         for m2 in c2.methods if m2.name == name and m2.body]
                m = impls[0] if len(impls) == 1 else None
            self._method[nid] = m
        return self._method[nid]

    def callees(self, method):
        """[(line in method's file, target Method)] the index records for `method`."""
        nid = self.node_of(method)
        out = []
        for tgt, line in self.edges.get(nid, ()) if nid else ():
            t = self.method_of(tgt)
            if t is not None and t.body:
                out.append((line, t))
        return out


class Index:
    def __init__(self):
        self.files = {}
        self.types = {}      # name -> [TypeDecl]
        self.methods = {}    # name -> [Method]
        for d in SOURCE_DIRS:
            for dirpath, dirnames, filenames in os.walk(ROOT / d):
                dirnames[:] = [x for x in dirnames if x not in SKIP_PARTS and not x.endswith("Tests")
                               and x != "Tests"]
                for fn in filenames:
                    if fn.endswith(".cs"):
                        p = Path(dirpath) / fn
                        f = SourceFile(p)
                        self.files[f.rel] = f
                        f.types = parse_types(f)
                        parse_methods(f, f.types)
                        for t in f.types:
                            self.types.setdefault(t.name, []).append(t)
                            for mt in t.methods:
                                self.methods.setdefault(mt.name, []).append(mt)
        self._proj, self._hosts = {}, {}
        self.graph = CodeGraph.load(self) if USE_CODEGRAPH else None

    def project(self, f):
        d = f.path.parent
        if d not in self._proj:
            self._proj[d] = nearest_csproj(f.path)
        return self._proj[d]

    def resolve_type(self, name, near):
        """The class `name` as seen from file `near`: the one in the same project, else the only one."""
        decls = [d for d in self.types.get(name, []) if d.kind in ("class", "record")]
        own = [d for d in decls if self.project(d.f) == self.project(near)]
        return (own or decls)[0] if len(own or decls) == 1 else None

    def hosted_in(self, proj):
        """Other host projects that load `proj`'s controllers with AddApplicationPart(typeof(X).Assembly), X being a
        type of `proj` (the monolith loads ApiSystem's this way). There the actions run under that host's
        exception handler and global filters, not under their own project's."""
        if not hasattr(self, "_parts"):
            self._parts = {}
            for f in self.files.values():
                for m in re.finditer(r"\bAddApplicationPart\s*\(\s*typeof\s*\(\s*([\w.]+)\s*\)\s*\.\s*Assembly", f.bl):
                    qualified = m.group(1)
                    name, _, ns = qualified.rpartition(".")[::-1]
                    decls = [d for d in self.types.get(name, []) if d.kind in ("class", "record")]
                    if ns:
                        decls = [d for d in decls if re.search(rf"\bnamespace\s+{re.escape(ns)}\s*[;{{]", d.f.nc)]
                    for d in decls:
                        part, host = self.project(d.f), self.project(f)
                        if part and host and part != host:
                            self._parts.setdefault(part, set()).add(host)
        return sorted(self._parts.get(proj, ()))

    def host(self, proj):
        """(exception handler TypeDecl or None, [global filter names]) that the startup class chain of a host
        project registers; read from AddExceptionHandler<T> and Filters.Add(...) in that chain."""
        if proj in self._hosts:
            return self._hosts[proj]
        handlers, filters, seen = {}, [], set()
        for f in self.files.values():
            if self.project(f) != proj:
                continue
            for t in f.types:
                if t.kind != "class":
                    continue
                for c in self.class_chain(t):
                    if id(c) in seen or not c.body:
                        continue
                    seen.add(id(c))
                    body = c.f.bl[c.body[0]:c.body[1]]
                    for m in re.finditer(r"\bAddExceptionHandler\s*<\s*([\w.]+)\s*>", body):
                        name = m.group(1).split(".")[-1]
                        d = self.resolve_type(name, c.f)
                        if d is None:
                            sys.exit(f"{c.f.rel} registers exception handler {name}, which resolves to "
                                     f"{len(self.types.get(name, []))} classes; read the registration by hand")
                        handlers[d.f.rel] = d
                    for m in re.finditer(r"\bFilters\s*\.\s*Add\w*\s*\(\s*(?:new\s+TypeFilterAttribute\s*\(\s*)?"
                                         r"(?:typeof\s*\(\s*([\w.]+)|new\s+([\w.]+))", body):
                        name = (m.group(1) or m.group(2)).split(".")[-1]
                        if name not in filters:
                            filters.append(name)
        if len(handlers) > 1:
            sys.exit(f"{proj.relative_to(ROOT).as_posix()} registers several exception handlers "
                     f"({', '.join(handlers)}); the first that handles an exception wins — read them by hand")
        self._hosts[proj] = (next(iter(handlers.values()), None), filters)
        return self._hosts[proj]

    def type_chain(self, name, seen=None):
        """Names of the type and all its bases, repository types first, then BCL."""
        seen = seen if seen is not None else []
        if name in seen:
            return seen
        seen.append(name)
        decls = self.types.get(name)
        if decls:
            for b in decls[0].bases:
                self.type_chain(b, seen)
        elif name in BCL_BASE:
            self.type_chain(BCL_BASE[name], seen)
        return seen

    def class_chain(self, t):
        out, todo = [], [t]
        while todo:
            c = todo.pop(0)
            if c in out:
                continue
            out.append(c)
            for b in c.bases:
                for d in self.types.get(b, []):
                    if d.kind in ("class", "record"):
                        todo.append(d)
        return out

    # ---- services a method calls (--audit-catches)
    def members(self, t):
        """{name: declared type} of a class: its primary-constructor parameters, fields and properties."""
        if not hasattr(self, "_members"):
            self._members = {}
        if id(t) in self._members:
            return self._members[id(t)]
        out, f = {}, t.f
        if t.body:
            m = re.search(r"\b" + re.escape(t.name) + r"\s*(?:<[^>]*>)?\s*\(", f.bl[t.start:t.body[0]])
            if m:
                open_ = t.start + m.end() - 1
                for p in split_top(f.nc[open_ + 1:match_close(f.bl, open_, "(", ")")]):
                    p = re.sub(r"\s*=.*$", "", re.sub(r"\[[^\]]*\]", "", p), flags=re.S)   # attributes, default value
                    parts = p.strip().rsplit(None, 1)
                    if len(parts) == 2:
                        out[parts[1]] = simple_name(parts[0])
            seg = f.bl[t.body[0] + 1:t.body[1]]
            for fm in re.finditer(r"(?:private|protected|internal|public)\s+(?:static\s+)?(?:readonly\s+)?"
                                  r"([A-Z][\w.]*(?:<[^;=(){}]*>)?\??)\s+(_?\w+)\s*(?:[;=]|\{\s*get)", seg):
                out.setdefault(fm.group(2), simple_name(fm.group(1)))
        self._members[id(t)] = out
        return out

    def implementations(self, type_name):
        """The classes behind a type name: the class itself, or every class implementing an interface of that name."""
        if not hasattr(self, "_impls"):
            self._impls = {}
        if type_name not in self._impls:
            decls = self.types.get(type_name, [])
            out = [d for d in decls if d.kind in ("class", "record")]
            if any(d.kind == "interface" for d in decls):
                out += [d for lst in self.types.values() for d in lst
                        if d.kind in ("class", "record") and type_name in d.bases]
            self._impls[type_name] = out
        return self._impls[type_name]

    def service_calls(self, method):
        """[(position, [target methods])] for each `member.Name(...)` in the body whose member's declared type is
        known. Calls through a local variable, a chained expression, a static or an extension method are not
        resolved."""
        f, (a, b) = method.f, method.body
        members = {}
        for c in self.class_chain(method.cls):
            for k, v in self.members(c).items():
                members.setdefault(k, v)
        out = []
        for m in re.finditer(r"(?<![\w.])(?:this\s*\.\s*)?(_?\w+)\s*\.\s*([A-Z]\w*)\s*(?:" + GENERIC + r")?\s*\(", f.bl[a:b]):
            type_name = members.get(m.group(1))
            if not type_name:
                continue
            targets = [mt for cls in self.implementations(type_name) for c in self.class_chain(cls)
                       for mt in c.methods if mt.name == m.group(2) and mt.body]
            if targets:
                close = match_close(f.bl, a + m.end() - 1, "(", ")")
                argc = len(split_top(f.bl[a + m.end():close]))
                fitting = [t for t in targets if arity(t)[0] <= argc <= arity(t)[1]]
                out.append((a + m.start(2), fitting or targets))
        return out


# The status a type the handler maps itself answers with when its constructor names none.
DEFAULT_STATUS = {"BadHttpRequestException": 400}


def ctor_args(bl, open_pos):
    """The argument list of the constructor call whose `(` or `{` is at `open_pos` in the blanked source,
    so a number in the next statement is no status (string contents are already blanked)."""
    if open_pos >= len(bl) or bl[open_pos] not in "({":
        return ""
    close = ")" if bl[open_pos] == "(" else "}"
    return bl[open_pos + 1:match_close(bl, open_pos, bl[open_pos], close)]


def status_of(name):
    return http.HTTPStatus[re.sub(r"(?<!^)(?=[A-Z])", "_", name).upper()].value


def parse_exception_table(handler):
    """([(exception type, code or None for its own status, condition or None)], the type carrying its own status)
    of one handler, in the order of its cases. A case with a property pattern (`case X { ServiceFailed: true }:`) or
    a `when` clause maps the type only when its condition holds; otherwise the exception goes on to the next cases
    and, if none takes it, to `default` (500). A handler without a `switch (exception)` maps nothing: the middleware
    answers 500 for every exception."""
    if handler is None or not handler.body:
        return [], None
    src = handler.f.nc[handler.body[0]:handler.body[1]]
    m = re.search(r"switch\s*\(\s*exception\s*\)\s*\{", src)
    if not m:
        return [], None
    body = src[m.end():match_close(src, m.end() - 1, "{", "}")]
    table, pending, custom, parsed = [], [], None, 0
    for line in body.splitlines():
        line = line.split("//")[0].strip()
        cm = re.match(r"case\s+([\w.]+)\s*(\{[^{}]*\})?\s*(?:(?!when\b)[A-Za-z_]\w*)?\s*(?:\bwhen\b(.*))?:\s*$", line)
        if cm:
            cond = " ".join(x.strip() for x in (cm.group(2), cm.group(3) and "when " + cm.group(3)) if x) or None
            pending.append((cm.group(1).split(".")[-1], cond))
            parsed += 1
            continue
        sm = re.match(r"status\s*=\s*HttpStatusCode\.(\w+)\s*;", line)
        if sm and pending:
            code = status_of(sm.group(1))
            table += [(t, code, c) for t, c in pending]
            pending = []
            continue
        if re.match(r"status\s*=\s*\(HttpStatusCode\)", line) and pending:
            custom = pending[0][0]
            table += [(t, None, c) for t, c in pending]
            pending = []
            continue
        if line.startswith("break") or line.startswith("default"):
            pending = []
    total = len(re.findall(r"^\s*case\b", body, re.M))
    if parsed != total or len(table) != total:
        sys.exit(f"read {len(table)} of {total} cases of the exception switch in {handler.f.rel}; "
                 f"the handler changed shape — read it by hand")
    return table, custom


def condition_holds(cond, init):
    """True / False / None (undecided) for a handler case condition against the text that creates the exception
    (`new X(...) { P = v }`): a property pattern `{ P: v }` is decided when the initializer or a named argument sets
    every P; a `when` clause never is."""
    if not cond or "when " in cond:
        return None if cond else True
    pairs = re.findall(r"(\w+)\s*:\s*([^,{}]+)", cond)
    if not pairs:
        return None
    for prop, value in pairs:
        sm = re.search(rf"\b{prop}\s*=(?!=)\s*([^,;{{}})]+)", init) or \
            re.search(rf"\b{prop[:1].lower() + prop[1:]}\s*:\s*([^,;{{}})]+)", init)
        if not sm:
            return None
        if sm.group(1).strip() != value.strip():
            return False
    return True


# ---------------------------------------------------------------- evidence

class Ev:
    """One piece of evidence. `entry` is where it enters the method whose list holds it (the throw itself, or the
    call that leads to it); `hops` how many service calls lie between the action and the throw; `note` why a
    service-level finding counts only as a hint (empty when it counts)."""

    def __init__(self, code, kind, f, pos, exc=None, via=(), strong=True, detail="", entry=None, hops=0, note=""):
        self.code, self.kind, self.f, self.pos, self.exc = code, kind, f, pos, exc
        self.via, self.strong, self.detail = tuple(via), strong, detail
        self.entry = pos if entry is None else entry
        self.hops, self.note = hops, note

    def moved(self, entry, step, caller_hops, strong=None, note=None):
        """The same evidence as seen from a caller: entered through the call at `entry`, `step` prepended to the
        path. A service call raises `hops`; a helper or Demand* call keeps the callee's."""
        return Ev(self.code, self.kind, self.f, self.pos, self.exc, (step,) + self.via,
                  self.strong if strong is None else strong, self.detail, entry, max(self.hops, caller_hops),
                  self.note if note is None else note)

    def where(self):
        return f"{self.f.rel}:{self.f.line(self.pos)}"

    def text(self):
        return self.detail or self.f.line_text(self.pos)


def try_blocks(bl, a, b):
    """[(try_open, try_close, [(catch_type or None, has_when, catch_open, catch_close)])] inside bl[a:b]."""
    out = []
    for m in re.finditer(r"\btry\s*\{", bl[a:b]):
        t_open = a + m.end() - 1
        t_close = match_close(bl, t_open, "{", "}")
        catches, k = [], t_close + 1
        while True:
            cm = re.match(r"\s*catch\b\s*(\(([^)]*)\))?\s*(when\s*\()?", bl[k:k + 300])
            if not cm:
                break
            k += cm.end()
            if cm.group(3):
                k = match_close(bl, k - 1, "(", ")") + 1
            k = bl.find("{", k)
            c_close = match_close(bl, k, "{", "}")
            ctype = simple_name(cm.group(2).split()[0]) if cm.group(2) and cm.group(2).strip() else None
            catches.append((ctype, bool(cm.group(3)), k, c_close))
            k = c_close + 1
        out.append((t_open, t_close, catches))
    return out


def swallowed(index, f, method, pos, exc):
    """True when a try/catch around `pos` in `method` catches `exc` without rethrowing it."""
    if exc is None or not method.body:
        return False
    chain = index.type_chain(exc)
    blocks = sorted((tb for tb in try_blocks(f.bl, *method.body) if tb[0] < pos < tb[1]), key=lambda tb: -tb[0])
    for _, _, catches in blocks:
        for ctype, has_when, c_open, c_close in catches:
            if has_when:
                continue
            if ctype is None or ctype == "Exception" or ctype in chain:
                cbody = f.bl[c_open:c_close]
                if re.search(r"\bthrow\s*;", cbody) or re.search(r"\bthrow\s+(?!new\b)\w+\s*;", cbody):
                    break           # rethrows: try the next enclosing block
                return True
    return False


def in_fallback_arm(bl, start, pos):
    """True when the throw at `pos` is itself the `_ =>` arm of a switch expression or the whole `default:` section
    (a throw nested deeper inside a `default:` block is that block's own logic)."""
    before = bl[start:pos]
    return bool(re.search(r"(?<![\w.])_\s*=>\s*$", before) or re.search(r"\bdefault\s*:\s*\{?\s*$", before))


class Analyzer:
    """Evidence under one exception handler: the same throw is a different status in a host with its own handler."""

    def __init__(self, index, handler, honour_catches=True):
        self.ix = index
        self.handler = handler
        self.table, self.custom_case = parse_exception_table(handler)
        self.cache = {}
        self.cuts = 0        # how many times the walk stopped at the depth limit or on a cycle
        # False only for --audit-catches: the same walk with every try/catch ignored, to see what the catches hide
        self.honour_catches = honour_catches

    def caught(self, f, method, pos, exc):
        return self.honour_catches and swallowed(self.ix, f, method, pos, exc)

    def exc_code(self, exc_name, init=""):
        """(code, known, [(condition, code)]): status this host's exception handler gives this exception type. A
        conditional case whose condition `init` (the text creating the exception) does not decide is passed over and
        returned in the list: the handler gives that code instead when the condition holds."""
        chain = self.ix.type_chain(exc_name)
        undecided = []
        for case_type, code, cond in self.table:
            if case_type not in chain:
                continue
            holds = condition_holds(cond, init)
            if holds:
                return code, True, undecided
            if holds is None:
                undecided.append((cond, code))
        known = chain[-1] in ("Exception", "SystemException") or exc_name == "Exception"
        return 500, known, undecided

    def own_status(self, exc):
        """True for a type the handler answers with the status it carries (CustomHttpException)."""
        return exc == self.custom_case or any(t == exc and c is None for t, c, _ in self.table)

    # ---- how much a finding inside a service says about the action
    def service_note(self, method, pos, exc, hops):
        """Why a throw `hops` service calls away from the action is only a hint, or "" when it counts. These are the
        throws whose trigger is almost never the request: a null guard, the fallback arm of a switch (an
        exhaustiveness guard), and a framework exception two or more services deep (an internal consistency check);
        the repository's own exception types are thrown on purpose for a response, at any depth."""
        if hops < 1 or exc is None:
            return ""
        if self.exc_code(exc)[0] == 401:
            return "a sign-in check in a service: the pipeline's 401 (conventions §1)"
        if "ArgumentNullException" in self.ix.type_chain(exc):
            return "a null guard in a service"
        if in_fallback_arm(method.f.bl, method.body[0], pos):
            return "the fallback arm of a switch in a service"
        if hops >= 2 and not any(d.kind in ("class", "record") for d in self.ix.types.get(exc, [])):
            return f"a framework exception {hops} service calls deep"
        return ""

    def dead_ranges(self, callee, args_text):
        """[(start, end)] of the blocks in `callee` that a literal true/false argument (or a literal default the call
        leaves out) switches off: `if (flag)` with false, `if (!flag)` with true. DemandRebrandingPermissionAsync(false)
        skips its tariff check this way."""
        if not callee.body or not re.search(r"\b(true|false)\b", args_text + callee.params):
            return []
        params = [p for p in split_top(callee.params) if p.strip()]
        names, defaults = [], {}
        for p in params:
            p = re.sub(r"\[[^\]]*\]", "", p)
            dm = re.search(r"=\s*(true|false)\s*$", p)
            decl = re.sub(r"\s*=.*$", "", p, flags=re.S).split()
            if not decl:
                return []
            names.append(decl[-1])
            if dm:
                defaults[decl[-1]] = dm.group(1) == "true"
        bound = dict(defaults)
        for i, arg in enumerate(a.strip() for a in split_top(args_text)):
            nm = re.match(r"(\w+)\s*:\s*(.*)$", arg, re.S)
            name, value = (nm.group(1), nm.group(2).strip()) if nm and nm.group(1) in names else \
                (names[i] if i < len(names) else None, arg)
            if name is None:
                continue
            if value in ("true", "false"):
                bound[name] = value == "true"
            else:
                bound.pop(name, None)
        bl, (a, b) = callee.f.bl, callee.body
        out = []
        for name, value in bound.items():
            for m in re.finditer(rf"\bif\s*\(\s*(!?)\s*{re.escape(name)}\s*\)", bl[a:b]):
                if value != bool(m.group(1)):
                    continue                    # the branch runs
                k = a + m.end()
                while k < b and bl[k].isspace():
                    k += 1
                out.append((k, match_close(bl, k, "{", "}") if bl.startswith("{", k) else bl.find(";", k)))
        return out

    MAX_DEPTH = 5
    # Service calls followed from an action: `member.Name(...)` into Name of the member's declared type (every class
    # implementing it when it is an interface). Deeper findings are mostly internal checks; see service_note.
    SERVICE_HOPS = 3

    def method_evidence(self, method, depth=0, stack=(), hops=0):
        """Evidence of `method` at `hops` service calls from the action: its own throws and results, its helpers and
        Demand* calls (same hops), and the services it calls (hops + 1, up to SERVICE_HOPS)."""
        key = (method.key, hops)
        hit = self.cache.get(key)
        if hit and depth + hit[1] <= self.MAX_DEPTH:
            return hit[0]
        if not method.body:
            return []
        # a cycle is the same method again at the same service level; a service call raises the level, which
        # SERVICE_HOPS bounds, so services calling each other end there without cutting (and uncaching) the walk
        if depth > self.MAX_DEPTH or key in stack:
            self.cuts += 1
            return []
        cuts_before, height = self.cuts, 0
        stack = stack + (key,)
        f, (a, b) = method.f, method.body
        bl = f.bl
        evs = []
        chain = self.ix.class_chain(method.cls)
        chain_methods = {}
        for c in chain:
            for mt in c.methods:
                chain_methods.setdefault(mt.name, []).append(mt)
        members, followed = {}, set()
        if hops < self.SERVICE_HOPS:
            for c in chain:
                for k, v in self.ix.members(c).items():
                    members.setdefault(k, v)

        def add(ev, exc_for_catch):
            if not self.caught(f, method, ev.pos, exc_for_catch):
                note = self.service_note(method, ev.pos, ev.exc, hops) if ev.kind.startswith("throw") else ""
                if note and ev.strong:
                    ev.strong, ev.note = False, note
                ev.hops = hops
                evs.append(ev)

        for m in re.finditer(r"\bthrow\s+new\s+([\w.]+)\s*(<[^>]*>)?\s*[({]", bl[a:b]):
            pos = a + m.start()
            exc = m.group(1).split(".")[-1]
            # every type the handler answers with its own status (CustomHttpException, BadHttpRequestException)
            if self.own_status(exc):
                args = ctor_args(bl, a + m.end() - 1)
                cm = re.search(r"HttpStatusCode\.(\w+)|\b([1-5]\d\d)\b", args)
                if cm and cm.group(1):
                    code = status_of(cm.group(1))
                elif cm:
                    code = int(cm.group(2))
                elif exc in DEFAULT_STATUS:
                    code = DEFAULT_STATUS[exc]
                else:
                    add(Ev(500, "throw?", f, pos, exc, strong=False, detail="custom status not readable"), exc)
                    continue
                add(Ev(code, "throw", f, pos, exc), exc)
                continue
            end = bl.find(";", a + m.end())
            code, known, undecided = self.exc_code(exc, bl[a + m.end() - 1:end if end >= 0 else b])
            add(Ev(code, "throw" if known else "throw?", f, pos, exc, strong=known), exc)
            for cond, alt in undecided:
                add(Ev(alt, "throw?", f, pos, exc, strong=False,
                       detail=f"{f.line_text(pos)}  [the handler answers {alt} only when {exc} {cond}]"), exc)

        for m in re.finditer(r"\b(\w+Exception)\.ThrowIf\w*\s*\(", bl[a:b]):
            pos = a + m.start()
            code, _, _ = self.exc_code(m.group(1))
            add(Ev(code, "arg-guard", f, pos, m.group(1), strong=False), m.group(1))

        for m in re.finditer(r"\bTryGetFromCache\s*\(", bl[a:b]):
            evs.append(Ev(304, "not-modified", f, a + m.start(), hops=hops))

        for m in re.finditer(r"(?<![.\w])(NotFound|BadRequest|Unauthorized|Forbid|Conflict|UnprocessableEntity|NoContent|StatusCode)\s*\(", bl[a:b]):
            pos = a + m.start()
            if m.group(1) == "StatusCode":
                args = bl[pos:match_close(bl, a + m.end() - 1, "(", ")")]
                cm = re.search(r"HttpStatusCode\.(\w+)|Status(\d{3})\w*|\b([1-5]\d\d)\b", args)
                if not cm:
                    continue
                if cm.group(1):
                    code = status_of(cm.group(1))
                else:
                    code = int(cm.group(2) or cm.group(3))
            else:
                code = RESULT_HELPERS[m.group(1)]
            evs.append(Ev(code, "result", f, pos, hops=hops))

        for m in re.finditer(r"\bStatusCode\s*=\s*(?:\(int\)\s*)?(?:HttpStatusCode\.(\w+)|StatusCodes\.Status(\d{3})\w*|([1-5]\d\d)\b)", bl[a:b]):
            if m.group(1):
                code = status_of(m.group(1))
            else:
                code = int(m.group(2) or m.group(3))
            if code >= 300:
                evs.append(Ev(code, "status", f, a + m.start(), hops=hops))

        # calls: helpers of the same class chain (any name), Demand* methods anywhere, services by declared type
        for m in re.finditer(r"(?:(\w+)\s*\.\s*)?\b([A-Z]\w*)\s*(?:" + GENERIC + r")?\s*\(", bl[a:b]):
            receiver, name = m.group(1), m.group(2)
            pos = a + m.start(2)
            if bl[max(0, a + m.start() - 4):a + m.start()].strip().endswith("new"):
                continue
            targets, kind = [], None
            if (receiver in (None, "this", "base") or any(c.name == receiver for c in chain)) and name in chain_methods:
                targets, kind = chain_methods[name], "helper"
            elif name.startswith("Demand"):
                targets = [mt for mt in self.ix.methods.get(name, [])
                           if not re.search(r"\bprivate\b", mt.modifiers) and mt.body]
                kind = "demand"
                if not targets:
                    evs.append(Ev(403, "demand?", f, pos, "AuthorizingException", strong=False,
                                  detail=f"{f.line_text(pos)}  [no declaration of {name} found]", hops=hops))
                    continue
            elif receiver and hops < self.SERVICE_HOPS and \
                    (bl[a + m.start() - 1] != "." or re.search(r"\bthis\s*\.\s*$", bl[a:a + m.start()])):
                if receiver in members:              # a field, property or constructor parameter: its declared type
                    type_name = members[receiver]
                    targets = [mt for cls in self.ix.implementations(type_name) for c in self.ix.class_chain(cls)
                               for mt in c.methods if mt.name == name and mt.body]
                else:                                # a static call on a repository class: `FileOperationsManager.GetIds(`
                    type_name = receiver
                    targets = [mt for cls in self.ix.types.get(receiver, []) if cls.kind in ("class", "record")
                               for mt in cls.methods if mt.name == name and mt.body
                               and re.search(r"\bstatic\b", mt.modifiers)]
                kind = "service"
            if not targets:
                continue
            # overloads: keep those whose parameter count fits the call's argument count
            close = match_close(bl, a + m.end() - 1, "(", ")")
            args_text = bl[a + m.end():close]
            argc = len(split_top(args_text))
            fitting = [t for t in targets if arity(t)[0] <= argc <= arity(t)[1]]
            targets = [t for t in (fitting or targets) if t.key != method.key]
            if kind == "service" and not fitting:
                continue
            followed.update(t.key for t in targets)
            per_target = []
            for t in targets:
                if kind == "service" and not (hops == 0 and self.ix.project(t.f) == self.ix.project(f)):
                    callee_evs = self.method_evidence(t, 0, stack, hops + 1)
                else:
                    # a helper, a Demand*, or a class of the action's own project (FilesControllerHelper): the
                    # controller's layer, no service hop
                    callee_evs = self.method_evidence(t, depth + 1, stack, hops)
                    if (t.key, hops) in self.cache:
                        height = max(height, 1 + self.cache[(t.key, hops)][1])
                dead = self.dead_ranges(t, args_text)
                per_target.append([ev for ev in callee_evs if ev.kind != "dto"
                                   and not any(lo <= ev.entry <= hi for lo, hi in dead)])
            # A service call that resolves to several bodies (implementations of an interface, overloads that fit):
            # a code counts only when every one of them produces it; otherwise which one runs decides, and the
            # scanner cannot tell.
            agreed = None
            if kind == "service" and len(targets) > 1:
                sets = [{ev.code for ev in lst if ev.strong} for lst in per_target]
                agreed = set.intersection(*sets) if sets else set()
            step_name = f"{type_name}.{name}" if kind == "service" else name
            for t, lst in zip(targets, per_target):
                for ev in lst:
                    if self.caught(f, method, pos, ev.exc):
                        continue
                    strong, note = None, None
                    if agreed is not None and ev.strong and ev.code not in agreed:
                        strong, note = False, f"only some of the {len(targets)} bodies `{step_name}` resolves to"
                    evs.append(ev.moved(pos, f"{step_name}@{f.rel.split('/')[-1]}:{f.line(pos)}", hops,
                                        strong, note))
        # CodeGraph, when the repository has an index: calls the text cannot resolve (a local variable, a chained
        # call, a lambda) are followed too, but what they lead to is only a hint. The graph links by name, so it
        # also links overloads and namesakes that are never called here.
        if self.ix.graph and hops < self.SERVICE_HOPS:
            for line, t in self.ix.graph.callees(method):
                if t.key in followed or t.key == method.key or t.name in chain_methods:
                    continue
                followed.add(t.key)
                lstart = f.line_start(line)
                p = bl.find(t.name, lstart)
                pos = p if a <= p <= b else max(a, lstart)
                for ev in self.method_evidence(t, 0, stack, hops + 1):
                    if ev.kind == "dto" or self.caught(f, method, pos, ev.exc):
                        continue
                    evs.append(ev.moved(pos, f"{t.cls.name}.{t.name}@{f.rel.split('/')[-1]}:{f.line(pos)}", hops,
                                        False, ev.note or "a call only CodeGraph links"))
        # one line per site: overload chains reach the same throw several times; keep a counted path over a hint,
        # then the shortest
        best = {}
        for ev in evs:
            k = (ev.code, ev.kind, ev.f.rel, ev.pos)
            if k not in best or (not ev.strong, len(ev.via)) < (not best[k].strong, len(best[k].via)):
                best[k] = ev
        evs = sorted(best.values(), key=lambda e: (e.hops, len(e.via), e.f.rel, e.pos))
        # At the action's own level only a complete walk is cached, with its height: reused from another depth, a
        # walk cut short would make an action's evidence (and the fingerprint of its rejections) depend on which
        # action was analysed first. Inside services, where methods call each other in cycles, refusing to cache a
        # cut walk makes the scan exponential; there the first walk is kept.
        if self.cuts == cuts_before or hops:
            self.cache[key] = (evs, height)
        return evs

    def deep_evidence(self, method):
        """{code: [Ev]} of `method` with the services it calls (--audit-catches): every finding that would count
        without the service-level hint rules, so the audit sees each producer of a declared code."""
        out = {}
        for e in self.method_evidence(method):
            if (e.strong or e.note) and e.kind != "dto":
                out.setdefault(e.code, []).append(e)
        return out

    # ---- DTO binding -> 400
    def props(self, t):
        if t.props is not None:
            return t.props
        out = []
        if t.body:
            bl, src = t.f.bl, t.f.nc
            seg = bl[t.body[0] + 1:t.body[1]]
            for m in re.finditer(r"\bpublic\s+((?:required\s+|virtual\s+|override\s+|new\s+)*)([\w<>\[\],.?\s]+?)\s+([A-Z]\w*)\s*(?:\{|=>)", seg):
                pos = t.body[0] + 1 + m.start()
                # only members of this type, not of a nested one
                if seg[:m.start()].count("{") - seg[:m.start()].count("}") != 0:
                    continue
                attrs = src[attr_block_start(bl, pos):pos]
                out.append(("required" in m.group(1), m.group(2).strip(), m.group(3), attrs, pos))
        t.props = out
        return out

    def validation_names(self):
        if not hasattr(self, "_vnames"):
            names = set(BCL_VALIDATION)
            for name in self.ix.types:
                if name.endswith("Attribute") and "ValidationAttribute" in self.ix.type_chain(name):
                    names.add(name[:-len("Attribute")])
            self._vnames = names
        return self._vnames

    def attr_names(self, attrs):
        return {re.sub(r"Attribute$", "", n.split(".")[-1])
                for n in re.findall(r"(?:\[|,)\s*([A-Z][\w.]*)\s*(?=[(\],])", attrs)}

    def dto_type(self, type_text):
        name = simple_name(type_text)
        if name in PRIMITIVES:
            return None
        for d in self.ix.types.get(name, []):
            if d.kind in ("class", "record", "struct"):
                return d
        return None

    def has_converter(self, t, attrs=""):
        return bool(re.search(r"\b(JsonConverter|TypeConverter|ModelBinder)\b", t.attrs + attrs))

    def type_props(self, t):
        for c in self.ix.class_chain(t):
            for p in self.props(c):
                yield c, p

    def body_evidence(self, t, via, depth, seen, evs):
        if depth > 4 or t.name in seen or self.has_converter(t):
            return
        seen = seen | {t.name}
        if "IValidatableObject" in self.ix.type_chain(t.name):
            evs.append(Ev(400, "dto", t.f, t.start, detail=f"{t.name} : IValidatableObject  [{' > '.join(via)}]"))
        vnames = self.validation_names()
        for c, (req, ptype, pname, attrs, pos) in self.type_props(t):
            used = self.attr_names(attrs) & vnames
            if req:
                evs.append(Ev(400, "dto", c.f, pos, detail=f"required {pname} in the JSON body  [{' > '.join(via)}]"))
            if used:
                evs.append(Ev(400, "dto", c.f, pos,
                              detail=f"[{', '.join(sorted(used))}] on {pname}  [{' > '.join(via)}]"))
            inner = generic_args(ptype) or [ptype]
            for it in inner:
                # an enum sent as a string is a type mismatch: binding answers it, the 400 text does not list it (§3)
                d = self.dto_type(it)
                if d and not self.has_converter(d, attrs):
                    self.body_evidence(d, via + [pname], depth + 1, seen, evs)

    def dto_evidence(self, action):
        evs = []
        vnames = self.validation_names()
        for p in split_top(action.params):
            p = p.strip()
            if not p:
                continue
            attrs = " ".join(re.findall(r"\[[^\]]*\]", p))
            decl = re.sub(r"\[[^\]]*\]", "", p).strip()
            decl = re.sub(r"\s*=.*$", "", decl)
            parts = decl.rsplit(None, 1)
            if len(parts) != 2:
                continue
            ptype, pname = parts
            used = self.attr_names(attrs) & vnames
            if used:
                evs.append(Ev(400, "dto", action.f, action.sig_pos, detail=f"[{', '.join(sorted(used))}] on parameter {pname}"))
            t = self.dto_type(ptype)
            if not t or self.has_converter(t, attrs):
                continue
            # [ApiController] infers [FromBody] for a complex parameter that names no source and whose members name
            # none either (AuthWithCodeRequestsDto): its whole graph is then validated like an explicit body
            inferred_body = not re.search(r"\b(From\w+|AsParameters|ModelBinder)\b", attrs) and not any(
                re.search(r"\b(From\w+|AsParameters|ModelBinder)\b", pa) for _, (_, _, _, pa, _) in self.type_props(t))
            if "FromBody" in attrs or inferred_body:
                self.body_evidence(t, [pname], 0, set(), evs)
                continue
            if "IValidatableObject" in self.ix.type_chain(t.name):
                evs.append(Ev(400, "dto", t.f, t.start, detail=f"{t.name} : IValidatableObject"))
            for c, (req, ptype2, pname2, pattrs, pos) in self.type_props(t):
                if "FromBody" in pattrs:
                    bt = self.dto_type(ptype2)
                    if bt:
                        self.body_evidence(bt, [pname, pname2], 0, set(), evs)
                    continue
                pused = self.attr_names(pattrs) & vnames
                if pused:
                    evs.append(Ev(400, "dto", c.f, pos, detail=f"[{', '.join(sorted(pused))}] on {pname2}"))
        return evs


# ---------------------------------------------------------------- actions and queue

class Action:
    def __init__(self, method, doc, public, proj):
        self.m = method
        self.doc, self.public, self.proj = doc, public, proj
        hm = re.search(r"\[Http(Get|Post|Put|Delete|Patch|Head)\s*(?:\(\s*\"([^\"]*)\")?", method.attrs)
        self.verb = hm.group(1).upper()
        self.route = hm.group(2) or ""
        self.declared = {}
        for dm in re.finditer(r"SwaggerResponse\(\s*(\d{3})\s*(?:,\s*\"((?:[^\"\\]|\\.)*)\")?", method.attrs):
            self.declared[int(dm.group(1))] = (dm.group(2) or "").replace('\\"', '"')
        self.id = f"{method.f.rel}::{method.name}"
        self.evs = []

    @property
    def line(self):
        return self.m.f.line(self.m.sig_pos)


def doc_of_project(csproj):
    text = csproj.read_text(encoding="utf-8-sig", errors="replace")
    m = re.search(r"--file-name\s+(\w+)", text)
    return m.group(1) if m else None


def public_docs():
    try:
        settings = json.loads(DOCS_SETTINGS.read_text(encoding="utf-8-sig"))
    except (OSError, ValueError):
        return []
    names = []
    for v in settings.get("join", {}).values():
        if isinstance(v, list) and v and str(v[-1]).endswith(".json"):
            names.append(re.sub(r"_.*$", "", Path(v[-1]).stem))
    return names


def nearest_csproj(path):
    d = path.parent
    while d != ROOT and d != d.parent:
        found = list(d.glob("*.csproj"))
        if found:
            return found[0]
        d = d.parent
    return None


def collect_actions(ix):
    pub = public_docs()
    proj_cache, actions = {}, []
    for t_list in ix.types.values():
        for t in t_list:
            if re.search(r"IgnoreApi\s*=\s*true", t.attrs):
                continue
            for mt in t.methods:
                if not re.search(r"\[Http(Get|Post|Put|Delete|Patch|Head)\b", mt.attrs):
                    continue
                if re.search(r"IgnoreApi\s*=\s*true", mt.attrs):
                    continue
                proj = ix.project(mt.f)
                if proj not in proj_cache:
                    proj_cache[proj] = doc_of_project(proj) if proj else None
                doc = proj_cache[proj]
                if not doc:
                    continue
                actions.append(Action(mt, doc, doc in pub, proj))
    counts = {}
    for a in actions:
        counts[a.id] = counts.get(a.id, 0) + 1
    for a in actions:
        if counts[a.id] > 1:
            a.id = f"{a.id}[{a.verb} {a.route}]"
    order = {d: i for i, d in enumerate(pub)}
    actions.sort(key=lambda a: (not a.public, order.get(a.doc, 99), a.doc, a.m.f.rel, a.line))
    return actions


def api_controller_attrs(ix):
    """[ApiController] and every repository attribute deriving from it: they switch on the automatic model-state 400."""
    names = {"ApiController"}
    for name in ix.types:
        if name.endswith("Attribute") and "ApiControllerAttribute" in ix.type_chain(name)[1:]:
            names.add(name[:-len("Attribute")])
    return re.compile(r"\[\s*(?:" + "|".join(sorted(names)) + r")(?:Attribute)?\b")


def analyse(ix, actions):
    """Evidence of every action, under the exception handler of its host.
    Returns {handler file or None: (Analyzer, global filters, documents)} for the header."""
    hosts, auto_400 = {}, api_controller_attrs(ix)
    for a in actions:
        handler, filters = ix.host(a.proj)
        key = handler.f.rel if handler else None
        if key not in hosts:
            hosts[key] = (Analyzer(ix, handler), filters, set())
        an, _, docs = hosts[key]
        docs.add(a.doc)
        dto = an.dto_evidence(a.m)
        if not any(auto_400.search(c.attrs) for c in ix.class_chain(a.m.cls)):
            for e in dto:
                e.kind, e.strong = "dto?", False
                e.detail += "  [no ApiController: only if the action checks ModelState itself]"
        a.evs = an.method_evidence(a.m) + dto
    # a project another host also loads: what each exception becomes there (conventions §1)
    for proj in {a.proj for a in actions if a.proj}:
        own = ix.host(proj)
        for other in ix.hosted_in(proj):
            handler, filters = ix.host(other)
            if (handler, filters) != own:
                FOREIGN.setdefault(proj, []).append((other.stem, Analyzer(ix, handler)))
    for a in actions:
        for where in {e.where() for e in a.evs if e.code == SERVER_ERROR and e.strong}:
            SITE_FANIN[where] = SITE_FANIN.get(where, 0) + 1
    return hosts


FOREIGN = {}     # project -> [(other host project name, its Analyzer)], filled by analyse()


def print_hosts(hosts):
    """What the pipeline of each host does, read from its startup chain on this run."""
    for key, (an, filters, docs) in sorted(hosts.items(), key=lambda kv: (kv[0] is None, kv[0] or "")):
        names = ", ".join(sorted(docs))
        if an.table:
            table = " | ".join(f"{t}{' ' + cond if cond else ''} {c if c else 'own status'}"
                               for t, c, cond in an.table) + " | anything else 500"
        else:
            table = "no mapping, every exception is 500"
        print(f"[{names}] exception -> status ({key or 'no exception handler registered'}, first match wins): {table}")
        print(f"[{names}] global filters: {', '.join(filters) or 'none'}")


def print_foreign_hosts(ix, actions):
    """A project whose controllers another host also loads answers differently there: an exception the action does
    not turn into a status itself gets the other host's table. Conventions §1 says what to document then."""
    for proj in sorted({a.proj for a in actions if a.proj}):
        docs = ", ".join(sorted({a.doc for a in actions if a.proj == proj}))
        own = ix.host(proj)
        for other in ix.hosted_in(proj):
            handler, filters = ix.host(other)
            if (handler, filters) == own:
                continue        # the same pipeline in both hosts: nothing to tell
            table = handler.f.rel if handler else "no exception handler registered"
            print(f"[{docs}] also hosted by {other.relative_to(ROOT).as_posix()} (AddApplicationPart): there an "
                  f"exception maps by {table}, global filters {', '.join(filters) or 'none'}")


def fingerprint(a, code):
    h = hashlib.sha1(norm(a.m.body_text()).encode())
    parts = sorted({f"{e.kind}|{norm(e.text())}|{'>'.join(v.split('@')[0] for v in e.via)}"
                    for e in a.evs if e.code == code})
    for p in parts:
        h.update(p.encode())
    return h.hexdigest()[:16]


def load_rejected():
    if REJECTED.exists():
        return json.loads(REJECTED.read_text(encoding="utf-8"))
    return []


def save_rejected(entries):
    if not entries:
        REJECTED.unlink(missing_ok=True)
        return
    entries.sort(key=lambda e: (e["id"], e["code"]))
    REJECTED.write_text(json.dumps(entries, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


SERVER_ERROR = 500  # an exception no handler case maps: a server defect, reported to the user, never documented


def gaps(a):
    strong = sorted({e.code for e in a.evs if e.strong})
    return [c for c in strong if c not in a.declared and not (200 <= c < 300) and c != SERVER_ERROR]


def server_errors(a):
    """Counted evidence of a 500. Kept out of the gap: the skill neither documents nor fixes it, it reports it.
    A throw site of this action alone comes first; one that many actions reach (tenant resolution, a lock) last."""
    return sorted((e for e in a.evs if e.code == SERVER_ERROR and e.strong), key=lambda e: SITE_FANIN.get(e.where(), 0))


SITE_FANIN = {}  # throw site -> number of actions with counted 500 evidence there, filled by analyse()


def unbacked_codes(a):
    """Declared non-2xx codes with no evidence of any strength (counted or hint) in the action or its services."""
    seen = {e.code for e in a.evs}
    return [c for c in sorted(a.declared) if c >= 300 and c not in seen]


def apply_rejections(actions, rejected):
    by_id = {a.id: a for a in actions}
    state = []
    for r in rejected:
        a = by_id.get(r["id"])
        if not a or r["code"] not in gaps(a):
            state.append((r, "orphaned"))
        elif fingerprint(a, r["code"]) == r["fingerprint"]:
            state.append((r, "valid"))
        else:
            state.append((r, "expired"))
    valid = {(r["id"], r["code"]) for r, s in state if s == "valid"}
    expired = {(r["id"], r["code"]): r for r, s in state if s == "expired"}
    return valid, expired, state


def dirty_files():
    try:
        out = subprocess.run(["git", "status", "--porcelain", "--", *SOURCE_DIRS], cwd=ROOT,
                             capture_output=True, text=True, encoding="utf-8").stdout
    except OSError:
        return set()
    return {line[3:].strip().strip('"') for line in out.splitlines() if line[:2].strip()}


# ---------------------------------------------------------------- output

def fmt_ev(e, mark, proj=None):
    via = f"  via {' > '.join(e.via)}" if e.via else ""
    exc = f" {e.exc}" if e.exc and e.kind.startswith("throw") else ""
    hops = f" [service hop {e.hops}]" if e.hops else ""
    note = f"  [hint: {e.note}]" if e.note else ""
    other = ""
    if e.exc and e.kind.startswith("throw") and proj in FOREIGN:
        alts = [f"{name}: {an.exc_code(e.exc)[0]}" for name, an in FOREIGN[proj]
                if not an.own_status(e.exc) and an.exc_code(e.exc)[0] != e.code]
        other = f"  [in {', '.join(alts)}]" if alts else ""
    return f"    {mark} {e.code} {e.kind}{exc}{hops}  {e.where()}  {e.text()[:150]}{note}{other}{via}"


def print_action(a, valid, expired, dirty, detail):
    open_codes = [c for c in gaps(a) if (a.id, c) not in valid]
    flag = "  [file has uncommitted changes]" if a.m.f.rel in dirty else ""
    print(f"{a.id}  {a.verb} {a.route or '(no route)'}  doc={a.doc}{'' if a.public else ' (not public)'}{flag}")
    print(f"    {a.m.f.rel}:{a.line}   declared: {' '.join(map(str, sorted(a.declared))) or '-'}   "
          f"gap: {' '.join(map(str, open_codes)) or '-'}")
    for c in open_codes:
        r = expired.get((a.id, c))
        if r:
            print(f"    ! {c} was rejected on {r['date']} ({r['reason']}), but the code behind it changed since")
        for e in [e for e in a.evs if e.code == c and e.strong][:6 if detail else 3]:
            print(fmt_ev(e, "+", a.proj))
    for e in server_errors(a)[:6 if detail else 3]:
        n = SITE_FANIN.get(e.where(), 0)
        print(fmt_ev(e, "*", a.proj) + (f"  [reached from {n} actions]" if n > 1 else ""))
    rej = [c for c in gaps(a) if (a.id, c) in valid]
    if rej:
        print(f"    rejected (still valid): {' '.join(map(str, rej))}")
    # a lead, not a verdict: calls through locals, deeper services and framework code are not followed
    unbacked = unbacked_codes(a)
    if unbacked and not detail:
        print(f"    no evidence in the action or the services followed for declared: {' '.join(map(str, unbacked))}")
    if detail:
        for e in [e for e in a.evs if not e.strong and e.code not in a.declared][:10]:
            print(fmt_ev(e, "?", a.proj))
        for e in [e for e in a.evs if e.code in a.declared and e.code != SERVER_ERROR][:12]:
            print(fmt_ev(e, "=", a.proj))
        for c in unbacked:
            print(f"    - {c} declared, nothing found behind it in the action or the services followed: "
                  f"\"{a.declared[c][:120]}\"")


def resolve(actions, ref):
    ref = ref.replace("\\", "/")
    hits = [a for a in actions if a.id == ref]
    if not hits:
        hits = [a for a in actions if a.id.startswith(ref + "[") or a.id.split("[")[0] == ref]
    if not hits:
        hits = [a for a in actions if a.m.f.rel == ref or a.m.f.rel.endswith("/" + ref)]
    if not hits:
        hits = [a for a in actions if a.id.endswith("::" + ref) or a.m.name == ref]
    return hits


def rel_path(p):
    path = Path(p)
    return (path.resolve().relative_to(ROOT) if path.is_absolute() else path).as_posix()


def snapshot_path(rel):
    """Where --snapshot keeps the pre-edit copy of a file: inside the git directory, never in the work tree."""
    git_path = subprocess.run(["git", "rev-parse", "--git-path", "openapi-missing-responses/snapshots"], cwd=ROOT,
                              capture_output=True, text=True).stdout.strip()
    return ROOT / git_path / rel


def eol_of(data):
    crlf, lf = data.count(b"\r\n"), data.count(b"\n")
    if lf == 0:
        return "none"
    return "CRLF" if crlf == lf else ("LF" if crlf == 0 else f"MIXED ({lf - crlf} LF-only lines)")


def head_commit():
    return subprocess.run(["git", "rev-parse", "HEAD"], cwd=ROOT, capture_output=True, text=True).stdout.strip()


def cmd_snapshot(paths):
    head = head_commit()
    for p in paths:
        rel = rel_path(p)
        data = (ROOT / rel).read_bytes()
        dst = snapshot_path(rel)
        dst.parent.mkdir(parents=True, exist_ok=True)
        dst.write_bytes(data)
        # the commit the snapshot was taken on: a snapshot left from an earlier batch is reported as stale
        dst.with_name(dst.name + ".head").write_text(head, encoding="utf-8")
        print(f"snapshot {rel}: {len(data)} bytes")


def cmd_diff_check(paths):
    ok = True
    for p in paths:
        rel = rel_path(p)
        data = (ROOT / rel).read_bytes()
        snap = snapshot_path(rel)
        if snap.exists():
            # the pre-edit copy: changes the user had made before the batch are part of it, not of the diff
            base = snap.read_bytes()
            base_name = "snapshot"
            head_file = snap.with_name(snap.name + ".head")
            taken_on = head_file.read_text(encoding="utf-8").strip() if head_file.exists() else ""
            if taken_on != head_commit():
                ok = False
                print(f"{rel}: the snapshot was taken on {taken_on[:10] or 'an unknown commit'}, HEAD is "
                      f"{head_commit()[:10]} now: likely left from an earlier batch; take a new one before editing")
            old = base.decode("utf-8", errors="replace").splitlines()
            new = data.decode("utf-8", errors="replace").splitlines()
            diff = [l for l in difflib.unified_diff(old, new, n=0, lineterm="") if not l.startswith(("+++", "---", "@@"))]
            eol_base = eol_of(base)
        else:
            base = subprocess.run(["git", "show", f"HEAD:{rel}"], cwd=ROOT, capture_output=True).stdout
            base_name = "HEAD"
            out = subprocess.run(["git", "diff", "-U0", "HEAD", "--", rel], cwd=ROOT, capture_output=True, text=True,
                                 encoding="utf-8").stdout
            diff = [l for l in out.splitlines() if l[:1] in "+-" and not l.startswith(("+++", "---"))]
            eol_base = None      # the HEAD blob is stored normalized; only mixed endings are a finding
        bad = [l for l in diff if not re.match(r"^[+-]\s*\[SwaggerResponse\(", l)]
        added = sum(1 for l in diff if l.startswith("+"))
        removed = sum(1 for l in diff if l.startswith("-"))
        bom_now, bom_base = data[:3] == b"\xef\xbb\xbf", base[:3] == b"\xef\xbb\xbf"
        eol = eol_of(data)
        print(f"{rel}: against {base_name}  +{added} -{removed}  BOM {'yes' if bom_now else 'no'} "
              f"({base_name} {'yes' if bom_base else 'no'})  EOL {eol}"
              f"{f' ({base_name} {eol_base})' if eol_base else ''}")
        for l in bad[:10]:
            print(f"    not a SwaggerResponse line: {l[:140]}")
        if bad or bom_now != bom_base or eol.startswith("MIXED") or (eol_base and eol != eol_base):
            ok = False
    print("VERDICT:", "clean" if ok else "PROBLEMS above")


def cmd_verify_doc(actions, refs):
    targets = [a for r in refs for a in resolve(actions, r)]
    if not targets:
        sys.exit("no action matches")
    docs = {}
    ok = True
    for a in targets:
        files = sorted(DOCS_DIR.glob(f"{a.doc}_*.json"))
        if not files:
            print(f"{a.id}: no document {a.doc}_*.json")
            ok = False
            continue
        path = files[0]
        if path not in docs:
            docs[path] = json.loads(path.read_text(encoding="utf-8-sig"))
        doc = docs[path]
        op_id = a.m.name[:-5] if a.m.name.endswith("Async") else a.m.name
        op_id = op_id[:1].lower() + op_id[1:]
        # the generator lower-cases the literal segments of a path (`fromTemplate` -> `fromtemplate`)
        route = re.sub(r"\{(\w+)[:?][^}]*\}", r"{\1}", a.route.strip("/~")).lower()
        ops = []
        for pth, item in doc.get("paths", {}).items():
            o = item.get(a.verb.lower())
            p = pth.rstrip("/").lower()
            if o and o.get("operationId") == op_id and (not route or p.endswith("/" + route) or p.endswith(route)):
                ops.append((pth, o))
        stale = path.stat().st_mtime < a.m.f.path.stat().st_mtime
        print(f"{a.id} -> {path.name}{'  [document older than the source: regenerate]' if stale else ''}")
        if not ops:
            print(f"    operation {a.verb} .../{route} ({op_id}) not found")
            ok = False
            continue
        for pth, o in ops:
            resp = o.get("responses", {})
            for code, text in sorted(a.declared.items()):
                r = resp.get(str(code))
                if r is None:
                    state = "MISSING"
                elif norm(r.get("description", "")) != norm(text):
                    state = f"TEXT DIFFERS: {r.get('description', '')[:100]!r}"
                else:
                    state = "ok"
                if state != "ok":
                    ok = False
                print(f"    {a.verb} {pth} {code}: {state}")
    print("VERDICT:", "documented" if ok else "PROBLEMS above")


def cmd_audit_catches(ix, actions, refs):
    """Declared error codes whose every producer found is caught on the way: the action's own code, its controller
    helpers, Demand* and the services it calls (Analyzer.deep_evidence) walked twice, with the try/catch filtering
    and without it. A code that exists only without it may be unreachable — conventions §7 decides."""
    targets = [a for r in refs for a in resolve(actions, r)] if refs else actions
    if refs and not targets:
        sys.exit("no action matches")
    honoured, ignored = {}, {}
    flagged = 0
    for a in targets:
        handler, _ = ix.host(a.proj)
        key = handler.f.rel if handler else None
        if key not in honoured:
            honoured[key] = Analyzer(ix, handler)
            ignored[key] = Analyzer(ix, handler, honour_catches=False)
        with_catches = honoured[key].deep_evidence(a.m)
        without = ignored[key].deep_evidence(a.m)
        own = {e.code for e in a.evs if not e.hops}
        hidden = [c for c in sorted(a.declared) if c >= 400 and c != SERVER_ERROR
                  and c in without and c not in with_catches and c not in own]
        if not hidden:
            continue
        flagged += 1
        print(f"\n{a.id}  {a.verb} {a.route or '(no route)'}  doc={a.doc}")
        print(f"    {a.m.f.rel}:{a.line}")
        if re.search(r"\bStatusCode\s*\(\s*(?!StatusCodes\b|HttpStatusCode\b|\d)[A-Za-z_][\w.]*\s*[,)]", a.m.body_text()):
            print("    note: the action answers StatusCode(<variable>, ...) - a status chosen by a callee is not seen here")
        for c in hidden:
            print(f"    {c} declared, every producer found is caught: \"{a.declared[c][:110]}\"")
            for e in without[c][:4]:
                print(f"        {e.exc or e.kind}  {e.where()}")
    print(f"\n{len(targets)} action(s) audited, {flagged} with a declared code that only a caught path produces. "
          f"A lead, not a verdict: calls through locals, chained expressions and extension methods are not "
          f"followed, nor services more than {Analyzer.SERVICE_HOPS} calls deep, and a producer that throws no "
          f"`throw new` of its own (a BCL call) is not seen.")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--batch", type=int, default=5)
    ap.add_argument("--all", action="store_true")
    ap.add_argument("--endpoint", nargs="+")
    ap.add_argument("--reject", nargs=2, metavar=("ID", "CODE"))
    ap.add_argument("--reason")
    ap.add_argument("--unreject", nargs="+", metavar="ID [CODE]")
    ap.add_argument("--rejected", action="store_true")
    ap.add_argument("--prune-orphaned", action="store_true")
    ap.add_argument("--snapshot", nargs="+")
    ap.add_argument("--diff-check", nargs="+")
    ap.add_argument("--verify-doc", nargs="+")
    ap.add_argument("--audit-catches", nargs="*", metavar="ID|FILE")
    ap.add_argument("--no-codegraph", action="store_true")
    args = ap.parse_args()

    if args.snapshot:
        return cmd_snapshot(args.snapshot)
    if args.diff_check:
        return cmd_diff_check(args.diff_check)

    global USE_CODEGRAPH
    USE_CODEGRAPH = not args.no_codegraph
    ix = Index()
    actions = collect_actions(ix)
    hosts = analyse(ix, actions)
    rejected = load_rejected()
    valid, expired, state = apply_rejections(actions, rejected)

    if args.verify_doc:
        return cmd_verify_doc(actions, args.verify_doc)

    if args.audit_catches is not None:
        return cmd_audit_catches(ix, actions, args.audit_catches)

    if args.reject:
        if not args.reason:
            sys.exit("--reject needs --reason")
        ref, code = args.reject[0], int(args.reject[1])
        hits = resolve(actions, ref)
        if len(hits) != 1:
            sys.exit(f"{len(hits)} actions match {ref}; pass the exact id")
        a = hits[0]
        if code == SERVER_ERROR:
            sys.exit("500 is never in the gap: report it to the user, do not reject it")
        if code not in gaps(a):
            sys.exit(f"{code} is not in the gap of {a.id} (gap: {gaps(a)})")
        rejected = [r for r in rejected if not (r["id"] == a.id and r["code"] == code)]
        rejected.append({"id": a.id, "code": code, "reason": args.reason, "fingerprint": fingerprint(a, code),
                         "date": date.today().isoformat()})
        save_rejected(rejected)
        print(f"rejected {code} on {a.id}")
        return

    if args.unreject:
        ref = args.unreject[0]
        code = int(args.unreject[1]) if len(args.unreject) > 1 else None
        keep = [r for r in rejected if not (r["id"] == ref and (code is None or r["code"] == code))]
        save_rejected(keep)
        print(f"removed {len(rejected) - len(keep)} rejection(s)")
        return

    if args.rejected:
        for r, s in state:
            print(f"{s:9} {r['id']} {r['code']}  {r['date']}  {r['reason']}")
        print(f"{len(state)} rejection(s)")
        return

    if args.prune_orphaned:
        keep = [r for r, s in state if s != "orphaned"]
        save_rejected(keep)
        print(f"removed {len(state) - len(keep)} orphaned rejection(s)")
        return

    dirty = dirty_files()
    print_hosts(hosts)
    print_foreign_hosts(ix, actions)
    graph = (f"used for hints ({sum(len(v) for v in ix.graph.edges.values())} call edges)" if ix.graph else
             "off (--no-codegraph)" if not USE_CODEGRAPH else "no index at .codegraph/codegraph.db")
    print(f"Calls followed: the action, its helpers and Demand*, services up to {Analyzer.SERVICE_HOPS} calls deep "
          f"by declared type; CodeGraph: {graph}")

    if args.endpoint:
        hits = [a for r in args.endpoint for a in resolve(actions, r)]
        if not hits:
            sys.exit("no action matches")
        for a in hits:
            print()
            print_action(a, valid, expired, dirty, detail=True)
        return

    queue = [a for a in actions if any((a.id, c) not in valid for c in gaps(a))]
    n_orphaned = sum(1 for _, s in state if s == "orphaned")
    print(f"Scope: {len(actions)} documented actions in {len({a.doc for a in actions})} documents "
          f"(public: {', '.join(public_docs())})")
    print(f"Queue: {len(queue)} actions with an undeclared code; rejections valid {len(valid)}, "
          f"expired by a code change {len(expired)}, orphaned {n_orphaned}")
    depth = {}
    for a in queue:
        for c in gaps(a):
            if (a.id, c) not in valid:
                h = min(e.hops for e in a.evs if e.code == c and e.strong)
                depth[h] = depth.get(h, 0) + 1
    print("Gap codes by where their nearest evidence is: " +
          ", ".join(f"{'own code' if h == 0 else f'service hop {h}'} {n}" for h, n in sorted(depth.items())))
    errors = [a for a in actions if server_errors(a)]
    # one throw site reached from many actions (tenant resolution, a lock provider) is one finding, not many
    sites = {}
    for a in errors:
        for e in server_errors(a):
            sites.setdefault((e.where(), e.exc or e.kind), []).append(a)
    print(f"Reachable 500: {len(errors)} actions, {len(sites)} throw sites - not documented, not fixed, reported to "
          f"the user (--all lists them)")
    if args.all:
        for a in queue:
            open_codes = [c for c in gaps(a) if (a.id, c) not in valid]
            print(f"  {a.id}  {a.verb}  gap {' '.join(map(str, open_codes))}  declared "
                  f"{' '.join(map(str, sorted(a.declared)))}{'  [dirty]' if a.m.f.rel in dirty else ''}")
        if errors:
            print("Reachable 500, by throw site:")
        for (where, exc), lst in sorted(sites.items(), key=lambda kv: (-len(kv[1]), kv[0])):
            names = [f"{a.id}{' [dirty]' if a.m.f.rel in dirty else ''}" for a in lst]
            more = f" and {len(names) - 5} more" if len(names) > 5 else ""
            print(f"  {where}  {exc}  {len(lst)} action(s): {', '.join(names[:5])}{more}")
        return
    if args.batch <= 0:
        return
    print(f"\nNext batch ({min(args.batch, len(queue))}):")
    for a in queue[:args.batch]:
        print()
        print_action(a, valid, expired, dirty, detail=False)


if __name__ == "__main__":
    main()
