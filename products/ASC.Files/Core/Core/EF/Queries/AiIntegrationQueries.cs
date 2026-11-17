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

namespace ASC.Files.Core.EF;

public partial class FilesDbContext
{
    [PreCompileQuery]
    public Task DeleteThreadsByFolderIdsAsync(int tenantId, IEnumerable<int> folderIds)
    {
        return AiIntegrationQueries.DeleteThreadsByFolderIdsAsync(this, tenantId, folderIds);
    }

    [PreCompileQuery]
    public Task DeleteAssignmentsByFolderIdsAsync(int tenantId, IEnumerable<int> folderIds)
    {
        return AiIntegrationQueries.DeleteAssignmentsByFolderIdsAsync(this, tenantId, folderIds);
    }

    [PreCompileQuery]
    public Task DeleteMcpServersByFolderIdsAsync(int tenantId, IEnumerable<int> folderIds)
    {
        return AiIntegrationQueries.DeleteMcpServersByFolderIdsAsync(this, tenantId, folderIds);
    }

    [PreCompileQuery]
    public Task DeleteMcpServerToolPrefsByFolderIdsAsync(int tenantId, IEnumerable<int> folderIds)
    {
        return AiIntegrationQueries.DeleteMcpServerToolPrefsByFolderIdsAsync(this, tenantId, folderIds);
    }

    [PreCompileQuery]
    public Task<int> SetThreadArtifactsFolderAsync(int tenantId, Guid threadId, int folderId)
    {
        return AiIntegrationQueries.SetThreadArtifactsFolderAsync(this, tenantId, threadId, folderId);
    }

    [PreCompileQuery]
    public Task<int> SetThreadAttachmentsFolderAsync(int tenantId, Guid threadId, int folderId)
    {
        return AiIntegrationQueries.SetThreadAttachmentsFolderAsync(this, tenantId, threadId, folderId);
    }

    [PreCompileQuery]
    public Task<int> TouchLiveThreadAsync(int tenantId, Guid threadId, DateTime lastEditDate)
    {
        return AiIntegrationQueries.TouchLiveThreadAsync(this, tenantId, threadId, lastEditDate);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<AiAgentChatBinding> AiAgentChatBindingsAsync(int tenantId)
    {
        return AiIntegrationQueries.AiAgentChatBindingsAsync(this, tenantId);
    }
}

static file class AiIntegrationQueries
{
    public static readonly Func<FilesDbContext, int, IEnumerable<int>, Task> DeleteThreadsByFolderIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery((FilesDbContext ctx, int tenantId, IEnumerable<int> folderIds) =>
            ctx.Threads
                .Where(x => x.TenantId == tenantId && x.EntryId != null && folderIds.Contains(x.EntryId.Value))
                .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, Task> DeleteAssignmentsByFolderIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery((FilesDbContext ctx, int tenantId, IEnumerable<int> folderIds) =>
            ctx.Assignments
                .Where(x => x.TenantId == tenantId && x.EntryId != null && folderIds.Contains(x.EntryId.Value))
                .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, Task> DeleteMcpServersByFolderIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery((FilesDbContext ctx, int tenantId, IEnumerable<int> folderIds) =>
            ctx.McpServers
                .Where(x => x.TenantId == tenantId && x.EntryId != null && folderIds.Contains(x.EntryId.Value))
                .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, Task> DeleteMcpServerToolPrefsByFolderIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery((FilesDbContext ctx, int tenantId, IEnumerable<int> folderIds) =>
            ctx.ToolPrefs
                .Where(x => x.TenantId == tenantId && x.EntryId != null && folderIds.Contains(x.EntryId.Value))
                .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, Guid, int, Task<int>> SetThreadArtifactsFolderAsync =
        (FilesDbContext ctx, int tenantId, Guid threadId, int folderId) =>
            ctx.Threads
                .Where(x => x.TenantId == tenantId && x.Id == threadId && x.ArtifactsFolderId == null && x.DeletedOn == null)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.ArtifactsFolderId, folderId));

    public static readonly Func<FilesDbContext, int, Guid, int, Task<int>> SetThreadAttachmentsFolderAsync =
        (FilesDbContext ctx, int tenantId, Guid threadId, int folderId) =>
            ctx.Threads
                .Where(x => x.TenantId == tenantId && x.Id == threadId && x.AttachmentsFolderId == null && x.DeletedOn == null)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.AttachmentsFolderId, folderId));

    public static readonly Func<FilesDbContext, int, Guid, DateTime, Task<int>> TouchLiveThreadAsync =
        (FilesDbContext ctx, int tenantId, Guid threadId, DateTime lastEditDate) =>
            ctx.Threads
                .Where(x => x.TenantId == tenantId && x.Id == threadId && x.DeletedOn == null)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.LastEditDate, lastEditDate));

    public static readonly Func<FilesDbContext, int, IAsyncEnumerable<AiAgentChatBinding>> AiAgentChatBindingsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery((FilesDbContext ctx, int tenantId) =>
            ctx.Folders
                .Where(f => f.TenantId == tenantId && f.FolderType == FolderType.AiRoom)
                .Join(ctx.Set<DbRoomChatSettings>(),
                    f => new { f.TenantId, RoomId = f.Id },
                    s => new { s.TenantId, s.RoomId },
                    (f, s) => new AiAgentChatBinding(s.RoomId, s.ChatProviderId, s.ChatParameters == null ? null : s.ChatParameters.ModelId)));
}
