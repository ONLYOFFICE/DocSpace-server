// Trigger -> payload dispatch, generated from docspace-webhooks.yaml by
// tools/gen-trigger-map.py. Do not edit; regenerate the webhook SDKs instead.
package onlyoffice.docspace.webhooks.sdk

import onlyoffice.docspace.webhooks.sdk.models.FileDelivery
import onlyoffice.docspace.webhooks.sdk.models.FilePayload
import onlyoffice.docspace.webhooks.sdk.models.FolderDelivery
import onlyoffice.docspace.webhooks.sdk.models.FolderPayload
import onlyoffice.docspace.webhooks.sdk.models.FormSubmitDelivery
import onlyoffice.docspace.webhooks.sdk.models.FormSubmitPayload
import onlyoffice.docspace.webhooks.sdk.models.GroupDelivery
import onlyoffice.docspace.webhooks.sdk.models.GroupPayload
import onlyoffice.docspace.webhooks.sdk.models.RoomDelivery
import onlyoffice.docspace.webhooks.sdk.models.RoomPayload
import onlyoffice.docspace.webhooks.sdk.models.UserDelivery
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

    /** Trigger name to the model of the whole delivery (the envelope with payload typed), for every typed trigger. */
    val deliveryTypes: Map<String, Class<*>> = linkedMapOf(
        "user.created" to UserDelivery::class.java,
        "user.invited" to UserDelivery::class.java,
        "user.updated" to UserDelivery::class.java,
        "user.deleted" to UserDelivery::class.java,
        "group.created" to GroupDelivery::class.java,
        "group.updated" to GroupDelivery::class.java,
        "group.deleted" to GroupDelivery::class.java,
        "file.created" to FileDelivery::class.java,
        "file.uploaded" to FileDelivery::class.java,
        "file.updated" to FileDelivery::class.java,
        "file.trashed" to FileDelivery::class.java,
        "file.deleted" to FileDelivery::class.java,
        "file.restored" to FileDelivery::class.java,
        "file.copied" to FileDelivery::class.java,
        "file.moved" to FileDelivery::class.java,
        "file.downloaded" to FileDelivery::class.java,
        "folder.created" to FolderDelivery::class.java,
        "folder.updated" to FolderDelivery::class.java,
        "folder.trashed" to FolderDelivery::class.java,
        "folder.deleted" to FolderDelivery::class.java,
        "folder.restored" to FolderDelivery::class.java,
        "folder.copied" to FolderDelivery::class.java,
        "folder.moved" to FolderDelivery::class.java,
        "folder.downloaded" to FolderDelivery::class.java,
        "room.created" to RoomDelivery::class.java,
        "room.updated" to RoomDelivery::class.java,
        "room.archived" to RoomDelivery::class.java,
        "room.deleted" to RoomDelivery::class.java,
        "room.restored" to RoomDelivery::class.java,
        "room.copied" to RoomDelivery::class.java,
        "form.submit" to FormSubmitDelivery::class.java,
        "form.filled.out" to FileDelivery::class.java,
        "form.stopped" to FileDelivery::class.java,
        "agent.created" to RoomDelivery::class.java,
        "agent.updated" to RoomDelivery::class.java,
        "agent.deleted" to RoomDelivery::class.java,
    )

    /** The delivery model of the trigger, or null when it has no typed body. */
    fun deliveryType(trigger: String): Class<*>? = deliveryTypes[trigger]
}

/** Registers a handler for `user.created`; closing the result removes it. */
fun WebhookRouter.onUserCreated(handler: (delivery: UserDelivery) -> Unit): WebhookRegistration =
    on("user.created", UserDelivery::class.java, handler)

/** Registers a handler for `user.invited`; closing the result removes it. */
fun WebhookRouter.onUserInvited(handler: (delivery: UserDelivery) -> Unit): WebhookRegistration =
    on("user.invited", UserDelivery::class.java, handler)

/** Registers a handler for `user.updated`; closing the result removes it. */
fun WebhookRouter.onUserUpdated(handler: (delivery: UserDelivery) -> Unit): WebhookRegistration =
    on("user.updated", UserDelivery::class.java, handler)

