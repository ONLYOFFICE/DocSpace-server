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
    public IAsyncEnumerable<DbFilesThirdpartyAccount> ThirdPartyAccountsAsync(int tenantId, Guid userId)
    {
        return Queries.ThirdPartyAccountsAsync(this, tenantId, userId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbFilesThirdpartyAccount> ThirdPartyAccountsByFilterAsync(int tenantId, int linkId, FolderType folderType, Guid userId, string searchText)
    {
        return Queries.ThirdPartyAccountsByFilterAsync(this, tenantId, linkId, folderType, userId, searchText);
    }

    [PreCompileQuery]
    public Task<DbFilesThirdpartyAccount> ThirdPartyAccountAsync(int tenantId, int linkId)
    {
        return Queries.ThirdPartyAccountAsync(this, tenantId, linkId);
    }

    [PreCompileQuery]
    public Task<int> UpdateThirdPartyAccountsAsync(int tenantId, int linkId, string login, string password, string token, string url)
    {
        return Queries.UpdateThirdPartyAccountsAsync(this, tenantId, linkId, login, password, token, url);
    }

    //[PreCompileQuery]
    public Task<DbFilesThirdpartyAccount> ThirdPartyAccountByLinkIdAsync(int tenantId, int linkId)
    {
        return Queries.ThirdPartyAccountByLinkIdAsync(this, tenantId, linkId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbFilesThirdpartyAccount> ThirdPartyAccountsByLinkIdAsync(int tenantId, int linkId)
    {
        return Queries.ThirdPartyAccountsByLinkIdAsync(this, tenantId, linkId);
    }

    [PreCompileQuery]
    public Task<int> DeleteThirdPartyAccountsByLinkIdAsync(int tenantId, int linkId)
    {
        return Queries.DeleteThirdPartyAccountsByLinkIdAsync(this, tenantId, linkId);
    }

    //[PreCompileQuery]
    public Task<DbFilesThirdpartyAccount> ThirdPartyBackupAccountAsync(int tenantId)
    {
        return Queries.ThirdPartyBackupAccountAsync(this, tenantId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<string> HashIdsAsync(int tenantId, string folderId)
    {
        return Queries.HashIdsAsync(this, tenantId, folderId);
    }

    [PreCompileQuery]
    public Task<int> DeleteDbFilesSecuritiesAsync(int tenantId, IEnumerable<string> entryIDs)
    {
        return Queries.DeleteDbFilesSecuritiesAsync(this, tenantId, entryIDs);
    }

    [PreCompileQuery]
    public Task<int> DeleteDbFilesTagLinksAsync(int tenantId, IEnumerable<string> entryIDs)
    {
        return Queries.DeleteDbFilesTagLinksAsync(this, tenantId, entryIDs);
    }
}

static file class Queries
{
    public static readonly Func<FilesDbContext, int, Guid, IAsyncEnumerable<DbFilesThirdpartyAccount>> ThirdPartyAccountsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid userId) =>
                ctx.ThirdpartyAccount
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.UserId == userId));

    public static readonly
        Func<FilesDbContext, int, int, FolderType, Guid, string, IAsyncEnumerable<DbFilesThirdpartyAccount>> ThirdPartyAccountsByFilterAsync =
            Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int linkId, FolderType folderType, Guid userId, string searchText) =>
                ctx.ThirdpartyAccount
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => !(folderType == FolderType.USER || folderType == FolderType.DEFAULT && linkId == -1) ||
                                r.UserId == userId || r.FolderType == FolderType.ThirdpartyBackup)
                    .Where(r => linkId == -1 || r.Id == linkId)
                    .Where(r => folderType == FolderType.DEFAULT &&
                        !(r.FolderType == FolderType.ThirdpartyBackup && linkId == -1) || r.FolderType == folderType)
                    .Where(r => searchText == "" || r.Title.ToLower().Contains(searchText)));

    public static readonly Func<FilesDbContext, int, int, Task<DbFilesThirdpartyAccount>> ThirdPartyAccountAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int linkId) =>
                ctx.ThirdpartyAccount
                    .FirstOrDefault(r => r.Id == linkId && r.TenantId == tenantId));

    public static readonly Func<FilesDbContext, int, int, string, string, string, string, Task<int>> UpdateThirdPartyAccountsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int linkId, string login, string password, string token, string url) =>
                ctx.ThirdpartyAccount
                    .Where(r => r.Id == linkId)
                    .Where(r => r.TenantId == tenantId)
                    .ExecuteUpdate(f => f
                        .SetProperty(p => p.UserName, login)
                        .SetProperty(p => p.Password, password)
                        .SetProperty(p => p.Token, token)
                        .SetProperty(p => p.Url, url)));

    public static readonly Func<FilesDbContext, int, int, Task<DbFilesThirdpartyAccount>> ThirdPartyAccountByLinkIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int linkId) =>
                ctx.ThirdpartyAccount
                    .Single(r => r.TenantId == tenantId && r.Id == linkId));

    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<DbFilesThirdpartyAccount>> ThirdPartyAccountsByLinkIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int linkId) =>
                ctx.ThirdpartyAccount
                    .AsTracking()
                    .Where(r => r.Id == linkId)
                    .Where(r => r.TenantId == tenantId));

    public static readonly Func<FilesDbContext, int, int, Task<int>> DeleteThirdPartyAccountsByLinkIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int linkId) =>
                ctx.ThirdpartyAccount
                    .AsTracking()
                    .Where(r => r.Id == linkId)
                    .Where(r => r.TenantId == tenantId)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, Task<DbFilesThirdpartyAccount>> ThirdPartyBackupAccountAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery((FilesDbContext ctx, int tenantId) =>
                ctx.ThirdpartyAccount.Single(r => r.TenantId == tenantId && r.FolderType == FolderType.ThirdpartyBackup));

    public static readonly Func<FilesDbContext, int, string, IAsyncEnumerable<string>> HashIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string folderId) =>
                ctx.ThirdpartyIdMapping
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id.StartsWith(folderId))
                    .Select(r => r.HashId));

    public static readonly Func<FilesDbContext, int, IEnumerable<string>, Task<int>> DeleteDbFilesSecuritiesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<string> entryIDs) =>
                ctx.Security
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => entryIDs.Any(a => a == r.EntryId))
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IEnumerable<string>, Task<int>> DeleteDbFilesTagLinksAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<string> entryIDs) =>
                ctx.TagLink
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => entryIDs.Any(e => e == r.EntryId))
                    .ExecuteDelete());
}