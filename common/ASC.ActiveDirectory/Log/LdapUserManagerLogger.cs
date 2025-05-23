﻿// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.ActiveDirectory.Log;
internal static partial class LdapUserManagerLogger
{
    [LoggerMessage(LogLevel.Debug, "TryAddLDAPUser(SID: {sid}): Email '{email}' UserName: {userName}")]
    public static partial void DebugTryAddLdapUser(this ILogger<LdapUserManager> logger, string sid, string email, string userName);

    [LoggerMessage(LogLevel.Debug, "TryAddLDAPUser(SID: {sid}): Email '{email}' already exists.")]
    public static partial void DebugUserAlredyExistsForEmail(this ILogger<LdapUserManager> logger, string sid, string email);

    [LoggerMessage(LogLevel.Debug, "TryAddLDAPUser(SID: {sid}): Username '{userName}' already exists.")]
    public static partial void DebugUserAlredyExistsForUserName(this ILogger<LdapUserManager> logger, string sid, string userName);

    [LoggerMessage(LogLevel.Debug, "TryAddLDAPUser(SID: {sid}): Username '{userName}' adding this user would exceed quota.")]
    public static partial void DebugExceedQuota(this ILogger<LdapUserManager> logger, string sid, string userName);

    [LoggerMessage(LogLevel.Debug, "CoreContext.UserManager.SaveUserInfo({userInfo})")]
    public static partial void DebugSaveUserInfo(this ILogger<LdapUserManager> logger, string userInfo);

    [LoggerMessage(LogLevel.Debug, "SecurityContext.SetUserPassword(ID:{id})")]
    public static partial void DebugSetUserPassword(this ILogger<LdapUserManager> logger, Guid id);

    [LoggerMessage(LogLevel.Error, "TryAddLDAPUser(UserName='{userName}' Sid='{sid}')")]
    public static partial void ErrorTryAddLdapUser(this ILogger<LdapUserManager> logger, string userName, string sid, Exception ex);

    [LoggerMessage(LogLevel.Debug, "TryChangeExistingUserName()")]
    public static partial void DebugTryChangeExistingUserName(this ILogger<LdapUserManager> logger);

    [LoggerMessage(LogLevel.Error, "TryChangeOtherUserName({userName})")]
    public static partial void ErrorTryChangeOtherUserName(this ILogger<LdapUserManager> logger, string userName, Exception ex);

    [LoggerMessage(LogLevel.Debug, "SyncUserLDAP(SID: {sid}, Username: '{userName}') ADD failed: Status is {status}")]
    public static partial void DebugSyncUserLdapFailedWithStatus(this ILogger<LdapUserManager> logger, string sid, string userName, string status);

    [LoggerMessage(LogLevel.Debug, "SyncUserLDAP(SID: {sid}, Username: '{userName}') ADD failed: Another ldap user with email '{email}' already exists")]
    public static partial void DebugSyncUserLdapFailedWithEmail(this ILogger<LdapUserManager> logger, string sid, string userName, string email);

    [LoggerMessage(LogLevel.Debug, "SyncUserLDAP(SID: {sid}, Username: '{userName}') No need to update, skipping")]
    public static partial void DebugSyncUserLdapSkipping(this ILogger<LdapUserManager> logger, string sid, string userName);

    [LoggerMessage(LogLevel.Debug, "SyncUserLDAP(SID: {sid}, Username: '{userName}') Userinfo is outdated, updating")]
    public static partial void DebugSyncUserLdapUpdaiting(this ILogger<LdapUserManager> logger, string sid, string userName);

    [LoggerMessage(LogLevel.Debug, "NeedUpdateUser by FirstName->portal: '{firstNamePortalUser}', ldap: '{firstNameLdapUser}'")]
    public static partial void DebugNeedUpdateUserByFirstName(this ILogger<LdapUserManager> logger, string firstNamePortalUser, string firstNameLdapUser);

    [LoggerMessage(LogLevel.Debug, "NeedUpdateUser by LastName -> portal: '{lastNamePortalUser}', ldap: '{lastNameLdapUser}'")]
    public static partial void DebugNeedUpdateUserByLastName(this ILogger<LdapUserManager> logger, string lastNamePortalUser, string lastNameLdapUser);

    [LoggerMessage(LogLevel.Debug, "NeedUpdateUser by UserName -> portal: '{userNamePorta}', ldap: '{userNameLdap}'")]
    public static partial void DebugNeedUpdateUserByUserName(this ILogger<LdapUserManager> logger, string userNamePorta, string userNameLdap);

    [LoggerMessage(LogLevel.Debug, "NeedUpdateUser by Email -> portal: '{emailPortalUser}', ldap: '{emailLdapUser}'")]
    public static partial void DebugNeedUpdateUserByEmail(this ILogger<LdapUserManager> logger, string emailPortalUser, string emailLdapUser);

    [LoggerMessage(LogLevel.Debug, "NeedUpdateUser by Sid -> portal: '{sidPortalUser}', ldap: '{sidLdapUser}'")]
    public static partial void DebugNeedUpdateUserBySid(this ILogger<LdapUserManager> logger, string sidPortalUser, string sidLdapUser);

    [LoggerMessage(LogLevel.Debug, "NeedUpdateUser by Title -> portal: '{titlePortalUser}', ldap: '{titleLdapUser}'")]
    public static partial void DebugNeedUpdateUserByTitle(this ILogger<LdapUserManager> logger, string titlePortalUser, string titleLdapUser);

