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

using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

#pragma warning disable CA2000

namespace ASC.Api.Core.Extensions;
public static class OpenTelemetryExtension
{
    public class OpenTelemetrySettings
    {
        public string ServiceName { get; set; }

        public InfluxDBSettings InfluxDB { get; set; }
    }

    public class InfluxDBSettings
    {
        public string Endpoint { get; set; }
        public string Token { get; set; }
        public string Org { get; set; }
        public string Bucket { get; set; }
    }

    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        public TBuilder ConfigureOpenTelemetry()
        {
            var telemetrySettings = builder.Configuration.GetSection("openTelemetry").Get<OpenTelemetrySettings>();

            var otlpEndpointValue = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
            var useOtlpExporter = !string.IsNullOrWhiteSpace(otlpEndpointValue);

            var fileExporterEndpoint = builder.Configuration["OTEL_FILE_EXPORTER_ENDPOINT"];
            var fileEndpoint = !string.IsNullOrWhiteSpace(fileExporterEndpoint) ? new Uri(fileExporterEndpoint) : null;

            var serviceName = new[]
            {
                telemetrySettings?.ServiceName,
                Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME"),
                builder.Environment.ApplicationName,
                Assembly.GetEntryAssembly()?.GetName().Name
            }.FirstOrDefault(static s => !string.IsNullOrWhiteSpace(s));

            var otlbuilder = builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddFusionCacheInstrumentation();

                    if (telemetrySettings?.InfluxDB != null)
                    {
                        metrics.AddInfluxDBMetricsExporter(options =>
                        {
                            options.Endpoint = new Uri(telemetrySettings.InfluxDB.Endpoint);
                            options.Token = telemetrySettings.InfluxDB.Token;
                            options.Bucket = telemetrySettings.InfluxDB.Bucket;
                            options.Org = telemetrySettings.InfluxDB.Org;
                        });
                    }

                    if (useOtlpExporter)
                    {
                        metrics.AddOtlpExporter();
                    }

                    if (fileEndpoint is not null)
                    {
                        metrics.AddOtlpExporter("file", options =>
                        {
                            options.Endpoint = fileEndpoint;
                            options.Protocol = OtlpExportProtocol.Grpc;
                        });
                    }

                    metrics.AddMeter("MySqlConnector");
                })
                .WithTracing(tracing =>
                {
                    tracing
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation()
                        .AddFusionCacheInstrumentation()
                        .AddEntityFrameworkCoreInstrumentation();

                    if (ServiceCollectionExtension.IsRedisEnabled(builder.Configuration))
                    {
                        tracing.AddRedisInstrumentation();
                    }

                    if (useOtlpExporter)
                    {
                        tracing.AddOtlpExporter();
                    }

                    if (fileEndpoint is not null)
                    {
                        tracing.AddOtlpExporter("file", options =>
                        {
                            options.Endpoint = fileEndpoint;
                            options.Protocol = OtlpExportProtocol.Grpc;
                        });
                    }
                });

            if (!string.IsNullOrWhiteSpace(serviceName))
            {
                // Do not auto-generate service.instance.id: under Aspire it would override the
                // instance id injected via OTEL_RESOURCE_ATTRIBUTES, so the dashboard could no longer
                // correlate telemetry to the orchestrated resource and showed a duplicate "phantom"
                // instance per service. Letting the env-provided instance id stand keeps one entry.
                otlbuilder.ConfigureResource(resource => resource.AddService(serviceName, autoGenerateServiceInstanceId: false));
            }

            return builder;
        }
    }
}
