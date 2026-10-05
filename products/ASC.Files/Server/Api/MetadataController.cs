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
    MetadataDtoMapper metadataDtoMapper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Lists the metadata templates of the portal with their fields, the dictionary a file, a folder or a room is described
    /// with. Any member of the portal can read it, the list is the same for everyone. The call is read-only. The templates
    /// come back ordered by their creation, each with its fields in their display order and the choice options of the choice
    /// fields; the `visible` parameter narrows the list to the templates shown in the pickers or to the hidden ones, without
    /// it both are returned. An empty list means the portal has no templates yet. The custom text fields set on the entries
    /// are not templates and are not listed here: read them on the entry with `GET api/2.0/files/metadata/file/{fileId}`.
    /// </remarks>
    /// <summary>Get metadata templates</summary>
    /// <path>api/2.0/files/metadata/templates</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "List of metadata templates", typeof(IAsyncEnumerable<MetadataTemplateDto>))]
    [HttpGet("metadata/templates")]
    public async IAsyncEnumerable<MetadataTemplateDto> GetTemplates(GetMetadataTemplatesRequestDto inDto)
    {
        await foreach (var template in metadataService.GetTemplatesAsync(inDto.Visible, withFields: true))
        {
            yield return metadataDtoMapper.Map(template);
        }
    }

    /// <remarks>
    /// Creates a metadata template for the whole portal, optionally with its fields in one call. Only a DocSpace admin can
    /// create templates. The template name must be unique on the portal regardless of case, at most 255 characters, and the
    /// name `System` is reserved. Every field needs a name unique within the template and a type from the published set; a
    /// choice field requires at least one option and the options must be unique, a field of another type takes no options.
    /// A field without `order` is placed after the fields that have one, in the order of the request. The template and
    /// its fields are stored together: an invalid field rejects the whole request and nothing is created.
    /// The answer is the created template with its fields and the generated option identifiers, which the values written
    /// with `PUT api/2.0/files/metadata/file/{fileId}/values` refer to. A name already in use or an invalid field is
    /// answered with 400; the request of a member who is not a DocSpace admin with 403.
    /// </remarks>
    /// <summary>Create a metadata template</summary>
    /// <path>api/2.0/files/metadata/templates</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "New metadata template", typeof(MetadataTemplateDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace admin")]
    [SwaggerResponse(400, "An invalid template or field: a name in use, reserved or too long, an unknown field type, duplicate field names or wrong options")]
    [HttpPost("metadata/templates")]
    public async Task<MetadataTemplateDto> CreateTemplate(CreateMetadataTemplateRequestDto inDto)
    {
        var fields = inDto.Fields?.Select(ToField);

        var template = await metadataService.CreateTemplateAsync(inDto.Name, inDto.Visible, fields);

        return metadataDtoMapper.Map(template);
    }

    /// <remarks>
    /// Returns one metadata template with its fields, in their display order, and the options of its choice fields. Any
    /// member of the portal can read a template, the call is read-only. Use it to resolve the template identifiers a file or
    /// a folder reports in `assignedMetadataTemplates` into names and fields. A template that does not exist is answered
    /// with 404.
    /// </remarks>
    /// <summary>Get a metadata template</summary>
    /// <path>api/2.0/files/metadata/templates/{templateId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Metadata template", typeof(MetadataTemplateDto))]
    [SwaggerResponse(404, "Template not found")]
    [HttpGet("metadata/templates/{templateId:int}")]
    public async Task<MetadataTemplateDto> GetTemplate(MetadataTemplateIdRequestDto inDto)
    {
        var template = await metadataService.GetTemplateAsync(inDto.TemplateId);

        return metadataDtoMapper.Map(template);
    }

    /// <remarks>
    /// Renames a metadata template or changes whether it is shown in the pickers. Only a DocSpace admin can change
    /// templates. The request is partial: a property left out keeps its value, the fields are not touched here and are
    /// changed with `PUT api/2.0/files/metadata/templates/{templateId}/fields/{fieldId}`. The new name follows the rules
    /// of the creation: unique on the portal regardless of case and at most 255 characters. The answer is the whole template
    /// with its fields. A template that does not exist is answered with 404, a name already in use or too long with 400.
    /// </remarks>
    /// <summary>Update a metadata template</summary>
    /// <path>api/2.0/files/metadata/templates/{templateId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Updated metadata template", typeof(MetadataTemplateDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Template not found")]
    [SwaggerResponse(400, "A template with this name already exists, or the name is too long")]
    [HttpPut("metadata/templates/{templateId:int}")]
    public async Task<MetadataTemplateDto> UpdateTemplate(UpdateMetadataTemplateRequestDto inDto)
    {
        var template = await metadataService.UpdateTemplateAsync(inDto.TemplateId, inDto.Update.Name, inDto.Update.Visible);

        return metadataDtoMapper.Map(template);
    }

    /// <remarks>
    /// Deletes a metadata template together with its fields, its assignments and every value written for its fields on any
    /// file, folder or room of the portal. Only a DocSpace admin can delete templates. The deletion is irreversible and there
    /// is no confirmation: the affected entries lose the template at once, their search documents are rebuilt and the clients
    /// viewing them are told to refresh. A template that does not exist, or was already deleted, is answered with 404.
    /// To take the template off a single entry and keep it for the others use
    /// `DELETE api/2.0/files/metadata/file/{fileId}/templates/{templateId}` instead.
    /// </remarks>
    /// <summary>Delete a metadata template</summary>
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

    /// <remarks>
    /// Adds a field to an existing metadata template. Only a DocSpace admin can change templates. The field name must be
    /// unique within the template regardless of case and at most 255 characters, the type must be one of the published ones,
    /// a choice field needs at least one option and unique option values, a field of another type takes no options. A
    /// field without `order` is placed after the last field of the template. The entries the template is already
    /// assigned to get the field without a value: nothing is written on them and no cascade runs. The answer is the
    /// created field with its generated option identifiers. A template that does not exist is answered with 404, an
    /// invalid field with 400.
    /// </remarks>
    /// <summary>Add a metadata field</summary>
    /// <path>api/2.0/files/metadata/templates/{templateId}/fields</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "New metadata field", typeof(MetadataFieldDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Template not found")]
    [SwaggerResponse(400, "Invalid field: an empty, repeated or too long name, an unknown type, options on a non-choice field or a choice field without options")]
    [HttpPost("metadata/templates/{templateId:int}/fields")]
    public async Task<MetadataFieldDto> CreateField(CreateMetadataFieldRequestDto inDto)
    {
        var field = await metadataService.CreateFieldAsync(inDto.TemplateId, ToField(inDto.Field));

        return metadataDtoMapper.Map(field);
    }

    /// <remarks>
    /// Changes the name, the type, the options or the display order of a metadata field. Only a DocSpace admin can change
    /// templates. The request is partial: a property left out keeps its value. The type can be changed only while no entry
    /// holds a value for the field, and an option can be removed only while no entry has selected it; a new option is sent
    /// without an identifier and gets one in the answer. A new name must be unique within the template regardless of case.
    /// The values already written are left as they are. The field is addressed through its own template: a field reached
    /// through another template's route is answered with 404, the same as a field that does not exist. A conflicting name,
    /// a type change on a field with values or the removal of an option in use is answered with 400.
    /// </remarks>
    /// <summary>Update a metadata field</summary>
    /// <path>api/2.0/files/metadata/templates/{templateId}/fields/{fieldId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Updated metadata field", typeof(MetadataFieldDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Field not found")]
    [SwaggerResponse(400, "Invalid field, a name another field of the template has, a type change on a field with values or the removal of an option in use")]
    [HttpPut("metadata/templates/{templateId:int}/fields/{fieldId:int}")]
    public async Task<MetadataFieldDto> UpdateField(UpdateMetadataFieldRequestDto inDto)
    {
        var field = await metadataService.UpdateFieldAsync(inDto.TemplateId, inDto.FieldId, ToFieldUpdate(inDto.Field));

        return metadataDtoMapper.Map(field);
    }

    /// <remarks>
    /// Deletes a metadata field from its template together with every value written for it on any file, folder or room of
    /// the portal. Only a DocSpace admin can change templates. The deletion is irreversible: the affected entries lose the
    /// value at once, their search documents are rebuilt and the clients viewing them are told to refresh. The template and
    /// its other fields stay as they are. A field that does not exist, or that belongs to another template than the one in
    /// the route, is answered with 404.
    /// </remarks>
    /// <summary>Delete a metadata field</summary>
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

    /// <remarks>
    /// Returns the metadata of a file: the templates assigned to it, directly or inherited from a cascading folder above
    /// it, each with its fields, and the custom text fields set on the file. The caller needs read access to the file: a
    /// member of the portal, or an anonymous caller through an external link that grants access to the file or to a
    /// folder above it, with the link key in the `Request-Token` header or in the `share` query parameter. The call is
    /// read-only. A field carries its value inside it; a field the file holds no value for comes without a `value`.
    /// The custom fields are name and value pairs and are not part of any template. A file without metadata is answered with
    /// empty lists, not with an error. The same shape is returned by `PUT api/2.0/files/metadata/file/{fileId}/values`
    /// after a write. A request with neither a session nor a link key is answered with 401; a file the caller cannot read
    /// with 403, a file that does not exist with 404.
    /// </remarks>
    /// <summary>Get file metadata</summary>
    /// <path>api/2.0/files/metadata/file/{fileId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "File metadata", typeof(EntryMetadataDto))]
    [SwaggerResponse(401, "The caller has neither a session nor an external link key")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "File not found")]
    [AllowAnonymous]
    [HttpGet("metadata/file/{fileId:int}")]
    public async Task<EntryMetadataDto> GetFileMetadata(FileIdRequestDto<int> inDto)
    {
        var metadata = await metadataService.GetEntryMetadataAsync(inDto.FileId, FileEntryType.File);

        return metadataDtoMapper.Map(metadata);
    }

    /// <remarks>
    /// Returns the metadata of a folder or a room: the templates assigned to it, directly or inherited from a cascading
    /// folder above it, each with its fields, and the custom text fields set on it. The caller needs read access to the
    /// folder: a member of the portal, or an anonymous caller through an external link that grants access to the folder
    /// or to a folder above it, with the link key in the `Request-Token` header or in the `share` query parameter. The
    /// call is read-only. A field carries its value inside it; a field the folder holds no value for comes without a
    /// `value`. The custom fields are name and value pairs and are not part of any template. A folder without metadata is
    /// answered with empty lists, not with an error. Whether a template cascades from this folder to its content is not
    /// reported here. A request with neither a session nor a link key is answered with 401; a folder the caller cannot
    /// read with 403, a folder that does not exist with 404.
    /// </remarks>
    /// <summary>Get folder metadata</summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Folder metadata", typeof(EntryMetadataDto))]
    [SwaggerResponse(401, "The caller has neither a session nor an external link key")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Folder not found")]
    [AllowAnonymous]
    [HttpGet("metadata/folder/{folderId:int}")]
    public async Task<EntryMetadataDto> GetFolderMetadata(FolderIdRequestDto<int> inDto)
    {
        var metadata = await metadataService.GetEntryMetadataAsync(inDto.FolderId, FileEntryType.Folder);

        return metadataDtoMapper.Map(metadata);
    }

    /// <remarks>
    /// Assigns one or more metadata templates to a file, so its fields can be filled with
    /// `PUT api/2.0/files/metadata/file/{fileId}/values`. The caller needs the right to edit the file. The assignment writes
    /// no values and is idempotent: a template the file already carries is skipped, the others are added, an empty list
    /// changes nothing. The call finishes in the request, nothing runs in the background. A template a cascading folder above
    /// the file already provides stays inherited. A file the caller cannot edit is answered with 403; a file, or a template,
    /// that does not exist with 404. To take a template off the file use
    /// `DELETE api/2.0/files/metadata/file/{fileId}/templates/{templateId}`.
    /// </remarks>
    /// <summary>Assign templates to a file</summary>
    /// <path>api/2.0/files/metadata/file/{fileId}/templates</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "OK")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The file or one of the templates does not exist")]
    [HttpPut("metadata/file/{fileId:int}/templates")]
    public async Task AssignFileTemplates(AssignFileMetadataTemplatesRequestDto<int> inDto)
    {
        await metadataService.AssignTemplatesAsync(inDto.FileId, FileEntryType.File, inDto.Assign.TemplateIds);
    }

    /// <remarks>
    /// Assigns one or more metadata templates to a folder or a room and, with `cascade` set, propagates them to every
    /// folder and file below it. The caller needs the right to edit the folder; for a room that is its manager. The
    /// assignment of the folder itself finishes in the request and writes no values. The cascade is asynchronous: a pass is
    /// queued that assigns the templates to the whole subtree and copies the values the folder holds for their fields, and
    /// the answer is the status of that pass. Poll `GET api/2.0/files/metadata/folder/{folderId}/templates/progress`
    /// until `isCompleted` is true; a failed pass reports its `error` there. The `conflictResolveType` decides what happens
    /// to a value an entry already holds: `Skip` keeps it, `Overwrite` replaces it with the folder's value. A folder inside
    /// the subtree that cascades the same template keeps its own values for its content. Entries created in or moved into
    /// the folder later inherit the templates and the values on their own. Without a cascade the answer is a completed
    /// operation without an identifier. A folder the caller cannot edit is answered with 403; a folder, or a template, that
    /// does not exist with 404.
    /// </remarks>
    /// <summary>Assign templates to a folder</summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}/templates</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Cascade operation status; a completed operation without an ID when no cascade is requested", typeof(MetadataOperationDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The folder or one of the templates does not exist")]
    [HttpPut("metadata/folder/{folderId:int}/templates")]
    public async Task<MetadataOperationDto> AssignFolderTemplates(AssignFolderMetadataTemplatesRequestDto<int> inDto)
    {
        var taskId = await metadataService.AssignTemplatesToFolderAsync(inDto.FolderId, inDto.Assign.TemplateIds, inDto.Assign.Cascade, inDto.Assign.ConflictResolveType);

        // without a cascade the assignment is finished in this call, which the answer states as a completed operation instead of a null body
        return metadataDtoMapper.Map(taskId == null ? null : await metadataService.GetCascadeStatusAsync(inDto.FolderId));
    }

    /// <remarks>
    /// Reports the cascade pass of a folder started by `PUT api/2.0/files/metadata/folder/{folderId}/templates`: the
    /// running one, otherwise the most recent one. The caller needs read access to the folder, the call is read-only.
    /// `progress` is the share of the subtree processed, `isCompleted` tells the pass is over and `error` carries the reason
    /// of a failed one; a completed pass without an error has written every template and value it was asked for. A folder
    /// that never cascaded, or whose passes were already dropped, is answered with a completed operation without an
    /// identifier rather than with an error. A folder that does not exist is answered with 404.
    /// </remarks>
    /// <summary>Get cascade progress</summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}/templates/progress</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Cascade operation status; a completed operation without an ID when the folder has no cascade to report", typeof(MetadataOperationDto))]
    [SwaggerResponse(404, "Folder not found")]
    [HttpGet("metadata/folder/{folderId:int}/templates/progress")]
    public async Task<MetadataOperationDto> GetCascadeProgress(FolderIdRequestDto<int> inDto)
    {
        return metadataDtoMapper.Map(await metadataService.GetCascadeStatusAsync(inDto.FolderId));
    }

    /// <remarks>
    /// Removes a metadata template from a file together with the values of its fields. The caller needs the right to edit
    /// the file. The removal is irreversible for the values, the template itself stays on the portal and on the other
    /// entries. It applies to a directly assigned template and to one inherited from a cascading folder alike; a later
    /// cascade from that folder assigns it again. A file the caller cannot edit is answered with 403; a file, or a template,
    /// that does not exist with 404.
    /// </remarks>
    /// <summary>Unassign a template from a file</summary>
    /// <path>api/2.0/files/metadata/file/{fileId}/templates/{templateId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "OK")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The file or the template does not exist")]
    [HttpDelete("metadata/file/{fileId:int}/templates/{templateId:int}")]
    public async Task UnassignFileTemplate(UnassignFileMetadataTemplateRequestDto<int> inDto)
    {
        await metadataService.UnassignTemplateAsync(inDto.FileId, FileEntryType.File, inDto.TemplateId);
    }

    /// <remarks>
    /// Removes a metadata template from a folder or a room together with the values of its fields. The caller needs the
    /// right to edit the folder; for a room that is its manager. When the template was cascaded from this folder, the
    /// cascade stops here: the folders and files below keep the template and their values as a direct assignment of their
    /// own, and there is no bulk rollback. To take the template off them as well, remove it entry by entry with
    /// `DELETE api/2.0/files/metadata/file/{fileId}/templates/{templateId}`. A pass of the cascade still running is stopped
    /// for this template. A folder the caller cannot edit is answered with 403; a folder, or a template, that does not exist
    /// with 404.
    /// </remarks>
    /// <summary>Unassign a template from a folder</summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}/templates/{templateId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "OK")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The folder or the template does not exist")]
    [HttpDelete("metadata/folder/{folderId:int}/templates/{templateId:int}")]
    public async Task UnassignFolderTemplate(UnassignFolderMetadataTemplateRequestDto<int> inDto)
    {
        await metadataService.UnassignTemplateFromFolderAsync(inDto.FolderId, inDto.TemplateId);
    }

    /// <remarks>
    /// Writes the values of metadata fields on a file. The caller needs the right to edit the file: a member with editing
    /// access, or an anonymous caller through an external link that grants editing, with the link key in the
    /// `Request-Token` header or in the `share` query parameter; a link that grants viewing, commenting, reviewing or
    /// form filling only is refused. Every field must belong to a template the file carries, assigned with
    /// `PUT api/2.0/files/metadata/file/{fileId}/templates` or inherited from a cascading folder, and a field may be
    /// listed once. A value carries exactly the member of its type: `stringValue` for a text field of at most 8000
    /// characters, `numberValue` for a number, `dateValue` for a date, `optionIds` for a choice field, a single option
    /// for a single choice; an empty value clears the field. A date without a time zone offset is read as UTC. The write
    /// finishes in the request, the file is re-indexed for the metadata filters at once. The custom text fields are not
    /// written here: use `PUT api/2.0/files/metadata/file/{fileId}/customFields`. The answer is the whole metadata of the
    /// file after the write, the same shape `GET api/2.0/files/metadata/file/{fileId}` returns. A value of the wrong type,
    /// a field of a template the file does not carry, a field listed twice or a custom field is answered with 400; a
    /// request with neither a session nor a link key with 401; a file the caller cannot edit with 403; a file or a field
    /// that does not exist with 404.
    /// </remarks>
    /// <summary>Set file metadata values</summary>
    /// <path>api/2.0/files/metadata/file/{fileId}/values</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The metadata of the file after the write: the assigned templates with the values of their fields, and the custom fields", typeof(EntryMetadataDto))]
    [SwaggerResponse(401, "The caller has neither a session nor an external link key")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The file or a field does not exist")]
    [SwaggerResponse(400, "A value does not match the field type, a field is listed twice or is a custom field, or the field belongs to a template the file does not have")]
    [AllowAnonymous]
    [HttpPut("metadata/file/{fileId:int}/values")]
    public async Task<EntryMetadataDto> SetFileValues(SetFileMetadataValuesRequestDto<int> inDto)
    {
        var metadata = await metadataService.SetValuesAsync(inDto.FileId, FileEntryType.File, ToValues(inDto.Set));

        return metadataDtoMapper.Map(metadata);
    }

    /// <remarks>
    /// Writes the values of metadata fields on a folder or a room. The caller needs the right to edit the folder; for a
    /// room that is its manager. Every field must belong to a template the folder carries, assigned with
    /// `PUT api/2.0/files/metadata/folder/{folderId}/templates` or inherited from a cascading folder, and a field may be
    /// listed once. A value carries exactly the member of its type: `stringValue` for a text field of at most 8000
    /// characters, `numberValue` for a number, `dateValue` for a date, `optionIds` for a choice field; an empty value
    /// clears the field. A date without a time zone offset is read as UTC. The write
    /// finishes in the request and touches the folder only: to push the new values down a cascading folder run the
    /// cascade again with `Overwrite`, while entries created or moved in later take them on their own. The custom text
    /// fields are written with `PUT api/2.0/files/metadata/folder/{folderId}/customFields` instead. The answer is the
    /// whole metadata of the folder after the write. A value of the wrong type, a field listed twice, a custom field or a
    /// field of a template the folder does not carry is answered with 400; a folder the caller cannot edit with 403; a
    /// folder or a field that does not exist with 404.
    /// </remarks>
    /// <summary>Set folder metadata values</summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}/values</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The metadata of the folder after the write: the assigned templates with the values of their fields, and the custom fields", typeof(EntryMetadataDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The folder or a field does not exist")]
    [SwaggerResponse(400, "A value does not match the field type, a field is listed twice or is a custom field, or the field belongs to a template the folder does not have")]
    [HttpPut("metadata/folder/{folderId:int}/values")]
    public async Task<EntryMetadataDto> SetFolderValues(SetFolderMetadataValuesRequestDto<int> inDto)
    {
        var metadata = await metadataService.SetValuesAsync(inDto.FolderId, FileEntryType.Folder, ToValues(inDto.Set));

        return metadataDtoMapper.Map(metadata);
    }

    /// <remarks>
    /// Sets the custom text fields of a file: free-form name and value pairs that need no template. The caller needs the
    /// right to edit the file. A field is addressed by its name regardless of case: a listed name gets the value, a null or
    /// empty value removes the field from the file, the names not listed are left alone, so a partial request is safe. A name
    /// the portal has not seen yet creates the field for the whole portal, and a name no entry holds a value for any more is
    /// dropped, so the set of names follows the values. A name is at most 255 characters, a value at most 8000, a name may
    /// be listed once and a file holds at most 50 custom fields. The write finishes in the request; the values take part in
    /// the free text search and in the `metadataFilters` of the listings. The answer is the custom fields of the file
    /// after the write. An empty list, a blank, repeated or over-long name, an over-long value or more than 50 fields is
    /// answered with 400; a file the caller cannot edit with 403; a file that does not exist with 404.
    /// </remarks>
    /// <summary>Set file custom fields</summary>
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

        return fields.Select(metadataDtoMapper.Map).ToList();
    }

    /// <remarks>
    /// Sets the custom text fields of a folder or a room: free-form name and value pairs that need no template. The
    /// caller needs the right to edit the folder; for a room that is its manager. A field is addressed by its name
    /// regardless of case: a listed name gets the value, a null or empty value removes the field, the names not listed
    /// are left alone. A name the portal has not seen yet creates the field for the whole portal, and a name no entry
    /// holds a value for any more is dropped. A name is at most 255 characters, a value at most
    /// 8000, a name may be listed once and a folder holds at most 50 custom fields. The custom fields never cascade to the
    /// content of the folder. The write finishes in the request; the values take part in the free text search and in the
    /// `metadataFilters` of the listings. The answer is the custom fields of the folder after the write. An empty list, a
    /// blank, repeated or over-long name, an over-long value or more than 50 fields is answered with 400; a folder the
    /// caller cannot edit with 403; a folder that does not exist with 404.
    /// </remarks>
    /// <summary>Set folder custom fields</summary>
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

        return fields.Select(metadataDtoMapper.Map).ToList();
    }

    /// <summary>
    /// The list is declared required, which only makes the key mandatory: an explicit null, or a null element, used to
    /// pass the binding and fail with a server error instead of a bad request.
    /// </summary>
    private static List<CustomFieldUpdate> ToCustomFieldUpdates(SetCustomFields set)
    {
        if (set.Fields is null || set.Fields.Contains(null))
        {
            throw new ArgumentException(@"The custom fields are required", nameof(set));
        }

        return set.Fields.Select(f => new CustomFieldUpdate(f.Name, f.Value)).ToList();
    }

    private static List<MetadataValue> ToValues(SetMetadataValues set)
    {
        if (set.Values is null || set.Values.Contains(null))
        {
            throw new ArgumentException(@"The values are required", nameof(set));
        }

        return set.Values.Select(ToValue).ToList();
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
