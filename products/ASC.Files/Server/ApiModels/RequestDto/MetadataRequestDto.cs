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

namespace ASC.Files.ApiModels.RequestDto;

/// <summary>
/// The parameters of a metadata field.
/// </summary>
public class MetadataFieldRequest
{
    /// <summary>
    /// The field name.
    /// </summary>
    /// <example>Contract number</example>
    public string Name { get; set; }

    /// <summary>
    /// The field type.
    /// </summary>
    public MetadataFieldType Type { get; set; }

    /// <summary>
    /// The choice options of the field.
    /// </summary>
    public List<MetadataFieldOptionRequest> Options { get; set; }

    /// <summary>
    /// The field display order inside the template. Omit it on update to keep the current order.
    /// </summary>
    public int? Order { get; set; }
}

/// <summary>
/// The parameters of a metadata field choice option.
/// </summary>
public class MetadataFieldOptionRequest
{
    /// <summary>
    /// The option ID. Omit it for a new option.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// The option value.
    /// </summary>
    /// <example>Red</example>
    public string Value { get; set; }
}

/// <summary>
/// The request parameters for listing metadata templates.
/// </summary>
public class GetMetadataTemplatesRequestDto
{
    /// <summary>
    /// Filters the templates by their visibility.
    /// </summary>
    [FromQuery(Name = "visible")]
    public bool? Visible { get; set; }
}

/// <summary>
/// The request parameters for creating a metadata template.
/// </summary>
public class CreateMetadataTemplateRequestDto
{
    /// <summary>
    /// The template name.
    /// </summary>
    /// <example>Contracts</example>
    public required string Name { get; set; }

    /// <summary>
    /// Specifies if the template is visible in the UI pickers.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// The template metadata fields.
    /// </summary>
    public List<MetadataFieldRequest> Fields { get; set; }
}

/// <summary>
/// The request parameters for a metadata template identified by its ID.
/// </summary>
public class MetadataTemplateIdRequestDto
{
    /// <summary>
    /// The template ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "templateId")]
    public required int TemplateId { get; set; }
}

/// <summary>
/// The request parameters for updating a metadata template.
/// </summary>
public class UpdateMetadataTemplateRequestDto
{
    /// <summary>
    /// The template ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "templateId")]
    public required int TemplateId { get; set; }

    /// <summary>
    /// The parameters for updating the template.
    /// </summary>
    [FromBody]
    public required UpdateMetadataTemplate Update { get; set; }
}

/// <summary>
/// The parameters for updating a metadata template.
/// </summary>
public class UpdateMetadataTemplate
{
    /// <summary>
    /// The new template name.
    /// </summary>
    /// <example>Contracts</example>
    public string Name { get; set; }

    /// <summary>
    /// Specifies if the template is visible in the UI pickers.
    /// </summary>
    public bool? Visible { get; set; }
}

/// <summary>
/// The request parameters for creating a metadata field.
/// </summary>
public class CreateMetadataFieldRequestDto
{
    /// <summary>
    /// The template ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "templateId")]
    public required int TemplateId { get; set; }

    /// <summary>
    /// The parameters of the field.
    /// </summary>
    [FromBody]
    public required MetadataFieldRequest Field { get; set; }
}

/// <summary>
/// The request parameters for updating a metadata field.
/// </summary>
public class UpdateMetadataFieldRequestDto
{
    /// <summary>
    /// The template ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "templateId")]
    public required int TemplateId { get; set; }

    /// <summary>
    /// The field ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fieldId")]
    public required int FieldId { get; set; }

    /// <summary>
    /// The parameters of the field.
    /// </summary>
    [FromBody]
    public required MetadataFieldRequest Field { get; set; }
}

/// <summary>
/// The request parameters for deleting a metadata field.
/// </summary>
public class DeleteMetadataFieldRequestDto
{
    /// <summary>
    /// The template ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "templateId")]
    public required int TemplateId { get; set; }

    /// <summary>
    /// The field ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fieldId")]
    public required int FieldId { get; set; }
}

/// <summary>
/// The request parameters for assigning metadata templates to a file.
/// </summary>
public class AssignFileMetadataTemplatesRequestDto<T>
{
    /// <summary>
    /// The file ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The parameters for assigning templates.
    /// </summary>
    [FromBody]
    public required AssignMetadataTemplates Assign { get; set; }
}

