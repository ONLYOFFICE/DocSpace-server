---
paths:
  - "**/*.cs"
---

# Logging

- **Always use source-generated logging** via `[LoggerMessage]` attribute on `partial` methods in a dedicated `static partial class` (e.g. `*Logger`).
- Never use string interpolation or `ILogger.LogInformation(...)` directly — always define `LoggerMessage`-attributed extension methods.
- Example pattern:

```csharp
public static partial class FooLogger
{
    [LoggerMessage(LogLevel.Information, "Found {count} items")]
    public static partial void InfoFoundItems(this ILogger<FooService> logger, int count);
}
```