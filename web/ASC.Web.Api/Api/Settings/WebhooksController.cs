// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

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
    WebhooksConfigDtoHelper webhooksConfigDtoHelper,
    IUrlValidator urlValidator,
    IHttpClientFactory clientFactory)
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Returns the webhook subscriptions of the current portal, each together with the outcome of its most recent
    /// delivery. The portal owner and a `DocSpaceAdmin` see every subscription in the portal, while a `RoomAdmin` or
    /// a `User` sees only the ones they created themselves, so the same call answers differently depending on who
    /// asks. A `Guest` may not use webhooks at all and is refused, and so is any non-admin caller while the portal
    /// keeps the developer tools restricted, which `GET api/2.0/settings/devtoolsaccess` reports. Every entry pairs
    /// the stored configuration with `status`, the HTTP status code the target answered on the last attempt, where 0
    /// means nothing has been delivered yet, while the secret key is not part of the response. The list is neither
    /// paginated nor ordered, and an empty list simply means no subscription exists for the caller. Nothing is
    /// written and the call is safe to repeat. Create a subscription with `POST api/2.0/settings/webhook`, and
    /// inspect single deliveries with `GET api/2.0/settings/webhooks/log`.
    /// </remarks>
    /// <summary>
    /// Get the portal webhooks
    /// </summary>
    /// <path>api/2.0/settings/webhook</path>
    /// <collection>list</collection>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "The webhook subscriptions visible to the caller, each with the status of its last delivery", typeof(IAsyncEnumerable<WebhooksConfigWithStatusDto>))]
    [SwaggerResponse(403, "The caller is a `Guest`, or a non-admin caller while the developer tools are restricted")]
    [HttpGet("webhook")]
    public async IAsyncEnumerable<WebhooksConfigWithStatusDto> GetTenantWebhooks()
    {
        Guid? userId = await CheckAdminPermissionsAsync() ? null : authContext.CurrentAccount.ID;

        await foreach (var webhook in dbWorker.GetTenantWebhooksWithStatus(userId))
        {
            yield return await webhooksConfigDtoHelper.GetAsync(webhook);
        }
    }

    /// <remarks>
    /// Creates a webhook subscription for the current portal: a target URL that the portal calls with a signed JSON
    /// payload whenever one of the subscribed events happens. The target is checked before anything is stored, so it
    /// has to be an absolute `http` or `https` address outside the installation's own network, and it has to answer a
    /// HEAD request with a success code, redirects not being followed. `secretKey` is mandatory here, has to satisfy
    /// the portal password rules published by `GET api/2.0/settings/security/password`, and signs the payloads; it
    /// does not appear in any response. `triggers` is a bitmask of the subscribed events with 0 standing for all of
    /// them; a flag the caller's role may not use is rejected, so take the allowed set from
    /// `GET api/2.0/settings/webhook/triggers`. `ssl=true` additionally demands an `https` target with a valid
    /// certificate, while `ssl=false` leaves the certificate unchecked. Set `targetId` to deliver events about a
    /// single entity only. A subscription fires only for events its creator is allowed to see, and only while it is
    /// enabled. Any role except `Guest` may create one, and each call adds another subscription rather than replacing
    /// an existing one.
    /// </remarks>
    /// <summary>
    /// Create a webhook
    /// </summary>
    /// <path>api/2.0/settings/webhook</path>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "The created webhook subscription, without its secret key", typeof(WebhooksConfigDto))]
    [SwaggerResponse(400, "The target URL is unusable or unreachable, or the secret key or a trigger flag was rejected")]
    [SwaggerResponse(403, "The caller is a `Guest`, or a non-admin caller while the developer tools are restricted")]
    [HttpPost("webhook")]
    public async Task<WebhooksConfigDto> CreateWebhook(CreateWebhooksConfigRequestsDto inDto)
    {
        _ = await CheckAdminPermissionsAsync();

        await CheckWebhook(inDto.Name, inDto.Uri, inDto.SecretKey, inDto.SSL, inDto.Triggers, true);

        var webhook = await dbWorker.AddWebhookConfig(inDto.Name, inDto.Uri, inDto.SecretKey, inDto.Enabled, inDto.SSL, inDto.Triggers, inDto.TargetId);

        messageService.Send(MessageAction.WebhookCreated, MessageTarget.Create(webhook.Id), webhook.Name);

        return await webhooksConfigDtoHelper.GetAsync(webhook);
    }

    /// <remarks>
    /// Replaces the stored parameters of one webhook subscription, which is addressed by `id` in the body rather than
    /// in the path. Every field of the request overwrites the stored one, so a payload that leaves out `enabled`,
    /// `ssl`, `triggers` or `targetId` resets them to off, all events and no target: read the current values with
    /// `GET api/2.0/settings/webhook` first and send back whatever should stay. `secretKey` is the one exception, an
    /// empty value keeping the existing secret and a new one having to satisfy the portal password rules. The new
    /// target is validated exactly as on creation, that is it must sit outside the installation's own network and
    /// answer a HEAD request, and trigger flags the caller's role may not use are rejected. That validation runs
    /// before the subscription is looked up, so an unusable payload is refused with 400 even when no subscription
    /// with this `id` exists. A `DocSpaceAdmin` may update any subscription in the portal, anyone else only their
    /// own, and a `Guest` is refused. Sending the same payload twice leaves the same state. Use
    /// `PUT api/2.0/settings/webhook/enable` to switch a subscription on or off without touching anything else.
    /// </remarks>
    /// <summary>
    /// Update a webhook
    /// </summary>
    /// <path>api/2.0/settings/webhook</path>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "The updated webhook subscription, without its secret key", typeof(WebhooksConfigDto))]
    [SwaggerResponse(400, "The target URL is unusable or unreachable, or the secret key or a trigger flag was rejected")]
    [SwaggerResponse(403, "The subscription belongs to another member, or the caller may not use webhooks at all")]
    [SwaggerResponse(404, "No webhook subscription with this ID exists in the portal")]
    [HttpPut("webhook")]
    public async Task<WebhooksConfigDto> UpdateWebhook(UpdateWebhooksConfigRequestsDto inDto)
    {
        await CheckWebhook(inDto.Name, inDto.Uri, inDto.SecretKey, inDto.SSL, inDto.Triggers, false);

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

    /// <remarks>
    /// Switches one webhook subscription on or off, leaving the rest of its parameters as they are. Only `id` and
    /// `enabled` are read from the body: `name`, `uri`, `secretKey`, `ssl`, `triggers` and `targetId` are demanded by
    /// the schema but ignored here, so change any of them with `PUT api/2.0/settings/webhook` instead. Switching a
    /// subscription on re-checks what is already stored, probing the saved URL with a HEAD request and re-validating
    /// the saved secret against the current portal password rules, and the call is refused with 400 when either
    /// fails: a subscription whose target has gone away, or whose secret predates a tightening of the password rules,
    /// cannot be switched on until it is updated. Switching one off is not validated. While a subscription is off its
    /// events are dropped rather than queued, so nothing arrives from that period once it is switched on again. A
    /// `DocSpaceAdmin` may switch any subscription in the portal, anyone else only their own, and a `Guest` is
    /// refused. The response carries the subscription in its new state, and repeating the call changes nothing
    /// further.
    /// </remarks>
    /// <summary>
    /// Switch a webhook on or off
    /// </summary>
    /// <path>api/2.0/settings/webhook/enable</path>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "The webhook subscription in its new state, without its secret key", typeof(WebhooksConfigDto))]
    [SwaggerResponse(400, "The saved target no longer answers, or the saved secret no longer passes the password rules")]
    [SwaggerResponse(403, "The subscription belongs to another member, or the caller may not use webhooks at all")]
    [SwaggerResponse(404, "No webhook subscription with this ID exists in the portal")]
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
            await CheckWebhook(existingWebhook.Name, existingWebhook.Uri, existingWebhook.SecretKey, existingWebhook.SSL, existingWebhook.Triggers, false);
        }

        existingWebhook.Enabled = inDto.Enabled;

        var webhook = await dbWorker.UpdateWebhookConfig(existingWebhook, true);

        messageService.Send(MessageAction.WebhookUpdated, MessageTarget.Create(webhook.Id), webhook.Name);

        return await webhooksConfigDtoHelper.GetAsync(webhook);
    }

    /// <remarks>
    /// Removes one webhook subscription from the current portal for good, addressed by `id` in the path. Deliveries
    /// stop with it: matching events are no longer queued, and there is no undo, so a subscription dropped by mistake
    /// has to be created again with `POST api/2.0/settings/webhook`, which gives it a new identifier and needs a new
    /// secret key. To pause deliveries without losing the configuration, switch the subscription off with
    /// `PUT api/2.0/settings/webhook/enable` instead. A `DocSpaceAdmin` may remove any subscription in the portal,
    /// anyone else only the ones they created, and a `Guest` may not use webhooks at all. The response repeats the
    /// subscription as it was just before the removal, so the caller can record what disappeared, again without the
    /// secret key. An identifier that no longer exists gives 404, which is what a second removal of the same
    /// subscription answers as well, so a repeated call is harmless but reports the state truthfully rather than
    /// pretending to succeed.
    /// </remarks>
    /// <summary>
    /// Remove a webhook
    /// </summary>
    /// <path>api/2.0/settings/webhook/{id}</path>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "The removed webhook subscription, as it was just before the removal", typeof(WebhooksConfigDto))]
    [SwaggerResponse(403, "The subscription belongs to another member, or the caller may not use webhooks at all")]
    [SwaggerResponse(404, "No webhook subscription with this ID exists in the portal")]
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

    /// <remarks>
    /// Returns the delivery records of the portal webhooks, one record per attempt, carrying the trigger, the request
    /// and response headers and bodies, the HTTP `status` the target answered and the `delivery` moment, the last two
    /// staying empty while an attempt is still on its way. Records come newest first and are paged with `startIndex`
    /// and `count`, at most 100 at a time, while the number of records matching the filter is reported as `total`
    /// beside the response. Filters combine with AND: `deliveryFrom` and `deliveryTo` bound the delivery moment,
    /// `hookUri` matches the subscription URL exactly, `configId` picks one subscription, `eventId` one single
    /// record, `groupStatus` keeps only the answered status classes it names with 0 meaning no status filter, and
    /// `trigger` narrows to one event with 0 meaning all of them. `userId` filters by who created the subscription
    /// rather than by who caused the event, and for a caller who is not a `DocSpaceAdmin` it is forced to the caller,
    /// so a non-admin only ever sees deliveries of their own subscriptions. A `Guest` is refused. Nothing is written.
    /// </remarks>
    /// <summary>
    /// Get the webhook delivery log
    /// </summary>
    /// <path>api/2.0/settings/webhooks/log</path>
    /// <collection>list</collection>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "The matching delivery records, newest first, with the total count reported beside them", typeof(IAsyncEnumerable<WebhooksLogDto>))]
    [SwaggerResponse(403, "The caller is a `Guest`, or a non-admin caller while the developer tools are restricted")]
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

    /// <remarks>
    /// Sends one past webhook delivery again. The `id` in the path is that of a delivery record from
    /// `GET api/2.0/settings/webhooks/log`, not of a subscription, and the payload kept in that record is sent once
    /// more to the subscription it belongs to. The work is asynchronous: a fresh delivery record is created and
    /// queued at once, and the response describes that new record, with an identifier of its own and with `status`
    /// and `delivery` not filled in yet. To learn the outcome, read `GET api/2.0/settings/webhooks/log` with
    /// `eventId` set to the returned identifier until `delivery` appears. The original record stays as it is, and
    /// every call queues one more attempt, so this is not safe to repeat blindly. A `DocSpaceAdmin` may retry any
    /// delivery in the portal, anyone else only deliveries of the subscriptions they created, and a `Guest` is
    /// refused. An `id` of 0 is rejected as an invalid request and an unknown one gives 404. The operation is rate
    /// limited, so a burst of calls is answered with 429; to retry several records use
    /// `PUT api/2.0/settings/webhook/retry`.
    /// </remarks>
    /// <summary>
    /// Retry a webhook delivery
    /// </summary>
    /// <path>api/2.0/settings/webhook/{id}/retry</path>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "The newly queued delivery record, with its status and delivery moment not filled in yet", typeof(WebhooksLogDto))]
    [SwaggerResponse(400, "The delivery record identifier is 0")]
    [SwaggerResponse(403, "The delivery belongs to another member's subscription, or the caller may not use webhooks")]
    [SwaggerResponse(404, "No delivery record with this ID exists in the portal")]
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

    /// <remarks>
    /// Sends a batch of past webhook deliveries again. `ids` holds the identifiers of delivery records from
    /// `GET api/2.0/settings/webhooks/log`; each of them is sent once more to the subscription it belongs to as a
    /// fresh delivery record, queued for asynchronous delivery, and the response lists those new records with
    /// `status` and `delivery` not filled in yet. Records that do not exist, and records of another member's
    /// subscription when the caller is not a `DocSpaceAdmin`, are skipped in silence instead of failing the call, so
    /// a response shorter than `ids` is the only sign that something was left out: compare the counts rather than
    /// assuming everything was queued. An empty `ids` list is accepted and queues nothing. Read the outcomes from
    /// `GET api/2.0/settings/webhooks/log`, matching the returned identifiers with `eventId`. Every call queues
    /// another round of attempts, and the original records stay as they are. A `Guest` is refused. The operation is
    /// rate limited, so a burst of calls is answered with 429. For a single record
    /// `PUT api/2.0/settings/webhook/{id}/retry` reports a missing or forbidden record instead of skipping it.
    /// </remarks>
    /// <summary>
    /// Retry webhook deliveries
    /// </summary>
    /// <path>api/2.0/settings/webhook/retry</path>
    /// <collection>list</collection>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "The newly queued delivery records, one for every identifier that could be retried", typeof(IAsyncEnumerable<WebhooksLogDto>))]
    [SwaggerResponse(403, "The caller is a `Guest`, or a non-admin caller while the developer tools are restricted")]
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

    /// <remarks>
    /// Returns the catalogue of events a webhook subscription can listen to, in the order the portal presents them:
    /// user events, then group, file, folder, room, form and agent ones. Each entry carries the event name as it
    /// appears in a payload, such as `file.created`, the bit value to put into the `triggers` bitmask of a
    /// subscription, and `available`, telling whether the caller's own role may subscribe to that event at all: a
    /// `User` cannot subscribe to the creation of users, groups or rooms, for instance, while a `RoomAdmin` can. Add
    /// the bit values of the wanted events together to build `triggers`; the entry named `*` has the value 0 and
    /// stands for every event, so it is used on its own rather than added. Events unavailable to the caller are
    /// listed all the same, but passing one to `POST api/2.0/settings/webhook` or `PUT api/2.0/settings/webhook` is
    /// rejected as an invalid request. This is fixed reference data: the same for every portal, not paginated,
    /// changing only with the product version, and readable by any authenticated caller, a `Guest` included. Nothing
    /// is written.
    /// </remarks>
    /// <summary>
    /// Get the webhook triggers
    /// </summary>
    /// <path>api/2.0/settings/webhook/triggers</path>
    /// <collection>list</collection>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "The triggers a webhook may subscribe to, each marked available for the caller's role or not", typeof(IEnumerable<WebhookTriggerDto>))]
    [HttpGet("webhook/triggers")]
    public async Task<IEnumerable<WebhookTriggerDto>> GetWebhookTriggers()
    {
        var userType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);

        return Enum.GetValues<WebhookTrigger>()
            .OrderBy(t => t.GetOrder())
            .Select(t => new WebhookTriggerDto
            {
                Name = t.ToCustomString(),
                Id = (long)t,
                Available = t.IsAvailableFor(userType)
            });
    }

    private async Task<bool> CheckAdminPermissionsAsync()
    {
        var currentUserId = authContext.CurrentAccount.ID;

        if (await userManager.IsDocSpaceAdminAsync(currentUserId))
        {
            return true;
        }

        if (await userManager.IsGuestAsync(currentUserId))
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

    private async Task CheckWebhook(string name, string uri, string secret, bool ssl, WebhookTrigger triggers, bool creation)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(uri);

        if (triggers != WebhookTrigger.All)
        {
            var userType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);

            foreach (var trigger in Enum.GetValues<WebhookTrigger>())
            {
                if (trigger == WebhookTrigger.All)
                {
                    continue;
                }

                if (triggers.HasFlag(trigger) && !trigger.IsAvailableFor(userType))
                {
                    throw new ArgumentException("Trigger is not available");
                }
            }
        }

        if (creation || !string.IsNullOrEmpty(secret))
        {
            ArgumentNullException.ThrowIfNull(secret);

            var passwordSettings = await settingsManager.LoadAsync<PasswordSettings>();

            passwordSettingsManager.CheckPassword(secret, passwordSettings);
        }

        // Validate URL using centralized UrlValidator (SSRF protection)
        var validationResult = await urlValidator.ValidateAsync(uri, new UrlValidationOptions
        {
            RequireHttps = ssl
        });

        if (!validationResult.IsValid)
        {
            throw new ArgumentException(validationResult.ErrorMessage);
        }

        var httpClientName = !ssl && Uri.UriSchemeHttps.Equals(validationResult.ParsedUri.Scheme, StringComparison.OrdinalIgnoreCase)
            ? UrlValidator.PinnedHttpClientSslIgnore
            : UrlValidator.PinnedHttpClient;

        var httpClient = clientFactory.CreateClient(httpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Head, validationResult.ParsedUri);
        request.Options.Set(UrlValidator.PinnedIpKey, validationResult.ResolvedAddresses[0]);
        using var response = await httpClient.SendAsync(request);

        if (response is not { IsSuccessStatusCode: true })
        {
            throw new ArgumentException(Resource.ErrorWebhookUrlNotAvaliable);
        }
    }
}
