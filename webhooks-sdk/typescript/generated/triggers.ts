/* eslint-disable */
/**
 * Trigger -> payload dispatch, generated from docspace-webhooks.yaml by
 * tools/gen-trigger-map.py. Do not edit; rerun ./generate.sh instead.
 */
import {
    FileEntryPayload,
    FileEntryPayloadFromJSON,
    FormSubmitPayload,
    FormSubmitPayloadFromJSON,
    GroupPayload,
    GroupPayloadFromJSON,
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
    'file.created': FileEntryPayload;
    'file.uploaded': FileEntryPayload;
    'file.updated': FileEntryPayload;
    'file.trashed': FileEntryPayload;
    'file.deleted': FileEntryPayload;
    'file.restored': FileEntryPayload;
    'file.copied': FileEntryPayload;
    'file.moved': FileEntryPayload;
    'file.downloaded': FileEntryPayload;
    'folder.created': FileEntryPayload;
    'folder.updated': FileEntryPayload;
    'folder.trashed': FileEntryPayload;
    'folder.deleted': FileEntryPayload;
    'folder.restored': FileEntryPayload;
    'folder.copied': FileEntryPayload;
    'folder.moved': FileEntryPayload;
    'folder.downloaded': FileEntryPayload;
    'room.created': FileEntryPayload;
    'room.updated': FileEntryPayload;
    'room.archived': FileEntryPayload;
    'room.deleted': FileEntryPayload;
    'room.restored': FileEntryPayload;
    'room.copied': FileEntryPayload;
    'agent.created': FileEntryPayload;
    'agent.updated': FileEntryPayload;
    'agent.deleted': FileEntryPayload;
    'form.submit': FormSubmitPayload;
    'form.filled.out': FileEntryPayload;
    'form.stopped': FileEntryPayload;
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
    'file.created': FileEntryPayloadFromJSON,
    'file.uploaded': FileEntryPayloadFromJSON,
    'file.updated': FileEntryPayloadFromJSON,
    'file.trashed': FileEntryPayloadFromJSON,
    'file.deleted': FileEntryPayloadFromJSON,
    'file.restored': FileEntryPayloadFromJSON,
    'file.copied': FileEntryPayloadFromJSON,
    'file.moved': FileEntryPayloadFromJSON,
    'file.downloaded': FileEntryPayloadFromJSON,
    'folder.created': FileEntryPayloadFromJSON,
    'folder.updated': FileEntryPayloadFromJSON,
    'folder.trashed': FileEntryPayloadFromJSON,
    'folder.deleted': FileEntryPayloadFromJSON,
    'folder.restored': FileEntryPayloadFromJSON,
    'folder.copied': FileEntryPayloadFromJSON,
    'folder.moved': FileEntryPayloadFromJSON,
    'folder.downloaded': FileEntryPayloadFromJSON,
    'room.created': FileEntryPayloadFromJSON,
    'room.updated': FileEntryPayloadFromJSON,
    'room.archived': FileEntryPayloadFromJSON,
    'room.deleted': FileEntryPayloadFromJSON,
    'room.restored': FileEntryPayloadFromJSON,
    'room.copied': FileEntryPayloadFromJSON,
    'agent.created': FileEntryPayloadFromJSON,
    'agent.updated': FileEntryPayloadFromJSON,
    'agent.deleted': FileEntryPayloadFromJSON,
    'form.submit': FormSubmitPayloadFromJSON,
    'form.filled.out': FileEntryPayloadFromJSON,
    'form.stopped': FileEntryPayloadFromJSON,
    '*': identity,
};

/** True when `trigger` is one this build of the SDK knows about. */
export function isKnownTrigger(trigger: string): trigger is WebhookTriggerName {
    return Object.prototype.hasOwnProperty.call(PAYLOAD_DESERIALIZERS, trigger);
}
