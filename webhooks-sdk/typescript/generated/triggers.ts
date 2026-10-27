/* eslint-disable */
/**
 * Trigger -> payload dispatch, generated from docspace-webhooks.yaml by
 * tools/gen-trigger-map.py. Do not edit; regenerate the webhook SDKs instead.
 */
import {
    FileDelivery,
    FileDeliveryFromJSON,
    FilePayload,
    FilePayloadFromJSON,
    FolderDelivery,
    FolderDeliveryFromJSON,
    FolderPayload,
    FolderPayloadFromJSON,
    FormSubmitDelivery,
    FormSubmitDeliveryFromJSON,
    FormSubmitPayload,
    FormSubmitPayloadFromJSON,
    GroupDelivery,
    GroupDeliveryFromJSON,
    GroupPayload,
    GroupPayloadFromJSON,
    RoomDelivery,
    RoomDeliveryFromJSON,
    RoomPayload,
    RoomPayloadFromJSON,
    UserDelivery,
    UserDeliveryFromJSON,
    UserPayload,
    UserPayloadFromJSON,
} from './models';

const identity = (json: any): unknown => json;

/** Every trigger DocSpace can send, mapped to the payload it carries. */
export interface TriggerPayloadMap {
    'user.created': UserPayload;
    'user.invited': UserPayload;
    'user.updated': UserPayload;
    'user.deleted': UserPayload;
    'group.created': GroupPayload;
    'group.updated': GroupPayload;
    'group.deleted': GroupPayload;
    'file.created': FilePayload;
    'file.uploaded': FilePayload;
    'file.updated': FilePayload;
    'file.trashed': FilePayload;
    'file.deleted': FilePayload;
    'file.restored': FilePayload;
    'file.copied': FilePayload;
    'file.moved': FilePayload;
    'file.downloaded': FilePayload;
    'folder.created': FolderPayload;
    'folder.updated': FolderPayload;
    'folder.trashed': FolderPayload;
    'folder.deleted': FolderPayload;
    'folder.restored': FolderPayload;
    'folder.copied': FolderPayload;
    'folder.moved': FolderPayload;
    'folder.downloaded': FolderPayload;
    'room.created': RoomPayload;
    'room.updated': RoomPayload;
    'room.archived': RoomPayload;
    'room.deleted': RoomPayload;
    'room.restored': RoomPayload;
    'room.copied': RoomPayload;
    'form.submit': FormSubmitPayload;
    'form.filled.out': FilePayload;
    'form.stopped': FilePayload;
    'agent.created': RoomPayload;
    'agent.updated': RoomPayload;
    'agent.deleted': RoomPayload;
    '*': unknown;
}

/** Trigger names as they appear in `event.trigger`. */
export type WebhookTriggerName = keyof TriggerPayloadMap;

/**
 * Runtime deserializers, keyed the same way. `'*'` is the legacy catch-all the
 * server emits only when it cannot read a stored payload; it stays opaque.
 */
export const PAYLOAD_DESERIALIZERS: {
    [K in WebhookTriggerName]: (json: any) => TriggerPayloadMap[K];
} = {
    'user.created': UserPayloadFromJSON,
    'user.invited': UserPayloadFromJSON,
    'user.updated': UserPayloadFromJSON,
    'user.deleted': UserPayloadFromJSON,
    'group.created': GroupPayloadFromJSON,
    'group.updated': GroupPayloadFromJSON,
    'group.deleted': GroupPayloadFromJSON,
    'file.created': FilePayloadFromJSON,
    'file.uploaded': FilePayloadFromJSON,
    'file.updated': FilePayloadFromJSON,
    'file.trashed': FilePayloadFromJSON,
    'file.deleted': FilePayloadFromJSON,
    'file.restored': FilePayloadFromJSON,
    'file.copied': FilePayloadFromJSON,
    'file.moved': FilePayloadFromJSON,
    'file.downloaded': FilePayloadFromJSON,
    'folder.created': FolderPayloadFromJSON,
    'folder.updated': FolderPayloadFromJSON,
    'folder.trashed': FolderPayloadFromJSON,
    'folder.deleted': FolderPayloadFromJSON,
    'folder.restored': FolderPayloadFromJSON,
    'folder.copied': FolderPayloadFromJSON,
    'folder.moved': FolderPayloadFromJSON,
    'folder.downloaded': FolderPayloadFromJSON,
    'room.created': RoomPayloadFromJSON,
    'room.updated': RoomPayloadFromJSON,
    'room.archived': RoomPayloadFromJSON,
    'room.deleted': RoomPayloadFromJSON,
    'room.restored': RoomPayloadFromJSON,
    'room.copied': RoomPayloadFromJSON,
    'form.submit': FormSubmitPayloadFromJSON,
    'form.filled.out': FilePayloadFromJSON,
    'form.stopped': FilePayloadFromJSON,
    'agent.created': RoomPayloadFromJSON,
    'agent.updated': RoomPayloadFromJSON,
    'agent.deleted': RoomPayloadFromJSON,
    '*': identity,
};

