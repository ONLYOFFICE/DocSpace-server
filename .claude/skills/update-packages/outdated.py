"""List the centrally pinned NuGet packages that have a newer version on nuget.org.

Usage (from the repo root):
  python -I .claude/skills/update-packages/outdated.py [PATTERN ...] [--all] [--json OUT]

PATTERN is a case-insensitive glob matched against the package id ("OpenTelemetry*", "Refit",
"*FusionCache*"). Without patterns every package in Directory.Packages.props is checked. The
Aspire.AppHost.Sdk version in common/ASC.AppHost/ASC.AppHost.csproj is checked too, because it is
pinned there and not in the props file.

The target version follows the line the pin is on: a stable pin moves to the newest stable release,
a prerelease pin (the OpenTelemetry beta/rc instrumentations, InfluxDB alpha) moves to the newest
release of any kind, so it is not stranded on an old beta. A newer prerelease of a stable pin is
listed only as a note. --all also prints the packages that are already current.

--json writes {id: {"current": ..., "target": ..., "bump": ...}} for the other scripts.
"""

import concurrent.futures
import fnmatch
import json
import re
import sys
import urllib.request

sys.stdout.reconfigure(encoding="utf-8")

PROPS = "Directory.Packages.props"
APPHOST = "common/ASC.AppHost/ASC.AppHost.csproj"


def parse_version(text):
    """Split '1.19.0-beta.1' into a sortable key: numeric core, then prerelease (stable sorts last)."""
    core, _, pre = text.partition("-")
    nums = tuple(int(p) if p.isdigit() else 0 for p in core.split("."))
    nums = nums + (0,) * (4 - len(nums))
    if not pre:
        return nums, (1,)
    parts = tuple((0, int(p), "") if p.isdigit() else (1, 0, p) for p in re.split(r"[.]", pre))
    return nums, (0,) + parts


def is_pre(text):
    return "-" in text


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


def versions(package):
    url = f"https://api.nuget.org/v3-flatcontainer/{package.lower()}/index.json"
    try:
        with urllib.request.urlopen(url, timeout=30) as r:
            return json.load(r)["versions"]
    except Exception as e:  # noqa: BLE001 - a missing package must not stop the scan
        return e


def main(argv):
    out_json = None
    if "--json" in argv:
        i = argv.index("--json")
        out_json = argv[i + 1]
        del argv[i:i + 2]
    show_all = "--all" in argv
    patterns = [a for a in argv if not a.startswith("--")]

    pinned = pins()
    selected = {p: v for p, v in pinned.items()
                if not patterns or any(fnmatch.fnmatch(p.lower(), pat.lower()) for pat in patterns)}
    if not selected:
        print("No package matches", patterns)
        return 1

    with concurrent.futures.ThreadPoolExecutor(16) as ex:
        found = dict(zip(selected, ex.map(versions, selected)))

    rows, current_rows, errors, result = [], [], [], {}
    for package, current in sorted(selected.items()):
        all_versions = found[package]
        if isinstance(all_versions, Exception):
            errors.append(f"{package}: {all_versions}")
            continue
        newer = [v for v in all_versions if parse_version(v) > parse_version(current)]
        stable = [v for v in newer if not is_pre(v)]
        candidates = newer if is_pre(current) else stable
        target = max(candidates, key=parse_version) if candidates else None
        note = ""
        if not is_pre(current):
            pre_newer = [v for v in newer if is_pre(v) and (target is None or parse_version(v) > parse_version(target))]
            if pre_newer:
                note = f"prerelease {max(pre_newer, key=parse_version)} exists"
        if target:
            kind = bump_kind(current, target)
            rows.append((package, current, target, kind, note))
            result[package] = {"current": current, "target": target, "bump": kind}
        else:
            current_rows.append((package, current, note))

    if rows:
        print("| Package | Current | Target | Bump | Note |")
        print("|---|---|---|---|---|")
        for package, current, target, kind, note in rows:
            print(f"| {package} | {current} | {target} | {kind} | {note} |")
    else:
        print("Everything selected is current.")
    if show_all and current_rows:
        print("\nAlready current:")
        for package, current, note in current_rows:
            print(f"  {package} {current}" + (f"  ({note})" if note else ""))
    for e in errors:
        print("ERROR", e)
    if out_json:
        with open(out_json, "w", encoding="utf-8") as f:
            json.dump(result, f, indent=1)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
