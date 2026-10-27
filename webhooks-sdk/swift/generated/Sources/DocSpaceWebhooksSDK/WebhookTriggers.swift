// Trigger -> payload dispatch, generated from docspace-webhooks.yaml by
// tools/gen-trigger-map.py. Do not edit; regenerate the webhook SDKs instead.

import Foundation

/// Which payload model each DocSpace trigger carries.
public enum WebhookTriggers {
    /// Trigger name to payload model name, for every trigger with a typed body.
    public static let payloadModels: [String: String] = [
        "user.created": "UserPayload",
        "user.invited": "UserPayload",
        "user.updated": "UserPayload",
        "user.deleted": "UserPayload",
        "group.created": "GroupPayload",
        "group.updated": "GroupPayload",
        "group.deleted": "GroupPayload",
        "file.created": "FilePayload",
        "file.uploaded": "FilePayload",
        "file.updated": "FilePayload",
        "file.trashed": "FilePayload",
        "file.deleted": "FilePayload",
        "file.restored": "FilePayload",
        "file.copied": "FilePayload",
        "file.moved": "FilePayload",
        "file.downloaded": "FilePayload",
        "folder.created": "FolderPayload",
        "folder.updated": "FolderPayload",
        "folder.trashed": "FolderPayload",
        "folder.deleted": "FolderPayload",
        "folder.restored": "FolderPayload",
        "folder.copied": "FolderPayload",
        "folder.moved": "FolderPayload",
        "folder.downloaded": "FolderPayload",
        "room.created": "RoomPayload",
        "room.updated": "RoomPayload",
        "room.archived": "RoomPayload",
        "room.deleted": "RoomPayload",
        "room.restored": "RoomPayload",
        "room.copied": "RoomPayload",
        "form.submit": "FormSubmitPayload",
        "form.filled.out": "FilePayload",
        "form.stopped": "FilePayload",
        "agent.created": "RoomPayload",
        "agent.updated": "RoomPayload",
        "agent.deleted": "RoomPayload",
    ]

    /// The legacy catch-all: sent only when the server cannot read a stored payload; no typed body.
    public static let untypedTriggers: Set<String> = ["*"]

    /// True when the trigger is in the contract this build was generated from, the catch-all included.
    public static func isKnown(_ trigger: String) -> Bool {
        payloadModels[trigger] != nil || untypedTriggers.contains(trigger)
    }

    /// Decodes the payload with the model of the trigger; nil when the trigger has no typed body.
    public static func decodePayload(trigger: String, from data: Data, decoder: JSONDecoder) throws -> Any? {
        switch payloadModels[trigger] {
        case "FilePayload": return try decoder.decode(FilePayload.self, from: data)
        case "FolderPayload": return try decoder.decode(FolderPayload.self, from: data)
        case "FormSubmitPayload": return try decoder.decode(FormSubmitPayload.self, from: data)
        case "GroupPayload": return try decoder.decode(GroupPayload.self, from: data)
        case "RoomPayload": return try decoder.decode(RoomPayload.self, from: data)
        case "UserPayload": return try decoder.decode(UserPayload.self, from: data)
        default: return nil
        }
    }

    /// Trigger name to the model name of the whole delivery (the envelope with payload typed), for every typed trigger.
    public static let deliveryModels: [String: String] = [
        "user.created": "UserDelivery",
        "user.invited": "UserDelivery",
        "user.updated": "UserDelivery",
        "user.deleted": "UserDelivery",
        "group.created": "GroupDelivery",
        "group.updated": "GroupDelivery",
        "group.deleted": "GroupDelivery",
        "file.created": "FileDelivery",
        "file.uploaded": "FileDelivery",
        "file.updated": "FileDelivery",
        "file.trashed": "FileDelivery",
        "file.deleted": "FileDelivery",
        "file.restored": "FileDelivery",
        "file.copied": "FileDelivery",
        "file.moved": "FileDelivery",
        "file.downloaded": "FileDelivery",
        "folder.created": "FolderDelivery",
        "folder.updated": "FolderDelivery",
        "folder.trashed": "FolderDelivery",
        "folder.deleted": "FolderDelivery",
        "folder.restored": "FolderDelivery",
        "folder.copied": "FolderDelivery",
        "folder.moved": "FolderDelivery",
        "folder.downloaded": "FolderDelivery",
        "room.created": "RoomDelivery",
        "room.updated": "RoomDelivery",
        "room.archived": "RoomDelivery",
        "room.deleted": "RoomDelivery",
        "room.restored": "RoomDelivery",
        "room.copied": "RoomDelivery",
        "form.submit": "FormSubmitDelivery",
        "form.filled.out": "FileDelivery",
        "form.stopped": "FileDelivery",
        "agent.created": "RoomDelivery",
        "agent.updated": "RoomDelivery",
        "agent.deleted": "RoomDelivery",
    ]