/** Registers a handler for `user.deleted`; closing the result removes it. */
fun WebhookRouter.onUserDeleted(handler: (delivery: UserDelivery) -> Unit): WebhookRegistration =
    on("user.deleted", UserDelivery::class.java, handler)

/** Registers a handler for `group.created`; closing the result removes it. */
fun WebhookRouter.onGroupCreated(handler: (delivery: GroupDelivery) -> Unit): WebhookRegistration =
    on("group.created", GroupDelivery::class.java, handler)

/** Registers a handler for `group.updated`; closing the result removes it. */
fun WebhookRouter.onGroupUpdated(handler: (delivery: GroupDelivery) -> Unit): WebhookRegistration =
    on("group.updated", GroupDelivery::class.java, handler)

/** Registers a handler for `group.deleted`; closing the result removes it. */
fun WebhookRouter.onGroupDeleted(handler: (delivery: GroupDelivery) -> Unit): WebhookRegistration =
    on("group.deleted", GroupDelivery::class.java, handler)

/** Registers a handler for `file.created`; closing the result removes it. */
fun WebhookRouter.onFileCreated(handler: (delivery: FileDelivery) -> Unit): WebhookRegistration =
    on("file.created", FileDelivery::class.java, handler)

/** Registers a handler for `file.uploaded`; closing the result removes it. */
fun WebhookRouter.onFileUploaded(handler: (delivery: FileDelivery) -> Unit): WebhookRegistration =
    on("file.uploaded", FileDelivery::class.java, handler)

/** Registers a handler for `file.updated`; closing the result removes it. */
fun WebhookRouter.onFileUpdated(handler: (delivery: FileDelivery) -> Unit): WebhookRegistration =
    on("file.updated", FileDelivery::class.java, handler)

/** Registers a handler for `file.trashed`; closing the result removes it. */
fun WebhookRouter.onFileTrashed(handler: (delivery: FileDelivery) -> Unit): WebhookRegistration =
    on("file.trashed", FileDelivery::class.java, handler)

/** Registers a handler for `file.deleted`; closing the result removes it. */
fun WebhookRouter.onFileDeleted(handler: (delivery: FileDelivery) -> Unit): WebhookRegistration =
    on("file.deleted", FileDelivery::class.java, handler)

/** Registers a handler for `file.restored`; closing the result removes it. */
fun WebhookRouter.onFileRestored(handler: (delivery: FileDelivery) -> Unit): WebhookRegistration =
    on("file.restored", FileDelivery::class.java, handler)

/** Registers a handler for `file.copied`; closing the result removes it. */
fun WebhookRouter.onFileCopied(handler: (delivery: FileDelivery) -> Unit): WebhookRegistration =
    on("file.copied", FileDelivery::class.java, handler)

/** Registers a handler for `file.moved`; closing the result removes it. */
fun WebhookRouter.onFileMoved(handler: (delivery: FileDelivery) -> Unit): WebhookRegistration =
    on("file.moved", FileDelivery::class.java, handler)

/** Registers a handler for `file.downloaded`; closing the result removes it. */
fun WebhookRouter.onFileDownloaded(handler: (delivery: FileDelivery) -> Unit): WebhookRegistration =
    on("file.downloaded", FileDelivery::class.java, handler)

/** Registers a handler for `folder.created`; closing the result removes it. */
fun WebhookRouter.onFolderCreated(handler: (delivery: FolderDelivery) -> Unit): WebhookRegistration =
    on("folder.created", FolderDelivery::class.java, handler)

/** Registers a handler for `folder.updated`; closing the result removes it. */
fun WebhookRouter.onFolderUpdated(handler: (delivery: FolderDelivery) -> Unit): WebhookRegistration =
    on("folder.updated", FolderDelivery::class.java, handler)

/** Registers a handler for `folder.trashed`; closing the result removes it. */
fun WebhookRouter.onFolderTrashed(handler: (delivery: FolderDelivery) -> Unit): WebhookRegistration =
    on("folder.trashed", FolderDelivery::class.java, handler)

/** Registers a handler for `folder.deleted`; closing the result removes it. */
fun WebhookRouter.onFolderDeleted(handler: (delivery: FolderDelivery) -> Unit): WebhookRegistration =
    on("folder.deleted", FolderDelivery::class.java, handler)

