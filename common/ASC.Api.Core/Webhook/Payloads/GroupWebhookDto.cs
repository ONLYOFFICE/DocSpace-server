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

namespace ASC.Api.Core.Webhook.Payloads;

// A copy of GroupDto, deliberately not derived from it: the webhook wire contract is versioned separately from the
// REST API. The member list is not carried, deliberately: a group can hold thousands of users, and every one of them
// would be expanded into the body of every group event.

/// <summary>
/// The group a group.* webhook is about.
/// </summary>
/// <remarks>
/// The payload describes the group at the time of the event, without the list of its members. A `group.updated`
/// delivery follows a change made through the group API - a rename, a new manager, or members added, removed,
/// replaced or moved to another group - but does not say which of these happened: compare the payload with the
/// previous one, and read the current members with `GET api/2.0/group/{id}` when they are needed.
/// </remarks>
public class GroupWebhookDto
{
    /// <summary>
    /// The ID of the group the event is about; `GET api/2.0/group/{id}` returns the group with its members by it.
    /// </summary>
    /// <example>c652dd0b-fa6e-4bb7-a3b2-9a2e4e6b7d1c</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the group as it is shown in the portal, after a rename the new one.
    /// </summary>
    /// <example>Marketing Team</example>
    public string Name { get; set; }

    /// <summary>
    /// The parent group ID; absent for a top-level group.
    /// </summary>
    /// <example>3f5c2a1e-8b7d-4e6f-9a0b-1c2d3e4f5a6b</example>
    public Guid? Parent { get; set; }

    /// <summary>
    /// The ID of the group category: the empty GUID for the groups members create, and one fixed category ID for
    /// the built-in system groups.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid Category { get; set; }

    /// <summary>
    /// Whether the group is synchronized from an LDAP directory, where its name and members are managed; `false` for
    /// a group created in the portal.
    /// </summary>
    /// <example>false</example>
    public bool IsLDAP { get; set; }

    /// <summary>
    /// `true` for a built-in system group, whose members follow from the user types rather than from membership
    /// changes. Absent for every other group, never `false`.
    /// </summary>
    /// <example>true</example>
    public bool? IsSystem { get; set; }

    /// <summary>
    /// The user who manages the group. Absent when the group has no manager. A
    /// subscription owner of the `User` or `Guest` type, who may not see the contacts of other users, gets it without
    /// the user name and the profile link.
    /// </summary>
    public UserSummaryWebhookDto Manager { get; set; }

    /// <summary>
    /// How many users belong to the group at the time of the event, disabled accounts not counted; 0 for a group
    /// without members.
    /// </summary>
    /// <example>12</example>
    public int MembersCount { get; set; }

    internal GroupWebhookDto Clone()
    {
        return (GroupWebhookDto)MemberwiseClone();
    }
}

/// <summary>
/// A group referenced from another webhook payload.
/// </summary>
public class GroupSummaryWebhookDto
{
    /// <summary>
    /// The ID of the group; `GET api/2.0/group/{id}` returns the group with its members by it.
    /// </summary>
    /// <example>c652dd0b-fa6e-4bb7-a3b2-9a2e4e6b7d1c</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the group as it is shown in the portal.
    /// </summary>
    /// <example>Marketing Team</example>
    public string Name { get; set; }

    /// <summary>
    /// The user name of the user who manages the group, not an ID. Absent when the group has no manager.
    /// </summary>
    /// <example>Mike.Zanyatski</example>
    public string Manager { get; set; }

    /// <summary>
    /// `true` for a built-in system group, whose members follow from the user types rather than from membership
    /// changes. Absent for every other group, never `false`.
    /// </summary>
    /// <example>true</example>
    public bool? IsSystem { get; set; }
}

/// <summary>
/// The flat part of a group.* webhook payload. The system flag, the member count and the manager need services
/// and are filled in by <see cref="GroupWebhookDtoHelper"/>.
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class GroupWebhookDtoMapper
{
    [MapProperty(nameof(ASC.Core.Users.GroupInfo.ID), nameof(GroupWebhookDto.Id))]
    [MapProperty(nameof(ASC.Core.Users.GroupInfo.CategoryID), nameof(GroupWebhookDto.Category))]
    [MapProperty(nameof(ASC.Core.Users.GroupInfo.Parent), nameof(GroupWebhookDto.Parent), Use = nameof(GetParentId))]
    [MapProperty(nameof(ASC.Core.Users.GroupInfo.Sid), nameof(GroupWebhookDto.IsLDAP), Use = nameof(GetIsLDAP))]
    [MapperIgnoreTarget(nameof(GroupWebhookDto.IsSystem))]
    [MapperIgnoreTarget(nameof(GroupWebhookDto.Manager))]
    [MapperIgnoreTarget(nameof(GroupWebhookDto.MembersCount))]
    public static partial GroupWebhookDto MapToWebhookDto(this ASC.Core.Users.GroupInfo source);

    private static Guid? GetParentId(ASC.Core.Users.GroupInfo parent) => parent?.ID;

    private static bool GetIsLDAP(string sid) => !string.IsNullOrEmpty(sid);
}

/// <summary>
/// A group summary of the REST API as a webhook payload references it: the same fields, the same values.
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class GroupSummaryWebhookDtoMapper
{
    public static partial GroupSummaryWebhookDto MapToWebhookDto(this GroupSummaryDto source);
}
