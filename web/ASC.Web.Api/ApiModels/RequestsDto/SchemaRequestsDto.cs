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

namespace ASC.Web.Api.ApiModel.RequestsDto;

/// <summary>
/// The request parameters for the team template identification.
/// </summary>
public class SchemaBaseRequestsDto
{
    /// <summary>
    /// The team template ID.
    /// </summary>
    /// <example>sales-team-template</example>
    public required string Id { get; init; }
}

/// <summary>
/// The request parameters for the comprehensive team template configuration.
/// </summary>
public class SchemaRequestsDto
{
    /// <summary>
    /// The team template ID.
    /// </summary>
    /// <example>sales-team-template</example>
    public required string Id { get; init; }

    /// <summary>
    /// The display name for the team template.
    /// </summary>
    /// <example>Sales Team</example>
    public string Name { get; set; }

    /// <summary>
    /// The label for the single user references.
    /// </summary>
    /// <example>User</example>
    public string UserCaption { get; init; }

    /// <summary>
    /// The label for the multiple user references.
    /// </summary>
    /// <example>Users</example>
    public string UsersCaption { get; init; }

    /// <summary>
    /// The label for the single group references.
    /// </summary>
    /// <example>Group</example>
    public string GroupCaption { get; init; }

    /// <summary>
    /// The label for the multiple group references.
    /// </summary>
    /// <example>Groups</example>
    public string GroupsCaption { get; init; }

    /// <summary>
    /// The label for the user position or status.
    /// </summary>
    /// <example>Position</example>
    public string UserPostCaption { get; init; }

    /// <summary>
    /// The label for the member registration date.
    /// </summary>
    /// <example>Registration Date</example>
    public string RegDateCaption { get; init; }

    /// <summary>
    /// The label for the group leader position.
    /// </summary>
    /// <example>Head</example>
    public string GroupHeadCaption { get; init; }

    /// <summary>
    /// The label for the single guest/external user references.
    /// </summary>
    /// <example>Guest</example>
    public string GuestCaption { get; init; }

    /// <summary>
    /// The label for the multiple guest/external user references.
    /// </summary>
    /// <example>Guests</example>
    public string GuestsCaption { get; init; }
}