/// <summary>
/// The request parameters for assigning metadata templates to a folder.
/// </summary>
public class AssignFolderMetadataTemplatesRequestDto<T>
{
    /// <summary>
    /// The folder ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "folderId")]
    public required T FolderId { get; set; }

    /// <summary>
    /// The parameters for assigning templates.
    /// </summary>
    [FromBody]
    public required AssignMetadataTemplates Assign { get; set; }
}

/// <summary>
/// The parameters for assigning metadata templates.
/// </summary>
public class AssignMetadataTemplates
{
    /// <summary>
    /// The metadata template IDs.
    /// </summary>
    public required List<int> TemplateIds { get; set; }

    /// <summary>
    /// Specifies if the templates are propagated to the folder sub-entries.
    /// </summary>
    public bool Cascade { get; set; }

    /// <summary>
    /// The conflict resolve type for the cascade assignment.
    /// </summary>
    public MetadataConflictResolveType ConflictResolveType { get; set; } = MetadataConflictResolveType.Skip;
}

/// <summary>
/// The request parameters for unassigning a metadata template from a file.
/// </summary>
public class UnassignFileMetadataTemplateRequestDto<T>
{
    /// <summary>
    /// The file ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The template ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "templateId")]
    public required int TemplateId { get; set; }
}

/// <summary>
/// The request parameters for unassigning a metadata template from a folder.
/// </summary>
public class UnassignFolderMetadataTemplateRequestDto<T>
{
    /// <summary>
    /// The folder ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "folderId")]
    public required T FolderId { get; set; }

    /// <summary>
    /// The template ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "templateId")]
    public required int TemplateId { get; set; }
}

/// <summary>
/// The parameters of a metadata field value.
/// </summary>
public class MetadataValueRequest
{
    /// <summary>
    /// The field ID.
    /// </summary>
    /// <example>1</example>
    public required int FieldId { get; set; }

    /// <summary>
    /// The string value.
    /// </summary>
    public string StringValue { get; set; }

    /// <summary>
    /// The number value.
    /// </summary>
    public long? NumberValue { get; set; }

    /// <summary>
    /// The date value.
    /// </summary>
    public DateTime? DateValue { get; set; }

    /// <summary>
    /// The selected choice option IDs.
    /// </summary>
    public List<Guid> OptionIds { get; set; }
}

/// <summary>
/// The request parameters for setting metadata values on a file.
/// </summary>
public class SetFileMetadataValuesRequestDto<T>
{
    /// <summary>
    /// The file ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The parameters for setting values.
    /// </summary>
    [FromBody]
    public required SetMetadataValues Set { get; set; }
}

/// <summary>
/// The request parameters for setting metadata values on a folder.
/// </summary>
public class SetFolderMetadataValuesRequestDto<T>
{
    /// <summary>
    /// The folder ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "folderId")]
    public required T FolderId { get; set; }

    /// <summary>
    /// The parameters for setting values.
    /// </summary>
    [FromBody]
    public required SetMetadataValues Set { get; set; }
}

/// <summary>
/// The parameters for setting metadata values.
/// </summary>
public class SetMetadataValues
{
    /// <summary>
    /// The metadata field values. An empty value clears the field.
    /// </summary>
    public required List<MetadataValueRequest> Values { get; set; }
}

/// <summary>
/// The request parameters for adding a custom text field to a file.
/// </summary>
public class AddFileCustomFieldRequestDto<T>
{
    /// <summary>
    /// The file ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The parameters of the custom field.
    /// </summary>
    [FromBody]
    public required AddCustomField Field { get; set; }
}

/// <summary>
/// The request parameters for adding a custom text field to a folder.
/// </summary>
public class AddFolderCustomFieldRequestDto<T>
{
    /// <summary>
    /// The folder ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "folderId")]
    public required T FolderId { get; set; }

    /// <summary>
    /// The parameters of the custom field.
    /// </summary>
    [FromBody]
    public required AddCustomField Field { get; set; }
}

/// <summary>
/// The parameters of a custom text field.
/// </summary>
public class AddCustomField
{
    /// <summary>
    /// The field name.
    /// </summary>
    /// <example>Project code</example>
    public required string Name { get; set; }

    /// <summary>
    /// The field value.
    /// </summary>
    /// <example>A-42</example>
    public string Value { get; set; }
}
