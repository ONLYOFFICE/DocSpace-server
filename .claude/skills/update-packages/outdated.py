"""List the centrally pinned NuGet packages that have a newer version on nuget.org.

Usage (from the repo root):
  python -I .claude/skills/update-packages/outdated.py [PATTERN ...] [--all] [--json OUT]

PATTERN is a case-insensitive glob matched against the package id ("OpenTelemetry*", "Refit",
"*FusionCache*"). Without patterns every package in Directory.Packages.props is checked. The
Aspire.AppHost.Sdk version in common/ASC.AppHost/ASC.AppHost.csproj is checked too, because it is
pinned there and not in the props file.

Versions come from the NuGet registration API (the RegistrationsBaseUrl of the service index), not
from the flat container: the flat container also lists UNLISTED versions, which a restore never picks
and which must not become a target. Unlisted versions are dropped.

The target version follows the line the pin is on: a stable pin moves to the newest stable release,
a prerelease pin (the OpenTelemetry beta/rc instrumentations, InfluxDB alpha) moves to the newest
release of any kind, so it is not stranded on an old beta. A newer prerelease of a stable pin is
listed only as a note. The note also carries the deprecation (reasons, alternate package) and the
known vulnerabilities (severity, advisory) of the current and of the target version.
--all also prints the packages that are already current.

--json writes {id: {"current", "target", "bump", "note"}} for the other scripts.
"""

import argparse
import concurrent.futures
import fnmatch
import gzip
import json
import re
import sys
import urllib.request

sys.stdout.reconfigure(encoding="utf-8")

PROPS = "Directory.Packages.props"
APPHOST = "common/ASC.AppHost/ASC.AppHost.csproj"
SERVICE_INDEX = "https://api.nuget.org/v3/index.json"
FALLBACK_REGISTRATIONS = "https://api.nuget.org/v3/registration5-gz-semver2/"
SEVERITY = {"0": "low", "1": "moderate", "2": "high", "3": "critical"}


def parse_version(text):
    """Split '1.19.0-beta.1' into a sortable key: numeric core, then prerelease (stable sorts last)."""
    text = text.split("+")[0]
    core, _, pre = text.partition("-")
    nums = tuple(int(p) if p.isdigit() else 0 for p in core.split("."))
    nums = nums + (0,) * (4 - len(nums))
    if not pre:
        return nums, (1,)
    parts = tuple((0, int(p), "") if p.isdigit() else (1, 0, p.lower()) for p in pre.split("."))
    return nums, (0,) + parts


def is_pre(text):
    return "-" in text.split("+")[0]


def bump_kind(current, target):
    a, b = parse_version(current)[0], parse_version(target)[0]
    if a[0] != b[0]:
        return "MAJOR"
    if a[1] != b[1]:
        return "minor"
    if a[:3] != b[:3]:
        return "patch"
    return "revision" if a != b else "prerelease"


def pins():
    text = open(PROPS, encoding="utf-8-sig").read()
    found = dict(re.findall(r'<PackageVersion\s+Include="([^"]+)"\s+Version="([^"]+)"', text))
    try:
        sdk = re.search(r'Sdk="Aspire\.AppHost\.Sdk/([^"]+)"', open(APPHOST, encoding="utf-8-sig").read())
        if sdk:
            found["Aspire.AppHost.Sdk"] = sdk.group(1)
    except FileNotFoundError:
        pass
    return found


def get_json(url):
    req = urllib.request.Request(url, headers={"Accept-Encoding": "gzip", "User-Agent": "docspace-update-packages"})
    with urllib.request.urlopen(req, timeout=30) as r:
        data = r.read()
    if data[:2] == b"\x1f\x8b":
        data = gzip.decompress(data)
    return json.loads(data.decode("utf-8-sig"))


def registrations_base():
    try:
        resources = get_json(SERVICE_INDEX)["resources"]
        by_type = {r["@type"]: r["@id"] for r in resources}
        for t in ("RegistrationsBaseUrl/3.6.0", "RegistrationsBaseUrl/Versioned"):
            if t in by_type:
                return by_type[t]
    except Exception:  # noqa: BLE001 - the well-known URL still works
        pass
    return FALLBACK_REGISTRATIONS


