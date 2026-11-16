---
name: logging-patterns
description: Java logging best practices with SLF4J, structured logging (JSON), and MDC for request tracing. Includes AI-friendly log formats for Claude Code debugging. Use when user asks about logging, debugging application flow, or analyzing logs.
---

# Logging Patterns Skill

Effective logging for Java applications with focus on structured, AI-parsable formats.

> **Rule for this codebase:** attach contextual fields (`client_id`, `id`, `tenant_id`, `user_id`) via SLF4J's **`MDC`**, not string concatenation and not `StructuredArguments.kv()`. See [Attaching Fields](#attaching-fields-use-mdc-not-structuredargumentskv) below.

> **Rule for this codebase:** know which level reaches which install. `dev` runs at `DEBUG`, `stage` at `INFO`, and `prod` (every on-premises install: Docker, DEB/RPM, Windows) at **`WARN`**. A `log.info` is therefore invisible on customer installs. Anything a support engineer needs to see on a customer's server must be `WARN` or `ERROR`; keep `INFO` for the stage and SaaS operational trail and `DEBUG` for local detail.

> **Rule for this codebase:** never put a secret into MDC or a message: no raw tokens, codes, client secrets or `x-signature` values. The dev `CONSOLE` pattern prints `%X` (all MDC entries), and every JSON encoder flattens MDC into top-level fields. Log a hash (see `AuthorizationService`, which stores `hashedToken`) or an id instead.

