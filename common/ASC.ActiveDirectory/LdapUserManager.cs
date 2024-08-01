﻿// (c) Copyright Ascensio System SIA 2009-2024
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

using ASC.Web.Core.Utility;

using Constants = ASC.Core.Users.Constants;
using Mapping = ASC.ActiveDirectory.Base.Settings.LdapSettings.MappingFields;
using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.ActiveDirectory;
[Scope]
public class LdapUserManager(ILogger<LdapUserManager> logger,
    IServiceProvider serviceProvider,
    UserManager userManager,
    TenantManager tenantManager,
    TenantUtil tenantUtil,
    SecurityContext securityContext,
    CommonLinkUtility commonLinkUtility,
    SettingsManager settingsManager,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    UserFormatter userFormatter,
    NovellLdapUserImporter novellLdapUserImporter,
    IServiceScopeFactory serviceScopeFactory,
    TenantQuotaFeatureStatHelper tenantQuotaFeatureStatHelper,
    QuotaSocketManager quotaSocketManager)
{
    private LdapLocalization _resource;

    public void Init(LdapLocalization resource = null)
    {
        _resource = resource ?? new LdapLocalization();
    }

    private async Task<bool> TestUniqueUserNameAsync(string uniqueName)
    {
        return !string.IsNullOrEmpty(uniqueName) && Equals(await userManager.GetUserByUserNameAsync(uniqueName), Constants.LostUser);
    }

    private async Task<string> MakeUniqueNameAsync(UserInfo userInfo)
    {
        if (string.IsNullOrEmpty(userInfo.Email))
        {
            throw new ArgumentException(_resource.ErrorEmailEmpty, nameof(userInfo));
        }

        var uniqueName = new MailAddress(userInfo.Email).User;
        var startUniqueName = uniqueName;
        var i = 0;
        while (!await TestUniqueUserNameAsync(uniqueName))
        {
            uniqueName = $"{startUniqueName}{(++i).ToString(CultureInfo.InvariantCulture)}";
        }
        return uniqueName;
    }

    private async Task<bool> CheckUniqueEmailAsync(Guid userId, string email)
    {
        var foundUser = await userManager.GetUserByEmailAsync(email);
        return Equals(foundUser, Constants.LostUser) || foundUser.Id == userId;
    }

    public async Task<UserInfo> TryAddLDAPUser(UserInfo ldapUserInfo, bool onlyGetChanges)
    {
        var portalUserInfo = Constants.LostUser;

        try
        {
            ArgumentNullException.ThrowIfNull(ldapUserInfo);

            logger.DebugTryAddLdapUser(ldapUserInfo.Sid, ldapUserInfo.Email, ldapUserInfo.UserName);

            if (!await CheckUniqueEmailAsync(ldapUserInfo.Id, ldapUserInfo.Email))
            {
                logger.DebugUserAlredyExistsForEmail(ldapUserInfo.Sid, ldapUserInfo.Email);

                return portalUserInfo;
            }

            if (!await TryChangeExistingUserNameAsync(ldapUserInfo.UserName, onlyGetChanges))
            {
                logger.DebugUserAlredyExistsForUserName(ldapUserInfo.Sid, ldapUserInfo.UserName);

                return portalUserInfo;
            }

            ldapUserInfo.WorkFromDate ??= tenantUtil.DateTimeNow();

            if (onlyGetChanges)
            {
                portalUserInfo = ldapUserInfo;
                return portalUserInfo;
            }

            logger.DebugSaveUserInfo(ldapUserInfo.GetUserInfoString());

            var settings = await settingsManager.LoadAsync<LdapSettings>();

            portalUserInfo = await userManager.SaveUserInfo(ldapUserInfo, EmployeeType.Collaborator);

            var groupId = settings.UsersType switch
            {
                EmployeeType.User => Constants.GroupUser.ID,
                EmployeeType.DocSpaceAdmin => Constants.GroupAdmin.ID,
                EmployeeType.Collaborator => Constants.GroupCollaborator.ID,
                _ => Guid.Empty
            };

            if (groupId != Guid.Empty)
            {
                await userManager.AddUserIntoGroupAsync(portalUserInfo.Id, groupId, true);
            }
            else if (settings.UsersType == EmployeeType.RoomAdmin)
            {
                var (name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<CountPaidUserFeature, int>();
                _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
            }

            var quotaSettings = await settingsManager.LoadAsync<TenantUserQuotaSettings>();
            if (quotaSettings.EnableQuota)
            {
                await settingsManager.SaveAsync(new UserQuotaSettings { UserQuota = ldapUserInfo.LdapQouta }, ldapUserInfo.Id);
            }


            var passwordHash = LdapUtils.GeneratePassword();

            logger.DebugSetUserPassword(portalUserInfo.Id);

            await securityContext.SetUserPasswordHashAsync(portalUserInfo.Id, passwordHash);
        }
        catch (TenantQuotaException)
        {
            // rethrow if quota
            throw;
        }
        catch (Exception ex)
        {
            if (ldapUserInfo != null)
            {
                logger.ErrorTryAddLdapUser(ldapUserInfo.UserName, ldapUserInfo.Sid, ex);
            }
        }

        return portalUserInfo;
    }

    private async Task<bool> TryChangeExistingUserNameAsync(string ldapUserName, bool onlyGetChanges)
    {
        try
        {
            if (string.IsNullOrEmpty(ldapUserName))
            {
                return false;
            }

            var otherUser = await userManager.GetUserByUserNameAsync(ldapUserName);

            if (Equals(otherUser, Constants.LostUser))
            {
                return true;
            }

            if (otherUser.IsLDAP())
            {
                return false;
            }

            otherUser.UserName = await MakeUniqueNameAsync(otherUser);

            if (onlyGetChanges)
            {
                return true;
            }

            logger.DebugTryChangeExistingUserName();

            logger.DebugSaveUserInfo(otherUser.GetUserInfoString());

            await userManager.UpdateUserInfoAsync(otherUser);

            return true;
        }
        catch (Exception ex)
        {
            logger.ErrorTryChangeOtherUserName(ldapUserName, ex);
        }

        return false;
    }

    public async Task<UserInfoAndLdapChangeCollectionWrapper> GetLDAPSyncUserChangeAsync(UserInfo ldapUserInfo, List<UserInfo> ldapUsers)
    {
        return await SyncLDAPUserAsync(ldapUserInfo, ldapUsers, true);
    }

    public async Task<UserInfo> SyncLDAPUserAsync(UserInfo ldapUserInfo, List<UserInfo> ldapUsers = null)
    {
        return (await SyncLDAPUserAsync(ldapUserInfo, ldapUsers, false)).UserInfo;
    }

    private async Task<UserInfoAndLdapChangeCollectionWrapper> SyncLDAPUserAsync(UserInfo ldapUserInfo, IReadOnlyCollection<UserInfo> ldapUsers, bool onlyGetChanges = false)
    {
        UserInfo userToUpdate;

        var wrapper = new UserInfoAndLdapChangeCollectionWrapper
        {
            LdapChangeCollection = new LdapChangeCollection(userFormatter),
            UserInfo = Constants.LostUser
        };

        var userBySid = await userManager.GetUserBySidAsync(ldapUserInfo.Sid);

        if (Equals(userBySid, Constants.LostUser))
        {
            var userByEmail = await userManager.GetUserByEmailAsync(ldapUserInfo.Email);

            if (Equals(userByEmail, Constants.LostUser))
            {
                if (ldapUserInfo.Status != EmployeeStatus.Active)
                {
                    if (onlyGetChanges)
                    {
                        wrapper.LdapChangeCollection.SetSkipUserChange(ldapUserInfo);
                    }

                    logger.DebugSyncUserLdapFailedWithStatus(ldapUserInfo.Sid, ldapUserInfo.UserName,
                        Enum.GetName(typeof(EmployeeStatus), ldapUserInfo.Status));

                    return wrapper;
                }
                wrapper.UserInfo = await TryAddLDAPUser(ldapUserInfo, onlyGetChanges);
                if (wrapper.UserInfo.Equals(Constants.LostUser))
                {
                    if (onlyGetChanges)
                    {
                        wrapper.LdapChangeCollection.SetSkipUserChange(ldapUserInfo);
                    }

                    return wrapper;
                }

                if (onlyGetChanges)
                {
                    wrapper.LdapChangeCollection.SetAddUserChange(wrapper.UserInfo, logger);
                }

                if (!onlyGetChanges && (await settingsManager.LoadAsync<LdapSettings>()).SendWelcomeEmail &&
                    (ldapUserInfo.ActivationStatus != EmployeeActivationStatus.AutoGenerated))
                {
                    using var scope = serviceProvider.CreateScope();
                    var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
                    var source = scope.ServiceProvider.GetRequiredService<LdapNotifySource>();
                    source.Init(await tenantManager.GetCurrentTenantAsync());
                    var workContext = scope.ServiceProvider.GetRequiredService<WorkContext>();
                    var client = workContext.RegisterClient(scope.ServiceProvider, source);
                    var urlShortener = scope.ServiceProvider.GetRequiredService<IUrlShortener>();

                    var confirmLink = await commonLinkUtility.GetConfirmationEmailUrlAsync(ldapUserInfo.Email, ConfirmType.EmailActivation);
                   
                    await client.SendNoticeToAsync(
                        NotifyConstants.ActionLdapActivation,
                        [new DirectRecipient(ldapUserInfo.Email, null, [ldapUserInfo.Email], false)],
                        [Core.Configuration.Constants.NotifyEMailSenderSysName],
                        null,
                        new TagValue(NotifyConstants.TagUserName, ldapUserInfo.DisplayUserName(displayUserSettingsHelper)),
                        new TagValue(NotifyConstants.TagUserEmail, ldapUserInfo.Email),
                        new TagValue(NotifyConstants.TagMyStaffLink, commonLinkUtility.GetFullAbsolutePath(commonLinkUtility.GetMyStaff())),
                        NotifyConstants.TagOrangeButton(_resource.NotifyButtonJoin,  await urlShortener.GetShortenLinkAsync(confirmLink)),
                        new TagValue(NotifyCommonTags.WithoutUnsubscribe, true));
                }

                return wrapper;
            }

            if (userByEmail.IsLDAP())
            {
                if (ldapUsers == null || ldapUsers.Any(u => u.Sid.Equals(userByEmail.Sid)))
                {
                    if (onlyGetChanges)
                    {
                        wrapper.LdapChangeCollection.SetSkipUserChange(ldapUserInfo);
                    }

                    logger.DebugSyncUserLdapFailedWithEmail(
                        ldapUserInfo.Sid, ldapUserInfo.UserName, ldapUserInfo.Email);

                    return wrapper;
                }
            }

            userToUpdate = userByEmail;
        }
        else
        {
            userToUpdate = userBySid;
        }

        UpdateLdapUserContacts(ldapUserInfo, userToUpdate.ContactsList);

        if (!await NeedUpdateUserAsync(userToUpdate, ldapUserInfo))
        {
            logger.DebugSyncUserLdapSkipping(ldapUserInfo.Sid, ldapUserInfo.UserName);
            if (onlyGetChanges)
            {
                wrapper.LdapChangeCollection.SetNoneUserChange(ldapUserInfo);
            }
            wrapper.UserInfo = userBySid;
            return wrapper;
        }

        logger.DebugSyncUserLdapUpdaiting(ldapUserInfo.Sid, ldapUserInfo.UserName);

        var (updated, uf) = await TryUpdateUserWithLDAPInfoAsync(userToUpdate, ldapUserInfo, onlyGetChanges);

        if (!updated)
        {
            if (onlyGetChanges)
            {
                wrapper.LdapChangeCollection.SetSkipUserChange(ldapUserInfo);
            }

            return wrapper;
        }

        if (onlyGetChanges)
        {
            wrapper.LdapChangeCollection.SetUpdateUserChange(ldapUserInfo, uf, logger);
        }
        wrapper.UserInfo = uf;
        return wrapper;
    }

    private const string EXT_MOB_PHONE = "extmobphone";
    private const string EXT_MAIL = "extmail";
    private const string EXT_PHONE = "extphone";
    private const string EXT_SKYPE = "extskype";

    private static void UpdateLdapUserContacts(UserInfo ldapUser, IReadOnlyList<string> portalUserContacts)
    {
        if (portalUserContacts == null || !portalUserContacts.Any())
        {
            return;
        }

        var newContacts = new List<string>(ldapUser.ContactsList);

        for (var i = 0; i < portalUserContacts.Count; i += 2)
        {
            if (portalUserContacts[i] == EXT_MOB_PHONE || portalUserContacts[i] == EXT_MAIL
                || portalUserContacts[i] == EXT_PHONE || portalUserContacts[i] == EXT_SKYPE)
            {
                continue;
            }

            if (i + 1 >= portalUserContacts.Count)
            {
                continue;
            }

            newContacts.Add(portalUserContacts[i]);
            newContacts.Add(portalUserContacts[i + 1]);
        }

        ldapUser.ContactsList = newContacts;
    }

    private async Task<bool> NeedUpdateUserAsync(UserInfo portalUser, UserInfo ldapUser)
    {
        var needUpdate = false;

        try
        {
            var settings = await settingsManager.LoadAsync<LdapSettings>();

            Func<string, string, bool> notEqual =
                (f1, f2) =>
                    f1 == null && f2 != null ||
                    f1 != null && !f1.Equals(f2, StringComparison.InvariantCultureIgnoreCase);

            if (notEqual(portalUser.FirstName, ldapUser.FirstName))
            {
                logger.DebugNeedUpdateUserByFirstName(portalUser.FirstName ?? "NULL",
                    ldapUser.FirstName ?? "NULL");
                needUpdate = true;
            }

            if (notEqual(portalUser.LastName, ldapUser.LastName))
            {
                logger.DebugNeedUpdateUserByLastName(portalUser.LastName ?? "NULL",
                    ldapUser.LastName ?? "NULL");
                needUpdate = true;
            }

            if (notEqual(portalUser.UserName, ldapUser.UserName))
            {
                logger.DebugNeedUpdateUserByUserName(portalUser.UserName ?? "NULL",
                    ldapUser.UserName ?? "NULL");
                needUpdate = true;
            }

            if (notEqual(portalUser.Email, ldapUser.Email) &&
                (ldapUser.ActivationStatus != EmployeeActivationStatus.AutoGenerated
                    || ldapUser.ActivationStatus == EmployeeActivationStatus.AutoGenerated && portalUser.ActivationStatus == EmployeeActivationStatus.AutoGenerated))
            {
                logger.DebugNeedUpdateUserByEmail(portalUser.Email ?? "NULL",
                    ldapUser.Email ?? "NULL");
                needUpdate = true;
            }

            if (notEqual(portalUser.Sid, ldapUser.Sid))
            {
                logger.DebugNeedUpdateUserBySid(portalUser.Sid ?? "NULL",
                    ldapUser.Sid ?? "NULL");
                needUpdate = true;
            }

            if (settings.LdapMapping.ContainsKey(Mapping.TitleAttribute) && notEqual(portalUser.Title, ldapUser.Title))
            {
                logger.DebugNeedUpdateUserByTitle(portalUser.Title ?? "NULL",
                    ldapUser.Title ?? "NULL");
                needUpdate = true;
            }

            if (settings.LdapMapping.ContainsKey(Mapping.LocationAttribute) && notEqual(portalUser.Location, ldapUser.Location))
            {
                logger.DebugNeedUpdateUserByLocation(portalUser.Location ?? "NULL",
                    ldapUser.Location ?? "NULL");
                needUpdate = true;
            }

            if (portalUser.ActivationStatus != ldapUser.ActivationStatus &&
                (!portalUser.ActivationStatus.HasFlag(EmployeeActivationStatus.Activated) || portalUser.Email != ldapUser.Email) &&
                ldapUser.ActivationStatus != EmployeeActivationStatus.AutoGenerated)
            {
                logger.DebugNeedUpdateUserByActivationStatus(portalUser.ActivationStatus,
                    ldapUser.ActivationStatus);
                needUpdate = true;
            }

            if (portalUser.Status != ldapUser.Status)
            {
                logger.DebugNeedUpdateUserByStatus(portalUser.Status,
                    ldapUser.Status);
                needUpdate = true;
            }

            if (portalUser.ContactsList == null && ldapUser.ContactsList.Count != 0 || portalUser.ContactsList != null && (ldapUser.ContactsList.Count != portalUser.ContactsList.Count ||
                !ldapUser.Contacts.All(portalUser.Contacts.Contains)))
            {
                logger.DebugNeedUpdateUserByContacts(string.Join("|", portalUser.Contacts),
                    string.Join("|", ldapUser.Contacts));
                needUpdate = true;
            }

            if (settings.LdapMapping.ContainsKey(Mapping.MobilePhoneAttribute) && notEqual(portalUser.MobilePhone, ldapUser.MobilePhone))
            {
                logger.DebugNeedUpdateUserByPrimaryPhone(portalUser.MobilePhone ?? "NULL",
                    ldapUser.MobilePhone ?? "NULL");
                needUpdate = true;
            }

            if (settings.LdapMapping.ContainsKey(Mapping.BirthDayAttribute) && portalUser.BirthDate == null && ldapUser.BirthDate != null || portalUser.BirthDate != null && !portalUser.BirthDate.Equals(ldapUser.BirthDate))
            {
                logger.DebugNeedUpdateUserByBirthDate(portalUser.BirthDate.ToString() ?? "NULL",
                    ldapUser.BirthDate.ToString() ?? "NULL");
                needUpdate = true;
            }

            if (settings.LdapMapping.ContainsKey(Mapping.GenderAttribute) && portalUser.Sex == null && ldapUser.Sex != null || portalUser.Sex != null && !portalUser.Sex.Equals(ldapUser.Sex))
            {
                logger.DebugNeedUpdateUserBySex(portalUser.Sex.ToString() ?? "NULL",
                    ldapUser.Sex.ToString() ?? "NULL");
                needUpdate = true;
            }
        }
        catch (Exception ex)
        {
            logger.DebugNeedUpdateUser(ex);
        }

        return needUpdate;
    }

    private async Task<(bool, UserInfo)> TryUpdateUserWithLDAPInfoAsync(UserInfo userToUpdate, UserInfo updateInfo, bool onlyGetChanges)
    {
        var portlaUserInfo = Constants.LostUser;
        try
        {
            logger.DebugTryUpdateUserWithLdapInfo();

            var settings = await settingsManager.LoadAsync<LdapSettings>();

            if (!userToUpdate.UserName.Equals(updateInfo.UserName, StringComparison.InvariantCultureIgnoreCase)
                && !await TryChangeExistingUserNameAsync(updateInfo.UserName, onlyGetChanges))
            {
                logger.DebugUpdateUserUserNameAlredyExists(userToUpdate.Id, userToUpdate.UserName, updateInfo.UserName);

                return (false, portlaUserInfo);
            }

            if (!userToUpdate.Email.Equals(updateInfo.Email, StringComparison.InvariantCultureIgnoreCase)
                && !await CheckUniqueEmailAsync(userToUpdate.Id, updateInfo.Email))
            {
                logger.DebugUpdateUserEmailAlreadyExists(userToUpdate.Id, userToUpdate.Email, updateInfo.Email);

                return (false, portlaUserInfo);
            }

            if (userToUpdate.Email != updateInfo.Email && !(updateInfo.ActivationStatus == EmployeeActivationStatus.AutoGenerated &&
                userToUpdate.ActivationStatus == (EmployeeActivationStatus.AutoGenerated | EmployeeActivationStatus.Activated)))
            {
                userToUpdate.ActivationStatus = updateInfo.ActivationStatus;
                userToUpdate.Email = updateInfo.Email;
            }

            userToUpdate.UserName = updateInfo.UserName;
            userToUpdate.FirstName = updateInfo.FirstName;
            userToUpdate.LastName = updateInfo.LastName;
            userToUpdate.Sid = updateInfo.Sid;
            userToUpdate.Contacts = updateInfo.Contacts;

            if (settings.LdapMapping.ContainsKey(Mapping.TitleAttribute))
            {
                userToUpdate.Title = updateInfo.Title;
            }

            if (settings.LdapMapping.ContainsKey(Mapping.LocationAttribute))
            {
                userToUpdate.Location = updateInfo.Location;
            }

            if (settings.LdapMapping.ContainsKey(Mapping.GenderAttribute))
            {
                userToUpdate.Sex = updateInfo.Sex;
            }

            if (settings.LdapMapping.ContainsKey(Mapping.BirthDayAttribute))
            {
                userToUpdate.BirthDate = updateInfo.BirthDate;
            }

            if (settings.LdapMapping.ContainsKey(Mapping.MobilePhoneAttribute))
            {
                userToUpdate.MobilePhone = updateInfo.MobilePhone;
            }

            if (!userToUpdate.IsOwner(await tenantManager.GetCurrentTenantAsync())) // Owner must never be terminated by LDAP!
            {
                userToUpdate.Status = updateInfo.Status;
            }

            if (!onlyGetChanges)
            {
                logger.DebugSaveUserInfo(userToUpdate.GetUserInfoString());

                portlaUserInfo = await userManager.UpdateUserInfoAsync(userToUpdate);
            }

            return (true, portlaUserInfo);
        }
        catch (Exception ex)
        {
            logger.ErrorUpdateUserWithLDAPInfo(userToUpdate.Id, userToUpdate.UserName,
                userToUpdate.Sid, ex);
        }

        return (false, portlaUserInfo);
    }

    public async Task<UserInfo> TryGetAndSyncLdapUserInfo(string login, string password)
    {
        var userInfo = Constants.LostUser;

        try
        {
            var settings = await settingsManager.LoadAsync<LdapSettings>();

            if (!settings.EnableLdapAuthentication)
            {
                return userInfo;
            }

            logger.DebugTryGetAndSyncLdapUserInfo(login);

            novellLdapUserImporter.Init(settings, _resource);

            var ldapUserInfo = await novellLdapUserImporter.LoginAsync(login, password);

            if (ldapUserInfo == null || ldapUserInfo.Item1.Equals(Constants.LostUser))
            {
                logger.DebugNovellLdapUserImporterLoginFailed(login);
                return userInfo;
            }

            var portalUser = await userManager.GetUserBySidAsync(ldapUserInfo.Item1.Sid);

            if (portalUser.Status == EmployeeStatus.Terminated || portalUser.Equals(Constants.LostUser))
            {
                if (!ldapUserInfo.Item2.IsDisabled)
                {
                    logger.DebugTryCheckAndSyncToLdapUser(ldapUserInfo.Item1.UserName, ldapUserInfo.Item1.Email, ldapUserInfo.Item2.DistinguishedName);
                    userInfo = await TryCheckAndSyncToLdapUser(ldapUserInfo, novellLdapUserImporter);
                    if (Equals(userInfo, Constants.LostUser))
                    {
                        logger.DebugTryCheckAndSyncToLdapUserFailed();
                        return userInfo;
                    }
                }
                else
                {
                    return userInfo;
                }
            }
            else
            {
                logger.DebugTryCheckAndSyncToLdapUser(ldapUserInfo.Item1.UserName, ldapUserInfo.Item1.Email, ldapUserInfo.Item2.DistinguishedName);

                var tenant = await tenantManager.GetCurrentTenantAsync();

                _ = Task.Run(Action);

                if (ldapUserInfo.Item2.IsDisabled)
                {
                    logger.DebugTryGetAndSyncLdapUserInfo(login);
                    return userInfo;
                }

                userInfo = portalUser;

                async Task Action()
                {
                    await using var scope = serviceScopeFactory.CreateAsyncScope();
                    var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
                    var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager>();
                    var cookiesManager = scope.ServiceProvider.GetRequiredService<CookiesManager>();
                    var log = scope.ServiceProvider.GetRequiredService<ILogger<LdapUserManager>>();

                    tenantManager.SetCurrentTenant(tenant);
                    await securityContext.AuthenticateMeAsync(Core.Configuration.Constants.CoreSystem);

                    var uInfo = await SyncLDAPUserAsync(ldapUserInfo.Item1);

                    var newLdapUserInfo = new Tuple<UserInfo, LdapObject>(uInfo, ldapUserInfo.Item2);

                    if (novellLdapUserImporter.Settings.GroupMembership)
                    {
                        if (!(await novellLdapUserImporter.TrySyncUserGroupMembership(newLdapUserInfo)))
                        {
                            log.DebugTryGetAndSyncLdapUserInfoDisablingUser(login, uInfo);
                            uInfo.Status = EmployeeStatus.Terminated;
                            uInfo.Sid = null;
                            await userManager.UpdateUserInfoAsync(uInfo);
                            await cookiesManager.ResetUserCookieAsync(uInfo.Id);
                        }
                    }
                }
            }

            return userInfo;
        }
        catch (Exception ex)
        {
            logger.ErrorTryGetLdapUserInfoFailed(login, ex);
            userInfo = Constants.LostUser;
            return userInfo;
        }
    }

    private async Task<UserInfo> TryCheckAndSyncToLdapUser(Tuple<UserInfo, LdapObject> ldapUserInfo, LdapUserImporter importer)
    {
        UserInfo userInfo;
        try
        {
            await securityContext.AuthenticateMeAsync(Core.Configuration.Constants.CoreSystem);

            userInfo = await SyncLDAPUserAsync(ldapUserInfo.Item1);

            if (userInfo == null || userInfo.Equals(Constants.LostUser))
            {
                throw new Exception("The user did not pass the configuration check by ldap user settings");
            }

            var newLdapUserInfo = new Tuple<UserInfo, LdapObject>(userInfo, ldapUserInfo.Item2);

            if (!importer.Settings.GroupMembership)
            {
                return userInfo;
            }

            if (!(await importer.TrySyncUserGroupMembership(newLdapUserInfo)))
            {
                userInfo.Sid = null;
                userInfo.Status = EmployeeStatus.Terminated;
                await userManager.UpdateUserInfoAsync(userInfo);
                throw new Exception("The user did not pass the configuration check by ldap group settings");
            }

            return userInfo;
        }
        catch (Exception ex)
        {
            logger.ErrorTrySyncLdapUser(ldapUserInfo.Item1.Sid,
                ldapUserInfo.Item1.Email, ex);
        }
        finally
        {
            securityContext.Logout();
        }

        userInfo = Constants.LostUser;
        return userInfo;
    }
}
