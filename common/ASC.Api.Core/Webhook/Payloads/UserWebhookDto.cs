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

// A copy of EmployeeFullDto, deliberately not derived from it: the webhook wire contract is versioned separately from
// the REST API and the two are free to diverge. Delivery is gated by WebhookUserAccessChecker against the subscription
// owner, and the fields the REST API shows by the reader's role (groups, quota, CreatedBy) are cut by the same rules
// for that owner in UserWebhookDtoHelper.RestrictForSubscriberAsync. Timestamps are UTC, matching the delivery
// envelope, rather than the tenant-local ApiDateTime the REST API uses.
// Not carried, deliberately: sid, ssoNameId and ssoSessionId (the LDAP and SAML identifiers and a SAML session ID, all
// of which the domain entity used to put on the wire), loginEventId, authCookieLifetime, tfaAppEnabled, theme, isAnonim
// and shared. `contacts` appears once, as a typed list, rather than twice in two shapes.

/// <summary>
/// The user a user.* webhook is about.
/// </summary>
/// <remarks>
/// All dates are in UTC. The quota fields go only to a DocSpace admin and to the user themselves, and the author of the
/// account only to a DocSpace admin. A subscription owner of the `User` or `Guest` type, who may not see the contacts of
/// other users, gets someone else's profile without the groups, the department and the profile link.
/// </remarks>
public class UserWebhookDto
{
    /// <summary>
    /// The ID of the user the event is about; `GET api/2.0/people/{userid}` returns the full profile by it.
    /// </summary>
    /// <example>9924256a-739c-462b-be1a-e1d3a1d6f2d4</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The user's full name in the name order set for the portal, HTML-encoded.
    /// </summary>
    /// <example>Mike Zanyatski</example>
    public string DisplayName { get; set; }

    /// <summary>
    /// The user's first name as entered in the profile.
    /// </summary>
    /// <example>Mike</example>
    public string FirstName { get; set; }

    /// <summary>
    /// The user's last name as entered in the profile.
    /// </summary>
    /// <example>Zanyatski</example>
    public string LastName { get; set; }

    /// <summary>
    /// The unique login name of the user, made from the local part of their email address when the account is created.
    /// Absent when the subscription's creator is a User or a Guest, who may not see the contacts of other users, unless
    /// this user is that creator.
    /// </summary>
    /// <example>mike.zanyatski</example>
    public string UserName { get; set; }

    /// <summary>
    /// The email address the user signs in with and receives notifications at. Absent when the subscription's creator
    /// is a User or a Guest, who may not see the contacts of other users, unless this user is that creator.
    /// </summary>
    /// <example>mike.zanyatski@example.com</example>
    [EmailAddress]
    public string Email { get; set; }

    /// <summary>
    /// The additional contacts from the user's profile, such as extra phone numbers, emails and social links. Absent
    /// when the profile has none, and when the subscription's creator is a User or a Guest, who may not see the
    /// contacts of other users, unless this user is that creator.
    /// </summary>
    public List<ContactWebhookDto> Contacts { get; set; }

    /// <summary>
    /// Whether the account is active or disabled. A disabled user cannot sign in; `terminated` says since when.
    /// </summary>
    /// <example>Active</example>
    public EmployeeStatus Status { get; set; }

    /// <summary>
    /// Whether the user has confirmed their email address. The `AutoGenerated` flag is never set here.
    /// </summary>
    /// <example>Activated</example>
    public EmployeeActivationStatus ActivationStatus { get; set; }

    /// <summary>
    /// When the account was disabled, in UTC. Absent while the account is active.
    /// </summary>
    /// <example>2008-04-10T06:30:00Z</example>
    public DateTime? Terminated { get; set; }

    /// <summary>
    /// The comma-separated names of the groups the user belongs to, as plain text rather than HTML-encoded; empty when
    /// the user is in no group. Omitted when the subscription owner is a `User` or a `Guest` and the event is about
    /// someone else.
    /// </summary>
    /// <example>Marketing Team, Sales</example>
    public string Department { get; set; }

