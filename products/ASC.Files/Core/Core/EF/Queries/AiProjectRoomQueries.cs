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
    public IAsyncEnumerable<int> AiProjectRoomIdsAsync(int tenantId, int projectId)
    {
        return AiProjectRoomQueries.AiProjectRoomIdsAsync(this, tenantId, projectId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbFolder> AiProjectLinkedRoomsAsync(int tenantId, int projectId, int roomsFolderId)
    {
        return AiProjectRoomQueries.AiProjectLinkedRoomsAsync(this, tenantId, projectId, roomsFolderId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbFolder> AiProjectLinkableRoomsAsync(int tenantId, int roomsFolderId, IEnumerable<int> roomIds)
    {
        return AiProjectRoomQueries.AiProjectLinkableRoomsAsync(this, tenantId, roomsFolderId, roomIds);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<AiRoomProjectsCount> AiRoomProjectsCountsAsync(int tenantId, IEnumerable<int> roomIds)
    {
        return AiProjectRoomQueries.AiRoomProjectsCountsAsync(this, tenantId, roomIds);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbFolderQuery> AiRoomProjectsAsync(int tenantId, int roomId, int roomsFolderId, Guid userId, bool isAdmin, int offset, int count)
    {
        return AiProjectRoomQueries.AiRoomProjectsAsync(this, tenantId, roomId, roomsFolderId, userId, isAdmin, offset, count);
    }

    [PreCompileQuery]
    public Task<int> AiRoomProjectsTotalAsync(int tenantId, int roomId, int roomsFolderId, Guid userId, bool isAdmin)
    {
        return AiProjectRoomQueries.AiRoomProjectsTotalAsync(this, tenantId, roomId, roomsFolderId, userId, isAdmin);
    }

    [PreCompileQuery]
    public Task<int> DeleteAiProjectRoomsAsync(int tenantId, int projectId, IEnumerable<int> roomIds)
    {
        return AiProjectRoomQueries.DeleteAiProjectRoomsAsync(this, tenantId, projectId, roomIds);
    }

    [PreCompileQuery]
    public Task<int> DeleteAiProjectRoomsByRoomAsync(int tenantId, int roomId)
    {
        return AiProjectRoomQueries.DeleteAiProjectRoomsByRoomAsync(this, tenantId, roomId);
    }

    [PreCompileQuery]
    public Task<int> DeleteAiProjectRoomsByProjectAsync(int tenantId, int projectId)
    {
        return AiProjectRoomQueries.DeleteAiProjectRoomsByProjectAsync(this, tenantId, projectId);
    }
}

public class AiRoomProjectsCount
{
    public int RoomId { get; init; }
    public int Count { get; init; }
}

static file class AiProjectRoomQueries
{
    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<int>> AiProjectRoomIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int projectId) =>
                ctx.AiProjectRooms
                    .Where(r => r.TenantId == tenantId && r.ProjectId == projectId)
                    .Select(r => r.RoomId));

    public static readonly Func<FilesDbContext, int, int, int, IAsyncEnumerable<DbFolder>> AiProjectLinkedRoomsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int projectId, int roomsFolderId) =>
                ctx.AiProjectRooms
                    .Where(r => r.TenantId == tenantId && r.ProjectId == projectId)
                    .Join(ctx.Folders, r => new { r.TenantId, Id = r.RoomId }, f => new { f.TenantId, f.Id }, (_, f) => f)
                    .Where(f => f.ParentId == roomsFolderId && (f.FolderType == FolderType.InstructionsRoom || f.FolderType == FolderType.KnowledgeRoom)));

    public static readonly Func<FilesDbContext, int, int, IEnumerable<int>, IAsyncEnumerable<DbFolder>> AiProjectLinkableRoomsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int roomsFolderId, IEnumerable<int> roomIds) =>
                ctx.Folders
                    .Where(f => f.TenantId == tenantId && f.ParentId == roomsFolderId && roomIds.Contains(f.Id))
                    .Where(f => f.FolderType == FolderType.InstructionsRoom || f.FolderType == FolderType.KnowledgeRoom));

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, IAsyncEnumerable<AiRoomProjectsCount>> AiRoomProjectsCountsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> roomIds) =>
                ctx.AiProjectRooms
                    .Where(r => r.TenantId == tenantId && roomIds.Contains(r.RoomId))
                    .GroupBy(r => r.RoomId)
                    .Select(g => new AiRoomProjectsCount { RoomId = g.Key, Count = g.Count() }));

    public static readonly Func<FilesDbContext, int, int, int, Guid, bool, int, int, IAsyncEnumerable<DbFolderQuery>> AiRoomProjectsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int roomId, int roomsFolderId, Guid userId, bool isAdmin, int offset, int count) =>
                ctx.AiProjectRooms
                    .Where(r => r.TenantId == tenantId && r.RoomId == roomId)
                    .Join(ctx.Folders, r => new { r.TenantId, Id = r.ProjectId }, f => new { f.TenantId, f.Id }, (_, f) => f)
                    .Where(f => f.FolderType == FolderType.AiProjectRoom && f.ParentId == roomsFolderId)
                    .Join(ctx.Folders,
                        f => new { f.TenantId, Id = f.ParentId },
                        root => new { root.TenantId, root.Id },
                        (f, root) => new { Folder = f, Root = root })
                    .GroupJoin(ctx.RoomSettings,
                        x => new { x.Folder.TenantId, RoomId = x.Folder.Id },
                        rs => new { rs.TenantId, rs.RoomId },
                        (x, settings) => new { x.Folder, x.Root, settings })
                    .SelectMany(x => x.settings.DefaultIfEmpty(), (x, rs) => new { x.Folder, x.Root, Settings = rs })
                    .Where(x => isAdmin || x.Folder.CreateBy == userId || ctx.Security.Any(s =>
                        s.TenantId == x.Folder.TenantId &&
                        s.InternalEntryId == x.Folder.Id &&
                        s.EntryType == FileEntryType.Folder &&
                        s.Subject == userId &&
                        s.Share != FileShare.Restrict))
                    .OrderBy(x => x.Folder.Title)
                    .ThenBy(x => x.Folder.Id)
                    .Skip(offset)
                    .Take(count)
                    .Select(x => new DbFolderQuery
                    {
                        Folder = x.Folder,
                        Root = x.Root,
                        Settings = x.Settings
                    }));

    public static readonly Func<FilesDbContext, int, int, int, Guid, bool, Task<int>> AiRoomProjectsTotalAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int roomId, int roomsFolderId, Guid userId, bool isAdmin) =>
                ctx.AiProjectRooms
                    .Where(r => r.TenantId == tenantId && r.RoomId == roomId)
                    .Join(ctx.Folders, r => new { r.TenantId, Id = r.ProjectId }, f => new { f.TenantId, f.Id }, (_, f) => f)
                    .Where(f => f.FolderType == FolderType.AiProjectRoom && f.ParentId == roomsFolderId)
                    .Count(f => isAdmin || f.CreateBy == userId || ctx.Security.Any(s =>
                        s.TenantId == f.TenantId &&
                        s.InternalEntryId == f.Id &&
                        s.EntryType == FileEntryType.Folder &&
                        s.Subject == userId &&
                        s.Share != FileShare.Restrict)));

    public static readonly Func<FilesDbContext, int, int, IEnumerable<int>, Task<int>> DeleteAiProjectRoomsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int projectId, IEnumerable<int> roomIds) =>
                ctx.AiProjectRooms
                    .Where(r => r.TenantId == tenantId && r.ProjectId == projectId && roomIds.Contains(r.RoomId))
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, int, Task<int>> DeleteAiProjectRoomsByRoomAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int roomId) =>
                ctx.AiProjectRooms
                    .Where(r => r.TenantId == tenantId && r.RoomId == roomId)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, int, Task<int>> DeleteAiProjectRoomsByProjectAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int projectId) =>
                ctx.AiProjectRooms
                    .Where(r => r.TenantId == tenantId && r.ProjectId == projectId)
                    .ExecuteDelete());
}
