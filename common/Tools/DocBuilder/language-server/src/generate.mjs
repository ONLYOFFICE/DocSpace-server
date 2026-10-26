// Generated rows for sample data: many realistic rows without writing them out.
//
//   { "$generate": {
//       "count": 5000, "seed": 1, "locale": "en-US",          // locale: en-US | ru-RU
//       "strings": true,                                      // every non-null value as a string (audit rows are string[])
//       "columns": ["ip", "country", { "kind": "date", "from": "2026-09-01", "to": "2026-10-22" }, ...]
//         - or -
//       "fields": { "name": "fileName", "size": { "kind": "int", "min": 0, "max": 900000000 } }
//   } }
//
// "columns" makes each row an array, "fields" an object. A column is a kind name or an object with
// "kind" and its options; any column also takes "nulls" (0..1, the share of null values).
// Kinds: ip, country, city, browser, platform, user, email, date, url, action, actionType, location,
// actionId, targetId, fileName, folderName, word, sentence, longText, int, number, bool, id, pick, const.
// Country and city, and browser and platform, come from one choice per row, so they match.
// The same seed gives the same rows on every run.

class GenerateError extends Error {}

const locales = {
    'en-US': {
        dateTime: d => `${d.getUTCMonth() + 1}/${d.getUTCDate()}/${d.getUTCFullYear()} ${(d.getUTCHours() % 12) || 12}:${pad(d.getUTCMinutes())}:${pad(d.getUTCSeconds())} ${d.getUTCHours() < 12 ? 'AM' : 'PM'}`,
        date: d => `${d.getUTCMonth() + 1}/${d.getUTCDate()}/${d.getUTCFullYear()}`,
        actions: [
            ['File uploaded: {file}', 'Upload'], ['File created: {file}', 'Create'], ['File updated: {file}', 'Update'],
            ['File downloaded: {file}', 'Download'], ['File moved to trash: {file}', 'Delete'], ['File renamed: {file}', 'Update'],
            ['Folder created: {folder}', 'Create'], ['Folder moved: {folder}', 'Move'], ['Files copied to {folder}', 'Copy'],
            ['Room created: {room}', 'Create'], ['Room archived: {room}', 'Update'], ['User invited to room {room}: {user}', 'Send'],
            ['Access rights changed in {room}', 'Update'], ['External link created: {file}', 'Create'], ['Form submitted: {file}', 'Send'],
        ],
        logins: ['Login Success', 'Login Success via Social Account', 'Login Fail Invalid Combination', 'Logout', 'Login Success via SSO', 'Login Fail Disabled Profile'],
        locations: ['Rooms/Finance', 'Rooms/Projekt Nordlicht', 'My documents', 'My documents/Reports', 'Rooms/Legal/Contracts', 'Trash', 'Rooms/HR'],
        words: ['report', 'quarterly', 'draft', 'contract', 'invoice', 'budget', 'meeting', 'notes', 'summary', 'review', 'plan', 'proposal', 'agenda', 'specification'],
    },
    'ru-RU': {
        dateTime: d => `${pad(d.getUTCDate())}.${pad(d.getUTCMonth() + 1)}.${d.getUTCFullYear()} ${d.getUTCHours()}:${pad(d.getUTCMinutes())}:${pad(d.getUTCSeconds())}`,
        date: d => `${pad(d.getUTCDate())}.${pad(d.getUTCMonth() + 1)}.${d.getUTCFullYear()}`,
        actions: [
            ['Файл загружен: {file}', 'Загрузка'], ['Файл создан: {file}', 'Создание'], ['Файл изменен: {file}', 'Изменение'],
            ['Файл скачан: {file}', 'Скачивание'], ['Файл перемещен в корзину: {file}', 'Удаление'], ['Файл переименован: {file}', 'Изменение'],
            ['Папка создана: {folder}', 'Создание'], ['Папка перемещена: {folder}', 'Перемещение'], ['Файлы скопированы в {folder}', 'Копирование'],
            ['Комната создана: {room}', 'Создание'], ['Комната архивирована: {room}', 'Изменение'], ['Пользователь приглашен в комнату {room}: {user}', 'Отправка'],
            ['Изменены права доступа в {room}', 'Изменение'], ['Создана внешняя ссылка: {file}', 'Создание'], ['Форма отправлена: {file}', 'Отправка'],
        ],
        logins: ['Успешный вход', 'Успешный вход через социальную сеть', 'Ошибка входа: неверная комбинация', 'Выход', 'Успешный вход через SSO', 'Ошибка входа: профиль отключен'],
        locations: ['Комнаты/Финансы', 'Комнаты/Проект Северное сияние', 'Мои документы', 'Мои документы/Отчеты', 'Комнаты/Юристы/Договоры', 'Корзина', 'Комнаты/Кадры'],
        words: ['отчет', 'квартальный', 'черновик', 'договор', 'счет', 'бюджет', 'встреча', 'заметки', 'сводка', 'проверка', 'план', 'предложение', 'повестка', 'спецификация'],
    },
};

