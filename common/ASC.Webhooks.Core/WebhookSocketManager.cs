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


namespace ASC.Webhooks.Core;

/// <summary>
/// Tells the open clients that a webhook subscription or a record of its delivery journal changed, so they re-read
/// it through <c>GET api/2.0/settings/webhook</c> or <c>GET api/2.0/settings/webhooks/log</c>. A subscription is not a
/// file or a folder, so there is no room of the entry to send to: the event goes to the users who may read the
/// subscription under the rule of those endpoints - the DocSpace admins and the owner, while the owner may use the
/// developer tools - each in the private room <c>{tenantId}-private-{userId}</c> the socket service puts them in.
/// The payload carries ids only.
/// </summary>
[Scope]
public class WebhookSocketManager(
    ITariffService tariffService,
    TenantManager tenantManager,
    ChannelWriter<SocketData> channelWriter,
    MachinePseudoKeys machinePseudoKeys,
    IConfiguration configuration,
    UserManager userManager,
    SettingsManager settingsManager,
    ILogger<WebhookSocketManager> logger)
    : SocketServiceClient(tariffService, tenantManager, channelWriter, machinePseudoKeys, configuration)
{
    protected override string Hub => "files";

    /// <summary>A subscription was created.</summary>
    public Task AddWebhookAsync(DbWebhooksConfig config)
    {
        return SendAsync("add-webhook", config, new { id = config.Id });
    }

    /// <summary>
    /// A subscription changed: edited or switched on or off, disabled by the sender, or its last success or failure
    /// moved after a delivery attempt.
    /// </summary>
    public Task UpdateWebhookAsync(DbWebhooksConfig config)
    {
        return SendAsync("update-webhook", config, new { id = config.Id });
    }

    /// <summary>A subscription was removed, and its delivery records with it.</summary>
    public Task DeleteWebhookAsync(DbWebhooksConfig config)
    {
        return SendAsync("delete-webhook", config, new { id = config.Id });
    }

    /// <summary>A delivery record was added: an event was published to the subscription, or a manual retry queued.</summary>
    public Task AddWebhookLogAsync(DbWebhooksConfig config, int id)
    {
        return SendAsync("add-webhook-log", config, new { id, configId = config.Id });
    }

    /// <summary>
    /// A delivery record changed: an attempt ended, the record was dropped with a note, or a manual retry took over
    /// its schedule. Not announced: the moves of the lease while an attempt is in flight, and the upkeep of the sender,
    /// which postpones the rows of inactive portals and deletes the records past the retention period in batches across
    /// portals.
    /// </summary>
    public Task UpdateWebhookLogAsync(DbWebhooksConfig config, int id)
    {
        return SendAsync("update-webhook-log", config, new { id, configId = config.Id });
    }

    // best effort: the change the event announces is already stored, so a failure to announce it, the resolution of the
    // recipients included, is logged and never fails the caller
    private async Task SendAsync(string method, DbWebhooksConfig config, object data)
    {
        try
        {
            var tenantId = config.TenantId;
            var userIds = await GetRecipientsAsync(config);

            foreach (var batch in userIds.Chunk(1000))
            {
                await MakeRequest(method, new { tenantId, userIds = batch, data }, tenantId);
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.WarningSocketEvent(method, config.Id, e);
        }
    }

    // A scoped memo, not a cache: it lives as long as this scoped service, i.e. one request of the publisher or one
    // delivery of the sender, so a bulk publish (one row per subscription and event) and the events of one attempt
    // resolve the recipients of a subscription once. The recipients depend on the portal and the subscription's
    // creator only; nothing outlives the scope, so a change of the admins or of the settings is seen by the next one.
    private readonly Dictionary<(int TenantId, Guid? CreatedBy), HashSet<Guid>> _recipients = new();

    private async Task<HashSet<Guid>> GetRecipientsAsync(DbWebhooksConfig config)
    {
        var key = (config.TenantId, config.CreatedBy);

        if (!_recipients.TryGetValue(key, out var recipients))
        {
            recipients = await ResolveRecipientsAsync(config);
            _recipients[key] = recipients;
        }

        return recipients;
    }

    private async Task<HashSet<Guid>> ResolveRecipientsAsync(DbWebhooksConfig config)
    {
        var tenant = await _tenantManager.GetTenantAsync(config.TenantId);
        var admins = await userManager.GetUsersByGroupAsync(Constants.GroupAdmin.ID);

        var recipients = admins.Select(u => u.Id).ToHashSet();
        recipients.Add(tenant.OwnerId);

        if (config.CreatedBy is { } ownerId && !recipients.Contains(ownerId) && await MayReadAsync(ownerId))
        {
            recipients.Add(ownerId);
        }

        return recipients;
    }

    // the rule of the read endpoints for a member who is not a DocSpace admin
    private async Task<bool> MayReadAsync(Guid userId)
    {
        if (await userManager.IsGuestAsync(userId))
        {
            return false;
        }

        return !(await settingsManager.LoadAsync<TenantDevToolsAccessSettings>()).LimitedAccessForUsers;
    }
}
