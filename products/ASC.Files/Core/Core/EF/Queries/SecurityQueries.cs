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
    public IAsyncEnumerable<DbFilesSecurity> ForDeleteShareRecordsAsync(FileShareRecord record)
    {
        return SecurityQueries.ForDeleteShareRecordsAsync(this, record);
    }
    
    public IAsyncEnumerable<int> FolderIdsAsync(string entryId)
    {
        return SecurityQueries.FolderIdsAsync(this, entryId);
    }
    
    public IAsyncEnumerable<string> FilesIdsAsync(int tenantId, IEnumerable<int> folders)
    {
        return SecurityQueries.FilesIdsAsync(this, tenantId, folders);
    }
    
    public Task<int> DeleteForSetShareAsync(int tenantId, Guid subject, IEnumerable<string> entryIds, FileEntryType type)
    {
        return SecurityQueries.DeleteForSetShareAsync(this, tenantId, subject, entryIds, type);
    }
    
    public Task<bool> IsSharedAsync(int tenantId, string entryId, FileEntryType type)
    {
        return SecurityQueries.IsSharedAsync(this, tenantId, entryId, type);
    }
    
    public IAsyncEnumerable<DbFilesSecurity> SharesAsync(int tenantId, IEnumerable<Guid> subjects)
    {
        return SecurityQueries.SharesAsync(this, tenantId, subjects);
    }
    
    public IAsyncEnumerable<DbFilesSecurity> PureShareRecordsDbAsync(int tenantId, IEnumerable<string> files, IEnumerable<string> folders)
    {
        return SecurityQueries.PureShareRecordsDbAsync(this, tenantId, files, folders);
    }
    
    public Task<int> RemoveBySubjectAsync(int tenantId, Guid subject)
    {
        return SecurityQueries.RemoveBySubjectAsync(this, tenantId, subject);
    }
    
    public Task<int> RemoveBySubjectWithoutOwnerAsync(int tenantId, Guid subject)
    {
        return SecurityQueries.RemoveBySubjectWithoutOwnerAsync(this, tenantId, subject);
    }
    
    public IAsyncEnumerable<DbFilesSecurity> EntrySharesBySubjectsAsync(int tenantId, string entryId, FileEntryType entryType, IEnumerable<Guid> subjects)
    {
        return SecurityQueries.EntrySharesBySubjectsAsync(this, tenantId, entryId, entryType, subjects);
    }
} 

static file class SecurityQueries
{
    public static readonly Func<FilesDbContext, FileShareRecord, IAsyncEnumerable<DbFilesSecurity>> ForDeleteShareRecordsAsync = 
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, FileShareRecord record) =>
                ctx.Security
                    .Where(r => r.TenantId == record.TenantId)
                    .Where(r => r.EntryType == record.EntryType)
                    .Where(r => r.Subject == record.Subject));

    public static readonly Func<FilesDbContext, string, IAsyncEnumerable<int>> FolderIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, string entryId) =>
                ctx.Tree.Where(r => r.ParentId.ToString() == entryId)
                    .Select(r => r.FolderId));

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, IAsyncEnumerable<string>> FilesIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> folders) =>
                ctx.Files.Where(r => r.TenantId == tenantId && folders.Contains(r.ParentId))
                    .Select(r => r.Id.ToString()));

    public static readonly
        Func<FilesDbContext, int, Guid, IEnumerable<string>, FileEntryType, Task<int>> DeleteForSetShareAsync = 
            Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid subject, IEnumerable<string> entryIds, FileEntryType type) =>
                ctx.Security
                    .Where(a => a.TenantId == tenantId &&
                                entryIds.Contains(a.EntryId) &&
                                a.EntryType == type &&
                                a.Subject == subject)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, string, FileEntryType, Task<bool>> IsSharedAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string entryId, FileEntryType type) =>
                ctx.Security.Any(r => r.TenantId == tenantId && r.EntryId == entryId && r.EntryType == type
                                      && !(new[] { FileConstant.DenyDownloadId, FileConstant.DenySharingId }).Contains(
                                          r.Subject))); 

    public static readonly Func<FilesDbContext, int, IEnumerable<Guid>, IAsyncEnumerable<DbFilesSecurity>> SharesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<Guid> subjects) =>
                ctx.Security
                    .Where(r => r.TenantId == tenantId && subjects.Contains(r.Subject)));

    public static readonly
        Func<FilesDbContext, int, IEnumerable<string>, IEnumerable<string>, IAsyncEnumerable<DbFilesSecurity>> PureShareRecordsDbAsync =
            Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<string> files, IEnumerable<string> folders) =>
                ctx.Security.Where(r =>
                    (r.TenantId == tenantId && files.Contains(r.EntryId) && r.EntryType == FileEntryType.File)
                    || (r.TenantId == tenantId && folders.Contains(r.EntryId) && r.EntryType == FileEntryType.Folder)));

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
}