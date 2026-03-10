// (c) Copyright Ascensio System SIA 2009-2026
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.People.ApiModels.RequestDto;

/// <summary>
/// The request parameters for the user email.
/// </summary>
public class EmailMemberRequestDto
{
    /// <summary>
    /// The user email address.
    /// </summary>
    /// <example>john.doe@example.com</example>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    /// <summary>
    /// The type of CAPTCHA validation used.
    /// </summary>
    /// <example>0</example>
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
    /// The user password.
    /// </summary>
    /// <example>P@ssw0rd</example>
    public string Password { get; set; }

    /// <summary>
    /// The user password hash.
    /// </summary>
    /// <example>5f4dcc3b5aa765d61d8327deb882cf99</example>
    public string PasswordHash { get; set; }
}

/// <summary>
/// The request parameters for updating a user password by their ID.
/// </summary>
public class ChangePasswordByIdRequestDto
{
    /// <summary>
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "userid")]
    public required Guid UserId { get; set; }

    /// <summary>
    /// The request parameters for updating a user password.
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
    /// The user email address.
    /// </summary>
    /// <example>john.doe@example.com</example>
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    /// <summary>
    /// The user encrypted email address.
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
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "userid")]
    public required Guid UserId { get; set; }

    /// <summary>
    /// The request parameters for updating a user email.
    /// </summary>
    /// <example>{"password": "P@ssw0rd", "email": "john.doe@example.com"}</example>
    [FromBody]
    public required ChangeEmailRequest ChangeEmailData { get; set; }
}

/// <summary>
/// The user request parameters.
/// </summary>
public class MemberRequestDto
{
    /// <summary>
    /// The user password.
    /// </summary>
    /// <example>P@ssw0rd</example>
    public string Password { get; set; }

    /// <summary>
    /// The user password hash.
    /// </summary>
    /// <example>5f4dcc3b5aa765d61d8327deb882cf99</example>
    public string PasswordHash { get; set; }

    /// <summary>
    /// The user email address.
    /// </summary>
    /// <example>john.doe@example.com</example>
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    /// <summary>
    /// The user type.
    /// </summary>
    /// <example>1</example>
    public EmployeeType Type { get; set; }

    /// <summary>
    /// Specifies if this is a guest or a user.
    /// </summary>
    /// <example>true</example>
    public bool? IsUser { get; set; }

    /// <summary>
    /// The user first name.
    /// </summary>
    /// <example>John</example>
    [StringLength(255)]
    public string FirstName { get; set; }

    /// <summary>
    /// The user last name.
    /// </summary>
    /// <example>Doe</example>
    [StringLength(255)]
    public string LastName { get; set; }

    /// <summary>
    /// The list of the user departments IDs.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000"]</example>
    public Guid[] Department { get; set; }

    /// <summary>
    /// The user title.
    /// </summary>
    /// <example>Manager</example>
    [StringLength(255)]
    public string Title { get; set; }

    /// <summary>
    /// The user location.
    /// </summary>
    /// <example>New York</example>
    public string Location { get; set; }

    /// <summary>
    /// The user sex (male or female).
    /// </summary>
    /// <example>1</example>
    public SexEnum? Sex { get; set; }

    /// <summary>
    /// The user birthday.
    /// </summary>
    /// <example>2025-01-01T00:00:00Z</example>
    public ApiDateTime Birthday { get; set; }

    /// <summary>
    /// The user registration date (if it is not specified, then the current date will be set).
    /// </summary>
    /// <example>2025-01-01T00:00:00Z</example>
    public ApiDateTime Worksfrom { get; set; }

    /// <summary>
    /// The user comment.
    /// </summary>
    /// <example>User comment</example>
    public string Comment { get; set; }

    /// <summary>
    /// The list of the user contacts.
    /// </summary>
    /// <example>[{"type": "email", "value": "john.doe@example.com"}]</example>
    public IEnumerable<Contact> Contacts { get; set; }

    /// <summary>
    /// The avatar photo URL.
    /// </summary>
    /// <example>https://example.com/avatar.jpg</example>
    public string Files { get; set; }

