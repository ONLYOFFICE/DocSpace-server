"""Compare the licenses and dependencies of package versions: planned ones, or pins changed against a git ref.

Usage (from the repo root):
  python -I .claude/skills/update-packages/licenses.py --plan PLAN.json   # phase 1: nothing edited yet
  python -I .claude/skills/update-packages/licenses.py [BASE_REF]          # phase 2: pins already bumped

--plan reads the file written by `outdated.py ... --json PLAN.json` ({id: {"current", "target"}}) and
compares current -> target without touching Directory.Packages.props.

BASE_REF defaults to HEAD (= the uncommitted bump). For a branch that already carries committed bumps,
pass the commit it started from (`git merge-base HEAD origin/develop`, or the hash before the first
bump). Old pins come from `git show BASE_REF:<file>`, new pins from the working tree, both for
Directory.Packages.props and for the Aspire.AppHost.Sdk version in the AppHost csproj.

For every changed package the nuspec of the old and the new version is read from nuget.org and the
script prints:
  1. LICENSE CHANGED  - packages whose license expression / file / url differs between the versions;
  2. the new license of every changed package, grouped;
  3. NEW DEPENDENCIES - dependency ids the new nuspec has and the old one did not, with their license;
  4. CHANGED TRANSITIVE - dependencies (not pinned by us) whose version moved, with their license.
     The version shown is the MINIMUM the nuspec asks for, and that is what NuGet resolves when nothing
     else asks for more - not the newest release of that dependency. A MAJOR move here is an update of
     its own: read its release notes too.
A license of type "file" or a bare licenseUrl cannot be judged from the id: open it and read it.
Dependencies are collected over every target-framework group of the nuspec, so a .NET Framework-only
group shows up although net10 never resolves it.
"""

import json

import collections
import concurrent.futures as cf
import re
import subprocess
import sys
import urllib.request
import xml.etree.ElementTree as ET

sys.stdout.reconfigure(encoding="utf-8")

PROPS = "Directory.Packages.props"
APPHOST = "common/ASC.AppHost/ASC.AppHost.csproj"


def pins(text_props, text_apphost):
    found = dict(re.findall(r'<PackageVersion\s+Include="([^"]+)"\s+Version="([^"]+)"', text_props))
    sdk = re.search(r'Sdk="Aspire\.AppHost\.Sdk/([^"]+)"', text_apphost or "")
    if sdk:
        found["Aspire.AppHost.Sdk"] = sdk.group(1)
    return found


def at_ref(ref, path):
    r = subprocess.run(["git", "show", f"{ref}:{path}"], capture_output=True, text=True, encoding="utf-8")
    return r.stdout if r.returncode == 0 else ""


def nuspec(package, version):
    """(license_kind, license_value, {dep_id: dep_version}) or ('ERR', message, {})."""
    url = f"https://api.nuget.org/v3-flatcontainer/{package.lower()}/{version.lower()}/{package.lower()}.nuspec"
    try:
        with urllib.request.urlopen(url, timeout=30) as r:
            text = r.read().decode("utf-8-sig")
    except Exception as e:  # noqa: BLE001
        return "ERR", str(e)[:60], {}
    root = ET.fromstring(re.sub(r'\sxmlns="[^"]+"', "", text, count=1))
    meta = root.find("metadata")
    lic = meta.find("license")
    deps = {}
    for d in root.iter("dependency"):
        deps.setdefault(d.get("id"), re.sub(r"[\[\]() ]", "", d.get("version") or "").split(",")[0])
    if lic is not None:
        return lic.get("type"), (lic.text or "").strip(), deps
    return "url", (meta.findtext("licenseUrl") or "").strip(), deps


