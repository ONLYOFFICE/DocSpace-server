---
paths:
  - "common/ASC.AI.Chat/app.ts"
  - "common/ASC.AI.Chat/app/createApp.ts"
  - "common/ASC.AI.Chat/server.js"
  - "common/ASC.AI.Chat/app/routes.ts"
  - "common/ASC.AI.Chat/app/cors.ts"
  - "common/ASC.AI.Chat/app/requestContext.ts"
  - "common/ASC.AI.Chat/app/controllers/**/*.ts"
  - "common/ASC.AI.Chat/package.json"
---

# ASC.AI.Chat as an Express application

The service follows the production guidance of the Express documentation: "Production Best
Practices: Security", "Production best practices: performance and reliability", "Error Handling",
"Health Checks and Graceful Shutdown" and "Express behind proxies" (expressjs.com). Each section
says what that guidance asks, how it applies to a JSON API that sits behind the portal's nginx, and
what a change to `app/createApp.ts`, `routes.ts` or a controller has to keep. §8 lists where the code does not
meet it yet. Cite this file as `[ts-ai-express.md §N]`.

## 1. Errors reach one place

- The service runs Express 4 (`package.json`). Express 4 does not pass a rejected promise of an
  `async` handler to `next`: the request hangs and the rejection is unhandled. Every handler and
  every async middleware is wrapped in `asyncHandler` (`controllers/_helpers.ts`); ESLint
  (`no-misused-promises`) refuses a bare `async` function given to `router.*` or `app.use`.
- An error handler takes four arguments `(err, req, res, next)` and is registered last, after the
  routes and the 404 handler. It answers the same `{ error }` JSON as `asyncHandler` and is what
  catches an error raised before a handler runs: a body that does not parse, a body over the limit.
  The one exception is the 413 of a body over the limit, sent as plain text: a stream client reads
  every JSON line of an answer as a frame, and the spec's "no frame is streamed" for a refused
  `send-with-stream` is pinned by `ChatHiddenFailureTests` in `ASC.AI.Tests`.
- **After the headers are sent** a status can no longer say that the answer failed. A stream writes
  its error frame (`streamNdjson`, `streamOpenAiSse`); anything else passes the error on with
  `next(err)`, so Express closes the connection and the client sees a failed transfer. Ending the
  response cleanly makes a truncated body look complete.
- Never listen for `uncaughtException` to keep the process running: after it the state is unknown.
  The process manager restarts the service; the code's job is to leave no promise unhandled
  (`no-floating-promises`).
- Guard: a request with a broken JSON body answers 400 as JSON and one over the limit 413 in plain text, with
  no stack (`app/__tests__/app.pipeline.test.ts`); `errorHandler` itself in
  `controllers/__tests__/errorHandler.test.ts`. An unknown path answers 404 `{ error }` as well.

## 2. The order of middleware is part of the contract

- The order in `createApp` (`app/createApp.ts`) and `registerRoutes`: body parsers, CORS, the request context, the open
  routes (`/isLife`, `/health`, `/openapi.json`, `/docs`), the authentication gate, the routes, the
  404 handler, the error handler. A middleware added in the wrong place runs for requests it must
  not see, or after the answer was sent.
- CORS comes before the gate: a preflight carries no credentials. The gate comes before every route
  that does work: it answers 401 for a request with neither the session cookie nor an
  `Authorization` header.
- A body parser is chosen for the route, and its limit is stated where it is chosen. The default
  limit (100 kB) is the rule; a route that needs more says why in a comment, as `text-to-docx` does,
  and a route that relays raw bytes is left unparsed. Do not raise the default, and do not add a
  parser no route reads.
- The parser is picked by the path, the route by Express's own matching (case-insensitive, with or
  without a trailing slash). Compare paths the way the router does, or the bigger limit and the raw
  body apply to one spelling of the path only.
- `app.ts` only starts what `createApp()` builds: telemetry, the storage, the port. Middleware and
  routes go into `createApp`, so that a test gets the same application through supertest.
