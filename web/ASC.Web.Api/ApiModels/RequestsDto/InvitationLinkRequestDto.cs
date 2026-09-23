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

namespace ASC.Web.Api.ApiModels.RequestsDto;

/// <summary>
/// Which role the portal invitation link grants.
/// </summary>
public class InvitationLinkRequestDto
{
    /// <summary>
    /// The role whoever follows the link joins with. Only `DocSpaceAdmin`, `RoomAdmin` and `User` have a link; any
    /// other role is refused. The portal keeps at most one link per role, so this value alone identifies it.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "employeeType")]
    public required EmployeeType EmployeeType { get; set; }
}

/// <summary>
/// The role a new invitation link grants, and the limits placed on it.
/// </summary>
public class InvitationLinkCreateRequestDto
{
    /// <summary>
    /// The role whoever follows the link joins with. Only `DocSpaceAdmin`, `RoomAdmin` and `User` are accepted, and
    /// the role cannot be changed afterwards - delete the link and create one for the other role instead.
    /// </summary>
    /// <example>1</example>
    public required EmployeeType EmployeeType { get; set; }

    /// <summary>
    /// When the link stops letting anyone in, read in the portal time zone. It has to lie in the future; leaving it
    /// out creates a link with no deadline at all.
    /// </summary>
    /// <example>2025-06-15T10:30:00.0000000Z</example>
    public DateTime? Expiration { get; set; }

    /// <summary>
    /// How many accounts may join through the link in total. Leaving it out creates a link with no use limit; the
    /// uses spent so far are reported as `currentUseCount`.
    /// </summary>
    /// <example>1</example>
    [Range(1, 1000)]
    public int? MaxUseCount { get; set; }
}

/// <summary>
/// The invitation link being changed, with the deadline and use limit it is to have afterwards.
/// </summary>
public class InvitationLinkUpdateRequestDto
{
    /// <summary>
    /// The link to change, by the `id` that creating or reading it returned. The role behind that id cannot be
    /// changed here.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public required Guid Id { get; set; }

    /// <summary>
    /// The new deadline, read in the portal time zone. The body is applied as a whole, so leaving it out clears the
    /// deadline rather than keeping the current one; a moment in the past is refused.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime? Expiration { get; set; }

    /// <summary>
    /// The new total number of accounts that may join through the link. It may not be lower than the uses already
    /// spent, which the link reports as `currentUseCount`, and leaving it out removes the limit rather than keeping
    /// the current one.
    /// </summary>
    /// <example>1</example>
    [Range(1, 1000)]
    public int? MaxUseCount { get; set; }
}

/// <summary>
/// Which invitation link is withdrawn.
/// </summary>
public class InvitationLinkDeleteRequestDto
{
    /// <summary>
    /// The link to delete, by the `id` that creating or reading it returned. A link recreated for the same role
    /// afterwards gets a new id, a new URL and a use count starting from zero.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public required Guid Id { get; set; }
}
