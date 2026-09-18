# DocSpace.Webhooks.SDK

C# models for **receiving** ONLYOFFICE DocSpace webhooks.

Generated from `common/ASC.Webhooks.Core/Contract/docspace-webhooks.yaml` by
`webhooks-sdk/generate.sh`. Do not edit anything in this directory by hand --
it is wiped and rewritten on every regeneration.

This package is deliberately **models only**. It contains no HTTP client, no
`Configuration`, and no API classes: a webhook receiver never makes a request,
it only decodes an inbound body. The hand-written verification and parsing
helpers live one level up, in `webhooks-sdk/csharp/src/`.

## Dependencies

- [Json.NET](https://www.nuget.org/packages/Newtonsoft.Json/) 13.0.3 or later

Nothing else. The stock generator README lists JsonSubTypes and
System.ComponentModel.Annotations as well; neither is used by these models.

## Usage

```csharp
using DocSpace.Webhooks.SDK;
using DocSpace.Webhooks.SDK.Model;

// Verify against the RAW body -- the signature covers exactly those bytes.
if (!WebhookSignature.Verify(body, secret, signature))
{
    return Results.Unauthorized();
}

var hook = WebhookParser.Parse(body);

if (hook.PayloadAs<FileEntryPayload>() is { } entry)
{
    // 1 = folder, 2 = file. The only discriminator there is: no File- or
    // Folder-specific member ever reaches the wire.
    var kind = entry.FileEntryType == 2 ? "file" : "folder";
    Console.WriteLine($"{kind}: {entry.Title}");
}
```

Acknowledge before doing real work: DocSpace retries five times with
exponential backoff from 1s and then gives up for good -- roughly 31 seconds in
total -- so a slow receiver loses events permanently.

<a id="documentation-for-models"></a>
## Documentation for Models

 - [Model.EntryId](docs/EntryId.md)
 - [Model.FileEntryPayload](docs/FileEntryPayload.md)
 - [Model.FormSubmitPayload](docs/FormSubmitPayload.md)
 - [Model.GroupPayload](docs/GroupPayload.md)
 - [Model.UserPayload](docs/UserPayload.md)
 - [Model.WebhookConfigInfo](docs/WebhookConfigInfo.md)
 - [Model.WebhookEnvelope](docs/WebhookEnvelope.md)
 - [Model.WebhookEventInfo](docs/WebhookEventInfo.md)
 - [Model.WebhookTargetInfo](docs/WebhookTargetInfo.md)


<a id="documentation-for-api-endpoints"></a>
## Documentation for API Endpoints

None. This package decodes inbound webhook bodies; it makes no requests.
The generated model docs link here from their footers.

## Triggers

`Triggers.cs` maps all 37 trigger names to the payload each one carries, and is
generated from the same contract. Use `WebhookTriggers.IsKnown(trigger)` before
trusting a payload type: DocSpace adds triggers over time, and one added after
this build was generated will not be in the table.
