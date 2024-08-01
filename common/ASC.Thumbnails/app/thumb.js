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