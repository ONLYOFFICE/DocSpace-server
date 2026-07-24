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
    /// Specifies if this is the system template with the global visibility.
    /// </summary>
    public bool IsSystem { get; set; }

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
/// The metadata field value of an entry.
/// </summary>
public class MetadataValueDto
{
    /// <summary>
    /// The field ID.
    /// </summary>
    public int FieldId { get; set; }

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
    public ApiDateTime DateValue { get; set; }

    /// <summary>
    /// The selected choice option IDs.
    /// </summary>
    public List<Guid> OptionIds { get; set; }
}

/// <summary>
/// The metadata of an entry: the template with the values of its fields.
/// </summary>
public class EntryMetadataDto
{
    /// <summary>
    /// The metadata template.
    /// </summary>
    public MetadataTemplateDto Template { get; set; }

    /// <summary>
    /// The metadata field values.
    /// </summary>
    public List<MetadataValueDto> Values { get; set; }
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
            IsSystem = template.IsSystem,
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
            FieldId = value.FieldId,
            StringValue = value.StringValue,
            NumberValue = value.NumberValue,
            DateValue = value.DateValue.HasValue ? apiDateTimeHelper.Get(value.DateValue.Value) : null,
            OptionIds = value.OptionIds
        };
    }

    public EntryMetadataDto Get(EntryMetadata metadata)
    {
        return new EntryMetadataDto
        {
            Template = Get(metadata.Template),
            Values = metadata.Values?.Select(Get).ToList()
        };
    }

    public MetadataOperationDto Get(MetadataCascadeOperation operation)
    {
        return operation == null
            ? null
            : new MetadataOperationDto
            {
                Id = operation.Id,
                Progress = operation.Percentage,
                IsCompleted = operation.IsCompleted,
                Error = operation.Exception?.Message
            };
    }
}
