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

const winston = require("winston"),
      WinstonCloudWatch = require('winston-cloudwatch');

require("winston-daily-rotate-file");

const path = require("path");
const config = require("../config");
const fs = require("fs");
const os = require("os");
const { randomUUID } = require('crypto');
const date = require('date-and-time');

let logConsole = config.get("logConsole");
let logpath = config.get("logPath");
let logLevel = config.get("logLevel") || "debug";
if(logpath != null)
{
    if(!path.isAbsolute(logpath))
    {
        logpath = path.join(__dirname, "..", "..", logpath);
    }
}

const fileName = logpath ? path.join(logpath, "socket-io.%DATE%.log") : path.join(__dirname, "..", "..", "..", "..", "Logs", "socket-io.%DATE%.log");
const dirName = path.dirname(fileName);

const aws = config.get("aws").cloudWatch;

const accessKeyId = aws.accessKeyId;
const secretAccessKey = aws.secretAccessKey;
const awsRegion = aws.region;
const logGroupName = aws.logGroupName;
const logStreamName = aws.logStreamName.replace("${hostname}", os.hostname())
                                      .replace("${applicationContext}", "SocketIO")
                                      .replace("${guid}", randomUUID())
                                      .replace("${date}", date.format(new Date(), 'YYYY/MM/DDTHH.mm.ss'));

if (!fs.existsSync(dirName)) {
  fs.mkdirSync(dirName);
}

var options = {
  file: {
    filename: fileName,
    level: logLevel,
    datePattern: "MM-DD",
    handleExceptions: true,
    humanReadableUnhandledException: true,
    zippedArchive: true,
    maxSize: "50m",
    maxFiles: "30d",
    json: true,
  },
  cloudWatch: {
    name: 'aws',
    level: logLevel,
    logStreamName: logStreamName,
    logGroupName: logGroupName,
    awsRegion: awsRegion,
    jsonMessage: true,
    awsOptions: {
      credentials: {
        accessKeyId: accessKeyId,
        secretAccessKey: secretAccessKey
      }
    }
  }
};

let transports = [
  new winston.transports.DailyRotateFile(options.file)
];

if (aws != null && aws.accessKeyId !== '')
{
  transports.push(new WinstonCloudWatch(options.cloudWatch));
}

if(logConsole) {
  transports.push(new winston.transports.Console({
    level: logLevel,
    handleExceptions: true,
    json: false,
    colorize: true,
  }));
}

const customFormat = winston.format(info => {
  const now = new Date();

  info.date = date.format(now, 'YYYY-MM-DD HH:mm:ss');
  info.applicationContext = "SocketIO";
  info.level = info.level.toUpperCase();

  const hostname = os.hostname();

  info["instance-id"] = hostname;

  return info;
})();

module.exports = new winston.createLogger({
  format: winston.format.combine(
    customFormat,
    winston.format.errors({ stack: true }),
    winston.format.json()
  ),
  transports: transports,
  exitOnError: false,
});
