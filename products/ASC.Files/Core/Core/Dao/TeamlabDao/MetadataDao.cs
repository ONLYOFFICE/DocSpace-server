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

namespace ASC.Files.Core.Data;

[Scope(typeof(IMetadataDao<int>))]
internal class MetadataDao(
    IDbContextFactory<FilesDbContext> dbContextManager,
    UserManager userManager,
    TenantManager tenantManager,
    TenantUtil tenantUtil,
    SetupInfo setupInfo,
    MaxTotalSizeStatistic maxTotalSizeStatistic,
    SettingsManager settingsManager,
    AuthContext authContext,
    IServiceProvider serviceProvider,
    IDistributedLockProvider distributedLockProvider)
    : AbstractDao(dbContextManager,
        userManager,
        tenantManager,
        tenantUtil,
        setupInfo,
        maxTotalSizeStatistic,
        settingsManager,
        authContext,
        serviceProvider,
        distributedLockProvider), IMetadataDao<int>
{
    public async Task<MetadataTemplate> SaveTemplateAsync(MetadataTemplate template)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        var now = _tenantUtil.DateTimeToUtc(_tenantUtil.DateTimeNow());
        var userId = _authContext.CurrentAccount.ID;

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        DbFilesMetadataTemplate dbTemplate;

        if (template.Id == 0)
        {
            dbTemplate = new DbFilesMetadataTemplate
            {
                TenantId = tenantId,
                Name = template.Name,
                Visible = template.Visible,
                IsSystem = template.IsSystem,
                CreateBy = userId,
                CreateOn = now,
                ModifiedBy = userId,
                ModifiedOn = now
            };

            await filesDbContext.MetadataTemplates.AddAsync(dbTemplate);
        }
        else
        {
            dbTemplate = await Query(filesDbContext.MetadataTemplates).FirstOrDefaultAsync(r => r.Id == template.Id);
            if (dbTemplate == null)
            {
                return null;
            }

            dbTemplate.Name = template.Name;
            dbTemplate.Visible = template.Visible;
            dbTemplate.ModifiedBy = userId;
            dbTemplate.ModifiedOn = now;
        }

        await filesDbContext.SaveChangesAsync();

        return ToTemplate(dbTemplate);
    }

    public async Task<MetadataTemplate> GetTemplateAsync(int templateId, bool withFields = true)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var dbTemplate = await Query(filesDbContext.MetadataTemplates).FirstOrDefaultAsync(r => r.Id == templateId);
        if (dbTemplate == null)
        {
            return null;
        }

        var template = ToTemplate(dbTemplate);

        if (withFields)
        {
            template.Fields = await Query(filesDbContext.MetadataFields)
                .Where(r => r.TemplateId == templateId)
                .OrderBy(r => r.Order)
                .Select(r => ToField(r))
                .ToListAsync();
        }

        return template;
    }

    public async IAsyncEnumerable<MetadataTemplate> GetTemplatesAsync(bool? visible = null, bool includeSystem = true, bool withFields = false)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var query = Query(filesDbContext.MetadataTemplates);

        if (visible.HasValue)
        {
            query = query.Where(r => r.Visible == visible.Value);
        }

        if (!includeSystem)
        {
            query = query.Where(r => !r.IsSystem);
        }

        var templates = await query.OrderBy(r => r.Id).ToListAsync();

        Dictionary<int, List<MetadataField>> fieldsByTemplate = null;

        if (withFields && templates.Count > 0)
        {
            var templateIds = templates.Select(t => t.Id).ToList();

            var fields = await Query(filesDbContext.MetadataFields)
                .Where(r => templateIds.Contains(r.TemplateId))
                .OrderBy(r => r.Order)
                .ToListAsync();

            fieldsByTemplate = fields
                .GroupBy(f => f.TemplateId)
                .ToDictionary(g => g.Key, g => g.Select(ToField).ToList());
        }

        foreach (var dbTemplate in templates)
        {
            var template = ToTemplate(dbTemplate);

            if (fieldsByTemplate != null && fieldsByTemplate.TryGetValue(dbTemplate.Id, out var fields))
            {
                template.Fields = fields;
            }

            yield return template;
        }
    }

    public async Task DeleteTemplateAsync(int templateId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var strategy = filesDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await using var tx = await context.Database.BeginTransactionAsync();

            var fieldIds = await context.MetadataFields
                .Where(r => r.TenantId == tenantId && r.TemplateId == templateId)
                .Select(r => r.Id)
                .ToListAsync();

            if (fieldIds.Count > 0)
            {
                await context.MetadataValues
                    .Where(r => r.TenantId == tenantId && fieldIds.Contains(r.FieldId))
                    .ExecuteDeleteAsync();

                await context.MetadataFields
                    .Where(r => r.TenantId == tenantId && r.TemplateId == templateId)
                    .ExecuteDeleteAsync();
            }

            await context.MetadataLinks
                .Where(r => r.TenantId == tenantId && r.TemplateId == templateId)
                .ExecuteDeleteAsync();

            await context.MetadataTemplates
                .Where(r => r.TenantId == tenantId && r.Id == templateId)
                .ExecuteDeleteAsync();

            await tx.CommitAsync();
        });
    }

    public async Task<MetadataTemplate> GetSystemTemplateAsync(bool withFields = true)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var dbTemplate = await Query(filesDbContext.MetadataTemplates).FirstOrDefaultAsync(r => r.IsSystem);
        if (dbTemplate == null)
        {
            return null;
        }

        return withFields ? await GetTemplateAsync(dbTemplate.Id) : ToTemplate(dbTemplate);
    }

    public async Task<MetadataField> SaveFieldAsync(MetadataField field)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        var now = _tenantUtil.DateTimeToUtc(_tenantUtil.DateTimeNow());
        var userId = _authContext.CurrentAccount.ID;

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        DbFilesMetadataField dbField;

        if (field.Id == 0)
        {
            dbField = new DbFilesMetadataField
            {
                TenantId = tenantId,
                TemplateId = field.TemplateId,
                Name = field.Name,
                Type = field.Type,
                Options = SerializeOptions(field.Options),
                Order = field.Order,
                CreateBy = userId,
                CreateOn = now,
                ModifiedBy = userId,
                ModifiedOn = now
            };

            await filesDbContext.MetadataFields.AddAsync(dbField);
        }
        else
        {
            dbField = await Query(filesDbContext.MetadataFields).FirstOrDefaultAsync(r => r.Id == field.Id);
            if (dbField == null)
            {
                return null;
            }

            dbField.Name = field.Name;
            dbField.Type = field.Type;
            dbField.Options = SerializeOptions(field.Options);
            dbField.Order = field.Order;
            dbField.ModifiedBy = userId;
            dbField.ModifiedOn = now;
        }

        await filesDbContext.SaveChangesAsync();

        return ToField(dbField);
    }

    public async Task<MetadataField> GetFieldAsync(int fieldId)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var dbField = await Query(filesDbContext.MetadataFields).FirstOrDefaultAsync(r => r.Id == fieldId);

        return dbField == null ? null : ToField(dbField);
    }

    public async IAsyncEnumerable<MetadataField> GetFieldsAsync(int templateId)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var fields = Query(filesDbContext.MetadataFields)
            .Where(r => r.TemplateId == templateId)
            .OrderBy(r => r.Order)
            .AsAsyncEnumerable();

        await foreach (var field in fields)
        {
            yield return ToField(field);
        }
    }

    public async IAsyncEnumerable<MetadataField> GetFieldsAsync(IEnumerable<int> fieldIds)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var fields = Query(filesDbContext.MetadataFields)
            .Where(r => fieldIds.Contains(r.Id))
            .AsAsyncEnumerable();

        await foreach (var field in fields)
        {
            yield return ToField(field);
        }
    }

    public async Task DeleteFieldAsync(int fieldId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var strategy = filesDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await using var tx = await context.Database.BeginTransactionAsync();

            await context.MetadataValues
                .Where(r => r.TenantId == tenantId && r.FieldId == fieldId)
                .ExecuteDeleteAsync();

            await context.MetadataFields
                .Where(r => r.TenantId == tenantId && r.Id == fieldId)
                .ExecuteDeleteAsync();

            await tx.CommitAsync();
        });
    }

    public async Task<bool> HasValuesAsync(int fieldId)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        return await Query(filesDbContext.MetadataValues).AnyAsync(r => r.FieldId == fieldId);
    }

    public async Task SaveLinksAsync(IEnumerable<MetadataTemplateLink> links)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        var now = _tenantUtil.DateTimeToUtc(_tenantUtil.DateTimeNow());
        var userId = _authContext.CurrentAccount.ID;

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        foreach (var link in links)
        {
            var entryId = (int)link.EntryId;

            var existing = await filesDbContext.MetadataLinks.FindAsync(tenantId, link.TemplateId, entryId, link.EntryType);

            if (existing == null)
            {
                await filesDbContext.MetadataLinks.AddAsync(new DbFilesMetadataLink
                {
                    TenantId = tenantId,
                    TemplateId = link.TemplateId,
                    EntryId = entryId,
                    EntryType = link.EntryType,
                    Cascade = link.Cascade,
                    SourceFolderId = link.SourceFolderId,
                    CreateBy = link.CreateBy != Guid.Empty ? link.CreateBy : userId,
                    CreateOn = now
                });
            }
            else
            {
                // direct assignment wins over cascaded provenance, cascade flag is never downgraded
                if (existing.SourceFolderId != null && link.SourceFolderId == null)
                {
                    existing.SourceFolderId = null;
                }

                if (link.Cascade && !existing.Cascade)
                {
                    existing.Cascade = true;
                }
            }
        }

        await filesDbContext.SaveChangesAsync();
    }

    public async IAsyncEnumerable<MetadataTemplateLink> GetLinksAsync(int entryId, FileEntryType entryType)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        await foreach (var link in filesDbContext.MetadataLinksByEntryAsync(tenantId, entryId, entryType))
        {
            yield return ToLink(link);
        }
    }

    public async IAsyncEnumerable<MetadataTemplateLink> GetLinksAsync(IEnumerable<int> entryIds, FileEntryType entryType)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        await foreach (var link in filesDbContext.MetadataLinksByEntriesAsync(tenantId, entryIds, entryType))
        {
            yield return ToLink(link);
        }
    }

    public async IAsyncEnumerable<int> GetCascadeTemplateIdsForAncestorsAsync(int folderId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        await foreach (var templateId in filesDbContext.MetadataCascadeTemplateIdsAsync(tenantId, folderId))
        {
            yield return templateId;
        }
    }

    public async Task DeleteLinksAsync(int entryId, FileEntryType entryType, int? templateId = null)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        if (templateId.HasValue)
        {
            await filesDbContext.DeleteMetadataLinkAsync(tenantId, entryId, entryType, templateId.Value);
        }
        else
        {
            await filesDbContext.DeleteMetadataLinksByEntriesAsync(tenantId, [entryId], entryType);
        }
    }

    public async Task DeleteLinksBySourceFolderAsync(int sourceFolderId, int? templateId = null)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        await filesDbContext.DeleteMetadataLinksBySourceFolderAsync(tenantId, sourceFolderId, templateId);
    }

    public async Task SetValuesAsync(int entryId, FileEntryType entryType, IEnumerable<MetadataValue> values)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        var now = _tenantUtil.DateTimeToUtc(_tenantUtil.DateTimeNow());
        var userId = _authContext.CurrentAccount.ID;

        var valuesList = values.ToList();
        if (valuesList.Count == 0)
        {
            return;
        }

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var strategy = filesDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await using var tx = await context.Database.BeginTransactionAsync();

            var fieldIds = valuesList.Select(v => v.FieldId).Distinct().ToList();

            await context.DeleteMetadataValuesByFieldsAsync(tenantId, entryId, entryType, fieldIds);

            foreach (var value in valuesList.Where(v => !v.IsEmpty))
            {
                foreach (var row in ToDbValues(value, tenantId, entryId, entryType, userId, now))
                {
                    await context.MetadataValues.AddAsync(row);
                }
            }

            await context.SaveChangesAsync();
            await tx.CommitAsync();
        });
    }

    public async IAsyncEnumerable<MetadataValue> GetValuesAsync(int entryId, FileEntryType entryType, IEnumerable<int> fieldIds = null)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var rows = await filesDbContext.MetadataValuesByEntryAsync(tenantId, entryId, entryType).ToListAsync();

        if (fieldIds != null)
        {
            var fieldIdSet = fieldIds.ToHashSet();
            rows = rows.Where(r => fieldIdSet.Contains(r.FieldId)).ToList();
        }

        foreach (var value in ToValues(rows))
        {
            yield return value;
        }
    }

    public async IAsyncEnumerable<MetadataValue> GetValuesAsync(IEnumerable<int> entryIds, FileEntryType entryType)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var rows = await filesDbContext.MetadataValuesByEntriesAsync(tenantId, entryIds, entryType).ToListAsync();

        foreach (var group in rows.GroupBy(r => r.EntryId))
        {
            foreach (var value in ToValues(group.ToList()))
            {
                yield return value;
            }
        }
    }

    public async Task DeleteValuesAsync(int entryId, FileEntryType entryType, IEnumerable<int> fieldIds = null)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        if (fieldIds != null)
        {
            await filesDbContext.DeleteMetadataValuesByFieldsAsync(tenantId, entryId, entryType, fieldIds);
        }
        else
        {
            await filesDbContext.DeleteMetadataValuesByEntriesAsync(tenantId, [entryId], entryType);
        }
    }

    public async IAsyncEnumerable<int> GetSubtreeFolderIdsAsync(int rootFolderId)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var folderIds = filesDbContext.Tree
            .Where(t => t.ParentId == rootFolderId && t.FolderId != rootFolderId)
            .OrderBy(t => t.FolderId)
            .Select(t => t.FolderId)
            .AsAsyncEnumerable();

        await foreach (var folderId in folderIds)
        {
            yield return folderId;
        }
    }

    public async IAsyncEnumerable<int> GetFileIdsByParentFoldersAsync(IEnumerable<int> folderIds)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var fileIds = filesDbContext.Files
            .Where(r => r.TenantId == tenantId && r.CurrentVersion && folderIds.Contains(r.ParentId))
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .Distinct()
            .AsAsyncEnumerable();

        await foreach (var fileId in fileIds)
        {
            yield return fileId;
        }
    }

    public async Task ApplyCascadeBatchAsync(IReadOnlyCollection<int> entryIds, FileEntryType entryType, IReadOnlyCollection<int> templateIds, int sourceFolderId, IReadOnlyCollection<MetadataValue> values, MetadataConflictResolveType conflict)
    {
        if (entryIds.Count == 0)
        {
            return;
        }

        var tenantId = _tenantManager.GetCurrentTenantId();
        var now = _tenantUtil.DateTimeToUtc(_tenantUtil.DateTimeNow());
        var userId = _authContext.CurrentAccount.ID;

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var strategy = filesDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await using var tx = await context.Database.BeginTransactionAsync();

            var existingLinks = await context.MetadataLinks
                .Where(r => r.TenantId == tenantId && r.EntryType == entryType && entryIds.Contains(r.EntryId) && templateIds.Contains(r.TemplateId))
                .Select(r => new { r.EntryId, r.TemplateId })
                .ToListAsync();

            var existingLinkSet = existingLinks.Select(l => (l.EntryId, l.TemplateId)).ToHashSet();

            foreach (var entryId in entryIds)
            {
                foreach (var templateId in templateIds)
                {
                    if (!existingLinkSet.Contains((entryId, templateId)))
                    {
                        await context.MetadataLinks.AddAsync(new DbFilesMetadataLink
                        {
                            TenantId = tenantId,
                            TemplateId = templateId,
                            EntryId = entryId,
                            EntryType = entryType,
                            SourceFolderId = sourceFolderId,
                            CreateBy = userId,
                            CreateOn = now
                        });
                    }
                }
            }

            var valuesList = values.Where(v => !v.IsEmpty).ToList();

            if (valuesList.Count > 0)
            {
                var fieldIds = valuesList.Select(v => v.FieldId).Distinct().ToList();

                var existingValues = await context.MetadataValues
                    .Where(r => r.TenantId == tenantId && r.EntryType == entryType && entryIds.Contains(r.EntryId) && fieldIds.Contains(r.FieldId))
                    .Select(r => new { r.EntryId, r.FieldId })
                    .ToListAsync();

                var existingValueSet = existingValues.Select(v => (v.EntryId, v.FieldId)).ToHashSet();

                if (conflict == MetadataConflictResolveType.Overwrite)
                {
                    await context.MetadataValues
                        .Where(r => r.TenantId == tenantId && r.EntryType == entryType && entryIds.Contains(r.EntryId) && fieldIds.Contains(r.FieldId))
                        .ExecuteDeleteAsync();
                }

                foreach (var entryId in entryIds)
                {
                    foreach (var value in valuesList)
                    {
                        if (conflict == MetadataConflictResolveType.Skip && existingValueSet.Contains((entryId, value.FieldId)))
                        {
                            continue;
                        }

                        foreach (var row in ToDbValues(value, tenantId, entryId, entryType, userId, now))
                        {
                            await context.MetadataValues.AddAsync(row);
                        }
                    }
                }
            }

            await context.SaveChangesAsync();
            await tx.CommitAsync();
        });
    }

    public async Task<List<MetadataTemplateLink>> GetLinksBySourceFolderAsync(int sourceFolderId, int? templateId = null)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var query = filesDbContext.MetadataLinks
            .Where(r => r.TenantId == tenantId && r.SourceFolderId == sourceFolderId);

        if (templateId.HasValue)
        {
            query = query.Where(r => r.TemplateId == templateId.Value);
        }

        return await query.Select(r => ToLink(r)).ToListAsync();
    }

    public async Task<List<MetadataTemplateLink>> GetLinksByTemplateAsync(int templateId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        return await filesDbContext.MetadataLinks
            .Where(r => r.TenantId == tenantId && r.TemplateId == templateId)
            .Select(r => ToLink(r))
            .ToListAsync();
    }

    public async Task<List<MetadataValue>> GetValueEntriesAsync(int fieldId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        return await filesDbContext.MetadataValues
            .Where(r => r.TenantId == tenantId && r.FieldId == fieldId)
            .Select(r => new MetadataValue { FieldId = r.FieldId, EntryId = r.EntryId, EntryType = r.EntryType })
            .ToListAsync();
    }

    public async Task RemoveCascadeBatchAsync(IReadOnlyCollection<int> entryIds, FileEntryType entryType, int sourceFolderId, IReadOnlyCollection<int> templateIds)
    {
        if (entryIds.Count == 0 || templateIds.Count == 0)
        {
            return;
        }

        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var strategy = filesDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await using var tx = await context.Database.BeginTransactionAsync();

            await context.MetadataLinks
                .Where(r => r.TenantId == tenantId && r.EntryType == entryType && entryIds.Contains(r.EntryId)
                    && templateIds.Contains(r.TemplateId) && r.SourceFolderId == sourceFolderId)
                .ExecuteDeleteAsync();

            var remainingLinks = await context.MetadataLinks
                .Where(r => r.TenantId == tenantId && r.EntryType == entryType && entryIds.Contains(r.EntryId) && templateIds.Contains(r.TemplateId))
                .Select(r => new { r.EntryId, r.TemplateId })
                .ToListAsync();

            var remainingLinkSet = remainingLinks.Select(l => (l.EntryId, l.TemplateId)).ToHashSet();

            var fieldsByTemplate = await context.MetadataFields
                .Where(r => r.TenantId == tenantId && templateIds.Contains(r.TemplateId))
                .Select(r => new { r.Id, r.TemplateId })
                .ToListAsync();

            foreach (var entryId in entryIds)
            {
                var orphanedFieldIds = fieldsByTemplate
                    .Where(f => !remainingLinkSet.Contains((entryId, f.TemplateId)))
                    .Select(f => f.Id)
                    .ToList();

                if (orphanedFieldIds.Count > 0)
                {
                    await context.MetadataValues
                        .Where(r => r.TenantId == tenantId && r.EntryType == entryType && r.EntryId == entryId && orphanedFieldIds.Contains(r.FieldId))
                        .ExecuteDeleteAsync();
                }
            }

            await tx.CommitAsync();
        });
    }

    private static MetadataTemplate ToTemplate(DbFilesMetadataTemplate dbTemplate)
    {
        return new MetadataTemplate
        {
            Id = dbTemplate.Id,
            Name = dbTemplate.Name,
            Visible = dbTemplate.Visible,
            IsSystem = dbTemplate.IsSystem,
            CreateBy = dbTemplate.CreateBy,
            CreateOn = dbTemplate.CreateOn,
            ModifiedBy = dbTemplate.ModifiedBy,
            ModifiedOn = dbTemplate.ModifiedOn
        };
    }

    private static MetadataField ToField(DbFilesMetadataField dbField)
    {
        return new MetadataField
        {
            Id = dbField.Id,
            TemplateId = dbField.TemplateId,
            Name = dbField.Name,
            Type = dbField.Type,
            Options = DeserializeOptions(dbField.Options),
            Order = dbField.Order,
            CreateBy = dbField.CreateBy,
            CreateOn = dbField.CreateOn,
            ModifiedBy = dbField.ModifiedBy,
            ModifiedOn = dbField.ModifiedOn
        };
    }

    private static MetadataTemplateLink ToLink(DbFilesMetadataLink dbLink)
    {
        return new MetadataTemplateLink
        {
            TemplateId = dbLink.TemplateId,
            EntryId = dbLink.EntryId,
            EntryType = dbLink.EntryType,
            Cascade = dbLink.Cascade,
            SourceFolderId = dbLink.SourceFolderId,
            CreateBy = dbLink.CreateBy,
            CreateOn = dbLink.CreateOn
        };
    }

    private static IEnumerable<MetadataValue> ToValues(List<DbFilesMetadataValue> rows)
    {
        foreach (var group in rows.GroupBy(r => r.FieldId))
        {
            var first = group.First();

            var value = new MetadataValue
            {
                FieldId = group.Key,
                EntryId = first.EntryId,
                EntryType = first.EntryType,
                CreateBy = first.CreateBy,
                CreateOn = first.CreateOn,
                ModifiedBy = first.ModifiedBy,
                ModifiedOn = first.ModifiedOn
            };

            var optionIds = group
                .Where(r => !string.IsNullOrEmpty(r.OptionId))
                .Select(r => Guid.Parse(r.OptionId))
                .ToList();

            if (optionIds.Count > 0)
            {
                value.OptionIds = optionIds;
            }
            else
            {
                value.StringValue = first.ValueString;
                value.NumberValue = first.ValueNumber;
                value.DateValue = first.ValueDate;
            }

            yield return value;
        }
    }

    private static IEnumerable<DbFilesMetadataValue> ToDbValues(MetadataValue value, int tenantId, int entryId, FileEntryType entryType, Guid userId, DateTime now)
    {
        if (value.OptionIds is { Count: > 0 })
        {
            foreach (var optionId in value.OptionIds.Distinct())
            {
                yield return new DbFilesMetadataValue
                {
                    TenantId = tenantId,
                    EntryId = entryId,
                    EntryType = entryType,
                    FieldId = value.FieldId,
                    OptionId = optionId.ToString(),
                    CreateBy = userId,
                    CreateOn = now,
                    ModifiedBy = userId,
                    ModifiedOn = now
                };
            }
        }
        else
        {
            yield return new DbFilesMetadataValue
            {
                TenantId = tenantId,
                EntryId = entryId,
                EntryType = entryType,
                FieldId = value.FieldId,
                OptionId = string.Empty,
                ValueString = value.StringValue,
                ValueNumber = value.NumberValue,
                ValueDate = value.DateValue,
                CreateBy = userId,
                CreateOn = now,
                ModifiedBy = userId,
                ModifiedOn = now
            };
        }
    }

    private static string SerializeOptions(List<MetadataFieldOption> options)
    {
        return options is { Count: > 0 } ? JsonSerializer.Serialize(options) : null;
    }

    private static List<MetadataFieldOption> DeserializeOptions(string options)
    {
        return string.IsNullOrEmpty(options) ? null : JsonSerializer.Deserialize<List<MetadataFieldOption>>(options);
    }
}
