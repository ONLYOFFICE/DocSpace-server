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

const fs = require('fs'),
    path = require('path'),
    co = require("co"),
    request = require("request");

const config = require('../config');
const appDataDirPath = path.join(__dirname, config.get("web:data"));

function getDataDirPath(subdir = "", createIfNotExist = true) {
    var fullPath = path.join(appDataDirPath, subdir);
    if (!createIfNotExist) return fullPath;

    return co(function* () {
        yield createDir(appDataDirPath);
        return createDir(fullPath);
    });
}

function createDir(pathToDir) {
    return co(function* () {
        const exist = yield checkDirExist(pathToDir);
        if (exist) {
            return pathToDir;
        }
        return new Promise((resolve, reject) => {
            fs.mkdir(pathToDir,
                (err) => {
                    if (err) {
                        reject(err);
                        return;
                    }

                    resolve(pathToDir);
                });
        });
    }).catch((err) => {
        log.error("Create App_Data error", err);
    });
}

function checkDirExist(pathToDir) {
    return new Promise((resolve, reject) => {
        fs.stat(pathToDir,
            function (err, stats) {
                if (err) {
                    if (err.code === 'ENOENT') {
                        resolve(false);
                    } else {
                        reject(err);
                    }
                    return;
                }

                resolve(stats.isDirectory());
            });
    });
}

function checkFileExist(pathToFile) {
    return new Promise((resolve, reject) => {
        fs.stat(pathToFile,
            function (err, stats) {
                if (err) {
                    if (err.code === 'ENOENT') {
                        resolve(false);
                    } else {
                        reject(err);
                    }
                    return;
                }

                resolve(stats.isFile());
            });
    });
}

function copyFile(source, target, append = false) {
    return new Promise(function (resolve, reject) {
        var rd = fs.createReadStream(source);
        rd.on('error', rejectCleanup);

        const writeOptions = { flags: append ? 'a' : 'w' };

        var wr = fs.createWriteStream(target, writeOptions);
        wr.on('error', rejectCleanup);

        function rejectCleanup(err) {
            rd.destroy();
            wr.end();
            reject(err);
        }
        wr.on('finish', resolve);
        rd.pipe(wr);
    });
}

function moveFile(from, to) {
    return co(function* () {
        const isExist = yield checkFileExist(from);
        if (!isExist) return;

        return new Promise((resolve, reject) => {
            fs.rename(from, to, () => { resolve(); });
        });
    })
        .catch((error) => {
            throw error;
        });
}

function deleteFile(pathToFile) {
    return co(function* () {
        const isExist = yield checkFileExist(pathToFile);
        if (!isExist) return;

        return new Promise((resolve, reject) => {
            fs.unlink(pathToFile, () => { resolve(); });
        });
    })
        .catch((error) => {
            throw error;
        });
}

function downloadFile(uri, filePath) {
    return new Promise((resolve, reject) => {
        const data = request
            .get(uri, { rejectUnauthorized: false })
            .on('error', (err) => {
                reject(err);
            })
            .pipe(fs.createWriteStream(filePath))
            .on('error', (err) => {
                reject(err);
            })
            .on('finish', () => {
                resolve(filePath);
            });
    });
}

module.exports = { checkFileExist, createDir, copyFile, moveFile, deleteFile, getDataDirPath, downloadFile };

