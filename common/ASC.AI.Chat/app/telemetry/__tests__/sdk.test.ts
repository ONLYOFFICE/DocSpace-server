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

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

// The OpenTelemetry SDK classes stood in for by recorders: no exporter is ever constructed for
// real, nothing is started, and process.exit is never reached.
const otel = vi.hoisted(() => ({
  config: {} as Record<string, unknown>,
  created: [] as [string, unknown][],
  started: 0,
  shutdown: { calls: 0, fail: false },
}));

function recorder(kind: string) {
  return class {
    constructor(options?: unknown) {
      otel.created.push([kind, options]);
    }
  };
}

// nconf, the boundary of config/index.ts, answers from `otel.config` (the environment and the
// settings files in one); the readers of config/index.ts stay the real ones. `app` is the
// section the config module needs to load.
vi.mock("nconf", () => {
  const nconf = {
    argv: () => nconf,
    env: () => nconf,
    file: () => nconf,
    get: (key: string): unknown =>
      key === "app" ? { appsettings: "/settings", environment: "test" } : otel.config[key],
  };
  return { default: nconf };
});
vi.mock("@opentelemetry/sdk-node", () => ({
  NodeSDK: class {
    constructor(options: unknown) {
      otel.created.push(["NodeSDK", options]);
    }
    start(): void {
      otel.started += 1;
    }
    shutdown(): Promise<void> {
      otel.shutdown.calls += 1;
      return otel.shutdown.fail ? Promise.reject(new Error("flush failed")) : Promise.resolve();
    }
  },
}));
vi.mock("@opentelemetry/sdk-metrics", () => ({
  PeriodicExportingMetricReader: recorder("PeriodicExportingMetricReader"),
}));
vi.mock("@opentelemetry/instrumentation-http", () => ({
  HttpInstrumentation: recorder("HttpInstrumentation"),
}));
vi.mock("@opentelemetry/instrumentation-undici", () => ({
  UndiciInstrumentation: recorder("UndiciInstrumentation"),
}));
vi.mock("@opentelemetry/exporter-trace-otlp-grpc", () => ({
  OTLPTraceExporter: recorder("grpc.OTLPTraceExporter"),
}));
vi.mock("@opentelemetry/exporter-metrics-otlp-grpc", () => ({
  OTLPMetricExporter: recorder("grpc.OTLPMetricExporter"),
}));
vi.mock("@opentelemetry/exporter-trace-otlp-proto", () => ({
  OTLPTraceExporter: recorder("proto.OTLPTraceExporter"),
}));
vi.mock("@opentelemetry/exporter-metrics-otlp-proto", () => ({
  OTLPMetricExporter: recorder("proto.OTLPMetricExporter"),
}));

type SigtermHandler = () => void;

const spyOnce = () => vi.spyOn(process, "once").mockImplementation(() => process);
const spyExit = () => vi.spyOn(process, "exit").mockImplementation(() => undefined as never);

let onceSpy: ReturnType<typeof spyOnce>;
let exitSpy: ReturnType<typeof spyExit>;

/** Loads sdk.ts afresh with this configuration; answers the logger of that load. */
async function load(config: Record<string, unknown>) {
  otel.config = config;
  vi.resetModules();
  await import("../sdk.js");
  return (await import("../../log.js")).default;
}

function created(kind: string): unknown[] {
  return otel.created.filter(([k]) => k === kind).map(([, options]) => options);
}

function sdkOptions(): {
  serviceName: string;
  traceExporter: unknown;
  metricReader: unknown;
  instrumentations: unknown[];
} {
  return created("NodeSDK")[0] as never;
}

const ENABLED = {
  "openTelemetry:enable": true,
  OTEL_EXPORTER_OTLP_ENDPOINT: "http://collector:4318",
};

const DISABLED_LINE =
  "OpenTelemetry disabled (openTelemetry.enable is off or OTEL_EXPORTER_OTLP_ENDPOINT is not set)";

beforeEach(() => {
  otel.created = [];
  otel.started = 0;
  otel.shutdown = { calls: 0, fail: false };
  onceSpy = spyOnce();
  exitSpy = spyExit();
  vi.stubEnv("OTEL_METRIC_EXPORT_INTERVAL", undefined);
});

afterEach(() => {
  vi.unstubAllEnvs();
});

