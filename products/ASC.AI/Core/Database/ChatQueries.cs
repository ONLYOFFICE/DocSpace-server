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

namespace ASC.AI.Core.Database;

public partial class AiDbContext
{
    [PreCompileQuery]
    public Task<DbChat?> GetChatAsync(int tenantId, Guid chatId)
    {
        return Queries.GetChatAsync(this, tenantId, chatId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbChat> GetChatsAsync(int tenantId, int roomId, Guid userId, int offset, int limit)
    {
        return Queries.GetChatsAsync(this, tenantId, roomId, userId, offset, limit);
    }

    [PreCompileQuery]
    public Task<int> GetChatsTotalCountAsync(int tenantId, int roomId, Guid userId)
    {
        return Queries.GetChatsTotalCountAsync(this, tenantId, roomId, userId);
    }

    [PreCompileQuery]
    public Task UpdateChatAsync(int tenantId, Guid chatId, DateTime date)
    {
        return Queries.UpdateChatDateAsync(this, tenantId, chatId, date);
    }

    [PreCompileQuery]
    public Task UpdateChatAsync(int tenantId, Guid chatId, string title, DateTime date)
    {
        return Queries.UpdateChatAsync(this, tenantId, chatId, title, date);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbChatMessage> GetMessagesAsync(Guid chatId, int offset, int limit)
    {
        return Queries.GetMessagesAsync(this, chatId, offset, limit);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbChatMessage> GetMessagesAsync(Guid chatId)
    {
        return Queries.GetAllMessagesAsync(this, chatId);
    }

    [PreCompileQuery]
    public Task<int> GetMessagesTotalCountAsync(Guid chatId)
    {
        return Queries.GetMessagesTotalCountAsync(this, chatId);
    }

    [PreCompileQuery]
    public async Task<bool> MarkChatAsDeletedAsync(int tenantId, Guid chatId, Guid userId, DateTime deletedOn)
    {
        return await Queries.MarkChatAsDeletedAsync(this, tenantId, chatId, userId, deletedOn) > 0;
    }

    [PreCompileQuery]
    public Task<DbChatMessage?> GetMessageAsync(int messageId, Guid userId)
    {
        return Queries.GetMessageAsync(this, messageId, userId);
    }

    [PreCompileQuery]
    public Task<DbChat?> GetChatByMessageIdAsync(int messageId)
    {
        return Queries.GetChatByMessageIdAsync(this, messageId);
    }

    [PreCompileQuery]
    public Task<DbUserChatSettings?> GetUserChatSettingsAsync(int tenantId, Guid userId, int roomId)
    {
        return Queries.GetUserChatSettingsAsync(this, tenantId, userId, roomId);
    }

    [PreCompileQuery]
    public Task UpdateChatTitleAsync(int tenantId, Guid chatId, string title)
    {
        return Queries.UpdateChatTitleAsync(this, tenantId, chatId, title);
    }

    [PreCompileQuery]
    public Task<DbChatMessage?> GetUserMessageByAssistantMessageIdAsync(int assistantMessageId, Guid chatId)
    {
        return Queries.GetUserMessageByAssistantMessageIdAsync(this, assistantMessageId, chatId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<int> GetChatAttachmentFileIdsAsync(int tenantId, Guid chatId)
    {
        return Queries.GetChatAttachmentFileIdsAsync(this, tenantId, chatId);
    }

    [PreCompileQuery]
    public Task<int> HardDeleteChatAsync(int tenantId, Guid chatId, Guid userId)
    {
        return Queries.HardDeleteChatAsync(this, tenantId, chatId, userId);
    }

    [PreCompileQuery]
    public Task<int> LinkAttachmentsToMessageAsync(int tenantId, Guid chatId, long messageId, IEnumerable<int> fileIds, DateTime modifiedOn)
    {
        return Queries.LinkAttachmentsToMessageAsync(this, tenantId, chatId, messageId, fileIds, modifiedOn);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<(int TenantId, Guid UserId, Guid ChatId)> GetDeletedChatsAsync(DateTime cutoffDate, int limit)
    {
        return Queries.GetDeletedChatsAsync(this, cutoffDate, limit);
    }

    [PreCompileQuery]
    public Task<int> UpdateDeletedChatsDeletedOnAsync(IEnumerable<Guid> chatIds, DateTime deletedOn)
    {
        return Queries.UpdateDeletedChatsDeletedOnAsync(this, chatIds, deletedOn);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<(int TenantId, int FileId)> GetOrphanedAttachmentsAsync(DateTime cutoffDate)
    {
        return Queries.GetOrphanedAttachmentsAsync(this, cutoffDate);
    }
}

static file class Queries
{
    public static readonly Func<AiDbContext, int, Guid, Task<DbChat?>> GetChatAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, int tenantId, Guid chatId) =>
                ctx.Chats.FirstOrDefault(x => x.TenantId == tenantId && x.Id == chatId && x.DeletedOn == null));

    public static readonly Func<AiDbContext, int, int, Guid, int, int, IAsyncEnumerable<DbChat>> GetChatsAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, int tenantId, int roomId, Guid userId, int offset, int limit) =>
                ctx.Chats
                    .Where(x => x.TenantId == tenantId && x.RoomId == roomId && x.UserId == userId && x.DeletedOn == null)
                    .OrderByDescending(x => x.ModifiedOn)
                    .Skip(offset)
                    .Take(limit));

    public static readonly Func<AiDbContext, int, int, Guid, Task<int>> GetChatsTotalCountAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, int tenantId, int roomId, Guid userId) =>
                ctx.Chats.Count(x => x.TenantId == tenantId && x.RoomId == roomId && x.UserId == userId && x.DeletedOn == null));

    public static readonly Func<AiDbContext, int, Guid, DateTime, Task<int>> UpdateChatDateAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, Guid chatId, DateTime date) =>
            ctx.Chats.Where(x => x.TenantId == tenantId && x.Id == chatId)
                .ExecuteUpdate(x =>
                    x.SetProperty(y => y.ModifiedOn, date)));

