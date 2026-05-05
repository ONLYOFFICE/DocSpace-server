// (c) Copyright Ascensio System SIA 2009-2025
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
