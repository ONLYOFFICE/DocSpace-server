---
paths:
  - "products/**/*.cs"
  - "web/**/*.cs"
  - "common/ASC.Core.Common/**/*.cs"
  - "common/ASC.Api.Core/**/*.cs"
  - "common/ASC.MessagingSystem/**/*.cs"
  - "common/ASC.Socket.IO/**/*.js"
---

# Real-time notifications: no client may be left with stale data

DocSpace pushes changes to open clients through the socket service (`common/ASC.Socket.IO`, Node).
When a change creates, updates, deletes, moves, copies, renames, shares or un-shares data, changes
tags (recent, favorites, "new" marks), finishes a long operation, or changes quota or a setting —
ask **which data a client could be showing goes stale**, and send the event. Past omissions of
exactly this kind: tags, sharing, recent/favorites, vectorization start/end, a form becoming ready,
external sharing / apps / AI / wallet settings, metadata values.

## The server goes first, the client catches up

The question is what the API exposes, not what today's UI shows. A change readable through the API
gets its event **in the same change that adds it** — even when no screen shows that data yet, and
even when the frontend has not started on the feature. The frontend catches up with the server and
subscribes to an event that is already there; the server does not wait for it.

"No screen consumes it yet" is therefore never a reason to skip an event. Neither is "the frontend
has not designed the room yet": when no existing room fits, define one now (see Semantics). Without
the event, the screen built later starts out stale and someone has to remember to come back.

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
- **An object that is not a file or a folder** (a portal-level entity with its own visibility rule)
  has no `{tenant}-DIR-{id}` room to use. Send it to the users who may read it under the API's own
  access rule — the same rule the read endpoint enforces — and never to a portal-wide room when the
  object is visible to some members only.
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

## A new event lands in both server halves at once

The socket service in `common/ASC.Socket.IO` is part of this repo and of the server. A new event is
done only when **both** halves are in the same change:

1. **C#** — the method on the matching notifier, called after the commit.
2. **Node** — the route in `ASC.Socket.IO/app/controllers/files.js` that receives that request, and
   the function in `app/hubs/files.js` that emits it to the room.

A C# call without its Node route posts to a path that does not exist — a wasted request and an error
in the socket log on every change — so never ship one half alone. Follow the route and hub function
of a neighbouring event: the same body shape (`room`, `userIds`, `data`), the same emit style.

## Contract with the frontend

Event and room names are a contract with the client (`SocketEvents` in
`../client/libs/ui-kit/utils/socket/index.ts`). File and folder methods (`create-file`,
`update-folder`, ...) reach the client as `s:modify-folder` with a `cmd`; rooms are
`{tenant}-DIR-{id}` / `{tenant}-FILE-{id}`.

The client entry is **not** a precondition for the server change: it is added when the frontend
picks the feature up. Because the client builds on what the server already sends, choose the event
name, the room and the payload as if they were final — once published, they are hard to change.
Say in the change description which event and room the frontend has to subscribe to. Never rename
or remove an existing event without the frontend change.