const places = [
    ['Germany', 'Düsseldorf'], ['Germany', 'München'], ['Russia', 'Moscow'], ['Russia', 'Saint Petersburg'], ['Latvia', 'Riga'],
    ['United States', 'Seattle'], ['United Kingdom', 'London'], ['France', 'Lyon'], ['Türkiye', 'İstanbul'], ['China', 'Shanghai'],
    ['Brazil', 'São Paulo'], ['Japan', 'Tokyo'], [null, null],
];
const clients = [
    ['Chrome 141.0', 'Windows 10'], ['Edge 141.0', 'Windows 10'], ['Firefox 144.0', 'Windows 10'], ['Chrome 141.0', 'Mac OS X 10.15'],
    ['Safari 18.6', 'Mac OS X 10.15'], ['Chrome Mobile 141.0', 'Android 15'], ['Mobile Safari 18.6', 'iOS 18.6'], ['Firefox 144.0', 'Ubuntu'],
    ['YaBrowser 25.8', 'Windows 10'], ['ONLYOFFICE Desktop Editors 9.1', 'Windows 11'],
];
const users = ['Анна Смирнова', 'Jürgen Müller', 'Hans-Peter Großmann', 'Olivia Bennett', 'Li Wei', 'Émilie Laurent', 'Mehmet Yılmaz',
    'Александр Константинопольский-Преображенский', 'O\'Brien, Siobhán', 'José María García', 'Administrator'];
const fileStems = ['Q3 Financial Statement', 'Rahmenvertrag_Entwurf_v4 (Kommentare Legal)', 'Техническое задание', 'Kick-off presentation',
    'Budget forecast Q1–Q4', 'NDA_Contoso', 'Приложение 1. Спецификация оборудования', 'meeting-notes-2026-10-14', 'logo', 'Fragebogen Lieferanten'];
const extensions = ['.docx', '.xlsx', '.pptx', '.pdf', '.md', '.png', '.zip', '.txt'];
const folders = ['01 Verträge', '02 Технические требования', 'Archive 2025', 'Drafts', 'Images & Renderings', 'Входящие'];
const rooms = ['Finance', 'Projekt Nordlicht — Ausschreibung 2027', 'Юридический отдел', 'Marketing', 'HR onboarding'];
const lorem = 'the quick brown fox jumps over the lazy dog while the document builder fills every cell of a long report with text'.split(' ');

const pad = n => String(n).padStart(2, '0');

