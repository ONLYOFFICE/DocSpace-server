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

namespace ASC.Files.Core.ApiModels.RequestDto;

/// <summary>
/// A field whose key the user changed in the editor.
/// </summary>
public class FormFieldKeyChangeRequest
{
    /// <summary>
    /// The key the field had when the editor opened the form.
    /// </summary>
    /// <example>fromWhom</example>
    [Required]
    [StringLength(255)]
    public string OldKey { get; set; }

    /// <summary>
    /// The key the field has now.
    /// </summary>
    /// <example>applicantName</example>
    [Required]
    [StringLength(255)]
    public string NewKey { get; set; }
}

/// <summary>
/// The field keys the user changed in the editor.
/// </summary>
public class FormFieldChangesRequest
{
    /// <summary>
    /// The version of the form the editor opened; the old keys are the keys of that version.
    /// </summary>
    /// <example>3</example>
    [Range(1, int.MaxValue)]
    public int Version { get; set; }

    /// <summary>
    /// The number of this report within the editor session, growing with each one, such as the time it was taken in
    /// milliseconds; a report with a number not above the one taken already for the session is dropped.
    /// </summary>
    /// <example>1762250000000</example>
    [Range(0, long.MaxValue)]
    public long Sequence { get; set; }

    /// <summary>
    /// Every field whose key the user changed since the editor opened the form, told apart by the field id the editor
    /// keeps; empty when no key changed. The other fields that differ from the opened form were added or removed.
    /// </summary>
    /// <example>[{"oldKey": "fromWhom", "newKey": "applicantName"}]</example>
    [Required]
    [MaxLength(1000)]
    public List<FormFieldKeyChangeRequest> Renames { get; set; }
}

/// <summary>
/// The request parameters for reporting the field keys changed in the editor.
/// </summary>
public class FormFieldChangesRequestDto
{
    /// <summary>
    /// The PDF form open in the editor: the original form of a form-filling room.
    /// </summary>
    /// <example>35</example>
    [FromRoute(Name = "fileId")]
    public required int FileId { get; set; }

    /// <summary>
    /// The changed keys.
    /// </summary>
    [FromBody]
    public required FormFieldChangesRequest Changes { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public static partial class FormFieldKeyChangeRequestMapper
{
    public static partial FormFieldRename Map(this FormFieldKeyChangeRequest source);
}
