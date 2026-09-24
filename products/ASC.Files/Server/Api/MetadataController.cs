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

namespace ASC.Files.Api;

public class MetadataController(
    MetadataService metadataService,
    MetadataDtoHelper metadataDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <summary>
    /// Returns the list of metadata templates.
    /// </summary>
    /// <path>api/2.0/files/metadata/templates</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "List of metadata templates", typeof(IAsyncEnumerable<MetadataTemplateDto>))]
    [HttpGet("metadata/templates")]
    public async IAsyncEnumerable<MetadataTemplateDto> GetTemplates(GetMetadataTemplatesRequestDto inDto)
    {
        await foreach (var template in metadataService.GetTemplatesAsync(inDto.Visible, withFields: true))
        {
            yield return metadataDtoHelper.Get(template);
        }
    }

    /// <summary>
    /// Creates a metadata template.
    /// </summary>
    /// <path>api/2.0/files/metadata/templates</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "New metadata template", typeof(MetadataTemplateDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(400, "Invalid template or a template with this name already exists")]
    [HttpPost("metadata/templates")]
    public async Task<MetadataTemplateDto> CreateTemplate(CreateMetadataTemplateRequestDto inDto)
    {
        var fields = inDto.Fields?.Select(ToField);

        var template = await metadataService.CreateTemplateAsync(inDto.Name, inDto.Visible, fields);

        return metadataDtoHelper.Get(template);
    }

    /// <summary>
    /// Returns a metadata template by its ID.
    /// </summary>
    /// <path>api/2.0/files/metadata/templates/{templateId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Metadata template", typeof(MetadataTemplateDto))]
    [SwaggerResponse(404, "Template not found")]
    [HttpGet("metadata/templates/{templateId:int}")]
    public async Task<MetadataTemplateDto> GetTemplate(MetadataTemplateIdRequestDto inDto)
    {
        var template = await metadataService.GetTemplateAsync(inDto.TemplateId);

        return metadataDtoHelper.Get(template);
    }

    /// <summary>
    /// Updates a metadata template.
    /// </summary>
    /// <path>api/2.0/files/metadata/templates/{templateId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Updated metadata template", typeof(MetadataTemplateDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Template not found")]
    [SwaggerResponse(400, "A template with this name already exists")]
    [HttpPut("metadata/templates/{templateId:int}")]
    public async Task<MetadataTemplateDto> UpdateTemplate(UpdateMetadataTemplateRequestDto inDto)
    {
        var template = await metadataService.UpdateTemplateAsync(inDto.TemplateId, inDto.Update.Name, inDto.Update.Visible);

        return metadataDtoHelper.Get(template);
    }

    /// <summary>
    /// Deletes a metadata template.
    /// </summary>
    /// <path>api/2.0/files/metadata/templates/{templateId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "OK")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Template not found")]
    [HttpDelete("metadata/templates/{templateId:int}")]
    public async Task DeleteTemplate(MetadataTemplateIdRequestDto inDto)
    {
        await metadataService.DeleteTemplateAsync(inDto.TemplateId);
    }

    /// <summary>
    /// Creates a metadata field in the template.
    /// </summary>
    /// <path>api/2.0/files/metadata/templates/{templateId}/fields</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "New metadata field", typeof(MetadataFieldDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Template not found")]
    [SwaggerResponse(400, "Invalid field: an empty name, options on a non-choice field or a choice field without options")]
    [HttpPost("metadata/templates/{templateId:int}/fields")]
    public async Task<MetadataFieldDto> CreateField(CreateMetadataFieldRequestDto inDto)
    {
        var field = await metadataService.CreateFieldAsync(inDto.TemplateId, ToField(inDto.Field));

        return metadataDtoHelper.Get(field);
    }

    /// <summary>
    /// Updates a metadata field.
    /// </summary>
    /// <path>api/2.0/files/metadata/templates/{templateId}/fields/{fieldId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Updated metadata field", typeof(MetadataFieldDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Field not found")]
    [SwaggerResponse(400, "Invalid field, a type change on a field with values or the removal of an option in use")]
    [HttpPut("metadata/templates/{templateId:int}/fields/{fieldId:int}")]
    public async Task<MetadataFieldDto> UpdateField(UpdateMetadataFieldRequestDto inDto)
    {
        var field = await metadataService.UpdateFieldAsync(inDto.TemplateId, inDto.FieldId, ToFieldUpdate(inDto.Field));

        return metadataDtoHelper.Get(field);
    }

    /// <summary>
    /// Deletes a metadata field.
    /// </summary>
    /// <path>api/2.0/files/metadata/templates/{templateId}/fields/{fieldId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "OK")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Field not found")]
    [HttpDelete("metadata/templates/{templateId:int}/fields/{fieldId:int}")]
    public async Task DeleteField(DeleteMetadataFieldRequestDto inDto)
    {
        await metadataService.DeleteFieldAsync(inDto.TemplateId, inDto.FieldId);
    }

    /// <summary>
    /// Returns the metadata of the file: the assigned templates with their values and the custom fields.
    /// </summary>
    /// <path>api/2.0/files/metadata/file/{fileId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "File metadata", typeof(EntryMetadataDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "File not found")]
    [HttpGet("metadata/file/{fileId:int}")]
    public async Task<EntryMetadataDto> GetFileMetadata(FileIdRequestDto<int> inDto)
    {
        var metadata = await metadataService.GetEntryMetadataAsync(inDto.FileId, FileEntryType.File);

        return metadataDtoHelper.Get(metadata);
    }

    /// <summary>
    /// Returns the metadata of the folder: the assigned templates with their values and the custom fields.
    /// </summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Folder metadata", typeof(EntryMetadataDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Folder not found")]
    [HttpGet("metadata/folder/{folderId:int}")]
    public async Task<EntryMetadataDto> GetFolderMetadata(FolderIdRequestDto<int> inDto)
    {
        var metadata = await metadataService.GetEntryMetadataAsync(inDto.FolderId, FileEntryType.Folder);

        return metadataDtoHelper.Get(metadata);
    }

    /// <summary>
    /// Assigns metadata templates to the file.
    /// </summary>
    /// <path>api/2.0/files/metadata/file/{fileId}/templates</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "OK")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "File not found")]
    [HttpPut("metadata/file/{fileId:int}/templates")]
    public async Task AssignFileTemplates(AssignFileMetadataTemplatesRequestDto<int> inDto)
    {
        await metadataService.AssignTemplatesAsync(inDto.FileId, FileEntryType.File, inDto.Assign.TemplateIds);
    }

    /// <summary>
    /// Assigns metadata templates to the folder, optionally propagating them to the sub-entries.
    /// </summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}/templates</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Cascade operation status; a completed operation without an ID when no cascade is requested", typeof(MetadataOperationDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Folder not found")]
    [HttpPut("metadata/folder/{folderId:int}/templates")]
    public async Task<MetadataOperationDto> AssignFolderTemplates(AssignFolderMetadataTemplatesRequestDto<int> inDto)
    {
        var taskId = await metadataService.AssignTemplatesToFolderAsync(inDto.FolderId, inDto.Assign.TemplateIds, inDto.Assign.Cascade, inDto.Assign.ConflictResolveType);

        // without a cascade the assignment is finished in this call, which the answer states as a completed operation instead of a null body
        return metadataDtoHelper.Get(taskId == null ? null : await metadataService.GetCascadeStatusAsync(inDto.FolderId));
    }

    /// <summary>
    /// Returns the cascade metadata assignment status of the folder.
    /// </summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}/templates/progress</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Cascade operation status; a completed operation without an ID when the folder has no cascade to report", typeof(MetadataOperationDto))]
    [SwaggerResponse(404, "Folder not found")]
    [HttpGet("metadata/folder/{folderId:int}/templates/progress")]
    public async Task<MetadataOperationDto> GetCascadeProgress(FolderIdRequestDto<int> inDto)
    {
        return metadataDtoHelper.Get(await metadataService.GetCascadeStatusAsync(inDto.FolderId));
    }

    /// <summary>
    /// Unassigns the metadata template from the file.
    /// </summary>
    /// <path>api/2.0/files/metadata/file/{fileId}/templates/{templateId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "OK")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "File not found")]
    [HttpDelete("metadata/file/{fileId:int}/templates/{templateId:int}")]
    public async Task UnassignFileTemplate(UnassignFileMetadataTemplateRequestDto<int> inDto)
    {
        await metadataService.UnassignTemplateAsync(inDto.FileId, FileEntryType.File, inDto.TemplateId);
    }

    /// <summary>
    /// Unassigns the metadata template from the folder; the sub-entries keep it as a direct assignment when it was cascaded.
    /// </summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}/templates/{templateId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "OK")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Folder not found")]
    [HttpDelete("metadata/folder/{folderId:int}/templates/{templateId:int}")]
    public async Task UnassignFolderTemplate(UnassignFolderMetadataTemplateRequestDto<int> inDto)
    {
        await metadataService.UnassignTemplateFromFolderAsync(inDto.FolderId, inDto.TemplateId);
    }

    /// <summary>
    /// Sets the metadata field values on the file and returns its metadata: the assigned templates with the values of their fields, and the custom fields.
    /// </summary>
    /// <path>api/2.0/files/metadata/file/{fileId}/values</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The metadata of the file after the write: the assigned templates with the values of their fields, and the custom fields", typeof(EntryMetadataDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "File not found")]
    [SwaggerResponse(400, "A value does not match the field type, or the field belongs to a template the file does not have")]
    [HttpPut("metadata/file/{fileId:int}/values")]
    public async Task<EntryMetadataDto> SetFileValues(SetFileMetadataValuesRequestDto<int> inDto)
    {
        var metadata = await metadataService.SetValuesAsync(inDto.FileId, FileEntryType.File, inDto.Set.Values.Select(ToValue));

        return metadataDtoHelper.Get(metadata);
    }

    /// <summary>
    /// Sets the metadata field values on the folder and returns its metadata: the assigned templates with the values of their fields, and the custom fields.
    /// </summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}/values</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The metadata of the folder after the write: the assigned templates with the values of their fields, and the custom fields", typeof(EntryMetadataDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Folder not found")]
    [SwaggerResponse(400, "A value does not match the field type, or the field belongs to a template the folder does not have")]
    [HttpPut("metadata/folder/{folderId:int}/values")]
    public async Task<EntryMetadataDto> SetFolderValues(SetFolderMetadataValuesRequestDto<int> inDto)
    {
        var metadata = await metadataService.SetValuesAsync(inDto.FolderId, FileEntryType.Folder, inDto.Set.Values.Select(ToValue));

        return metadataDtoHelper.Get(metadata);
    }

    /// <summary>
    /// Sets the custom text fields of the file by name: a listed field gets the value, a null or empty value removes the field, the fields not listed are left alone.
    /// </summary>
    /// <path>api/2.0/files/metadata/file/{fileId}/customFields</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The custom fields of the file with their values", typeof(List<CustomFieldValueDto>))]
    [SwaggerResponse(400, "Invalid custom fields or too many of them")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "File not found")]
    [HttpPut("metadata/file/{fileId:int}/customFields")]
    public async Task<List<CustomFieldValueDto>> SetFileCustomFields(SetFileCustomFieldsRequestDto<int> inDto)
    {
        var fields = await metadataService.SetCustomFieldsAsync(inDto.FileId, FileEntryType.File, ToCustomFieldUpdates(inDto.Set));

        return fields.Select(MetadataDtoHelper.Get).ToList();
    }

    /// <summary>
    /// Sets the custom text fields of the folder by name: a listed field gets the value, a null or empty value removes the field, the fields not listed are left alone.
    /// </summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}/customFields</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The custom fields of the folder with their values", typeof(List<CustomFieldValueDto>))]
    [SwaggerResponse(400, "Invalid custom fields or too many of them")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Folder not found")]
    [HttpPut("metadata/folder/{folderId:int}/customFields")]
    public async Task<List<CustomFieldValueDto>> SetFolderCustomFields(SetFolderCustomFieldsRequestDto<int> inDto)
    {
        var fields = await metadataService.SetCustomFieldsAsync(inDto.FolderId, FileEntryType.Folder, ToCustomFieldUpdates(inDto.Set));

        return fields.Select(MetadataDtoHelper.Get).ToList();
    }

    private static List<CustomFieldUpdate> ToCustomFieldUpdates(SetCustomFields set)
    {
        return set.Fields.Select(f => new CustomFieldUpdate(f.Name, f.Value)).ToList();
    }

    private static MetadataField ToField(MetadataFieldRequest request)
    {
        return new MetadataField
        {
            Name = request.Name,
            Type = request.Type,
            Options = request.Options?.Select(o => new MetadataFieldOption(o.Id ?? Guid.Empty, o.Value)).ToList(),
            Order = request.Order ?? 0
        };
    }

    private static MetadataFieldUpdate ToFieldUpdate(UpdateMetadataFieldRequest request)
    {
        return new MetadataFieldUpdate
        {
            Name = request.Name,
            Type = request.Type,
            Options = request.Options?.Select(o => new MetadataFieldOption(o.Id ?? Guid.Empty, o.Value)).ToList(),
            Order = request.Order
        };
    }

    private static MetadataValue ToValue(MetadataValueRequest request)
    {
        return new MetadataValue
        {
            FieldId = request.FieldId,
            StringValue = request.StringValue,
            NumberValue = request.NumberValue,
            DateValue = request.DateValue,
            OptionIds = request.OptionIds
        };
    }
}
