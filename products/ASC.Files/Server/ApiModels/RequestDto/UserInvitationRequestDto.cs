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
/// Which pending room invitations are to be sent again.
/// </summary>
public class UserInvitation
{
    /// <summary>
    /// The accounts to write to, taken from `GET api/2.0/files/rooms/{id}/share`. Anyone who has already joined, is
    /// not in the room, or is invisible to the caller is skipped without an error, and the field is ignored once
    /// every pending invitation is being resent.
    /// </summary>
    /// <example>["e9a7b4c1-2d3f-4a56-8b90-1c2d3e4f5a6b"]</example>
    public IEnumerable<Guid> UsersIds { get; set; }

    /// <summary>
    /// Whether every invitation of the room that is still waiting is sent again. With it on the list of accounts is
    /// ignored, and with it off an empty list means that nothing is sent at all.
    /// </summary>
    /// <example>false</example>
    public bool ResendAll { get; set; }
}

/// <summary>
/// The user invitation request parameters.
/// </summary>
public class UserInvitationRequestDto<T>
{
    /// <summary>
    /// The room whose invitations are resent, named by the identifier that `GET api/2.0/files/rooms` reports for it.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }

    /// <summary>
    /// Which pending invitations to send again.
    /// </summary>
    [FromBody]
    public required UserInvitation UserInvitation { get; set; }
}
