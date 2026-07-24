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

/// <summary>
/// The metadata template - a named group of metadata fields.
/// </summary>
public class MetadataTemplate
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool Visible { get; set; }
    public bool IsSystem { get; set; }
    public Guid CreateBy { get; set; }
    public DateTime CreateOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public DateTime ModifiedOn { get; set; }
    public List<MetadataField> Fields { get; set; } = [];
}

/// <summary>
/// The metadata field definition.
/// </summary>
public class MetadataField
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public string Name { get; set; }
    public MetadataFieldType Type { get; set; }
    public List<MetadataFieldOption> Options { get; set; }
    public int Order { get; set; }
    public Guid CreateBy { get; set; }
    public DateTime CreateOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public DateTime ModifiedOn { get; set; }
}

/// <summary>
/// The metadata field choice option.
/// </summary>
public record MetadataFieldOption(Guid Id, string Value);

/// <summary>
/// The metadata field value assigned to an entry.
/// </summary>
public class MetadataValue
{
    public int FieldId { get; set; }
    public object EntryId { get; set; }
    public FileEntryType EntryType { get; set; }
    public string StringValue { get; set; }
    public long? NumberValue { get; set; }
    public DateTime? DateValue { get; set; }
    public List<Guid> OptionIds { get; set; }
    public Guid CreateBy { get; set; }
    public DateTime CreateOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public DateTime ModifiedOn { get; set; }

    public bool IsEmpty =>
        string.IsNullOrEmpty(StringValue)
        && NumberValue == null
        && DateValue == null
        && (OptionIds == null || OptionIds.Count == 0);
}

/// <summary>
/// The metadata template to entry binding.
/// </summary>
public class MetadataTemplateLink
{
    public int TemplateId { get; set; }
    public object EntryId { get; set; }
    public FileEntryType EntryType { get; set; }
    public bool Cascade { get; set; }
    public int? SourceFolderId { get; set; }
    public Guid CreateBy { get; set; }
    public DateTime CreateOn { get; set; }
}

/// <summary>
/// The metadata of an entry: the template with the values of its fields.
/// </summary>
public class EntryMetadata
{
    public MetadataTemplate Template { get; set; }
    public List<MetadataValue> Values { get; set; } = [];
}