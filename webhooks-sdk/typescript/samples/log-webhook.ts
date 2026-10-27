/**
 * Logs a DocSpace webhook delivery to the console.
 *
 *   npm run sample                       # replay the shared fixture
 *   npm run sample -- <body> <secret> <signature> [<x-docspace-event-id>]
 */
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

import { DELIVERY_MODEL_NAMES, TypedTriggerName, hasDeliveryModel } from '../generated/triggers';
import {
    InMemoryWebhookEventStore,
    WebhookRouter,
    deliveryModelName,
    payloadModelName,
    type AnyWebhook,
    type WebhookConfigInfo,
    type WebhookEventInfo,
} from '../src';

const fixtures = resolve(__dirname, '../../../samples');
const [bodyPath, secret, signature, eventIdHeader] = [
    process.argv[2] ?? resolve(fixtures, 'file.created.json'),
    process.argv[3] ?? readFileSync(resolve(fixtures, 'SECRET'), 'utf8').trim(),
    process.argv[4] ?? readFileSync(resolve(fixtures, 'file.created.sig'), 'utf8').trim(),
    process.argv[5],
];

/**
 * The router this sample hands the delivery to. It checks the signature, drops
 * an event it has processed before (DocSpace delivers at least once, and a retry
 * carries the same event.id), reads the body into the delivery model of its
 * trigger and awaits the handlers. The store lives in memory for the sake of
 * the sample; a real receiver persists the ids for as long as an event can still
 * arrive -- the automatic retries span about 18 hours on the default schedule,
 * and a manual retry can come at any time later.
 */
const router = new WebhookRouter({ secret, store: new InMemoryWebhookEventStore() });

// One handler for every trigger with a typed body, from the generated table. A
// receiver normally registers only the triggers it cares about.
const typedTriggers = Object.keys(DELIVERY_MODEL_NAMES).filter(hasDeliveryModel) as TypedTriggerName[];

router.on(typedTriggers, (delivery, trigger) => {
    printHeader(trigger, delivery.event, delivery.webhook, deliveryModelName(trigger));
    printPayload(delivery.payload);
});

// A handler for one trigger: its argument is a FileDelivery, typed by the name.
router.on('file.created', d => {
    console.log(`  on('file.created'): "${d.payload?.title}" in folder ${d.payload?.parentId}`);
});

// Whatever has no handler: the catch-all '*', and triggers newer than this SDK.
router.onOther((hook: AnyWebhook) => {
    printHeader(hook.trigger, hook.event, hook.webhook, 'none');
    printPayload(hook.payload);
});

function printHeader(trigger: string, event: WebhookEventInfo | undefined, webhook: WebhookConfigInfo | undefined, delivery: string) {
    const known = payloadModelName(trigger) === 'unknown' ? '  (unknown to this build)' : '';
    console.log(`${trigger}${known}`);
    console.log('  signature ok');
    console.log(`  model: ${payloadModelName(trigger)}`);
    console.log(`  delivery: ${delivery}`);
    console.log(`  event #${event?.id}  at ${event?.createOn?.toISOString()}  by ${event?.createBy}`);
    console.log(`  subscription #${webhook?.id} "${webhook?.name}"`);
}

function printPayload(payload: unknown) {
    console.log();
    console.log(JSON.stringify(payload, null, 2).split('\n').map(l => '  ' + l).join('\n'));
}

async function main(): Promise<number> {
    // Read as bytes, never as a parsed object: the signature covers the raw body.
    const body = readFileSync(bodyPath);

    // In an HTTP handler, answer with result.statusCode once this resolves: a 2xx
    // acknowledges the delivery, anything else makes DocSpace send it again.
    const result = await router.handle(body, signature, eventIdHeader);

    switch (result.outcome) {
        case 'rejected':
            console.error('REJECTED: signature does not match');
            break;
        case 'invalid':
            console.error(`unparseable body: ${result.error}`);
            break;
        case 'failed':
            console.error(`a handler failed: ${result.error}`);
            break;
        case 'duplicate':
            console.log(`duplicate of event #${result.eventId}, already processed; skipped`);
            break;
    }

    return result.statusCode >= 200 && result.statusCode < 300 ? 0 : 1;
}

main().then(code => process.exit(code));
