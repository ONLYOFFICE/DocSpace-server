#!/usr/bin/env bash
# Build the services and the tests, then run the whole test solution one module at a time with TRX reports.
# Usage: bash .claude/skills/openspec-maintain/scripts/run_full.sh <out-dir>
# Writes <out-dir>/commit.txt, build.log, test.log and results/*.trx. Run from the repository root.
set -u
OUT="${1:?usage: run_full.sh <out-dir>}"
mkdir -p "$OUT"
git rev-parse HEAD > "$OUT/commit.txt"
date -u +%Y-%m-%d > "$OUT/date.txt"

# The services first: a test-only build leaves the Aspire services on old product code.
dotnet build ASC.Web.slnx -c Debug -v q -nologo > "$OUT/build.log" 2>&1 || { echo "service build failed, see $OUT/build.log"; exit 2; }
dotnet build ASC.Tests.slnx -c Debug -v q -nologo >> "$OUT/build.log" 2>&1 || { echo "test build failed, see $OUT/build.log"; exit 2; }

dotnet test ASC.Tests.slnx --no-build --max-parallel-test-modules 1 \
    --results-directory "$OUT/results" --report-xunit-trx > "$OUT/test.log" 2>&1
echo "test exit $?"
ls "$OUT/results"/*.trx 2>/dev/null | wc -l
tail -12 "$OUT/test.log" | cut -c1-200
