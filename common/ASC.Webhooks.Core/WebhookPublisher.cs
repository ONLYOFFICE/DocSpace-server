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

using System.Text.Json;
using System.Text.Json.Serialization;

using ASC.Core.Tenants;

namespace ASC.Webhooks.Core;

[Scope(typeof(IWebhookPublisher))]
public class WebhookPublisher(
    DbWorker dbWorker,
    IEventBus eventBus,
    TenantUtil tenantUtil,
    AuthContext authContext)
    : IWebhookPublisher
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        Converters = { new TenantToUtcDateTimeJsonConverter(tenantUtil) },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    private class TenantToUtcDateTimeJsonConverter(TenantUtil tenantUtil) : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Kind == DateTimeKind.Local ? tenantUtil.DateTimeToUtc(value) : value.ToUniversalTime());
        }
    }


    public async Task<IEnumerable<DbWebhooksConfig>> GetWebhookConfigsAsync<T>(WebhookTrigger trigger, IWebhookAccessChecker<T> checker, T data)
    {
        var result = new List<DbWebhooksConfig>();

        var webhookConfigs = await dbWorker.GetActiveWebhookConfigsFromCache();

        foreach (var config in webhookConfigs)
        {
            if (config.Triggers != WebhookTrigger.All && !config.Triggers.HasFlag(trigger))
            {
                continue;
            }

            if (checker != null)
            {
                if (!string.IsNullOrEmpty(config.TargetId) && !checker.CheckIsTarget(data, config.TargetId))
                {
                    continue;
                }

                if (config.CreatedBy.HasValue && authContext.CurrentAccount.ID != config.CreatedBy.Value)
                {
                    if (!await checker.CheckAccessAsync(data, config.CreatedBy.Value))
                    {
                        continue;
                    }
                }
            }

            result.Add(config);
        }

        return result;
    }

    public async Task PublishAsync<T1, T2>(WebhookTrigger trigger, IEnumerable<DbWebhooksConfig> webhookConfigs, T1 data, T2 dataId)
    {
        foreach (var config in webhookConfigs)
        {
            _ = await PublishAsync(trigger, config, data, dataId);
        }
    }

    public async Task PublishAsync<T1, T2>(WebhookTrigger trigger, IWebhookAccessChecker<T1> checker, T1 data, T2 dataId)
    {
        var webhookConfigs = await GetWebhookConfigsAsync(trigger, checker, data);

        foreach (var config in webhookConfigs)
        {
            _ = await PublishAsync(trigger, config, data, dataId);
        }
    }

    private async Task<DbWebhooksLog> PublishAsync<T1, T2>(WebhookTrigger trigger, DbWebhooksConfig webhookConfig, T1 data, T2 dataId)
    {
        var payload = new WebhookPayload<T1, T2>(trigger, webhookConfig, data, dataId, authContext.CurrentAccount.ID);

        var payloadStr = JsonSerializer.Serialize(payload, _serializerOptions);

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


    public async Task<DbWebhooksLog> RetryPublishAsync(DbWebhooksLog webhookLog)
    {
        var webhooksLog = new DbWebhooksLog
        {
            TenantId = webhookLog.TenantId,
            ConfigId = webhookLog.ConfigId,
            Uid = authContext.CurrentAccount.ID,
            Trigger = webhookLog.Trigger,
            CreationTime = DateTime.UtcNow,
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