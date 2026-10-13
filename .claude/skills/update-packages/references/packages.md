# Package notes

Stable, non-obvious knowledge per package family — only what neither the code nor the changelog tells
you. Read the sections of the packages being assessed.

Per family, at most four parts:
- **Release notes** — where the real notes are, when that is not the GitHub releases of the nuspec repo.
- **Verification** — checks and tests specific to the package, beyond "build and run the suites".
- **Traps** — facts about our setup a search will not reveal: configs outside the repo, deliberate pins,
  code paths the library silently skips for us.
- **Blockers for the next attempt** — what an assessment found that must change before the update can be
  taken. Undated, no test results. Delete the entry once the update is taken.

What does NOT belong here: assessment or update logs ("assessed X → Y", "taken, tests green") — git history
of `Directory.Packages.props` records that; lists of classes that use a package — they go stale, and the
skill re-checks usage through LSP on every run anyway.

---

## Magick.NET (`Magick.NET-Q8-AnyCPU`)

- **Release notes:** each release lists the ImageMagick security fixes (GHSA) and bundled native library
  versions — check for a newly bundled library (license).
- **Verification:** People `--filter-namespace "*_06_Photos*"`; Files `"*Thumbnails*"`, `"*_03_Rooms.Logos*"`, `"*_03_Rooms.Covers*"`.

## SkiaSharp (+ `NativeAssets.Linux`, `NativeAssets.Linux.NoDependencies`)

- **Release notes:** GitHub releases are summaries; the real notes are
  `https://mono.github.io/SkiaSharp/docs/releases/<version>.html` — read skipped patch versions too.
- **Verification:** no tests cover it. Smoke app with the code of `SkiaSharpWhiteLabelLogoConverter`: render
  an SVG with `<text>` through `Svg.Skia`, re-encode a JPEG, feed garbage bytes (`FromEncodedData` → null).
- **Traps:** `Svg.Skia` pulls `HarfBuzzSharp` on its own version line. Web.Api references both Linux native
  packages; both ship `libSkiaSharp.so` to the same path, and NoDependencies has no fontconfig.

## Refit (`Refit`, `Refit.HttpClientFactory`)

- **Release notes:** release lines are one-liners; read the PRs (`https://api.github.com/repos/reactiveui/refit/pulls/N`) for features.
- **Verification:** `dotnet build common/ASC.Core.Common/ASC.Core.Common.csproj --no-dependencies --no-incremental -p:RefitRequireGeneratedRequests=true`
  fails if a method falls back to reflection. Tests: `common/Tests/ASC.Core.Common.Tests`, Web.Api `"*DocsCloud*"`.

## Swashbuckle.AspNetCore / Microsoft.OpenApi

- **Verification:** generation output changes silently. Build
  `common/Tools/ASC.Api.Documentation/ASC.Api.Documentation.slnx` once with the old pins and once with the
  new, copy `json/*.json` and `common/ASC.AI.Chat/app/generated/openapi-schemas.json` aside after each, diff
  old vs new (never new vs the committed JSON — it lags the code), then `git checkout --` them.
- **Traps:** our document filters key on schema shape (`SwaggerSuccessApiResponseFilter`) — a new shape can
  silently re-wrap responses. `Microsoft.OpenApi` is pinned above Swashbuckle's need for the
  `dotnet-getdocument` tool; read the comment above the pin before moving it.

## Scalar.AspNetCore (+ `.Swashbuckle`)

- **Release notes:** monorepo with shared releases — `--changelog integrations/dotnet/aspnetcore/CHANGELOG.md`.
- **Verification:** served by Web.Studio behind the proxy at `http://localhost:8092/scalar/` (not 5003);
  grep the served HTML for the config keys of new options.
- **Traps:** `x-thirdparty-variant` is unreadable through `ShowExtensions` (raw tree of the twin operation).

## OpenTelemetry (core + contrib instrumentations, `Exporter.InfluxDB`)

- **Release notes:** core — `open-telemetry/opentelemetry-dotnet`, `--tag-prefix core-` (`coreunstable-` for
  Prometheus etc.). Contrib — `--repo open-telemetry/opentelemetry-dotnet-contrib --tag-prefix Instrumentation.<Name>-`;
  bodies often say only "updated core", the details are in `src/<Component>/CHANGELOG.md`.
- **Verification:** `OTEL_FILE_LOGGING=true` test run + `otel_summary.py`.
- **Traps:** EF instrumentation does not recognise our provider `Microting.EntityFrameworkCore.MySql`
  (`db.system=other_sql`, no SQL sanitisation) — MySQL sanitisation fixes do not reach us.

