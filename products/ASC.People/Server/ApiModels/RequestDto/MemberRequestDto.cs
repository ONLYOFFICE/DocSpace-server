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
/// The request parameters for the user email.
/// </summary>
public class EmailMemberRequestDto
{
    /// <summary>
    /// The address to send the password recovery link to. It is required and validated even by
    /// `POST api/2.0/people/guests/share/approve`, which then ignores its value and takes the account from the
    /// confirmation token instead.
    /// </summary>
    /// <example>john.doe@example.com</example>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    /// <summary>
    /// Which CAPTCHA the `recaptchaResponse` comes from: `Default` for the web reCAPTCHA, `AndroidV2` or `iOSV2` for
    /// the mobile ones, and `hCaptcha` when the portal is configured with hCaptcha. It matters only for an
    /// unauthenticated request on a portal that has a CAPTCHA.
    /// </summary>
    /// <example>Default</example>
    public RecaptchaType RecaptchaType { get; set; }

    /// <summary>
    /// The user's response to the CAPTCHA challenge.
    /// </summary>
    /// <example>03AGdBq27...</example>
    public string RecaptchaResponse { get; set; }
}

/// <summary>
/// The request parameters for updating a user password.
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// The new password in plain text. It is checked against the portal password policy and rejected with 400 when
    /// it is too weak, then hashed by the portal. Send it only over a secure connection, and prefer `passwordHash`
    /// when the client can compute it.
    /// </summary>
    /// <example>P@ssw0rd</example>
    public string Password { get; set; }

    /// <summary>
    /// The new password already hashed by the client, which is what the portal stores. It is a PBKDF2-HMACSHA256
    /// hash of the plain password, computed with the salt, the iteration count and the key size the portal settings
    /// publish, and written as lowercase hexadecimal. When it is sent, `password` is ignored and the password policy
    /// is not applied.
    /// </summary>
    /// <example>c1ba1a0bcbe0f0f42b6c86e1b41a1b4a4a9b4b0e3f2b7d2c1a0e9f8d7c6b5a49</example>
    public string PasswordHash { get; set; }
}

/// <summary>
/// The request parameters for updating a user password by their ID.
/// </summary>
public class ChangePasswordByIdRequestDto
{
    /// <summary>
    /// The ID of the account whose password is set, taken from the route. It has to match the account the
    /// confirmation token was issued for, and the account has to be active.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "userid")]
    public required Guid UserId { get; set; }

    /// <summary>
    /// The new password, sent either in plain text or already hashed. Exactly one of the two fields is needed.
    /// </summary>
    /// <example>{"password": "P@ssw0rd"}</example>
    [FromBody]
    public required ChangePasswordRequest ChangePasswordData { get; set; }
}

/// <summary>
/// The request parameters for updating a user email.
/// </summary>
public class ChangeEmailRequest
{
    /// <summary>
    /// The new address in plain text, up to 255 characters. It is stored in lowercase, and one of this field and
    /// `encEmail` is required.
    /// </summary>
    /// <example>john.doe@example.com</example>
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    /// <summary>
    /// The new address in the encrypted form the confirmation link carries. Pass the value from the link unchanged;
    /// it is used only when `email` is empty.
    /// </summary>
    /// <example>encrypted_email_string</example>
    public string EncEmail { get; init; }
}

/// <summary>
/// The request parameters for updating a user email by their ID.
/// </summary>
public class ChangeEmailByIdRequestDto
{
    /// <summary>
    /// The ID of the account whose address is set, taken from the route. It has to match the account the
    /// confirmation token was issued for, and the account has to be active.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "userid")]
    public required Guid UserId { get; set; }

    /// <summary>
    /// The new address, in plain text or in the encrypted form the confirmation link carries.
    /// </summary>
    /// <example>{"email": "john.doe@example.com"}</example>
    [FromBody]
    public required ChangeEmailRequest ChangeEmailData { get; set; }
}

