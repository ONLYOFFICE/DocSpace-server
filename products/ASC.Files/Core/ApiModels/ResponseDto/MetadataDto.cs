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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The metadata template information.
/// </summary>
public class MetadataTemplateDto
{
    /// <summary>
    /// The template ID.
    /// </summary>
    /// <example>3</example>
    public int Id { get; set; }

    /// <summary>
    /// The template name.
    /// </summary>
    /// <example>Contracts</example>
    public string Name { get; set; }

    /// <summary>
    /// Specifies if the template is visible in the UI pickers.
    /// </summary>
    /// <example>true</example>
    public bool Visible { get; set; }

    /// <summary>
    /// The user who created the template. A user who has since left the portal comes back as the "lost user" placeholder.
    /// </summary>
    public EmployeeDto CreateBy { get; set; }

    /// <summary>
    /// The template creation date.
    /// </summary>
    public ApiDateTime CreateOn { get; set; }

    /// <summary>
    /// The user who changed the template itself last: its name or its visibility. The creator until somebody changes
    /// it; a change of a field is recorded on the field, not here.
    /// </summary>
    public EmployeeDto ModifiedBy { get; set; }

    /// <summary>
    /// The date when the template was modified last.
    /// </summary>
    public ApiDateTime ModifiedOn { get; set; }

    /// <summary>
    /// The template metadata fields.
    /// </summary>
    /// <example>[{"id": 9, "templateId": 3, "name": "Customer", "type": 0, "order": 0}]</example>
    public List<MetadataFieldDto> Fields { get; set; }
}

/// <summary>
/// The metadata field information.
/// </summary>
public class MetadataFieldDto
{
    /// <summary>
    /// The field ID.
    /// </summary>
    /// <example>9</example>
    public int Id { get; set; }

    /// <summary>
    /// The ID of the template the field belongs to.
    /// </summary>
    /// <example>3</example>
    public int TemplateId { get; set; }

    /// <summary>
    /// The field name.
    /// </summary>
    /// <example>Customer</example>
    public string Name { get; set; }

    /// <summary>
    /// The field type.
    /// </summary>
    /// <example>0</example>
    public MetadataFieldType Type { get; set; }

    /// <summary>
    /// The choice options of the field.
    /// </summary>
    /// <example>[{"id": "4f1e2d3c-5b6a-4788-99aa-0c1d2e3f4a55", "value": "Red"}]</example>
    public List<MetadataFieldOptionDto> Options { get; set; }

    /// <summary>
    /// The field display order inside the template.
    /// </summary>
    /// <example>0</example>
    public int Order { get; set; }
}

/// <summary>
/// The metadata field choice option.
/// </summary>
public class MetadataFieldOptionDto
{
    /// <summary>
    /// The option ID.
    /// </summary>
    /// <example>4f1e2d3c-5b6a-4788-99aa-0c1d2e3f4a55</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The option value.
    /// </summary>
    /// <example>Red</example>
    public string Value { get; set; }
}

/// <summary>
/// The value of a metadata field on an entry. Exactly one of the value properties is set, the one matching the field type:
/// <c>stringValue</c> for a string field, <c>numberValue</c> for a number field, <c>dateValue</c> for a date field,
/// <c>optionIds</c> for a single or multiple choice field.
/// </summary>
public class MetadataValueDto
{
    /// <summary>
    /// The string value.
    /// </summary>
    /// <example>ACME Corp</example>
    public string StringValue { get; set; }

    /// <summary>
    /// The number value.
    /// </summary>
    /// <example>150000</example>
    public long? NumberValue { get; set; }

    /// <summary>
    /// The date value.
    /// </summary>
    public ApiDateTime DateValue { get; set; }

    /// <summary>
    /// The selected choice option IDs.
    /// </summary>
    /// <example>["4f1e2d3c-5b6a-4788-99aa-0c1d2e3f4a55"]</example>
    public List<Guid> OptionIds { get; set; }
}

