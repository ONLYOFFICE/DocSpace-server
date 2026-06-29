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

namespace ASC.Data.Backup.Storage;

[Scope]
public class BackupRepository(IDbContextFactory<BackupsContext> dbContextFactory, CreatorDbContext creatorDbContext) : IBackupRepository
{
    public async Task SaveBackupRecordAsync(BackupRecord backupRecord)
    {
        await using var backupContext = await dbContextFactory.CreateDbContextAsync();
        await backupContext.AddOrUpdateAsync(b => b.Backups, backupRecord);
        await backupContext.SaveChangesAsync();
    }

    public async Task<BackupRecord> GetBackupRecordAsync(Guid id)
    {
        await using var backupContext = await dbContextFactory.CreateDbContextAsync();
        return await backupContext.Backups.FindAsync(id);
    }

    public async Task<BackupRecord> GetBackupRecordAsync(string hash, int tenant)
    {
        await using var backupContext = await dbContextFactory.CreateDbContextAsync();
        return await Queries.BackupAsync(backupContext, tenant, hash);
    }

    public async Task<List<BackupRecord>> GetExpiredBackupRecordsAsync()
    {
        await using var backupContext = await dbContextFactory.CreateDbContextAsync();
        return await Queries.ExpiredBackupsAsync(backupContext).ToListAsync();
    }

    public async Task<List<BackupRecord>> GetScheduledBackupRecordsAsync()
    {
        await using var backupContext = await dbContextFactory.CreateDbContextAsync();
        return await Queries.ScheduledBackupsAsync(backupContext).ToListAsync();
    }

    public async Task<List<BackupRecord>> GetBackupRecordsByTenantIdAsync(int tenantId)
    {
        await using var backupContext = await dbContextFactory.CreateDbContextAsync();
        return await Queries.BackupsAsync(backupContext, tenantId).ToListAsync();
    }

    public async Task MigrationBackupRecordsAsync(int tenantId, int newTenantId, string region)
    {
        await using var backupContext = await dbContextFactory.CreateDbContextAsync();

        var backups = await Queries.BackupsForMigrationAsync(backupContext, tenantId).ToListAsync();

        backups.ForEach(backup =>
        {
            backup.TenantId = newTenantId;
            backup.Id = Guid.NewGuid();
        });

        await using var backupContextByNewTenant = creatorDbContext.CreateDbContext<BackupsContext>(region);
        await backupContextByNewTenant.Backups.AddRangeAsync(backups);
        await backupContextByNewTenant.SaveChangesAsync();
    }

    public async Task DeleteBackupRecordAsync(Guid id)
    {
        await using var backupContext = await dbContextFactory.CreateDbContextAsync();

        var backup = await backupContext.Backups.FindAsync(id);
        if (backup != null)
        {
            backup.Removed = true;
            backupContext.Update(backup);
            await backupContext.SaveChangesAsync();
        }
    }

    public async Task SaveBackupScheduleAsync(BackupSchedule schedule)
    {
        await using var backupContext = await dbContextFactory.CreateDbContextAsync();
        await backupContext.AddOrUpdateAsync(q => q.Schedules, schedule);
        await backupContext.SaveChangesAsync();
    }

    public async Task DeleteBackupScheduleAsync(int tenantId, string storageBasePath = null)
    {
        await using var backupContext = await dbContextFactory.CreateDbContextAsync();
        await Queries.DeleteSchedulesAsync(backupContext, tenantId, storageBasePath);
    }

    public async Task<List<BackupSchedule>> GetBackupSchedulesAsync()
    {
        await using var backupContext = await dbContextFactory.CreateDbContextAsync();
        return await Queries.BackupSchedulesAsync(backupContext).ToListAsync();
    }

    public async Task<BackupSchedule> GetBackupScheduleAsync(int tenantId, bool? dump)
    {
        await using var backupContext = await dbContextFactory.CreateDbContextAsync();
        if (dump.HasValue)
        {
            return await Queries.BackupScheduleWithDumpAsync(backupContext, tenantId, dump.Value);
        }

        return await Queries.BackupScheduleAsync(backupContext, tenantId);
    }

    public async Task<int> GetBackupsCountAsync(int tenantId, bool paid, DateTime from, DateTime to)
    {
        await using var backupContext = await dbContextFactory.CreateDbContextAsync();
        return await Queries.GetBackupsCount(backupContext, tenantId, paid, from, to);
    }

