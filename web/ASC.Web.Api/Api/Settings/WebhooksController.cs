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
    /// <category>Webhooks</category>
    /// <path>api/2.0/settings/webhook</path>
    /// <httpMethod>GET</httpMethod>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.WebhooksConfigDto, ASC.Web.Api">List of tenant webhooks with their config parameters</returns>
    /// <collection>list</collection>
    [Tags("Settings / Webhooks")]
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
    /// <category>Webhooks</category>
    /// <param type="ASC.Web.Api.ApiModels.RequestsDto.WebhooksConfigRequestsDto, ASC.Web.Api" name="inDto">Webhook request parameters</param>
    /// <path>api/2.0/settings/webhook</path>
    /// <httpMethod>POST</httpMethod>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.WebhooksConfigDto, ASC.Web.Api">Tenant webhook with its config parameters</returns>
    [Tags("Settings / Webhooks")]
    [HttpPost("webhook")]
    public async Task<WebhooksConfigDto> CreateWebhook(WebhooksConfigRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        ArgumentNullException.ThrowIfNull(inDto.Uri);
        ArgumentNullException.ThrowIfNull(inDto.SecretKey);
        ArgumentNullException.ThrowIfNull(inDto.Name);

        var webhook = await dbWorker.AddWebhookConfig(inDto.Uri, inDto.Name, inDto.SecretKey, inDto.Enabled, inDto.SSL);

        return mapper.Map<WebhooksConfig, WebhooksConfigDto>(webhook);
    }

    /// <summary>
    /// Updates the tenant webhook with the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Update a webhook
    /// </short>
    /// <category>Webhooks</category>
    /// <param type="ASC.Web.Api.ApiModels.RequestsDto.WebhooksConfigRequestsDto, ASC.Web.Api" name="inDto">New webhook request parameters</param>
    /// <path>api/2.0/settings/webhook</path>
    /// <httpMethod>PUT</httpMethod>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.WebhooksConfigDto, ASC.Web.Api">Updated tenant webhook with its config parameters</returns>
    [Tags("Settings / Webhooks")]
    [HttpPut("webhook")]
    public async Task<WebhooksConfigDto> UpdateWebhook(WebhooksConfigRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        ArgumentNullException.ThrowIfNull(inDto.Uri);
        ArgumentNullException.ThrowIfNull(inDto.Name);

        var webhook = await dbWorker.UpdateWebhookConfig(inDto.Id, inDto.Name, inDto.Uri, inDto.SecretKey, inDto.Enabled, inDto.SSL);

        return mapper.Map<WebhooksConfig, WebhooksConfigDto>(webhook);
    }

    /// <summary>
    /// Removes the tenant webhook with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Remove a webhook
    /// </short>
    /// <category>Webhooks</category>
    /// <param type="System.Int32, System" method="url" name="id">Webhook ID</param>
    /// <path>api/2.0/settings/webhook</path>
    /// <httpMethod>DELETE</httpMethod>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.WebhooksConfigDto, ASC.Web.Api">Tenant webhook with its config parameters</returns>
    [Tags("Settings / Webhooks")]
    [HttpDelete("webhook/{id:int}")]
    public async Task<WebhooksConfigDto> RemoveWebhook(int id)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var webhook = await dbWorker.RemoveWebhookConfigAsync(id);

        return mapper.Map<WebhooksConfig, WebhooksConfigDto>(webhook);
    }

    /// <summary>
    /// Returns the logs of the webhook activities.
    /// </summary>
    /// <short>
    /// Get webhook logs
    /// </short>
    /// <category>Webhooks</category>
    /// <param type="System.Nullable{System.DateTime}, System" name="deliveryFrom">Delivey start time</param>
    /// <param type="System.Nullable{System.DateTime}, System" name="deliveryTo">Delivey end time</param>
    /// <param type="System.String, System" name="hookUri">Hook URI</param>
    /// <param type="System.Nullable{System.Int32}, System" name="webhookId">Webhook ID</param>
    /// <param type="System.Nullable{System.Int32}, System" name="configId">Config ID</param>
    /// <param type="System.Nullable{System.Int32}, System" name="eventId">Event ID</param>
    /// <param type="System.Nullable{ASC.Webhooks.Core.WebhookGroupStatus}, System" name="groupStatus">Webhook group status</param>
    /// <path>api/2.0/settings/webhooks/log</path>
    /// <httpMethod>GET</httpMethod>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.WebhooksLogDto, ASC.Web.Api">Logs of the webhook activities</returns>
    /// <collection>list</collection>
    [Tags("Settings / Webhooks")]
    [HttpGet("webhooks/log")]
    public async IAsyncEnumerable<WebhooksLogDto> GetJournal(DateTime? deliveryFrom, DateTime? deliveryTo, string hookUri, int? webhookId, int? configId, int? eventId, WebhookGroupStatus? groupStatus)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        context.SetTotalCount(await dbWorker.GetTotalByQuery(deliveryFrom, deliveryTo, hookUri, webhookId, configId, eventId, groupStatus));

        var startIndex = Convert.ToInt32(context.StartIndex);
        var count = Convert.ToInt32(context.Count);

        await foreach (var j in dbWorker.ReadJournal(startIndex, count, deliveryFrom, deliveryTo, hookUri, webhookId, configId, eventId, groupStatus))
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
    /// <category>Webhooks</category>
    /// <param type="System.Int32, System" method="url" name="id">Webhook ID</param>
    /// <path>api/2.0/settings/webhook/{id}/retry</path>
    /// <httpMethod>PUT</httpMethod>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.WebhooksLogDto, ASC.Web.Api">Logs of the webhook activities</returns>
    [Tags("Settings / Webhooks")]
    [HttpPut("webhook/{id:int}/retry")]
    public async Task<WebhooksLogDto> RetryWebhook(int id)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (id == 0)
        {
            throw new ArgumentException(nameof(id));
        }

        var item = await dbWorker.ReadJournal(id);

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
    /// <category>Webhooks</category>
    /// <param type="ASC.Web.Api.ApiModels.RequestsDto.WebhookRetryRequestsDto, ASC.Web.Api" name="inDto">Request parameters to retry webhooks</param>
    /// <path>api/2.0/settings/webhook/retry</path>
    /// <httpMethod>PUT</httpMethod>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.WebhooksLogDto, ASC.Web.Api">Logs of the webhook activities</returns>
    /// <collection>list</collection>
    [Tags("Settings / Webhooks")]
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
    /// <category>Webhooks</category>
    /// <path>api/2.0/settings/webhooks</path>
    /// <httpMethod>GET</httpMethod>
    /// <returns type="ASC.Webhooks.Core.Webhook, ASC.Webhooks.Core">List of webhook settings</returns>
    /// <collection>list</collection>
    [Tags("Settings / Webhooks")]
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
    /// <category>Webhooks</category>
    /// <param type="System.Int32, System" method="url" name="id">Webhook ID</param>
    /// <path>api/2.0/settings/webhook/{id}</path>
    /// <httpMethod>PUT</httpMethod>
    /// <returns type="ASC.Webhooks.Core.Webhook, ASC.Webhooks.Core">Webhook settings</returns>
    [Tags("Settings / Webhooks")]
    [HttpPut("webhook/{id:int}")]
    public async Task<Webhook> DisableWebHook(int id)
    {
        var settings = await settingsManager.LoadAsync<WebHooksSettings>();

        Webhook result = null;

        if (!settings.Ids.Contains(id) && (result = await dbWorker.GetWebhookAsync(id)) != null)
        {
            settings.Ids.Add(id);
        }

        if (result != null)
        {
            await settingsManager.SaveAsync(settings);
        }

        return result;
    }
}