/// <summary>
/// The user request parameters.
/// </summary>
public class MemberRequestDto
{
    /// <summary>
    /// The password in plain text. It is checked against the portal password policy and rejected with 400 when it is
    /// too weak. When neither this field nor `passwordHash` is sent, a random password is generated and nobody
    /// learns it, so the account can only be used after a password recovery.
    /// </summary>
    /// <example>P@ssw0rd</example>
    public string Password { get; set; }

    /// <summary>
    /// The password already hashed by the client, which is what the portal stores. It is a PBKDF2-HMACSHA256 hash of
    /// the plain password, computed with the salt, the iteration count and the key size the portal settings publish,
    /// and written as lowercase hexadecimal. When it is sent, `password` is ignored and the password policy is not
    /// applied.
    /// </summary>
    /// <example>c1ba1a0bcbe0f0f42b6c86e1b41a1b4a4a9b4b0e3f2b7d2c1a0e9f8d7c6b5a49</example>
    public string PasswordHash { get; set; }

    /// <summary>
    /// The email address of the new account, up to 255 characters. It is required in practice and has to be a real
    /// address, and it becomes the sign-in name of the account.
    /// </summary>
    /// <example>john.doe@example.com</example>
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    /// <summary>
    /// The type of the new account: `User`, `RoomAdmin` or `DocSpaceAdmin`. `Guest` is not accepted here, and the
    /// value is ignored entirely when `fromInviteLink` is set, because the invitation link decides the type. When no
    /// paid seat is free, the account is created as `User` whatever was asked for.
    /// </summary>
    /// <example>RoomAdmin</example>
    public EmployeeType Type { get; set; }

    /// <summary>
    /// Only chooses which entry the operation writes to the audit trail - the one for a guest or the one for a
    /// member. It does not change the type of the account; `type` and the invitation link do that.
    /// </summary>
    /// <example>true</example>
    public bool? IsUser { get; set; }

    /// <summary>
    /// The first name, up to 255 characters. It is checked together with `lastName`, and a pair the portal does not
    /// accept as a name answers 400.
    /// </summary>
    /// <example>John</example>
    [StringLength(255)]
    public string FirstName { get; set; }

    /// <summary>
    /// The last name, up to 255 characters. It is checked together with `firstName`, and a pair the portal does not
    /// accept as a name answers 400.
    /// </summary>
    /// <example>Doe</example>
    [StringLength(255)]
    public string LastName { get; set; }

    /// <summary>
    /// The groups to put the new account into, by group ID. Read the IDs from `GET api/2.0/group`; an ID that
    /// matches no group is skipped without an error.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000"]</example>
    public Guid[] Department { get; set; }

    // /// <summary>
    // /// The user title.
    // /// </summary>
    // /// <example>Manager</example>
    // [StringLength(255)]
    // public string Title { get; set; }

    /// <summary>
    /// The free-text location shown on the profile. It is stored as it is given and is not validated.
    /// </summary>
    /// <example>New York</example>
    public string Location { get; set; }

    // /// <summary>
    // /// The user sex (male or female).
    // /// </summary>
    // /// <example>1</example>
    // public SexEnum? Sex { get; set; }

    // /// <summary>
    // /// The user birthday.
    // /// </summary>
    // /// <example>2025-01-01T00:00:00Z</example>
    // public ApiDateTime Birthday { get; set; }

    // /// <summary>
    // /// The user registration date (if it is not specified, then the current date will be set).
    // /// </summary>
    // /// <example>2025-01-01T00:00:00Z</example>
    // public ApiDateTime Worksfrom { get; set; }

    /// <summary>
    /// The free-text note kept with the profile, shown to administrators. It is stored as it is given.
    /// </summary>
    /// <example>User comment</example>
    public string Comment { get; set; }

