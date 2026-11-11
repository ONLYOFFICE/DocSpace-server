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

const config = require("../../config").get();
const saml = require("samlify");
const validator = require("@authenio/samlify-node-xmllint");
const urn = saml.Constants;
const _ = require("lodash");
const fs = require("fs");
const path = require("path");
const minifyXML = require("minify-xml").minify;
const logger = require("../log.js");

saml.setSchemaValidator(validator);

const ServiceProvider = saml.ServiceProvider;
const IdentityProvider = saml.IdentityProvider;

const templDir = path.join(process.cwd(), "/app/templates/");

// The certificate fields of IdP metadata the way samlify 2.10 extracted them; 2.15 drops
// sharedCertificate and fills certificate differently, and the portal reads the 2.10 shape.
const legacyCertificateFields = [
  {
    key: "sharedCertificate",
    localPath: ["EntityDescriptor", "~SSODescriptor", "KeyDescriptor", "KeyInfo", "X509Data", "X509Certificate"],
    attributes: [],
  },
  {
    key: "certificate",
    localPath: ["EntityDescriptor", "~SSODescriptor", "KeyDescriptor"],
    index: ["use"],
    attributePath: ["KeyInfo", "X509Data", "X509Certificate"],
    attributes: [],
  },
];

module.exports = function () {
  function removeCertHead(cert) {
    var newCert = cert;
    if (cert && cert[0] === "-") {
      newCert = cert.replace(/-----.*-----/g, "");
    }
    return newCert;
  }

  function loadTemplate(conf, templName) {
    try {
      const tmplPath = path.join(templDir, `${templName}.xml`);

      const template = minifyXML(
        fs.readFileSync(tmplPath, { encoding: "utf-8" })
      );

      conf[`${templName}`] = {
        context: template,
      };
    } catch (e) {
      logger.error(`loadTemplate ${e.message}`);
    }
  }

  function toMetadataResponse(entityMeta) {
    const { sharedCertificate, certificate } = saml.Extractor.extract(
      entityMeta.xmlString,
      legacyCertificateFields
    );
    const meta = { ...entityMeta.meta };
    delete meta.sharedCertificate;

    if (typeof sharedCertificate === "string") {
      meta.certificate = { signing: sharedCertificate, encryption: sharedCertificate };
    } else {
      meta.sharedCertificate = sharedCertificate;
      meta.certificate = certificate;
    }

    return { xmlString: entityMeta.xmlString, meta };
  }

  // Reads an IdP metadata file into the response the portal settings page expects.
  // The XML parser of samlify 2.15 refuses a byte order mark or whitespace before the
  // XML declaration, which 2.10 accepted and which files saved by Windows editors often have.
  function readIdpMetadata(content) {
    const xml = content.toString("utf8").replace(/^\uFEFF?\s*/, "");
    const idp = IdentityProvider({ metadata: xml });
    return toMetadataResponse(idp.entityMeta);
  }

  return {
    readIdpMetadata: readIdpMetadata,

    toMetadataResponse: toMetadataResponse,

    toIdp: function (ssoConfig) {
      if (!ssoConfig && typeof ssoConfig !== "string") return undefined;

      const idpSetting = {
        entityID: ssoConfig.IdpSettings.EntityId,
        nameIDFormat: [ssoConfig.IdpSettings.NameIdFormat],

        requestSignatureAlgorithm:
          ssoConfig.IdpCertificateAdvanced.VerifyAlgorithm,
        dataEncryptionAlgorithm:
          ssoConfig.IdpCertificateAdvanced.DecryptAlgorithm,

        singleSignOnService: [
          {
            Binding: ssoConfig.IdpSettings.SsoBinding,
            Location: ssoConfig.IdpSettings.SsoUrl,
          },
        ],
        singleLogoutService: [
          {
            Binding: ssoConfig.IdpSettings.SloBinding,
            Location: ssoConfig.IdpSettings.SloUrl,
          },
        ],
        wantAuthnRequestsSigned:
          ssoConfig.SpCertificateAdvanced.SignAuthRequests,
        wantLogoutResponseSigned:
          ssoConfig.SpCertificateAdvanced.SignLogoutResponses,
        wantLogoutRequestSigned:
          ssoConfig.SpCertificateAdvanced.SignLogoutRequests,
        isAssertionEncrypted: ssoConfig.SpCertificateAdvanced.EncryptAssertions,
      };

      if (
        Array.isArray(ssoConfig.IdpCertificates) &&
        ssoConfig.IdpCertificates.length > 0
      ) {
        idpSetting.signingCert = removeCertHead(
          _.result(
            _.find(ssoConfig.IdpCertificates, function (obj) {
              return (
                obj.Action === "verification" ||
                obj.Action === "verification and decrypt"
              );
            }),
            "Crt"
          )
        );

        idpSetting.encryptCert = removeCertHead(
          _.result(
            _.find(ssoConfig.IdpCertificates, function (obj) {
              return (
                obj.Action === "decrypt" ||
                obj.Action === "verification and decrypt"
              );
            }),
            "Crt"
          )
        );
      }

      const idp = new IdentityProvider(idpSetting);

      return idp;
    },

    toSp: function (ssoConfig, baseUrl) {
      if (!ssoConfig && typeof ssoConfig !== "string")
        throw "Invalid ssoConfig";

      const metaUrl = baseUrl + "/sso" + config.routes.metadata,
        acsUrl = baseUrl + "/sso" + config.routes.login_callback,
        sloUrl = baseUrl + "/sso" + config.routes.logout_callback;

      const spSetting = {
        entityID: metaUrl,

        nameIDFormat: [ssoConfig.IdpSettings.NameIdFormat],

        requestSignatureAlgorithm:
          ssoConfig.SpCertificateAdvanced.SigningAlgorithm,
        dataEncryptionAlgorithm:
          ssoConfig.SpCertificateAdvanced.EncryptAlgorithm,

        // Two of the checks samlify 2.15 added are switched off, as 2.10 did not have them,
        // so that IdP setups that worked before go on working. Other new checks stay on and have
        // no switch, for example: a bearer SubjectConfirmation with an expiry, InResponseTo,
        // SessionNotOnOrAfter, the issuer and expiry of a LogoutRequest, the message type,
        // the signed query string of the Redirect binding, and well-formed XML.
        //
        // The encryption algorithm is taken from the incoming assertion, not from the portal setting,
        // so TripleDES and RSA 1.5 are allowed for every portal; samlify warns about each to stderr.
        allowInsecureDecryptionAlgorithms: true,
        // Audience, Destination and Recipient are not matched against this SP.
        wantAudienceCheck: false,

        assertionConsumerService: [
          {
            Binding: urn.namespace.binding.post,
            Location: acsUrl,
          },
          {
            Binding: urn.namespace.binding.redirect,
            Location: acsUrl,
          },
        ],

        singleLogoutService: [
          {
            Binding: urn.namespace.binding.post,
            Location: sloUrl,
          },
          {
            Binding: urn.namespace.binding.redirect,
            Location: sloUrl,
          },
        ],

        /*requestedAttributes: [
                    {
                        FriendlyName: "mail",
                        Name: "urn:oid:0.9.2342.19200300.100.1.3",
                        NameFormat: "urn:oasis:names:tc:SAML:2.0:attrname-format:uri"
                    },
                    {
                        FriendlyName: "givenName",
                        Name: "urn:oid:2.5.4.42",
                        NameFormat: "urn:oasis:names:tc:SAML:2.0:attrname-format:uri"
                    },
                    {
                        FriendlyName: "sn",
                        Name: "urn:oid:2.5.4.4",
                        NameFormat: "urn:oasis:names:tc:SAML:2.0:attrname-format:uri"
                    },
                    {
                        FriendlyName: "mobile",
                        Name: "urn:oid:0.9.2342.19200300.100.1.41",
                        NameFormat: "urn:oasis:names:tc:SAML:2.0:attrname-format:uri"
                    },
                    {
                        FriendlyName: "title",
                        Name: "urn:oid:2.5.4.12",
                        NameFormat: "urn:oasis:names:tc:SAML:2.0:attrname-format:uri"
                    },
                    {
                        FriendlyName: "l",
                        Name: "urn:oid:2.5.4.7",
                        NameFormat: "urn:oasis:names:tc:SAML:2.0:attrname-format:uri"
                    }
                ],*/
        authnRequestsSigned: ssoConfig.SpCertificateAdvanced.SignAuthRequests,
        wantAssertionsSigned:
          ssoConfig.IdpCertificateAdvanced.VerifyAuthResponsesSign,
        wantLogoutResponseSigned:
          ssoConfig.IdpCertificateAdvanced.VerifyLogoutResponsesSign,
        wantLogoutRequestSigned:
          ssoConfig.IdpCertificateAdvanced.VerifyLogoutRequestsSign,
        elementsOrder: [
          "KeyDescriptor",
          "SingleLogoutService",
          "NameIDFormat",
          "AssertionConsumerService",
          "AttributeConsumingService",
        ],
        //clockDrifts: [-60000, 60000],
      };

      if (
        Array.isArray(ssoConfig.SpCertificates) &&
        ssoConfig.SpCertificates.length > 0
      ) {
        spSetting.privateKey = _.result(
          _.find(ssoConfig.SpCertificates, function (obj) {
            return (
              obj.Action === "signing" || obj.Action === "signing and encrypt"
            );
          }),
          "Key"
        );

        spSetting.privateKeyPass = "";

        spSetting.encPrivateKey = _.result(
          _.find(ssoConfig.SpCertificates, function (obj) {
            return (
              obj.Action === "encrypt" || obj.Action === "signing and encrypt"
            );
          }),
          "Key"
        );

        spSetting.encPrivateKeyPass = "";

        spSetting.signingCert = _.result(
          _.find(ssoConfig.SpCertificates, function (obj) {
            return (
              obj.Action === "signing" || obj.Action === "signing and encrypt"
            );
          }),
          "Crt"
        );
        spSetting.encryptCert = _.result(
          _.find(ssoConfig.SpCertificates, function (obj) {
            return (
              obj.Action === "encrypt" || obj.Action === "signing and encrypt"
            );
          }),
          "Crt"
        );

        // must have if assertion signature fails validation
        spSetting.transformationAlgorithms = [
          "http://www.w3.org/2000/09/xmldsig#enveloped-signature",
          "http://www.w3.org/2001/10/xml-exc-c14n#",
        ];
      }

      loadTemplate(spSetting, "loginRequestTemplate");
      loadTemplate(spSetting, "logoutRequestTemplate");
      loadTemplate(spSetting, "logoutResponseTemplate");

      //if (config.app.organization) {
      //    spSetting.organization = config.app.organization;
      //}

      //if (config.app.contact) {
      //    spSetting.contact = config.app.contact;
      //}

      const sp = new ServiceProvider(spSetting);

      return sp;
    },
  };
};
