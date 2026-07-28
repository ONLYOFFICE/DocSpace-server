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

namespace ASC.Web.Files.Core.Search;

/// <summary>
/// The nested OpenSearch document for a single metadata field value.
/// </summary>
public class MetadataFieldValueSearch
{
    public int TemplateId { get; set; }
    public int FieldId { get; set; }

    /// <summary>
    /// The string value lowered at write time so the term query is case-insensitive.
    /// </summary>
    [Keyword]
    public string StringValue { get; set; }

    public DateTime? DateValue { get; set; }
    public long? NumberValue { get; set; }

    [Keyword]
    public List<string> OptionIds { get; set; }
}

/// <summary>
/// The base OpenSearch document for entry metadata (one document per entry).
/// </summary>
public abstract class MetadataSearchItemBase : ISearchItem
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ParentId { get; set; }

    [Nested]
    public List<DbFolderTree> Folders { get; set; }

    [Nested]
    public List<MetadataFieldValueSearch> Values { get; set; }

    /// <summary>
    /// The concatenated string values of the globally visible system template fields.
    /// They participate in the general text search without selecting a template.
    /// </summary>
    [Text(Analyzer = "whitespacecustom")]
    public string GlobalText { get; set; }

    [Ignore]
    public abstract string IndexName { get; }

    public Expression<Func<ISearchItem, object[]>> GetSearchContentFields(SearchSettingsHelper searchSettings)
    {
        return a => new object[] { GlobalText };
    }
}

[Transient]
public class DbFileMetadataSearch : MetadataSearchItemBase
{
    [Ignore]
    public override string IndexName => "files_metadata_file";
}

[Transient]
public class DbFolderMetadataSearch : MetadataSearchItemBase
{
    [Ignore]
    public override string IndexName => "files_metadata_folder";
}

[Scope]
public class BaseIndexerFileMetadata(Client client,
    ILogger<BaseIndexerFileMetadata> log,
    IDbContextFactory<WebstudioDbContext> dbContextManager,
    TenantManager tenantManager,
    BaseIndexerHelper baseIndexerHelper,
    Settings settings,
    IServiceProvider serviceProvider)
    : BaseIndexer<DbFileMetadataSearch>(client, log, dbContextManager, tenantManager, baseIndexerHelper, settings, serviceProvider);

[Scope]
public class BaseIndexerFolderMetadata(Client client,
    ILogger<BaseIndexerFolderMetadata> log,
    IDbContextFactory<WebstudioDbContext> dbContextManager,
    TenantManager tenantManager,
    BaseIndexerHelper baseIndexerHelper,
    Settings settings,
    IServiceProvider serviceProvider)
    : BaseIndexer<DbFolderMetadataSearch>(client, log, dbContextManager, tenantManager, baseIndexerHelper, settings, serviceProvider);

[Scope(typeof(IFactoryIndexer))]
public class FactoryIndexerFileMetadata(
    ILoggerFactory loggerFactory,
    TenantManager tenantManager,
    SearchSettingsHelper searchSettingsHelper,
    FactoryIndexer factoryIndexer,
    BaseIndexerFileMetadata baseIndexer,
    IServiceProvider serviceProvider,
    IDbContextFactory<FilesDbContext> dbContextFactory)
    : FactoryIndexer<DbFileMetadataSearch>(loggerFactory, tenantManager, searchSettingsHelper, factoryIndexer, baseIndexer, serviceProvider)
{
    public override async Task IndexAllAsync()
    {
        await MetadataSearchHelper.IndexAllAsync<DbFileMetadataSearch>(_indexer, dbContextFactory, FileEntryType.File, Index, Logger);
    }
}

[Scope(typeof(IFactoryIndexer))]
public class FactoryIndexerFolderMetadata(
    ILoggerFactory loggerFactory,
    TenantManager tenantManager,
    SearchSettingsHelper searchSettingsHelper,
    FactoryIndexer factoryIndexer,
    BaseIndexerFolderMetadata baseIndexer,
    IServiceProvider serviceProvider,
    IDbContextFactory<FilesDbContext> dbContextFactory)
    : FactoryIndexer<DbFolderMetadataSearch>(loggerFactory, tenantManager, searchSettingsHelper, factoryIndexer, baseIndexer, serviceProvider)
{
    public override async Task IndexAllAsync()
    {
        await MetadataSearchHelper.IndexAllAsync<DbFolderMetadataSearch>(_indexer, dbContextFactory, FileEntryType.Folder, Index, Logger);
    }
}