    /// Decodes the whole body with the delivery model of the trigger; nil when the trigger has no typed body.
    public static func decodeDelivery(trigger: String, from data: Data, decoder: JSONDecoder) throws -> Any? {
        switch deliveryModels[trigger] {
        case "FileDelivery": return try decoder.decode(FileDelivery.self, from: data)
        case "FolderDelivery": return try decoder.decode(FolderDelivery.self, from: data)
        case "FormSubmitDelivery": return try decoder.decode(FormSubmitDelivery.self, from: data)
        case "GroupDelivery": return try decoder.decode(GroupDelivery.self, from: data)
        case "RoomDelivery": return try decoder.decode(RoomDelivery.self, from: data)
        case "UserDelivery": return try decoder.decode(UserDelivery.self, from: data)
        default: return nil
        }
    }
}

public extension WebhookRouter {
    /// Registers a handler for `user.created`; cancelling the result removes it.
    @discardableResult
    func onUserCreated(_ handler: @escaping @Sendable (UserDelivery) async throws -> Void) -> WebhookRegistration {
        on("user.created", UserDelivery.self, handler)
    }

    /// Registers a handler for `user.invited`; cancelling the result removes it.
    @discardableResult
    func onUserInvited(_ handler: @escaping @Sendable (UserDelivery) async throws -> Void) -> WebhookRegistration {
        on("user.invited", UserDelivery.self, handler)
    }

    /// Registers a handler for `user.updated`; cancelling the result removes it.
    @discardableResult
    func onUserUpdated(_ handler: @escaping @Sendable (UserDelivery) async throws -> Void) -> WebhookRegistration {
        on("user.updated", UserDelivery.self, handler)
    }

    /// Registers a handler for `user.deleted`; cancelling the result removes it.
    @discardableResult
    func onUserDeleted(_ handler: @escaping @Sendable (UserDelivery) async throws -> Void) -> WebhookRegistration {
        on("user.deleted", UserDelivery.self, handler)
    }

    /// Registers a handler for `group.created`; cancelling the result removes it.
    @discardableResult
    func onGroupCreated(_ handler: @escaping @Sendable (GroupDelivery) async throws -> Void) -> WebhookRegistration {
        on("group.created", GroupDelivery.self, handler)
    }

    /// Registers a handler for `group.updated`; cancelling the result removes it.
    @discardableResult
    func onGroupUpdated(_ handler: @escaping @Sendable (GroupDelivery) async throws -> Void) -> WebhookRegistration {
        on("group.updated", GroupDelivery.self, handler)
    }

    /// Registers a handler for `group.deleted`; cancelling the result removes it.
    @discardableResult
    func onGroupDeleted(_ handler: @escaping @Sendable (GroupDelivery) async throws -> Void) -> WebhookRegistration {
        on("group.deleted", GroupDelivery.self, handler)
    }

    /// Registers a handler for `file.created`; cancelling the result removes it.
    @discardableResult
    func onFileCreated(_ handler: @escaping @Sendable (FileDelivery) async throws -> Void) -> WebhookRegistration {
        on("file.created", FileDelivery.self, handler)
    }