    /// <summary>
    /// The additional ways to reach the person, each as a type and a value pair. The type is a free-text label such
    /// as `email`, `phone`, `skype` or `telegram`, and an entry with an empty value is dropped.
    /// </summary>
    /// <example>[{"type": "email", "value": "john.doe@example.com"}]</example>
    public IEnumerable<Contact> Contacts { get; set; }

    /// <summary>
    /// The address the portal downloads the avatar from. It has to use HTTPS unless the request itself came over
    /// HTTP, an address the portal refuses to fetch is rejected, and passing the default avatar path means no
    /// avatar is downloaded.
    /// </summary>
    /// <example>https://example.com/avatar.jpg</example>
    public string Files { get; set; }

    /// <summary>
    /// Set it to true when the account is created by somebody accepting an invitation, which makes `key` required
    /// and lets the link decide the type. With the default false the caller has to hold the permission to add an
    /// account of the requested type.
    /// </summary>
    /// <example>false</example>
    public bool FromInviteLink { get; set; }

    /// <summary>
    /// The key of the invitation link being accepted, taken from the link itself. It is read only when
    /// `fromInviteLink` is true, and an expired or already used key answers 403.
    /// </summary>
    /// <example>user_key_string</example>
    public string Key { get; set; }

    /// <summary>
    /// The interface language of the new account, as a culture code. It is applied whether or not the portal has
    /// that culture enabled, so send a code the portal supports.
    /// </summary>
    /// <example>en-US</example>
    public string CultureName { get; set; }

    /// <summary>
    /// Not used. The handler reads nothing from this field, and it is kept only so that existing clients keep
    /// working.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid Target { get; set; }

    /// <summary>
    /// Whether the account agrees to receive tips, updates and offers. It defaults to false, which means no such
    /// mail is sent.
    /// </summary>
    /// <example>false</example>
    public bool? Spam { get; set; }
}

/// <summary>
/// The request parameters for updating the user information.
/// </summary>
public class UpdateMemberRequestDto
{
    /// <summary>
    /// The account the change applies to. It is read from this body by `POST api/2.0/people/email`, while
    /// `PUT api/2.0/people/{userid}` takes the account from the route and ignores this field.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public string UserId { get; set; }

    /// <summary>
    /// Set it to true to give the account the `Terminated` status and end every session it has, and to false to
    /// bring it back. It is applied only when the caller edits somebody else, and omitting it keeps the current
    /// status.
    /// </summary>
    /// <example>false</example>
    public bool? Disable { get; set; }

    /// <summary>
    /// The new email address, up to 255 characters. It is read only by `POST api/2.0/people/email`, which either
    /// mails a confirmation letter or, for an administrator acting on somebody else, applies the address at once;
    /// `PUT api/2.0/people/{userid}` ignores it.
    /// </summary>
    /// <example>john.doe@example.com</example>
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    /// <summary>
    /// Set it to true to turn the account into a guest and to false to turn it back into a member. Either direction
    /// takes a seat and can answer 402, it is applied only when the caller edits somebody else, and a request to
    /// make the portal owner, a DocSpace administrator or a module administrator a guest is ignored.
    /// </summary>
    /// <example>true</example>
    public bool? IsUser { get; set; }

    /// <summary>
    /// The new first name, up to 255 characters. It is applied only to the caller's own profile, is left alone on an
    /// LDAP or SSO account, and a pair the portal does not accept as a name answers 400.
    /// </summary>
    /// <example>John</example>
    [StringLength(255)]
    public string FirstName { get; set; }

    /// <summary>
    /// The new last name, up to 255 characters. It is applied only to the caller's own profile, is left alone on an
    /// LDAP or SSO account, and a pair the portal does not accept as a name answers 400.
    /// </summary>
    /// <example>Doe</example>
    [StringLength(255)]
    public string LastName { get; set; }

    /// <summary>
    /// The groups the profile should belong to, by group ID, replacing the current ones. It is applied only to the
    /// caller's own profile.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000"]</example>
    public Guid[] Department { get; set; }

