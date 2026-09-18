package com.onlyoffice.docspace.webhooks.sdk;

// Trigger -> payload dispatch, generated from docspace-webhooks.yaml by
// tools/gen-trigger-map.py. Do not edit; rerun ./generate.sh instead.

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import com.google.gson.JsonDeserializer;
import com.google.gson.JsonPrimitive;
import com.google.gson.JsonSerializer;

import com.onlyoffice.docspace.webhooks.sdk.model.*;

import java.time.OffsetDateTime;
import java.time.format.DateTimeFormatter;

public final class JSON {

    private static final Gson GSON = new GsonBuilder()
            // Gson has no built-in OffsetDateTime support.
            .registerTypeAdapter(OffsetDateTime.class,
                    (JsonDeserializer<OffsetDateTime>) (json, type, ctx) ->
                            OffsetDateTime.parse(json.getAsString(),
                                    DateTimeFormatter.ISO_OFFSET_DATE_TIME))
            .registerTypeAdapter(OffsetDateTime.class,
                    (JsonSerializer<OffsetDateTime>) (src, type, ctx) ->
                            new JsonPrimitive(
                                    DateTimeFormatter.ISO_OFFSET_DATE_TIME.format(src)))
            .registerTypeAdapterFactory(new EntryId.CustomTypeAdapterFactory())
            .registerTypeAdapterFactory(new FileEntryPayload.CustomTypeAdapterFactory())
            .registerTypeAdapterFactory(new FormSubmitPayload.CustomTypeAdapterFactory())
            .registerTypeAdapterFactory(new GroupPayload.CustomTypeAdapterFactory())
            .registerTypeAdapterFactory(new UserPayload.CustomTypeAdapterFactory())
            .registerTypeAdapterFactory(new WebhookConfigInfo.CustomTypeAdapterFactory())
            .registerTypeAdapterFactory(new WebhookEnvelope.CustomTypeAdapterFactory())
            .registerTypeAdapterFactory(new WebhookEventInfo.CustomTypeAdapterFactory())
            .registerTypeAdapterFactory(new WebhookTargetInfo.CustomTypeAdapterFactory())
            .create();

    private JSON() {
    }

    public static Gson getGson() {
        return GSON;
    }
}