    /// Registers a handler for `file.uploaded`; cancelling the result removes it.
    @discardableResult
    func onFileUploaded(_ handler: @escaping @Sendable (FileDelivery) async throws -> Void) -> WebhookRegistration {
        on("file.uploaded", FileDelivery.self, handler)
    }

    /// Registers a handler for `file.updated`; cancelling the result removes it.
    @discardableResult
    func onFileUpdated(_ handler: @escaping @Sendable (FileDelivery) async throws -> Void) -> WebhookRegistration {
        on("file.updated", FileDelivery.self, handler)
    }

    /// Registers a handler for `file.trashed`; cancelling the result removes it.
    @discardableResult
    func onFileTrashed(_ handler: @escaping @Sendable (FileDelivery) async throws -> Void) -> WebhookRegistration {
        on("file.trashed", FileDelivery.self, handler)
    }

    /// Registers a handler for `file.deleted`; cancelling the result removes it.
    @discardableResult
    func onFileDeleted(_ handler: @escaping @Sendable (FileDelivery) async throws -> Void) -> WebhookRegistration {
        on("file.deleted", FileDelivery.self, handler)
    }

    /// Registers a handler for `file.restored`; cancelling the result removes it.
    @discardableResult
    func onFileRestored(_ handler: @escaping @Sendable (FileDelivery) async throws -> Void) -> WebhookRegistration {
        on("file.restored", FileDelivery.self, handler)
    }

    /// Registers a handler for `file.copied`; cancelling the result removes it.
    @discardableResult
    func onFileCopied(_ handler: @escaping @Sendable (FileDelivery) async throws -> Void) -> WebhookRegistration {
        on("file.copied", FileDelivery.self, handler)
    }

    /// Registers a handler for `file.moved`; cancelling the result removes it.
    @discardableResult
    func onFileMoved(_ handler: @escaping @Sendable (FileDelivery) async throws -> Void) -> WebhookRegistration {
        on("file.moved", FileDelivery.self, handler)
    }

    /// Registers a handler for `file.downloaded`; cancelling the result removes it.
    @discardableResult
    func onFileDownloaded(_ handler: @escaping @Sendable (FileDelivery) async throws -> Void) -> WebhookRegistration {
        on("file.downloaded", FileDelivery.self, handler)
    }

    /// Registers a handler for `folder.created`; cancelling the result removes it.
    @discardableResult
    func onFolderCreated(_ handler: @escaping @Sendable (FolderDelivery) async throws -> Void) -> WebhookRegistration {
        on("folder.created", FolderDelivery.self, handler)
    }

    /// Registers a handler for `folder.updated`; cancelling the result removes it.
    @discardableResult
    func onFolderUpdated(_ handler: @escaping @Sendable (FolderDelivery) async throws -> Void) -> WebhookRegistration {
        on("folder.updated", FolderDelivery.self, handler)
    }

    /// Registers a handler for `folder.trashed`; cancelling the result removes it.
    @discardableResult
    func onFolderTrashed(_ handler: @escaping @Sendable (FolderDelivery) async throws -> Void) -> WebhookRegistration {
        on("folder.trashed", FolderDelivery.self, handler)
    }

    /// Registers a handler for `folder.deleted`; cancelling the result removes it.
    @discardableResult
    func onFolderDeleted(_ handler: @escaping @Sendable (FolderDelivery) async throws -> Void) -> WebhookRegistration {
        on("folder.deleted", FolderDelivery.self, handler)
    }

    /// Registers a handler for `folder.restored`; cancelling the result removes it.
    @discardableResult
    func onFolderRestored(_ handler: @escaping @Sendable (FolderDelivery) async throws -> Void) -> WebhookRegistration {
        on("folder.restored", FolderDelivery.self, handler)
    }

    /// Registers a handler for `folder.copied`; cancelling the result removes it.
    @discardableResult
    func onFolderCopied(_ handler: @escaping @Sendable (FolderDelivery) async throws -> Void) -> WebhookRegistration {
        on("folder.copied", FolderDelivery.self, handler)
    }

