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

module.exports = (app, config) => {
  const logger = require("../log.js");
  const urlResolver = require("../utils/resolver")();
  const coder = require("../utils/coder");
  const converter = require("../utils/converter")();
  const _ = require("lodash");
  const fetch = require("node-fetch");
  const routes = _.values(config.routes);
  const machineKey = config["core"].machinekey
    ? config["core"].machinekey
    : config.get("app").machinekey;

  const fetchWithRetry = async (url, options, maxRetries = 2) => {
    let lastError;
    for (let attempt = 0; attempt <= maxRetries; attempt++) {
      try {
        return await fetch(url, options);
      } catch (error) {
        lastError = error;
        if (attempt < maxRetries) {
          logger.warn(`SSO config fetch attempt ${attempt + 1} failed: ${error.message}, retrying...`);
          await new Promise(resolve => setTimeout(resolve, 300));
        }
      }
    }
    throw lastError;
  };

  const fetchConfig = async (req, res, next) => {
    const foundRoutes =
      req.url && req.url.length > 0
        ? routes.filter(function (route) {
            return 0 === req.url.indexOf(route);
          })
        : [];

    if (req.originalUrl == "/isLife") {
      res.sendStatus(200);
      return;
    }

    if (req.originalUrl == "/health") {
      res.status(200).json({status: "Healthy"});
      return;
    }

    if (!foundRoutes.length) {
      logger.error(`invalid route ${req.originalUrl}`);
      return res.redirect(urlResolver.getPortal404Url(req));
    }

    const noConfigRoutes = [
      config.routes.generatecert,
      config.routes.validatecerts,
      config.routes.uploadmetadata,
      config.routes.loadmetadata,
    ];
    if (noConfigRoutes.includes(req.url)) {
      return next();
    }

    try
    {
        const baseUrl = urlResolver.getBaseUrl(req).originUrl;
        var urls = urlResolver.getPortalSsoConfigUrl(req);

        let headers = { Origin: urls.originUrl }
        const response = await fetchWithRetry(urls.url, { headers });

        if (!response || response.status === 404) {
            if (response) {
                logger.error(response.statusText);
            }
            return res.redirect(urlResolver.getPortal404Url(req));
        } else if (response.status !== 200) {
            throw new Error(`Invalid response status ${response.status}`);
        }
        logger.info(
            `SSO config response: status=${response.status}, content-length=${response.headers.get("content-length")}, transfer-encoding=${response.headers.get("transfer-encoding")}`
        );
        const text = await response.text();
        if (!text) {
            throw new Error("Empty config response");
        }

        const ssoConfig = coder.decodeData(text, machineKey);

        const idp = converter.toIdp(ssoConfig);

        const sp = converter.toSp(ssoConfig, baseUrl);

        const providersInfo = {
            sp: sp,
            idp: idp,
            mapping: ssoConfig.FieldMapping,
            settings: ssoConfig,
        };

        req.providersInfo = providersInfo;

        next();
    } catch (error) {
        logger.error(error);
        return res.redirect(
            urlResolver.getPortalAuthErrorUrl(
                req,
                urlResolver.ErrorMessageKey.SsoError
            )
        );
    }
  };

  app.use(fetchConfig);
};
