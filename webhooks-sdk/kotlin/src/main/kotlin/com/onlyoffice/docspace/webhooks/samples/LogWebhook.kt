package com.onlyoffice.docspace.webhooks.samples

import com.squareup.moshi.JsonAdapter
import com.squareup.moshi.JsonReader
import com.squareup.moshi.JsonWriter
import com.squareup.moshi.Moshi
import com.squareup.moshi.Types
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory

import onlyoffice.docspace.webhooks.sdk.models.EntryId
import onlyoffice.docspace.webhooks.sdk.models.FileEntryPayload
import onlyoffice.docspace.webhooks.sdk.models.GroupPayload
import onlyoffice.docspace.webhooks.sdk.models.UserPayload
import onlyoffice.docspace.webhooks.sdk.models.WebhookConfigInfo
import onlyoffice.docspace.webhooks.sdk.models.WebhookEventInfo

import java.io.File
import java.security.MessageDigest
import java.time.OffsetDateTime
import java.util.HexFormat
import java.util.UUID
import javax.crypto.Mac
import javax.crypto.spec.SecretKeySpec

/**
 * Logs a DocSpace webhook delivery to the console.
 *
 *     mvn -q compile exec:java                                   # shared fixture
 *     mvn -q compile exec:java -Dexec.args="<body> <secret> <sig>"
 *
 * Kotlin has no src/ runtime yet, so signature checking lives here.
 */

const val SIGNATURE_HEADER = "x-docspace-signature-256"

/** `sha256=` + UPPERCASE hex, mirroring WebhookSender.GetSecretHash. */
fun computeSignature(body: ByteArray, secret: String): String {
    val mac = Mac.getInstance("HmacSHA256")
    mac.init(SecretKeySpec(secret.toByteArray(Charsets.UTF_8), "HmacSHA256"))
    return "sha256=" + HexFormat.of().withUpperCase().formatHex(mac.doFinal(body))
}

/**
 * Constant-time, case-insensitive check over the RAW body. DocSpace emits
 * uppercase hex where GitHub emits lowercase, so the comparison folds case.
 */
fun verifySignature(body: ByteArray, secret: String, signature: String?): Boolean {
    if (signature.isNullOrBlank()) return false
    return MessageDigest.isEqual(
        computeSignature(body, secret).lowercase().toByteArray(Charsets.UTF_8),
        signature.trim().lowercase().toByteArray(Charsets.UTF_8),
    )
}

/**
 * The Kotlin generator renders the EntryId oneOf as an empty class, so the
 * scalar it wraps has nowhere to go. Without this adapter Moshi walks into a
 * bare number expecting an object and throws. The value is read from the raw
 * JSON instead -- see the id/parent line below.
 */
private val entryIdAdapter = object : JsonAdapter<EntryId>() {
    override fun fromJson(reader: JsonReader): EntryId {
        reader.skipValue()
        return EntryId()
    }

    override fun toJson(writer: JsonWriter, value: EntryId?) {
        writer.nullValue()
    }
}

private class TimeAdapters {
    @com.squareup.moshi.FromJson
    fun dateFromJson(value: String): OffsetDateTime = OffsetDateTime.parse(value)

    @com.squareup.moshi.ToJson
    fun dateToJson(value: OffsetDateTime): String = value.toString()

    @com.squareup.moshi.FromJson
    fun uuidFromJson(value: String): UUID = UUID.fromString(value)

    @com.squareup.moshi.ToJson
    fun uuidToJson(value: UUID): String = value.toString()
}

private val moshi: Moshi = Moshi.Builder()
    .add(EntryId::class.java, entryIdAdapter)
    .add(TimeAdapters())
    // Reflection-based: the models carry no @JsonClass(generateAdapter = true),
    // so no kapt/KSP codegen step is needed.
    .add(KotlinJsonAdapterFactory())
    .build()

private val mapAdapter: JsonAdapter<Map<String, Any?>> = moshi.adapter(
    Types.newParameterizedType(Map::class.java, String::class.java, Any::class.java)
)

fun main(args: Array<String>) {
    val fixtures = File("..", "samples")

    val bodyFile = if (args.size > 0) File(args[0]) else File(fixtures, "file.created.json")
    val secret = if (args.size > 1) args[1] else File(fixtures, "SECRET").readText().trim()
    val signature =
        if (args.size > 2) args[2] else File(fixtures, "file.created.sig").readText().trim()

    // Bytes, not a re-encoded object: the signature covers what arrived.
    val body = bodyFile.readBytes()

    if (!verifySignature(body, secret, signature)) {
        System.err.println("REJECTED: signature does not match")
        kotlin.system.exitProcess(1)
    }

    val envelope = mapAdapter.fromJson(String(body, Charsets.UTF_8))
        ?: run {
            System.err.println("body is not a webhook envelope")
            kotlin.system.exitProcess(1)
        }

    @Suppress("UNCHECKED_CAST")
    val eventMap = envelope["event"] as? Map<String, Any?>
    val trigger = eventMap?.get("trigger") as? String
    if (trigger == null) {
        System.err.println("envelope has no event.trigger")
        kotlin.system.exitProcess(1)
    }

    val event = moshi.adapter(WebhookEventInfo::class.java).fromJsonValue(envelope["event"])
    val config = moshi.adapter(WebhookConfigInfo::class.java).fromJsonValue(envelope["webhook"])
    val payloadValue = envelope["payload"]

    println(trigger)
    println("  signature ok")
    println("  event #${event?.id}  at ${event?.createOn}  by ${event?.createBy}")
    println("  subscription #${config?.id} \"${config?.name}\"")

    @Suppress("UNCHECKED_CAST")
    val payloadMap = payloadValue as? Map<String, Any?> ?: emptyMap()

    when {
        trigger.startsWith("user.") -> {
            val u = moshi.adapter(UserPayload::class.java).fromJsonValue(payloadValue)
            println("  user: ${u?.userName} <${u?.email}>")
        }
        trigger.startsWith("group.") -> {
            val g = moshi.adapter(GroupPayload::class.java).fromJsonValue(payloadValue)
            println("  group: ${g?.name}")
        }
        else -> {
            // file.*, folder.*, room.*, agent.* and form.* all arrive as the
            // FileEntry<T> base; fileEntryType is the only discriminator.
            val e = moshi.adapter(FileEntryPayload::class.java).fromJsonValue(payloadValue)
            val kind = if (e?.fileEntryType == 2) "file" else "folder"
            // Ids come from the raw map, not the model: see entryIdAdapter.
            println(
                "  $kind: ${e?.title}  id=${fmt(payloadMap["id"])}" +
                    "  parent=${fmt(payloadMap["parentId"])}"
            )
        }
    }

    @Suppress("UNCHECKED_CAST")
    val printable = normalize(payloadMap) as Map<String, Any?>

    println()
    println("  " + mapAdapter.indent("  ").toJson(printable).replace("\n", "\n  "))
}

/**
 * Moshi decodes every JSON number as Double, so an id round-trips as 3358285.0.
 * Walk the tree and put integral values back to Long before printing.
 */
private fun normalize(value: Any?): Any? = when (value) {
    is Map<*, *> -> value.entries.associate { (k, v) -> k.toString() to normalize(v) }
    is List<*> -> value.map { normalize(it) }
    is Double -> if (value == Math.floor(value) && !value.isInfinite()) value.toLong() else value
    else -> value
}

/** Moshi decodes every JSON number as Double; print integral ids without the .0 */
private fun fmt(value: Any?): String = when (value) {
    null -> "<none>"
    is Double -> if (value == Math.floor(value)) value.toLong().toString() else value.toString()
    else -> value.toString()
}