    /// Registers a handler for `folder.moved`; cancelling the result removes it.
    @discardableResult
    func onFolderMoved(_ handler: @escaping @Sendable (FolderDelivery) async throws -> Void) -> WebhookRegistration {
        on("folder.moved", FolderDelivery.self, handler)
    }

    /// Registers a handler for `folder.downloaded`; cancelling the result removes it.
    @discardableResult
    func onFolderDownloaded(_ handler: @escaping @Sendable (FolderDelivery) async throws -> Void) -> WebhookRegistration {
        on("folder.downloaded", FolderDelivery.self, handler)
    }

    /// Registers a handler for `room.created`; cancelling the result removes it.
    @discardableResult
    func onRoomCreated(_ handler: @escaping @Sendable (RoomDelivery) async throws -> Void) -> WebhookRegistration {
        on("room.created", RoomDelivery.self, handler)
    }

    /// Registers a handler for `room.updated`; cancelling the result removes it.
    @discardableResult
    func onRoomUpdated(_ handler: @escaping @Sendable (RoomDelivery) async throws -> Void) -> WebhookRegistration {
        on("room.updated", RoomDelivery.self, handler)
    }

    /// Registers a handler for `room.archived`; cancelling the result removes it.
    @discardableResult
    func onRoomArchived(_ handler: @escaping @Sendable (RoomDelivery) async throws -> Void) -> WebhookRegistration {
        on("room.archived", RoomDelivery.self, handler)
    }

    /// Registers a handler for `room.deleted`; cancelling the result removes it.
    @discardableResult
    func onRoomDeleted(_ handler: @escaping @Sendable (RoomDelivery) async throws -> Void) -> WebhookRegistration {
        on("room.deleted", RoomDelivery.self, handler)
    }

    /// Registers a handler for `room.restored`; cancelling the result removes it.
    @discardableResult
    func onRoomRestored(_ handler: @escaping @Sendable (RoomDelivery) async throws -> Void) -> WebhookRegistration {
        on("room.restored", RoomDelivery.self, handler)
    }

    /// Registers a handler for `room.copied`; cancelling the result removes it.
    @discardableResult
    func onRoomCopied(_ handler: @escaping @Sendable (RoomDelivery) async throws -> Void) -> WebhookRegistration {
        on("room.copied", RoomDelivery.self, handler)
    }

    /// Registers a handler for `form.submit`; cancelling the result removes it.
    @discardableResult
    func onFormSubmit(_ handler: @escaping @Sendable (FormSubmitDelivery) async throws -> Void) -> WebhookRegistration {
        on("form.submit", FormSubmitDelivery.self, handler)
    }

    /// Registers a handler for `form.filled.out`; cancelling the result removes it.
    @discardableResult
    func onFormFilledOut(_ handler: @escaping @Sendable (FileDelivery) async throws -> Void) -> WebhookRegistration {
        on("form.filled.out", FileDelivery.self, handler)
    }

    /// Registers a handler for `form.stopped`; cancelling the result removes it.
    @discardableResult
    func onFormStopped(_ handler: @escaping @Sendable (FileDelivery) async throws -> Void) -> WebhookRegistration {
        on("form.stopped", FileDelivery.self, handler)
    }

    /// Registers a handler for `agent.created`; cancelling the result removes it.
    @discardableResult
    func onAgentCreated(_ handler: @escaping @Sendable (RoomDelivery) async throws -> Void) -> WebhookRegistration {
        on("agent.created", RoomDelivery.self, handler)
    }

    /// Registers a handler for `agent.updated`; cancelling the result removes it.
    @discardableResult
    func onAgentUpdated(_ handler: @escaping @Sendable (RoomDelivery) async throws -> Void) -> WebhookRegistration {
        on("agent.updated", RoomDelivery.self, handler)
    }

    /// Registers a handler for `agent.deleted`; cancelling the result removes it.
    @discardableResult
    func onAgentDeleted(_ handler: @escaping @Sendable (RoomDelivery) async throws -> Void) -> WebhookRegistration {
        on("agent.deleted", RoomDelivery.self, handler)
    }
}
