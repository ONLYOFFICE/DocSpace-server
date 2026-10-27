import {
    WebhookConfigInfo,
    WebhookConfigInfoFromJSON,
    WebhookEventInfo,
    WebhookEventInfoFromJSON,
} from '../generated/models';
import {
    DELIVERY_DESERIALIZERS,
    DELIVERY_MODEL_NAMES,
    PAYLOAD_DESERIALIZERS,
    PAYLOAD_MODEL_NAMES,
    TriggerDeliveryMap,
    TriggerPayloadMap,
    TypedTriggerName,
    WebhookTriggerName,
    hasDeliveryModel,
    isKnownTrigger,
} from '../generated/triggers';
import type { RawBody } from './verify';

export type { WebhookTriggerName, TriggerPayloadMap, TypedTriggerName, TriggerDeliveryMap };
export { isKnownTrigger, hasDeliveryModel };

/**
 * A delivery whose trigger this build of the SDK does not recognise, so the
 * payload could not be narrowed. DocSpace gains triggers over time; a receiver
 * that treats an unfamiliar one as an error breaks on the next server upgrade.
 */
export interface AnyWebhook {
    trigger: string;
    event: WebhookEventInfo;
    payload: unknown;
    webhook: WebhookConfigInfo;
    /** The envelope as received, before narrowing. */
    raw: unknown;
}

/**
 * A delivery on a trigger from the contract, with `payload` narrowed to the
 * type that trigger carries. Switch on `trigger` and the payload follows:
 *
 * ```ts
 * const hook = parseWebhook(raw);
 * if (!isKnownWebhook(hook)) return ack();   // new trigger, nothing to do
 *
 * switch (hook.trigger) {
 *     case 'file.created':
 *         hook.payload.version;     // FilePayload -- file fields, not just the shared ones
 *         break;
 *     case 'user.created':
 *         hook.payload.email;       // UserPayload
 *         break;
 * }
 * ```
 */
export type DocSpaceWebhook = {
    [K in WebhookTriggerName]: {
        trigger: K;
        event: WebhookEventInfo;
        payload: TriggerPayloadMap[K];
        webhook: WebhookConfigInfo;
        raw: unknown;
    };
}[WebhookTriggerName];

export class WebhookParseError extends Error {}

/**
 * Turn a verified body into a delivery.
 *
 * Call this only on a body that has already passed `verifySignature` -- parsing
 * first and verifying afterwards defeats the signature.
 *
 * The result is deliberately the wide `AnyWebhook`. Narrow it with
 * `isKnownWebhook` to get a typed payload; that one extra line is what makes a
 * receiver survive triggers added after this SDK was built.
 */
export function parseWebhook(body: RawBody): AnyWebhook {
    const { json, trigger } = readEnvelope(body);

    // A typed trigger always carries its payload object; the generated deserializers
    // pass null and undefined through, which would hand the caller a missing typed
    // payload. The untyped '*' stays opaque, whatever it carries.
    const model = isKnownTrigger(trigger) ? PAYLOAD_MODEL_NAMES[trigger] : null;
    if (model !== null
        && (json.payload === null || typeof json.payload !== 'object' || Array.isArray(json.payload))) {
        throw new WebhookParseError(`no ${model} payload for '${trigger}'`);
    }

    const payload = isKnownTrigger(trigger)
        ? PAYLOAD_DESERIALIZERS[trigger](json.payload)
        : (json.payload as unknown);

    return {
        trigger,
        event: WebhookEventInfoFromJSON(json.event),
        payload,
        webhook: WebhookConfigInfoFromJSON(json.webhook),
        raw: json,
    };
}

/**
 * The name of the payload model the trigger carries, as the contract names the
 * schema (`FilePayload`, `UserPayload`, ...); `'untyped'` for the legacy
 * catch-all `'*'`, and `'unknown'` for a trigger added after this build.
 */
export function payloadModelName(trigger: string): string {
    return isKnownTrigger(trigger) ? (PAYLOAD_MODEL_NAMES[trigger] ?? 'untyped') : 'unknown';
}

/**
 * Narrow a parsed delivery to one of the contract's triggers, so that
 * `payload` becomes the concrete type for that trigger.
 */
export function isKnownWebhook(hook: AnyWebhook): hook is DocSpaceWebhook {
    return isKnownTrigger(hook.trigger);
}

/**
 * A delivery read whole into the model its trigger carries: `FileDelivery` for
 * `'file.created'`, and so on. Narrowing on `trigger` narrows `delivery`.
 */
export type DocSpaceDelivery = {
    [K in TypedTriggerName]: {
        trigger: K;
        delivery: TriggerDeliveryMap[K];
    };
}[TypedTriggerName];

/**
 * Parse a verified body into the delivery model of its trigger.
 *
 * Returns `null` when the trigger has no typed body: the untyped `'*'`, or a
 * trigger added after this build -- not an error, for the same reason as in
 * `parseWebhook`. Throws `WebhookParseError` when the body is not an envelope or
 * a typed trigger arrives without its payload object.
 */
export function parseDelivery(body: RawBody): DocSpaceDelivery | null {
    const { json, trigger } = readEnvelope(body);

    if (!hasDeliveryModel(trigger)) {
        return null;
    }

    if (json.payload === null || typeof json.payload !== 'object' || Array.isArray(json.payload)) {
        throw new WebhookParseError(`no payload object for '${trigger}'`);
    }

    return { trigger, delivery: DELIVERY_DESERIALIZERS[trigger](json) } as DocSpaceDelivery;
}

/**
 * The name of the delivery model the trigger carries (`FileDelivery`, ...);
 * `'none'` when it has no typed body.
 */
export function deliveryModelName(trigger: string): string {
    return isKnownTrigger(trigger) ? (DELIVERY_MODEL_NAMES[trigger] ?? 'none') : 'none';
}

function readEnvelope(body: RawBody): { json: any; trigger: string } {
    const text = typeof body === 'string' ? body : Buffer.from(body).toString('utf8');

    let json: any;
    try {
        json = JSON.parse(text);
    } catch (cause) {
        throw new WebhookParseError(`body is not JSON: ${(cause as Error).message}`);
    }

    if (json === null || typeof json !== 'object' || Array.isArray(json)) {
        throw new WebhookParseError('body is not a webhook envelope');
    }

    const trigger: unknown = json.event?.trigger;
    if (typeof trigger !== 'string') {
        throw new WebhookParseError('envelope has no event.trigger');
    }

    return { json, trigger };
}
