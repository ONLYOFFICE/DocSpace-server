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

namespace ASC.Webhooks.Core.EF.Context;

public class WebhooksDbContext(DbContextOptions<WebhooksDbContext> options) : DbContext(options)
{
    public DbSet<WebhooksConfig> WebhooksConfigs { get; set; }
    public DbSet<WebhooksLog> WebhooksLogs { get; set; }
    public DbSet<DbWebhook> Webhooks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ModelBuilderWrapper
        .From(modelBuilder, Database)
        .AddDbWebhooks()
        .AddWebhooksConfig()
        .AddWebhooksLog()
        .AddDbTenant();
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, null, null])]
    public Task<WebhooksConfig> WebhooksConfigByUriAsync(int tenantId, string uri, string name)
    {
        return Queries.WebhooksConfigByUriAsync(this, tenantId, uri, name);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<WebhooksConfigWithStatus> WebhooksConfigWithStatusAsync(int tenantId)
    {
        return Queries.WebhooksConfigWithStatusAsync(this, tenantId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<WebhooksConfig> WebhooksConfigsAsync(int tenantId)
    {
        return Queries.WebhooksConfigsAsync(this, tenantId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<WebhooksConfig> WebhooksConfigAsync(int tenantId, int id)
    {
        return Queries.WebhooksConfigAsync(this, tenantId, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public Task<DbWebhooks> WebhooksLogAsync(int id)
    {
        return Queries.WebhooksLogAsync(this, id);
    }

    [PreCompileQuery([])]
    public IAsyncEnumerable<DbWebhook> DbWebhooksAsync()
    {
        return Queries.DbWebhooksAsync(this);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public Task<DbWebhook> DbWebhookAsync(int id)
    {
        return Queries.DbWebhookAsync(this, id);
    }

    [PreCompileQuery([null, null])]
    public Task<DbWebhook> DbWebhookByMethodAsync(string method, string routePattern)
    {
        return Queries.DbWebhookByMethodAsync(this, method, routePattern);
    }
}

static file class Queries
{
    public static readonly Func<WebhooksDbContext, int, string, string, Task<WebhooksConfig>> WebhooksConfigByUriAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId, string uri, string name) =>
                ctx.WebhooksConfigs.FirstOrDefault(r => r.TenantId == tenantId && r.Uri == uri && r.Name == name));

    public static readonly Func<WebhooksDbContext, int, IAsyncEnumerable<WebhooksConfigWithStatus>> WebhooksConfigWithStatusAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId) =>
                ctx.WebhooksConfigs.Where(it => it.TenantId == tenantId)
             .GroupJoin(ctx.WebhooksLogs, c => c.Id, l => l.ConfigId, (configs, logs) => new { configs, logs })
            .Select(it =>
                new WebhooksConfigWithStatus
                {
                    WebhooksConfig = it.configs,
                    Status = it.logs.OrderBy(webhooksLog => webhooksLog.Delivery).LastOrDefault().Status
                }));

    public static readonly Func<WebhooksDbContext, int, IAsyncEnumerable<WebhooksConfig>> WebhooksConfigsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId) =>
                ctx.WebhooksConfigs.Where(it => it.TenantId == tenantId));

    public static readonly Func<WebhooksDbContext, int, int, Task<WebhooksConfig>> WebhooksConfigAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId, int id) =>
                ctx.WebhooksConfigs.FirstOrDefault(it => it.TenantId == tenantId && it.Id == id));

    public static readonly Func<WebhooksDbContext, int, Task<DbWebhooks>> WebhooksLogAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int id) =>
                ctx.WebhooksLogs
                    .Where(it => it.Id == id)
                    .Join(ctx.WebhooksConfigs, r => r.ConfigId, r => r.Id, (log, config) => new DbWebhooks { Log = log, Config = config })
                    .FirstOrDefault());

    public static readonly Func<WebhooksDbContext, IAsyncEnumerable<DbWebhook>> DbWebhooksAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx) => ctx.Webhooks);

    public static readonly Func<WebhooksDbContext, int, Task<DbWebhook>> DbWebhookAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int id) => ctx.Webhooks.FirstOrDefault(r => r.Id == id));

    public static readonly Func<WebhooksDbContext, string, string, Task<DbWebhook>> DbWebhookByMethodAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, string method, string routePattern) =>
                ctx.Webhooks.FirstOrDefault(r => r.Method == method && r.Route == routePattern));
}
