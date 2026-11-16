---
name: systematic-debugging
description: >-
  Systematic debugging for bugs, test failures, or unexpected behavior.
  Use before proposing fixes — find root cause first. Especially for
  issues that cross registration, authorization, gRPC, RabbitMQ and Redis.
---

# Systematic Debugging

Adapted from [obra/superpowers](https://github.com/obra/superpowers) (`systematic-debugging`). See [LICENSE](LICENSE) (MIT).

## Overview

**Core principle:** ALWAYS find root cause before attempting fixes. Symptom fixes are failure.

**Violating the letter of this process is violating the spirit of debugging.**

## The Iron Law

```
NO FIXES WITHOUT ROOT CAUSE INVESTIGATION FIRST
```

If you haven't completed Phase 1, you cannot propose fixes.

## When to Use

Use for ANY technical issue:
- Test failures
- Bugs in production
- Unexpected behavior
- Performance problems
- Build failures
- Integration issues (gRPC, RabbitMQ, Redis, MySQL, DynamoDB, Testcontainers, Docker Compose)

**Use this ESPECIALLY when:**
- Under time pressure
- "Just one quick fix" seems obvious
- You've already tried multiple fixes
- Previous fix didn't work

## The Four Phases

You MUST complete each phase before proceeding to the next.

### Phase 1: Root Cause Investigation

**BEFORE attempting ANY fix:**

1. **Read Error Messages Carefully**
   - Read stack traces completely
   - Note line numbers, file paths, error codes
   - Check related logs (MDC `client_id`, `id`; `applicationContext` and `instanceId` in JSON logs)

2. **Reproduce Consistently**
   - Exact steps? Every time?
   - If not reproducible → gather more data, don't guess

3. **Check Recent Changes**
   - `git diff`, recent commits, profile YAMLs, dependency bumps

4. **Gather Evidence in Multi-Component Systems**

   A request crosses DocSpace (`x-signature`), the nginx router, authorization, registration over
   gRPC, the client caches (Caffeine and Redis), MySQL or DynamoDB, and RabbitMQ for clean-up and
   cache eviction (across regions in SaaS).

   **BEFORE proposing fixes, add diagnostic instrumentation at boundaries:**
   - Log what enters/exits each component (`client_id`, `id`, outcome)
   - Verify config/env propagation (active profiles, `SERVER_PORT`, gRPC addresses, secrets)
   - Check state at each layer (DB row, cache entry, `identity_pending_messages`, queue depth)

   Run once to gather evidence showing **WHERE** it breaks, then investigate that component.

5. **Trace Data Flow**

   WHEN error is deep in call stack: see [root-cause-tracing.md](root-cause-tracing.md).
   Fix at source, not at symptom.

### Phase 2: Pattern Analysis

1. Find working examples in the same codebase
2. Compare against reference implementations completely (don't skim)
3. List every difference between working and broken
4. Understand dependencies (profiles, env, exchanges and queues, gRPC addresses)

### Phase 3: Hypothesis and Testing

1. Form a **single** hypothesis: "X is root cause because Y"
2. Test with the **smallest** possible change — one variable
3. Did it work? → Phase 4. No? → new hypothesis (don't stack fixes)
4. When you don't know — say so; research more

### Phase 4: Implementation

1. **Create failing test** — simplest reproduction. Use `test-driven-development`.
2. **Implement single fix** — root cause only; no drive-by refactors
3. **Verify** — test passes; no other breakage; issue actually resolved
4. **If fix doesn't work** — STOP. If <3 attempts: return to Phase 1. If ≥3: question architecture with your human partner
5. Defense in depth after root cause: [defense-in-depth.md](defense-in-depth.md)
6. Flaky timing tests: [condition-based-waiting.md](condition-based-waiting.md)

## Red Flags — STOP and Follow Process

- "Quick fix for now, investigate later"
- "Just try changing X"
- Multiple changes at once
- Skip the test
- "It's probably X"
- Proposing solutions before tracing data flow
- "One more fix" after 2+ failures

**ALL of these mean: STOP. Return to Phase 1.**

## Common Rationalizations

| Excuse | Reality |
|--------|---------|
| "Issue is simple" | Simple issues have root causes too |
| "Emergency, no time" | Systematic is faster than thrashing |
| "Try this first, then investigate" | First fix sets the pattern — do it right |
| "Test after confirming fix" | Untested fixes don't stick |
| "Multiple fixes at once saves time" | Can't isolate what worked |
| "I see the problem" | Seeing symptoms ≠ understanding root cause |

## Quick Reference

| Phase | Key Activities | Success Criteria |
|-------|----------------|------------------|
| 1. Root Cause | Read errors, reproduce, check changes, gather evidence | Understand WHAT and WHY |
| 2. Pattern | Find working examples, compare | Identify differences |
| 3. Hypothesis | Form theory, test minimally | Confirmed or new hypothesis |
| 4. Implementation | Create test, fix, verify | Bug resolved, tests pass |

## Observability hooks (ASC.Identity)

When gathering evidence, prefer existing signals:
- Logs: MDC keys (`client_id`, `id`) via `logging-patterns`; do not invent a second correlation scheme
- Health: `/health` (liveness and readiness) of each service
- Queues: the RabbitMQ management UI for the clean-up, retry and dead-letter queues
- Outbox: rows in `identity_pending_messages` that the relay has not sent yet
- Stale jars: an `Unresolved compilation problem` at runtime means the IDE wrote `target/classes`; run `mvn clean`

## When Process Reveals "No Root Cause"

If truly environmental/timing/external after full investigation:

1. Document what you investigated
2. Implement appropriate handling (retry, timeout, clear error)
3. Add monitoring/logging for future investigation

**But:** 95% of "no root cause" cases are incomplete investigation.
