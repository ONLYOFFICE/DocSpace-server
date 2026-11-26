---
paths:
  - "common/ASC.AI.Chat/**/*.ts"
---

# ASC.AI.Chat: what a failed step, a refusal and a bad body leave behind

The service is written for the success path of a call to the .NET services. The 74 commits that
fixed it up to November 2026 keep returning to the nine questions below, and more than once to the
same one in a file written days later. Answer them while writing the code. Each section
names the commits it comes from and the test that proves it; ESLint (`eslint.config.js`) refuses the
ones a machine can see. It is the TypeScript counterpart of `failure-paths.md`, which loads for C#
only. Cite it as `[ts-ai-failure-paths.md §N]`.

## 1. A failed read is not "absent"

- An upstream read turns into "none", "empty" or "no access" only for the status that means it: 404,
  or the 400 the AI service gives a malformed id. A timeout, a dropped connection, a 5xx and a body
  of the wrong shape are **unknown**: rethrow them, so the caller gets a 5xx and can retry.
- A `.catch()` that answers `undefined`, `null`, `[]`, `{}` or `false` is refused by ESLint, as an
  arrow (`() => null`) or as a block that logs and returns it, returns bare, or neither returns nor
  throws while its value is read (`app/__tests__/eslintConfig.test.ts` lists the forms). A handler
  made by a factory (`.catch(tolerate(...))`) is not seen: do not write one. Where an outage may
  degrade one optional part of an answer, write a `try/catch` that tests the status and says in a
  comment why unknown is safe to read as absent there.
- A 200 whose body the mapper cannot read is unknown too: a single-entity read passes its result
  through `mapped(value, call)`, a read where nothing stored is a legal answer through
  `objectOrNone(raw, call)` (an object or `null`), a list read through `listAnswer(raw, call)`, a body that has to
  be JSON is read with `jsonAnswer(res, call)`, and an answer relayed with its envelope (`raw: true`)
  through `envelopeAnswer(raw, call)` (`storage/upstreamShape.ts`). They throw
  `UpstreamShapeError`, which the error boundary answers
  as `502 { error: "Bad Gateway" }` and logs with the name of the call. Dropping one malformed
  element of a list is fine; reading the whole answer as "none" is not.
- A check that protects money or access fails closed: a read that failed does not count as "paid",
  "allowed" or "enabled".
- Never recognize an error by the text of its `message`; test its class and its `status`.
- Seen in review: `storage.profiles.readById(id).catch(() => undefined)` answered 404 "the AI profile
  does not exist" while the AI service was down (written in 465e9381df, fixed in 6ac1fdd726);
  2945e945ad wrote `.catch(() => null)` on the portal's model and 668748269b removed it the same
  day; f6ad0a839a then removed it from thread creation, the round's profile check and the model of an
  AI project.
- Guard: for every read with an "absent" branch, a test where the upstream answers 500 and one
  where it times out; the route answers 5xx, not 404, 400, 409 or an empty list.

## 2. A refusal of the engine is an HTTP status

- The engine reports a refusal as a value, `{ success: false, error }`, instead of throwing.
  `res.json(result)` sends it with 200. Send a `*MutationResult` or `*BulkResult` through a helper
  that maps `error.code` to 400, 404 or 409, as `sendMutation` in `promptsController.ts` does.
- Where a refusal already goes out with 200, the status is contract: the "Soft refusals" scenario
  of `openspec/specs/ai-agents/spec.md` and the suites in `products/ASC.AI/ASC.AI.Tests` (MCP
  servers, assignments) read `{ success: false }` from a 200 through the generated
  client, which throws on a 4xx. Moving such a route to a 4xx is an OpenSpec change
  (`openspec-propose`) with the .NET tests in the same branch, not a fix made in this service alone.
  `profiles/create` and `profiles/update` are not among them: the spec asks 400 for an unusable
  profile and `ProfileProviderTypeTests` accepts either answer, so they answer a refusal with 400
  (`sendProfileMutation`).
- A plain `Error` the engine throws for a state the caller caused (an unknown id, a missing field)
  is caught and mapped to its 4xx; left alone it becomes a 500.
- Seen in review: prompt refusals went out with 200 (d50e326b63), and so did a refused
  `web-search/configure` (e823b239bc). Each was fixed for its own controller only.
- Guard: one test per refusal code of the route, asserting the status, not only the body.

## 3. The access gate comes before any work

- A route reaches the AI access gate (`assertAiAccess`, or a first call to a gated AI endpoint)
  before it validates the body, calls out, runs a tool or writes. A guest, or a portal with AI
  switched off, gets 403 for any body the parser accepted, never the 400 of a route's own check and
  never an outbound request made on their behalf.
- The body parser of `createApp` runs in front of both gates (`ts-ai-express.md` §2): a body that
  does not parse is 400 and one over the parser's limit 413 for every caller, with or without
  credentials or AI access. Neither answer says anything about the route or the caller.
- Seen in review: `editor-tools/call` ran MCP tools with AI switched off,
  `profiles/list-provider-models` made an outbound request for a guest, and `web-search/configure`
  validated the body and probed the provider before the 403 (0dc046cacd).
- The input is the body, the query and the path parameters alike: a query check or a Files API
  read of the `entityId` comes after the gate as well.
- Some routes judge their input first, or have no AI gate, by contract; they keep that order until
  an OpenSpec change moves it. `NOT_GATED_FIRST` in `app/__tests__/app.aiGate.test.ts` lists each
  with its source: `editor-tools/call` (`EditorToolAiDisabledTests`, CONTRACT passthrough-13),
  `text-to-docx` ("400 before 403"), the per-user settings and `aiSettingsGet` (usable while AI is
  off), the tool permission mode writes ("validation comes first on the write"), the profile id
  routes (`ProfileAccessGuestTests`: "id-format checks run before the role check") and the four
  `context` reads, which go to the Files API only.