## ZiggyCreatures.FusionCache

- **Verification:** `otel_summary.py` shows the `ZiggyCreatures.Caching.Fusion` scope version; hit/miss metrics per cache name.
- **Traps:** most fixes are tied to options (eager refresh, distributed locker, `Clear`, `SizeLimit`) — check
  whether `ServiceCollectionExtension.AddHybridCache/AddMemoryCache` sets them before claiming a benefit.

## NLog (`NLog`, `NLog.Web.AspNetCore`, `NLog.DiagnosticSource`; extensions `NLog.Targets.*`, `AWS.Logger.NLog`)

- **Release notes:** for a major the project page is the authority (NLog 6:
  `https://nlog-project.org/2025/04/29/nlog-6-0-major-changes.html`).
- **Verification:** smoke app loading a copy of `nlog.config` with the new versions and every extension
  package; after tests check `../Logs/test/` — fresh timestamps, `*.sql.log` format, archive names and compression.
- **Traps:** the effective config is `../buildtools/config/nlog.config`, loaded with `throwConfigExceptions="true"`:
  one unknown attribute stops logging in every service.
- **Blockers for the next attempt (NLog 6):** the config does not load (`enableArchiveFileCompression`
  removed); `${replace:regex=true}` and `regex-matches` need the new `NLog.RegEx` package and
  `${regex-replace}` — which makes the config unloadable on NLog 5, so config and update ship together;
  archives change from `name.MM-dd.N.zip` to `name_MM-dd_NN.log`, uncompressed (decide: `archiveSuffixFormat`,
  legacy `NLog.Targets.ConcurrentFile`, or external compression); nullable warnings in ASC.AI `Program.cs`
  (`GetLogger(typeof(Startup).Namespace ?? AppName)`); extension targets are built for NLog 4.6.8/5.x.

## Autofac (`Autofac.Extensions.DependencyInjection`, `Autofac.Configuration`; core `Autofac` is not pinned)

- **Release notes:** three repositories — `autofac/Autofac`, `autofac/Autofac.Extensions.DependencyInjection`, `autofac/Autofac.Configuration`.
- **Traps:** registrations come from `../buildtools/config/autofac*.json` through Autofac.Configuration. The
  unpinned core resolves to the nuspec minimum of the integration packages, not to the newest patch.

## StackExchange.Redis (+ `StackExchange.Redis.Extensions.*`)

- **Release notes:** docs at `https://seredis.dev`. Extensions keep migration guides in the repo,
  `doc/migration-vNN-to-vMM.md` — read every guide between the old and the new major.
- **Verification:** no isolated Redis tests; a smoke against a throw-away container
  (`docker run -d --rm -p 127.0.0.1:56379:6379 redis:7.0`) can cover the dependent pins (FusionCache backplane,
  DistributedLock.Redis, RedisRateLimiting, Caching.StackExchangeRedis, HealthChecks.Redis, OTel instrumentation).
- **Traps:** `../buildtools/config/redis.json` sets `SyncTimeout` explicitly, so default-timeout changes do not reach us.
- **Blockers for the next attempt (SE.Redis 3):** `Execute("FLUSHALL")` in the AppHost "Clear Cache" dev command
  (`ConnectionStringManager`) needs `AllowAdmin`; RESP3 by default and a separate pub/sub connection (3.4) —
  check the production Redis version and connection limits; new analyzers SER305–SER308 may add warnings;
  the seven dependent pins are built for 2.x and none has a newer version yet — smoke them.

## AWS SDK (`AWSSDK.*`)

- **Release notes:** no GitHub releases per service. Yearly changelog
  `https://raw.githubusercontent.com/aws/aws-sdk-net/main/changelogs/SDK.CHANGELOG.<year>.md`, entries
  `* Core 4.0.102.10` / `* S3 (4.0.104.1)`; a service revision without an entry is only a rebuild against the new Core.
- **Verification:** S3 is not exercised by the integration tests (local storage) — only start-up and unit tests cover it.

## Test infrastructure (`xunit.v3*`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Aspire.Hosting.Testing`, `Moq`)

- **Release notes:** xunit — `https://xunit.net/releases/v3/<version>` (the GitHub release only links there);
  Aspire — `https://aspire.dev/whats-new/aspire-<major>-<minor>/`, breaking-changes section.
- **Verification:** `ASC.Tests.slnx` build (compare its pre-existing warnings by id) and one integration assembly.
- **Traps:** keep `Aspire.Hosting.Testing` on the same version as the AppHost SDK and `Aspire.Hosting.*`.
  Moq is used only by legacy projects outside `ASC.Tests.slnx`.
