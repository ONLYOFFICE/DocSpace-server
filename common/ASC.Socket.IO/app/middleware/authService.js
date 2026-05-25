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

module.exports = () => {
  const config = require("../../config"),
    crypto = require("crypto"),
    moment = require("moment");

  const skey = config.get("core").machinekey;
  const trustInterval = 5 * 60 * 1000;

  function check(token) {
    if (!token || typeof token !== "string") return false;

    const splitted = token.split(":");
    if (splitted.length < 3) return false;

    const pkey = splitted[0].substr(4);
    const date = splitted[1];
    const orighash = splitted[2];

    const timestamp = moment.utc(date, "YYYYMMDDHHmmss");
    if (moment.utc() - timestamp > trustInterval) {
      return false;
    }

    const hasher = crypto.createHmac("sha1", skey);
    const hash = hasher.update(date + "\n" + pkey);

    if (hash.digest("base64") !== orighash) {
      return false;
    }

    return true;
  }

  return check;
};
