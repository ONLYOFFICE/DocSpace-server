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
    public IAsyncEnumerable<DbFilesMetadataLink> MetadataLinksByEntryAsync(int tenantId, int entryId, FileEntryType entryType)
    {
        return MetadataQueries.MetadataLinksByEntryAsync(this, tenantId, entryId, entryType);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbFilesMetadataLink> MetadataLinksByEntriesAsync(int tenantId, IEnumerable<int> entryIds, FileEntryType entryType)
    {
        return MetadataQueries.MetadataLinksByEntriesAsync(this, tenantId, entryIds, entryType);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<int> MetadataCascadeTemplateIdsAsync(int tenantId, int folderId)
    {
        return MetadataQueries.MetadataCascadeTemplateIdsAsync(this, tenantId, folderId);
    }

    [PreCompileQuery]
    public Task<bool> MetadataCascadeLinksExistAsync(int tenantId, int folderId)
    {
        return MetadataQueries.MetadataCascadeLinksExistAsync(this, tenantId, folderId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbFilesMetadataValue> MetadataValuesByEntryAsync(int tenantId, int entryId, FileEntryType entryType)
    {
        return MetadataQueries.MetadataValuesByEntryAsync(this, tenantId, entryId, entryType);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbFilesMetadataValue> MetadataValuesByEntriesAsync(int tenantId, IEnumerable<int> entryIds, FileEntryType entryType)
    {
        return MetadataQueries.MetadataValuesByEntriesAsync(this, tenantId, entryIds, entryType);
    }

    [PreCompileQuery]
    public Task<int> DeleteMetadataLinksByEntriesAsync(int tenantId, IEnumerable<int> entryIds, FileEntryType entryType)
    {
        return MetadataQueries.DeleteMetadataLinksByEntriesAsync(this, tenantId, entryIds, entryType);
    }

    [PreCompileQuery]
    public Task<int> DeleteMetadataValuesByEntriesAsync(int tenantId, IEnumerable<int> entryIds, FileEntryType entryType)
    {
        return MetadataQueries.DeleteMetadataValuesByEntriesAsync(this, tenantId, entryIds, entryType);
    }

    [PreCompileQuery]
    public Task<int> DeleteMetadataLinkAsync(int tenantId, int entryId, FileEntryType entryType, int templateId)
    {
        return MetadataQueries.DeleteMetadataLinkAsync(this, tenantId, entryId, entryType, templateId);
    }

    [PreCompileQuery]
    public Task<int> ConvertMetadataCascadeLinksToDirectAsync(int tenantId, int sourceFolderId, int? templateId)
    {
        return MetadataQueries.ConvertMetadataCascadeLinksToDirectAsync(this, tenantId, sourceFolderId, templateId);
    }

    [PreCompileQuery]
    public Task<int> DeleteMetadataValuesByFieldsAsync(int tenantId, int entryId, FileEntryType entryType, IEnumerable<int> fieldIds)
    {
        return MetadataQueries.DeleteMetadataValuesByFieldsAsync(this, tenantId, entryId, entryType, fieldIds);
    }

    public async Task ApplyMetadataCascadeLinksAsync(int tenantId, int entryId, FileEntryType entryType, int parentFolderId, Guid createBy)
    {
        var levelByFolderId = await Tree
            .Where(t => t.FolderId == parentFolderId)
            .ToDictionaryAsync(t => t.ParentId, t => t.Level);

        if (levelByFolderId.Count == 0)
        {
            return;
        }

        var ancestorIds = levelByFolderId.Keys.ToList();

        var cascadeLinks = await MetadataLinks
            .Where(r => r.TenantId == tenantId && r.Cascade && r.EntryType == FileEntryType.Folder && ancestorIds.Contains(r.EntryId))
            .ToListAsync();

        if (cascadeLinks.Count == 0)
        {
            return;
        }

        var existingTemplateIds = await MetadataLinks
            .Where(r => r.TenantId == tenantId && r.EntryId == entryId && r.EntryType == entryType)
            .Select(r => r.TemplateId)
            .ToListAsync();

        var nearestSources = MetadataCascadeResolver.ResolveNearestSources(
            cascadeLinks.Select(l => (l.TemplateId, l.EntryId)),
            levelByFolderId);

        var added = false;

        foreach (var (templateId, sourceFolderId) in nearestSources.Where(s => !existingTemplateIds.Contains(s.Key)))
        {
            await MetadataLinks.AddAsync(new DbFilesMetadataLink
            {
                TenantId = tenantId,
                TemplateId = templateId,
                EntryId = entryId,
                EntryType = entryType,
                SourceFolderId = sourceFolderId,
                CreateBy = createBy,
                CreateOn = DateTime.UtcNow
            });

            added = true;
        }

        // the entry inherits the values of the cascade source folders, but only into its empty
        // fields: the entry's own values always win, so a move never destroys existing metadata
        var sourceFolderIds = cascadeLinks.Select(l => l.EntryId).Distinct().ToList();

        var sourceValues = await MetadataValues
            .Where(r => r.TenantId == tenantId && r.EntryType == FileEntryType.Folder && sourceFolderIds.Contains(r.EntryId))
            .ToListAsync();

        if (sourceValues.Count > 0)
        {
            var sourceFieldIds = sourceValues.Select(v => v.FieldId).Distinct().ToList();

            var fieldTemplates = await MetadataFields
                .Where(f => f.TenantId == tenantId && sourceFieldIds.Contains(f.Id))
                .Select(f => new { f.Id, f.TemplateId })
                .ToDictionaryAsync(f => f.Id, f => f.TemplateId);

            var filledFieldIds = (await MetadataValues
                    .Where(r => r.TenantId == tenantId && r.EntryId == entryId && r.EntryType == entryType)
                    .Select(r => r.FieldId)
                    .ToListAsync())
                .ToHashSet();

            var fieldSources = MetadataCascadeResolver.ResolveFieldSources(
                sourceValues.Select(v => (v.FieldId, v.EntryId)),
                cascadeLinks.Select(l => (l.TemplateId, l.EntryId)),
                fieldTemplates,
                levelByFolderId);

            foreach (var (fieldId, sourceEntryId) in fieldSources)
            {
                if (filledFieldIds.Contains(fieldId))
                {
                    continue;
                }

                foreach (var value in sourceValues.Where(v => v.FieldId == fieldId && v.EntryId == sourceEntryId))
                {
                    await MetadataValues.AddAsync(new DbFilesMetadataValue
                    {
                        TenantId = tenantId,
                        EntryId = entryId,
                        EntryType = entryType,
                        FieldId = value.FieldId,
                        OptionId = value.OptionId,
                        ValueString = value.ValueString,
                        ValueNumber = value.ValueNumber,
                        ValueDate = value.ValueDate,
                        CreateBy = createBy,
                        CreateOn = DateTime.UtcNow,
                        ModifiedBy = createBy,
                        ModifiedOn = DateTime.UtcNow
                    });

                    added = true;
                }
            }
        }

        if (added)
        {
            await SaveChangesAsync();
        }
    }

    public async Task CopyMetadataAsync(int tenantId, int fromEntryId, int toEntryId, FileEntryType entryType, Guid createBy)
    {
        var sourceLinks = await MetadataLinks
            .Where(r => r.TenantId == tenantId && r.EntryId == fromEntryId && r.EntryType == entryType && r.SourceFolderId == null)
            .ToListAsync();

        var existingTemplateIds = await MetadataLinks
            .Where(r => r.TenantId == tenantId && r.EntryId == toEntryId && r.EntryType == entryType)
            .Select(r => r.TemplateId)
            .ToListAsync();

        foreach (var link in sourceLinks.Where(l => !existingTemplateIds.Contains(l.TemplateId)))
        {
            await MetadataLinks.AddAsync(new DbFilesMetadataLink
            {
                TenantId = tenantId,
                TemplateId = link.TemplateId,
                EntryId = toEntryId,
                EntryType = entryType,
                Cascade = entryType == FileEntryType.Folder && link.Cascade,
                CreateBy = createBy,
                CreateOn = DateTime.UtcNow
            });
        }

        var sourceValues = await MetadataValues
            .Where(r => r.TenantId == tenantId && r.EntryId == fromEntryId && r.EntryType == entryType)
            .ToListAsync();

        if (sourceValues.Count > 0)
        {
            await MetadataValues
                .Where(r => r.TenantId == tenantId && r.EntryId == toEntryId && r.EntryType == entryType)
                .ExecuteDeleteAsync();

            foreach (var value in sourceValues)
            {
                await MetadataValues.AddAsync(new DbFilesMetadataValue
                {
                    TenantId = tenantId,
                    EntryId = toEntryId,
                    EntryType = entryType,
                    FieldId = value.FieldId,
                    OptionId = value.OptionId,
                    ValueString = value.ValueString,
                    ValueNumber = value.ValueNumber,
                    ValueDate = value.ValueDate,
                    CreateBy = createBy,
                    CreateOn = DateTime.UtcNow,
                    ModifiedBy = createBy,
                    ModifiedOn = DateTime.UtcNow
                });
            }
        }

        await SaveChangesAsync();
    }
}

static file class MetadataQueries
{
    public static readonly Func<FilesDbContext, int, int, FileEntryType, IAsyncEnumerable<DbFilesMetadataLink>> MetadataLinksByEntryAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int entryId, FileEntryType entryType) =>
                ctx.MetadataLinks
                    .Where(r => r.TenantId == tenantId && r.EntryId == entryId && r.EntryType == entryType));

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, FileEntryType, IAsyncEnumerable<DbFilesMetadataLink>> MetadataLinksByEntriesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> entryIds, FileEntryType entryType) =>
                ctx.MetadataLinks
                    .Where(r => r.TenantId == tenantId && r.EntryType == entryType && entryIds.Contains(r.EntryId)));

    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<int>> MetadataCascadeTemplateIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Tree
                    .Where(t => t.FolderId == folderId)
                    .Join(ctx.MetadataLinks
                            .Where(l => l.TenantId == tenantId && l.Cascade && l.EntryType == FileEntryType.Folder),
                        t => t.ParentId,
                        l => l.EntryId,
                        (t, l) => l.TemplateId)
                    .Distinct());

    public static readonly Func<FilesDbContext, int, int, Task<bool>> MetadataCascadeLinksExistAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Tree
                    .Where(t => t.FolderId == folderId)
                    .Any(t => ctx.MetadataLinks.Any(l =>
                        l.TenantId == tenantId && l.Cascade && l.EntryType == FileEntryType.Folder && l.EntryId == t.ParentId)));

    public static readonly Func<FilesDbContext, int, int, FileEntryType, IAsyncEnumerable<DbFilesMetadataValue>> MetadataValuesByEntryAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int entryId, FileEntryType entryType) =>
                ctx.MetadataValues
                    .Where(r => r.TenantId == tenantId && r.EntryId == entryId && r.EntryType == entryType));

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, FileEntryType, IAsyncEnumerable<DbFilesMetadataValue>> MetadataValuesByEntriesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> entryIds, FileEntryType entryType) =>
                ctx.MetadataValues
                    .Where(r => r.TenantId == tenantId && r.EntryType == entryType && entryIds.Contains(r.EntryId)));

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, FileEntryType, Task<int>> DeleteMetadataLinksByEntriesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> entryIds, FileEntryType entryType) =>
                ctx.MetadataLinks
                    .Where(r => r.TenantId == tenantId && r.EntryType == entryType && entryIds.Contains(r.EntryId))
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, FileEntryType, Task<int>> DeleteMetadataValuesByEntriesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> entryIds, FileEntryType entryType) =>
                ctx.MetadataValues
                    .Where(r => r.TenantId == tenantId && r.EntryType == entryType && entryIds.Contains(r.EntryId))
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, int, FileEntryType, int, Task<int>> DeleteMetadataLinkAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int entryId, FileEntryType entryType, int templateId) =>
                ctx.MetadataLinks
                    .Where(r => r.TenantId == tenantId && r.EntryId == entryId && r.EntryType == entryType && r.TemplateId == templateId)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, int, int?, Task<int>> ConvertMetadataCascadeLinksToDirectAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int sourceFolderId, int? templateId) =>
                ctx.MetadataLinks
                    .Where(r => r.TenantId == tenantId && r.SourceFolderId == sourceFolderId)
                    .Where(r => templateId == null || r.TemplateId == templateId)
                    .ExecuteUpdate(s => s.SetProperty(r => r.SourceFolderId, (int?)null)));

    public static readonly Func<FilesDbContext, int, int, FileEntryType, IEnumerable<int>, Task<int>> DeleteMetadataValuesByFieldsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int entryId, FileEntryType entryType, IEnumerable<int> fieldIds) =>
                ctx.MetadataValues
                    .Where(r => r.TenantId == tenantId && r.EntryId == entryId && r.EntryType == entryType && fieldIds.Contains(r.FieldId))
                    .ExecuteDelete());
}
