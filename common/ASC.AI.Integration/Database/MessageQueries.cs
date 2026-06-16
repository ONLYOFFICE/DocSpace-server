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

namespace ASC.AI.Integration.Database;

public partial class AiIntegrationContext
{
    [PreCompileQuery]
    public Task<DbMessage?> GetMessageAsync(int tenantId, Guid id)
    {
        return MessageQueriesContainer.GetMessageAsync(this, tenantId, id);
    }

    [PreCompileQuery]
    public Task<DbMessage?> GetMessageWithThreadAsync(int tenantId, Guid id)
    {
        return MessageQueriesContainer.GetMessageWithThreadAsync(this, tenantId, id);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbMessage> GetMessagesByThreadAsync(int tenantId, Guid threadId, int skip, int take)
    {
        return MessageQueriesContainer.GetMessagesByThreadAsync(this, tenantId, threadId, skip, take);
    }

    [PreCompileQuery]
    public Task<int> UpdateMessageContentsAsync(int tenantId, Guid id, string contents, DateTime timestamp)
    {
        return MessageQueriesContainer.UpdateMessageContentsAsync(this, tenantId, id, contents, timestamp);
    }

    [PreCompileQuery]
    public Task<int> DeleteMessageAsync(int tenantId, Guid id)
    {
        return MessageQueriesContainer.DeleteMessageAsync(this, tenantId, id);
    }

    [PreCompileQuery]
    public Task<int> DeleteMessagesByThreadAsync(int tenantId, Guid threadId)
    {
        return MessageQueriesContainer.DeleteMessagesByThreadAsync(this, tenantId, threadId);
    }
}

static file class MessageQueriesContainer
{
    public static readonly Func<AiIntegrationContext, int, Guid, Task<DbMessage?>> GetMessageAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid id) =>
                ctx.Messages.FirstOrDefault(x => x.TenantId == tenantId && x.Id == id));

    public static readonly Func<AiIntegrationContext, int, Guid, Task<DbMessage?>> GetMessageWithThreadAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid id) =>
                ctx.Messages
                    .Include(x => x.Thread)
                    .FirstOrDefault(x => x.TenantId == tenantId && x.Id == id));

    public static readonly Func<AiIntegrationContext, int, Guid, int, int, IAsyncEnumerable<DbMessage>> GetMessagesByThreadAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid threadId, int skip, int take) =>
                ctx.Messages
                    .Where(x => x.TenantId == tenantId && x.ThreadId == threadId)
                    .OrderBy(x => x.Timestamp)
                    .Skip(skip)
                    .Take(take));

    public static readonly Func<AiIntegrationContext, int, Guid, string, DateTime, Task<int>> UpdateMessageContentsAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid id, string contents, DateTime timestamp) =>
                ctx.Messages
                    .Where(x => x.TenantId == tenantId && x.Id == id)
                    .ExecuteUpdate(x => x
                        .SetProperty(y => y.Contents, contents)
                        .SetProperty(y => y.Timestamp, timestamp)));

    public static readonly Func<AiIntegrationContext, int, Guid, Task<int>> DeleteMessageAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid id) =>
                ctx.Messages
                    .Where(x => x.TenantId == tenantId && x.Id == id)
                    .ExecuteDelete());

    public static readonly Func<AiIntegrationContext, int, Guid, Task<int>> DeleteMessagesByThreadAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid threadId) =>
                ctx.Messages
                    .Where(x => x.TenantId == tenantId && x.ThreadId == threadId)
                    .ExecuteDelete());
}
