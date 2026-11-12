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
    /// Returns the metadata templates of the portal with their fields. A template is a set of fields that describes a file,
    /// a folder, or a room. Any signed-in user can call it. The call is read-only. Every caller gets the same list. The
    /// templates come in the order of their creation. Their fields come in display order, with the options of choice
    /// fields. Without `visible`, the call returns both shown and hidden templates. The author and the last editor of a
    /// template come as user objects. An empty list means the portal has no templates. The custom text fields of entries
    /// are not templates, and the call does not return them. To read them, use `GET api/2.0/files/metadata/file/{fileId}`
    /// or `GET api/2.0/files/metadata/folder/{folderId}`.
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
            yield return await metadataDtoMapper.MapAsync(template);
        }
    }

    /// <remarks>
    /// Creates a metadata template of the portal together with its fields. Requires a DocSpace admin. Other callers
    /// get 403. The call changes data. A repeated call returns 400, since the name is then taken. A field without `order` goes
    /// after the fields with one, in the order of the request. The response is the created template with the generated
    /// option IDs. The values of `PUT api/2.0/files/metadata/file/{fileId}/values` refer to these IDs. To add fields
    /// later, use `POST api/2.0/files/metadata/templates/{templateId}/fields`. The call compares names regardless of case.
    /// It returns 400 if `fields` is empty, or the name is empty, `System`, or taken. It also returns 400 for a name
    /// over 255 characters, repeated field names, an unknown field type, or wrong options. A choice field needs at least
    /// one option, and its options must be unique. Other field types take no options. If any field is invalid, the call
    /// creates nothing.
    /// </remarks>
    /// <summary>Create a metadata template</summary>
    /// <path>api/2.0/files/metadata/templates</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The created template with its fields and the generated identifiers of their options", typeof(MetadataTemplateDto))]
    [SwaggerResponse(400, "An invalid template or field: no fields, a name in use, reserved or too long, an unknown field type, duplicate field names or wrong options")]
    [SwaggerResponse(403, "The caller is not a DocSpace admin")]
    [HttpPost("metadata/templates")]
    public async Task<MetadataTemplateDto> CreateTemplate(CreateMetadataTemplateRequestDto inDto)
    {
        var fields = inDto.Fields?.Select(ToField);

        var template = await metadataService.CreateTemplateAsync(inDto.Name, inDto.Visible, fields);

        return await metadataDtoMapper.MapAsync(template);
    }

    /// <remarks>
    /// Returns one metadata template with its fields and the options of its choice fields. The fields come in display
    /// order. Any signed-in user can call it. The call is read-only. Use it to get the names and fields of the IDs
    /// in `assignedMetadataTemplates` of a file or a folder. If no template has this ID, the call returns 404.
    /// </remarks>
    /// <summary>Get a metadata template</summary>
    /// <path>api/2.0/files/metadata/templates/{templateId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The template with its fields in display order and the options of its choice fields", typeof(MetadataTemplateDto))]
    [SwaggerResponse(404, "No metadata template with this identifier exists on the portal")]
    [HttpGet("metadata/templates/{templateId:int}")]
    public async Task<MetadataTemplateDto> GetTemplate(MetadataTemplateIdRequestDto inDto)
    {
        var template = await metadataService.GetTemplateAsync(inDto.TemplateId);

        return await metadataDtoMapper.MapAsync(template);
    }

    /// <remarks>
    /// Renames a metadata template or changes whether it is shown in the pickers. Requires a DocSpace admin. Other
    /// callers get 403. The call changes data, and you can repeat it with the same body. A property left out keeps its
    /// value. This call does not change the fields. To change a field, use
    /// `PUT api/2.0/files/metadata/templates/{templateId}/fields/{fieldId}`. With `visible` false, the template keeps its
    /// assignments and values. Its fields leave the pickers, the filling, and the default text search. An entry with the
    /// template then has `hasMetadataFields` false if no other visible template or custom field gives it fields. With
    /// `visible` true, the call reverses this. The response is the whole template with its fields. If no template has
    /// this ID, the call returns 404. The call returns 400 if another template has the new name in any case. It also
    /// returns 400 for a name over 255 characters.
    /// </remarks>
    /// <summary>Update a metadata template</summary>
    /// <path>api/2.0/files/metadata/templates/{templateId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The whole template after the change, with its fields as they were", typeof(MetadataTemplateDto))]
    [SwaggerResponse(400, "A template with this name already exists, or the name is too long")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "No metadata template with this identifier exists on the portal")]
    [HttpPut("metadata/templates/{templateId:int}")]
    public async Task<MetadataTemplateDto> UpdateTemplate(UpdateMetadataTemplateRequestDto inDto)
    {
        var template = await metadataService.UpdateTemplateAsync(inDto.TemplateId, inDto.Update.Name, inDto.Update.Visible);

        return await metadataDtoMapper.MapAsync(template);
    }

    /// <remarks>
    /// Deletes a metadata template with its fields, assignments, and the values of its fields on all files, folders,
    /// and rooms. Requires a DocSpace admin. Other callers get 403. The deletion cannot be undone, and the call asks for
    /// no confirmation. All entries lose the template immediately. The call returns 404 for an unknown ID, so a repeated
    /// call also returns 404. To remove the template from one entry only, use
    /// `DELETE api/2.0/files/metadata/file/{fileId}/templates/{templateId}` or
    /// `DELETE api/2.0/files/metadata/folder/{folderId}/templates/{templateId}`.
    /// </remarks>
    /// <summary>Delete a metadata template</summary>
    /// <path>api/2.0/files/metadata/templates/{templateId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The template, its fields, its assignments and their values are deleted; the response carries no content")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "No metadata template with this identifier exists on the portal, or it was already deleted")]
    [HttpDelete("metadata/templates/{templateId:int}")]
    public async Task DeleteTemplate(MetadataTemplateIdRequestDto inDto)
    {
        await metadataService.DeleteTemplateAsync(inDto.TemplateId);
    }

    /// <remarks>
    /// Adds a field to a metadata template. Requires a DocSpace admin. Other callers get 403. The call changes data. A
    /// repeated call returns 400, since the field name is then taken. A field without `order` goes after the last field
    /// of the template. Entries with the template get the field without a value. The response is the created field with
    /// the generated option IDs. If no template has this ID, the call returns 404. The call returns 400 if the name is
    /// empty, over 255 characters, or taken in any case. It also returns 400 for an unknown type or wrong options. A
    /// choice field needs at least one option, and its options must be unique. Other field types take no options.
    /// </remarks>
    /// <summary>Add a metadata field</summary>
    /// <path>api/2.0/files/metadata/templates/{templateId}/fields</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The created field with the generated identifiers of its options", typeof(MetadataFieldDto))]
    [SwaggerResponse(400, "Invalid field: an empty, repeated or too long name, an unknown type, options on a non-choice field or a choice field without options")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "No metadata template with this identifier exists on the portal")]
    [HttpPost("metadata/templates/{templateId:int}/fields")]
    public async Task<MetadataFieldDto> CreateField(CreateMetadataFieldRequestDto inDto)
    {
        var field = await metadataService.CreateFieldAsync(inDto.TemplateId, ToField(inDto.Field));

        return metadataDtoMapper.Map(field);
    }

    /// <remarks>
    /// Changes the name, the type, the options, or the display order of a metadata field. Requires a DocSpace admin.
    /// Other callers get 403. The call changes data. A property left out keeps its value. The `options` list replaces
    /// all options of the field. Send a new option without an ID. The response gives it a new ID. So a repeated call
    /// gives new IDs to such options. The values already written stay as they are. The call returns 400 if another field
    /// of the template has the new name in any case. It also returns 400 if you change the type of a field with values.
    /// It also returns 400 if you remove an option that an entry has selected, or if the field is invalid. If the
    /// template has no field with this ID, the call returns 404. A field of another template also returns 404.
    /// </remarks>
    /// <summary>Update a metadata field</summary>
    /// <path>api/2.0/files/metadata/templates/{templateId}/fields/{fieldId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The field after the change, with a generated identifier for every new option", typeof(MetadataFieldDto))]
    [SwaggerResponse(400, "Invalid field, a name another field of the template has, a type change on a field with values or the removal of an option in use")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The template does not exist, or it has no field with this identifier")]
    [HttpPut("metadata/templates/{templateId:int}/fields/{fieldId:int}")]
    public async Task<MetadataFieldDto> UpdateField(UpdateMetadataFieldRequestDto inDto)
    {
        var field = await metadataService.UpdateFieldAsync(inDto.TemplateId, inDto.FieldId, ToFieldUpdate(inDto.Field));

        return metadataDtoMapper.Map(field);
    }

    /// <remarks>
    /// Deletes a metadata field with its values on all files, folders, and rooms. Requires a DocSpace admin. Other callers
    /// get 403. The deletion cannot be undone. The template and its other fields stay. A template always keeps at least
    /// one field. If this is the last field, the call returns 400 and deletes nothing. To delete the whole template, use
    /// `DELETE api/2.0/files/metadata/templates/{templateId}`. If the template has no field with this ID, the call
    /// returns 404. So a repeated call also returns 404.
    /// </remarks>
    /// <summary>Delete a metadata field</summary>
    /// <path>api/2.0/files/metadata/templates/{templateId}/fields/{fieldId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The field and every value written for it are deleted; the response carries no content")]
    [SwaggerResponse(400, "The field is the last one of the template; delete the template instead")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The template does not exist, or it has no field with this identifier")]
    [HttpDelete("metadata/templates/{templateId:int}/fields/{fieldId:int}")]
    public async Task DeleteField(DeleteMetadataFieldRequestDto inDto)
    {
        await metadataService.DeleteFieldAsync(inDto.TemplateId, inDto.FieldId);
    }

    /// <remarks>
    /// Returns the metadata templates of a file with their fields and values, and the custom text fields of the file. A
    /// template can be assigned directly or inherited from a cascading folder above. The response does not tell these
    /// apart. Requires read access to the file. Other callers get 403. An anonymous caller can use an external link to
    /// the file or to a folder above it. Pass the link key in the `Request-Token` header or the `share` query parameter.
    /// Without a session or a link key, the call returns 401. The call is read-only. A field without a value on the file
    /// has no `value`. For a file, `cascade` is always false, and `conflictResolveType` is absent. The custom fields are
    /// name and value pairs outside any template. A file without metadata returns empty lists. The `hasMetadataFields`
    /// flag is true if a visible template has a field or the file has a custom field. After a write,
    /// `PUT api/2.0/files/metadata/file/{fileId}/values` returns the same object. If no file has this ID, the call
    /// returns 404.
    /// </remarks>
    /// <summary>Get file metadata</summary>
    /// <path>api/2.0/files/metadata/file/{fileId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The templates assigned to the file with their fields and values, and the custom fields of the file", typeof(EntryMetadataDto))]
    [SwaggerResponse(401, "The caller has neither a session nor an external link key")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "No file with this identifier exists")]
    [AllowAnonymous]
    [HttpGet("metadata/file/{fileId:int}")]
    public async Task<EntryMetadataDto> GetFileMetadata(FileIdRequestDto<int> inDto)
    {
        var metadata = await metadataService.GetEntryMetadataAsync(inDto.FileId, FileEntryType.File);

        return metadataDtoMapper.Map(metadata);
    }

    /// <remarks>
    /// Returns the metadata templates of a folder or a room with their fields and values, and its custom text fields. A
    /// template can be assigned directly or inherited from a cascading folder above. Requires read access to the folder.
    /// Other callers get 403. An anonymous caller can use an external link to the folder or to a folder above it. Pass
    /// the link key in the `Request-Token` header or the `share` query parameter. Without a session or a link key, the
    /// call returns 401. The call is read-only. A field without a value on the folder has no `value`. A template has
    /// `cascade` true if this folder passes it down to its content. Then `conflictResolveType` gives the rule for
    /// existing values. Otherwise, `conflictResolveType` is absent. The custom fields are name and value pairs outside any
    /// template. A folder without metadata returns empty lists. The `hasMetadataFields` flag is true if a visible
    /// template has a field or the folder has a custom field. To turn a cascade on or off, use
    /// `PUT api/2.0/files/metadata/folder/{folderId}/templates/{templateId}`. If no folder or room has this ID, the call
    /// returns 404.
    /// </remarks>
    /// <summary>Get folder metadata</summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The templates assigned to the folder with their fields, values and cascade state, and the custom fields of the folder", typeof(EntryMetadataDto))]
    [SwaggerResponse(401, "The caller has neither a session nor an external link key")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "No folder or room with this identifier exists")]
    [AllowAnonymous]
    [HttpGet("metadata/folder/{folderId:int}")]
    public async Task<EntryMetadataDto> GetFolderMetadata(FolderIdRequestDto<int> inDto)
    {
        var metadata = await metadataService.GetEntryMetadataAsync(inDto.FolderId, FileEntryType.Folder);

        return metadataDtoMapper.Map(metadata);
    }

    /// <remarks>
    /// Assigns metadata templates to a file. After this call, fill their fields with
    /// `PUT api/2.0/files/metadata/file/{fileId}/values`. Requires editing access to the file. Other callers get 403. The
    /// call changes data but writes no values. You can repeat it with the same body. The call skips templates that the
    /// file already has, and an empty list changes nothing. If the file or any template does not exist, the call returns
    /// 404 and assigns nothing. The call returns 400 if the body has no `templateIds`. To remove a template, use
    /// `DELETE api/2.0/files/metadata/file/{fileId}/templates/{templateId}`.
    /// </remarks>
    /// <summary>Assign templates to a file</summary>
    /// <path>api/2.0/files/metadata/file/{fileId}/templates</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The templates are assigned to the file, the ones it already carried are skipped; the response carries no content")]
    [SwaggerResponse(400, "The request body cannot be read or has no `templateIds`")]
    [SwaggerResponse(403, "The caller cannot edit the file")]
    [SwaggerResponse(404, "The file or one of the templates does not exist")]
    [HttpPut("metadata/file/{fileId:int}/templates")]
    public async Task AssignFileTemplates(AssignFileMetadataTemplatesRequestDto<int> inDto)
    {
        await metadataService.AssignTemplatesAsync(inDto.FileId, FileEntryType.File, inDto.Assign.TemplateIds);
    }

    /// <remarks>
    /// Assigns metadata templates to a folder or a room. With `cascade` true, it also passes them down to the content.
    /// Requires the right to create content in the folder, which only the room manager has in a room. Other callers
    /// get 403. The root folders of sections also return 403. The call changes data but writes no values.
    /// With `cascade` false, the assignment ends in the request. The response is then a completed operation without an
    /// ID. With `cascade` true, the call also queues a pass over all folders and files below. The pass assigns the
    /// templates and copies the values of the folder. The response is that pass with the `templateIds` it passes down.
    /// Repeat `GET api/2.0/files/metadata/folder/{folderId}/templates/progress` until `isCompleted` is true. A failed
    /// pass reports its `error` there. The `conflictResolveType` value `Skip` keeps an existing value below, and
    /// `Overwrite` replaces it with the folder value. A folder below that cascades the same template keeps its own values for its
    /// content. A template that already cascades keeps cascading. To turn a cascade off, use
    /// `PUT api/2.0/files/metadata/folder/{folderId}/templates/{templateId}`. You can repeat the call. With `cascade`
    /// true, each call queues a new pass. An empty list changes nothing. If the folder or any template does not exist,
    /// the call returns 404. The call returns 400 if the body has no `templateIds`.
    /// </remarks>
    /// <summary>Assign templates to a folder</summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}/templates</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Cascade operation status; a completed operation without an ID when no cascade is requested", typeof(MetadataOperationDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `templateIds`")]
    [SwaggerResponse(403, "The caller cannot create in the folder, or is not the manager of the room")]
    [SwaggerResponse(404, "The folder or one of the templates does not exist")]
    [HttpPut("metadata/folder/{folderId:int}/templates")]
    public async Task<MetadataOperationDto> AssignFolderTemplates(AssignFolderMetadataTemplatesRequestDto<int> inDto)
    {
        var taskId = await metadataService.AssignTemplatesToFolderAsync(inDto.FolderId, inDto.Assign.TemplateIds, inDto.Assign.Cascade, inDto.Assign.ConflictResolveType);

        // without a cascade the assignment is finished in this call, which the answer states as a completed operation instead of a null body
        return metadataDtoMapper.Map(taskId == null ? null : await metadataService.GetCascadeOperationAsync(taskId));
    }

    /// <remarks>
    /// Returns the running cascade pass of a folder, or the most recent one if none is running. A pass starts with
    /// `PUT api/2.0/files/metadata/folder/{folderId}/templates` or
    /// `PUT api/2.0/files/metadata/folder/{folderId}/templates/{templateId}`. Requires read access to the folder. Other
    /// callers get 403. The call is read-only. This is the call to poll. Repeat it until `isCompleted` is true. The
    /// `templateIds` field names the templates of the pass. The `progress` field is the share of the subtree done. A
    /// failed pass has an `error`. A completed pass without an error has written all its templates and values. If the
    /// folder has no pass to report, the response is a completed operation with empty `templateIds`. If no folder or room
    /// has this ID, the call returns 404.
    /// </remarks>
    /// <summary>Get cascade progress</summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}/templates/progress</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Cascade operation status; a completed operation without an ID when the folder has no cascade to report", typeof(MetadataOperationDto))]
    [SwaggerResponse(403, "The caller cannot read the folder")]
    [SwaggerResponse(404, "No folder or room with this identifier exists")]
    [HttpGet("metadata/folder/{folderId:int}/templates/progress")]
    public async Task<MetadataOperationDto> GetCascadeProgress(FolderIdRequestDto<int> inDto)
    {
        return metadataDtoMapper.Map(await metadataService.GetCascadeStatusAsync(inDto.FolderId));
    }

    /// <remarks>
    /// Removes a metadata template from a file, together with the values of its fields. Requires editing access to the
    /// file. Other callers get 403. The deletion of the values cannot be undone. The template stays on the portal and on other
    /// entries. The call also removes a template inherited from a cascading folder. A later cascade from that folder
    /// assigns it again. A repeated call changes nothing. If the file or the template does not exist, the call
    /// returns 404.
    /// </remarks>
    /// <summary>Unassign a template from a file</summary>
    /// <path>api/2.0/files/metadata/file/{fileId}/templates/{templateId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The template and the values of its fields are removed from the file; the response carries no content")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The file or the template does not exist")]
    [HttpDelete("metadata/file/{fileId:int}/templates/{templateId:int}")]
    public async Task UnassignFileTemplate(UnassignFileMetadataTemplateRequestDto<int> inDto)
    {
        await metadataService.UnassignTemplateAsync(inDto.FileId, FileEntryType.File, inDto.TemplateId);
    }

    /// <remarks>
    /// Removes a metadata template from a folder or a room, together with the values of its fields. Requires the right to
    /// create content in the folder, which only the room manager has in a room. Other callers get 403. The root folders
    /// of sections also return 403. The deletion of the values cannot be undone. If this folder cascaded the
    /// template, the cascade stops here. A running pass also stops for this template. The folders and files below keep
    /// the template and their values as direct assignments. To remove it from them, call this operation or
    /// `DELETE api/2.0/files/metadata/file/{fileId}/templates/{templateId}` on each entry. To stop the cascade but keep
    /// the template and values on the folder, use `PUT api/2.0/files/metadata/folder/{folderId}/templates/{templateId}`.
    /// Set `cascade` to false there. A repeated call changes nothing. If the folder or the template does not exist, the
    /// call returns 404.
    /// </remarks>
    /// <summary>Unassign a template from a folder</summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}/templates/{templateId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The template and its values are removed from the folder, and a cascade of it from this folder stops; the response carries no content")]
    [SwaggerResponse(403, "The caller cannot create in the folder, or is not the manager of the room")]
    [SwaggerResponse(404, "The folder or the template does not exist")]
    [HttpDelete("metadata/folder/{folderId:int}/templates/{templateId:int}")]
    public async Task UnassignFolderTemplate(UnassignFolderMetadataTemplateRequestDto<int> inDto)
    {
        await metadataService.UnassignTemplateFromFolderAsync(inDto.FolderId, inDto.TemplateId);
    }

    /// <remarks>
    /// Turns the cascade of a metadata template on a folder or a room on or off. The template stays on the folder. First,
    /// assign the template with `PUT api/2.0/files/metadata/folder/{folderId}/templates`, or inherit it from a folder
    /// above. Requires the right to create content in the folder, which only the room manager has in a room. Other
    /// callers get 403. The root folders of sections also return 403. The call changes data. With `cascade` true, the
    /// template cascades with the given `conflictResolveType`. An inherited assignment becomes the folder's own. The call
    /// queues a pass, even if the cascade was already on. The response is that pass. Repeat
    /// `GET api/2.0/files/metadata/folder/{folderId}/templates/progress` until `isCompleted` is true. With `cascade`
    /// false or left out, the call turns the cascade off. The folder then keeps the template and its values. The entries
    /// below keep them as their own assignments. New entries no longer inherit the template from this folder. A running
    /// pass stops at its next batch. The response is then a completed operation without an ID, and the call ignores
    /// `conflictResolveType`. You can repeat the call. With `cascade` true, each call queues a new pass. The call returns
    /// 404 if the folder or template does not exist, or the template is not on the folder. If the body cannot be
    /// read, the call returns 400.
    /// </remarks>
    /// <summary>Update a template assignment on a folder</summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}/templates/{templateId}</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "Cascade operation status; a completed operation without an ID when the cascade was turned off", typeof(MetadataOperationDto))]
    [SwaggerResponse(400, "The request body cannot be read")]
    [SwaggerResponse(403, "The caller cannot create in the folder, or is not the manager of the room")]
    [SwaggerResponse(404, "The folder or the template does not exist, or the template is not assigned to the folder")]
    [HttpPut("metadata/folder/{folderId:int}/templates/{templateId:int}")]
    public async Task<MetadataOperationDto> UpdateFolderTemplate(UpdateFolderMetadataTemplateRequestDto<int> inDto)
    {
        var taskId = await metadataService.SetFolderTemplateCascadeAsync(inDto.FolderId, inDto.TemplateId, inDto.Update.Cascade, inDto.Update.ConflictResolveType);

        // the cascade turned off is finished in this call, which the answer states as a completed operation instead of a null body
        return metadataDtoMapper.Map(taskId == null ? null : await metadataService.GetCascadeOperationAsync(taskId));
    }

    /// <remarks>
    /// Writes the values of metadata fields on a file. Requires editing access to the file. Other callers get 403. An
    /// anonymous caller can use an external link that grants editing. Pass the link key in the `Request-Token` header or
    /// the `share` query parameter. A link for viewing, commenting, reviewing, or form filling gets 403. Without a session
    /// or a link key, the call returns 401. The call changes data, and you can repeat it with the same body. Each field
    /// must belong to a template of the file, assigned with `PUT api/2.0/files/metadata/file/{fileId}/templates` or
    /// inherited. Fill only the member for the field type: `stringValue`, `numberValue`, `dateValue`, or `optionIds`. A
    /// text value has at most 8000 characters. A single-choice field takes one option. An empty value clears the field.
    /// The metadata filters of listings find the new values immediately. To set the custom text fields, use
    /// `PUT api/2.0/files/metadata/file/{fileId}/customFields`. The response is the whole metadata of the file, as
    /// `GET api/2.0/files/metadata/file/{fileId}` returns it. The call returns 400 if a value does not fit the field type
    /// or limits. It also returns 400 for a field listed twice, a custom field, or a field of another template. If any
    /// value is invalid, the call writes nothing. If the file or a field does not exist, the call returns 404.
    /// </remarks>
    /// <summary>Set file metadata values</summary>
    /// <path>api/2.0/files/metadata/file/{fileId}/values</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The metadata of the file after the write: the assigned templates with the values of their fields, and the custom fields", typeof(EntryMetadataDto))]
    [SwaggerResponse(400, "A value does not match the field type or exceeds the number limit, a field is listed twice or is a custom field, or the field belongs to a template the file does not have")]
    [SwaggerResponse(401, "The caller has neither a session nor an external link key")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The file or a field does not exist")]
    [AllowAnonymous]
    [HttpPut("metadata/file/{fileId:int}/values")]
    public async Task<EntryMetadataDto> SetFileValues(SetFileMetadataValuesRequestDto<int> inDto)
    {
        var metadata = await metadataService.SetValuesAsync(inDto.FileId, FileEntryType.File, ToValues(inDto.Set));

        return metadataDtoMapper.Map(metadata);
    }

    /// <remarks>
    /// Writes the values of metadata fields on a folder or a room. Requires the right to create content in the folder,
    /// which only the room manager has in a room. Other callers get 403. The root folders of sections also
    /// return 403. The call changes data, and you can repeat it with the same body. Each field must belong to a template
    /// of the folder, assigned with `PUT api/2.0/files/metadata/folder/{folderId}/templates` or inherited. Fill only the
    /// member for the field type: `stringValue`, `numberValue`, `dateValue`, or `optionIds`. A text value has at most
    /// 8000 characters. A single-choice field takes one option. An empty value clears the field. The call writes on the
    /// folder only. To pass the new values down, run the cascade again with `Overwrite`. To set the custom text fields,
    /// use `PUT api/2.0/files/metadata/folder/{folderId}/customFields`. The response is the whole metadata of the folder.
    /// The call returns 400 if a value does not fit the field type or limits. It also returns 400 for a field listed
    /// twice, a custom field, or a field of another template. If any value is invalid, the call writes nothing. If the
    /// folder or a field does not exist, the call returns 404.
    /// </remarks>
    /// <summary>Set folder metadata values</summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}/values</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The metadata of the folder after the write: the assigned templates with the values of their fields, and the custom fields", typeof(EntryMetadataDto))]
    [SwaggerResponse(400, "A value does not match the field type or exceeds the number limit, a field is listed twice or is a custom field, or the field belongs to a template the folder does not have")]
    [SwaggerResponse(403, "The caller cannot create in the folder, or is not the manager of the room")]
    [SwaggerResponse(404, "The folder or a field does not exist")]
    [HttpPut("metadata/folder/{folderId:int}/values")]
    public async Task<EntryMetadataDto> SetFolderValues(SetFolderMetadataValuesRequestDto<int> inDto)
    {
        var metadata = await metadataService.SetValuesAsync(inDto.FolderId, FileEntryType.Folder, ToValues(inDto.Set));

        return metadataDtoMapper.Map(metadata);
    }

    /// <remarks>
    /// Sets the custom text fields of a file. These are name and value pairs that need no template. Requires editing
    /// access to the file. Other callers get 403. The call changes data, and you can repeat it with the same body. The
    /// call matches names regardless of case. A listed name gets the value, and a null or empty value removes the field.
    /// The call leaves the names that are not listed unchanged. A new name creates the field for the whole portal. When no
    /// entry has a value for a name, the portal deletes that field. The text search and the `metadataFilters` of listings
    /// find the values. The response is the custom fields of the file after the write. The call returns 400 for an empty
    /// list, or a blank or repeated name. It also returns 400 for a name over 255 characters or a value over 8000. It
    /// also returns 400 if the file would have more than 50 custom fields. If no file has this ID, the call returns 404.
    /// </remarks>
    /// <summary>Set file custom fields</summary>
    /// <path>api/2.0/files/metadata/file/{fileId}/customFields</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The custom fields of the file with their values", typeof(List<CustomFieldValueDto>))]
    [SwaggerResponse(400, "Invalid custom fields or too many of them")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "No file with this identifier exists")]
    [HttpPut("metadata/file/{fileId:int}/customFields")]
    public async Task<List<CustomFieldValueDto>> SetFileCustomFields(SetFileCustomFieldsRequestDto<int> inDto)
    {
        var fields = await metadataService.SetCustomFieldsAsync(inDto.FileId, FileEntryType.File, ToCustomFieldUpdates(inDto.Set));

        return fields.Select(metadataDtoMapper.Map).ToList();
    }

    /// <remarks>
    /// Sets the custom text fields of a folder or a room. These are name and value pairs that need no template. Requires
    /// the right to create content in the folder, which only the room manager has in a room. Other callers
    /// get 403. The root folders of sections also return 403. The call changes data, and you can repeat it with the same
    /// body. The call matches names regardless of case. A listed name gets the value, and a null or empty value removes
    /// the field. The call leaves the names that are not listed unchanged. A new name creates the field for the whole
    /// portal. When no entry has a value for a name, the portal deletes that field. The custom fields never cascade to the
    /// content of the folder. The response is the custom fields of the folder after the write. The call returns 400 for
    /// an empty list, or a blank or repeated name. It also returns 400 for a name over 255 characters or a value
    /// over 8000. It also returns 400 if the folder would have more than 50 custom fields. If no folder or room has this ID,
    /// the call returns 404.
    /// </remarks>
    /// <summary>Set folder custom fields</summary>
    /// <path>api/2.0/files/metadata/folder/{folderId}/customFields</path>
    [Tags("Files / Metadata")]
    [SwaggerResponse(200, "The custom fields of the folder with their values", typeof(List<CustomFieldValueDto>))]
    [SwaggerResponse(400, "Invalid custom fields or too many of them")]
    [SwaggerResponse(403, "The caller cannot create in the folder, or is not the manager of the room")]
    [SwaggerResponse(404, "No folder or room with this identifier exists")]
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
