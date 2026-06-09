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
    public Task<DbThread?> GetThreadAsync(int tenantId, Guid id)
    {
        return ThreadQueriesContainer.GetThreadAsync(this, tenantId, id);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbThread> GetAllThreadsAsync(int tenantId, Guid createdBy)
    {
        return ThreadQueriesContainer.GetAllThreadsAsync(this, tenantId, createdBy);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbThread> GetAllThreadsByEntryAsync(int tenantId, Guid createdBy, int entryId)
    {
        return ThreadQueriesContainer.GetAllThreadsByEntryAsync(this, tenantId, createdBy, entryId);
    }

    [PreCompileQuery]
    public Task<int> UpdateThreadTitleAsync(int tenantId, Guid id, string title)
    {
        return ThreadQueriesContainer.UpdateThreadTitleAsync(this, tenantId, id, title);
    }

    [PreCompileQuery]
    public Task<int> TouchThreadAsync(int tenantId, Guid id, DateTime lastEditDate)
    {
        return ThreadQueriesContainer.TouchThreadAsync(this, tenantId, id, lastEditDate);
    }

    [PreCompileQuery]
    public Task<int> TouchThreadWithProfileAsync(int tenantId, Guid id, DateTime lastEditDate, Guid? profileId)
    {
        return ThreadQueriesContainer.TouchThreadWithProfileAsync(this, tenantId, id, lastEditDate, profileId);
    }

    [PreCompileQuery]
    public Task<int> DeleteThreadAsync(int tenantId, Guid id)
    {
        return ThreadQueriesContainer.DeleteThreadAsync(this, tenantId, id);
    }

    [PreCompileQuery]
    public Task<int> ClearThreadsProfileAsync(int tenantId, Guid profileId)
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

    public static readonly Func<AiIntegrationContext, int, Guid, IAsyncEnumerable<DbThread>> GetAllThreadsAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid createdBy) =>
                ctx.Threads
                    .Where(x => x.TenantId == tenantId && x.CreatedBy == createdBy && x.EntryId == null)
                    .OrderByDescending(x => x.LastEditDate)
                    .AsQueryable());

    public static readonly Func<AiIntegrationContext, int, Guid, int, IAsyncEnumerable<DbThread>> GetAllThreadsByEntryAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid createdBy, int entryId) =>
                ctx.Threads
                    .Where(x => x.TenantId == tenantId && x.CreatedBy == createdBy && x.EntryId == entryId)
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

    public static readonly Func<AiIntegrationContext, int, Guid, DateTime, Guid?, Task<int>> TouchThreadWithProfileAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid id, DateTime lastEditDate, Guid? profileId) =>
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

    public static readonly Func<AiIntegrationContext, int, Guid, Task<int>> ClearThreadsProfileAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid profileId) =>
                ctx.Threads
                    .Where(x => x.TenantId == tenantId && x.ProfileId == profileId)
                    .ExecuteUpdate(x => x.SetProperty(y => y.ProfileId, (Guid?)null)));
}
