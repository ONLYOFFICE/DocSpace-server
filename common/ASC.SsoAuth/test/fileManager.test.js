// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

"use strict";

const { test, before, after } = require("node:test");
const assert = require("node:assert/strict");
const http = require("node:http");
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");

const { downloadFile } = require("../app/fileManager");

const metadata = "<EntityDescriptor/>";
const tooLarge = 10 * 1024 * 1024 + 1;

let server;
let baseUrl;
let tempDir;
let fileIndex = 0;

function handle(req, res) {
    switch (req.url) {
        case "/metadata":
            res.end(metadata);
            break;
        case "/relative-redirect":
            res.writeHead(302, { Location: "/metadata" });
            res.end();
            break;
        case "/invalid-location":
            res.writeHead(302, { Location: "http://[bad" });
            res.end();
            break;
        case "/loop":
            res.writeHead(302, { Location: "/loop" });
            res.end();
            break;
        case "/ftp-redirect":
            res.writeHead(302, { Location: "ftp://example.com/metadata.xml" });
            res.end();
            break;
        case "/large-declared":
            res.writeHead(200, { "Content-Length": tooLarge });
            res.end();
            break;
        case "/large-chunked": {
            res.writeHead(200);
            const chunk = Buffer.alloc(1024 * 1024, "a");
            let sent = 0;
            const write = () => {
                while (sent < tooLarge) {
                    sent += chunk.length;
                    if (!res.write(chunk)) {
                        res.once("drain", write);
                        return;
                    }
                }
                res.end();
            };
            write();
            break;
        }
        case "/slow": {
            res.writeHead(200, { "Content-Type": "application/xml" });
            const timer = setInterval(() => res.write("<"), 100);
            res.on("close", () => clearInterval(timer));
            break;
        }
        default:
            res.writeHead(404);
            res.end();
    }
}

function nextFile() {
    fileIndex++;
    return path.join(tempDir, `${fileIndex}.xml`);
}

before(async () => {
    tempDir = fs.mkdtempSync(path.join(os.tmpdir(), "sso-download-"));
    server = http.createServer(handle);
    await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
    baseUrl = `http://127.0.0.1:${server.address().port}`;
});

after(async () => {
    server.closeAllConnections();
    await new Promise((resolve) => server.close(resolve));
    fs.rmSync(tempDir, { recursive: true, force: true });
});

test("downloads the file", async () => {
    const filePath = await downloadFile(`${baseUrl}/metadata`, nextFile());
    assert.equal(fs.readFileSync(filePath, "utf8"), metadata);
});

test("follows a relative redirect", async () => {
    const filePath = await downloadFile(`${baseUrl}/relative-redirect`, nextFile());
    assert.equal(fs.readFileSync(filePath, "utf8"), metadata);
});

test("rejects an invalid Location instead of throwing out of the response callback", async () => {
    await assert.rejects(downloadFile(`${baseUrl}/invalid-location`, nextFile()), { code: "ERR_INVALID_URL" });
});

test("stops after five redirects", async () => {
    await assert.rejects(downloadFile(`${baseUrl}/loop`, nextFile()), /Too many redirects/);
});

test("rejects a status other than 200", async () => {
    await assert.rejects(downloadFile(`${baseUrl}/missing`, nextFile()), /status 404/);
});

test("rejects a protocol other than http and https", async () => {
    await assert.rejects(downloadFile("ftp://example.com/metadata.xml", nextFile()), /Unsupported protocol ftp:/);
    await assert.rejects(downloadFile(`${baseUrl}/ftp-redirect`, nextFile()), /Unsupported protocol ftp:/);
});

test("rejects a response that declares more than the size limit", async () => {
    await assert.rejects(downloadFile(`${baseUrl}/large-declared`, nextFile()), /exceeds/);
});

test("rejects a response body larger than the size limit", async () => {
    await assert.rejects(downloadFile(`${baseUrl}/large-chunked`, nextFile()), /exceeds/);
});

test("gives up on a server that keeps the body trickling past the deadline", async () => {
    const started = Date.now();
    await assert.rejects(downloadFile(`${baseUrl}/slow`, nextFile(), { timeout: 1000 }), /timed out/);
    assert.ok(Date.now() - started < 5000, "the deadline covers the whole download, not only idle time");
});
