<!-- Generated from the DocSpace webhook contract. Do not edit by hand. -->

# Webhook events

DocSpace can notify your endpoint about 36 events. Each delivery names the
one that caused it in `event.trigger`, and carries that event's subject in
`payload`.

A subscription chooses which events it wants. Subscribe to `*` to receive all of
them, including any added in future. `GET api/2.0/settings/webhook/triggers`
lists every event with the value to subscribe with, and whether your role is
allowed to.

## Payloads

Files, folders, rooms, agents and forms all deliver the **same payload shape**.
Nothing in it is specific to one kind of entry, so use `fileEntryType` to tell
them apart: `1` is a folder, `2` is a file.

Note that a file's name arrives in `title`, and that absent fields are simply
defaults — a property missing from the JSON means empty, `false` or zero, never
"unknown".

## Users

| Event | Payload |
|---|---|
| `user.created` | [`UserPayload`](docs/UserPayload.md) |
| `user.invited` | [`UserPayload`](docs/UserPayload.md) |
| `user.updated` | [`UserPayload`](docs/UserPayload.md) |
| `user.deleted` | [`UserPayload`](docs/UserPayload.md) |

## Groups

| Event | Payload |
|---|---|
| `group.created` | [`GroupPayload`](docs/GroupPayload.md) |
| `group.updated` | [`GroupPayload`](docs/GroupPayload.md) |
| `group.deleted` | [`GroupPayload`](docs/GroupPayload.md) |

## Files

| Event | Payload |
|---|---|
| `file.created` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `file.uploaded` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `file.updated` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `file.trashed` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `file.deleted` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `file.restored` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `file.copied` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `file.moved` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `file.downloaded` | [`FileEntryPayload`](docs/FileEntryPayload.md) |

## Folders

| Event | Payload |
|---|---|
| `folder.created` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `folder.updated` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `folder.trashed` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `folder.deleted` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `folder.restored` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `folder.copied` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `folder.moved` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `folder.downloaded` | [`FileEntryPayload`](docs/FileEntryPayload.md) |

## Rooms

| Event | Payload |
|---|---|
| `room.created` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `room.updated` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `room.archived` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `room.deleted` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `room.restored` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `room.copied` | [`FileEntryPayload`](docs/FileEntryPayload.md) |

## Agents

| Event | Payload |
|---|---|
| `agent.created` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `agent.updated` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `agent.deleted` | [`FileEntryPayload`](docs/FileEntryPayload.md) |

## Forms

| Event | Payload |
|---|---|
| `form.submit` | [`FormSubmitPayload`](docs/FormSubmitPayload.md) |
| `form.filled.out` | [`FileEntryPayload`](docs/FileEntryPayload.md) |
| `form.stopped` | [`FileEntryPayload`](docs/FileEntryPayload.md) |

## Events added later

New events are introduced over time, and a subscription to `*` will start
receiving them as soon as the portal is upgraded. Treat an unfamiliar
`event.trigger` as something to ignore rather than an error, so an upgrade
cannot break your endpoint.
