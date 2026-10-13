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
  1. DEPENDENCY GROUP - the target-framework group read for net10.0, old -> new;
  2. LICENSE CHANGED  - packages whose license expression / file / url differs between the versions;
  3. the new license of every changed package, grouped;
  4. NEW DEPENDENCIES - dependency ids the new nuspec has and the old one did not, with their license;
  5. CHANGED TRANSITIVE - dependencies (not pinned by us) whose version moved, with their license.
     The version shown is the MINIMUM the nuspec asks for, and that is what NuGet resolves when nothing
     else asks for more - not the newest release of that dependency. A MAJOR move here is an update of
     its own: read its release notes too.
A license of type "file" or a bare licenseUrl cannot be judged from the id: open it and read it.

Dependencies are read from the ONE target-framework group NuGet picks for net10.0: an exact net10.0
group, else the highest netX.Y / netcoreappX.Y below it, else the highest netstandard up to 2.1, else
the group without a targetFramework. "[-]" means the nuspec has no group a net10.0 project can use.
A nuspec that cannot be fetched or parsed is listed under ERRORS and left out of the dependency
comparison instead of stopping the report.
"""

import collections
import concurrent.futures as cf
import json
import re
import subprocess
import sys
import urllib.request
import xml.etree.ElementTree as ET

sys.stdout.reconfigure(encoding="utf-8")

PROPS = "Directory.Packages.props"
APPHOST = "common/ASC.AppHost/ASC.AppHost.csproj"
TARGET = (10, 0)  # net10.0, the global TargetFramework of Directory.Packages.props


def pins(text_props, text_apphost):
    found = dict(re.findall(r'<PackageVersion\s+Include="([^"]+)"\s+Version="([^"]+)"', text_props))
    sdk = re.search(r'Sdk="Aspire\.AppHost\.Sdk/([^"]+)"', text_apphost or "")
    if sdk:
        found["Aspire.AppHost.Sdk"] = sdk.group(1)
    return found


def at_ref(ref, path):
    r = subprocess.run(["git", "show", f"{ref}:{path}"], capture_output=True, text=True, encoding="utf-8")
    return r.stdout if r.returncode == 0 else ""


def tfm_rank(tfm):
    """Sort key of a nuspec group targetFramework for a net10.0 consumer; None = not usable."""
    t = (tfm or "").strip().lower()
    if not t:
        return (0, 0, 0)  # group without targetFramework: last resort
    if "," in t or "-" in t:
        return None  # multi-framework legacy groups, platform-specific groups (net10.0-windows)
    m = re.fullmatch(r"\.?netstandard(\d+)\.(\d+)", t)
    if m:
        v = (int(m.group(1)), int(m.group(2)))
        return (1,) + v if v <= (2, 1) else None
    m = re.fullmatch(r"\.?(netcoreapp|net)(\d+)\.(\d+)", t)
    if m:
        v = (int(m.group(2)), int(m.group(3)))
        if m.group(1) == "net" and v[0] < 5:
            return None  # .NET Framework written with a dot (net4.8)
        if v == TARGET:
            return (3,) + v
        return (2,) + v if v < TARGET else None
    return None  # .NETFramework*, net462, portable-*, ...


def nuspec(package, version):
    """(license_kind, license_value, {dep_id: dep_version}, chosen_tfm) or ('ERR', message, {}, '')."""
    url = f"https://api.nuget.org/v3-flatcontainer/{package.lower()}/{version.lower()}/{package.lower()}.nuspec"
    try:
        with urllib.request.urlopen(url, timeout=30) as r:
            text = r.read().decode("utf-8-sig")
        root = ET.fromstring(re.sub(r'\sxmlns="[^"]+"', "", text, count=1))
        meta = root.find("metadata")
        deps, tfm, items = {}, "-", []
        block = meta.find("dependencies")
        if block is not None:
            groups = block.findall("group")
            if groups:
                ranked = [(tfm_rank(g.get("targetFramework")), g) for g in groups]
                ranked = [(k, g) for k, g in ranked if k is not None]
                if ranked:
                    group = max(ranked, key=lambda x: x[0])[1]
                    tfm = group.get("targetFramework") or "any"
                    items = group.findall("dependency")
            else:
                tfm, items = "any", block.findall("dependency")
        for d in items:
            deps[d.get("id")] = re.sub(r"[\[\]() ]", "", d.get("version") or "").split(",")[0]
        lic = meta.find("license")
        if lic is not None:
            return lic.get("type"), (lic.text or "").strip(), deps, tfm
        return "url", (meta.findtext("licenseUrl") or "").strip(), deps, tfm
    except Exception as e:  # noqa: BLE001 - one bad package must not kill the report
        return "ERR", f"{type(e).__name__}: {str(e)[:100]}", {}, ""


def main(argv):
    if argv[:1] == ["--plan"]:
        if len(argv) < 2:
            print("--plan needs the path of the JSON written by outdated.py --json")
            return 2
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
    errors = [f"{p} {changed[p][0] if side == 'old' else changed[p][1]}: {r[1]}"
              for (p, side), r in res.items() if r[0] == "ERR"]
    broken = {p for (p, side), r in res.items() if r[0] == "ERR"}

    print(f"{len(changed)} changed pins against {ref}\n")
    print("== DEPENDENCY GROUP read for net10.0 (old -> new)")
    for p, (o, n) in sorted(changed.items()):
        if p in broken:
            continue
        old_tfm = res[(p, "old")][3] if o else "(new package)"
        print(f"  {p}: [{old_tfm}] -> [{res[(p, 'new')][3]}]")

    print("\n== LICENSE CHANGED")
    any_changed = False
    for p, (o, n) in sorted(changed.items()):
        if p in broken:
            continue
        nk, nv = res[(p, "new")][:2]
        if o is None:
            print(f"  {p} (new package) {n}: {nk}={nv}")
            any_changed = True
            continue
        ok, ov = res[(p, "old")][:2]
        if (ok, ov) != (nk, nv):
            print(f"  {p} {o} -> {n}: {ok}={ov}  =>  {nk}={nv}")
            any_changed = True
    if not any_changed:
        print("  none")

    print("\n== License of every changed package")
    groups = collections.defaultdict(list)
    for p, (o, n) in sorted(changed.items()):
        k, v = res[(p, "new")][:2]
        groups[(k, v)].append(f"{p} {o}->{n}")
    for (k, v), items in sorted(groups.items(), key=lambda x: -len(x[1])):
        print(f"  {k}={v} ({len(items)}): " + ", ".join(items))

    new_deps, moved = {}, {}
    for p, (o, n) in changed.items():
        if p in broken:
            continue  # a missing side would turn every dependency into a "new" one
        nd, ntfm = res[(p, "new")][2], res[(p, "new")][3]
        od = res[(p, "old")][2] if o else {}
        for d, v in nd.items():
            if d in new:
                continue  # pinned by us, already covered above
            if d not in od:
                new_deps[(d, v)] = (p, ntfm)
            elif od[d] != v and v:
                moved[(d, v)] = (p, ntfm, od[d])
    with cf.ThreadPoolExecutor(16) as ex:
        keys = list(new_deps) + list(moved)
        dep_lic = dict(zip(keys, ex.map(lambda k: nuspec(k[0], k[1]), keys)))

    print("\n== NEW DEPENDENCIES")
    for (d, v), (p, tfm) in sorted(new_deps.items()):
        k, lv = dep_lic[(d, v)][:2]
        print(f"  {d} {v} (via {p} [{tfm}]): {k}={lv}")
    if not new_deps:
        print("  none")

    print("\n== CHANGED TRANSITIVE")
    for (d, v), (p, tfm, ov) in sorted(moved.items()):
        k, lv = dep_lic[(d, v)][:2]
        print(f"  {d} {ov} -> {v} (via {p} [{tfm}]): {k}={lv}")
    if not moved:
        print("  none")

    # Our other pins that depend on a package being moved, built against an older major of it: NuGet
    # accepts them (open-ended ranges), binary compatibility is not guaranteed.
    others = {p: v for p, v in new.items() if p not in changed}
    with cf.ThreadPoolExecutor(16) as ex:
        other_res = dict(zip(others, ex.map(lambda p: nuspec(p, others[p]), others)))

    def major(v):
        return v.split(".")[0] if v else ""

    print("\n== OTHER PINS BUILT AGAINST AN OLDER MAJOR OF A MOVED PACKAGE")
    hits = 0
    for p, r in sorted(other_res.items()):
        if r[0] == "ERR":
            errors.append(f"{p} {others[p]} (other pin, older-major check skipped; not on nuget.org = local/legacy feed): {r[1]}")
            continue
        for d, v in r[2].items():
            if d in changed and major(v) != major(changed[d][1]):
                print(f"  {p} {others[p]} [{r[3]}] depends on {d} >= {v} (moving to {changed[d][1]}): "
                      f"check it works, look for a newer {p}")
                hits += 1
    if not hits:
        print("  none")

    errors += [f"{d} {v} (dependency): {r[1]}" for (d, v), r in dep_lic.items() if r[0] == "ERR"]
    if errors:
        print("\n== ERRORS (nuspec not fetched or not parsed - check these by hand)")
        for e in sorted(errors):
            print(f"  {e}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
