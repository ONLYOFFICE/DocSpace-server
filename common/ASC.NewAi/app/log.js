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

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const logConsole = config.get("logConsole");
let logpath = config.get("logPath");
const logLevel = config.get("logLevel") || "debug";
if (logpath != null) {
  if (!path.isAbsolute(logpath)) {
    logpath = path.join(__dirname, "..", "..", logpath);
  }
}

const fileName = logpath
  ? path.join(logpath, "web.newai.%DATE%.log")
  : path.join(__dirname, "..", "..", "..", "..", "Logs", "web.newai.%DATE%.log");
const dirName = path.dirname(fileName);

const aws = config.get("aws")?.cloudWatch;

const accessKeyId = aws?.accessKeyId;
const secretAccessKey = aws?.secretAccessKey;
const awsRegion = aws?.region;
const logGroupName = aws?.logGroupName;
const logStreamName = (aws?.logStreamName || "")
  .replace("${hostname}", os.hostname())
  .replace("${applicationContext}", "NewAi")
  .replace("${guid}", randomUUID())
  .replace("${date}", date.format(new Date(), "YYYY/MM/DDTHH.mm.ss"));

if (!fs.existsSync(dirName)) {
  fs.mkdirSync(dirName, { recursive: true });
}

const options = {
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
    name: "aws",
    level: logLevel,
    logStreamName: logStreamName,
    logGroupName: logGroupName,
    awsRegion: awsRegion,
    jsonMessage: true,
    awsOptions: {
      credentials: {
        accessKeyId: accessKeyId,
        secretAccessKey: secretAccessKey,
      },
    },
  },
};

const transports = [
  new winston.transports.DailyRotateFile(options.file),
];

if (aws != null && aws.accessKeyId) {
  transports.push(new WinstonCloudWatch(options.cloudWatch));
}

if (logConsole) {
  transports.push(new winston.transports.Console({
    level: logLevel,
    handleExceptions: true,
    json: false,
    colorize: true,
  }));
}

const customFormat = winston.format((info) => {
  const now = new Date();

  info.date = date.format(now, "YYYY-MM-DD HH:mm:ss");
  info.applicationContext = "NewAi";
  info.level = info.level.toUpperCase();

  info["instance-id"] = os.hostname();

  return info;
})();

const logger = winston.createLogger({
  format: winston.format.combine(customFormat, winston.format.json()),
  transports: transports,
  exitOnError: false,
});

export default logger;
