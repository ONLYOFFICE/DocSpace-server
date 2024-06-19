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

const logger = require('./logger.js');

const logSplitter = ' | ';

var getHeader = function (contentType, token) {
    const headers = {
        ContentType: contentType,
        Accept: 'application/json'
    };
    if (token) {
        headers.Authorization = token;
    }
    return headers;
};

var getHeaderPeople = function (token) {
    const headers = {
        Accept: 'text/html,application/xhtml+xml,application/xml'
    };
    if (token) {
        headers.Authorization = token;
    }
    return headers;
};

var getErrorLogMsg = function (error) { 
    let message = '';
    if (error) {
        if (error.message) {
            message = `${logSplitter}${error.message}`;
        }
        if (error.request) {
            message += `${logSplitter}${error.request.method + ' ' + error.request.path}`
        }
        if (error.response) {
            if (error.response.config) {
                message += `${logSplitter}${error.response.config.data}`
            }
            if (error.response.data && error.response.data.error) {
                message += `${logSplitter}${error.response.data.error.message + '\n' + error.response.data.error.stack}`
            }
        }
    }
    return message;
};

var getContextLogMsg = function (ctx) { 
    let message = '';
    if (ctx) {
        if (ctx.user) {
            message += `${logSplitter}${ctx.user.username}`
        }
        if (ctx.headers) {
            message += `${logSplitter}${ctx.headers.headers["user-agent"]}`
        }
        if (ctx.request) {
            message += `${logSplitter}${ctx.request.method} ${ctx.requested.uri}`
        }
        if (ctx.response) {
            message += `${logSplitter}${ctx.response.statusCode} ${ctx.response.statusMessage || ''}`
        }
    }
    return message;
}

var getResponseLogMsg = function (response) { 
    let message = '';
    if (response) {
        if (response.config) {
            let url = response.config.baseURL
                ? response.config.baseURL + "/" + response.config.url.replace(/^\//, '')
                : response.config.url;

            message += `${logSplitter}${response.config.method} ${url}`
            if (response.config.data) {
                message += `${logSplitter}${response.config.data}`
            }
        }
        if (response.status) {
            message += `${logSplitter}${response.status} ${response.statusText}`
        }
        if (response.data) {
            //message += `${logSplitter}${JSON.stringify(response.data.response)}`
        }
    }
    return message;
}

var logError = function (error, method) { 
    let message = method || '';
    message += getErrorLogMsg(error);
    logger.error(message);
};

var logResponse = function (ctx, response, method) { 
    let message = method || '';
    message += getContextLogMsg(ctx);
    message += getResponseLogMsg(response);
    logMessage(message);
};

var logContext = function (ctx, method) { 
    let message = method || '';
    message += getContextLogMsg(ctx);
    logMessage(message);
}

var logMessage = function () { 
    if (arguments.length > 0) {
        logger.debug(Array.prototype.join.call(arguments, logSplitter));
    }
}

module.exports = {
    getHeader,
    getHeaderPeople,
    logError,
    logResponse,
    logContext,
    logMessage
};