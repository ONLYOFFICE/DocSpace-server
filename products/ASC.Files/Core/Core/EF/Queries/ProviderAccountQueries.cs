// (c) Copyright Ascensio System SIA 2009-2024
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
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public IAsyncEnumerable<DbFilesThirdpartyAccount> ThirdPartyAccountsAsync(int tenantId, Guid userId)
    {
        return Queries.ThirdPartyAccountsAsync(this, tenantId, userId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, int.MaxValue, FolderType.ThirdpartyBackup, PreCompileQuery.DefaultGuid, null])]
    public IAsyncEnumerable<DbFilesThirdpartyAccount> ThirdPartyAccountsByFilterAsync(int tenantId, int linkId, FolderType folderType, Guid userId, string searchText)
    {
        return Queries.ThirdPartyAccountsByFilterAsync(this, tenantId, linkId, folderType, userId, searchText);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<DbFilesThirdpartyAccount> ThirdPartyAccountAsync(int tenantId, int linkId)
    {
        return Queries.ThirdPartyAccountAsync(this, tenantId, linkId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, null, null, null, null])]
    public Task<int> UpdateThirdPartyAccountsAsync(int tenantId, int linkId, string login, string password, string token, string url)
    {
        return Queries.UpdateThirdPartyAccountsAsync(this, tenantId, linkId, login, password, token, url);
    }
    
    //[PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<DbFilesThirdpartyAccount> ThirdPartyAccountByLinkIdAsync(int tenantId, int linkId)
    {
        return Queries.ThirdPartyAccountByLinkIdAsync(this, tenantId, linkId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbFilesThirdpartyAccount> ThirdPartyAccountsByLinkIdAsync(int tenantId, int linkId)
    {
        return Queries.ThirdPartyAccountsByLinkIdAsync(this, tenantId, linkId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<int> DeleteThirdPartyAccountsByLinkIdAsync(int tenantId, int linkId)
    {
        return Queries.DeleteThirdPartyAccountsByLinkIdAsync(this, tenantId, linkId);
    }
    
    //[PreCompileQuery([PreCompileQuery.DefaultInt])]
    public Task<DbFilesThirdpartyAccount> ThirdPartyBackupAccountAsync(int tenantId)
    {
        return Queries.ThirdPartyBackupAccountAsync(this, tenantId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<string> HashIdsAsync(int tenantId, string folderId)
    {
        return Queries.HashIdsAsync(this, tenantId, folderId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteDbFilesSecuritiesAsync(int tenantId, IEnumerable<string> entryIDs)
    {
        return Queries.DeleteDbFilesSecuritiesAsync(this, tenantId, entryIDs);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
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
                    .FirstOrDefault(r =>  r.Id == linkId && r.TenantId == tenantId));

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
                        .SetProperty(p => p.Url, url)
                        .SetProperty(p => p.ModifiedOn, DateTime.UtcNow)));

    public static readonly Func<FilesDbContext, int, int, Task<DbFilesThirdpartyAccount>> ThirdPartyAccountByLinkIdAsync = 
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int linkId) =>
                ctx.ThirdpartyAccount
                    .Single(r => r.TenantId == tenantId &&r.Id == linkId));

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