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
/// The request parameters for inviting users to the room.
/// </summary>
public class RoomInvitationRequest
{
    /// <summary>
    /// The collection of invitation parameters.
    /// </summary>
    /// <example>[{"id": "00000000-0000-0000-0000-000000000000", "access": 1}]</example>
    [MaxEmailInvitations]
    public List<RoomInvitation> Invitations { get; set; }

    /// <summary>
    /// Specifies whether to notify users about the shared room or not.
    /// </summary>
    /// <example>true</example>
    public bool Notify { get; set; }

    /// <summary>
    /// The message to send when notifying about the shared room.
    /// </summary>
    /// <example>You have been invited to the room</example>
    public string Message { get; set; }

    /// <summary>
    /// The language of the room invitation.
    /// </summary>
    /// <example>en-US</example>
    public string Culture { get; set; }

    /// <summary>
    /// Specifies whether to forcibly delete a user with form roles from the room.
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
    /// The room ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }

    /// <summary>
    /// The room invitation request.
    /// </summary>
    [FromBody]
    public required RoomInvitationRequest RoomInvitation { get; set; }
}
