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
/// Which team template the portal is switched to.
/// </summary>
public class SchemaBaseRequestsDto
{
    /// <summary>
    /// The template to apply, by the `id` that `GET api/2.0/settings/customschemas` reports. The reserved id
    /// `custom` selects the template the portal wrote itself, which is edited with
    /// `PUT api/2.0/settings/customschemas`.
    /// </summary>
    /// <example>sales-team-template</example>
    public required string Id { get; init; }
}

/// <summary>
/// A team template: the wording the portal uses for its people, groups and their attributes.
/// </summary>
public class SchemaRequestsDto
{
    /// <summary>
    /// The template this wording belongs to, by the `id` that `GET api/2.0/settings/customschemas` reports. It is
    /// filled in on the way out; the operation that stores a template ignores it and always writes the portal own
    /// `custom` template, so a built-in template cannot be overwritten by naming it here.
    /// </summary>
    /// <example>sales-team-template</example>
    public required string Id { get; init; }

    /// <summary>
    /// The template name shown when the templates are offered for choosing. It is filled in by the portal and is not
    /// stored when a custom template is written.
    /// </summary>
    /// <example>Sales Team</example>
    public string Name { get; set; }

    /// <summary>
    /// What one member of the portal is called. When a template is stored every caption has to be non-empty once
    /// its surrounding whitespace is trimmed, or the whole call is refused with 400, and each is silently cut to 30
    /// characters.
    /// </summary>
    /// <example>User</example>
    public string UserCaption { get; init; }

    /// <summary>
    /// What several members of the portal are called - the plural of `userCaption`, which the interface uses for
    /// lists and counts. It must not be empty and is silently cut to 30 characters.
    /// </summary>
    /// <example>Users</example>
    public string UsersCaption { get; init; }

    /// <summary>
    /// What one group of members is called. It must not be empty and is silently cut to 30 characters.
    /// </summary>
    /// <example>Group</example>
    public string GroupCaption { get; init; }

    /// <summary>
    /// What several groups are called - the plural of `groupCaption`. It must not be empty and is silently cut to
    /// 30 characters.
    /// </summary>
    /// <example>Groups</example>
    public string GroupsCaption { get; init; }

    /// <summary>
    /// What a member job title is called on their profile. It must not be empty and is silently cut to 30
    /// characters.
    /// </summary>
    /// <example>Position</example>
    public string UserPostCaption { get; init; }

    /// <summary>
    /// What the date a member joined the portal is called on their profile. It must not be empty and is silently
    /// cut to 30 characters.
    /// </summary>
    /// <example>Registration Date</example>
    public string RegDateCaption { get; init; }

    /// <summary>
    /// What the member who leads a group is called. It must not be empty and is silently cut to 30 characters.
    /// </summary>
    /// <example>Head</example>
    public string GroupHeadCaption { get; init; }

    /// <summary>
    /// What one person from outside the portal is called. It must not be empty and is silently cut to 30
    /// characters.
    /// </summary>
    /// <example>Guest</example>
    public string GuestCaption { get; init; }

    /// <summary>
    /// What several people from outside the portal are called - the plural of `guestCaption`. It must not be empty
    /// and is silently cut to 30 characters.
    /// </summary>
    /// <example>Guests</example>
    public string GuestsCaption { get; init; }
}
