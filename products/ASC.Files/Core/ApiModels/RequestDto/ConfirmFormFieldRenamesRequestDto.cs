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
/// The answer to the renamed fields of a form version.
/// </summary>
public class ConfirmFormFieldRenamesRequest
{
    /// <summary>
    /// The form version the answer is for: the `version` that `GET api/2.0/files/file/{fileId}/fieldrenames` reported
    /// together with the renamed fields.
    /// </summary>
    /// <example>4</example>
    public int Version { get; set; }

    /// <summary>
    /// True when every reported field keeps its meaning under the new key, false when they are new fields.
    /// </summary>
    /// <example>true</example>
    public bool Confirm { get; set; }
}

/// <summary>
/// The request parameters for answering the renamed fields of a form.
/// </summary>
public class ConfirmFormFieldRenamesRequestDto
{
    /// <summary>
    /// The PDF form the answer is for: the original form of a form-filling room.
    /// </summary>
    /// <example>35</example>
    [FromRoute(Name = "fileId")]
    public required int FileId { get; set; }

    /// <summary>
    /// The answer.
    /// </summary>
    [FromBody]
    public required ConfirmFormFieldRenamesRequest Answer { get; set; }
}
