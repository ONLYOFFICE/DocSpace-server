# ASC.Identity handover

---

## 1. Service overview

**A core DocSpace backend service.** ASC.Identity is the OAuth 2.0 / OpenID Connect
authorization server of ONLYOFFICE DocSpace. It lets third-party apps (and ONLYOFFICE's own, such
as the DocSpace MCP server) act on behalf of a DocSpace user. It ships inside every DocSpace
distribution and has no separate product listing.

- **Source:** `common/ASC.Identity` in the DocSpace server monorepo (Gitea
  `git.onlyoffice.com/ONLYOFFICE/DocSpace-server`, mirrored to GitHub `ONLYOFFICE/DocSpace-server`).
  Pull requests go through Gitea.
- **Stack:** Java 25, Spring Boot 4.1, Spring Authorization Server 7.1, Maven, gRPC, RabbitMQ, Redis,
  MySQL. In SaaS also DynamoDB and CloudWatch.
- **Deployable units:**

  | Unit | Role | Production port | Image / package / service name |
  |---|---|---|---|
  | `registration-container` | REST API for OAuth apps (`/api/2.0/clients`, `/scopes`), consent list | 9090 (gRPC 8888) | image `onlyoffice/apps-identity-api`, package and service `<product>-identity-api`, `ASC.Identity.Registration` |
  | `authorization-container` | `/oauth2/*` and `/.well-known/*` | 8080 (gRPC 9999) | image `onlyoffice/apps-identity-authorization`, package and service `<product>-identity-authorization`, `ASC.Identity.Authorization` |
  | `minified` | both of the above in one process, MySQL only | 8080 | runs inside the all-in-one "standalone" Docker image, with profile `minified,stage` |

  CI images go to `onlyoffice/4testing-apps-identity-*`. A release retags them as
  `onlyoffice/apps-identity-*`.

**How it works with DocSpace (.NET):**

- **Who the user is.** Identity has no users or passwords of its own. DocSpace issues a short-lived
  (5 min) HS256 JWT, the `x-signature`, at `GET /api/2.0/security/oauth2/token`
  (`web/ASC.Web.Api/Api/SecurityController.cs`). It carries user id, tenant id and URL, and the
  admin, guest and public flags. Identity verifies it with a key derived from DocSpace's
  `core:machinekey` (`SPRING_APPLICATION_SIGNATURE_SECRET`, `MachinePseudoKeys`).
- **UI.** The login and consent pages are in the DocSpace client (`/login?type=oauth2`). Identity
  only redirects there (`LoginController`, `AuthorizationConsentController`, `FallbackController`).
- **Routing.** The nginx router of DocSpace (`DocSpace-buildtools/config/nginx/onlyoffice.conf`)
  sends `/oauth2/clients` and `/oauth2/scopes` (prefix rewritten), as well as `/clients` and
  `/scopes`, to registration. It sends `/oauth2/*` and the `.well-known` documents to
  authorization.
- **DocSpace calls identity:**
  - for token introspection on every OAuth bearer request (`common/ASC.Api.Core/Auth/JwtBearerAuthHandler.cs`);
  - for CORS origins (`DynamicCorsPolicyProvider.cs`);
  - to delete the clients of a removed user or portal (`common/ASC.Core.Common/Identity/IdentityClient.cs`).
- **Identity writes to DocSpace tables:** `audit_events` and `login_events`.
- **The database is shared** with DocSpace, and the `identity_*` tables are created by the DocSpace
  EF Core migrations. Backup and restore of DocSpace include them (`IdentityModuleSpecifics.cs`).
- **Locally**, the Aspire AppHost builds and starts both services (`common/ASC.AppHost/Configuration/ProjectConfigurator.cs`, `AddIdentity`).

**Supported versions:**

- Identity is versioned and released together with DocSpace from the same branch. There is no
  separate compatibility matrix: an identity build supports the DocSpace build it ships with.
- **Mixing versions is not supported.** The schema comes from that DocSpace version's migrations,
  and the signature format is shared.
- **MySQL:** 8.0 or newer (tests use 8.0, Docker installs 8.3). MySQL Group Replication is
  supported since 4.1.
- **PostgreSQL** exists only on the .NET side. The Java code is MySQL-only.
- **Java runtime:** 25. Linux packages depend on a Java 25 JRE (OpenJDK, Temurin, Zulu or
  Corretto); Windows uses the `java` found on the host.

## 2. Main features

