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
- **Testing**: xUnit v3, FluentAssertions, Aspire.Hosting.Testing
- **License**: AGPL 3.0

## Repository Structure

```
server/
├── common/              # Shared core libraries
│   ├── ASC.Common       # Utilities, caching, extensions
│   ├── ASC.Core.Common  # Domain models, auth, user management
│   ├── ASC.Api.Core     # API conventions, middleware, health checks
│   ├── ASC.Data.Storage  # File storage abstraction
│   ├── ASC.EventBus*    # Event bus (RabbitMQ, ActiveMQ, Redis)
│   ├── ASC.FederatedLogin # OAuth/SSO
│   ├── ASC.Data.Backup* # Backup/restore
│   ├── services/        # Background services (Notify, AuditTrail, etc.) + ASC.Monolith (all-in-one single-process host)
│   ├── Tools/           # Dev tools: ASC.Migration.Runner (applies DB migrations), ASC.Migration.Creator, ASC.Api.Documentation, etc.
│   └── Tests/           # ASC.Tests.Common (shared integration-test harness) + ASC.Core.Common.Tests; the other projects here are legacy
├── products/            # Feature modules
│   ├── ASC.Files/       # File management (Server, Core, Worker, Tests)
│   ├── ASC.People/      # User/team management (Server, Tests)
│   └── ASC.AI/          # AI features (Server, Core, Worker, Tests)
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
- `ASC.Web.sln` — main solution (a parallel `ASC.Web.slnx` exists but is out of sync with the `.sln` — prefer `ASC.Web.sln`)
- `ASC.Tests.slnx` — test solution (there is NO `ASC.Tests.sln`)
- `ASC.Migrations.sln` — database migrations
- `thirdparty.sln` — third-party libraries

**Common commands:**
```bash
dotnet build ASC.Web.sln
dotnet test ASC.Tests.slnx
dotnet run --project common/ASC.AppHost --launch-profile development  # Run via Aspire orchestration
cd common/Tools/ASC.Migration.Runner && dotnet run                    # Apply DB migrations
```

The AppHost has 5 launch profiles: `development`, `test`, `preview`, `integration-test`, `frontend-dev` — always pass `--launch-profile` explicitly.

**Aspire CLI:** works from the repo root — the committed `.aspire/settings.json` points to the AppHost, no `--apphost` flag needed. The resource graph is defined in `common/ASC.AppHost/Program.cs` plus `common/ASC.AppHost/Configuration/` (`ProjectConfigurator.cs`, `ConnectionStringManager.cs`, `NginxConfiguration.cs`) — not in a single-file `apphost.cs`.

**Package management:** Centralized in `Directory.Packages.props` — all version pins AND the global `TargetFramework` (net10.0) live there. `Directory.Build.props` only enables OpenAPI doc generation and strips native NuGet .pdb files; most csprojs set no TFM of their own.

**More detail:** `README.md` documents service ports (Web.Api 5000, Studio 5003, People 5004, Files 5007/5009, AI 5157/5154), Aspire dashboard / Scalar / DBGate / Mailpit URLs, prerequisites, and troubleshooting.

## Logs & Configs (Local Dev)

Both live **outside this repo**, as siblings of `server/` in the parent `docspace/` directory:

- **Configs**: `../buildtools/config/` — the effective runtime configuration for all services: `appsettings*.json`, `apisystem.json`, `redis.json`, `rabbitmq.json`, `elastic.json`, `autofac*.json`, `nlog.config`, `nginx/`, etc. Environment variables override values from these files; local `config/` files inside this repo are NOT the source of truth.
- **Logs**: `../Logs/` — one file per service: `web.api.log`, `files.log`, `files.worker.log`, `backup.log`, `notify.log`, `people.log`, `apisystem.log`, etc. Some are date-suffixed (e.g. `web.login.07-29.log`). Check these first when diagnosing a running service.

## Coding Conventions

C# naming, style, and API conventions live in `.claude/rules/csharp-style.md` (loaded automatically when working with `.cs` files). Logging conventions: `.claude/rules/logging.md`. Caching conventions (FusionCache only — never hand-rolled caches; two cache instances, keys/tags, invalidation): `.claude/rules/caching.md`. HTTP client conventions (IHttpClientFactory only, reuse the standard named clients from BaseStartup): `.claude/rules/http-clients.md`. Code navigation rules (LSP-only): `.claude/rules/csharp-lsp.md`.

## Testing

Conventions for writing integration tests (per-test portal, roles, `ApiException` assertions, access-level matrices, class size vs parallelism): `.claude/rules/tests.md`.

- **Framework**: xUnit v3 with `UseMicrosoftTestingPlatformRunner`
- **Assertions**: FluentAssertions
- **Infrastructure**: integration tests do NOT use Testcontainers — they boot the real Aspire AppHost via `Aspire.Hosting.Testing` (`DistributedApplicationTestingBuilder.CreateAsync<Projects.ASC_AppHost>` with the `integration-test` launch profile), which provides MySQL/PostgreSQL/RabbitMQ/Redis/OpenSearch containers. The harness is shared: `common/Tests/ASC.Tests.Common` holds `AspireHostFixture<TClients>`, `PortalClientsBase`, `Initializer` and `RawApiClient`; each suite only derives a thin `AspireAppFixture`/`PortalClients` pair in its own `ApiFactories/` folder
- **Fake data**: Bogus
- **DB cleanup**: Respawn
- **Test locations**: `products/*/Tests/`, `web/ASC.Web.Api.Tests/` (Web.Api REST suite) and `common/Tests/` (`ASC.Core.Common.Tests`, `ASC.Notify.Tests` letter suite, `ASC.Data.Backup.Core.Tests`). The remaining projects in `common/Tests/` are legacy (net7/net8), NOT part of `ASC.Tests.slnx`, and are not run by `dotnet test`

```bash
dotnet test ASC.Tests.slnx
```

## Git Workflow

- **Main branch**: `master`
- **Integration branch**: `develop`
- **Branch naming**: `feature/*`, `bugfix/*`
- **Submodules**: 9 total — 8 SDK submodules in `sdk/` plus `common/resources/DocStore` (document templates); use `git submodule update --init` after clone

## Architecture Notes

- **Modular microservices**: Products (Files, People, AI) are separate deployable units
- **Event-driven**: Event bus abstraction with RabbitMQ/ActiveMQ/Redis backends
- **Multi-database**: MySQL and PostgreSQL with separate migration paths (SaaS vs Standalone)
- **Auth**: JWT Bearer, OpenID Connect, SAML, federated login/SSO
- **Caching**: Redis distributed cache with FusionCache L2 and cache invalidation notifications
- **Health checks**: ASP.NET Core health checks for all infrastructure dependencies
- **Observability**: OpenTelemetry tracing and metrics throughout