/**
 * The name of the payload model each trigger carries, as the contract names
 * the schema; `null` for the untyped `'*'`.
 */
export const PAYLOAD_MODEL_NAMES: { [K in WebhookTriggerName]: string | null } = {
    'user.created': 'UserPayload',
    'user.invited': 'UserPayload',
    'user.updated': 'UserPayload',
    'user.deleted': 'UserPayload',
    'group.created': 'GroupPayload',
    'group.updated': 'GroupPayload',
    'group.deleted': 'GroupPayload',
    'file.created': 'FilePayload',
    'file.uploaded': 'FilePayload',
    'file.updated': 'FilePayload',
    'file.trashed': 'FilePayload',
    'file.deleted': 'FilePayload',
    'file.restored': 'FilePayload',
    'file.copied': 'FilePayload',
    'file.moved': 'FilePayload',
    'file.downloaded': 'FilePayload',
    'folder.created': 'FolderPayload',
    'folder.updated': 'FolderPayload',
    'folder.trashed': 'FolderPayload',
    'folder.deleted': 'FolderPayload',
    'folder.restored': 'FolderPayload',
    'folder.copied': 'FolderPayload',
    'folder.moved': 'FolderPayload',
    'folder.downloaded': 'FolderPayload',
    'room.created': 'RoomPayload',
    'room.updated': 'RoomPayload',
    'room.archived': 'RoomPayload',
    'room.deleted': 'RoomPayload',
    'room.restored': 'RoomPayload',
    'room.copied': 'RoomPayload',
    'form.submit': 'FormSubmitPayload',
    'form.filled.out': 'FilePayload',
    'form.stopped': 'FilePayload',
    'agent.created': 'RoomPayload',
    'agent.updated': 'RoomPayload',
    'agent.deleted': 'RoomPayload',
    '*': null,
};

/**
 * True when `trigger` is in the contract this build of the SDK was generated
 * from, the untyped `'*'` included; false for a trigger added on the server
 * later. The C# SDK's `WebhookTriggers.IsKnown` answers the same way.
 */
export function isKnownTrigger(trigger: string): trigger is WebhookTriggerName {
    return Object.prototype.hasOwnProperty.call(PAYLOAD_DESERIALIZERS, trigger);
}

/**
 * Every trigger with a typed body, mapped to the model of the whole delivery:
 * the envelope with `payload` typed, as the contract's `webhooks` section gives
 * it. The untyped `'*'` has none.
 */
export interface TriggerDeliveryMap {
    'user.created': UserDelivery;
    'user.invited': UserDelivery;
    'user.updated': UserDelivery;
    'user.deleted': UserDelivery;
    'group.created': GroupDelivery;
    'group.updated': GroupDelivery;
    'group.deleted': GroupDelivery;
    'file.created': FileDelivery;
    'file.uploaded': FileDelivery;
    'file.updated': FileDelivery;
    'file.trashed': FileDelivery;
    'file.deleted': FileDelivery;
    'file.restored': FileDelivery;
    'file.copied': FileDelivery;
    'file.moved': FileDelivery;
    'file.downloaded': FileDelivery;
    'folder.created': FolderDelivery;
    'folder.updated': FolderDelivery;
    'folder.trashed': FolderDelivery;
    'folder.deleted': FolderDelivery;
    'folder.restored': FolderDelivery;
    'folder.copied': FolderDelivery;
    'folder.moved': FolderDelivery;
    'folder.downloaded': FolderDelivery;
    'room.created': RoomDelivery;
    'room.updated': RoomDelivery;
    'room.archived': RoomDelivery;
    'room.deleted': RoomDelivery;
    'room.restored': RoomDelivery;
    'room.copied': RoomDelivery;
    'form.submit': FormSubmitDelivery;
    'form.filled.out': FileDelivery;
    'form.stopped': FileDelivery;
    'agent.created': RoomDelivery;
    'agent.updated': RoomDelivery;
    'agent.deleted': RoomDelivery;
}

