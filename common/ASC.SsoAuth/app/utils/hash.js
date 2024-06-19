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
