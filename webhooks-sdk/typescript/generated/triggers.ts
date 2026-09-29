/* eslint-disable */
/**
 * Trigger -> payload dispatch, generated from docspace-webhooks.yaml by
 * tools/gen-trigger-map.py. Do not edit; rerun ./generate.sh instead.
 */
import {
    FilePayload,
    FilePayloadFromJSON,
    FolderPayload,
    FolderPayloadFromJSON,
    FormSubmitPayload,
    FormSubmitPayloadFromJSON,
    GroupPayload,
    GroupPayloadFromJSON,
    RoomPayload,
    RoomPayloadFromJSON,
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
    'agent.created': RoomPayload;
    'agent.updated': RoomPayload;
    'agent.deleted': RoomPayload;
    'form.submit': FormSubmitPayload;
    'form.filled.out': FilePayload;
    'form.stopped': FilePayload;
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
    'agent.created': RoomPayloadFromJSON,
    'agent.updated': RoomPayloadFromJSON,
    'agent.deleted': RoomPayloadFromJSON,
    'form.submit': FormSubmitPayloadFromJSON,
    'form.filled.out': FilePayloadFromJSON,
    'form.stopped': FilePayloadFromJSON,
    '*': identity,
};

/** True when `trigger` is one this build of the SDK knows about. */
export function isKnownTrigger(trigger: string): trigger is WebhookTriggerName {
    return Object.prototype.hasOwnProperty.call(PAYLOAD_DESERIALIZERS, trigger);
}