public static class MetadataSearchHelper
{
    public static async Task IndexAllAsync<TDoc>(
        BaseIndexer<TDoc> indexer,
        IDbContextFactory<FilesDbContext> dbContextFactory,
        FileEntryType entryType,
        Func<List<TDoc>, bool, int, Task> index,
        ILogger logger)
        where TDoc : MetadataSearchItemBase, new()
    {
        try
        {
            var now = DateTime.UtcNow;

            await foreach (var data in indexer.IndexAllAsync(GetCount, GetIds, GetData))
            {
                await index(data, true, 0);
            }

            await indexer.OnComplete(now);
        }
        catch (Exception e)
        {
            logger.ErrorMetadataIndexAll(e);
            throw;
        }

        return;

        List<int> GetIds(DateTime lastIndexed)
        {
            using var filesDbContext = dbContextFactory.CreateDbContext();

            return filesDbContext.MetadataValues
                .Where(r => r.EntryType == entryType && r.ModifiedOn >= lastIndexed)
                .Select(r => r.EntryId)
                .Distinct()
                .OrderBy(r => r)
                .ToList();
        }

        List<TDoc> GetData(long start, long stop, DateTime lastIndexed)
        {
            using var filesDbContext = dbContextFactory.CreateDbContext();

            var entryIds = filesDbContext.MetadataValues
                .Where(r => r.EntryType == entryType && r.ModifiedOn >= lastIndexed && r.EntryId >= start && r.EntryId <= stop)
                .Select(r => r.EntryId)
                .Distinct()
                .ToList();

            return BuildDocsAsync<TDoc>(filesDbContext, entryType, entryIds, tenantId: null).GetAwaiter().GetResult();
        }

        (int, int, int) GetCount(DateTime lastIndexed)
        {
            using var filesDbContext = dbContextFactory.CreateDbContext();

            var query = filesDbContext.MetadataValues
                .Where(r => r.EntryType == entryType && r.ModifiedOn >= lastIndexed)
                .Select(r => r.EntryId)
                .Distinct();

            var count = query.Count();
            var minId = count > 0 ? query.Min() : 0;
            var maxId = count > 0 ? query.Max() : 0;

            return (count, maxId, minId);
        }
    }

