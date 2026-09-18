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
 * Acknowledge before doing any real work: DocSpace retries 5 times with
 * exponential backoff from 1s and then gives up for good -- about 31 seconds
 * in total -- so a slow receiver loses events permanently.
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
    isKnownWebhook,
    isKnownTrigger,
    WebhookParseError,
    type AnyWebhook,
    type DocSpaceWebhook,
    type WebhookTriggerName,
    type TriggerPayloadMap,
} from './parse';

export * from '../generated/models';
