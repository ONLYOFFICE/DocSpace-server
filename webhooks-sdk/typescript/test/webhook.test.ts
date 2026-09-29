import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

import { computeSignature, verifySignature, readSignatureHeader } from '../src/verify';
import { parseWebhook, isKnownWebhook, isKnownTrigger, WebhookParseError } from '../src/parse';

// The key of the subscription that sent the captured delivery, and the signature
// DocSpace put on the wire for it. Read from the shared fixture files so the nine
// language samples and these tests cannot drift apart.
const REAL_SECRET = readFileSync(resolve(__dirname, '../../../samples/SECRET'), 'utf8').trim();
const REAL_SIGNATURE = readFileSync(resolve(__dirname, '../../../samples/file.created.sig'), 'utf8').trim();

// A synthetic body used for the algorithm edge cases below, with its own key.
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

// A real production delivery, captured from a DocSpace portal. The parse tests
// run against this rather than a hand-written body, because the wire shape had
// surprises no amount of reading the C# predicted.
const REAL = readFileSync(resolve(__dirname, '../../../samples/file.created.json'), 'utf8');

const EXPECTED = 'sha256=E87CD656882927F296A70F1B7A67F83F9F70D7F087A108088E7B5CE223B3088E';

test('computeSignature reproduces a real DocSpace signature', () => {
    // The one test here that proves parity with WebhookSender.GetSecretHash rather
    // than with another implementation of the same idea: body, key and signature
    // are all captured from a portal, so agreeing with them means agreeing with the
    // server. It also pins the fixture bytes -- reformatting file.created.json by so
    // much as a space breaks this.
    assert.equal(computeSignature(REAL, REAL_SECRET), REAL_SIGNATURE);
    assert.equal(verifySignature(REAL, REAL_SECRET, REAL_SIGNATURE), true);
});

test('computeSignature is self-consistent on a synthetic body', () => {
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
    assert.equal(hook.event.id, 3);
    assert.equal(hook.webhook.name, 'test');
    assert.ok(isKnownWebhook(hook));

    if (hook.trigger === 'file.created') {
        assert.equal(hook.payload.title, 'TestDoc.docx');
        assert.equal(hook.payload.fileEntryType, 2);
        assert.equal(hook.payload.id, 47);
    } else {
        assert.fail('trigger did not narrow');
    }
});

test('the captured delivery carries File-specific fields', () => {
    // The whole point of the 1.0.0 payload change, asserted against a real capture.
    // Before it, the publisher typed the payload as the abstract FileEntry<T>, so
    // System.Text.Json serialized by declared type and every one of these was
    // silently dropped -- fileEntryType was the only thing telling a file from a
    // folder. If this test ever goes red, the declared type has been narrowed again
    // somewhere on the publish path.
    const hook = parseWebhook(REAL);

    assert.ok(isKnownWebhook(hook));
    if (hook.trigger !== 'file.created') {
        return assert.fail('fixture is no longer a file.created delivery');
    }

    assert.equal(hook.payload.version, 1);
    assert.equal(hook.payload.versionGroup, 1);
    assert.equal(hook.payload.contentLength, 7726);
    assert.equal(hook.payload.fileType, 7);
    assert.equal(hook.payload.fileExst, '.docx');
    assert.equal(hook.payload.comment, 'Created');

    // contentLength is a number of bytes, not the REST API's formatted string.
    assert.equal(typeof hook.payload.contentLength, 'number');
});

test('the captured delivery carries no caller-relative state', () => {
    // 1.0.0 stopped putting one user's permission matrix, and the SSO/LDAP
    // identifiers, on the wire. Guard that they stay off it.
    const payload = JSON.parse(REAL).payload as Record<string, unknown>;

    for (const absent of ['security', 'securityByUsers', 'shareRecord', 'access',
                          'shared', 'sharedForUser', 'sharedExternal', 'parentShared',
                          'availableShareRights', 'requestToken']) {
        assert.ok(!(absent in payload), `${absent} should not be on the wire`);
    }
});

test('nested users are summaries, not full user payloads', () => {
    const payload = JSON.parse(REAL).payload as { createdBy: Record<string, unknown> };

    assert.equal(payload.createdBy.userName, 'administrator');
    // A summary: no groups, quota, admin flags or SSO identifiers ride along.
    for (const absent of ['groups', 'quotaLimit', 'isAdmin', 'sid', 'ssoNameId', 'ssoSessionId']) {
        assert.ok(!(absent in payload.createdBy), `createdBy.${absent} should not be on the wire`);
    }
});

test('parseWebhook tolerates a trigger this build does not know', () => {
    const body = REAL.replace('"file.created"', '"file.teleported"');
    const hook = parseWebhook(body);

    assert.equal(hook.trigger, 'file.teleported');
    assert.equal(isKnownWebhook(hook), false);
    assert.equal(isKnownTrigger(hook.trigger), false);
    // Payload survives unnarrowed rather than blowing up.
    assert.equal((hook.payload as { title: string }).title, 'TestDoc.docx');
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
