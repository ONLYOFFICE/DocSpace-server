import { createHmac, timingSafeEqual } from 'node:crypto';

/** Header DocSpace signs every delivery with. */
export const SIGNATURE_HEADER = 'x-docspace-signature-256';

const PREFIX = 'sha256=';

/** Anything an HTTP framework is likely to hand you as the raw body. */
export type RawBody = string | Uint8Array;

function toBytes(body: RawBody): Buffer {
    // The server signs Encoding.UTF8.GetBytes(payload), so a string body must
    // be measured in UTF-8 bytes, not UTF-16 code units.
    return typeof body === 'string' ? Buffer.from(body, 'utf8') : Buffer.from(body);
}

/**
 * The signature DocSpace would send for this body, in the server's own format:
 * `sha256=` followed by UPPERCASE hex.
 *
 * Mirrors `WebhookSender.GetSecretHash` (HMACSHA256 + Convert.ToHexString).
 */
export function computeSignature(body: RawBody, secret: string): string {
    const mac = createHmac('sha256', Buffer.from(secret, 'utf8'));
    mac.update(toBytes(body));
    return PREFIX + mac.digest('hex').toUpperCase();
}

/**
 * Check the `x-docspace-signature-256` header against the body.
 *
 * Three things this gets right that a hand-rolled check usually does not:
 *
 * 1. It hashes the **raw** body. Verify before parsing; a body that has been
 *    through `JSON.parse` + `JSON.stringify` is a different byte sequence and
 *    will never match. With Express, that means `express.raw()` or
 *    `express.json({ verify })`, not the parsed `req.body`.
 * 2. It compares case-insensitively. DocSpace emits uppercase hex where GitHub
 *    emits lowercase, so receiver code ported from GitHub's docs fails here.
 * 3. It compares in constant time.
 *
 * Returns false rather than throwing for a missing or malformed header, so a
 * caller can treat every rejection the same way.
 */
export function verifySignature(
    body: RawBody,
    secret: string,
    signature: string | string[] | undefined | null,
): boolean {
    if (typeof signature !== 'string' || signature.length === 0) {
        return false;
    }

    const expected = Buffer.from(computeSignature(body, secret).toLowerCase(), 'utf8');
    const received = Buffer.from(signature.trim().toLowerCase(), 'utf8');

    // Length is not a secret (a SHA-256 hex digest is always 64 chars), and
    // timingSafeEqual throws on a length mismatch, so screen it out first.
    if (expected.length !== received.length) {
        return false;
    }

    return timingSafeEqual(expected, received);
}

/** Case-insensitive header lookup, for frameworks that do not normalise. */
export function readSignatureHeader(
    headers: Record<string, string | string[] | undefined>,
): string | undefined {
    for (const [key, value] of Object.entries(headers)) {
        if (key.toLowerCase() === SIGNATURE_HEADER) {
            return Array.isArray(value) ? value[0] : value;
        }
    }
    return undefined;
}
