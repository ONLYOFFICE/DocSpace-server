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

// The real logger needs the AWS section of the appsettings.
const logPath = require.resolve("../app/log.js");
const noop = () => {};
require.cache[logPath] = {
    id: logPath,
    filename: logPath,
    loaded: true,
    exports: { error: noop, warn: noop, info: noop, debug: noop },
};

const express = require("express");
const baseConfig = require("../config").get();

const config = { ...baseConfig, core: baseConfig.core || { machinekey: "test" } };

const signingCert = "MIIC\n  AAAA\n  BBBB==";
const encryptionCert = "MIIDCCCC";
const metadata = '<?xml version="1.0"?>' +
    '<md:EntityDescriptor xmlns:md="urn:oasis:names:tc:SAML:2.0:metadata" entityID="https://idp.example.com/metadata">' +
    '<md:IDPSSODescriptor protocolSupportEnumeration="urn:oasis:names:tc:SAML:2.0:protocol">' +
    `<md:KeyDescriptor use="signing"><ds:KeyInfo xmlns:ds="http://www.w3.org/2000/09/xmldsig#"><ds:X509Data><ds:X509Certificate>${signingCert}</ds:X509Certificate></ds:X509Data></ds:KeyInfo></md:KeyDescriptor>` +
    `<md:KeyDescriptor use="encryption"><ds:KeyInfo xmlns:ds="http://www.w3.org/2000/09/xmldsig#"><ds:X509Data><ds:X509Certificate>${encryptionCert}</ds:X509Certificate></ds:X509Data></ds:KeyInfo></md:KeyDescriptor>` +
    '<md:SingleSignOnService Binding="urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Redirect" Location="https://idp.example.com/sso"/>' +
    "</md:IDPSSODescriptor></md:EntityDescriptor>";
// What a Windows editor saves: a byte order mark and a line break before the XML declaration.
const metadataWithBom = "\uFEFF\r\n" + metadata;

let appServer;
let appUrl;
let idpServer;
let idpUrl;

before(async () => {
    const app = express();
    app.use(express.json());
    require("../app/routes")(app, config);
    appServer = http.createServer(app);
    await new Promise((resolve) => appServer.listen(0, "127.0.0.1", resolve));
    appUrl = `http://127.0.0.1:${appServer.address().port}`;

    idpServer = http.createServer((req, res) => {
        res.setHeader("Content-Type", "application/xml");
        res.end(req.url === "/bom.xml" ? metadataWithBom : req.url === "/broken.xml" ? "<md:EntityDescriptor" : metadata);
    });
    await new Promise((resolve) => idpServer.listen(0, "127.0.0.1", resolve));
    idpUrl = `http://127.0.0.1:${idpServer.address().port}`;
});

after(async () => {
    for (const server of [appServer, idpServer]) {
        server.closeAllConnections();
        await new Promise((resolve) => server.close(resolve));
    }
});

function uploadMetadata(content) {
    const body = new FormData();
    body.append("metadata", new Blob([content], { type: "application/xml" }), "metadata.xml");
    return fetch(appUrl + config.routes.uploadmetadata, { method: "POST", body });
}

function loadMetadata(url) {
    return fetch(appUrl + config.routes.loadmetadata, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ url }),
    });
}

// The portal settings page reads the certificates in the shape samlify 2.10 returned them.
async function assertLegacyShape(response) {
    assert.equal(response.status, 200);
    const { meta } = await response.json();
    assert.equal(meta.entityID, "https://idp.example.com/metadata");
    assert.deepEqual(meta.certificate, { signing: signingCert, encryption: encryptionCert });
    assert.deepEqual(meta.sharedCertificate, [signingCert, encryptionCert]);
}

test("an uploaded metadata file is answered in the samlify 2.10 shape", async () => {
    await assertLegacyShape(await uploadMetadata(metadata));
});

test("an uploaded metadata file with a byte order mark is read", async () => {
    await assertLegacyShape(await uploadMetadata(metadataWithBom));
});

test("metadata loaded by URL is answered in the samlify 2.10 shape", async () => {
    await assertLegacyShape(await loadMetadata(`${idpUrl}/metadata.xml`));
});

test("metadata with a byte order mark loaded by URL is read", async () => {
    await assertLegacyShape(await loadMetadata(`${idpUrl}/bom.xml`));
});

test("metadata that cannot be parsed, loaded by URL, is reported as invalid metadata", async () => {
    const response = await loadMetadata(`${idpUrl}/broken.xml`);
    assert.equal(response.status, 500);
    assert.equal(await response.text(), "Invalid metadata xml file");
});

test("an upload that declares a body larger than the limit is refused before it is read", async () => {
    // Only the headers are sent: the answer must not wait for a body that never comes.
    const status = await new Promise((resolve, reject) => {
        const req = http.request(appUrl + config.routes.uploadmetadata, {
            method: "POST",
            headers: {
                "Content-Type": "multipart/form-data; boundary=x",
                "Content-Length": String(50 * 1024 * 1024),
            },
        }, (res) => {
            res.resume();
            resolve(res.statusCode);
            req.destroy();
        });
        req.on("error", reject);
        req.flushHeaders();
    });
    assert.equal(status, 413);
});
