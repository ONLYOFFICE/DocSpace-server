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

namespace ASC.Webhooks.Core;

[Scope(typeof(IWebhookPublisher))]
public class WebhookPublisher(
    DbWorker dbWorker,
    IEventBus eventBus,
    AuthContext authContext)
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

        foreach (var config in webhookConfigs)
        {
            if (config.Triggers != WebhookTrigger.All && !config.Triggers.HasFlag(trigger))
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

            _ = await PublishAsync(trigger, config, data);
        }
    }

    private async Task<DbWebhooksLog> PublishAsync<T>(WebhookTrigger trigger, DbWebhooksConfig webhookConfig, T data)
    {
        var payload = new WebhookPayload<T>(trigger, webhookConfig, data, authContext.CurrentAccount.ID);

        var payloadStr = JsonSerializer.Serialize(payload, _serializerOptions);

        var webhooksLog = new DbWebhooksLog
        {
            TenantId = webhookConfig.TenantId,
            ConfigId = webhookConfig.Id,
            Uid = authContext.CurrentAccount.ID,
            Trigger = trigger,
            CreationTime = DateTime.UtcNow,
            WebhookId = 0,
            RequestPayload = payloadStr
        };

        return await PublishAsync(webhooksLog);
    }

    public async Task<DbWebhooksLog> RetryPublishAsync(DbWebhooksLog webhookLog)
    {
        var webhooksLog = new DbWebhooksLog
        {
            TenantId = webhookLog.TenantId,
            ConfigId = webhookLog.ConfigId,
            Uid = authContext.CurrentAccount.ID,
            Trigger = webhookLog.Trigger,
            CreationTime = DateTime.UtcNow,
            WebhookId = webhookLog.WebhookId,
            RequestPayload = webhookLog.RequestPayload
        };

        return await PublishAsync(webhooksLog);
    }

    private async Task<DbWebhooksLog> PublishAsync(DbWebhooksLog webhookLog)
    {
        var newWebhooksLog = await dbWorker.WriteToJournal(webhookLog);

        var @event = new WebhookRequestIntegrationEvent(authContext.CurrentAccount.ID, newWebhooksLog.TenantId, newWebhooksLog.Id);

        await eventBus.PublishAsync(@event);

        return newWebhooksLog;
    }
}
