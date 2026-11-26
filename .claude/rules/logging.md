---
paths:
  - "**/*.cs"
---

# Logging

- **Always use source-generated logging** via `[LoggerMessage]` attribute on `partial` methods in a dedicated `static partial class` (e.g. `*Logger`).
- Never use string interpolation or `ILogger.LogInformation(...)` directly — always define `LoggerMessage`-attributed extension methods.
- A failure is logged with the exception itself, as the method's `Exception` parameter, not with `ex.Message`: the message alone drops the stack trace, the inner exceptions and the exception type.
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
