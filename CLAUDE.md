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
└── .aspire/             # Aspire AppHost configuration
```

## Build & Run

**Solution files:**
- `ASC.Web.sln` — main solution
- `ASC.Tests.sln` — test solution
- `ASC.Migrations.sln` — database migrations

**Common commands:**
```bash
dotnet build ASC.Web.sln
dotnet test ASC.Tests.sln
dotnet run --project .aspire/AppHost  # Run via Aspire orchestration
```

**Package management:** Centralized in `Directory.Packages.props` (all version pins live there).

## Coding Conventions

### Naming
- **Namespaces**: `ASC.<Module>[.<Feature>][.<Layer>]` (e.g., `ASC.Files.Core.ApiModels.RequestDto`)
- **Controllers**: `*Controller`
- **DTOs**: `*RequestDto`, `*ResponseDto`
- **Custom attributes**: `[Singleton]`, `[Scope]`, `[DefaultRoute]`, `[ControllerName]`

### Style (enforced via `.editorconfig`)
- **Indentation**: 4 spaces (no tabs); 2 spaces for XML/JSON/YAML
- **`var` usage**: preferred everywhere (`csharp_style_var_*` = true:warning)
- **Namespaces**: file-scoped (`namespace Foo;`) — enforced with warning
- **Usings**: `ImplicitUsings` enabled; system directives sorted first, separated into groups; placement outside namespace. Common namespaces should be in `GlobalUsings.cs` — unnecessary per-file usings flagged by `IDE0005` (warning)
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
dotnet test ASC.Tests.sln
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