    /// <summary>
    /// Specifies if the user is added via the invitation link or not.
    /// </summary>
    /// <example>false</example>
    public bool FromInviteLink { get; set; }

    /// <summary>
    /// The user key.
    /// </summary>
    /// <example>user_key_string</example>
    public string Key { get; set; }

    /// <summary>
    /// The user culture code.
    /// </summary>
    /// <example>en-US</example>
    public string CultureName { get; set; }

    /// <summary>
    /// The user target ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid Target { get; set; }

    /// <summary>
    /// Specifies if tips, updates and offers are allowed to be sent to the user or not.
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
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public string UserId { get; set; }

    /// <summary>
    /// Specifies whether to disable a user or not.
    /// </summary>
    /// <example>false</example>
    public bool? Disable { get; set; }

    /// <summary>
    /// The user email address.
    /// </summary>
    /// <example>john.doe@example.com</example>
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    /// <summary>
    /// Specifies if this is a guest or a user.
    /// </summary>
    /// <example>true</example>
    public bool? IsUser { get; set; }

    /// <summary>
    /// The user first name.
    /// </summary>
    /// <example>John</example>
    [StringLength(255)]
    public string FirstName { get; set; }

    /// <summary>
    /// The user last name.
    /// </summary>
    /// <example>Doe</example>
    [StringLength(255)]
    public string LastName { get; set; }

    /// <summary>
    /// The list of the user departments.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000"]</example>
    public Guid[] Department { get; set; }

    /// <summary>
    /// The user title.
    /// </summary>
    /// <example>Manager</example>
    [StringLength(255)]
    public string Title { get; set; }

    /// <summary>
    /// The user location.
    /// </summary>
    /// <example>New York</example>
    public string Location { get; set; }

    /// <summary>
    /// The user sex (male or female).
    /// </summary>
    /// <example>1</example>
    public SexEnum? Sex { get; set; }

    /// <summary>
    /// The user birthday.
    /// </summary>
    /// <example>2025-01-01T00:00:00Z</example>
    public ApiDateTime Birthday { get; set; }

    /// <summary>
    /// The user registration date (if it is not specified, then the current date will be set).
    /// </summary>
    /// <example>2025-01-01T00:00:00Z</example>
    public ApiDateTime Worksfrom { get; set; }

    /// <summary>
    /// The user comment.
    /// </summary>
    /// <example>User comment</example>
    public string Comment { get; set; }

    /// <summary>
    /// The list of the user contacts.
    /// </summary>
    /// <example>[{"type": "email", "value": "john.doe@example.com"}]</example>
    public IEnumerable<Contact> Contacts { get; set; }

    /// <summary>
    /// The user avatar photo URL.
    /// </summary>
    /// <example>https://example.com/avatar.jpg</example>
    public string Files { get; set; }

    /// <summary>
    /// Specifies if tips, updates and offers are allowed to be sent to the user or not.
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
/// The user sex.
/// </summary>
public enum SexEnum
{
    [Description("Female")]
    Female = 0,

    [Description("Male")]
    Male = 1
}

/// <summary>
/// The request parameters for updating a photo.
/// </summary>
public class UpdatePhotoMemberRequest
{
    /// <summary>
    /// The avatar photo URL.
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
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "userid")]
    public required string UserId { get; set; }

    /// <summary>
    /// The request parameters for updating a photo.
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
    /// The search query.
    /// </summary>
    /// <example>John</example>
    [FromRoute(Name = "query")]
    public required string Query { get; set; }

    /// <summary>
    /// Specifies a filter criteria for the user search query.
    /// </summary>
    /// <example>displayName</example>
    [FromQuery(Name = "filterBy")]
    public string FilterBy { get; set; }

    /// <summary>
    /// The value used for filtering users, allowing additional constraints for the query.
    /// </summary>
    /// <example>John</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}

/// <summary>
/// The request parameters for getting people by the search query.
/// </summary>
public class GetPeopleByQueryRequestDto
{
    /// <summary>
    /// The search query.
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
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "userid")]
    public Guid UserId { get; set; }
}