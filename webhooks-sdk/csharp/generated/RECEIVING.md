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
`x-docspace-event-timestamp`, duplicating `event.id` and `event.createOn` so a
stale or already-seen delivery can be dropped without touching the body.

**These headers are not covered by the signature.** Anything in transit can
rewrite them, so:

- Rejecting on them is safe — dropping a delivery is fail-safe.
- **Accepting on them is not.** A replay with its timestamp header rewritten to
  "now" still carries a valid signature. A receiver that checks freshness only
  against the header has no replay protection whatsoever.

After verifying, re-check `event.createOn` and `event.id` from the parsed body.
Comparing them against the headers also detects tampering for free.

## 3. Parse and narrow

```csharp
var hook = WebhookParser.Parse(body);

if (hook.PayloadAs<FileEntryPayload>() is { } entry)
{
    // 1 = folder, 2 = file. The only discriminator there is: no File- or
    // Folder-specific member ever reaches the wire.
    var kind = entry.FileEntryType == 2 ? "file" : "folder";
    Console.WriteLine($"{kind}: {entry.Title}");
}
```

`IsKnownTrigger` is false when DocSpace sends a trigger added after this build.
That is not an error — check it rather than throwing, or the receiver breaks on
the next server upgrade. The raw JSON stays available on `RawPayloadJson`.

## 4. Acknowledge immediately

Return 200 as soon as the signature checks out, and do the real work afterwards.

A delivery gets five attempts with exponential backoff from one second — about
31 seconds in total — after which it is abandoned, and only an administrator can
replay it from the delivery log.

Sustained failures cost the subscription itself, not just the delivery:

- **Three days without a single successful delivery and it is switched off**,
  receiving nothing further until someone re-enables it. The clock runs from the
  last success, or from when the subscription was created if it has never
  succeeded once — so a single bad delivery is never enough on its own. Three
  days is the default and the portal can be configured otherwise.
- **410 Gone deletes it outright**, on the first occurrence, with no three-day
  grace. Only answer 410 if you mean "never send here again".

## 5. Deduplicate on `event.id`

It is stable across the server's automatic retries. It is **not** stable across
a manual retry from the admin UI, which creates a new delivery record and
therefore a new id — the same logical event arrives with a different key.

## Models

 - [Model.EntryId](docs/EntryId.md)
 - [Model.FileEntryPayload](docs/FileEntryPayload.md)
 - [Model.FormSubmitPayload](docs/FormSubmitPayload.md)
 - [Model.GroupPayload](docs/GroupPayload.md)
 - [Model.UserPayload](docs/UserPayload.md)
 - [Model.WebhookConfigInfo](docs/WebhookConfigInfo.md)
 - [Model.WebhookEnvelope](docs/WebhookEnvelope.md)
 - [Model.WebhookEventInfo](docs/WebhookEventInfo.md)
 - [Model.WebhookTargetInfo](docs/WebhookTargetInfo.md)