> **Rule for this codebase:** prefer putting dynamic values into **`MDC`** over `{}` placeholders in the message. Every JSON encoder flattens MDC into top-level fields, so an MDC entry is a queryable field instead of text embedded in `message`, and the message template stays constant, which log aggregators group on. Reserve `{}` for values that do not deserve a field (an exception's own text, a count that reads naturally in the sentence).

## When to Use
- User says "add logging" / "improve logs" / "debug this"
- Analyzing application flow from logs
- Setting up structured logging (JSON)
- Request tracing with correlation IDs
- AI/Claude Code needs to analyze application behavior

---

## AI-Friendly Logging

> **Key insight:** JSON logs are better for AI analysis - faster parsing, fewer tokens, direct field access.

### Why JSON for AI/Claude Code?

```
# Text format - AI must "interpret" the string
2026-01-29 10:15:30 INFO OrderService - Order 12345 created for user-789, total: 99.99

# JSON format - AI extracts fields directly
{"date":"2026-01-29T10:15:30Z","level":"INFO","sourceContext":"OrderService","message":"Order created","jobId":"12345","userId":"user-789"}
```

| Aspect | Text | JSON |
|--------|------|------|
| Parsing | Regex/interpretation | Direct field access |
| Token usage | Higher (repeated patterns) | Lower (structured) |
| Error extraction | Parse stack trace text | `stack_trace` field |
| Filtering | grep patterns | `jq` queries |

---

## Appenders in This Project

Config lives in **`infrastructure/infrastructure-configuration/src/main/resources/logback-spring.xml`** and is shared by every service. It is a hand-rolled `logstash-logback-encoder` setup, **not** Spring Boot's native `logging.structured.format`; that key is a no-op here.

Which appender is active depends on the logging profile, on `saas`, and on whether `LOG_FILE_PATH` is set (production installs set it, so they log to a file):

| Profile | `LOG_FILE_PATH` unset | `LOG_FILE_PATH` set | `saas` | Root level |
|---|---|---|---|---|
| `default`, `development`, `dev`, `d` | `CONSOLE` (colored text, prints `%X`) | `JSON_FILE` | | `DEBUG` |
| `staging`, `stage`, `s` | `JSON_CONSOLE` | `JSON_FILE` | `CLOUDWATCH` | `INFO` |
| `production`, `prod`, `p` | `JSON_CONSOLE` | `JSON_FILE` | `CLOUDWATCH` | `WARN` |

- `JSON_FILE` rolls daily and keeps 30 days. On-premises the files are `ASC.Identity.Registration.log` and `ASC.Identity.Authorization.log` in the DocSpace log directory (`/var/log/onlyoffice` in Docker).
- `CLOUDWATCH` is the custom `com.asc.common.autoconfigurations.cloudwatch.CloudWatchAppender`, enabled only when `logging.cloudwatch.enabled=true` (`SPRING_LOGGING_CLOUDWATCH_ENABLED`).
- Async wrappers are `net.logstash.logback.appender.LoggingEventAsyncDisruptorAppender`, not logback's `AsyncAppender`.
- `org.hibernate.orm.jdbc.batch`, `org.hibernate.engine` and `java.sql.BatchUpdateException` are switched off on purpose (deadlock noise from concurrent grant writes and purges).

**Gotcha:** if something does not show up in a customer's log, check the level first: production is `WARN`, so `INFO` is dropped by design.

### JSON field names are remapped

Every JSON encoder overrides `LogstashEncoder`'s default field names:

```xml
<fieldNames>
    <level>level</level>
    <logger>sourceContext</logger>
    <timestamp>date</timestamp>
    <thread>threadId</thread>
    <levelValue>[ignore]</levelValue>
</fieldNames>
```

So in `jq` filters use **`sourceContext`**, **`date`**, **`threadId`**, not the logstash defaults (`logger_name`, `@timestamp`, `thread_name`). `message` and MDC keys stay as they are. `customFields` adds `applicationContext` (`spring.application.name`: `ASC.Identity.Registration`, `ASC.Identity.Authorization`) and `instanceId` (`${HOSTNAME}`) to every record. Stack traces are shortened, root cause first.

### Activating profiles locally

```bash
# Human-readable console (default)
SPRING_PROFILES_ACTIVE=dev,server java -jar registration/registration-container/target/*.jar

# JSON console, staging-shaped
SPRING_PROFILES_ACTIVE=stage,server java -jar authorization/authorization-container/target/*.jar

# JSON file, as on an install
LOG_FILE_PATH=/tmp/identity.log SPRING_PROFILES_ACTIVE=prod,server java -jar ...
```

### Reading logs with AI/Claude Code

```bash
# Recent errors
jq 'select(.level == "ERROR")' ASC.Identity.Authorization.log | tail -20

# Follow one OAuth client through both services
jq 'select(.client_id == "<client id>")' ASC.Identity.*.log

# Narrow by service
jq 'select(.applicationContext == "ASC.Identity.Authorization")' identity.log
```

---

## Attaching Fields: Use MDC, Not `StructuredArguments.kv()`

This codebase does **not** use `net.logstash.logback.argument.StructuredArguments.kv()`; services attach structured fields via SLF4J's `MDC`, which the encoders flatten into top-level JSON keys. Stay consistent with that.

### Existing pattern: put, then clear in `finally`

Used across controllers, services and listeners (for example `AuthorizationConsentController`, `AuthorizationService`):

```java
try {
  MDC.put("client_id", clientId);
  log.info("Got a new consent request");
  // ...
} finally {
  MDC.clear();
}
```

### Preferred in new code: `MDC.putCloseable`

`MDC.clear()` wipes everything, including keys an outer scope set (a filter, a listener). `putCloseable` only unwinds what you added and cannot leak on an exception:

```java
try (var _ = MDC.putCloseable("client_id", clientId);
     var _ = MDC.putCloseable("tenant_id", String.valueOf(tenantId))) {
  cleanupService.removeClientGrants(clientId);
} catch (Exception ex) {
  log.error("Could not clean up the grants of a removed client", ex);
  throw ex;
}
```

Do not mix the two in one call chain: an inner `MDC.clear()` removes the outer `putCloseable` keys before the outer block logs.

---

## Prefer MDC Fields Over `{}` Placeholders

Putting a value in MDC instead of a `{}` placeholder makes it a first-class field in every appender (`%X` in `CONSOLE`, a top-level key in every JSON and CloudWatch encoder) rather than text baked into `message`. It also avoids the same value being duplicated — once as a structured field, once again re-parsed out of a sentence.

```java
// ✅ Preferred — status becomes its own filterable field, message stays a stable template
try (var _ = MDC.putCloseable("client_id", clientId);
     var _ = MDC.putCloseable("status", String.valueOf(status))) {
  log.warn("Registration service answered with an error");
}

// ❌ Avoid — value is only reachable by parsing the message string
log.warn("Registration service answered {} for client {}", status, clientId);
```

If a field is already open in an outer `MDC.putCloseable` scope for the whole method (e.g. `client_id` for the duration of a gRPC call), don't also repeat it as a `{}` argument — that's the exact duplication this rule avoids. Open a narrower, nested `MDC.putCloseable` around just the log call for values that are only known at that point (e.g. a response status, a resolved path).

Exceptions are logged with their own `Throwable` argument as the last parameter (`log.error("...", ex)`), not via `{}` — SLF4J recognizes a trailing `Throwable` and renders the stack trace separately (`stack_trace` field in JSON), so this isn't a violation of the rule above.

---

## SLF4J Basics

### Logger Declaration

Services use Lombok's `@Slf4j`, which generates `private static final org.slf4j.Logger log`:

```java
@Slf4j
@Service
@RequiredArgsConstructor
public class ConsentService {
  // use `log` directly
}
```

The IDE's Eclipse compiler has no Lombok and reports "log cannot be resolved" everywhere. That is noise; trust the Maven build.

### Parameterized Logging

```java
// ✅ GOOD: Evaluated only if level enabled
log.debug("Processing order {} for user {}", orderId, userId);

// ❌ BAD: Always concatenates
log.debug("Processing order " + orderId + " for user " + userId);

// ✅ For expensive operations (e.g. building a large payload to log)
if (log.isDebugEnabled()) {
  log.debug("Payload: {}", expensiveToString(payload));
}
```