    // /// <summary>
    // /// The user title.
    // /// </summary>
    // /// <example>Manager</example>
    // [StringLength(255)]
    // public string Title { get; set; }

    /// <summary>
    /// The new free-text location shown on the profile. It is applied only to the caller's own profile and is left
    /// alone on an LDAP or SSO account.
    /// </summary>
    /// <example>New York</example>
    public string Location { get; set; }

    // /// <summary>
    // /// The user sex (male or female).
    // /// </summary>
    // /// <example>1</example>
    // public SexEnum? Sex { get; set; }

    // /// <summary>
    // /// The user birthday.
    // /// </summary>
    // /// <example>2025-01-01T00:00:00Z</example>
    // public ApiDateTime Birthday { get; set; }

    // /// <summary>
    // /// The user registration date (if it is not specified, then the current date will be set).
    // /// </summary>
    // /// <example>2025-01-01T00:00:00Z</example>
    // public ApiDateTime Worksfrom { get; set; }

    /// <summary>
    /// The new free-text note kept with the profile. It is applied only to the caller's own profile.
    /// </summary>
    /// <example>User comment</example>
    public string Comment { get; set; }

    /// <summary>
    /// The additional ways to reach the person, replacing the current ones. Each entry is a free-text type such as
    /// `email`, `phone`, `skype` or `telegram` and its value, an entry with an empty value is dropped, and the field
    /// is applied only to the caller's own profile.
    /// </summary>
    /// <example>[{"type": "email", "value": "john.doe@example.com"}]</example>
    public IEnumerable<Contact> Contacts { get; set; }

    /// <summary>
    /// The address the portal downloads the new avatar from. It is applied only to the caller's own profile, has to
    /// use HTTPS unless the request itself came over HTTP, and passing the address the profile already uses
    /// downloads nothing.
    /// </summary>
    /// <example>https://example.com/avatar.jpg</example>
    public string Files { get; set; }

    /// <summary>
    /// Whether the account agrees to receive tips, updates and offers. It is applied only to the caller's own
    /// profile, and omitting it on such a request stores false rather than keeping the current value.
    /// </summary>
    /// <example>false</example>
    public bool? Spam { get; set; }
}

/// <summary>
/// The request parameters for updating the user information by ID.
/// </summary>
public class UpdateMemberByIdRequestDto
{
    /// <summary>
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "userid")]
    public required string UserId { get; set; }

    /// <summary>
    /// The request parameters for updating the user information.
    /// </summary>
    /// <example>{"firstName": "John", "lastName": "Doe", "email": "john.doe@example.com"}</example>
    [FromBody]
    public required UpdateMemberRequestDto UpdateMember { get; set; }
}

/// <summary>
/// The request parameters for updating the user culture code by ID.
/// </summary>
public class UpdateMemberCultureByIdRequestDto
{
    /// <summary>
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "userid")]
    public required string UserId { get; set; }

    /// <summary>
    /// The culture name parameters.
    /// </summary>
    /// <example>{"cultureName": "en-US"}</example>
    [FromBody]
    public Culture Culture { get; set; }
}

/// <summary>
/// The culture name parameters.
/// </summary>
public class Culture
{
    /// <summary>
    /// The user culture name (en-US, de, fr, es, ...).
    /// </summary>
    /// <example>en-US</example>
    [Required]
    [StringLength(85)]
    public string CultureName { get; set; }
}

/// <summary>
/// The request parameters for updating a photo.
/// </summary>
public class UpdatePhotoMemberRequest
{
    /// <summary>
    /// The address the portal downloads the new avatar from. It has to be absolute or relative to the portal, and it
    /// has to use HTTPS unless the request itself came over HTTP; an address the portal refuses to fetch is rejected.
    /// It is required - an empty value is answered with 400 rather than clearing the avatar.
    /// </summary>
    /// <example>https://example.com/avatar.jpg</example>
    public string Files { get; set; }
}