def main(argv):
    if argv[:1] == ["--plan"]:
        with open(argv[1], encoding="utf-8") as f:
            plan = json.load(f)
        ref = f"plan {argv[1]}"
        new = pins(open(PROPS, encoding="utf-8-sig").read(), open(APPHOST, encoding="utf-8-sig").read())
        changed = {p: (v["current"], v["target"]) for p, v in plan.items()}
    else:
        ref = argv[0] if argv else "HEAD"
        old = pins(at_ref(ref, PROPS), at_ref(ref, APPHOST))
        if not old:
            print(f"Cannot read {PROPS} at {ref}")
            return 1
        new = pins(open(PROPS, encoding="utf-8-sig").read(), open(APPHOST, encoding="utf-8-sig").read())
        changed = {p: (old.get(p), v) for p, v in new.items() if old.get(p) != v}
    if not changed:
        print(f"No package pin differs from {ref}.")
        return 0

    with cf.ThreadPoolExecutor(16) as ex:
        jobs = {}
        for p, (o, n) in changed.items():
            jobs[(p, "new")] = ex.submit(nuspec, p, n)
            if o:
                jobs[(p, "old")] = ex.submit(nuspec, p, o)
        res = {k: f.result() for k, f in jobs.items()}

    print(f"{len(changed)} changed pins against {ref}\n")
    print("== LICENSE CHANGED")
    any_changed = False
    for p, (o, n) in sorted(changed.items()):
        nk, nv, _ = res[(p, "new")]
        if o is None:
            print(f"  {p} (new package) {n}: {nk}={nv}")
            any_changed = True
            continue
        ok, ov, _ = res[(p, "old")]
        if (ok, ov) != (nk, nv):
            print(f"  {p} {o} -> {n}: {ok}={ov}  =>  {nk}={nv}")
            any_changed = True
    if not any_changed:
        print("  none")

    print("\n== License of every changed package")
    groups = collections.defaultdict(list)
    for p, (o, n) in sorted(changed.items()):
        k, v, _ = res[(p, "new")]
        groups[(k, v)].append(f"{p} {o}->{n}")
    for (k, v), items in sorted(groups.items(), key=lambda x: -len(x[1])):
        print(f"  {k}={v} ({len(items)}): " + ", ".join(items))

    new_deps, moved = {}, {}
    for p, (o, n) in changed.items():
        nd = res[(p, "new")][2]
        od = res[(p, "old")][2] if o else {}
        for d, v in nd.items():
            if d in new:
                continue  # pinned by us, already covered above
            if d not in od:
                new_deps[(d, v)] = p
            elif od[d] != v and v:
                moved[(d, v)] = (p, od[d])
    with cf.ThreadPoolExecutor(16) as ex:
        dep_lic = dict(zip(list(new_deps) + list(moved),
                           ex.map(lambda k: nuspec(k[0], k[1]), list(new_deps) + list(moved))))

    print("\n== NEW DEPENDENCIES")
    for (d, v), p in sorted(new_deps.items()):
        k, lv, _ = dep_lic[(d, v)]
        print(f"  {d} {v} (via {p}): {k}={lv}")
    if not new_deps:
        print("  none")

    print("\n== CHANGED TRANSITIVE")
    for (d, v), (p, ov) in sorted(moved.items()):
        k, lv, _ = dep_lic[(d, v)]
        print(f"  {d} {ov} -> {v} (via {p}): {k}={lv}")
    if not moved:
        print("  none")

    # Our other pins that depend on a package being moved, built against an older major of it: NuGet
    # accepts them (open-ended ranges), binary compatibility is not guaranteed.
    others = {p: v for p, v in new.items() if p not in changed}
    with cf.ThreadPoolExecutor(16) as ex:
        other_deps = dict(zip(others, ex.map(lambda p: nuspec(p, others[p])[2], others)))
    major = lambda v: v.split(".")[0] if v else ""
    print("\n== OTHER PINS BUILT AGAINST AN OLDER MAJOR OF A MOVED PACKAGE")
    hits = 0
    for p, deps in sorted(other_deps.items()):
        for d, v in deps.items():
            if d in changed and major(v) != major(changed[d][1]):
                print(f"  {p} {others[p]} depends on {d} >= {v} (moving to {changed[d][1]}): check it works, look for a newer {p}")
                hits += 1
    if not hits:
        print("  none")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
