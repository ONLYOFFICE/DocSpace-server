import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

import type { FileDelivery } from '../generated/models';
import { InMemoryWebhookEventStore, WebhookRouter } from '../src/router';
import { computeSignature } from '../src/verify';

// The captured deliveries the nine samples replay, with the key that signed them.
const SAMPLES = resolve(__dirname, '../../../samples');
const SECRET = readFileSync(resolve(SAMPLES, 'SECRET'), 'utf8').trim();
const fixture = (name: string) => ({
    body: readFileSync(resolve(SAMPLES, `${name}.json`), 'utf8'),
    signature: readFileSync(resolve(SAMPLES, `${name}.sig`), 'utf8').trim(),
});

test('on() hands the trigger its delivery model', async () => {
    const router = new WebhookRouter({ secret: SECRET });
    const { body, signature } = fixture('file.created');
    const seen: FileDelivery[] = [];

    router.on('file.created', d => {
        seen.push(d);
    });

    const result = await router.handle(body, signature);

    assert.equal(result.outcome, 'handled');
    assert.equal(result.statusCode, 200);
    assert.equal(seen.length, 1);
    assert.equal(seen[0].payload?.title, JSON.parse(body).payload.title);
    assert.equal(seen[0].event?.trigger, 'file.created');
});

test('every handler of a trigger runs, in registration order, and is awaited', async () => {
    const router = new WebhookRouter({ secret: SECRET });
    const { body, signature } = fixture('file.created');
    const order: string[] = [];

    router.on('file.created', async () => {
        await new Promise(r => setTimeout(r, 10));
        order.push('first');
    });
    router.on('file.created', () => {
        order.push('second');
    });

    await router.handle(body, signature);

    assert.deepEqual(order, ['first', 'second']);
});

test('the function on() returns removes the handler, as off() does', async () => {
    const router = new WebhookRouter({ secret: SECRET });
    const { body, signature } = fixture('file.created');
    let calls = 0;
    const handler = () => {
        calls++;
    };

    const off = router.on('file.created', handler);
    off();
    assert.equal((await router.handle(body, signature)).outcome, 'unhandled');

    router.on('file.created', handler);
    router.off('file.created', handler);
    assert.equal((await router.handle(body, signature)).outcome, 'unhandled');

    assert.equal(calls, 0);
});

test('one handler can take several triggers', async () => {
    const router = new WebhookRouter({ secret: SECRET });
    const triggers: string[] = [];

    router.on(['user.created', 'user.deleted'], (d, trigger) => {
        triggers.push(`${trigger}:${d.payload?.id !== undefined}`);
    });

    for (const name of ['user.created', 'user.deleted', 'user.updated']) {
        const { body, signature } = fixture(name);
        await router.handle(body, signature);
    }

    assert.deepEqual(triggers, ['user.created:true', 'user.deleted:true']);
});

test('onOther() takes what no handler takes, the unknown trigger included', async () => {
    const router = new WebhookRouter({ secret: SECRET });
    const other: string[] = [];

    router.on('file.created', () => undefined);
    router.onOther(hook => {
        other.push(hook.trigger);
    });

    for (const name of ['file.created', 'room.created', 'unknown.trigger']) {
        const { body, signature } = fixture(name);
        assert.equal((await router.handle(body, signature)).outcome, 'handled');
    }

    assert.deepEqual(other, ['room.created', JSON.parse(fixture('unknown.trigger').body).event.trigger]);
});

test('a bad signature is rejected before any handler runs', async () => {
    const router = new WebhookRouter({ secret: SECRET });
    const { body } = fixture('file.created');
    let called = false;

    router.on('file.created', () => {
        called = true;
    });

    const result = await router.handle(body, 'sha256=' + '0'.repeat(64));

    assert.equal(result.outcome, 'rejected');
    assert.equal(result.statusCode, 401);
    assert.equal(called, false);
});

test('a typed trigger without its payload is invalid', async () => {
    const router = new WebhookRouter({ secret: SECRET });
    const body = readFileSync(resolve(SAMPLES, 'negative/no-payload.json'), 'utf8');
    const signature = readFileSync(resolve(SAMPLES, 'negative/no-payload.sig'), 'utf8').trim();

    const result = await router.handle(body, signature);

    assert.equal(result.outcome, 'invalid');
    assert.equal(result.statusCode, 400);
});

test('a throwing handler fails the delivery, and the event stays unprocessed', async () => {
    const store = new InMemoryWebhookEventStore();
    const router = new WebhookRouter({ secret: SECRET, store });
    const { body, signature } = fixture('file.created');
    let fail = true;

    router.on('file.created', () => {
        if (fail) {
            throw new Error('database is down');
        }
    });

    const failed = await router.handle(body, signature);
    assert.equal(failed.outcome, 'failed');
    assert.equal(failed.statusCode, 500);
    assert.equal(failed.error, 'database is down');

    // DocSpace retries; this time the handler succeeds
    fail = false;
    assert.equal((await router.handle(body, signature)).outcome, 'handled');
});

test('a processed event is a duplicate, by body and by header', async () => {
    const router = new WebhookRouter({ secret: SECRET, store: new InMemoryWebhookEventStore() });
    const { body, signature } = fixture('file.created');
    let calls = 0;

    router.on('file.created', () => {
        calls++;
    });

    assert.equal((await router.handle(body, signature)).outcome, 'handled');
    // a retry of the event carries the same event.id
    assert.equal((await router.handle(body, signature)).outcome, 'duplicate');
    assert.equal((await router.handle('not even json', null, String(JSON.parse(body).event.id))).outcome, 'duplicate');

    assert.equal(calls, 1);
});

test('a router needs a secret unless it is for unsigned replay', async () => {
    assert.throws(() => new WebhookRouter({}));

    const router = new WebhookRouter({ unsignedReplay: true });
    const body = fixture('file.created').body;

    assert.equal((await router.handle(body, undefined)).outcome, 'unhandled');
    assert.notEqual(computeSignature(body, SECRET), '');
});
