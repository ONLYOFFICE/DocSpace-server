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

namespace ASC.Files.Core.EF;

public partial class FilesDbContext
{
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbFilesGroup> GetGroupsAsync(int tenantId)
    {
        return GroupsQueries.GetGroupsAsync(this, tenantId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<DbFilesGroup> GroupForUpdateAsync(int tenantId, int id)
    {
        return GroupsQueries.GroupForUpdateAsync(this, tenantId, id);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<DbFilesRoomGroup> FirstOrDefaultInternalRoomGroupAsync(int tenantId, int groupId, int roomId)
    {
        return GroupsQueries.FirstOrDefaultInternalRoomGroupAsync(this, tenantId, groupId, roomId);
    }
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, null])]
    public Task<DbFilesRoomGroup> FirstOrDefaultThirdpartyRoomGroupAsync(int tenantId, int groupId, string roomId)
    {
        return GroupsQueries.FirstOrDefaultThirdpartyRoomGroupAsync(this, tenantId, groupId, roomId);
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

}

static partial class GroupsQueries
{
    public static readonly Func<FilesDbContext, int, IAsyncEnumerable<DbFilesGroup>> GetGroupsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId) =>
                ctx.Set<DbFilesGroup>()
                    .AsTracking()
                    .Where(g => g.TenantId == tenantId));

    public static readonly Func<FilesDbContext, int, int, Task<DbFilesGroup>> GroupForUpdateAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int id) =>
                ctx.RoomGroup.FirstOrDefault(r => r.TenantId == tenantId && r.Id == id));

    public static readonly Func<FilesDbContext, int, int, int, Task<DbFilesRoomGroup>> FirstOrDefaultInternalRoomGroupAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int groupId, int roomId) =>
                ctx.RoomGroupRef
                    .FirstOrDefault(r => r.TenantId == tenantId && r.GroupId == groupId && r.InternalRoomId == roomId));

    public static readonly Func<FilesDbContext, int, int, string, Task<DbFilesRoomGroup>> FirstOrDefaultThirdpartyRoomGroupAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int groupId, string roomId) =>
                ctx.RoomGroupRef
                    .FirstOrDefault(r => r.TenantId == tenantId && r.GroupId == groupId && r.ThirdpartyRoomId == roomId));

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
}