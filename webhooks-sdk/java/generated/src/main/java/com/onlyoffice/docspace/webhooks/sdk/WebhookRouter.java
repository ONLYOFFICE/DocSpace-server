package com.onlyoffice.docspace.webhooks.sdk;

// Trigger -> payload dispatch, generated from docspace-webhooks.yaml by
// tools/gen-trigger-map.py. Do not edit; regenerate the webhook SDKs instead.

import com.onlyoffice.docspace.webhooks.sdk.model.FileDelivery;
import com.onlyoffice.docspace.webhooks.sdk.model.FolderDelivery;
import com.onlyoffice.docspace.webhooks.sdk.model.FormSubmitDelivery;
import com.onlyoffice.docspace.webhooks.sdk.model.GroupDelivery;
import com.onlyoffice.docspace.webhooks.sdk.model.RoomDelivery;
import com.onlyoffice.docspace.webhooks.sdk.model.UserDelivery;

/** Dispatches verified deliveries to handlers registered per trigger; see {@link AbstractWebhookRouter}. */
public final class WebhookRouter extends AbstractWebhookRouter {

    /** A router that accepts only deliveries signed with {@code secret}; {@code store} may be null. */
    public WebhookRouter(String secret, WebhookEventStore store) {
        super(requireSecret(secret), store);
    }

    private WebhookRouter(WebhookEventStore store) {
        super(null, store);
    }

    /**
     * A router that does not check signatures, for replaying saved bodies during
     * development. Never put it behind a reachable endpoint.
     */
    public static WebhookRouter forUnsignedReplay(WebhookEventStore store) {
        return new WebhookRouter(store);
    }

    /** Registers a handler for {@code user.created}; closing the result removes it. */
    public WebhookRegistration onUserCreated(DeliveryHandler<UserDelivery> handler) {
        return on("user.created", UserDelivery.class, handler);
    }

    /** Registers a handler for {@code user.invited}; closing the result removes it. */
    public WebhookRegistration onUserInvited(DeliveryHandler<UserDelivery> handler) {
        return on("user.invited", UserDelivery.class, handler);
    }

    /** Registers a handler for {@code user.updated}; closing the result removes it. */
    public WebhookRegistration onUserUpdated(DeliveryHandler<UserDelivery> handler) {
        return on("user.updated", UserDelivery.class, handler);
    }

    /** Registers a handler for {@code user.deleted}; closing the result removes it. */
    public WebhookRegistration onUserDeleted(DeliveryHandler<UserDelivery> handler) {
        return on("user.deleted", UserDelivery.class, handler);
    }

    /** Registers a handler for {@code group.created}; closing the result removes it. */
    public WebhookRegistration onGroupCreated(DeliveryHandler<GroupDelivery> handler) {
        return on("group.created", GroupDelivery.class, handler);
    }

    /** Registers a handler for {@code group.updated}; closing the result removes it. */
    public WebhookRegistration onGroupUpdated(DeliveryHandler<GroupDelivery> handler) {
        return on("group.updated", GroupDelivery.class, handler);
    }

    /** Registers a handler for {@code group.deleted}; closing the result removes it. */
    public WebhookRegistration onGroupDeleted(DeliveryHandler<GroupDelivery> handler) {
        return on("group.deleted", GroupDelivery.class, handler);
    }

    /** Registers a handler for {@code file.created}; closing the result removes it. */
    public WebhookRegistration onFileCreated(DeliveryHandler<FileDelivery> handler) {
        return on("file.created", FileDelivery.class, handler);
    }

    /** Registers a handler for {@code file.uploaded}; closing the result removes it. */
    public WebhookRegistration onFileUploaded(DeliveryHandler<FileDelivery> handler) {
        return on("file.uploaded", FileDelivery.class, handler);
    }

    /** Registers a handler for {@code file.updated}; closing the result removes it. */
    public WebhookRegistration onFileUpdated(DeliveryHandler<FileDelivery> handler) {
        return on("file.updated", FileDelivery.class, handler);
    }

    /** Registers a handler for {@code file.trashed}; closing the result removes it. */
    public WebhookRegistration onFileTrashed(DeliveryHandler<FileDelivery> handler) {
        return on("file.trashed", FileDelivery.class, handler);
    }

    /** Registers a handler for {@code file.deleted}; closing the result removes it. */
    public WebhookRegistration onFileDeleted(DeliveryHandler<FileDelivery> handler) {
        return on("file.deleted", FileDelivery.class, handler);
    }

    /** Registers a handler for {@code file.restored}; closing the result removes it. */
    public WebhookRegistration onFileRestored(DeliveryHandler<FileDelivery> handler) {
        return on("file.restored", FileDelivery.class, handler);
    }

    /** Registers a handler for {@code file.copied}; closing the result removes it. */
    public WebhookRegistration onFileCopied(DeliveryHandler<FileDelivery> handler) {
        return on("file.copied", FileDelivery.class, handler);
    }

    /** Registers a handler for {@code file.moved}; closing the result removes it. */
    public WebhookRegistration onFileMoved(DeliveryHandler<FileDelivery> handler) {
        return on("file.moved", FileDelivery.class, handler);
    }

