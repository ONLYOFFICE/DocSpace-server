// Sample scenarios and generated rows, without a builder.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { readSample, merge, SampleError } from '../src/samples.mjs';
import { generate } from '../src/generate.mjs';

function folder(files) {
    const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'docbuilder-samples-'));
    for (const [name, value] of Object.entries(files)) {
        fs.mkdirSync(path.dirname(path.join(dir, name)), { recursive: true });
        fs.writeFileSync(path.join(dir, name), JSON.stringify(value));
    }
    return dir;
}

test('objects merge key by key, arrays and values replace', () => {
    assert.deepEqual(
        merge({ a: { x: 1, y: [1, 2] }, b: 1 }, { a: { y: [3] }, c: 2 }),
        { a: { x: 1, y: [3] }, b: 1, c: 2 });
});

test('a scenario is the file extends, then its base, then its own extends, then its placeholders', () => {
    const dir = folder({
        '_shared/en.json': { placeholders: { inputData: { label: 'Company:', lang: 'en' } } },
        '_shared/ru.json': { extends: ['en'], placeholders: { inputData: { label: 'Компания:', lang: 'ru' } } },
        'Report.sample.json': {
            extends: ['_shared/en'],
            default: 'main',
            scenarios: {
                main: { placeholders: { inputData: { title: 'Main', rows: [1, 2] } } },
                russian: { base: 'main', extends: ['_shared/ru'], placeholders: { inputData: { title: 'Главный' } } },
            },
        },
    });
    const sample = readSample(path.join(dir, 'Report.sample.json'));
    assert.equal(sample.default, 'main');
    assert.deepEqual(sample.scenarios.map(s => s.name), ['main', 'russian']);
    assert.deepEqual(sample.resolve().placeholders.inputData, { label: 'Company:', lang: 'en', title: 'Main', rows: [1, 2] });
    assert.deepEqual(sample.resolve('russian').placeholders.inputData, { label: 'Компания:', lang: 'ru', title: 'Главный', rows: [1, 2] });
});

test('the first format - one "placeholders" object - is a single scenario', () => {
    const dir = folder({ 'Old.sample.json': { placeholders: { inputData: { a: 1 } } } });
    const sample = readSample(path.join(dir, 'Old.sample.json'));
    assert.deepEqual(sample.scenarios.map(s => s.name), ['default']);
    assert.deepEqual(sample.resolve().placeholders, { inputData: { a: 1 } });
});

test('mistakes in a sample are named, not thrown as crashes', () => {
    const dir = folder({
        'Bad.sample.json': { scenarios: { a: { base: 'b' }, b: { base: 'a' }, c: { base: 'missing' }, d: { extends: ['_shared/nope'] } } },
    });
    const sample = readSample(path.join(dir, 'Bad.sample.json'));
    assert.throws(() => sample.resolve('a'), e => e instanceof SampleError && /loop/.test(e.message));
    assert.throws(() => sample.resolve('c'), e => e instanceof SampleError && /unknown base "missing"/.test(e.message));
    assert.throws(() => sample.resolve('d'), e => e instanceof SampleError && /nope\.json: not found/.test(e.message));
    assert.throws(() => sample.resolve('zzz'), e => e instanceof SampleError && /no scenario "zzz"/.test(e.message));
});

test('generated rows are the same for the same seed and differ for another', () => {
    const spec = { count: 50, seed: 4, strings: true, columns: ['ip', 'country', 'city', 'date', 'user', { kind: 'int', min: 1, max: 9 }] };
    const first = generate(spec);
    assert.equal(first.length, 50);
    assert.deepEqual(generate(spec), first);
    assert.notDeepEqual(generate({ ...spec, seed: 5 }), first);
    assert.ok(first.every(row => row.length === 6 && row.every(v => v === null || typeof v === 'string')));
});

test('generated dates run newest first and use the culture format', () => {
    const en = generate({ count: 30, columns: [{ kind: 'date', from: '2026-09-01', to: '2026-10-22' }] }).map(r => r[0]);
    assert.match(en[0], /^\d{1,2}\/\d{1,2}\/2026 \d{1,2}:\d{2}:\d{2} (AM|PM)$/);
    const ru = generate({ count: 30, locale: 'ru-RU', columns: [{ kind: 'date', format: 'iso' }] }).map(r => r[0]);
    assert.deepEqual([...ru].sort().reverse(), ru);
    assert.match(generate({ count: 1, locale: 'ru-RU', columns: ['date'] })[0][0], /^\d{2}\.\d{2}\.2026 \d{1,2}:\d{2}:\d{2}$/);
});

test('country and city come from one place, fields make objects, nulls appear at their share', () => {
    const rows = generate({ count: 200, seed: 2, fields: { country: 'country', city: 'city', note: { kind: 'sentence', nulls: 0.5 } } });
    const places = new Set(rows.map(r => `${r.country}|${r.city}`));
    assert.ok([...places].every(p => !p.startsWith('Germany|') || /Düsseldorf|München/.test(p)));
    const nulls = rows.filter(r => r.note === null).length;
    assert.ok(nulls > 60 && nulls < 140, `${nulls} nulls of 200`);
});

test('every sample of the repository resolves in every scenario', () => {
    const samples = path.resolve(import.meta.dirname, '../../../../../products/ASC.Files/Core/Services/DocumentBuilderService/ScriptTemplates');
    assert.ok(fs.readdirSync(samples).some(f => f.endsWith('.sample.json')), 'samples found next to the templates');
    for (const file of fs.readdirSync(samples).filter(f => f.endsWith('.sample.json'))) {
        const sample = readSample(path.join(samples, file));
        for (const { name } of sample.scenarios) assert.ok(sample.resolve(name).placeholders, `${file} / ${name}`);
    }
});
