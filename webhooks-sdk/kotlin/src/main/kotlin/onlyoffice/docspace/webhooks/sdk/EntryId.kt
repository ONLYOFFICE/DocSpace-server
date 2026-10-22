package onlyoffice.docspace.webhooks.sdk

import com.squareup.moshi.FromJson
import com.squareup.moshi.JsonDataException
import com.squareup.moshi.JsonReader
import com.squareup.moshi.JsonWriter
import com.squareup.moshi.ToJson

/**
 * Entry identifier: a number for entries in DocSpace's own storage, a string for entries backed by a
 * third-party provider. Both occur on the same portal.
 *
 * Hand-written because the Kotlin generator renders the contract's `oneOf[integer, string]` as an empty
 * class that drops the value. The generator maps the `EntryId` schema onto this type (`--schema-mappings`).
 */
class EntryId private constructor(
    /** The id as it was on the wire: a [Long] or a [String]. */
    val value: Any,
) {
    /** The numeric id, or null for a third-party entry. */
    val asLong: Long? get() = value as? Long

    override fun equals(other: Any?): Boolean = other is EntryId && other.value == value

    override fun hashCode(): Int = value.hashCode()

    override fun toString(): String = value.toString()

    companion object {
        fun of(value: Long): EntryId = EntryId(value)

        fun of(value: String): EntryId = EntryId(value)
    }
}

/** Register with `Moshi.Builder().add(EntryIdAdapter())`. */
class EntryIdAdapter {
    @FromJson
    fun fromJson(reader: JsonReader): EntryId? = when (reader.peek()) {
        JsonReader.Token.NUMBER -> EntryId.of(reader.nextLong())
        JsonReader.Token.STRING -> EntryId.of(reader.nextString())
        JsonReader.Token.NULL -> reader.nextNull<EntryId>()
        else -> throw JsonDataException("Expected a number or a string for an entry id at ${reader.path}")
    }

    @ToJson
    fun toJson(writer: JsonWriter, value: EntryId?) {
        when (val raw = value?.value) {
            null -> writer.nullValue()
            is Long -> writer.value(raw)
            else -> writer.value(raw.toString())
        }
    }
}
