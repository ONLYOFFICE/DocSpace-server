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

using Microsoft.AspNetCore.RateLimiting;

namespace ASC.Web.Api.Controllers.Settings;

[WebhookDisable]
public class WebhooksController(
    ApiContext context,
    AuthContext authContext,
    WebItemManager webItemManager,
    IFusionCache fusionCache,
    DbWorker dbWorker,
    TenantManager tenantManager,
    UserManager userManager,
    WebhooksLogDtoMapper mapper,
    IWebhookPublisher webhookPublisher,
    MessageService messageService,
    SettingsManager settingsManager,
    PasswordSettingsManager passwordSettingsManager,
    IHttpClientFactory clientFactory,
    IConfiguration configuration,
    WebhooksConfigDtoHelper webhooksConfigDtoHelper)
    : BaseSettingsController(fusionCache, webItemManager)
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
    [SwaggerResponse(200, "List of tenant webhooks with their config parameters", typeof(IAsyncEnumerable<WebhooksConfigWithStatusDto>))]
    [HttpGet("webhook")]
    public async IAsyncEnumerable<WebhooksConfigWithStatusDto> GetTenantWebhooks()
    {
        Guid? userId = await CheckAdminPermissionsAsync() ? null : authContext.CurrentAccount.ID;

        await foreach (var webhook in dbWorker.GetTenantWebhooksWithStatus(userId))
        {
            yield return await webhooksConfigDtoHelper.GetAsync(webhook);
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
    public async Task<WebhooksConfigDto> CreateWebhook(CreateWebhooksConfigRequestsDto inDto)
    {
        _ = await CheckAdminPermissionsAsync();

        await CheckWebhook(inDto.Name, inDto.Uri, inDto.SecretKey, inDto.SSL, true);

        var webhook = await dbWorker.AddWebhookConfig(inDto.Name, inDto.Uri, inDto.SecretKey, inDto.Enabled, inDto.SSL, inDto.Triggers, inDto.TargetId);

        messageService.Send(MessageAction.WebhookCreated, MessageTarget.Create(webhook.Id), webhook.Name);

        return await webhooksConfigDtoHelper.GetAsync(webhook);
    }

    /// <summary>
    /// Updates a tenant webhook with the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Update a webhook
    /// </short>
    /// <path>api/2.0/settings/webhook</path>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "Updated tenant webhook with its config parameters", typeof(WebhooksConfigDto))]
    [HttpPut("webhook")]
    public async Task<WebhooksConfigDto> UpdateWebhook(UpdateWebhooksConfigRequestsDto inDto)
    {
        await CheckWebhook(inDto.Name, inDto.Uri, inDto.SecretKey, inDto.SSL, false);

        var existingWebhook = await dbWorker.GetWebhookConfig(tenantManager.GetCurrentTenantId(), inDto.Id);

        if (existingWebhook == null)
        {
            throw new ItemNotFoundException();
        }

        if (!await CheckAdminPermissionsAsync())
        {
            if (existingWebhook.CreatedBy != authContext.CurrentAccount.ID)
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }
        }

        existingWebhook.Name = inDto.Name;
        existingWebhook.Uri = inDto.Uri;
        existingWebhook.Enabled = inDto.Enabled;
        existingWebhook.SSL = inDto.SSL;
        existingWebhook.Triggers = inDto.Triggers;
        existingWebhook.TargetId = inDto.TargetId;

        if (!string.IsNullOrEmpty(inDto.SecretKey))
        {
            existingWebhook.SecretKey = inDto.SecretKey;
        }

        var webhook = await dbWorker.UpdateWebhookConfig(existingWebhook, true);

        messageService.Send(MessageAction.WebhookUpdated, MessageTarget.Create(webhook.Id), webhook.Name);

        return await webhooksConfigDtoHelper.GetAsync(webhook);
    }

    /// <summary>
    /// Enables or disables a tenant webhook with the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Enable a webhook
    /// </short>
    /// <path>api/2.0/settings/webhook/enable</path>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "Enable or disable tenant webhook", typeof(WebhooksConfigDto))]
    [HttpPut("webhook/enable")]
    public async Task<WebhooksConfigDto> EnableWebhook(UpdateWebhooksConfigRequestsDto inDto)
    {
        var existingWebhook = await dbWorker.GetWebhookConfig(tenantManager.GetCurrentTenantId(), inDto.Id);

        if (existingWebhook == null)
        {
            throw new ItemNotFoundException();
        }

        if (!await CheckAdminPermissionsAsync())
        {
            if (existingWebhook.CreatedBy != authContext.CurrentAccount.ID)
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }
        }

        if (inDto.Enabled)
        {
            await CheckWebhook(existingWebhook.Name, existingWebhook.Uri, existingWebhook.SecretKey, existingWebhook.SSL, false);
        }

        existingWebhook.Enabled = inDto.Enabled;

        var webhook = await dbWorker.UpdateWebhookConfig(existingWebhook, true);

        messageService.Send(MessageAction.WebhookUpdated, MessageTarget.Create(webhook.Id), webhook.Name);

        return await webhooksConfigDtoHelper.GetAsync(webhook);
    }

    /// <summary>
    /// Removes a tenant webhook with the ID specified in the request.
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
        var existingWebhook = await dbWorker.GetWebhookConfig(tenantManager.GetCurrentTenantId(), inDto.Id);

        if (existingWebhook == null)
        {
            throw new ItemNotFoundException();
        }

        if (!await CheckAdminPermissionsAsync())
        {
            if (existingWebhook.CreatedBy != authContext.CurrentAccount.ID)
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }
        }

        var webhook = await dbWorker.RemoveWebhookConfigAsync(inDto.Id);

        messageService.Send(MessageAction.WebhookDeleted, MessageTarget.Create(webhook.Id), webhook.Name);

        return await webhooksConfigDtoHelper.GetAsync(webhook);
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
    [SwaggerResponse(200, "Logs of the webhook activities", typeof(IAsyncEnumerable<WebhooksLogDto>))]
    [HttpGet("webhooks/log")]
    public async IAsyncEnumerable<WebhooksLogDto> GetWebhooksLogs(WebhookLogsRequestDto inDto)
    {
        if (!await CheckAdminPermissionsAsync())
        {
            inDto.UserId = authContext.CurrentAccount.ID;
        }

        context.SetTotalCount(await dbWorker.GetTotalByQuery(inDto.DeliveryFrom, inDto.DeliveryTo, inDto.HookUri, inDto.ConfigId, inDto.EventId, inDto.GroupStatus, inDto.UserId, inDto.Trigger));

        await foreach (var j in dbWorker.ReadJournal(inDto.StartIndex, inDto.Count, inDto.DeliveryFrom, inDto.DeliveryTo, inDto.HookUri, inDto.ConfigId, inDto.EventId, inDto.GroupStatus, inDto.UserId, inDto.Trigger))
        {
            j.Log.Config = j.Config;
            yield return mapper.Map(j.Log);
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
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task<WebhooksLogDto> RetryWebhook(IdRequestDto<int> inDto)
    {
        if (inDto.Id == 0)
        {
            throw new ArgumentException(nameof(inDto.Id));
        }

        var item = await dbWorker.ReadJournal(tenantManager.GetCurrentTenantId(), inDto.Id);

        if (item == null)
        {
            throw new ItemNotFoundException();
        }

        if (!await CheckAdminPermissionsAsync())
        {
            if (item.Config.CreatedBy != authContext.CurrentAccount.ID)
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }
        }

        var result = await webhookPublisher.RetryPublishAsync(item);

        return mapper.Map(result);
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
    [SwaggerResponse(200, "Logs of the webhook activities", typeof(IAsyncEnumerable<WebhooksLogDto>))]
    [HttpPut("webhook/retry")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async IAsyncEnumerable<WebhooksLogDto> RetryWebhooks(WebhookRetryRequestsDto inDto)
    {
        var isAdmin = await CheckAdminPermissionsAsync();
        var tenantId = tenantManager.GetCurrentTenantId();

        foreach (var id in inDto.Ids)
        {
            var item = await dbWorker.ReadJournal(tenantId, id);

            if (item == null)
            {
                continue;
            }

            if (!isAdmin && item.Config.CreatedBy != authContext.CurrentAccount.ID)
            {
                continue;
            }

            var result = await webhookPublisher.RetryPublishAsync(item);

            yield return mapper.Map(result);
        }
    }

    /// <summary>
    /// Returns a list of triggers for a webhook.
    /// </summary>
    /// <short>
    /// Get webhook triggers
    /// </short>
    /// <path>api/2.0/settings/webhook/triggers</path>
    /// <collection>list</collection>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "List of triggers for a webhook", typeof(Dictionary<string, int>))]
    [HttpGet("webhook/triggers")]
    public Dictionary<string, int> GetWebhookTriggers()
    {
        return Enum.GetValues<WebhookTrigger>().ToDictionary(item => item.ToCustomString(), item => (int)item);
    }

    private async Task<bool> CheckAdminPermissionsAsync()
    {
        var currentUser = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);

        if (await userManager.IsDocSpaceAdminAsync(currentUser))
        {
            return true;
        }

        if (await userManager.IsGuestAsync(currentUser))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var settings = await settingsManager.LoadAsync<TenantDevToolsAccessSettings>();

        if (settings.LimitedAccessForUsers)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        return false;
    }

    private async Task CheckWebhook(string name, string uri, string secret, bool ssl, bool creation)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(uri);

        if (creation || !string.IsNullOrEmpty(secret))
        {
            ArgumentNullException.ThrowIfNull(secret);

            var passwordSettings = await settingsManager.LoadAsync<PasswordSettings>();

            passwordSettingsManager.CheckPassword(secret, passwordSettings);
        }

        var restrictions = configuration.GetSection("webhooks:blacklist").Get<List<string>>() ?? [];

        if (Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri) &&
            IPAddress.TryParse(parsedUri.Host, out _) &&
            restrictions.Any(r => IPAddressRange.MatchIPs(parsedUri.Host, r)))
        {
            throw new ArgumentException();
        }

        var httpClientName = "";

        if (Uri.UriSchemeHttps.Equals(parsedUri.Scheme.ToLower(), StringComparison.OrdinalIgnoreCase) && !ssl)
        {
            httpClientName = "defaultHttpClientSslIgnore";
        }

        using var httpClient = clientFactory.CreateClient(httpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Head, uri);
        using var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new ArgumentException(Resource.ErrorWebhookUrlNotAvaliable);
        }
    }
}