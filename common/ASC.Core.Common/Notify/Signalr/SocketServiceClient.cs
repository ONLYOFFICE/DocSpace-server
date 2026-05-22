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

using System.Threading.Channels;

using ASC.Common.Threading;

namespace ASC.Core.Notify.Socket;

[Scope]
public class SocketServiceClient(
    ITariffService tariffService,
    TenantManager tenantManager,
    ChannelWriter<SocketData> channelWriter,
    MachinePseudoKeys machinePseudoKeys,
    IConfiguration configuration)
{
    public const string HttpClientName = "socketio";

    protected readonly TenantManager _tenantManager = tenantManager;
    private byte[] SKey => machinePseudoKeys.GetMachineConstant();
    private string Url => configuration["web:hub:internal"];

    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static string _cachedToken;
    private static DateTime _cachedTokenExpires;
    private static readonly Lock _tokenLock = new();

    protected virtual string Hub => "default";

    protected async Task MakeRequest(string method, object data, int? tenantId = null)
    {
        if (string.IsNullOrEmpty(Url))
        {
            return;
        }

        // CA2000: HttpRequestMessage is disposed by HttpClient.SendAsync in SocketService.ExecuteAsync
#pragma warning disable CA2000
        var request = GenerateRequest(method, data);
#pragma warning restore CA2000
        if (await channelWriter.WaitToWriteAsync())
        {
            var tenant = tenantId ?? _tenantManager.GetCurrentTenantId();

            var tariff = await tariffService.GetTariffAsync(tenant);
            await channelWriter.WriteAsync(new SocketData(request, tariff.State));
        }
    }

    private HttpRequestMessage GenerateRequest(string method, object data)
    {
        var jsonData = JsonSerializer.Serialize(data, _options);
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(Method(method)),
            Content = new StringContent(jsonData, Encoding.UTF8, "application/json")
        };

        request.Headers.Add("Authorization", CreateAuthToken());
        return request;
    }

    private string Method(string method)
    {
        return $"{Url.TrimEnd('/')}/controller/{Hub}/{method}";
    }

    private string CreateAuthToken(string pkey = "socketio")
    {
        lock (_tokenLock)
        {
            if (_cachedToken is not null && DateTime.UtcNow < _cachedTokenExpires)
            {
                return _cachedToken;
            }

            using var hasher = new HMACSHA1(SKey);
            var now = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            var hash = Convert.ToBase64String(hasher.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", now, pkey))));
            _cachedToken = $"ASC {pkey}:{now}:{hash}";
            _cachedTokenExpires = DateTime.UtcNow.AddMinutes(4);

            return _cachedToken;
        }
    }
}

[Singleton]
public class SocketService(
    ILogger<SocketServiceClient> logger,
    IHttpClientFactory clientFactory,
    ChannelReader<SocketData> channelReader,
    IConfiguration configuration
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!int.TryParse(configuration["web:hub:maxDegreeOfParallelism"], out var maxDegreeOfParallelism))
        {
            maxDegreeOfParallelism = 10;
        }

        List<ChannelReader<SocketData>> readers = [channelReader];

        if ((int)(maxDegreeOfParallelism * 0.3) > 0)
        {
            var splitter = channelReader.Split(2, (_, _, p) => p.TariffState == TariffState.Paid ? 0 : 1, stoppingToken);
            var premiumChannels = splitter[0].Split((int)(maxDegreeOfParallelism * 0.7), null, stoppingToken);
            var freeChannel = splitter[1].Split((int)(maxDegreeOfParallelism * 0.3), null, stoppingToken);
            readers = premiumChannels.Union(freeChannel).ToList();
        }

        var tasks = readers.Select(reader1 => Task.Run(async () =>
            {
                await foreach (var socketData in reader1.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        var httpClient = clientFactory.CreateClient(SocketServiceClient.HttpClientName);
                        await httpClient.SendAsync(socketData.RequestMessage, HttpCompletionOption.ResponseHeadersRead, stoppingToken);
                    }
                    catch (Exception e)
                    {
                        logger.ErrorService(e);
                    }
                    finally
                    {
                        socketData.RequestMessage.Dispose();
                    }

                }
            }, stoppingToken))
            .ToList();

        await Task.WhenAll(tasks);
    }
}

public static class SocketHttpClientExtension
{
    public static void AddSocketHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        var url = configuration["web:hub:internal"];

        services.AddHttpClient(SocketServiceClient.HttpClientName, client =>
            {
                if (!string.IsNullOrEmpty(url))
                {
                    client.BaseAddress = new Uri(url);
                }

                client.Timeout = TimeSpan.FromSeconds(5);
                client.DefaultRequestHeaders.ConnectionClose = false;
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));
    }
}

public record SocketData(HttpRequestMessage RequestMessage, TariffState TariffState);
