# DocSpace.Webhooks.SDK

The ONLYOFFICE DocSpace Webhooks SDK for C# is a library that provides tools for receiving webhook deliveries from DocSpace. It verifies the signature on each delivery and decodes its payload into ready-to-use models.

It is the inbound counterpart to `DocSpace.API.SDK`: that library calls DocSpace, this one handles what DocSpace sends you. It makes no requests of its own and contains no HTTP client.

For more information, please visit [https://helpdesk.onlyoffice.com/hc/en-us](https://helpdesk.onlyoffice.com/hc/en-us)

## Installation

To get started, install the package from NuGet

```
dotnet add package DocSpace.Webhooks.SDK
```

## Usage

A receiver does two things, in this order: verify, then parse.

```csharp
if (!WebhookSignature.Verify(body, secret, signature))
{
    return Results.Unauthorized();
}

var hook = WebhookParser.Parse(body);
```

`body` must be the **raw bytes or string exactly as received**. A body that has been deserialized and re-serialized is a different byte sequence and will never match the signature, so bind the request to a `string` or read the stream yourself rather than to a model.

`secret` is the secret key of the subscription the delivery belongs to, as set when it was created through `POST api/2.0/settings/webhook`. It is never included in a delivery, so keep it alongside your endpoint configuration.

Answer as soon as the signature checks out and do the real work afterwards. A delivery gets five attempts over roughly 31 seconds, after which it is abandoned; a subscription that goes three days without a single successful delivery is switched off.

## Getting Started

```csharp
using DocSpace.Webhooks.SDK;
using DocSpace.Webhooks.SDK.Model;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

const string secret = "YOUR_SUBSCRIPTION_SECRET_KEY";

app.MapPost("/webhook", async (HttpRequest request) =>
{
    // Read the body untouched -- the signature is computed over these bytes.
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();

    var signature = request.Headers[WebhookSignature.HeaderName];

    if (!WebhookSignature.Verify(body, secret, signature))
    {
        return Results.Unauthorized();
    }

    var hook = WebhookParser.Parse(body);

    switch (hook.Payload)
    {
        case FileEntryPayload entry:
            // fileEntryType tells a file from a folder: 1 folder, 2 file.
            var kind = entry.FileEntryType == 2 ? "file" : "folder";
            Console.WriteLine($"{hook.Trigger}: {kind} \"{entry.Title}\"");
            break;

        case UserPayload user:
            Console.WriteLine($"{hook.Trigger}: {user.UserName} <{user.Email}>");
            break;

        case GroupPayload group:
            Console.WriteLine($"{hook.Trigger}: group \"{group.Name}\"");
            break;
    }

    // Acknowledge now; anything slow belongs after this point.
    return Results.Ok();
});

app.Run();
```

## Documentation for Verification

Every delivery carries `x-docspace-signature-256`, an HMAC-SHA256 of the raw body keyed with the subscription's secret, formatted as `sha256=` followed by uppercase hexadecimal.

`WebhookSignature.Verify` handles the three details that are easy to get wrong: it hashes the raw body, compares in constant time, and folds case — DocSpace emits uppercase hexadecimal where some other services emit lowercase.

Two further headers, `x-docspace-event-id` and `x-docspace-event-timestamp`, repeat `event.id` and `event.createOn` so that a stale or already-seen delivery can be dropped without reading the body. They are **not** covered by the signature: reject on them freely, but never accept on them. The values inside the verified body are the authoritative ones.

See [RECEIVING.md](RECEIVING.md) for the full sequence, including replay and duplicate handling.

<a id="documentation-for-api-endpoints"></a>
## Documentation for Events

This package exposes no API endpoints; it decodes what DocSpace sends. The events you can subscribe to, and the payload each one delivers, are listed in [TRIGGERS.md](TRIGGERS.md).

`WebhookTriggers.PayloadTypes` maps each event to its payload type at runtime. Call `WebhookTriggers.IsKnown(trigger)` before trusting one — DocSpace adds events over time, and an event introduced after this build will not be in the table. `DocSpaceWebhook.RawPayloadJson` always holds the body as received, whether or not the event was recognised.

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

