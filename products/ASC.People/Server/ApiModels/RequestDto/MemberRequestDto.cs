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
/// Member request parameters
/// </summary>
public class EmailMemberRequestDto
{
    /// <summary>
    /// Email
    /// </summary>
    [EmailAddress]
    [StringLength(255)]
    [OpenApiDescription("Email")]
    public string Email { get; set; }
}

/// <summary>
/// Request parameters for setting new password
/// </summary>
public class MemberBaseRequestDto : EmailMemberRequestDto
{
    /// <summary>
    /// Password
    /// </summary>
    [OpenApiDescription("Password")]
    public string Password { get; set; }

    /// <summary>
    /// Password hash
    /// </summary>
    [OpenApiDescription("Password hash")]
    public string PasswordHash { get; set; }
}

/// <summary>
/// Request parameters for setting new password
/// </summary>
public class MemberBaseByIdRequestDto
{
    /// <summary>
    /// User ID
    /// </summary>
    [FromRoute(Name = "userid")]
    [OpenApiDescription("User ID")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Member base
    /// </summary>
    [FromBody]
    [OpenApiDescription("Member base")]
    public MemberBaseRequestDto MemberBase { get; set; }
}

/// <summary>
/// Member request parameters
/// </summary>
public class MemberRequestDto : MemberBaseRequestDto
{
    /// <summary>
    /// Employee type
    /// </summary>
    [OpenApiDescription("Employee type")]
    public EmployeeType Type { get; set; }

    /// <summary>
    /// Specifies if this is a guest or a user
    /// </summary>
    [OpenApiDescription("Specifies if this is a guest or a user")]
    public bool? IsUser { get; set; }

    /// <summary>
    /// First name
    /// </summary>
    [StringLength(255)]
    [OpenApiDescription("First name")]
    public string FirstName { get; set; }

    /// <summary>
    /// Last name
    /// </summary>
    [StringLength(255)]
    [OpenApiDescription("Last name")]
    public string LastName { get; set; }

    /// <summary>
    /// List of user departments
    /// </summary>
    [OpenApiDescription("List of user departments")]
    public Guid[] Department { get; set; }

    /// <summary>
    /// Title
    /// </summary>
    [StringLength(255)]
    [OpenApiDescription("Title")]
    public string Title { get; set; }

    /// <summary>
    /// Location
    /// </summary>
    [OpenApiDescription("Location")]
    public string Location { get; set; }

    /// <summary>
    /// Sex (male or female)
    /// </summary>
    [OpenApiDescription("Sex (male or female)")]
    public SexEnum? Sex { get; set; }

    /// <summary>
    /// Birthday
    /// </summary>
    [OpenApiDescription("Birthday")]
    public ApiDateTime Birthday { get; set; }

    /// <summary>
    /// Registration date (if it is not specified, then the current date will be set)
    /// </summary>
    [OpenApiDescription("Registration date (if it is not specified, then the current date will be set)")]
    public ApiDateTime Worksfrom { get; set; }

    /// <summary>
    /// Comment
    /// </summary>
    [OpenApiDescription("Comment")]
    public string Comment { get; set; }

    /// <summary>
    /// List of user contacts
    /// </summary>
    [OpenApiDescription("List of user contacts")]
    public IEnumerable<Contact> Contacts { get; set; }

    /// <summary>
    /// Avatar photo URL
    /// </summary>
    [OpenApiDescription("Avatar photo URL")]
    public string Files { get; set; }

    /// <summary>
    /// Specifies if the user is added via the invitation link or not
    /// </summary>
    [OpenApiDescription("Specifies if the user is added via the invitation link or not")]
    public bool FromInviteLink { get; set; }

    /// <summary>
    /// Key
    /// </summary>
    [OpenApiDescription("Key")]
    public string Key { get; set; }

    /// <summary>
    /// Language
    /// </summary>
    [OpenApiDescription("Language")]
    public string CultureName { get; set; }

    /// <summary>
    /// Target
    /// </summary>
    [OpenApiDescription("Target")]
    public Guid Target { get; set; }

