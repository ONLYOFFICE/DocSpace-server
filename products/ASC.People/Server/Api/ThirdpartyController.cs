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

using ASC.Common.Log;
using ASC.Web.Studio.UserControls.Management;

using Constants = ASC.Core.Configuration.Constants;

namespace ASC.People.Api;

/// <remarks>
/// Third-party API.
/// </remarks>
[ApiEndpoint(Template = "thirdparty")]
public class ThirdpartyController(
    ILogger<ThirdpartyController> logger,
    AccountLinker accountLinker,
    CoreBaseSettings coreBaseSettings,
    CustomNamingPeople customNamingPeople,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    IHttpClientFactory httpClientFactory,
    MobileDetector mobileDetector,
    ProviderManager providerManager,
    UserHelpTourHelper userHelpTourHelper,
    EmployeeDtoHelper employeeDtoHelper,
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
    EmailValidationKeyModelHelper emailValidationKeyModelHelper,
    UserSocketManager socketManager,
    UserWebhookManager webhookManager,
    GeolocationHelper geolocationHelper)
    : ApiControllerBase
{


    /// <remarks>
    /// Returns the third-party identity providers that the portal has turned on. Each item has the URL that starts
    /// sign-in with that provider. Anyone can call it without signing in. The call is read-only. An empty list means
    /// that the portal has no provider turned on. The `linked` field shows if the provider is linked to the account
    /// of the caller. For an anonymous caller, `linked` is always false. The portal sets the order, but a caller in
    /// China gets `weixin` first. If `fromOnly` is set, the call keeps only that provider. The value `openid`
    /// selects `google`. With `inviteView`, the call leaves out the providers that cannot accept an invitation. With
    /// `settingsView` or `inviteView`, the URLs always open in a popup. Otherwise, a URL redirects only for a mobile
    /// browser or the desktop app. To link a provider to your account, use
    /// `PUT api/2.0/people/thirdparty/linkaccount`. To create an account through a provider, use
    /// `POST api/2.0/people/thirdparty/signup`.
    /// </remarks>
    /// <summary>Get third-party providers</summary>
    /// <path>api/2.0/people/thirdparty/providers</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <collection>list</collection>
    [Tags("People / Third-party accounts")]
    [SwaggerResponse(200, "The enabled providers, each with its login URL and its link state for the caller", typeof(ICollection<AccountInfoDto>))]
    [AllowAnonymous, AllowNotPayment]
    [HttpGet("providers")]
    public async Task<ICollection<AccountInfoDto>> GetThirdPartyAuthProviders(AuthProvidersRequestDto inDto)
    {
        var infos = new List<AccountInfoDto>();
        var linkedAccounts = new List<LoginProfile>();

        if (authContext.IsAuthenticated)
        {
            linkedAccounts = await accountLinker.GetLinkedProfilesAsync(authContext.CurrentAccount.ID.ToString());
        }

        inDto.FromOnly = string.IsNullOrWhiteSpace(inDto.FromOnly) ? string.Empty : inDto.FromOnly.ToLower();

        var geoInfoKey = (await geolocationHelper.GetIPGeolocationFromHttpContextAsync()).Key;

        foreach (var provider in ProviderManager.GetSortedAuthProviders(geoInfoKey).Where(provider => string.IsNullOrEmpty(inDto.FromOnly) || inDto.FromOnly == provider || (provider == "google" && inDto.FromOnly == "openid")))
        {
            if (inDto.InviteView && ProviderManager.InviteExceptProviders.Contains(provider))
            {
                continue;
            }
            var loginProvider = providerManager.GetLoginProvider(provider);
            if (loginProvider is { IsEnabled: true })
            {

                var url = VirtualPathUtility.ToAbsolute("~/login.ashx") + $"?auth={provider}";
                var mode = inDto.SettingsView || inDto.InviteView || (!mobileDetector.IsMobile() && !Request.DesktopApp())
                        ? $"&mode=popup&callback={inDto.ClientCallback}"
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

    /// <remarks>
    /// Links a third-party identity to the account of the caller. After this call, the account can sign in through
    /// that provider. First, complete the sign-in that a URL of `GET api/2.0/people/thirdparty/providers` starts.
    /// Then pass the `serializedProfile` that this sign-in returns. Any signed-in user can call it. If the portal
    /// tariff does not include third-party sign-in, the call returns 403. A standalone installation always includes
    /// it. The call changes data and returns no body. The call returns 400 if `serializedProfile` is empty, comes
    /// from another portal, or is already linked to an account. So a repeated call returns 400. If the user
    /// cancelled the sign-in at the provider, the call returns 200 and links nothing. To check the link, read
    /// `linked` in `GET api/2.0/people/thirdparty/providers`. To delete a link, use
    /// `DELETE api/2.0/people/thirdparty/unlinkaccount`.
    /// </remarks>
    /// <summary>
    /// Link a third-party account
    /// </summary>
    /// <path>api/2.0/people/thirdparty/linkaccount</path>
    [Tags("People / Third-party accounts")]
    [SwaggerResponse(200, "The third-party identity is linked to the calling profile. No content is returned")]
    [SwaggerResponse(400, "The third-party identity is already linked to a portal profile, or `serializedProfile` is missing, empty or was issued by another portal")]
    [SwaggerResponse(403, "The portal tariff does not include third-party authorization")]
    [HttpPut("linkaccount")]
    public async Task LinkThirdPartyAccount(LinkAccountRequestDto inDto)
    {
        var profile = await loginProfileTransport.FromTransport(inDto.SerializedProfile);

        if (!(coreBaseSettings.Standalone || (await tenantManager.GetCurrentTenantQuotaAsync()).Oauth))
        {
            throw new SecurityException(Resource.ErrorNotAllowedOption);
        }

        if (string.IsNullOrEmpty(profile.AuthorizationError))
        {
            await accountLinker.AddLinkAsync(securityContext.CurrentAccount.ID, profile);
            messageService.Send(MessageAction.UserLinkedSocialAccount, GetMeaningfulProviderName(profile.Provider));
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

    /// <remarks>
    /// Creates an account from a third-party identity and accepts the invitation of `key`. So a person can join by
    /// signing in with a provider instead of a password. Pass the `serializedProfile` that the provider sign-in
    /// returns. Anyone can call it without signing in. Instead, the call needs the key of a valid invitation link. If
    /// the link is invalid or expired, the call returns 403. The invitation link sets the user type of the new
    /// account. The value of `employeeType` only selects the kind of link to check. The call changes data. If an
    /// account already has this identity or its email, the call links the provider to that account. It then returns
    /// that account and creates no second one. So a repeated call with a valid key returns the same profile. If that
    /// account is not activated yet, the call returns 403. For a room invitation link, the call also adds the account
    /// to the room. The call returns 200 with an empty body if the user cancelled the sign-in. The body is also empty
    /// if the portal fails to create the account. So check the body, not only the status. A `weixin` or `nextcloud`
    /// identity has no email. For it, the portal generates an email, and the account stays in the `AutoGenerated`
    /// activation state. The call returns 400 if `key` or `serializedProfile` is missing, or `serializedProfile` is
    /// from another portal.
    /// </remarks>
    /// <summary>
    /// Sign up with a provider
    /// </summary>
    /// <path>api/2.0/people/thirdparty/signup</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [RaisesWebhook(WebhookTrigger.UserCreated, When = "only when the call creates a new account")]
    [Tags("People / Third-party accounts")]
    [SwaggerResponse(200, "The profile linked to the third-party identity, or an empty body when the authorization was cancelled or the profile could not be created", typeof(EmployeeDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `key` or `serializedProfile`, or `serializedProfile` is empty or was issued by another portal")]
    [SwaggerResponse(403, "The invitation link is invalid or has expired, or the email already belongs to a profile that has not been activated yet")]
    [AllowAnonymous]
    [HttpPost("signup")]
    public async Task<EmployeeDto> SignupThirdPartyAccount(SignupAccountRequestDto inDto)
    {
        var thirdPartyProfile = await loginProfileTransport.FromTransport(inDto.SerializedProfile);
        if (!string.IsNullOrEmpty(thirdPartyProfile.AuthorizationError))
        {
            // ignore cancellation
            if (thirdPartyProfile.AuthorizationError != "Canceled at provider")
            {
                throw new Exception(thirdPartyProfile.AuthorizationError);
            }

            return null;
        }

        var email = thirdPartyProfile.EMail;
        var autoGeneratedEmail = false;

        if (string.IsNullOrWhiteSpace(email) &&
            ProviderManager.DummyEmailProviders.Contains(thirdPartyProfile.Provider)&&
            providerManager.GetLoginProvider(thirdPartyProfile.Provider) is IDummyEmailProvider provider)
        {
            email = provider.GenerateEmail(thirdPartyProfile);
            autoGeneratedEmail = true;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new Exception(Resource.ErrorNotCorrectEmail);
        }

        var model = emailValidationKeyModelHelper.GetModel();
        var linkData = await invitationService.GetLinkDataAsync(inDto.Key, null, null, inDto.EmployeeType ?? EmployeeType.RoomAdmin, model?.UiD);

        if (!linkData.IsCorrect)
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_InvintationLink);
        }

        var passwordHash = UserManagerWrapper.GeneratePassword();
        var employeeType = linkData.EmployeeType;
        var quotaLimit = false;

        var user = await GetUserByThirdPartyProfileAsync(thirdPartyProfile);
        if (user.Id == ASC.Core.Users.Constants.LostUser.Id)
        {
            try
            {
                await securityContext.AuthenticateMeWithoutCookieAsync(Constants.CoreSystem);

                var invitedByEmail = linkData.LinkType == InvitationLinkType.Individual;

                (user, quotaLimit) = await CreateNewUser(
                    thirdPartyProfile.FirstName,
                    thirdPartyProfile.LastName,
                    thirdPartyProfile.DisplayName,
                    email,
                    passwordHash,
                    employeeType,
                    false,
                    invitedByEmail,
                    inDto.Culture,
                    model?.UiD,
                    autoGeneratedEmail);

                var messageAction = employeeType == EmployeeType.RoomAdmin ? MessageAction.UserCreatedViaInvite : MessageAction.GuestCreatedViaInvite;
                messageService.Send(MessageInitiator.System, messageAction, MessageTarget.Create(user.Id), description: user.DisplayUserName(false, displayUserSettingsHelper));

                if (!string.IsNullOrEmpty(thirdPartyProfile.Avatar))
                {
                    await SaveContactImage(user.Id, thirdPartyProfile.Avatar);
                }

                await accountLinker.AddLinkAsync(user.Id, thirdPartyProfile);

                await webhookManager.PublishAsync(WebhookTrigger.UserCreated, user);

                if (!autoGeneratedEmail)
                {
                    await studioNotifyService.UserPasswordChangeAsync(user, true);
                }

                await userHelpTourHelper.SetIsNewUser(true);

                await securityContext.AuthenticateMeWithoutCookieAsync(user.Id);

                await studioNotifyService.UserHasJoinAsync();
            }
            catch (Exception ex)
            {
                logger.ErrorWithException(ex);
            }
            finally
            {
                securityContext.Logout();
            }
        }

        if (user.Id == ASC.Core.Users.Constants.LostUser.Id)
        {
            return null;
        }

        if (linkData is { LinkType: InvitationLinkType.CommonToRoom })
        {
            await invitationService.AddUserToRoomByInviteAsync(linkData, user, quotaLimit);
        }

        return await employeeDtoHelper.GetAsync(user);
    }

    private async Task<UserInfo> GetUserByThirdPartyProfileAsync(LoginProfile profile)
    {
        if (!string.IsNullOrEmpty(profile.HashId))
        {
            var linkedProfiles = await accountLinker.GetLinkedObjectsByHashIdAsync(profile.HashId);
            foreach (var profileId in linkedProfiles)
            {
                if (Guid.TryParse(profileId, out var userId))
                {
                    var user = await userManager.GetUsersAsync(userId);
                    if (user.Id != Core.Users.Constants.LostUser.Id)
                    {
                        return user;
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(profile.EMail))
        {
            var user = await userManager.GetUserByEmailAsync(profile.EMail);
            if (user.Id != Core.Users.Constants.LostUser.Id && user.Status != EmployeeStatus.Terminated)
            {
                if (user.ActivationStatus != EmployeeActivationStatus.Activated)
                {
                    var msg = await customNamingPeople.Substitute<Resource>("ErrorEmailAlreadyExists");
                    throw new InvalidOperationException(msg);
                }

                var linkedProfiles = await accountLinker.GetLinkedProfilesAsync(user.Id.ToString(), profile.Provider);
                if (!linkedProfiles.Any())
                {
                    await accountLinker.AddLinkAsync(user.Id, profile);
                }

                return user;
            }
        }

        return ASC.Core.Users.Constants.LostUser;
    }

    /// <remarks>
    /// Deletes the link between the account of the caller and a third-party provider. After this call, the account
    /// can no longer sign in through that provider. The account itself stays, with its password. Any signed-in user
    /// can call it. The call changes data and returns no body. If the provider is not linked, the call changes
    /// nothing and returns 200. So you can repeat it. To link the provider again, use
    /// `PUT api/2.0/people/thirdparty/linkaccount`.
    /// </remarks>
    /// <summary>
    /// Unlink a third-party account
    /// </summary>
    /// <path>api/2.0/people/thirdparty/unlinkaccount</path>
    [Tags("People / Third-party accounts")]
    [SwaggerResponse(200, "The third-party identity is no longer linked to the calling profile. No content is returned")]
    [HttpDelete("unlinkaccount")]
    public async Task UnlinkThirdPartyAccount(UnlinkAccountRequestDto inDto)
    {
        await accountLinker.RemoveProviderAsync(securityContext.CurrentAccount.ID, inDto.Provider);

        messageService.Send(MessageAction.UserUnlinkedSocialAccount, GetMeaningfulProviderName(inDto.Provider));
    }

    private async Task<(UserInfo, bool)> CreateNewUser(string firstName, string lastName, string displayName, string email, string passwordHash, EmployeeType employeeType, bool fromInviteLink,
        bool inviteByEmail, string cultureName, Guid? invitedBy, bool autoGeneratedEmail)
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

        if (!inviteByEmail)
        {
            user.CreatedBy = invitedBy;
        }

        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName) && !string.IsNullOrWhiteSpace(displayName))
        {
            firstName = displayName;
        }

        user.FirstName = string.IsNullOrWhiteSpace(firstName) ? UserControlsCommonResource.UnknownFirstName : firstName;
        user.LastName = string.IsNullOrWhiteSpace(lastName) ? string.Empty : lastName;
        user.Email = email;

        if (autoGeneratedEmail)
        {
            user.ActivationStatus = EmployeeActivationStatus.AutoGenerated;
        }

        if (coreBaseSettings.EnabledCultures.Find(c => string.Equals(c.Name, cultureName, StringComparison.InvariantCultureIgnoreCase)) != null)
        {
            user.CultureName = cultureName;
        }

        var quotaLimit = false;
        var notify = !autoGeneratedEmail;
        try
        {
            user = await userManagerWrapper.AddUserAsync(user, passwordHash, true, notify, employeeType, fromInviteLink, updateExising: inviteByEmail);
            if (employeeType is EmployeeType.Guest)
            {
                await socketManager.AddGuestAsync(user);
            }
            else
            {
                await socketManager.AddUserAsync(user);
            }
        }
        catch (TenantQuotaException)
        {
            quotaLimit = true;
            user = await userManagerWrapper.AddUserAsync(user, passwordHash, true, notify, EmployeeType.User, fromInviteLink, updateExising: inviteByEmail);
            await socketManager.AddUserAsync(user);
        }

        return (user, quotaLimit);
    }

    private async Task SaveContactImage(Guid userID, string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        #pragma warning disable CA2000
        var httpClient = httpClientFactory.CreateClient();
        #pragma warning restore CA2000

        using var response = await httpClient.SendAsync(request);
        var bytes = await response.Content.ReadAsByteArrayAsync();

        await userPhotoManager.SaveOrUpdatePhoto(userID, bytes);
    }

    private static string GetMeaningfulProviderName(string providerName)
    {
        var result = string.IsNullOrEmpty(providerName)
            ? null
            : ConsumerExtension.GetResourceString(providerName == "openid" ? "Google" : providerName);

        return result ?? "Unknown Provider";
    }
}
