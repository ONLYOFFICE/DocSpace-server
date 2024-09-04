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

const winston = require("winston"),
      WinstonCloudWatch = require('winston-cloudwatch');

require("winston-daily-rotate-file");

const path = require("path");
const config = require("../config");
const fs = require("fs");
const os = require("os");
const { randomUUID } = require('crypto');
const date = require('date-and-time');

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
  console: {
    level: logLevel,
    handleExceptions: true,
    json: false,
    colorize: true,
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
  new winston.transports.Console(options.console),
  new winston.transports.DailyRotateFile(options.file)  
];

if (aws != null && aws.accessKeyId !== '')
{
  transports.push(new WinstonCloudWatch(options.cloudWatch));
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
    winston.format.json()    
  ),
  transports: transports,
  exitOnError: false,
});