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

using ZiggyCreatures.Caching.Fusion;

namespace ASC.Webhooks.Core;

[Scope]
public class DbWorker(
    IDbContextFactory<WebhooksDbContext> dbContextFactory,
    TenantManager tenantManager,
    AuthContext authContext,
    WebhookCache webhookCache)
{
    private static string GetCacheKey(int tenantId)
    {
        return $"webhooks_configs_{tenantId}";
    }

    public async Task<DbWebhooksConfig> AddWebhookConfig(string name, string uri, string secretKey, bool enabled, bool ssl, WebhookTrigger triggers, string targetId)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var tenantId = tenantManager.GetCurrentTenantId();
        var existingConfig = await webhooksDbContext.WebhooksConfigByUriAsync(tenantId, uri, name);

        if (existingConfig != null)
        {
            throw new ArgumentException("Webhook with the same name and payload URL already exists");
        }

        var toAdd = new DbWebhooksConfig
        {
            TenantId = tenantId,
            Uri = uri,
            SecretKey = secretKey,
            Name = name,
            Enabled = enabled,
            SSL = ssl,
            Triggers = triggers,
            TargetId = targetId,
            CreatedBy = authContext.CurrentAccount.ID,
            CreatedOn = DateTime.UtcNow
        };

        toAdd = await webhooksDbContext.AddOrUpdateAsync(r => r.WebhooksConfigs, toAdd);
        await webhooksDbContext.SaveChangesAsync();

        await webhookCache.ClearAsync(GetCacheKey(tenantId));

        return toAdd;
    }

    public async IAsyncEnumerable<WebhooksConfigWithStatus> GetTenantWebhooksWithStatus(Guid? userId)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var q = webhooksDbContext.WebhooksConfigWithStatusAsync(tenantId, userId);

        await foreach (var webhook in q)
        {
            yield return webhook;
        }
    }

    public async Task<DbWebhooksConfig> GetWebhookConfig(int tenantId, int id)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var result = await webhooksDbContext.WebhooksConfigAsync(tenantId, id);

        return result;
    }

    public async Task<List<DbWebhooksConfig>> GetActiveWebhookConfigsFromCache()
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var key = GetCacheKey(tenantId);

        var result = await webhookCache.GetAsync<List<DbWebhooksConfig>>(key);

        if (result != null)
        {
            return result;
        }

        result = await GetWebhookConfigs(true).ToListAsync();

        await webhookCache.InsertAsync(key, result);

        return result;
    }

    public async IAsyncEnumerable<DbWebhooksConfig> GetWebhookConfigs(bool? enabled)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var q = webhooksDbContext.WebhooksConfigsAsync(tenantId, enabled);

        await foreach (var webhook in q)
        {
            yield return webhook;
        }
    }

    public async Task<DbWebhooksConfig> UpdateWebhookConfig(DbWebhooksConfig dbWebhooksConfig, bool clearCache)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var updateObj = await webhooksDbContext.WebhooksConfigAsync(dbWebhooksConfig.TenantId, dbWebhooksConfig.Id);

        updateObj.Name = dbWebhooksConfig.Name;
        updateObj.Uri = dbWebhooksConfig.Uri;
        updateObj.SecretKey = dbWebhooksConfig.SecretKey;
        updateObj.Enabled = dbWebhooksConfig.Enabled;
        updateObj.SSL = dbWebhooksConfig.SSL;
        updateObj.Triggers = dbWebhooksConfig.Triggers;
        updateObj.TargetId = dbWebhooksConfig.TargetId;

        updateObj.ModifiedBy = dbWebhooksConfig.ModifiedBy ?? authContext.CurrentAccount.ID;
        updateObj.ModifiedOn = DateTime.UtcNow;

        updateObj.LastFailureOn = dbWebhooksConfig.LastFailureOn;
        updateObj.LastFailureContent = dbWebhooksConfig.LastFailureContent;
        updateObj.LastSuccessOn = dbWebhooksConfig.LastSuccessOn;

        webhooksDbContext.WebhooksConfigs.Update(updateObj);

        await webhooksDbContext.SaveChangesAsync();

        if (clearCache)
        {
            await webhookCache.ClearAsync(GetCacheKey(updateObj.TenantId));
        }

        return updateObj;
    }

    public async Task<DbWebhooksConfig> RemoveWebhookConfigAsync(int id)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var removeObj = await webhooksDbContext.WebhooksConfigAsync(tenantId, id);

        if (removeObj != null)
        {
            webhooksDbContext.WebhooksConfigs.Remove(removeObj);
            await webhooksDbContext.SaveChangesAsync();

            await webhookCache.ClearAsync(GetCacheKey(tenantId));
        }

        return removeObj;
    }

    public async IAsyncEnumerable<DbWebhooks> ReadJournal(
        int startIndex,
        int limit,
        DateTime? deliveryFrom,
        DateTime? deliveryTo,
        string hookUri,
        int? configId,
        int? eventId,
        WebhookGroupStatus? webhookGroupStatus,
        Guid? userId,
        WebhookTrigger? trigger)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();
        var q = await GetQueryForJournal(webhooksDbContext, deliveryFrom, deliveryTo, hookUri, configId, eventId, webhookGroupStatus, userId, trigger);

        if (startIndex != 0)
        {
            q = q.Skip(startIndex);
        }

        if (limit != 0)
        {
            q = q.Take(limit);
        }

        foreach (var r in q)
        {
            yield return r;
        }
    }

    public async Task<int> GetTotalByQuery(DateTime? deliveryFrom,
        DateTime? deliveryTo,
        string hookUri,
        int? configId,
        int? eventId,
        WebhookGroupStatus? webhookGroupStatus,
        Guid? userId,
        WebhookTrigger? trigger)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();
        return await (await GetQueryForJournal(webhooksDbContext, deliveryFrom, deliveryTo, hookUri, configId, eventId, webhookGroupStatus, userId, trigger)).CountAsync();
    }

    public async Task<DbWebhooksLog> ReadJournal(int tenantId, int id)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var fromDb = await webhooksDbContext.WebhooksLogAsync(tenantId, id);

        fromDb?.Log.Config = fromDb.Config;

        return fromDb?.Log;
    }

    public async Task<DbWebhooksLog> WriteToJournal(DbWebhooksLog webhook)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var entity = await webhooksDbContext.WebhooksLogs.AddAsync(webhook);
        await webhooksDbContext.SaveChangesAsync();

        return entity.Entity;
    }

    public async Task<DbWebhooksLog> UpdateWebhookJournal(
        int id,
        int tenantId,
        int status,
        DateTime? delivery,
        string requestPayload,
        string requestHeaders,
        string responsePayload,
        string responseHeaders)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var webhook = (await webhooksDbContext.WebhooksLogAsync(tenantId, id))?.Log;

        if (webhook != null)
        {
            webhook.Status = status;
            webhook.RequestHeaders = requestHeaders;
            webhook.RequestPayload = requestPayload;
            webhook.ResponsePayload = responsePayload;
            webhook.ResponseHeaders = responseHeaders;
            webhook.Delivery = delivery;

            webhooksDbContext.WebhooksLogs.Update(webhook);
            await webhooksDbContext.SaveChangesAsync();
        }

        return webhook;
    }

    private async Task<IQueryable<DbWebhooks>> GetQueryForJournal(
        WebhooksDbContext webhooksDbContext,
        DateTime? deliveryFrom,
        DateTime? deliveryTo,
        string hookUri,
        int? configId,
        int? eventId,
        WebhookGroupStatus? webhookGroupStatus,
        Guid? userId,
        WebhookTrigger? trigger)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        var q = webhooksDbContext.WebhooksLogs
            .OrderByDescending(t => t.Id)
            .Where(r => r.TenantId == tenantId)
            .Join(webhooksDbContext.WebhooksConfigs, r => r.ConfigId, r => r.Id, (log, config) => new DbWebhooks { Log = log, Config = config });

        if (userId.HasValue)
        {
            q = q.Where(r => r.Config.CreatedBy == userId.Value);
        }

        if (trigger.HasValue && trigger.Value != WebhookTrigger.All)
        {
            q = q.Where(r => r.Log.Trigger == trigger.Value);
        }

        if (deliveryFrom.HasValue)
        {
            var from = deliveryFrom.Value;
            q = q.Where(r => r.Log.Delivery >= from);
        }

        if (deliveryTo.HasValue)
        {
            var to = deliveryTo.Value;
            q = q.Where(r => r.Log.Delivery <= to);
        }

        if (!string.IsNullOrEmpty(hookUri))
        {
            q = q.Where(r => r.Config.Uri == hookUri);
        }

        if (configId != null)
        {
            q = q.Where(r => r.Log.ConfigId == configId);
        }

        if (eventId != null)
        {
            q = q.Where(r => r.Log.Id == eventId);
        }

        if (webhookGroupStatus != null && webhookGroupStatus != WebhookGroupStatus.None)
        {
            if ((webhookGroupStatus & WebhookGroupStatus.NotSent) != WebhookGroupStatus.NotSent)
            {
                q = q.Where(r => r.Log.Status != 0);
            }
            if ((webhookGroupStatus & WebhookGroupStatus.Status2xx) != WebhookGroupStatus.Status2xx)
            {
                q = q.Where(r => r.Log.Status < 200 || r.Log.Status >= 300);
            }
            if ((webhookGroupStatus & WebhookGroupStatus.Status3xx) != WebhookGroupStatus.Status3xx)
            {
                q = q.Where(r => r.Log.Status < 300 || r.Log.Status >= 400);
            }
            if ((webhookGroupStatus & WebhookGroupStatus.Status4xx) != WebhookGroupStatus.Status4xx)
            {
                q = q.Where(r => r.Log.Status < 400 || r.Log.Status >= 500);
            }
            if ((webhookGroupStatus & WebhookGroupStatus.Status5xx) != WebhookGroupStatus.Status5xx)
            {
                q = q.Where(r => r.Log.Status < 500);
            }
        }

        return q;
    }
}
public class DbWebhooks
{
    public DbWebhooksLog Log { get; init; }
    public DbWebhooksConfig Config { get; init; }
}

/// <summary>
/// The status of the webhook delivery group.
/// </summary>
[Flags]
public enum WebhookGroupStatus
{
    [Description("None")]
    None = 0,

    [Description("Not sent")]
    NotSent = 1,

    [Description("Status2xx")]
    Status2xx = 2,

    [Description("Status3xx")]
    Status3xx = 4,

    [Description("Status4xx")]
    Status4xx = 8,

    [Description("Status5xx")]
    Status5xx = 16
}

[Singleton]
public class WebhookCache(IFusionCacheProvider cacheProvider)
{
    private readonly IFusionCache _cache = cacheProvider.GetMemoryCache();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);

    public async Task<T> GetAsync<T>(string key) where T : class
    {
        return await _cache.GetOrDefaultAsync<T>(key);
    }

    public async Task InsertAsync<T>(string key, T value) where T : class
    {
        await _cache.SetAsync(key, value, _cacheExpiration);
    }

    public async Task ClearAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }
}