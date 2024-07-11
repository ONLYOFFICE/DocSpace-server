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

const request = require('request');
const axios = require('axios');
const {
    getHeader,
    getHeaderPeople,
    logError,
    logResponse,
    logMessage
} = require('../helper/helper.js');
const {
    apiPort,
    api,
    apiFiles,
    apiAuth,
    fileHandlerPath,
    method,
    isHttps
} = require('./config.js');
const renamingDuplicateElements = require('../helper/renamingDuplicateElements.js');


function getDomain(ctx) {
    const proto = ctx.headers.headers['x-forwarded-proto']?.split(',').shift();
    const host = ctx.headers.headers['x-forwarded-host']?.split(',').shift();

    return `${proto}://${host}`;
}

function instanceFunc(ctx, token = null, header = 'application/json', service = 'asc.files') {
    const domain = getDomain(ctx);

    switch (service) {
        case 'asc.files':
            return axios.create({
                baseURL: `${domain}${api}`,
                timeout: 30000,
                headers: getHeader(header, token)
            });
        case 'asc.people':
            return axios.create({
                baseURL: `${domain}${api}`,
                timeout: 30000,
                headers: getHeaderPeople(token)
            });
    }

}

function logErrorAndCheckStatus(ctx, error, method) {
    logError(error, method);
    try {
        if (error.response && error.response.status == 401) {
            ctx.server.httpAuthentication.userManager.storeUser.deleteUser(ctx.user.username);
            ctx.server.fileSystems['/'].manageResource.structСache.deleteStruct(ctx.user.uid);
            delete ctx.server.httpAuthentication.userManager.users[ctx.user.username];
            delete ctx.server.privilegeManager.rights[ctx.user.uid];
        }
    } catch (ex) {
        logError(ex, "requestAPI.logErrorAndCheckStatus");
    }
}

var requestAuth = async function (ctx, username, password) {
    try {
        const instance = instanceFunc(ctx);
        const response = await instance.post(`${apiAuth}`, {
            "userName": username,
            "password": password
        });
        logResponse(ctx, response, "requestAPI.requestAuth");
        return response.data.response.token;
    } catch (error) {
        logError(error, "requestAPI.requestAuth");
        throw error;
    }
};

var requestUser = async function (ctx, token) {
    try {
        const instance = instanceFunc(ctx, token, undefined, 'asc.people');
        const response = await instance.get("people/@self.json");
        logResponse(ctx, response, "requestAPI.requestUser");
        return response.data.response.id;
    } catch (error) {
        logError(error, "requestAPI.requestUser");
        throw error;
    }
};

var getStructDirectory = async function (ctx, folderId, token) {
    try {
        const instance = instanceFunc(ctx.context, token);
        let response = await instance.get(`${apiFiles}/${folderId}`);
        response = renamingDuplicateElements.addRealTitle(response, folderId);
        response = renamingDuplicateElements.localRename(response, folderId);
        logResponse(ctx.context, response, "requestAPI.getStructDirectory");
        return response.data.response;
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.getStructDirectory");
        throw error;
    }
};

var createFile = async function (ctx, folderId, title, token, enableExternalExt) {
    try {
        const instance = instanceFunc(ctx.context, token);
        let response = await instance.post(`${apiFiles}/${folderId}${method.file}`, {
            "title": title,
            "EnableExternalExt": enableExternalExt
        });
        response.data.response['realTitle'] = response.data.response.title;
        logResponse(ctx.context, response, "requestAPI.createFile id=" + response.data.response.id);
        return response.data.response;
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.createFile");
        throw error;
    }
};

var createFolder = async function (ctx, parentId, title, token) {
    try {
        const instance = instanceFunc(ctx.context, token);
        const response = await instance.post(`${apiFiles}${method.folder}/${parentId}`, {
            "title": title
        });
        logResponse(ctx.context, response, "requestAPI.createFolder");
        return response.data.response;
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.createFolder");
        throw error;
    }
};

var deleteFile = async function (ctx, fileId, token) {
    try {
        const instance = instanceFunc(ctx.context, token);
        const response = await instance.delete(`${apiFiles}${method.file}/${fileId}`, {
            data: {
                "DeleteAfter": true,
                "Immediately": false
            }
        });
        logResponse(ctx.context, response, "requestAPI.deleteFile id=" + fileId);
        return response.data.response[0];
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.deleteFile");
        throw error;
    }
};

