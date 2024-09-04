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

const config = require("../../config").get(),
    logger = require("../log.js");
    URL = require("url");

// ReSharper disable once InconsistentNaming
module.exports = function () {

    function getBaseUrl(req) {
        const proto = req.headers['x-forwarded-proto']?.split(',').shift();
        const host = req.headers['x-forwarded-host']?.split(',').shift();

        return `${proto}://${host}`;
    }

    function getPortalSsoHandlerUrl(req) {
        const url = getBaseUrl(req) + config.app.portal.ssoUrl;
        return url;
    }

    function getPortalSsoConfigUrl(req) {
        const url = getPortalSsoHandlerUrl(req) +
            "?config=saml";
        logger.debug("getPortalSsoConfigUrl: " + url);
        return url;
    }

    function getPortalSsoLoginUrl(req, data) {
        const url = getPortalSsoHandlerUrl(req) + "?auth=true&data=" + data;
        logger.debug("getPortalSsoLoginUrl: " + url);
        return url;
    }

    function getPortalSsoLogoutUrl(req, data) {
        const url = getPortalSsoHandlerUrl(req) + "?logout=true&data=" + data;
        logger.debug("getPortalSsoLogoutUrl: " + url);
        return url;
    }

    function getPortalAuthUrl(req) {
        const url = getBaseUrl(req) + config.app.portal.authUrl;
        logger.debug("getPortalAuthUrl: " + url);
        return url;
    }

    const ErrorMessageKey = {
        Error: 1,
        SsoError: 17,
        SsoAuthFailed: 18,
        SsoAttributesNotFound: 19,
    };

    function getPortalAuthErrorUrl(req, errorKey) {
        const url = getBaseUrl(req) + "/login/error?messageKey=" + errorKey;
        logger.debug("getPortalAuthErrorUrl: " + url);
        return url;
    }

    function getPortalErrorUrl(req) {
        const url = getBaseUrl(req) + "/login/error?messageKey=" + ErrorMessageKey.Error;
        logger.debug("getPortal500Url: " + url);
        return url;
    }

    function getPortal404Url(req) {
        const url = getBaseUrl(req) + "/login/error?messageKey=" + ErrorMessageKey.SsoError;
        logger.debug("getPortal404Url: " + url);
        return url;
    }

    return {
        getBaseUrl: getBaseUrl,

        getPortalSsoHandlerUrl: getPortalSsoHandlerUrl,

        getPortalSsoConfigUrl: getPortalSsoConfigUrl,

        getPortalSsoLoginUrl: getPortalSsoLoginUrl,

        getPortalSsoLogoutUrl: getPortalSsoLogoutUrl,

        getPortalAuthUrl: getPortalAuthUrl,

        ErrorMessageKey: ErrorMessageKey,

        getPortalAuthErrorUrl: getPortalAuthErrorUrl,

        getPortal500Url: getPortalErrorUrl,

        getPortal404Url: getPortal404Url
    };
};