    public static async Task<List<TDoc>> BuildDocsAsync<TDoc>(FilesDbContext filesDbContext, FileEntryType entryType, IReadOnlyCollection<int> entryIds, int? tenantId)
        where TDoc : MetadataSearchItemBase, new()
    {
        if (entryIds.Count == 0)
        {
            return [];
        }

        var rowsQuery = filesDbContext.MetadataValues
            .Where(r => r.EntryType == entryType && entryIds.Contains(r.EntryId));

        if (tenantId.HasValue)
        {
            rowsQuery = rowsQuery.Where(r => r.TenantId == tenantId.Value);
        }

        var rows = await rowsQuery.ToListAsync();

        if (rows.Count == 0)
        {
            return [];
        }

        var fieldIds = rows.Select(r => r.FieldId).Distinct().ToList();

        var fields = await filesDbContext.MetadataFields
            .Where(f => fieldIds.Contains(f.Id))
            .Select(f => new { f.Id, f.TenantId, f.TemplateId })
            .ToListAsync();

        var fieldTemplates = fields.ToDictionary(f => f.Id, f => f.TemplateId);

        var tenantIds = rows.Select(r => r.TenantId).Distinct().ToList();

        var systemTemplates = await filesDbContext.MetadataTemplates
            .Where(t => t.IsSystem && tenantIds.Contains(t.TenantId))
            .Select(t => new { t.TenantId, t.Id })
            .ToDictionaryAsync(t => t.TenantId, t => t.Id);

        var ids = rows.Select(r => r.EntryId).Distinct().ToList();

        Dictionary<(int TenantId, int EntryId), int> parents;

        if (entryType == FileEntryType.File)
        {
            parents = (await filesDbContext.Files
                    .Where(f => f.CurrentVersion && ids.Contains(f.Id))
                    .Select(f => new { f.TenantId, f.Id, f.ParentId })
                    .ToListAsync())
                .GroupBy(f => (f.TenantId, f.Id))
                .ToDictionary(g => g.Key, g => g.First().ParentId);
        }
        else
        {
            parents = (await filesDbContext.Folders
                    .Where(f => ids.Contains(f.Id))
                    .Select(f => new { f.TenantId, f.Id, f.ParentId })
                    .ToListAsync())
                .GroupBy(f => (f.TenantId, f.Id))
                .ToDictionary(g => g.Key, g => g.First().ParentId);
        }

        // the doc is scoped by the ancestor chain: for files - the ancestors of the parent folder,
        // for folders - the ancestors of the folder itself
        var treeRootIds = entryType == FileEntryType.File ? parents.Values.Distinct().ToList() : ids;

        var trees = (await filesDbContext.Tree
                .Where(t => treeRootIds.Contains(t.FolderId))
                .ToListAsync())
            .GroupBy(t => t.FolderId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var docs = new List<TDoc>();

        foreach (var entryGroup in rows.GroupBy(r => new { r.TenantId, r.EntryId }))
        {
            if (!parents.TryGetValue((entryGroup.Key.TenantId, entryGroup.Key.EntryId), out var parentId))
            {
                continue;
            }

            var values = new List<MetadataFieldValueSearch>();
            var globalTextParts = new List<string>();

            systemTemplates.TryGetValue(entryGroup.Key.TenantId, out var systemTemplateId);

            foreach (var fieldGroup in entryGroup.GroupBy(r => r.FieldId))
            {
                if (!fieldTemplates.TryGetValue(fieldGroup.Key, out var templateId))
                {
                    continue;
                }

                var first = fieldGroup.First();

                var optionIds = fieldGroup
                    .Where(r => !string.IsNullOrEmpty(r.OptionId))
                    .Select(r => r.OptionId)
                    .ToList();

                values.Add(new MetadataFieldValueSearch
                {
                    TemplateId = templateId,
                    FieldId = fieldGroup.Key,
                    StringValue = optionIds.Count > 0 ? null : first.ValueString?.ToLowerInvariant(),
                    DateValue = optionIds.Count > 0 ? null : first.ValueDate,
                    NumberValue = optionIds.Count > 0 ? null : first.ValueNumber,
                    OptionIds = optionIds.Count > 0 ? optionIds : null
                });

                if (systemTemplateId != 0 && templateId == systemTemplateId && !string.IsNullOrEmpty(first.ValueString))
                {
                    globalTextParts.Add(first.ValueString);
                }
            }

            var treeRootId = entryType == FileEntryType.File ? parentId : entryGroup.Key.EntryId;

            docs.Add(new TDoc
            {
                Id = entryGroup.Key.EntryId,
                TenantId = entryGroup.Key.TenantId,
                ParentId = parentId,
                Folders = trees.TryGetValue(treeRootId, out var tree) ? tree : [],
                Values = values,
                GlobalText = globalTextParts.Count > 0 ? string.Join(' ', globalTextParts) : null
            });
        }

        return docs;
    }
}

[Scope]
public class MetadataIndexHelper(
    IDbContextFactory<FilesDbContext> dbContextFactory,
    TenantManager tenantManager,
    FactoryIndexerFileMetadata fileIndexer,
    FactoryIndexerFolderMetadata folderIndexer,
    ILogger<MetadataIndexHelper> logger)
{
    public async Task IndexEntriesAsync(FileEntryType entryType, IReadOnlyCollection<int> entryIds)
    {
        if (entryIds.Count == 0)
        {
            return;
        }

        try
        {
            var tenantId = tenantManager.GetCurrentTenantId();

            await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

            if (entryType == FileEntryType.File)
            {
                await IndexAsync(filesDbContext, fileIndexer, FileEntryType.File, entryIds, tenantId);
            }
            else
            {
                await IndexAsync(filesDbContext, folderIndexer, FileEntryType.Folder, entryIds, tenantId);
            }
        }
        catch (Exception e)
        {
            logger.WarningMetadataIndexFailed(e);
        }
    }

    private static async Task IndexAsync<TDoc>(FilesDbContext filesDbContext, FactoryIndexer<TDoc> indexer, FileEntryType entryType, IReadOnlyCollection<int> entryIds, int tenantId)
        where TDoc : MetadataSearchItemBase, new()
    {
        var docs = await MetadataSearchHelper.BuildDocsAsync<TDoc>(filesDbContext, entryType, entryIds, tenantId);

        if (docs.Count > 0)
        {
            await indexer.Index(docs);
        }

        foreach (var missingId in entryIds.Except(docs.Select(d => d.Id)))
        {
            await indexer.DeleteAsync(r => r.Where(a => a.Id, missingId));
        }
    }
}

public static partial class MetadataSearchLogger
{
    [LoggerMessage(LogLevel.Error, "Metadata IndexAll error")]
    public static partial void ErrorMetadataIndexAll(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Metadata index update failed")]
    public static partial void WarningMetadataIndexFailed(this ILogger logger, Exception exception);
}
