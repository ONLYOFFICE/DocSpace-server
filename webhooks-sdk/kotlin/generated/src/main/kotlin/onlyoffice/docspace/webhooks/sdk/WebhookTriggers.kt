// Trigger -> payload dispatch, generated from docspace-webhooks.yaml by
// tools/gen-trigger-map.py. Do not edit; regenerate the webhook SDKs instead.
package onlyoffice.docspace.webhooks.sdk

import onlyoffice.docspace.webhooks.sdk.models.FilePayload
import onlyoffice.docspace.webhooks.sdk.models.FolderPayload
import onlyoffice.docspace.webhooks.sdk.models.FormSubmitPayload
import onlyoffice.docspace.webhooks.sdk.models.GroupPayload
import onlyoffice.docspace.webhooks.sdk.models.RoomPayload
import onlyoffice.docspace.webhooks.sdk.models.UserPayload

/** Which payload model each DocSpace trigger carries. */
object WebhookTriggers {
    /** Trigger name to payload model, for every trigger with a typed body. */
    val payloadTypes: Map<String, Class<*>> = linkedMapOf(
        "user.created" to UserPayload::class.java,
        "user.invited" to UserPayload::class.java,
        "user.updated" to UserPayload::class.java,
        "user.deleted" to UserPayload::class.java,
        "group.created" to GroupPayload::class.java,
        "group.updated" to GroupPayload::class.java,
        "group.deleted" to GroupPayload::class.java,
        "file.created" to FilePayload::class.java,
        "file.uploaded" to FilePayload::class.java,
        "file.updated" to FilePayload::class.java,
        "file.trashed" to FilePayload::class.java,
        "file.deleted" to FilePayload::class.java,
        "file.restored" to FilePayload::class.java,
        "file.copied" to FilePayload::class.java,
        "file.moved" to FilePayload::class.java,
        "file.downloaded" to FilePayload::class.java,
        "folder.created" to FolderPayload::class.java,
        "folder.updated" to FolderPayload::class.java,
        "folder.trashed" to FolderPayload::class.java,
        "folder.deleted" to FolderPayload::class.java,
        "folder.restored" to FolderPayload::class.java,
        "folder.copied" to FolderPayload::class.java,
        "folder.moved" to FolderPayload::class.java,
        "folder.downloaded" to FolderPayload::class.java,
        "room.created" to RoomPayload::class.java,
        "room.updated" to RoomPayload::class.java,
        "room.archived" to RoomPayload::class.java,
        "room.deleted" to RoomPayload::class.java,
        "room.restored" to RoomPayload::class.java,
        "room.copied" to RoomPayload::class.java,
        "form.submit" to FormSubmitPayload::class.java,
        "form.filled.out" to FilePayload::class.java,
        "form.stopped" to FilePayload::class.java,
        "agent.created" to RoomPayload::class.java,
        "agent.updated" to RoomPayload::class.java,
        "agent.deleted" to RoomPayload::class.java,
    )

    /** The legacy catch-all: sent only when the server cannot read a stored payload; no typed body. */
    val untypedTriggers: Set<String> = setOf("*")

    /** True when the trigger is in the contract this build was generated from, the catch-all included. */
    fun isKnown(trigger: String): Boolean = trigger in payloadTypes || trigger in untypedTriggers

    /** The payload model of the trigger, or null when it has no typed body. */
    fun payloadType(trigger: String): Class<*>? = payloadTypes[trigger]
}
