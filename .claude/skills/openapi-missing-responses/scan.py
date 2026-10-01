#!/usr/bin/env python3
"""Queue of API actions whose own code produces a status code that no [SwaggerResponse] declares.

The queue keeps no state: it is the set of actions whose evidence codes minus declared codes is not empty,
computed from the sources on every run, so an action added or changed since the last run is in it automatically.
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

The exception -> status table and the global MVC filters are read, per host project, from the startup class chain
that registers them (AddExceptionHandler<T>, Filters.Add), so a service with its own handler gets its own table.
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

def blank(src):
    """Same-length copy of a C# source with comments and string/char literal contents replaced by spaces."""
    out = list(src)
    i, n = 0, len(src)

    def wipe(a, b):
        for k in range(a, b):
            if out[k] not in "\r\n":
                out[k] = " "

    while i < n:
        c = src[i]
        if c == "/" and src.startswith("//", i):
            j = src.find("\n", i)
            j = n if j < 0 else j
            wipe(i, j)
            i = j
        elif c == "/" and src.startswith("/*", i):
            j = src.find("*/", i + 2)
            j = n if j < 0 else j + 2
            wipe(i, j)
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
    params = [p for p in split_top(method.params) if p.strip()]
    if any(re.match(r"\s*(?:\[[^\]]*\]\s*)*params\b", p) for p in params):
        return len(params) - 1, 10 ** 6
    required = sum(1 for p in params if "=" not in p and not re.match(r"\s*this\b", p))
    return required, len(params)


def norm(text):
    return re.sub(r"\s+", " ", text).strip()


# ---------------------------------------------------------------- source model

class SourceFile:
    def __init__(self, path):
        self.path = path
        self.rel = path.relative_to(ROOT).as_posix()
        self.src = path.read_text(encoding="utf-8-sig", errors="replace")
        self.bl = blank(self.src)
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
    def __init__(self, f, cls, name, decl_start, sig_pos, params, body, attrs):
        self.f, self.cls, self.name = f, cls, name
        self.decl_start, self.sig_pos, self.params, self.body, self.attrs = decl_start, sig_pos, params, body, attrs

    @property
    def key(self):
        return f"{self.f.rel}::{self.cls.name}.{self.name}@{self.f.line(self.sig_pos)}"

    def body_text(self):
        return self.f.bl[self.body[0]:self.body[1] + 1] if self.body else ""


TYPE_RE = re.compile(r"\b(class|record|struct|interface|enum)\s+(?:class\s+|struct\s+)?([A-Za-z_]\w*)")
METHOD_RE = re.compile(r"\b([A-Za-z_]\w*)\s*(<[^(){};=]*>)?\s*\(")


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
        attrs = f.src[attr_block_start(bl, m.start()):m.start()]
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
        if not re.match(MODIFIERS + r"[^=;(){}]*$", prefix_noattr, re.S):
            continue
        if re.search(r"\b(class|record|struct|interface|enum|delegate|event|operator)\b", prefix_noattr):
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
        params = f.src[m.end():close]
        attrs = f.src[stmt:m.start()]
        owner.methods.append(Method(f, owner, name, stmt, m.start(), params, body, attrs))


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


def status_of(name):
    return http.HTTPStatus[re.sub(r"(?<!^)(?=[A-Z])", "_", name).upper()].value