- Guard: `app/__tests__/app.aiGate.test.ts` sends every published operation outside that list a
  path parameter, a repeated query parameter and a body of the wrong type, with the AI service
  answering 403: the answer is 403 and nothing but the AI service was called. A new route is in it
  without a line of its own; a new exception goes into the list with its source. The same file pins
  the parser's 400 and 413 in front of the gates.

## 4. A request body is narrowed, not cast

- `req.body` and the fields of `unpackPositional(...)` are `unknown`. Read each field through
  `app/narrow.ts` (`asString`, `getArray`, `isObject`, ...) and answer 400 naming the field when it
  has the wrong type. A wrong body is never a 500 and never a 200 that stored something else.
- The JSON parser runs with `strict: false` (`app/createApp.ts`), so a body can be `null`, a string or a
  number: test `isObject(req.body)` before reading a field.
- Every array in the input has a named limit (`MAX_ATTACHMENTS_PER_REQUEST`), and repeated ids are
  collapsed before the upstream call.
- Seen in review: `toolNames` sent as a string was spread into its characters, and the string
  `"false"` granted a permanent permission (eafd5d3872); f6ad0a839a bounded three attachment routes
  and left `deleteMany` with `(args.ids as string[]) ?? []`.
- Guard: a table of bodies per route (`null`, `{}`, a string, a wrong type for each field, an array
  over the limit), each answering 400.

## 5. What leaves the process is listed, not echoed

- An answer is built from named fields. Returning the engine's or the storage's object as it came
  sends whatever the .NET side adds to it tomorrow.
- A profile is read for an answer with `forClient: true` and without `key` and `headers`; an MCP
  server's config leaves without `headers`; the body of an upstream 5xx is not relayed.
- The caller's cookie and authorization go to the portal's own origin only, never to a storage, CDN
  or provider host.
- A fix of this kind covers every route that returns the same object: list, get, resolve, the
  answer of the write itself.
- Seen in review: `profiles/list` served every non-guest caller the decrypted provider key
  (6dbd77cf2c);
  f6ad0a839a removed the headers from `get-custom-server` and left them in `list-custom-servers`,
  closed by 9fab800d24 the same day.
- Guard: store a profile and an MCP server whose secrets carry a marker string, call every route
  that can return them, and assert that no answer contains the marker.

## 6. A multi-step operation says what it has already made

- Every check that can refuse runs before the first object is created.
- After that, a failure of a later step (a timeout and a non-HTTP error included) either undoes what
  was made or answers with the ids of what exists and a warning. It never answers a bare error that
  leaves rooms or files the caller cannot find.
- A file that cannot be put in its new place stays attached where it is.
- A step that creates something and gets an unknown answer (§1: a timeout, a dropped connection, a
  5xx, a 2xx that cannot be read) may have created it. Nothing is deleted on such an answer, and
  the answer says that the object may exist: only a refusal (a 4xx) says it does not. A template
  install names such a room as `unconfirmed`; a generated image whose attachment record got an
  unknown answer is kept (`recordWasRefused` in `attachmentsStorage.ts`).
- Seen in review: installing a template left an agent's Instructions rooms behind when the Files
  service refused the Knowledge room (c373d83b5e); a model write that timed out failed the whole
  installation instead of being one of its warnings (9a9cf905cc).
- Guard: make step N fail for each N, and assert what exists afterwards and what the answer names.

## 7. A timeout covers the body, and the body has a limit

- In a `try` whose `finally` cancels the timeout, the result is `return await`ed. `return promise`
  runs the `finally` first, and the body is then read with no timeout at all. ESLint refuses it.
- The body of an answer from outside (a file, a provider, a search service) is read with a byte
  limit, never by a bare `arrayBuffer()` or `text()`.
- Seen in review: `return readTextCapped(res, MAX_SKILL_BYTES)` cancelled the timeout before the read
  (5bc3aae36e); the same line stayed in `httpClient.ts` until f6ad0a839a.
- Guard: fake timers and an upstream that sends headers and then stalls; the call rejects.

## 8. State that outlives a request

- A module-level cache, `Map` or memoized promise is shared by every portal and user of the
  process. Key it by what makes the value differ (portal, user, entity), bound its size and age, and
  say in a comment why it is safe.
- A memoized promise that rejected, or resolved to an answer that means "the read went wrong", is
  dropped, so the next call asks again.
- A queue the upstream shares between callers (file operations) is read by the caller's own
  operation id, never by taking whatever is in it.
- Seen in review: a failed write or read of the document agent settings stuck (3400e63edb);
  concurrent conversions each polled the file-operations list, so one could consume another's
  result, and an empty `extsConvertible` table was cached for the process (d2847629c9).
- Guard: the first call fails and the second one reaches the network again; two parallel callers
  each get their own result.

## 9. A route of a .NET service exists in that service

- The path, the method, the query parameters and the limits of a call to the AI, Files or People
  service are read from the C# action that serves it, not from memory. Mind the page limit and the
  `{ response }` envelope.
- A vitest stub of a .NET route answers whatever the test tells it to, so it cannot show that the
  route exists (`ts-ai-tests.md` §2). A new composed route gets a test in
  `products/ASC.AI/ASC.AI.Tests`, which runs this service against the real ones.
- Seen in review: the template installer sent `PUT rooms/{id}/instructions`, a route the Files
  service does not have (written in f2ec853291, removed in ba2531c2af); a list asked for
  `count=1000` where the Files service allows 100 per page and answered 400 (a9a7d50552).
