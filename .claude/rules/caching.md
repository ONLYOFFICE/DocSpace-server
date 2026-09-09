---
paths:
  - "**/*.cs"
---

# Caching

## Rule #1: Always use FusionCache — never hand-roll caches
Do NOT suggest or implement caching via `Interlocked`, static `ConcurrentDictionary`, `Lazy<T>` singletons, or raw `IMemoryCache`/`IDistributedCache`. This project standardizes on FusionCache; hand-rolled caches have no cross-node invalidation, no telemetry, and no expiration discipline.

## Two FusionCache instances — don't mix them up
- **Default `IFusionCache`** (inject directly): L1 memory + **L2 Redis** + backplane. Distributed — visible to all nodes. Used by `SettingsManager`, `FileTrackerHelper`, `CspSettingsHelper`.
- **Named `"memory"`** (inject `IFusionCacheProvider`, call `.GetMemoryCache()`): **L1-only** (no Redis reads/writes), but still wired to the Redis backplane so `Remove`/`RemoveByTagAsync` invalidate across nodes. Used by most `Cached*Service` classes in `common/ASC.Core.Common/Caching/`.
- Registration: `common/ASC.Api.Core/Extensions/ServiceCollectionExtension.cs` (`AddHybridCache`, `AddMemoryCache(connection)`). Note: no-arg `services.AddMemoryCache()` is Microsoft's `IMemoryCache` — a different thing.

## Rules
- **Always pass an explicit duration** to `GetOrSet`/`Set` — the configured default is `TimeSpan.MaxValue` (never expires).
- **Tenant scoping is manual**: every cache key and tag must embed `tenantId`. There is no ambient prefix; omitting it leaks data across tenants.
- **Tags come from `CacheExtention`** (`common/ASC.Common/Caching/CacheExtention.cs`) — the central tag registry (`user-{tenant}-{id}`, `settings-{tenant}-{name}`, ...). Never build tag strings inline; add new helpers there.
- **Keys** are built by static `Get*CacheKey` methods on the owning service (concatenation without separators: `tenant + "users" + userId`).
- **Canonical pattern** — cache-aside wrapper: `Cached*Service` wraps the EF service; reads use `GetOrSetAsync(key, factory, duration, [tags])`, writes call the inner service then `RemoveByTagAsync(tag)`. Reference examples: `CachedUserService.cs`, `CachedTenantService.cs`, `CachedQuotaService.cs` in `common/ASC.Core.Common/Caching/`.

## Cross-node invalidation: `ICacheNotify<T>`
- Pub/sub notifications (Redis pattern channel / RabbitMQ fanout / Kafka / in-memory fallback — picked by config in `AddCacheNotify`). Distinct from the event bus (`ASC.EventBus.*`), which is for domain integration events.
- `T` must be `[ProtoContract]` with explicit `[ProtoMember(n)]` numbers; contracts live in `common/ASC.Core.Common/protos/`. Renumbering members is a wire-breaking change during rolling deploys.
- Subscribe only from `[Singleton]` constructors/startup — `Subscribe` blocks (sync-over-async) and handlers cannot be selectively removed.
- Two serializers coexist: cache **values** are System.Text.Json (FusionCache L2), cache **notifications** and the event bus are protobuf.

## Config & no-Redis mode
- Redis config is outside this repo: `buildtools/config/redis.json` (loaded via `pathToConf`); env vars override it.
- With `Redis:Enabled=false` there is no backplane and `MemoryCacheNotify` is process-local — code must not assume cross-node cache consistency, and `IConnectionMultiplexer` may be `null`.
