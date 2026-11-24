---
paths:
  - "common/ASC.AI.Chat/**/__tests__/**/*.ts"
  - "common/ASC.AI.Chat/vitest.config.ts"
---

# Tests of the ASC.AI.Chat service (vitest)

Unit tests of the Node service live next to the code, in `app/**/__tests__/*.test.ts`
(`vitest.config.ts` includes nothing else). `yarn test` runs them all, `yarn vitest run <file>` one
file, `-t "<name>"` one test, `yarn test:coverage` writes the coverage report to `coverage/`. They
are not part of `dotnet test ASC.Tests.slnx`. The service came without tests until October 2026:
none of the 35 fixes before vitest arrived brought one, 30 of the 39 after it did. Style rules (`ts-ai-style.md`) apply to tests too; ESLint leaves them `any`, `!`
and `fetch`. Cite this file as `[ts-ai-tests.md §N]`.

## 1. Which suite proves what

- **vitest**: a decision the service makes by itself: what a body is refused for, how an upstream
  status is mapped, what is left out of an answer, what happens when step N fails, a timeout, a
  race. Everything `ts-ai-failure-paths.md` names as a guard is a vitest test.
- **`products/ASC.AI/ASC.AI.Tests`** (`tests.md`): what needs the real services: that a .NET route
  exists and takes these parameters, who may call a route, what the stored state is afterwards. Its
  AppHost starts this service (`AddAiChat` in `common/ASC.AppHost/Program.cs`).
- A change of a route's contract (a status, a field, an access rule) is covered in the .NET suite
  even when a vitest test covers the decision.

## 2. Mock the boundary, not the subject

- Mock what the module talks to: the network, the engine class of `@onlyoffice/ai-chat/core`, the
  storage a controller reads. Never mock the module under test or the function the test is about.
- The harness is in `app/__tests__/helpers/`, shared fixtures of upstream DTOs in
  `app/__tests__/fixtures/`. Both stand on libraries; use them instead of a hand-made request,
  response, `fetch` stub, engine class or DTO literal:

  | Need | Helper | Library |
  |---|---|---|
  | call one handler, read status, body and headers | `invoke(handler, { body, query, headers })` in `http.ts` | node-mocks-http |
  | run code inside the real request context | `inRequestContext(headers, fn)` in `http.ts` | node-mocks-http |
  | answer the AI, Files or People service, a provider | `useUpstream()`, `http`, `docspace`, `docspaceError` in `upstream.ts` | msw |
  | stand in for an engine class | `engineMock<Engine>()`, `engineClass(engine)` in `engine.ts` | vitest-mock-extended |
  | an answer or a body against the published OpenAPI schema | `responseViolations(operationId, status, body)`, `requestViolations` in `schema.ts` | ajv |
  | the frames of an NDJSON or SSE answer, a value against a named schema | `ndjsonFrames`, `sseFrames` in `frames.ts`, `schemaViolations(name, value)` in `schema.ts` | ajv |
  | bytes of an answer as they arrive, a chunked body, a client that hangs up | `listen(app)`, `send(port, request)` in `rawHttp.ts` | node:net |
  | an MCP server the library's own client talks to | `mcpStub.ts` | msw |
  | `FolderDto` and `FileDto` of the Files API, a round's folder snapshot | `fixtures/filesApi.ts` (`folderDto`, `fileDto`, `snapshotWithFolders`) | |
  | the chat context aggregate and a primed round | `fixtures/chatContext.ts` (`chatContextDto`, `inRound`) | |
  | the properties of a C# DTO class, the members of an enum, the fields a Node function reads | `readClass`, `readEnum`, `readsOf` in `helpers/csharp.ts` | |
  | `ProfileDto` and a stubbed model provider | `fixtures/providers.ts` (`profileDto`, `useProfile`, `useProvider`) | |
  | the whole app: parsers, gate, routes, 404 | `request(createApp())` | supertest |
  | waits between polls and retries | `useFastTimers()` in `time.ts` | vitest fake timers |

  `promptsController.test.ts`, `storage/__tests__/httpClient.test.ts` and `app/__tests__/app.test.ts`
  show one of each.
- `app/__tests__/setup.ts` runs before every file. It stubs the logger (a test that asserts on a
  line imports the logger and reads the spy; do not declare the same `vi.mock` again) and points
  the appsettings directory at one that does not exist, so every machine sees the defaults of the
  code: CORS off, no document agent, no MCP servers. A test that needs a setting stubs its
  environment variable; none pins the defaults again.
