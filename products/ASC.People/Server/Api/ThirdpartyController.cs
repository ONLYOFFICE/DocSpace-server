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
    /// Returns the third-party identity providers this portal has enabled, each with the URL that starts the login
    /// with it, so a client can render the social sign-in buttons.
    /// It needs no authentication and is the operation to call before showing a login or an invitation page; an
    /// empty list means the portal has no provider configured, not that the call failed.
    /// The call is read-only, and `linked` says whether the provider is already connected to the calling profile -
    /// for an anonymous caller there is nothing to compare against, so every entry comes back with false.
    /// The order is fixed by the portal, except that a caller located in China gets `weixin` first.
    /// Pass `fromOnly` to keep a single provider, `inviteView` to leave out the providers that cannot be used on an
    /// invitation page, and `settingsView` or `clientCallback` to get URLs that open in a popup instead of
    /// redirecting the desktop application.
    /// Use `PUT api/2.0/people/thirdparty/linkaccount` to connect one of these providers to an existing profile and
    /// `POST api/2.0/people/thirdparty/signup` to create a profile through one.
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
    /// Connects a third-party identity to the calling profile, so that the account can afterwards sign in through
    /// that provider.
    /// The profile has to come from a completed provider authorization: pass the serialized `LoginProfile` the login
    /// flow started from `GET api/2.0/people/thirdparty/providers` handed back, not a hand-written object.
    /// It acts on the authenticated account only, and the portal has to be a standalone installation or have a
    /// tariff that includes third-party authorization, otherwise the operation answers 403.
    /// The call returns no body and is not idempotent: one third-party identity can be linked to a single portal
    /// profile, so repeating it, or linking an identity somebody else already uses, answers 400.
    /// A profile whose authorization was cancelled by the user is accepted and ignored, so a cancelled login also
    /// answers 200 and links nothing - read `GET api/2.0/people/thirdparty/providers` afterwards and check `linked`
    /// to find out whether the link exists.
    /// Use `DELETE api/2.0/people/thirdparty/unlinkaccount` to remove a link.
    /// </remarks>
    /// <summary>
    /// Link a third-party account
    /// </summary>
    /// <path>api/2.0/people/thirdparty/linkaccount</path>
    [Tags("People / Third-party accounts")]
    [SwaggerResponse(200, "The third-party identity is linked to the calling profile. No content is returned")]
    [SwaggerResponse(400, "The third-party identity is already linked to a portal profile")]
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
    /// Creates a portal profile from a third-party identity and joins the invitation the `key` belongs to, which is
    /// how a person accepts an invitation by signing in with a provider instead of setting a password.
    /// It needs no authentication, but it does need a valid invitation: `key` has to be the key of a live invitation
    /// link, and `serializedProfile` has to be the profile a completed provider authorization produced.
    /// The resulting type comes from the invitation link itself, and `employeeType` only says which type to look the
    /// link up as, defaulting to `RoomAdmin`.
    /// When the identity or its email already belongs to a portal profile, that existing profile is returned and the
    /// provider is linked to it instead of a second account being created, so the call can be repeated safely.
    /// The answer is the profile the caller ends up with - and it is empty, still with status 200, when the provider
    /// authorization was cancelled or when the profile could not be created, so check for an empty body instead of
    /// relying on the status alone.
    /// A `weixin` or `nextcloud` identity carries no email address, so the portal generates one and the profile stays
    /// in the `AutoGenerated` activation state; every other provider has to supply an email.
    /// </remarks>
    /// <summary>
    /// Sign up with a provider
    /// </summary>
    /// <path>api/2.0/people/thirdparty/signup</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("People / Third-party accounts")]
    [SwaggerResponse(200, "The profile linked to the third-party identity, or an empty body when the authorization was cancelled or the profile could not be created", typeof(EmployeeDto))]
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
    /// Removes the link between the calling profile and the named third-party provider, so that the account can no
    /// longer sign in through it.
    /// It acts on the authenticated account only and takes the provider name in the query, using the same lowercase
    /// values `GET api/2.0/people/thirdparty/providers` returns, such as `google` or `microsoft`.
    /// The call returns no body and is idempotent: unlinking a provider that is not linked answers 200 and changes
    /// nothing.
    /// The portal profile itself is kept, together with its password, so the account stays usable through the
    /// ordinary sign-in; only the third-party route is removed.
    /// Link the provider again through `PUT api/2.0/people/thirdparty/linkaccount`.
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
