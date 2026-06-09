// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
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

import winston from "winston";
import WinstonCloudWatch from "winston-cloudwatch";
import "winston-daily-rotate-file";
import path from "path";
import fs from "fs";
import os from "os";
import { randomUUID } from "crypto";
import { fileURLToPath } from "url";
import date from "date-and-time";
import config from "../config/index.js";
import type { AwsCloudWatchConfig } from "./types.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const logConsole: boolean | undefined = config.get("logConsole");
let logpath: string | undefined = config.get("logPath");
const logLevel: string = config.get("logLevel") || "debug";
if (logpath != null) {
  if (!path.isAbsolute(logpath)) {
    logpath = path.join(__dirname, "..", "..", logpath);
  }
}

const fileName: string = logpath
  ? path.join(logpath, "web.newai.%DATE%.log")
  : path.join(__dirname, "..", "..", "..", "..", "Logs", "web.newai.%DATE%.log");
const dirName = path.dirname(fileName);

const awsRoot: { cloudWatch?: AwsCloudWatchConfig } | undefined = config.get("aws");
const aws: AwsCloudWatchConfig | undefined = awsRoot?.cloudWatch;

const accessKeyId = aws?.accessKeyId;
const secretAccessKey = aws?.secretAccessKey;
const awsRegion = aws?.region;
const logGroupName = aws?.logGroupName;
const logStreamName = (aws?.logStreamName ?? "")
  .replace("${hostname}", os.hostname())
  .replace("${applicationContext}", "NewAi")
  .replace("${guid}", randomUUID())
  .replace("${date}", date.format(new Date(), "YYYY/MM/DDTHH.mm.ss"));

if (!fs.existsSync(dirName)) {
  fs.mkdirSync(dirName, { recursive: true });
}

const transports: winston.transport[] = [
  new winston.transports.DailyRotateFile({
    filename: fileName,
    level: logLevel,
    datePattern: "MM-DD",
    handleExceptions: true,
    zippedArchive: true,
    maxSize: "50m",
    maxFiles: "30d",
    json: true,
  }),
];

if (aws != null && accessKeyId) {
  transports.push(new WinstonCloudWatch({
    name: "aws",
    level: logLevel,
    logStreamName,
    logGroupName,
    awsRegion,
    jsonMessage: true,
    awsOptions: {
      credentials: {
        accessKeyId,
        secretAccessKey: secretAccessKey ?? "",
      },
    },
  }));
}

if (logConsole) {
  transports.push(new winston.transports.Console({
    level: logLevel,
    handleExceptions: true,
  }));
}

const customFormat = winston.format((info) => {
  const now = new Date();
  info["date"] = date.format(now, "YYYY-MM-DD HH:mm:ss");
  info["applicationContext"] = "NewAi";
  if (typeof info.level === "string") {
    info.level = info.level.toUpperCase();
  }
  info["instance-id"] = os.hostname();
  return info;
})();

const logger = winston.createLogger({
  format: winston.format.combine(customFormat, winston.format.json()),
  transports,
  exitOnError: false,
});

export const logStream = {
  write(message: string): void {
    logger.info(message.trim());
  },
};

export default logger;