- `invoke` hands the body over as given, `null` and `""` included. It answers when the handler
  returns: a stream the client closes half-way needs a response double of the test's own
  (`controllers/__tests__/aiController.harness.ts`).
- `useUpstream({ localApp: true })` in a file that drives `createApp()` with supertest: its own
  requests to 127.0.0.1 pass and stay out of `seen`. msw does not end an answer's body when the
  request is aborted and reads an answer to its end: a stalled or over-long body is a
  `ReadableStream` the handler drives from `request.signal`. A client that does not use the global
  `fetch` (the A2A SDK over undici) is not intercepted; mock its class.
- A file that mocks an engine class with `vi.mock` and also imports `helpers/schema.js` passes a
  function, `engineClass(() => engine)`: the schema helper loads `@onlyoffice/ai-chat/core`, and
  with it the mock's factory, before `engine` is initialized.
- `useUpstream()` runs the real `httpClient.js` and fails the test on a request no handler answers.
  Prefer it to `vi.mock("../httpClient.js")` and to `vi.stubGlobal("fetch")`: most older tests mock
  `requestContext.js` and `storage/httpClient.js`, so they show nothing about which headers are
  forwarded, what a timeout does or how a status is mapped.
- A stub answers what the test tells it to. It cannot show that the .NET route exists or that its
  answer has this shape: say which C# action the stubbed route is (a comment with its path), and
  leave the contract to the .NET suite (`ts-ai-failure-paths.md` §9).
- `engineMock` is typed against the real class, so a method the library renames stops the test
  from compiling instead of leaving it green. `restoreMocks` does not reset it: call
  `mockReset(engine)` in `beforeEach`.
- Before adding a helper or a test library, look for it in the table; a new one is added to the
  table in the same commit.

## 3. A test that cannot fail proves nothing

- A fix comes with a test that is red without it. Check it: take the fix out (`git stash` the
  product file, or revert the lines), run `yarn vitest run <file>`, see it fail for the reason the
  fix addresses, put the fix back.
- Assert the outcome a caller sees: the status and the body, the upstream calls that were or were
  not made (`expect(fetchMock).not.toHaveBeenCalled()`), the object that exists afterwards. An
  assertion that the handler did not throw passes for the broken code too.
- A failure test asserts what the answer must not contain as well: the internal URL, the upstream
  body, a key.

## 4. Names and bug numbers

- A module's tests are `<module>.test.ts`; a second file for one aspect of it is
  `<module>.<aspect>.test.ts`, the aspect being what it exercises: `routes` (a controller's
  handlers and their DTO tables), `upstream` (the real clients over the network stub), `stream`,
  `edge` (the cases a first suite left). A helper used by one area stays next to its tests without
  the `.test` suffix; one a second area needs moves to `app/__tests__/helpers/` or `fixtures/`.
- A test name states the behaviour as a sentence: `it("answers 400 when toolNames is a string")`.
- A test written for a tracked defect carries the number in its name, `(Bug 84402)`, so the test of
  a bug can be found by its number. No test written before this rule carries one.

## 5. Time and the network

- No test reaches the network or DNS: `useUpstream()` fails on a request nobody answers,
  `dns/promises` is mocked.
- Anything that waits (a timeout, a poll, a retry, the stream heartbeat) runs on fake timers:
  `useFastTimers()` when only the outcome matters, `vi.useFakeTimers()` with
  `vi.advanceTimersByTimeAsync()` when the test asserts the moment. One file waiting on real
  timers took 20 of the suite's 20.5 seconds until it moved to `useFastTimers()`. The whole suite,
  6040 tests, runs in about 7 seconds: a single test stays under about 50 ms.
- `restoreMocks: true` resets spies between tests. Module state (a cache, a memoized promise) is
  not reset: re-import the module (`vi.resetModules()`), or the tests depend on their order.

## 6. What every new piece gets

- A route: a table of bad bodies answering 400 (`ts-ai-failure-paths.md` §4), and the access gate
  answering 403 before any other upstream call (§3).
- A read with an "absent" branch: the upstream answering 500 and timing out (§1).
- An answer that carries a profile, an MCP config or an upstream error: no secret in it (§5).
- A multi-step write: step N failing, for each N (§6).
- A config value: absent, empty, non-numeric, zero, negative, too large
  (`app/__tests__/compactionConfig.test.ts`).