def parse_exception_table(handler):
    """([(exception type, code or None for its own status)], the type carrying its own status) of one handler.
    A handler without a `switch (exception)` maps nothing: the middleware answers 500 for every exception."""
    if handler is None or not handler.body:
        return [], None
    src = handler.f.src[handler.body[0]:handler.body[1]]
    m = re.search(r"switch\s*\(\s*exception\s*\)\s*\{", src)
    if not m:
        return [], None
    body = src[m.end():match_close(src, m.end() - 1, "{", "}")]
    table, pending, custom, parsed = [], [], None, 0
    for line in body.splitlines():
        line = line.split("//")[0].strip()
        cm = re.match(r"case\s+([\w.]+)(?:\s+\w+)?\s*:", line)
        if cm:
            pending.append(cm.group(1).split(".")[-1])
            parsed += 1
            continue
        sm = re.match(r"status\s*=\s*HttpStatusCode\.(\w+)\s*;", line)
        if sm and pending:
            code = status_of(sm.group(1))
            table += [(t, code) for t in pending]
            pending = []
            continue
        if re.match(r"status\s*=\s*\(HttpStatusCode\)", line) and pending:
            custom = pending[0]
            table += [(t, None) for t in pending]
            pending = []
            continue
        if line.startswith("break") or line.startswith("default"):
            pending = []
    total = len(re.findall(r"^\s*case\b", body, re.M))
    if parsed != total or len(table) != total:
        sys.exit(f"read {len(table)} of {total} cases of the exception switch in {handler.f.rel}; "
                 f"the handler changed shape — read it by hand")
    return table, custom


# ---------------------------------------------------------------- evidence

class Ev:
    def __init__(self, code, kind, f, pos, exc=None, via=(), strong=True, detail=""):
        self.code, self.kind, self.f, self.pos, self.exc = code, kind, f, pos, exc
        self.via, self.strong, self.detail = tuple(via), strong, detail

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


