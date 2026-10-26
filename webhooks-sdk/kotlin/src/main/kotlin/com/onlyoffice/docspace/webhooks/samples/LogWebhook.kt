package com.onlyoffice.docspace.webhooks.samples

import com.squareup.moshi.JsonAdapter
import com.squareup.moshi.JsonDataException
import com.squareup.moshi.Moshi
import com.squareup.moshi.Types
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory

import onlyoffice.docspace.webhooks.sdk.EntryIdAdapter
import onlyoffice.docspace.webhooks.sdk.WebhookTriggers
import onlyoffice.docspace.webhooks.sdk.models.FilePayload
import onlyoffice.docspace.webhooks.sdk.models.FolderPayload
import onlyoffice.docspace.webhooks.sdk.models.FormSubmitPayload
import onlyoffice.docspace.webhooks.sdk.models.GroupPayload
import onlyoffice.docspace.webhooks.sdk.models.RoomPayload
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
    // Ids are a number or a string on the wire; see EntryId.
    .add(EntryIdAdapter())
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
    println("  model: ${modelName(trigger)}")
    println("  event #${event?.id}  at ${event?.createOn}  by ${event?.createBy}")
    println("  subscription #${config?.id} \"${config?.name}\"")

    @Suppress("UNCHECKED_CAST")
    val payloadMap = payloadValue as? Map<String, Any?> ?: emptyMap()

    // A parse error stops the sample with a non-zero exit instead of being
    // skipped: a sample that prints "ok" over a payload its model cannot read
    // hides a broken SDK.
    try {
        logPayload(trigger, payloadValue)
    } catch (e: JsonDataException) {
        System.err.println("unparseable $trigger payload: ${e.message}")
        kotlin.system.exitProcess(1)
    }

    @Suppress("UNCHECKED_CAST")
    val printable = normalize(payloadMap) as Map<String, Any?>

    println()
    println("  " + mapAdapter.indent("  ").toJson(printable).replace("\n", "\n  "))
}

/**
 * The payload model the generated trigger table names: "untyped" for the legacy
 * catch-all, "unknown" for a trigger newer than this SDK.
 */
fun modelName(trigger: String): String =
    WebhookTriggers.payloadType(trigger)?.simpleName
        ?: if (trigger in WebhookTriggers.untypedTriggers) "untyped" else "unknown"

/** Parses the payload with the model of its trigger and prints a one-line summary. */
private fun logPayload(trigger: String, payloadValue: Any?) {
    val model = WebhookTriggers.payloadType(trigger)
    if (model == null) {
        // The catch-all, or a trigger added on the server after this SDK was
        // generated: acknowledge it and keep the raw payload rather than
        // parsing it with some other model.
        println("  no payload model for this trigger; raw payload below")
        return
    }

    when (val payload = moshi.adapter(model).fromJsonValue(payloadValue)) {
        is UserPayload -> println("  user: ${payload.userName} <${payload.email}>")
        is GroupPayload -> println("  group: ${payload.name}")
        is FilePayload -> println("  file: ${payload.title}  id=${payload.id ?: "<none>"}  parent=${payload.parentId ?: "<none>"}")
        is FolderPayload -> println("  folder: ${payload.title}  id=${payload.id ?: "<none>"}  parent=${payload.parentId ?: "<none>"}")
        is RoomPayload -> println("  room: ${payload.title}  id=${payload.id ?: "<none>"}  parent=${payload.parentId ?: "<none>"}")
        is FormSubmitPayload -> println("  form: ${payload.originalForm?.title}  submitted as ${payload.submittedForm?.title}")
        else -> throw JsonDataException("the payload is empty")
    }
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

