#!/usr/bin/env bash
# Report what the TypeScript API suite covers and the .NET integration suites do not.
#
# Both sides are compared on identifiers that survive the rewrite, over the whole tree, with no
# baseline commit: bug numbers, and the SDK operation names both suites are generated from. A
# git-range delta of test titles cannot do this job — it only sees what changed between two
# commits, so anything never ported before the baseline stays invisible on every run.
#
# Usage:  .claude/scripts/ts-sync-gaps.sh [ts-area ...]     (default: files folders rooms)
# Run from the repo root (server/). The TypeScript suite is expected at ../tests/api-tests.

set -euo pipefail

ts_root="../tests/api-tests/src/tests"
areas=("${@:-}")
[ -z "${areas[0]:-}" ] && areas=(files folders rooms)

[ -d "$ts_root" ] || { echo "TypeScript suite not found at $ts_root" >&2; exit 1; }

tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT

cd "$ts_root"

# --- 1. Bug numbers -----------------------------------------------------------------------------
# Every .NET suite is searched, not just Files: a room bug may be covered in Web.Api or People.
grep -rhoE "BUG [0-9]{5}" "${areas[@]}" | grep -oE "[0-9]{5}" | sort -u > "$tmp/ts_bugs"
cd - >/dev/null
grep -rhoE 'Trait\("Bug", "[0-9]{5}"' --include=*.cs products web common \
  | grep -oE "[0-9]{5}" | sort -u > "$tmp/net_bugs"

echo "== Bug numbers in TypeScript with no [Trait(\"Bug\", ...)] anywhere in the .NET suites =="
comm -23 "$tmp/ts_bugs" "$tmp/net_bugs" | while read -r bug; do
    printf '  %s  %s\n' "$bug" "$(cd "$ts_root" && grep -rl "BUG $bug" "${areas[@]}" | paste -sd' ' -)"
done
echo

# --- 2. SDK operation names ---------------------------------------------------------------------
# Both SDKs are generated from the same OpenAPI document, so TS `getFolderHistory` and .NET
# `GetFolderHistoryAsync` are the same endpoint. This pass is what finds whole uncovered endpoint
# families; the bug pass cannot, because a family may carry only a couple of bug-tagged tests.
# Anchored on the TS SDK's own area segments (`ownerApi.rooms.getRoomsFolder(...)`). Without that
# anchor the pattern also swallows assertion and helper calls — toBe, toContain, skip, delete.
ts_areas='rooms|files|folders|operations|filesSettings|sharing|thirdPartyIntegration|roomQuota|groups|privacyroom|profiles|userStatus|userType|settingsQuota'
(cd "$ts_root" && grep -rhoE "\.($ts_areas)\.[a-zA-Z0-9_]+\(" "${areas[@]}") \
  | sed -E 's/.*\.([a-zA-Z0-9_]+)\($/\1/' | sort -u > "$tmp/ts_ops"

grep -rhoE "[A-Za-z0-9_]+Async\(" --include=*.cs \
     products/ASC.Files/Tests web/ASC.Web.Api.Tests products/ASC.People/Tests \
  | sed 's/Async(//' | sort -u > "$tmp/net_ops"

echo "== TypeScript SDK operations with no matching *Async in the .NET suites =="
echo "   (expect false positives where .NET wraps the call in a differently named helper —"
echo "    check each one before calling it a gap)"
while read -r op; do
    pascal="$(printf '%s' "${op:0:1}" | tr '[:lower:]' '[:upper:]')${op:1}"
    grep -qix "$pascal" "$tmp/net_ops" || {
        n=$(cd "$ts_root" && grep -rc "$op" "${areas[@]}" -r | grep -v ':0$' | paste -sd' ' -)
        printf '  %-34s %s\n' "$op" "$n"
    }
done < "$tmp/ts_ops"
