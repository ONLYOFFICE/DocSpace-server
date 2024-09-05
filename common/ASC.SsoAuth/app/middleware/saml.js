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
    : config.app.machinekey;

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

    if (!foundRoutes.length) {
      logger.error(`invalid route ${req.originalUrl}`);
      return res.redirect(urlResolver.getPortal404Url(req));
    }

    const baseUrl = urlResolver.getBaseUrl(req);

    const promise = new Promise(async (resolve) => {
      var url = urlResolver.getPortalSsoConfigUrl(req);

      const response = await fetch(url);

      if (!response || response.status === 404) {
        if (response) {
          logger.error(response.statusText);
        }
        return resolve(res.redirect(urlResolver.getPortal404Url(req)));
      } else if (response.status !== 200) {
        throw new Error(`Invalid response status ${response.status}`);
      } else if (!response.body) {
        throw new Error("Empty config response");
      }

      const text = await response.text();

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

      return resolve(next());
    }).catch((error) => {
      logger.error(error);
      return res.redirect(
        urlResolver.getPortalAuthErrorUrl(
          req,
          urlResolver.ErrorMessageKey.SsoError
        )
      );
    });

    return promise;
  };

  app.use(fetchConfig);
};
