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
/// The user carried by a user.* webhook.
/// </summary>
/// <remarks>
/// A copy of EmployeeFullDto, deliberately not derived from it: the webhook wire contract is versioned
/// separately from the REST API and the two are free to diverge. Fields answering "what may the caller see"
/// rather than "what is this user" are not copied, because a webhook has no caller - delivery is already gated
/// by WebhookUserAccessChecker against the subscription owner. Session and authentication state is not copied
/// either. Timestamps are UTC, matching the delivery envelope, rather than the tenant-local ApiDateTime the
/// REST API uses.
/// </remarks>
[WebhookPayload(WebhookPayloadKind.User)]
public class UserWebhookDto
{
    /// <summary>
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The HTML-encoded user display name formatted according to the portal default format.
    /// </summary>
    /// <example>Mike Zanyatski</example>
    public string DisplayName { get; set; }

    /// <summary>
    /// The user first name.
    /// </summary>
    /// <example>Mike</example>
    public string FirstName { get; set; }

    /// <summary>
    /// The user last name.
    /// </summary>
    /// <example>Zanyatski</example>
    public string LastName { get; set; }

    /// <summary>
    /// The user name.
    /// </summary>
    /// <example>Mike.Zanyatski</example>
    public string UserName { get; set; }

    /// <summary>
    /// The user email address.
    /// </summary>
    /// <example>mike.zanyatski@example.com</example>
    public string Email { get; set; }

    /// <summary>
    /// The list of user contacts.
    /// </summary>
    public List<ContactWebhookDto> Contacts { get; set; }

    /// <summary>
    /// The user status.
    /// </summary>
    /// <example>Active</example>
    public EmployeeStatus Status { get; set; }

    /// <summary>
    /// The user activation status.
    /// </summary>
    /// <example>Activated</example>
    public EmployeeActivationStatus ActivationStatus { get; set; }

    /// <summary>
    /// The UTC date when the user was terminated.
    /// </summary>
    /// <example>2008-04-10T06:30:00Z</example>
    public DateTime? Terminated { get; set; }

    /// <summary>
    /// The comma-separated names of the groups the user belongs to.
    /// </summary>
    /// <example>Marketing Team, Sales</example>
    public string Department { get; set; }

    /// <summary>
    /// The list of groups the user belongs to.
    /// </summary>
    public List<GroupSummaryWebhookDto> Groups { get; set; }

    /// <summary>
    /// The user location.
    /// </summary>
    /// <example>Palo Alto</example>
    public string Location { get; set; }

    /// <summary>
    /// The user notes.
    /// </summary>
    /// <example>Notes to worker</example>
    public string Notes { get; set; }

    /// <summary>
    /// Specifies whether the user is a DocSpace administrator or not.
    /// </summary>
    /// <example>false</example>
    public bool IsAdmin { get; set; }

    /// <summary>
    /// Specifies whether the user is a room administrator or not.
    /// </summary>
    /// <example>false</example>
    public bool IsRoomAdmin { get; set; }

    /// <summary>
    /// Specifies whether the user is the portal owner or not.
    /// </summary>
    /// <example>false</example>
    public bool IsOwner { get; set; }

    /// <summary>
    /// Specifies whether the user is a guest or not.
    /// </summary>
    /// <example>false</example>
    public bool IsVisitor { get; set; }

    /// <summary>
    /// Specifies whether the user is a user with limited rights or not.
    /// </summary>
    /// <example>false</example>
    public bool IsCollaborator { get; set; }

    /// <summary>
    /// Specifies whether the user comes from LDAP or not.
    /// </summary>
    /// <example>false</example>
    public bool IsLDAP { get; set; }

    /// <summary>
    /// Specifies whether the user comes from SSO or not.
    /// </summary>
    /// <example>false</example>
    public bool IsSSO { get; set; }

