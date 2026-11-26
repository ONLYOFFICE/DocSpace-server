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
- **A standard client is registered in `AddStandardHttpClients` only**, never in one startup. Every service host calls it — `BaseStartup`, `BaseWorkerStartup`, and the hosts that derive from neither (ApiSystem, Site.Core) — because shared code (`ASC.Files.Core`, `ASC.FederatedLogin`) runs in all of them; a new service host that derives from neither calls it too. Two hosts do not: Web.HealthChecks.UI, which references no DocSpace project, and the `ASC.Migration.Runner` tool. The same holds for the document service clients (`AddDocumentServiceHttpClient`) in every host that reaches `DocumentServiceConnector` or `DocumentServiceLicense`. `IHttpClientFactory` does not refuse a name that was never registered: it returns a client with the default handler, which keeps cookies. That is how `WebDavStorage` in Files.Worker replayed one Nextcloud account's session for another, after bug 83399 had been fixed in the API hosts only. `StandardHttpClientsTests` pins the handler settings of every name. It does not check that a host calls `AddStandardHttpClients`. In the workers, only `customHttpClientNoCookie` is covered, for Files.Worker, by `ThirdPartyWorkerSessionIsolationTests`; no test notices another name missing from a worker. `HostHttpClientRegistrationTests` checks that People and AI.Worker register the document service clients; the hosts that register them in `ConfigureServices` (ApiSystem) are not checked. Do not drop the call from a startup on the strength of the unit tests.
- A new named client just to set a timeout is NOT a strong reason. Per-call deadlines: pass a `CancellationTokenSource` timeout. Inside a FusionCache factory: bound waiting with `FactorySoftTimeout`/`FactoryHardTimeout` + fail-safe default instead of the client timeout (see caching rule).
- **A body from outside is read with a limit and a deadline.** Refuse a `Content-Length` above the
  limit before reading, read at most the limit, and read only the prefix a check needs (a file
  signature) before copying the rest. With `HttpCompletionOption.ResponseHeadersRead`,
  `HttpClient.Timeout` ends when the headers arrive, so the body is read under the same per-call
  `CancellationTokenSource`. Never copy a whole body into a `MemoryStream` to look at its first bytes.
  Seen in review: a template was copied whole into memory, and twice more for a PDF, before a check of
  its first kilobyte.

## Checklist

- No `new HttpClient()`; every client comes from `IHttpClientFactory` (bullet 1)
- The client is one of the standard names; no new named client without a strong reason (bullet 2)
- `UrlValidator.PinnedHttpClient` is used only in flows that validate a user-supplied URL (bullet 2)
- A standard client is registered only in `AddStandardHttpClients`, never in one startup (bullet 3)
- A new service host calls `AddStandardHttpClients`, and `AddDocumentServiceHttpClient` where needed (bullet 3)
- A per-call deadline is a `CancellationTokenSource` timeout, not a new named client (bullet 4)
- A body from outside is read under the call's deadline, with a byte limit, prefix first (bullet 5)
