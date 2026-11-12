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

const fs = require('fs'),
    path = require('path'),
    http = require("http"),
    https = require("https"),
    { pipeline } = require("stream/promises"),
    { IncomingForm } = require("formidable"),
    co = require("co");

const downloadMaxRedirects = 5;
const downloadTimeout = 30 * 1000;
const metadataMaxBytes = 10 * 1024 * 1024;

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

// Every failure must end in a rejection: anything thrown inside a response
// callback escapes the promise and leaves the request without a response.
function getResponse(location, baseUrl, signal, redirectsLeft) {
    return new Promise((resolve, reject) => {
        let url;
        try {
            url = new URL(location, baseUrl);
        } catch (err) {
            reject(err);
            return;
        }

        const client = url.protocol === "https:" ? https : url.protocol === "http:" ? http : null;
        if (!client) {
            reject(new Error(`Unsupported protocol ${url.protocol}`));
            return;
        }

        // IdP metadata is often published on hosts with self-signed certificates.
        const req = client.get(url, { rejectUnauthorized: false, signal }, (res) => {
            const { statusCode, headers } = res;

            if (statusCode >= 300 && statusCode < 400 && headers.location) {
                res.resume();
                if (redirectsLeft <= 0) {
                    reject(new Error("Too many redirects"));
                    return;
                }
                resolve(getResponse(headers.location, url, signal, redirectsLeft - 1));
                return;
            }

            if (statusCode !== 200) {
                res.resume();
                reject(new Error(`Download failed with status ${statusCode}`));
                return;
            }

            const contentLength = Number(headers["content-length"]);
            if (contentLength > metadataMaxBytes) {
                res.destroy();
                reject(new Error(`Download exceeds ${metadataMaxBytes} bytes`));
                return;
            }

            resolve(res);
        });

        req.on("error", reject);
    });
}

async function* limitSize(source) {
    let size = 0;
    for await (const chunk of source) {
        size += chunk.length;
        if (size > metadataMaxBytes) {
            throw new Error(`Download exceeds ${metadataMaxBytes} bytes`);
        }
        yield chunk;
    }
}

// The deadline covers the whole download, redirects and body included:
// a socket timeout alone never fires on a server that keeps sending a byte now and then.
async function downloadFile(uri, filePath, options = {}) {
    const timeout = options.timeout || downloadTimeout;
    const signal = AbortSignal.timeout(timeout);
    try {
        const res = await getResponse(uri, undefined, signal, downloadMaxRedirects);
        await pipeline(res, limitSize, fs.createWriteStream(filePath), { signal });
        return filePath;
    } catch (err) {
        if (signal.aborted) {
            throw new Error(`Download timed out after ${timeout} ms`, { cause: err });
        }
        throw err;
    }
}

// An uploaded metadata file gets the same size limit as a download by URL. Only the first file
// under `metadata` is stored, so the caller has exactly that one file to delete. A limit of
// maxFiles is not used: formidable opens the extra file after it has already cleaned up for the
// error, and that file would stay on disk.
function createMetadataUploadForm(uploadDir) {
    let metadataTaken = false;
    return new IncomingForm({
        uploadDir,
        maxFileSize: metadataMaxBytes,
        maxFieldsSize: 64 * 1024,
        filter: (part) => {
            if (part.name !== "metadata" || metadataTaken) {
                return false;
            }
            metadataTaken = true;
            return true;
        },
    });
}

// A metadata upload is one file within the limit plus a few form fields; a declared body larger
// than that is refused before it is read. A chunked body has no length to check and is read in full.
function isMetadataUploadTooLarge(req) {
    return Number(req.headers["content-length"]) > metadataMaxBytes + 64 * 1024;
}

module.exports = { checkFileExist, createDir, copyFile, moveFile, deleteFile, getDataDirPath, downloadFile, createMetadataUploadForm, isMetadataUploadTooLarge };

