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

namespace ASC.Web.Api.ApiModels.RequestsDto;

/// <summary>
/// The whole LDAP integration of the portal: the directory to talk to, what to import from it, and how the imported
/// records map onto portal accounts.
/// </summary>
public class LdapRequestsDto
{
    /// <summary>
    /// Whether portal members may sign in with their directory credentials. Switching it off leaves the accounts
    /// already imported in place but makes them sign in with their portal password instead.
    /// </summary>
    /// <example>true</example>
    public bool EnableLdapAuthentication { get; set; }

    /// <summary>
    /// Whether the connection is upgraded to TLS after it is opened, on the ordinary LDAP port. It is the
    /// alternative to `ssl`, which connects over TLS from the start; the two are not combined.
    /// </summary>
    /// <example>true</example>
    public bool StartTls { get; set; }

    /// <summary>
    /// Whether the connection is opened over TLS from the start, which usually means the directory answers on the
    /// LDAPS port rather than the plain one, so set `portNumber` to match.
    /// </summary>
    /// <example>true</example>
    public bool Ssl { get; set; }

    /// <summary>
    /// Whether each account created by the import is mailed an activation letter. Switching it off imports the
    /// people silently, leaving them to be told about the portal by other means.
    /// </summary>
    /// <example>true</example>
    public bool SendWelcomeEmail { get; set; }

    /// <summary>
    /// Whether an imported account is treated as already confirmed, so that its owner is not asked to verify the
    /// address the directory supplied before using the portal.
    /// </summary>
    /// <example>true</example>
    public bool DisableEmailVerification { get; set; }

    /// <summary>
    /// The directory host, as a hostname or an IP address with the scheme - `LDAP://` for a plain connection and
    /// `LDAPS://` for one that is encrypted from the start.
    /// </summary>
    /// <example>ldap.example.com</example>
    public string Server { get; set; }

    /// <summary>
    /// The absolute path to the top level directory containing users for the import. Everything below it is
    /// searched, so a base that is too high makes the import slow and one that is too low silently misses people.
    /// </summary>
    /// <example>ou=users,dc=example,dc=com</example>
    public string UserDN { get; set; }

    /// <summary>
    /// The TCP port the directory answers on. It has to match the connection kind - the plain port for an
    /// unencrypted or StartTLS connection, the LDAPS port when `ssl` is set.
    /// </summary>
    /// <example>389</example>
    public int PortNumber { get; set; }

    /// <summary>
    /// The user filter value to import the users who correspond to the specified search criteria. The default filter
    /// value (uid=*) allows importing all users. It is an LDAP search filter and is combined with `userDN`, so it
    /// narrows the subtree rather than replacing it; a malformed filter fails the import rather than this call.
    /// </summary>
    /// <example>uid=*</example>
    public string UserFilter { get; set; }

    /// <summary>
    /// The attribute in a user record that corresponds to the login that LDAP server users will use to log in to
    /// ONLYOFFICE. Its value has to be unique across the imported people, since it is what a directory record and a
    /// portal account are matched by on every later synchronization.
    /// </summary>
    /// <example>uid</example>
    public string LoginAttribute { get; set; }

    /// <summary>
    /// The correspondence between the user data fields on the portal and the attributes in the LDAP server user
    /// record. This is the whole mapping that is to hold afterwards; a portal field left out of it is left empty on
    /// the imported accounts rather than keeping whatever it had.
    /// </summary>
    /// <example>{ "FirstName": "givenName", "SecondName": "sn", "Mail": "mail" }</example>
    public Dictionary<MappingFields, string> LdapMapping { get; set; }

    /// <summary>
    /// The portal role each named directory group is imported with, as the group name and the role it grants. It
    /// decides what the members of that group may do once they are on the portal.
    /// </summary>
    /// <example>{ "Admin": "FullAccess", "User": "ReadOnly" }</example>
    //ToDo: use SId instead of group name
    public Dictionary<AccessRight, string> AccessRights { get; set; }

    /// <summary>
    /// Whether the directory groups are imported as portal groups as well. While it is off only people are imported
    /// and the group settings below are not read at all.
    /// </summary>
    /// <example>true</example>
    public bool GroupMembership { get; set; }

    /// <summary>
    /// The absolute path to the top level directory containing groups for the import. It is read only while
    /// `groupMembership` is on.
    /// </summary>
    /// <example>ou=groups,dc=example,dc=com</example>
    // ReSharper disable once InconsistentNaming
    public string GroupDN { get; set; }

    /// <summary>
    /// The attribute that determines whether the user is a member of the groups - the one on a user record that
    /// lists the groups it belongs to, membership being read from the user side.
    /// </summary>
    /// <example>memberOf</example>
    public string UserAttribute { get; set; }

    /// <summary>
    /// The group filter value to import the groups who correspond to the specified search criteria. The default
    /// filter value (objectClass=posixGroup) allows importing all groups, and it narrows the subtree under `groupDN`
    /// rather than replacing it.
    /// </summary>
    /// <example>objectClass=posixGroup</example>
    public string GroupFilter { get; set; }

    /// <summary>
    /// The attribute that specifies the users that the group includes - the one on a group record that lists its
    /// members, membership being read from the group side.
    /// </summary>
    /// <example>member</example>
    public string GroupAttribute { get; set; }

    /// <summary>
    /// The attribute that corresponds to a name of the group where the user is included. Its value becomes the name
    /// of the portal group created for that directory group.
    /// </summary>
    /// <example>cn</example>
    public string GroupNameAttribute { get; set; }

    /// <summary>
    /// Whether the portal binds to the directory with the credentials below instead of connecting anonymously. Set
    /// it whenever the directory does not allow an anonymous search, which most do not.
    /// </summary>
    /// <example>true</example>
    public bool Authentication { get; set; }

    /// <summary>
    /// The distinguished name the portal binds with, in the form the directory expects. It is the account that reads
    /// the directory, not a portal account, and it needs read access to the user and group subtrees.
    /// </summary>
    /// <example>cn=admin,dc=example,dc=com</example>
    public string Login { get; set; }

    /// <summary>
    /// The password of the bind account. It is stored encrypted for this portal and is never handed back by any
    /// operation.
    /// </summary>
    /// <example>password123</example>
    public string Password { get; set; }

    /// <summary>
    /// Whether a directory certificate that does not validate is accepted anyway. Setting it lets a self-signed
    /// certificate through and with it the possibility that the connection is being intercepted.
    /// </summary>
    /// <example>true</example>
    public bool AcceptCertificate { get; set; }

    /// <summary>
    /// The role every imported person is created with, unless a directory group they belong to grants another one
    /// through `accessRights`. It applies to accounts created from now on and does not change the ones already
    /// imported.
    /// </summary>
    /// <example>User</example>
    public EmployeeType UsersType { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class LdapRequestsMapper
{
    public static partial LdapSettings MapToSettings(this LdapRequestsDto source);
}
