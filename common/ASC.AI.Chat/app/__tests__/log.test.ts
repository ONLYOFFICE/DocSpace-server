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

import os from "os";
import path from "path";
import { fileURLToPath } from "url";
import type { Logform } from "winston";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

// The real log.ts (setup.ts stubs it for every other file). Its boundaries are mocked so that
// nothing is written: the winston transports, the CloudWatch transport, `fs` (no directory is
// created) and the configuration it reads. `createLogger` answers a stub whose `info` is a spy;
// the format it was handed is run directly.

vi.unmock("../log.js");

const w = vi.hoisted(() => ({
  daily: vi.fn(),
  console: vi.fn(),
  cloud: vi.fn(),
  createLogger: vi.fn(),
  exists: vi.fn<(dir: string) => boolean>(),
  mkdir: vi.fn(),
  config: {} as Record<string, unknown>,
}));

vi.mock("winston", async (original) => {
  const real = (await original<{ default: typeof import("winston") }>()).default;
  const transports = {
    DailyRotateFile: class {
      constructor(options: unknown) {
        w.daily(options);
      }
    },
    Console: class {
      constructor(options: unknown) {
        w.console(options);
      }
    },
  };
  return { default: { ...real, transports, createLogger: w.createLogger } };
});
vi.mock("winston-daily-rotate-file", () => ({ default: {} }));
vi.mock("winston-cloudwatch", () => ({
  default: class {
    constructor(options: unknown) {
      w.cloud(options);
    }
  },
}));
vi.mock("fs", async (original) => {
  const real = await original<typeof import("fs")>();
  const existsSync = (dir: string) => w.exists(dir);
  const mkdirSync = (...args: unknown[]) => w.mkdir(...args);
  return { ...real, existsSync, mkdirSync, default: { ...real, existsSync, mkdirSync } };
});
vi.mock("../../config/index.js", () => ({
  default: { get: (key: string) => w.config[key] },
}));

const APP_DIR = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const LOGGER = { info: vi.fn() };

async function load(config: Record<string, unknown>, dirExists = true) {
  w.config = config;
  w.exists.mockReturnValue(dirExists);
  w.createLogger.mockReturnValue(LOGGER);
  vi.resetModules();
  return await import("../log.js");
}

// The first import pays for loading winston; done here so that no test carries it.
await load({});

const DAILY = {
  level: "debug",
  datePattern: "MM-DD",
  handleExceptions: true,
  zippedArchive: true,
  maxSize: "50m",
  maxFiles: "30d",
  json: true,
};

beforeEach(() => {
  vi.useFakeTimers({ toFake: ["Date"] });
  vi.setSystemTime(new Date(2026, 0, 2, 3, 4, 5));
});

afterEach(() => {
  vi.useRealTimers();
});

describe("the log file", () => {
  it("goes to the Logs directory next to the repository by default, at debug", async () => {
    const dir = path.join(APP_DIR, "..", "..", "..", "..", "Logs");
    const log = await load({});

    expect(log.logDir).toBe(dir);
    expect(w.exists).toHaveBeenCalledWith(dir);
    expect(w.mkdir).not.toHaveBeenCalled();
    expect(w.daily).toHaveBeenCalledExactlyOnceWith({
      ...DAILY,
      filename: path.join(dir, "web.ai-chat.%DATE%.log"),
    });
    expect(w.cloud).not.toHaveBeenCalled();
    expect(w.console).not.toHaveBeenCalled();
    expect(log.default).toBe(LOGGER);
  });

  it("creates the directory when it is missing", async () => {
    const log = await load({ logPath: "/var/log/docspace" }, false);

    expect(log.logDir).toBe("/var/log/docspace");
    expect(w.mkdir).toHaveBeenCalledExactlyOnceWith("/var/log/docspace", { recursive: true });
  });

  it("resolves a relative logPath against the service directory and takes logLevel", async () => {
    const log = await load({ logPath: "logs", logLevel: "warn" });

    expect(log.logDir).toBe(path.join(APP_DIR, "..", "..", "logs"));
    expect(w.daily).toHaveBeenCalledWith({
      ...DAILY,
      level: "warn",
      filename: path.join(APP_DIR, "..", "..", "logs", "web.ai-chat.%DATE%.log"),
    });
  });

  it("falls back to debug for an empty logLevel", async () => {
    await load({ logLevel: "" });

    expect(w.daily).toHaveBeenCalledWith(expect.objectContaining({ level: "debug" }));
  });

  it("hands the transports to one logger that does not exit on an error", async () => {
    await load({ logConsole: true });

    expect(w.createLogger).toHaveBeenCalledExactlyOnceWith({
      format: expect.anything(),
      transports: [expect.anything(), expect.anything()],
      exitOnError: false,
    });
  });
});

