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
/// The revision of the form to open and what the caller intends to do with it.
/// </summary>
public class CheckFillFormDraft
{
    /// <summary>
    /// The revision of the form to open. Pass 0 for the current revision; a positive number addresses that entry of
    /// the file history and is accepted only from a caller who may read the history, so a member who only has
    /// fill-forms access must send 0.
    /// </summary>
    /// <example>0</example>
    public required int Version { get; set; }

    /// <summary>
    /// What the caller intends to do with the form. `view` asks for a read-only address and `embedded` for an address
    /// to be shown inside a frame; both only resolve the address and leave the file untouched. Leave it out to enter
    /// the filling flow, where the personal draft is created or reused. The value is matched case-insensitively, and
    /// anything else behaves like an empty value.
    /// </summary>
    /// <example>view</example>
    public string Action { get; set; }

    /// <summary>
    /// Whether the caller asked for a read-only address. The server derives it from `action` being `view` and ignores
    /// any value sent with the request.
    /// </summary>
    /// <example>false</example>
    public bool RequestView => (Action ?? "").Equals("view", StringComparison.InvariantCultureIgnoreCase);

    /// <summary>
    /// Whether the caller asked for an address to be shown inside a frame. The server derives it from `action` being
    /// `embedded` and ignores any value sent with the request.
    /// </summary>
    /// <example>false</example>
    public bool RequestEmbedded => (Action ?? "").Equals("embedded", StringComparison.InvariantCultureIgnoreCase);
}


/// <summary>
/// The form to open for filling, together with the revision and the intent of the call.
/// </summary>
public class CheckFillFormDraftRequestDto<T>
{
    /// <summary>
    /// The identifier of the PDF form to open, as it is returned by a room listing such as
    /// `GET api/2.0/files/{folderId}`. The identifier of an already created draft is accepted here as well.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The revision of the form to open and what the caller intends to do with it.
    /// </summary>
    /// <example>{"version": 0, "action": "view"}</example>
    [FromBody]
    public required CheckFillFormDraft File { get; set; }
}