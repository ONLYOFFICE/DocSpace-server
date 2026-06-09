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

using Thread = ASC.AI.Integration.Threads.Thread;

namespace ASC.AI.Integration.Messages;

[Scope]
public class MessagesStorage(IDbContextFactory<AiIntegrationContext> dbContextFactory)
{
    public async Task<Message> CreateAsync(int tenantId, Guid threadId, string contents)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var entity = new DbMessage
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ThreadId = threadId,
            Contents = contents,
            Timestamp = DateTime.UtcNow
        };

        context.Messages.Add(entity);
        await context.SaveChangesAsync();

        return ToDomainEntity(entity);
    }

    public async Task<ThreadMessage?> ReadByIdAsync(int tenantId, Guid messageId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var entity = await context.GetMessageWithThreadAsync(tenantId, messageId);
        return entity == null ? null : ToThreadMessageEntity(entity);
    }

    public async Task<List<Message>> ReadByThreadAsync(int tenantId, Guid threadId, int? limit = null, int? startIndex = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var skip = startIndex ?? 0;
        var take = limit ?? int.MaxValue;

        return await context.GetMessagesByThreadAsync(tenantId, threadId, skip, take)
            .Select(ToDomainEntity)
            .ToListAsync();
    }

    public async Task UpdateAsync(int tenantId, Guid messageId, string contents)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        await context.UpdateMessageContentsAsync(tenantId, messageId, contents, DateTime.UtcNow);
    }

    public async Task DeleteAsync(int tenantId, Guid messageId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        await context.DeleteMessageAsync(tenantId, messageId);
    }

    public async Task DeleteByThreadAsync(int tenantId, Guid threadId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        await context.DeleteMessagesByThreadAsync(tenantId, threadId);
    }

    private static Message ToDomainEntity(DbMessage entity)
    {
        return new Message
        {
            Id = entity.Id,
            ThreadId = entity.ThreadId,
            Contents = entity.Contents,
            Timestamp = entity.Timestamp
        };
    }

    private static ThreadMessage ToThreadMessageEntity(DbMessage entity)
    {
        return new ThreadMessage
        {
            Message = ToDomainEntity(entity),
            Thread = ToThreadDomainEntity(entity.Thread)
        };
    }

    private static Thread ToThreadDomainEntity(DbThread entity)
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
