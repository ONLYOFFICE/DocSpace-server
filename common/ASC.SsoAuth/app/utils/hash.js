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

const logger = require("../log.js");

var Hash = function () {
  var getHashBase64 = function (str) {
    const crypto = require("crypto");
    const sha256 = crypto.createHash("sha256");
    sha256.update(str, "utf8");
    const result = sha256.digest("base64");
    return result;
  };

  return {
    encode: function (str, secret) {
      try {
        const strHash = getHashBase64(str + secret) + "?" + str;

        let data = Buffer.from(strHash).toString("base64");

        data = data.replace(/\+/g, "-").replace(/\//g, "_");

        return data;
      } catch (ex) {
        logger?.error("hash.encode", str, secret, ex);
        return null;
      }
    },
    decode: function (str, secret) {
      try {
        let strDecoded = Buffer.from(unescape(str), "base64").toString();

        const lastIndex = strDecoded.lastIndexOf("}");

        if (lastIndex + 1 < strDecoded.length) {
          strDecoded = strDecoded.substring(0, lastIndex + 1);
        }

        const index = strDecoded.indexOf("?");

        if (index > 0 && strDecoded[index + 1] == "{") {
          let hash = strDecoded.substring(0, index);
          let data = strDecoded.substring(index + 1);
          if (getHashBase64(data + secret) === hash) {
            return data;
          }
        }

        // Sig incorrect
        return null;
      } catch (ex) {
        logger?.error("hash.decode", str, secret, ex);
        return null;
      }
    },
  };
};

module.exports = Hash();
