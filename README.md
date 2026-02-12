# ONLYOFFICE DocSpace Server

[![Release Notes](https://img.shields.io/github/release/ONLYOFFICE/DocSpace?style=flat-square)](https://github.com/ONLYOFFICE/DocSpace/releases)
[![License](https://img.shields.io/badge/license-AGPLv3-orange)](https://opensource.org/license/agpl-v3)
[![GitHub stars](https://img.shields.io/github/stars/ONLYOFFICE/DocSpace?style=flat-square)](https://star-history.com/#ONLYOFFICE/DocSpace)
[![Open Issues](https://img.shields.io/github/issues-raw/ONLYOFFICE/DocSpace?style=flat-square)](https://github.com/ONLYOFFICE/DocSpace/issues)

This repository contains the **backend** for [ONLYOFFICE DocSpace](https://github.com/ONLYOFFICE/DocSpace) — a room-based collaborative platform for document management.

> For the full product overview, see the [main repository README](https://github.com/ONLYOFFICE/DocSpace#readme).
> For the frontend setup and architecture, see the [client README](https://github.com/ONLYOFFICE/DocSpace-client#readme).

## Table of Contents

- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Quick Start](#quick-start)
  - [Launch Profiles](#launch-profiles)
  - [Development with VSCode](#development-with-vscode)
  - [Clear Aspire Docker Artifacts](#clear-aspire-docker-artifacts)
- [Port Allocation](#port-allocation)
- [Testing](#testing)
  - [Database Migrations](#database-migrations)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [Licensing](#licensing)

## Technology Stack

### Core
- **Language:** C# 14.0
- **Runtime:** .NET 10.0 with ASP.NET Core
- **Orchestration:** .NET Aspire 13.1
- **DI Container:** Autofac 10.0
- **API Versioning:** Asp.Versioning 8.1
- **Object Mapping:** Riok.Mapperly 4.3

### Data & Storage
- **Database:** MySQL 9.5 (primary), PostgreSQL (supported)
- **Caching:** Redis (StackExchange.Redis 2.10, FusionCache 2.5)
- **Search:** OpenSearch
- **Storage:** Abstracted storage layer with multiple providers

### Messaging & Communication
- **Message Brokers:** RabbitMQ 7.2 (primary), Apache Kafka, ActiveMQ, RedisMQ
- **WebSockets:** Socket.IO for real-time updates
- **Webhooks:** Built-in webhook support

### Authentication & Security
- **Authentication:** JWT Bearer, OpenID Connect
- **Federation:** SAML SSO, Active Directory, LDAP
- **Security:** IP filtering, brute force protection, 2FA, rate limiting

### Observability
- **Logging:** NLog 5.5 with ElasticSearch, Syslog, AWS CloudWatch targets
- **Tracing:** OpenTelemetry 1.15 with OTLP exporter
- **Health Checks:** ASP.NET Health Checks UI for all services

### API Documentation
- **Swagger:** Swashbuckle 10.1
- **Interactive Docs:** Scalar 2.12

### AI
- **AI Integration:** Mscc.GenerativeAI.Microsoft 2.9

### Infrastructure
- **Containerization:** Docker 28.5+
- **Reverse Proxy:** OpenResty (Nginx-based)

## Project Structure

This project is organized as a **.NET solution** with a microservices architecture, containing multiple services and shared libraries.

### Solution Overview

```
server/
├── common/                     # Shared libraries and services
│   ├── ASC.AppHost/           # .NET Aspire orchestrator
│   ├── ASC.Api.Core/          # API foundation
│   ├── ASC.Core.Common/       # Core business logic
│   ├── ASC.Common/            # Common utilities
│   ├── ASC.Data.Storage/      # Storage abstraction
│   ├── ASC.Data.Backup.Core/  # Backup core library
│   ├── ASC.Data.Encryption/   # Data encryption
│   ├── ASC.Data.Reassigns/    # User data reassignment
│   ├── ASC.EventBus/          # Event bus (RabbitMQ, ActiveMQ, Redis, Kafka)
│   ├── ASC.FederatedLogin/    # Federation/SSO
│   ├── ASC.Identity/          # Identity management
│   ├── ASC.ActiveDirectory/   # Active Directory integration
│   ├── ASC.IPSecurity/        # IP security
│   ├── ASC.MessagingSystem/   # Internal messaging
│   ├── ASC.Migration/         # Migration core
│   ├── ASC.Resource.Manager/  # Resource management
│   ├── ASC.Socket.IO/         # WebSocket support
│   ├── ASC.SsoAuth/           # SSO authentication
│   ├── ASC.Thumbnails/        # Thumbnail generation
│   ├── ASC.WebDav/            # WebDAV support
│   ├── ASC.Webhooks.Core/     # Webhook support
│   ├── ASC.Analyzers/         # Custom code analyzers
│   ├── services/              # Background services
│   │   ├── ASC.Notify/        # Notification service
│   │   ├── ASC.Studio.Notify/ # Studio notifications
│   │   ├── ASC.Data.Backup/   # Backup service
│   │   ├── ASC.Data.Backup.Worker/  # Backup worker
│   │   ├── ASC.ElasticSearch/ # Search infrastructure
│   │   ├── ASC.ApiSystem/     # API system services
│   │   ├── ASC.TelegramService/    # Telegram integration
│   │   ├── ASC.AuditTrail/   # Audit logging
│   │   └── ASC.ClearEvents/  # Event cleanup
│   └── Tools/                 # Development tools
│       ├── ASC.Migration.Runner/      # DB migration executor
│       ├── ASC.Migrations.Core/       # Migration framework
│       ├── ASC.Api.Documentation/     # API docs generator
│       └── ASC.Data.Stress/           # Stress testing
├── products/                   # Main product modules
│   ├── ASC.Files/             # Document management
│   │   ├── Server/            # Files API (port 5007)
│   │   ├── Worker/            # Files worker (port 5009)
│   │   ├── Core/              # Files business logic
│   │   └── Tests/             # Files tests
│   ├── ASC.People/            # User management
│   │   ├── Server/            # People API (port 5004)
│   │   └── Tests/             # People tests
│   └── ASC.AI/                # AI features
│       ├── Server/            # AI API (port 5157)
│       ├── Worker/            # AI worker (port 5154)
│       └── Core/              # AI business logic
├── web/                        # Web layer
│   ├── ASC.Web.Api/           # Main REST API (port 5000)
│   ├── ASC.Web.Studio/        # Studio backend (port 5003)
│   ├── ASC.Web.Core/          # Shared web core
│   └── ASC.Web.HealthChecks.UI/  # Health monitoring
├── migrations/                 # Database migrations
│   ├── mysql/                 # MySQL migrations
│   │   ├── SaaS/             # SaaS deployment
│   │   └── Standalone/       # Standalone deployment
│   └── postgre/               # PostgreSQL migrations
│       ├── SaaS/
│       └── Standalone/
├── sdk/                        # API SDKs (submodules)
│   ├── docspace-api-sdk-python/
│   ├── docspace-api-sdk-java/
│   ├── docspace-api-sdk-kotlin/
│   ├── docspace-api-sdk-swift/
│   ├── docspace-api-sdk-php/
│   ├── docspace-api-sdk-typescript/
│   ├── docspace-api-sdk-csharp/
│   └── docspace-api-postman-collections/
├── thirdparty/                 # Third-party libraries
├── ASC.Web.sln                 # Main solution
├── ASC.Tests.sln               # Test solution
├── ASC.Migrations.sln          # Migrations solution
└── Directory.Packages.props    # Centralized NuGet versions
```

### Service Responsibilities

#### ASC.Web.Api — Main REST API
Central API gateway for all DocSpace operations: file operations, user management, room management, settings, authentication.
**Port:** 5000

#### ASC.Web.Studio — Studio Backend
Backend for the DocSpace web interface: portal management, white-label customization, plugin management.
**Port:** 5003

#### ASC.Files — Document Management
Core document management: file storage, third-party cloud integrations, document conversion, sharing, permissions, background processing.
**Ports:** 5007 (Server), 5009 (Worker)

#### ASC.People — User Management
User and group management: profiles, settings, import/export, data reassignment.
**Port:** 5004

#### ASC.AI — AI Features
AI-powered functionality: AI assistant integration, background AI processing, generative AI support.
**Ports:** 5157 (Server), 5154 (Worker)

#### ASC.AppHost — Aspire Orchestrator
.NET Aspire host that orchestrates all services and infrastructure: service discovery, Docker container management (MySQL, Redis, RabbitMQ, OpenSearch), development tools (MailPit, DBGate, RedisInsight).

### Event Bus

The backend supports multiple message broker implementations:
- **RabbitMQ** — Primary message broker
- **ActiveMQ** — Alternative broker
- **RedisMQ** — Redis-based messaging
- **Kafka** — High-throughput streaming

### API SDKs

Official API SDKs are available as submodules for multiple languages:
- Python, Java, Kotlin, Swift, PHP, TypeScript, C#
- Postman collections for API exploration

## Getting Started

> **Note:** This is a **development/testing environment**, not suitable for production use.
> For production deployment, see [ONLYOFFICE DocSpace Downloads](https://www.onlyoffice.com/download#docspace-enterprise).

### Prerequisites

| Tool | Version | Verification Command |
|------|---------|---------------------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 | `dotnet --version` |
| [Docker](https://www.docker.com/) | >= 28.5.0 | `docker --version` |

### Quick Start

```bash
# From the DocSpace root
cd server/common/ASC.AppHost
dotnet run --launch-profile development
```

**Access:**
- Aspire Dashboard: http://localhost:15208
- API Documentation (Scalar): http://localhost:8092/scalar/#ascfiles
- DB Gate: http://localhost:56161
- Mailpit: http://localhost:56162

> To run the full application with frontend, see the [client README](https://github.com/ONLYOFFICE/DocSpace-client#readme).

### Launch Profiles

Navigate to `server/common/ASC.AppHost` and choose a profile:

| Profile | Command | Description |
|---------|---------|-------------|
| `development` | `dotnet run --launch-profile development` | Full development setup with all services |
| `frontend-dev` | `dotnet run --launch-profile frontend-dev` | All backend services, skips client build (for separate frontend dev) |
| `preview` | `dotnet run --launch-profile preview` | Minimal Docker-based setup |

> **Note:** Aspire launches multiple services — some run directly, others in Docker containers. Press `Ctrl+C` to stop all services.

### Development with VSCode

**1. Install recommended extensions:**

- [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) — solution explorer, IntelliSense, refactoring
- [.NET Aspire](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-aspire) — Aspire orchestration support (run/debug from VSCode)

**2. Open the solution:**

```bash
code server/
```

VSCode will detect `ASC.Web.sln` automatically via C# Dev Kit. You can also open a filtered solution for faster loading:

- `ASC.Web.sln` — full solution (all projects)
- `ASC.Web.slnf` — filtered solution (faster load)
- `ASC.Tests.sln` — test projects only
- `ASC.Migrations.sln` — migration projects only

**3. Run and debug:**

With the .NET Aspire extension, you can start `ASC.AppHost` directly from VSCode — press `F5` or use the Run and Debug panel (`Ctrl+Shift+D`). The Aspire extension will orchestrate all services and open the Aspire Dashboard automatically.

To debug a specific service, set breakpoints in the code and attach the debugger to the running process via the Aspire Dashboard or the VSCode debugger.

### Clear Aspire Docker Artifacts

Linux/macOS (bash):
```bash
docker ps -a --format '{{.Names}}' | grep -E 'mysql|redis|cache-|rabbitmq|messaging-|opensearch|mailpit|dbgate|redisinsight|onlyoffice-editors|openresty' | xargs -r docker stop && \
docker ps -a --format '{{.Names}}' | grep -E 'mysql|redis|cache-|rabbitmq|messaging-|opensearch|mailpit|dbgate|redisinsight|onlyoffice-editors|openresty' | xargs -r docker rm && \
docker volume prune -f && docker network prune -f
```

Windows (PowerShell):
```powershell
$c = docker ps -a --format '{{.Names}}' | Where-Object { $_ -match 'mysql|redis|cache-|rabbitmq|messaging-|opensearch|mailpit|dbgate|redisinsight|onlyoffice-editors|openresty' }; if ($c) { $c | ForEach-Object { docker stop $_ }; $c | ForEach-Object { docker rm $_ } }; docker volume prune -f; docker network prune -f
```

## Port Allocation

| Service | Port | Description |
|---------|------|-------------|
| OpenResty (reverse proxy) | 8092 | API gateway |
| Aspire Dashboard | 15208 | Backend services monitoring |
| DB Gate | 56161 | Database management UI |
| Mailpit | 56162 | Email testing interface |
| Web API | 5000 | Main REST API |
| Web Studio | 5003 | Studio backend |
| People | 5004 | User management |
| Notify | 5005 | Notification service |
| Studio Notify | 5006 | Studio notifications |
| Files | 5007 | Document management |
| Files Worker | 5009 | File processing |
| API System | 5010 | System APIs |
| Backup | 5012 | Backup service |
| Clear Events | 5027 | Event cleanup |
| Backup Worker | 5032 | Backup worker |
| Telegram | 5050 | Telegram integration |
| AI | 5157 | AI service |
| AI Worker | 5154 | AI processing |
| Identity Authorization | 8080 | Auth service |
| Identity Registration | 9090 | Identity service |
| Socket.IO | 9899 | WebSocket real-time |
| SSO Auth | 9834 | SSO authentication |
| WebDAV | 1900 | WebDAV protocol |
| MySQL | 3306 | Database server |
| Redis | 6379 | Cache server |
| RabbitMQ | 5672, 15672 | Message broker + Management UI |
| OpenSearch | 9200, 9600 | Search engine |

## Testing

The test solution (`ASC.Tests.sln`) contains unit and integration tests for backend services.

```bash
# Run all tests
dotnet test ASC.Tests.sln

# Run tests for a specific project
dotnet test products/ASC.Files/Tests/ASC.Files.Tests.csproj

# Run tests for People
dotnet test products/ASC.People/Tests/ASC.People.Tests.csproj
```

### Database Migrations

Migrations are managed per database engine and deployment type:

```bash
# Run MySQL standalone migrations
cd common/Tools/ASC.Migration.Runner
dotnet run
```

## Troubleshooting

<details>
<summary><b>Port 8092 is already in use</b></summary>

Kill the process using the port:
```bash
# macOS/Linux
lsof -ti:8092 | xargs kill -9

# Windows
netstat -ano | findstr :8092
taskkill /PID <PID> /F
```
</details>

<details>
<summary><b>Docker containers fail to start</b></summary>

1. Check Docker is running: `docker ps`
2. Clear Docker artifacts (see [Clear Aspire Docker Artifacts](#clear-aspire-docker-artifacts))
3. Restart Docker Desktop
4. Try starting backend again
</details>

<details>
<summary><b>dotnet run fails with SDK version error</b></summary>

Ensure you have .NET 10.0 SDK installed. The `global.json` specifies `rollForward: latestMajor`, so any .NET 9.0+ SDK should work, but .NET 10.0 is recommended:
```bash
dotnet --list-sdks
```
</details>

<details>
<summary><b>Backend services won't stop</b></summary>

Force stop all Docker containers:
```bash
docker stop $(docker ps -aq) && docker rm $(docker ps -aq)
```
</details>

For more issues, check our [Issue Tracker](https://github.com/ONLYOFFICE/DocSpace/issues) or [Forum](https://forum.onlyoffice.com/).

## Contributing

### Development Workflow

1. **Fork** the repository
2. **Clone** your fork: `git clone https://github.com/YOUR_USERNAME/DocSpace.git`
3. **Create** a feature branch: `git checkout -b feature/amazing-feature`
4. **Make** your changes
5. **Run** tests: `dotnet test`
6. **Commit** your changes: `git commit -m 'Add amazing feature'`
7. **Push** to your fork: `git push origin feature/amazing-feature`
8. **Open** a Pull Request

### Code Standards

- Follow C# and .NET best practices
- Run `dotnet build` to ensure no compilation errors
- Write tests for new features
- Keep commits atomic and well-described
- Use centralized package versions from `Directory.Packages.props`

### Commit Message Convention

Follow [Conventional Commits](https://www.conventionalcommits.org/):
- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `style:` Code style changes
- `refactor:` Code refactoring
- `test:` Test changes
- `chore:` Build/tooling changes

## Licensing

ONLYOFFICE DocSpace is released under AGPLv3 license. See the LICENSE file for more information.

## Need help for developers? 

Check our [official API documentation](https://api.onlyoffice.com/docspace/).