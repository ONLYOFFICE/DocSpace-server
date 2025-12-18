// (c) Copyright Ascensio System SIA 2009-2025
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
public class WebhookSender(
    ILogger<WebhookSender> logger,
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory clientFactory,
    ResiliencePipelineProvider<string> resiliencePipelineProvider,
    Settings settings)
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IgnoreReadOnlyProperties = true
    };

    private const string SignatureHeader = "x-docspace-signature-256";

    public const string WebhookClientName = "webhookClientName ";
    public const string WebhookClientNameSkipSSL = "webhookClientNameSkipSSL";
    public const string WebhookPipelineName = "webhookResiliencePipeline";

    public static ResiliencePropertyKey<int> RetryCountPropKey = new("retryCount");
    public static ResiliencePropertyKey<string> ErrorMessagePropKey = new("errorMessage");

    public async Task Send(WebhookRequestIntegrationEvent webhookRequest, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbWorker = scope.ServiceProvider.GetRequiredService<DbWorker>();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var messageService = scope.ServiceProvider.GetRequiredService<MessageService>();

            await tenantManager.SetCurrentTenantAsync(webhookRequest.TenantId);

            var entry = await dbWorker.ReadJournal(webhookRequest.TenantId, webhookRequest.WebhookLogId);

            if (entry == null)
            {
                return;
            }

            var webhookPayload = JsonSerializer.Deserialize<WebhookPayload<object, object>>(entry.RequestPayload, _jsonSerializerOptions);

            // trying to send an old webhook
            if (webhookPayload?.Event == null || webhookPayload?.Webhook == null)
            {
                webhookPayload = new WebhookPayload<object, object>(WebhookTrigger.All, entry.Config, entry.RequestPayload, null, webhookRequest.CreateBy);
            }

            webhookPayload.Event.Id = entry.Id;
            webhookPayload.Webhook.LastFailureOn = null;
            webhookPayload.Webhook.LastFailureContent = null;
            webhookPayload.Webhook.LastSuccessOn = null;
            webhookPayload.Webhook.RetryCount = 0;
            webhookPayload.Webhook.RetryOn = null;

            var status = 0;
            DateTime? delivery = null;
            var requestDate = webhookPayload.GetShortUtcNow();
            string responsePayload;
            string responseHeaders = null;
            var requestPayload = JsonSerializer.Serialize(webhookPayload, _jsonSerializerOptions);
            string requestHeaders = null;

            var clientName = entry.Config.SSL ? WebhookClientName : WebhookClientNameSkipSSL;
            var httpClient = clientFactory.CreateClient(clientName);

            var context = ResilienceContextPool.Shared.Get(cancellationToken);

            try
            {
                context.Properties.Set(RetryCountPropKey, 0);
                context.Properties.Set(ErrorMessagePropKey, "");

                var pipeline = resiliencePipelineProvider.GetPipeline<HttpResponseMessage>(WebhookPipelineName);

                var response = await pipeline.ExecuteAsync(async context =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, entry.Config.Uri);

                    var retryCount = context.Properties.GetValue(RetryCountPropKey, 0);

                    if (retryCount > 0)
                    {
                        webhookPayload.Webhook.LastFailureOn = webhookPayload.Webhook.RetryOn ?? requestDate;
                        webhookPayload.Webhook.LastFailureContent = context.Properties.GetValue(ErrorMessagePropKey, "");
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
                }, context);

                status = (int)response.StatusCode;
                responseHeaders = JsonSerializer.Serialize(response.Headers.ToDictionary(r => r.Key, v => v.Value), _jsonSerializerOptions);
                responsePayload = await response.Content.ReadAsStringAsync(cancellationToken);

                entry.Config.LastSuccessOn = delivery = DateTime.UtcNow;

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

                var lastSuccessOn = entry.Config.LastSuccessOn ?? entry.Config.CreatedOn;

                if (lastSuccessOn.HasValue && entry.Config.LastFailureOn - lastSuccessOn.Value > TimeSpan.FromDays(settings.TrustedDaysCount ?? 3))
                {
                    entry.Config.Enabled = false;
                }

                logger.WarningWithException(e);
            }
            catch (Exception e)
            {
                responsePayload = e.Message;

                entry.Config.LastFailureContent = e.Message;
                entry.Config.LastFailureOn = DateTime.UtcNow;

                status = (int)HttpStatusCode.InternalServerError;
                logger.ErrorWithException(e);
            }
            finally
            {
                ResilienceContextPool.Shared.Return(context);
            }

            var configDisabled = !entry.Config.Enabled;

            await dbWorker.UpdateWebhookJournal(entry.Id, entry.TenantId, status, delivery, requestPayload, requestHeaders, responsePayload, responseHeaders);
            await dbWorker.UpdateWebhookConfig(entry.Config, configDisabled);

            if (configDisabled)
            {
                messageService.SendHeadersMessage(MessageAction.WebhookUpdated, MessageTarget.Create(entry.ConfigId), null, $"{entry.Config.Name} (more than {settings.TrustedDaysCount} days without success)");
            }
        }
        catch (Exception e)
        {
            logger.Error($"Failed to send webhook tenant: {webhookRequest.TenantId}, user: {webhookRequest.CreateBy}, log: {webhookRequest.WebhookLogId}");
            logger.ErrorWithException(e);
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


public static class WebhookSenderExtension
{
    public static void AddWebhookSenderHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        var lifeTime = TimeSpan.FromMinutes(5);
        var repeatCount = Convert.ToInt32(configuration["webhooks:repeatcount"] ?? "5");

        services.AddHttpClient(WebhookSender.WebhookClientName)
            .SetHandlerLifetime(lifeTime);

        services.AddHttpClient(WebhookSender.WebhookClientNameSkipSSL)
            .SetHandlerLifetime(lifeTime)
            .ConfigurePrimaryHttpMessageHandler(_ =>
            {
                return new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
            });

        services.AddResiliencePipeline<string, HttpResponseMessage>(WebhookSender.WebhookPipelineName, pipelineBuilder =>
        {
            pipelineBuilder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = repeatCount,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>()
                        .HandleResult(response => !response.IsSuccessStatusCode),
                OnRetry = args =>
                {
                    args.Context.Properties.Set(WebhookSender.RetryCountPropKey, args.AttemptNumber + 1);
                    args.Context.Properties.Set(WebhookSender.ErrorMessagePropKey, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            });
        });
    }
}