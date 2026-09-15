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
/// The room group to change and the changes to apply to it.
/// </summary>
public class UpdateRoomGroupRequestDto
{
    /// <summary>
    /// The room group to change, identified by the value `GET api/2.0/files/group` reports for it. A group of another
    /// account cannot be addressed and reads as missing.
    /// </summary>
    /// <example>42</example>
    [FromRoute(Name = "id")]
    public required int Id { get; set; }

    /// <summary>
    /// The changes to apply. Carrying none of them leaves the group as it is, and each of them may be sent on its own
    /// or together with the others.
    /// </summary>
    /// <example>{"groupName": "Client projects", "roomsToAdd": [12, 15], "roomsToRemove": [7]}</example>
    [FromBody]
    public required UpdateRoomGroupRequest UpdateRoom { get; set; }
}

/// <summary>
/// The changes to apply to a room group: a new name, rooms to attach and rooms to detach, in any combination.
/// </summary>
public class UpdateRoomGroupRequest
{
    /// <summary>
    /// The rooms to attach to the group, each given as a number for a room stored in the portal or as a string for a
    /// room on a connected third-party account. Every identifier has to name a room the caller can read; repeats and
    /// rooms the group already holds are collapsed rather than refused.
    /// </summary>
    /// <example>[12, 15]</example>
    public List<JsonElement> RoomsToAdd
    {
        get;
        set
        {
            field = value;
            HasPayload = true;
        }
    }

    /// <summary>
    /// The rooms to detach from the group, in the same two forms. Detaching leaves the room and its content
    /// untouched, and a room the group already holds can be detached even when the caller has lost access to it in
    /// the meantime.
    /// </summary>
    /// <example>[7]</example>
    public List<JsonElement> RoomsToRemove
    {
        get;
        set
        {
            field = value;
            HasPayload = true;
        }
    }

    /// <summary>
    /// The new name of the group, trimmed of surrounding spaces before it is stored. Leaving the member out keeps the
    /// current name, and a name that is blank once trimmed is refused.
    /// </summary>
    /// <example>Client projects</example>
    [StringLength(128)]
    public string GroupName
    {
        get;
        set
        {
            field = value;
            HasPayload = true;
        }
    }

    /// <summary>
    /// Whether the body carried at least one of the members above, even when its value was null.
    /// An empty body (<c>{}</c>) is a no-op, while a body that explicitly nulls every member asks
    /// for an update that cannot be performed and is rejected.
    /// </summary>
    internal bool HasPayload { get; private set; }
}