1. **App registration.** A DocSpace user, or only admins depending on the portal setting, creates
   an OAuth app with name, logo, redirect URIs, allowed origins and scopes. They can then edit it,
   regenerate its secret, enable or disable it, and delete it. Public info about an app needs no
   login.
2. **Authorization code flow.** The app sends the user to `/oauth2/authorize`, the user logs in and
   consents in DocSpace, and the app exchanges the code at `/oauth2/token`. Confidential apps use
   `client_secret_post`. Public apps use PKCE (S256) with no secret.
3. **Token lifecycle.**
   - Refresh rotates the refresh token on every use; the old one stops working.
   - `/oauth2/revoke` works for both confidential and public apps.
   - `/oauth2/introspect` is what DocSpace itself uses.
   - Also available: JWKS, userinfo and discovery documents.
   - Default TTLs: access token 60 min, code 1 min, refresh token 60 days.
4. **Personal access token** grant (`personal_access_token`). A token is issued for the user from
   the `x-signature`, without the redirect.
5. **Consents.** A user lists the apps they have authorized and revokes them.
6. **Clean-up.** Deleting an app, a user or a portal removes its authorizations and consents
   (through the outbox, see section 5). Grants that have been stale for 30 days are purged.
7. **Signing keys** (EC by default) rotate every 30 minutes and are stored in `identity_certs`.
8. **SaaS only:** clients in DynamoDB shared by all regions, region-prefixed codes and tokens,
   cross-region introspection and refresh over RabbitMQ RPC, client limits of 30 per tenant and
   10 per user.

## 3. Build

**Toolchain:**

- JDK 25 and Maven 3.9; the Docker build uses `maven:3.9-eclipse-temurin-25`.
- Docker is needed for integration tests and images.
- GraalVM 25 is needed only for the experimental native build (`make native-binary`), which no
  release uses.
- No private repositories or dependencies: everything resolves from Maven Central, and internal
  modules are part of the reactor.

**Commands** (see `CLAUDE.md` for the full list): `mvn clean install`, `make test`,
`make integration-tests`, `make minified-tests`, `make smoke-tests`, `make docs`.

**Things that are not standard:**

- `fmt-maven-plugin` reformats sources in the `validate` phase. Builds in the Makefile and in
  Docker skip it with `-Dfmt.skip=true`.
- **Run `mvn clean` every time before running jars or `*IT` tests.** The IDE's Eclipse compiler
  (no Lombok) writes into `target/classes`. A non-clean build packages those classes, and they fail
  at runtime with `Unresolved compilation problem`.
- The minified build is a separate Maven profile: `mvn clean install -Pminified -pl minified -am`.
- `protobuf-maven-plugin` sometimes cannot delete
  `common/common-application/target/protoc-dependencies`. Remove that directory by hand and build
  again.
- The production build is in `DocSpace-buildtools`, not here:
  - Docker: the `build-java` stage of `install/docker/build/Dockerfile` and the `java-services`
    group in `build.hcl`.
  - Linux packages: `install/common/packages-build.sh`.
  - Windows: `install/win/` (WinSW service definitions `tools/Identity.*.xml`).

  These run the Maven builds themselves; the Dockerfiles in this folder are used only for local
  work and the Aspire AppHost.
- **Schema changes ship as .NET EF Core migrations** (`migrations/mysql/SaaS` and
  `migrations/mysql/Standalone` in the monorepo).
  - The Flyway runner here (`infrastructure-migration-runner`) is for local development only, and
    each migration has to be mirrored there by hand.
  - Never point Flyway at an EF-built database without `baseline-version` 1.15: V1_1 drops the
    identity tables.

**Where secrets live** (values are never stored in the repository):

| Secret | Docker install | Linux packages | Windows | Local dev |
|---|---|---|---|---|
| Signature secret (= DocSpace `core.machinekey`) | `APP_CORE_MACHINEKEY` in the install `.env` | `SPRING_APPLICATION_SIGNATURE_SECRET` in `/etc/onlyoffice/<product>/systemd.env` | WinSW XML, filled in by the installer | `core:machinekey` from `../buildtools/config` |
| Encryption secret (AES for client secrets and tokens at rest) | `IDENTITY_ENCRYPTION_SECRET` in the install `.env` | `SPRING_APPLICATION_ENCRYPTION_SECRET` in `systemd.env` | WinSW XML | profile fallback `secret` |
| MySQL, Redis, RabbitMQ credentials | install `.env` | `systemd.env` | installer | compose files (dev passwords only) |

