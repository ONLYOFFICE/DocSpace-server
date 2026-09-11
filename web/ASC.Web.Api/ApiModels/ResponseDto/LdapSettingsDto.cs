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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// How the portal talks to its directory server and which of its records become portal accounts.
/// </summary>
/// <example>
/// {
///   "enableLdapAuthentication": true,
///   "disableEmailVerification": true,
///   "startTls": false,
///   "ssl": true,
///   "sendWelcomeEmail": true,
///   "server": "ldap.example.com",
///   "userDN": "ou=people,dc=example,dc=com",
///   "portNumber": 636,
///   "userFilter": "(uid=*)",
///   "loginAttribute": "sAMAccountName",
///   "ldapMapping": { "FirstNameAttribute": "givenName" },
///   "groupMembership": true,
///   "groupDN": "ou=groups,dc=example,dc=com",
///   "userAttribute": "memberOf",
///   "groupFilter": "(objectClass=posixGroup)",
///   "groupAttribute": "member",
///   "groupNameAttribute": "cn",
///   "authentication": true,
///   "login": "admin@example.com",
///   "password": "",
///   "acceptCertificate": true,
///   "isDefault": true
/// }
/// </example>
public class LdapSettingsDto
{
    /// <summary>
    /// Whether members may sign in with their directory credentials. While it is `false` the rest of these
    /// settings are still reported but nothing acts on them, and switching it off also clears the
    /// synchronisation schedule.
    /// </summary>
    /// <example>true</example>
    public bool EnableLdapAuthentication { get; set; }

    /// <summary>
    /// Whether an imported account skips the e-mail confirmation step and counts as active straight away. Note
    /// the sense: `true` means no confirmation is asked for.
    /// </summary>
    /// <example>true</example>
    public bool DisableEmailVerification { get; set; }

    /// <summary>
    /// Whether the connection is upgraded to TLS after it opens, on the plain port. It is the alternative to
    /// `ssl`, which encrypts from the start, and the two are not meant to be set together.
    /// </summary>
    /// <example>false</example>
    public bool StartTls { get; set; }

    /// <summary>
    /// Whether the connection is encrypted from the start, on the directory server's secure port. See
    /// `startTls` for the other way of securing it.
    /// </summary>
    /// <example>true</example>
    public bool Ssl { get; set; }

    /// <summary>
    /// Whether a newly imported account is sent the portal's welcome letter. It has no effect on accounts that
    /// were already imported.
    /// </summary>
    /// <example>true</example>
    public bool SendWelcomeEmail { get; set; }

    /// <summary>
    /// The host name or address of the directory server, with any `LDAP://` prefix stripped off, so it is a bare
    /// host even where the setting was saved with one.
    /// </summary>
    /// <example>ldap.example.com</example>
    public string Server { get; set; }

    /// <summary>
    /// The distinguished name of the directory subtree the accounts are read from. Everything below it is
    /// searched, and `userFilter` narrows that down further.
    /// </summary>
    /// <example>ou=people,dc=example,dc=com</example>
    // ReSharper disable once InconsistentNaming
    public string UserDN { get; set; }

    /// <summary>
    /// The port the directory server is reached on, conventionally 389 for a plain or StartTLS connection and 636
    /// for one encrypted from the start.
    /// </summary>
    /// <example>636</example>
    public int PortNumber { get; set; }

    /// <summary>
    /// The LDAP search filter that decides which records under `userDN` become portal accounts. `(uid=*)`, the
    /// value the portal starts with, takes every one of them.
    /// </summary>
    /// <example>(uid=*)</example>
    public string UserFilter { get; set; }

    /// <summary>
    /// The directory attribute whose value a member types as their portal login - typically `sAMAccountName` on
    /// Active Directory and `uid` elsewhere.
    /// </summary>
    /// <example>sAMAccountName</example>
    public string LoginAttribute { get; set; }

    /// <summary>
    /// Which directory attribute fills which field of a portal profile, keyed by the profile field. A field left
    /// out of the map is not imported at all rather than imported empty.
    /// </summary>
    /// <example>{ "FirstNameAttribute": "givenName" }</example>
    public Dictionary<MappingFields, string> LdapMapping { get; set; }

    /// <summary>
    /// Which directory group grants which portal administrator right, keyed by the right and holding the group
    /// name. It is empty when no such grant is configured, in which case rights are managed in the portal alone.
    /// </summary>
    /// <example>{ "Documents": "cn=docs-admins,ou=groups,dc=example,dc=com" }</example>
    //ToDo: use SId instead of group name
    public Dictionary<AccessRight, string> AccessRights { get; set; }

    /// <summary>
    /// Whether directory groups are imported as portal groups as well as accounts. While it is `false` the five
    /// group settings below are reported but not acted on.
    /// </summary>
    /// <example>true</example>
    public bool GroupMembership { get; set; }

    /// <summary>
    /// The distinguished name of the subtree the groups are read from, the counterpart of `userDN`.
    /// </summary>
    /// <example>ou=groups,dc=example,dc=com</example>
    // ReSharper disable once InconsistentNaming
    public string GroupDN { get; set; }

    /// <summary>
    /// The attribute on a user record that lists the groups it belongs to, which is how membership is resolved
    /// from the account's side.
    /// </summary>
    /// <example>memberOf</example>
    public string UserAttribute { get; set; }

    /// <summary>
    /// The LDAP search filter that decides which records under `groupDN` become portal groups.
    /// `(objectClass=posixGroup)`, the value the portal starts with, takes every one of them.
    /// </summary>
    /// <example>(objectClass=posixGroup)</example>
    public string GroupFilter { get; set; }

    /// <summary>
    /// The attribute on a group record that lists its members, which is how membership is resolved from the
    /// group's side.
    /// </summary>
    /// <example>member</example>
    public string GroupAttribute { get; set; }

    /// <summary>
    /// The attribute whose value becomes the portal group's name - typically `cn`.
    /// </summary>
    /// <example>cn</example>
    public string GroupNameAttribute { get; set; }

    /// <summary>
    /// Whether the portal binds with the credentials below instead of reading the directory anonymously. While
    /// it is `false` `login` and `password` are ignored.
    /// </summary>
    /// <example>true</example>
    public bool Authentication { get; set; }

    /// <summary>
    /// The account the portal binds to the directory as, which needs read access to the subtrees above but no
    /// write access anywhere.
    /// </summary>
    /// <example>admin@example.com</example>
    public string Login { get; set; }

    /// <summary>
    /// Always empty here: the stored password is stripped from the answer, so a client that sends these settings
    /// back has to supply it again rather than echoing what it read.
    /// </summary>
    /// <example></example>
    public string Password { get; set; }

    /// <summary>
    /// Whether the portal accepts the server's certificate even when it cannot verify it. It is what clears the
    /// `certificateConfirmRequest` an operation stops on.
    /// </summary>
    /// <example>true</example>
    public bool AcceptCertificate { get; set; }

    /// <summary>
    /// Whether every setting above still holds the value the portal starts out with, which is also what a portal
    /// that has never been configured reports.
    /// </summary>
    /// <example>true</example>
    public bool IsDefault { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class LdapSettingsDtoMapper
{
    [MapProperty(nameof(LdapSettings.Server), nameof(LdapSettingsDto.Server), Use = nameof(MapServer))]
    public static partial LdapSettingsDto MapToSettingsDto(this LdapSettings source);

    [UserMapping(Default = false)]
    private static string MapServer(string source)
    {
        return source.Replace("LDAP://", "");
    }
}