/** Triggers whose body has a delivery model of its own. */
export type TypedTriggerName = keyof TriggerDeliveryMap;

/** Runtime deserializers of the whole body, keyed like `TriggerDeliveryMap`. */
export const DELIVERY_DESERIALIZERS: {
    [K in TypedTriggerName]: (json: any) => TriggerDeliveryMap[K];
} = {
    'user.created': UserDeliveryFromJSON,
    'user.invited': UserDeliveryFromJSON,
    'user.updated': UserDeliveryFromJSON,
    'user.deleted': UserDeliveryFromJSON,
    'group.created': GroupDeliveryFromJSON,
    'group.updated': GroupDeliveryFromJSON,
    'group.deleted': GroupDeliveryFromJSON,
    'file.created': FileDeliveryFromJSON,
    'file.uploaded': FileDeliveryFromJSON,
    'file.updated': FileDeliveryFromJSON,
    'file.trashed': FileDeliveryFromJSON,
    'file.deleted': FileDeliveryFromJSON,
    'file.restored': FileDeliveryFromJSON,
    'file.copied': FileDeliveryFromJSON,
    'file.moved': FileDeliveryFromJSON,
    'file.downloaded': FileDeliveryFromJSON,
    'folder.created': FolderDeliveryFromJSON,
    'folder.updated': FolderDeliveryFromJSON,
    'folder.trashed': FolderDeliveryFromJSON,
    'folder.deleted': FolderDeliveryFromJSON,
    'folder.restored': FolderDeliveryFromJSON,
    'folder.copied': FolderDeliveryFromJSON,
    'folder.moved': FolderDeliveryFromJSON,
    'folder.downloaded': FolderDeliveryFromJSON,
    'room.created': RoomDeliveryFromJSON,
    'room.updated': RoomDeliveryFromJSON,
    'room.archived': RoomDeliveryFromJSON,
    'room.deleted': RoomDeliveryFromJSON,
    'room.restored': RoomDeliveryFromJSON,
    'room.copied': RoomDeliveryFromJSON,
    'form.submit': FormSubmitDeliveryFromJSON,
    'form.filled.out': FileDeliveryFromJSON,
    'form.stopped': FileDeliveryFromJSON,
    'agent.created': RoomDeliveryFromJSON,
    'agent.updated': RoomDeliveryFromJSON,
    'agent.deleted': RoomDeliveryFromJSON,
};

/**
 * The name of the delivery model of each trigger, as the contract names the
 * schema; `null` for the untyped `'*'`.
 */
export const DELIVERY_MODEL_NAMES: { [K in WebhookTriggerName]: string | null } = {
    'user.created': 'UserDelivery',
    'user.invited': 'UserDelivery',
    'user.updated': 'UserDelivery',
    'user.deleted': 'UserDelivery',
    'group.created': 'GroupDelivery',
    'group.updated': 'GroupDelivery',
    'group.deleted': 'GroupDelivery',
    'file.created': 'FileDelivery',
    'file.uploaded': 'FileDelivery',
    'file.updated': 'FileDelivery',
    'file.trashed': 'FileDelivery',
    'file.deleted': 'FileDelivery',
    'file.restored': 'FileDelivery',
    'file.copied': 'FileDelivery',
    'file.moved': 'FileDelivery',
    'file.downloaded': 'FileDelivery',
    'folder.created': 'FolderDelivery',
    'folder.updated': 'FolderDelivery',
    'folder.trashed': 'FolderDelivery',
    'folder.deleted': 'FolderDelivery',
    'folder.restored': 'FolderDelivery',
    'folder.copied': 'FolderDelivery',
    'folder.moved': 'FolderDelivery',
    'folder.downloaded': 'FolderDelivery',
    'room.created': 'RoomDelivery',
    'room.updated': 'RoomDelivery',
    'room.archived': 'RoomDelivery',
    'room.deleted': 'RoomDelivery',
    'room.restored': 'RoomDelivery',
    'room.copied': 'RoomDelivery',
    'form.submit': 'FormSubmitDelivery',
    'form.filled.out': 'FileDelivery',
    'form.stopped': 'FileDelivery',
    'agent.created': 'RoomDelivery',
    'agent.updated': 'RoomDelivery',
    'agent.deleted': 'RoomDelivery',
    '*': null,
};

/** True when `trigger` has a delivery model in this build. */
export function hasDeliveryModel(trigger: string): trigger is TypedTriggerName {
    return Object.prototype.hasOwnProperty.call(DELIVERY_DESERIALIZERS, trigger);
}