    /// <summary>Spam</summary>
    /// <type>System.Boolean, System</type>
    [OpenApiDescription("Spam")]
    public bool? Spam { get; set; }
}

/// <summary>
/// Request parameters for updating user information
/// </summary>
public class UpdateMemberRequestDto : MemberRequestDto
{
    /// <summary>
    /// User ID
    /// </summary>
    [OpenApiDescription("User ID")]
    public string UserId { get; set; }

    /// <summary>
    /// Specifies whether to disable a user or not
    /// </summary>
    [OpenApiDescription("Specifies whether to disable a user or not")]
    public bool? Disable { get; set; }
}

/// <summary>
/// 
/// </summary>
public class UpdateMemberByIdRequestDto
{
    /// <summary>
    /// User ID
    /// </summary>
    [FromRoute(Name = "userid")]
    [OpenApiDescription("User ID")]
    public string UserId { get; set; }

    /// <summary>
    /// Update member
    /// </summary>
    [FromBody]
    [OpenApiDescription("Update member")]
    public UpdateMemberRequestDto UpdateMember { get; set; }
}

public enum SexEnum
{
    [OpenApiEnum("Female")]
    Female = 0,

    [OpenApiEnum("Male")]
    Male = 1
}

/// <summary>
/// Request parameters for updating user photo
/// </summary>
public class UpdatePhotoMemberRequest
{
    /// <summary>
    /// Avatar photo URL
    /// </summary>
    [OpenApiDescription("Avatar photo URL")]
    public string Files { get; set; }
}

/// <summary>
/// Request parameters for updating user photo
/// </summary>
public class UpdatePhotoMemberRequestDto
{
    /// <summary>
    /// User ID
    /// </summary>
    [FromRoute(Name = "userid")]
    [OpenApiDescription("User ID")]
    public string UserId { get; set; }

    /// <summary>
    /// Update photo
    /// </summary>
    [FromBody]
    [OpenApiDescription("Update photo")]
    public UpdatePhotoMemberRequest UpdatePhoto { get; set; }
}


/// <summary>
/// 
/// </summary>
public class GetMemberByIdRequestDto
{
    /// <summary>
    /// User ID
    /// </summary>
    [FromRoute(Name = "userid")]
    [OpenApiDescription("User ID")]
    public string UserId { get; set; }
}

/// <summary>
/// 
/// </summary>
public class GetMemberByEmailRequestDto
{
    /// <summary>
    /// User email address
    /// </summary>
    [FromQuery(Name = "email")]
    [EmailAddress]
    [StringLength(255)]
    [OpenApiDescription("User email address")]
    public string Email { get; set; }
}

/// <summary>
/// 
/// </summary>
public class GetMemberByQueryRequestDto
{
    /// <summary>
    /// Search query
    /// </summary>
    [FromRoute(Name = "query")]
    [OpenApiDescription("Search query")]
    public string Query { get; set; }
}

/// <summary>
/// 
/// </summary>
public class GetPeopleByQueryRequestDto
{
    /// <summary>
    /// Search query
    /// </summary>
    [FromQuery(Name = "query")]
    [OpenApiDescription("Search query")]
    public string Query { get; set; }
}

/// <summary>
/// Request parameters for updating user contacts
/// </summary>
public class UpdateMemberSimpleRequestDto
{
    /// <summary>
    /// User ID
    /// </summary>
    [OpenApiDescription("User ID")]
    public string UserId { get; set; }
}

/// <summary>
/// Parameters for updating user contacts
/// </summary>
public class ContactsRequest
{
    /// <summary>
    /// List of user contacts
    /// </summary>
    [OpenApiDescription("List of user contacts")]
    public IEnumerable<Contact> Contacts { get; set; }
}


/// <summary>
/// Request parameters for updating user contacts
/// </summary>
public class ContactsRequestDto
{
    /// <summary>
    /// User ID
    /// </summary>
    [FromRoute(Name = "userid")]
    [OpenApiDescription("User ID")]
    public string UserId { get; set; }

    /// <summary>
    /// Contacts
    /// </summary>
    [FromBody]
    [OpenApiDescription("Contacts")]
    public ContactsRequest Contacts { get; set; }
}