describe("the console transport", () => {
  it("is added at the log level when logConsole is set", async () => {
    await load({ logConsole: true, logLevel: "info" });

    expect(w.console).toHaveBeenCalledExactlyOnceWith({ level: "info", handleExceptions: true });
  });

  it.each([false, undefined, 0])("is not added for logConsole %j", async (logConsole) => {
    await load({ logConsole });

    expect(w.console).not.toHaveBeenCalled();
  });
});

describe("the CloudWatch transport", () => {
  it("is added with the stream name filled in when an access key is configured", async () => {
    await load({
      logLevel: "info",
      aws: {
        cloudWatch: {
          accessKeyId: "AKIA",
          secretAccessKey: "s3cr3t",
          region: "eu-west-1",
          logGroupName: "group",
          logStreamName: "${hostname}/${applicationContext}/${guid}/${date}/${hostname}",
        },
      },
    });

    expect(w.cloud).toHaveBeenCalledExactlyOnceWith({
      name: "aws",
      level: "info",
      logStreamName: expect.stringMatching(
        new RegExp(
          `^${os.hostname().replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}/AI\\.Chat/[0-9a-f-]{36}/2026/01/02T03\\.04\\.05/\\$\\{hostname\\}$`,
        ),
      ),
      logGroupName: "group",
      awsRegion: "eu-west-1",
      jsonMessage: true,
      awsOptions: { credentials: { accessKeyId: "AKIA", secretAccessKey: "s3cr3t" } },
    });
  });

  it("sends an empty secret and an empty stream name when they are not configured", async () => {
    await load({ aws: { cloudWatch: { accessKeyId: "AKIA" } } });

    expect(w.cloud).toHaveBeenCalledWith(
      expect.objectContaining({
        logStreamName: "",
        logGroupName: undefined,
        awsRegion: undefined,
        awsOptions: { credentials: { accessKeyId: "AKIA", secretAccessKey: "" } },
      }),
    );
  });

  it.each([
    ["no aws section", undefined],
    ["no cloudWatch section", {}],
    ["no access key", { cloudWatch: { secretAccessKey: "s" } }],
    ["an empty access key", { cloudWatch: { accessKeyId: "" } }],
  ])("is not added with %s", async (_what, aws) => {
    await load({ aws });

    expect(w.cloud).not.toHaveBeenCalled();
  });
});

describe("the line format", () => {
  const formatOf = async (): Promise<Logform.Format> => {
    await load({});
    const options = w.createLogger.mock.calls[0]?.[0] as { format: Logform.Format };
    return options.format;
  };

  it("adds the date, the service, the host and an upper-case level, as JSON", async () => {
    const format = await formatOf();
    const info = format.transform({ level: "info", message: "hello" }) as Record<
      string | symbol,
      unknown
    >;

    expect(info).toMatchObject({
      level: "INFO",
      message: "hello",
      date: "2026-01-02 03:04:05",
      applicationContext: "AI.Chat",
      "instance-id": os.hostname(),
    });
    expect(JSON.parse(String(info[Symbol.for("message")]))).toEqual({
      level: "INFO",
      message: "hello",
      date: "2026-01-02 03:04:05",
      applicationContext: "AI.Chat",
      "instance-id": os.hostname(),
    });
  });

  it("leaves a level that is not a string as it is", async () => {
    const format = await formatOf();
    const info = format.transform({ level: 5 as unknown as string, message: "m" }) as Record<
      string,
      unknown
    >;

    expect(info["level"]).toBe(5);
  });
});

describe("logStream", () => {
  it("writes each access-log line to info, trimmed", async () => {
    const log = await load({});

    log.logStream.write('  ::1 - - "GET /isLife HTTP/1.1" 200\n');

    expect(LOGGER.info).toHaveBeenCalledExactlyOnceWith('::1 - - "GET /isLife HTTP/1.1" 200');
  });
});
