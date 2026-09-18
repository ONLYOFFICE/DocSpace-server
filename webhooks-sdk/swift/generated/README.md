# DocSpaceWebhooksSDK

The ONLYOFFICE DocSpace Webhooks SDK for Swift is a library that provides tools for receiving webhook deliveries from DocSpace. It decodes each delivery into ready-to-use models.

It is the inbound counterpart to the DocSpace API SDK: that library calls DocSpace, this one handles what DocSpace sends you. It makes no requests of its own.

For more information, please visit [https://helpdesk.onlyoffice.com/hc/en-us](https://helpdesk.onlyoffice.com/hc/en-us)

## Requirements

Swift 5.9+

## Installation

Add the package to your `Package.swift`:

```swift
dependencies: [
    .package(url: "https://github.com/ONLYOFFICE/docspace-webhooks-sdk-swift.git", from: "1.0.0"),
    // Foundation has no HMAC; swift-crypto provides it on every platform.
    .package(url: "https://github.com/apple/swift-crypto.git", from: "3.0.0")
]
```

## Usage

A receiver does two things, in this order: verify the signature, then decode.

Verify against the **raw request body**. A body that has been decoded and re-encoded is a different byte sequence and will never match, so hash the `Data` that arrived before passing it to `JSONDecoder`.

Answer as soon as the signature checks out and do the real work afterwards. A delivery gets five attempts over roughly 31 seconds, after which it is abandoned; a subscription that goes three days without a single successful delivery is switched off.

## Getting Started

The handler below is framework-agnostic: pass it the raw body and the signature header from whichever server you use.

```swift
import Foundation
import Crypto
import DocSpaceWebhooksSDK

let secret = "YOUR_SUBSCRIPTION_SECRET_KEY"

/// Decodes the hexadecimal digest and lets CryptoKit do the comparison, which
/// is constant-time. Parsing hex also makes the check case-insensitive, and
/// that matters: DocSpace emits UPPERCASE where some other services emit lower.
func verify(body: Data, signature: String?) -> Bool {
    guard let signature,
          signature.lowercased().hasPrefix("sha256=") else { return false }

    let hex = signature.dropFirst("sha256=".count)
    var expected = Data(capacity: hex.count / 2)
    var index = hex.startIndex
    while index < hex.endIndex {
        let next = hex.index(index, offsetBy: 2)
        guard let byte = UInt8(hex[index..<next], radix: 16) else { return false }
        expected.append(byte)
        index = next
    }

    return HMAC<SHA256>.isValidAuthenticationCode(
        expected,
        authenticating: body,
        using: SymmetricKey(data: Data(secret.utf8)))
}

struct Envelope: Decodable {
    let event: WebhookEventInfo?
}

func handle(body: Data, signature: String?) throws -> Bool {
    guard verify(body: body, signature: signature) else {
        return false          // answer 401
    }

    let decoder = JSONDecoder()
    // The contract emits UTC ISO-8601; Date properties need this.
    decoder.dateDecodingStrategy = .iso8601

    let envelope = try decoder.decode(Envelope.self, from: body)
    let trigger = envelope.event?.trigger ?? ""

    guard let root = try JSONSerialization.jsonObject(with: body) as? [String: Any] else {
        return true
    }
    let payload = try JSONSerialization.data(withJSONObject: root["payload"] ?? NSNull())

    if trigger.hasPrefix("user.") {
        let user = try decoder.decode(UserPayload.self, from: payload)
        print("\(trigger): \(user.userName ?? "")")
    } else {
        // Files, folders, rooms, agents and forms share one payload shape.
        // fileEntryType tells them apart: 1 folder, 2 file.
        let entry = try decoder.decode(FileEntryPayload.self, from: payload)
        let kind = entry.fileEntryType == 2 ? "file" : "folder"
        print("\(trigger): \(kind) \"\(entry.title ?? "")\"")
    }

    return true               // answer 200, then do the real work
}
```

Entry identifiers decode as the `EntryId` enum, because the contract declares them as `oneOf(integer, string)`:

```swift
switch entry.id {
case .typeInt(let value):    print(value)
case .typeString(let value): print(value)
case .none:                  break
}
```

## Documentation for Verification

Every delivery carries `x-docspace-signature-256`, an HMAC-SHA256 of the raw body keyed with the subscription's secret, formatted as `sha256=` followed by uppercase hexadecimal.

Two further headers, `x-docspace-event-id` and `x-docspace-event-timestamp`, repeat `event.id` and `event.createOn` so a stale or already-seen delivery can be dropped without reading the body. They are **not** covered by the signature: reject on them freely, but never accept on them. The values inside the verified body are the authoritative ones.

Deduplicate on `event.id`. It is stable across the server's automatic retries, though a manual retry by an administrator creates a new record and therefore a new id.

## Documentation for Events

`event.trigger` names the event that caused the delivery, for example `file.created`. `GET api/2.0/settings/webhook/triggers` lists every event, the value to subscribe with, and whether your role may subscribe to it.

Files, folders, rooms, agents and forms all deliver the same payload shape; use `fileEntryType` (1 folder, 2 file) to tell them apart. A file's name arrives in `title`, and a field missing from the JSON means empty, `false` or zero — never "unknown".

New events are added over time. Treat an unfamiliar `event.trigger` as something to ignore rather than an error, so an upgrade cannot break your endpoint.

## Documentation for Models

 - [EntryId](docs/EntryId.md)
 - [FileEntryPayload](docs/FileEntryPayload.md)
 - [FormSubmitPayload](docs/FormSubmitPayload.md)
 - [GroupPayload](docs/GroupPayload.md)
 - [UserPayload](docs/UserPayload.md)
 - [WebhookConfigInfo](docs/WebhookConfigInfo.md)
 - [WebhookEnvelope](docs/WebhookEnvelope.md)
 - [WebhookEventInfo](docs/WebhookEventInfo.md)
 - [WebhookTargetInfo](docs/WebhookTargetInfo.md)

