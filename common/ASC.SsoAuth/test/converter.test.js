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

const { test, describe } = require("node:test");
const assert = require("node:assert/strict");
const crypto = require("node:crypto");

// The real logger needs the AWS section of the appsettings; the converter only logs template errors.
const logPath = require.resolve("../app/log.js");
const noop = () => {};
require.cache[logPath] = {
    id: logPath,
    filename: logPath,
    loaded: true,
    exports: { error: noop, warn: noop, info: noop, debug: noop },
};

const saml = require("samlify");
const forge = require("node-forge");
const converter = require("../app/utils/converter")();

const urn = saml.Constants;
const portalUrl = "https://portal.example.com";
const idpEntityId = "https://idp.example.com/metadata";

function credentials(commonName) {
    const { publicKey, privateKey } = crypto.generateKeyPairSync("rsa", {
        modulusLength: 2048,
        publicKeyEncoding: { type: "spki", format: "pem" },
        privateKeyEncoding: { type: "pkcs1", format: "pem" },
    });
    const cert = forge.pki.createCertificate();
    cert.publicKey = forge.pki.publicKeyFromPem(publicKey);
    cert.serialNumber = "01";
    cert.validity.notBefore = new Date(Date.now() - 60 * 60 * 1000);
    cert.validity.notAfter = new Date(Date.now() + 24 * 60 * 60 * 1000);
    const attrs = [{ name: "commonName", value: commonName }];
    cert.setSubject(attrs);
    cert.setIssuer(attrs);
    cert.sign(forge.pki.privateKeyFromPem(privateKey), forge.md.sha256.create());
    return { key: privateKey, cert: forge.pki.certificateToPem(cert) };
}

const idpCredentials = credentials("idp.example.com");
const spCredentials = credentials("portal.example.com");

// The SSO settings of a portal as the portal hands them to the service.
function ssoConfig({ encryptAssertions = false } = {}) {
    return {
        IdpSettings: {
            EntityId: idpEntityId,
            NameIdFormat: urn.namespace.format.emailAddress,
            SsoBinding: urn.namespace.binding.post,
            SsoUrl: "https://idp.example.com/sso",
            SloBinding: urn.namespace.binding.redirect,
            SloUrl: "https://idp.example.com/slo",
        },
        IdpCertificates: [{ Action: "verification", Crt: idpCredentials.cert }],
        IdpCertificateAdvanced: {
            VerifyAlgorithm: urn.algorithms.signature.RSA_SHA256,
            // The portal setting says AES; the IdP below still encrypts with TripleDES and RSA 1.5.
            DecryptAlgorithm: urn.algorithms.encryption.data.AES_256,
            VerifyAuthResponsesSign: true,
            VerifyLogoutRequestsSign: false,
            VerifyLogoutResponsesSign: false,
        },
        SpCertificates: [{ Action: "signing and encrypt", Key: spCredentials.key, Crt: spCredentials.cert }],
        SpCertificateAdvanced: {
            SigningAlgorithm: urn.algorithms.signature.RSA_SHA256,
            EncryptAlgorithm: urn.algorithms.encryption.data.AES_256,
            SignAuthRequests: false,
            SignLogoutRequests: false,
            SignLogoutResponses: false,
            EncryptAssertions: encryptAssertions,
        },
    };
}

// Issues a signed login response the way an IdP would, for the SP described by `audience`.
async function loginResponse({ audience, acs, encrypt = false }) {
    const issuer = saml.IdentityProvider({
        entityID: idpEntityId,
        privateKey: idpCredentials.key,
        signingCert: idpCredentials.cert,
        isAssertionEncrypted: encrypt,
        dataEncryptionAlgorithm: urn.algorithms.encryption.data.TRI_DEC,
        keyEncryptionAlgorithm: urn.algorithms.encryption.key.RSA_1_5,
        singleSignOnService: [{ Binding: urn.namespace.binding.post, Location: "https://idp.example.com/sso" }],
    });
    const target = saml.ServiceProvider({
        entityID: audience,
        assertionConsumerService: [{ Binding: urn.namespace.binding.post, Location: acs }],
        encryptCert: spCredentials.cert,
        wantAssertionsSigned: true,
    });
    // The login request the portal sent; a response without InResponseTo fails the schema check.
    const requestInfo = { extract: { request: { id: "_" + crypto.randomUUID() } } };
    const { context } = await issuer.createLoginResponse(target, requestInfo, "post", { email: "user@example.com" });
    return context;
}

async function parseOnPortal(samlResponse, config) {
    const sp = converter.toSp(config, portalUrl);
    const idp = converter.toIdp(config);
    return sp.parseLoginResponse(idp, "post", { body: { SAMLResponse: samlResponse } });
}

