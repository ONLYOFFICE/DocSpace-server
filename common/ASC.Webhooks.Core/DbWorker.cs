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

namespace ASC.Webhooks.Core;

[Scope]
public class DbWorker(
    IDbContextFactory<WebhooksDbContext> dbContextFactory,
    TenantManager tenantManager,
    AuthContext authContext)
{
    public async Task<DbWebhooksConfig> AddWebhookConfig(string name, string uri, string secretKey, bool enabled, bool ssl, WebhookTrigger triggers)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var tenantId = tenantManager.GetCurrentTenantId();
        var existingConfig = await webhooksDbContext.WebhooksConfigByUriAsync(tenantId, uri, name);

        if (existingConfig != null)
        {
            return existingConfig;
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
            CreatedBy = authContext.CurrentAccount.ID,
            CreatedOn = DateTime.UtcNow
        };

        toAdd = await webhooksDbContext.AddOrUpdateAsync(r => r.WebhooksConfigs, toAdd);
        await webhooksDbContext.SaveChangesAsync();

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

    public async IAsyncEnumerable<DbWebhooksConfig> GetWebhookConfigs(bool? enabled)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var q = webhooksDbContext.WebhooksConfigsAsync(tenantId, enabled);

        await foreach (var webhook in q)
        {
            yield return webhook;
        }
    }

    public async Task<DbWebhooksConfig> UpdateWebhookConfig(DbWebhooksConfig dbWebhooksConfig)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var updateObj = await webhooksDbContext.WebhooksConfigAsync(dbWebhooksConfig.TenantId, dbWebhooksConfig.Id);

        updateObj.Name = dbWebhooksConfig.Name;
        updateObj.Uri = dbWebhooksConfig.Uri;
        updateObj.SecretKey = dbWebhooksConfig.SecretKey;
        updateObj.Enabled = dbWebhooksConfig.Enabled;
        updateObj.SSL = dbWebhooksConfig.SSL;
        updateObj.Triggers = dbWebhooksConfig.Triggers;

        updateObj.ModifiedBy = dbWebhooksConfig.ModifiedBy ?? authContext.CurrentAccount.ID;
        updateObj.ModifiedOn = DateTime.UtcNow;

        updateObj.LastFailureOn =  dbWebhooksConfig.LastFailureOn;
        updateObj.LastFailureContent = dbWebhooksConfig.LastFailureContent;
        updateObj.LastSuccessOn = dbWebhooksConfig.LastSuccessOn;

        webhooksDbContext.WebhooksConfigs.Update(updateObj);

        await webhooksDbContext.SaveChangesAsync();
    
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
        var q = await GetQueryForJournal(deliveryFrom, deliveryTo, hookUri, configId, eventId, webhookGroupStatus, userId, trigger);

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
        return await (await GetQueryForJournal(deliveryFrom, deliveryTo, hookUri, configId, eventId, webhookGroupStatus, userId, trigger)).CountAsync();
    }

    public async Task<DbWebhooksLog> ReadJournal(int id)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var fromDb = await webhooksDbContext.WebhooksLogAsync(id);

        if (fromDb != null)
        {
            fromDb.Log.Config = fromDb.Config;
        }

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
        int status,
        DateTime delivery,
        string requestPayload,
        string requestHeaders,
        string responsePayload,
        string responseHeaders)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var webhook = (await webhooksDbContext.WebhooksLogAsync(id))?.Log;

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

        var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

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

[Flags]
public enum WebhookGroupStatus
{
    [SwaggerEnum("None")]
    None = 0,

    [SwaggerEnum("Not sent")]
    NotSent = 1,

    [SwaggerEnum("Status2xx")]
    Status2xx = 2,

    [SwaggerEnum("Status3xx")]
    Status3xx = 4,

    [SwaggerEnum("Status4xx")]
    Status4xx = 8,

    [SwaggerEnum("Status5xx")]
    Status5xx = 16
}