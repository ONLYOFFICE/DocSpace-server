---
paths:
  - "**/*.cs"
---

# Logging

- **Always use source-generated logging** via `[LoggerMessage]` attribute on `partial` methods in a dedicated `static partial class` (e.g. `*Logger`).
- Never use string interpolation or `ILogger.LogInformation(...)` directly — always define `LoggerMessage`-attributed extension methods.
- A failure is logged with the exception itself, as the method's `Exception` parameter, not with `ex.Message`: the message alone drops the stack trace, the inner exceptions and the exception type.
- **A lasting condition is logged when it is found or changes, not on every request.** A stored value
  that cannot be read (a setting that does not decrypt, a broken plugin), a dependency that stays
  down, an item that keeps waiting: log the first occurrence with the exception, then at most once per
  interval per tenant and item (a mark with an explicit duration, `caching.md`; in the `"memory"`
  instance the limit holds per process, and the comment says so). A read path any member reaches never
  writes an Error per request. Seen in review: every page load wrote an error with a stack for a stored
  value that could not be read, until an admin saved it again.
- Example pattern:

```csharp
public static partial class FooLogger
{
    [LoggerMessage(LogLevel.Information, "Found {count} items")]
    public static partial void InfoFoundItems(this ILogger<FooService> logger, int count);
}
```

## Checklist

- Every new log call is a `[LoggerMessage]`-attributed `partial` method (bullet 1)
- Those methods live in a dedicated `static partial class`, such as `*Logger` (bullet 1)
- No direct `ILogger.LogInformation(...)` or other `Log*` call is added (bullet 2)
- No string interpolation in a log message; values go in as method parameters (bullet 2)
- A failure is logged with the exception as the `Exception` parameter, never `ex.Message` (bullet 3)
- A lasting condition is logged when found or changed, then throttled per tenant and item, never per request (bullet 4)
