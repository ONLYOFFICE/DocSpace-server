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
/// The parameters for renaming a custom room tag in the portal catalog.
/// </summary>
public class UpdateTagRequestDto : IValidatableObject
{
    /// <summary>
    /// The name of the tag to rename, matched against the catalog exactly as it is stored rather than searched for.
    /// Read the stored spelling from `GET api/2.0/files/tags`.
    /// </summary>
    /// <example>Confidential</example>
    [StringLength(CreateTagRequestDto.MaxNameLength)]
    public required string OldName { get; set; }

    /// <summary>
    /// The name to store instead. It has to be free: names are unique across the portal, so a name another tag
    /// already carries is refused, and merging two tags this way is not possible.
    /// </summary>
    /// <example>Restricted</example>
    [StringLength(CreateTagRequestDto.MaxNameLength)]
    public required string NewName { get; set; }

    /// <summary>
    /// Neither end of a rename may be blank — the same rule that applies when the tag is created.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(OldName))
        {
            yield return new ValidationResult("A tag name cannot be empty or consist of whitespace only.", [nameof(OldName)]);
        }

        if (string.IsNullOrWhiteSpace(NewName))
        {
            yield return new ValidationResult("A tag name cannot be empty or consist of whitespace only.", [nameof(NewName)]);
        }
    }
}
