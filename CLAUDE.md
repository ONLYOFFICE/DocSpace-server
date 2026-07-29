# CLAUDE.md - DocSpace Server

## Project Overview

ONLYOFFICE DocSpace server — a multi-tenant SaaS platform for document management, collaboration, and file sharing. Built on **ASP.NET Core 10.0** with a modular microservices architecture.

## Tech Stack

- **Runtime**: .NET 10.0 (C#)
- **Web Framework**: ASP.NET Core 10.0
- **Orchestration**: .NET Aspire
- **Databases**: MySQL, PostgreSQL
- **Caching**: Redis (StackExchange.Redis) + FusionCache
- **Messaging**: RabbitMQ, Apache Kafka
- **DI Container**: Autofac
- **Logging**: NLog + OpenTelemetry
- **API Docs**: Swashbuckle (Swagger) + Scalar UI
- **Testing**: xUnit v3, FluentAssertions, Testcontainers
- **License**: AGPL 3.0

## Repository Structure

```
server/
├── common/              # Shared core libraries (~31 modules)
│   ├── ASC.Common       # Utilities, caching, extensions
│   ├── ASC.Core.Common  # Domain models, auth, user management
│   ├── ASC.Api.Core     # API conventions, middleware, health checks
│   ├── ASC.Data.Storage  # File storage abstraction
│   ├── ASC.EventBus*    # Event bus (RabbitMQ, ActiveMQ, Redis)
│   ├── ASC.FederatedLogin # OAuth/SSO
│   ├── ASC.Data.Backup* # Backup/restore
│   ├── services/        # Background services (Notify, AuditTrail, etc.)
│   └── Tests/           # Core test projects
├── products/            # Feature modules
│   ├── ASC.Files/       # File management (Server, Core, Service, Tests)
│   ├── ASC.People/      # User/team management (Server, Tests)
│   └── ASC.AI/          # AI features (Server, Core, Service)
├── web/                 # Web layer
│   ├── ASC.Web.Api      # REST API controllers
│   ├── ASC.Web.Core     # Web infrastructure
│   ├── ASC.Web.Studio   # UI backend
│   └── ASC.Web.HealthChecks.UI
├── sdk/                 # Multi-language API SDKs (git submodules)
├── migrations/          # DB migrations (mysql/, postgre/ × SaaS/Standalone)
├── thirdparty/          # Third-party libs (Google.Authenticator, MS Graph, etc.)
└── .aspire/             # Aspire CLI settings (settings.json); the AppHost project lives in common/ASC.AppHost
```

## Build & Run

**Solution files:**
- `ASC.Web.sln` — main solution
- `ASC.Tests.slnx` — test solution
- `ASC.Migrations.sln` — database migrations

**Common commands:**
```bash
dotnet build ASC.Web.sln
dotnet test ASC.Tests.slnx
dotnet run --project common/ASC.AppHost  # Run via Aspire orchestration
```

**Package management:** Centralized in `Directory.Packages.props` (all version pins live there).

## Logs & Configs (Local Dev)

Both live **outside this repo**, as siblings of `server/` in the parent `docspace/` directory:

- **Configs**: `../buildtools/config/` — the effective runtime configuration for all services: `appsettings*.json`, `apisystem.json`, `redis.json`, `rabbitmq.json`, `elastic.json`, `autofac*.json`, `nlog.config`, `nginx/`, etc. Environment variables override values from these files; local `config/` files inside this repo are NOT the source of truth.
- **Logs**: `../Logs/` — one file per service: `web.api.log`, `files.log`, `files.worker.log`, `backup.log`, `notify.log`, `people.log`, `apisystem.log`, etc. Some are date-suffixed (e.g. `web.login.07-29.log`). Check these first when diagnosing a running service.

## Coding Conventions

### Naming
- **Namespaces**: `ASC.<Module>[.<Feature>][.<Layer>]` (e.g., `ASC.Files.Core.ApiModels.RequestDto`)
- **Controllers**: `*Controller`
- **DTOs**: `*RequestDto`, `*ResponseDto`
- **Custom attributes**: `[Singleton]`, `[Scope]`, `[DefaultRoute]`, `[ControllerName]`
- **Route segments**: camelCase (e.g., `{id}/externalDbSync`, `fromTemplate`) — never snake_case or kebab-case

### Style (enforced via `.editorconfig`)
- **Indentation**: 4 spaces (no tabs); 2 spaces for XML/JSON/YAML
- **`var` usage**: preferred everywhere (`csharp_style_var_*` = true:warning)
- **Namespaces**: file-scoped (`namespace Foo;`) — enforced with warning
- **Usings**: `ImplicitUsings` enabled; system directives sorted first, separated into groups. **All `using` directives must be placed in `GlobalUsings.cs`** (one per project), never in individual `.cs` files.
- **Braces**: always required (`csharp_prefer_braces` = true:warning)
- **`using` statements**: prefer simple form (`using var x = ...`)
- **Object creation**: prefer target-typed `new()` when type is apparent
- **Default expressions**: prefer `default` over `default(T)`
- **Index/Range**: prefer `^1` and `..` operators
- **Null checks**: prefer `is null` / `is not null` over `ReferenceEquals`
- **Access modifiers**: explicit modifiers required (warning)
- **Readonly fields**: enforced with warning
- **Private fields**: `_camelCase`; public fields / constants / types: `PascalCase`; interfaces: `IName`
- **XML docs**: `<summary>`, `<remarks>`, `<example>` on API models; `GenerateDocumentationFile=True`
- **License header**: AGPL 3.0 header required on all source files
- **Line endings**: CRLF; `insert_final_newline = true`; trailing whitespace trimmed

### Logging
- **Always use source-generated logging** via `[LoggerMessage]` attribute on `partial` methods in a dedicated `static partial class` (e.g., `*Logger`).
- Never use string interpolation or `ILogger.LogInformation(...)` directly — always define `LoggerMessage`-attributed extension methods.
- Example pattern:
```csharp
public static partial class FooLogger
{
    [LoggerMessage(LogLevel.Information, "Found {count} items")]
    public static partial void InfoFoundItems(this ILogger<FooService> logger, int count);
}
```

### API Patterns
- API versioning via `Asp.Versioning`
- Swagger annotations for OpenAPI generation
- Controllers inherit common base, use `[DefaultRoute]` attribute
- Request/Response models in `ApiModels/RequestDto` and `ApiModels/ResponseDto` namespaces

## Testing

- **Framework**: xUnit v3 with `UseMicrosoftTestingPlatformRunner`
- **Assertions**: FluentAssertions
- **Containers**: Testcontainers (MySQL, PostgreSQL, RabbitMQ, Redis, OpenSearch)
- **Fake data**: Bogus
- **DB cleanup**: Respawn
- **Test locations**: `common/Tests/`, `products/*/Tests/`

```bash
dotnet test ASC.Tests.slnx
```

## Git Workflow

- **Main branch**: `master`
- **Integration branch**: `develop`
- **Branch naming**: `feature/*`, `bugfix/*`
- **Submodules**: 8 SDK submodules in `sdk/` — use `git submodule update --init` after clone

## Architecture Notes

- **Modular microservices**: Products (Files, People, AI) are separate deployable units
- **Event-driven**: Event bus abstraction with RabbitMQ/ActiveMQ/Redis backends
- **Multi-database**: MySQL and PostgreSQL with separate migration paths (SaaS vs Standalone)
- **Auth**: JWT Bearer, OpenID Connect, SAML, federated login/SSO
- **Caching**: Redis distributed cache with FusionCache L2 and cache invalidation notifications
- **Health checks**: ASP.NET Core health checks for all infrastructure dependencies
- **Observability**: OpenTelemetry tracing and metrics throughout

### Code Intelligence

**HARD RULE: C# code navigation goes through LSP ONLY. Never Grep for a C# symbol.**

At the start of any task that touches C# code, load the LSP tool FIRST
(via ToolSearch if it is deferred) — before the first search, so it is
already at hand when you need it.

Trigger → action:
- Find where a class/method/property is defined → `workspaceSymbol` or `goToDefinition`. NOT Grep.
- Find all usages of a symbol → `findReferences`. NOT Grep.
- Find implementations of an interface/abstract member → `goToImplementation`. NOT Grep.
- List symbols in a file → `documentSymbol`. NOT Read.
- Need a type signature → `hover`. NOT Read.
- Trace callers/callees → `incomingCalls` / `outgoingCalls`. NOT Grep.

`workspaceSymbol` matches by substring (fuzzy) — a query like `Chunk`
also returns `ChunkSize`, `UploadChunkAsync`, etc. Filter the results
by exact name instead of assuming the first hit is the right one.

For `workspaceSymbol`, pass any existing `.cs` file as `filePath` (e.g.
`common/ASC.Common/Data/TempPath.cs`) — the LSP server is selected by file
extension, so `.sln`/`.csproj` paths fail with "No LSP server available".
`line`/`character` values are ignored for this operation.

Before renaming or changing a function signature, use `findReferences`
to find all call sites first.

Grep/Glob are allowed ONLY for non-symbol text: string literals,
comments, config files (.json/.yml/.props), route templates, SQL.
Calling Grep with a pattern that is a C# identifier (class, method,
property name) is a violation of this rule.

After writing or editing code, check LSP diagnostics before moving on.
Fix any type errors or missing imports immediately.