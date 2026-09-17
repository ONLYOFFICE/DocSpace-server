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

    /// <summary>
    /// Turns the links inherited from the folder into direct assignments. Not precompiled on purpose:
    /// an <c>ExecuteUpdate</c> inside <c>EF.CompileAsyncQuery</c> fails to translate, which surfaced as a 403 on every un-cascade.
    /// </summary>
    public Task<int> ConvertMetadataCascadeLinksToDirectAsync(int tenantId, int sourceFolderId, int? templateId)
    {
        var links = MetadataLinks.Where(r => r.TenantId == tenantId && r.SourceFolderId == sourceFolderId);

        if (templateId is { } id)
        {
            links = links.Where(r => r.TemplateId == id);
        }

        return links.ExecuteUpdateAsync(s => s.SetProperty(r => r.SourceFolderId, (int?)null));
    }

    [PreCompileQuery]
    public Task<int> DeleteMetadataValuesByFieldsAsync(int tenantId, int entryId, FileEntryType entryType, IEnumerable<int> fieldIds)
    {
        return MetadataQueries.DeleteMetadataValuesByFieldsAsync(this, tenantId, entryId, entryType, fieldIds);
    }

    /// <summary>
    /// Stamps the entry with the cascading templates of its new ancestors and inherits their values, see the batch overload.
    /// Returns <c>true</c> when anything was written, so the caller knows the entry's metadata document must be refreshed after the commit.
    /// </summary>
    public async Task<bool> ApplyMetadataCascadeLinksAsync(int tenantId, int entryId, FileEntryType entryType, int parentFolderId, Guid createBy)
    {
        var changed = await ApplyMetadataCascadeLinksAsync(tenantId, [entryId], entryType, parentFolderId, createBy);

        return changed.Count > 0;
    }

    /// <summary>
    /// Stamps the entries, all placed into the same parent folder, with the cascading templates of their new ancestors and
    /// inherits the values of the nearest cascading ancestor into them. A field is filled when the entry has no value for it;
    /// when the folder supplying the field cascades with <see cref="MetadataConflictResolveType.Overwrite"/>, the entry's own
    /// value is replaced. An inherited link that already exists is re-pointed to the new nearest source, a direct one is left alone.
    /// Runs on the caller's context and transaction. Returns the identifiers of the entries that were written, so their
    /// metadata documents can be refreshed after the commit.
    /// </summary>
    public async Task<IReadOnlyCollection<int>> ApplyMetadataCascadeLinksAsync(int tenantId, IReadOnlyCollection<int> entryIds, FileEntryType entryType, int parentFolderId, Guid createBy)
    {
        if (entryIds.Count == 0)
        {
            return [];
        }

        var levelByFolderId = await Tree
            .Where(t => t.FolderId == parentFolderId)
            .ToDictionaryAsync(t => t.ParentId, t => t.Level);

        if (levelByFolderId.Count == 0)
        {
            return [];
        }

        var ancestorIds = levelByFolderId.Keys.ToList();

        var cascadeLinks = await MetadataLinks
            .Where(r => r.TenantId == tenantId && r.Cascade && r.EntryType == FileEntryType.Folder && ancestorIds.Contains(r.EntryId))
            .ToListAsync();

        if (cascadeLinks.Count == 0)
        {
            return [];
        }

        var linkTuples = cascadeLinks.Select(l => (l.TemplateId, l.EntryId)).ToList();

        var nearestSources = MetadataCascadeResolver.ResolveNearestSources(linkTuples, levelByFolderId);

        // how the folder supplying a template treats the values the entry already holds: (template, source folder) -> mode
        var conflictBySource = cascadeLinks.ToDictionary(l => (l.TemplateId, l.EntryId), l => l.CascadeConflict);

        var existingLinksByEntry = (await MetadataLinks
                .Where(r => r.TenantId == tenantId && r.EntryType == entryType && entryIds.Contains(r.EntryId))
                .ToListAsync())
            .ToLookup(l => l.EntryId);

        var changed = new HashSet<int>();
        var now = DateTime.UtcNow;

        foreach (var entryId in entryIds)
        {
            var entryLinks = existingLinksByEntry[entryId].ToDictionary(l => l.TemplateId);

            foreach (var (templateId, sourceFolderId) in nearestSources)
            {
                if (entryLinks.TryGetValue(templateId, out var existing))
                {
                    // a direct assignment keeps its provenance; an inherited one follows the entry to its new nearest source,
                    // otherwise an un-cascade on the old source would convert an entry that left its tree long ago
                    if (existing.SourceFolderId != null && existing.SourceFolderId != sourceFolderId)
                    {
                        existing.SourceFolderId = sourceFolderId;
                        MetadataLinks.Update(existing);
                        changed.Add(entryId);
                    }

                    continue;
                }

                await MetadataLinks.AddAsync(new DbFilesMetadataLink
                {
                    TenantId = tenantId,
                    TemplateId = templateId,
                    EntryId = entryId,
                    EntryType = entryType,
                    SourceFolderId = sourceFolderId,
                    CreateBy = createBy,
                    CreateOn = now
                });

                changed.Add(entryId);
            }
        }

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

            var fieldSources = MetadataCascadeResolver.ResolveFieldSources(
                sourceValues.Select(v => (v.FieldId, v.EntryId)),
                linkTuples,
                fieldTemplates,
                levelByFolderId);

            if (fieldSources.Count > 0)
            {
                var fieldIds = fieldSources.Keys.ToList();

                // the fields whose source folder cascades with Overwrite replace the entries' own values, the others only fill the gaps
                var overwriteFieldIds = fieldSources
                    .Where(s => conflictBySource.GetValueOrDefault((fieldTemplates[s.Key], s.Value)) == MetadataConflictResolveType.Overwrite)
                    .Select(s => s.Key)
                    .ToHashSet();

                var filledFieldsByEntry = (await MetadataValues
                        .Where(r => r.TenantId == tenantId && r.EntryType == entryType && entryIds.Contains(r.EntryId) && fieldIds.Contains(r.FieldId))
                        .Select(r => new { r.EntryId, r.FieldId })
                        .Distinct()
                        .ToListAsync())
                    .ToLookup(r => r.EntryId, r => r.FieldId);

                var overwrittenEntryIds = entryIds
                    .Where(id => filledFieldsByEntry[id].Any(overwriteFieldIds.Contains))
                    .ToList();

                if (overwrittenEntryIds.Count > 0)
                {
                    var overwriteFieldIdList = overwriteFieldIds.ToList();

                    await MetadataValues
                        .Where(r => r.TenantId == tenantId && r.EntryType == entryType && overwrittenEntryIds.Contains(r.EntryId) && overwriteFieldIdList.Contains(r.FieldId))
                        .ExecuteDeleteAsync();
                }

                foreach (var entryId in entryIds)
                {
                    var filledFieldIds = filledFieldsByEntry[entryId].ToHashSet();

                    foreach (var (fieldId, sourceEntryId) in fieldSources)
                    {
                        if (filledFieldIds.Contains(fieldId) && !overwriteFieldIds.Contains(fieldId))
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
                                CreateOn = now,
                                ModifiedBy = createBy,
                                ModifiedOn = now
                            });

                            changed.Add(entryId);
                        }
                    }
                }
            }
        }

        if (changed.Count > 0)
        {
            await SaveChangesAsync();
        }

        return changed;
    }

    /// <summary>
    /// Copies the directly assigned templates of the source entry and the values of their fields (plus the values of the
    /// system template) onto the copy. The metadata the copy has already inherited from its new ancestors is left intact:
    /// only the values of the copied templates are replaced. Runs on the caller's context and transaction — the caller owns
    /// the execution strategy, so a retried attempt starts from a fresh change tracker (see <c>IMetadataDao.CopyMetadataAsync</c>).
    /// Returns <c>true</c> when anything was written.
    /// </summary>
    public async Task<bool> CopyMetadataAsync(int tenantId, int fromEntryId, int toEntryId, FileEntryType entryType, Guid createBy)
    {
        var sourceLinks = await MetadataLinks
            .Where(r => r.TenantId == tenantId && r.EntryId == fromEntryId && r.EntryType == entryType && r.SourceFolderId == null)
            .ToListAsync();

        var copiedTemplateIds = sourceLinks.Select(l => l.TemplateId).ToList();

        var systemTemplateId = await MetadataTemplates
            .Where(t => t.TenantId == tenantId && t.IsSystem)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();

        if (systemTemplateId.HasValue)
        {
            copiedTemplateIds.Add(systemTemplateId.Value);
        }

        if (copiedTemplateIds.Count == 0)
        {
            return false;
        }

        var existingLinks = (await MetadataLinks
                .Where(r => r.TenantId == tenantId && r.EntryId == toEntryId && r.EntryType == entryType)
                .ToListAsync())
            .ToDictionary(l => l.TemplateId);

        var changed = false;
        var now = DateTime.UtcNow;

        foreach (var link in sourceLinks)
        {
            var cascade = entryType == FileEntryType.Folder && link.Cascade;

            if (existingLinks.TryGetValue(link.TemplateId, out var existing))
            {
                // the copy inherited the template from its new ancestors while the source had it assigned directly:
                // the direct assignment wins over the cascaded provenance, the same rule SaveLinksAsync applies
                if (existing.SourceFolderId != null)
                {
                    existing.SourceFolderId = null;
                    existing.Cascade = existing.Cascade || cascade;
                    existing.CascadeConflict = existing.Cascade ? link.CascadeConflict : MetadataConflictResolveType.Skip;
                    MetadataLinks.Update(existing);
                    changed = true;
                }

                continue;
            }

            await MetadataLinks.AddAsync(new DbFilesMetadataLink
            {
                TenantId = tenantId,
                TemplateId = link.TemplateId,
                EntryId = toEntryId,
                EntryType = entryType,
                Cascade = cascade,
                CascadeConflict = cascade ? link.CascadeConflict : MetadataConflictResolveType.Skip,
                CreateBy = createBy,
                CreateOn = now
            });

            changed = true;
        }

        // only the fields of the copied templates travel with the copy; a value of a template that is not
        // linked on the copy would be invisible in the UI yet still match the metadata filters
        var copiedFieldIds = await MetadataFields
            .Where(f => f.TenantId == tenantId && copiedTemplateIds.Contains(f.TemplateId))
            .Select(f => f.Id)
            .ToListAsync();

        var sourceValues = await MetadataValues
            .Where(r => r.TenantId == tenantId && r.EntryId == fromEntryId && r.EntryType == entryType && copiedFieldIds.Contains(r.FieldId))
            .ToListAsync();

        if (sourceValues.Count > 0)
        {
            var replacedFieldIds = sourceValues.Select(v => v.FieldId).Distinct().ToList();

            await MetadataValues
                .Where(r => r.TenantId == tenantId && r.EntryId == toEntryId && r.EntryType == entryType && replacedFieldIds.Contains(r.FieldId))
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
                    CreateOn = now,
                    ModifiedBy = createBy,
                    ModifiedOn = now
                });
            }

            changed = true;
        }

        if (changed)
        {
            await SaveChangesAsync();
        }

        return changed;
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

    public static readonly Func<FilesDbContext, int, int, FileEntryType, IEnumerable<int>, Task<int>> DeleteMetadataValuesByFieldsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int entryId, FileEntryType entryType, IEnumerable<int> fieldIds) =>
                ctx.MetadataValues
                    .Where(r => r.TenantId == tenantId && r.EntryId == entryId && r.EntryType == entryType && fieldIds.Contains(r.FieldId))
                    .ExecuteDelete());
}
