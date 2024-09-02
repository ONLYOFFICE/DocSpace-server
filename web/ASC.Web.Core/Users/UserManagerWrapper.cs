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

using ASC.FederatedLogin;

using Constants = ASC.Core.Users.Constants;

namespace ASC.Web.Core.Users;

/// <summary>
/// Web studio user manager helper
/// </summary>
/// 
[Scope]
public sealed class UserManagerWrapper(
    AuditEventsRepository auditEventsRepository,
    CommonLinkUtility commonLinkUtility,
    IUrlShortener urlShortener,
    StudioNotifyService studioNotifyService,
    UserManager userManager,
    SecurityContext securityContext,
    CustomNamingPeople customNamingPeople,
    TenantUtil tenantUtil,
    SettingsManager settingsManager,
    UserFormatter userFormatter,
    CountPaidUserChecker countPaidUserChecker,
    TenantManager tenantManager,
    WebItemSecurityCache webItemSecurityCache,
    QuotaSocketManager quotaSocketManager,
    TenantQuotaFeatureStatHelper tenantQuotaFeatureStatHelper, 
    IDistributedLockProvider distributedLockProvider,
    PasswordSettingsManager passwordSettingsManager,
    AccountLinker accountLinker,
    ProviderManager providerManager,
    DisplayUserSettingsHelper displayUserSettingsHelper)
{
    private async Task<bool> TestUniqueUserNameAsync(string uniqueName)
    {
        if (string.IsNullOrEmpty(uniqueName))
        {
            return false;
        }

        return Equals(await userManager.GetUserByUserNameAsync(uniqueName), Constants.LostUser);
    }

    public async Task<string> MakeUniqueNameAsync(UserInfo userInfo)
    {
        if (string.IsNullOrEmpty(userInfo.Email))
        {
            throw new ArgumentException(Resource.ErrorEmailEmpty, nameof(userInfo));
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

    public async Task<bool> CheckUniqueEmailAsync(Guid userId, string email)
    {
        var foundUser = await userManager.GetUserByEmailAsync(email);
        return Equals(foundUser, Constants.LostUser) || foundUser.Id == userId;
    }

    public async Task<UserInfo> AddInvitedUserAsync(string email, EmployeeType type, string culture)
    {
        var mail = new MailAddress(email);
        var emailLinkedCheckTask = IsEmailLinkedAsync(mail.Address);

        if ((await userManager.GetUserByEmailAsync(mail.Address)).Id != Constants.LostUser.Id)
        {
            throw new Exception(await customNamingPeople.Substitute<Resource>("ErrorEmailAlreadyExists"));
        }
        
        var result = await emailLinkedCheckTask;
        if (result.Linked)
        {
            var linkedUser = await userManager.GetUsersAsync(result.UserId);
            var pattern = await customNamingPeople.Substitute<Resource>("ErrorEmailLinked");
            
            throw new Exception(string.Format(pattern, linkedUser.DisplayUserName(displayUserSettingsHelper)));
        }

        var user = new UserInfo
        {
            Email = mail.Address,
            UserName = mail.User,
            LastName = string.Empty,
            FirstName = string.Empty,
            ActivationStatus = EmployeeActivationStatus.Pending,
            Status = EmployeeStatus.Pending,
            CultureName = culture,
            CreatedBy = securityContext.CurrentAccount.ID
        };

        user.UserName = await MakeUniqueNameAsync(user);

        var newUser = await userManager.SaveUserInfo(user, type);

        var groupId = type switch
        {
            EmployeeType.User => Constants.GroupUser.ID,
            EmployeeType.DocSpaceAdmin => Constants.GroupAdmin.ID,
            EmployeeType.Collaborator => Constants.GroupCollaborator.ID,
            _ => Guid.Empty
        };

        if (groupId != Guid.Empty)
        {
            await userManager.AddUserIntoGroupAsync(newUser.Id, groupId, true);
        }
        else if (type == EmployeeType.RoomAdmin)
        {
            var (name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<CountPaidUserFeature, int>();
            _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
        }

        return newUser;
    }

    public async Task<UserInfo> AddUserAsync(UserInfo userInfo, string passwordHash, bool afterInvite = false, bool notify = true, EmployeeType type = EmployeeType.RoomAdmin, bool fromInviteLink = false, bool makeUniqueName = true, bool isCardDav = false,
        bool updateExising = false)
    {
        ArgumentNullException.ThrowIfNull(userInfo);

        if (!userFormatter.IsValidUserName(userInfo.FirstName, userInfo.LastName))
        {
            throw new Exception(Resource.ErrorIncorrectUserName);
        }

        if (!updateExising && !await CheckUniqueEmailAsync(userInfo.Id, userInfo.Email))
        {
            throw new Exception(await customNamingPeople.Substitute<Resource>("ErrorEmailAlreadyExists"));
        }

        if (makeUniqueName && !updateExising)
        {
            userInfo.UserName = await MakeUniqueNameAsync(userInfo);
        }

        userInfo.WorkFromDate ??= tenantUtil.DateTimeNow();

        if (!fromInviteLink || updateExising)
        {
            userInfo.ActivationStatus = !afterInvite ? EmployeeActivationStatus.Pending : EmployeeActivationStatus.Activated;
        }

        UserInfo newUserInfo;
        if (updateExising)
        {
            newUserInfo = await userManager.UpdateUserInfoAsync(userInfo, true);
        }
        else
        {
            newUserInfo = await userManager.SaveUserInfo(userInfo, type, isCardDav);
        }

        await securityContext.SetUserPasswordHashAsync(newUserInfo.Id, passwordHash);

        if ((newUserInfo.Status & EmployeeStatus.Active) == EmployeeStatus.Active && notify)
        {
            //NOTE: Notify user only if it's active
            if (afterInvite)
            {
                if (type is EmployeeType.User)
                {
                    await studioNotifyService.GuestInfoAddedAfterInviteAsync(newUserInfo);
                }
                else
                {
                    await studioNotifyService.UserInfoAddedAfterInviteAsync(newUserInfo);
                }

                if (fromInviteLink && newUserInfo.ActivationStatus != EmployeeActivationStatus.Activated)
                {
                    await studioNotifyService.SendEmailActivationInstructionsAsync(newUserInfo, newUserInfo.Email);
                }
            }
            else
            {
                //Send user invite
                if (type is EmployeeType.User)
                {
                    await studioNotifyService.GuestInfoActivationAsync(newUserInfo);
                }
                else
                {
                    await studioNotifyService.UserInfoActivationAsync(newUserInfo);
                }

            }
        }

        if (updateExising)
        {
            return newUserInfo;
        }

        switch (type)
        {
            case EmployeeType.User:
                await userManager.AddUserIntoGroupAsync(newUserInfo.Id, Constants.GroupUser.ID, true);
                break;
            case EmployeeType.DocSpaceAdmin:
                await userManager.AddUserIntoGroupAsync(newUserInfo.Id, Constants.GroupAdmin.ID, true);
                break;
            case EmployeeType.Collaborator:
                await userManager.AddUserIntoGroupAsync(newUserInfo.Id, Constants.GroupCollaborator.ID, true);
                break;
        }

        return newUserInfo;
    }

    #region Password

    public async Task<bool> UpdateUserTypeAsync(UserInfo user, EmployeeType type)
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();
        var currentUser = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var changed = false;

        if (user.IsOwner(tenant) || user.IsMe(currentUser.Id))
        {
            return await Task.FromResult(false);
        }

        var currentType = await userManager.GetUserTypeAsync(user.Id);
        IDistributedLockHandle lockHandle = null;

        try
        {
            if (type is EmployeeType.DocSpaceAdmin && currentUser.IsOwner(tenant))
            {
                if (currentType is EmployeeType.RoomAdmin)
                {
                    await userManager.AddUserIntoGroupAsync(user.Id, Constants.GroupAdmin.ID, notifyWebSocket: false);
                    webItemSecurityCache.ClearCache(tenant.Id);
                    changed = true;
                }
                else if (currentType is EmployeeType.Collaborator)
                {
                    await userManager.RemoveUserFromGroupAsync(user.Id, Constants.GroupCollaborator.ID);
                    await userManager.AddUserIntoGroupAsync(user.Id, Constants.GroupAdmin.ID);
                    webItemSecurityCache.ClearCache(tenant.Id);
                    changed = true;
                }
                else if (currentType is EmployeeType.User)
                {
                    lockHandle = await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetPaidUsersCountCheckKey(tenant.Id));
                    
                    await countPaidUserChecker.CheckAppend();
                    await userManager.RemoveUserFromGroupAsync(user.Id, Constants.GroupUser.ID);
                    await userManager.AddUserIntoGroupAsync(user.Id, Constants.GroupAdmin.ID);
                    webItemSecurityCache.ClearCache(tenant.Id);
                    changed = true;
                }
            }
            else if (type is EmployeeType.RoomAdmin)
            {
                if (currentType is EmployeeType.DocSpaceAdmin && currentUser.IsOwner(tenant))
                {
                    await userManager.RemoveUserFromGroupAsync(user.Id, Constants.GroupAdmin.ID);
                    webItemSecurityCache.ClearCache(tenant.Id);
                    changed = true;
                }
                else if (currentType is EmployeeType.Collaborator)
                {
                    await userManager.RemoveUserFromGroupAsync(user.Id, Constants.GroupCollaborator.ID);
                    webItemSecurityCache.ClearCache(tenant.Id);
                    changed = true;
                }
                else if (currentType is EmployeeType.User)
                {
                    lockHandle = await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetPaidUsersCountCheckKey(tenant.Id));
                    
                    await countPaidUserChecker.CheckAppend();
                    await userManager.RemoveUserFromGroupAsync(user.Id, Constants.GroupUser.ID);
                    webItemSecurityCache.ClearCache(tenant.Id);
                    changed = true;
                }
            }
            else if (type is EmployeeType.Collaborator && currentType is EmployeeType.User)
            {
                lockHandle = await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetPaidUsersCountCheckKey(tenant.Id));
                
                await countPaidUserChecker.CheckAppend();
                await userManager.RemoveUserFromGroupAsync(user.Id, Constants.GroupUser.ID);
                await userManager.AddUserIntoGroupAsync(user.Id, Constants.GroupCollaborator.ID);
                webItemSecurityCache.ClearCache(tenant.Id);
                changed = true;
            }
        }
        finally
        {
            if (lockHandle != null)
            {
                await lockHandle.ReleaseAsync();
            }
        }

        return changed;
    }

    public async Task CheckPasswordPolicyAsync(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new Exception(Resource.ErrorPasswordEmpty);
        }

        var passwordSettings = await settingsManager.LoadAsync<PasswordSettings>();

        passwordSettingsManager.CheckPassword(password, passwordSettings);
    }

    public async Task<string> SendUserPasswordAsync(string email)
    {
        email = (email ?? "").Trim();
        if (!email.TestEmailRegex())
        {
            throw new ArgumentNullException(nameof(email), Resource.ErrorNotCorrectEmail);
        }

        var userInfo = await userManager.GetUserByEmailAsync(email);
        if (!userManager.UserExists(userInfo) || string.IsNullOrEmpty(userInfo.Email))
        {
            return string.Format(Resource.ErrorUserNotFoundByEmail, email);
        }
        if (userInfo.Status == EmployeeStatus.Terminated)
        {
            return Resource.ErrorDisabledProfile;
        }
        if (userInfo.IsLDAP())
        {
            return Resource.CouldNotRecoverPasswordForLdapUser;
        }
        if (userInfo.IsSSO())
        {
            return Resource.CouldNotRecoverPasswordForSsoUser;
        }
        
        if (userInfo.ActivationStatus == EmployeeActivationStatus.Pending && userInfo.Status == EmployeeStatus.Pending)
        {
            var type = await userManager.GetUserTypeAsync(userInfo.Id);

            var @event = await auditEventsRepository.GetByFilterAsync(action: MessageAction.SendJoinInvite, target: userInfo.Email);
            var createBy = @event.LastOrDefault()?.UserId;
            var link = await commonLinkUtility.GetInvitationLinkAsync(userInfo.Email, type, createBy ??  (await tenantManager.GetCurrentTenantAsync()).OwnerId, userInfo.GetCulture()?.Name);
            var shortenLink = await urlShortener.GetShortenLinkAsync(link);

            await studioNotifyService.SendDocSpaceRegistration(userInfo.Email, shortenLink);
            return null;
        }
        
        await studioNotifyService.UserPasswordChangeAsync(userInfo);
        return null;
    }

    public static string GeneratePassword()
    {
        return Guid.NewGuid().ToString();
    }

    internal static string GeneratePassword(int minLength, int maxLength, string noise)
    {
        var length = RandomNumberGenerator.GetInt32(minLength, maxLength + 1);

        var sb = new StringBuilder();
        while (length-- > 0)
        {
            sb.Append(noise[RandomNumberGenerator.GetInt32(noise.Length - 1)]);
        }
        return sb.ToString();
    }
    
    private async Task<(bool Linked, Guid UserId)> IsEmailLinkedAsync(string email)
    {
        var profiles = await accountLinker.GetLinkedProfilesAsync();
        
        var profile = profiles.FirstOrDefault(x => x.EMail.Equals(email));
        if (profile == null)
        {
            return (false, Guid.Empty);
        }
        
        var providerType = ProviderManager.AuthProviders.FirstOrDefault(x => x.Equals(profile.Provider));
        var provider = providerManager.GetLoginProvider(providerType);

        if (provider is { IsEnabled: true })
        {
            return Guid.TryParse(profile.LinkId, out var userId) ? (true, userId) : (true, Guid.Empty);
        }

        await accountLinker.RemoveProviderAsync(profile.LinkId, profile.Provider, profile.HashId);
        return (false, Guid.Empty);
    }

    private static string GetPasswordHelpMessage(PasswordSettings passwordSettings)
    {
        var text = new StringBuilder();

        text.Append($"{Resource.ErrorPasswordMessage} ");
        text.AppendFormat(Resource.ErrorPasswordLength, passwordSettings.MinLength, PasswordSettingsManager.MaxLength);
        text.Append($", {Resource.ErrorPasswordOnlyLatinLetters}");
        text.Append($", {Resource.ErrorPasswordNoSpaces}");

        if (passwordSettings.UpperCase)
        {
            text.Append($", {Resource.ErrorPasswordNoUpperCase}");
        }

        if (passwordSettings.Digits)
        {
            text.Append($", {Resource.ErrorPasswordNoDigits}");
        }

        if (passwordSettings.SpecSymbols)
        {
            text.Append($", {Resource.ErrorPasswordNoSpecialSymbols}");
        }

        return text.ToString();
    }

    #endregion

    public static bool ValidateEmail(string email)
    {
        const string pattern = @"^(([^<>()[\]\\.,;:\s@\""]+"
                               + @"(\.[^<>()[\]\\.,;:\s@\""]+)*)|(\"".+\""))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}"
                               + @"\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$";
        const RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Compiled;
        return new Regex(pattern, options).IsMatch(email);
    }
}