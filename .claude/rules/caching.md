---
paths:
  - "**/*.cs"
---

# Caching

## Rule #1: Always use FusionCache — never hand-roll caches
Do NOT suggest or implement caching via `Interlocked`, static `ConcurrentDictionary`, `Lazy<T>` singletons, or raw `IMemoryCache`/`IDistributedCache`. This project standardizes on FusionCache; hand-rolled caches have no cross-node invalidation, no telemetry, and no expiration discipline.

## Two FusionCache instances — don't mix them up
- **Default `IFusionCache`** (inject directly): L1 memory + **L2 Redis** + backplane. Distributed — visible to all nodes. Used by `SettingsManager`, `FileTrackerHelper`, `CspSettingsHelper`.
- **Named `"memory"`** (inject `IFusionCacheProvider`, call `.GetMemoryCache()`): **L1-only** (no Redis reads/writes), wired to the Redis backplane **but with `SkipBackplaneNotifications = true` by default** — a factory fill or a plain `SetAsync` stays local. An L1-only cache that notified on every fill made every other process evict and re-read the same key from the DB (tenant -1 settings like `BaseDomain` were re-read ~2.7k times per service per test run, 2026-09-03). Invalidations that other nodes must see go through `CacheExtention.RemoveAndNotifyAsync` / `RemoveByTagAndNotifyAsync` — **never call bare `RemoveAsync`/`RemoveByTagAsync` on the memory cache**, they would only clear the local node. Used by most `Cached*Service` classes in `common/ASC.Core.Common/Caching/`.
- Registration: `common/ASC.Api.Core/Extensions/ServiceCollectionExtension.cs` (`AddHybridCache`, `AddMemoryCache(connection)`). Note: no-arg `services.AddMemoryCache()` is Microsoft's `IMemoryCache` — a different thing.

## Which instance: who obtains the value, who reads it
- **`"memory"`** fits a value every process computes for itself from a source all processes share (the DB, stored settings) and can compute again after a restart: losing it costs one read, never a different decision.
- **The default distributed `IFusionCache`** (or a stored setting, when the value must outlive the cache) is required when one process obtains the value and another reads it (a worker asks an external service, an API process decides on the answer), when the value must survive a restart, or when a decision falls back on the "last known answer". In `"memory"` the other processes get at most a notification that their entry is stale, never the value, and a restart forgets it. Seen in review: a setup wizard in the API process decided by a "last known answer" of an external service that only the background worker had received.
- **A failure keeps the known value.** A refresh that gets an unknown answer (timeout, unreadable body) leaves the entry alone; a known answer is written with `SetAsync`, not by removing the entry. `RemoveAsync` drops the entry with its fail-safe copy, so the next read has nothing to fall back on (seen in review: a daily worker pass that met a silent external service erased the last good answer, and the next anonymous request decided as if there had never been one).

## Rules
- **Always pass an explicit duration** to `GetOrSet`/`Set` — the configured default is `TimeSpan.MaxValue` (never expires).
- **Tenant scoping is manual**: every cache key and tag must embed `tenantId`. There is no ambient prefix; omitting it leaks data across tenants.
- **Tags come from `CacheExtention`** (`common/ASC.Common/Caching/CacheExtention.cs`) — the central tag registry (`user-{tenant}-{id}`, `settings-{tenant}-{name}`, ...). Never build tag strings inline; add new helpers there.
- **Keys** are built by static `Get*CacheKey` methods on the owning service (concatenation without separators: `tenant + "users" + userId`) — a fixed key of one value per installation (tenant `-1`) too, not a key field. A format string that such a method fills is fine. Older code keeps several fixed keys in fields (`*CacheKey` constants and static fields) and fills some key formats inline; this rule does not ask for them to be rewritten, but a key you add or change goes through a method.
- **Canonical pattern** — cache-aside wrapper: `Cached*Service` wraps the EF service; reads use `GetOrSetAsync(key, factory, duration, [tags])`, writes call the inner service then `RemoveByTagAsync(tag)`. Reference examples: `CachedUserService.cs`, `CachedTenantService.cs`, `CachedQuotaService.cs` in `common/ASC.Core.Common/Caching/`.

## Cross-node invalidation: `ICacheNotify<T>`
- Pub/sub notifications (Redis pattern channel / RabbitMQ fanout / Kafka / in-memory fallback — picked by config in `AddCacheNotify`). Distinct from the event bus (`ASC.EventBus.*`), which is for domain integration events.
- `T` must be `[ProtoContract]` with explicit `[ProtoMember(n)]` numbers; contracts live in `common/ASC.Core.Common/protos/`. Renumbering members is a wire-breaking change during rolling deploys.
- Subscribe only from `[Singleton]` constructors/startup — `Subscribe` blocks (sync-over-async) and handlers cannot be selectively removed.
- Two serializers coexist: cache **values** are System.Text.Json (FusionCache L2), cache **notifications** and the event bus are protobuf.

## Config & no-Redis mode
- Redis config is outside this repo: `buildtools/config/redis.json` (loaded via `pathToConf`); env vars override it.
- With `Redis:Enabled=false` there is no backplane and `MemoryCacheNotify` is process-local — code must not assume cross-node cache consistency, and `IConnectionMultiplexer` may be `null`.
