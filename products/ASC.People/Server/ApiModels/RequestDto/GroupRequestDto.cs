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

namespace ASC.People.ApiModels.RequestDto;

/// <summary>
/// The group request parameters.
/// </summary>
public class GroupRequestDto
{
    /// <summary>
    /// The accounts to put into the new group. Every one of them has to be an active member that is not a guest,
    /// otherwise the whole call is rejected. Omit it to create an empty group.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000", "11111111-1111-1111-1111-111111111111"]</example>
    public IEnumerable<Guid> Members { get; init; }

    /// <summary>
    /// The account to make the manager of the new group. It is added to the group as well, so it does not have to be
    /// repeated in `members`. Omit it to create a group without a manager.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid GroupManager { get; set; }

    /// <summary>
    /// The name of the group, from 1 to 128 characters. It is required, it may not be blank, and it does not have to
    /// be unique.
    /// </summary>
    /// <example>Marketing Team</example>
    [StringLength(128, MinimumLength = 1)]
    public required string GroupName { get; set; }
}

/// <summary>
/// The accounts a member operation applies to.
/// </summary>
public class MembersRequest
{
    /// <summary>
    /// The accounts the operation applies to. When adding or replacing members, an account that is a guest, is
    /// disabled or does not exist is skipped without an error; when removing them, an ID that is not a member is
    /// skipped as well.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000", "11111111-1111-1111-1111-111111111111"]</example>
    public IEnumerable<Guid> Members { get; init; }
}

/// <summary>
/// The member request parameters.
/// </summary>
public class MembersRequestDto
{
    /// <summary>
    /// The ID of the group whose members are changed, taken from the route. It has to be a group that has not been
    /// deleted, otherwise the operation answers 404.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "id")]
    public required Guid Id { get; set; }

    /// <summary>
    /// The accounts to add, replace with, or remove.
    /// </summary>
    /// <example>{"members":["00000000-0000-0000-0000-000000000000"]}</example>
    [FromBody]
    public required MembersRequest Members { get; set; }
}
