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
    IHttpClientFactory clientFactory,
    WebhookUserAccessChecker webhookUserAccessChecker,
    WebhookSocketManager webhookSocketManager,
    WebhookGroupAccessChecker webhookGroupAccessChecker)
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Returns the webhook subscriptions of the portal, each with the outcome of its last delivery. Any signed-in
    /// user can call it, except a guest. A guest gets 403. If the portal restricts the developer tools, only DocSpace
    /// admins can call it. Other callers then get 403. To read this setting, use
    /// `GET api/2.0/settings/devtoolsaccess`. The portal owner and DocSpace admins see every subscription. Other
    /// users see only the subscriptions they created. The call is read-only. The response never contains the secret
    /// key. The list has no paging and no fixed order. An empty list means that the caller has no subscriptions. To
    /// create a subscription, use `POST api/2.0/settings/webhook`. To read single deliveries, use
    /// `GET api/2.0/settings/webhooks/log`.
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
    /// Creates a webhook subscription of the portal. The portal then posts a signed JSON payload to `uri` when a
    /// subscribed event happens. Any signed-in user can call it, except a guest. A guest gets 403. If the portal
    /// restricts the developer tools, only DocSpace admins can call it. Other callers then get 403. The call changes
    /// data. A repeated call with the same `name` and `uri` returns 400. The response never contains the secret key.
    /// Before saving, the call sends a HEAD request to `uri` and does not follow redirects. If you leave out `ssl`, it
    /// is `true` for an `https` address and `false` for an `http` one. If `triggers` is 0, the subscription gets every
    /// event that the role of its creator allows. To find the triggers your role allows, use
    /// `GET api/2.0/settings/webhook/triggers`. A subscription delivers only while it is on, and only events that its
    /// creator can see. The call returns 400 if `name` or `uri` is empty, a value is too long, or `uri` is not allowed.
    /// It also returns 400 if `uri` answers the HEAD request with an error code. It also returns 400 if `secretKey`
    /// breaks the password rules or your role does not allow a trigger. For user or group events, `targetId` must name
    /// a user or group that you can see. If it does not, the call returns 400.
    /// </remarks>
    /// <summary>
    /// Create a webhook
    /// </summary>
    /// <path>api/2.0/settings/webhook</path>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "The created webhook subscription, without its secret key", typeof(WebhooksConfigDto))]
    [SwaggerResponse(400, "The request body cannot be read, `name` or `uri` is missing or empty, `name` or `secretKey` is longer than 50 characters or `targetId` longer than 255, the target URL is unusable or answers the HEAD request with a non-success code, or the secret key, a trigger flag or the `targetId` of user or group events was rejected: no user or group the creator may see has that ID, or the portal already has a subscription with the same `name` and `uri`")]
    [SwaggerResponse(403, "The caller is a `Guest`, or a non-admin caller while the developer tools are restricted")]
    [HttpPost("webhook")]
    public async Task<WebhooksConfigDto> CreateWebhook(CreateWebhooksConfigRequestsDto inDto)
    {
        _ = await CheckAdminPermissionsAsync();

        var ssl = ResolveSsl(inDto.SSL, inDto.Uri);
        var targetId = await CheckWebhook(inDto.Name, inDto.Uri, inDto.SecretKey, ssl, inDto.Triggers, inDto.TargetId, authContext.CurrentAccount.ID, true);

        var webhook = await dbWorker.AddWebhookConfig(inDto.Name, inDto.Uri, inDto.SecretKey, inDto.Enabled, ssl, inDto.Triggers, targetId);

        messageService.Send(MessageAction.WebhookCreated, MessageTarget.Create(webhook.Id), webhook.Name);
        await webhookSocketManager.AddWebhookAsync(webhook);

        return await webhooksConfigDtoHelper.GetAsync(webhook);
    }

    /// <remarks>
    /// Changes all parameters of a webhook subscription that the body names by `id`. The call overwrites every stored
    /// field with the body. So a left-out `enabled`, `triggers`, or `targetId` becomes `false`, 0, or empty. First
    /// read the current values with `GET api/2.0/settings/webhook`. Then send back the values that must stay. Two
    /// fields are exceptions. An empty `secretKey` keeps the stored secret. A left-out `ssl` keeps the stored
    /// setting. So pass `ssl` when you move the target between `http` and `https`. Any signed-in user can call it,
    /// except a guest. A guest gets 403. The portal owner and DocSpace admins can change any subscription. Other
    /// users can change only the subscriptions they created and get 403 for others. If the portal restricts the
    /// developer tools, users who are not DocSpace admins get 403 too. The call changes data. You can repeat it with
    /// the same body. A missing `name` or `uri` returns 400 before any other check. If no subscription has this `id`,
    /// the call returns 404 before the access check. Then the call checks the body as `POST api/2.0/settings/webhook`
    /// does. It checks the triggers against the role of the subscription's creator, not of the caller. If a check
    /// fails, the call returns 400. To turn a subscription on or off only, use `PUT api/2.0/settings/webhook/enable`.
    /// </remarks>
    /// <summary>
    /// Update a webhook
    /// </summary>
    /// <path>api/2.0/settings/webhook</path>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "The updated webhook subscription, without its secret key", typeof(WebhooksConfigDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `id`, `name` or `uri` is missing or empty, `name` or `secretKey` is longer than 50 characters or `targetId` longer than 255, the target URL is unusable or answers the HEAD request with a non-success code, or the secret key, a trigger flag or the `targetId` of user or group events was rejected: no user or group the subscription's creator may see has that ID")]
    [SwaggerResponse(403, "The subscription belongs to another member, or the caller may not use webhooks at all")]
    [SwaggerResponse(404, "No webhook subscription with this ID exists in the portal")]
    [HttpPut("webhook")]
    public async Task<WebhooksConfigDto> UpdateWebhook(UpdateWebhooksConfigRequestsDto inDto)
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

        // the triggers are the owner's to receive, so they are checked against the owner's role, not the editor's
        var ownerId = existingWebhook.CreatedBy ?? authContext.CurrentAccount.ID;
        // left out, ssl keeps the stored value, so a client that omits it does not switch certificate checking on for a
        // subscription saved with it off. That covers only clients that really omit it: the C# SDK, generated while ssl
        // was a plain boolean, always sends the field - false unless the caller sets it - so an update through it turns
        // checking off until the SDK is regenerated from the nullable spec, which follows the merge of this change
        var ssl = inDto.SSL ?? existingWebhook.SSL;
        var targetId = await CheckWebhook(inDto.Name, inDto.Uri, inDto.SecretKey, ssl, inDto.Triggers, inDto.TargetId, ownerId, false);

        existingWebhook.Name = inDto.Name;
        existingWebhook.Uri = inDto.Uri;
        existingWebhook.Enabled = inDto.Enabled;
        existingWebhook.SSL = ssl;
        existingWebhook.Triggers = inDto.Triggers;
        existingWebhook.TargetId = targetId;

        if (!string.IsNullOrEmpty(inDto.SecretKey))
        {
            existingWebhook.SecretKey = inDto.SecretKey;
        }

        var webhook = await dbWorker.UpdateWebhookConfig(existingWebhook, true);

        messageService.Send(MessageAction.WebhookUpdated, MessageTarget.Create(webhook.Id), webhook.Name);
        await webhookSocketManager.UpdateWebhookAsync(webhook);

        return await webhooksConfigDtoHelper.GetAsync(webhook);
    }

    /// <remarks>
    /// Turns a webhook subscription on or off and keeps its other parameters. The call reads only `id` and `enabled`
    /// from the body. The schema still requires `name` and `uri`, but the call ignores their values. To change other
    /// fields, use `PUT api/2.0/settings/webhook`. Any signed-in user can call it, except a guest. A guest gets 403.
    /// The portal owner and DocSpace admins can switch any subscription. Other users can switch only the
    /// subscriptions they created and get 403 for others. If the portal restricts the developer tools, users who are
    /// not DocSpace admins get 403 too. The call changes data. You can repeat it with the same body. If no
    /// subscription has this `id`, the call returns 404 before the access check. Turning a subscription off checks
    /// nothing. Turning it on checks the stored values again. The call sends a HEAD request to the stored address. It
    /// checks the stored secret against the current password rules. It checks the stored triggers against the current
    /// role of the subscription's creator. If any of these checks fails, the call returns 400. So you cannot turn on
    /// a subscription whose target no longer works. Update the subscription first.
    /// </remarks>
    /// <summary>
    /// Switch a webhook on or off
    /// </summary>
    /// <path>api/2.0/settings/webhook/enable</path>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "The webhook subscription in its new state, without its secret key", typeof(WebhooksConfigDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `id`, `name` or `uri` is missing or empty, `name` or `secretKey` is longer than 50 characters or `targetId` longer than 255, although this call reads only `id` and `enabled`, or on switching on the saved target answers the HEAD request with a non-success code or the saved secret no longer passes the password rules, or a saved trigger is no longer open to the creator's role")]
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
            // the saved target is not re-validated: an entity that has gone away only means no more events
            await CheckWebhook(existingWebhook.Name, existingWebhook.Uri, existingWebhook.SecretKey, existingWebhook.SSL, existingWebhook.Triggers, null,
                existingWebhook.CreatedBy ?? authContext.CurrentAccount.ID, false);
        }

        existingWebhook.Enabled = inDto.Enabled;

        var webhook = await dbWorker.UpdateWebhookConfig(existingWebhook, true);

        messageService.Send(MessageAction.WebhookUpdated, MessageTarget.Create(webhook.Id), webhook.Name);
        await webhookSocketManager.UpdateWebhookAsync(webhook);

        return await webhooksConfigDtoHelper.GetAsync(webhook);
    }

    /// <remarks>
    /// Deletes a webhook subscription of the portal by its `id`. The portal then stops queueing its events. Any
    /// signed-in user can call it, except a guest. A guest gets 403. The portal owner and DocSpace admins can delete
    /// any subscription. Other users can delete only the subscriptions they created and get 403 for others. If the
    /// portal restricts the developer tools, users who are not DocSpace admins get 403 too. The call cannot be
    /// undone. A repeated call returns 404. The response returns the subscription as it was before the deletion,
    /// without the secret key. To restore a subscription, create it again with `POST api/2.0/settings/webhook`. It
    /// then gets a new `id`, and you must send the secret key again. To pause deliveries and keep the settings, use
    /// `PUT api/2.0/settings/webhook/enable`. If no subscription has this `id`, the call returns 404.
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
        await webhookSocketManager.DeleteWebhookAsync(webhook);

        return await webhooksConfigDtoHelper.GetAsync(webhook);
    }

    /// <remarks>
    /// Returns the delivery log of the portal webhooks, newest first. Each record is one delivery of an event.
    /// Automatic retries update the same record. A manual retry adds a new record with the same `eventId`. Any
    /// signed-in user can call it, except a guest. A guest gets 403. If the portal restricts the developer tools,
    /// only DocSpace admins can call it. Other callers then get 403. Users who are not DocSpace admins see only the
    /// deliveries of their own subscriptions. For them, the call replaces `userId` with their own ID. The call is
    /// read-only. The call pages with `startIndex` and `count`, up to 100 records a page. The response gives the
    /// number of matching records in `total`. All filters combine with AND. A `groupStatus` or `trigger` of 0 means
    /// no filter. To get every record of one event, filter by its `eventId`. With the `id` of a manual retry record
    /// as `eventId`, the call returns only that record. If `count` is out of its range, the call returns 400.
    /// </remarks>
    /// <summary>
    /// Get the webhook delivery log
    /// </summary>
    /// <path>api/2.0/settings/webhooks/log</path>
    /// <collection>list</collection>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "The matching delivery records, newest first, with the total count reported beside them", typeof(IAsyncEnumerable<WebhooksLogDto>))]
    [SwaggerResponse(400, "The `count` is outside its allowed range")]
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
            yield return await mapper.MapAsync(j.Log);
        }
    }

    /// <remarks>
    /// Sends one past webhook delivery again. The path `id` is a delivery record from
    /// `GET api/2.0/settings/webhooks/log`, not a subscription. Any signed-in user can call it, except a guest. A
    /// guest gets 403. The portal owner and DocSpace admins can retry any delivery. Other users can retry only
    /// deliveries of their own subscriptions and get 403 for others. If the portal restricts the developer tools,
    /// users who are not DocSpace admins get 403 too. The call queues work. It returns a new delivery record with its
    /// own `id`, the old `eventId`, and the caller in `retriedBy`. The old record keeps its outcome. Each call queues
    /// a new delivery and stops the deliveries queued earlier for the event. The receiver gets the same event ID in a
    /// body that the portal signs again. To learn the outcome, poll `GET api/2.0/settings/webhooks/log` with
    /// `eventId` set to the returned `id`. Repeat it until `nextAttemptOn` is empty. Then a set `delivery` means
    /// success, and an empty one means that the portal gave up. On the default schedule, the portal tries six more
    /// times over about 18 hours. The call is rate limited. More calls return 429. If `id` is 0, the call
    /// returns 400. If no delivery record has this `id`, the call returns 404. It returns 409 and queues nothing for
    /// an event being sent now, a disabled subscription, or an outdated record.
    /// </remarks>
    /// <summary>
    /// Retry a webhook delivery
    /// </summary>
    /// <path>api/2.0/settings/webhook/{id}/retry</path>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "The new delivery record queued for the event, with its own identifier and the retried record's event identifier", typeof(WebhooksLogDto))]
    [SwaggerResponse(400, "The delivery record identifier is 0")]
    [SwaggerResponse(403, "The delivery belongs to another member's subscription, or the caller may not use webhooks")]
    [SwaggerResponse(404, "No delivery record with this ID exists in the portal")]
    [SwaggerResponse(409, "Nothing was queued: a delivery of the same event is being sent at this moment, the subscription is disabled, or the record predates the current delivery format")]
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

        switch (result.Status)
        {
            case WebhookRetryStatus.NotFound:
                // the record was removed between the read above and the requeue
                throw new ItemNotFoundException();
            case WebhookRetryStatus.InProgress:
                throw new CustomHttpException(HttpStatusCode.Conflict, Resource.ErrorWebhookRetryInProgress);
            case WebhookRetryStatus.Disabled:
                throw new CustomHttpException(HttpStatusCode.Conflict, Resource.ErrorWebhookRetryDisabled);
            case WebhookRetryStatus.Outdated:
                throw new CustomHttpException(HttpStatusCode.Conflict, Resource.ErrorWebhookRetryOutdated);
        }

        messageService.Send(MessageAction.WebhookRetried, MessageTarget.Create(item.ConfigId), item.Config.Name);

        return await mapper.MapAsync(result.Log);
    }

    /// <remarks>
    /// Sends several past webhook deliveries again. The body lists delivery records from
    /// `GET api/2.0/settings/webhooks/log` in `ids`. Any signed-in user can call it, except a guest. A guest
    /// gets 403. If the portal restricts the developer tools, only DocSpace admins can call it. Other callers then
    /// get 403. The call queues work. For each record, it queues a new delivery of the same event and returns the new
    /// record. Each new record has its own `id`, the old `eventId`, and the caller in `retriedBy`. Each call also
    /// stops the deliveries queued earlier for these events. The call retries each event once and skips other records
    /// of the same event. It also skips unknown records and records that it cannot queue now. Users who are not
    /// DocSpace admins can retry only records of their own subscriptions. The call skips other records for them. So
    /// compare the response with `ids`. An empty `ids` queues nothing. Each receiver gets the same event ID in a body
    /// that the portal signs again. To learn the outcome, poll `GET api/2.0/settings/webhooks/log` with `eventId` set
    /// to each returned `id`. Repeat it until `nextAttemptOn` is empty. Then a set `delivery` means success, and an
    /// empty one means that the portal gave up. The call is rate limited. More calls return 429. If `ids` has more
    /// than 100 records, the call returns 400. To learn why the call skipped a record, use
    /// `PUT api/2.0/settings/webhook/{id}/retry`.
    /// </remarks>
    /// <summary>
    /// Retry webhook deliveries
    /// </summary>
    /// <path>api/2.0/settings/webhook/retry</path>
    /// <collection>list</collection>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "The new delivery records, one for every event that could be retried, each with its own identifier", typeof(IAsyncEnumerable<WebhooksLogDto>))]
    [SwaggerResponse(400, "The request body cannot be read, or `ids` holds more than 100 identifiers")]
    [SwaggerResponse(403, "The caller is a `Guest`, or a non-admin caller while the developer tools are restricted")]
    [HttpPut("webhook/retry")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async IAsyncEnumerable<WebhooksLogDto> RetryWebhooks(WebhookRetryRequestsDto inDto)
    {
        var isAdmin = await CheckAdminPermissionsAsync();
        var tenantId = tenantManager.GetCurrentTenantId();

        // each retry of an event stops the rows queued earlier for it, so a second id of the same event would cancel
        // the record the first one has just returned
        var events = new HashSet<int>();

        // a missing list queues nothing, like an empty one
        foreach (var id in (inDto.Ids ?? []).Distinct())
        {
            var item = await dbWorker.ReadJournal(tenantId, id);

            if (item == null || !events.Add(item.EventId))
            {
                continue;
            }

            if (!isAdmin && item.Config.CreatedBy != authContext.CurrentAccount.ID)
            {
                continue;
            }

            var result = await webhookPublisher.RetryPublishAsync(item);

            if (result.Status != WebhookRetryStatus.Requeued)
            {
                continue;
            }

            messageService.Send(MessageAction.WebhookRetried, MessageTarget.Create(item.ConfigId), item.Config.Name);

            yield return await mapper.MapAsync(result.Log);
        }
    }

    /// <remarks>
    /// Returns the events that a webhook subscription can listen to. Any signed-in user can call it, including a
    /// guest. The developer tools restriction does not apply here. The call is read-only. The list is the same for
    /// every portal and has no paging. It shows user, group, file, folder, room, form, and agent events, in this
    /// order. To build `triggers`, add the `id` values of the wanted events. The entry `*` has the value 0 and means
    /// every event that the role of the creator allows. Use it alone. The `available` field shows whether your role
    /// can subscribe to the event. The list also includes the events that your role cannot use. The portal refuses
    /// such an event when you create or change a subscription.
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

    /// <summary>
    /// The certificate check a request asks for: what it sent, or, left out, on for an `https` target and off for an
    /// `http` one - a plain `http` target with the check on would be refused here and disabled by the sender.
    /// </summary>
    private static bool ResolveSsl(bool? ssl, string uri)
    {
        return ssl ?? (Uri.TryCreate(uri, UriKind.Absolute, out var parsed) && parsed.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Validates a subscription about to be saved for <paramref name="ownerId"/>, and returns its target in the form
    /// the access checkers compare it in.
    /// </summary>
    private async Task<string> CheckWebhook(string name, string uri, string secret, bool ssl, WebhookTrigger triggers, string targetId, Guid ownerId, bool creation)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(uri);

        if (triggers != WebhookTrigger.All)
        {
            var userType = await userManager.GetUserTypeAsync(ownerId);

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

        return await CheckTargetAsync(targetId, triggers, ownerId);
    }

    /// <summary>
    /// A target of user or group events has to name a user or group <paramref name="ownerId"/> may see, by the same
    /// rules the events are filtered with when they are published; it is returned as the lowercase GUID the checkers
    /// compare with. One that does not exist and one the owner may not see are refused alike, so the answer tells
    /// nothing about who is in the portal. A target of file, folder, room, agent or form events is not looked up
    /// here: the Files service owns those entries, and the creator's read access to the entry is checked on every
    /// event anyway. When such events are mixed with user or group ones, any target is accepted as it was before
    /// the check existed: an entry ID then simply never matches the user and group events.
    /// </summary>
    private async Task<string> CheckTargetAsync(string targetId, WebhookTrigger triggers, Guid ownerId)
    {
        if (string.IsNullOrEmpty(targetId) || triggers == WebhookTrigger.All)
        {
            return targetId;
        }

        var families = Enum.GetValues<WebhookTrigger>()
            .Where(t => t != WebhookTrigger.All && triggers.HasFlag(t))
            .Select(t => t.GetTargetType())
            .ToHashSet();

        var forUsers = families.Contains("user");
        var forGroups = families.Contains("group");

        if (!forUsers && !forGroups)
        {
            return targetId;
        }

        if (Guid.TryParse(targetId, out var id))
        {
            if (forUsers)
            {
                var user = await userManager.GetUsersAsync(id);

                if (!user.Equals(ASC.Core.Users.Constants.LostUser) && await webhookUserAccessChecker.CheckAccessAsync(user, ownerId))
                {
                    return id.ToString();
                }
            }

            if (forGroups)
            {
                var group = await userManager.GetGroupInfoAsync(id);

                if (group.ID != ASC.Core.Users.Constants.LostGroupInfo.ID && await webhookGroupAccessChecker.CheckAccessAsync(group, ownerId))
                {
                    return id.ToString();
                }
            }
        }

        if (families.Any(family => family is not "user" and not "group"))
        {
            return targetId;
        }

        throw new ArgumentException(Resource.ErrorWebhookTargetNotFound);
    }
}