/// <summary>
/// A metadata template assigned to an entry: the template with every field of it, each field carrying its value on the entry.
/// </summary>
public class EntryTemplateDto
{
    /// <summary>
    /// The template ID.
    /// </summary>
    /// <example>3</example>
    public int Id { get; set; }

    /// <summary>
    /// The template name.
    /// </summary>
    /// <example>Project</example>
    public string Name { get; set; }

    /// <summary>
    /// Specifies if the template is visible in the UI pickers.
    /// </summary>
    /// <example>true</example>
    public bool Visible { get; set; }

    /// <summary>
    /// Whether the template cascades from this entry to the folders and files below it. Only a folder or a room can
    /// cascade; on a file the value is always false.
    /// </summary>
    /// <example>true</example>
    public bool Cascade { get; set; }

    /// <summary>
    /// How the cascade of this entry treats a value an entry below already holds. Null while the template does not
    /// cascade from this entry.
    /// </summary>
    /// <example>0</example>
    public MetadataConflictResolveType? ConflictResolveType { get; set; }

    /// <summary>
    /// The template fields with their values on the entry.
    /// </summary>
    /// <example>[{"id": 9, "name": "Customer", "type": 0, "order": 0, "value": {"stringValue": "ACME Corp"}}]</example>
    public List<EntryFieldDto> Fields { get; set; }
}

/// <summary>
/// A metadata template field with its value on the entry.
/// </summary>
public class EntryFieldDto
{
    /// <summary>
    /// The field ID.
    /// </summary>
    /// <example>9</example>
    public int Id { get; set; }

    /// <summary>
    /// The field name.
    /// </summary>
    /// <example>Customer</example>
    public string Name { get; set; }

    /// <summary>
    /// The field type.
    /// </summary>
    /// <example>0</example>
    public MetadataFieldType Type { get; set; }

    /// <summary>
    /// The choice options of the field.
    /// </summary>
    /// <example>[{"id": "4f1e2d3c-5b6a-4788-99aa-0c1d2e3f4a55", "value": "Red"}]</example>
    public List<MetadataFieldOptionDto> Options { get; set; }

    /// <summary>
    /// The field display order inside the template.
    /// </summary>
    /// <example>0</example>
    public int Order { get; set; }

    /// <summary>
    /// The value of the field on the entry, or <c>null</c> when the entry holds no value for it.
    /// </summary>
    public MetadataValueDto Value { get; set; }
}

/// <summary>
/// The custom text field of an entry: a free-form name with its value. Custom fields belong to no template, need no
/// assignment and are addressed by name.
/// </summary>
public class CustomFieldValueDto
{
    /// <summary>
    /// The field name.
    /// </summary>
    /// <example>Project code</example>
    public string Name { get; set; }

    /// <summary>
    /// The field value on the entry.
    /// </summary>
    /// <example>A-42</example>
    public string Value { get; set; }
}

/// <summary>
/// The metadata of an entry: the assigned templates with their values, and the custom fields holding a value.
/// </summary>
public class EntryMetadataDto
{
    /// <summary>
    /// The assigned metadata templates, each field carrying its value on the entry.
    /// </summary>
    /// <example>[{"id": 3, "name": "Contracts", "visible": true, "fields": []}]</example>
    public List<EntryTemplateDto> Templates { get; set; }

    /// <summary>
    /// The custom fields with their values.
    /// </summary>
    /// <example>[{"name": "Project code", "value": "A-42"}]</example>
    public List<CustomFieldValueDto> CustomFields { get; set; }
}

/// <summary>
/// The cascade metadata assignment operation status.
/// </summary>
public class MetadataOperationDto
{
    /// <summary>
    /// The operation ID.
    /// </summary>
    /// <example>a1f4c9b2-3d8e-4f77-9b16-2c5de8f0a913</example>
    public string Id { get; set; }

    /// <summary>
    /// The operation progress percentage.
    /// </summary>
    /// <example>100</example>
    public double Progress { get; set; }

