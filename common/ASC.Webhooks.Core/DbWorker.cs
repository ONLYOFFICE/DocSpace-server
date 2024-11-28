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

using System.Security;

using AutoMapper;

using Microsoft.Extensions.Configuration;

namespace ASC.Webhooks.Core;

[Scope]
public class DbWorker(
    IDbContextFactory<WebhooksDbContext> dbContextFactory,
    TenantManager tenantManager,
    AuthContext authContext,
    IMapper mapper,
    IHttpClientFactory clientFactory,
    IConfiguration configuration)
{
    public static readonly IReadOnlyList<string> MethodList = new List<string>
    {
        "POST",
        "PUT",
        "DELETE"
    };
    

    public async Task<DbWebhooksConfig> AddWebhookConfig(string uri, string name, string secretKey, bool? enabled, bool? ssl)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();
        
        var tenantId = tenantManager.GetCurrentTenantId();
        var objForCreate = await webhooksDbContext.WebhooksConfigByUriAsync(tenantId, uri, name);

        if (objForCreate != null)
        {
            return objForCreate;
        }

        var restrictions = configuration.GetSection("webhooks:blacklist").Get<List<string>>() ?? [];
        
        if (Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri) &&         
            System.Net.IPAddress.TryParse(parsedUri.Host, out _) && 
            restrictions.Any(r => IPAddressRange.MatchIPs(parsedUri.Host, r)))
        {
            throw new SecurityException();
        }

        var httpClientName = "";

        if (Uri.UriSchemeHttps.Equals(parsedUri.Scheme.ToLower(), StringComparison.OrdinalIgnoreCase) &&
           ssl.HasValue && !ssl.Value)
        {
            httpClientName = "defaultHttpClientSslIgnore";
        }

        var httpClient = clientFactory.CreateClient(httpClientName);

        // validate webhook uri 
        var request = new HttpRequestMessage(HttpMethod.Head, uri);
        var response = await httpClient.SendAsync(request);

        if (response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new Exception($"Webhook with {uri} is not avaliable. HEAD request is not responce 200 http status.");
        }

        var toAdd = new DbWebhooksConfig
        {
            TenantId = tenantId,
            Uri = uri,
            SecretKey = secretKey,
            Name = name,
            Enabled = enabled ?? true,
            SSL = ssl ?? true
        };

        toAdd = await webhooksDbContext.AddOrUpdateAsync(r => r.WebhooksConfigs, toAdd);
        await webhooksDbContext.SaveChangesAsync();

        return toAdd;
    }

    public async IAsyncEnumerable<WebhooksConfigWithStatus> GetTenantWebhooksWithStatus()
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();
        
        var q = webhooksDbContext.WebhooksConfigWithStatusAsync(tenantId);

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

    public async IAsyncEnumerable<DbWebhooksConfig> GetWebhookConfigs()
    {        
        var tenantId = tenantManager.GetCurrentTenantId();
        
        var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var q = webhooksDbContext.WebhooksConfigsAsync(tenantId);

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
        updateObj.LastSuccessOn = dbWebhooksConfig.LastSuccessOn;
        updateObj.LastFailureOn =  dbWebhooksConfig.LastFailureOn;
        updateObj.LastFailureContent = dbWebhooksConfig.LastFailureContent;

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

    public async IAsyncEnumerable<DbWebhooks> ReadJournal(int startIndex, int limit, DateTime? deliveryFrom, DateTime? deliveryTo, string hookUri, int? hookId, int? configId, int? eventId, WebhookGroupStatus? webhookGroupStatus)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();
        var q = await GetQueryForJournal(deliveryFrom, deliveryTo, hookUri, hookId, configId, eventId, webhookGroupStatus);

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

    public async Task<int> GetTotalByQuery(DateTime? deliveryFrom, DateTime? deliveryTo, string hookUri, int? hookId, int? configId, int? eventId, WebhookGroupStatus? webhookGroupStatus)
    {
        return await (await GetQueryForJournal(deliveryFrom, deliveryTo, hookUri, hookId, configId, eventId, webhookGroupStatus)).CountAsync();
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
        webhook.TenantId = tenantManager.GetCurrentTenantId();
        webhook.Uid = authContext.CurrentAccount.ID;

        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var entity = await webhooksDbContext.WebhooksLogs.AddAsync(webhook);
        await webhooksDbContext.SaveChangesAsync();

        return entity.Entity;
    }

    public async Task<DbWebhooksLog> UpdateWebhookJournal(int id,
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

    public async Task Register(List<Webhook> webhooks)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var dbWebhooks = await webhooksDbContext.DbWebhooksAsync().ToListAsync();

        foreach (var webhook in webhooks)
        {
            if (!dbWebhooks.Exists(r => r.Route == webhook.Route && r.Method == webhook.Method))
            {
                try
                {
                    await webhooksDbContext.Webhooks.AddAsync(mapper.Map<DbWebhook>(webhook));
                    await webhooksDbContext.SaveChangesAsync();
                }
                catch (Exception)
                {

                }
            }
        }
    }

    public async Task<List<Webhook>> GetWebhooksAsync()
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();
        var webHooks = await webhooksDbContext.DbWebhooksAsync().ToListAsync();
        return mapper.Map<List<DbWebhook>, List<Webhook>>(webHooks);
    }

    public async Task<Webhook> GetWebhookAsync(int id)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();
        var webHook = await webhooksDbContext.DbWebhookAsync(id);
        return mapper.Map<DbWebhook, Webhook>(webHook);
    }

    public async Task<Webhook> GetWebhookAsync(string method, string routePattern)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var webHook = await webhooksDbContext.DbWebhookByMethodAsync(method, routePattern);

        return mapper.Map<DbWebhook, Webhook>(webHook);
    }

    private async Task<IQueryable<DbWebhooks>> GetQueryForJournal(DateTime? deliveryFrom, DateTime? deliveryTo, string hookUri, int? hookId, int? configId, int? eventId, WebhookGroupStatus? webhookGroupStatus)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        
        var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var q = webhooksDbContext.WebhooksLogs
            
            .OrderByDescending(t => t.Id)
            .Where(r => r.TenantId == tenantId)
            .Join(webhooksDbContext.WebhooksConfigs, r => r.ConfigId, r => r.Id, (log, config) => new DbWebhooks { Log = log, Config = config });

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

        if (hookId != null)
        {
            q = q.Where(r => r.Log.WebhookId == hookId);
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