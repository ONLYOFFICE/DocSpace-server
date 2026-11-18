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
└── aspire.config.json   # Aspire CLI settings (points at the AppHost in common/ASC.AppHost)
```

## Build & Run

**Solution files:**
- `ASC.Web.slnx` — main solution. Use this one. It holds the services and the libraries they are built from — not every project of the repo. A tool needed only to produce the documentation or by the tests stays out of it: `common/Tools/ASC.Webhooks.Contract` is run with `dotnet run --project` after the API documents are emitted and is built through `ASC.Tests.slnx`, whose `ASC.Core.Common.Tests` references it
- `ASC.Web.sln` — **deprecated**, kept for tooling that cannot read `.slnx` yet. As of 2026-09-03 it still mirrors the `.slnx` exactly (the same 48 projects, `Solution Items` and `migrations` folder, the same Debug/Release configurations), but it is no longer the source of truth: add new service and library projects to `ASC.Web.slnx`, and only mirror them into the `.sln` if you actually need it
- `ASC.Tests.slnx` — test solution (there is NO `ASC.Tests.sln`)
- `ASC.Migrations.sln` — database migrations
- `thirdparty.sln` — third-party libraries

**Common commands:**
```bash
dotnet build ASC.Web.slnx
dotnet test ASC.Tests.slnx
dotnet run --project common/ASC.AppHost --launch-profile development  # Run via Aspire orchestration
cd common/Tools/ASC.Migration.Runner && dotnet run                    # Apply DB migrations
```

The AppHost has 7 launch profiles: `development`, `test`, `preview`, `frontend-dev`, and the three test stacks `integration-test`, `integration-test-billing` (SaaS, billing/accounting/docscloud stubbed by the billing suite) and `notify-test` — always pass `--launch-profile` explicitly.

**Aspire CLI:** works from the repo root — the committed `aspire.config.json` points to the AppHost (`appHostPath`, relative to the repo root), no `--apphost` flag needed. CLI 13.x dropped the older `.aspire/settings.json`; if the config is missing, recreate it with `aspire config set appHostPath "common/ASC.AppHost/ASC.AppHost.csproj"` (the CLI never writes the file on its own). The resource graph is defined in `common/ASC.AppHost/Program.cs` plus `common/ASC.AppHost/Configuration/` (`ProjectConfigurator.cs`, `ConnectionStringManager.cs`, `NginxConfiguration.cs`) — not in a single-file `apphost.cs`.

**Package management:** Centralized in `Directory.Packages.props` — all version pins AND the global `TargetFramework` (net10.0) live there. `Directory.Build.props` only enables OpenAPI doc generation and strips native NuGet .pdb files; most csprojs set no TFM of their own.

**More detail:** `README.md` documents service ports (Web.Api 5000, Studio 5003, People 5004, Files 5007/5009, AI 5157/5154), Aspire dashboard / Scalar / DBGate / Mailpit URLs, prerequisites, and troubleshooting.

## Logs & Configs (Local Dev)

Both live **outside this repo**, as siblings of `server/` in the parent `docspace/` directory:

- **Configs**: `../buildtools/config/` — the effective runtime configuration for all services: `appsettings*.json`, `apisystem.json`, `redis.json`, `rabbitmq.json`, `elastic.json`, `autofac*.json`, `nlog.config`, `nginx/`, etc. Environment variables override values from these files; local `config/` files inside this repo are NOT the source of truth.
- **Logs**: `../Logs/` — one file per service: `web.api.log`, `files.log`, `files.worker.log`, `backup.log`, `notify.log`, `people.log`, `apisystem.log`, etc. Some are date-suffixed (e.g. `web.login.07-29.log`). Check these first when diagnosing a running service.

## Coding Conventions

C# naming, style, and API conventions live in `.claude/rules/csharp-style.md` (loaded automatically when working with `.cs` files). Logging conventions: `.claude/rules/logging.md`. Caching conventions (FusionCache only — never hand-rolled caches; two cache instances, keys/tags, invalidation): `.claude/rules/caching.md`. HTTP client conventions (IHttpClientFactory only, reuse the standard named clients from `AddStandardHttpClients`, which every host calls): `.claude/rules/http-clients.md`. Database queries (precompiled through `[PreCompileQuery]` context methods, never inline LINQ in a DAO; the `ExecuteUpdate` exception): `.claude/rules/ef-compiled-queries.md`. Code navigation rules (CodeGraph + LSP, never Grep for a C# symbol): `.claude/rules/csharp-lsp.md`.
Cross-cutting checks a change must not forget: DTO conversion through Riok.Mapperly (`.claude/rules/dto-mapping.md`), backup/restore declarations for every schema change (`.claude/rules/backup-tables.md`), socket events for every change the API exposes, sent ahead of the frontend and landed in the C# notifier and the Node socket service together (`.claude/rules/socket-events.md`), the Files/Rooms/Forms/AI Agents sections and their subsections (`.claude/rules/files-sections.md`), the OpenSearch index and its SQL fallback (`.claude/rules/full-text-search.md`), every source and every output channel of a filter that decides what a caller may see, checked per caller and entry kind (`.claude/rules/visibility-filters.md`), `[RaisesWebhook]` on every API action that causes a webhook — including one a new publish call in a service starts reaching (`.claude/rules/openapi-endpoint-docs.md` §2.1), zero new build and analyzer warnings (`.claude/rules/build-warnings.md`), what a failed step or a race leaves behind — order of audit and marks, marks released on failure, best-effort publishes in their own catch, an unknown answer never read as a refusal (`.claude/rules/failure-paths.md`), configuration values read through `ConfigurationValueExtensions` with both bounds, a safe default in the code and no way for an optional value to stop the host (`.claude/rules/configuration.md`). Commit messages and PRs carry no AI attribution: `.claude/rules/git-commits.md`. Fixing review findings — the whole class, the other paths of the changed code, a test that fails without the fix: `.claude/rules/fixing-findings.md`. Texts that state behaviour — remarks, comments, spec deltas, task lists, PR and commit messages — each claim and number with the code or test behind it, re-read after every fix round: `.claude/rules/claims.md`.
Endpoint documentation for AI consumers — how `<summary>`/`<remarks>`, DTO property texts and `[SwaggerResponse]` have to read once the OpenAPI documents are used as agent tool definitions: `.claude/rules/openapi-endpoint-docs.md` for the operation (loaded with controllers) and `.claude/rules/openapi-dto-docs.md` for properties and enum members (loaded with DTOs, `ApiModels` and enums). The campaign that works through the backlog of those texts is the `openapi-desc-opt` skill.
Which types may cross the HTTP boundary — only `ApiModels` DTOs, never settings, EF entities, core or foreign classes; separate request/response classes; Mapperly/`*DtoHelper` mapping; moving an old endpoint without changing its JSON: `.claude/rules/api-models.md` (loaded with controllers and `ApiModels`).
## Specs (OpenSpec)

`openspec/specs/<capability>/spec.md` describe what the API does today; `openspec/deviations.md` lists known
defects (security defects are kept out of the repository until a release fixes them). When `openspec/` exists:

- **Before changing what an endpoint does** (a status code, a field, an access rule, a side effect), search
  `openspec/specs/*/spec.md` for its operationId or route and read the requirements that govern it. A change that
  contradicts one goes through the `openspec-propose` skill (its "Breaks current behaviour" section), not
  straight into code; tell the user which requirement it breaks before writing it. A change no requirement
  covers, and a pure refactoring, needs no proposal.
- **Fixing a listed defect**: the `fix-bug` skill updates the register and the spec (its step 6).
- **Keeping the specs current** after a merge, coverage, run marks: the `openspec-maintain` skill.

## Testing

Conventions for writing integration tests (per-test portal, roles, `ApiException` assertions, access-level matrices, class size vs parallelism): `.claude/rules/tests.md`.

- **Framework**: xUnit v3 with `UseMicrosoftTestingPlatformRunner`
- **Assertions**: FluentAssertions
- **Infrastructure**: integration tests do NOT use Testcontainers — they boot the real Aspire AppHost via `Aspire.Hosting.Testing` (`DistributedApplicationTestingBuilder.CreateAsync<Projects.ASC_AppHost>` with the `integration-test` launch profile; the billing suite uses `integration-test-billing`, the letter suite `notify-test`), which provides MySQL/PostgreSQL/RabbitMQ/Redis/OpenSearch containers. The harness is shared: `common/Tests/ASC.Tests.Common` holds `AspireHostFixture<TClients>`, `PortalClientsBase`, `Initializer` and `RawApiClient`; each suite only derives a thin `AspireAppFixture`/`PortalClients` pair in its own `ApiFactories/` folder
- **Fake data**: Bogus
- **DB cleanup**: Respawn
- **Test locations**: `products/*/Tests/`, `web/ASC.Web.Api.Tests/` (Web.Api REST suite) and `common/Tests/` (`ASC.Core.Common.Tests`, `ASC.Notify.Tests` letter suite, `ASC.Data.Backup.Core.Tests`, `ASC.ClearEvents.Tests` audit trail cleanup suite). The remaining projects in `common/Tests/` are legacy (net7/net8), NOT part of `ASC.Tests.slnx`, and are not run by `dotnet test`

```bash
dotnet test ASC.Tests.slnx
```

## Git Workflow

- **Main branch**: `master`
- **Integration branch**: `develop`
- **Branch naming**: `feature/*`, `bugfix/*`
- **Submodules**: 9 total — 8 SDK submodules in `sdk/` plus `common/resources/DocStore` (document templates); use `git submodule update --init` after clone
- **Offer the hand-over check before review**: when a branch you wrote is finished, offer the user the `review-branch` hand-over check (its §0 "Hand-over"): two independent passes on the state that goes to review, the fixes in one batch and their re-check, `scan.py --gate`, and the Hand-over block for the PR description; on a yes, do what its §0 "Hand-over" says. The reviewer checks the block first and sends back a branch whose block fails those checks (`review-branch` §1; a branch without a block is reviewed, with "no hand-over check" in the report header), and runs the same skill, so whatever the check finds would otherwise cost a review round; it costs more than a review (two passes and a re-check), so it is the user's call. Bring the base in first when the scan reports that the base changed rules or review tooling that apply to the branch. After a review round, offer the re-check of your fixes with its fresh reviewer, then the gate and a new block; when a round adds a new kind of state or artifact (a cache, a lock, a fallback, a new class, an OpenSpec change), offer the hand-over check again instead. A remark or question you do not fix goes under "Known limitations" in the PR description, with the decision and the reason. The details and where each sequence ends: `.claude/rules/fixing-findings.md` §6 and §9–§11, which load only with `.cs` files, so read them for a branch without them; texts and commit messages follow `.claude/rules/claims.md`. Offer it for every branch, also one that changes only scripts, skills or rules

## Architecture Notes

- **Modular microservices**: Products (Files, People, AI) are separate deployable units
- **Event-driven**: Event bus abstraction with RabbitMQ/ActiveMQ/Redis backends
- **Multi-database**: MySQL and PostgreSQL with separate migration paths (SaaS vs Standalone)
- **Auth**: JWT Bearer, OpenID Connect, SAML, federated login/SSO
- **Caching**: Redis distributed cache with FusionCache L2 and cache invalidation notifications
- **Health checks**: ASP.NET Core health checks for all infrastructure dependencies
- **Observability**: OpenTelemetry tracing and metrics throughout
