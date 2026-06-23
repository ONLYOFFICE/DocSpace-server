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

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<DbFilesGroup> GroupForUpdateAsync(int tenantId, int id)
    {
        return GroupsQueries.GroupForUpdateAsync(this, tenantId, id);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<DbFilesRoomGroup> GetRoomsByGroupAsync(int tenantId, int groupId)
    {
        return GroupsQueries.GetRoomsByGroupAsync(this, tenantId, groupId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<bool> AnyRoomGroupRefAsync(int tenantId, int groupId, int roomId)
    {
        return GroupsQueries.AnyInternalRoomGroupRefAsync(this, tenantId, groupId, roomId);
    }
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, null])]
    public Task<bool> AnyRoomGroupRefAsync(int tenantId, int groupId, string roomId)
    {
        return GroupsQueries.AnyThirdpartyRoomGroupRefAsync(this, tenantId, groupId, roomId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteRoomGroupRefByFolderIdsAsync(int tenantId, IEnumerable<int> folderIds)
    {
        return GroupsQueries.DeleteRoomGroupRefByFolderIdsAsync(this, tenantId, folderIds);
    }

}

internal static partial class GroupsQueries
{
    public static readonly Func<FilesDbContext, int, int, Task<DbFilesGroup>> GroupForUpdateAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int id) =>
                ctx.RoomGroup.FirstOrDefault(r => r.TenantId == tenantId && r.Id == id));

    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<DbFilesRoomGroup>> GetRoomsByGroupAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int groupId) =>
                ctx.RoomGroupRef
                    .Where(r => r.TenantId == tenantId && r.GroupId == groupId));

    public static readonly Func<FilesDbContext, int, int, int, Task<bool>> AnyInternalRoomGroupRefAsync =
       Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
           (FilesDbContext ctx, int tenantId, int groupId, int roomId) =>
               ctx.RoomGroupRef.Any(r => r.TenantId == tenantId && r.GroupId == groupId && r.InternalRoomId == roomId));

    public static readonly Func<FilesDbContext, int, int, string, Task<bool>> AnyThirdpartyRoomGroupRefAsync =
       Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
           (FilesDbContext ctx, int tenantId, int groupId, string roomId) =>
               ctx.RoomGroupRef.Any(r => r.TenantId == tenantId && r.GroupId == groupId && r.ThirdpartyRoomId == roomId));

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, Task<int>> DeleteRoomGroupRefByFolderIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> folderIds) =>
                ctx.RoomGroupRef
                    .Where(r => r.TenantId == tenantId && r.InternalRoomId != null && folderIds.Contains(r.InternalRoomId.Value))
                    .ExecuteDelete());
}