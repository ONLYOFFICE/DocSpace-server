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
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<DbFileQuery> DbFileQueryAsync(int tenantId, int fileId)
    {
        return FileQueries.DbFileQueryAsync(this, tenantId, fileId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<DbFileQuery> DbFileQueryByFileVersionAsync(int tenantId, int fileId, int fileVersion)
    {
        return FileQueries.DbFileQueryByFileVersionAsync(this, tenantId, fileId, fileVersion);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<DbFileQuery> DbFileQueryFileStableAsync(int tenantId, int fileId, int fileVersion)
    {
        return FileQueries.DbFileQueryFileStableAsync(this, tenantId, fileId, fileVersion);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null, PreCompileQuery.DefaultInt])]
    public Task<DbFileQuery> DbFileQueryByTitleAsync(int tenantId, string title, int parentId)
    {
        return FileQueries.DbFileQueryByTitleAsync(this, tenantId, title, parentId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbFileQuery> DbFileQueriesAsync(int tenantId, int fileId)
    {
        return FileQueries.DbFileQueriesAsync(this, tenantId, fileId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<DbFileQuery> DbFileQueriesByFileIdsAsync(int tenantId, IEnumerable<int> fileIds)
    {
        return FileQueries.DbFileQueriesByFileIdsAsync(this, tenantId, fileIds);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<int> FileIdsAsync(int tenantId, int parentId)
    {
        return FileQueries.FileIdsAsync(this, tenantId, parentId);
    }
    
    [PreCompileQuery([])]
    public Task<int> FileMaxIdAsync()
    {
        return FileQueries.FileMaxIdAsync(this);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<int> DisableCurrentVersionAsync(int tenantId, int fileId)
    {
        return FileQueries.DisableCurrentVersionAsync(this, tenantId, fileId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbFolderTree> DbFolderTreesAsync(int folderId)
    {
        return FileQueries.DbFolderTreesAsync(this, folderId);
    }
    
    [PreCompileQuery([null, PreCompileQuery.DefaultDateTime, PreCompileQuery.DefaultGuid, PreCompileQuery.DefaultInt])]
    public Task<int> UpdateFoldersAsync(IEnumerable<int> parentFoldersIds, DateTime modifiedOn, Guid modifiedBy, int tenantId)
    {
        return FileQueries.UpdateFoldersAsync(this, parentFoldersIds, modifiedOn, modifiedBy, tenantId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<DbFile> DbFileByVersionAsync(int tenantId, int id, int version)
    {
        return FileQueries.DbFileByVersionAsync(this, tenantId, id, version);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public  IAsyncEnumerable<DbFolderTree> DbFolderTeesAsync(int parentId)
    {
        return FileQueries.DbFolderTeesAsync(this, parentId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<int> DeleteDbFilesByVersionAsync(int tenantId, int fileId, int version)
    {
        return FileQueries.DeleteDbFilesByVersionAsync(this, tenantId, fileId, version);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<int> UpdateDbFilesByVersionAsync(int tenantId, int fileId, int version)
    {
        return FileQueries.UpdateDbFilesByVersionAsync(this, tenantId, fileId, version);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<int> ParentIdsAsync(int tenantId, int fileId)
    {
        return FileQueries.ParentIdsAsync(this, tenantId, fileId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteTagLinksAsync(int tenantId, string fileId)
    {
        return FileQueries.DeleteTagLinksAsync(this, tenantId, fileId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null, TagType.Custom])]
    public Task<int> DeleteTagLinksByTypeAsync(int tenantId, string fileId, TagType type)
    {
        return FileQueries.DeleteTagLinksByTypeAsync(this, tenantId, fileId, type);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbFile> DbFilesAsync(int tenantId, int fileId)
    {
        return FileQueries.DbFilesAsync(this, tenantId, fileId);
    }
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<int> PdfTenantFileIdsAsync(int tenantId)
    {
        return FileQueries.PdfTenantFileIdsAsync(this, tenantId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteSecurityAsync(int tenantId, string fileId)
    {
        return FileQueries.DeleteSecurityAsync(this, tenantId, fileId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null, PreCompileQuery.DefaultInt])]
    public  Task<bool> DbFilesAnyAsync(int tenantId, string title, int folderId)
    {
        return FileQueries.DbFilesAnyAsync(this, tenantId, title, folderId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<DbFile> DbFileAsync(int tenantId, int fileId)
    {
        return FileQueries.DbFileAsync(this, tenantId, fileId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, null])]
    public Task<int> UpdateDbFilesCommentAsync(int tenantId, int fileId, int fileVersion, string comment)
    {
        return FileQueries.UpdateDbFilesCommentAsync(this, tenantId, fileId, fileVersion, comment);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<int> UpdateDbFilesVersionGroupAsync(int tenantId, int fileId, int fileVersion)
    {
        return FileQueries.UpdateDbFilesVersionGroupAsync(this, tenantId, fileId, fileVersion);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<int> VersionGroupAsync(int tenantId, int fileId, int fileVersion)
    {
        return FileQueries.VersionGroupAsync(this, tenantId, fileId, fileVersion);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<int> UpdateVersionGroupAsync(int tenantId, int fileId, int fileVersion, int versionGroup)
    {
        return FileQueries.UpdateVersionGroupAsync(this, tenantId, fileId, fileVersion, versionGroup);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, PreCompileQuery.DefaultGuid])]
    public Task<int> ReassignFilesAsync(int tenantId, Guid oldOwnerId, Guid newOwnerId)
    {
        return FileQueries.ReassignFilesAsync(this, tenantId, oldOwnerId, newOwnerId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, PreCompileQuery.DefaultGuid, null])]
    public Task<int> ReassignFilesPartiallyAsync(int tenantId, Guid oldOwnerId, Guid newOwnerId, IEnumerable<int> exceptFolderIds)
    {
        return FileQueries.ReassignFilesPartiallyAsync(this, tenantId, oldOwnerId, newOwnerId, exceptFolderIds);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<DbFileQuery> DbFileQueriesByTextAsync(int tenantId, string text)
    {
        return FileQueries.DbFileQueriesByTextAsync(this, tenantId, text);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, null])]
    public Task<int> UpdateChangesAsync(int tenantId, int fileId, int version, string changes)
    {
        return FileQueries.UpdateChangesAsync(this, tenantId, fileId, version, changes);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbFile> DbFilesByVersionAndWithoutForcesaveAsync(int tenantId, int fileId, int version)
    {
        return FileQueries.DbFilesByVersionAndWithoutForcesaveAsync(this, tenantId, fileId, version);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<bool> DbFileAnyAsync(int tenantId, int fileId, int version)
    {
        return FileQueries.DbFileAnyAsync(this, tenantId, fileId, version);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultDateTime, PreCompileQuery.DefaultDateTime])]
    public IAsyncEnumerable<DbFileQueryWithSecurity> DbFileQueryWithSecurityByPeriodAsync(int tenantId, DateTime from, DateTime to)
    {
        return FileQueries.DbFileQueryWithSecurityByPeriodAsync(this, tenantId, from, to);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbFileQueryWithSecurity> DbFileQueryWithSecurityAsync(int tenantId)
    {
        return FileQueries.DbFileQueryWithSecurityAsync(this, tenantId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultDateTime])]
    public IAsyncEnumerable<int> TenantIdsByFilesAsync(DateTime fromTime)
    {
        return FileQueries.TenantIdsByFilesAsync(this, fromTime);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultDateTime])]
    public IAsyncEnumerable<int> TenantIdsBySecurityAsync(DateTime fromTime)
    {
        return FileQueries.TenantIdsBySecurityAsync(this, fromTime);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, Thumbnail.Created])]
    public Task<int> UpdateThumbnailStatusAsync(int tenantId, int fileId, int version, Thumbnail status)
    {
        return FileQueries.UpdateThumbnailStatusAsync(this, tenantId, fileId, version, status);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<string> DataAsync(int tenantId, string entryId)
    {
        return FileQueries.DataAsync(this, tenantId, entryId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteFilesPropertiesAsync(int tenantId, string entryId)
    {
        return FileQueries.DeleteFilesPropertiesAsync(this, tenantId, entryId);
    }
    
    [PreCompileQuery([])]
    public IAsyncEnumerable<FilesConverts> FilesConvertsAsync()
    {
        return FileQueries.FilesConvertsAsync(this);
    }
}

static file class FileQueries
{
    public static readonly Func<FilesDbContext, int, int, Task<DbFileQuery>> DbFileQueryAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == fileId && r.CurrentVersion)

                    .Select(r => new DbFileQuery
                    {
                        File = r,
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
                        Shared = ctx.Security.Any(x => 
                            x.TenantId == r.TenantId && 
                            (x.SubjectType == SubjectType.ExternalLink || x.SubjectType == SubjectType.PrimaryExternalLink) &&
                            ((x.EntryId == r.Id.ToString() && x.EntryType == FileEntryType.File) ||
                             (x.EntryType == FileEntryType.Folder && 
                              x.EntryId == ctx.Tree
                                  .Where(t => t.FolderId == r.ParentId)
                                  .OrderByDescending(t => t.Level)
                                  .Select(t => t.ParentId)
                                  .Skip(1)
                                  .FirstOrDefault()
                                  .ToString())))
                    })
                    .SingleOrDefault());

    public static readonly Func<FilesDbContext, int, int, int, Task<DbFileQuery>> DbFileQueryByFileVersionAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId, int fileVersion) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == fileId && r.Version == fileVersion)

                    .Select(r => new DbFileQuery
                    {
                        File = r,
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
                        Shared = ctx.Security.Any(x => 
                            x.TenantId == r.TenantId && 
                            (x.SubjectType == SubjectType.ExternalLink || x.SubjectType == SubjectType.PrimaryExternalLink) &&
                            ((x.EntryId == r.Id.ToString() && x.EntryType == FileEntryType.File) ||
                             (x.EntryType == FileEntryType.Folder && 
                              x.EntryId == ctx.Tree
                                  .Where(t => t.FolderId == r.ParentId)
                                  .OrderByDescending(t => t.Level)
                                  .Select(t => t.ParentId)
                                  .Skip(1)
                                  .FirstOrDefault()
                                  .ToString())))
                    })
                    .SingleOrDefault());

    public static readonly Func<FilesDbContext, int, int, int, Task<DbFileQuery>> DbFileQueryFileStableAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId, int fileVersion) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == fileId && r.Forcesave == ForcesaveType.None)
                    .Where(r => fileVersion < 0 || r.Version <= fileVersion)
                    .OrderByDescending(r => r.Version)

                    .Select(r => new DbFileQuery
                    {
                        File = r,
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
                    })
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, string, int, Task<DbFileQuery>> DbFileQueryByTitleAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string title, int parentId) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Title == title && r.CurrentVersion && r.ParentId == parentId)

                    .OrderBy(r => r.CreateOn)
                    .Select(r => new DbFileQuery
                    {
                        File = r,
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
                    })
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<DbFileQuery>> DbFileQueriesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == fileId)
                    .OrderByDescending(r => r.Version)

                    .Select(r => new DbFileQuery
                    {
                        File = r,
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
                    }));

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, IAsyncEnumerable<DbFileQuery>> DbFileQueriesByFileIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> fileIds) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => fileIds.Contains(r.Id) && r.CurrentVersion)

                    .Select(r => new DbFileQuery
                    {
                        File = r,
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
                    }));

    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<int>> FileIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int parentId) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.ParentId == parentId && r.CurrentVersion)
                    .Select(r => r.Id));

    public static readonly Func<FilesDbContext, Task<int>> FileMaxIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx) => 
                ctx.Files.OrderByDescending(r => r.Id).Select(r=> r.Id).FirstOrDefault());

    public static readonly Func<FilesDbContext, int, int, Task<int>> DisableCurrentVersionAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == fileId && r.CurrentVersion)
                    .ExecuteUpdate(f => f.SetProperty(p => p.CurrentVersion, false)));

    public static readonly Func<FilesDbContext, int, IAsyncEnumerable<DbFolderTree>> DbFolderTreesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int folderId) =>
                ctx.Tree
                    .Where(r => r.FolderId == folderId)
                    .OrderByDescending(r => r.Level)
                    .AsQueryable());

    public static readonly Func<FilesDbContext, IEnumerable<int>, DateTime, Guid, int, Task<int>> UpdateFoldersAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, IEnumerable<int> parentFoldersIds, DateTime modifiedOn, Guid modifiedBy, int tenantId) =>
                ctx.Folders
                    .Where(r => parentFoldersIds.Contains(r.Id) && r.TenantId == tenantId)
                    .ExecuteUpdate(f => f
                        .SetProperty(p => p.ModifiedOn, modifiedOn)
                        .SetProperty(p => p.ModifiedBy, modifiedBy)));

    public static readonly Func<FilesDbContext, int, int, int, Task<DbFile>> DbFileByVersionAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int id, int version) =>
                ctx.Files
                    .FirstOrDefault(r => r.Id == id
                                         && r.Version == version
                                         && r.TenantId == tenantId));

    public static readonly Func<FilesDbContext, int, IAsyncEnumerable<DbFolderTree>> DbFolderTeesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int parentId) =>
                ctx.Tree
                    .Where(r => r.FolderId == parentId)
                    .OrderByDescending(r => r.Level)
                    .AsQueryable());

    public static readonly Func<FilesDbContext, int, int, int, Task<int>> DeleteDbFilesByVersionAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId, int version) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == fileId && r.Version == version)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, int, int, Task<int>> UpdateDbFilesByVersionAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId, int version) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == fileId && r.Version == version)
                    .ExecuteUpdate(q => q.SetProperty(p => p.CurrentVersion, true)));

    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<int>> ParentIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == fileId)
                    .Select(a => a.ParentId)
                    .Distinct());

    public static readonly Func<FilesDbContext, int, string, Task<int>> DeleteTagLinksAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string fileId) =>
                ctx.TagLink
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.EntryId == fileId && r.EntryType == FileEntryType.File)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, string, TagType, Task<int>> DeleteTagLinksByTypeAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string fileId, TagType type) =>
                ctx.Tag
                    .Where(t => t.TenantId == tenantId)
                    .Where(t => t.Type == type)
                    .Join(ctx.TagLink, t => t.Id, l => l.TagId, (t, l) => l)
                    .Where(l => l.EntryId == fileId)
                    .Where(l => l.EntryType == FileEntryType.File)
                    .ExecuteDelete());
                

    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<DbFile>> DbFilesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == fileId));

    public static readonly Func<FilesDbContext, int, IAsyncEnumerable<int>> PdfTenantFileIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Category == (int)FilterType.None)
                    .Where(r => r.Title.EndsWith(".pdf"))
                    .Select(r => r.Id));

    public static readonly Func<FilesDbContext, int, string, Task<int>> DeleteSecurityAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string fileId) =>
                ctx.Security
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.EntryId == fileId)
                    .Where(r => r.EntryType == FileEntryType.File)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, string, int, Task<bool>> DbFilesAnyAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string title, int folderId) =>
                ctx.Files
                    .Any(r => r.Title == title &&
                              r.ParentId == folderId &&
                              r.CurrentVersion &&
                              r.TenantId == tenantId));

    public static readonly Func<FilesDbContext, int, int, Task<DbFile>> DbFileAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId) =>
                ctx.Files.FirstOrDefault(r => r.TenantId == tenantId && r.Id == fileId && r.CurrentVersion));

    public static readonly Func<FilesDbContext, int, int, int, string, Task<int>> UpdateDbFilesCommentAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId, int fileVersion, string comment) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == fileId)
                    .Where(r => r.Version == fileVersion)
                    .ExecuteUpdate(f => f.SetProperty(p => p.Comment, comment)));

    public static readonly Func<FilesDbContext, int, int, int, Task<int>> UpdateDbFilesVersionGroupAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId, int fileVersion) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == fileId)
                    .Where(r => r.Version > fileVersion)
                    .ExecuteUpdate(f => f.SetProperty(p => p.VersionGroup, p => p.VersionGroup + 1)));

    public static readonly Func<FilesDbContext, int, int, int, Task<int>> VersionGroupAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId, int fileVersion) =>
                ctx.Files

                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == fileId)
                    .Where(r => r.Version == fileVersion)
                    .Select(r => r.VersionGroup)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, int, int, int, Task<int>> UpdateVersionGroupAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId, int fileVersion, int versionGroup) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == fileId)
                    .Where(r => r.Version > fileVersion)
                    .Where(r => r.VersionGroup > versionGroup)
                    .ExecuteUpdate(f => f.SetProperty(p => p.VersionGroup, p => p.VersionGroup - 1)));

    public static readonly Func<FilesDbContext, int, Guid, Guid, Task<int>> ReassignFilesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid oldOwnerId, Guid newOwnerId) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.CreateBy == oldOwnerId)
                    .ExecuteUpdate(p => p.SetProperty(f => f.CreateBy, newOwnerId)));

    public static readonly Func<FilesDbContext, int, Guid, Guid, IEnumerable<int>, Task<int>> ReassignFilesPartiallyAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid oldOwnerId, Guid newOwnerId, IEnumerable<int> exceptFolderIds) =>
                ctx.Files
                    .Where(f => f.TenantId == tenantId)
                    .Where(f => f.CreateBy == oldOwnerId)
                    .Where(f => ctx.Tree.FirstOrDefault(t => t.FolderId == f.ParentId && exceptFolderIds.Contains(t.ParentId)) == null)
                    .ExecuteUpdate(p => p.SetProperty(f => f.CreateBy, newOwnerId)));

    public static readonly Func<FilesDbContext, int, string, IAsyncEnumerable<DbFileQuery>> DbFileQueriesByTextAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string text) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.CurrentVersion)
                    .Where(r => r.Title.ToLower().Contains(text))

                    .Select(r => new DbFileQuery
                    {
                        File = r,
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
                    }));

    public static readonly Func<FilesDbContext, int, int, int, string, Task<int>> UpdateChangesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId, int version, string changes) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == fileId)
                    .Where(r => r.Version == version)
                    .ExecuteUpdate(f => f.SetProperty(p => p.Changes, changes)));

    public static readonly Func<FilesDbContext, int, int, int, IAsyncEnumerable<DbFile>> DbFilesByVersionAndWithoutForcesaveAsync = 
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId, int version) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == fileId)
                    .Where(r => r.Forcesave == ForcesaveType.None)
                    .Where(r => version <= 0 || r.Version == version)
                    .OrderBy(r => r.Version)
                    .AsQueryable());

    public static readonly Func<FilesDbContext, int, int, int, Task<bool>> DbFileAnyAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId, int version) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Any(r => r.Id == fileId &&
                              r.Version == version &&
                              r.Changes != null));

    public static readonly Func<FilesDbContext, int, DateTime, DateTime, IAsyncEnumerable<DbFileQueryWithSecurity>> DbFileQueryWithSecurityByPeriodAsync = 
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, DateTime from, DateTime to) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.CurrentVersion)
                    .Where(r => r.ModifiedOn >= from && r.ModifiedOn <= to)
                    .Select(r => new DbFileQuery
                    {
                        File = r,
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
                    })
                    .Select(r => new DbFileQueryWithSecurity { DbFileQuery = r, Security = null }));

    public static readonly Func<FilesDbContext, int, IAsyncEnumerable<DbFileQueryWithSecurity>> DbFileQueryWithSecurityAsync = 
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.CurrentVersion)
                    .Select(r => new DbFileQuery
                    {
                        File = r,
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
                    })
                    .Join(ctx.Security.DefaultIfEmpty(), r => r.File.Id.ToString(), s => s.EntryId,
                        (f, s) => new DbFileQueryWithSecurity { DbFileQuery = f, Security = s })
                    .Where(r => r.Security.TenantId == tenantId)
                    .Where(r => r.Security.EntryType == FileEntryType.File)
                    .Where(r => r.Security.Share == FileShare.Restrict));

    public static readonly Func<FilesDbContext, DateTime, IAsyncEnumerable<int>> TenantIdsByFilesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, DateTime fromTime) =>
                ctx.Files
                    .Where(r => r.ModifiedOn > fromTime)
                    .Select(r => r.TenantId)
                    .Distinct());

    public static readonly Func<FilesDbContext, DateTime, IAsyncEnumerable<int>> TenantIdsBySecurityAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, DateTime fromTime) =>
                ctx.Security
                    .Where(r => r.TimeStamp > fromTime)
                    .Select(r => r.TenantId)
                    .Distinct());

    public static readonly Func<FilesDbContext, int, int, int, Thumbnail, Task<int>> UpdateThumbnailStatusAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId, int version, Thumbnail status) =>
                ctx.Files
                    .Where(r => r.Id == fileId && r.Version == version && r.TenantId == tenantId)
                    .ExecuteUpdate(f => f.SetProperty(p => p.ThumbnailStatus, status)));

    public static readonly Func<FilesDbContext, int, string, Task<string>> DataAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string entryId) =>
                ctx.FilesProperties
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.EntryId == entryId)
                    .Select(r => r.Data)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, string, Task<int>> DeleteFilesPropertiesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string entryId) =>
                ctx.FilesProperties
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.EntryId == entryId)
                    .ExecuteDelete());
    
    public static readonly Func<FilesDbContext, IAsyncEnumerable<FilesConverts>> FilesConvertsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery((FilesDbContext ctx) => ctx.FilesConverts);
}