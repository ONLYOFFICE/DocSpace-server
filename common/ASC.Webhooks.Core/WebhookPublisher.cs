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

[Scope(typeof(IWebhookPublisher))]
public class WebhookPublisher(
    DbWorker dbWorker,
    IEventBus eventBus,
    TenantUtil tenantUtil,
    AuthContext authContext,
    UserManager userManager,
    SettingsManager settingsManager)
    : IWebhookPublisher
{
    // One instance for the process: the serializer builds its metadata per options instance, so options created
    // per scope would rebuild it for every event. The converter needs the TenantUtil of the publishing scope, which
    // is handed to it for the duration of one synchronous Serialize call.
    private static readonly AsyncLocal<TenantUtil> _serializingTenantUtil = new();
    private static readonly JsonSerializerOptions _serializerOptions = CreateSerializerOptions(() => _serializingTenantUtil.Value);

    /// <summary>
    /// The options every webhook body is serialized with. Public so the payload tests serialize exactly what goes
    /// on the wire instead of a copy of these settings; <paramref name="tenantUtil"/> is only consulted for a
    /// <see cref="DateTimeKind.Local"/> value.
    /// </summary>
    public static JsonSerializerOptions CreateSerializerOptions(TenantUtil tenantUtil)
    {
        return CreateSerializerOptions(() => tenantUtil);
    }

    private static JsonSerializerOptions CreateSerializerOptions(Func<TenantUtil> tenantUtil)
    {
        return new JsonSerializerOptions
        {
            Converters = { new TenantToUtcDateTimeJsonConverter(tenantUtil) },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };
    }

    /// <summary>
    /// How every timestamp of a webhook body is written: UTC, whole seconds, with the Z.
    /// </summary>
    public const string WholeSecondsUtcFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private class TenantToUtcDateTimeJsonConverter(Func<TenantUtil> tenantUtil) : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var utc = value.Kind switch
            {
                // TenantUtil marks a value it converted into the portal's time zone as Local
                DateTimeKind.Local => tenantUtil().DateTimeToUtc(value),
                // read back from the database, which stores every timestamp in UTC; ToUniversalTime would take it for
                // the server's local time and shift it by the server's offset
                DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
                _ => value
            };

            // whole seconds, as the envelope: a fraction of up to seven digits is what Swift's ISO-8601 decoder refuses
            writer.WriteStringValue(utc.ToString(WholeSecondsUtcFormat, CultureInfo.InvariantCulture));
        }
    }

    private string Serialize<T>(T payload)
    {
        _serializingTenantUtil.Value = tenantUtil;

        try
        {
            return JsonSerializer.Serialize(payload, _serializerOptions);
        }
        finally
        {
            _serializingTenantUtil.Value = null;
        }
    }


    public async Task<List<DbWebhooksConfig>> GetWebhookConfigsAsync<TSource>(WebhookTrigger trigger, IWebhookAccessChecker<TSource> checker, TSource source)
    {
        var result = new List<DbWebhooksConfig>();

        var webhookConfigs = await dbWorker.GetActiveWebhookConfigsFromCache();
        bool? devToolsLimited = null;

        // one creator usually owns several subscriptions, and both answers below depend on the creator and the event
        // only, so each is worked out once per creator for this event
        var entitledCreators = new Dictionary<Guid, bool>();
        var creatorsWithAccess = new Dictionary<Guid, bool>();

        foreach (var config in webhookConfigs)
        {
            if (config.Triggers != WebhookTrigger.All && !config.Triggers.HasFlag(trigger))
            {
                continue;
            }

            // the creator was allowed to subscribe when the subscription was saved; what counts is what they may receive
            // now. A catch-all subscription is narrowed the same way: it stands for the events the creator's role may
            // subscribe to, not for every event of the portal
            if (config.CreatedBy is { } creatorId)
            {
                if (!entitledCreators.TryGetValue(creatorId, out var entitled))
                {
                    devToolsLimited ??= (await settingsManager.LoadAsync<TenantDevToolsAccessSettings>()).LimitedAccessForUsers;
                    entitled = await IsCreatorEntitledAsync(userManager, creatorId, trigger, devToolsLimited.Value);
                    entitledCreators[creatorId] = entitled;
                }

                if (!entitled)
                {
                    continue;
                }
            }

            if (checker != null)
            {
                if (!string.IsNullOrEmpty(config.TargetId) && !checker.CheckIsTarget(source, config.TargetId))
                {
                    continue;
                }

                if (config.CreatedBy is { } ownerId && authContext.CurrentAccount.ID != ownerId)
                {
                    if (!creatorsWithAccess.TryGetValue(ownerId, out var hasAccess))
                    {
                        hasAccess = await checker.CheckAccessAsync(source, ownerId);
                        creatorsWithAccess[ownerId] = hasAccess;
                    }

                    if (!hasAccess)
                    {
                        continue;
                    }
                }
            }

            result.Add(config);
        }

        return result;
    }

    /// <summary>
    /// Whether the creator of a subscription may still receive <paramref name="trigger"/>: an active member whose
    /// current role may subscribe to it, and a DocSpace admin while the developer tools are limited for users. A
    /// creator who was removed, terminated, turned into a guest or downgraded to a role without that trigger gets
    /// nothing, as the API would no longer let them subscribe to it. The sender asks the same before every attempt
    /// (<see cref="WebhookCreatorEntitlement"/>), so the retries of an event stop as well.
    /// </summary>
    internal static async Task<bool> IsCreatorEntitledAsync(UserManager userManager, Guid creatorId, WebhookTrigger trigger, bool devToolsLimited)
    {
        var creator = await userManager.GetUsersAsync(creatorId);

        if (creator.Equals(Constants.LostUser) || creator.Status != EmployeeStatus.Active)
        {
            return false;
        }

        var creatorType = await userManager.GetUserTypeAsync(creator);

        if (devToolsLimited && creatorType != EmployeeType.DocSpaceAdmin)
        {
            return false;
        }

        return trigger.IsAvailableFor(creatorType);
    }

    public async Task PublishAsync<TPayload, TId>(WebhookTrigger trigger, IEnumerable<DbWebhooksConfig> webhookConfigs, TPayload payload, TId dataId)
    {
        foreach (var config in webhookConfigs)
        {
            _ = await PublishAsync(trigger, config, payload, dataId);
        }
    }

    private async Task<DbWebhooksLog> PublishAsync<TPayload, TId>(WebhookTrigger trigger, DbWebhooksConfig webhookConfig, TPayload data, TId dataId)
    {
        var payload = new WebhookPayload<TPayload, TId>(trigger, webhookConfig, data, dataId, authContext.CurrentAccount.ID);

        var payloadStr = Serialize(payload);

        var webhooksLog = new DbWebhooksLog
        {
            TenantId = webhookConfig.TenantId,
            ConfigId = webhookConfig.Id,
            Uid = authContext.CurrentAccount.ID,
            Trigger = trigger,
            CreationTime = DateTime.UtcNow,
            RequestPayload = payloadStr
        };

        return await PublishAsync(webhooksLog);
    }


    public async Task<WebhookRetryResult> RetryPublishAsync(DbWebhooksLog webhookLog)
    {
        var result = await dbWorker.RetryJournalAsync(webhookLog, authContext.CurrentAccount.ID);

        if (result.Status != WebhookRetryStatus.Requeued)
        {
            return result;
        }

        await eventBus.PublishAsync(new WebhookRequestIntegrationEvent(authContext.CurrentAccount.ID, result.Log.TenantId, result.Log.Id));

        result.Log.Config = webhookLog.Config;

        return result;
    }

    private async Task<DbWebhooksLog> PublishAsync(DbWebhooksLog webhookLog)
    {
        webhookLog.Attempts = 0;
        webhookLog.NextAttemptOn = webhookLog.CreationTime;

        var newWebhooksLog = await dbWorker.WriteToJournal(webhookLog);

        var @event = new WebhookRequestIntegrationEvent(authContext.CurrentAccount.ID, newWebhooksLog.TenantId, newWebhooksLog.Id);

        await eventBus.PublishAsync(@event);

        return newWebhooksLog;
    }

}

/// <summary>
/// The entitlement check of <see cref="WebhookPublisher"/> for the sender: a subscription's creator who may no longer
/// receive an event gets neither its automatic retries nor a manual retry, which carry the body stored at publish.
/// Asks in the portal of the current tenant.
/// </summary>
[Scope]
public class WebhookCreatorEntitlement(UserManager userManager, SettingsManager settingsManager)
{
    public async Task<bool> IsEntitledAsync(Guid creatorId, WebhookTrigger trigger)
    {
        var devToolsLimited = (await settingsManager.LoadAsync<TenantDevToolsAccessSettings>()).LimitedAccessForUsers;

        return await WebhookPublisher.IsCreatorEntitledAsync(userManager, creatorId, trigger, devToolsLimited);
    }
}