describe("telemetry switched off", () => {
  it.each<[string, Record<string, unknown>]>([
    ["nothing configured", {}],
    ["enable missing", { OTEL_EXPORTER_OTLP_ENDPOINT: "http://collector:4318" }],
    ["enable false", { ...ENABLED, "openTelemetry:enable": false }],
    ['enable "false"', { ...ENABLED, "openTelemetry:enable": "false" }],
    ["enable 1", { ...ENABLED, "openTelemetry:enable": 1 }],
    ['enable "TRUE"', { ...ENABLED, "openTelemetry:enable": "TRUE" }],
    ["no endpoint", { "openTelemetry:enable": true }],
    ["an empty endpoint", { ...ENABLED, OTEL_EXPORTER_OTLP_ENDPOINT: "" }],
    ["a numeric endpoint", { ...ENABLED, OTEL_EXPORTER_OTLP_ENDPOINT: 4318 }],
  ])("starts nothing for %s", async (_name, config) => {
    const log = await load(config);

    expect(otel.created).toEqual([]);
    expect(otel.started).toBe(0);
    expect(onceSpy).not.toHaveBeenCalled();
    expect(log.info).toHaveBeenCalledWith(DISABLED_LINE);
  });
});

describe("telemetry switched on", () => {
  it("starts the SDK with http/protobuf exporters, the metric reader and both instrumentations", async () => {
    const log = await load({ ...ENABLED, "openTelemetry:enable": "true" });

    expect(otel.created.map(([kind]) => kind)).toEqual([
      "proto.OTLPTraceExporter",
      "proto.OTLPMetricExporter",
      "PeriodicExportingMetricReader",
      "HttpInstrumentation",
      "UndiciInstrumentation",
      "NodeSDK",
    ]);
    expect(created("proto.OTLPTraceExporter")).toEqual([
      { url: "http://collector:4318/v1/traces" },
    ]);
    expect(created("proto.OTLPMetricExporter")).toEqual([
      { url: "http://collector:4318/v1/metrics" },
    ]);
    expect(created("PeriodicExportingMetricReader")).toEqual([
      { exporter: expect.any(Object), exportIntervalMillis: 15_000 },
    ]);
    expect(created("UndiciInstrumentation")).toEqual([undefined]);
    expect(sdkOptions()).toEqual({
      serviceName: "ASC.AI.Chat",
      traceExporter: expect.any(Object),
      metricReader: expect.any(Object),
      instrumentations: [expect.any(Object), expect.any(Object)],
    });
    expect(otel.started).toBe(1);
    expect(log.info).toHaveBeenCalledWith(
      "OpenTelemetry enabled: service=ASC.AI.Chat endpoint=http://collector:4318 protocol=http/protobuf",
    );
  });

  it.each<[string, unknown, string, string]>([
    ["grpc", "grpc", "grpc", "http://collector:4318"],
    ["a trimmed grpc", "  grpc ", "grpc", "http://collector:4318"],
    ["an empty protocol as http/protobuf", "", "proto", "http://collector:4318/v1/traces"],
    ["a blank protocol as http/protobuf", "   ", "proto", "http://collector:4318/v1/traces"],
    ["http/json as http/protobuf", "http/json", "proto", "http://collector:4318/v1/traces"],
    ["GRPC in capitals as http/protobuf", "GRPC", "proto", "http://collector:4318/v1/traces"],
    ["a numeric protocol as http/protobuf", 1, "proto", "http://collector:4318/v1/traces"],
  ])("picks the exporters for %s", async (_name, protocol, family, traceUrl) => {
    await load({ ...ENABLED, OTEL_EXPORTER_OTLP_PROTOCOL: protocol });

    expect(created(`${family}.OTLPTraceExporter`)).toEqual([{ url: traceUrl }]);
    expect(created(`${family === "grpc" ? "proto" : "grpc"}.OTLPTraceExporter`)).toEqual([]);
  });

  it("logs the protocol as configured, trimmed", async () => {
    const log = await load({ ...ENABLED, OTEL_EXPORTER_OTLP_PROTOCOL: " http/json " });

    expect(log.info).toHaveBeenCalledWith(
      "OpenTelemetry enabled: service=ASC.AI.Chat endpoint=http://collector:4318 protocol=http/json",
    );
  });

  it.each<[string, string, string]>([
    ["grpc", "http://collector:4317///", "http://collector:4317"],
    ["http/protobuf", "http://collector:4318/otlp/", "http://collector:4318/otlp/v1/metrics"],
  ])(
    "strips the trailing slashes of the endpoint for %s",
    async (protocol, endpoint, metricUrl) => {
      await load({
        ...ENABLED,
        OTEL_EXPORTER_OTLP_ENDPOINT: endpoint,
        OTEL_EXPORTER_OTLP_PROTOCOL: protocol,
      });

      const family = protocol === "grpc" ? "grpc" : "proto";
      expect(created(`${family}.OTLPMetricExporter`)).toEqual([{ url: metricUrl }]);
    },
  );

  // OTEL_SERVICE_NAME is read through nconf like the other OTEL_* settings: the environment
  // first, then the settings files.
  it.each<[string, unknown, unknown, string]>([
    ["the configured name", "DocSpace.AI.Chat", "FromEnv", "DocSpace.AI.Chat"],
    ["the environment for an empty configured name", "", "FromEnv", "FromEnv"],
    ["the environment for a non-string configured name", 5, "FromEnv", "FromEnv"],
    ["the default without either", undefined, undefined, "ASC.AI.Chat"],
    ["the default for an empty environment value", undefined, "", "ASC.AI.Chat"],
    ["the default for a non-string environment value", undefined, 5, "ASC.AI.Chat"],
  ])("names the service with %s", async (_name, configured, env, expected) => {
    await load({
      ...ENABLED,
      "openTelemetry:ServiceName": configured,
      OTEL_SERVICE_NAME: env,
    });

    expect(sdkOptions().serviceName).toBe(expected);
  });

  it.each<[string | undefined, number]>([
    [undefined, 15_000],
    ["", 15_000],
    ["abc", 15_000],
    ["5000", 5_000],
    ["1000", 1_000],
    ["3600000", 3_600_000],
    ["5s", 15_000],
    ["0", 15_000],
    ["-5", 15_000],
    ["999", 15_000],
    ["3600001", 15_000],
    ["99999999999", 15_000],
  ])("exports metrics every interval for OTEL_METRIC_EXPORT_INTERVAL=%j", async (env, expected) => {
    vi.stubEnv("OTEL_METRIC_EXPORT_INTERVAL", env);

    await load(ENABLED);

    expect(created("PeriodicExportingMetricReader")).toEqual([
      { exporter: expect.any(Object), exportIntervalMillis: expected },
    ]);
  });

  it.each<[string, string | undefined, boolean]>([
    ["/health", "/health", true],
    ["a path ending in /health", "/api/2.0/ai/health", true],
    ["another path", "/ai/chat", false],
    ["a health path with a query", "/health?ready=1", false],
    ["a request without a url", undefined, false],
  ])("leaves %s out of the incoming request traces", async (_name, url, ignored) => {
    await load(ENABLED);

    const options = created("HttpInstrumentation")[0] as {
      ignoreIncomingRequestHook: (req: { url?: string }) => boolean;
    };
    expect(options.ignoreIncomingRequestHook({ url })).toBe(ignored);
  });

  describe("SIGTERM", () => {
    async function sigterm(): Promise<SigtermHandler> {
      await load(ENABLED);
      expect(onceSpy).toHaveBeenCalledTimes(1);
      expect(onceSpy.mock.calls[0]?.[0]).toBe("SIGTERM");
      return onceSpy.mock.calls[0]?.[1] as SigtermHandler;
    }

    it("flushes the SDK, then exits with 0", async () => {
      const handler = await sigterm();

      handler();
      expect(otel.shutdown.calls).toBe(1);
      expect(exitSpy).not.toHaveBeenCalled();
      await vi.waitFor(() => expect(exitSpy).toHaveBeenCalledWith(0), {
        interval: 1,
        timeout: 500,
      });
    });

    it("exits with 0 when the flush fails", async () => {
      otel.shutdown.fail = true;
      const handler = await sigterm();

      handler();

      await vi.waitFor(() => expect(exitSpy).toHaveBeenCalledWith(0), {
        interval: 1,
        timeout: 500,
      });
      expect(exitSpy).toHaveBeenCalledTimes(1);
    });
  });
});
