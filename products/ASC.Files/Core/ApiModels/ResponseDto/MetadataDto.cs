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
    public int Id { get; set; }

    /// <summary>
    /// The template name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Specifies if the template is visible in the UI pickers.
    /// </summary>
    public bool Visible { get; set; }

    /// <summary>
    /// The user who created the template.
    /// </summary>
    public Guid CreateBy { get; set; }

    /// <summary>
    /// The template creation date.
    /// </summary>
    public ApiDateTime CreateOn { get; set; }

    /// <summary>
    /// The user who modified the template last.
    /// </summary>
    public Guid ModifiedBy { get; set; }

    /// <summary>
    /// The date when the template was modified last.
    /// </summary>
    public ApiDateTime ModifiedOn { get; set; }

    /// <summary>
    /// The template metadata fields.
    /// </summary>
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
    public int Id { get; set; }

    /// <summary>
    /// The ID of the template the field belongs to.
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// The field name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The field type.
    /// </summary>
    public MetadataFieldType Type { get; set; }

    /// <summary>
    /// The choice options of the field.
    /// </summary>
    public List<MetadataFieldOptionDto> Options { get; set; }

    /// <summary>
    /// The field display order inside the template.
    /// </summary>
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
    public Guid Id { get; set; }

    /// <summary>
    /// The option value.
    /// </summary>
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
    /// The template fields with their values on the entry.
    /// </summary>
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
    public MetadataFieldType Type { get; set; }

    /// <summary>
    /// The choice options of the field.
    /// </summary>
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
    public List<EntryTemplateDto> Templates { get; set; }

    /// <summary>
    /// The custom fields with their values.
    /// </summary>
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
    public string Id { get; set; }

    /// <summary>
    /// The operation progress percentage.
    /// </summary>
    public double Progress { get; set; }

    /// <summary>
    /// Specifies if the operation is completed.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// The operation error message.
    /// </summary>
    public string Error { get; set; }
}

[Scope]
public class MetadataDtoHelper(ApiDateTimeHelper apiDateTimeHelper)
{
    public MetadataTemplateDto Get(MetadataTemplate template)
    {
        return new MetadataTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Visible = template.Visible,
            CreateBy = template.CreateBy,
            CreateOn = apiDateTimeHelper.Get(template.CreateOn),
            ModifiedBy = template.ModifiedBy,
            ModifiedOn = apiDateTimeHelper.Get(template.ModifiedOn),
            Fields = template.Fields?.Select(Get).ToList()
        };
    }

    public MetadataFieldDto Get(MetadataField field)
    {
        return new MetadataFieldDto
        {
            Id = field.Id,
            TemplateId = field.TemplateId,
            Name = field.Name,
            Type = field.Type,
            Options = field.Options?.Select(o => new MetadataFieldOptionDto { Id = o.Id, Value = o.Value }).ToList(),
            Order = field.Order
        };
    }

    public MetadataValueDto Get(MetadataValue value)
    {
        return new MetadataValueDto
        {
            StringValue = value.StringValue,
            NumberValue = value.NumberValue,
            DateValue = value.DateValue.HasValue ? apiDateTimeHelper.Get(value.DateValue.Value) : null,
            OptionIds = value.OptionIds
        };
    }

    public EntryTemplateDto Get(TemplateMetadata metadata)
    {
        // the value is placed inside its field: a client used to join a separate values list to the template fields by id
        var values = (metadata.Values ?? []).ToDictionary(v => v.FieldId);

        return new EntryTemplateDto
        {
            Id = metadata.Template.Id,
            Name = metadata.Template.Name,
            Visible = metadata.Template.Visible,
            Fields = (metadata.Template.Fields ?? []).Select(f => Get(f, values.GetValueOrDefault(f.Id))).ToList()
        };
    }

    public EntryFieldDto Get(MetadataField field, MetadataValue value)
    {
        return new EntryFieldDto
        {
            Id = field.Id,
            Name = field.Name,
            Type = field.Type,
            Options = field.Options?.Select(o => new MetadataFieldOptionDto { Id = o.Id, Value = o.Value }).ToList(),
            Order = field.Order,
            Value = value == null ? null : Get(value)
        };
    }

    public static CustomFieldValueDto Get(CustomFieldValue customField)
    {
        return new CustomFieldValueDto
        {
            Name = customField.Field.Name,
            Value = customField.Value
        };
    }

    public EntryMetadataDto Get(EntryMetadata metadata)
    {
        return new EntryMetadataDto
        {
            Templates = metadata.Templates.Select(Get).ToList(),
            CustomFields = metadata.CustomFields.Select(Get).ToList()
        };
    }

    /// <summary>
    /// A missing operation means the folder has nothing running and nothing recent to report. That is answered as a
    /// completed operation without an ID, so a caller never gets a null body with a 200.
    /// </summary>
    public MetadataOperationDto Get(MetadataCascadeOperation operation)
    {
        return operation == null
            ? new MetadataOperationDto { Progress = 100, IsCompleted = true }
            : new MetadataOperationDto
            {
                Id = operation.Id,
                Progress = operation.Percentage,
                IsCompleted = operation.IsCompleted,
                Error = operation.Exception?.Message
            };
    }
}
