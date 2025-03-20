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

using ASC.Core;
using ASC.MessagingSystem.Core;
using ASC.MessagingSystem.EF.Model;

namespace ASC.Webhooks;

[Singleton]
public class WebhookSender(ILogger<WebhookSender> logger, IServiceScopeFactory scopeFactory, IHttpClientFactory clientFactory, Settings settings)
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IgnoreReadOnlyProperties = true
    };

    private const string SignatureHeader = "x-docspace-signature-256";

    public const string WEBHOOK = "webhook";
    public const string WEBHOOK_SKIP_SSL = "webhookSkipSSL";

    public async Task Send(WebhookRequestIntegrationEvent webhookRequest, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbWorker = scope.ServiceProvider.GetRequiredService<DbWorker>();
        var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
        var messageService = scope.ServiceProvider.GetRequiredService<MessageService>();

        await tenantManager.SetCurrentTenantAsync(webhookRequest.TenantId);

        var entry = await dbWorker.ReadJournal(webhookRequest.WebhookLogId);

        var webhookPayload = JsonSerializer.Deserialize<WebhookPayload<object, object>>(entry.RequestPayload, _jsonSerializerOptions);
        webhookPayload.Event.Id = entry.Id;
        webhookPayload.Webhook.RetryCount = 0;
        webhookPayload.Webhook.RetryOn = webhookPayload.GetShortUtcNow();

        var status = 0;
        string responsePayload = null;
        string responseHeaders = null;
        string requestPayload = JsonSerializer.Serialize(webhookPayload, _jsonSerializerOptions);
        string requestHeaders = null;

        var clientName = entry.Config.SSL ? WEBHOOK : WEBHOOK_SKIP_SSL;
        var httpClient = clientFactory.CreateClient(clientName);
        var policy = HttpPolicyExtensions.HandleTransientHttpError()
                                          .OrResult(x => x.StatusCode != HttpStatusCode.OK)
                                          .WaitAndRetryAsync(settings.RepeatCount ?? 5,
                                                             retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                                                             onRetry: (response, delay, retryCount, context) =>
                                                             {
                                                                 context["retryCount"] = retryCount;
                                                                 context["errorMessage"] = response.Exception?.Message;
                                                             });
        try
        {
            var response = await policy.ExecuteAsync(async (context) =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, entry.Config.Uri);

                var retryCount = (int)context["retryCount"];

                if (retryCount > 0)
                {
                    webhookPayload.Webhook.LastFailureOn = webhookPayload.Webhook.RetryOn;
                    webhookPayload.Webhook.LastFailureContent = (string)context["errorMessage"];
                    webhookPayload.Webhook.LastSuccessOn = entry.Config.LastSuccessOn;

                    webhookPayload.Webhook.RetryCount = retryCount;
                    webhookPayload.Webhook.RetryOn = webhookPayload.GetShortUtcNow();

                    requestPayload = JsonSerializer.Serialize(webhookPayload, _jsonSerializerOptions);
                }

                request.Headers.Add("Accept", "*/*");
                request.Headers.Add(SignatureHeader, $"sha256={GetSecretHash(entry.Config.SecretKey, requestPayload)}");

                request.Content = new StringContent(requestPayload, Encoding.UTF8, "application/json");

                requestHeaders = JsonSerializer.Serialize(request.Headers.ToDictionary(r => r.Key, v => v.Value), _jsonSerializerOptions);

                var response = await httpClient.SendAsync(request, cancellationToken);

                response.EnsureSuccessStatusCode();

                return response;
            }, new Context { { "retryCount", 0 }, { "errorMessage", "" } });

            status = (int)response.StatusCode;
            responseHeaders = JsonSerializer.Serialize(response.Headers.ToDictionary(r => r.Key, v => v.Value), _jsonSerializerOptions);
            responsePayload = await response.Content.ReadAsStringAsync(cancellationToken);

            entry.Config.LastSuccessOn = DateTime.UtcNow;

            logger.DebugResponse(response);
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
                messageService.SendHeadersMessage(MessageAction.WebhookDeleted, MessageTarget.Create(entry.ConfigId), null, $"{entry.Config.Name} (HTTP status 410)");
                return;
            }

            responsePayload = e.Message;

            entry.Config.LastFailureContent = e.Message;
            entry.Config.LastFailureOn = DateTime.UtcNow;

            if (entry.Config.LastSuccessOn.HasValue &&
                (entry.Config.LastFailureOn - entry.Config.LastSuccessOn.Value > TimeSpan.FromDays(settings.TrustedDaysCount ?? 3)))
            {
                entry.Config.Enabled = false;
            }

            logger.ErrorWithException(e);
        }
        catch (Exception e)
        {
            responsePayload = e.Message;

            entry.Config.LastFailureContent = e.Message;
            entry.Config.LastFailureOn = DateTime.UtcNow;

            status = (int)HttpStatusCode.InternalServerError;
            logger.ErrorWithException(e);
        }

        await dbWorker.UpdateWebhookJournal(entry.Id, status, delivery: DateTime.UtcNow, requestPayload, requestHeaders, responsePayload, responseHeaders);
        await dbWorker.UpdateWebhookConfig(entry.Config);

        if (!entry.Config.Enabled)
        {
            messageService.SendHeadersMessage(MessageAction.WebhookUpdated, MessageTarget.Create(entry.ConfigId), null, $"{entry.Config.Name} (more than {settings.TrustedDaysCount} days without success)");
        }
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