// (c) Copyright Ascensio System SIA 2009-2024
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

"use strict";

const config = require("../../config").get();
const saml = require("samlify");
const validator = require("@authenio/samlify-node-xmllint");
const urn = require("samlify/build/src/urn");
const _ = require("lodash");
const fs = require("fs");
const path = require("path");
const minifyXML = require("minify-xml").minify;
const logger = require("../log.js");

saml.setSchemaValidator(validator);

const ServiceProvider = saml.ServiceProvider;
const IdentityProvider = saml.IdentityProvider;

const templDir = path.join(process.cwd(), "/app/templates/");

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

  return {
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
