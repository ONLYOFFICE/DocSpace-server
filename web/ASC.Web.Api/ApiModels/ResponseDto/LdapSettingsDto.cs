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

namespace ASC.Web.Api.ApiModels.ResponseDto;

public class LdapSettingsDto : IMapFrom<LdapSettings>
{
    [SwaggerSchemaCustom("Specifies if the LDAP authentication is enabled or not")]
    public bool EnableLdapAuthentication { get; set; }

    [SwaggerSchemaCustom("Specifies if the StartTLS is enabled or not")]
    public bool StartTls { get; set; }

    [SwaggerSchemaCustom("Specifies if the SSL is enabled or not")]
    public bool Ssl { get; set; }

    [SwaggerSchemaCustom("Specifies if the welcome email is sent or not")]
    public bool SendWelcomeEmail { get; set; }

    [SwaggerSchemaCustom("LDAP server URL address")]
    public string Server { get; set; }

    [SwaggerSchemaCustom("Absolute path to the top level directory containing users for the import")]
    // ReSharper disable once InconsistentNaming
    public string UserDN { get; set; }

    [SwaggerSchemaCustom("Port number")]
    public int PortNumber { get; set; }

    [SwaggerSchemaCustom("User filter value to import the users who correspond to the specified search criteria. The default filter value (uid=*) allows importing all users")]
    public string UserFilter { get; set; }

    [SwaggerSchemaCustom("Attribute in a user record that corresponds to the login that LDAP server users will use to log in to ONLYOFFICE")]
    public string LoginAttribute { get; set; }

    [SwaggerSchemaCustom<Dictionary<MappingFields, string>>("Correspondence between the user data fields on the portal and the attributes in the LDAP server user record")]
    public Dictionary<MappingFields, string> LdapMapping { get; set; }

    [SwaggerSchemaCustom<Dictionary<AccessRight, string>>("Group access rights")]
    //ToDo: use SId instead of group name
    public Dictionary<AccessRight, string> AccessRights { get; set; }

    [SwaggerSchemaCustom("Specifies if the groups from the LDAP server are added to the portal or not")]
    public bool GroupMembership { get; set; }

    [SwaggerSchemaCustom("The absolute path to the top level directory containing groups for the import")]
    // ReSharper disable once InconsistentNaming
    public string GroupDN { get; set; }

    [SwaggerSchemaCustom("Attribute that determines whether this user is a member of the groups")]
    public string UserAttribute { get; set; }

    [SwaggerSchemaCustom("Group filter value to import the groups who correspond to the specified search criteria. The default filter value (objectClass=posixGroup) allows importing all users")]
    public string GroupFilter { get; set; }

    [SwaggerSchemaCustom("Attribute that specifies the users that the group includes")]
    public string GroupAttribute { get; set; }

    [SwaggerSchemaCustom("Attribute that corresponds to a name of the group where the user is included")]
    public string GroupNameAttribute { get; set; }

    [SwaggerSchemaCustom("Specifies if the user has rights to read data from LDAP server or not")]
    public bool Authentication { get; set; }

    [SwaggerSchemaCustom("Login")]
    public string Login { get; set; }

    [SwaggerSchemaCustom("Password")]
    public string Password { get; set; }

    [SwaggerSchemaCustom("Specifies if the certificate is accepted or not")]
    public bool AcceptCertificate { get; set; }

    [SwaggerSchemaCustom("Specifies if the default LDAP settings are used or not")]
    public bool IsDefault { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<LdapSettings, LdapSettingsDto>();
    }

}