    public async Task<(int Free, int Paid)> GetBackupsCountAsync(int tenantId, DateTime from, DateTime to)
    {
        await using var backupContext = await dbContextFactory.CreateDbContextAsync();

        var counts = await Queries.GetBackupsCountByPaid(backupContext, tenantId, from, to).ToListAsync();

        var free = counts.FirstOrDefault(c => !c.Paid)?.Count ?? 0;
        var paid = counts.FirstOrDefault(c => c.Paid)?.Count ?? 0;

        return (free, paid);
    }
}

file class BackupPaidCount
{
    public bool Paid { get; init; }
    public int Count { get; init; }
}

static file class Queries
{

    public static readonly Func<BackupsContext, int, string, Task<BackupRecord>> BackupAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (BackupsContext ctx, int tenantId, string hash) =>
                ctx.Backups

                    .SingleOrDefault(b => b.Hash == hash && b.TenantId == tenantId));

    public static readonly Func<BackupsContext, IAsyncEnumerable<BackupRecord>> ExpiredBackupsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (BackupsContext ctx) =>
                ctx.Backups

                    .Where(b => b.ExpiresOn != DateTime.MinValue
                                && b.ExpiresOn <= DateTime.UtcNow
                                && b.Removed == false));

    public static readonly Func<BackupsContext, IAsyncEnumerable<BackupRecord>> ScheduledBackupsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (BackupsContext ctx) =>
                ctx.Backups

                    .Where(b => b.IsScheduled == true && b.Removed == false));

    public static readonly Func<BackupsContext, int, IAsyncEnumerable<BackupRecord>> BackupsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (BackupsContext ctx, int tenantId) =>
                ctx.Backups

                    .Where(b => b.TenantId == tenantId && b.Removed == false));

    public static readonly Func<BackupsContext, int, IAsyncEnumerable<BackupRecord>> BackupsForMigrationAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (BackupsContext ctx, int tenantId) =>
                ctx.Backups

                    .Where(b => b.TenantId == tenantId));

    public static readonly Func<BackupsContext, int, string, Task<int>> DeleteSchedulesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (BackupsContext ctx, int tenantId, string storageBasePath) =>
                ctx.Schedules
                    .Where(s => s.TenantId == tenantId)
                    .Where(r => string.IsNullOrEmpty(storageBasePath) || r.StorageBasePath.StartsWith(storageBasePath))
                    .ExecuteDelete());

    public static readonly Func<BackupsContext, IAsyncEnumerable<BackupSchedule>> BackupSchedulesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (BackupsContext ctx) =>
                ctx.Schedules.Join(ctx.Tenants,
                        s => s.TenantId,
                        t => t.Id,
                        (s, t) => new { schedule = s, tenant = t })
                    .Where(q => q.tenant.Status == TenantStatus.Active || q.tenant.Id == -1)
                    .Select(q => q.schedule));

    public static readonly Func<BackupsContext, int, bool, Task<BackupSchedule>> BackupScheduleWithDumpAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (BackupsContext ctx, int tenantId, bool dump) =>
                ctx.Schedules
                    .SingleOrDefault(s => s.TenantId == tenantId && s.Dump == dump));

    public static readonly Func<BackupsContext, int, Task<BackupSchedule>> BackupScheduleAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (BackupsContext ctx, int tenantId) =>
                ctx.Schedules
                    .SingleOrDefault(s => s.TenantId == tenantId));

    public static readonly Func<BackupsContext, int, bool, DateTime, DateTime, Task<int>> GetBackupsCount =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (BackupsContext ctx, int tenantId, bool paid, DateTime from, DateTime to) =>
                ctx.Backups.Count(b => b.TenantId == tenantId && b.Paid == paid && b.CreatedOn >= from && b.CreatedOn <= to));

    public static readonly Func<BackupsContext, int, DateTime, DateTime, IAsyncEnumerable<BackupPaidCount>> GetBackupsCountByPaid =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (BackupsContext ctx, int tenantId, DateTime from, DateTime to) =>
                ctx.Backups
                    .Where(b => b.TenantId == tenantId && b.CreatedOn >= from && b.CreatedOn <= to)
                    .GroupBy(b => b.Paid)
                    .Select(g => new BackupPaidCount { Paid = g.Key, Count = g.Count() }));
}
