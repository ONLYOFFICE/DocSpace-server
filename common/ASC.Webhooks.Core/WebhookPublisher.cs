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

using System.Text.Json;
using System.Text.Json.Nodes;

using ASC.Core.Tenants;

using Microsoft.Extensions.DependencyInjection;

namespace ASC.Webhooks.Core;

[Scope(typeof(IWebhookPublisher))]
public class WebhookPublisher(
    IServiceProvider serviceProvider,
    DbWorker dbWorker,
    IEventBus eventBus,
    AuthContext authContext,
    TenantManager tenantManager,
    TenantUtil tenantUtil)
    : IWebhookPublisher
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task PublishAsync<T>(WebhookTrigger trigger, IWebhookAccessChecker<T> checher, T data)
    {
        if (data == null)
        {
            return;
        }

        var webhookConfigs = await dbWorker.GetWebhookConfigs(enabled: true).ToListAsync();

        var requestPayload = JsonSerializer.Serialize(data, _serializerOptions);

        foreach (var config in webhookConfigs)
        {
            if (config.Trigger != WebhookTrigger.All && !config.Trigger.HasFlag(trigger))
            {
                continue;
            }

            if (checher != null && config.CreatedBy.HasValue && authContext.CurrentAccount.ID != config.CreatedBy.Value)
            {
                if (!await checher.CheckAccessAsync(data, config.CreatedBy.Value))
                {
                    continue;
                }
            }

            await PublishAsync(0, requestPayload, config.Id, trigger); //TODO: webhookId
        }
    }

    public async Task PublishAsync(Webhook webhook, string requestPayload, WebhookData webhookData, WebhookTrigger trigger)
    {
        if (string.IsNullOrEmpty(requestPayload))
        {
            return;
        }

        var webhookConfigs = await dbWorker.GetWebhookConfigs(enabled: true).ToListAsync();

        foreach (var config in webhookConfigs)
        {
            if (config.CreatedBy.HasValue)
            {
                if (authContext.CurrentAccount.ID != config.CreatedBy.Value)
                {
                    if (webhookData?.AccessCheckerType == null)
                    {
                        continue;
                    }

                    webhookData.TargetUserId = config.CreatedBy.Value;
                    webhookData.ResponseString = requestPayload;

                    var accessChecker = (IWebhookAccessChecker)serviceProvider.GetRequiredService(webhookData.AccessCheckerType);
                    if (!await accessChecker.CheckAccessAsync(webhookData))
                    {
                        continue;
                    }
                }
            }

            await PublishAsync(webhook.Id, requestPayload, config.Id, trigger);
        }
    }

    public async Task<DbWebhooksLog> PublishAsync(int webhookId, string requestResponse, int configId, WebhookTrigger trigger)
    {
        if (string.IsNullOrEmpty(requestResponse))
        {
            return null;
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        var webhooksLog = new DbWebhooksLog
        {
            WebhookId = webhookId,
            CreationTime = DateTime.UtcNow,
            RequestPayload = requestResponse,
            ConfigId = configId,
            TenantId = tenantId,
            Trigger = trigger,
            Uid = authContext.CurrentAccount.ID
        };

        webhooksLog = await PreProcessWebHookLog(webhooksLog);
        var webhook = await dbWorker.WriteToJournal(webhooksLog);

        var @event = new WebhookRequestIntegrationEvent(authContext.CurrentAccount.ID, tenantId)
        {
            WebhookId = webhook.Id
        };

        await eventBus.PublishAsync(@event);

        return webhook;
    }

    private async Task<DbWebhooksLog> PreProcessWebHookLog(DbWebhooksLog dbWebhooksLog)
    {
        var requestResponse = dbWebhooksLog.RequestPayload;
       
        var webhooksConfig = await dbWorker.GetWebhookConfig(dbWebhooksLog.TenantId, dbWebhooksLog.ConfigId);

        var jsonNode = JsonNode.Parse(requestResponse);
        var data = (jsonNode["response"] ?? jsonNode["data"] ?? jsonNode.Root);

        var requestPayload = new
        {
            Action = new
            {
                //                Id = entry.Id,
                CreateOn = dbWebhooksLog.CreationTime,
                CreateBy = authContext.CurrentAccount.ID,
                Trigger = "*"
            },
            Data = data,
            Webhook = new
            {
                Id = webhooksConfig.Id,
                Name = webhooksConfig.Name,
                PayloadUrl = webhooksConfig.Uri,
                LastSuccessOn = webhooksConfig.LastSuccessOn.HasValue ? tenantUtil.DateTimeFromUtc(webhooksConfig.LastSuccessOn.Value) : new DateTime?(),
                LastFailureOn = webhooksConfig.LastFailureOn.HasValue ? tenantUtil.DateTimeFromUtc(webhooksConfig.LastFailureOn.Value) : new DateTime?(),
                LastFailureContent = webhooksConfig.LastFailureContent,
                RetryOn = new DateTime?(),
                RetryCount = 0,

                //Target = new {
                //  Type = "room", //  room | folder | file,
                //  Id = 111
                //},
                Triggers = "[*]"
            }
        };

        dbWebhooksLog.RequestPayload = JsonSerializer.Serialize(requestPayload, _serializerOptions);

        return dbWebhooksLog;
    }
}
