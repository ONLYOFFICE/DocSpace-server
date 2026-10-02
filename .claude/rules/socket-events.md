---
paths:
  - "products/**/*.cs"
  - "web/**/*.cs"
  - "common/ASC.Core.Common/**/*.cs"
  - "common/ASC.Api.Core/**/*.cs"
  - "common/ASC.MessagingSystem/**/*.cs"
  - "common/ASC.Socket.IO/**/*.js"
---

# Real-time notifications: an open screen must not go stale

DocSpace pushes changes to open clients through the socket service (`common/ASC.Socket.IO`, Node).
When a change creates, updates, deletes, moves, copies, renames, shares or un-shares data, changes
tags (recent, favorites, "new" marks), finishes a long operation, or changes quota or a setting the
UI shows — ask **which open screen goes stale**, and send the event. Past omissions of exactly this
kind: tags, sharing, recent/favorites, vectorization start/end, a form becoming ready, external
sharing / apps / AI / wallet settings, metadata values.

## Send through the existing notifiers

All derive from `SocketServiceClient` (`common/ASC.Core.Common/Notify/Signalr/`) and call its
`MakeRequest`:

| Notifier | What it covers |
|---|---|
| `SocketManager` (`products/ASC.Files/Core/Utils/`) | files, folders, rooms, forms, marks, recent/favorites/shared, access rights, backup/restore progress |
| `MetadataEntryNotifier` | metadata changes on entries (wraps `SocketManager`, batched) |
| `QuotaSocketManager` | quota, tariff, wallet, sessions, AI and external-sharing settings |
| `UserSocketManager` | users, groups, guests, user type |
| `AppsSocketManager`, `HistorySocketManager`, `AiSocketManager` | apps toggle, entry history, AI form analysis |

A new kind of event is a new method on the matching notifier. Never create an `HttpClient` for the
hub or POST to `web:hub:internal` yourself (`http-clients.md`).

## Semantics

- **Recipients**: let `SocketManager` resolve them (`WhoCanRead` — users who can read the entry,
  shared users through the Shared-with-me room), or pass a list resolved once per parent folder for
  bulk work. For a **delete**, capture recipients before the `files_security` rows go
  (`GetDeleteRecipientsAsync` or the `action` callback) — afterwards nobody "can read" it.
- **Payload is a nudge to re-read**: ids, title, parent. Joining a socket room is not
  access-checked, so never put content, access rights or personal data into the payload.
- **After the commit**, never inside the transaction: an event for a change that is rolled back
  leaves phantom changes on every client.
- **Await the call.** It only enqueues — the HTTP send happens in the background `SocketService` — so
  awaiting costs nothing. No `_ =` fire-and-forget, no `Task.WhenAll` over a scoped notifier (it
  resolves recipients through the scoped `DbContext`).
- **Best effort**: a socket failure must not fail the operation, and no retries are added around it.
  Recipient resolution runs inline and can throw — keep it outside paths whose failure would roll
  back the business change.
- **Bulk and background operations**: notify only for the top-level entries of an operation, batch
  recipients by 1000, and make sure the scope carries the tenant (or pass `tenantId`).

## Contract with the frontend

Event and room names are a contract with the client (`SocketEvents` in
`../client/libs/ui-kit/utils/socket/index.ts`). File and folder methods (`create-file`,
`update-folder`, ...) reach the client as `s:modify-folder` with a `cmd`; rooms are
`{tenant}-DIR-{id}` / `{tenant}-FILE-{id}`. A new event needs a route in
`ASC.Socket.IO/app/controllers/files.js`, an emit in `app/hubs/files.js` and an entry on the client
side. Never rename or remove an existing event without the frontend change.