- The installers generate both secrets. **The encryption secret must never change after install**:
  stored client secrets and tokens are encrypted with it. On update, `install-Docker.sh` falls
  back to `secret` when none is set, because older installs used the profile default.
- Docker Hub credentials for CI are GitHub Actions secrets (`DOCKERHUB_USERNAME`,
  `DOCKERHUB_TOKEN`) of `ONLYOFFICE/DocSpace-buildtools`.
- **SaaS:** the AWS (DynamoDB, CloudWatch), Redis and RabbitMQ credentials and the production
  `machinekey` are not in any repository. They are held by the SaaS operations (DevOps) team, who
  manage the store and the list of people with access.

## 4. Distribution and release

Identity has no release of its own. It reaches users only as part of DocSpace, and the DocSpace
release process (owned by the release and DevOps engineers, in `ONLYOFFICE/DocSpace-buildtools`)
publishes it. No step of it is specific to identity:

1. A push to `develop`, `release/v*` or `hotfix/v*` of DocSpace-server triggers
   `trigger-action.yml`. It dispatches a build to DocSpace-buildtools (`main-build.yml`), which pushes
   `onlyoffice/4testing-apps-*` images, identity included. A nightly cron does the same.
2. `ci-oci-docker-install.yml` installs the build and runs `tests/smoke/smoke_test.py`.
   `test_oauth_clients` gets a signature, lists scopes, creates a client, reads its public info and
   deletes it through the real nginx router.
3. `release-docker.yaml` (run by hand) retags the tested `4testing` images as release images on
   Docker Hub. `build_packages.yml` builds the DEB and RPM packages, and the Windows installer is
   built from `install/win`.

What the identity maintainer has to do before a DocSpace release:

- Bump `<version>` in `pom.xml` and in all module poms (they must match).
- Run `make docs` if endpoints changed.
- Make sure every Flyway change has an EF migration in both SaaS and Standalone.

## 5. Implicit knowledge

**Why it is built this way:**

- **No user store, only `x-signature`.** DocSpace stays the only place that knows users and
  sessions. That is also why the password encoder is a `NoOpPasswordEncoder`. Client secrets are
  compared after AES decryption.
- **Two services plus gRPC.** Registration owns clients; authorization asks for them over gRPC and
  caches them. In SaaS the client table is global (DynamoDB) while tokens are regional (MySQL per
  region), so tokens carry a region prefix and other regions are reached over RabbitMQ RPC.
- **Minified exists for the all-in-one image.** That image has no RabbitMQ or Redis, so the
  in-process replacements (`InMemoryClientCacheService`, `DirectAuthorizationCleanupPublisher`,
  `DirectAuditMessagePublisher`) stand in for them.
- **`requireProofKey(false)` is deliberate.** PKCE is optional, and public apps (auth method
  `none`) use it. Making it mandatory would break existing confidential apps, which send no
  `code_challenge`.
- **Custom public-client providers.** `PublicClientRefreshTokenAuthenticationProvider` and its
  converter exist because Spring's own provider rejects refresh and revoke requests from public
  clients, which have no `code_verifier` at that point. Reuse of a rotated refresh token is
  rejected, but on purpose the token family is not revoked (see the class javadoc).
- **The client owner is resolved before the transaction** (`AuthorizationService`,
  `ConsentService`). A cache miss means a gRPC call, which must not hold a DB connection.
- **No foreign keys to clients** (dropped in V1_6; in SaaS clients are not in MySQL at all). As a
  result, orphaned authorizations and consents are possible, which is why clean-up goes through an
  outbox:
  - `RabbitPersistableMessagePublisher` waits for the broker confirm and otherwise stores the
    message in `identity_pending_messages`;
  - `PendingMessageRelayService` resends it under ShedLock;
  - a 30-day purge (V1_17) catches the rest.

  Some gaps remain; see section 6.
- **Cache eviction happens before the commit.** Every change to a client evicts it in every
  instance and region first, so a stale cache can never bring back a deleted or disabled client.
  The cache has two tiers (Caffeine 10 s plus Redis 3 min, region-local), with entry versions
  that stop a slow fill from bringing back an evicted value, and an "outage latch" that skips Redis
  while it is down. Do not simplify this without understanding all three.
- **Audit events skip the outbox on purpose.** Losing an audit row is acceptable; blocking a
  request on it is not.
- **404 for both "missing" and "not yours".** Client endpoints answer 404 in both cases, so they do
  not reveal which clients exist.
