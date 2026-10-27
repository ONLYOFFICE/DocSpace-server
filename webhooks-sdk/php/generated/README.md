# onlyoffice/docspace-webhooks-sdk

The ONLYOFFICE DocSpace Webhooks SDK for PHP is a library that provides tools for receiving webhook deliveries from DocSpace. It decodes each delivery into ready-to-use models.

It is the inbound counterpart to the DocSpace API SDK: that library calls DocSpace, this one handles what DocSpace sends you. It makes no requests of its own.

For more information, please visit [https://helpdesk.onlyoffice.com/hc/en-us](https://helpdesk.onlyoffice.com/hc/en-us)

## Requirements

PHP 8.1+

## Installation

```
composer require onlyoffice/docspace-webhooks-sdk
```

## Usage

A receiver does two things, in this order: verify the signature, then decode.

Verify against the **raw request body** — `file_get_contents('php://input')`, not `$_POST`. A body that has been parsed and re-encoded is a different byte sequence and will never match.

Answer as soon as the signature checks out and do the real work afterwards: the server waits 30 seconds for a response. A failed delivery is retried after about 1 min, 5 min, 30 min, 2 h, 5 h and 10 h (each ±10%) — seven attempts over roughly 18 hours by default — after which it is abandoned; a subscription that goes three days without a single successful delivery is switched off.

## Getting Started

```php
<?php

require_once __DIR__ . '/vendor/autoload.php';

use OnlyOffice\DocSpace\Webhooks\Sdk\Model\FileEntryPayload;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\UserPayload;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\WebhookEventInfo;
use OnlyOffice\DocSpace\Webhooks\Sdk\ObjectSerializer;

const SECRET = 'YOUR_SUBSCRIPTION_SECRET_KEY';

/**
 * Constant-time, case-insensitive check over the raw body. DocSpace emits
 * UPPERCASE hexadecimal where some other services emit lowercase.
 */
function verify(string $body, ?string $signature): bool
{
    if ($signature === null || $signature === '') {
        return false;
    }
    $expected = 'sha256=' . hash_hmac('sha256', $body, SECRET);
    return hash_equals(strtolower($expected), strtolower(trim($signature)));
}

// The signature covers exactly these bytes.
$body = file_get_contents('php://input');
$signature = $_SERVER['HTTP_X_DOCSPACE_SIGNATURE_256'] ?? null;

if (!verify($body, $signature)) {
    http_response_code(401);
    exit;
}

// Acknowledge now; anything slow belongs after this point.
http_response_code(200);

$envelope = json_decode($body);
$event = ObjectSerializer::deserialize($envelope->event, WebhookEventInfo::class);
$trigger = $event->getTrigger();

if (str_starts_with($trigger, 'user.')) {
    $user = ObjectSerializer::deserialize($envelope->payload, UserPayload::class);
    error_log(sprintf('%s: %s', $trigger, $user->getUserName()));
} else {
    // Files, folders and rooms each have their own schema; they share the fields
    // of FileEntryPayload, which is enough for a generic log line. Deserialize
    // FilePayload / FolderPayload / RoomPayload for the rest.
    $entry = ObjectSerializer::deserialize($envelope->payload, FileEntryPayload::class);
    $kind = $entry->getFileEntryType() === 2 ? 'file' : 'folder';
    error_log(sprintf('%s: %s "%s"', $trigger, $kind, $entry->getTitle()));
}
```

Entry identifiers — `id`, `parentId` and the other ids of an entry, and `webhook.target.id` — are typed `mixed`: an `int` for entries in DocSpace's own storage, a `string` for entries backed by a third-party provider. Both occur on the same portal.

## Documentation for Verification

Every delivery carries `x-docspace-signature-256`, an HMAC-SHA256 of the raw body keyed with the subscription's secret, formatted as `sha256=` followed by uppercase hexadecimal. Compare it with `hash_equals` and case-insensitively, as above.

Two further headers, `x-docspace-event-id` and `x-docspace-event-timestamp`, repeat `event.id` and `event.createOn` so an already-seen delivery can be dropped without reading the body. They are **not** covered by the signature: reject on them freely, but never accept on them. The values inside the verified body are the authoritative ones. The signature covers the body only; it does not prove when the request was sent, and `event.createOn` stays the same on every automatic and manual retry, so deduplicate on `event.id` instead of rejecting a delivery for its age.

Deduplicate on `event.id`. It is stable across every resend of one delivery — the server's automatic retries and a manual retry from the delivery log alike — so keep the ids you have seen for longer than the ~18-hour retry window: a manual retry can bring the same event back days later.

## Documentation for Events

`event.trigger` names the event that caused the delivery, for example `file.created`. `GET api/2.0/settings/webhook/triggers` lists every event, the value to subscribe with, and whether your role may subscribe to it.

Files, folders, rooms, agents and forms all deliver the same payload shape; use `fileEntryType` (1 folder, 2 file) to tell them apart. A file's name arrives in `title`, and a field missing from the JSON means empty, `false` or zero — never "unknown".

New events are added over time. Treat an unfamiliar `event.trigger` as something to ignore rather than an error, so an upgrade cannot break your endpoint.

## Documentation for Models

 - [ContactPayload](docs/Model/ContactPayload.md)
 - [FileDelivery](docs/Model/FileDelivery.md)
 - [FileEntryPayload](docs/Model/FileEntryPayload.md)
 - [FilePayload](docs/Model/FilePayload.md)
 - [FolderDelivery](docs/Model/FolderDelivery.md)
 - [FolderPayload](docs/Model/FolderPayload.md)
 - [FormSubmitDelivery](docs/Model/FormSubmitDelivery.md)
 - [FormSubmitPayload](docs/Model/FormSubmitPayload.md)
 - [GroupDelivery](docs/Model/GroupDelivery.md)
 - [GroupPayload](docs/Model/GroupPayload.md)
 - [GroupSummaryPayload](docs/Model/GroupSummaryPayload.md)
 - [RoomDelivery](docs/Model/RoomDelivery.md)
 - [RoomPayload](docs/Model/RoomPayload.md)
 - [UserDelivery](docs/Model/UserDelivery.md)
 - [UserPayload](docs/Model/UserPayload.md)
 - [UserSummaryPayload](docs/Model/UserSummaryPayload.md)
 - [WebhookConfigInfo](docs/Model/WebhookConfigInfo.md)
 - [WebhookEnvelope](docs/Model/WebhookEnvelope.md)
 - [WebhookEventInfo](docs/Model/WebhookEventInfo.md)
 - [WebhookTargetInfo](docs/Model/WebhookTargetInfo.md)

