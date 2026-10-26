// What a spreadsheet looks like, read straight from its XML, and what changed between two of them.
//
// The builder API cannot read alignment or borders back, so a hand edit is found by comparing the
// workbook parts themselves: every cell's value and effective style (font, fill, borders, number format,
// alignment), column widths, row heights, merged areas, frozen panes and tables. Changes come back
// grouped: one line per property and from/to pair, over the smallest set of rectangles that cover it.

import fs from 'node:fs';
import zlib from 'node:zlib';

export class XlsxError extends Error {}

// ---------- the zip container ----------

export function readZip(buffer) {
    const files = new Map();
    let end = buffer.length - 22;
    while (end >= 0 && buffer.readUInt32LE(end) !== 0x06054b50) end--;
    if (end < 0) throw new XlsxError('Not a zip file.');
    const count = buffer.readUInt16LE(end + 10);
    let at = buffer.readUInt32LE(end + 16);
    for (let i = 0; i < count; i++) {
        if (buffer.readUInt32LE(at) !== 0x02014b50) throw new XlsxError('Broken zip directory.');
        const method = buffer.readUInt16LE(at + 10);
        const size = buffer.readUInt32LE(at + 20);
        const nameLength = buffer.readUInt16LE(at + 28);
        const extraLength = buffer.readUInt16LE(at + 30);
        const commentLength = buffer.readUInt16LE(at + 32);
        const local = buffer.readUInt32LE(at + 42);
        const name = buffer.toString('utf8', at + 46, at + 46 + nameLength);
        const dataAt = local + 30 + buffer.readUInt16LE(local + 26) + buffer.readUInt16LE(local + 28);
        const data = buffer.subarray(dataAt, dataAt + size);
        files.set(name, method === 8 ? zlib.inflateRawSync(data) : data);
        at += 46 + nameLength + extraLength + commentLength;
    }
    return files;
}

// ---------- a little XML ----------

