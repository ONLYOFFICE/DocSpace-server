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

namespace ASC.Files.Core;

public interface IMetadataDao<T>
{
    Task<MetadataTemplate> SaveTemplateAsync(MetadataTemplate template);

    /// <summary>
    /// Creates a template together with its fields in one transaction, so a failed field write cannot leave a template
    /// without its fields behind. Returns the saved template with the saved fields.
    /// </summary>
    Task<MetadataTemplate> SaveTemplateWithFieldsAsync(MetadataTemplate template, IEnumerable<MetadataField> fields);

    Task<MetadataTemplate> GetTemplateAsync(int templateId, bool withFields = true);
    IAsyncEnumerable<MetadataTemplate> GetTemplatesAsync(bool? visible = null, bool includeSystem = true, bool withFields = false);
    Task DeleteTemplateAsync(int templateId);
    Task<MetadataTemplate> GetSystemTemplateAsync(bool withFields = true);

    Task<MetadataField> SaveFieldAsync(MetadataField field);
    Task<MetadataField> GetFieldAsync(int fieldId);
    IAsyncEnumerable<MetadataField> GetFieldsAsync(int templateId);
    IAsyncEnumerable<MetadataField> GetFieldsAsync(IEnumerable<int> fieldIds);
    Task DeleteFieldAsync(int fieldId);
    Task<bool> HasValuesAsync(int fieldId);

    /// <summary>
    /// The identifiers of the fields of the template that no entry holds a value for.
    /// </summary>
    Task<List<int>> GetUnusedFieldIdsAsync(int templateId);

    /// <summary>
    /// Whether any entry has one of the options selected in the choice field.
    /// </summary>
    Task<bool> HasValuesAsync(int fieldId, IEnumerable<Guid> optionIds);

    Task SaveLinksAsync(IEnumerable<MetadataTemplateLink> links);
    IAsyncEnumerable<MetadataTemplateLink> GetLinksAsync(T entryId, FileEntryType entryType);
    IAsyncEnumerable<MetadataTemplateLink> GetLinksAsync(IEnumerable<T> entryIds, FileEntryType entryType);
    IAsyncEnumerable<MetadataTemplateLink> GetLinksAsync(IEnumerable<T> fileIds, IEnumerable<T> folderIds);
    IAsyncEnumerable<int> GetCascadeTemplateIdsForAncestorsAsync(T folderId);
    Task DeleteLinksAsync(T entryId, FileEntryType entryType, int? templateId = null);

    /// <summary>
    /// Removes the template from the entry together with the values of the template's fields, in one transaction.
    /// </summary>
    Task DeleteLinkWithValuesAsync(T entryId, FileEntryType entryType, int templateId);

    Task ConvertCascadeLinksToDirectAsync(int sourceFolderId, int? templateId = null);

    Task SetValuesAsync(T entryId, FileEntryType entryType, IEnumerable<MetadataValue> values);
    IAsyncEnumerable<MetadataValue> GetValuesAsync(T entryId, FileEntryType entryType, IEnumerable<int> fieldIds = null);
    IAsyncEnumerable<MetadataValue> GetValuesAsync(IEnumerable<T> entryIds, FileEntryType entryType);
    Task DeleteValuesAsync(T entryId, FileEntryType entryType, IEnumerable<int> fieldIds = null);

    /// <summary>
    /// Copies the directly assigned templates and the values of the source entry onto its copy in a transaction of its own.
    /// Returns <c>true</c> when anything was written, so the copy's metadata document must be refreshed.
    /// </summary>
    Task<bool> CopyMetadataAsync(T fromEntryId, T toEntryId, FileEntryType entryType);

    IAsyncEnumerable<int> GetSubtreeFolderIdsAsync(int rootFolderId);
    IAsyncEnumerable<int> GetFileIdsByParentFoldersAsync(IEnumerable<int> folderIds);
    Task ApplyCascadeBatchAsync(IReadOnlyCollection<int> entryIds, FileEntryType entryType, IReadOnlyCollection<int> templateIds, int sourceFolderId, IReadOnlyCollection<MetadataValue> values, MetadataConflictResolveType conflict);
    Task<List<MetadataTemplateLink>> GetCascadeLinksByFoldersAsync(IEnumerable<int> folderIds);
    Task<List<MetadataTemplateLink>> GetCascadeLinksInSubtreeAsync(int rootFolderId, IEnumerable<int> templateIds);
    Task<List<int>> GetFolderIdsInSubtreesAsync(IEnumerable<int> rootFolderIds);
    Task<Dictionary<int, int>> GetAncestorLevelsAsync(int folderId);
    Task<List<MetadataTemplateLink>> GetLinksByTemplateAsync(int templateId);
    Task<List<MetadataValue>> GetValueEntriesAsync(int fieldId);
}
