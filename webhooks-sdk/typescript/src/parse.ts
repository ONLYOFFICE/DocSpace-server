import {
    WebhookConfigInfo,
    WebhookConfigInfoFromJSON,
    WebhookEventInfo,
    WebhookEventInfoFromJSON,
} from '../generated/models';
import {
    PAYLOAD_DESERIALIZERS,
    TriggerPayloadMap,
    WebhookTriggerName,
    isKnownTrigger,
} from '../generated/triggers';
import type { RawBody } from './verify';

export type { WebhookTriggerName, TriggerPayloadMap };
export { isKnownTrigger };

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
 *         hook.payload.pureTitle;   // FilePayload -- note: files have no `title`
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
 * Narrow a parsed delivery to one of the contract's triggers, so that
 * `payload` becomes the concrete type for that trigger.
 */
export function isKnownWebhook(hook: AnyWebhook): hook is DocSpaceWebhook {
    return isKnownTrigger(hook.trigger);
}
