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

const config = require("../../config"),
    logger = require("../log.js");
    URL = require("url");

// ReSharper disable once InconsistentNaming
module.exports = function () {

    function getBaseUrl(req) {
        const proto = req.headers['x-forwarded-proto']?.split(',').shift();
        const host = req.headers['x-forwarded-host']?.split(',').shift();
        const originUrl = `${proto}://${host}`;
        const baseUrl = config.get("API_HOST")?.replace(/\/$/g, "") || originUrl;

        return { baseUrl, originUrl };
    }

    function getPortalSsoHandlerUrl(req) {
        var urls = getBaseUrl(req);
        const url = urls.baseUrl + config.get("app").portal.ssoUrl;
        return { url, originUrl: urls.originUrl };
    }

    function getPortalSsoConfigUrl(req) {
        var urls = getPortalSsoHandlerUrl(req);
        const url = urls.url + "?config=saml";
        logger.debug("getPortalSsoConfigUrl: " + url);
        return { url, originUrl: urls.originUrl }
    }

    function getPortalSsoLoginUrl(req, data) {
        const url = getBaseUrl(req).originUrl + config.get("app").portal.ssoUrl + "?auth=true&data=" + data;
        logger.debug("getPortalSsoLoginUrl: " + url);
        return url;
    }

    function getPortalSsoLogoutUrl(req, data) {
        var urls = getPortalSsoHandlerUrl(req);
        const url = urls.url + "?logout=true&data=" + data;
        logger.debug("getPortalSsoLogoutUrl: " + url);
        return { url, originUrl: urls.originUrl }
    }

    function getPortalAuthUrl(req) {
        const url = getBaseUrl(req).originUrl + config.get("app").portal.authUrl;
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
        const url = getBaseUrl(req).originUrl + "/login/error?messageKey=" + errorKey;
        logger.debug("getPortalAuthErrorUrl: " + url);
        return url;
    }

    function getPortalErrorUrl(req) {
        const url = getBaseUrl(req).originUrl + "/login/error?messageKey=" + ErrorMessageKey.Error;
        logger.debug("getPortal500Url: " + url);
        return url;
    }

    function getPortal404Url(req) {
        const url = getBaseUrl(req).originUrl + "/login/error?messageKey=" + ErrorMessageKey.SsoError;
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
