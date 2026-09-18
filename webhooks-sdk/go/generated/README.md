# docspace_webhooks_sdk

The ONLYOFFICE DocSpace Webhooks SDK for Go is a library that provides tools for receiving webhook deliveries from DocSpace. It decodes each delivery into ready-to-use models.

It is the inbound counterpart to the DocSpace API SDK: that library calls DocSpace, this one handles what DocSpace sends you. It makes no requests of its own.

For more information, please visit [https://helpdesk.onlyoffice.com/hc/en-us](https://helpdesk.onlyoffice.com/hc/en-us)

## Requirements

Go 1.21+

## Installation

```
go get github.com/ONLYOFFICE/docspace-webhooks-sdk-go
```

## Usage

A receiver does two things, in this order: verify the signature, then decode.

Verify against the **raw request body**. A body that has been decoded and re-encoded is a different byte sequence and will never match, so hash what arrived before unmarshalling it.

Answer as soon as the signature checks out and do the real work afterwards. A delivery gets five attempts over roughly 31 seconds, after which it is abandoned; a subscription that goes three days without a single successful delivery is switched off.

## Getting Started

```go
package main

import (
	"crypto/hmac"
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"io"
	"log"
	"net/http"
	"strings"

	sdk "github.com/ONLYOFFICE/docspace-webhooks-sdk-go"
)

const secret = "YOUR_SUBSCRIPTION_SECRET_KEY"

// verify compares in constant time and folds case: DocSpace emits UPPERCASE
// hexadecimal where some other services emit lowercase.
func verify(body []byte, signature string) bool {
	mac := hmac.New(sha256.New, []byte(secret))
	mac.Write(body)
	expected := "sha256=" + strings.ToUpper(hex.EncodeToString(mac.Sum(nil)))
	return hmac.Equal(
		[]byte(strings.ToLower(expected)),
		[]byte(strings.ToLower(strings.TrimSpace(signature))),
	)
}

type envelope struct {
	Event   sdk.WebhookEventInfo `json:"event"`
	Payload json.RawMessage      `json:"payload"`
}

func main() {
	http.HandleFunc("/webhook", func(w http.ResponseWriter, r *http.Request) {
		body, err := io.ReadAll(r.Body)
		if err != nil {
			w.WriteHeader(http.StatusBadRequest)
			return
		}

		if !verify(body, r.Header.Get("x-docspace-signature-256")) {
			w.WriteHeader(http.StatusUnauthorized)
			return
		}

		var env envelope
		if err := json.Unmarshal(body, &env); err != nil {
			w.WriteHeader(http.StatusBadRequest)
			return
		}

		// Acknowledge now; anything slow belongs after this point.
		w.WriteHeader(http.StatusOK)

		trigger := env.Event.GetTrigger()

		if strings.HasPrefix(trigger, "user.") {
			var u sdk.UserPayload
			if json.Unmarshal(env.Payload, &u) == nil {
				log.Printf("%s: %s", trigger, u.GetUserName())
			}
			return
		}

		// Files, folders, rooms, agents and forms share one payload shape.
		// fileEntryType tells them apart: 1 folder, 2 file.
		var e sdk.FileEntryPayload
		if json.Unmarshal(env.Payload, &e) == nil {
			kind := "folder"
			if e.GetFileEntryType() == 2 {
				kind = "file"
			}
			log.Printf("%s: %s %q", trigger, kind, e.GetTitle())
		}
	})

	log.Fatal(http.ListenAndServe(":5555", nil))
}
```

## Documentation for Verification

Every delivery carries `x-docspace-signature-256`, an HMAC-SHA256 of the raw body keyed with the subscription's secret, formatted as `sha256=` followed by uppercase hexadecimal. Compare it in constant time and case-insensitively, as above.

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

