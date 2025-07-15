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

using Message = ASC.AI.Core.Chat.Models.Message;

namespace ASC.AI.Core.Chat.Database;

[Scope]
public class DbChatDao(IDbContextFactory<ChatDbContext> dbContextFactory, IMapper mapper)
{
    public async Task<ChatSession> AddChatAsync(int tenantId, int roomId, Guid userId, string title, Message message)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();
        
        DbChat chat = null!;

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            
            var now = DateTime.UtcNow;
            var id = Guid.NewGuid();

            var dbMessage = new DbChatMessage
            {
                ChatId = id,
                Role = message.Role,
                Content = JsonSerializer.Serialize(message.Contents, AiUtils.SerializerOptions),
                CreatedOn = now
            };

            chat = new DbChat
            {
                Id = id,
                TenantId = tenantId,
                RoomId = roomId, 
                UserId = userId,
                Title = title,
                CreatedOn = now,
                ModifiedOn = now,
                Messages = [dbMessage]
            };
            
            await context.Chats.AddAsync(chat);
            await context.SaveChangesAsync();
        });
        
        return mapper.Map<ChatSession>(chat);
    }

    public async Task UpdateChatAsync(int tenantId, Guid chatId, Message message)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        { 
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            await context.UpdateChatAsync(tenantId, chatId, DateTime.UtcNow);
            
            var dbMessage = new DbChatMessage
            {
                ChatId = chatId, 
                Role = message.Role, 
                Content = JsonSerializer.Serialize(message.Contents, AiUtils.SerializerOptions),
                CreatedOn = DateTime.UtcNow
            };
            
            await context.Messages.AddAsync(dbMessage);
            
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    public async Task UpdateChatAsync(ChatSession chatSession)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();
        
        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            await context.UpdateChatAsync(chatSession.TenantId, chatSession.Id, chatSession.Title, chatSession.ModifiedOn);
            
            await context.SaveChangesAsync();
        });
    }

    public async Task<ChatSession?> GetChatAsync(int tenantId, Guid chatId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var chat = await dbContext.GetChatAsync(tenantId, chatId);
        
        return chat == null ? null : mapper.Map<ChatSession>(chat);
    }

    public async IAsyncEnumerable<ChatSession> GetChatsAsync(int tenantId, int roomId, Guid userId, int offset, int limit)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var chats = dbContext.GetChatsAsync(tenantId, roomId, userId, offset, limit);

        await foreach (var chat in chats)
        {
            yield return mapper.Map<ChatSession>(chat);
        }
    }

    public async Task<int> GetChatsTotalCountAsync(int tenantId, int roomId, Guid userId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.GetChatsTotalCountAsync(tenantId, roomId, userId);
    }

    public async Task DeleteChatsAsync(int tenantId, IEnumerable<Guid> chatIds)
    {
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = filesDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await context.DeleteChatsAsync(tenantId, chatIds);
            await context.SaveChangesAsync();
        });
    }

    public async Task AddMessagesAsync(Guid chatId, IEnumerable<Message> messages)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();
        
        var now = DateTime.UtcNow;

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            
            var dbMessages = messages.Select(msg => 
                new DbChatMessage 
                { 
                    ChatId = chatId,
                    Role = msg.Role,
                    Content = JsonSerializer.Serialize(msg.Contents, AiUtils.SerializerOptions),
                    CreatedOn = now
                });

            await context.Messages.AddRangeAsync(dbMessages);
            await context.SaveChangesAsync();
        });
    }
    
    public async IAsyncEnumerable<Message> GetMessagesAsync(Guid chatId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var messages = dbContext.GetMessagesAsync(chatId);

        await foreach (var msg in messages.AsAsyncEnumerable())
        {
            yield return new Message(
                msg.Role, 
                JsonSerializer.Deserialize<List<MessageContent>>(msg.Content, AiUtils.SerializerOptions)!,
                msg.CreatedOn);
        }
    }

    public async IAsyncEnumerable<Message> GetMessagesAsync(Guid chatId, int offset, int limit)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var messages = dbContext.GetMessagesAsync(chatId, offset, limit);

        await foreach (var msg in messages.AsAsyncEnumerable())
        {
            yield return new Message(
                msg.Role, 
                JsonSerializer.Deserialize<List<MessageContent>>(msg.Content, AiUtils.SerializerOptions)!,
                msg.CreatedOn);
        }
    }

    public async Task<int> GetMessagesTotalCountAsync(Guid chatId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.GetMessagesTotalCountAsync(chatId);
    }
}