/** Registers a handler for `folder.restored`; closing the result removes it. */
fun WebhookRouter.onFolderRestored(handler: (delivery: FolderDelivery) -> Unit): WebhookRegistration =
    on("folder.restored", FolderDelivery::class.java, handler)

/** Registers a handler for `folder.copied`; closing the result removes it. */
fun WebhookRouter.onFolderCopied(handler: (delivery: FolderDelivery) -> Unit): WebhookRegistration =
    on("folder.copied", FolderDelivery::class.java, handler)

/** Registers a handler for `folder.moved`; closing the result removes it. */
fun WebhookRouter.onFolderMoved(handler: (delivery: FolderDelivery) -> Unit): WebhookRegistration =
    on("folder.moved", FolderDelivery::class.java, handler)

/** Registers a handler for `folder.downloaded`; closing the result removes it. */
fun WebhookRouter.onFolderDownloaded(handler: (delivery: FolderDelivery) -> Unit): WebhookRegistration =
    on("folder.downloaded", FolderDelivery::class.java, handler)

/** Registers a handler for `room.created`; closing the result removes it. */
fun WebhookRouter.onRoomCreated(handler: (delivery: RoomDelivery) -> Unit): WebhookRegistration =
    on("room.created", RoomDelivery::class.java, handler)

/** Registers a handler for `room.updated`; closing the result removes it. */
fun WebhookRouter.onRoomUpdated(handler: (delivery: RoomDelivery) -> Unit): WebhookRegistration =
    on("room.updated", RoomDelivery::class.java, handler)

/** Registers a handler for `room.archived`; closing the result removes it. */
fun WebhookRouter.onRoomArchived(handler: (delivery: RoomDelivery) -> Unit): WebhookRegistration =
    on("room.archived", RoomDelivery::class.java, handler)

/** Registers a handler for `room.deleted`; closing the result removes it. */
fun WebhookRouter.onRoomDeleted(handler: (delivery: RoomDelivery) -> Unit): WebhookRegistration =
    on("room.deleted", RoomDelivery::class.java, handler)

/** Registers a handler for `room.restored`; closing the result removes it. */
fun WebhookRouter.onRoomRestored(handler: (delivery: RoomDelivery) -> Unit): WebhookRegistration =
    on("room.restored", RoomDelivery::class.java, handler)

/** Registers a handler for `room.copied`; closing the result removes it. */
fun WebhookRouter.onRoomCopied(handler: (delivery: RoomDelivery) -> Unit): WebhookRegistration =
    on("room.copied", RoomDelivery::class.java, handler)

/** Registers a handler for `form.submit`; closing the result removes it. */
fun WebhookRouter.onFormSubmit(handler: (delivery: FormSubmitDelivery) -> Unit): WebhookRegistration =
    on("form.submit", FormSubmitDelivery::class.java, handler)

/** Registers a handler for `form.filled.out`; closing the result removes it. */
fun WebhookRouter.onFormFilledOut(handler: (delivery: FileDelivery) -> Unit): WebhookRegistration =
    on("form.filled.out", FileDelivery::class.java, handler)

/** Registers a handler for `form.stopped`; closing the result removes it. */
fun WebhookRouter.onFormStopped(handler: (delivery: FileDelivery) -> Unit): WebhookRegistration =
    on("form.stopped", FileDelivery::class.java, handler)

/** Registers a handler for `agent.created`; closing the result removes it. */
fun WebhookRouter.onAgentCreated(handler: (delivery: RoomDelivery) -> Unit): WebhookRegistration =
    on("agent.created", RoomDelivery::class.java, handler)

/** Registers a handler for `agent.updated`; closing the result removes it. */
fun WebhookRouter.onAgentUpdated(handler: (delivery: RoomDelivery) -> Unit): WebhookRegistration =
    on("agent.updated", RoomDelivery::class.java, handler)

/** Registers a handler for `agent.deleted`; closing the result removes it. */
fun WebhookRouter.onAgentDeleted(handler: (delivery: RoomDelivery) -> Unit): WebhookRegistration =
    on("agent.deleted", RoomDelivery::class.java, handler)

