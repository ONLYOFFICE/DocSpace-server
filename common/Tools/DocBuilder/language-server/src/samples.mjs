// Sample data a preview runs a template with: <Template>.sample.json and the shared files it extends.
//
//   {
//     "description": "...",
//     "extends": ["_shared/header.en-US"],         // merged first, paths relative to this file
//     "default": "audit",
//     "scenarios": {
//       "audit":    { "description": "...", "placeholders": { "inputData": {...}, "dataValues": [...] } },
//       "rowLimit": { "base": "audit", "placeholders": { "inputData": { "info": { "rowLimit": "..." } } } },
//       "ru-RU":    { "base": "audit", "extends": ["_shared/header.ru-RU"], "placeholders": {...} }
//     }
//   }
//
// A scenario is a deep merge: the file's extends, then its base scenario, then its own extends, then
// its placeholders. Objects merge key by key; arrays and plain values replace. A shared file holds
// { "placeholders": {...} } and may extend others. A file without "scenarios" - the first format -
// is one scenario, "default", made of its "placeholders".
//
// Anywhere in the result, { "$generate": {...} } stands for generated rows (generate.mjs).

import fs from 'node:fs';
import path from 'node:path';
import { generate } from './generate.mjs';

export class SampleError extends Error {}

export function readSample(file) {
    const sample = readJson(file);
    const scenarios = sample.scenarios ?? { default: { placeholders: sample.placeholders ?? {} } };
    const names = Object.keys(scenarios);
    if (!names.length) throw new SampleError(`${file}: no scenarios`);
    const fallback = sample.default && scenarios[sample.default] ? sample.default : names[0];
    return {
        file,
        description: sample.description ?? '',
        default: fallback,
        scenarios: names.map(name => ({ name, description: scenarios[name].description ?? '' })),
        resolve: name => resolveScenario(file, sample, scenarios, name ?? fallback),
    };
}

function resolveScenario(file, sample, scenarios, name) {
    if (!scenarios[name]) throw new SampleError(`${file}: no scenario "${name}"; there are ${Object.keys(scenarios).join(', ')}`);
    const folder = path.dirname(file);

    const scenarioPlaceholders = (current, seen) => {
        if (seen.includes(current)) throw new SampleError(`${file}: scenarios base each other in a loop: ${[...seen, current].join(' -> ')}`);
        const scenario = scenarios[current];
        if (!scenario) throw new SampleError(`${file}: scenario "${seen.at(-1)}" has an unknown base "${current}"`);
        let result = scenario.base ? scenarioPlaceholders(scenario.base, [...seen, current]) : {};
        for (const shared of scenario.extends ?? []) result = merge(result, sharedPlaceholders(folder, shared, [file]));
        return merge(result, scenario.placeholders ?? {});
    };

    let placeholders = {};
    for (const shared of sample.extends ?? []) placeholders = merge(placeholders, sharedPlaceholders(folder, shared, [file]));
    placeholders = merge(placeholders, scenarioPlaceholders(name, []));
    return { scenario: name, placeholders: expand(placeholders, `${path.basename(file)} / ${name}`) };
}

function sharedPlaceholders(folder, reference, chain) {
    const file = path.resolve(folder, reference.endsWith('.json') ? reference : `${reference}.json`);
    if (chain.includes(file)) throw new SampleError(`shared samples extend each other in a loop: ${[...chain, file].join(' -> ')}`);
    const shared = readJson(file);
    let result = {};
    for (const next of shared.extends ?? []) result = merge(result, sharedPlaceholders(path.dirname(file), next, [...chain, file]));
    return merge(result, shared.placeholders ?? {});
}

function readJson(file) {
    let text;
    try {
        text = fs.readFileSync(file, 'utf8');
    } catch {
        throw new SampleError(`${file}: not found`);
    }
    try {
        return JSON.parse(text.replace(/^﻿/, ''));
    } catch (e) {
        throw new SampleError(`${file}: ${e.message}`);
    }
}

const isObject = v => v !== null && typeof v === 'object' && !Array.isArray(v);

export function merge(target, source) {
    if (!isObject(target) || !isObject(source) || '$generate' in source) return structuredClone(source);
    const result = { ...target };
    for (const [key, value] of Object.entries(source)) result[key] = key in target ? merge(target[key], value) : structuredClone(value);
    return result;
}

function expand(value, where) {
    if (Array.isArray(value)) return value.map(v => expand(v, where));
    if (!isObject(value)) return value;
    if ('$generate' in value) return generate(value.$generate, where);
    return Object.fromEntries(Object.entries(value).map(([k, v]) => [k, expand(v, where)]));
}
