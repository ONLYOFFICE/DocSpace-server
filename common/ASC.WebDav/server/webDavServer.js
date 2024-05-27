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
const express = require('express');
const logger = require('../helper/logger.js');

const FileSystem = require('../manager/customFileSystem');
const customUserManager = require('../user/customUserManager');
const customHTTPBasicAuthentication = require('../user/authentication/customHTTPBasicAuthentication');
const fs = require('fs');
const {
    port,
    usersCleanupInterval,
    pfxKeyPath,
    pfxPassPhrase,
    certPath,
    keyPath,
    isHttps,
    virtualPath
} = require('./config.js');

const { logContext, logMessage } = require('../helper/helper.js');

const userManager = new customUserManager();
const privilegeManager = new webdav.SimplePathPrivilegeManager();

const options = {
    port: process.env.port || port,
    requireAuthentification: true,
    httpAuthentication: new customHTTPBasicAuthentication(userManager),
    rootFileSystem: new FileSystem(),
    privilegeManager: privilegeManager
};
if (isHttps) {
    if (!(pfxKeyPath && pfxPassPhrase) && !(certPath && keyPath)) {
        throw new Error("A secure connection is activated, but there are no keys");
    }
    if (pfxKeyPath && pfxPassPhrase) {
        options.https = {
            pfx: fs.readFileSync(pfxKeyPath),
            passphrase: pfxPassPhrase
        };
    } else {
        options.https = {
            cert: fs.readFileSync(certPath),
            key: fs.readFileSync(keyPath)
        };
    }
}

const server = new webdav.WebDAVServer(
    options
);

setInterval(function () {
    userManager.storeUser.deleteExpiredUsers((expiredUserIds) => {
        logMessage("server.deleteExpiredUsers", expiredUserIds);
        server.fileSystems['/'].manageResource.structСache.deleteStructs(expiredUserIds);
    })
}, usersCleanupInterval);

server.afterRequest((ctx, next) => {
    //logContext(ctx, "afterRequest");
    next();
});
server.beforeRequest((ctx, next) => {
    if (virtualPath) {
        if (ctx.requested.path.paths[0] != virtualPath) {
            ctx.requested.path.paths.unshift(virtualPath);
            ctx.requested.uri = ctx.requested.path.toString();
        }
    }
    //logContext(ctx, "beforeRequest");
    next();
});
//server.start((s) => console.log('Ready on port', s.address().port));

const app = express();

app.use("/isLife", (req, res) => {
    res.sendStatus(200);
});

app.use(webdav.extensions.express('/', server));

app.listen(options.port,() => {
    logger.info(`Start WebDav Service Provider listening on port ${options.port} for http`);
});