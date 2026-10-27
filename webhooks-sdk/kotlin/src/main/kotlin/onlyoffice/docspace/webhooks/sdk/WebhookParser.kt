package onlyoffice.docspace.webhooks.sdk

import com.squareup.moshi.JsonAdapter
import com.squareup.moshi.JsonDataException
import com.squareup.moshi.Types

import onlyoffice.docspace.webhooks.sdk.models.WebhookConfigInfo
import onlyoffice.docspace.webhooks.sdk.models.WebhookEventInfo

import java.io.IOException

/** Thrown when a body is not a DocSpace webhook envelope. */
class WebhookParseException(message: String, cause: Throwable? = null) : Exception(message, cause)

/** One delivery, with its payload typed where the trigger is known. */
class DocSpaceWebhook(
    /** e.g. `file.created`. */
    val trigger: String,
    val event: WebhookEventInfo?,
    val webhook: WebhookConfigInfo?,
    /**
     * The payload read into the model this trigger carries ([WebhookTriggers.payloadType]), or null when the
     * trigger is not one this build knows, or is the untyped legacy catch-all `*`.
     */
    val payload: Any?,
    /**
     * The payload exactly as received, as a JSON value: maps, lists, strings, booleans and numbers, an integral
     * number as a [Long] rather than the [Double] Moshi reads.
     */
    val rawPayload: Any?,
) {
    /**
     * False when DocSpace sent a trigger added after this SDK was built. The
     * payload is then available only through [rawPayload].
     */
    val isKnownTrigger: Boolean get() = WebhookTriggers.isKnown(trigger)
}

/** Turns a verified request body into a [DocSpaceWebhook] or the delivery model of its trigger. */
object WebhookParser {
    private val mapAdapter: JsonAdapter<Map<String, Any?>> = WebhookJson.moshi.adapter(
        Types.newParameterizedType(Map::class.java, String::class.java, Any::class.java)
    )

    /**
     * Parses a delivery body. Call this only on a body that has already passed [WebhookSignature.verify]:
     * parsing first and verifying afterwards defeats the signature.
     *
     * An unrecognised trigger is deliberately not an error: DocSpace gains triggers over time, and a receiver
     * that throws on an unfamiliar one breaks the moment the server is upgraded.
     */
    @Throws(WebhookParseException::class)
    fun parse(body: ByteArray): DocSpaceWebhook {
        val (root, trigger) = readEnvelope(body)
        val payloadValue = root["payload"]

        val payload = WebhookTriggers.payloadType(trigger)
            ?.takeIf { payloadValue != null }
            ?.let { fromValue(payloadValue, it, "payload did not match the schema for '$trigger'") }

        return DocSpaceWebhook(
            trigger = trigger,
            event = fromValue(root["event"], WebhookEventInfo::class.java, "event did not match the schema"),
            webhook = fromValue(root["webhook"], WebhookConfigInfo::class.java, "webhook did not match the schema"),
            payload = payload,
            rawPayload = normalize(payloadValue),
        )
    }

    /**
     * Parses a delivery body into the model of the whole delivery its trigger carries -- `FileDelivery` for
     * `file.created`, and so on, as [WebhookTriggers.deliveryType] pairs them.
     *
     * @return the delivery, or null when the trigger has no typed body: the untyped catch-all `*`, or a trigger
     *   added after this SDK was built. As with [parse], that is not an error.
     * @throws WebhookParseException the body is not an envelope, or a typed trigger arrived without its payload
     *   object.
     */
    @Throws(WebhookParseException::class)
    fun parseDelivery(body: ByteArray): Any? {
        val (root, trigger) = readEnvelope(body)
        val type = WebhookTriggers.deliveryType(trigger) ?: return null

        // a typed trigger always carries its payload object; without it the model
        // would come back with a null payload the caller has no reason to expect
        if (root["payload"] !is Map<*, *>) {
            throw WebhookParseException("no payload object for '$trigger', delivered as ${type.simpleName}")
        }

        return read("body did not match ${type.simpleName} for '$trigger'") {
            WebhookJson.moshi.adapter(type).fromJson(String(body, Charsets.UTF_8))
        }
    }

    private fun readEnvelope(body: ByteArray): Pair<Map<String, Any?>, String> {
        val root = try {
            mapAdapter.fromJson(String(body, Charsets.UTF_8))
        } catch (e: JsonDataException) {
            throw WebhookParseException("body is not a JSON object", e)
        } catch (e: IOException) {
            throw WebhookParseException("body is not a JSON object", e)
        } ?: throw WebhookParseException("body is not a JSON object")

        val trigger = (root["event"] as? Map<*, *>)?.get("trigger") as? String
        if (trigger.isNullOrEmpty()) {
            throw WebhookParseException("envelope has no event.trigger")
        }

        return root to trigger
    }

    private fun <T> fromValue(value: Any?, type: Class<T>, message: String): T? =
        read(message) { WebhookJson.moshi.adapter(type).fromJsonValue(value) }

    private inline fun <T> read(message: String, block: () -> T): T = try {
        block()
    } catch (e: JsonDataException) {
        throw WebhookParseException(message, e)
    } catch (e: IOException) {
        throw WebhookParseException(message, e)
    } catch (e: IllegalArgumentException) {
        // fromJsonValue reports a value of the wrong shape this way
        throw WebhookParseException(message, e)
    }

    /** Moshi reads every JSON number as a Double, so an id would come out as 3358285.0. */
    private fun normalize(value: Any?): Any? = when (value) {
        is Map<*, *> -> value.entries.associate { (k, v) -> k.toString() to normalize(v) }
        is List<*> -> value.map { normalize(it) }
        is Double -> if (value == Math.floor(value) && !value.isInfinite() && Math.abs(value) < 9.0E15) value.toLong() else value
        else -> value
    }
}