    /// <summary>
    /// Specifies if the operation is completed.
    /// </summary>
    /// <example>true</example>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// The operation error message.
    /// </summary>
    /// <example>Folder not found.</example>
    public string Error { get; set; }

    /// <summary>
    /// The IDs of the metadata templates the operation propagates to the subtree. Empty when there is no operation to
    /// report, so the answer describes nothing in progress.
    /// </summary>
    /// <example>[3]</example>
    public List<int> TemplateIds { get; set; }
}

[Scope]
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public partial class MetadataDtoMapper(ApiDateTimeHelper apiDateTimeHelper, EmployeeDtoHelper employeeDtoHelper)
{
    /// <summary>
    /// The authors come back as users, not as identifiers: a client listing the templates used to resolve every author
    /// with a request of its own. The helper caches the users within the request, so a list by one author costs one lookup.
    /// </summary>
    public async Task<MetadataTemplateDto> MapAsync(MetadataTemplate source)
    {
        var result = MapTemplate(source);

        result.CreateBy = await employeeDtoHelper.GetAsync(source.CreateBy);
        result.ModifiedBy = await employeeDtoHelper.GetAsync(source.ModifiedBy);

        return result;
    }

    public partial MetadataFieldDto Map(MetadataField source);

    public partial MetadataFieldOptionDto Map(MetadataFieldOption source);

    public partial MetadataValueDto Map(MetadataValue source);

    [MapProperty("Field.Name", nameof(CustomFieldValueDto.Name))]
    public partial CustomFieldValueDto Map(CustomFieldValue source);

    public partial EntryMetadataDto Map(EntryMetadata source);

    /// <summary>
    /// The value is placed inside its field: a client used to join a separate values list to the template fields by id.
    /// </summary>
    public EntryTemplateDto Map(TemplateMetadata source)
    {
        var values = (source.Values ?? []).ToDictionary(v => v.FieldId);

        return new EntryTemplateDto
        {
            Id = source.Template.Id,
            Name = source.Template.Name,
            Visible = source.Template.Visible,
            Cascade = source.Cascade,
            // the mode is stored for every link, but it means something only while the link cascades
            ConflictResolveType = source.Cascade ? source.CascadeConflict : null,
            Fields = (source.Template.Fields ?? []).Select(f => Map(f, values.GetValueOrDefault(f.Id))).ToList()
        };
    }

    public EntryFieldDto Map(MetadataField field, MetadataValue value)
    {
        var result = MapEntryField(field);

        result.Value = value == null ? null : Map(value);

        return result;
    }

    /// <summary>
    /// A missing operation means the folder has nothing running and nothing recent to report. That is answered as a
    /// completed operation without an ID, so a caller never gets a null body with a 200.
    /// </summary>
    public MetadataOperationDto Map(MetadataCascadeOperation source)
    {
        return source == null
            ? new MetadataOperationDto { Progress = 100, IsCompleted = true, TemplateIds = [] }
            : new MetadataOperationDto
            {
                Id = source.Id,
                Progress = source.Percentage,
                IsCompleted = source.IsCompleted,
                Error = source.Exception?.Message,
                TemplateIds = [.. source.TemplateIds]
            };
    }

    [MapperIgnoreTarget(nameof(MetadataTemplateDto.CreateBy))]
    [MapperIgnoreTarget(nameof(MetadataTemplateDto.ModifiedBy))]
    private partial MetadataTemplateDto MapTemplate(MetadataTemplate source);

    [MapperIgnoreTarget(nameof(EntryFieldDto.Value))]
    private partial EntryFieldDto MapEntryField(MetadataField source);

    private ApiDateTime MapDate(DateTime source)
    {
        return apiDateTimeHelper.Get(source);
    }

    private ApiDateTime MapDate(DateTime? source)
    {
        return source.HasValue ? apiDateTimeHelper.Get(source.Value) : null;
    }
}
