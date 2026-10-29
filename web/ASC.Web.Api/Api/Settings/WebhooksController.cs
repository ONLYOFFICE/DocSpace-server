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
    /// payload whenever one of the subscribed events happens. The target is checked before anything is stored: an
    /// absolute `http` or `https` address outside the installation's own network that answers a HEAD request with a
    /// success code, redirects not followed. `secretKey` is mandatory, has to satisfy the password rules of `GET
    /// api/2.0/settings/security/password`, signs the payloads and never appears in a response. `triggers` is a bitmask
    /// of the subscribed events, 0 standing for all of them; a flag the caller's role may not use is rejected, so take
    /// the allowed set from `GET api/2.0/settings/webhook/triggers`. `ssl` demands an `https` target with a valid
    /// certificate, `false` leaves the certificate unchecked, and left out it is `true` for an `https` target and
    /// `false` for an `http` one.
    /// `targetId` limits the events to one entity, for user or group events a user or group the creator may see. A
    /// subscription fires only while it is enabled, for events its creator may see and their current role may
    /// subscribe to. Any role except `Guest` may create one, and each call adds another subscription.
    /// </remarks>
    /// <summary>
    /// Create a webhook
    /// </summary>
    /// <path>api/2.0/settings/webhook</path>
    [Tags("Settings / Webhooks")]
    [SwaggerResponse(200, "The created webhook subscription, without its secret key", typeof(WebhooksConfigDto))]
    [SwaggerResponse(400, "The request body cannot be read, `name` or `uri` is missing or empty, `name` or `secretKey` is longer than 50 characters or `targetId` longer than 255, the target URL is unusable or answers the HEAD request with a non-success code, or the secret key, a trigger flag or the `targetId` of user or group events was rejected: no user or group the creator may see has that ID")]
    [SwaggerResponse(403, "The caller is a `Guest`, or a non-admin caller while the developer tools are restricted")]
    [SwaggerResponse(500, "The target URL gives no answer to the HEAD request: the connection fails or times out, or the certificate is not valid while `ssl` is `true`")]
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
    /// Replaces the stored parameters of one webhook subscription, addressed by `id` in the body rather than in the
    /// path. Every field of the request overwrites the stored one, so a payload that leaves out `enabled`, `triggers`
    /// or `targetId` resets them to off, all events and no target: read the current values with `GET
    /// api/2.0/settings/webhook` first and send back whatever should stay. `secretKey` and `ssl` are the exceptions:
    /// an empty `secretKey` keeps the existing secret, a new one having to satisfy the portal password rules, and a
    /// left-out `ssl` keeps the stored setting, so pass it explicitly when moving the target between `http` and
    /// `https`. A `DocSpaceAdmin`
    /// may update any subscription in the portal, anyone else only their own, and a `Guest` is refused. Once the body
    /// passes the schema checks (a missing `name` or `uri` gives 400 first), an unknown `id` gives 404 and another
    /// member's subscription 403 before the target, the secret and the triggers are checked. The payload is then validated
    /// as on creation, except that trigger flags are checked against the role of the subscription's creator rather than
    /// the caller's. Sending the same payload twice leaves the same state. Use `PUT api/2.0/settings/webhook/enable` to
    /// switch a subscription on or off without touching anything else.
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
    [SwaggerResponse(500, "The target URL gives no answer to the HEAD request: the connection fails or times out, or the certificate is not valid while `ssl` is `true`")]
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
    /// Switches one webhook subscription on or off, leaving the rest of its parameters as they are. Only `id` and
    /// `enabled` are read from the body: `name` and `uri` must be present because the schema requires them, but their
    /// values are ignored here, as are `secretKey`, `ssl`, `triggers` and `targetId` when sent - change any of them
    /// with `PUT api/2.0/settings/webhook` instead. Switching a subscription on re-checks what is already stored: the
    /// saved URL is probed with a HEAD request, the saved secret is checked against the current password rules, and the saved triggers against the current role of the
    /// subscription's creator. The call is refused with 400 when any of these fails, so a subscription whose target has
    /// gone away cannot be switched on until it is updated. Switching one off is not validated. While a subscription is
    /// off its events are dropped rather than queued. A `DocSpaceAdmin` may switch any subscription in the portal,
    /// anyone else only their own, and a `Guest` is refused. The response carries the subscription in its new state,
    /// and repeating the call changes nothing further.
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
    [SwaggerResponse(500, "On switching on, the saved target URL gives no answer to the HEAD request: the connection fails or times out, or the certificate is not valid while the subscription has `ssl` set")]
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
        await webhookSocketManager.DeleteWebhookAsync(webhook);

        return await webhooksConfigDtoHelper.GetAsync(webhook);
    }

    /// <remarks>
    /// Returns the delivery log of the portal webhooks, one record per delivery of an event, updated in place by its
    /// automatic retries: the trigger, the request and response headers and bodies, the HTTP `status` of the latest
    /// attempt, 0 while no answer has come back, and the `delivery` moment, empty until an attempt succeeds. `attempts`
    /// counts the tries so far and `nextAttemptOn` is set while another one is due. A manual retry adds a record of its
    /// own, with a new `id`, the same `eventId` and the member in `retriedBy`. Records come newest first, paged with
    /// `startIndex` and `count`, at most 100 at a time, with the number of matches as `total`. Filters combine with
    /// AND: `deliveryFrom` and `deliveryTo` bound the delivery moment, `hookUri` matches the subscription URL exactly,
    /// `configId` picks one subscription, `groupStatus` keeps the status classes it names (0 means no filter), and
    /// `trigger` one event type (0 means all). `eventId` keeps every record of one event; the `id` of a record a manual
    /// retry returned keeps that record alone. `userId` filters by the creator of the subscription and is forced to the
    /// caller for anyone but a `DocSpaceAdmin`. A `Guest` is refused. Nothing is written.
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
    /// Sends one past webhook delivery again to its subscription. The `id` in the path is that of a delivery record
    /// from `GET api/2.0/settings/webhooks/log`, not of a subscription. A `DocSpaceAdmin` may retry any delivery in the
    /// portal, anyone else only deliveries of the subscriptions they created, and a `Guest` is refused. The work is
    /// asynchronous: a new delivery record is queued and returned with its own `id`, the `eventId` of the retried one
    /// and the caller in `retriedBy`; the retried record keeps its outcome, and automatic retries still due for the
    /// event stop. The receiver gets the same `event.id` and `x-docspace-event-id` header in a body serialized and
    /// signed anew. To learn the outcome, read the log with `eventId` set to the returned `id` until `nextAttemptOn` is
    /// empty: `delivery` is then set on success and empty when the portal gave up after its automatic retries (on the
    /// default schedule, six more tries over about 18 hours). An `id` of 0 gives 400 and an unknown one 404. 409 means
    /// nothing was queued: a delivery of the event is being sent right now, the subscription is disabled, or the record
    /// predates the current delivery format. Rate limited with 429.
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
    /// Sends a batch of past webhook deliveries again. `ids` holds identifiers of delivery records from `GET
    /// api/2.0/settings/webhooks/log`; for each one a new delivery record of the same event is queued, with its own
    /// `id`, the retried record's `eventId` and the caller in `retriedBy`, and the response lists those new records.
    /// Each event is retried once per call, so a second identifier of the same event is passed over. Records that do
    /// not exist, records of another member's subscription when the caller is not a `DocSpaceAdmin`, and records
    /// that cannot be queued now (an event being sent right now, a disabled subscription, a record older than the
    /// current delivery format) are skipped in silence, so compare the response with `ids`; an empty `ids` queues
    /// nothing. A `Guest` is refused. Each receiver gets the same `event.id` and `x-docspace-event-id` header in a
    /// body serialized and signed anew. The work is asynchronous: read the log with `eventId` for each returned
    /// identifier until `nextAttemptOn` is empty; a set `delivery` then means success, an empty one that the portal
    /// gave up. At most 100 identifiers per call, rate limited with 429. `PUT api/2.0/settings/webhook/{id}/retry`
    /// reports why a single record was not queued.
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
    /// Returns the catalogue of events a webhook subscription can listen to, in the order the portal presents them:
    /// user events, then group, file, folder, room, form and agent ones. Each entry carries the event name as it
    /// appears in a payload, such as `file.created`, the bit value to put into the `triggers` bitmask of a
    /// subscription, and `available`, telling whether the caller's own role may subscribe to that event at all: a
    /// `User` cannot subscribe to the creation of users, groups or rooms, for instance, while a `RoomAdmin` can. Add
    /// the bit values of the wanted events together to build `triggers`; the entry named `*` has the value 0 and
    /// stands for every event the subscription creator's current role may subscribe to, so it is used on its own
    /// rather than added. Events unavailable to the caller are listed all the same, but passing one to `POST
    /// api/2.0/settings/webhook` or `PUT api/2.0/settings/webhook` is rejected as an invalid request. This is fixed
    /// reference data: the same for every portal, not paginated, changing only with the product version, and readable
    /// by any authenticated caller, a `Guest` included. Nothing is written.
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
