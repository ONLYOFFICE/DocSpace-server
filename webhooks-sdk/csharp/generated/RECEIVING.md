# Receiving DocSpace webhooks

Everything a `DocSpace.Webhooks.SDK` receiver has to get right, in the order it
matters. See [TRIGGERS.md](TRIGGERS.md) for what each event carries, and
[README.md](README.md) for the model index.

## 1. Verify before you parse

```csharp
if (!WebhookSignature.Verify(body, secret, signature))
{
    return Results.Unauthorized();
}
```

`body` must be the **raw bytes**. A body that has been deserialized and
re-serialized is a different byte sequence and will never match. In ASP.NET Core
that means enabling request buffering and reading the stream, not the
model-bound object.

## 2. Cheap rejection, before parsing at all

Every delivery also carries `x-docspace-event-id` and
`x-docspace-event-timestamp`, duplicating `event.id` and `event.createOn` so an
already-seen delivery can be dropped without touching the body.

**These headers are not covered by the signature.** Anything in transit can
rewrite them, so:

- Rejecting on them is safe — dropping a delivery is fail-safe.
- **Accepting on them is not.** After verifying, re-check `event.id` from the
  parsed body. Comparing it against the header also detects tampering for free.

**Replay.** The signature covers the body only; it does not prove when the
request was sent. Deduplicate on `event.id`, and keep the ids you have processed
for at least the retry window: about 18 hours on the default schedule, longer if
a portal administrator may replay old deliveries from the log. Do not reject a
delivery for its age: `event.createOn` is when the event happened and stays the
same on every automatic and manual retry, so a freshness check on it throws away
legitimate retries.

## 3. Parse and narrow

```csharp
var hook = WebhookParser.Parse(body);

// Files, folders and rooms each have their own schema, so ask for the one the
// trigger carries. PayloadAs returns null when the delivery is something else.
if (hook.PayloadAs<FilePayload>() is { } file)
{
    Console.WriteLine($"file: {file.Title} ({file.ContentLength} bytes)");
}
else if (hook.PayloadAs<FolderPayload>() is { } folder)
{
    Console.WriteLine($"folder: {folder.Title}");
}
else if (hook.PayloadAs<RoomPayload>() is { } room)
{
    Console.WriteLine($"room: {room.Title} (type {room.RoomType})");
}
```

`IsKnownTrigger` is false when DocSpace sends a trigger added after this build.
That is not an error — check it rather than throwing, or the receiver breaks on
the next server upgrade. The raw JSON stays available on `RawPayloadJson`.

## 4. Acknowledge immediately

Return 200 as soon as the signature checks out, and do the real work afterwards.

The server waits 30 seconds for a response; a timeout, a network error or any
non-2xx status counts as a failed attempt. A failed delivery is retried after
about 1 minute, 5 minutes, 30 minutes, 2 hours, 5 hours and 10 hours (each ±10%)
— seven attempts over roughly 18 hours by default — after which it is abandoned.
From then on only a portal administrator or the member who created the
subscription can replay it from the delivery log.

Sustained failures cost the subscription itself, not just the delivery:

- **Three days without a single successful delivery and it is switched off**,
  receiving nothing further until someone re-enables it. The clock runs from the
  last success, or from when the subscription was created if it has never
  succeeded once — so a single bad delivery is never enough on its own. Three
  days is the default and the portal can be configured otherwise.
- **410 Gone deletes it outright**, on the first occurrence, with no three-day
  grace. Only answer 410 if you mean "never send here again".

## 5. Deduplicate on `event.id`

It is stable across every resend of one delivery — the server's automatic retries
and a manual retry from the delivery log alike. A manual retry is a delivery
record of its own, but it sends the `event.id` of the first one. The same id
always means the same logical event, so it is a sound idempotency key on its own.

Note what that implies: a delivery you have already processed can arrive again at
any time, days later, if someone presses retry. Keep the ids you have seen for
longer than the retry budget, not just for the ~18 hours the automatic retries
span.

## Models

 - [Model.ContactPayload](docs/ContactPayload.md)
 - [Model.EntryId](docs/EntryId.md)
 - [Model.FileDelivery](docs/FileDelivery.md)
 - [Model.FileEntryPayload](docs/FileEntryPayload.md)
 - [Model.FilePayload](docs/FilePayload.md)
 - [Model.FolderDelivery](docs/FolderDelivery.md)
 - [Model.FolderPayload](docs/FolderPayload.md)
 - [Model.FormSubmitDelivery](docs/FormSubmitDelivery.md)
 - [Model.FormSubmitPayload](docs/FormSubmitPayload.md)
 - [Model.GroupDelivery](docs/GroupDelivery.md)
 - [Model.GroupPayload](docs/GroupPayload.md)
 - [Model.GroupSummaryPayload](docs/GroupSummaryPayload.md)
 - [Model.RoomDelivery](docs/RoomDelivery.md)
 - [Model.RoomPayload](docs/RoomPayload.md)
 - [Model.UserDelivery](docs/UserDelivery.md)
 - [Model.UserPayload](docs/UserPayload.md)
 - [Model.UserSummaryPayload](docs/UserSummaryPayload.md)
 - [Model.WebhookConfigInfo](docs/WebhookConfigInfo.md)
 - [Model.WebhookEnvelope](docs/WebhookEnvelope.md)
 - [Model.WebhookEventInfo](docs/WebhookEventInfo.md)
 - [Model.WebhookTargetInfo](docs/WebhookTargetInfo.md)