const attrs = text => {
    const result = {};
    for (const m of text.matchAll(/([\w:]+)="([^"]*)"/g)) result[m[1]] = unescape(m[2]);
    return result;
};
const unescape = text => text.replace(/&(lt|gt|quot|apos|amp|#x[0-9a-f]+|#\d+);/gi, (_, e) =>
    ({ lt: '<', gt: '>', quot: '"', apos: "'", amp: '&' })[e.toLowerCase()] ??
    String.fromCodePoint(e[1].toLowerCase() === 'x' ? parseInt(e.slice(2), 16) : parseInt(e.slice(1), 10)));

// Elements named `tag` (no prefix) as { attrs, body }; body is '' for a self-closing one.
function elements(xml, tag) {
    const result = [];
    const re = new RegExp(`<${tag}(\\s[^>]*?)?(/>|>([\\s\\S]*?)</${tag}>)`, 'g');
    for (const m of xml.matchAll(re)) result.push({ attrs: attrs(m[1] ?? ''), body: m[3] ?? '' });
    return result;
}
const first = (xml, tag) => elements(xml, tag)[0] ?? null;
const text = body => elements(body, 't').map(t => unescape(t.body)).join('');

// ---------- cell references ----------

export function columnName(index) {
    let name = '';
    for (let n = index + 1; n > 0; n = Math.floor((n - 1) / 26)) name = String.fromCharCode(65 + ((n - 1) % 26)) + name;
    return name;
}
function parseRef(ref) {
    const m = /^([A-Z]+)(\d+)$/.exec(ref);
    let col = 0;
    for (const c of m[1]) col = col * 26 + c.charCodeAt(0) - 64;
    return { row: Number(m[2]) - 1, col: col - 1 };
}
const cellName = (row, col) => `${columnName(col)}${row + 1}`;

// ---------- colors ----------

const indexed = [
    '000000', 'FFFFFF', 'FF0000', '00FF00', '0000FF', 'FFFF00', 'FF00FF', '00FFFF', '000000', 'FFFFFF', 'FF0000', '00FF00', '0000FF', 'FFFF00', 'FF00FF', '00FFFF',
    '800000', '008000', '000080', '808000', '800080', '008080', 'C0C0C0', '808080', '9999FF', '993366', 'FFFFCC', 'CCFFFF', '660066', 'FF8080', '0066CC', 'CCCCFF',
    '000080', 'FF00FF', 'FFFF00', '00FFFF', '800080', '800000', '008080', '0000FF', '00CCFF', 'CCFFFF', 'CCFFCC', 'FFFF99', '99CCFF', 'FF99CC', 'CC99FF', 'FFCC99',
    '3366FF', '33CCCC', '99CC00', 'FFCC00', 'FF9900', 'FF6600', '666699', '969696', '003366', '339966', '003300', '333300', '993300', '993366', '333399', '333333',
    '000000', 'FFFFFF', // 64: system foreground, 65: system background
];

// The theme's colors in the order a `theme="n"` attribute counts them: light 1 and dark 1 swap places.
function themeColors(xml) {
    if (!xml) return [];
    const scheme = /<a:clrScheme[\s\S]*?<\/a:clrScheme>/.exec(xml)?.[0] ?? '';
    const color = name => {
        const part = new RegExp(`<a:${name}>([\\s\\S]*?)</a:${name}>`).exec(scheme)?.[1] ?? '';
        return /(?:srgbClr val|lastClr)="([0-9A-Fa-f]{6})"/.exec(part)?.[1]?.toUpperCase() ?? null;
    };
    return ['lt1', 'dk1', 'lt2', 'dk2', 'accent1', 'accent2', 'accent3', 'accent4', 'accent5', 'accent6', 'hlink', 'folHlink'].map(color);
}

function applyTint(hex, tint) {
    if (!tint) return hex;
    const channel = i => {
        const value = parseInt(hex.slice(i, i + 2), 16);
        const tinted = tint < 0 ? value * (1 + tint) : value + (255 - value) * tint;
        return Math.round(tinted).toString(16).padStart(2, '0');
    };
    return (channel(0) + channel(2) + channel(4)).toUpperCase();
}

function color(a, theme) {
    if (!a) return null;
    let hex = null;
    if (a.rgb) hex = a.rgb.slice(-6).toUpperCase();
    else if (a.theme !== undefined) hex = theme[Number(a.theme)] ?? null;
    else if (a.indexed !== undefined) hex = indexed[Number(a.indexed)] ?? null;
    else if (a.auto === '1') hex = '000000';
    if (!hex) return a.theme !== undefined ? `theme ${a.theme}` : null;
    return '#' + applyTint(hex, Number(a.tint ?? 0));
}

// ---------- styles ----------

const builtinFormats = {
    0: 'General', 1: '0', 2: '0.00', 3: '#,##0', 4: '#,##0.00', 9: '0%', 10: '0.00%', 11: '0.00E+00', 12: '# ?/?', 13: '# ??/??',
    14: 'm/d/yyyy', 15: 'd-mmm-yy', 16: 'd-mmm', 17: 'mmm-yy', 18: 'h:mm AM/PM', 19: 'h:mm:ss AM/PM', 20: 'h:mm', 21: 'h:mm:ss',
    22: 'm/d/yyyy h:mm', 37: '#,##0 ;(#,##0)', 38: '#,##0 ;[Red](#,##0)', 39: '#,##0.00;(#,##0.00)', 40: '#,##0.00;[Red](#,##0.00)',
    45: 'mm:ss', 46: '[h]:mm:ss', 47: 'mmss.0', 48: '##0.0E+0', 49: '@',
};

function readStyles(xml, theme) {
    const formats = { ...builtinFormats };
    for (const f of elements(xml, 'numFmt')) formats[f.attrs.numFmtId] = f.attrs.formatCode;

    const fonts = elements(first(xml, 'fonts')?.body ?? '', 'font').map(({ body }) => {
        const on = tag => {
            const e = first(body, tag);
            return !!e && e.attrs.val !== '0' && e.attrs.val !== 'false' && e.attrs.val !== 'none';
        };
        return {
            name: first(body, 'name')?.attrs.val ?? null,
            size: Number(first(body, 'sz')?.attrs.val ?? 11),
            bold: on('b'),
            italic: on('i'),
            underline: first(body, 'u') ? first(body, 'u').attrs.val ?? 'single' : 'none',
            strike: on('strike'),
            color: color(first(body, 'color')?.attrs, theme) ?? '#000000',
        };
    });

    const fills = elements(first(xml, 'fills')?.body ?? '', 'fill').map(({ body }) => {
        const pattern = first(body, 'patternFill');
        if (pattern) {
            const type = pattern.attrs.patternType ?? 'none';
            if (type === 'none' || type === 'gray125') return 'none';
            const fg = color(first(pattern.body, 'fgColor')?.attrs, theme);
            return type === 'solid' ? fg ?? 'none' : `${type} ${fg ?? ''}`.trim();
        }
        return first(body, 'gradientFill') ? 'gradient' : 'none';
    });

    const borders = elements(first(xml, 'borders')?.body ?? '', 'border').map(({ body }) => {
        const side = name => {
            const e = first(body, name);
            const style = e?.attrs.style ?? 'none';
            if (style === 'none') return 'none';
            return `${style} ${color(first(e.body, 'color')?.attrs, theme) ?? '#000000'}`;
        };
        return { left: side('left'), right: side('right'), top: side('top'), bottom: side('bottom') };
    });

    const xfs = elements(first(xml, 'cellXfs')?.body ?? '', 'xf').map(({ attrs: a, body }) => {
        const align = first(body, 'alignment')?.attrs ?? {};
        return {
            font: fonts[Number(a.fontId ?? 0)] ?? fonts[0],
            fill: fills[Number(a.fillId ?? 0)] ?? 'none',
            border: borders[Number(a.borderId ?? 0)] ?? borders[0],
            numberFormat: formats[a.numFmtId ?? 0] ?? `format ${a.numFmtId}`,
            align: {
                horizontal: align.horizontal ?? 'general',
                vertical: align.vertical ?? 'bottom',
                wrap: align.wrapText === '1' || align.wrapText === 'true',
                indent: Number(align.indent ?? 0),
                rotation: Number(align.textRotation ?? 0),
            },
        };
    });
    return xfs;
}

// The properties of a style as flat "path: value" pairs, the unit a change is reported in.
function flatten(style) {
    const flat = {};
    for (const [k, v] of Object.entries(style.font)) flat[`font.${k}`] = v;
    flat.fill = style.fill;
    for (const [k, v] of Object.entries(style.border)) flat[`border.${k}`] = v;
    flat.numberFormat = style.numberFormat;
    for (const [k, v] of Object.entries(style.align)) flat[`align.${k}`] = v;
    return flat;
}

// ---------- the workbook ----------

const resolveTarget = (base, target) => {
    if (target.startsWith('/')) return target.slice(1);
    const parts = base.split('/').slice(0, -1);
    for (const p of target.split('/')) {
        if (p === '..') parts.pop();
        else if (p !== '.') parts.push(p);
    }
    return parts.join('/');
};
function relationships(files, part) {
    const relsPath = part.replace(/([^/]+)$/, '_rels/$1.rels');
    const xml = files.get(relsPath)?.toString('utf8') ?? '';
    return new Map(elements(xml, 'Relationship').map(r => [r.attrs.Id, resolveTarget(part, r.attrs.Target)]));
}

// { sheets: [{ name, hidden, cells: Map("r,c" -> { value, formula, style }), columns: Map, rows: Map,
//   defaultWidth, defaultHeight, merges: [], frozen, tables: [] }] }
export function readWorkbook(file) {
    const files = readZip(fs.readFileSync(file));
    const get = name => files.get(name)?.toString('utf8') ?? null;
    const workbook = get('xl/workbook.xml');
    if (!workbook) throw new XlsxError(`${file} is not a spreadsheet.`);
    const workbookRels = relationships(files, 'xl/workbook.xml');
    const themePart = [...workbookRels.values()].find(t => /theme\/theme\d*\.xml$/.test(t));
    const theme = themeColors(get(themePart ?? 'xl/theme/theme1.xml'));
    const styles = readStyles(get('xl/styles.xml') ?? '', theme);
    const shared = elements(get('xl/sharedStrings.xml') ?? '', 'si').map(si => text(si.body));

    const sheets = elements(first(workbook, 'sheets')?.body ?? '', 'sheet').map(({ attrs: a }) => {
        const part = workbookRels.get(a['r:id']);
        const xml = get(part) ?? '';
        const sheet = {
            name: a.name,
            hidden: a.state === 'hidden' || a.state === 'veryHidden',
            cells: new Map(),
            columns: new Map(),
            rows: new Map(),
            defaultWidth: Number(first(xml, 'sheetFormatPr')?.attrs.defaultColWidth ?? 8.43),
            defaultHeight: Number(first(xml, 'sheetFormatPr')?.attrs.defaultRowHeight ?? 15),
            merges: elements(xml, 'mergeCell').map(m => m.attrs.ref).sort(),
            frozen: null,
            tables: [],
            // What a cell the sheet does not list looks like.
            defaultStyle: flatten(styles[0]),
        };

        const columnStyle = new Map();
        for (const c of elements(first(xml, 'cols')?.body ?? '', 'col')) {
            for (let i = Number(c.attrs.min) - 1; i < Number(c.attrs.max); i++) {
                sheet.columns.set(i, { width: c.attrs.width !== undefined ? round(Number(c.attrs.width)) : null, hidden: c.attrs.hidden === '1' });
                if (c.attrs.style) columnStyle.set(i, Number(c.attrs.style));
            }
        }

        for (const row of elements(first(xml, 'sheetData')?.body ?? '', 'row')) {
            const r = Number(row.attrs.r) - 1;
            sheet.rows.set(r, { height: row.attrs.customHeight === '1' && row.attrs.ht ? round(Number(row.attrs.ht)) : null, hidden: row.attrs.hidden === '1' });
            const rowStyle = row.attrs.customFormat === '1' ? Number(row.attrs.s ?? 0) : null;
            for (const c of elements(row.body, 'c')) {
                const { row: cr, col } = parseRef(c.attrs.r);
                const raw = first(c.body, 'v')?.body;
                let value = null;
                if (c.attrs.t === 's') value = shared[Number(raw)] ?? '';
                else if (c.attrs.t === 'inlineStr') value = text(first(c.body, 'is')?.body ?? '');
                else if (c.attrs.t === 'b') value = raw === '1';
                else if (c.attrs.t === 'str' || c.attrs.t === 'e') value = raw !== undefined ? unescape(raw) : null;
                else if (raw !== undefined) value = Number(raw);
                const formula = first(c.body, 'f')?.body;
                const styleIndex = c.attrs.s !== undefined ? Number(c.attrs.s) : rowStyle ?? columnStyle.get(col) ?? 0;
                sheet.cells.set(`${cr},${col}`, { value, formula: formula ? unescape(formula) : null, style: flatten(styles[styleIndex] ?? styles[0]) });
            }
        }

        const pane = first(xml, 'pane');
        if (pane && (pane.attrs.state === 'frozen' || pane.attrs.state === 'frozenSplit')) {
            sheet.frozen = pane.attrs.topLeftCell ?? cellName(Number(pane.attrs.ySplit ?? 0), Number(pane.attrs.xSplit ?? 0));
        }

        const sheetRels = relationships(files, part);
        for (const t of elements(first(xml, 'tableParts')?.body ?? '', 'tablePart')) {
            const tableXml = get(sheetRels.get(t.attrs['r:id'])) ?? '';
            const table = first(tableXml, 'table')?.attrs ?? {};
            const info = first(tableXml, 'tableStyleInfo')?.attrs ?? {};
            sheet.tables.push({
                name: table.displayName ?? table.name,
                ref: table.ref,
                style: info.name ?? null,
                rowStripes: info.showRowStripes === '1',
                columnStripes: info.showColumnStripes === '1',
                firstColumn: info.showFirstColumn === '1',
                lastColumn: info.showLastColumn === '1',
                totalsRow: table.totalsRowShown !== '0' && !!table.totalsRowCount,
            });
        }
        return sheet;
    });
    return { sheets };
}

const round = n => Math.round(n * 100) / 100;

// ---------- the difference ----------

// { changes: [{ sheet, kind: 'format' | 'data' | 'layout', property, from, to, cells, count }], format, data, layout }
export function diffWorkbooks(beforeFile, afterFile) {
    const before = readWorkbook(beforeFile);
    const after = readWorkbook(afterFile);
    const changes = [];
    const add = (sheet, kind, property, from, to, cells = null) => changes.push({ sheet, kind, property, from, to, cells, count: cells?.length ?? 1 });

    const names = new Set([...before.sheets.map(s => s.name), ...after.sheets.map(s => s.name)]);
    for (const name of names) {
        const a = before.sheets.find(s => s.name === name);
        const b = after.sheets.find(s => s.name === name);
        if (!a || !b) {
            add(name, 'layout', 'sheet', a ? 'present' : 'absent', b ? 'present' : 'absent');
            continue;
        }
        if (a.hidden !== b.hidden) add(name, 'layout', 'sheet.hidden', a.hidden, b.hidden);

        // Cell properties, grouped by property and from/to.
        const groups = new Map();
        const note = (kind, property, from, to, row, col) => {
            const id = `${kind}\u0000${property}\u0000${JSON.stringify(from)}\u0000${JSON.stringify(to)}`;
            if (!groups.has(id)) groups.set(id, { kind, property, from, to, cells: [] });
            groups.get(id).cells.push([row, col]);
        };
        const blank = { value: null, formula: null, style: null };
        for (const id of new Set([...a.cells.keys(), ...b.cells.keys()])) {
            const [row, col] = id.split(',').map(Number);
            const x = a.cells.get(id) ?? blank;
            const y = b.cells.get(id) ?? blank;
            if (x.formula !== y.formula) note('data', 'formula', x.formula, y.formula, row, col);
            else if (!x.formula && x.value !== y.value && !(isEmpty(x.value) && isEmpty(y.value))) note('data', 'value', x.value, y.value, row, col);
            const sx = x.style ?? a.defaultStyle;
            const sy = y.style ?? b.defaultStyle;
            for (const property of Object.keys(sy)) {
                if (sx[property] !== sy[property]) note('format', property, sx[property], sy[property], row, col);
            }
        }
        for (const g of groups.values()) {
            // Values differ cell by cell; one line per cell would drown the formatting, so data changes
            // keep their cells but are not merged into rectangles of one value.
            // What the first cell of an area holds tells which code writes it: an address alone does
            // not, as the rows a template writes depend on its data.
            const areas = rectangles(g.cells);
            const texts = g.kind === 'data' ? [] : areas.slice(0, 12).map(area => {
                const { row, col } = parseRef(area.split(':')[0]);
                const value = (b.cells.get(`${row},${col}`) ?? a.cells.get(`${row},${col}`))?.value;
                return isEmpty(value) || value === undefined ? null : value;
            });
            changes.push({ sheet: name, kind: g.kind, property: g.property, from: g.from, to: g.to, cells: areas, texts, count: g.cells.length });
        }

        // Column widths and row heights, grouped the same way.
        const sizes = (kind, mapA, mapB, field, fallbackA, fallbackB, label, name_) => {
            const grouped = new Map();
            for (const i of new Set([...mapA.keys(), ...mapB.keys()])) {
                const from = mapA.get(i)?.[field] ?? fallbackA;
                const to = mapB.get(i)?.[field] ?? fallbackB;
                if (from === to) continue;
                const id = `${from}\u0000${to}`;
                if (!grouped.has(id)) grouped.set(id, { from, to, items: [] });
                grouped.get(id).items.push(i);
            }
            for (const g of grouped.values()) {
                add(name, kind, label, g.from, g.to, runs(g.items).map(([s, e]) => (s === e ? name_(s) : `${name_(s)}:${name_(e)}`)));
            }
        };
        sizes('layout', a.columns, b.columns, 'width', a.defaultWidth, b.defaultWidth, 'column.width', columnName);
        sizes('layout', a.columns, b.columns, 'hidden', false, false, 'column.hidden', columnName);
        sizes('layout', a.rows, b.rows, 'height', null, null, 'row.height', i => String(i + 1));
        sizes('layout', a.rows, b.rows, 'hidden', false, false, 'row.hidden', i => String(i + 1));

        const merged = a.merges.filter(m => !b.merges.includes(m));
        const unmerged = b.merges.filter(m => !a.merges.includes(m));
        if (unmerged.length) add(name, 'layout', 'merge', 'separate', 'merged', unmerged);
        if (merged.length) add(name, 'layout', 'merge', 'merged', 'separate', merged);
        if (a.frozen !== b.frozen) add(name, 'layout', 'freezePanes', a.frozen, b.frozen);
        const tableKey = t => JSON.stringify(t);
        if (a.tables.map(tableKey).join() !== b.tables.map(tableKey).join()) {
            for (const t of b.tables) {
                const old = a.tables.find(o => o.name === t.name);
                for (const [k, v] of Object.entries(t)) {
                    if (k !== 'name' && old?.[k] !== v) add(name, 'layout', `table ${t.name}.${k}`, old?.[k] ?? null, v);
                }
            }
            for (const t of a.tables.filter(o => !b.tables.some(n => n.name === o.name))) add(name, 'layout', `table ${t.name}`, 'present', 'absent');
        }
    }

    const order = { layout: 0, format: 1, data: 2 };
    changes.sort((x, y) => order[x.kind] - order[y.kind] || x.sheet.localeCompare(y.sheet) || y.count - x.count);
    const count = kind => changes.filter(c => c.kind === kind).reduce((n, c) => n + c.count, 0);
    return { changes, format: count('format'), data: count('data'), layout: count('layout') };
}

const isEmpty = v => v === null || v === '';

// Cells to the fewest rectangles a column-then-row sweep finds: runs down each column, then neighbouring
// columns with the same runs merged into one rectangle.
export function rectangles(cells) {
    const byColumn = new Map();
    for (const [row, col] of cells) {
        if (!byColumn.has(col)) byColumn.set(col, []);
        byColumn.get(col).push(row);
    }
    const columns = [...byColumn.keys()].sort((x, y) => x - y).map(col => ({ col, runs: runs(byColumn.get(col)) }));
    const result = [];
    let open = [];
    let previous = null;
    for (const { col, runs: r } of columns) {
        const next = [];
        for (const [s, e] of r) {
            const extend = previous === col - 1 && open.find(o => o.s === s && o.e === e);
            if (extend) {
                extend.c2 = col;
                next.push(extend);
            } else {
                const rect = { s, e, c1: col, c2: col };
                result.push(rect);
                next.push(rect);
            }
        }
        open = next;
        previous = col;
    }
    return result.map(r => (r.s === r.e && r.c1 === r.c2 ? cellName(r.s, r.c1) : `${cellName(r.s, r.c1)}:${cellName(r.e, r.c2)}`));
}

function runs(numbers) {
    const sorted = [...new Set(numbers)].sort((x, y) => x - y);
    const result = [];
    for (const n of sorted) {
        const last = result[result.length - 1];
        if (last && last[1] === n - 1) last[1] = n;
        else result.push([n, n]);
    }
    return result;
}

// The difference as text, for a person or a model: the layout and the formatting in full (up to
// `limit` lines), the data changes as a sample of cells with a count.
export function formatDiff(diff, { limit = 150, dataLimit = 20 } = {}) {
    if (!diff.changes.length) return 'No differences.';
    const show = v => (v === null || v === undefined ? '(none)' : typeof v === 'string' ? JSON.stringify(v) : String(v));
    const where = c => {
        if (!c.cells) return '';
        const text = i => {
            const value = c.texts?.[i];
            if (value === null || value === undefined) return '';
            const shown = String(value).length > 40 ? String(value).slice(0, 40) + '…' : String(value);
            return ` (${c.cells[i].includes(':') ? 'first ' : ''}${JSON.stringify(shown)})`;
        };
        const list = c.cells.slice(0, 12).map((area, i) => area + text(i)).join(', ') + (c.cells.length > 12 ? `, … (${c.cells.length} areas)` : '');
        return ` ${list}`;
    };
    const lines = [];
    const formatting = diff.changes.filter(c => c.kind !== 'data');
    for (const c of formatting.slice(0, limit)) lines.push(`[${c.sheet}]${where(c)} ${c.property}: ${show(c.from)} -> ${show(c.to)}`);
    if (formatting.length > limit) lines.push(`… ${formatting.length - limit} more formatting changes.`);
    const data = diff.changes.filter(c => c.kind === 'data');
    if (data.length) {
        lines.push(`Data: ${diff.data} cell(s) hold another value or formula.`);
        for (const c of data.slice(0, dataLimit)) lines.push(`[${c.sheet}]${where(c)} ${c.property}: ${show(c.from)} -> ${show(c.to)}`);
        if (data.length > dataLimit) lines.push(`… ${data.length - dataLimit} more data changes.`);
    }
    return lines.join('\n');
}
