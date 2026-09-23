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

using ASC.Files.Core;

namespace ASC.Web.Api.ApiModel.RequestsDto;

/// <summary>
/// The portal member named as the new owner of the portal.
/// </summary>
/// <example>
/// {
///   "ownerId": "00000000-0000-0000-0000-000000000001"
/// }
/// </example>
public class OwnerIdSettingsRequestDto
{
    /// <summary>
    /// The member who is to become the portal owner, by user ID. They have to be an active member of this portal and
    /// not a guest; a member who is not a DocSpace administrator yet is promoted to one as part of the transfer, so
    /// the portal needs a paid seat for them.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    public required Guid OwnerId { get; set; }
}

/// <summary>
/// The portal version the tenant is switched to.
/// </summary>
/// <example>
/// {
///   "versionId": 2
/// }
/// </example>
public class SettingsRequestsDto
{
    /// <summary>
    /// The version to put the tenant on, by version ID. It has to be one of the versions
    /// `GET api/2.0/settings/version` reports for this installation; any other value answers 404.
    /// </summary>
    /// <example>2</example>
    public required int VersionId { get; set; }
}

/// <summary>
/// Whether the interface tips are shown to the calling user.
/// </summary>
/// <example>
/// {
///   "show": true
/// }
/// </example>
public class TipsRequestDto
{
    /// <summary>
    /// Whether the interface tips are shown. The setting belongs to the calling account alone and never affects
    /// anybody else; switching the tips off also unsubscribes that account from the tips mailing.
    /// </summary>
    /// <example>true</example>
    public bool Show { get; set; } //tips
}

/// <summary>
/// The section the calling user's account opens into after signing in.
/// </summary>
/// <example>
/// {
///   "defaultFolderType": 1
/// }
/// </example>
public class DefaultProductRequestDto
{
    /// <summary>
    /// The section to land on. Only the folder types the client offers as a landing page are accepted - the rooms
    /// list, My documents, shared with me, favorites, recent, forms and the AI agents folder - and anything else is
    /// refused. My documents is refused for a guest as well, since a guest has no personal storage.
    /// </summary>
    /// <example>1</example>
    public required FolderType DefaultFolderType { get; set; }
}

/// <summary>
/// The portal interface language and time zone, set together.
/// </summary>
/// <example>
/// {
///   "lng": "en",
///   "timeZoneID": "America/New_York"
/// }
/// </example>
public class TimeZoneRequestDto
{
    /// <summary>
    /// The portal interface language, as a culture name such as `en-US` or a bare language such as `en`. It has to
    /// be one of the cultures enabled for the installation; a culture that is not enabled leaves the language as it
    /// was instead of failing the call.
    /// </summary>
    /// <example>en</example>
    public required string Lng { get; set; }

    /// <summary>
    /// The time zone every portal date is rendered in, as an IANA identifier such as `America/New_York`. A Windows
    /// identifier is converted to its IANA equivalent, and a value that matches nothing falls back to UTC rather
    /// than failing the call.
    /// </summary>
    /// <example>America/New_York</example>
    public string TimeZoneID { get; set; }
}

/// <summary>
/// Whether the `User` role is barred from the portal developer tools.
/// </summary>
/// <example>
/// {
///   "limitedAccessForUsers": false
/// }
/// </example>
public class TenantDevToolsAccessSettingsDto
{
    /// <summary>
    /// Whether members holding the `User` role are barred from the developer tools - API keys, OAuth applications
    /// and webhooks. Room administrators and DocSpace administrators keep their access either way.
    /// </summary>
    /// <example>false</example>
    public bool LimitedAccessForUsers { get; set; }
}

/// <summary>
/// Whether the portal promotional banners are hidden.
/// </summary>
/// <example>
/// {
///   "hidden": true
/// }
/// </example>
public class TenantBannerSettingsDto
{
    /// <summary>
    /// Whether the promotional banners are hidden from every user of the portal. The flag is only honoured on a
    /// self-hosted installation; a SaaS portal keeps showing the banners whatever is stored here.
    /// </summary>
    /// <example>true</example>
    public bool Hidden { get; set; }
}

/// <summary>
/// Whether AI functionality is available on the portal.
/// </summary>
/// <example>
/// {
///   "enabled": false
/// }
/// </example>
public class TenantAiAccessSettingsDto
{
    /// <summary>
    /// Whether AI is available on the portal at all - chat, agents and vectorization together. Switching it off
    /// hides the AI Agents folder and makes every AI endpoint unreachable for all members at once, not only for the
    /// caller, and the change is pushed to connected clients rather than waiting for their next request.
    /// </summary>
    /// <example>false</example>
    public bool Enabled { get; set; }
}
