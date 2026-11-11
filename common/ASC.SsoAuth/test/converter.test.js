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

const { test } = require("node:test");
const assert = require("node:assert/strict");

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
const converter = require("../app/utils/converter")();

const urn = saml.Constants;

function ssoConfig(decryptAlgorithm) {
    return {
        IdpSettings: {
            EntityId: "https://idp.example.com/metadata",
            NameIdFormat: urn.namespace.format.emailAddress,
            SsoBinding: urn.namespace.binding.redirect,
            SsoUrl: "https://idp.example.com/sso",
            SloBinding: urn.namespace.binding.redirect,
            SloUrl: "https://idp.example.com/slo",
        },
        IdpCertificates: [],
        IdpCertificateAdvanced: {
            VerifyAlgorithm: urn.algorithms.signature.RSA_SHA256,
            DecryptAlgorithm: decryptAlgorithm,
            VerifyAuthResponsesSign: true,
            VerifyLogoutRequestsSign: true,
            VerifyLogoutResponsesSign: true,
        },
        SpCertificates: [],
        SpCertificateAdvanced: {
            SigningAlgorithm: urn.algorithms.signature.RSA_SHA256,
            EncryptAlgorithm: urn.algorithms.encryption.data.AES_256,
            SignAuthRequests: false,
            SignLogoutRequests: false,
            SignLogoutResponses: false,
            EncryptAssertions: false,
        },
    };
}

// samlify 2.15 started to refuse what 2.10 accepted; the portal keeps the 2.10 behaviour
// so that IdP setups that worked before the update go on working.
for (const decryptAlgorithm of [urn.algorithms.encryption.data.AES_256, urn.algorithms.encryption.data.TRI_DEC]) {
    test(`keeps the samlify 2.10 checks for ${decryptAlgorithm}`, () => {
        const sp = converter.toSp(ssoConfig(decryptAlgorithm), "https://portal.example.com");

        // The encryption algorithm is read from the incoming assertion, not from the portal setting.
        assert.equal(sp.entitySetting.allowInsecureDecryptionAlgorithms, true);
        assert.equal(sp.entitySetting.wantAudienceCheck, false);
    });
}