// mulberry32: small, fast and the same everywhere.
function random(seed) {
    let a = seed >>> 0;
    return () => {
        a = (a + 0x6d2b79f5) >>> 0;
        let t = a;
        t = Math.imul(t ^ (t >>> 15), t | 1);
        t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
        return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
}

export function generate(spec, where = 'sample') {
    if (!spec || typeof spec !== 'object') throw new GenerateError(`${where}: $generate needs an object`);
    const count = spec.count ?? 20;
    if (!Number.isInteger(count) || count < 0 || count > 1_000_000) throw new GenerateError(`${where}: $generate.count must be 0..1000000`);
    const locale = locales[spec.locale ?? 'en-US'];
    if (!locale) throw new GenerateError(`${where}: unknown locale "${spec.locale}"; known: ${Object.keys(locales).join(', ')}`);
    if (!spec.columns === !spec.fields) throw new GenerateError(`${where}: $generate needs either "columns" or "fields"`);

    const next = random(spec.seed ?? 1);
    const int = (min, max) => min + Math.floor(next() * (max - min + 1));
    const pick = list => list[Math.floor(next() * list.length)];

    const columns = spec.columns
        ? spec.columns.map((c, i) => [i, normalize(c, where)])
        : Object.entries(spec.fields).map(([name, c]) => [name, normalize(c, where)]);

    // Dates walk from "to" back to "from" (newest first, as the reports read them) unless asked otherwise.
    const dateState = new Map();

    const rows = [];
    for (let r = 0; r < count; r++) {
        const row = {
            place: pick(places),
            client: pick(clients),
            action: pick(locale.actions),
            file: pick(fileStems) + pick(extensions),
            folder: pick(folders),
            room: pick(rooms),
            user: pick(users),
        };
        const values = spec.columns ? [] : {};
        for (const [key, column] of columns) {
            let value = column.nulls && next() < column.nulls ? null : produce(column, key);
            if (spec.strings && value !== null && value !== undefined && typeof value !== 'string') value = String(value);
            values[key] = value;
        }
        rows.push(values);

        function produce(column, key) {
            switch (column.kind) {
                case 'ip': return next() < 0.1 ? `2a02:6b8:${int(0, 0xffff).toString(16)}::${int(1, 0xffff).toString(16)}` : `${int(5, 223)}.${int(0, 255)}.${int(0, 255)}.${int(1, 254)}`;
                case 'country': return row.place[0];
                case 'city': return row.place[1];
                case 'browser': return row.client[0];
                case 'platform': return row.client[1];
                case 'user': return row.user;
                case 'email': return `${row.user.normalize('NFD').replace(/[^A-Za-z]/g, '').toLowerCase().slice(0, 12) || 'user'}${int(1, 99)}@example.com`;
                case 'date': return date(column, key);
                case 'url': return `https://lumen-partner.onlyoffice.com/rooms/shared/${int(100, 999)}/filter?folder=${int(100, 9999)}&count=100&page=1&sortby=DateAndTime&sortorder=descending`;
                case 'action': return column.login ? pick(locale.logins) : fill(row.action[0]);
                case 'actionType': return row.action[1];
                case 'location': return pick(locale.locations);
                case 'actionId': return int(column.min ?? 1000, column.max ?? 6000);
                case 'targetId': return `${pick(['file', 'file', 'folder', 'room'])}_${int(1, 9999)}`;
                case 'fileName': return row.file;
                case 'folderName': return row.folder;
                case 'word': return pick(locale.words);
                case 'sentence': return sentence(int(4, 12));
                case 'longText': return Array.from({ length: int(column.lines ?? 4, (column.lines ?? 4) + 3) }, () => sentence(int(8, 16))).join('\n');
                case 'int': return int(column.min ?? 0, column.max ?? 1000);
                case 'number': return Math.round(((column.min ?? 0) + next() * ((column.max ?? 1000) - (column.min ?? 0))) * 10 ** (column.digits ?? 2)) / 10 ** (column.digits ?? 2);
                case 'bool': return next() < 0.5;
                case 'id': return (column.start ?? 1) + r;
                case 'pick': return pick(column.values);
                case 'const': return column.value;
                default: throw new GenerateError(`${where}: unknown kind "${column.kind}"`);
            }
        }

        function fill(text) {
            return text.replace('{file}', row.file).replace('{folder}', row.folder).replace('{room}', row.room).replace('{user}', row.user);
        }
    }
    return rows;

    function sentence(words) {
        const text = Array.from({ length: words }, () => pick(next() < 0.5 ? locale.words : lorem)).join(' ');
        return text[0].toUpperCase() + text.slice(1) + '.';
    }

    function date(column, key) {
        const from = Date.parse(column.from ?? '2026-09-01T00:00:00Z');
        const to = Date.parse(column.to ?? '2026-10-22T23:59:59Z');
        let value;
        if (column.order === 'random') {
            value = from + next() * (to - from);
        } else {
            const step = (to - from) / Math.max(count, 1);
            const previous = dateState.get(key) ?? (column.order === 'asc' ? from : to);
            value = column.order === 'asc' ? previous + next() * 2 * step : previous - next() * 2 * step;
            value = Math.min(Math.max(value, from), to);
            dateState.set(key, value);
        }
        const d = new Date(Math.round(value / 1000) * 1000);
        return column.format === 'd' ? locale.date(d) : column.format === 'iso' ? d.toISOString() : locale.dateTime(d);
    }
}

function normalize(column, where) {
    const result = typeof column === 'string' ? { kind: column } : { ...column };
    if (!result.kind) throw new GenerateError(`${where}: a $generate column needs a "kind": ${JSON.stringify(column)}`);
    if (result.kind === 'pick' && !Array.isArray(result.values)) throw new GenerateError(`${where}: kind "pick" needs "values"`);
    return result;
}

export { GenerateError };
