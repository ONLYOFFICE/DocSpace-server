# docspace-webhooks-sdk

The ONLYOFFICE DocSpace Webhooks SDK for Ruby is a library that provides tools for receiving webhook deliveries from DocSpace. It decodes each delivery into ready-to-use models.

It is the inbound counterpart to the DocSpace API SDK: that library calls DocSpace, this one handles what DocSpace sends you. It makes no requests of its own.

For more information, please visit [https://helpdesk.onlyoffice.com/hc/en-us](https://helpdesk.onlyoffice.com/hc/en-us)

## Requirements

Ruby 2.7+

## Installation

```
gem install docspace-webhooks-sdk
```

Or add it to your `Gemfile`:

```ruby
gem 'docspace-webhooks-sdk'
```

## Usage

A receiver does two things, in this order: verify the signature, then decode.

Verify against the **raw request body**. A body that has been parsed and re-serialized is a different byte sequence and will never match, so read the raw input before calling `JSON.parse`.

Answer as soon as the signature checks out and do the real work afterwards. A delivery gets five attempts over roughly 31 seconds, after which it is abandoned; a subscription that goes three days without a single successful delivery is switched off.

## Getting Started

```ruby
require 'json'
require 'openssl'
require 'sinatra'
require 'docspace-webhooks-sdk'

SECRET = 'YOUR_SUBSCRIPTION_SECRET_KEY'

# Constant-time, case-insensitive check over the raw body. DocSpace emits
# UPPERCASE hexadecimal where some other services emit lowercase.
def verify(body, signature)
  return false if signature.nil? || signature.strip.empty?

  expected = "sha256=#{OpenSSL::HMAC.hexdigest('SHA256', SECRET, body)}"
  OpenSSL.secure_compare(expected.downcase, signature.strip.downcase)
end

post '/webhook' do
  body = request.body.read

  halt 401 unless verify(body, request.env['HTTP_X_DOCSPACE_SIGNATURE_256'])

  envelope = JSON.parse(body)
  event = DocspaceWebhooksSdk::WebhookEventInfo.build_from_hash(envelope['event'])
  trigger = event.trigger

  if trigger.start_with?('user.')
    user = DocspaceWebhooksSdk::UserPayload.build_from_hash(envelope['payload'])
    logger.info format('%s: %s <%s>', trigger, user.user_name, user.email)
  else
    # Files, folders, rooms, agents and forms share one payload shape.
    # file_entry_type tells them apart: 1 folder, 2 file.
    entry = DocspaceWebhooksSdk::FileEntryPayload.build_from_hash(envelope['payload'])
    kind = entry.file_entry_type == 2 ? 'file' : 'folder'
    logger.info format('%s: %s "%s"', trigger, kind, entry.title)
  end

  # Acknowledge now; anything slow belongs after this point.
  status 200
  body ''
end
```

## Documentation for Verification

Every delivery carries `x-docspace-signature-256`, an HMAC-SHA256 of the raw body keyed with the subscription's secret, formatted as `sha256=` followed by uppercase hexadecimal. Compare it with `OpenSSL.secure_compare` and case-insensitively, as above.

Two further headers, `x-docspace-event-id` and `x-docspace-event-timestamp`, repeat `event.id` and `event.createOn` so a stale or already-seen delivery can be dropped without reading the body. They are **not** covered by the signature: reject on them freely, but never accept on them. The values inside the verified body are the authoritative ones.

Deduplicate on `event.id`. It is stable across the server's automatic retries, though a manual retry by an administrator creates a new record and therefore a new id.

## Documentation for Events

`event.trigger` names the event that caused the delivery, for example `file.created`. `GET api/2.0/settings/webhook/triggers` lists every event, the value to subscribe with, and whether your role may subscribe to it.

Files, folders, rooms, agents and forms all deliver the same payload shape; use `file_entry_type` (1 folder, 2 file) to tell them apart. A file's name arrives in `title`, and a field missing from the JSON means empty, `false` or zero — never "unknown".

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

