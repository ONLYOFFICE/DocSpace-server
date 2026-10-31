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
    public Task<DbFilesSkill> SkillAsync(int tenantId, int fileId)
    {
        return SkillQueries.SkillAsync(this, tenantId, fileId);
    }

    [PreCompileQuery]
    public Task<int> DeleteSkillAsync(int tenantId, int fileId)
    {
        return SkillQueries.DeleteSkillAsync(this, tenantId, fileId);
    }

    [PreCompileQuery]
    public Task<int> DeleteSkillsAsync(int tenantId, IEnumerable<int> fileIds)
    {
        return SkillQueries.DeleteSkillsAsync(this, tenantId, fileIds);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbFilesSkillQuery> SkillsByRoomsAsync(int tenantId, IEnumerable<int> roomIds)
    {
        return SkillQueries.SkillsByRoomsAsync(this, tenantId, roomIds);
    }
}

public class DbFilesSkillQuery
{
    public int RoomId { get; init; }
    public int FileId { get; init; }
    public string Title { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
}

static file class SkillQueries
{
    public static readonly Func<FilesDbContext, int, int, Task<DbFilesSkill>> SkillAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId) =>
                ctx.Skills.FirstOrDefault(s => s.TenantId == tenantId && s.FileId == fileId));

    public static readonly Func<FilesDbContext, int, int, Task<int>> DeleteSkillAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId) =>
                ctx.Skills
                    .Where(s => s.TenantId == tenantId && s.FileId == fileId)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, Task<int>> DeleteSkillsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> fileIds) =>
                ctx.Skills
                    .Where(s => s.TenantId == tenantId && fileIds.Contains(s.FileId))
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, IAsyncEnumerable<DbFilesSkillQuery>> SkillsByRoomsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> roomIds) =>
                ctx.Files
                    .Where(f => f.TenantId == tenantId && f.CurrentVersion && roomIds.Contains(f.ParentId))
                    .Join(ctx.Skills, f => new { f.TenantId, FileId = f.Id }, s => new { s.TenantId, s.FileId },
                        (f, s) => new DbFilesSkillQuery
                        {
                            RoomId = f.ParentId,
                            FileId = f.Id,
                            Title = f.Title,
                            Name = s.Name,
                            Description = s.Description
                        }));
}
