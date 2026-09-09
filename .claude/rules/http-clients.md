---
paths:
  - "**/*.cs"
---

# HTTP Clients

- **Never `new HttpClient()`** — always go through `IHttpClientFactory` (socket exhaustion, stale DNS in singletons).
- **Do NOT register new named clients** without a strong reason. `BaseStartup` (`common/ASC.Api.Core/Core/BaseStartup.cs`) already registers a standard set — pick one:
  - default unnamed — `factory.CreateClient()`;
  - `"customHttpClient"` — `AllowAutoRedirect = false`;
  - `"customHttpClientSslIgnore"` — no redirects + SSL errors ignored;
  - `"customHttpClientNoCookie"` — `UseCookies = false`;
  - `UrlValidator.PinnedHttpClient` / `PinnedHttpClientSslIgnore` — 10s timeout, SSRF protection via `PinnedConnectCallback`; only for flows that validate user-supplied URLs.
- A new named client just to set a timeout is NOT a strong reason. Per-call deadlines: pass a `CancellationTokenSource` timeout. Inside a FusionCache factory: bound waiting with `FactorySoftTimeout`/`FactoryHardTimeout` + fail-safe default instead of the client timeout (see caching rule).