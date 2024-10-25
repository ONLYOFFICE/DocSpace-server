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

using System.Text.Json.Nodes;

using ASC.Core;
using ASC.Core.Tenants;

namespace ASC.Webhooks;

[Singleton]
public class WebhookSender(ILoggerProvider options, IServiceScopeFactory scopeFactory, IHttpClientFactory clientFactory)
{
    private readonly ILogger _log = options.CreateLogger("ASC.Webhooks.Core");

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IgnoreReadOnlyProperties = true
    };
    private const string SignatureHeader = "x-docspace-signature-256";

    public const string WEBHOOK = "webhook";
    public const string WEBHOOK_SKIP_SSL = "webhookSkipSSL";

    public async Task Send(WebhookRequestIntegrationEvent webhookRequest, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbWorker = scope.ServiceProvider.GetRequiredService<DbWorker>();
        var apiDateTimeHelper = scope.ServiceProvider.GetRequiredService<ApiDateTimeHelper>();
        var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
        var tenantUtil = scope.ServiceProvider.GetRequiredService<TenantUtil>();

        await tenantManager.SetCurrentTenantAsync(webhookRequest.TenantId);


        var entry = await dbWorker.ReadJournal(webhookRequest.WebhookId);
        var webhooksConfig = await dbWorker.GetWebhookConfig(webhookRequest.TenantId, entry.ConfigId);
        var ssl = entry.Config.SSL;

        int status = 0;
        string responsePayload = null;
        string responseHeaders = null;
        string requestPayload = null;
        string requestHeaders = null;

        var clientName = ssl ? WEBHOOK : WEBHOOK_SKIP_SSL;
        var httpClient = clientFactory.CreateClient(clientName);
        var settings = scope.ServiceProvider.GetRequiredService<Settings>();
        var policy = HttpPolicyExtensions.HandleTransientHttpError()
                                          .OrResult(x => x.StatusCode != HttpStatusCode.OK)
                                          .WaitAndRetryAsync(settings.RepeatCount ?? 5,
                                                             retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                                                             onRetry: (response, delay, retryCount, context) =>
                                                             {
                                                                 context["retryCount"] = retryCount;

                                                             });
        try
        {        
            var response = await policy.ExecuteAsync(async (context) =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, entry.Config.Uri);

                request.Headers.Add("Accept", "*/*");
                request.Headers.Add(SignatureHeader, $"sha256={GetSecretHash(entry.Config.SecretKey, entry.RequestPayload)}");

                requestPayload = entry.RequestPayload;
                var retryCount = (int)context["retryCount"];
                
                if (retryCount > 0)
                {
                    var jsonNode = JsonNode.Parse(requestPayload);

                    jsonNode["webhook"]["retryCount"] = retryCount;
                    jsonNode["webhook"]["retryOn"] = apiDateTimeHelper.Get(tenantUtil.DateTimeNow()).ToString();
                                      
                    requestPayload = jsonNode.ToString();
                }

                request.Content = new StringContent(requestPayload, Encoding.UTF8, "application/json");

                requestHeaders = JsonSerializer.Serialize(request.Headers.ToDictionary(r => r.Key, v => v.Value), _jsonSerializerOptions);
               
                var response = await httpClient.SendAsync(request, cancellationToken);

                response.EnsureSuccessStatusCode();

                return response;
            }, new Polly.Context { { "retryCount", 0 } });

            status = (int)response.StatusCode;
            responseHeaders = JsonSerializer.Serialize(response.Headers.ToDictionary(r => r.Key, v => v.Value), _jsonSerializerOptions);
            responsePayload = await response.Content.ReadAsStringAsync(cancellationToken);

            webhooksConfig.LastSuccessOn = DateTime.UtcNow;

            _log.DebugResponse(response);
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode.HasValue)
            {
                status = (int)e.StatusCode.Value;
            }

            if (e.StatusCode == HttpStatusCode.Gone)
            {
                await dbWorker.RemoveWebhookConfigAsync(entry.ConfigId);

                return;
            }

            var lastFailureOn = DateTime.UtcNow;
            var lastFailureContent = e.Message;

            if ((webhooksConfig.LastSuccessOn.HasValue) &&
                (lastFailureOn - webhooksConfig.LastSuccessOn.Value > TimeSpan.FromDays(3)))
            {
                await dbWorker.RemoveWebhookConfigAsync(entry.ConfigId);

                return;
            }

            webhooksConfig.LastFailureContent = lastFailureContent;
            webhooksConfig.LastFailureOn = lastFailureOn;
            responsePayload = e.Message;

            _log.ErrorWithException(e);
        }
        catch (Exception e)
        {
            webhooksConfig.LastFailureContent = e.Message;
            webhooksConfig.LastFailureOn = DateTime.UtcNow;

            status = (int)HttpStatusCode.InternalServerError;
            _log.ErrorWithException(e);
        }

        var delivery = DateTime.UtcNow;

        await dbWorker.UpdateWebhookJournal(entry.Id, status, delivery, requestPayload, requestHeaders, responsePayload, responseHeaders);
        await dbWorker.UpdateWebhookConfig(webhooksConfig);
    }

    private string GetSecretHash(string secretKey, string body)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secretKey);

        using var hasher = new HMACSHA256(secretBytes);

        var data = Encoding.UTF8.GetBytes(body);
        var hash = hasher.ComputeHash(data);

        return Convert.ToHexString(hash);
    }
  }