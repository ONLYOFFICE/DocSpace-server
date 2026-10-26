# frozen_string_literal: true

# Trigger -> payload dispatch, generated from docspace-webhooks.yaml by
# tools/gen-trigger-map.py. Do not edit; regenerate the webhook SDKs instead.

module DocspaceWebhooksSdk
  # Which payload model each DocSpace trigger carries.
  module WebhookTriggers
    # Trigger name to payload model name, for every trigger with a typed body.
    PAYLOAD_TYPES = {
      'user.created' => 'UserPayload',
      'user.invited' => 'UserPayload',
      'user.updated' => 'UserPayload',
      'user.deleted' => 'UserPayload',
      'group.created' => 'GroupPayload',
      'group.updated' => 'GroupPayload',
      'group.deleted' => 'GroupPayload',
      'file.created' => 'FilePayload',
      'file.uploaded' => 'FilePayload',
      'file.updated' => 'FilePayload',
      'file.trashed' => 'FilePayload',
      'file.deleted' => 'FilePayload',
      'file.restored' => 'FilePayload',
      'file.copied' => 'FilePayload',
      'file.moved' => 'FilePayload',
      'file.downloaded' => 'FilePayload',
      'folder.created' => 'FolderPayload',
      'folder.updated' => 'FolderPayload',
      'folder.trashed' => 'FolderPayload',
      'folder.deleted' => 'FolderPayload',
      'folder.restored' => 'FolderPayload',
      'folder.copied' => 'FolderPayload',
      'folder.moved' => 'FolderPayload',
      'folder.downloaded' => 'FolderPayload',
      'room.created' => 'RoomPayload',
      'room.updated' => 'RoomPayload',
      'room.archived' => 'RoomPayload',
      'room.deleted' => 'RoomPayload',
      'room.restored' => 'RoomPayload',
      'room.copied' => 'RoomPayload',
      'form.submit' => 'FormSubmitPayload',
      'form.filled.out' => 'FilePayload',
      'form.stopped' => 'FilePayload',
      'agent.created' => 'RoomPayload',
      'agent.updated' => 'RoomPayload',
      'agent.deleted' => 'RoomPayload',
    }.freeze

    # The legacy catch-all: sent only when the server cannot read a stored
    # payload; no typed body.
    UNTYPED_TRIGGERS = ['*'].freeze

    # True when the trigger is in the contract this build was generated from,
    # the catch-all included.
    def self.known?(trigger)
      PAYLOAD_TYPES.key?(trigger) || UNTYPED_TRIGGERS.include?(trigger)
    end

    # The payload model class of the trigger, or nil when it has no typed body.
    def self.payload_type(trigger)
      name = PAYLOAD_TYPES[trigger]
      name && DocspaceWebhooksSdk.const_get(name)
    end
  end
end
