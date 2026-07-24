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

    public async Task<MetadataTemplate> CreateTemplateAsync(string name, bool visible, IEnumerable<MetadataField> fields = null)
    {
        await DemandTemplateManagementAsync(create: true);

        ArgumentException.ThrowIfNullOrEmpty(name);

        var metadataDao = daoFactory.GetMetadataDao<int>();

        await CheckTemplateNameIsFreeAsync(metadataDao, name, 0);

        var template = await metadataDao.SaveTemplateAsync(new MetadataTemplate { Name = name, Visible = visible });

        if (fields != null)
        {
            foreach (var field in fields)
            {
                field.TemplateId = template.Id;
                template.Fields.Add(await CreateFieldInternalAsync(metadataDao, template, field));
            }
        }

        filesMessageService.Send(MessageAction.MetadataTemplateCreated, template.Name);

        return template;
    }

    public async Task<MetadataTemplate> UpdateTemplateAsync(int templateId, string name, bool? visible)
    {
        await DemandTemplateManagementAsync(create: false);

        var metadataDao = daoFactory.GetMetadataDao<int>();

        var template = await metadataDao.GetTemplateAsync(templateId, withFields: false) ?? throw new ItemNotFoundException();

        if (template.IsSystem)
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        if (!string.IsNullOrEmpty(name) && !name.Equals(template.Name, StringComparison.OrdinalIgnoreCase))
        {
            await CheckTemplateNameIsFreeAsync(metadataDao, name, templateId);
        }

        template.Name = string.IsNullOrEmpty(name) ? template.Name : name;
        template.Visible = visible ?? template.Visible;

        var saved = await metadataDao.SaveTemplateAsync(template);

        filesMessageService.Send(MessageAction.MetadataTemplateUpdated, saved.Name);

        return saved;
    }

    public async Task DeleteTemplateAsync(int templateId)
    {
        await DemandTemplateManagementAsync(create: false);

        var metadataDao = daoFactory.GetMetadataDao<int>();

        var template = await metadataDao.GetTemplateAsync(templateId, withFields: false) ?? throw new ItemNotFoundException();

        if (template.IsSystem)
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var affectedLinks = await metadataDao.GetLinksByTemplateAsync(templateId);

        await metadataDao.DeleteTemplateAsync(templateId);

        filesMessageService.Send(MessageAction.MetadataTemplateDeleted, template.Name);

        await ReindexEntriesAsync(affectedLinks.Select(l => ((int)l.EntryId, l.EntryType)));
    }

    public async Task<MetadataTemplate> GetTemplateAsync(int templateId)
    {
        return await daoFactory.GetMetadataDao<int>().GetTemplateAsync(templateId) ?? throw new ItemNotFoundException();
    }

    public IAsyncEnumerable<MetadataTemplate> GetTemplatesAsync(bool? visible = null, bool withFields = false)
    {
        return daoFactory.GetMetadataDao<int>().GetTemplatesAsync(visible, includeSystem: true, withFields);
    }

    public async Task<MetadataTemplate> GetOrCreateSystemTemplateAsync()
    {
        var metadataDao = daoFactory.GetMetadataDao<int>();

        var template = await metadataDao.GetSystemTemplateAsync();
        if (template != null)
        {
            return template;
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        await using (await distributedLockProvider.TryAcquireFairLockAsync($"metadata_system_template_{tenantId}"))
        {
            template = await metadataDao.GetSystemTemplateAsync();
            if (template != null)
            {
                return template;
            }

            return await metadataDao.SaveTemplateAsync(new MetadataTemplate
            {
                Name = SystemTemplateName,
                Visible = true,
                IsSystem = true
            });
        }
    }

    public async Task<MetadataField> CreateFieldAsync(int templateId, MetadataField field)
    {
        await DemandTemplateManagementAsync(create: false);

        var metadataDao = daoFactory.GetMetadataDao<int>();

        var template = await metadataDao.GetTemplateAsync(templateId, withFields: false) ?? throw new ItemNotFoundException();

        field.TemplateId = templateId;

        var saved = await CreateFieldInternalAsync(metadataDao, template, field);

        filesMessageService.Send(MessageAction.MetadataFieldCreated, saved.Name);

        return saved;
    }

    public async Task<MetadataField> UpdateFieldAsync(int fieldId, MetadataField update)
    {
        await DemandTemplateManagementAsync(create: false);

        var metadataDao = daoFactory.GetMetadataDao<int>();

        var field = await metadataDao.GetFieldAsync(fieldId) ?? throw new ItemNotFoundException();
        var template = await metadataDao.GetTemplateAsync(field.TemplateId, withFields: false) ?? throw new ItemNotFoundException();

        if (!string.IsNullOrEmpty(update.Name))
        {
            field.Name = update.Name;
        }

        if (update.Type != field.Type)
        {
            if (template.IsSystem && update.Type != MetadataFieldType.String)
            {
                throw new ArgumentException(@"The system template supports only string fields", nameof(update));
            }

            if (await metadataDao.HasValuesAsync(fieldId))
            {
                throw new ArgumentException(@"The field type cannot be changed because values exist", nameof(update));
            }

            field.Type = update.Type;
        }

        if (update.Options != null)
        {
            var removedOptionIds = (field.Options ?? [])
                .Select(o => o.Id)
                .Except(update.Options.Select(o => o.Id))
                .ToList();

            if (removedOptionIds.Count > 0 && await metadataDao.HasValuesAsync(fieldId))
            {
                throw new ArgumentException(@"An option in use cannot be removed", nameof(update));
            }

            field.Options = update.Options;
        }

        field.Order = update.Order;

        ValidateField(field);

        var saved = await metadataDao.SaveFieldAsync(field);

        filesMessageService.Send(MessageAction.MetadataFieldUpdated, saved.Name);

        return saved;
    }

    public async Task DeleteFieldAsync(int fieldId)
    {
        await DemandTemplateManagementAsync(create: false);

        var metadataDao = daoFactory.GetMetadataDao<int>();

        var field = await metadataDao.GetFieldAsync(fieldId) ?? throw new ItemNotFoundException();

        var affectedValues = await metadataDao.GetValueEntriesAsync(fieldId);

        await metadataDao.DeleteFieldAsync(fieldId);

        filesMessageService.Send(MessageAction.MetadataFieldDeleted, field.Name);

        await ReindexEntriesAsync(affectedValues.Select(v => ((int)v.EntryId, v.EntryType)));
    }

    public async Task<List<EntryMetadata>> GetEntryMetadataAsync(int entryId, FileEntryType entryType)
    {
        await DemandEntryAccessAsync(entryId, entryType, edit: false);

        var metadataDao = daoFactory.GetMetadataDao<int>();

        var templateIds = await metadataDao.GetLinksAsync(entryId, entryType)
            .Select(l => l.TemplateId)
            .ToListAsync();

        var values = await metadataDao.GetValuesAsync(entryId, entryType).ToListAsync();

        var result = new List<EntryMetadata>();

        var systemTemplate = await metadataDao.GetSystemTemplateAsync();
        if (systemTemplate != null)
        {
            templateIds.Remove(systemTemplate.Id);

            result.Add(new EntryMetadata
            {
                Template = systemTemplate,
                Values = FilterValues(values, systemTemplate)
            });
        }

        foreach (var templateId in templateIds)
        {
            var template = await metadataDao.GetTemplateAsync(templateId);
            if (template == null)
            {
                continue;
            }

            result.Add(new EntryMetadata
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
            _ = await metadataDao.GetTemplateAsync(templateId, withFields: false) ?? throw new ItemNotFoundException();

            links.Add(new MetadataTemplateLink
            {
                TemplateId = templateId,
                EntryId = entryId,
                EntryType = entryType
            });
        }

        await metadataDao.SaveLinksAsync(links);

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
            _ = await metadataDao.GetTemplateAsync(templateId, withFields: false) ?? throw new ItemNotFoundException();
        }

        await metadataDao.SaveLinksAsync(templateIdsList.Select(templateId => new MetadataTemplateLink
        {
            TemplateId = templateId,
            EntryId = folderId,
            EntryType = FileEntryType.Folder,
            Cascade = cascade
        }));

        await filesMessageService.SendAsync(MessageAction.MetadataTemplateAssigned, entry, entry.Title);

        await NotifyUpdateAsync(entry);

        if (!cascade)
        {
            return null;
        }

        await filesMessageService.SendAsync(MessageAction.MetadataCascadeStarted, entry, entry.Title);

        var tenantId = tenantManager.GetCurrentTenantId();

        return await cascadeWorker.StartAsync(tenantId, authContext.CurrentAccount.ID, folderId, templateIdsList, conflict, unassign: false);
    }

    public async Task<string> UnassignTemplateFromFolderAsync(int folderId, int templateId, bool deleteValues = true)
    {
        var entry = await DemandEntryAccessAsync(folderId, FileEntryType.Folder, edit: true);

        var metadataDao = daoFactory.GetMetadataDao<int>();

        var link = await metadataDao.GetLinksAsync(folderId, FileEntryType.Folder)
            .FirstOrDefaultAsync(l => l.TemplateId == templateId);

        await UnassignTemplateAsync(folderId, FileEntryType.Folder, templateId, deleteValues);

        await NotifyUpdateAsync(entry);

        if (link is not { Cascade: true })
        {
            return null;
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        return await cascadeWorker.StartAsync(tenantId, authContext.CurrentAccount.ID, folderId, [templateId], MetadataConflictResolveType.Skip, unassign: true);
    }

    public async Task<MetadataCascadeOperation> GetCascadeStatusAsync(int folderId)
    {
        await DemandEntryAccessAsync(folderId, FileEntryType.Folder, edit: false);

        return await cascadeWorker.GetStatusAsync(tenantManager.GetCurrentTenantId(), folderId);
    }

    public async Task UnassignTemplateAsync(int entryId, FileEntryType entryType, int templateId, bool deleteValues = true)
    {
        var entry = await DemandEntryAccessAsync(entryId, entryType, edit: true);

        var metadataDao = daoFactory.GetMetadataDao<int>();

        await metadataDao.DeleteLinksAsync(entryId, entryType, templateId);

        if (deleteValues)
        {
            var fieldIds = await metadataDao.GetFieldsAsync(templateId).Select(f => f.Id).ToListAsync();
            if (fieldIds.Count > 0)
            {
                await metadataDao.DeleteValuesAsync(entryId, entryType, fieldIds);
            }
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
        if (systemTemplate != null)
        {
            assignedTemplateIds.Add(systemTemplate.Id);
        }

        foreach (var value in valuesList)
        {
            if (!fields.TryGetValue(value.FieldId, out var field))
            {
                throw new ItemNotFoundException();
            }

            if (!assignedTemplateIds.Contains(field.TemplateId))
            {
                throw new ArgumentException(@"The field does not belong to a template assigned to the entry", nameof(values));
            }

            ValidateValue(field, value);
        }

        await metadataDao.SetValuesAsync(entryId, entryType, valuesList);

        await filesMessageService.SendAsync(MessageAction.MetadataValuesUpdated, entry, entry.Title);

        await NotifyUpdateAsync(entry);

        await metadataIndexHelper.IndexEntriesAsync(entryType, [entryId]);

        return await metadataDao.GetValuesAsync(entryId, entryType, valuesList.Select(v => v.FieldId)).ToListAsync();
    }

    public async Task<MetadataValue> AddCustomFieldAsync(int entryId, FileEntryType entryType, string name, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var entry = await DemandEntryAccessAsync(entryId, entryType, edit: true);

        var metadataDao = daoFactory.GetMetadataDao<int>();

        var systemTemplate = await GetOrCreateSystemTemplateAsync();

        var field = systemTemplate.Fields.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (field == null)
        {
            field = await metadataDao.SaveFieldAsync(new MetadataField
            {
                TemplateId = systemTemplate.Id,
                Name = name,
                Type = MetadataFieldType.String,
                Order = systemTemplate.Fields.Count
            });
        }

        var metadataValue = new MetadataValue { FieldId = field.Id, StringValue = value };

        await metadataDao.SetValuesAsync(entryId, entryType, [metadataValue]);

        await filesMessageService.SendAsync(MessageAction.MetadataValuesUpdated, entry, entry.Title);

        await NotifyUpdateAsync(entry);

        await metadataIndexHelper.IndexEntriesAsync(entryType, [entryId]);

        return (await metadataDao.GetValuesAsync(entryId, entryType, [field.Id]).ToListAsync()).FirstOrDefault();
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

                value.DateValue = value.DateValue.Value.Kind == DateTimeKind.Utc
                    ? value.DateValue
                    : value.DateValue.Value.ToUniversalTime();

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

                if (field.Type == MetadataFieldType.SingleChoice && value.OptionIds.Count > 1)
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

    private async Task<MetadataField> CreateFieldInternalAsync(IMetadataDao<int> metadataDao, MetadataTemplate template, MetadataField field)
    {
        if (template.IsSystem && field.Type != MetadataFieldType.String)
        {
            throw new ArgumentException(@"The system template supports only string fields", nameof(field));
        }

        field.Options = field.Options?.Select(o => o.Id == Guid.Empty ? o with { Id = Guid.NewGuid() } : o).ToList();

        ValidateField(field);

        return await metadataDao.SaveFieldAsync(field);
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
        }
        else if (field.Options is { Count: > 0 })
        {
            throw new ArgumentException(@"Options are supported by choice fields only", nameof(field));
        }
    }

    private async Task DemandTemplateManagementAsync(bool create)
    {
        var userType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);

        var allowed = create
            ? userType is EmployeeType.RoomAdmin or EmployeeType.DocSpaceAdmin
            : userType is EmployeeType.DocSpaceAdmin;

        if (!allowed)
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }
    }

    private async Task CheckTemplateNameIsFreeAsync(IMetadataDao<int> metadataDao, string name, int exceptTemplateId)
    {
        var exists = await metadataDao.GetTemplatesAsync()
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
