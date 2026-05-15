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

const fs = require('fs');
const path = require('path');

const co = require("co");
const filenamify = require('filenamify-url');
const webshot = require('./webshot/webshot');
const config = require('../config');
const log = require('./log.js');
const fetch = require("node-fetch");
const dns = require("dns");
const Address = require("ipaddr.js");

const linkReg = /http(s)?:\/\/.*/;
let urls = [];
const queue = config.get("queue");
const noThumb = path.resolve(__dirname, "..", config.noThumb);

const nodeCache = require('node-cache');

const cache = new nodeCache({
    stdTTL: 60 * 60,
    checkperiod: 60 * 60,
    useClones: false
});

function checkValidUrl(url) {
  return new Promise((resolve, reject) => {
    fetch(url)
      .then(res => {
        var host = new URL(res.url).host;
        dns.lookup(host, (err, ip, family) => {
          if (err) {
            log.error(error);
            resolve(false);
            return;
          }
          const address = Address.parse(ip);
          const range = address.range();
          resolve(range === 'unicast');
        });
      })
      .catch(error => {
        log.error(error);
        resolve(false);
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

function takeWebShot(url, pathToFile) {
  return new Promise((resolve, reject) => {
    urls.push(url);
    webshot(url, pathToFile, config.get("webshotOptions"), function(err) {      
        urls = urls.filter((item) => item !== url);

        if (err) {
            var cached = cache.get(url);
            if (cached != undefined) {
                cache.set(url, ++cached);
                log.warn("failed to load '" + url + "': " + cached);
            } else {
                cache.set(url, 1);
                log.warn("failed to load " + url);
            }

            reject(err);
            return;
        }
        resolve(pathToFile);
        });
    });
}

function error(res, e) {
    res.sendFile(noThumb);
    if(e) {
        log.error(e);
    }
}

module.exports = function (req, res) {
    try {
      if (!req.query.url || !linkReg.test(req.query.url)) throw new Error('Empty or wrong url');

      var url = req.query.url;

      const fileName = filenamify(url);
      const pathToFile = config.pathToFile(fileName);
      const root = path.join(__dirname, "..");

      function success() {
        res.sendFile(pathToFile);
      }
  
      co(function* () {        
        const isValidUrl = yield checkValidUrl(url);
        if (!isValidUrl) {
          res.sendFile(noThumb);
          return;
        }
        
        const exists = yield checkFileExist(pathToFile);
        if (exists) {
          success();
          return;
        }

        if (urls.find(r => r === url) || urls.count > queue) {
            error(res);
        } else {
            res.sendFile(noThumb);

            var cached = cache.get(url);
            if (cached != undefined && cached > 2) return;

            yield takeWebShot(url, pathToFile);
        }

      }).catch((e) => error(res, e));
    } catch (e) {
      error(res, e);
    }
  }