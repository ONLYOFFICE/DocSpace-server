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
internal static partial class LdapOperationJobLogger
{
    [LoggerMessage(LogLevel.Error, "Can't save default LDAP settings.")]
    public static partial void ErrorSaveDefaultLdapSettings(this ILogger<LdapOperationJob> logger);

    [LoggerMessage(LogLevel.Information, "Start '{operationtype}' operation")]
    public static partial void InfoStartOperation(this ILogger<LdapOperationJob> logger, string operationtype);

    [LoggerMessage(LogLevel.Debug, "PrepareSettings()")]
    public static partial void DebugPrepareSettings(this ILogger<LdapOperationJob> logger);

    [LoggerMessage(LogLevel.Debug, "PrepareSettings() Error: {error}")]
    public static partial void DebugPrepareSettingsError(this ILogger<LdapOperationJob> logger, string error);

    [LoggerMessage(LogLevel.Debug, "ldapSettingsChecker.CheckSettings() Error: {error}")]
    public static partial void DebugCheckSettingsError(this ILogger<LdapOperationJob> logger, string error);

    [LoggerMessage(LogLevel.Error, "{error}")]
    public static partial void ErrorAuthorizing(this ILogger<LdapOperationJob> logger, string error, Exception exception);

    [LoggerMessage(LogLevel.Error, "TenantQuotaException")]
    public static partial void ErrorTenantQuota(this ILogger<LdapOperationJob> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "FormatException")]
    public static partial void ErrorFormatException(this ILogger<LdapOperationJob> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Internal server error")]
    public static partial void ErrorInternal(this ILogger<LdapOperationJob> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "LdapOperation finalization problem")]
    public static partial void ErrorLdapOperationFinalizationlProblem(this ILogger<LdapOperationJob> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Can't save LDAP settings.")]
    public static partial void ErrorSaveLdapSettings(this ILogger<LdapOperationJob> logger);

    [LoggerMessage(LogLevel.Debug, "{setting}")]
    public static partial void DebugLdapSettings(this ILogger<LdapOperationJob> logger, string setting);

    [LoggerMessage(LogLevel.Debug, "TurnOffLDAP()")]
    public static partial void DebugTurnOffLDAP(this ILogger<LdapOperationJob> logger);

    [LoggerMessage(LogLevel.Error, "CheckSettings(acceptCertificate={acceptCertificate}, cert thumbprint: {acceptCertificatehash})")]
    public static partial void ErrorCheckSettings(this ILogger<LdapOperationJob> logger, bool acceptCertificate, string acceptCertificatehash, Exception exception);

    [LoggerMessage(LogLevel.Debug, "CoreContext.UserManager.SaveUserInfo({userInfo})")]
    public static partial void DebugSaveUserInfo(this ILogger<LdapOperationJob> logger, string userInfo);

    [LoggerMessage(LogLevel.Debug, "SyncLDAPUsers()")]
    public static partial void DebugSyncLDAPUsers(this ILogger<LdapOperationJob> logger);

    [LoggerMessage(LogLevel.Debug, "SyncLDAPUsersInGroups()")]
    public static partial void DebugSyncLDAPUsersInGroups(this ILogger<LdapOperationJob> logger);

    [LoggerMessage(LogLevel.Information, "SyncLdapAvatar() Removing photo for '{guid}'")]
    public static partial void InfoSyncLdapAvatarsRemovingPhoto(this ILogger<LdapOperationJob> logger, Guid guid);

    [LoggerMessage(LogLevel.Debug, "SyncLdapAvatar() Found photo for '{sid}'")]
    public static partial void DebugSyncLdapAvatarsFoundPhoto(this ILogger<LdapOperationJob> logger, string sid);

    [LoggerMessage(LogLevel.Debug, "SyncLdapAvatar() Same hash, skipping.")]
    public static partial void DebugSyncLdapAvatarsSkipping(this ILogger<LdapOperationJob> logger);

    [LoggerMessage(LogLevel.Debug, "SyncLdapAvatar() Couldn't save photo for '{guid}'")]
    public static partial void DebugSyncLdapAvatarsCouldNotSavePhoto(this ILogger<LdapOperationJob> logger, Guid guid);

    [LoggerMessage(LogLevel.Debug, "TakeUsersRights() CurrentAccessRights is empty, skipping")]
    public static partial void DebugAccessRightsIsEmpty(this ILogger<LdapOperationJob> logger);

    [LoggerMessage(LogLevel.Debug, "TakeUsersRights() Attempting to take admin rights from yourself `{user}`, skipping")]
    public static partial void DebugAttemptingTakeAdminRights(this ILogger<LdapOperationJob> logger, string user);

    [LoggerMessage(LogLevel.Debug, "TakeUsersRights() Taking admin rights ({accessRight}) from '{user}'")]
    public static partial void DebugTakingAdminRights(this ILogger<LdapOperationJob> logger, LdapSettings.AccessRight accessRight, string user);

    [LoggerMessage(LogLevel.Debug, "GiveUsersRights() No ldap groups found for ({accessRight}) access rights, skipping")]
    public static partial void DebugGiveUsersRightsNoLdapGroups(this ILogger<LdapOperationJob> logger, LdapSettings.AccessRight accessRight);

    [LoggerMessage(LogLevel.Debug, "GiveUsersRights() Couldn't find portal group for '{sid}'")]
    public static partial void DebugGiveUsersRightsCouldNotFindPortalGroup(this ILogger<LdapOperationJob> logger, string sid);

    [LoggerMessage(LogLevel.Debug, "GiveUsersRights() Found '{countUsers}' users for group '{groupName}' ({groupId})")]
    public static partial void DebugGiveUsersRightsFoundUsersForGroup(this ILogger<LdapOperationJob> logger, int countUsers, string groupName, Guid groupId);

    [LoggerMessage(LogLevel.Debug, "GiveUsersRights() Cleared manually added user rights for '{userName}'")]
    public static partial void DebugGiveUsersRightsClearedAndAddedRights(this ILogger<LdapOperationJob> logger, string userName);

    [LoggerMessage(LogLevel.Debug, "Importer.GetDiscoveredUsersByAttributes() Success: Users count: {countUsers}")]
    public static partial void DebugGetDiscoveredUsersByAttributes(this ILogger<LdapOperationJob> logger, int countUsers);

    [LoggerMessage(LogLevel.Debug, "Importer.GetDiscoveredGroupsByAttributes() Success: Groups count: {countGroups}")]
    public static partial void DebugGetDiscoveredGroupsByAttributes(this ILogger<LdapOperationJob> logger, int countGroups);

    [LoggerMessage(LogLevel.Debug, "GetGroupsUsers() Success: Users count: {countUsers}")]
    public static partial void DebugGetGroupsUsers(this ILogger<LdapOperationJob> logger, int countUsers);

    [LoggerMessage(LogLevel.Debug, "RemoveOldDbUsers() Attempting to exclude yourself `{id}` from group or user filters, skipping.")]
    public static partial void DebugRemoveOldDbUsersAttemptingExcludeYourself(this ILogger<LdapOperationJob> logger, Guid id);

    [LoggerMessage(LogLevel.Information, "Progress: {percentage}% {status} {source}")]
    public static partial void InfoProgress(this ILogger<LdapOperationJob> logger, double percentage, string status, string source);

    [LoggerMessage(LogLevel.Error, "Wrong LDAP settings were received from client.")]
    public static partial void ErrorWrongLdapSettings(this ILogger<LdapOperationJob> logger);

    [LoggerMessage(LogLevel.Error, "settings.Server is null or empty.")]
    public static partial void ErrorServerIsNullOrEmpty(this ILogger<LdapOperationJob> logger);

    [LoggerMessage(LogLevel.Error, "settings.UserDN is null or empty.")]
    public static partial void ErrorUserDnIsNullOrEmpty(this ILogger<LdapOperationJob> logger);

    [LoggerMessage(LogLevel.Error, "settings.LoginAttribute is null or empty.")]
    public static partial void ErrorLoginAttributeIsNullOrEmpty(this ILogger<LdapOperationJob> logger);

    [LoggerMessage(LogLevel.Error, "settings.GroupDN is null or empty.")]
    public static partial void ErrorGroupDnIsNullOrEmpty(this ILogger<LdapOperationJob> logger);

    [LoggerMessage(LogLevel.Error, "settings.GroupAttribute is null or empty.")]
    public static partial void ErrorGroupAttributeIsNullOrEmpty(this ILogger<LdapOperationJob> logger);

    [LoggerMessage(LogLevel.Error, "settings.UserAttribute is null or empty.")]
    public static partial void ErrorUserAttributeIsNullOrEmpty(this ILogger<LdapOperationJob> logger);

    [LoggerMessage(LogLevel.Error, "settings.Login is null or empty.")]
    public static partial void ErrorloginIsNullOrEmpty(this ILogger<LdapOperationJob> logger);

    [LoggerMessage(LogLevel.Error, "settings.PasswordBytes is null.")]
    public static partial void ErrorPasswordBytesIsNullOrEmpty(this ILogger<LdapOperationJob> logger);

    [LoggerMessage(LogLevel.Error, "settings.Password is null or empty.")]
    public static partial void ErrorPasswordIsNullOrEmpty(this ILogger<LdapOperationJob> logger);

}