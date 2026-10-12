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
/// One batch of membership changes for a room.
/// </summary>
public class RoomInvitationRequest
{
    /// <summary>
    /// Who is added, changed or removed, one entry per subject. The same subject named twice keeps the level of the
    /// last entry, and an empty list is accepted and changes nothing.
    /// </summary>
    /// <example>[{"id": "e9a7b4c1-2d3f-4a56-8b90-1c2d3e4f5a6b", "access": 10}]</example>
    [MaxEmailInvitations]
    public List<RoomInvitation> Invitations { get; set; }

    /// <summary>
    /// Whether the subjects that gained access are told about it by email. With it off the change is silent, which is
    /// the usual choice when membership is synchronised from another system.
    /// </summary>
    /// <example>true</example>
    public bool Notify { get; set; }

    /// <summary>
    /// The line added to the invitation email. It is used only while the notification is on, and it reaches nobody
    /// whose access was removed.
    /// </summary>
    /// <example>Please review the contract by Friday</example>
    public string Message { get; set; }

    /// <summary>
    /// The language of the invitation email, as a portal culture name such as en-US. Leaving it out sends each
    /// message in the language of its recipient.
    /// </summary>
    /// <example>en-US</example>
    public string Culture { get; set; }

    /// <summary>
    /// Whether a member who still holds a role in an unfinished form is removed anyway. With it off such a removal is
    /// refused and reported through the error of the answer, so the form can be reassigned first.
    /// </summary>
    /// <example>false</example>
    public bool Force { get; set; }
}

/// <summary>
/// The generic request parameters for inviting users to the room.
/// </summary>
public class RoomInvitationRequestDto<T>
{
    /// <summary>
    /// The room whose membership changes, named by the identifier that `GET api/2.0/files/rooms` reports for it.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }

    /// <summary>
    /// The membership changes to apply, together with how the people concerned are notified.
    /// </summary>
    [FromBody]
    public required RoomInvitationRequest RoomInvitation { get; set; }
}
