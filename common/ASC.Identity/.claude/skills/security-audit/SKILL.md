---
name: security-audit
description: >-
  Trail of Bits–style security audit for the ASC.Identity OAuth 2.0 server:
  differential review of diffs/PRs, insecure defaults, sharp-edge APIs,
  and variant hunting after a known bug. Use when the user says "security
  review", "audit", "threat model", "OWASP", "insecure default", "variant
  analysis", or before merging auth/crypto/input-handling changes.
---

# Security Audit (Trail of Bits methodology)

Security-focused review for the ASC.Identity OAuth 2.0 / OIDC server
(`authorization`, `registration`, `common`, `infrastructure`, `minified`).

> Adapted from [Trail of Bits Skills](https://github.com/trailofbits/skills).
> See [ATTRIBUTION.md](ATTRIBUTION.md). Deep procedure lives under `references/`.

## When to Use

- Security review of a PR, commit range, or module
- Auth, validation, deserialization, file upload, or crypto changes
- Hunting insecure defaults / fail-open config
- After finding one bug: search for variants elsewhere
- Pre-merge gate on HIGH-risk paths

## When NOT to Use

- Pure refactor with no security surface (use `code-quality`)
- Docs/formatting-only diffs
- Greenfield design brainstorm (use OpenSpec explore first)

## Core Principles

1. **Risk-first** — auth, crypto, external I/O, document parsing, ACLs
2. **Evidence-based** — line numbers, commits, concrete attack scenarios
3. **Honest coverage** — state what you did not check
4. **Root cause + variants** — one finding → hunt the family
5. **Write a report artifact** — do not leave findings only in chat

## Project risk hotspots

Prioritize these paths when triaging:

| Area | Where to look |
|------|----------------|
| Caller identity (`x-signature` from DocSpace) | `BasicSignatureAuthenticationFilter`, `SignatureAuthenticationProvider`, `SignatureService`, `MachinePseudoKeys` |
| OAuth endpoints | `AuthorizationServerConfiguration`, custom converters and providers (`PublicClientRefreshToken*`, `PersonalAccessToken*`, `TokenIntrospectionAuthenticationProvider`) |
| Codes, tokens, redirects | `Prefixed*Generator`, `AuthorizationSuccessResponseHandler`, `SecurityUtils.redirectWithError`, redirect URI validation |
| Client management (IDOR across tenants) | `ClientCommandController`, `ClientQueryController`, repository adapters, `CoreDynamoClientRepository` |
| Secrets at rest | `EncryptionService` (AES), `identity_certs` key pairs, client secret generation |
| Cross-service and cross-region trust | gRPC services, RabbitMQ RPC and clean-up listeners |
| Config defaults | profile YAMLs (fallback secrets), `application-security.yml` TTLs, rate limiters, CORS |

## Workflow (choose path)

```
Diff/PR?     → Differential review  (references/methodology.md)
Known bug?   → Variant analysis     (references/variant-analysis.md)
API/config?  → Sharp edges          (references/lang-java.md, auth-patterns.md, config-patterns.md)
Defaults?    → Insecure defaults    (references/insecure-defaults/)
HIGH risk?   → Adversarial model    (references/adversarial.md)
Done         → Report               (references/reporting.md)
```

### 1. Differential review (PRs / commits)

Quick reference (full detail: [references/methodology.md](references/methodology.md)):

| Size | Strategy |
|------|----------|
| <20 files | DEEP — read deps, git blame removed security code |
| 20–200 | FOCUSED — HIGH risk deep, MEDIUM surface |
| 200+ | SURGICAL — critical paths only |

| Risk | Triggers |
|------|----------|
| HIGH | Auth, crypto, external calls, validation removal, public APIs |
| MEDIUM | Business logic, state changes |
| LOW | Comments, tests, logging-only |

**Red flags — escalate immediately:**
- Removed code from "security" / "CVE" / "fix" commits
- Access checks weakened or bypassed
- Validation removed without replacement
- New external/document input without bounds checks
- High blast radius (many callers) + HIGH risk change

### 2. Insecure defaults sweep

Scan the scoped path for:

| Class | Reference |
|-------|-----------|
| Fallback / hardcoded secrets | [references/insecure-defaults/fallback-secrets.md](references/insecure-defaults/fallback-secrets.md) |
| Default credentials | [references/insecure-defaults/default-credentials.md](references/insecure-defaults/default-credentials.md) |
| Fail-open security switches | [references/insecure-defaults/fail-open-security.md](references/insecure-defaults/fail-open-security.md) |
| Weak crypto | [references/insecure-defaults/weak-crypto.md](references/insecure-defaults/weak-crypto.md) |
| Permissive access | [references/insecure-defaults/permissive-access.md](references/insecure-defaults/permissive-access.md) |
| Debug leakage | [references/insecure-defaults/debug-features.md](references/insecure-defaults/debug-features.md) |

For each candidate: **refute or confirm** by tracing to the actual security decision. Do not report untraced pattern matches.

### 3. Sharp edges (misuse-resistant design)

Ask: does the easy path lead to insecurity?

- Java-specific footguns: [references/lang-java.md](references/lang-java.md)
- Auth APIs: [references/auth-patterns.md](references/auth-patterns.md)
- Config schemas: [references/config-patterns.md](references/config-patterns.md)
- Crypto ergonomics: [references/crypto-apis.md](references/crypto-apis.md)

### 4. Variant analysis (after one confirmed bug)

Follow [references/variant-analysis.md](references/variant-analysis.md):

1. Extract root cause (why wrong, not what broke)
2. Exact-match pattern → confirm hit
3. Generalize one element at a time; stop when >50% noise
4. Triage with severity
5. Propose a regression guard (test and/or Semgrep if practical)

### 5. Report

Use [references/reporting.md](references/reporting.md). Minimum sections:

- Scope & methodology (what was / was not reviewed)
- Findings by severity with file:line and attack scenario
- Blast radius / variants
- Residual risk & recommended tests

Prefer writing `SECURITY_REVIEW_REPORT.md` (or a path the user names) rather than chat-only output.

## Java / Spring checklist (ASC.Identity)

- [ ] Input validation on client requests: redirect URIs, origins, logo size and type
- [ ] Authorization tied to `tenant_id` / `user_id` from `x-signature`: no IDOR via `client_id`
- [ ] Redirect URIs matched exactly; no open redirect in error redirects
- [ ] Codes single-use, refresh tokens rotated, revoked tokens rejected; PKCE enforced for `none` clients
- [ ] Introspection and refresh never accept a token of another client or another region
- [ ] Secrets only from config/env. The existing profile fallbacks (`secret`, `1123askdasjklasbnd`) are kept on purpose for old installs; do not report them again and add no new ones
- [ ] Actuator / management exposure locked down
- [ ] SQL via JPA / parameterized `@Query`, no string concatenation in native SQL or migrations
- [ ] Logging: no secrets/PII in MDC or message bodies ([logging-patterns](../logging-patterns/SKILL.md))
- [ ] Raw tokens, codes, secrets and signatures never logged or put into MDC
- [ ] RabbitMQ and gRPC messages: trust boundary assumptions documented

## Quality gate before finishing

- [ ] All HIGH-risk changed files analyzed
- [ ] Git blame on removed security-relevant code
- [ ] Concrete attack scenarios (not generic OWASP labels alone)
- [ ] Variants considered for each confirmed finding
- [ ] Report artifact written
- [ ] False positives explicitly called out

## Related skills

- `code-quality` — general review (not security-first)
- `systematic-debugging` — root-cause before “security fix” thrashing
- `test-driven-development` — regression tests for confirmed vulns
