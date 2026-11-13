---
paths:
  - "**/*.cs"
---

# HTTP Clients

- **Never `new HttpClient()`** — always go through `IHttpClientFactory` (socket exhaustion, stale DNS in singletons).
- **Do NOT register new named clients** without a strong reason. `AddStandardHttpClients` (`common/ASC.Api.Core/Extensions/ServiceCollectionExtension.cs`) already registers a standard set — pick one:
  - default unnamed — `factory.CreateClient()`;
  - `"customHttpClient"` — `AllowAutoRedirect = false`;
  - `"customHttpClientSslIgnore"` — no redirects + SSL errors ignored;
  - `"customHttpClientNoCookie"` — `UseCookies = false`;
  - `UrlValidator.PinnedHttpClient` / `PinnedHttpClientSslIgnore` — 10s timeout, SSRF protection via `PinnedConnectCallback`; only for flows that validate user-supplied URLs.
- **A standard client is registered in `AddStandardHttpClients` only**, never in one startup. `BaseStartup` and `BaseWorkerStartup` both call it, because shared code (`ASC.Files.Core`, `ASC.FederatedLogin`) runs in the API hosts and in the workers alike. `IHttpClientFactory` does not refuse a name that was never registered: it returns a client with the default handler, which keeps cookies. That is how `WebDavStorage` in Files.Worker replayed one Nextcloud account's session for another, after bug 83399 had been fixed in the API hosts only. `StandardHttpClientsTests` pins the handler settings of every name. It does not check that a host calls `AddStandardHttpClients`. In the workers, only `customHttpClientNoCookie` is covered, for Files.Worker, by `ThirdPartyWorkerSessionIsolationTests`; no test notices another name missing from a worker. Do not drop the call from a startup on the strength of the unit test.
- A new named client just to set a timeout is NOT a strong reason. Per-call deadlines: pass a `CancellationTokenSource` timeout. Inside a FusionCache factory: bound waiting with `FactorySoftTimeout`/`FactoryHardTimeout` + fail-safe default instead of the client timeout (see caching rule).