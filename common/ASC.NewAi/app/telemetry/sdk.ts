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
import nconf from "../../config/index.js";
import logger from "../log.js";
import { isObject, parseInt10 } from "../narrow.js";

function otelSection(): Record<string, unknown> {
  const section: unknown = nconf.get("openTelemetry");
  return isObject(section) ? section : {};
}

function resolveServiceName(): string {
  const fromConfig = otelSection()["ServiceName"];
  if (typeof fromConfig === "string" && fromConfig.length > 0) {
    return fromConfig;
  }
  // Matches the .NET services' fallback style (assembly-like name).
  return process.env["OTEL_SERVICE_NAME"] || "ASC.NewAi";
}

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

// Env (Aspire) or shared appsettings root; nconf gives env precedence.
function resolveOtelSetting(key: string): string | undefined {
  const value: unknown = nconf.get(key);
  return typeof value === "string" && value.length > 0 ? value : undefined;
}

const otlpEndpoint = resolveOtelSetting("OTEL_EXPORTER_OTLP_ENDPOINT");

if (otelSection()["enable"] === true && otlpEndpoint) {
  const protocol = resolveOtelSetting("OTEL_EXPORTER_OTLP_PROTOCOL")?.trim() || "http/protobuf";
  const { traceExporter, metricExporter } = await createExporters(protocol, otlpEndpoint);
  const serviceName = resolveServiceName();

  const sdk = new NodeSDK({
    serviceName,
    traceExporter,
    metricReader: new PeriodicExportingMetricReader({
      exporter: metricExporter,
      exportIntervalMillis: parseInt10(process.env["OTEL_METRIC_EXPORT_INTERVAL"], 15_000),
    }),
    instrumentations: [
      new HttpInstrumentation({
        // Health checks are polled every few seconds — pure noise in traces.
        ignoreIncomingRequestHook: (req) => req.url?.endsWith("/health") ?? false,
      }),
      new UndiciInstrumentation(),
    ],
  });

  sdk.start();
  logger.info(
    `OpenTelemetry enabled: service=${serviceName} endpoint=${otlpEndpoint} protocol=${protocol}`,
  );

  // Flush buffered telemetry, then exit as the default SIGTERM would have.
  process.once("SIGTERM", () => {
    sdk
      .shutdown()
      .catch(() => {})
      .finally(() => process.exit(0));
  });
} else {
  logger.info(
    "OpenTelemetry disabled (openTelemetry.enable is off or OTEL_EXPORTER_OTLP_ENDPOINT is not set)",
  );
}