var deleteFolder = async function (ctx, folderId, token) {
    try {
        const instance = instanceFunc(ctx.context, token);
        const response = await instance.delete(`${apiFiles}${method.folder}/${folderId}`, {
            data: {
                "DeleteAfter": true,
                "Immediately": false
            }
        });
        logResponse(ctx.context, response, "requestAPI.deleteFolder");
        return response.data.response[0];
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.deleteFolder");
        throw error;
    }
};

var copyFile = async function (ctx, folderId, files, token) {
    try {
        const instance = instanceFunc(ctx.context, token);
        const response = await instance.put(`${apiFiles}${method.fileops}${method.copy}`, {
            "destFolderId": folderId,
            "folderIds": [],
            "fileIds": [files],
            "conflictResolveType": "Skip",
            "deleteAfter": true
        });
        logResponse(ctx.context, response, "requestAPI.copyFile");
        return response.data.response[0];
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.copyFile");
        throw error;
    }
};

var copyFolder = async function (ctx, folderId, folders, token) {
    try {
        const instance = instanceFunc(ctx.context, token);
        const response = await instance.put(`${apiFiles}${method.fileops}${method.copy}`, {
            "destFolderId": folderId,
            "folderIds": [folders],
            "fileIds": [],
            "conflictResolveType": "Skip",
            "deleteAfter": true
        });
        logResponse(ctx.context, response, "requestAPI.copyFolder");
        return response.data.response[0];
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.copyFolder");
        throw error;
    }
};

var moveFile = async function (ctx, folderId, files, token) {
    try {
        const instance = instanceFunc(ctx.context, token);
        const response = await instance.put(`${apiFiles}${method.fileops}${method.move}`, {
            "destFolderId": folderId,
            "folderIds": [],
            "fileIds": [files],
            "resolveType": "Skip",
            "holdResult": true
        });
        logResponse(ctx.context, response, "requestAPI.moveFile");
        return response.data.response[0];
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.moveFile");
        throw error;
    }
};

var moveFolder = async function (ctx, folderId, folders, token) {
    try {
        const instance = instanceFunc(ctx.context, token);
        const response = await instance.put(`${apiFiles}${method.fileops}${method.move}`, {
            "destFolderId": folderId,
            "folderIds": [folders],
            "fileIds": [],
            "resolveType": "Skip",
            "holdResult": true
        });
        logResponse(ctx.context, response, "requestAPI.moveFolder");
        return response.data.response[0];
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.moveFolder");
        throw error;
    }
};

var statusOperation = async function (ctx, operationId, token) {
    try {
        const instance = instanceFunc(ctx.context, token);
        const response = await instance.get(`${apiFiles}${method.fileops}`);
        const operation = response.data.response.find(operation => operation.id == operationId);
        logResponse(ctx.context, response, "requestAPI.statusOperation");
        return operation;
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.statusOperation");
        throw error;
    }
}

var renameFile = async function (ctx, fileId, newName, token) {
    try {
        const instance = instanceFunc(ctx.context, token);
        const response = await instance.put(`${apiFiles}${method.file}/${fileId}`, {
            "title": newName
        });
        logResponse(ctx.context, response, "requestAPI.renameFile");
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.renameFile");
        throw error;
    }
};

var renameFolder = async function (ctx, folderId, newName, token) {
    try {
        const instance = instanceFunc(ctx.context, token);
        const response = await instance.put(`${apiFiles}${method.folder}/${folderId}`, {
            "title": newName
        });
        logResponse(ctx.context, response, "requestAPI.renameFolder");
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.renameFolder");
        throw error;
    }
};

var rewritingFile = async function (ctx, fileId, data, token) {
    try {
        const Authorization = token ? token : null;
        const instance = instanceFunc(ctx.context, token);
        const response = await instance.put(`${apiFiles}${method.file}/${fileId}${method.saveediting}`, data, {
            maxBodyLength: Infinity,
            headers: {
                Authorization,
                "Content-Type": `multipart/form-data; boundary=${data._boundary}`
            }
        });
        logResponse(ctx.context, response, "requestAPI.rewritingFile");
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.rewritingFile");
        throw error;
    }
};

