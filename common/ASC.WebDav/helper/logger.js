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
      WinstonCloudWatch = require('winston-cloudwatch'),
      { randomUUID } = require('crypto'),
      date = require('date-and-time'),
      os = require("os");


const { format } = require("winston");
require("winston-daily-rotate-file");

const path = require("path");
const config = require("../server/config.js");
const fs = require("fs");
const logLevel = process.env.logLevel || config.logLevel || "info";
const fileName = process.env.logPath || path.join(__dirname, "..", "logs", "web.webdav.%DATE%.log");
const dirName = path.dirname(fileName);

if (!fs.existsSync(dirName)) {
    fs.mkdirSync(dirName);
}

const fileTransport = new (winston.transports.DailyRotateFile)({
    filename: fileName,
    datePattern: "MM-DD",
    handleExceptions: true,
    humanReadableUnhandledException: true,
    zippedArchive: true,
    maxSize: "50m",
    maxFiles: "30d"
});

const nconf = require("nconf");

nconf.argv()
     .env();

var appsettings = config.appsettings;

if(!path.isAbsolute(appsettings)){
    appsettings = path.join(__dirname, appsettings);
}

var fileWithEnv = path.join(appsettings, 'appsettings.' + config.environment + '.json');

if(fs.existsSync(fileWithEnv)){
    nconf.file("appsettings", fileWithEnv);
}
else{
    nconf.file("appsettings", path.join(appsettings, 'appsettings.json'));
}

const aws = nconf.get("aws").cloudWatch;

const accessKeyId = aws.accessKeyId; 
const secretAccessKey = aws.secretAccessKey; 
const awsRegion = aws.region; 
const logGroupName = aws.logGroupName;
const logStreamName = aws.logStreamName.replace("${hostname}", os.hostname())
                                      .replace("${applicationContext}", "WebDav")                  
                                      .replace("${guid}", randomUUID())
                                      .replace("${date}", date.format(new Date(), 'YYYY/MM/DDTHH.mm.ss'));      

let transports = [
    new (winston.transports.Console)(),
    fileTransport
];


if (aws != null && aws.accessKeyId !== '')
{
  transports.push(new WinstonCloudWatch({
    name: 'aws',
    level: "debug",
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
  }));
}

const customFormat = winston.format(info => {
    const now = new Date();
  
    info.date = date.format(now, 'YYYY-MM-DD HH:mm:ss');
    info.applicationContext = "WebDav";
    info.level = info.level.toUpperCase();
  
    const hostname = os.hostname();
  
    info["instance-id"] = hostname;
  
    return info;
  })();

winston.exceptions.handle(fileTransport);

module.exports = winston.createLogger({
    level: logLevel,
    transports: transports,
    exitOnError: false,
    format: format.combine(
        customFormat,
        winston.format.json()    
)});