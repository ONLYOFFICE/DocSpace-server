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

using Constants = ASC.Core.Configuration.Constants;

namespace ASC.People.Api;

[DefaultRoute("thirdparty")]
public class ThirdpartyController(
    AccountLinker accountLinker,
    CookiesManager cookiesManager,
    CoreBaseSettings coreBaseSettings,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    IHttpClientFactory httpClientFactory,
    MobileDetector mobileDetector,
    ProviderManager providerManager,
    UserHelpTourHelper userHelpTourHelper,
    UserManagerWrapper userManagerWrapper,
    UserPhotoManager userPhotoManager,
    AuthContext authContext,
    SecurityContext securityContext,
    MessageService messageService,
    UserManager userManager,
    StudioNotifyService studioNotifyService,
    TenantManager tenantManager,
    InvitationService invitationService,
    LoginProfileTransport loginProfileTransport,
    EmailValidationKeyModelHelper emailValidationKeyModelHelper)
    : ApiControllerBase
    {
    

    /// <summary>
    /// Returns a list of the available third-party accounts.
    /// </summary>
    /// <short>Get third-party accounts</short>
    /// <category>Third-party accounts</category>
    /// <param type="System.Boolean, System" name="inviteView">Specifies whether to return providers that are available for invitation links, i.e. the user can login or register through these providers</param>
    /// <param type="System.Boolean, System" name="settingsView">Specifies whether to return URLs in the format that is used on the Settings page</param>
    /// <param type="System.String, System" name="clientCallback">Method that is called after authorization</param>
    /// <param type="System.String, System" name="fromOnly">Provider name if the response only from this provider is needed</param>
    /// <returns type="ASC.People.ApiModels.ResponseDto.AccountInfoDto, ASC.People">List of third-party accounts</returns>
    /// <path>api/2.0/people/thirdparty/providers</path>
    /// <httpMethod>GET</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <collection>list</collection>
    [AllowAnonymous, AllowNotPayment]
    [HttpGet("providers")]
    public async Task<ICollection<AccountInfoDto>> GetAuthProvidersAsync(bool inviteView, bool settingsView, string clientCallback, string fromOnly)
    {
        var infos = new List<AccountInfoDto>();
        IEnumerable<LoginProfile> linkedAccounts = new List<LoginProfile>();

        if (authContext.IsAuthenticated)
        {
            linkedAccounts = await accountLinker.GetLinkedProfilesAsync(authContext.CurrentAccount.ID.ToString());
        }

        fromOnly = string.IsNullOrWhiteSpace(fromOnly) ? string.Empty : fromOnly.ToLower();

        foreach (var provider in ProviderManager.AuthProviders.Where(provider => string.IsNullOrEmpty(fromOnly) || fromOnly == provider || (provider == "google" && fromOnly == "openid")))
        {
            if (inviteView && ProviderManager.InviteExceptProviders.Contains(provider))
            {
                continue;
            }
            var loginProvider = providerManager.GetLoginProvider(provider);
            if (loginProvider is { IsEnabled: true })
            {

                var url = VirtualPathUtility.ToAbsolute("~/login.ashx") + $"?auth={provider}";
                var mode = settingsView || inviteView || (!mobileDetector.IsMobile() && !Request.DesktopApp())
                        ? $"&mode=popup&callback={clientCallback}"
                        : "&mode=Redirect&desktop=true";

                infos.Add(new AccountInfoDto
                {
                    Linked = linkedAccounts.Any(x => x.Provider == provider),
                    Provider = provider,
                    Url = url + mode
                });
            }
        }

        return infos;
    }

    /// <summary>
    /// Links a third-party account specified in the request to the user profile.
    /// </summary>
    /// <short>
    /// Link a third-pary account
    /// </short>
    /// <category>Third-party accounts</category>
    /// <param type="ASC.People.ApiModels.RequestDto.LinkAccountRequestDto, ASC.People" name="inDto">Request parameters for linking accounts</param>
    /// <path>api/2.0/people/thirdparty/linkaccount</path>
    /// <httpMethod>PUT</httpMethod>
    /// <returns></returns>
    [HttpPut("linkaccount")]
    public async Task LinkAccountAsync(LinkAccountRequestDto inDto)
    {
        var profile = await loginProfileTransport.FromTransport(inDto.SerializedProfile);

        if (!(coreBaseSettings.Standalone || (await tenantManager.GetCurrentTenantQuotaAsync()).Oauth))
        {
            throw new Exception("ErrorNotAllowedOption");
        }

        if (string.IsNullOrEmpty(profile.AuthorizationError))
        {
            await accountLinker.AddLinkAsync(securityContext.CurrentAccount.ID, profile);
            await messageService.SendAsync(MessageAction.UserLinkedSocialAccount, GetMeaningfulProviderName(profile.Provider));
        }
        else
        {
            // ignore cancellation
            if (profile.AuthorizationError != "Canceled at provider")
            {
                throw new Exception(profile.AuthorizationError);
            }
        }
    }

    /// <summary>
    /// Creates a third-party account with the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Create a third-pary account
    /// </short>
    /// <category>Third-party accounts</category>
    /// <param type="ASC.People.ApiModels.RequestDto.SignupAccountRequestDto, ASC.People" name="inDto">Request parameters for creating a third-party account</param>
    /// <path>api/2.0/people/thirdparty/signup</path>
    /// <httpMethod>POST</httpMethod>
    /// <returns></returns>
    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowAnonymous]
    [HttpPost("signup")]
    public async Task SignupAccountAsync(SignupAccountRequestDto inDto)
    {
        var passwordHash = inDto.PasswordHash;
        var mustChangePassword = false;
        if (string.IsNullOrEmpty(passwordHash))
        {
            passwordHash = UserManagerWrapper.GeneratePassword();
            mustChangePassword = true;
        }

        var thirdPartyProfile = await loginProfileTransport.FromTransport(inDto.SerializedProfile);
        if (!string.IsNullOrEmpty(thirdPartyProfile.AuthorizationError))
        {
            // ignore cancellation
            if (thirdPartyProfile.AuthorizationError != "Canceled at provider")
            {
                throw new Exception(thirdPartyProfile.AuthorizationError);
            }

            return;
        }

        if (string.IsNullOrEmpty(thirdPartyProfile.EMail))
        {
            throw new Exception(Resource.ErrorNotCorrectEmail);
        }
        
        var model = emailValidationKeyModelHelper.GetModel();
        var linkData = await invitationService.GetLinkDataAsync(inDto.Key, inDto.Email, inDto.EmployeeType ?? EmployeeType.RoomAdmin, model?.UiD);

        if (!linkData.IsCorrect)
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_InvintationLink);
        }

        var employeeType = linkData.EmployeeType;
        bool quotaLimit;

        Guid userId;
        try
        {
            await securityContext.AuthenticateMeWithoutCookieAsync(Constants.CoreSystem);

            var invitedByEmail = linkData.LinkType == InvitationLinkType.Individual;

            (var newUser, quotaLimit) = await CreateNewUser(
                GetFirstName(inDto, thirdPartyProfile), 
                GetLastName(inDto, thirdPartyProfile), 
                GetEmailAddress(inDto, thirdPartyProfile), 
                passwordHash, 
                employeeType, 
                true, 
                invitedByEmail,
                inDto.Culture);
            
            var messageAction = employeeType == EmployeeType.RoomAdmin ? MessageAction.UserCreatedViaInvite : MessageAction.GuestCreatedViaInvite;
            await messageService.SendAsync(MessageInitiator.System, messageAction, MessageTarget.Create(newUser.Id), description: newUser.DisplayUserName(false, displayUserSettingsHelper));
            userId = newUser.Id;
            if (!string.IsNullOrEmpty(thirdPartyProfile.Avatar))
            {
                await SaveContactImage(userId, thirdPartyProfile.Avatar);
            }

            await accountLinker.AddLinkAsync(userId, thirdPartyProfile);
        }
        finally
        {
            securityContext.Logout();
        }

        var user = await userManager.GetUsersAsync(userId);

        await cookiesManager.AuthenticateMeAndSetCookiesAsync(user.Id);

        await studioNotifyService.UserHasJoinAsync();

        if (mustChangePassword)
        {
            await studioNotifyService.UserPasswordChangeAsync(user);
        }

        await userHelpTourHelper.SetIsNewUser(true);

        if (linkData is { LinkType: InvitationLinkType.CommonToRoom })
        {
            await invitationService.AddUserToRoomByInviteAsync(linkData, user, quotaLimit);
        }
    }

    /// <summary>
    /// Unlinks a third-party account specified in the request from the user profile.
    /// </summary>
    /// <short>
    /// Unlink a third-pary account
    /// </short>
    /// <category>Third-party accounts</category>
    /// <param type="System.String, System" name="provider">Provider name</param>
    /// <path>api/2.0/people/thirdparty/unlinkaccount</path>
    /// <httpMethod>DELETE</httpMethod>
    /// <returns></returns>
    [HttpDelete("unlinkaccount")]
    public async Task UnlinkAccountAsync(string provider)
    {
        await accountLinker.RemoveProviderAsync(securityContext.CurrentAccount.ID.ToString(), provider);

        await messageService.SendAsync(MessageAction.UserUnlinkedSocialAccount, GetMeaningfulProviderName(provider));
    }

    private async Task<(UserInfo, bool)> CreateNewUser(string firstName, string lastName, string email, string passwordHash, EmployeeType employeeType, bool fromInviteLink, bool inviteByEmail, string cultureName)
    {
        if (SetupInfo.IsSecretEmail(email))
        {
            fromInviteLink = false;
        }

        var user = new UserInfo();

        if (inviteByEmail)
        {
            user = await userManager.GetUserByEmailAsync(email);

            if (user.Equals(Core.Users.Constants.LostUser) || user.ActivationStatus != EmployeeActivationStatus.Pending)
            {
                throw new SecurityException(FilesCommonResource.ErrorMessage_InvintationLink);
            }
        }

        user.FirstName = string.IsNullOrEmpty(firstName) ? UserControlsCommonResource.UnknownFirstName : firstName;
        user.LastName = string.IsNullOrEmpty(lastName) ? UserControlsCommonResource.UnknownLastName : lastName;
        user.Email = email;
        
        if (coreBaseSettings.EnabledCultures.Find(c => string.Equals(c.Name, cultureName, StringComparison.InvariantCultureIgnoreCase)) != null)
        {
            user.CultureName = cultureName;
        }

        var quotaLimit = false;

        try
        {
            user = await userManagerWrapper.AddUserAsync(user, passwordHash, true, true, employeeType, fromInviteLink, updateExising: inviteByEmail);
        }
        catch (TenantQuotaException)
        {
            quotaLimit = true;
            user = await userManagerWrapper.AddUserAsync(user, passwordHash, true, true, EmployeeType.User, fromInviteLink, updateExising: inviteByEmail);
        }

        return (user, quotaLimit);
    }

    private async Task SaveContactImage(Guid userID, string url)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(url)
        };

        var httpClient = httpClientFactory.CreateClient();
        using var response = await httpClient.SendAsync(request);
        var bytes = await response.Content.ReadAsByteArrayAsync();

        await userPhotoManager.SaveOrUpdatePhoto(userID, bytes);
    }

    private string GetEmailAddress(SignupAccountRequestDto inDto)
    {
        if (!string.IsNullOrEmpty(inDto.Email))
        {
            return inDto.Email.Trim();
        }

        return string.Empty;
    }

    private string GetEmailAddress(SignupAccountRequestDto inDto, LoginProfile account)
    {
        var value = GetEmailAddress(inDto);

        return string.IsNullOrEmpty(value) ? account.EMail : value;
    }

    private string GetFirstName(SignupAccountRequestDto inDto)
    {
        var value = string.Empty;
        if (!string.IsNullOrEmpty(inDto.FirstName))
        {
            value = inDto.FirstName.Trim();
        }

        return HtmlUtil.GetText(value);
    }

    private string GetFirstName(SignupAccountRequestDto inDto, LoginProfile account)
    {
        var value = GetFirstName(inDto);

        return string.IsNullOrEmpty(value) ? account.FirstName : value;
    }

    private string GetLastName(SignupAccountRequestDto inDto)
    {
        var value = string.Empty;
        if (!string.IsNullOrEmpty(inDto.LastName))
        {
            value = inDto.LastName.Trim();
        }

        return HtmlUtil.GetText(value);
    }

    private string GetLastName(SignupAccountRequestDto inDto, LoginProfile account)
    {
        var value = GetLastName(inDto);

        return string.IsNullOrEmpty(value) ? account.LastName : value;
    }

    private static string GetMeaningfulProviderName(string providerName)
    {
        switch (providerName)
        {
            case "google":
            case "openid":
                return "Google";
            case "facebook":
                return "Facebook";
            case "twitter":
                return "Twitter";
            case "linkedin":
                return "LinkedIn";
            default:
                return "Unknown Provider";
        }
    }
}
