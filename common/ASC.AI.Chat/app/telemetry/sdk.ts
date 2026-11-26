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

// OpenTelemetry bootstrap. Mirrors the .NET contract: on only when
// `openTelemetry.enable` (shared appsettings) and an OTLP endpoint are both
// present; otherwise a no-op. Undici instrumentation covers every outgoing
// fetch (storage adapters, provider SDKs) and propagates traceparent to .NET.

import { NodeSDK } from "@opentelemetry/sdk-node";
import { PeriodicExportingMetricReader } from "@opentelemetry/sdk-metrics";
import { HttpInstrumentation } from "@opentelemetry/instrumentation-http";
import { UndiciInstrumentation } from "@opentelemetry/instrumentation-undici";
import { context, metrics, propagation, trace } from "@opentelemetry/api";
import { getMetricExportIntervalMs, getTelemetrySettings } from "../../config/index.js";
import logger from "../log.js";

// The endpoint may come from appsettings rather than env, so it is passed
// explicitly — and an explicit `url` is taken verbatim, hence the per-signal
// /v1/* paths are appended here. Anything but grpc falls back to http/protobuf.
async function createExporters(protocol: string, endpoint: string) {
  const base = endpoint.replace(/\/+$/, "");
  if (protocol === "grpc") {
    const [traces, metrics] = await Promise.all([
      import("@opentelemetry/exporter-trace-otlp-grpc"),
      import("@opentelemetry/exporter-metrics-otlp-grpc"),
    ]);
    return {
      traceExporter: new traces.OTLPTraceExporter({ url: base }),
      metricExporter: new metrics.OTLPMetricExporter({ url: base }),
    };
  }
  const [traces, metrics] = await Promise.all([
    import("@opentelemetry/exporter-trace-otlp-proto"),
    import("@opentelemetry/exporter-metrics-otlp-proto"),
  ]);
  return {
    traceExporter: new traces.OTLPTraceExporter({ url: `${base}/v1/traces` }),
    metricExporter: new metrics.OTLPMetricExporter({ url: `${base}/v1/metrics` }),
  };
}

const { enabled, otlpEndpoint, otlpProtocol: protocol, serviceName } = getTelemetrySettings();

/**
 * Takes back what a `start()` that threw may have set up: it patches http and undici first and
 * registers the global providers after, and `NodeSDK.shutdown()` stops the providers only, so the
 * instrumentations are disabled and the globals reset here. A failure of the shutdown is logged;
 * the error of `start()` is the one the caller reports.
 */
async function undoStart(
  sdk: NodeSDK,
  instrumentations: readonly { disable(): void }[],
): Promise<void> {
  for (const instrumentation of instrumentations) {
    instrumentation.disable();
  }
  trace.disable();
  metrics.disable();
  propagation.disable();
  context.disable();
  try {
    await sdk.shutdown();
  } catch (shutdownErr) {
    logger.warn(
      `OpenTelemetry: the shutdown after a failed start failed too: ${
        shutdownErr instanceof Error ? shutdownErr.message : String(shutdownErr)
      }`,
    );
  }
}

/** Starts the SDK; throws what an exporter or the SDK refuses its configuration with. */
async function start(endpoint: string): Promise<NodeSDK> {
  const { traceExporter, metricExporter } = await createExporters(protocol, endpoint);

  const instrumentations = [
    new HttpInstrumentation({
      // Health checks are polled every few seconds — pure noise in traces.
      ignoreIncomingRequestHook: (req) => req.url?.endsWith("/health") ?? false,
    }),
    new UndiciInstrumentation(),
  ];
  const sdk = new NodeSDK({
    serviceName,
    traceExporter,
    metricReader: new PeriodicExportingMetricReader({
      exporter: metricExporter,
      exportIntervalMillis: getMetricExportIntervalMs(),
    }),
    instrumentations,
  });

  try {
    sdk.start();
  } catch (err) {
    await undoStart(sdk, instrumentations);
    throw err;
  }
  return sdk;
}

// Telemetry is optional, and this module is the first import of app.ts: whatever the exporters
// or the SDK throw for their configuration is logged, and the service starts without telemetry
// (ts-ai-style.md, "Config").
let sdk: NodeSDK | undefined;
if (enabled && otlpEndpoint) {
  try {
    sdk = await start(otlpEndpoint);
  } catch (err) {
    logger.error(
      `OpenTelemetry disabled: it could not be started (protocol=${protocol}): ${
        err instanceof Error ? err.message : String(err)
      }`,
    );
  }
}

if (sdk) {
  const started = sdk;
  logger.info(
    `OpenTelemetry enabled: service=${serviceName} endpoint=${otlpEndpoint} protocol=${protocol}`,
  );

  // Flush buffered telemetry, then exit with status 0: the process goes down whether the flush
  // worked or not. (A handler replaces the default action of SIGTERM, which ends the process
  // by the signal itself, so the exit is made here.)
  process.once("SIGTERM", () => {
    const exit = (): never => process.exit(0);
    started.shutdown().then(exit, exit);
  });
} else if (!enabled || !otlpEndpoint) {
  logger.info(
    "OpenTelemetry disabled (openTelemetry.enable is off or OTEL_EXPORTER_OTLP_ENDPOINT is not set)",
  );
}
