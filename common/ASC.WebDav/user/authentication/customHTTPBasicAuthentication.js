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

const webdav = require('webdav-server').v2;

class customHTTPBasicAuthentication extends webdav.HTTPBasicAuthentication
{

    getUser (ctx, callback) {
        var _this = this;
        var onError = function (error) {
            _this.userManager.getDefaultUser(function (defaultUser) {
                callback(error, defaultUser);
            });
        };
        var authHeader = ctx.headers.find('Authorization');
        if (!authHeader) {
            onError(webdav.Errors.MissingAuthorisationHeader);
            return;
        }
        if (!/^Basic \s*[a-zA-Z0-9]+=*\s*$/.test(authHeader)) {
            onError(webdav.Errors.WrongHeaderFormat);
            return;
        }
        var value = Buffer.from(/^Basic \s*([a-zA-Z0-9]+=*)\s*$/.exec(authHeader)[1], 'base64').toString().split(':', 2);
        var username = value[0];
        var password = value[1];
        this.userManager.getUserByNamePassword(ctx, username, password, function (e, user) {
            if (e)
                onError(webdav.Errors.BadAuthentication);
            else
                callback(null, user);
        });
    }
}

module.exports = customHTTPBasicAuthentication;