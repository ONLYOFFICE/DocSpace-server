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
    [SwaggerSchemaCustom(Example = "some text", Description = "Email")]
    public string Email { get; set; }
}
public class MemberBaseRequestDto : EmailMemberRequestDto
{
    [SwaggerSchemaCustom(Example = "some text", Description = "Password")]
    public string Password { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Password hash")]
    public string PasswordHash { get; set; }
}
public class MemberRequestDto : MemberBaseRequestDto
{
    [SwaggerSchemaCustom(Example = "All", Description = "Employee type (All, RoomAdmin, User, DocSpaceAdmin, Collaborator)")]
    public EmployeeType Type { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Specifies if this is a guest or a user", Nullable = true)]
    public bool? IsUser { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "First name")]
    public string Firstname { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Last name")]
    public string Lastname { get; set; }

    [SwaggerSchemaCustom(Example = "9924256A-739C-462b-AF15-E652A3B1B6EB", Description = "List of user departments")]
    public Guid[] Department { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Title")]
    public string Title { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Location")]
    public string Location { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Sex (male or female)")]
    public string Sex { get; set; }

    [SwaggerSchemaCustom(Example = "2008-04-10T06-30-00.000Z", Description = "Birthday")]
    public ApiDateTime Birthday { get; set; }

    [SwaggerSchemaCustom(Example = "2008-04-10T06-30-00.000Z", Description = "Registration date (if it is not specified, then the current date will be set)")]
    public ApiDateTime Worksfrom { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Comment")]
    public string Comment { get; set; }

    [SwaggerSchemaCustom(Description = "List of user contacts")]
    public IEnumerable<Contact> Contacts { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Avatar photo URL", Format ="uri")]
    public string Files { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Specifies if the user is added via the invitation link or not")]
    public bool FromInviteLink { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Key")]
    public string Key { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Language")]
    public string CultureName { get; set; }

    [SwaggerSchemaCustom(Example = "9924256A-739C-462b-AF15-E652A3B1B6EB", Description = "Target")]
    public Guid Target { get; set; }
}

public class UpdateMemberRequestDto : MemberRequestDto
{
    [SwaggerSchemaCustom(Example = "some text", Description = "User ID")]
    public string UserId { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Specifies whether to disable a user or not", Nullable = true)]
    public bool? Disable { get; set; }
}

public class UpdatePhotoMemberRequestDto
{
    [SwaggerSchemaCustom(Example = "some text", Description = "Avatar photo URL", Format = "uri")]
    public string Files { get; set; }
}
public class UpdateMemberSimpleRequestDto
{
    [SwaggerSchemaCustom(Example = "some text", Description = "User ID")]
    public string UserId { get; set; }
}

public class ContactsRequestDto
{
    [SwaggerSchemaCustom(Description = "List of user contacts")]
    public IEnumerable<Contact> Contacts { get; set; }
}