var createSession = async function (ctx, folderId, formData, token) {
    try {
        const instance = instanceFunc(ctx.context, token)
        const response = await instance.post(`${apiFiles}/${folderId}${method.upload}${method.createSession}`, formData, {
            headers: {
                Connection: "keep-alive"
            }
        });
        logResponse(ctx.context, response, "requestAPI.createSession");
        return response.data.response.data.location;
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.createSession");
        throw error;
    }
}

var createEditSession = async function (ctx, fileId, fileSize, token) {
    try {
        const instance = instanceFunc(ctx.context, token)
        const response = await instance.post(`${apiFiles}${method.file}/${fileId}${method.editSession}`, {
            fileSize: fileSize
        });
        logResponse(ctx.context, response, "requestAPI.createEditSession");
        return response.data.response.data.location;
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.createEditSession");
        throw error;
    }
}

var chunkedUploader = async function (ctx, url, data, token, bytesAmount, firstPosition, secondPosition) {
    try {
        const Authorization = token ? token : null;
        const response = await axios.post(url, data, {
            maxBodyLength: Infinity,
            headers: {
                Authorization,
                "Content-Type": `multipart/form-data; boundary=${data._boundary}`,
                "Content-Range": `bytes ${firstPosition}-${secondPosition}/${bytesAmount}`
            }
        });
        logResponse(null, response, "requestAPI.chunkedUploader");
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.chunkedUploader");
        throw error;
    }
}

var getPresignedUri = async function (ctx, fileId, token) {
    try {
        const instance = instanceFunc(ctx.context, token);
        const response = await instance.get(`${apiFiles}${method.file}/${fileId}${method.presigneduri}`);
        logResponse(ctx.context, response, "requestAPI.getPresignedUri");
        return response.data.response;
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.getPresignedUri");
        throw error;
    }
};

var getReadStream = async function (ctx, uri, token) {
    try {
        const streamRequest = await request.get({
            url: uri,
            headers: getHeader("application/octet-stream", token)
        });
        logMessage("requestAPI.getReadStream", uri, "OK");
        return streamRequest;
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.getReadStream");
        throw error;
    }
};

var readFile = function (ctx, fileId, token, callback) {
    try {
        const url = getDomain(ctx.context) + fileHandlerPath.replace("{0}", fileId);
        const headers = getHeader("application/octet-stream", token);
        const range = ctx.context.headers.find("Range");
        const streamRequest = request.get({ url, headers });
        callback(null, streamRequest);
        streamRequest.end();
        logMessage("requestAPI.readFile", "fileId: " + fileId + (range ? " range: " + range : ""), "OK");
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.readFile");
        callback(error);
    }
};

var createFileTxt = async function (ctx, folderId, title, token) {
    try {
        const instance = await instanceFunc(ctx.context, token);
        const response = await instance.post(`${apiFiles}/${folderId}${method.text}`, {
            "title": title,
            "content": ' '
        });
        response.data.response['realTitle'] = response.data.response.title;
        logResponse(ctx.context, response, "requestAPI.createFileTxt");
        return response.data.response;
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.createFileTxt");
        throw error;
    }
};

var createFileHtml = async function (ctx, folderId, title, token) {
    try {
        const instance = await instanceFunc(ctx.context, token);
        const response = await instance.post(`${apiFiles}/${folderId}${method.html}`, {
            "title": title,
            "content": ' '
        });
        response.data.response['realTitle'] = response.data.response.title;
        logResponse(ctx.context, response, "requestAPI.createFileHtml");
        return response.data.response;
    } catch (error) {
        logErrorAndCheckStatus(ctx.context, error, "requestAPI.createFileHtml");
        throw error;
    }
};

module.exports = {
    getStructDirectory,
    getPresignedUri,
    getReadStream,
    readFile,
    createSession,
    rewritingFile,
    createFileTxt,
    createFileHtml,
    chunkedUploader,
    requestAuth,
    requestUser,
    createFile,
    createFolder,
    deleteFile,
    deleteFolder,
    copyFile,
    copyFolder,
    moveFile,
    moveFolder,
    renameFile,
    renameFolder,
    createEditSession,
    statusOperation
};