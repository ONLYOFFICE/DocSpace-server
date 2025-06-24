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

using FirebaseAdmin.Auth;

namespace ASC.AI.Core.Chat.Database;

public partial class ChatDbContext
{
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<DbChat?> GetChatAsync(int tenantId, Guid chatId)
    {
        return Queries.GetChatAsync(this, tenantId, chatId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbChat> GetChatsAsync(int tenantId, int roomId, Guid userId, int offset, int limit)
    {
        return Queries.GetChatsAsync(this, tenantId, roomId, userId, offset, limit);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<int> GetChatsTotalCountAsync(int tenantId, int roomId, Guid userId)
    {
        return Queries.GetChatsTotalCountAsync(this, tenantId, roomId, userId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, PreCompileQuery.DefaultDateTime])]
    public Task UpdateChatAsync(int tenantId, Guid chatId, DateTime date)
    {
        return Queries.UpdateChatDateAsync(this, tenantId, chatId, date);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, null, PreCompileQuery.DefaultDateTime])]
    public Task UpdateChatAsync(int tenantId, Guid chatId, string title, DateTime date)
    {
        return Queries.UpdateChatAsync(this, tenantId, chatId, title, date);
    }

    [PreCompileQuery([PreCompileQuery.DefaultGuid, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbChatMessage> GetMessagesAsync(Guid chatId, int offset, int limit)
    {
        return Queries.GetMessagesAsync(this, chatId, offset, limit);
    }

    [PreCompileQuery([PreCompileQuery.DefaultGuid])]
    public IAsyncEnumerable<DbChatMessage> GetMessagesAsync(Guid chatId)
    {
        return Queries.GetAllMessagesAsync(this, chatId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultGuid])]
    public Task<int> GetMessagesTotalCountAsync(Guid chatId)
    {
        return Queries.GetMessagesTotalCountAsync(this, chatId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task DeleteChatsAsync(int tenantId, IEnumerable<Guid> chatIds)
    {
        return Queries.DeleteChatsAsync(this, tenantId, chatIds);
    }
}

static file class Queries
{
    public static readonly Func<ChatDbContext, int, Guid, Task<DbChat?>> GetChatAsync =
        EF.CompileAsyncQuery(
            (ChatDbContext ctx, int tenantId, Guid chatId) => 
                ctx.Chats.FirstOrDefault(x => x.TenantId == tenantId && x.Id == chatId));
    
    public static readonly Func<ChatDbContext, int, int, Guid, int, int, IAsyncEnumerable<DbChat>> GetChatsAsync =
        EF.CompileAsyncQuery(
            (ChatDbContext ctx, int tenantId, int roomId, Guid userId, int offset, int limit) => 
                ctx.Chats
                    .Where(x => x.TenantId == tenantId && x.RoomId == roomId && x.UserId == userId)
                    .OrderByDescending(x => x.ModifiedOn)
                    .Skip(offset)
                    .Take(limit));
    
    public static readonly Func<ChatDbContext, int, int, Guid, Task<int>> GetChatsTotalCountAsync =
        EF.CompileAsyncQuery(
            (ChatDbContext ctx, int tenantId, int roomId, Guid userId) => 
                ctx.Chats.Count(x => x.TenantId == tenantId && x.RoomId == roomId && x.UserId == userId));

    public static readonly Func<ChatDbContext, int, Guid, DateTime, Task<int>> UpdateChatDateAsync =
        EF.CompileAsyncQuery((ChatDbContext ctx, int tenantId, Guid chatId, DateTime date) =>
            ctx.Chats.Where(x => x.TenantId == tenantId && x.Id == chatId)
                .ExecuteUpdate(x =>
                    x.SetProperty(y => y.ModifiedOn, date)));
    
    public static readonly Func<ChatDbContext, int, Guid, string, DateTime, Task<int>> UpdateChatAsync =
        EF.CompileAsyncQuery((ChatDbContext ctx, int tenantId, Guid chatId, string title, DateTime date) =>
            ctx.Chats.Where(x => x.TenantId == tenantId && x.Id == chatId)
                .ExecuteUpdate(x =>
                    x.SetProperty(y => y.ModifiedOn, date)
                        .SetProperty(y => y.Title, title)));
    
    public static readonly Func<ChatDbContext, Guid, int, int, IAsyncEnumerable<DbChatMessage>> GetMessagesAsync =
        EF.CompileAsyncQuery(
            (ChatDbContext ctx, Guid chatId, int offset, int limit) => 
                ctx.Messages
                    .Where(x => x.ChatId == chatId)
                    .OrderByDescending(x => x.Id)
                    .Skip(offset)
                    .Take(limit));

    public static readonly Func<ChatDbContext, Guid, IAsyncEnumerable<DbChatMessage>> GetAllMessagesAsync =
        EF.CompileAsyncQuery((ChatDbContext ctx, Guid chatId) =>
            ctx.Messages
                .Where(x => x.ChatId == chatId)
                .OrderBy(x => x.Id)
                .Select(x => x));
    
    public static readonly Func<ChatDbContext, Guid, Task<int>> GetMessagesTotalCountAsync =
        EF.CompileAsyncQuery(
            (ChatDbContext ctx, Guid chatId) => 
                ctx.Messages.Count(x => x.ChatId == chatId));
    
    public static readonly Func<ChatDbContext, int, IEnumerable<Guid>, Task<int>> DeleteChatsAsync =
        EF.CompileAsyncQuery(
            (ChatDbContext ctx, int tenantId, IEnumerable<Guid> chatIds) => 
                ctx.Chats.Where(x => x.TenantId == tenantId && chatIds.Contains(x.Id)).ExecuteDelete());
}