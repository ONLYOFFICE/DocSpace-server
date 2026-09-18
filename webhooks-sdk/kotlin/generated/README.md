# onlyoffice.docspace.webhooks.sdk

The ONLYOFFICE DocSpace Webhooks SDK for Kotlin is a library that provides tools for receiving webhook deliveries from DocSpace. It decodes each delivery into ready-to-use models.

It is the inbound counterpart to the DocSpace API SDK: that library calls DocSpace, this one handles what DocSpace sends you. It makes no requests of its own.

For more information, please visit [https://helpdesk.onlyoffice.com/hc/en-us](https://helpdesk.onlyoffice.com/hc/en-us)

## Requirements

Java 17+, Kotlin 2.0+

## Installation

```xml
<dependency>
  <groupId>com.onlyoffice</groupId>
  <artifactId>docspace-webhooks-sdk-kotlin</artifactId>
  <version>1.0.0</version>
</dependency>
```

The models are serialized with [Moshi](https://github.com/square/moshi); add `com.squareup.moshi:moshi-kotlin` alongside it for the reflection adapter.

## Usage

A receiver does two things, in this order: verify the signature, then decode.

Verify against the **raw request body**. A body that has been parsed and re-serialized is a different byte sequence and will never match, so hash the bytes that arrived before decoding them.

Answer as soon as the signature checks out and do the real work afterwards. A delivery gets five attempts over roughly 31 seconds, after which it is abandoned; a subscription that goes three days without a single successful delivery is switched off.

## Getting Started

```kotlin
import com.squareup.moshi.Moshi
import com.squareup.moshi.Types
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory
import com.sun.net.httpserver.HttpServer

import onlyoffice.docspace.webhooks.sdk.models.FileEntryPayload
import onlyoffice.docspace.webhooks.sdk.models.UserPayload

import java.net.InetSocketAddress
import java.security.MessageDigest
import java.util.HexFormat
import javax.crypto.Mac
import javax.crypto.spec.SecretKeySpec

const val SECRET = "YOUR_SUBSCRIPTION_SECRET_KEY"

/**
 * Constant-time, case-insensitive check over the raw body. DocSpace emits
 * UPPERCASE hexadecimal where some other services emit lowercase.
 */
fun verify(body: ByteArray, signature: String?): Boolean {
    if (signature.isNullOrBlank()) return false
    val mac = Mac.getInstance("HmacSHA256")
    mac.init(SecretKeySpec(SECRET.toByteArray(), "HmacSHA256"))
    val expected = "sha256=" + HexFormat.of().formatHex(mac.doFinal(body))
    return MessageDigest.isEqual(
        expected.lowercase().toByteArray(),
        signature.trim().lowercase().toByteArray(),
    )
}

private val moshi = Moshi.Builder().add(KotlinJsonAdapterFactory()).build()

private val mapAdapter = moshi.adapter<Map<String, Any?>>(
    Types.newParameterizedType(Map::class.java, String::class.java, Any::class.java)
)

fun main() {
    val server = HttpServer.create(InetSocketAddress(5555), 0)

    server.createContext("/webhook") { exchange ->
        val body = exchange.requestBody.readAllBytes()
        val signature = exchange.requestHeaders.getFirst("x-docspace-signature-256")

        if (!verify(body, signature)) {
            exchange.sendResponseHeaders(401, -1)
            exchange.close()
            return@createContext
        }

        // Acknowledge now; anything slow belongs after this point.
        exchange.sendResponseHeaders(200, -1)
        exchange.close()

        val envelope = mapAdapter.fromJson(String(body)) ?: return@createContext

        @Suppress("UNCHECKED_CAST")
        val trigger = (envelope["event"] as? Map<String, Any?>)?.get("trigger") as? String
            ?: return@createContext
        val payload = envelope["payload"]

        if (trigger.startsWith("user.")) {
            val user = moshi.adapter(UserPayload::class.java).fromJsonValue(payload)
            println("$trigger: ${user?.userName}")
        } else {
            // Files, folders, rooms, agents and forms share one payload shape.
            // fileEntryType tells them apart: 1 folder, 2 file.
            val entry = moshi.adapter(FileEntryPayload::class.java).fromJsonValue(payload)
            val kind = if (entry?.fileEntryType == 2) "file" else "folder"
            println("$trigger: $kind \"${entry?.title}\"")
        }
    }

    server.start()
}
```

## Documentation for Verification

Every delivery carries `x-docspace-signature-256`, an HMAC-SHA256 of the raw body keyed with the subscription's secret, formatted as `sha256=` followed by uppercase hexadecimal. Compare it with `MessageDigest.isEqual` and case-insensitively, as above.

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

