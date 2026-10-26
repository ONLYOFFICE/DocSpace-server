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
}
