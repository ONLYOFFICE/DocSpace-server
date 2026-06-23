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

namespace ASC.ActiveDirectory.Log;
internal static partial class LdapUserImporterLogger
{
    [LoggerMessage(LogLevel.Debug, "LdapUserImporter.GetGroupUsers(Group name: {groupName})")]
    public static partial void DebugGetGroupUsers(this ILogger<LdapUserImporter> logger, string groupName);

    [LoggerMessage(LogLevel.Debug, "Found nested LDAP Group: {groupName}")]
    public static partial void DebugFoundNestedLdapGroup(this ILogger<LdapUserImporter> logger, string groupName);

    [LoggerMessage(LogLevel.Debug, "Skip already watched nested LDAP Group: {groupName}")]
    public static partial void DebugSkipAlreadyWatched(this ILogger<LdapUserImporter> logger, string groupName);

    [LoggerMessage(LogLevel.Error, "IsUserExistInGroups(login: '{login}' sid: '{sid}')")]
    public static partial void ErrorIsUserExistInGroups(this ILogger<LdapUserImporter> logger, string login, string sid, Exception exception);

    [LoggerMessage(LogLevel.Error, "GetAndCheckCurrentGroups(login: '{login}' sid: '{sid}')")]
    public static partial void ErrorGetAndCheckCurrentGroups(this ILogger<LdapUserImporter> logger, string login, string sid, Exception exception);

    [LoggerMessage(LogLevel.Debug, "TrySyncUserGroupMembership(groupname: '{groupName}' sid: '{sid}') no portal group found, creating")]
    public static partial void DebugTrySyncUserGroupMembershipCreatingPortalGroup(this ILogger<LdapUserImporter> logger, string groupName, string sid);

    [LoggerMessage(LogLevel.Debug, "TrySyncUserGroupMembership(username: '{userName}' sid: '{userSid}') adding user to group (groupname: '{groupName}' sid: '{groupSid}')")]
    public static partial void DebugTrySyncUserGroupMembershipAddingUserToGroup(this ILogger<LdapUserImporter> logger, string userName, string userSid, string groupName, string groupSid);

    [LoggerMessage(LogLevel.Debug, "TrySyncUserGroupMembership(username: '{userName}' sid: '{userSid}') removing user from group (groupname: '{groupName}' sid: '{groupSid}')")]
    public static partial void DebugTrySyncUserGroupMembershipRemovingUserFromGroup(this ILogger<LdapUserImporter> logger, string userName, string userSid, string groupName, string groupSid);

    [LoggerMessage(LogLevel.Error, "TryLoadLDAPUsers(): Incorrect filter. userFilter = {userFilter}")]
    public static partial void ErrorTryLoadLDAPUsersIncorrectUserFilter(this ILogger<LdapUserImporter> logger, string userFilter);

    [LoggerMessage(LogLevel.Error, "TryLoadLDAPGroups(): Incorrect group filter. groupFilter = {groupFilter}")]
    public static partial void ErrorTryLoadLDAPUsersIncorrectGroupFilter(this ILogger<LdapUserImporter> logger, string groupFilter);

    [LoggerMessage(LogLevel.Error, "LoadLDAPDomain(): Error")]
    public static partial void ErrorLoadLDAPDomain(this ILogger<LdapUserImporter> logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Login Attribute parameter ({loginAttributeParametr}) not found: DN = {distinguishedName}")]
    public static partial void DebugLoginAttributeParameterNotFound(this ILogger<LdapUserImporter> logger, string loginAttributeParametr, string distinguishedName);

    [LoggerMessage(LogLevel.Error, "Login Attribute parameter ({loginAttributeParametr}) not found: loginAttribute = {loginAttribute}")]
    public static partial void ErrorLoginAttributeParameterNotFound(this ILogger<LdapUserImporter> logger, string loginAttributeParametr, string loginAttribute, Exception exception);

    [LoggerMessage(LogLevel.Debug, "User Attribute parameter ({userAttributeParametr}) not found: DN = {distinguishedName}")]
    public static partial void DebugUserAttributeParameterNotFound(this ILogger<LdapUserImporter> logger, string userAttributeParametr, string distinguishedName);

    [LoggerMessage(LogLevel.Error, "User Attribute parameter ({userAttributeParametr}) not found: userAttr = {userAttribute}")]
    public static partial void ErrorUserAttributeParameterNotFound(this ILogger<LdapUserImporter> logger, string userAttributeParametr, string userAttribute, Exception exception);

    [LoggerMessage(LogLevel.Error, "Group Attribute parameter ({groupAttributeParametr}) not found: {groupAttribute}")]
    public static partial void ErrorGroupAttributeParameterNotFound(this ILogger<LdapUserImporter> logger, string groupAttributeParametr, string groupAttribute, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Group Name Attribute parameter ({groupNameAttributeParametr}) not found: {groupAttribute}")]
    public static partial void DebugGroupNameAttributeParameterNotFound(this ILogger<LdapUserImporter> logger, string groupNameAttributeParametr, string groupAttribute);

    [LoggerMessage(LogLevel.Debug, "LdapUserImporter.FindUsersByPrimaryGroup()")]
    public static partial void DebugFindUsersByPrimaryGroup(this ILogger<LdapUserImporter> logger);

    [LoggerMessage(LogLevel.Debug, "LdapUserImporter.FindUserByMember(user attr: {userAttribute})")]
    public static partial void DebugFindUserByMember(this ILogger<LdapUserImporter> logger, string userAttribute);

    [LoggerMessage(LogLevel.Debug, "LdapUserImporter.FindGroupByMember(member: {member})")]
    public static partial void DebugFindGroupByMember(this ILogger<LdapUserImporter> logger, string member);

    [LoggerMessage(LogLevel.Error, "FindLdapUser->ToUserInfo() failed")]
    public static partial void ErrorToUserInfo(this ILogger<LdapUserImporter> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "LDAP: DN: '{distinguishedName}' Login Attribute '{loginAttribute}' is empty")]
    public static partial void WarnLoginAttributeIsEmpty(this ILogger<LdapUserImporter> logger, string distinguishedName, string loginAttribute);

    [LoggerMessage(LogLevel.Debug, "FindLdapUsers(login '{login}') found: {usersCount} users")]
    public static partial void DebugFindLdapUsers(this ILogger<LdapUserImporter> logger, string login, int usersCount);

    [LoggerMessage(LogLevel.Debug, "LdapUserImporter->Login(login: '{login}', dn: '{sid}') failed. Error: missing DN or SID")]
    public static partial void DebugLdapUserImporterFailed(this ILogger<LdapUserImporter> logger, string login, string sid);

    [LoggerMessage(LogLevel.Debug, "LdapUserImporter.Login('{login}')")]
    public static partial void DebugLdapUserImporterLogin(this ILogger<LdapUserImporter> logger, string login);

    [LoggerMessage(LogLevel.Error, "LdapUserImporter->Login(login: '{login}') failed")]
    public static partial void ErrorLdapUserImporterLoginFailed(this ILogger<LdapUserImporter> logger, string login, Exception exception);
}