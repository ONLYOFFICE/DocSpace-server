package com.onlyoffice.docspace.webhooks.sdk;

// Trigger -> payload dispatch, generated from docspace-webhooks.yaml by
// tools/gen-trigger-map.py. Do not edit; regenerate the webhook SDKs instead.

import com.onlyoffice.docspace.webhooks.sdk.model.FilePayload;
import com.onlyoffice.docspace.webhooks.sdk.model.FolderPayload;
import com.onlyoffice.docspace.webhooks.sdk.model.FormSubmitPayload;
import com.onlyoffice.docspace.webhooks.sdk.model.GroupPayload;
import com.onlyoffice.docspace.webhooks.sdk.model.RoomPayload;
import com.onlyoffice.docspace.webhooks.sdk.model.UserPayload;

import java.util.Collections;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Set;

/** Which payload model each DocSpace trigger carries. */
public final class WebhookTriggers {

    /** Trigger name to payload model, for every trigger with a typed body. */
    public static final Map<String, Class<?>> PAYLOAD_TYPES;

    static {
        Map<String, Class<?>> types = new LinkedHashMap<>();
        types.put("user.created", UserPayload.class);
        types.put("user.invited", UserPayload.class);
        types.put("user.updated", UserPayload.class);
        types.put("user.deleted", UserPayload.class);
        types.put("group.created", GroupPayload.class);
        types.put("group.updated", GroupPayload.class);
        types.put("group.deleted", GroupPayload.class);
        types.put("file.created", FilePayload.class);
        types.put("file.uploaded", FilePayload.class);
        types.put("file.updated", FilePayload.class);
        types.put("file.trashed", FilePayload.class);
        types.put("file.deleted", FilePayload.class);
        types.put("file.restored", FilePayload.class);
        types.put("file.copied", FilePayload.class);
        types.put("file.moved", FilePayload.class);
        types.put("file.downloaded", FilePayload.class);
        types.put("folder.created", FolderPayload.class);
        types.put("folder.updated", FolderPayload.class);
        types.put("folder.trashed", FolderPayload.class);
        types.put("folder.deleted", FolderPayload.class);
        types.put("folder.restored", FolderPayload.class);
        types.put("folder.copied", FolderPayload.class);
        types.put("folder.moved", FolderPayload.class);
        types.put("folder.downloaded", FolderPayload.class);
        types.put("room.created", RoomPayload.class);
        types.put("room.updated", RoomPayload.class);
        types.put("room.archived", RoomPayload.class);
        types.put("room.deleted", RoomPayload.class);
        types.put("room.restored", RoomPayload.class);
        types.put("room.copied", RoomPayload.class);
        types.put("form.submit", FormSubmitPayload.class);
        types.put("form.filled.out", FilePayload.class);
        types.put("form.stopped", FilePayload.class);
        types.put("agent.created", RoomPayload.class);
        types.put("agent.updated", RoomPayload.class);
        types.put("agent.deleted", RoomPayload.class);
        PAYLOAD_TYPES = Collections.unmodifiableMap(types);
    }

    /** The legacy catch-all: sent only when the server cannot read a stored payload; no typed body. */
    public static final Set<String> UNTYPED_TRIGGERS = Set.of("*");

    private WebhookTriggers() {
    }

    /** True when the trigger is in the contract this build was generated from, the catch-all included. */
    public static boolean isKnown(String trigger) {
        return trigger != null && (PAYLOAD_TYPES.containsKey(trigger) || UNTYPED_TRIGGERS.contains(trigger));
    }

    /** The payload model of the trigger, or null when it has no typed body. */
    public static Class<?> payloadType(String trigger) {
        return trigger == null ? null : PAYLOAD_TYPES.get(trigger);
    }
}
