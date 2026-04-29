// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.AI.Integration.Messages;

[Scope]
public class MessagesStorage(IDbContextFactory<AiIntegrationContext> dbContextFactory)
{
    public async Task<Message?> CreateAsync(int tenantId, Guid threadId, string contents)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var thread = await context.GetThreadAsync(tenantId, threadId);
        if (thread == null)
        {
            return null;
        }

        var entity = new DbMessage
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ThreadId = threadId,
            Contents = contents,
            Timestamp = DateTime.UtcNow
        };

        await context.Messages.AddAsync(entity);
        await context.SaveChangesAsync();

        return ToDomainEntity(entity);
    }

    public async Task<Message?> ReadByIdAsync(int tenantId, Guid messageId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var entity = await context.GetMessageAsync(tenantId, messageId);
        return entity == null ? null : ToDomainEntity(entity);
    }

    public async Task<List<Message>> ReadByThreadAsync(int tenantId, Guid threadId, int? limit = null, int? startIndex = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var skip = startIndex ?? 0;
        var take = limit ?? int.MaxValue;

        var result = new List<Message>();
        await foreach (var entity in context.GetMessagesByThreadAsync(tenantId, threadId, skip, take))
        {
            result.Add(ToDomainEntity(entity));
        }

        return result;
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
}
