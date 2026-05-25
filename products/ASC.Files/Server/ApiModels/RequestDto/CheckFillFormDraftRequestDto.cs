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
/// The parameters for checking the form draft filling.
/// </summary>
public class CheckFillFormDraft
{
    /// <summary>
    /// The file version of the form draft.
    /// </summary>
    /// <example>1</example>
    public required int Version { get; set; }

    /// <summary>
    /// The action with the form draft.
    /// </summary>
    /// <example>view</example>
    public string Action { get; set; }

    /// <summary>
    /// Specifies whether to request the form for viewing or not.
    /// </summary>
    /// <example>false</example>
    public bool RequestView => (Action ?? "").Equals("view", StringComparison.InvariantCultureIgnoreCase);

    /// <summary>
    /// Specifies whether to request an embedded form or not.
    /// </summary>
    /// <example>false</example>
    public bool RequestEmbedded => (Action ?? "").Equals("embedded", StringComparison.InvariantCultureIgnoreCase);
}


/// <summary>
/// The request parameters for checking the form draft filling.
/// </summary>
public class CheckFillFormDraftRequestDto<T>
{
    /// <summary>
    /// The file ID of the form draft.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The parameters for checking the form draft filling.
    /// </summary>
    /// <example>{"version": 1, "action": "view"}</example>
    [FromBody]
    public required CheckFillFormDraft File { get; set; }
}