    /// <summary>
    /// The groups the user belongs to; absent when the user is in no group. Omitted as well when the subscription owner
    /// is a `User` or a `Guest` and the event is about someone else.
    /// </summary>
    public List<GroupSummaryWebhookDto> Groups { get; set; }

    /// <summary>
    /// The location the user entered in the profile; empty or absent when not filled in.
    /// </summary>
    /// <example>Palo Alto</example>
    public string Location { get; set; }

    /// <summary>
    /// The free-text notes on the user's profile; empty or absent when not filled in.
    /// </summary>
    /// <example>Notes to worker</example>
    public string Notes { get; set; }

    /// <summary>
    /// True when the user is a DocSpace admin, who manages the portal settings and every room. False for the portal
    /// owner, who has every admin right as well: read `isOwner` for that user.
    /// </summary>
    /// <example>false</example>
    public bool IsAdmin { get; set; }

    /// <summary>
    /// True when the user's type is Room admin, who may create rooms and manage the rooms they own.
    /// </summary>
    /// <example>false</example>
    public bool IsRoomAdmin { get; set; }

    /// <summary>
    /// True for the portal owner, the single account that owns the portal.
    /// </summary>
    /// <example>false</example>
    public bool IsOwner { get; set; }

    /// <summary>
    /// True when the user's type is Guest, an external account that works only in the rooms it was invited to.
    /// </summary>
    /// <example>false</example>
    public bool IsVisitor { get; set; }

    /// <summary>
    /// True when the user's type is User, who works in the rooms they are invited to but cannot create rooms.
    /// </summary>
    /// <example>false</example>
    public bool IsCollaborator { get; set; }

    /// <summary>
    /// True when the account is synchronized from an LDAP directory, which then owns its name and email.
    /// </summary>
    /// <example>false</example>
    public bool IsLDAP { get; set; }

    /// <summary>
    /// True when the account came from single sign-on and signs in through the identity provider.
    /// </summary>
    /// <example>false</example>
    public bool IsSSO { get; set; }

    /// <summary>
    /// The IDs of the product modules the user administers. Absent when the user administers none.
    /// </summary>
    /// <example>["e67be73d-f9ae-4ce1-8fec-1880cb518cb4"]</example>
    public List<string> ListAdminModules { get; set; }

    /// <summary>
    /// The interface language the user chose, as a culture code. Absent when the user follows the portal's language.
    /// </summary>
    /// <example>en-US</example>
    public string CultureName { get; set; }

    /// <summary>
    /// The mobile phone number from the user's profile; absent when none is set, and when the subscription's creator
    /// is a User or a Guest, who may not see the contacts of other users, unless this user is that creator.
    /// </summary>
    /// <example>+1 916 555 0100</example>
    public string MobilePhone { get; set; }

    /// <summary>
    /// Whether the mobile phone number has been confirmed, as SMS sign-in confirmation requires.
    /// </summary>
    /// <example>Activated</example>
    public MobilePhoneActivationStatus MobilePhoneActivationStatus { get; set; }

    /// <summary>
    /// How much storage the user may use, in bytes; -1 when the limit is switched off for this user. Absent when user
    /// quotas are turned off on the portal or the portal keeps no storage statistics, and for any subscription owner
    /// other than a `DocSpaceAdmin` or the user themselves.
    /// </summary>
    /// <example>10737418240</example>
    public long? QuotaLimit { get; set; }

    /// <summary>
    /// How much storage the user's files take, in bytes. Absent when the portal keeps no storage statistics, and for
    /// any subscription owner other than a `DocSpaceAdmin` or the user themselves.
    /// </summary>
    /// <example>12345</example>
    public double? UsedSpace { get; set; }

