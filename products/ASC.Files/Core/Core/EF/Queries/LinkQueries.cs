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
    [PreCompileQuery([PreCompileQuery.DefaultInt, null, PreCompileQuery.DefaultGuid])]
    public Task<string> SourceIdAsync(int tenantId, string linkedId, Guid id)
    {
        return LinkQueries.SourceIdAsync(this, tenantId, linkedId, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null, PreCompileQuery.DefaultGuid])]
    public Task<string> LinkedIdAsync(int tenantId, string sourceId, Guid id)
    {
        return LinkQueries.LinkedIdAsync(this, tenantId, sourceId, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null, PreCompileQuery.DefaultGuid])]
    public IAsyncEnumerable<DbFilesLink> FilesLinksAsync(int tenantId, IEnumerable<string> sourceIds, Guid id)
    {
        return LinkQueries.FilesLinksAsync(this, tenantId, sourceIds, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null, PreCompileQuery.DefaultGuid])]
    public Task<DbFilesLink> FileLinkAsync(int tenantId, string sourceId, Guid id)
    {
        return LinkQueries.FileLinkAsync(this, tenantId, sourceId, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteFileLinks(int tenantId, string fileId)
    {
        return LinkQueries.DeleteFileLinks(this, tenantId, fileId);
    }
}

static file class LinkQueries
{
    public static readonly Func<FilesDbContext, int, string, Guid, Task<string>> SourceIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string linkedId, Guid id) =>
                ctx.FilesLink
                    .Where(r => r.TenantId == tenantId && r.LinkedId == linkedId && r.LinkedFor == id)
                    .Select(r => r.SourceId)
                    .SingleOrDefault());

    public static readonly Func<FilesDbContext, int, string, Guid, Task<string>> LinkedIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string sourceId, Guid id) =>
                ctx.FilesLink
                    .Where(r => r.TenantId == tenantId && r.SourceId == sourceId && r.LinkedFor == id)
                    .Select(r => r.LinkedId)
                    .OrderByDescending(r => r)
                    .LastOrDefault());

    public static readonly Func<FilesDbContext, int, IEnumerable<string>, Guid, IAsyncEnumerable<DbFilesLink>> FilesLinksAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<string> sourceIds, Guid id) =>
                ctx.FilesLink
                    .Where(r => r.TenantId == tenantId && sourceIds.Contains(r.SourceId) && r.LinkedFor == id)
                    .GroupBy(r => r.SourceId)
                    .Select(g => g.OrderByDescending(r => r.LinkedId).LastOrDefault()));

    public static readonly Func<FilesDbContext, int, string, Guid, Task<DbFilesLink>> FileLinkAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string sourceId, Guid id) =>
                ctx.FilesLink
                    .SingleOrDefault(r => r.TenantId == tenantId && r.SourceId == sourceId && r.LinkedFor == id));

    public static readonly Func<FilesDbContext, int, string, Task<int>> DeleteFileLinks =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string fileId) =>
                ctx.FilesLink
                    .Where(r => r.TenantId == tenantId && (r.SourceId == fileId || r.LinkedId == fileId))
                    .ExecuteDelete());
}