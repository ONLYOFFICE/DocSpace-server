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
    public IAsyncEnumerable<int> AiAgentRoomIdsAsync(int tenantId, IEnumerable<int> parentIds)
    {
        return AiAgentMigrationQueries.AiAgentRoomIdsAsync(this, tenantId, parentIds);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbFolder> AiAgentChildFoldersAsync(int tenantId, int roomId)
    {
        return AiAgentMigrationQueries.AiAgentChildFoldersAsync(this, tenantId, roomId);
    }

    [PreCompileQuery]
    public Task<DbFolder> AiAgentKnowledgeFolderAsync(int tenantId, int roomId)
    {
        return AiAgentMigrationQueries.AiAgentKnowledgeFolderAsync(this, tenantId, roomId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<int> AiAgentKnowledgeFileIdsAsync(int tenantId, int folderId)
    {
        return AiAgentMigrationQueries.AiAgentKnowledgeFileIdsAsync(this, tenantId, folderId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<int> AiAgentNestedFileIdsAsync(int tenantId, int folderId)
    {
        return AiAgentMigrationQueries.AiAgentNestedFileIdsAsync(this, tenantId, folderId);
    }

    [PreCompileQuery]
    public Task<int> TouchAiAgentSubtreeFilesAsync(int tenantId, int folderId, DateTime modifiedOn)
    {
        return AiAgentMigrationQueries.TouchAiAgentSubtreeFilesAsync(this, tenantId, folderId, modifiedOn);
    }

    [PreCompileQuery]
    public Task<int> TouchAiAgentSubtreeFoldersAsync(int tenantId, int folderId, DateTime modifiedOn)
    {
        return AiAgentMigrationQueries.TouchAiAgentSubtreeFoldersAsync(this, tenantId, folderId, modifiedOn);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<int> AiAgentSubtreeFolderIdsAsync(int tenantId, int folderId)
    {
        return AiAgentMigrationQueries.AiAgentSubtreeFolderIdsAsync(this, tenantId, folderId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<int> AiAgentSubtreeFileIdsAsync(int tenantId, int folderId)
    {
        return AiAgentMigrationQueries.AiAgentSubtreeFileIdsAsync(this, tenantId, folderId);
    }

    [PreCompileQuery]
    public Task<int> ClaimAiAgentRoomAsync(int tenantId, int roomId, string title, Guid modifiedBy, DateTime modifiedOn)
    {
        return AiAgentMigrationQueries.ClaimAiAgentRoomAsync(this, tenantId, roomId, title, modifiedBy, modifiedOn);
    }

    public Task<int> ResetAiAgentRoomSettingsAsync(int tenantId, int roomId)
    {
        return AiAgentMigrationQueries.ResetAiAgentRoomSettingsAsync(this, tenantId, roomId);
    }

    [PreCompileQuery]
    public Task<int> DeleteAiAgentKnowledgeFolderAsync(int tenantId, int folderId)
    {
        return AiAgentMigrationQueries.DeleteAiAgentKnowledgeFolderAsync(this, tenantId, folderId);
    }

    [PreCompileQuery]
    public Task<int> MoveAiAgentKnowledgeFilesAsync(int tenantId, IEnumerable<int> fileIds, int knowledgeFolderId, int roomId, DateTime modifiedOn)
    {
        return AiAgentMigrationQueries.MoveAiAgentKnowledgeFilesAsync(this, tenantId, fileIds, knowledgeFolderId, roomId, modifiedOn);
    }

    [PreCompileQuery]
    public Task<int> MoveAiAgentThreadsAsync(int tenantId, int roomId, int projectId)
    {
        return AiAgentMigrationQueries.MoveAiAgentThreadsAsync(this, tenantId, roomId, projectId);
    }

    [PreCompileQuery]
    public Task<int> MoveAiAgentAssignmentsAsync(int tenantId, int roomId, int projectId)
    {
        return AiAgentMigrationQueries.MoveAiAgentAssignmentsAsync(this, tenantId, roomId, projectId);
    }

    [PreCompileQuery]
    public Task<int> MoveAiAgentToolPrefsAsync(int tenantId, int roomId, int projectId)
    {
        return AiAgentMigrationQueries.MoveAiAgentToolPrefsAsync(this, tenantId, roomId, projectId);
    }

    [PreCompileQuery]
    public Task<int> MoveAiAgentMcpServersAsync(int tenantId, int roomId, int projectId)
    {
        return AiAgentMigrationQueries.MoveAiAgentMcpServersAsync(this, tenantId, roomId, projectId);
    }

    [PreCompileQuery]
    public Task<int> MoveAiAgentPreferencesAsync(int tenantId, int roomId, int projectId)
    {
        return AiAgentMigrationQueries.MoveAiAgentPreferencesAsync(this, tenantId, roomId, projectId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbFilesSecurity> AiAgentMemberSharesAsync(int tenantId, int roomId)
    {
        return AiAgentMigrationQueries.AiAgentMemberSharesAsync(this, tenantId, roomId);
    }

    [PreCompileQuery]
    public Task<int> DeleteAiAgentRoomSharesAsync(int tenantId, int roomId)
    {
        return AiAgentMigrationQueries.DeleteAiAgentRoomSharesAsync(this, tenantId, roomId);
    }

    [PreCompileQuery]
    public Task<int> DeleteAiAgentFileSharesAsync(int tenantId, IEnumerable<int> fileIds)
    {
        return AiAgentMigrationQueries.DeleteAiAgentFileSharesAsync(this, tenantId, fileIds);
    }

    [PreCompileQuery]
    public Task<int> MoveAiAgentTagLinksAsync(int tenantId, string roomId, string projectId, IEnumerable<TagType> tagTypes)
    {
        return AiAgentMigrationQueries.MoveAiAgentTagLinksAsync(this, tenantId, roomId, projectId, tagTypes);
    }

    [PreCompileQuery]
    public Task<int> DeleteAiAgentTagLinksAsync(int tenantId, string roomId, IEnumerable<TagType> tagTypes)
    {
        return AiAgentMigrationQueries.DeleteAiAgentTagLinksAsync(this, tenantId, roomId, tagTypes);
    }
}

static file class AiAgentMigrationQueries
{
    public static readonly Func<FilesDbContext, int, IEnumerable<int>, IAsyncEnumerable<int>> AiAgentRoomIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> parentIds) =>
                ctx.Folders
                    .Where(f => f.TenantId == tenantId && parentIds.Contains(f.ParentId) && f.FolderType == FolderType.AiRoom)
                    .Select(f => f.Id));

    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<DbFolder>> AiAgentChildFoldersAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int roomId) =>
                ctx.Folders
                    .AsNoTracking()
                    .Where(f => f.TenantId == tenantId && f.ParentId == roomId));

    public static readonly Func<FilesDbContext, int, int, Task<DbFolder>> AiAgentKnowledgeFolderAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int roomId) =>
                ctx.Folders
                    .AsNoTracking()
                    .Where(f => f.TenantId == tenantId && f.ParentId == roomId && f.FolderType == FolderType.Knowledge)
                    .OrderBy(f => f.Id)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<int>> AiAgentKnowledgeFileIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Files
                    .Where(f => f.TenantId == tenantId && f.ParentId == folderId)
                    .Select(f => f.Id)
                    .Distinct());

    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<int>> AiAgentNestedFileIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Files
                    .Where(f => f.TenantId == tenantId &&
                                ctx.Tree.Any(t => t.ParentId == folderId && t.FolderId == f.ParentId && t.Level > 0))
                    .Select(f => f.Id)
                    .Distinct());

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    public static readonly Func<FilesDbContext, int, int, DateTime, Task<int>> TouchAiAgentSubtreeFilesAsync =
        (FilesDbContext ctx, int tenantId, int folderId, DateTime modifiedOn) =>
            ctx.Files
                .Where(f => f.TenantId == tenantId && (f.CurrentVersion || f.Version == 1) && ctx.Tree.Any(t => t.ParentId == folderId && t.FolderId == f.ParentId))
                .ExecuteUpdateAsync(f => f.SetProperty(p => p.ModifiedOn, modifiedOn));

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    public static readonly Func<FilesDbContext, int, int, DateTime, Task<int>> TouchAiAgentSubtreeFoldersAsync =
        (FilesDbContext ctx, int tenantId, int folderId, DateTime modifiedOn) =>
            ctx.Folders
                .Where(f => f.TenantId == tenantId && f.Id != folderId && ctx.Tree.Any(t => t.ParentId == folderId && t.FolderId == f.Id))
                .ExecuteUpdateAsync(f => f.SetProperty(p => p.ModifiedOn, modifiedOn));

    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<int>> AiAgentSubtreeFolderIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Folders
                    .Where(f => f.TenantId == tenantId && f.Id != folderId && ctx.Tree.Any(t => t.ParentId == folderId && t.FolderId == f.Id))
                    .Select(f => f.Id));

    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<int>> AiAgentSubtreeFileIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Files
                    .Where(f => f.TenantId == tenantId && ctx.Tree.Any(t => t.ParentId == folderId && t.FolderId == f.ParentId))
                    .Select(f => f.Id)
                    .Distinct());

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // The type in the predicate makes the statement the claim of the conversion: it changes no row once
    // the room is no longer an agent, whoever converted it.
    public static readonly Func<FilesDbContext, int, int, string, Guid, DateTime, Task<int>> ClaimAiAgentRoomAsync =
        (FilesDbContext ctx, int tenantId, int roomId, string title, Guid modifiedBy, DateTime modifiedOn) =>
            ctx.Folders
                .Where(f => f.TenantId == tenantId && f.Id == roomId && f.FolderType == FolderType.AiRoom)
                .ExecuteUpdateAsync(f => f
                    .SetProperty(p => p.FolderType, FolderType.KnowledgeRoom)
                    .SetProperty(p => p.Title, p => title ?? p.Title)
                    .SetProperty(p => p.ModifiedBy, modifiedBy)
                    .SetProperty(p => p.ModifiedOn, modifiedOn));

    public static readonly Func<FilesDbContext, int, int, Task<int>> ResetAiAgentRoomSettingsAsync =
        (FilesDbContext ctx, int tenantId, int roomId) =>
            ctx.RoomSettings
                .Where(s => s.TenantId == tenantId && s.RoomId == roomId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Private, false)
                    .SetProperty(p => p.SendFormToExternalDB, false)
                    .SetProperty(p => p.SaveFormAsXLSX, false)
                    .SetProperty(p => p.Indexing, false)
                    .SetProperty(p => p.DenyDownload, false)
                    .SetProperty(p => p.Lifetime, (DbRoomDataLifetime)null)
                    .SetProperty(p => p.Watermark, (DbRoomWatermark)null));

    public static readonly Func<FilesDbContext, int, int, Task<int>> DeleteAiAgentKnowledgeFolderAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Folders
                    .Where(f => f.TenantId == tenantId && f.Id == folderId && f.FolderType == FolderType.Knowledge)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, int, int, DateTime, Task<int>> MoveAiAgentKnowledgeFilesAsync =
        (FilesDbContext ctx, int tenantId, IEnumerable<int> fileIds, int knowledgeFolderId, int roomId, DateTime modifiedOn) =>
            ctx.Files
                .Where(f => f.TenantId == tenantId && fileIds.Contains(f.Id) && f.ParentId == knowledgeFolderId)
                .ExecuteUpdateAsync(f => f
                    .SetProperty(p => p.ParentId, roomId)
                    .SetProperty(p => p.ModifiedOn, p => p.CurrentVersion || p.Version == 1 ? modifiedOn : p.ModifiedOn));

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    public static readonly Func<FilesDbContext, int, int, int, Task<int>> MoveAiAgentThreadsAsync =
        (FilesDbContext ctx, int tenantId, int roomId, int projectId) =>
            ctx.Threads
                .Where(x => x.TenantId == tenantId && x.EntryId == roomId)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.EntryId, projectId));

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    public static readonly Func<FilesDbContext, int, int, int, Task<int>> MoveAiAgentAssignmentsAsync =
        (FilesDbContext ctx, int tenantId, int roomId, int projectId) =>
            ctx.Assignments
                .Where(x => x.TenantId == tenantId && x.EntryId == roomId)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.EntryId, projectId));

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    public static readonly Func<FilesDbContext, int, int, int, Task<int>> MoveAiAgentToolPrefsAsync =
        (FilesDbContext ctx, int tenantId, int roomId, int projectId) =>
            ctx.ToolPrefs
                .Where(x => x.TenantId == tenantId && x.EntryId == roomId)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.EntryId, projectId));

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    public static readonly Func<FilesDbContext, int, int, int, Task<int>> MoveAiAgentMcpServersAsync =
        (FilesDbContext ctx, int tenantId, int roomId, int projectId) =>
            ctx.McpServers
                .Where(x => x.TenantId == tenantId && x.EntryId == roomId)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.EntryId, projectId));

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    public static readonly Func<FilesDbContext, int, int, int, Task<int>> MoveAiAgentPreferencesAsync =
        (FilesDbContext ctx, int tenantId, int roomId, int projectId) =>
            ctx.Preferences
                .Where(x => x.TenantId == tenantId && x.EntryId == roomId)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.EntryId, projectId));

    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<DbFilesSecurity>> AiAgentMemberSharesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int roomId) =>
                ctx.Security
                    .AsNoTracking()
                    .Where(s => s.TenantId == tenantId && s.EntryType == FileEntryType.Folder && s.InternalEntryId == roomId)
                    .Where(s => s.SubjectType == SubjectType.User || s.SubjectType == SubjectType.Group));

    public static readonly Func<FilesDbContext, int, int, Task<int>> DeleteAiAgentRoomSharesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int roomId) =>
                ctx.Security
                    .Where(s => s.TenantId == tenantId && s.EntryType == FileEntryType.Folder && s.InternalEntryId == roomId)
                    .Where(s => !((s.SubjectType == SubjectType.User || s.SubjectType == SubjectType.Group)
                                  && (s.Share == FileShare.RoomManager || s.Share == FileShare.ContentCreator)))
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, Task<int>> DeleteAiAgentFileSharesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> fileIds) =>
                ctx.Security
                    .Where(s => s.TenantId == tenantId && s.EntryType == FileEntryType.File && fileIds.Contains(s.InternalEntryId))
                    .ExecuteDelete());

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    public static readonly Func<FilesDbContext, int, string, string, IEnumerable<TagType>, Task<int>> MoveAiAgentTagLinksAsync =
        (FilesDbContext ctx, int tenantId, string roomId, string projectId, IEnumerable<TagType> tagTypes) =>
            ctx.TagLink
                .Where(l => l.TenantId == tenantId && l.EntryType == FileEntryType.Folder && l.EntryId == roomId)
                .Where(l => ctx.Tag.Any(t => t.TenantId == tenantId && t.Id == l.TagId && tagTypes.Contains(t.Type)))
                .ExecuteUpdateAsync(l => l.SetProperty(p => p.EntryId, projectId));

    public static readonly Func<FilesDbContext, int, string, IEnumerable<TagType>, Task<int>> DeleteAiAgentTagLinksAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string roomId, IEnumerable<TagType> tagTypes) =>
                ctx.TagLink
                    .Where(l => l.TenantId == tenantId && l.EntryType == FileEntryType.Folder && l.EntryId == roomId)
                    .Where(l => ctx.Tag.Any(t => t.TenantId == tenantId && t.Id == l.TagId && tagTypes.Contains(t.Type)))
                    .ExecuteDelete());
}
