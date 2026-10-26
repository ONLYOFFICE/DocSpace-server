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

Each event family has a payload of its own, named in the tables below: a file
event carries the file, a folder event the folder, a room or agent event the
room. Files, folders and rooms share a common core (`id`, `title`, `parentId`,
`created`, `createdBy` and the like), and `fileEntryType` in it tells a folder
(`1`) from a file (`2`). `form.submit` carries both the original form and the
submitted copy, while `form.filled.out` and `form.stopped` carry the form file
itself.

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
| `file.created` | [`FilePayload`](docs/FilePayload.md) |
| `file.uploaded` | [`FilePayload`](docs/FilePayload.md) |
| `file.updated` | [`FilePayload`](docs/FilePayload.md) |
| `file.trashed` | [`FilePayload`](docs/FilePayload.md) |
| `file.deleted` | [`FilePayload`](docs/FilePayload.md) |
| `file.restored` | [`FilePayload`](docs/FilePayload.md) |
| `file.copied` | [`FilePayload`](docs/FilePayload.md) |
| `file.moved` | [`FilePayload`](docs/FilePayload.md) |
| `file.downloaded` | [`FilePayload`](docs/FilePayload.md) |

## Folders

| Event | Payload |
|---|---|
| `folder.created` | [`FolderPayload`](docs/FolderPayload.md) |
| `folder.updated` | [`FolderPayload`](docs/FolderPayload.md) |
| `folder.trashed` | [`FolderPayload`](docs/FolderPayload.md) |
| `folder.deleted` | [`FolderPayload`](docs/FolderPayload.md) |
| `folder.restored` | [`FolderPayload`](docs/FolderPayload.md) |
| `folder.copied` | [`FolderPayload`](docs/FolderPayload.md) |
| `folder.moved` | [`FolderPayload`](docs/FolderPayload.md) |
| `folder.downloaded` | [`FolderPayload`](docs/FolderPayload.md) |

## Rooms

| Event | Payload |
|---|---|
| `room.created` | [`RoomPayload`](docs/RoomPayload.md) |
| `room.updated` | [`RoomPayload`](docs/RoomPayload.md) |
| `room.archived` | [`RoomPayload`](docs/RoomPayload.md) |
| `room.deleted` | [`RoomPayload`](docs/RoomPayload.md) |
| `room.restored` | [`RoomPayload`](docs/RoomPayload.md) |
| `room.copied` | [`RoomPayload`](docs/RoomPayload.md) |

## Forms

| Event | Payload |
|---|---|
| `form.submit` | [`FormSubmitPayload`](docs/FormSubmitPayload.md) |
| `form.filled.out` | [`FilePayload`](docs/FilePayload.md) |
| `form.stopped` | [`FilePayload`](docs/FilePayload.md) |

## Agents

| Event | Payload |
|---|---|
| `agent.created` | [`RoomPayload`](docs/RoomPayload.md) |
| `agent.updated` | [`RoomPayload`](docs/RoomPayload.md) |
| `agent.deleted` | [`RoomPayload`](docs/RoomPayload.md) |

## Events added later

New events are introduced over time, and a subscription to `*` will start
receiving them as soon as the portal is upgraded. Treat an unfamiliar
`event.trigger` as something to ignore rather than an error, so an upgrade
cannot break your endpoint.
