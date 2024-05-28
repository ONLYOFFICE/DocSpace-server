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
const {
    requestAuth,
    requestUser
} = require('../server/requestAPI.js');
const customUserLayout = require('./customUserLayout.js');

class customUserManager extends webdav.SimpleUserManager {
    constructor() {
        super();
        this.storeUser = new customUserLayout();
    }

    addUser(name, token, uid) {
        const user = this.storeUser.setUser(name, token, uid);
        this.users[name] = user;
        return user;
    }

    async getUserByNamePassword(ctx, username, password, callback) {
        try {
            let user = this.storeUser.getUser(username);
            if (user) {
                user = this.addUser(username, user.token, user.uid); // update last visit
            } else {
                const token = await requestAuth(ctx, username, password);
                const uid = await requestUser(ctx, token);
                user = this.addUser(username, token, uid);
                ctx.server.privilegeManager.setRights(user, '/', ['canRead']);
            }
            callback(null, user);
        } catch (error) {
            callback(webdav.Errors.UserNotFound);
        }
    }
}

module.exports = customUserManager;