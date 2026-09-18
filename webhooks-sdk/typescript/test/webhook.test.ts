import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

import { computeSignature, verifySignature, readSignatureHeader } from '../src/verify';
import { parseWebhook, isKnownWebhook, isKnownTrigger, WebhookParseError } from '../src/parse';

const SECRET = 'test123!@#%ABC';

// A realistic envelope. Kept as an exact string, not JSON.stringify of an
// object: the signature is over these precise bytes, and re-serializing would
// change them.
const BODY =
    '{"event":{"id":42,"createOn":"2026-09-16T10:30:00Z",' +
    '"createBy":"c9b1f0e2-3a4b-4c5d-8e9f-0a1b2c3d4e5f",' +
    '"trigger":"file.created","triggerId":128},' +
    '"payload":{"id":1001,"pureTitle":"Q3 report.docx","fileEntryType":1},' +
    '"webhook":{"id":7,"name":"ci","url":"https://example.com/hooks",' +
    '"triggers":["file.created"]}}';

// Computed independently (Python hmac/hashlib) from the same inputs, so this
// pins parity with WebhookSender.GetSecretHash rather than just with itself.
// A real production delivery, captured from a DocSpace portal. The parse tests
// run against this rather than a hand-written body, because the wire shape had
// surprises no amount of reading the C# predicted.
const REAL = readFileSync(resolve(__dirname, '../../../samples/file.created.json'), 'utf8');

const EXPECTED = 'sha256=E87CD656882927F296A70F1B7A67F83F9F70D7F087A108088E7B5CE223B3088E';

test('computeSignature matches the server algorithm', () => {
    assert.equal(computeSignature(BODY, SECRET), EXPECTED);
});

test('signature is uppercase hex, as DocSpace emits it', () => {
    const hex = computeSignature(BODY, SECRET).slice('sha256='.length);
    assert.equal(hex, hex.toUpperCase());
    assert.equal(hex.length, 64);
});

test('hashes UTF-8 bytes, not UTF-16 units', () => {
    const body = '{"payload":{"pureTitle":"Отчёт.docx"}}';
    assert.equal(
        computeSignature(body, SECRET),
        'sha256=08EF5457202BA6E1E7D023B9C1975755B307BB10D49A09F0675A2FB97065E0CC',
    );
});

test('string and byte bodies agree', () => {
    assert.equal(computeSignature(Buffer.from(BODY, 'utf8'), SECRET), EXPECTED);
});

test('verifySignature accepts the genuine signature', () => {
    assert.equal(verifySignature(BODY, SECRET, EXPECTED), true);
});

test('verifySignature accepts a lowercased signature', () => {
    // Receivers ported from GitHub's docs lowercase the digest; DocSpace does
    // not, so the comparison has to be case-insensitive.
    assert.equal(verifySignature(BODY, SECRET, EXPECTED.toLowerCase()), true);
});

test('verifySignature rejects a tampered body', () => {
    const tampered = BODY.replace('Q3 report.docx', 'Q4 report.docx');
    assert.equal(verifySignature(tampered, SECRET, EXPECTED), false);
});

test('verifySignature rejects the wrong secret', () => {
    assert.equal(verifySignature(BODY, 'not-the-secret', EXPECTED), false);
});

test('verifySignature rejects missing or malformed headers', () => {
    assert.equal(verifySignature(BODY, SECRET, undefined), false);
    assert.equal(verifySignature(BODY, SECRET, null), false);
    assert.equal(verifySignature(BODY, SECRET, ''), false);
    assert.equal(verifySignature(BODY, SECRET, 'garbage'), false);
    // right digest, missing the sha256= prefix
    assert.equal(verifySignature(BODY, SECRET, EXPECTED.slice(7)), false);
});

test('readSignatureHeader is case-insensitive and unwraps arrays', () => {
    assert.equal(readSignatureHeader({ 'X-DocSpace-Signature-256': EXPECTED }), EXPECTED);
    assert.equal(readSignatureHeader({ 'x-docspace-signature-256': [EXPECTED] }), EXPECTED);
    assert.equal(readSignatureHeader({ 'content-type': 'application/json' }), undefined);
});

test('parseWebhook narrows the payload by trigger', () => {
    const hook = parseWebhook(REAL);

    assert.equal(hook.trigger, 'file.created');
    assert.equal(hook.event.id, 138769);
    assert.equal(hook.webhook.name, 'testsdk');
    assert.ok(isKnownWebhook(hook));

    if (hook.trigger === 'file.created') {
        // One shape for files and folders: the publisher serializes the
        // FileEntry<T> base, so `title` is present (not `pureTitle`) and
        // fileEntryType is the only discriminator. 2 = file, 1 = folder.
        assert.equal(hook.payload.title, '321.xlsx');
        assert.equal(hook.payload.fileEntryType, 2);
        assert.equal(hook.payload.id, 3358285);
    } else {
        assert.fail('trigger did not narrow');
    }
});

test('no File- or Folder-specific field is ever sent', () => {
    // Guards the correction that the captured delivery forced: File<T> and
    // Folder<T> members never reach the wire, because T1 binds to the base.
    const payload = JSON.parse(REAL).payload as Record<string, unknown>;
    for (const absent of ['pureTitle', 'version', 'contentLength', 'folderType',
                          'filesCount', 'foldersCount', 'isRoom']) {
        assert.ok(!(absent in payload), `${absent} should not be on the wire`);
    }
    assert.ok('title' in payload);
});

test('parseWebhook tolerates a trigger this build does not know', () => {
    const body = REAL.replace('"file.created"', '"file.teleported"');
    const hook = parseWebhook(body);

    assert.equal(hook.trigger, 'file.teleported');
    assert.equal(isKnownWebhook(hook), false);
    assert.equal(isKnownTrigger(hook.trigger), false);
    // Payload survives unnarrowed rather than blowing up.
    assert.equal((hook.payload as { title: string }).title, '321.xlsx');
});

test('every contract trigger is dispatchable', () => {
    for (const t of ['user.created', 'group.deleted', 'folder.moved', 'room.archived',
                     'agent.updated', 'form.submit', 'form.stopped', 'file.downloaded']) {
        assert.equal(isKnownTrigger(t), true, `${t} missing from the dispatch table`);
    }
});

test('parseWebhook rejects bodies that are not envelopes', () => {
    assert.throws(() => parseWebhook('not json'), WebhookParseError);
    assert.throws(() => parseWebhook('[]'), WebhookParseError);
    assert.throws(() => parseWebhook('{"event":{}}'), WebhookParseError);
});
