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
/// The data of the separate form item.
/// </summary>
public class FormsItemDataDto
{
    /// <summary>
    /// The form data key.
    /// </summary>
    /// <example>first_name</example>
    public string Key { get; init; }

    /// <summary>
    /// The form data tag.
    /// </summary>
    /// <example>personal_info</example>
    public string Tag { get; init; }

    /// <summary>
    /// The form data value.
    /// </summary>
    /// <example>John</example>
    public string Value { get; init; }

    /// <summary>
    /// The form data type.
    /// </summary>
    /// <example>text</example>
    public string Type { get; init; } = "";
}

#nullable enable

/// <summary>
/// The metadata of a single form field.
/// </summary>
public class FormMetadataDto
{
    /// <summary>
    /// The form field key.
    /// </summary>
    /// <example>name</example>
    public string Key { get; init; } = "";

    /// <summary>
    /// The form field type.
    /// </summary>
    /// <example>text</example>
    public string Type { get; init; } = "";

    /// <summary>
    /// The form field format.
    /// </summary>
    /// <example>date</example>
    public string? Format { get; init; }

    /// <summary>
    /// The list of possible values for the form field.
    /// </summary>
    /// <example>[]</example>
    public List<string>? PossibleValues { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class FormMetadataDtoMapper
{
    public static partial FormMetadataDto Map(this FormMetadata source);
}