    public static readonly Func<AiDbContext, int, Guid, string, DateTime, Task<int>> UpdateChatAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, Guid chatId, string title, DateTime date) =>
            ctx.Chats.Where(x => x.TenantId == tenantId && x.Id == chatId)
                .ExecuteUpdate(x =>
                    x.SetProperty(y => y.ModifiedOn, date)
                        .SetProperty(y => y.Title, title)));

    public static readonly Func<AiDbContext, Guid, int, int, IAsyncEnumerable<DbChatMessage>> GetMessagesAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, Guid chatId, int offset, int limit) =>
                ctx.Messages
                    .Where(x => x.ChatId == chatId)
                    .OrderByDescending(x => x.Id)
                    .Skip(offset)
                    .Take(limit));

    public static readonly Func<AiDbContext, Guid, IAsyncEnumerable<DbChatMessage>> GetAllMessagesAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, Guid chatId) =>
            ctx.Messages
                .Where(x => x.ChatId == chatId)
                .OrderBy(x => x.Id)
                .Select(x => x));

    public static readonly Func<AiDbContext, Guid, Task<int>> GetMessagesTotalCountAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, Guid chatId) =>
                ctx.Messages.Count(x => x.ChatId == chatId));

    public static readonly Func<AiDbContext, int, Guid, Guid, DateTime, Task<int>> MarkChatAsDeletedAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, int tenantId, Guid chatId, Guid userId, DateTime deletedOn) =>
                ctx.Chats.Where(x => x.TenantId == tenantId && x.Id == chatId && x.UserId == userId)
                    .ExecuteUpdate(x => x
                        .SetProperty(y => y.DeletedOn, deletedOn)));

    public static readonly Func<AiDbContext, int, Guid, Task<DbChatMessage?>> GetMessageAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, int messageId, Guid userId) =>
                ctx.Messages.Where(x => x.Id == messageId)
                    .Join(ctx.Chats, x => x.ChatId, y => y.Id, (x, y) => new { Message = x, y.UserId })
                    .Where(x => x.UserId == userId)
                    .Select(x => x.Message)
                    .FirstOrDefault());

    public static readonly Func<AiDbContext, int, Task<DbChat?>> GetChatByMessageIdAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, int messageId) =>
                ctx.Messages.Where(x => x.Id == messageId)
                    .Join(ctx.Chats, x => x.ChatId, y => y.Id, (x, y) => y)
                    .FirstOrDefault());

    public static readonly Func<AiDbContext, int, Guid, int, Task<DbUserChatSettings?>> GetUserChatSettingsAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, Guid userId, int roomId) =>
            ctx.UserChatSettings.FirstOrDefault(x => x.TenantId == tenantId && x.UserId == userId && x.RoomId == roomId));

    public static readonly Func<AiDbContext, int, Guid, string, Task<int>> UpdateChatTitleAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, Guid chatId, string title) =>
            ctx.Chats.Where(x => x.TenantId == tenantId && x.Id == chatId)
                .ExecuteUpdate(x =>
                    x.SetProperty(y => y.Title, title)));

    public static readonly Func<AiDbContext, int, Guid, Task<DbChatMessage?>> GetUserMessageByAssistantMessageIdAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int assistantMessageId, Guid chatId) =>
            ctx.Messages.Where(x => x.ChatId == chatId && x.Role == Role.User && x.Id < assistantMessageId)
                .OrderByDescending(x => x.Id)
                .FirstOrDefault());

    public static readonly Func<AiDbContext, int, Guid, IAsyncEnumerable<int>> GetChatAttachmentFileIdsAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, Guid chatId) =>
            ctx.MessageAttachments
                .Where(x => x.TenantId == tenantId && x.ChatId == chatId)
                .Select(x => x.FileId));

    public static readonly Func<AiDbContext, int, Guid, Guid, Task<int>> HardDeleteChatAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, Guid chatId, Guid userId) =>
            ctx.Chats
                .Where(x => x.TenantId == tenantId && x.Id == chatId && x.UserId == userId)
                .ExecuteDelete());

    public static readonly Func<AiDbContext, int, Guid, long, IEnumerable<int>, DateTime, Task<int>> LinkAttachmentsToMessageAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, Guid chatId, long messageId, IEnumerable<int> fileIds, DateTime modifiedOn) =>
            ctx.MessageAttachments
                .Where(a => a.TenantId == tenantId && a.ChatId == chatId && fileIds.Contains(a.FileId))
                .ExecuteUpdate(s => s
                    .SetProperty(a => a.MessageId, messageId)
                    .SetProperty(a => a.ModifiedOn, modifiedOn)));

    public static readonly Func<AiDbContext, DateTime, int, IAsyncEnumerable<(int TenantId, Guid UserId, Guid ChatId)>> GetDeletedChatsAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, DateTime cutoffDate, int limit) =>
            ctx.Chats
                .Where(x => x.DeletedOn != null && x.DeletedOn <= cutoffDate)
                .Take(limit)
                .Select(x => new ValueTuple<int, Guid, Guid>(x.TenantId, x.UserId, x.Id)));

    public static readonly Func<AiDbContext, IEnumerable<Guid>, DateTime, Task<int>> UpdateDeletedChatsDeletedOnAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, IEnumerable<Guid> chatIds, DateTime deletedOn) =>
            ctx.Chats
                .Where(x => chatIds.Contains(x.Id))
                .ExecuteUpdate(s => s.SetProperty(y => y.DeletedOn, deletedOn)));

    public static readonly Func<AiDbContext, DateTime, IAsyncEnumerable<(int TenantId, int FileId)>> GetOrphanedAttachmentsAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, DateTime cutoffDate) =>
            ctx.MessageAttachments
                .Where(x => x.MessageId == null && x.ModifiedOn <= cutoffDate)
                .Select(x => new ValueTuple<int, int>(x.TenantId, x.FileId)));
}
