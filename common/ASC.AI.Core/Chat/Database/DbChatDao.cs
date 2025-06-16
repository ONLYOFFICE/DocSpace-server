// (c) Copyright Ascensio System SIA 2009-2025
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

using ASC.AI.Core.Chat.Database.Models;

namespace ASC.AI.Core.Chat.Database;

[Scope]
public class DbChatDao(IDbContextFactory<ChatDbContext> dbContextFactory, IMapper mapper)
{
    public async Task AddChatAsync(Guid chatId, int roomId, Guid userId, Message message)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();
            
            var now = DateTime.UtcNow;

            var session = new DbChat
            {
                Id = chatId,
                RoomId = roomId, 
                UserId = userId,
                Title = message.Content[..Math.Min(message.Content.Length, 60)],
                CreatedOn = now,
                ModifiedOn = now
            };
                
            await context.Chats.AddAsync(session);

            var dbChatMessage = new DbChatMessage
            {
                ChatId = chatId, 
                Content = message.Content
            };

            await context.Messages.AddAsync(dbChatMessage);
            
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    public async Task UpdateChatAsync(Chat chat)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();
        
        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            await context.Chats.Where(c => c.Id == chat.Id)
                .ExecuteUpdateAsync(x => 
                    x.SetProperty(y => y.ModifiedOn, DateTime.UtcNow)
                        .SetProperty(y => y.Title, chat.Title));
            
            await context.SaveChangesAsync();
        });
    }

    public async IAsyncEnumerable<Chat> GetChatsAsync(int roomId, Guid userId, int offset, int limit)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var query = dbContext.Chats
            .Where(x => x.RoomId == roomId && x.UserId == userId)
            .OrderByDescending(x => x.ModifiedOn)
            .Skip(offset)
            .Take(limit)
            .AsAsyncEnumerable();

        await foreach (var chat in query)
        {
            yield return mapper.Map<Chat>(chat);
        }
    }

    public async Task DeleteChatsAsync(IEnumerable<Guid> chatIds)
    {
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = filesDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await context.Chats.Where(x => chatIds.Contains(x.Id)).ExecuteDeleteAsync();
            await context.SaveChangesAsync();
        });
    }

    public async Task AddMessagesAsync(Guid chatId, IEnumerable<Message> messages)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            var dbMessages = messages.Select(msg => 
                new DbChatMessage 
                { 
                    ChatId = chatId,
                    Role = msg.Role,
                    Content = msg.Content 
                });

            await context.Messages.AddRangeAsync(dbMessages);
            await context.SaveChangesAsync();
        });
    }

    public async IAsyncEnumerable<Message> GetMessagesAsync(Guid chatId, int? offset = null, int? limit = null)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        IQueryable<DbChatMessage> query = dbContext.Messages
            .Where(x => x.ChatId == chatId)
            .OrderBy(x => x.Id);

        if (offset.HasValue)
        {
            query = query.Skip(offset.Value);
        }

        if (limit is > 0)
        {
            query = query.Take(limit.Value);
        }

        await foreach (var msg in query.AsAsyncEnumerable())
        {
            yield return mapper.Map<Message>(msg);
        }
    }
}