    /// <summary>
    /// True when `quotaLimit` was set for this user individually, false when it is the portal's default user quota.
    /// Absent whenever `quotaLimit` is absent because quotas are off or statistics are not kept, and for any
    /// subscription owner other than a `DocSpaceAdmin` or the user themselves.
    /// </summary>
    /// <example>false</example>
    public bool? IsCustomQuota { get; set; }

    /// <summary>
    /// The user who created this account. Sent only to a `DocSpaceAdmin`, and absent when that user has been deleted.
    /// </summary>
    public UserSummaryWebhookDto CreatedBy { get; set; }

    /// <summary>
    /// When the account was created, in UTC.
    /// </summary>
    /// <example>2008-04-10T06:30:00Z</example>
    public DateTime? RegistrationDate { get; set; }

    /// <summary>
    /// False when the user has not uploaded a photo; the avatar URLs then point to the default picture.
    /// </summary>
    /// <example>true</example>
    public bool HasAvatar { get; set; }

    /// <summary>
    /// The absolute URL of the 82x82 avatar, with a hash query parameter that changes when the profile does.
    /// </summary>
    /// <example>https://example.com/avatar.jpg</example>
    public string Avatar { get; set; }

    /// <summary>
    /// The absolute URL of the original avatar, with the same hash parameter.
    /// </summary>
    /// <example>https://example.com/avatar_original.jpg</example>
    public string AvatarOriginal { get; set; }

    /// <summary>
    /// The absolute URL of the 200x200 avatar, with the same hash parameter.
    /// </summary>
    /// <example>https://example.com/avatar_max.jpg</example>
    public string AvatarMax { get; set; }

    /// <summary>
    /// The absolute URL of the 48x48 avatar, with the same hash parameter.
    /// </summary>
    /// <example>https://example.com/avatar_medium.jpg</example>
    public string AvatarMedium { get; set; }

    /// <summary>
    /// The absolute URL of the 32x32 avatar, with the same hash parameter.
    /// </summary>
    /// <example>https://example.com/avatar_small.jpg</example>
    public string AvatarSmall { get; set; }

    /// <summary>
    /// The absolute link to the user in the portal's list of people, or of guests for a guest; today it is a search
    /// of that list by the user's email address. Absent when the subscription's creator is a User or a Guest, who may
    /// not see the contacts of other users, unless this user is that creator.
    /// </summary>
    /// <example>https://example.onlyoffice.com/accounts/people/filter?search=mike.zanyatski%40example.com</example>
    public string ProfileUrl { get; set; }

    internal UserWebhookDto Clone()
    {
        return (UserWebhookDto)MemberwiseClone();
    }
}

/// <summary>
/// A short reference to a user from another payload: the author or the last editor of a file, folder or room, the
/// manager of a group, or the creator of an account.
/// </summary>
public class UserSummaryWebhookDto
{
    /// <summary>
    /// The ID of the referenced user; `GET api/2.0/people/{userid}` returns the full profile by it.
    /// </summary>
    /// <example>9924256a-739c-462b-be1a-e1d3a1d6f2d4</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The user's full name in the name order set for the portal, HTML-encoded.
    /// </summary>
    /// <example>Mike Zanyatski</example>
    public string DisplayName { get; set; }

    /// <summary>
    /// The unique login name of the user, made from the local part of their email address when the account is created.
    /// Absent when the subscription's creator is a User or a Guest, who may not see the contacts of other users, unless
    /// this user is that creator.
    /// </summary>
    /// <example>mike.zanyatski</example>
    public string UserName { get; set; }

    /// <summary>
    /// The absolute URL of the 82x82 avatar, with a hash query parameter that changes when the profile does.
    /// </summary>
    /// <example>https://example.com/avatar.jpg</example>
    public string Avatar { get; set; }

    /// <summary>
    /// The absolute link to the user in the portal's list of people, or of guests for a guest; today it is a search
    /// of that list by the user's email address. Absent when the subscription's creator is a User or a Guest, who may
    /// not see the contacts of other users, unless this user is that creator.
    /// </summary>
    /// <example>https://example.onlyoffice.com/accounts/people/filter?search=mike.zanyatski%40example.com</example>
    public string ProfileUrl { get; set; }

