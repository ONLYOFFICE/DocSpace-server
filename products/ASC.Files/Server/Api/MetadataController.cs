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
    [HttpPost("metadata/templates")]
    public async Task<MetadataTemplateDto> CreateTemplate(CreateMetadataTemplateRequestDto inDto)
    {
        var fields = inDto.Fields?.Select(ToField);

        var template = await metadataService.CreateTemplateAsync(inDto.Name, inDto.Visible, fields);

        return metadataDtoHelper.Get(template);
    }

    /// <summary>
    /// Returns the system metadata template with the global visibility.
    /// </summary>
    /// <path>api/2.0/files/metadata/templates/system</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "System metadata template", typeof(MetadataTemplateDto))]
    [HttpGet("metadata/templates/system")]
    public async Task<MetadataTemplateDto> GetSystemTemplate()
    {
        var template = await metadataService.GetOrCreateSystemTemplateAsync();

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
    [HttpPut("metadata/templates/{templateId:int}/fields/{fieldId:int}")]
    public async Task<MetadataFieldDto> UpdateField(UpdateMetadataFieldRequestDto inDto)
    {
        var field = await metadataService.UpdateFieldAsync(inDto.FieldId, ToField(inDto.Field));

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
        await metadataService.DeleteFieldAsync(inDto.FieldId);
    }

    /// <summary>
    /// Returns the metadata of the file.
    /// </summary>
    /// <path>api/2.0/files/metadata/file/{fileId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "File metadata", typeof(List<EntryMetadataDto>))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "File not found")]
    [HttpGet("metadata/file/{fileId:int}")]
    public async Task<List<EntryMetadataDto>> GetFileMetadata(FileIdRequestDto<int> inDto)
    {
        var metadata = await metadataService.GetEntryMetadataAsync(inDto.FileId, FileEntryType.File);

        return metadata.Select(metadataDtoHelper.Get).ToList();
    }

    /// <summary>
    /// Returns the metadata of the folder.
    /// </summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Folder metadata", typeof(List<EntryMetadataDto>))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Folder not found")]
    [HttpGet("metadata/folder/{folderId:int}")]
    public async Task<List<EntryMetadataDto>> GetFolderMetadata(FolderIdRequestDto<int> inDto)
    {
        var metadata = await metadataService.GetEntryMetadataAsync(inDto.FolderId, FileEntryType.Folder);

        return metadata.Select(metadataDtoHelper.Get).ToList();
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
    [SwaggerResponse(200, "Cascade operation status or null when no cascade is requested", typeof(MetadataOperationDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Folder not found")]
    [HttpPut("metadata/folder/{folderId:int}/templates")]
    public async Task<MetadataOperationDto> AssignFolderTemplates(AssignFolderMetadataTemplatesRequestDto<int> inDto)
    {
        var taskId = await metadataService.AssignTemplatesToFolderAsync(inDto.FolderId, inDto.Assign.TemplateIds, inDto.Assign.Cascade, inDto.Assign.ConflictResolveType);

        return taskId == null ? null : metadataDtoHelper.Get(await metadataService.GetCascadeStatusAsync(inDto.FolderId));
    }

    /// <summary>
    /// Returns the cascade metadata assignment status of the folder.
    /// </summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}/templates/progress</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Cascade operation status", typeof(MetadataOperationDto))]
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
    /// Unassigns the metadata template from the folder, removing it from the sub-entries when it was cascaded.
    /// </summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}/templates/{templateId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Cascade operation status or null when no cascade cleanup is required", typeof(MetadataOperationDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Folder not found")]
    [HttpDelete("metadata/folder/{folderId:int}/templates/{templateId:int}")]
    public async Task<MetadataOperationDto> UnassignFolderTemplate(UnassignFolderMetadataTemplateRequestDto<int> inDto)
    {
        var taskId = await metadataService.UnassignTemplateFromFolderAsync(inDto.FolderId, inDto.TemplateId);

        return taskId == null ? null : metadataDtoHelper.Get(await metadataService.GetCascadeStatusAsync(inDto.FolderId));
    }

    /// <summary>
    /// Sets the metadata field values on the file.
    /// </summary>
    /// <path>api/2.0/files/metadata/file/{fileId}/values</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Updated metadata values", typeof(List<MetadataValueDto>))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "File not found")]
    [HttpPut("metadata/file/{fileId:int}/values")]
    public async Task<List<MetadataValueDto>> SetFileValues(SetFileMetadataValuesRequestDto<int> inDto)
    {
        var values = await metadataService.SetValuesAsync(inDto.FileId, FileEntryType.File, inDto.Set.Values.Select(ToValue));

        return values.Select(metadataDtoHelper.Get).ToList();
    }

    /// <summary>
    /// Sets the metadata field values on the folder.
    /// </summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}/values</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Updated metadata values", typeof(List<MetadataValueDto>))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Folder not found")]
    [HttpPut("metadata/folder/{folderId:int}/values")]
    public async Task<List<MetadataValueDto>> SetFolderValues(SetFolderMetadataValuesRequestDto<int> inDto)
    {
        var values = await metadataService.SetValuesAsync(inDto.FolderId, FileEntryType.Folder, inDto.Set.Values.Select(ToValue));

        return values.Select(metadataDtoHelper.Get).ToList();
    }

    /// <summary>
    /// Adds a custom text field from the system template to the file and sets its value.
    /// </summary>
    /// <path>api/2.0/files/metadata/file/{fileId}/customfield</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Metadata value of the custom field", typeof(MetadataValueDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "File not found")]
    [HttpPost("metadata/file/{fileId:int}/customfield")]
    public async Task<MetadataValueDto> AddFileCustomField(AddFileCustomFieldRequestDto<int> inDto)
    {
        var value = await metadataService.AddCustomFieldAsync(inDto.FileId, FileEntryType.File, inDto.Field.Name, inDto.Field.Value);

        return value == null ? null : metadataDtoHelper.Get(value);
    }

    /// <summary>
    /// Adds a custom text field from the system template to the folder and sets its value.
    /// </summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}/customfield</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Metadata value of the custom field", typeof(MetadataValueDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Folder not found")]
    [HttpPost("metadata/folder/{folderId:int}/customfield")]
    public async Task<MetadataValueDto> AddFolderCustomField(AddFolderCustomFieldRequestDto<int> inDto)
    {
        var value = await metadataService.AddCustomFieldAsync(inDto.FolderId, FileEntryType.Folder, inDto.Field.Name, inDto.Field.Value);

        return value == null ? null : metadataDtoHelper.Get(value);
    }

    private static MetadataField ToField(MetadataFieldRequest request)
    {
        return new MetadataField
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
