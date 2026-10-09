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

namespace ASC.Webhooks.Core.EF.Context;

public class WebhooksDbContext(DbContextOptions<WebhooksDbContext> options) : BaseDbContext(options)
{
    public DbSet<DbWebhook> Webhooks { get; set; } // TODO: Deprecated
    public DbSet<DbWebhooksConfig> WebhooksConfigs { get; set; }
    public DbSet<DbWebhooksLog> WebhooksLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ModelBuilderWrapper
            .From(modelBuilder, Database)
            .AddDbWebhooks()
            .AddWebhooksConfig()
            .AddWebhooksLog()
            .AddDbTenant();
    }

    [PreCompileQuery]
    public Task<DbWebhooksConfig> WebhooksConfigByUriAsync(int tenantId, string uri, string name)
    {
        return Queries.WebhooksConfigByUriAsync(this, tenantId, uri, name);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<WebhooksConfigWithStatus> WebhooksConfigWithStatusAsync(int tenantId, Guid? userId)
    {
        return Queries.WebhooksConfigWithStatusAsync(this, tenantId, userId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbWebhooksConfig> WebhooksConfigsAsync(int tenantId, bool? enabled)
    {
        return Queries.WebhooksConfigsAsync(this, tenantId, enabled);
    }

    [PreCompileQuery]
    public Task<DbWebhooksConfig> WebhooksConfigAsync(int tenantId, int id)
    {
        return Queries.WebhooksConfigAsync(this, tenantId, id);
    }

    [PreCompileQuery]
    public Task<DbWebhooks> WebhooksLogAsync(int tenantId, int id)
    {
        return Queries.WebhooksLogAsync(this, tenantId, id);
    }

    [PreCompileQuery]
    public Task<int?> WebhooksLogAttemptsAsync(int tenantId, int id)
    {
        return Queries.WebhooksLogAttemptsAsync(this, tenantId, id);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DueWebhooksLog> DueWebhooksLogsAsync(DateTime now, int count)
    {
        return Queries.DueWebhooksLogsAsync(this, now, count);
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // Claims one due record by moving NextAttemptOn forward, only if no other instance has moved it since it was
    // read; 1 means this caller owns the record until leaseUntil, 0 means another instance got there first.
    public Task<int> ClaimWebhooksLogAsync(int id, DateTime? expectedNextAttemptOn, DateTime leaseUntil)
    {
        return WebhooksLogs
            .Where(r => r.Id == id && r.NextAttemptOn == expectedNextAttemptOn)
            .ExecuteUpdateAsync(q => q.SetProperty(p => p.NextAttemptOn, leaseUntil));
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    public Task<int> ResetWebhooksLogAttemptsAsync(int tenantId, int id, DateTime nextAttemptOn)
    {
        return WebhooksLogs
            .Where(r => r.TenantId == tenantId && r.Id == id)
            .ExecuteUpdateAsync(q => q
                .SetProperty(p => p.Attempts, 0)
                .SetProperty(p => p.NextAttemptOn, nextAttemptOn)
                .SetProperty(p => p.Status, 0)
                .SetProperty(p => p.Delivery, (DateTime?)null)
                .SetProperty(p => p.ResponsePayload, (string)null)
                .SetProperty(p => p.ResponseHeaders, (string)null));
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    public Task<int> CancelWebhooksLogAttemptsAsync(int tenantId, int id)
    {
        return WebhooksLogs
            .Where(r => r.TenantId == tenantId && r.Id == id)
            .ExecuteUpdateAsync(q => q.SetProperty(p => p.NextAttemptOn, (DateTime?)null));
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    public Task<int> ScheduleWebhooksLogAttemptAsync(int tenantId, int id, int attempts, DateTime? nextAttemptOn)
    {
        return WebhooksLogs
            .Where(r => r.TenantId == tenantId && r.Id == id)
            .ExecuteUpdateAsync(q => q
                .SetProperty(p => p.Attempts, attempts)
                .SetProperty(p => p.NextAttemptOn, nextAttemptOn));
    }
}

static file class Queries
{
    public static readonly Func<WebhooksDbContext, int, string, string, Task<DbWebhooksConfig>> WebhooksConfigByUriAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId, string uri, string name) =>
                ctx.WebhooksConfigs.FirstOrDefault(r => r.TenantId == tenantId && r.Uri == uri && r.Name == name));

    public static readonly Func<WebhooksDbContext, int, Guid?, IAsyncEnumerable<WebhooksConfigWithStatus>> WebhooksConfigWithStatusAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId, Guid? userId) =>
                ctx.WebhooksConfigs.Where(it => it.TenantId == tenantId && (userId == null || it.CreatedBy == userId))
                .GroupJoin(ctx.WebhooksLogs, c => c.Id, l => l.ConfigId, (configs, logs) => new { configs, logs })
                .Select(it => new WebhooksConfigWithStatus
                {
                    WebhooksConfig = it.configs,
                    Status = it.logs.OrderByDescending(webhooksLog => webhooksLog.Delivery).FirstOrDefault().Status
                }));

    public static readonly Func<WebhooksDbContext, int, bool?, IAsyncEnumerable<DbWebhooksConfig>> WebhooksConfigsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId, bool? enabled) =>
                ctx.WebhooksConfigs.Where(it => it.TenantId == tenantId && (enabled == null || it.Enabled == enabled)));

    public static readonly Func<WebhooksDbContext, int, int, Task<DbWebhooksConfig>> WebhooksConfigAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId, int id) =>
                ctx.WebhooksConfigs.FirstOrDefault(it => it.TenantId == tenantId && it.Id == id));

    public static readonly Func<WebhooksDbContext, int, int, Task<DbWebhooks>> WebhooksLogAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId, int id) =>
                ctx.WebhooksLogs
                    .Where(it => it.TenantId == tenantId && it.Id == id)
                    .Join(ctx.WebhooksConfigs, r => r.ConfigId, r => r.Id, (log, config) => new DbWebhooks { Log = log, Config = config })
                    .FirstOrDefault());

    public static readonly Func<WebhooksDbContext, int, int, Task<int?>> WebhooksLogAttemptsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId, int id) =>
                ctx.WebhooksLogs
                    .Where(it => it.TenantId == tenantId && it.Id == id)
                    .Select(it => (int?)it.Attempts)
                    .FirstOrDefault());

    public static readonly Func<WebhooksDbContext, DateTime, int, IAsyncEnumerable<DueWebhooksLog>> DueWebhooksLogsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, DateTime now, int count) =>
                ctx.WebhooksLogs
                    .Where(r => r.NextAttemptOn != null && r.NextAttemptOn <= now && r.Tenant.Status == TenantStatus.Active)
                    .OrderBy(r => r.NextAttemptOn)
                    .ThenBy(r => r.Id)
                    .Take(count)
                    .Select(r => new DueWebhooksLog { TenantId = r.TenantId, Id = r.Id, NextAttemptOn = r.NextAttemptOn }));
}