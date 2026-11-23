---
paths:
  - "common/ASC.AI.Chat/**/*.ts"
  - "common/ASC.AI.Chat/eslint.config.js"
---

# TypeScript Conventions (ASC.AI.Chat service)

The Node service in `common/ASC.AI.Chat` is a thin HTTP layer: the engine and its DTOs come from
`@onlyoffice/ai-chat/core`, the data lives in the .NET AI, Files and People services. What a failed
step leaves behind is in `ts-ai-failure-paths.md`, the Express application itself (middleware order,
the error handler, shutdown, the proxy in front) in `ts-ai-express.md`, tests in `ts-ai-tests.md`; `claims.md` covers
the texts in `app/openapi.ts`, `app/apiCatalog.ts` and `scripts/schema/` like any other text that
states behaviour. Cite this file as `[ts-ai-style.md, <section>]`.

## Checks

- `yarn check` in `common/ASC.AI.Chat` runs `typecheck`, `lint`, `format:check` and the tests with
  their coverage thresholds (`ts-ai-tests.md` §8); a change
  is handed over with it green. `.github/workflows/ai-chat.yml` runs the same and fails when
  `yarn openapi` changes `app/generated`.
- **ESLint** (`eslint.config.js`) holds one rule per class of defect, no style rule. Code that
  predates it is counted per file and rule in `eslint-suppressions.json`. A new violation is fixed,
  not added to the count: never run `eslint --suppress-all`, and never silence a rule with
  `eslint-disable`, `@ts-ignore` or `@ts-expect-error`. After fixing a listed violation run
  `yarn eslint . --prune-suppressions` and commit the smaller file.
- **Prettier** owns formatting (`.prettierrc.json`): `yarn prettier --write <the files you changed>`.
  `templates/` is content handed to the model and stays out of it (`.prettierignore`).
- No build step: the service runs through `tsx`. `app/generated/openapi-schemas.json` is written by
  `yarn openapi` and committed, never edited by hand.

## Naming

- **Files**: camelCase (`httpClient.ts`, `profilesController.ts`)
- **Controllers**: `*Controller`, a const object of `asyncHandler`-wrapped methods, not a class
- **Storage**: `Http*Storage` classes implementing an `@onlyoffice/ai-chat/core` interface
- **Types**: PascalCase; wire shapes `*Dto`; errors `*Error`. **Constants**: `UPPER_SNAKE_CASE`

## Modules

- ESM only. **Relative imports carry `.js`** although the source is `.ts` (`"../log.js"`).
- **`import type`** for type-only imports, in a statement of its own; `verbatimModuleSyntax` makes
  the compiler refuse anything else.

## Types

- Strict everywhere (`noUncheckedIndexedAccess`): indexing yields `T | undefined`, handle it.
- **Never `any`**, never `!`. External input (a request body, an upstream answer) is `unknown`
  until the helpers of `app/narrow.ts` have checked it.
- **A cast is not a check.** `args.x as string` on a field of `req.body` or of
  `unpackPositional(...)` is refused by ESLint in `app/controllers/`; the generic of
  `asyncHandler<Dto>` is a cast too and proves nothing about the body. See
  `ts-ai-failure-paths.md` §4.

## Errors and the error boundary

- A controller method is wrapped in `asyncHandler` (`controllers/_helpers.ts`). Its `clientError`
  decides what the caller sees: the status and reason phrase of an `AiServiceHttpError` or a
  `DocspaceApiHttpError`, the message of an error with `expose === true` and a 4xx `status`, and
  `500 "Internal server error"` for everything else. The answer is `{ error: string }`.
- An error class extends `Error` and carries `readonly status`; `expose = true` says its message is
  written for the caller. `clientError` honours `expose` for 4xx only, so an error that must reach
  the caller as 5xx with its own text (a 503 "no model") is caught in the controller that knows it.
- The `message` of an upstream error holds the internal URL and the upstream body: it goes to the
  log, never into a response.
- After the headers are sent (a stream) an error is a frame of the stream without details
  (`streamNdjson`, `streamOpenAiSse`), not a status; see `ts-ai-express.md` §1 for an answer that is
  not a stream.

## Upstream calls

- The AI service is called through `aiService.*` (`storage/httpClient.ts`), the Files API through
  `filesApiGet` / `filesApiSend` (`storage/docspaceFilesApi.ts`). ESLint refuses a bare `fetch`
  outside `httpClient.ts`; the 23 that predate the rule are in the baseline, and a new upstream call
  does not add one.
- Every call is bounded: `withTimeout(signal)` with `cancel()` in a `finally`, and the result is
  awaited inside that `try` (`ts-ai-failure-paths.md` §7).
- An error of the Files API is a `DocspaceApiHttpError`, an error of the AI service an
  `AiServiceHttpError`. Code that tells them apart with `instanceof` depends on the right class.
- Storage adapters map the upstream DTO to the engine type and `encodeURIComponent` every id put
  into a path.

## State and logging

- Data of one request lives in `AsyncLocalStorage` (`requestContext.ts`). A module-level `Map`,
  cache or memoized promise is shared by every portal the process serves: see
  `ts-ai-failure-paths.md` §8.
- Log through `winston` (`log.js`), never `console.*`. The one exception is `config/index.ts`, which
  runs before the logger exists (`log.ts` imports the config).
- A log line names the field or the status that was wrong, not the payload: upstream bodies carry
  keys and headers.

## Config

- The environment and the settings files are read in `config/index.ts` only; ESLint refuses
  `process.env` anywhere else. Env variables are read in bracket notation
  (`process.env["AI_SERVICE_URL"]`) and override the file.
- A value goes through a reader that knows its bounds and falls back to a default in the code when
  the value is absent or malformed: `positiveInt`, `nonNegativeInt`, `unitFraction`, `flag` in
  `config/index.ts` are the pattern. A number has a lower and an upper bound; a duration says its
  unit in the name. An optional value never stops the service.
- Seen in the code: `AI_CHAT_UPSTREAM_TIMEOUT_MS` is read in `storage/httpClient.ts` with
  `parseInt10` and no bounds, so `0` or `30s` (parsed as 30) aborts every call to the AI service.
- Guard: a vitest table over absent, empty, non-numeric, zero, negative and too large, as in
  `app/__tests__/compactionConfig.test.ts`.

## Security

- **SSRF**: an outbound request to a URL a user or an administrator supplied passes
  `assertSafeBaseUrl` (`security.ts`).
- **Secrets stay inside**: a profile's `key` and `headers`, the headers of an MCP server and
  internal base URLs never leave in a response (`ts-ai-failure-paths.md` §5).
- **Sanitize untrusted text** that reaches the prompt or other users via `sanitizeInstruction`.
- The service does no authentication of its own: it forwards the caller's credentials downstream.
  Do not add local auth logic (see the gate in `routes.ts`).

## Routing

- Engine routes are declared once in `apiCatalog.ts` and consumed by `routes.ts` and the OpenAPI
  generator.
- A custom route is declared twice, in `routes.ts` and in `CUSTOM_ROUTE_DOCS` of `apiCatalog.ts`:
  add, rename and remove both in the same commit.
- Register literal paths before parameterized ones (`/agents/news` before `/agents/:id`).

## Misc

- **License header**: every source file, tests included, starts with the AGPL 3.0 `//`-comment block
  ending in `SPDX-License-Identifier: AGPL-3.0-only`; copy it from an existing file.
- **Comments explain the why** (an upstream-contract quirk, a security implication), not what the
  code does. A change that fixes a tracked issue cites it inline (`(Bug 82821)`), and so does the
  name of its test (`ts-ai-tests.md` §4).
