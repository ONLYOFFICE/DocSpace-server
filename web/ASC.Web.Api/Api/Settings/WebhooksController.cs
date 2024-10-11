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

namespace ASC.Web.Api.Controllers.Settings;

public class WebhooksController(ApiContext context,
        PermissionContext permissionContext,
        ApiContext apiContext,
        WebItemManager webItemManager,
        IMemoryCache memoryCache,
        DbWorker dbWorker,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        WebhookPublisher webhookPublisher,
        SettingsManager settingsManager)
    : BaseSettingsController(apiContext, memoryCache, webItemManager, httpContextAccessor)
{
    /// <summary>
    /// Returns a list of the tenant webhooks.
    /// </summary>
    /// <short>
    /// Get webhooks
    /// </short>
    /// <path>api/2.0/settings/webhook</path>
    /// <collection>list</collection>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "List of tenant webhooks with their config parameters", typeof(WebhooksConfigWithStatusDto))]
    [HttpGet("webhook")]
    public async IAsyncEnumerable<WebhooksConfigWithStatusDto> GetTenantWebhooks()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await foreach (var webhook in dbWorker.GetTenantWebhooksWithStatus())
        {
            yield return mapper.Map<WebhooksConfigWithStatusDto>(webhook);
        }
    }

    /// <summary>
    /// Creates a new tenant webhook with the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Create a webhook
    /// </short>
    /// <path>api/2.0/settings/webhook</path>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "Tenant webhook with its config parameters", typeof(WebhooksConfigDto))]
    [HttpPost("webhook")]
    public async Task<WebhooksConfigDto> CreateWebhook(WebhooksConfigRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        ArgumentNullException.ThrowIfNull(inDto.SecretKey);

        var webhook = await dbWorker.AddWebhookConfig(inDto.Uri, inDto.Name, inDto.SecretKey, inDto.Enabled, inDto.SSL);

        return mapper.Map<WebhooksConfig, WebhooksConfigDto>(webhook);
    }

    /// <summary>
    /// Updates the tenant webhook with the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Update a webhook
    /// </short>
    /// <path>api/2.0/settings/webhook</path>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "Updated tenant webhook with its config parameters", typeof(WebhooksConfigDto))]
    [HttpPut("webhook")]
    public async Task<WebhooksConfigDto> UpdateWebhook(WebhooksConfigRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var webhook = await dbWorker.UpdateWebhookConfig(inDto.Id, inDto.Name, inDto.Uri, inDto.SecretKey, inDto.Enabled, inDto.SSL);

        return mapper.Map<WebhooksConfig, WebhooksConfigDto>(webhook);
    }

    /// <summary>
    /// Removes the tenant webhook with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Remove a webhook
    /// </short>
    /// <path>api/2.0/settings/webhook</path>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "Tenant webhook with its config parameters", typeof(WebhooksConfigDto))]
    [HttpDelete("webhook/{id:int}")]
    public async Task<WebhooksConfigDto> RemoveWebhook(IdRequestDto<int> inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var webhook = await dbWorker.RemoveWebhookConfigAsync(inDto.Id);

        return mapper.Map<WebhooksConfig, WebhooksConfigDto>(webhook);
    }

    /// <summary>
    /// Returns the logs of the webhook activities.
    /// </summary>
    /// <short>
    /// Get webhook logs
    /// </short>
    /// <path>api/2.0/settings/webhooks/log</path>
    /// <collection>list</collection>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "Logs of the webhook activities", typeof(WebhooksLogDto))]
    [HttpGet("webhooks/log")]
    public async IAsyncEnumerable<WebhooksLogDto> GetJournal(WebhookLogsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        context.SetTotalCount(await dbWorker.GetTotalByQuery(inDto.DeliveryFrom, inDto.DeliveryTo, inDto.HookUri, inDto.WebhookId, inDto.ConfigId, inDto.EventId, inDto.GroupStatus));

        var startIndex = Convert.ToInt32(context.StartIndex);
        var count = Convert.ToInt32(context.Count);

        await foreach (var j in dbWorker.ReadJournal(startIndex, count, inDto.DeliveryFrom, inDto.DeliveryTo, inDto.HookUri, inDto.WebhookId, inDto.ConfigId, inDto.EventId, inDto.GroupStatus))
        {
            j.Log.Config = j.Config;
            yield return mapper.Map<WebhooksLog, WebhooksLogDto>(j.Log);
        }
    }

    /// <summary>
    /// Retries a webhook with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Retry a webhook
    /// </short>
    /// <path>api/2.0/settings/webhook/{id}/retry</path>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "Logs of the webhook activities", typeof(WebhooksLogDto))]
    [SwaggerResponse(400, "Id incorrect")]
    [SwaggerResponse(404, "Item not found")]
    [HttpPut("webhook/{id:int}/retry")]
    public async Task<WebhooksLogDto> RetryWebhook(IdRequestDto<int> inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (inDto.Id == 0)
        {
            throw new ArgumentException(nameof(inDto.Id));
        }

        var item = await dbWorker.ReadJournal(inDto.Id);

        if (item == null)
        {
            throw new ItemNotFoundException();
        }

        var result = await webhookPublisher.PublishAsync(item.Id, item.RequestPayload, item.ConfigId);

        return mapper.Map<WebhooksLog, WebhooksLogDto>(result);
    }

    /// <summary>
    /// Retries all the webhooks with the IDs specified in the request.
    /// </summary>
    /// <short>
    /// Retry webhooks
    /// </short>
    /// <path>api/2.0/settings/webhook/retry</path>
    /// <collection>list</collection>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "Logs of the webhook activities", typeof(WebhooksLogDto))]
    [HttpPut("webhook/retry")]
    public async IAsyncEnumerable<WebhooksLogDto> RetryWebhooks(WebhookRetryRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        foreach (var id in inDto.Ids)
        {
            var item = await dbWorker.ReadJournal(id);

            if (item == null)
            {
                continue;
            }

            var result = await webhookPublisher.PublishAsync(item.Id, item.RequestPayload, item.ConfigId);

            yield return mapper.Map<WebhooksLog, WebhooksLogDto>(result);
        }
    }

    /// <summary>
    /// Returns settings of all webhooks.
    /// </summary>
    /// <short>
    /// Get webhook settings
    /// </short>
    /// <path>api/2.0/settings/webhooks</path>
    /// <collection>list</collection>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "List of webhook settings", typeof(Webhook))]
    [HttpGet("webhooks")]
    public async IAsyncEnumerable<Webhook> Settings()
    {
        var settings = await settingsManager.LoadAsync<WebHooksSettings>();

        foreach (var w in await dbWorker.GetWebhooksAsync())
        {
            w.Disable = settings.Ids.Contains(w.Id);
            yield return w;
        }
    }

    /// <summary>
    /// Disables a webhook with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Disable a webhook
    /// </short>
    /// <path>api/2.0/settings/webhook/{id}</path>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "Webhook settings", typeof(Webhook))]
    [HttpPut("webhook/{id:int}")]
    public async Task<Webhook> DisableWebHook(IdRequestDto<int> inDto)
    {
        var settings = await settingsManager.LoadAsync<WebHooksSettings>();

        Webhook result = null;

        if (!settings.Ids.Contains(inDto.Id) && (result = await dbWorker.GetWebhookAsync(inDto.Id)) != null)
        {
            settings.Ids.Add(inDto.Id);
        }

        if (result != null)
        {
            await settingsManager.SaveAsync(settings);
        }

        return result;
    }
}