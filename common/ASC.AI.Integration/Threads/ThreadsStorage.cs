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

namespace ASC.AI.Integration.Threads;

[Scope]
public class ThreadsStorage(IDbContextFactory<AiIntegrationContext> dbContextFactory)
{
    public async Task<Thread> CreateAsync(int tenantId, Guid createdBy, string title, Guid? profileId = null, int? entryId = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var now = TruncateToMilliseconds(DateTime.UtcNow);
        var entity = new DbThread
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Title = title,
            ProfileId = profileId,
            EntryId = entryId,
            CreatedBy = createdBy,
            LastEditDate = now,
            CreatedAt = now
        };

        context.Threads.Add(entity);
        await context.SaveChangesAsync();

        return ToDomainEntity(entity);
    }

    public async Task<Thread?> ReadByIdAsync(int tenantId, Guid threadId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var entity = await context.GetThreadAsync(tenantId, threadId);
        return entity == null ? null : ToDomainEntity(entity);
    }

    public async Task<List<Thread>> ReadAllAsync(int tenantId, Guid createdBy, int count, int? entryId = null, ThreadsCursor? cursor = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var threads = cursor == null
            ? entryId.HasValue
                ? context.GetThreadsByEntryAsync(tenantId, createdBy, entryId.Value, count)
                : context.GetThreadsAsync(tenantId, createdBy, count)
            : entryId.HasValue
                ? context.GetThreadsByEntryFromCursorAsync(tenantId, createdBy, entryId.Value, cursor.LastEditDate, cursor.Id, count)
                : context.GetThreadsFromCursorAsync(tenantId, createdBy, cursor.LastEditDate, cursor.Id, count);

        return await threads
            .Select(ToDomainEntity)
            .ToListAsync();
    }

    public async Task UpdateAsync(int tenantId, Guid threadId, string? title)
    {
        if (title == null)
        {
            return;
        }

        await using var context = await dbContextFactory.CreateDbContextAsync();
        await context.UpdateThreadTitleAsync(tenantId, threadId, title, TruncateToMilliseconds(DateTime.UtcNow));
    }

    public async Task TouchAsync(int tenantId, Guid threadId, DateTime lastEditDate, Guid? profileId = null, bool clearProfile = false)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        if (clearProfile || profileId.HasValue)
        {
            var newProfileId = clearProfile ? null : profileId;
            await context.TouchThreadWithProfileAsync(tenantId, threadId, lastEditDate, newProfileId);
        }
        else
        {
            await context.TouchThreadAsync(tenantId, threadId, lastEditDate);
        }
    }

    public async Task DeleteAsync(int tenantId, Guid threadId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        await context.DeleteThreadAsync(tenantId, threadId);
    }

    private static DateTime TruncateToMilliseconds(DateTime value)
    {
        return new DateTime(value.Ticks - value.Ticks % TimeSpan.TicksPerMillisecond, value.Kind);
    }

    private static Thread ToDomainEntity(DbThread entity)
    {
        return new Thread
        {
            Id = entity.Id,
            Title = entity.Title,
            ProfileId = entity.ProfileId,
            EntryId = entity.EntryId,
            CreatedBy = entity.CreatedBy,
            LastEditDate = entity.LastEditDate,
            CreatedAt = entity.CreatedAt
        };
    }
}