- Guard: a test through the real app for each: 401 without credentials, the open routes without
  them, a preflight, the limit of each parser, an unknown path (`app/__tests__/app.test.ts` has the
  first, second and last).

## 3. Synchronous work stays out of a request

- Synchronous `fs` and `child_process` calls, and CPU-heavy loops over a request's data, block every
  other request of the process, streams included. They are allowed at module load only (reading the
  config, creating the log directory).
- A long reply is streamed, never built in memory. A write to a stream respects backpressure: when
  `res.write` returns `false`, wait for `drain` before pulling the next chunk.
- A stream stops when the client goes away: the close of the response aborts the upstream call and
  the generator behind it.

## 4. Behind the proxy

- TLS, compression and response caching belong to the portal's nginx, not to this process. A stream
  must not be buffered or compressed on its way: it sets `X-Accel-Buffering: no`, and every `GET`
  answers `Cache-Control: no-store` (`routes.ts`), since the answers are per user and per entity.
- The address the caller used (`req.protocol`, `req.hostname`, `req.ip`) is read through Express
  with `trust proxy` set to the proxies in front of the service. `Origin`, `Referer` and
  `X-Forwarded-*` are sent by the caller unless the proxy overwrites them: never take the portal's
  address from them by hand.
- The service answers JSON and one HTML page (`/docs`). Response headers that harden a browser
  (`helmet`) and the removal of `X-Powered-By` (`app.disable("x-powered-by")`) are set once in
  `createApp`, not per route.

## 5. The process

- `NODE_ENV=production` in production: `server.js` sets it unless the host did. Express then keeps
  stack traces out of its own error pages. Code never branches on `NODE_ENV` to change what a route
  does.
- **Graceful shutdown**: on `SIGTERM` the server stops accepting connections (`server.close`), the
  requests in flight finish or are aborted after a bounded wait, buffered telemetry and logs are
  flushed, then the process exits. One handler owns the sequence.
- **Health**: `/isLife` and `/health` answer without credentials and without calling an upstream,
  so that an outage of the AI service does not restart this one.
- The process is one of several and can be restarted at any moment: nothing a later request needs
  lives only in its memory (`ts-ai-failure-paths.md` §8).

## 6. Logging

- `winston` (`log.js`) for the service's own lines, `morgan` for the access log; no `console.*`
  (`ts-ai-style.md`, State and logging). An error is logged once, where it is handled: the boundary
  logs a 5xx with its details and answers without them.
- The access log writes the URL with its query string: never put a secret into a query parameter.

## 7. Dependencies

- Express, `body-parser` and the other middleware stay on a maintained release.
  `yarn npm audit --environment production` is run before a change of `package.json` is handed
  over, and an advisory it adds is fixed or named in the PR description with the reason.
- A new middleware package is a decision, not a convenience: it runs for every request of every
  portal. Say in the commit what it does that Express or the code in place cannot.
- An upgrade to Express 5 changes §1: it forwards rejected promises by itself. `asyncHandler` stays,
  as it also maps the error to a status.

## 8. Where the code stands (2026-11-23)

Read from `app.ts`, `app/createApp.ts`, `app/routes.ts`, `controllers/_helpers.ts`, `app/telemetry/sdk.ts` and
`package.json` on that date. A change that closes a line removes it here in the same commit.

| Guidance | Now |
|---|---|
| §1 `next(err)` after the headers are sent | `asyncHandler` calls `res.end()` |
| §2 no parser without a reader | `bodyParser.urlencoded` runs on every parsed route (not on the raw ones); a handler reads `req.body` whatever parser made it, so a form posted to a JSON route is served |
| §4 `trust proxy`, `helmet` | not set |
| §5 graceful shutdown | `SIGTERM` flushes telemetry and exits; with telemetry off there is no handler |
| §7 maintained releases | `express` 4.17.1 and `body-parser` 1.19.0 are pinned |