// samlify 2.15 started to refuse what 2.10 accepted; the portal keeps the 2.10 behaviour
// for the two checks that can be switched off, so that IdP setups that worked go on working.
describe("login responses accepted as by samlify 2.10", () => {
    test("a response addressed to this portal", async () => {
        const sp = converter.toSp(ssoConfig(), portalUrl);
        const context = await loginResponse({
            audience: sp.entityMeta.getEntityID(),
            acs: `${portalUrl}/sso/acs`,
        });

        const { extract } = await parseOnPortal(context, ssoConfig());

        assert.equal(extract.nameID, "user@example.com");
    });

    test("a response whose Audience, Destination and Recipient name another service provider", async () => {
        const context = await loginResponse({
            audience: "https://other.example.com/sso/metadata",
            acs: "https://other.example.com/sso/acs",
        });

        const { extract } = await parseOnPortal(context, ssoConfig());

        assert.equal(extract.nameID, "user@example.com");
    });

    test("an assertion encrypted with TripleDES and RSA 1.5 while the portal setting says AES", async () => {
        const sp = converter.toSp(ssoConfig(), portalUrl);
        const context = await loginResponse({
            audience: sp.entityMeta.getEntityID(),
            acs: `${portalUrl}/sso/acs`,
            encrypt: true,
        });

        const { extract } = await parseOnPortal(context, ssoConfig({ encryptAssertions: true }));

        assert.equal(extract.nameID, "user@example.com");
    });
});

const certA = "MIIC\n  AAAA\n  BBBB==";
const certB = "MIIDCCCC";

function keyDescriptor(use, cert) {
    return `<md:KeyDescriptor${use ? ` use="${use}"` : ""}><ds:KeyInfo xmlns:ds="http://www.w3.org/2000/09/xmldsig#">` +
        `<ds:X509Data><ds:X509Certificate>${cert}</ds:X509Certificate></ds:X509Data></ds:KeyInfo></md:KeyDescriptor>`;
}

function idpMetadata(keyDescriptors) {
    return `<md:EntityDescriptor xmlns:md="urn:oasis:names:tc:SAML:2.0:metadata" entityID="${idpEntityId}">` +
        `<md:IDPSSODescriptor protocolSupportEnumeration="urn:oasis:names:tc:SAML:2.0:protocol">${keyDescriptors}` +
        `<md:SingleSignOnService Binding="${urn.namespace.binding.redirect}" Location="https://idp.example.com/sso"/>` +
        "</md:IDPSSODescriptor></md:EntityDescriptor>";
}

// The portal settings page reads the certificates of uploaded IdP metadata in the shape samlify 2.10 returned.
// The expected values were produced by samlify 2.10.1 from the same metadata.
describe("IdP metadata response in the samlify 2.10 shape", () => {
    const cases = [
        { name: "no key descriptor", keys: "", certificate: {}, sharedCertificate: null },
        {
            name: "one key descriptor without use",
            keys: keyDescriptor(null, certA),
            certificate: { signing: certA, encryption: certA },
            sharedCertificate: undefined,
        },
        {
            name: "one signing key descriptor",
            keys: keyDescriptor("signing", certA),
            certificate: { signing: certA, encryption: certA },
            sharedCertificate: undefined,
        },
        {
            name: "signing and encryption key descriptors",
            keys: keyDescriptor("signing", certA) + keyDescriptor("encryption", certB),
            certificate: { signing: certA, encryption: certB },
            sharedCertificate: [certA, certB],
        },
        {
            name: "a key descriptor without use and an encryption one",
            keys: keyDescriptor(null, certA) + keyDescriptor("encryption", certB),
            certificate: { encryption: certA },
            sharedCertificate: [certA, certB],
        },
        {
            name: "two key descriptors without use",
            keys: keyDescriptor(null, certA) + keyDescriptor(null, certB),
            certificate: {},
            sharedCertificate: [certA, certB],
        },
        {
            name: "two signing key descriptors",
            keys: keyDescriptor("signing", certA) + keyDescriptor("signing", certB),
            certificate: { signing: [certA, certB] },
            sharedCertificate: [certA, certB],
        },
    ];

    for (const { name, keys, certificate, sharedCertificate } of cases) {
        test(name, () => {
            const idp = saml.IdentityProvider({ metadata: Buffer.from(idpMetadata(keys)) });

            const response = JSON.parse(JSON.stringify(converter.toMetadataResponse(idp.entityMeta)));

            assert.deepEqual(response.meta.certificate, certificate);
            assert.deepEqual(response.meta.sharedCertificate, sharedCertificate);
            assert.equal(response.meta.entityID, idpEntityId);
            assert.ok(response.meta.singleSignOnService);
            assert.ok(response.xmlString);
        });
    }
});
