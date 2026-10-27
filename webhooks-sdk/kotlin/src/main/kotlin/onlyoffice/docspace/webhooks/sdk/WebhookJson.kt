package onlyoffice.docspace.webhooks.sdk

import com.squareup.moshi.FromJson
import com.squareup.moshi.JsonDataException
import com.squareup.moshi.Moshi
import com.squareup.moshi.ToJson
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory

import java.time.OffsetDateTime
import java.time.format.DateTimeParseException
import java.util.UUID

/**
 * The [Moshi] that reads the generated models: entry ids, dates and GUIDs as DocSpace writes them. The router and
 * [WebhookParser] use it; take it for any model of this SDK you read yourself.
 */
object WebhookJson {
    val moshi: Moshi = Moshi.Builder()
        // Ids are a number or a string on the wire; see EntryId.
        .add(EntryIdAdapter())
        .add(TimeAdapters())
        // Reflection-based: the models carry no @JsonClass(generateAdapter = true),
        // so no kapt/KSP codegen step is needed.
        .add(KotlinJsonAdapterFactory())
        .build()
}

/** A malformed date or GUID is a [JsonDataException] like any other mismatch, not an escaping runtime error. */
private class TimeAdapters {
    @FromJson
    fun dateFromJson(value: String): OffsetDateTime = try {
        OffsetDateTime.parse(value)
    } catch (e: DateTimeParseException) {
        throw JsonDataException("not a date: $value", e)
    }

    @ToJson
    fun dateToJson(value: OffsetDateTime): String = value.toString()

    @FromJson
    fun uuidFromJson(value: String): UUID = try {
        UUID.fromString(value)
    } catch (e: IllegalArgumentException) {
        throw JsonDataException("not a GUID: $value", e)
    }

    @ToJson
    fun uuidToJson(value: UUID): String = value.toString()
}
