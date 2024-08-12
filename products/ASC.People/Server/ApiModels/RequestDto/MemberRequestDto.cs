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

public class EmailMemberRequestDto
{
    [SwaggerSchemaCustomString("Email")]
    public string Email { get; set; }
}
public class MemberBaseRequestDto : EmailMemberRequestDto
{
    [SwaggerSchemaCustomString("Password")]
    public string Password { get; set; }

    [SwaggerSchemaCustomString("Password hash")]
    public string PasswordHash { get; set; }
}
public class MemberRequestDto : MemberBaseRequestDto
{
    [SwaggerSchemaCustomString("Employee type (All, RoomAdmin, User, DocSpaceAdmin, Collaborator)", Example = "All")]
    public EmployeeType Type { get; set; }

    [SwaggerSchemaCustom("Specifies if this is a guest or a user", Nullable = true)]
    public bool? IsUser { get; set; }

    [SwaggerSchemaCustomString("First name")]
    public string Firstname { get; set; }

    [SwaggerSchemaCustomString("Last name")]
    public string Lastname { get; set; }

    [SwaggerSchemaCustom("List of user departments")]
    public Guid[] Department { get; set; }

    [SwaggerSchemaCustomString("Title")]
    public string Title { get; set; }

    [SwaggerSchemaCustomString("Location")]
    public string Location { get; set; }

    [SwaggerSchemaCustomString("Sex (male or female)")]
    public string Sex { get; set; }

    [SwaggerSchemaCustom<ApiDateTime>("Birthday")]
    public ApiDateTime Birthday { get; set; }

    [SwaggerSchemaCustom<ApiDateTime>("Registration date (if it is not specified, then the current date will be set)")]
    public ApiDateTime Worksfrom { get; set; }

    [SwaggerSchemaCustomString("Comment")]
    public string Comment { get; set; }

    [SwaggerSchemaCustom<IEnumerable<Contact>>("List of user contacts")]
    public IEnumerable<Contact> Contacts { get; set; }

    [SwaggerSchemaCustomString("Avatar photo URL", Format ="uri")]
    public string Files { get; set; }

    [SwaggerSchemaCustom("Specifies if the user is added via the invitation link or not")]
    public bool FromInviteLink { get; set; }

    [SwaggerSchemaCustomString("Key")]
    public string Key { get; set; }

    [SwaggerSchemaCustomString("Language")]
    public string CultureName { get; set; }

    [SwaggerSchemaCustom("Target")]
    public Guid Target { get; set; }
}

public class UpdateMemberRequestDto : MemberRequestDto
{
    [SwaggerSchemaCustomString("User ID")]
    public string UserId { get; set; }

    [SwaggerSchemaCustom("Specifies whether to disable a user or not", Nullable = true)]
    public bool? Disable { get; set; }
}

public class UpdatePhotoMemberRequestDto
{
    [SwaggerSchemaCustomString("Avatar photo URL", Format = "uri")]
    public string Files { get; set; }
}
public class UpdateMemberSimpleRequestDto
{
    [SwaggerSchemaCustomString("User ID")]
    public string UserId { get; set; }
}

public class ContactsRequestDto
{
    [SwaggerSchemaCustom<IEnumerable<Contact>>("List of user contacts")]
    public IEnumerable<Contact> Contacts { get; set; }
}
