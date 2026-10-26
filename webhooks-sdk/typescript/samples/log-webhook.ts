/**
 * Logs a DocSpace webhook delivery to the console.
 *
 *   npm run sample                       # replay the shared fixture
 *   npm run sample -- <body> <secret> <signature> [<x-docspace-event-id>]
 */
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

import {
    verifySignature,
    parseWebhook,
    isKnownWebhook,
    payloadModelName,
    WebhookParseError,
} from '../src';

/**
 * The events already handled. DocSpace delivers at least once: an automatic
 * retry, or a manual retry from the delivery log, sends the same event.id
 * again, and the signature does not prove when a request was sent. So the id is
 * the replay and duplicate defence. This set lives in memory for the sake of the
 * sample; a real receiver persists the ids for as long as an event can still
 * arrive -- the automatic retries span about 18 hours on the default schedule,
 * and a manual retry can come at any time later.
 */
const processedEvents = new Set<number>();

/**
 * A cheap pre-check on the `x-docspace-event-id` header, before verifying or
 * parsing anything. The header is not signed, so it may only ever make the
 * receiver DROP a delivery (fail-safe: whoever can rewrite a header can drop the
 * request anyway), never accept one. In an HTTP handler:
 *
 *     if (seenByHeader(req.headers['x-docspace-event-id'])) return res.status(200).end();
 */
function seenByHeader(header: string | string[] | undefined): boolean {
    const id = Number(Array.isArray(header) ? header[0] : header);
    return Number.isInteger(id) && processedEvents.has(id);
}

const fixtures = resolve(__dirname, '../../../samples');
const [bodyPath, secret, signature] = [
    process.argv[2] ?? resolve(fixtures, 'file.created.json'),
    process.argv[3] ?? readFileSync(resolve(fixtures, 'SECRET'), 'utf8').trim(),
    process.argv[4] ?? readFileSync(resolve(fixtures, 'file.created.sig'), 'utf8').trim(),
];

// The header pre-check comes first: dropping on it costs nothing.
if (seenByHeader(process.argv[5])) {
    console.log(`duplicate of event #${process.argv[5]} (header pre-check), skipped`);
    process.exit(0);
}

// Read as bytes, never as a parsed object: the signature covers the raw body.
const body = readFileSync(bodyPath);

if (!verifySignature(body, secret, signature)) {
    console.error('REJECTED: signature does not match');
    process.exit(1);
}

let hook;
try {
    hook = parseWebhook(body);
} catch (err) {
    if (err instanceof WebhookParseError) {
        console.error(`unparseable body: ${err.message}`);
        process.exit(1);
    }
    throw err;
}

// The authoritative duplicate check, on the id from the verified body.
if (processedEvents.has(hook.event.id!)) {
    console.log(`${hook.trigger}  duplicate of event #${hook.event.id}, already processed; skipped`);
    process.exit(0);
}
processedEvents.add(hook.event.id!);

console.log(`${hook.trigger}${isKnownWebhook(hook) ? '' : '  (unknown to this build)'}`);
console.log('  signature ok');
console.log(`  model: ${payloadModelName(hook.trigger)}`);
console.log(`  event #${hook.event.id}  at ${hook.event.createOn?.toISOString()}  by ${hook.event.createBy}`);
console.log(`  subscription #${hook.webhook.id} "${hook.webhook.name}"`);

if (isKnownWebhook(hook)) {
    switch (hook.trigger) {
        case 'user.created':
        case 'user.updated':
            console.log(`  user: ${hook.payload.userName} <${hook.payload.email}>`);
            break;
        case 'file.created':
            // FilePayload: the file fields, not just the ones files, folders
            // and rooms share.
            console.log(`  file: ${hook.payload.title}  id=${hook.payload.id}  parent=${hook.payload.parentId}`);
            break;
        case 'folder.created':
            console.log(`  folder: ${hook.payload.title}  id=${hook.payload.id}  parent=${hook.payload.parentId}`);
            break;
        case 'room.created':
            console.log(`  room: ${hook.payload.title}  id=${hook.payload.id}  roomType=${hook.payload.roomType}`);
            break;
    }
}

console.log();
console.log(JSON.stringify((hook.raw as { payload: unknown }).payload, null, 2)
    .split('\n').map(l => '  ' + l).join('\n'));