/// <summary>
/// The request parameters for updating a user photo.
/// </summary>
public class UpdatePhotoMemberRequestDto
{
    /// <summary>
    /// The profile whose avatar is replaced, taken from the route. Either the ID of the account or its user name is
    /// accepted, and it has to be the calling account, because a profile photo can only be changed by its owner.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "userid")]
    public required string UserId { get; set; }

    /// <summary>
    /// The address of the image to use as the new avatar.
    /// </summary>
    /// <example>{"files": "https://example.com/avatar.jpg"}</example>
    [FromBody]
    public required UpdatePhotoMemberRequest UpdatePhoto { get; set; }
}


/// <summary>
/// The request parameters for getting a user by their ID.
/// </summary>
public class GetMemberByIdRequestDto
{
    /// <summary>
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "userid")]
    public required string UserId { get; set; }
}

/// <summary>
/// The request parameters for getting a user by the email address.
/// </summary>
public class GetMemberByEmailRequestDto
{
    /// <summary>
    /// The user email address.
    /// </summary>
    /// <example>john.doe@example.com</example>
    [FromQuery(Name = "email")]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    /// <summary>
    /// The user encrypted email address.
    /// </summary>
    /// <example>encrypted_email_string</example>
    [FromQuery(Name = "encemail")]
    public string EncEmail { get; set; }

    /// <summary>
    /// Culture
    /// </summary>
    /// <example>en-US</example>
    [FromQuery(Name = "culture")]
    public string Culture { get; set; }
}

/// <summary>
/// The request parameters for getting a user by the search query.
/// </summary>
public class GetMemberByQueryRequestDto
{
    /// <summary>
    /// The term to look for, taken from the route. Only accounts with the `Active` status are searched.
    /// </summary>
    /// <example>John</example>
    [FromRoute(Name = "query")]
    public required string Query { get; set; }

    /// <summary>
    /// The only recognised value is `group`, which turns `filterValue` into a group ID and keeps only the members of
    /// that group. Any other value, and omitting the field, applies no group filter.
    /// </summary>
    /// <example>group</example>
    [FromQuery(Name = "filterBy")]
    public string FilterBy { get; set; }

    /// <summary>
    /// The group ID to keep the members of, used only when `filterBy` is `group`. It has to be a valid identifier -
    /// a group name is not accepted.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}

/// <summary>
/// The request parameters for getting people by the search query.
/// </summary>
public class GetPeopleByQueryRequestDto
{
    /// <summary>
    /// The term to look for. Only accounts with the `Active` status are searched, and this is the only parameter the
    /// operation reads.
    /// </summary>
    /// <example>John</example>
    [FromQuery(Name = "query")]
    public string Query { get; set; }
}

/// <summary>
/// The request parameters for updating user contacts.
/// </summary>
public class UpdateMemberSimpleRequestDto
{
    /// <summary>
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public string UserId { get; set; }
}

/// <summary>
/// The contacts request.
/// </summary>
public class ContactsRequest
{
    /// <summary>
    /// The list of user contacts.
    /// </summary>
    /// <example>[{"type": "email", "value": "john.doe@example.com"}]</example>
    public IEnumerable<Contact> Contacts { get; set; }
}

/// <summary>
/// The request parameters for updating user contacts.
/// </summary>
public class ContactsRequestDto
{
    /// <summary>
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "userid")]
    public required string UserId { get; set; }

    /// <summary>
    /// The contacts request.
    /// </summary>
    /// <example>{"contacts": [{"type": "email", "value": "john.doe@example.com"}]}</example>
    [FromBody]
    public required ContactsRequest Contacts { get; set; }
}

/// <summary>
/// The request parameters for sharing a guest with another user.
/// </summary>
public class GuestShareRequestDto
{
    /// <summary>
    /// The ID of the guest to be handed over, taken from the route. The account has to exist, has to be a guest, and
    /// has to be one the caller can see.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "userid")]
    public Guid UserId { get; set; }
}