    [LoggerMessage(LogLevel.Debug, "NeedUpdateUser by Location -> portal: '{locationPortalUser}', ldap: '{locationLdapUser}'")]
    public static partial void DebugNeedUpdateUserByLocation(this ILogger<LdapUserManager> logger, string locationPortalUser, string locationLdapUser);

    [LoggerMessage(LogLevel.Debug, "NeedUpdateUser by ActivationStatus -> portal: '{activationStatusPortalUser}', ldap: '{activationStatusLdapUser}'")]
    public static partial void DebugNeedUpdateUserByActivationStatus(this ILogger<LdapUserManager> logger, EmployeeActivationStatus activationStatusPortalUser, EmployeeActivationStatus activationStatusLdapUser);

    [LoggerMessage(LogLevel.Debug, "NeedUpdateUser by Status -> portal: '{statusPortalUser}', ldap: '{statusLdapUser}'")]
    public static partial void DebugNeedUpdateUserByStatus(this ILogger<LdapUserManager> logger, EmployeeStatus statusPortalUser, EmployeeStatus statusLdapUser);

    [LoggerMessage(LogLevel.Debug, "NeedUpdateUser by Contacts -> portal: '{contactsPortalUser}', ldap: '{contactsLdapUser}'")]
    public static partial void DebugNeedUpdateUserByContacts(this ILogger<LdapUserManager> logger, string contactsPortalUser, string contactsLdapUser);

    [LoggerMessage(LogLevel.Debug, "NeedUpdateUser by PrimaryPhone -> portal: '{primaryPhonePortalUser}', ldap: '{primaryPhoneLdapUser}'")]
    public static partial void DebugNeedUpdateUserByPrimaryPhone(this ILogger<LdapUserManager> logger, string primaryPhonePortalUser, string primaryPhoneLdapUser);

    [LoggerMessage(LogLevel.Debug, "NeedUpdateUser by BirthDate -> portal: '{birthDatePortalUser}', ldap: '{birthDateLdapUser}'")]
    public static partial void DebugNeedUpdateUserByBirthDate(this ILogger<LdapUserManager> logger, string birthDatePortalUser, string birthDateLdapUser);

    [LoggerMessage(LogLevel.Debug, "NeedUpdateUser by Sex -> portal: '{sexPortalUser}', ldap: '{sexDateLdapUser}'")]
    public static partial void DebugNeedUpdateUserBySex(this ILogger<LdapUserManager> logger, string sexPortalUser, string sexDateLdapUser);

    [LoggerMessage(LogLevel.Debug, "NeedUpdateUser")]
    public static partial void DebugNeedUpdateUser(this ILogger<LdapUserManager> logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "TryUpdateUserWithLDAPInfo()")]
    public static partial void DebugTryUpdateUserWithLdapInfo(this ILogger<LdapUserManager> logger);

    [LoggerMessage(LogLevel.Debug, "UpdateUserWithLDAPInfo(ID: {userId}): New username already exists. (Old: '{OldUserInfo})' New: '{NewUserInfo}'")]
    public static partial void DebugUpdateUserUserNameAlredyExists(this ILogger<LdapUserManager> logger, Guid userId, string oldUserInfo, string newUserInfo);

    [LoggerMessage(LogLevel.Debug, "UpdateUserWithLDAPInfo(ID: {userId}): New email already exists. (Old: '{oldEmail})' New: '{newEmail}'")]
    public static partial void DebugUpdateUserEmailAlreadyExists(this ILogger<LdapUserManager> logger, Guid userId, string oldEmail, string newEmail);

    [LoggerMessage(LogLevel.Error, "UpdateUserWithLDAPInfo(Id='{userId}' UserName='{userName}' Sid='{sid}')")]
    public static partial void ErrorUpdateUserWithLDAPInfo(this ILogger<LdapUserManager> logger, Guid userId, string userName, string sid, Exception exception);

    [LoggerMessage(LogLevel.Debug, "TryGetAndSyncLdapUserInfo(login: \"{login}\")")]
    public static partial void DebugTryGetAndSyncLdapUserInfo(this ILogger<LdapUserManager> logger, string login);

    [LoggerMessage(LogLevel.Debug, "NovellLdapUserImporter.Login('{login}') failed.")]
    public static partial void DebugNovellLdapUserImporterLoginFailed(this ILogger<LdapUserManager> logger, string login);

    [LoggerMessage(LogLevel.Debug, "TryCheckAndSyncToLdapUser(Username: '{userName}', Email: {email}, DN: {distinguishedName})")]
    public static partial void DebugTryCheckAndSyncToLdapUser(this ILogger<LdapUserManager> logger, string userName, string email, string distinguishedName);

    [LoggerMessage(LogLevel.Debug, "TryCheckAndSyncToLdapUser() failed")]
    public static partial void DebugTryCheckAndSyncToLdapUserFailed(this ILogger<LdapUserManager> logger);

    [LoggerMessage(LogLevel.Error, "TryGetLdapUserInfo(login: '{login}')")]
    public static partial void ErrorTryGetLdapUserInfoFailed(this ILogger<LdapUserManager> logger, string login, Exception exception);

    [LoggerMessage(LogLevel.Error, "TrySyncLdapUser(SID: '{sid}', Email: {email})")]
    public static partial void ErrorTrySyncLdapUser(this ILogger<LdapUserManager> logger, string sid, string email, Exception exception);

    [LoggerMessage(LogLevel.Debug, "TryGetAndSyncLdapUserInfo(login: \"{login}\") disabling user {userInfo} due to not being included in any ldap group")]
    public static partial void DebugTryGetAndSyncLdapUserInfoDisablingUser(this ILogger<LdapUserManager> logger, string login, UserInfo userInfo);


}
