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
    public Task<DbThread?> GetThreadAsync(int tenantId, Guid id)
    {
        return ThreadQueriesContainer.GetThreadAsync(this, tenantId, id);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbThread> GetAllThreadsAsync(int tenantId)
    {
        return ThreadQueriesContainer.GetAllThreadsAsync(this, tenantId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, null])]
    public Task<int> UpdateThreadTitleAsync(int tenantId, Guid id, string title)
    {
        return ThreadQueriesContainer.UpdateThreadTitleAsync(this, tenantId, id, title);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, PreCompileQuery.DefaultDateTime])]
    public Task<int> TouchThreadAsync(int tenantId, Guid id, DateTime lastEditDate)
    {
        return ThreadQueriesContainer.TouchThreadAsync(this, tenantId, id, lastEditDate);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, PreCompileQuery.DefaultDateTime, null])]
    public Task<int> TouchThreadWithProfileAsync(int tenantId, Guid id, DateTime lastEditDate, int? profileId)
    {
        return ThreadQueriesContainer.TouchThreadWithProfileAsync(this, tenantId, id, lastEditDate, profileId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<int> DeleteThreadAsync(int tenantId, Guid id)
    {
        return ThreadQueriesContainer.DeleteThreadAsync(this, tenantId, id);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<int> ClearThreadsProfileAsync(int tenantId, int profileId)
    {
        return ThreadQueriesContainer.ClearThreadsProfileAsync(this, tenantId, profileId);
    }
}

static file class ThreadQueriesContainer
{
    public static readonly Func<AiIntegrationContext, int, Guid, Task<DbThread?>> GetThreadAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid id) =>
                ctx.Threads.FirstOrDefault(x => x.TenantId == tenantId && x.Id == id));

    public static readonly Func<AiIntegrationContext, int, IAsyncEnumerable<DbThread>> GetAllThreadsAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId) =>
                ctx.Threads
                    .Where(x => x.TenantId == tenantId)
                    .OrderByDescending(x => x.LastEditDate)
                    .AsQueryable());

    public static readonly Func<AiIntegrationContext, int, Guid, string, Task<int>> UpdateThreadTitleAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid id, string title) =>
                ctx.Threads
                    .Where(x => x.TenantId == tenantId && x.Id == id)
                    .ExecuteUpdate(x => x.SetProperty(y => y.Title, title)));

    public static readonly Func<AiIntegrationContext, int, Guid, DateTime, Task<int>> TouchThreadAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid id, DateTime lastEditDate) =>
                ctx.Threads
                    .Where(x => x.TenantId == tenantId && x.Id == id)
                    .ExecuteUpdate(x => x.SetProperty(y => y.LastEditDate, lastEditDate)));

    public static readonly Func<AiIntegrationContext, int, Guid, DateTime, int?, Task<int>> TouchThreadWithProfileAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid id, DateTime lastEditDate, int? profileId) =>
                ctx.Threads
                    .Where(x => x.TenantId == tenantId && x.Id == id)
                    .ExecuteUpdate(x => x
                        .SetProperty(y => y.LastEditDate, lastEditDate)
                        .SetProperty(y => y.ProfileId, profileId)));

    public static readonly Func<AiIntegrationContext, int, Guid, Task<int>> DeleteThreadAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid id) =>
                ctx.Threads
                    .Where(x => x.TenantId == tenantId && x.Id == id)
                    .ExecuteDelete());

    public static readonly Func<AiIntegrationContext, int, int, Task<int>> ClearThreadsProfileAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, int profileId) =>
                ctx.Threads
                    .Where(x => x.TenantId == tenantId && x.ProfileId == profileId)
                    .ExecuteUpdate(x => x.SetProperty(y => y.ProfileId, (int?)null)));
}
