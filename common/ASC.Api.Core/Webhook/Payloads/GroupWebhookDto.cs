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

/// <summary>
/// The group carried by a group.* webhook.
/// </summary>
/// <remarks>
/// A copy of GroupDto, deliberately not derived from it. The member list is not carried: a group can hold
/// thousands of users and every one of them would be expanded into the body of every group event. Receivers
/// that need the roster should call GET api/2.0/group/{id} with MembersCount as the hint that it changed.
/// </remarks>
[WebhookPayload(WebhookPayloadKind.Group)]
public class GroupWebhookDto
{
    /// <summary>
    /// The group ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The group name.
    /// </summary>
    /// <example>Marketing Team</example>
    public string Name { get; set; }

    /// <summary>
    /// The parent group ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid? Parent { get; set; }

    /// <summary>
    /// The group category ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid Category { get; set; }

    /// <summary>
    /// Specifies whether the group comes from LDAP or not.
    /// </summary>
    /// <example>false</example>
    public bool IsLDAP { get; set; }

    /// <summary>
    /// Specifies whether the group is a system group or not.
    /// </summary>
    /// <example>false</example>
    public bool? IsSystem { get; set; }

    /// <summary>
    /// The group manager.
    /// </summary>
    public UserSummaryWebhookDto Manager { get; set; }

    /// <summary>
    /// The number of group members.
    /// </summary>
    /// <example>0</example>
    public int MembersCount { get; set; }
}

/// <summary>
/// A group referenced from another webhook payload.
/// </summary>
public class GroupSummaryWebhookDto
{
    /// <summary>
    /// The group ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The group name.
    /// </summary>
    /// <example>Marketing Team</example>
    public string Name { get; set; }

    /// <summary>
    /// The user name of the group manager.
    /// </summary>
    /// <example>Mike.Zanyatski</example>
    public string Manager { get; set; }

    /// <summary>
    /// Specifies whether the group is a system group or not.
    /// </summary>
    /// <example>false</example>
    public bool? IsSystem { get; set; }
}