def versions(base, package, current):
    """{version: catalogEntry} of the LISTED versions >= current, or the exception that stopped it."""
    try:
        index = get_json(f"{base.rstrip('/')}/{package.lower()}/index.json")
        floor = parse_version(current)
        found = {}
        for page in index.get("items", []):
            if parse_version(page.get("upper", "0")) < floor:
                continue  # every version in the page is older than the pin
            leaves = page.get("items")
            if leaves is None:  # large packages: the page is only referenced, fetch it
                leaves = get_json(page["@id"]).get("items", [])
            for leaf in leaves:
                entry = leaf.get("catalogEntry", {})
                version = entry.get("version")
                if version and entry.get("listed", True) is not False:
                    found[version] = entry
        return found
    except Exception as e:  # noqa: BLE001 - a missing package must not stop the scan
        return e


def find_entry(entries, version):
    key = parse_version(version)
    return next((e for v, e in entries.items() if parse_version(v) == key), None)


def health(entry, label):
    """'current deprecated (Legacy; use X)' / 'target vulnerable: high GHSA-...' parts for the note."""
    if not entry:
        return []
    out = []
    dep = entry.get("deprecation")
    if dep:
        text = f"{label} deprecated ({', '.join(dep.get('reasons') or []) or 'no reason'}"
        alt = dep.get("alternatePackage")
        if alt:
            text += f"; use {alt.get('id')} {alt.get('range') or ''}".rstrip()
        out.append(text + ")")
    vulns = entry.get("vulnerabilities") or []
    if vulns:
        out.append(f"{label} vulnerable: " + ", ".join(
            f"{SEVERITY.get(str(v.get('severity')), v.get('severity'))} {v.get('advisoryUrl', '')}".strip() for v in vulns))
    return out


def main(argv):
    ap = argparse.ArgumentParser(description="List outdated centrally pinned NuGet packages.")
    ap.add_argument("patterns", nargs="*", metavar="PATTERN", help="case-insensitive glob over package ids")
    ap.add_argument("--all", action="store_true", help="also print packages that are already current")
    ap.add_argument("--json", metavar="OUT", help="write the plan for licenses.py / release_notes.py")
    args = ap.parse_args(argv)

    pinned = pins()
    selected = {p: v for p, v in pinned.items()
                if not args.patterns or any(fnmatch.fnmatch(p.lower(), pat.lower()) for pat in args.patterns)}
    if not selected:
        print("No package matches", args.patterns)
        return 1

    base = registrations_base()
    with concurrent.futures.ThreadPoolExecutor(16) as ex:
        found = dict(zip(selected, ex.map(lambda p: versions(base, p, selected[p]), selected)))

    rows, current_rows, errors, result = [], [], [], {}
    for package, current in sorted(selected.items()):
        entries = found[package]
        if isinstance(entries, Exception):
            errors.append(f"{package}: {entries}")
            continue
        newer = [v for v in entries if parse_version(v) > parse_version(current)]
        stable = [v for v in newer if not is_pre(v)]
        candidates = newer if is_pre(current) else stable
        target = max(candidates, key=parse_version) if candidates else None
        notes = []
        if not is_pre(current):
            pre_newer = [v for v in newer if is_pre(v) and (target is None or parse_version(v) > parse_version(target))]
            if pre_newer:
                notes.append(f"prerelease {max(pre_newer, key=parse_version)} exists")
        current_entry = find_entry(entries, current)
        if current_entry is None:
            notes.append("current version is unlisted or missing")
        notes += health(current_entry, "current")
        if target:
            notes += health(entries[target], "target")
        note = "; ".join(notes)
        if target:
            kind = bump_kind(current, target)
            rows.append((package, current, target, kind, note))
            result[package] = {"current": current, "target": target, "bump": kind, "note": note}
        else:
            current_rows.append((package, current, note))

    if rows:
        print("| Package | Current | Target | Bump | Note |")
        print("|---|---|---|---|---|")
        for package, current, target, kind, note in rows:
            print(f"| {package} | {current} | {target} | {kind} | {note} |")
    else:
        print("Everything selected is current.")
    flagged = [r for r in current_rows if r[2]]
    if (args.all or flagged) and current_rows:
        print("\nAlready current:" if args.all else "\nAlready current, but with a note:")
        for package, current, note in (current_rows if args.all else flagged):
            print(f"  {package} {current}" + (f"  ({note})" if note else ""))
    for e in errors:
        print("ERROR", e)
    if args.json:
        with open(args.json, "w", encoding="utf-8") as f:
            json.dump(result, f, indent=1)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
