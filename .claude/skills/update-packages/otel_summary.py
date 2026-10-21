"""Summarise an OTLP-JSON file export: which services sent telemetry, from which instrumentation scope
and version, and with which resource attributes.

Usage (from the repo root, after a test run with OTEL_FILE_LOGGING=true):
  python -I .claude/skills/update-packages/otel_summary.py [../Logs/otel/traces.log] [../Logs/otel/metrics.log]

Use it to prove that an updated OpenTelemetry / FusionCache / instrumentation package really runs:
the scope list carries the instrumentation library version (`ZiggyCreatures.Caching.Fusion 2.9.0`,
`OpenTelemetry.Instrumentation.EntityFrameworkCore 1.19.1-beta.1`), the resource carries
`telemetry.sdk.version`. One line per span/metric batch; files of several hundred MB take a minute.
Lines that are not valid JSON (an empty line, or a last line the collector is still writing) are
skipped and counted.
"""

import collections
import json
import sys

sys.stdout.reconfigure(encoding="utf-8")

RESOURCE_KEYS = ("service.instance.id", "deployment.environment.name", "telemetry.sdk.version", "service.version")


def value(v):
    return next(iter(v.values())) if v else None


def summarise(path, kind):
    resources = collections.defaultdict(set)
    scopes = collections.Counter()
    root, inner, items = ("resourceSpans", "scopeSpans", "spans") if kind == "traces" else \
                         ("resourceMetrics", "scopeMetrics", "metrics")
    lines = skipped = 0
    try:
        f = open(path, encoding="utf-8", errors="replace")
    except OSError as e:
        print(f"== {kind}: {path}\n  cannot open: {e}")
        return
    with f:
        for line in f:
            if not line.strip():
                continue
            lines += 1
            try:
                batch = json.loads(line)
            except json.JSONDecodeError:
                skipped += 1
                continue
            for rs in batch.get(root, []):
                attrs = {a["key"]: value(a["value"]) for a in rs.get("resource", {}).get("attributes", [])}
                service = attrs.get("service.name")
                resources[service].add(tuple((k, str(attrs.get(k))) for k in RESOURCE_KEYS if k in attrs))
                for ss in rs.get(inner, []):
                    scope = ss.get("scope", {})
                    scopes[(scope.get("name"), scope.get("version"))] += len(ss.get(items, []))
    print(f"== {kind}: {path}")
    if skipped:
        print(f"  note: {skipped} of {lines} non-empty lines were not valid JSON and were skipped "
              "(a half-written last line is normal while the collector runs)")
    if not resources:
        print("  no telemetry in this file")
        return
    print("  services (resource attributes; more than one row per service = several instances):")
    for service, rows in sorted(resources.items(), key=lambda x: str(x[0])):
        for row in sorted(rows):
            print(f"    {service}: " + ", ".join(f"{k}={v}" for k, v in row))
    print(f"  {kind} by instrumentation scope:")
    for (name, version), count in scopes.most_common():
        print(f"    {count:8} {name} {version or ''}")


def main(argv):
    traces = argv[0] if argv else "../Logs/otel/traces.log"
    metrics = argv[1] if len(argv) > 1 else "../Logs/otel/metrics.log"
    summarise(traces, "traces")
    summarise(metrics, "metrics")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