- **Group Replication.** `V1_9_1__group_replication_keys.sql` and the matching EF fix
  (`Standalone_MigrationContext_Upgrade5`) add primary keys so that MySQL Group Replication accepts
  the tables. Do not "clean up" invisible keys.

**Workarounds, and why they exist:**

- `io.grpc.netty.shaded.io.netty.noUnsafe=true` is set in the three application main classes. It
  keeps the shaded Netty of gRPC away from `sun.misc.Unsafe`. Remove it when gRPC is upgraded (TODO
  in code).
- `jdk.net` is in the `jlink` module list of the local Dockerfiles. Spring Boot 4.1 needs it at
  startup.
- `FallbackScopeAuthorizationCodeRequestConverter`: an authorize request without `scope` gets all
  of the client's scopes. Existing apps rely on this.
- SaaS is detected with `environment.getActiveProfiles()` inside shared beans instead of
  `@Profile` (TODOs in `AuthorizationService`, `TokenIntrospectionAuthenticationProvider`,
  `PrefixedRefreshTokenGenerator`). This works, but keep it in mind when you change profiles.

**Do not touch without a strong reason:**

- the fallback secrets in the profile YAMLs;
- the `x-signature` key derivation;
- the region prefix format of codes and tokens;
- the cache eviction order;
- the outbox and relay timings (the budget is shorter than the ShedLock lease);
- the Flyway baseline.

## 6. Plans and commitments

Known plans:

- **Orphan sweeper.** A periodic reconciliation job for orphaned grants, and clean-up published
  only after the delete commits.
- **Clean-up of debt.** Replace the runtime profile checks with `@Profile` beans, and remove the
  `noUnsafe` workaround after the gRPC upgrade.

Commitments to customers or partners: none are recorded in the code.

- Known consumers to keep working:
  - the DocSpace MCP server, which is bundled in the standalone image;
  - the AI integrations behind the "Claude proto" support added in 1.0.2.
- Ask the DocSpace product manager whether any partner integration depends on specific scopes,
  TTLs or endpoint behaviour.

## 7. Smoke check

**Automated:**

- `make smoke-tests` runs `minified/.../OAuthSmokeIT` against the minified build and a MySQL
  container, in about 1 minute. It covers:
  - health, discovery documents, JWKS, scopes;
  - client registration and public info;
  - the confidential code flow: token, active introspection, refresh rotation and reuse rejection,
    revoke;
  - the PKCE flow of a public client: a wrong verifier is rejected, then token and refresh without
    a secret, then revoke;
  - the consent list;
  - deleting a client invalidates its tokens.
- The DocSpace-buildtools smoke test (`test_oauth_clients`) covers the same client API through the
  real router on every CI install.

**Manual checklist** (things only a real DocSpace shows), on a fresh install and after an update:

1. In DocSpace, open Settings > Developer Tools > OAuth. Create an app with a logo, two redirect URIs
   and `files:read` + `openid`. It appears in the list with its logo.
2. Open
   `https://<portal>/oauth2/authorize?response_type=code&client_id=<id>&redirect_uri=<uri>&scope=files:read`
   in a browser while logged out. The DocSpace login page opens, then the consent page with the
   app's name and scopes.
3. Approve. The browser lands on the redirect URI with `?code=`. Exchange the code with curl at
   `https://<portal>/oauth2/token` (form: `grant_type`, `code`, `redirect_uri`, `client_id`,
   `client_secret`).
4. Call `https://<portal>/api/2.0/files/@my` with `Authorization: Bearer <access_token>`. It
   answers 200 with the user's folder (this proves DocSpace's introspection).
5. In the profile's authorized apps list the app is shown. Revoke it there, then repeat step 4
   with a new token from the old refresh token: refresh fails.
6. Disable the app in Developer Tools. A new authorize attempt shows an error on the login page.
   Enable it again.
7. Delete the app. Its public info URL (`/api/2.0/clients/<id>/public/info`) answers 404.
8. `https://<portal>/.well-known/openid-configuration` answers 200 with the portal's URLs.
9. After an update only: an app created before the update still works with its old secret. This
   proves the encryption secret was kept.
10. Check the logs for errors: `ASC.Identity.Registration.log` and
    `ASC.Identity.Authorization.log` in the DocSpace log directory (`/var/log/onlyoffice` in Docker),
    or `docker logs onlyoffice-identity-api` and `docker logs onlyoffice-identity-authorization`.