class Analyzer:
    """Evidence under one exception handler: the same throw is a different status in a host with its own handler."""

    def __init__(self, index, handler):
        self.ix = index
        self.handler = handler
        self.table, self.custom_case = parse_exception_table(handler)
        self.cache = {}

    def exc_code(self, exc_name):
        """(code, known): status this host's exception handler gives this exception type."""
        chain = self.ix.type_chain(exc_name)
        for case_type, code in self.table:
            if case_type in chain:
                return code, True
        known = chain[-1] in ("Exception", "SystemException") or exc_name == "Exception"
        return 500, known

    def method_evidence(self, method, depth=0, stack=()):
        if method.key in self.cache:
            return self.cache[method.key]
        if depth > 5 or method.key in stack or not method.body:
            return []
        stack = stack + (method.key,)
        f, (a, b) = method.f, method.body
        bl = f.bl
        evs = []
        chain = self.ix.class_chain(method.cls)
        chain_methods = {}
        for c in chain:
            for mt in c.methods:
                chain_methods.setdefault(mt.name, []).append(mt)

        def add(ev, exc_for_catch):
            if not swallowed(self.ix, f, method, ev.pos, exc_for_catch):
                evs.append(ev)

        for m in re.finditer(r"\bthrow\s+new\s+([\w.]+)\s*(<[^>]*>)?\s*[({]", bl[a:b]):
            pos = a + m.start()
            exc = m.group(1).split(".")[-1]
            if exc == self.custom_case:
                args = bl[pos:pos + 200]
                cm = re.search(r"HttpStatusCode\.(\w+)|\b([1-5]\d\d)\b", args)
                if cm and cm.group(1):
                    code = status_of(cm.group(1))
                elif cm:
                    code = int(cm.group(2))
                else:
                    add(Ev(500, "throw?", f, pos, exc, strong=False, detail="custom status not readable"), exc)
                    continue
                add(Ev(code, "throw", f, pos, exc), exc)
                continue
            code, known = self.exc_code(exc)
            add(Ev(code, "throw" if known else "throw?", f, pos, exc, strong=known), exc)

        for m in re.finditer(r"\b(\w+Exception)\.ThrowIf\w*\s*\(", bl[a:b]):
            pos = a + m.start()
            code, _ = self.exc_code(m.group(1))
            add(Ev(code, "arg-guard", f, pos, m.group(1), strong=False), m.group(1))

        for m in re.finditer(r"\bTryGetFromCache\s*\(", bl[a:b]):
            evs.append(Ev(304, "not-modified", f, a + m.start()))

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
            evs.append(Ev(code, "result", f, pos))

        for m in re.finditer(r"\bStatusCode\s*=\s*(?:\(int\)\s*)?(?:HttpStatusCode\.(\w+)|StatusCodes\.Status(\d{3})\w*|([1-5]\d\d)\b)", bl[a:b]):
            if m.group(1):
                code = status_of(m.group(1))
            else:
                code = int(m.group(2) or m.group(3))
            if code >= 300:
                evs.append(Ev(code, "status", f, a + m.start()))

        # calls: helpers of the same class chain (any name) and Demand* methods anywhere
        for m in re.finditer(r"(?:(\w+)\s*\.\s*)?\b([A-Z]\w*)\s*(?:<[^(){};=]*>)?\s*\(", bl[a:b]):
            receiver, name = m.group(1), m.group(2)
            pos = a + m.start(2)
            if bl[max(0, a + m.start() - 4):a + m.start()].strip().endswith("new"):
                continue
            targets, kind = [], None
            if (receiver in (None, "this", "base")) and name in chain_methods:
                targets, kind = chain_methods[name], "helper"
            elif name.startswith("Demand"):
                targets = [mt for mt in self.ix.methods.get(name, [])
                           if "private" not in mt.attrs.split("(")[0] and mt.body]
                kind = "demand"
                if not targets:
                    evs.append(Ev(403, "demand?", f, pos, "AuthorizingException", strong=False,
                                  detail=f"{f.line_text(pos)}  [no declaration of {name} found]"))
                    continue
            if not targets:
                continue
            # overloads: keep those whose parameter count fits the call's argument count
            close = match_close(bl, a + m.end() - 1, "(", ")")
            argc = len(split_top(bl[a + m.end():close]))
            fitting = [t for t in targets if arity(t)[0] <= argc <= arity(t)[1]]
            targets = fitting or targets
            for t in targets:
                if t.key == method.key:
                    continue
                for ev in self.method_evidence(t, depth + 1, stack):
                    if ev.kind == "dto":
                        continue
                    if not swallowed(self.ix, f, method, pos, ev.exc):
                        evs.append(Ev(ev.code, ev.kind, ev.f, ev.pos, ev.exc,
                                      via=(f"{name}@{f.rel.split('/')[-1]}:{f.line(pos)}",) + ev.via,
                                      strong=ev.strong, detail=ev.detail))
        # one line per site: overload chains reach the same throw several times; keep the shortest path
        best = {}
        for ev in evs:
            k = (ev.code, ev.kind, ev.f.rel, ev.pos)
            if k not in best or len(ev.via) < len(best[k].via):
                best[k] = ev
        evs = sorted(best.values(), key=lambda e: (len(e.via), e.f.rel, e.pos))
        self.cache[method.key] = evs
        return evs

    # ---- DTO binding -> 400
    def props(self, t):
        if t.props is not None:
            return t.props
        out = []
        if t.body:
            bl, src = t.f.bl, t.f.src
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

    def is_enum(self, type_text):
        return any(d.kind == "enum" for d in self.ix.types.get(simple_name(type_text), []))

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
                if self.is_enum(it) and "JsonConverter" not in attrs:
                    evs.append(Ev(400, "enum-body", c.f, pos, strong=False,
                                  detail=f"enum {pname} sent as a string  [{' > '.join(via)}]"))
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
            if "FromBody" in attrs:
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
    return hosts


def print_hosts(hosts):
    """What the pipeline of each host does, read from its startup chain on this run."""
    for key, (an, filters, docs) in sorted(hosts.items(), key=lambda kv: (kv[0] is None, kv[0] or "")):
        names = ", ".join(sorted(docs))
        if an.table:
            table = " | ".join(f"{t} {c if c else 'own status'}" for t, c in an.table) + " | anything else 500"
        else:
            table = "no mapping, every exception is 500"
        print(f"[{names}] exception -> status ({key or 'no exception handler registered'}, first match wins): {table}")
        print(f"[{names}] global filters: {', '.join(filters) or 'none'}")


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


