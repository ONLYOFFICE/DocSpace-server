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
    public Task<string> SourceIdAsync(int tenantId, string linkedId, Guid id)
    {
        return LinkQueries.SourceIdAsync(this, tenantId, linkedId, id);
    }

    [PreCompileQuery]
    public Task<string> LinkedIdAsync(int tenantId, string sourceId, Guid id)
    {
        return LinkQueries.LinkedIdAsync(this, tenantId, sourceId, id);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbFilesLink> FilesLinksAsync(int tenantId, IEnumerable<string> sourceIds, Guid id)
    {
        return LinkQueries.FilesLinksAsync(this, tenantId, sourceIds, id);
    }

    [PreCompileQuery]
    public Task<DbFilesLink> FileLinkAsync(int tenantId, string sourceId, Guid id)
    {
        return LinkQueries.FileLinkAsync(this, tenantId, sourceId, id);
    }

    [PreCompileQuery]
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