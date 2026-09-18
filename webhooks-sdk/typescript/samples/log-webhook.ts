/**
 * Logs a DocSpace webhook delivery to the console.
 *
 *   npm run sample                       # replay the shared fixture
 *   npm run sample -- <body> <secret> <signature>
 */
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

import {
    verifySignature,
    parseWebhook,
    isKnownWebhook,
    WebhookParseError,
} from '../src';

const fixtures = resolve(__dirname, '../../../samples');
const [bodyPath, secret, signature] = [
    process.argv[2] ?? resolve(fixtures, 'file.created.json'),
    process.argv[3] ?? readFileSync(resolve(fixtures, 'SECRET'), 'utf8').trim(),
    process.argv[4] ?? readFileSync(resolve(fixtures, 'file.created.sig'), 'utf8').trim(),
];

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

console.log(`${hook.trigger}${isKnownWebhook(hook) ? '' : '  (unknown to this build)'}`);
console.log('  signature ok');
console.log(`  event #${hook.event.id}  at ${hook.event.createOn?.toISOString()}  by ${hook.event.createBy}`);
console.log(`  subscription #${hook.webhook.id} "${hook.webhook.name}"`);

if (isKnownWebhook(hook)) {
    switch (hook.trigger) {
        case 'user.created':
        case 'user.updated':
            console.log(`  user: ${hook.payload.userName} <${hook.payload.email}>`);
            break;
        case 'file.created':
        case 'folder.created':
        case 'room.created':
            // One payload shape for files and folders alike; fileEntryType is
            // the only discriminator (1 = folder, 2 = file).
            console.log(
                `  ${hook.payload.fileEntryType === 2 ? 'file' : 'folder'}: ` +
                `${hook.payload.title}  id=${hook.payload.id}  parent=${hook.payload.parentId}`,
            );
            break;
    }
}

console.log();
console.log(JSON.stringify((hook.raw as { payload: unknown }).payload, null, 2)
    .split('\n').map(l => '  ' + l).join('\n'));