    /** Registers a handler for {@code file.downloaded}; closing the result removes it. */
    public WebhookRegistration onFileDownloaded(DeliveryHandler<FileDelivery> handler) {
        return on("file.downloaded", FileDelivery.class, handler);
    }

    /** Registers a handler for {@code folder.created}; closing the result removes it. */
    public WebhookRegistration onFolderCreated(DeliveryHandler<FolderDelivery> handler) {
        return on("folder.created", FolderDelivery.class, handler);
    }

    /** Registers a handler for {@code folder.updated}; closing the result removes it. */
    public WebhookRegistration onFolderUpdated(DeliveryHandler<FolderDelivery> handler) {
        return on("folder.updated", FolderDelivery.class, handler);
    }

    /** Registers a handler for {@code folder.trashed}; closing the result removes it. */
    public WebhookRegistration onFolderTrashed(DeliveryHandler<FolderDelivery> handler) {
        return on("folder.trashed", FolderDelivery.class, handler);
    }

    /** Registers a handler for {@code folder.deleted}; closing the result removes it. */
    public WebhookRegistration onFolderDeleted(DeliveryHandler<FolderDelivery> handler) {
        return on("folder.deleted", FolderDelivery.class, handler);
    }

    /** Registers a handler for {@code folder.restored}; closing the result removes it. */
    public WebhookRegistration onFolderRestored(DeliveryHandler<FolderDelivery> handler) {
        return on("folder.restored", FolderDelivery.class, handler);
    }

    /** Registers a handler for {@code folder.copied}; closing the result removes it. */
    public WebhookRegistration onFolderCopied(DeliveryHandler<FolderDelivery> handler) {
        return on("folder.copied", FolderDelivery.class, handler);
    }

    /** Registers a handler for {@code folder.moved}; closing the result removes it. */
    public WebhookRegistration onFolderMoved(DeliveryHandler<FolderDelivery> handler) {
        return on("folder.moved", FolderDelivery.class, handler);
    }

    /** Registers a handler for {@code folder.downloaded}; closing the result removes it. */
    public WebhookRegistration onFolderDownloaded(DeliveryHandler<FolderDelivery> handler) {
        return on("folder.downloaded", FolderDelivery.class, handler);
    }

    /** Registers a handler for {@code room.created}; closing the result removes it. */
    public WebhookRegistration onRoomCreated(DeliveryHandler<RoomDelivery> handler) {
        return on("room.created", RoomDelivery.class, handler);
    }

    /** Registers a handler for {@code room.updated}; closing the result removes it. */
    public WebhookRegistration onRoomUpdated(DeliveryHandler<RoomDelivery> handler) {
        return on("room.updated", RoomDelivery.class, handler);
    }

    /** Registers a handler for {@code room.archived}; closing the result removes it. */
    public WebhookRegistration onRoomArchived(DeliveryHandler<RoomDelivery> handler) {
        return on("room.archived", RoomDelivery.class, handler);
    }

    /** Registers a handler for {@code room.deleted}; closing the result removes it. */
    public WebhookRegistration onRoomDeleted(DeliveryHandler<RoomDelivery> handler) {
        return on("room.deleted", RoomDelivery.class, handler);
    }

    /** Registers a handler for {@code room.restored}; closing the result removes it. */
    public WebhookRegistration onRoomRestored(DeliveryHandler<RoomDelivery> handler) {
        return on("room.restored", RoomDelivery.class, handler);
    }

    /** Registers a handler for {@code room.copied}; closing the result removes it. */
    public WebhookRegistration onRoomCopied(DeliveryHandler<RoomDelivery> handler) {
        return on("room.copied", RoomDelivery.class, handler);
    }

    /** Registers a handler for {@code form.submit}; closing the result removes it. */
    public WebhookRegistration onFormSubmit(DeliveryHandler<FormSubmitDelivery> handler) {
        return on("form.submit", FormSubmitDelivery.class, handler);
    }

    /** Registers a handler for {@code form.filled.out}; closing the result removes it. */
    public WebhookRegistration onFormFilledOut(DeliveryHandler<FileDelivery> handler) {
        return on("form.filled.out", FileDelivery.class, handler);
    }

    /** Registers a handler for {@code form.stopped}; closing the result removes it. */
    public WebhookRegistration onFormStopped(DeliveryHandler<FileDelivery> handler) {
        return on("form.stopped", FileDelivery.class, handler);
    }

    /** Registers a handler for {@code agent.created}; closing the result removes it. */
    public WebhookRegistration onAgentCreated(DeliveryHandler<RoomDelivery> handler) {
        return on("agent.created", RoomDelivery.class, handler);
    }

    /** Registers a handler for {@code agent.updated}; closing the result removes it. */
    public WebhookRegistration onAgentUpdated(DeliveryHandler<RoomDelivery> handler) {
        return on("agent.updated", RoomDelivery.class, handler);
    }

    /** Registers a handler for {@code agent.deleted}; closing the result removes it. */
    public WebhookRegistration onAgentDeleted(DeliveryHandler<RoomDelivery> handler) {
        return on("agent.deleted", RoomDelivery.class, handler);
    }
}