    /// <summary>
    /// The list of the modules the user administers.
    /// </summary>
    /// <example>["e67be73d-f9ae-4ce1-8fec-1880cb518cb4"]</example>
    public List<string> ListAdminModules { get; set; }

    /// <summary>
    /// The user culture code.
    /// </summary>
    /// <example>en-US</example>
    public string CultureName { get; set; }

    /// <summary>
    /// The user mobile phone number.
    /// </summary>
    /// <example>+1 916 555 0100</example>
    public string MobilePhone { get; set; }

    /// <summary>
    /// The mobile phone activation status.
    /// </summary>
    /// <example>Activated</example>
    public MobilePhoneActivationStatus MobilePhoneActivationStatus { get; set; }

    /// <summary>
    /// The user quota limit in bytes.
    /// </summary>
    /// <example>0</example>
    public long? QuotaLimit { get; set; }

    /// <summary>
    /// The portal space used by the user in bytes.
    /// </summary>
    /// <example>12345</example>
    public double? UsedSpace { get; set; }

    /// <summary>
    /// Specifies whether the user has a custom quota or not.
    /// </summary>
    /// <example>false</example>
    public bool? IsCustomQuota { get; set; }

    /// <summary>
    /// The user who created this user.
    /// </summary>
    public UserSummaryWebhookDto CreatedBy { get; set; }

    /// <summary>
    /// The UTC date when the user was registered.
    /// </summary>
    /// <example>2008-04-10T06:30:00Z</example>
    public DateTime? RegistrationDate { get; set; }

    /// <summary>
    /// Specifies whether the user has an avatar or not.
    /// </summary>
    /// <example>true</example>
    public bool HasAvatar { get; set; }

    /// <summary>
    /// The user avatar.
    /// </summary>
    /// <example>https://example.com/avatar.jpg</example>
    public string Avatar { get; set; }

    /// <summary>
    /// The user original size avatar.
    /// </summary>
    /// <example>https://example.com/avatar_original.jpg</example>
    public string AvatarOriginal { get; set; }

    /// <summary>
    /// The user maximum size avatar.
    /// </summary>
    /// <example>https://example.com/avatar_max.jpg</example>
    public string AvatarMax { get; set; }

    /// <summary>
    /// The user medium size avatar.
    /// </summary>
    /// <example>https://example.com/avatar_medium.jpg</example>
    public string AvatarMedium { get; set; }

    /// <summary>
    /// The user small size avatar.
    /// </summary>
    /// <example>https://example.com/avatar_small.jpg</example>
    public string AvatarSmall { get; set; }

    /// <summary>
    /// The user profile URL.
    /// </summary>
    /// <example>https://example.com/profile/user123</example>
    public string ProfileUrl { get; set; }
}

/// <summary>
/// A user referenced from another webhook payload.
/// </summary>
public class UserSummaryWebhookDto
{
    /// <summary>
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The HTML-encoded user display name.
    /// </summary>
    /// <example>Mike Zanyatski</example>
    public string DisplayName { get; set; }

    /// <summary>
    /// The user name.
    /// </summary>
    /// <example>Mike.Zanyatski</example>
    public string UserName { get; set; }

    /// <summary>
    /// The user email address.
    /// </summary>
    /// <example>mike.zanyatski@example.com</example>
    public string Email { get; set; }

    /// <summary>
    /// The user avatar.
    /// </summary>
    /// <example>https://example.com/avatar.jpg</example>
    public string Avatar { get; set; }

    /// <summary>
    /// The user profile URL.
    /// </summary>
    /// <example>https://example.com/profile/user123</example>
    public string ProfileUrl { get; set; }
}

/// <summary>
/// A user contact.
/// </summary>
public class ContactWebhookDto
{
    /// <summary>
    /// The contact type.
    /// </summary>
    /// <example>mail</example>
    public string Type { get; set; }

    /// <summary>
    /// The contact value.
    /// </summary>
    /// <example>mike.zanyatski@example.com</example>
    public string Value { get; set; }
}
