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
    [PreCompileQuery([PreCompileQuery.DefaultInt, FileEntryType.File, PreCompileQuery.DefaultGuid, null])]
    public IAsyncEnumerable<DbFilesSecurity> ForDeleteShareRecordsAsync(int tenantId, FileEntryType entryType, Guid subject, string entryId)
    {
        return SecurityQueries.ForDeleteShareRecordsAsync(this, tenantId, entryType, subject, entryId);
    }

    [PreCompileQuery([null])]
    public IAsyncEnumerable<int> FolderIdsAsync(int entryId)
    {
        return SecurityQueries.FolderIdsAsync(this, entryId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<int> FilesIdsAsync(int tenantId, IEnumerable<int> folders)
    {
        return SecurityQueries.FilesIdsAsync(this, tenantId, folders);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, null, null, FileEntryType.File])]
    public Task<int> DeleteForSetShareAsync(int tenantId, Guid subject, IEnumerable<int> internalEntryIds, IEnumerable<string> thirdPartyEntryIds, FileEntryType type)
    {
        return SecurityQueries.DeleteForSetShareAsync(this, tenantId, subject, internalEntryIds, thirdPartyEntryIds, type);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, null, PreCompileQuery.DefaultGuid])]
    public IAsyncEnumerable<DbFilesSecurity> SharesAsync(int tenantId, IEnumerable<Guid> subjects, Guid ownerId)
    {
        return SecurityQueries.SharesAsync(this, tenantId, subjects, ownerId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, null, null])]
    public IAsyncEnumerable<DbFilesSecurity> PureShareRecordsDbAsync(int tenantId, IEnumerable<string> files, IEnumerable<string> folders)
    {
        return SecurityQueries.PureShareRecordsDbAsync(this, tenantId, files, folders);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<DbFilesSecurity> ExternalLinkRecordsDbAsync(int tenantId, IEnumerable<string> folders)
    {
        return SecurityQueries.ExternalLinkRecordsDbAsync(this, tenantId, folders);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<int> RemoveBySubjectAsync(int tenantId, Guid subject)
    {
        return SecurityQueries.RemoveBySubjectAsync(this, tenantId, subject);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<int> RemoveBySubjectWithoutOwnerAsync(int tenantId, Guid subject)
    {
        return SecurityQueries.RemoveBySubjectWithoutOwnerAsync(this, tenantId, subject);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, null, FileEntryType.File, null])]
    public IAsyncEnumerable<DbFilesSecurity> EntrySharesBySubjectsAsync(int tenantId, string entryId, FileEntryType entryType, IEnumerable<Guid> subjects)
    {
        return SecurityQueries.EntrySharesBySubjectsAsync(this, tenantId, entryId, entryType, subjects);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, PreCompileQuery.DefaultGuid, SubjectType.User])]
    public Task<int> RemoveSecuritiesAsync(int tenantId, Guid subject, Guid owner, SubjectType subjectType)
    {
        return SecurityQueries.RemoveSecuritiesAsync(this, tenantId, subject, owner, subjectType);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<int> RemoveUserRoomChatsAsync(int tenantId, int roomId, Guid userId)
    {
        return SecurityQueries.RemoveUserRoomChatsAsync(this, tenantId, roomId, userId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<int> RemoveUserRoomChatsSettingsAsync(int tenantId, int roomId, Guid userId)
    {
        return SecurityQueries.RemoveUserRoomChatsSettingsAsync(this, tenantId, roomId, userId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<int> RemoveUserRoomMcpSettingsAsync(int tenantId, int roomId, Guid userId)
    {
        return SecurityQueries.RemoveUserRoomMcpSettingsAsync(this, tenantId, roomId, userId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, null, FileShare.Read])]
    public Task<int> UpdateShareByFolderTypesAsync(int tenantId, Guid subject, IEnumerable<FolderType> folderTypes, FileShare share)
    {
        return SecurityQueries.UpdateShareByFolderTypesAsync(this, tenantId, subject, folderTypes, share);
    }
}

static file class SecurityQueries
{
    public static readonly Func<FilesDbContext, int, FileEntryType, Guid, string, IAsyncEnumerable<DbFilesSecurity>> ForDeleteShareRecordsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, FileEntryType entryType, Guid subject, string entryId) =>
                ctx.Security
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.EntryType == entryType)
                    .Where(r => r.Subject == subject));//TODO: check entryId

    public static readonly Func<FilesDbContext, int, IAsyncEnumerable<int>> FolderIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int entryId) =>
                ctx.Tree.Where(r => r.ParentId == entryId)
                    .Select(r => r.FolderId));

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, IAsyncEnumerable<int>> FilesIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> folders) =>
                ctx.Files.Where(r => r.TenantId == tenantId && folders.Contains(r.ParentId)).Select(r => r.Id));

    public static readonly
        Func<FilesDbContext, int, Guid, IEnumerable<int>, IEnumerable<string>, FileEntryType, Task<int>> DeleteForSetShareAsync =
            Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid subject, IEnumerable<int> internalEntryIds, IEnumerable<string> thirdPartyEntryIds, FileEntryType type) =>
                ctx.Security
                    .Where(a => a.TenantId == tenantId &&
                                (internalEntryIds.Contains(a.InternalEntryId) || thirdPartyEntryIds.Contains(a.EntryId)) &&
                                a.EntryType == type &&
                                a.Subject == subject)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IEnumerable<Guid>, Guid, IAsyncEnumerable<DbFilesSecurity>> SharesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<Guid> subjects, Guid ownerId) =>
                ctx.Security
                    .Where(r => r.TenantId == tenantId && subjects.Contains(r.Subject) && (ownerId == Guid.Empty || ownerId == r.Owner)));

    public static readonly
        Func<FilesDbContext, int, IEnumerable<string>, IEnumerable<string>, IAsyncEnumerable<DbFilesSecurity>> PureShareRecordsDbAsync =
            Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<string> files, IEnumerable<string> folders) =>
                ctx.Security.Where(r =>
                    (r.TenantId == tenantId && files.Contains(r.EntryId) && r.EntryType == FileEntryType.File)
                    || (r.TenantId == tenantId && folders.Contains(r.EntryId) && r.EntryType == FileEntryType.Folder)));

    public static readonly
        Func<FilesDbContext, int, IEnumerable<string>, IAsyncEnumerable<DbFilesSecurity>> ExternalLinkRecordsDbAsync =
            Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<string> folders) =>
                ctx.Security.Where(r =>
                    r.TenantId == tenantId
                    && folders.Contains(r.EntryId)
                    && r.EntryType == FileEntryType.Folder
                    && (r.SubjectType == SubjectType.ExternalLink || r.SubjectType == SubjectType.PrimaryExternalLink)));

    public static readonly Func<FilesDbContext, int, Guid, Task<int>> RemoveBySubjectAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid subject) =>
                ctx.Security
                    .Where(r => r.TenantId == tenantId
                                && (r.Subject == subject || r.Owner == subject))
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, Guid, Task<int>> RemoveBySubjectWithoutOwnerAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid subject) =>
                ctx.Security
                    .Where(r => r.TenantId == tenantId && r.Subject == subject)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, string, FileEntryType, IEnumerable<Guid>, IAsyncEnumerable<DbFilesSecurity>> EntrySharesBySubjectsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string entryId, FileEntryType entryType, IEnumerable<Guid> subjects) =>
                ctx.Security
                    .Where(r => r.TenantId == tenantId && r.EntryId == entryId && r.EntryType == entryType && subjects.Contains(r.Subject)));

    public static readonly Func<FilesDbContext, int, Guid, Guid, SubjectType, Task<int>> RemoveSecuritiesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid subject, Guid owner, SubjectType subjectType) =>
                ctx.Security
                    .Where(r =>
                        r.TenantId == tenantId &&
                        r.Subject == subject &&
                        r.Owner == owner &&
                        r.SubjectType == subjectType)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, int, Guid, Task<int>> RemoveUserRoomChatsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int roomId, Guid userId) =>
                ctx.Chats
                    .Where(r => r.TenantId == tenantId && r.RoomId == roomId && r.UserId == userId)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, int, Guid, Task<int>> RemoveUserRoomChatsSettingsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int roomId, Guid userId) =>
                ctx.UserChatSettings
                    .Where(r => r.TenantId == tenantId && r.RoomId == roomId && r.UserId == userId)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, int, Guid, Task<int>> RemoveUserRoomMcpSettingsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int roomId, Guid userId) =>
                ctx.McpServerSettings
                    .Where(r => r.TenantId == tenantId && r.RoomId == roomId && r.UserId == userId)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, Guid, IEnumerable<FolderType>, FileShare, Task<int>>
        UpdateShareByFolderTypesAsync =
            Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
                (FilesDbContext ctx, int tenantId, Guid subject, IEnumerable<FolderType> folderTypes, FileShare share) =>
                    ctx.Security
                        .Where(r => r.TenantId == tenantId && r.Subject == subject && r.EntryType == FileEntryType.Folder)
                        .Where(r => ctx.Folders.Any(f =>
                            f.Id == r.InternalEntryId && f.TenantId == tenantId && folderTypes.Contains(f.FolderType)))
                        .ExecuteUpdate(f => f.SetProperty(p => p.Share, share)));
}
