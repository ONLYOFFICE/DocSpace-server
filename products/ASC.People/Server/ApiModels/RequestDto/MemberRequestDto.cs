// (c) Copyright Ascensio System SIA 2009-2024
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
/// The member request parameters.
/// </summary>
public class EmailMemberRequestDto
{
    /// <summary>
    /// The email of the member.
    /// </summary>
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }
}

/// <summary>
/// The request parameters for setting new password.
/// </summary>
public class MemberBaseRequestDto : EmailMemberRequestDto
{
    /// <summary>
    /// The password of the member.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// The password hash of the member.
    /// </summary>
    public string PasswordHash { get; set; }
}

/// <summary>
/// The request parameters for setting new password.
/// </summary>
public class MemberBaseByIdRequestDto
{
    /// <summary>
    /// The user ID.
    /// </summary>
    [FromRoute(Name = "userid")]
    public Guid UserId { get; set; }

    /// <summary>
    /// The member base.
    /// </summary>
    [FromBody]
    public MemberBaseRequestDto MemberBase { get; set; }
}

/// <summary>
/// The member request parameters.
/// </summary>
public class MemberRequestDto : MemberBaseRequestDto
{
    /// <summary>
    /// The employee type of the member.
    /// </summary>
    public EmployeeType Type { get; set; }

    /// <summary>
    /// Specifies if this is the guest or the user.
    /// </summary>
    public bool? IsUser { get; set; }

    /// <summary>
    /// The first name.
    /// </summary>
    [StringLength(255)]
    public string FirstName { get; set; }

    /// <summary>
    /// The last name.
    /// </summary>
    [StringLength(255)]
    public string LastName { get; set; }

    /// <summary>
    /// The list of user departments.
    /// </summary>
    public Guid[] Department { get; set; }

    /// <summary>
    /// The title of the member.
    /// </summary>
    [StringLength(255)]
    public string Title { get; set; }

    /// <summary>
    /// The location of the member.
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// The sex (male or female).
    /// </summary>
    public SexEnum? Sex { get; set; }

    /// <summary>
    /// The birthday of the member.
    /// </summary>
    public ApiDateTime Birthday { get; set; }

    /// <summary>
    /// The registration date (if it is not specified, then the current date will be set).
    /// </summary>
    public ApiDateTime Worksfrom { get; set; }

    /// <summary>
    /// The comment of the member.
    /// </summary>
    public string Comment { get; set; }

    /// <summary>
    /// The list of user contacts.
    /// </summary>
    public IEnumerable<Contact> Contacts { get; set; }

    /// <summary>
    /// The avatar photo URL.
    /// </summary>
    public string Files { get; set; }

    /// <summary>
    /// Specifies if the user is added via the invitation link or not.
    /// </summary>
    public bool FromInviteLink { get; set; }

    /// <summary>
    /// The key of the member.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// The language of the member.
    /// </summary>
    public string CultureName { get; set; }

    /// <summary>
    /// The target of the member.
    /// </summary>
    public Guid Target { get; set; }

    /// <summary>Specifies if the member is spam or not.</summary>
    /// <type>System.Boolean, System</type>
    public bool? Spam { get; set; }
}

/// <summary>
/// The request parameters for updating user information.
/// </summary>
public class UpdateMemberRequestDto : MemberRequestDto
{
    /// <summary>
    /// The user ID.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Specifies whether to disable the user or not.
    /// </summary>
    public bool? Disable { get; set; }
}

/// <summary>
/// The request parameters for updating user information by ID.
/// </summary>
public class UpdateMemberByIdRequestDto
{
    /// <summary>
    /// The user ID.
    /// </summary>
    [FromRoute(Name = "userid")]
    public string UserId { get; set; }

    /// <summary>
    /// The update member request parameters.
    /// </summary>
    [FromBody]
    public UpdateMemberRequestDto UpdateMember { get; set; }
}

public enum SexEnum
{
    [SwaggerEnum("Female")]
    Female = 0,

    [SwaggerEnum("Male")]
    Male = 1
}

/// <summary>
/// Request parameters for updating user photo.
/// </summary>
public class UpdatePhotoMemberRequest
{
    /// <summary>
    /// The avatar photo URL.
    /// </summary>
    public string Files { get; set; }
}

/// <summary>
/// The request parameters for updating user photo.
/// </summary>
public class UpdatePhotoMemberRequestDto
{
    /// <summary>
    /// The user ID.
    /// </summary>
    [FromRoute(Name = "userid")]
    public string UserId { get; set; }

    /// <summary>
    /// The update photo request parameters.
    /// </summary>
    [FromBody]
    public UpdatePhotoMemberRequest UpdatePhoto { get; set; }
}


/// <summary>
/// The request parameters for getting user ID.
/// </summary>
public class GetMemberByIdRequestDto
{
    /// <summary>
    /// The user ID.
    /// </summary>
    [FromRoute(Name = "userid")]
    public string UserId { get; set; }
}

/// <summary>
/// The request parameters for getting user email.
/// </summary>
public class GetMemberByEmailRequestDto
{
    /// <summary>
    /// The user email address.
    /// </summary>
    [FromQuery(Name = "email")]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }
}

/// <summary>
/// The request parameters for getting user search query.
/// </summary>
public class GetMemberByQueryRequestDto
{
    /// <summary>
    /// The search query.
    /// </summary>
    [FromRoute(Name = "query")]
    public string Query { get; set; }
}

/// <summary>
/// The request parameters for getting people search query.
/// </summary>
public class GetPeopleByQueryRequestDto
{
    /// <summary>
    /// The search query.
    /// </summary>
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
    public string UserId { get; set; }
}

/// <summary>
/// The parameters for updating user contacts.
/// </summary>
public class ContactsRequest
{
    /// <summary>
    /// The list of user contacts.
    /// </summary>
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
    [FromRoute(Name = "userid")]
    public string UserId { get; set; }

    /// <summary>
    /// The contacts request.
    /// </summary>
    [FromBody]
    public ContactsRequest Contacts { get; set; }
}