    internal UserSummaryWebhookDto Clone()
    {
        return (UserSummaryWebhookDto)MemberwiseClone();
    }
}

/// <summary>
/// The flat part of a user summary. The display name, the avatar and the profile link need services and are filled in
/// by <see cref="UserWebhookDtoHelper.GetSummaryAsync(UserInfo)"/>.
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class UserSummaryWebhookDtoMapper
{
    [MapperIgnoreTarget(nameof(UserSummaryWebhookDto.DisplayName))]
    [MapperIgnoreTarget(nameof(UserSummaryWebhookDto.Avatar))]
    [MapperIgnoreTarget(nameof(UserSummaryWebhookDto.ProfileUrl))]
    public static partial UserSummaryWebhookDto MapToSummaryWebhookDto(this UserInfo source);
}

/// <summary>
/// An additional contact from a user's profile.
/// </summary>
public class ContactWebhookDto
{
    /// <summary>
    /// The kind of contact as stored in the profile, such as `mail`, `phone` or `facebook`.
    /// </summary>
    /// <example>mail</example>
    public string Type { get; set; }

    /// <summary>
    /// The contact itself: an address, a number or a profile link, as the user entered it.
    /// </summary>
    /// <example>mike.zanyatski@example.com</example>
    public string Value { get; set; }
}

/// <summary>
/// The flat part of a user.* webhook payload. Names, roles, groups, avatars, quota and the author need services
/// and are filled in by <see cref="UserWebhookDtoHelper"/>.
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class UserWebhookDtoMapper
{
    [MapProperty(nameof(UserInfo.ActivationStatus), nameof(UserWebhookDto.ActivationStatus), Use = nameof(GetActivationStatus))]
    [MapProperty(nameof(UserInfo.TerminatedDate), nameof(UserWebhookDto.Terminated))]
    [MapProperty(nameof(UserInfo.CreateDate), nameof(UserWebhookDto.RegistrationDate))]
    [MapProperty(nameof(UserInfo.ContactsList), nameof(UserWebhookDto.Contacts), Use = nameof(GetContacts))]
    [MapPropertyFromSource(nameof(UserWebhookDto.IsLDAP), Use = nameof(GetIsLDAP))]
    [MapPropertyFromSource(nameof(UserWebhookDto.IsSSO), Use = nameof(GetIsSSO))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.DisplayName))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.Department))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.Groups))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.IsAdmin))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.IsRoomAdmin))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.IsOwner))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.IsVisitor))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.IsCollaborator))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.ListAdminModules))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.CultureName))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.QuotaLimit))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.UsedSpace))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.IsCustomQuota))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.CreatedBy))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.HasAvatar))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.Avatar))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.AvatarOriginal))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.AvatarMax))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.AvatarMedium))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.AvatarSmall))]
    [MapperIgnoreTarget(nameof(UserWebhookDto.ProfileUrl))]
    public static partial UserWebhookDto MapToWebhookDto(this UserInfo source);

    private static EmployeeActivationStatus GetActivationStatus(EmployeeActivationStatus status) => status & ~EmployeeActivationStatus.AutoGenerated;

    private static bool GetIsLDAP(UserInfo source) => source.IsLDAP();

    private static bool GetIsSSO(UserInfo source) => source.IsSSO();

    /// <summary>
    /// ContactsList stores the contacts flat, as type, value, type, value. No contacts gives null, not an empty list.
    /// </summary>
    private static List<ContactWebhookDto> GetContacts(List<string> contactsList)
    {
        if (contactsList == null)
        {
            return null;
        }

        var contacts = new List<ContactWebhookDto>();

        for (var i = 0; i + 1 < contactsList.Count; i += 2)
        {
            contacts.Add(new ContactWebhookDto { Type = contactsList[i], Value = contactsList[i + 1] });
        }

        return contacts.Count > 0 ? contacts : null;
    }
}
