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

[Scope]
public class MetadataService(
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    AuthContext authContext,
    UserManager userManager,
    TenantManager tenantManager,
    SocketManager socketManager,
    IDistributedLockProvider distributedLockProvider,
    MetadataCascadeWorker cascadeWorker,
    FilesMessageService filesMessageService,
    MetadataIndexHelper metadataIndexHelper)
{
    public const string SystemTemplateName = "System";

    /// <summary>
    /// The most custom fields one entry may hold. The custom fields are free-form and any editor creates them, so the
    /// limit keeps a single entry from turning into a spreadsheet.
    /// </summary>
    public const int MaxCustomFieldsPerEntry = 50;

    private const int MaxCustomFieldNameLength = 255;

    /// <summary>
    /// The assignment of a template is a check-then-insert on the link table; two concurrent assignments of the same
    /// template to the same entry (a double click) would otherwise collide on the primary key instead of being idempotent.
    /// </summary>
    private string GetLinksLockKey(int entryId, FileEntryType entryType)
    {
        return $"metadata_links_{tenantManager.GetCurrentTenantId()}_{(int)entryType}_{entryId}";
    }

    public async Task<MetadataTemplate> CreateTemplateAsync(string name, bool visible, IEnumerable<MetadataField> fields = null)
    {
        await DemandTemplateManagementAsync();

        ArgumentException.ThrowIfNullOrEmpty(name);

        var metadataDao = daoFactory.GetMetadataDao<int>();

        await CheckTemplateNameIsFreeAsync(metadataDao, name, 0);

        var fieldsList = fields?.ToList();

        // the whole batch is validated before anything is persisted, and the template is saved together with its fields
        // in one transaction, otherwise an invalid field or a failed write would leave an orphan template behind
        foreach (var field in fieldsList ?? [])
        {
            PrepareField(field);
        }

        var template = await metadataDao.SaveTemplateWithFieldsAsync(new MetadataTemplate { Name = name, Visible = visible }, fieldsList);

        filesMessageService.Send(MessageAction.MetadataTemplateCreated, template.Name);

        return template;
    }

    public async Task<MetadataTemplate> UpdateTemplateAsync(int templateId, string name, bool? visible)
    {
        await DemandTemplateManagementAsync();

        var metadataDao = daoFactory.GetMetadataDao<int>();

        // loaded with the fields: the update touches the name and the visibility only, but the response is the whole
        // template, and the saved copy the DAO returns carries no fields
        var template = await GetUserTemplateAsync(metadataDao, templateId, withFields: true);

        if (!string.IsNullOrEmpty(name) && !name.Equals(template.Name, StringComparison.OrdinalIgnoreCase))
        {
            await CheckTemplateNameIsFreeAsync(metadataDao, name, templateId);
        }

        template.Name = string.IsNullOrEmpty(name) ? template.Name : name;
        template.Visible = visible ?? template.Visible;

        var saved = await metadataDao.SaveTemplateAsync(template);
        saved.Fields = template.Fields;

        filesMessageService.Send(MessageAction.MetadataTemplateUpdated, saved.Name);

        return saved;
    }

    public async Task DeleteTemplateAsync(int templateId)
    {
        await DemandTemplateManagementAsync();

        var metadataDao = daoFactory.GetMetadataDao<int>();

        var template = await GetUserTemplateAsync(metadataDao, templateId, withFields: false);

        var affectedLinks = await metadataDao.GetLinksByTemplateAsync(templateId);

        await metadataDao.DeleteTemplateAsync(templateId);

        filesMessageService.Send(MessageAction.MetadataTemplateDeleted, template.Name);

        await ReindexEntriesAsync(affectedLinks.Select(l => ((int)l.EntryId, l.EntryType)));
    }

    public async Task<MetadataTemplate> GetTemplateAsync(int templateId)
    {
        return await GetUserTemplateAsync(daoFactory.GetMetadataDao<int>(), templateId, withFields: true);
    }

    /// <summary>
    /// The templates a user can see and assign. The system template holding the custom fields is not one of them:
    /// the API shows the custom fields on the entries themselves (see <see cref="SetCustomFieldsAsync"/>).
    /// </summary>
    public IAsyncEnumerable<MetadataTemplate> GetTemplatesAsync(bool? visible = null, bool withFields = false)
    {
        return daoFactory.GetMetadataDao<int>().GetTemplatesAsync(visible, includeSystem: false, withFields);
    }

    public async Task<MetadataField> CreateFieldAsync(int templateId, MetadataField field)
    {
        await DemandTemplateManagementAsync();

        var metadataDao = daoFactory.GetMetadataDao<int>();

        _ = await GetUserTemplateAsync(metadataDao, templateId, withFields: false);

        field.TemplateId = templateId;

        var saved = await CreateFieldInternalAsync(metadataDao, field);

        filesMessageService.Send(MessageAction.MetadataFieldCreated, saved.Name);

        return saved;
    }

    public async Task<MetadataField> UpdateFieldAsync(int templateId, int fieldId, MetadataFieldUpdate update)
    {
        await DemandTemplateManagementAsync();

        var metadataDao = daoFactory.GetMetadataDao<int>();

        _ = await GetUserTemplateAsync(metadataDao, templateId, withFields: false);
        var field = await GetTemplateFieldAsync(metadataDao, templateId, fieldId);

        if (!string.IsNullOrEmpty(update.Name))
        {
            field.Name = update.Name;
        }

        // the type is optional in a partial update: only an explicitly requested different type is a type change
        if (update.Type is { } type && type != field.Type)
        {
            if (await metadataDao.HasValuesAsync(fieldId))
            {
                throw new ArgumentException(@"The field type cannot be changed because values exist", nameof(update));
            }

            field.Type = type;
        }

        if (update.Options != null)
        {
            // new options arrive without an id and must get one here as well, not only on create
            var options = WithGeneratedOptionIds(update.Options);

            var removedOptionIds = (field.Options ?? [])
                .Select(o => o.Id)
                .Except(options.Select(o => o.Id))
                .ToList();

            // only an option somebody has selected is protected; any value of the field used to block the removal of
            // every option, so an unused one could not be dropped without clearing the field on all entries first
            if (removedOptionIds.Count > 0 && await metadataDao.HasValuesAsync(fieldId, removedOptionIds))
            {
                throw new ArgumentException(@"An option in use cannot be removed", nameof(update));
            }

            field.Options = options;
        }

        field.Order = update.Order ?? field.Order;

        ValidateField(field);

        var saved = await metadataDao.SaveFieldAsync(field);

        filesMessageService.Send(MessageAction.MetadataFieldUpdated, saved.Name);

        return saved;
    }

    public async Task DeleteFieldAsync(int templateId, int fieldId)
    {
        await DemandTemplateManagementAsync();

        var metadataDao = daoFactory.GetMetadataDao<int>();

        _ = await GetUserTemplateAsync(metadataDao, templateId, withFields: false);
        var field = await GetTemplateFieldAsync(metadataDao, templateId, fieldId);

        await DeleteFieldInternalAsync(metadataDao, field);
    }

    /// <summary>
    /// Sets the custom fields of the entry by name: a listed field gets the value, a null or empty value removes the field
    /// from the entry, the fields not listed are left alone. A name the tenant has not seen yet creates the field; a field
    /// no entry holds a value for any more is dropped, so the hidden dictionary follows the values instead of growing forever.
    /// Every write of the custom fields of a tenant runs under one lock: a create, a drop and a value write must not
    /// interleave, or a value could land on a field another request has just dropped.
    /// </summary>
    public async Task<List<CustomFieldValue>> SetCustomFieldsAsync(int entryId, FileEntryType entryType, IReadOnlyCollection<CustomFieldUpdate> updates)
    {
        var normalized = NormalizeCustomFieldUpdates(updates);

        var entry = await DemandEntryAccessAsync(entryId, entryType, edit: true);

        var metadataDao = daoFactory.GetMetadataDao<int>();
        var tenantId = tenantManager.GetCurrentTenantId();

        MetadataTemplate systemTemplate;
        List<MetadataValue> values;

        await using (await distributedLockProvider.TryAcquireFairLockAsync($"metadata_system_template_{tenantId}"))
        {
            systemTemplate = await metadataDao.GetSystemTemplateAsync();

            if (systemTemplate == null)
            {
                // the tenant has no custom fields yet: nothing to remove from, and nothing to create when only removals came
                if (normalized.All(u => u.Value == null))
                {
                    return [];
                }

                systemTemplate = await metadataDao.SaveTemplateAsync(new MetadataTemplate { Name = SystemTemplateName, Visible = true, IsSystem = true });
            }

            // two fields with one name should not exist, but the fields used to be renamed without the lock: the first one
            // by order is the field the name means, the same choice the reads make, so a stale duplicate cannot fail the write
            var fieldsByName = systemTemplate.Fields
                .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // the limit is checked before anything is written, so a refused request leaves no fields behind
            var heldFieldIds = await metadataDao.GetValuesAsync(entryId, entryType, systemTemplate.Fields.Select(f => f.Id))
                .Select(v => v.FieldId)
                .ToHashSetAsync();

            var newFields = 0;

            foreach (var update in normalized)
            {
                if (fieldsByName.TryGetValue(update.Name, out var known))
                {
                    if (update.Value == null)
                    {
                        heldFieldIds.Remove(known.Id);
                    }
                    else
                    {
                        heldFieldIds.Add(known.Id);
                    }
                }
                else if (update.Value != null)
                {
                    newFields++;
                }
            }

            if (heldFieldIds.Count + newFields > MaxCustomFieldsPerEntry)
            {
                throw new ArgumentException($@"An entry can hold at most {MaxCustomFieldsPerEntry} custom fields", nameof(updates));
            }

            var writes = new List<MetadataValue>();

            foreach (var update in normalized)
            {
                if (!fieldsByName.TryGetValue(update.Name, out var field))
                {
                    if (update.Value == null)
                    {
                        // removing a field the tenant does not have is a no-op, not an error
                        continue;
                    }

                    field = await metadataDao.SaveFieldAsync(new MetadataField
                    {
                        TemplateId = systemTemplate.Id,
                        Name = update.Name,
                        Type = MetadataFieldType.String,
                        Order = systemTemplate.Fields.Count
                    });

                    systemTemplate.Fields.Add(field);
                    fieldsByName[field.Name] = field;
                }

                // a null value is an empty MetadataValue, which the DAO stores as "no row"
                writes.Add(new MetadataValue { FieldId = field.Id, StringValue = update.Value });
            }

            await metadataDao.SetValuesAsync(entryId, entryType, writes);

            await DropUnusedCustomFieldsAsync(metadataDao, systemTemplate);

            values = await metadataDao.GetValuesAsync(entryId, entryType, systemTemplate.Fields.Select(f => f.Id)).ToListAsync();
        }

        await filesMessageService.SendAsync(MessageAction.MetadataValuesUpdated, entry, entry.Title);

        await NotifyUpdateAsync(entry);

        await metadataIndexHelper.IndexEntriesAsync(entryType, [entryId]);

        return ToCustomFields(systemTemplate, values);
    }

    public async Task<EntryMetadata> GetEntryMetadataAsync(int entryId, FileEntryType entryType)
    {
        await DemandEntryAccessAsync(entryId, entryType, edit: false);

        var metadataDao = daoFactory.GetMetadataDao<int>();

        var templateIds = await metadataDao.GetLinksAsync(entryId, entryType)
            .Select(l => l.TemplateId)
            .ToListAsync();

        var values = await metadataDao.GetValuesAsync(entryId, entryType).ToListAsync();

        var result = new EntryMetadata();

        // the custom fields are never assigned: a field is on the entry when the entry holds a value for it
        var systemTemplate = await metadataDao.GetSystemTemplateAsync();
        if (systemTemplate != null)
        {
            templateIds.Remove(systemTemplate.Id);

            result.CustomFields = ToCustomFields(systemTemplate, values);
        }

        foreach (var templateId in templateIds)
        {
            var template = await metadataDao.GetTemplateAsync(templateId);
            if (template == null)
            {
                continue;
            }

            result.Templates.Add(new TemplateMetadata
            {
                Template = template,
                Values = FilterValues(values, template)
            });
        }

        return result;
    }

    public async Task AssignTemplatesAsync(int entryId, FileEntryType entryType, IEnumerable<int> templateIds)
    {
        var entry = await DemandEntryAccessAsync(entryId, entryType, edit: true);

        var metadataDao = daoFactory.GetMetadataDao<int>();

        var links = new List<MetadataTemplateLink>();

        foreach (var templateId in templateIds.Distinct())
        {
            _ = await GetUserTemplateAsync(metadataDao, templateId, withFields: false);

            links.Add(new MetadataTemplateLink
            {
                TemplateId = templateId,
                EntryId = entryId,
                EntryType = entryType
            });
        }

        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetLinksLockKey(entryId, entryType)))
        {
            await metadataDao.SaveLinksAsync(links);
        }

        await filesMessageService.SendAsync(MessageAction.MetadataTemplateAssigned, entry, entry.Title);

        await NotifyUpdateAsync(entry);
    }

    public async Task<string> AssignTemplatesToFolderAsync(int folderId, IEnumerable<int> templateIds, bool cascade, MetadataConflictResolveType conflict)
    {
        var entry = await DemandEntryAccessAsync(folderId, FileEntryType.Folder, edit: true);

        var metadataDao = daoFactory.GetMetadataDao<int>();

        var templateIdsList = templateIds.Distinct().ToList();

        foreach (var templateId in templateIdsList)
        {
            _ = await GetUserTemplateAsync(metadataDao, templateId, withFields: false);
        }

        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetLinksLockKey(folderId, FileEntryType.Folder)))
        {
            await metadataDao.SaveLinksAsync(templateIdsList.Select(templateId => new MetadataTemplateLink
            {
                TemplateId = templateId,
                EntryId = folderId,
                EntryType = FileEntryType.Folder,
                Cascade = cascade,
                CascadeConflict = cascade ? conflict : MetadataConflictResolveType.Skip
            }));
        }

        await filesMessageService.SendAsync(MessageAction.MetadataTemplateAssigned, entry, entry.Title);

        await NotifyUpdateAsync(entry);

        if (!cascade)
        {
            return null;
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        var taskId = await cascadeWorker.StartAsync(tenantId, authContext.CurrentAccount.ID, folderId, templateIdsList, conflict, MetadataCascadeMode.Assign);

        // recorded after the start so the audit never claims a cascade that was not enqueued
        await filesMessageService.SendAsync(MessageAction.MetadataCascadeStarted, entry, entry.Title);

        return taskId;
    }

    public async Task UnassignTemplateFromFolderAsync(int folderId, int templateId)
    {
        var entry = await DemandEntryAccessAsync(folderId, FileEntryType.Folder, edit: true);

        var metadataDao = daoFactory.GetMetadataDao<int>();

        var link = await metadataDao.GetLinksAsync(folderId, FileEntryType.Folder)
            .FirstOrDefaultAsync(l => l.TemplateId == templateId);

        await UnassignTemplateAsync(folderId, FileEntryType.Folder, templateId);

        await NotifyUpdateAsync(entry);

        if (link is { Cascade: true })
        {
            // cancelling the cascade never touches the sub-entries: their inherited
            // links become direct assignments and the inherited values stay
            await metadataDao.ConvertCascadeLinksToDirectAsync(folderId, templateId);
        }
    }

    public async Task<MetadataCascadeOperation> GetCascadeStatusAsync(int folderId)
    {
        await DemandEntryAccessAsync(folderId, FileEntryType.Folder, edit: false);

        return await cascadeWorker.GetStatusAsync(tenantManager.GetCurrentTenantId(), folderId);
    }

    /// <summary>
    /// Removes the template from the entry together with the values of its fields: a value without its template
    /// would be invisible in the UI yet still match the metadata filters.
    /// </summary>
    public async Task UnassignTemplateAsync(int entryId, FileEntryType entryType, int templateId)
    {
        var entry = await DemandEntryAccessAsync(entryId, entryType, edit: true);

        var metadataDao = daoFactory.GetMetadataDao<int>();

        _ = await GetUserTemplateAsync(metadataDao, templateId, withFields: false);

        await metadataDao.DeleteLinksAsync(entryId, entryType, templateId);

        var fieldIds = await metadataDao.GetFieldsAsync(templateId).Select(f => f.Id).ToListAsync();
        if (fieldIds.Count > 0)
        {
            await metadataDao.DeleteValuesAsync(entryId, entryType, fieldIds);
        }

        await filesMessageService.SendAsync(MessageAction.MetadataTemplateUnassigned, entry, entry.Title);

        await NotifyUpdateAsync(entry);

        await metadataIndexHelper.IndexEntriesAsync(entryType, [entryId]);
    }

    public async Task<List<MetadataValue>> SetValuesAsync(int entryId, FileEntryType entryType, IEnumerable<MetadataValue> values)
    {
        var entry = await DemandEntryAccessAsync(entryId, entryType, edit: true);

        var metadataDao = daoFactory.GetMetadataDao<int>();

        var valuesList = values.ToList();

        var fields = await metadataDao.GetFieldsAsync(valuesList.Select(v => v.FieldId).Distinct()).ToDictionaryAsync(f => f.Id);

        var assignedTemplateIds = await metadataDao.GetLinksAsync(entryId, entryType)
            .Select(l => l.TemplateId)
            .ToListAsync();

        var systemTemplate = await metadataDao.GetSystemTemplateAsync(withFields: false);

        foreach (var value in valuesList)
        {
            if (!fields.TryGetValue(value.FieldId, out var field))
            {
                throw new ItemNotFoundException();
            }

            // the custom fields have their own endpoint and it is their only writer: it creates and drops the fields
            // under a lock, and a value written past that lock could land on a field being dropped
            if (systemTemplate != null && field.TemplateId == systemTemplate.Id)
            {
                throw new ArgumentException(@"The custom fields are set through the customFields endpoint", nameof(values));
            }

            if (!assignedTemplateIds.Contains(field.TemplateId))
            {
                throw new ArgumentException(@"The field does not belong to a template assigned to the entry", nameof(values));
            }

            NormalizeValue(value);
            ValidateValue(field, value);
        }

        await metadataDao.SetValuesAsync(entryId, entryType, valuesList);

        await filesMessageService.SendAsync(MessageAction.MetadataValuesUpdated, entry, entry.Title);

        await NotifyUpdateAsync(entry);

        await metadataIndexHelper.IndexEntriesAsync(entryType, [entryId]);

        // the answer is the state of the entry, not an echo of the request: the same rule the custom fields follow,
        // so a client needs no second request after a write
        return await GetTemplateValuesAsync(metadataDao, entryId, entryType, systemTemplate);
    }

    /// <summary>
    /// Every value the entry holds for the fields of its templates. The custom fields are left out: they have their own answer.
    /// </summary>
    private static async Task<List<MetadataValue>> GetTemplateValuesAsync(IMetadataDao<int> metadataDao, int entryId, FileEntryType entryType, MetadataTemplate systemTemplate)
    {
        var values = await metadataDao.GetValuesAsync(entryId, entryType).ToListAsync();

        if (systemTemplate == null || values.Count == 0)
        {
            return values;
        }

        var customFieldIds = await metadataDao.GetFieldsAsync(systemTemplate.Id).Select(f => f.Id).ToHashSetAsync();

        return values.Where(v => !customFieldIds.Contains(v.FieldId)).ToList();
    }

    /// <summary>
    /// Brings the value to its stored form. A date without a time zone offset is treated as UTC — the same rule
    /// the metadata filters apply to their date bounds, so a value can be found by the day it was written with.
    /// </summary>
    public static void NormalizeValue(MetadataValue value)
    {
        if (value.DateValue is { } date)
        {
            value.DateValue = date.Kind switch
            {
                DateTimeKind.Utc => date,
                DateTimeKind.Local => date.ToUniversalTime(),
                _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
            };
        }
    }

    public static void ValidateValue(MetadataField field, MetadataValue value)
    {
        if (value.IsEmpty)
        {
            return;
        }

        switch (field.Type)
        {
            case MetadataFieldType.String:
                if (value.NumberValue != null || value.DateValue != null || value.OptionIds is { Count: > 0 })
                {
                    throw new ArgumentException($@"The field '{field.Name}' accepts a string value only", nameof(value));
                }

                break;
            case MetadataFieldType.Date:
                if (value.DateValue == null || !string.IsNullOrEmpty(value.StringValue) || value.NumberValue != null || value.OptionIds is { Count: > 0 })
                {
                    throw new ArgumentException($@"The field '{field.Name}' accepts a date value only", nameof(value));
                }

                break;
            case MetadataFieldType.Number:
                if (value.NumberValue == null || !string.IsNullOrEmpty(value.StringValue) || value.DateValue != null || value.OptionIds is { Count: > 0 })
                {
                    throw new ArgumentException($@"The field '{field.Name}' accepts a number value only", nameof(value));
                }

                break;
            case MetadataFieldType.SingleChoice:
            case MetadataFieldType.MultiChoice:
                if (value.OptionIds is not { Count: > 0 } || !string.IsNullOrEmpty(value.StringValue) || value.NumberValue != null || value.DateValue != null)
                {
                    throw new ArgumentException($@"The field '{field.Name}' accepts option values only", nameof(value));
                }

                // duplicates are collapsed on write, so repeating the same option is still a single choice
                if (field.Type == MetadataFieldType.SingleChoice && value.OptionIds.Distinct().Count() > 1)
                {
                    throw new ArgumentException($@"The field '{field.Name}' accepts a single option only", nameof(value));
                }

                var knownOptionIds = (field.Options ?? []).Select(o => o.Id).ToHashSet();
                if (value.OptionIds.Any(id => !knownOptionIds.Contains(id)))
                {
                    throw new ArgumentException($@"The field '{field.Name}' does not contain the specified option", nameof(value));
                }

                break;
        }
    }

    internal async Task<FileEntry<int>> DemandEntryAccessAsync(int entryId, FileEntryType entryType, bool edit)
    {
        FileEntry<int> entry = entryType == FileEntryType.File
            ? await daoFactory.GetFileDao<int>().GetFileAsync(entryId)
            : await daoFactory.GetFolderDao<int>().GetFolderAsync(entryId);

        if (entry == null)
        {
            throw new ItemNotFoundException();
        }

        var allowed = edit ? await fileSecurity.CanEditAsync(entry) : await fileSecurity.CanReadAsync(entry);

        if (!allowed)
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        return entry;
    }

    /// <summary>
    /// Loads a template the API exposes. The system template holding the custom fields does not exist for the template
    /// endpoints: it cannot be read, changed, deleted, assigned or given fields through them, the custom field endpoints are its face.
    /// </summary>
    private static async Task<MetadataTemplate> GetUserTemplateAsync(IMetadataDao<int> metadataDao, int templateId, bool withFields)
    {
        var template = await metadataDao.GetTemplateAsync(templateId, withFields);

        if (template == null || template.IsSystem)
        {
            throw new ItemNotFoundException();
        }

        return template;
    }

    /// <summary>
    /// Drops the custom fields no entry holds a value for. Runs under the tenant's custom field lock, so a field found
    /// unused here is not about to receive a value from another request.
    /// </summary>
    private static async Task DropUnusedCustomFieldsAsync(IMetadataDao<int> metadataDao, MetadataTemplate systemTemplate)
    {
        foreach (var fieldId in await metadataDao.GetUnusedFieldIdsAsync(systemTemplate.Id))
        {
            await metadataDao.DeleteFieldAsync(fieldId);

            systemTemplate.Fields.RemoveAll(f => f.Id == fieldId);
        }
    }

    private static List<CustomFieldUpdate> NormalizeCustomFieldUpdates(IReadOnlyCollection<CustomFieldUpdate> updates)
    {
        if (updates is not { Count: > 0 })
        {
            throw new ArgumentException(@"At least one custom field is required", nameof(updates));
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<CustomFieldUpdate>(updates.Count);

        foreach (var update in updates)
        {
            var name = update.Name?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(@"The custom field name cannot be empty", nameof(updates));
            }

            if (name.Length > MaxCustomFieldNameLength)
            {
                throw new ArgumentException($@"The custom field name cannot be longer than {MaxCustomFieldNameLength} characters", nameof(updates));
            }

            // the name is the key of the field, so the same field twice in one request has no single meaning
            if (!names.Add(name))
            {
                throw new ArgumentException($@"The custom field '{name}' is listed more than once", nameof(updates));
            }

            result.Add(new CustomFieldUpdate(name, string.IsNullOrEmpty(update.Value) ? null : update.Value));
        }

        return result;
    }

    /// <summary>
    /// The custom fields the entry holds a value for, in their display order. A custom field is never assigned: it is on
    /// the entry exactly when the entry holds a value for it.
    /// </summary>
    private static List<CustomFieldValue> ToCustomFields(MetadataTemplate systemTemplate, List<MetadataValue> values)
    {
        var valueByField = FilterValues(values, systemTemplate).ToDictionary(v => v.FieldId);

        return systemTemplate.Fields
            .Where(f => valueByField.ContainsKey(f.Id))
            .OrderBy(f => f.Order)
            .ThenBy(f => f.Id)
            .Select(f => new CustomFieldValue { Field = f, Value = valueByField[f.Id].StringValue })
            .ToList();
    }

    private async Task DeleteFieldInternalAsync(IMetadataDao<int> metadataDao, MetadataField field)
    {
        var affectedValues = await metadataDao.GetValueEntriesAsync(field.Id);

        await metadataDao.DeleteFieldAsync(field.Id);

        filesMessageService.Send(MessageAction.MetadataFieldDeleted, field.Name);

        await ReindexEntriesAsync(affectedValues.Select(v => ((int)v.EntryId, v.EntryType)));
    }

    /// <summary>
    /// Loads the field and checks that it belongs to the template named in the route, so a field cannot be reached through another template.
    /// </summary>
    private static async Task<MetadataField> GetTemplateFieldAsync(IMetadataDao<int> metadataDao, int templateId, int fieldId)
    {
        var field = await metadataDao.GetFieldAsync(fieldId);

        if (field == null || field.TemplateId != templateId)
        {
            throw new ItemNotFoundException();
        }

        return field;
    }

    private static async Task<MetadataField> CreateFieldInternalAsync(IMetadataDao<int> metadataDao, MetadataField field)
    {
        PrepareField(field);

        return await metadataDao.SaveFieldAsync(field);
    }

    private static void PrepareField(MetadataField field)
    {
        field.Options = WithGeneratedOptionIds(field.Options);

        ValidateField(field);
    }

    private static List<MetadataFieldOption> WithGeneratedOptionIds(List<MetadataFieldOption> options)
    {
        return options?.Select(o => o.Id == Guid.Empty ? o with { Id = Guid.NewGuid() } : o).ToList();
    }

    private static void ValidateField(MetadataField field)
    {
        ArgumentException.ThrowIfNullOrEmpty(field.Name);

        var isChoice = field.Type is MetadataFieldType.SingleChoice or MetadataFieldType.MultiChoice;

        if (isChoice)
        {
            if (field.Options is not { Count: > 0 })
            {
                throw new ArgumentException(@"A choice field requires at least one option", nameof(field));
            }

            if (field.Options.Any(o => string.IsNullOrWhiteSpace(o.Value)))
            {
                throw new ArgumentException(@"An option value cannot be empty", nameof(field));
            }

            if (field.Options.Select(o => o.Value.ToLowerInvariant()).Distinct().Count() != field.Options.Count)
            {
                throw new ArgumentException(@"Option values must be unique", nameof(field));
            }

            if (field.Options.Select(o => o.Id).Distinct().Count() != field.Options.Count)
            {
                throw new ArgumentException(@"Option identifiers must be unique", nameof(field));
            }
        }
        else if (field.Options is { Count: > 0 })
        {
            throw new ArgumentException(@"Options are supported by choice fields only", nameof(field));
        }
    }

    /// <summary>
    /// The templates and their fields are a portal-wide dictionary, so every change to them, the creation included, is
    /// for the DocSpace admins only. A room admin manages rooms, not the vocabulary the whole portal files by.
    /// </summary>
    private async Task DemandTemplateManagementAsync()
    {
        var userType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);

        if (userType != EmployeeType.DocSpaceAdmin)
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }
    }

    private async Task CheckTemplateNameIsFreeAsync(IMetadataDao<int> metadataDao, string name, int exceptTemplateId)
    {
        // the system template is not visible, so its name is not taken from the users' point of view
        var exists = await metadataDao.GetTemplatesAsync(includeSystem: false)
            .AnyAsync(t => t.Id != exceptTemplateId && t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            throw new ArgumentException($@"Template with name '{name}' already exists", nameof(name));
        }
    }

    private static List<MetadataValue> FilterValues(List<MetadataValue> values, MetadataTemplate template)
    {
        var fieldIds = template.Fields.Select(f => f.Id).ToHashSet();

        return values.Where(v => fieldIds.Contains(v.FieldId)).ToList();
    }

    private async Task ReindexEntriesAsync(IEnumerable<(int EntryId, FileEntryType EntryType)> entries)
    {
        foreach (var group in entries.GroupBy(e => e.EntryType))
        {
            foreach (var batch in group.Select(e => e.EntryId).Distinct().Chunk(1000))
            {
                await metadataIndexHelper.IndexEntriesAsync(group.Key, batch);
            }
        }
    }

    private async Task NotifyUpdateAsync(FileEntry<int> entry)
    {
        if (entry is File<int> file)
        {
            await socketManager.UpdateFileAsync(file);
        }
        else if (entry is Folder<int> folder)
        {
            await socketManager.UpdateFolderAsync(folder);
        }
    }
}
