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
