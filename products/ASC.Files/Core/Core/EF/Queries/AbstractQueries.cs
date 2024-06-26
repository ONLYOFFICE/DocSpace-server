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
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<DbFolder> FoldersAsync(int tenantId, IEnumerable<int> folderId)
    {
        return AbstractQueries.FoldersAsync(this, tenantId, folderId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, int.MaxValue])]
    public Task<int> FilesCountAsync(int tenantId, int folderId)
    {
        return AbstractQueries.FilesCountAsync(this, tenantId, folderId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<string> IdAsync(int tenantId, string hashId)
    {
        return AbstractQueries.IdAsync(this, tenantId, hashId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, FileEntryType.File])]
    public Task<bool> IsIndexingAsync(int tenantId, int parentFolderId, FileEntryType entryType)
    {
        return AbstractQueries.IsIndexingAsync(this, tenantId, parentFolderId, entryType);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, FileEntryType.File])]
    public Task<DbFileOrder> GetFileOrderAsync(int tenantId, int entryId, FileEntryType entryType)
    {
        return AbstractQueries.GetFileOrderAsync(this, tenantId, entryId, entryType);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, FileEntryType.File])]
    public Task ClearFileOrderAsync(int tenantId, int parentFolderId, FileEntryType entryType)
    {
        return AbstractQueries.ClearFileOrderAsync(this, tenantId, parentFolderId, entryType);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, FileEntryType.File])]
    public Task<int> GetLastFileOrderAsync(int tenantId, int parentFolderId, FileEntryType entryType)
    {
        return AbstractQueries.GetLastFileOrderAsync(this, tenantId, parentFolderId, entryType);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task IncreaseFileOrderAsync(int tenantId, int parentFolderId, int newOrder, int currentOrder)
    {
        return AbstractQueries.IncreaseFileOrderAsync(this, tenantId, parentFolderId, newOrder, currentOrder);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task DecreaseFileOrderAsync(int tenantId, int parentFolderId, int newOrder, int currentOrder)
    {
        return AbstractQueries.DecreaseFileOrderAsync(this, tenantId, parentFolderId, newOrder, currentOrder);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task ChangeFilesCountAsync(int tenantId, int folderId, int counter)
    {
        return AbstractQueries.ChangeFilesCountAsync(this, tenantId, folderId, counter);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task ChangeFoldersCountAsync(int tenantId, int folderId, int counter)
    {
        return AbstractQueries.ChangeFoldersCountAsync(this, tenantId, folderId, counter);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, FileEntryType.File])]
    public Task<int> DeleteAuditReferencesAsync(int entryId, FileEntryType entryType)
    {
        return AbstractQueries.DeleteAuditReferencesAsync(this, entryId, entryType);
    }
}

static file class AbstractQueries
{
    public static readonly Func<FilesDbContext, int, IEnumerable<int>, IAsyncEnumerable<DbFolder>> FoldersAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> folderIds) =>
                ctx.Folders
                    .AsTracking()
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => ctx.Tree.Any(a => folderIds.Contains(a.FolderId) && a.ParentId == r.Id)));
    
    public static readonly Func<FilesDbContext, int, int, Task<int>> FilesCountAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Files
                    .Join(ctx.Tree, a => a.ParentId, b => b.FolderId, (file, tree) => new { file, tree })
                    .Where(r => r.file.TenantId == tenantId)
                    .Where(r => r.tree.ParentId == folderId)
                    .Select(r => r.file.Id)
                    .Distinct()
                    .Count());

    public static readonly Func<FilesDbContext, int, string, Task<string>> IdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string hashId) =>
                ctx.ThirdpartyIdMapping
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.HashId == hashId)
                    .Select(r => r.Id)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, int, FileEntryType, Task<bool>> IsIndexingAsync =
    Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
        (FilesDbContext ctx, int tenantId, int parentFolderId, FileEntryType entryType) =>
            (from rs in ctx.RoomSettings 
                where rs.TenantId == tenantId && rs.RoomId ==
                    (from t in ctx.Tree
                        where t.FolderId == parentFolderId
                        orderby t.Level descending
                        select t.ParentId
                    ).Skip(1).FirstOrDefault()
                select rs.Indexing).FirstOrDefault());
    
    public static readonly Func<FilesDbContext, int, int, FileEntryType, Task<DbFileOrder>> GetFileOrderAsync =
    Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
        (FilesDbContext ctx, int tenantId, int entryId, FileEntryType entryType) =>
            ctx.FileOrder
                .AsTracking()
                .FirstOrDefault(r =>  r.TenantId == tenantId && r.EntryId == entryId && r.EntryType == entryType));

    public static readonly Func<FilesDbContext, int, int, FileEntryType, Task> ClearFileOrderAsync =
    Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
        (FilesDbContext ctx, int tenantId, int parentFolderId, FileEntryType entryType) =>
            ctx.FileOrder
                .Where(r => r.TenantId == tenantId && r.EntryType == entryType && r.ParentFolderId == parentFolderId)
                .ExecuteDelete());


    public static readonly Func<FilesDbContext, int, int, FileEntryType, Task<int>> GetLastFileOrderAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int parentFolderId, FileEntryType entryType) =>
                ctx.FileOrder
                    .AsTracking()
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.ParentFolderId == parentFolderId)
                    .Where(r => r.EntryType == entryType)
                    .OrderBy(r => r.Order)
                    .Select(r => r.Order)
                    .LastOrDefault());

    public static readonly Func<FilesDbContext, int, int, int, int, Task> IncreaseFileOrderAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int parentFolderId, int newOrder, int currentOrder) =>
                ctx.FileOrder
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.ParentFolderId == parentFolderId)
                    .Where(r => r.Order >= newOrder && r.Order < currentOrder)
                    .ExecuteUpdate(f => f.SetProperty(p => p.Order, p => p.Order + 1)));

    public static readonly Func<FilesDbContext, int, int, int, int, Task> DecreaseFileOrderAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int parentFolderId, int newOrder, int currentOrder) =>
                ctx.FileOrder
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.ParentFolderId == parentFolderId)
                    .Where(r => r.Order <= newOrder && r.Order > currentOrder)
                    .ExecuteUpdate(f => f.SetProperty(p => p.Order, p => p.Order - 1)));
    
    public static readonly Func<FilesDbContext, int, int, int, Task> ChangeFilesCountAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId, int counter) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId && ctx.Tree.Any(a => a.FolderId == folderId && a.ParentId == r.Id))
                    .ExecuteUpdate(r => r.SetProperty(a => a.FilesCount, a => a.FilesCount + counter)));
    
    public static readonly Func<FilesDbContext, int, int, int, Task> ChangeFoldersCountAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId, int counter) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId && ctx.Tree.Any(a => a.FolderId == folderId && a.ParentId == r.Id))
                    .ExecuteUpdate(r => r.SetProperty(a => a.FoldersCount, a => a.FoldersCount + counter)));
    
    public static readonly Func<FilesDbContext, int, FileEntryType, Task<int>> DeleteAuditReferencesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int entryId, FileEntryType entryType) =>
                ctx.FilesAuditReference
                    .Where(r => r.EntryId == entryId)
                    .Where(r => r.EntryType == (byte)entryType)
                    .ExecuteDelete());
}