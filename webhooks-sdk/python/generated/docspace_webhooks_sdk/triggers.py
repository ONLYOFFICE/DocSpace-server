# Trigger -> payload dispatch, generated from docspace-webhooks.yaml by
# tools/gen-trigger-map.py. Do not edit; regenerate the webhook SDKs instead.
"""Which payload model each DocSpace trigger carries."""

from docspace_webhooks_sdk.models.file_delivery import FileDelivery
from docspace_webhooks_sdk.models.file_payload import FilePayload
from docspace_webhooks_sdk.models.folder_delivery import FolderDelivery
from docspace_webhooks_sdk.models.folder_payload import FolderPayload
from docspace_webhooks_sdk.models.form_submit_delivery import FormSubmitDelivery
from docspace_webhooks_sdk.models.form_submit_payload import FormSubmitPayload
from docspace_webhooks_sdk.models.group_delivery import GroupDelivery
from docspace_webhooks_sdk.models.group_payload import GroupPayload
from docspace_webhooks_sdk.models.room_delivery import RoomDelivery
from docspace_webhooks_sdk.models.room_payload import RoomPayload
from docspace_webhooks_sdk.models.user_delivery import UserDelivery
from docspace_webhooks_sdk.models.user_payload import UserPayload

#: Trigger name to payload model, for every trigger with a typed body.
PAYLOAD_TYPES = {
    "user.created": UserPayload,
    "user.invited": UserPayload,
    "user.updated": UserPayload,
    "user.deleted": UserPayload,
    "group.created": GroupPayload,
    "group.updated": GroupPayload,
    "group.deleted": GroupPayload,
    "file.created": FilePayload,
    "file.uploaded": FilePayload,
    "file.updated": FilePayload,
    "file.trashed": FilePayload,
    "file.deleted": FilePayload,
    "file.restored": FilePayload,
    "file.copied": FilePayload,
    "file.moved": FilePayload,
    "file.downloaded": FilePayload,
    "folder.created": FolderPayload,
    "folder.updated": FolderPayload,
    "folder.trashed": FolderPayload,
    "folder.deleted": FolderPayload,
    "folder.restored": FolderPayload,
    "folder.copied": FolderPayload,
    "folder.moved": FolderPayload,
    "folder.downloaded": FolderPayload,
    "room.created": RoomPayload,
    "room.updated": RoomPayload,
    "room.archived": RoomPayload,
    "room.deleted": RoomPayload,
    "room.restored": RoomPayload,
    "room.copied": RoomPayload,
    "form.submit": FormSubmitPayload,
    "form.filled.out": FilePayload,
    "form.stopped": FilePayload,
    "agent.created": RoomPayload,
    "agent.updated": RoomPayload,
    "agent.deleted": RoomPayload,
}

#: The legacy catch-all: sent only when the server cannot read a stored
#: payload; no typed body.
UNTYPED_TRIGGERS = frozenset({"*"})


def is_known_trigger(trigger):
    """True when the trigger is in the contract this build was generated from, the catch-all included."""
    return trigger in PAYLOAD_TYPES or trigger in UNTYPED_TRIGGERS


def payload_type(trigger):
    """The payload model of the trigger, or None when it has no typed body."""
    return PAYLOAD_TYPES.get(trigger)


#: Trigger name to the model of the whole delivery (the envelope with payload
#: typed), for every trigger with a typed body.
DELIVERY_TYPES = {
    "user.created": UserDelivery,
    "user.invited": UserDelivery,
    "user.updated": UserDelivery,
    "user.deleted": UserDelivery,
    "group.created": GroupDelivery,
    "group.updated": GroupDelivery,
    "group.deleted": GroupDelivery,
    "file.created": FileDelivery,
    "file.uploaded": FileDelivery,
    "file.updated": FileDelivery,
    "file.trashed": FileDelivery,
    "file.deleted": FileDelivery,
    "file.restored": FileDelivery,
    "file.copied": FileDelivery,
    "file.moved": FileDelivery,
    "file.downloaded": FileDelivery,
    "folder.created": FolderDelivery,
    "folder.updated": FolderDelivery,
    "folder.trashed": FolderDelivery,
    "folder.deleted": FolderDelivery,
    "folder.restored": FolderDelivery,
    "folder.copied": FolderDelivery,
    "folder.moved": FolderDelivery,
    "folder.downloaded": FolderDelivery,
    "room.created": RoomDelivery,
    "room.updated": RoomDelivery,
    "room.archived": RoomDelivery,
    "room.deleted": RoomDelivery,
    "room.restored": RoomDelivery,
    "room.copied": RoomDelivery,
    "form.submit": FormSubmitDelivery,
    "form.filled.out": FileDelivery,
    "form.stopped": FileDelivery,
    "agent.created": RoomDelivery,
    "agent.updated": RoomDelivery,
    "agent.deleted": RoomDelivery,
}


def delivery_type(trigger):
    """The delivery model of the trigger, or None when it has no typed body."""
    return DELIVERY_TYPES.get(trigger)
