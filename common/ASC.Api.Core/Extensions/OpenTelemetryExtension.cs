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

namespace ASC.Api.Core.Extensions;
public static class OpenTelemetryExtension
{
    public static IServiceCollection AddOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var telemetryConfig = configuration.GetSection("openTelemetry");
        var serviceName = telemetryConfig.GetValue<string>("serviceName");
        var influxConfig = telemetryConfig.GetSection("influxdb");
        var influxEndpoint = influxConfig.GetValue<string>("endpoint");
        var influxToken = influxConfig.GetValue<string>("token");
        var influxOrg = influxConfig.GetValue<string>("org");
        var influxBucket = influxConfig.GetValue<string>("bucket");
        var influxClient = new InfluxDBClient(influxEndpoint, influxToken);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName)) 
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();

                metrics.AddMeter("CustomMetrics");
                metrics.AddInstrumentation(serviceProvider =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<MetricCollector>>();

                    return new MetricCollector(() =>
                    {
                        var cpuUsage = GetCpuUsage();
                        var ramAvailable = GetMemoryUsage();

                        logger.LogInformation($"CPU Usage: {cpuUsage}%, Available ram: {ramAvailable}GB");
                        WriteToInfluxDB(influxClient, influxOrg, influxBucket, serviceName, cpuUsage, ramAvailable);
                    });
                });
            });

        return services;
    }
    private static void WriteToInfluxDB(InfluxDBClient influxClient, string org, string bucket, string serviceName, double cpuUsage, double memoryUsage)
    {
        using (var writeApi = influxClient.GetWriteApi())
        {
            var cpuPoint = PointData.Measurement("cpu_usage")
                .Tag("service", serviceName)
                .Field("usage", cpuUsage)
                .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            var memoryPoint = PointData.Measurement("ram_available")
                .Tag("service", serviceName)
                .Field("available", memoryUsage)
                .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            writeApi.WritePoint(cpuPoint, bucket, org);
            writeApi.WritePoint(memoryPoint, bucket, org);
        }
    }
    public static double GetCpuUsage()
    {
        if (OperatingSystem.IsWindows())
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue();
            Thread.Sleep(500);
            return (int)cpuCounter.NextValue();
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            var cpuInfo = System.IO.File.ReadAllLines("/proc/stat").FirstOrDefault(line => line.StartsWith("cpu ")) ?? throw new InvalidOperationException("Unable to read CPU stats from /proc/stat.");
            var cpuData = cpuInfo.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(double.Parse).ToArray();

            var totalTime = cpuData.Sum();
            var idleTime = cpuData[3];

            Thread.Sleep(500);
            var cpuInfo2 = System.IO.File.ReadAllLines("/proc/stat").First(line => line.StartsWith("cpu "));
            var cpuData2 = cpuInfo2.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(double.Parse).ToArray();

            var totalTime2 = cpuData2.Sum();
            var idleTime2 = cpuData2[3];
            var totalDiff = totalTime2 - totalTime;
            var idleDiff = idleTime2 - idleTime;
            var cpuUsage = (1.0 - (idleDiff / totalDiff)) * 100.0;

            return (int)cpuUsage;
        }
        else
        {
            throw new PlatformNotSupportedException("Platform not supported for CPU usage retrieval.");
        }
    }

    public static double GetMemoryUsage()
    {
        if (OperatingSystem.IsWindows())
        {
            var ramCounter = new PerformanceCounter("Memory", "Available MBytes", true);
            var availableMemory = ramCounter.NextValue();

            return Math.Round(availableMemory / 1024, 2);
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            var memInfo = System.IO.File.ReadAllLines("/proc/meminfo");
            var memAvailableLine = memInfo.FirstOrDefault(line => line.StartsWith("MemAvailable:"));

            if (memAvailableLine != null)
            {
                var availableMemoryKB = double.Parse(memAvailableLine.Split(':')[1].Trim().Split(' ')[0]);

                return Math.Round(availableMemoryKB / (1024.0 * 1024.0), 2);
            }
            else
            {
                throw new InvalidOperationException("Unable to read memory information from /proc/meminfo.");
            }
        }
        else
        {
            throw new PlatformNotSupportedException("Platform not supported for memory usage retrieval.");
        }
    }
}

public class MetricCollector : IDisposable
{
    private readonly System.Action _collectMetricsAction;
    private readonly Timer _timer;

    public MetricCollector(System.Action collectMetricsAction, TimeSpan interval = default)
    {
        _collectMetricsAction = collectMetricsAction ?? throw new ArgumentNullException(nameof(collectMetricsAction));
        _timer = new Timer(_ => _collectMetricsAction(), null, TimeSpan.Zero, interval == default ? TimeSpan.FromSeconds(10) : interval);
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}