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
/// One member of a portal group together with the access that member has on the file or folder the group was granted
/// rights to. Every line of the answer describes the same file or folder and differs only in the member and in the
/// level that applies to them.
/// </summary>
public class GroupMemberSecurityRequestDto
{
    /// <summary>
    /// The member the line is about, as the portal reports the account: the display name, the avatar and the portal
    /// role to show next to the access level.
    /// </summary>
    /// <example>{"displayName": "John Doe"}</example>
    public required EmployeeFullDto User { get; init; }

    /// <summary>
    /// The level granted to the group as a whole on this file or folder. It belongs to the group record rather than
    /// to the member, so the same value repeats on every line of the answer; a group whose record was set back to
    /// none is answered with an empty list instead.
    /// </summary>
    /// <example>2</example>
    public required FileShare GroupAccess { get; init; }

    /// <summary>
    /// The level granted to this member alone on the same file or folder, or `null` when the member has no record of
    /// their own and the group level is what applies. The member who created the file or folder is always reported
    /// here as a room manager, whatever their own record says.
    /// </summary>
    /// <example>10</example>
    public FileShare? UserAccess { get; init; }

    /// <summary>
    /// Whether `userAccess` is the level that decides what the member may do. When it is false the member inherits
    /// `groupAccess`, and the creator of the file or folder is always reported as overridden because of the room
    /// manager level forced onto them.
    /// </summary>
    /// <example>true</example>
    public required bool Overridden { get; init; }

    /// <summary>
    /// Whether the caller may still change the level of this member. It comes back false on the line of the member
    /// who created the file or folder, on the line of the caller themselves, and on every line at once when the
    /// caller may read the file or folder but not manage access to it.
    /// </summary>
    /// <example>true</example>
    public required bool CanEditAccess { get; init; }

    /// <summary>
    /// Whether this member created the file or folder - the owner of the entry, not the owner of the group. Their
    /// level is reported as a room manager one and cannot be taken away through this group.
    /// </summary>
    /// <example>false</example>
    public required bool Owner { get; init; }
}