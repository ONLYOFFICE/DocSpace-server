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

using System.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using InfluxDB.Client;
using InfluxDB.Client.Writes;
using InfluxDB.Client.Api.Domain;
using System.Reflection.PortableExecutable;

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

    public static IServiceCollection AddOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var telemetrySettings = configuration.GetSection("openTelemetry").Get<OpenTelemetrySettings>();
        var influxClient = new InfluxDBClient(telemetrySettings.InfluxDB.Endpoint, telemetrySettings.InfluxDB.Token);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(telemetrySettings.ServiceName))
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();
                metrics.AddRuntimeInstrumentation();
                metrics.AddProcessInstrumentation();
                metrics.AddInfluxDBMetricsExporter(options =>
                {
                    options.Endpoint = new Uri(telemetrySettings.InfluxDB.Endpoint);
                    options.Token = telemetrySettings.InfluxDB.Token;
                    options.Bucket = telemetrySettings.InfluxDB.Bucket;
                    options.Org = telemetrySettings.InfluxDB.Org;
                });
            });

        return services;
    }
}