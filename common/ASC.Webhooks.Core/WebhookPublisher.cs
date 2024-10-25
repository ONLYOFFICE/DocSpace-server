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

using ASC.Core.Tenants;
using ASC.Web.Webhooks;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ASC.Webhooks.Core;

[Scope(typeof(IWebhookPublisher))]
public class WebhookPublisher(
    DbWorker dbWorker,
    IEventBus eventBus,
    SecurityContext securityContext,
    TenantManager tenantManager,
    TenantUtil tenantUtil)
    : IWebhookPublisher
{
    public async Task PublishAsync(int webhookId, string requestPayload)
    {
        if (string.IsNullOrEmpty(requestPayload))
        {
            return;
        }

        var webhookConfigs = await dbWorker.GetWebhookConfigs().Where(r => r.Enabled).ToListAsync();

        foreach (var config in webhookConfigs)
        {
            await PublishAsync(webhookId, requestPayload, config.Id);
        }
    }

    public async Task<DbWebhooksLog> PublishAsync(int webhookId, string requestResponse, int configId)
    {
        if (string.IsNullOrEmpty(requestResponse))
        {
            return null;
        }

        var tenantId = (await tenantManager.GetCurrentTenantAsync()).Id;

        var webhooksLog = new DbWebhooksLog
        {
            WebhookId = webhookId,
            CreationTime = DateTime.UtcNow,
            RequestPayload = requestResponse,
            ConfigId = configId,
            TenantId = tenantId
        };

        webhooksLog = await PreProcessWebHookLog(webhooksLog);        
        var webhook = await dbWorker.WriteToJournal(webhooksLog);

        var @event = new WebhookRequestIntegrationEvent(securityContext.CurrentAccount.ID, tenantId)
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
                CreateBy = securityContext.CurrentAccount.ID,
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

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase            
        };

        dbWebhooksLog.RequestPayload = JsonSerializer.Serialize(requestPayload, jsonSerializerOptions);

        return dbWebhooksLog;
    }
}
