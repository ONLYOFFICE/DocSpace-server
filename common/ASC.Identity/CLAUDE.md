# CLAUDE.md - ASC.Identity

The OAuth 2.0 / OpenID Connect server of ONLYOFFICE DocSpace. Java 25, Spring Boot 4.1, Spring
Authorization Server 7.1, Maven multi-module. It lives inside the DocSpace server monorepo, but it
builds and tests on its own. The monorepo's .NET rules (`../../CLAUDE.md`, `../../.claude/rules/`)
do not apply to Java code here, with two exceptions: `git-commits.md` (no AI attribution) and
`failure-paths.md` (order of side effects). Background, history and open problems: `HANDOVER.md`.

## Services

| Module | What | HTTP | gRPC |
|---|---|---|---|
| `registration/` | OAuth app (client) CRUD, consents list, scopes | 9090 in production (8080 is the code default) | 8888 |
| `authorization/` | `/oauth2/*`: authorize, token, introspect, revoke, jwks, userinfo, discovery | 8080 in production (9090 is the code default) | 9999 |
| `minified/` | both services in one process, MySQL only, no RabbitMQ or Redis (`-Pminified`) | 8080 | in-process |
| `common/` | shared domain, crypto, cache, messaging, gRPC protos | | |
| `infrastructure/` | shared YAML config (`infrastructure-configuration`), Flyway runner (dev only), k6 scripts | | |

Production sets the ports through `SERVER_PORT`. `docker-compose-server.yml` uses the code
defaults, so its ports are the reverse of production.

Profiles: `server` (on-premises, clients in MySQL), `saas` (clients in DynamoDB, Redis caches,
eu/us regions), `minified`. Each is combined with a logging profile: `dev`, `stage`, `prod`.

## Commands

```bash
mvn clean install -DskipTests -Dfmt.skip=true          # build all (no minified)
mvn clean install -Pminified -pl minified -am -DskipTests -Dfmt.skip=true
make test                 # unit tests
make integration-tests    # *IT with Testcontainers (Docker required): MySQL, RabbitMQ, Redis, LocalStack
make minified-tests       # *IT of the minified build
make smoke-tests          # OAuthSmokeIT: the main OAuth scenarios end to end, about 1 min
make docs                 # regenerate docs/identity-*-openapi.json
make all                  # Docker images through Dockerfile.shared + Dockerfile.service (local only)
docker compose -f docker-compose-common.yml -f docker-compose-server.yml up   # local stack
```

Integration tests run only with `-DRUN_INTEGRATION_TESTS=true`; without it they are skipped.

## Rules

- **Always `mvn clean` before running jars or `*IT` tests.** The IDE's Java language server (Eclipse
  compiler, no Lombok) writes broken classes into `target/classes`. An incremental build packages
  them, and they fail at runtime with `Unresolved compilation problem`. The IDE also reports
  "log cannot be resolved" and "builder() is undefined" errors; that is the same noise, so trust
  Maven.
- **The `fmt` plugin reformats sources on every build** (google-java-format, `validate` phase).
  Pass `-Dfmt.skip=true` when you do not want files touched. Run it before you hand code back.
- **A schema change ships as a .NET EF Core migration**, in
  `../../migrations/mysql/{SaaS,Standalone}` (see the monorepo `db-migrations` skill). Production
  applies only those. The Flyway files in
  `infrastructure/infrastructure-migration-runner/src/main/resources/migration/` mirror them for
  local development. Never point Flyway at an EF-built database without `baseline-version` 1.15:
  the default baseline of 1 runs V1_1, which drops all identity data.
- A new or changed `identity_*` table also needs the backup declaration in
  `../../common/ASC.Data.Backup.Core/Tasks/Modules/IdentityModuleSpecifics.cs`.
- Test schemas come from a hand-written `src/test/resources/init.sql` in each module, not from the
  migrations. Update them with every schema change.
- Never change the fallback secrets in the profile YAMLs (`secret`, `1123askdasjklasbnd`). Older
  installs encrypted their data with them, and the installer falls back to `secret` on update.
- Never change `SPRING_APPLICATION_SIGNATURE_SECRET` handling: it must equal DocSpace's
  `core:machinekey`, which is how the `x-signature` JWT from DocSpace is verified.
- A new JDK module used at runtime must be added to the `jlink --add-modules` list in
  `Dockerfile`, `Dockerfile.shared` and `Dockerfile.minified`. Otherwise the local and Aspire
  images fail at startup with `ClassNotFoundException`. Production images use a full Temurin JRE
  and are not affected.
- Every client change must evict caches in every instance and region before the change commits.
  Understand the two-tier cache (Caffeine plus Redis, entry versions, outage latch) before touching
  `RedisClientCacheService`, `CompositeCache` or the eviction publishers.
- Clean-up after removing a client, user or tenant goes through the outbox
  (`identity_pending_messages`, `RabbitPersistableMessagePublisher`). Do not publish such events
  with plain `RabbitTemplate.send`.
- Endpoint docs (`docs/*.json`) are generated. Change the annotations, then run `make docs`.

## Where to look

- Security filter chain: `authorization/authorization-application/.../configuration/authorization/AuthorizationServerConfiguration.java`
- Caller authentication (`x-signature` from DocSpace): `BasicSignatureAuthenticationFilter`, `SignatureAuthenticationProvider` in both services
- Client store: `ClientCommandRepositoryDomainAdapter` (MySQL), `CoreDynamoClientRepository` (DynamoDB, saas)
- Token persistence and grant clean-up: `AuthorizationService`, `ConsentService`, `AuthorizationMessagingCleanupListener`
- Production packaging, nginx routes, smoke test: the `ONLYOFFICE/DocSpace-buildtools` repository
