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

    # Trigger name to the model name of the whole delivery (the envelope with
    # payload typed), for every trigger with a typed body.
    DELIVERY_TYPES = {
      'user.created' => 'UserDelivery',
      'user.invited' => 'UserDelivery',
      'user.updated' => 'UserDelivery',
      'user.deleted' => 'UserDelivery',
      'group.created' => 'GroupDelivery',
      'group.updated' => 'GroupDelivery',
      'group.deleted' => 'GroupDelivery',
      'file.created' => 'FileDelivery',
      'file.uploaded' => 'FileDelivery',
      'file.updated' => 'FileDelivery',
      'file.trashed' => 'FileDelivery',
      'file.deleted' => 'FileDelivery',
      'file.restored' => 'FileDelivery',
      'file.copied' => 'FileDelivery',
      'file.moved' => 'FileDelivery',
      'file.downloaded' => 'FileDelivery',
      'folder.created' => 'FolderDelivery',
      'folder.updated' => 'FolderDelivery',
      'folder.trashed' => 'FolderDelivery',
      'folder.deleted' => 'FolderDelivery',
      'folder.restored' => 'FolderDelivery',
      'folder.copied' => 'FolderDelivery',
      'folder.moved' => 'FolderDelivery',
      'folder.downloaded' => 'FolderDelivery',
      'room.created' => 'RoomDelivery',
      'room.updated' => 'RoomDelivery',
      'room.archived' => 'RoomDelivery',
      'room.deleted' => 'RoomDelivery',
      'room.restored' => 'RoomDelivery',
      'room.copied' => 'RoomDelivery',
      'form.submit' => 'FormSubmitDelivery',
      'form.filled.out' => 'FileDelivery',
      'form.stopped' => 'FileDelivery',
      'agent.created' => 'RoomDelivery',
      'agent.updated' => 'RoomDelivery',
      'agent.deleted' => 'RoomDelivery',
    }.freeze

    # The delivery model class of the trigger, or nil when it has no typed body.
    def self.delivery_type(trigger)
      name = DELIVERY_TYPES[trigger]
      name && DocspaceWebhooksSdk.const_get(name)
    end
  end
end
