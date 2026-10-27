/**
 * Receive ONLYOFFICE DocSpace webhooks in TypeScript.
 *
 * Two steps, in this order:
 *
 * ```ts
 * import { verifySignature, parseWebhook, readSignatureHeader } from '@onlyoffice/docspace-webhooks-sdk';
 *
 * // `raw` must be the unparsed request body.
 * if (!verifySignature(raw, secret, readSignatureHeader(req.headers))) {
 *     return res.status(401).end();
 * }
 * const hook = parseWebhook(raw);
 * if (!isKnownWebhook(hook)) return ack();   // trigger added after this build
 * ```
 *
 * Acknowledge before doing any real work: DocSpace waits 30 seconds for a
 * response, counts a timeout as a failed attempt, and gives up for good after
 * seven attempts over roughly 18 hours -- so a slow receiver both gets the same
 * event again and, after three days without a success, has its subscription
 * switched off.
 */
export {
    SIGNATURE_HEADER,
    computeSignature,
    verifySignature,
    readSignatureHeader,
    type RawBody,
} from './verify';

export {
    parseWebhook,
    parseDelivery,
    deliveryModelName,
    hasDeliveryModel,
    isKnownWebhook,
    isKnownTrigger,
    payloadModelName,
    WebhookParseError,
    type AnyWebhook,
    type DocSpaceWebhook,
    type WebhookTriggerName,
    type TriggerPayloadMap,
    type DocSpaceDelivery,
    type TriggerDeliveryMap,
    type TypedTriggerName,
} from './parse';

export * from '../generated/models';

export {
    WebhookRouter,
    InMemoryWebhookEventStore,
    type WebhookRouterOptions,
    type WebhookEventStore,
    type WebhookHandleResult,
    type WebhookOutcome,
    type DeliveryHandler,
    type OtherHandler,
} from './router';
