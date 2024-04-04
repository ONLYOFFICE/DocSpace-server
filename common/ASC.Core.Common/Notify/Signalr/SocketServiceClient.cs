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

using System.Threading.Channels;

using ASC.Common.Threading;

namespace ASC.Core.Notify.Socket;

[Scope]
public class SocketServiceClient(
    ChannelWriter<SocketData> channelWriter,
    MachinePseudoKeys machinePseudoKeys,
    IConfiguration configuration)
{
    private byte[] SKey => machinePseudoKeys.GetMachineConstant();
    private string Url =>  configuration["web:hub:internal"];

    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    protected virtual string Hub { get => "default"; }

    public async Task MakeRequest(string method, object data)
    {
        if (string.IsNullOrEmpty(Url))
        {
            return;
        }

        var request = GenerateRequest(method, data);
        if (await channelWriter.WaitToWriteAsync())
        {
            await channelWriter.WriteAsync(new SocketData(request, TariffState.Paid));
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
        using var hasher = new HMACSHA1(SKey);
        var now = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var hash = Convert.ToBase64String(hasher.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", now, pkey))));
        return $"ASC {pkey}:{now}:{hash}";
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
        if(!int.TryParse(configuration["web:hub:maxDegreeOfParallelism"], out var maxDegreeOfParallelism))
        {
            maxDegreeOfParallelism = 10;
        }

        List<ChannelReader<SocketData>> readers = [channelReader];

        if (((int)(maxDegreeOfParallelism * 0.3)) > 0)
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
                        var httpClient = clientFactory.CreateClient();
                        await httpClient.SendAsync(socketData.RequestMessage, HttpCompletionOption.ResponseHeadersRead, stoppingToken);
                    }
                    catch (Exception e)
                    {
                        logger.ErrorService(e);
                    }

                }
            }, stoppingToken))
            .ToList();

        await Task.WhenAll(tasks);
    }
}

public record SocketData(HttpRequestMessage RequestMessage, TariffState TariffState);