## 7. Tests cited by the specs

- A `verification.md` row may cite a vitest test as `file.test.ts:line`. It then says "at unit
  level": the status TEST-CONFIRMED is defined there as a .NET test, and the run marks are written
  from the .NET run only, so a vitest citation reads `[run: NO-LINK]`.
- A commit that moves the cited lines updates the rows (`claims.md` §4).

## 8. The contract with the .NET services

- `app/__tests__/contract.dotnet.test.ts` compares the sources of both sides, without running a
  service: the properties a C# DTO class declares with the fields the Node function that maps it
  reads (`helpers/csharp.ts` reads both), and every enum that crosses the boundary with the
  values Node knows. It fails when a property or an enum member is added, removed, renamed or
  changes its kind (string, number, boolean, array, object) on either side alone.
- A new mapper of an upstream answer gets a row in `CONTRACTS`: the C# class or classes, the Node
  function. A difference that is meant is listed in the row with its reason (`notSent`: read by
  Node, not declared by C#; `unread`: declared, not read; `otherKind`); a reason states what the
  code does, not what was intended. For the Files and People DTOs only the fields Node reads are
  checked, as it reads a few properties out of dozens.
- A numeric constant that stands for a C# enum member (a folder or room type) gets a row in the
  enum table of the same file.
- A fixture of an upstream DTO carries the properties the C# class declares. A test row for a
  field the class does not declare says so in a comment above it.
- What the comparison cannot see: a property that is declared and left null, a value the
  serializer writes differently from the declaration, the bodies Node sends (the request DTOs).
  Those show only against the running services, in `products/ASC.AI/ASC.AI.Tests`.

## 9. The suite pins what the code does today

- The tests are characterization tests: they assert the exact status, body, upstream requests and
  log level of the code as it is, so that a refactoring which changes any of it turns a test red.
  A change of behaviour therefore changes a test in the same commit, and the commit message says
  which behaviour moved. A test that goes red without such an intent is the deviation the suite
  exists to catch: fix the code, not the assertion.
- Every answer of a route handler is checked against the schema the service publishes for that
  operation and status (`responseViolations`), every request body by a table over each field
  (missing, `null`, wrong type, empty, over a limit), every upstream DTO by a fixture whose shape
  is taken from the C# class named above it, then each field missing, `null` and of the wrong type.
- **Behaviour that is pinned although it is wrong** carries a comment directly above the test:
  `// DEFECT <area>-<n>: <what it does> — expected <what it should do> (<rule §N or source>)`.
  `grep -rn "// DEFECT" common/ASC.AI.Chat/app` lists them (138 when the suite was written). The
  fix of one changes its test to the expected behaviour and removes the comment; a new one found
  while writing a test gets the next number of its area.
- A DEFECT comment is a claim about the whole system, and a vitest test sees one service with the
  rest stubbed. Before writing one, and before fixing one, read the route in
  `openspec/specs/ai-agents/spec.md` and in `products/ASC.AI/ASC.AI.Tests`, and the C# that
  answers behind it. Two other labels say what such a reading found, in the same place and form:
  `// CONTRACT <id>: <what it does>. Pinned by <spec line or .NET test>.` for behaviour the spec
  or the .NET suite pins (a refusal answered 200 with `success: false`, `200 null` for an unknown
  prompt), which changes only through an OpenSpec change with the .NET tests; and
  `// HANDLED DOWNSTREAM <id>: <what it does>. In production <who refuses or prevents it>.` for
  what the AI service or the engine refuses itself, so that only the stubbed test shows it.
  `grep -rn "// DEFECT"` then lists what is open (none on 2026-11-24, after the 36 open that morning were fixed or relabelled).
- A defect that touches security (a secret or a credential in an answer or a log, a credential
  sent to another host, a missing access check, a request to an address the caller chose) is not
  pinned and not described in the repository: it goes to the private register
  (`fix-bug` §6), and its test comes with the fix.
- **Coverage** is part of `yarn check`: `vitest.config.ts` fails the run under 99.5% of lines,
  100% of functions and 98.5% of branches. What is left is code no input reaches (a `??` after a
  value that is always set, a branch for a non-`Error` nothing throws). New code comes with its
  tests; do not lower a threshold, and do not add `/* v8 ignore */` to reach it.
