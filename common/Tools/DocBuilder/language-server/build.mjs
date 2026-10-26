// Bundles the language server into dist/ for the IDE plugins: one .cjs file, the Document Builder
// declarations and the TypeScript standard library declarations the language service loads at run time.
//
//   node build.mjs

import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawn } from 'node:child_process';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
import { build } from 'esbuild';

const here = path.dirname(fileURLToPath(import.meta.url));
const dist = path.join(here, 'dist');
const require = createRequire(import.meta.url);

fs.rmSync(dist, { recursive: true, force: true });
fs.mkdirSync(dist, { recursive: true });

await build({
    entryPoints: [path.join(here, 'src', 'server.mjs')],
    outfile: path.join(dist, 'docbuilder-language-server.cjs'),
    bundle: true,
    platform: 'node',
    target: 'node18',
    format: 'cjs',
    // The sources find their neighbours through import.meta.url, which a CommonJS bundle does not have.
    define: { 'import.meta.url': '__importMetaUrl' },
    banner: { js: "const __importMetaUrl = require('url').pathToFileURL(__filename).href;" },
    logLevel: 'warning',
});

fs.copyFileSync(path.join(here, '..', 'typings', 'onlyoffice-docbuilder.d.ts'), path.join(dist, 'onlyoffice-docbuilder.d.ts'));

const tsLib = path.dirname(require.resolve('typescript/lib/typescript.js'));
for (const f of fs.readdirSync(tsLib).filter(f => /^lib\..*\.d\.ts$/.test(f))) fs.copyFileSync(path.join(tsLib, f), path.join(dist, f));

const size = fs.readdirSync(dist).reduce((sum, f) => sum + fs.statSync(path.join(dist, f)).size, 0);
console.log(`dist: ${fs.readdirSync(dist).length} files, ${(size / 1024 / 1024).toFixed(1)} MB`);

// The plugins install dist/ far from any node_modules: start a copy of it from an empty folder and
// make sure it answers initialize, or a package left out of the bundle only shows up in the IDE.
const isolated = fs.mkdtempSync(path.join(os.tmpdir(), 'docbuilder-dist-'));
try {
    fs.cpSync(dist, isolated, { recursive: true });
    const answer = await initialize(path.join(isolated, 'docbuilder-language-server.cjs'));
    if (!answer.capabilities?.completionProvider) throw new Error(`unexpected answer: ${JSON.stringify(answer)}`);
    console.log('dist: starts on its own and answers initialize');
} finally {
    fs.rmSync(isolated, { recursive: true, force: true });
}

function initialize(server) {
    return new Promise((resolve, reject) => {
        const child = spawn(process.execPath, [server, '--stdio'], { cwd: os.tmpdir() });
        let out = '';
        let err = '';
        const timer = setTimeout(() => finish(new Error('no answer to initialize in 20 s')), 20000);
        const finish = (error, value) => {
            clearTimeout(timer);
            child.kill();
            error ? reject(error) : resolve(value);
        };
        child.stderr.on('data', d => (err += d));
        child.on('exit', code => finish(new Error(`the bundle exited with ${code}:\n${err}`)));
        child.stdout.on('data', d => {
            out += d;
            const body = out.split('\r\n\r\n')[1];
            if (body && Buffer.byteLength(body) >= Number(out.match(/Content-Length: (\d+)/)?.[1])) finish(null, JSON.parse(body).result);
        });
        const message = JSON.stringify({ jsonrpc: '2.0', id: 1, method: 'initialize', params: { processId: null, rootUri: null, capabilities: {} } });
        child.stdin.write(`Content-Length: ${Buffer.byteLength(message)}\r\n\r\n${message}`);
    });
}
