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
    [PreCompileQuery([PreCompileQuery.DefaultInt, int.MaxValue])]
    public Task<DbFolderQuery> DbFolderQueryAsync(int tenantId, int folderId)
    {
        return FolderQueries.DbFolderQueryAsync(this, tenantId, folderId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<DbFolder> FolderAsync(int tenantId, int folderId)
    {
        return FolderQueries.FolderAsync(this, tenantId, folderId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<bool> AnyTreeAsync(int parentId, int folderId)
    {
        return FolderQueries.AnyTreeAsync(this, parentId, folderId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<int> CountFilesAsync(int tenantId, int folderId)
    {
        return FolderQueries.CountFilesAsync(this, tenantId, folderId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public Task<int> CountTreesAsync(int parentId)
    {
        return FolderQueries.CountTreesAsync(this, parentId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<int> FolderIdAsync(int tenantId, int folderId, int parentId)
    {
        return FolderQueries.FolderIdAsync(this, tenantId, folderId, parentId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public Task<int> ParentIdAsync(int folderId)
    {
        return FolderQueries.ParentIdAsync(this, folderId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<string> RightNodeAsync(int tenantId, string key)
    {
        return FolderQueries.RightNodeAsync(this, tenantId, key);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<DbFolder> FolderForUpdateAsync(int tenantId, int id)
    {
        return FolderQueries.FolderForUpdateAsync(this, tenantId, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<DbFolder> FolderWithSettingsAsync(int tenantId, int folderId)
    {
        return FolderQueries.FolderWithSettingsAsync(this, tenantId, folderId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<int> ParentIdByIdAsync(int tenantId, int id)
    {
        return FolderQueries.ParentIdByIdAsync(this, tenantId, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<DbFolderQuery> DbFolderQueryWithSharedAsync(int tenantId, int folderId)
    {
        return FolderQueries.DbFolderQueryWithSharedAsync(this, tenantId, folderId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<int> ArrayAsync(int tenantId, int folderId)
    {
        return FolderQueries.ArrayAsync(this, tenantId, folderId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<DbFilesBunchObjects> NodeAsync(int tenantId, string[] keys)
    {
        return FolderQueries.NodeAsync(this, tenantId, keys);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<DbFilesBunchObjects> NodeOnlyAsync(int tenantId, string key)
    {
        return FolderQueries.NodeOnlyAsync(this, tenantId, key);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbFolderTree> SubfolderAsync(int folderId)
    {
        return FolderQueries.SubfolderAsync(this, folderId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbFile> DbFilesAsync(int tenantId, int folderId, int conflict)
    {
        return FolderQueries.DbFilesAsync(this, tenantId, folderId, conflict);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<OriginData> OriginsDataAsync(int tenantId, IEnumerable<int> entriesIds)
    {
        return FolderQueries.OriginsDataAsync(this, tenantId, entriesIds);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<int> SubfolderIdsAsync(int id)
    {
        return FolderQueries.SubfolderIdsAsync(this, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbFolderQuery> DbFolderQueriesAsync(int tenantId, int folderId)
    {
        return FolderQueries.DbFolderQueriesAsync(this, tenantId, folderId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbFolderTree> TreesOrderByLevel(int toFolderId)
    {
        return FolderQueries.TreesOrderByLevel(this, toFolderId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<DbFolder> DbFoldersForDeleteAsync(int tenantId, IEnumerable<int> subfolders)
    {
        return FolderQueries.DbFoldersForDeleteAsync(this, tenantId, subfolders);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<FolderTypeUsedSpacePair> FolderTypeUsedSpaceAsync(int tenantId, IEnumerable <FolderType> folderTypes)
    {
        return FolderQueries.FolderTypeUsedSpaceAsync(this, tenantId, folderTypes);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<DbFilesBunchObjects> NodeByFolderIdsAsync(int tenantId, IEnumerable<string> folderIds)
    {
        return FolderQueries.NodeByFolderIdsAsync(this, tenantId, folderIds);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<ParentIdTitlePair> ParentIdTitlePairAsync(int folderId)
    {
        return FolderQueries.ParentIdTitlePairAsync(this, folderId);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public Task<DbFolderQuery> FirstParentAsync(int folderId)
    {
        return FolderQueries.FirstParentAsync(this, folderId);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<DbFolderQuery> DbFolderQueriesByIdsAsync(int tenantId, IEnumerable<int> ids)
    {
        return FolderQueries.DbFolderQueriesByIdsAsync(this, tenantId, ids);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<DbFolderQuery> DbFolderQueriesByTextAsync(int tenantId, string text)
    {
        return FolderQueries.DbFolderQueriesByTextAsync(this, tenantId, text);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbFolderTree> FolderTreeAsync(int id, int parentId)
    {
        return FolderQueries.FolderTreeAsync(this, id, parentId);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<int> UpdateFoldersCountAsync(int tenantId, int id)
    {
        return FolderQueries.UpdateFoldersCountAsync(this, tenantId, id);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> UpdateFoldersCountsAsync(int tenantId, IEnumerable<int> ids)
    {
        return FolderQueries.UpdateFoldersCountsAsync(this, tenantId, ids);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt, null, PreCompileQuery.DefaultInt])]
    public Task<DbFolderQuery> DbFolderQueryByTitleAndParentIdAsync(int tenantId, string title, int parentId)
    {
        return FolderQueries.DbFolderQueryByTitleAndParentIdAsync(this, tenantId, title, parentId);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<int> ParentIdByFileIdAsync(int tenantId, int fileId)
    {
        return FolderQueries.ParentIdByFileIdAsync(this, tenantId, fileId);
    }
        
    [PreCompileQuery([null])]
    public Task<int> DeleteOrderAsync(IEnumerable<int> subfolders)
    {
        return FolderQueries.DeleteOrderAsync(this, subfolders);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteTagLinksAsync(int tenantId, IEnumerable<string> subfolders)
    {
        return FolderQueries.DeleteTagLinksAsync(this, tenantId, subfolders);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public Task<int> DeleteTagsAsync(int tenantId)
    {
        return FolderQueries.DeleteTagsAsync(this, tenantId);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt, null, null])]
    public Task<int> DeleteTagLinkByTagOriginAsync(int tenantId, string id, IEnumerable<string> subfolders)
    {
        return FolderQueries.DeleteTagLinkByTagOriginAsync(this, tenantId, id, subfolders);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt, null, null])]
    public Task<int> DeleteTagOriginAsync(int tenantId, string id, IEnumerable<string> subfolders)
    {
        return FolderQueries.DeleteTagOriginAsync(this, tenantId, id, subfolders);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteBunchObjectsAsync(int tenantId, string id)
    {
        return FolderQueries.DeleteBunchObjectsAsync(this, tenantId, id);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteFilesSecurityAsync(int tenantId, IEnumerable<string> subfolders)
    {
        return FolderQueries.DeleteFilesSecurityAsync(this, tenantId, subfolders);
    }
        
    [PreCompileQuery([null])]
    public Task<int> DeleteTreesBySubfoldersDictionaryAsync(IEnumerable<int> subfolders)
    {
        return FolderQueries.DeleteTreesBySubfoldersDictionaryAsync(this, subfolders);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<int> UpdateFoldersAsync(int tenantId, int folderId, int parentId, Guid modifiedBy)
    {
        return FolderQueries.UpdateFoldersAsync(this, tenantId, folderId, parentId, modifiedBy);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, PreCompileQuery.DefaultGuid])]
    public Task<int> ReassignFoldersAsync(int tenantId, Guid oldOwnerId, Guid newOwnerId)
    {
        return FolderQueries.ReassignFoldersAsync(this, tenantId, oldOwnerId, newOwnerId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, PreCompileQuery.DefaultGuid, null])]
    public Task<int> ReassignFoldersPartiallyAsync(int tenantId, Guid oldOwnerId, Guid newOwnerId, IEnumerable<int> exceptFolderIds)
    {
        return FolderQueries.ReassignFoldersPartiallyAsync(this, tenantId, oldOwnerId, newOwnerId, exceptFolderIds);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<string> LeftNodeAsync(int tenantId, string key)
    {
        return FolderQueries.LeftNodeAsync(this, tenantId, key);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, long.MaxValue])]
    public Task<int> UpdateTreeFolderCounterAsync(int tenantId, int folderId, long size)
    {
        return FolderQueries.UpdateTreeFolderCounterAsync(this, tenantId, folderId, size);
    }
}

static file class FolderQueries
{
    public static readonly Func<FilesDbContext, int, int, Task<DbFolderQuery>> DbFolderQueryAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == folderId)
                    .Select(r =>
                        new DbFolderQuery
                        {
                            Folder = r,
                            Root = (from f in ctx.Folders
                                    where f.Id ==
                                          (from t in ctx.Tree
                                           where t.FolderId == r.ParentId
                                           orderby t.Level descending
                                           select t.ParentId
                                          ).FirstOrDefault()
                                    where f.TenantId == r.TenantId
                                    select f
                                ).FirstOrDefault(),
                            Order = (
                                from f in ctx.FileOrder
                                where (
                                    from rs in ctx.RoomSettings 
                                    where rs.TenantId == f.TenantId && rs.RoomId ==
                                        (from t in ctx.Tree
                                            where t.FolderId == r.ParentId
                                            orderby t.Level descending
                                            select t.ParentId
                                        ).Skip(1).FirstOrDefault()
                                    select rs.Indexing).FirstOrDefault() && f.EntryId == r.Id && f.TenantId == r.TenantId && f.EntryType == FileEntryType.Folder
                                select f.Order
                            ).FirstOrDefault(),
                            Settings = (from f in ctx.RoomSettings 
                                where f.TenantId == r.TenantId && f.RoomId == r.Id 
                                select f).FirstOrDefault()
                        }
                    ).SingleOrDefault());

    public static readonly Func<FilesDbContext, int, int, Task<DbFolderQuery>> DbFolderQueryWithSharedAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == folderId)
                    .Select(r =>
                        new DbFolderQuery
                        {
                            Folder = r,
                            Root = (from f in ctx.Folders
                                    where f.Id ==
                                          (from t in ctx.Tree
                                           where t.FolderId == r.ParentId
                                           orderby t.Level descending
                                           select t.ParentId
                                          ).FirstOrDefault()
                                    where f.TenantId == r.TenantId
                                    select f
                                ).FirstOrDefault(),
                            Shared = (r.FolderType == FolderType.CustomRoom || r.FolderType == FolderType.PublicRoom || r.FolderType == FolderType.FillingFormsRoom) && 
                                     ctx.Security.Any(s => 
                                         s.TenantId == tenantId && 
                                         s.EntryId == r.Id.ToString() && 
                                         s.EntryType == FileEntryType.Folder && 
                                         s.SubjectType == SubjectType.PrimaryExternalLink),
                            Settings = (from f in ctx.RoomSettings 
                                where f.TenantId == r.TenantId && f.RoomId == r.Id 
                                select f).FirstOrDefault()
                        }
                    ).SingleOrDefault());

    public static readonly Func<FilesDbContext, int, string, int, Task<DbFolderQuery>> DbFolderQueryByTitleAndParentIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string title, int parentId) =>
                ctx.Folders.Where(r => r.TenantId == tenantId)
                    .Where(r => r.Title == title && r.ParentId == parentId)
                    .OrderBy(r => r.CreateOn)
                    .Select(r =>
                        new DbFolderQuery
                        {
                            Folder = r,
                            Root = (from f in ctx.Folders
                                    where f.Id ==
                                          (from t in ctx.Tree
                                           where t.FolderId == r.ParentId
                                           orderby t.Level descending
                                           select t.ParentId
                                          ).FirstOrDefault()
                                    where f.TenantId == r.TenantId
                                    select f
                                ).FirstOrDefault()
                        }
                    ).FirstOrDefault());

    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<DbFolderQuery>> DbFolderQueriesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Join(ctx.Tree, r => r.Id, a => a.ParentId, (folder, tree) => new { folder, tree })
                    .Where(r => r.tree.FolderId == folderId)
                    .OrderByDescending(r => r.tree.Level)
                    .Select(r =>
                        new DbFolderQuery
                        {
                            Folder = r.folder,
                            Root = (from f in ctx.Folders
                                    where f.Id ==
                                          (from t in ctx.Tree
                                           where t.FolderId == r.folder.ParentId
                                           orderby t.Level descending
                                           select t.ParentId
                                          ).FirstOrDefault()
                                    where f.TenantId == r.folder.TenantId
                                    select f
                                ).FirstOrDefault(),
                            Order = (
                                from f in ctx.FileOrder
                                where (
                                    from rs in ctx.RoomSettings 
                                    where rs.TenantId == f.TenantId && rs.RoomId ==
                                        (from t in ctx.Tree
                                            where t.FolderId == r.folder.ParentId
                                            orderby t.Level descending
                                            select t.ParentId
                                        ).Skip(1).FirstOrDefault()
                                    select rs.Indexing).FirstOrDefault() && f.EntryId == r.folder.Id && f.TenantId == r.folder.TenantId && f.EntryType == FileEntryType.Folder
                                select f.Order
                            ).FirstOrDefault(),
                            Settings = (from f in ctx.RoomSettings 
                                where f.TenantId == r.folder.TenantId && f.RoomId == r.folder.Id 
                                select f).FirstOrDefault()
                        }
                    ));

    public static readonly Func<FilesDbContext, int, Task<int>> ParentIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int folderId) =>
                ctx.Tree
                    .Where(r => r.FolderId == folderId)
                    .OrderByDescending(r => r.Level)
                    .Select(r => r.ParentId)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, int, Task<int>> ParentIdByFileIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId) =>
                ctx.Tree
                    .Where(r => ctx.Files
                        .Where(f => f.TenantId == tenantId)
                        .Where(f => f.Id == fileId && f.CurrentVersion)
                        .Select(f => f.ParentId)
                        .Distinct()
                        .Contains(r.FolderId))
                    .OrderByDescending(r => r.Level)
                    .Select(r => r.ParentId)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, int, Task<DbFolder>> FolderForUpdateAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int id) =>
                ctx.Folders.FirstOrDefault(r => r.TenantId == tenantId && r.Id == id));

    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<DbFolderTree>> FolderTreeAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int id, int parentId) =>
                ctx.Tree
                    .Where(r => r.FolderId == parentId)
                    .Select(o =>  new DbFolderTree
                    {
                        FolderId = id,
                        ParentId = o.ParentId,
                        Level = o.Level + 1
                    }));

    public static readonly Func<FilesDbContext, int, IAsyncEnumerable<int>> SubfolderIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int id) =>
                ctx.Tree
                    .Where(r => r.ParentId == id)
                    .Select(r => r.FolderId));

    public static readonly Func<FilesDbContext, int, int, Task<int>> ParentIdByIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int id) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == id)
                    .Select(r => r.ParentId)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, IAsyncEnumerable<DbFolder>> DbFoldersForDeleteAsync = 
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> subfolders) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => subfolders.Contains(r.Id)));


    public static readonly Func<FilesDbContext, IEnumerable<int>, Task<int>> DeleteOrderAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, IEnumerable<int> subfolders) =>
                ctx.Tree
                    .Where(r => subfolders.Contains(r.FolderId))
                    .ExecuteDelete());
    
    public static readonly Func<FilesDbContext, int, IEnumerable<string>, Task<int>> DeleteTagLinksAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<string> subfolders) =>
                ctx.TagLink
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => subfolders.Contains(r.EntryId))
                    .Where(r => r.EntryType == FileEntryType.Folder)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, Task<int>> DeleteTagsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId) =>
                ctx.Tag
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => !ctx.TagLink.Any(a => a.TenantId == tenantId && a.TagId == r.Id))
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, string, IEnumerable<string>, Task<int>> DeleteTagLinkByTagOriginAsync = 
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string id, IEnumerable<string> subfolders) =>
                ctx.TagLink
                    .Where(r => r.TenantId == tenantId)
                    .Where(l =>
                        ctx.Tag
                            .Where(r => r.TenantId == tenantId)
                            .Where(t => t.Name == id || subfolders.Contains(t.Name))
                            .Select(t => t.Id)
                            .Contains(l.TagId)
                    )
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, string, IEnumerable<string>, Task<int>> DeleteTagOriginAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string id, IEnumerable<string> subfolders) =>
                ctx.Tag
                    .Where(r => r.TenantId == tenantId)
                    .Where(t => t.Name == id || subfolders.Contains(t.Name))
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IEnumerable<string>, Task<int>> DeleteFilesSecurityAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<string> subfolders) =>
                ctx.Security
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => subfolders.Contains(r.EntryId))
                    .Where(r => r.EntryType == FileEntryType.Folder)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, string, Task<int>> DeleteBunchObjectsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string id) =>
                ctx.BunchObjects
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.LeftNode == id)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, int, int, Guid, Task<int>> UpdateFoldersAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId, int parentId, Guid modifiedBy) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == folderId)
                    .ExecuteUpdate(toUpdate => toUpdate
                        .SetProperty(p => p.ParentId, parentId)
                        .SetProperty(p => p.ModifiedOn, DateTime.UtcNow)
                        .SetProperty(p => p.ModifiedBy, modifiedBy)
                    ));

    public static readonly Func<FilesDbContext, int, IAsyncEnumerable<DbFolderTree>> SubfolderAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int folderId) =>
                ctx.Tree
                    .Where(r => r.ParentId == folderId));

    public static readonly Func<FilesDbContext, IEnumerable<int>, Task<int>> DeleteTreesBySubfoldersDictionaryAsync = 
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, IEnumerable<int> subfolders) =>
                ctx.Tree
                    .Where(r => subfolders.Contains(r.FolderId) && !subfolders.Contains(r.ParentId))
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IAsyncEnumerable<DbFolderTree>> TreesOrderByLevel =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int toFolderId) =>
                ctx.Tree
                    .Where(r => r.FolderId == toFolderId)
                    .OrderBy(r => r.Level)
                    .AsQueryable());

    public static readonly Func<FilesDbContext, int, int, Task<bool>> AnyTreeAsync = Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
        (FilesDbContext ctx, int parentId, int folderId) =>
            ctx.Tree
                .Any(r => r.ParentId == parentId && r.FolderId == folderId));

    public static readonly Func<FilesDbContext, int, int, int, Task<int>> FolderIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId, int parentId) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(a => a.Title.ToLower() == ctx.Folders
                        .Where(r => r.TenantId == tenantId)
                        .Where(r => r.Id == folderId)
                        .Select(r => r.Title.ToLower())
                        .FirstOrDefault()
                    )
                    .Where(r => r.ParentId == parentId)
                    .Select(r => r.Id)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, int, int, IAsyncEnumerable<DbFile>> DbFilesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId, int conflict) =>
                ctx.Files
                    .Join(ctx.Files, f1 => f1.Title.ToLower(), f2 => f2.Title.ToLower(), (f1, f2) => new { f1, f2 })
                    .Where(r => r.f1.TenantId == tenantId && r.f1.CurrentVersion && r.f1.ParentId == folderId)
                    .Where(r => r.f2.TenantId == tenantId && r.f2.CurrentVersion && r.f2.ParentId == conflict)
                    .Select(r => r.f1));

    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<int>> ArrayAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.ParentId == folderId)
                    .Select(r => r.Id));

    public static readonly Func<FilesDbContext, int, int, Task<DbFolder>> FolderAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Folders.FirstOrDefault(r => r.TenantId == tenantId && r.Id == folderId));
    
    public static readonly Func<FilesDbContext, int, int, Task<DbFolder>> FolderWithSettingsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Folders.Include(r => r.Settings).FirstOrDefault(r => r.TenantId == tenantId && r.Id == folderId));

    public static readonly Func<FilesDbContext, int, Task<int>> CountTreesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int parentId) =>
                ctx.Tree
                    .Join(ctx.Folders, tree => tree.FolderId, folder => folder.Id, (tree, folder) => tree)
                    .Count(r => r.ParentId == parentId && r.Level > 0));

    public static readonly Func<FilesDbContext, int, int, Task<int>> CountFilesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Join(ctx.Tree, r => r.ParentId, r => r.FolderId, (file, tree) => new { tree, file })
                    .Where(r => r.tree.ParentId == folderId)
                    .Select(r => r.file.Id)
                    .Distinct()
                    .Count());

    public static readonly Func<FilesDbContext, int, int, Task<int>> UpdateFoldersCountAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int id) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Join(ctx.Tree, r => r.Id, r => r.ParentId, (file, tree) => new { file, tree })
                    .Where(r => r.tree.FolderId == id)
                    .Select(r => r.file)
                    .ExecuteUpdate(q =>
                        q.SetProperty(r => r.FoldersCount, r => ctx.Tree.Count(t => t.ParentId == r.Id) - 1)
                    ));
    
    public static readonly Func<FilesDbContext, int, IEnumerable<int>, Task<int>> UpdateFoldersCountsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> ids) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Join(ctx.Tree, r => r.Id, r => r.ParentId, (file, tree) => new { file, tree })
                    .Where(r => ids.Contains(r.tree.FolderId))
                    .Select(r => r.file)
                    .ExecuteUpdate(q =>
                        q.SetProperty(r => r.FoldersCount, r => ctx.Tree.Count(t => t.ParentId == r.Id) - 1)
                    ));

    public static readonly Func<FilesDbContext, int, Guid, Guid, Task<int>> ReassignFoldersAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid oldOwnerId, Guid newOwnerId) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.CreateBy == oldOwnerId)
                    .ExecuteUpdate(f => f.SetProperty(p => p.CreateBy, newOwnerId)));

    public static readonly Func<FilesDbContext, int, Guid, Guid, IEnumerable<int>, Task<int>> ReassignFoldersPartiallyAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid oldOwnerId, Guid newOwnerId, IEnumerable<int> exceptFolderIds) =>
                ctx.Folders
                    .Where(f => f.TenantId == tenantId)
                    .Where(f => f.CreateBy == oldOwnerId)
                    .Where(f => ctx.Tree.FirstOrDefault(t => t.FolderId == f.Id && exceptFolderIds.Contains(t.ParentId)) == null)
                    .ExecuteUpdate(p => p.SetProperty(f => f.CreateBy, newOwnerId)));

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, IAsyncEnumerable<DbFolderQuery>>
        DbFolderQueriesByIdsAsync = Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> ids) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => ids.Contains(r.Id))
                    .Select(r =>
                        new DbFolderQuery
                        {
                            Folder = r,
                            Root = (from f in ctx.Folders
                                    where f.Id ==
                                          (from t in ctx.Tree
                                           where t.FolderId == r.ParentId
                                           orderby t.Level descending
                                           select t.ParentId
                                          ).FirstOrDefault()
                                    where f.TenantId == r.TenantId
                                    select f
                                ).FirstOrDefault()
                        }
                    ));

    public static readonly Func<FilesDbContext, int, string, IAsyncEnumerable<DbFolderQuery>> DbFolderQueriesByTextAsync = 
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string text) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Title.ToLower().Contains(text))
                    .Select(r =>
                        new DbFolderQuery
                        {
                            Folder = r,
                            Root = (from f in ctx.Folders
                                    where f.Id ==
                                          (from t in ctx.Tree
                                           where t.FolderId == r.ParentId
                                           orderby t.Level descending
                                           select t.ParentId
                                          ).FirstOrDefault()
                                    where f.TenantId == r.TenantId
                                    select f
                                ).FirstOrDefault()
                        }
                    ));

    public static readonly Func<FilesDbContext, int, string[], IAsyncEnumerable<DbFilesBunchObjects>> NodeAsync = 
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string[] keys) =>
                ctx.BunchObjects
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => keys.Any(a => a == r.RightNode)));

    public static readonly Func<FilesDbContext, int, string, IAsyncEnumerable<DbFilesBunchObjects>> NodeOnlyAsync = 
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string key) =>
                ctx.BunchObjects
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.RightNode == key));

    public static readonly Func<FilesDbContext, int, string, Task<string>> LeftNodeAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string key) =>
                ctx.BunchObjects
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.RightNode == key)
                    .Select(r => r.LeftNode)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, string, Task<string>> RightNodeAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string key) =>
                ctx.BunchObjects
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.LeftNode == key)
                    .Select(r => r.RightNode)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, IAsyncEnumerable<OriginData>> OriginsDataAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> entriesIds) =>
                ctx.TagLink
                    .Where(l => l.TenantId == tenantId)
                    .Where(l => entriesIds.Contains(Convert.ToInt32(l.EntryId)))
                    .Join(ctx.Tag
                            .Where(t => t.Type == TagType.Origin), l => l.TagId, t => t.Id,
                        (l, t) => new { t.Name, t.Type, l.EntryType, l.EntryId })
                    .GroupBy(r => r.Name, r => new { r.EntryId, r.EntryType })
                    .Select(r => new OriginData
                    {
                        OriginRoom = ctx.Folders.FirstOrDefault(f => f.TenantId == tenantId &&
                            f.Id == ctx.Tree
                                .Where(t => t.FolderId == Convert.ToInt32(r.Key))
                                .OrderByDescending(t => t.Level)
                                .Select(t => t.ParentId)
                                .Skip(1)
                                .FirstOrDefault()),
                        OriginFolder =
                            ctx.Folders.FirstOrDefault(f =>
                                f.TenantId == tenantId && f.Id == Convert.ToInt32(r.Key)),
                        Entries = r.Select(e => new KeyValuePair<string, FileEntryType>(e.EntryId, e.EntryType))
                            .ToHashSet()
                    }));

    public static readonly Func<FilesDbContext, int, IEnumerable<string>, IAsyncEnumerable<DbFilesBunchObjects>> NodeByFolderIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<string> folderIds) =>
                ctx.BunchObjects
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => folderIds.Any(a => a == r.LeftNode)));

    public static readonly Func<FilesDbContext, int, IAsyncEnumerable<ParentIdTitlePair>> ParentIdTitlePairAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int folderId) =>
                ctx.Tree
                    .Join(ctx.Folders, r => r.ParentId, s => s.Id, (t, f) => new { Tree = t, Folders = f })
                    .Where(r => r.Tree.FolderId == folderId)
                    .OrderByDescending(r => r.Tree.Level)
                    .Select(r => new ParentIdTitlePair { ParentId = r.Tree.ParentId, Title = r.Folders.Title }));

    public static readonly Func<FilesDbContext, int, Task<DbFolderQuery>> FirstParentAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int folderId) =>
                ctx.Tree.Join(ctx.Folders, r => r.ParentId, s => s.Id, (t, f) => new { Tree = t, Folders = f })
                    .Where(r => r.Tree.FolderId == folderId)
                    .OrderByDescending(r => r.Tree.Level)
                    .Select(r => new DbFolderQuery 
                    { 
                        Folder = r.Folders,
                        Settings = ctx.RoomSettings.FirstOrDefault(x => x.TenantId == r.Folders.TenantId && x.RoomId == r.Folders.Id)
                    })
                    .Skip(1)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, IEnumerable<FolderType>, IAsyncEnumerable<FolderTypeUsedSpacePair>> FolderTypeUsedSpaceAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable <FolderType> folderTypes) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .AsNoTracking()
                    .Where(r => folderTypes.Contains(r.FolderType))
                    .GroupBy(r => r.FolderType)
                    .Select(f => new FolderTypeUsedSpacePair { FolderType = f.Select(r => r.FolderType).FirstOrDefault(), UsedSpace = f.Sum(r => r.Counter) }));

    public static readonly Func<FilesDbContext, int, int, long, Task<int>> UpdateTreeFolderCounterAsync = 
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId, long size) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .AsNoTracking()
                    .Join(ctx.Tree, r => r.Id, a => a.ParentId, (folder, tree) => new { folder, tree })
                    .Where(r => r.tree.FolderId == folderId)
                    .OrderByDescending(r => r.tree.Level)
                    .ExecuteUpdate(toUpdate => toUpdate
                            .SetProperty(p => p.folder.Counter, p => p.folder.Counter + size)
                        ));
}