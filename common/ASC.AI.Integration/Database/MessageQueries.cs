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

namespace ASC.AI.Integration.Database;

public partial class AiIntegrationContext
{
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<DbMessage?> GetMessageAsync(int tenantId, Guid id)
    {
        return MessageQueriesContainer.GetMessageAsync(this, tenantId, id);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, 0, int.MaxValue])]
    public IAsyncEnumerable<DbMessage> GetMessagesByThreadAsync(int tenantId, Guid threadId, int skip, int take)
    {
        return MessageQueriesContainer.GetMessagesByThreadAsync(this, tenantId, threadId, skip, take);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, null, PreCompileQuery.DefaultDateTime])]
    public Task<int> UpdateMessageContentsAsync(int tenantId, Guid id, string contents, DateTime timestamp)
    {
        return MessageQueriesContainer.UpdateMessageContentsAsync(this, tenantId, id, contents, timestamp);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<int> DeleteMessageAsync(int tenantId, Guid id)
    {
        return MessageQueriesContainer.DeleteMessageAsync(this, tenantId, id);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
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
