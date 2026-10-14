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
/// The form versions whose fields look renamed since the version before them, waiting for the answer whether the
/// fields keep their meaning.
/// </summary>
public class FormFieldRenamesDto
{
    /// <summary>
    /// The versions waiting for an answer, oldest first; answer them in this order. Empty when nothing waits for an
    /// answer: the fields are the same, added or removed, a version needs a table of its own anyway, or every question
    /// was answered.
    /// </summary>
    /// <example>[{"version": 4, "renames": [{"oldKey": "dob", "newKey": "birth_date", "type": "dateTime", "options": null, "tip": "Date of birth"}]}]</example>
    public List<FormVersionFieldRenamesDto> Versions { get; set; }
}

/// <summary>
/// The renamed fields of one form version.
/// </summary>
public class FormVersionFieldRenamesDto
{
    /// <summary>
    /// The form version the fields were renamed in; pass it with the answer.
    /// </summary>
    /// <example>4</example>
    public int Version { get; set; }

    /// <summary>
    /// The renamed fields in form order.
    /// </summary>
    /// <example>[{"oldKey": "dob", "newKey": "birth_date", "type": "dateTime", "options": null, "tip": "Date of birth"}]</example>
    public List<FormFieldRenameDto> Renames { get; set; }
}

/// <summary>
/// A form field whose key changed since the previous version, described by what the form author set, never by the
/// values people entered.
/// </summary>
public class FormFieldRenameDto
{
    /// <summary>
    /// The key of the field in the previous version.
    /// </summary>
    /// <example>dob</example>
    public string OldKey { get; set; }

    /// <summary>
    /// The key of the field in this version.
    /// </summary>
    /// <example>birth_date</example>
    public string NewKey { get; set; }

    /// <summary>
    /// The field type as the editor names it, such as text, checkBox, comboBox, dropDownList, radio or dateTime. Null
    /// when it is unknown.
    /// </summary>
    /// <example>dateTime</example>
    public string Type { get; set; }

    /// <summary>
    /// The values a list or a radio group offers. Null for any other field.
    /// </summary>
    /// <example>["Yes", "No"]</example>
    public List<string> Options { get; set; }

    /// <summary>
    /// The tip the form author set for the field. Null when there is none or it could not be read.
    /// </summary>
    /// <example>Date of birth</example>
    public string Tip { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class FormFieldRenamesDtoMapper
{
    public static partial FormVersionFieldRenamesDto Map(this FormVersionLink source);

    public static partial FormFieldRenameDto Map(this FormFieldRename source);
}