def gaps(a):
    strong = sorted({e.code for e in a.evs if e.strong})
    return [c for c in strong if c not in a.declared and not (200 <= c < 300)]


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

def fmt_ev(e, mark):
    via = f"  via {' > '.join(e.via)}" if e.via else ""
    exc = f" {e.exc}" if e.exc and e.kind.startswith("throw") else ""
    return f"    {mark} {e.code} {e.kind}{exc}  {e.where()}  {e.text()[:150]}{via}"


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
            print(fmt_ev(e, "+"))
    rej = [c for c in gaps(a) if (a.id, c) in valid]
    if rej:
        print(f"    rejected (still valid): {' '.join(map(str, rej))}")
    if detail:
        for e in [e for e in a.evs if not e.strong and e.code not in a.declared][:10]:
            print(fmt_ev(e, "?"))
        for e in [e for e in a.evs if e.code in a.declared][:12]:
            print(fmt_ev(e, "="))


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
    return "CRLF" if crlf == lf else ("LF" if crlf == 0 else f"MIXED ({lf - crlf} LF-only lines)")


def cmd_snapshot(paths):
    for p in paths:
        rel = rel_path(p)
        data = (ROOT / rel).read_bytes()
        dst = snapshot_path(rel)
        dst.parent.mkdir(parents=True, exist_ok=True)
        dst.write_bytes(data)
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
            old = base.decode("utf-8", errors="replace").splitlines()
            new = data.decode("utf-8", errors="replace").splitlines()
            diff = [l for l in difflib.unified_diff(old, new, n=0, lineterm="") if not l.startswith(("+++", "---", "@@"))]
            eol_base = eol_of(base)
        else:
            base = subprocess.run(["git", "show", f"HEAD:{rel}"], cwd=ROOT, capture_output=True).stdout
            base_name = "HEAD"
            out = subprocess.run(["git", "diff", "-U0", "--", rel], cwd=ROOT, capture_output=True, text=True,
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
        route = re.sub(r"\{(\w+)[:?][^}]*\}", r"{\1}", a.route.strip("/~"))
        ops = []
        for pth, item in doc.get("paths", {}).items():
            o = item.get(a.verb.lower())
            if o and o.get("operationId") == op_id and (not route or pth.rstrip("/").endswith("/" + route)
                                                        or pth.rstrip("/").endswith(route)):
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
    args = ap.parse_args()

    if args.snapshot:
        return cmd_snapshot(args.snapshot)
    if args.diff_check:
        return cmd_diff_check(args.diff_check)

    ix = Index()
    actions = collect_actions(ix)
    hosts = analyse(ix, actions)
    rejected = load_rejected()
    valid, expired, state = apply_rejections(actions, rejected)

    if args.verify_doc:
        return cmd_verify_doc(actions, args.verify_doc)

    if args.reject:
        if not args.reason:
            sys.exit("--reject needs --reason")
        ref, code = args.reject[0], int(args.reject[1])
        hits = resolve(actions, ref)
        if len(hits) != 1:
            sys.exit(f"{len(hits)} actions match {ref}; pass the exact id")
        a = hits[0]
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
    if args.all:
        for a in queue:
            open_codes = [c for c in gaps(a) if (a.id, c) not in valid]
            print(f"  {a.id}  {a.verb}  gap {' '.join(map(str, open_codes))}  declared "
                  f"{' '.join(map(str, sorted(a.declared)))}{'  [dirty]' if a.m.f.rel in dirty else ''}")
        return
    if args.batch <= 0:
        return
    print(f"\nNext batch ({min(args.batch, len(queue))}):")
    for a in queue[:args.batch]:
        print()
        print_action(a, valid, expired, dirty, detail=False)


if __name__ == "__main__":
    main()
