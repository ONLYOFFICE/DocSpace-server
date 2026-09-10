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

using Constants = ASC.Core.Users.Constants;

namespace ASC.Web.Api.Controllers;

/// <remarks>
/// Portal sign-in: exchanging an email and password, a confirmation link or a third-party account for the
/// authentication token that every other operation of this API expects in the `Authorization` header, passing the
/// second factor the portal may require of a user (an SMS code or an authenticator app), and ending the session
/// again. Every operation here is open to unauthenticated callers and keeps working while the portal's payment has
/// lapsed, except `POST api/2.0/authentication/setphone`, which is reached with the confirmation link the portal
/// issues for phone activation.
/// </remarks>
/// <name>authentication</name>
[Scope]
[ApiEndpoint("authentication")]
[WebhookDisable]
public class AuthenticationController(
    UserManager userManager,
    LdapUserManager ldapUserManager,
    TenantManager tenantManager,
    SecurityContext securityContext,
    TenantCookieSettingsHelper tenantCookieSettingsHelper,
    CookiesManager cookiesManager,
    PasswordHasher passwordHasher,
    EmailValidationKeyModelHelper emailValidationKeyModelHelper,
    SetupInfo setupInfo,
    MessageService messageService,
    ProviderManager providerManager,
    AccountLinker accountLinker,
    CoreBaseSettings coreBaseSettings,
    Signature signature,
    CustomNamingPeople customNamingPeople,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    StudioSmsNotificationSettingsHelper studioSmsNotificationSettingsHelper,
    SettingsManager settingsManager,
    SmsManager smsManager,
    TfaManager tfaManager,
    SmsKeyStorage smsKeyStorage,
    CommonLinkUtility commonLinkUtility,
    AuthContext authContext,
    CookieStorage cookieStorage,
    QuotaSocketManager quotaSocketManager,
    DbLoginEventsManager dbLoginEventsManager,
    BruteForceLoginManager bruteForceLoginManager,
    TfaAppAuthSettingsHelper tfaAppAuthSettingsHelper,
    InvitationService invitationService,
    UserSocketManager socketManager,
    LoginProfileTransport loginProfileTransport,
    AuditEventsRepository auditEventsRepository)
    : ControllerBase
{
    /// <remarks>
    /// Reports whether the credentials that came with this very request identify a signed-in user of the current
    /// portal - the authentication cookie, or the token in the `Authorization` header. Nothing has to be called
    /// first: the operation is open to unauthenticated callers, who simply get `false`, it is read-only and
    /// idempotent, and it answers even while the portal's payment has lapsed. The result is a bare boolean that
    /// carries no reason, so `false` covers a missing, malformed, expired and revoked token alike; the way to recover
    /// from it is to sign in again with `POST api/2.0/authentication`. It says nothing about who the caller is or how
    /// long the session still lasts - read `GET api/2.0/people/@self` for the profile behind the token.
    /// </remarks>
    /// <summary>Check authentication</summary>
    /// <path>api/2.0/authentication</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Authentication")]
    [SwaggerResponse(200, "`true` when the request carries a valid token or cookie of an active portal user, `false` in every other case", typeof(bool))]
    [AllowNotPayment, AllowAnonymous]
    [HttpGet]
    public bool GetIsAuthentificated()
    {
        return securityContext.IsAuthenticated;
    }

    /// <remarks>
    /// Finishes a two-factor sign-in: checks the one-time code and, when it matches, issues the authentication token.
    /// Call it only after `POST api/2.0/authentication` answered with `sms` or `tfa` set, and repeat the same
    /// credentials in the body next to `code` - the code alone does not identify the user. The code comes from the
    /// SMS the portal sent, which `POST api/2.0/authentication/sendsms` resends, or from the authenticator app;
    /// whichever second factor the portal has enabled for this user is the one checked here. Open to unauthenticated
    /// callers, mutating and not idempotent: a code is single-use, the sign-in is written to the login history, and
    /// the first code accepted from an authenticator app also connects that app to the user. The answer carries
    /// `token` for the `Authorization` header, `expires` unless `session=true` tied the token to the browser session,
    /// and either `sms` with the masked phone number or `tfa`. A wrong, empty or expired code fails with 401 and
    /// counts against the brute-force limit, which then refuses further attempts with 403.
    /// </remarks>
    /// <summary>
    /// Authenticate a user by code
    /// </summary>
    /// <path>api/2.0/authentication/{code}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Authentication")]
    [SwaggerResponse(200, "The authentication token to send in the `Authorization` header, together with the second factor that was accepted", typeof(AuthenticationTokenDto))]
    [SwaggerResponse(400, "The request body could not be validated, for example `confirmData.email` is not an email address")]
    [SwaggerResponse(401, "The credentials were rejected, or the two-factor code is wrong, empty or expired")]
    [SwaggerResponse(403, "The user is disabled, or too many failed attempts have blocked further sign-ins for these credentials")]
    [SwaggerResponse(404, "No user of this portal matches the credentials in the request body")]
    [SwaggerResponse(429, "The portal rate limiter rejected the call - retry after the interval in the `Retry-After` header")]
    [AllowNotPayment, AllowAnonymous]
    // AuthWithCodeRequestsDto carries `code` in the body, so the route placeholder is unbound; the
    // client (loginWithTfaCode) puts the same code in both.
    [SwaggerPathParameter("code", "The two-factor authentication code. Send the same value as the `code` of the request body, which is the one the handler reads.")]
    [HttpPost("{code}", Order = 1)]
    public async Task<AuthenticationTokenDto> AuthenticateMeFromBodyWithCode(AuthWithCodeRequestsDto inDto)
    {
        var tenantId = tenantManager.GetCurrentTenant().Id;
        var user = (await GetUserAsync(inDto)).UserInfo;
        var session = inDto.Session;

        if (user == null || Equals(user, Constants.LostUser))
        {
            throw new ItemNotFoundException(Resource.ErrorUserNotFound);
        }

        if (user.Status != EmployeeStatus.Active)
        {
            throw new InvalidOperationException(Resource.ErrorUserDisabled);
        }

        var sms = false;
        string token = default;

        try
        {
            if (await studioSmsNotificationSettingsHelper.IsVisibleAndAvailableSettingsAsync() && await studioSmsNotificationSettingsHelper.TfaEnabledForUserAsync(user.Id))
            {
                sms = true;
                var (smsValidationResult, smsAuthToken) = await smsManager.ValidateSmsCodeAsync(user, inDto.Code, true, session);
                if (smsValidationResult)
                {
                    token = smsAuthToken;
                }
            }
            else if (tfaAppAuthSettingsHelper.IsVisibleSettings && await tfaAppAuthSettingsHelper.TfaEnabledForUserAsync(user.Id))
            {
                var (tfaValidationResult, tfaAuthToken) = await tfaManager.ValidateAuthCodeAsync(user, inDto.Code, true, true, session);
                if (tfaValidationResult)
                {
                    token = tfaAuthToken;
                    messageService.Send(MessageAction.UserConnectedTfaApp, MessageTarget.Create(user.Id));
                    await socketManager.UpdateUserAsync(userManager.GetUsers(authContext.CurrentAccount.ID));
                }
            }
            else
            {
                throw new SecurityException("Auth code is not available");
            }

            token = string.IsNullOrEmpty(token) ? await cookiesManager.AuthenticateMeAndSetCookiesAsync(user.Id) : token;

            if (!string.IsNullOrEmpty(inDto.Culture) && user.CultureName != inDto.Culture)
            {
                await userManager.ChangeUserCulture(user, inDto.Culture);
                messageService.Send(MessageAction.UserUpdatedLanguage, MessageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper));
            }

            var result = new AuthenticationTokenDto
            {
                Token = token
            };

            if (!session)
            {
                var expires = await tenantCookieSettingsHelper.GetExpiresTimeAsync(tenantId);
                result.Expires = new ApiDateTime(tenantManager, expires);
            }

            if (sms)
            {
                result.Sms = true;
                result.PhoneNoise = SmsSender.BuildPhoneNoise(user.MobilePhone);
            }
            else
            {
                result.Tfa = true;
            }

            return result;
        }
        catch (Exception ex)
        {
            messageService.SendLoginMessage(sms ? MessageAction.LoginFailViaApiSms : MessageAction.LoginFailViaApiTfa,
                                    user.DisplayUserName(false, displayUserSettingsHelper),
                                    MessageTarget.Create(user.Id));
            throw new AuthenticationException(Resource.UserAuthenticationFailed, ex);
        }
        finally
        {
            securityContext.Logout();
        }
    }

    /// <remarks>
    /// Signs a user in to the current portal and either issues the authentication token or reports which second
    /// factor is still missing. Credentials go in the body as `userName` with `password` or `passwordHash`, as the
    /// key of a confirmation link in `confirmData`, or as a third-party account (`provider` with `accessToken`, or
    /// `serializedProfile`), which only a standalone installation or a tariff with third-party sign-in allows. Open
    /// to unauthenticated callers, mutating and not
    /// idempotent: it writes a login event, sets the portal cookies and counts every failure against the brute-force
    /// limit. When a second factor is required for this user the answer carries no `token` but `sms` with the masked
    /// phone number - or a `confirmUrl` pointing at `POST api/2.0/authentication/setphone` while no number is
    /// activated yet - or `tfa` with the setup key while the authenticator app is not connected; submit the code to
    /// `POST api/2.0/authentication/{code}` to finish such a sign-in. Otherwise the answer carries `token` for the
    /// `Authorization` header and `expires`, which is omitted when `session=true` ties the token to the browser
    /// session. An unknown user fails with 404, rejected credentials with 401, a disabled or blocked user with 403.
    /// </remarks>
    /// <summary>
    /// Authenticate a user
    /// </summary>
    /// <path>api/2.0/authentication</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Authentication")]
    [SwaggerResponse(200, "The authentication token, or the second factor that has to be passed before a token is issued", typeof(AuthenticationTokenDto))]
    [SwaggerResponse(400, "The request body could not be validated, for example `confirmData.email` is not an email address")]
    [SwaggerResponse(401, "The password, the confirmation key or the third-party profile was rejected, or third-party sign-in is not allowed for this portal")]
    [SwaggerResponse(403, "The user is disabled, or too many failed attempts and CAPTCHA failures have blocked further sign-ins for these credentials")]
    [SwaggerResponse(404, "No user of this portal matches the credentials in the request body")]
    [SwaggerResponse(429, "The portal rate limiter rejected the call - retry after the interval in the `Retry-After` header")]
    [AllowNotPayment, AllowAnonymous]
    [HttpPost]
    public async Task<AuthenticationTokenDto> AuthenticateMe(AuthRequestsDto inDto)
    {
        var wrapper = await GetUserAsync(inDto);
        var user = wrapper.UserInfo;
        var session = inDto.Session;

        if (user == null || Equals(user, Constants.LostUser))
        {
            throw new ItemNotFoundException(Resource.ErrorUserNotFound);
        }

        if (user.Status != EmployeeStatus.Active)
        {
            throw new InvalidOperationException(Resource.ErrorUserDisabled);
        }

        if (await studioSmsNotificationSettingsHelper.IsVisibleAndAvailableSettingsAsync() && await studioSmsNotificationSettingsHelper.TfaEnabledForUserAsync(user.Id))
        {
            if (string.IsNullOrEmpty(user.MobilePhone) || user.MobilePhoneActivationStatus == MobilePhoneActivationStatus.NotActivated)
            {
                return new AuthenticationTokenDto
                {
                    Sms = true,
                    ConfirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.PhoneActivation)
                };
            }

            await smsManager.PutAuthCodeAsync(user, false);

            return new AuthenticationTokenDto
            {
                Sms = true,
                PhoneNoise = SmsSender.BuildPhoneNoise(user.MobilePhone),
                Expires = new ApiDateTime(tenantManager, DateTime.UtcNow.Add(smsKeyStorage.StoreInterval)),
                ConfirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.PhoneAuth)
            };
        }

        if (tfaAppAuthSettingsHelper.IsVisibleSettings && await tfaAppAuthSettingsHelper.TfaEnabledForUserAsync(user.Id))
        {
            var tfaExpired = await TfaAppUserSettings.TfaExpiredAndResetAsync(settingsManager, auditEventsRepository, user.Id);

            if (tfaExpired || !await TfaAppUserSettings.EnableForUserAsync(settingsManager, user.Id))
            {
                var (urlActivation, keyActivation) = commonLinkUtility.GetConfirmationUrlAndKey(user.Id, ConfirmType.TfaActivation);
                await cookiesManager.SetCookiesAsync(CookiesType.ConfirmKey, keyActivation, true, $"_{ConfirmType.TfaActivation}");
                return new AuthenticationTokenDto
                {
                    Tfa = true,
                    TfaKey = (await tfaManager.GenerateSetupCodeAsync(user)).ManualEntryKey,
                    ConfirmUrl = urlActivation
                };
            }

            var (urlAuth, keyAuth) = commonLinkUtility.GetConfirmationUrlAndKey(user.Id, ConfirmType.TfaAuth);
            await cookiesManager.SetCookiesAsync(CookiesType.ConfirmKey, keyAuth, true, $"_{ConfirmType.TfaAuth}");
            return new AuthenticationTokenDto
            {
                Tfa = true,
                ConfirmUrl = urlAuth
            };
        }

        try
        {
            MessageAction action;
            string initiator = null;
            string[] description = null;

            switch (wrapper.LoginType)
            {
                case LoginType.EmailAndPassword:
                    action = MessageAction.LoginSuccessViaApi;
                    break;
                case LoginType.EmailAndPasswordHash:
                    action = MessageAction.LoginSuccessViaPassword;
                    break;
                case LoginType.ConfirmLink:
                    action = MessageAction.AuthLinkActivated;
                    initiator = inDto.ConfirmData?.Email;
                    description = [inDto.ConfirmData?.Key];
                    break;
                case LoginType.SocialAccount:
                    action = MessageAction.LoginSuccessViaApiSocialAccount;
                    description = [ConsumerExtension.GetResourceString(wrapper.Provider) ?? wrapper.Provider];
                    break;
                default:
                    action = MessageAction.LoginSuccess;
                    break;
            }

            var token = await cookiesManager.AuthenticateMeAndSetCookiesAsync(user.Id, action, session, initiator, true, description);

            if (!string.IsNullOrEmpty(inDto.Culture) && user.CultureName != inDto.Culture)
            {
                await userManager.ChangeUserCulture(user, inDto.Culture);
                messageService.Send(MessageAction.UserUpdatedLanguage, MessageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper));
            }

            var outDto = new AuthenticationTokenDto
            {
                Token = token
            };

            if (!session)
            {
                var tenant = tenantManager.GetCurrentTenantId();
                var expires = await tenantCookieSettingsHelper.GetExpiresTimeAsync(tenant);

                outDto.Expires = new ApiDateTime(tenantManager, expires);
            }

            return outDto;
        }
        catch (Exception ex)
        {
            MessageAction action;
            var loginName = user.DisplayUserName(false, displayUserSettingsHelper);
            string[] description = null;

            switch (wrapper.LoginType)
            {
                case LoginType.EmailAndPassword:
                    action = MessageAction.LoginFailViaApi;
                    break;
                case LoginType.SocialAccount:
                    action = MessageAction.LoginFailViaApiSocialAccount;
                    description = [wrapper.Provider];
                    break;
                default:
                    action = MessageAction.LoginFail;
                    break;
            }

            messageService.SendLoginMessage(action, loginName, description);
            throw new AuthenticationException("User authentication failed", ex);
        }
        finally
        {
            securityContext.Logout();
        }
    }

    /// <remarks>
    /// Ends the session the request itself was made with: the login event behind the authentication cookie is closed,
    /// the sockets opened for it are disconnected, the portal cookies are cleared and a logout event is written to
    /// the login history. Send it with the cookie or token of the session that is to be closed; an anonymous call is
    /// accepted and closes nothing. The operation is mutating and idempotent - the same session cannot be closed
    /// twice - and it touches only that one session: the other sessions of the same user stay alive and are ended by
    /// `PUT api/2.0/security/activeconnections/logoutallexceptthis` or
    /// `PUT api/2.0/security/activeconnections/logout/{loginEventId}`. The answer is a single logout URL when the
    /// user signed in through SSO and the portal has an SLO endpoint configured, and the client has to open that URL
    /// to end the session on the identity provider as well; for everyone else it is empty and nothing more is needed.
    /// </remarks>
    /// <summary>
    /// Log out
    /// </summary>
    /// <path>api/2.0/authentication/logout</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Authentication")]
    [SwaggerResponse(200, "The single logout URL to open when the user signed in through SSO, or an empty result when no further action is needed", typeof(string))]
    [AllowNotPayment, AllowAnonymous]
    [HttpPost("logout")]
    public async Task<string> Logout()
    {
        var cookie = cookiesManager.GetCookies(CookiesType.AuthKey);
        var (loginEventId, _) = cookieStorage.GetLoginEventIdFromCookie(cookie);
        var tenantId = tenantManager.GetCurrentTenantId();
        await dbLoginEventsManager.LogOutEventAsync(tenantId, loginEventId);
        await quotaSocketManager.LogoutSession(securityContext.CurrentAccount.ID, loginEventId);

        var user = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var loginName = user.DisplayUserName(false, displayUserSettingsHelper);
        messageService.SendLoginMessage(MessageAction.Logout, loginName);

        cookiesManager.ClearCookies(CookiesType.AuthKey);
        cookiesManager.ClearCookies(CookiesType.SocketIO);

        securityContext.Logout();


        if (!string.IsNullOrEmpty(user.SsoNameId))
        {
            var settings = await settingsManager.LoadAsync<SsoSettingsV2>();

            if (settings.EnableSso.GetValueOrDefault() && !string.IsNullOrEmpty(settings.IdpSettings.SloUrl))
            {
                var logoutSsoUserData = signature.Create(new LogoutSsoUserData
                {
                    NameId = user.SsoNameId,
                    SessionId = user.SsoSessionId
                });

                return setupInfo.SsoSamlLogoutUrl + "?data=" + HttpUtility.UrlEncode(logoutSsoUserData);
            }
        }

        return null;
    }

    /// <remarks>
    /// Checks the key of a confirmation link that the portal sent by email and reports whether the action behind that
    /// link can still be carried out - an employee invitation, phone activation, a password change, portal removal
    /// and so on. Take `key` and `type` from the query string of the link; when `key` is left empty, the key saved in
    /// the confirmation cookie of the same `type` is used instead. Open to unauthenticated callers and read-only: it
    /// neither accepts the invitation nor signs anyone in. `result` is `Ok` when the link may be used, `Invalid` when
    /// the key does not match the type or the email, `Expired` when it is too old, and `TariffLimit`, `UserExisted`,
    /// `UserExcluded` or `QuotaFailed` when the key is sound but the invitation behind it cannot be accepted. Only
    /// `Ok` should be followed by the operation that performs the action - `POST api/2.0/people` with
    /// `fromInviteLink` for an invitation, `POST api/2.0/authentication` with `confirmData` for a sign-in link - and
    /// for an invitation to a room the answer also carries the identifier and the title of that room.
    /// </remarks>
    /// <summary>
    /// Check a confirmation link
    /// </summary>
    /// <path>api/2.0/authentication/confirm</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Authentication")]
    [SwaggerResponse(200, "Whether the confirmation link may be used, with the room and the email it was issued for when it is an invitation", typeof(ConfirmDto))]
    [SwaggerResponse(403, "The portal's IP restrictions do not allow this address to check an invitation link")]
    [AllowNotPayment, AllowSuspended, AllowAnonymous]
    [HttpPost("confirm")]
    public async Task<ConfirmDto> CheckConfirm(EmailValidationKeyModel inDto)
    {
        if (string.IsNullOrEmpty(inDto.Key))
        {
            inDto.Key = cookiesManager.GetCookies(CookiesType.ConfirmKey, $"_{inDto.Type}");
        }

        if (inDto.Type != ConfirmType.LinkInvite)
        {
            var (validationResult, validationEmail) = await emailValidationKeyModelHelper.ValidateAsync(inDto);
            return new ConfirmDto { Result = validationResult, Email = validationEmail };
        }

        var email = string.IsNullOrEmpty(inDto.Email) && !string.IsNullOrEmpty(inDto.EncEmail)
            ? emailValidationKeyModelHelper.DecryptEmail(inDto.EncEmail)
            : inDto.Email;

        var result = await invitationService.ConfirmAsync(inDto.Key, email, inDto.EmplType ?? default, inDto.RoomId, inDto.UiD);

        return result.Map();
    }

    /// <remarks>
    /// Stores the mobile phone number of a user who is going through phone activation and sends the first SMS
    /// authentication code to it. It is reachable only with the phone-activation confirmation link that
    /// `POST api/2.0/authentication` returns in `confirmUrl` when SMS two-factor is required and the user has no
    /// activated number yet: that link authorizes the call in place of an authentication token, and no token is
    /// issued here. The operation is mutating and not idempotent - it saves the number as not activated, writes an
    /// audit event and sends a message - and an already activated number is not replaced this way, the stored number
    /// has to be erased first. The answer carries `sms`, the masked number and `expires`, the moment the code stops
    /// being accepted. Submit that code to `POST api/2.0/authentication/{code}`, which signs the user in and marks
    /// the number activated, or ask for another one with `POST api/2.0/authentication/sendsms`.
    /// </remarks>
    /// <summary>
    /// Set a mobile phone
    /// </summary>
    /// <path>api/2.0/authentication/setphone</path>
    [Tags("Authentication")]
    [SwaggerResponse(200, "The masked phone number the code was sent to and the moment that code expires - no authentication token yet", typeof(AuthenticationTokenDto))]
    [AllowNotPayment]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "PhoneActivation")]
    [HttpPost("setphone")]
    public async Task<AuthenticationTokenDto> SaveMobilePhone(MobileRequestsDto inDto)
    {
        await securityContext.AuthByClaimAsync();
        var user = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);
        inDto.MobilePhone = await smsManager.SaveMobilePhoneAsync(user, inDto.MobilePhone);
        messageService.Send(MessageAction.UserUpdatedMobileNumber, MessageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper), inDto.MobilePhone);

        return new AuthenticationTokenDto
        {
            Sms = true,
            PhoneNoise = SmsSender.BuildPhoneNoise(inDto.MobilePhone),
            Expires = new ApiDateTime(tenantManager, DateTime.UtcNow.Add(smsKeyStorage.StoreInterval))
        };
    }

    /// <remarks>
    /// Sends a new SMS authentication code to the phone number stored for the user and reports when that code
    /// expires. The credentials in the body are checked exactly as by `POST api/2.0/authentication`, so use this
    /// operation to resend the code after that call answered with `sms`; the user needs SMS two-factor enabled and a
    /// phone number already stored, which `POST api/2.0/authentication/setphone` registers. Open to unauthenticated
    /// callers, mutating and not idempotent: every call sends a message, is counted in the portal's SMS usage and
    /// spends one of the few codes a number is allowed within the code lifetime (ten minutes by default), after which
    /// the call fails until those codes expire. Codes sent earlier stay valid, so a resent code does not invalidate
    /// them, and the first one to be accepted invalidates all of them. The answer carries `sms`, the masked number
    /// and `expires`, and no token - submit the code to `POST api/2.0/authentication/{code}`.
    /// </remarks>
    /// <summary>
    /// Send SMS code
    /// </summary>
    /// <path>api/2.0/authentication/sendsms</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Authentication")]
    [SwaggerResponse(200, "The masked phone number the code was sent to and the moment that code expires - no authentication token yet", typeof(AuthenticationTokenDto))]
    [SwaggerResponse(400, "The request body could not be validated, for example `confirmData.email` is not an email address")]
    [SwaggerResponse(401, "The password, the confirmation key or the third-party profile was rejected")]
    [SwaggerResponse(403, "The user is disabled, or too many failed attempts have blocked further sign-ins for these credentials")]
    [SwaggerResponse(404, "No user of this portal matches the credentials in the request body")]
    [SwaggerResponse(429, "The portal rate limiter rejected the call - retry after the interval in the `Retry-After` header")]
    [AllowNotPayment, AllowAnonymous]
    [HttpPost("sendsms")]
    public async Task<AuthenticationTokenDto> SendSmsCode(AuthRequestsDto inDto)
    {
        var user = (await GetUserAsync(inDto)).UserInfo;

        if (user == null || Equals(user, Constants.LostUser))
        {
            throw new ItemNotFoundException(Resource.ErrorUserNotFound);
        }

        if (user.Status != EmployeeStatus.Active)
        {
            throw new InvalidOperationException(Resource.ErrorUserDisabled);
        }

        await smsManager.PutAuthCodeAsync(user, true);

        return new AuthenticationTokenDto
        {
            Sms = true,
            PhoneNoise = SmsSender.BuildPhoneNoise(user.MobilePhone),
            Expires = new ApiDateTime(tenantManager, DateTime.UtcNow.Add(smsKeyStorage.StoreInterval))
        };
    }

    private async Task<UserInfoWrapper> GetUserAsync(AuthRequestsDto inDto)
    {
        var wrapper = new UserInfoWrapper();

        var action = MessageAction.LoginFailViaApi;
        UserInfo user = null;

        try
        {
            if (inDto.ConfirmData != null)
            {
                wrapper.LoginType = LoginType.ConfirmLink;

                var email = inDto.ConfirmData.Email;

                var (checkKeyResult, _) = await emailValidationKeyModelHelper.ValidateAsync(new EmailValidationKeyModel { Key = inDto.ConfirmData.Key, Email = email, Type = ConfirmType.Auth, First = inDto.ConfirmData.First.ToString() });

                if (checkKeyResult == ValidationResult.Ok)
                {
                    user = email.Contains('@')
                                   ? await userManager.GetUserByEmailAsync(email)
                                   : await userManager.GetUsersAsync(new Guid(email));

                    if (securityContext.IsAuthenticated && securityContext.CurrentAccount.ID != user.Id)
                    {
                        securityContext.Logout();
                        cookiesManager.ClearCookies(CookiesType.AuthKey);
                        cookiesManager.ClearCookies(CookiesType.SocketIO);
                    }
                }
            }
            else if ((string.IsNullOrEmpty(inDto.Provider) && string.IsNullOrEmpty(inDto.SerializedProfile)) || inDto.Provider == "email")
            {
                wrapper.LoginType = LoginType.EmailAndPasswordHash;

                inDto.UserName.ThrowIfNull(new ArgumentException(@"userName empty", "userName"));
                if (!string.IsNullOrEmpty(inDto.Password))
                {
                    inDto.Password.ThrowIfNull(new ArgumentException(@"password empty", "password"));
                }
                else
                {
                    inDto.PasswordHash.ThrowIfNull(new ArgumentException(@"PasswordHash empty", "PasswordHash"));
                }

                inDto.PasswordHash = (inDto.PasswordHash ?? "").Trim();

                if (string.IsNullOrEmpty(inDto.PasswordHash))
                {
                    wrapper.LoginType = LoginType.EmailAndPassword;

                    inDto.Password = (inDto.Password ?? "").Trim();

                    if (!string.IsNullOrEmpty(inDto.Password))
                    {
                        inDto.PasswordHash = passwordHasher.GetClientPassword(inDto.Password);
                    }
                }
                var ldapSettings = await settingsManager.LoadAsync<LdapSettings>();
                var ldapLocalization = new LdapLocalization();
                ldapLocalization.Init(Resource.ResourceManager);
                ldapUserManager.Init(ldapLocalization);

                if (ldapSettings.EnableLdapAuthentication)
                {
                    user = await ldapUserManager.TryGetAndSyncLdapUserInfo(inDto.UserName, inDto.Password);
                }

                if (user == null || Equals(user, Constants.LostUser))
                {
                    user = await userManager.GetUsersByPasswordHashAsync(tenantManager.GetCurrentTenantId(), inDto.UserName, inDto.PasswordHash);
                }

                user = await bruteForceLoginManager.AttemptAsync(inDto.UserName, inDto.RecaptchaType, inDto.RecaptchaResponse, user);
            }
            else
            {
                if (!(coreBaseSettings.Standalone || (await tenantManager.GetTenantQuotaAsync(tenantManager.GetCurrentTenantId())).Oauth))
                {
                    throw new Exception(Resource.ErrorNotAllowedOption);
                }

                action = MessageAction.LoginFailViaApiSocialAccount;

                var thirdPartyProfile = !string.IsNullOrEmpty(inDto.SerializedProfile) ?
                    await loginProfileTransport.FromTransport(inDto.SerializedProfile) :
                    providerManager.GetLoginProfile(inDto.Provider, inDto.AccessToken, inDto.CodeOAuth);

                wrapper.LoginType = LoginType.SocialAccount;
                wrapper.Provider = inDto.Provider ?? thirdPartyProfile.Provider;

                inDto.UserName = thirdPartyProfile.EMail;

                user = await bruteForceLoginManager.AttemptAsync(inDto.UserName, inDto.RecaptchaType, inDto.RecaptchaResponse, await GetUserByThirdParty(thirdPartyProfile));
            }
        }
        catch (BruteForceCredentialException)
        {
            messageService.SendLoginMessage(MessageAction.LoginFailBruteForce, !string.IsNullOrEmpty(inDto.UserName) ? inDto.UserName : AuditResource.EmailNotSpecified);
            throw new BruteForceCredentialException(Resource.ErrorTooManyLoginAttempts);
        }
        catch (RecaptchaException)
        {
            messageService.SendLoginMessage(MessageAction.LoginFailRecaptcha, !string.IsNullOrEmpty(inDto.UserName) ? inDto.UserName : AuditResource.EmailNotSpecified);
            throw new RecaptchaException(Resource.RecaptchaInvalid);
        }
        catch (AuthenticationException ex)
        {
            messageService.SendLoginMessage(action, !string.IsNullOrEmpty(inDto.UserName) ? inDto.UserName : AuditResource.EmailNotSpecified);
            throw new AuthenticationException(ex.Message, ex);
        }
        catch (Exception ex)
        {
            messageService.SendLoginMessage(action, !string.IsNullOrEmpty(inDto.UserName) ? inDto.UserName : AuditResource.EmailNotSpecified);
            throw new AuthenticationException("User authentication failed", ex);
        }
        wrapper.UserInfo = user;
        return wrapper;
    }

    private async Task<UserInfo> GetUserByThirdParty(LoginProfile loginProfile)
    {
        try
        {
            if (!string.IsNullOrEmpty(loginProfile.AuthorizationError))
            {
                // ignore cancellation
                if (loginProfile.AuthorizationError != "Canceled at provider")
                {
                    throw new Exception(loginProfile.AuthorizationError);
                }
                return Constants.LostUser;
            }

            var userInfo = Constants.LostUser;

            var (success, userId) = await TryGetUserByHashAsync(loginProfile.HashId);
            if (success)
            {
                userInfo = await userManager.GetUsersAsync(userId);
            }
            else if (!string.IsNullOrEmpty(loginProfile.EMail) && !string.IsNullOrEmpty(loginProfile.HashId))
            {
                userInfo = await userManager.GetUserByEmailAsync(loginProfile.EMail);
                if (userInfo.Id != Constants.LostUser.Id && userInfo.Status != EmployeeStatus.Terminated)
                {
                    if (userInfo.ActivationStatus != EmployeeActivationStatus.Activated)
                    {
                        var msg = await customNamingPeople.Substitute<Resource>("ErrorEmailAlreadyExists");
                        throw new AuthenticationException(msg);
                    }

                    await accountLinker.AddLinkAsync(userInfo.Id, loginProfile);
                }
            }

            // var isNew = false;
            //
            // if (isNew)
            // {
            //     //TODO:
            //     //var spam = HttpContext.Current.Request["spam"];
            //     //if (spam != "on")
            //     //{
            //     //    try
            //     //    {
            //     //        const string _databaseID = "com";
            //     //        using (var db = DbManager.FromHttpContext(_databaseID))
            //     //        {
            //     //            db.ExecuteNonQuery(new SqlInsert("template_unsubscribe", false)
            //     //                                   .InColumnValue("email", userInfo.Email.ToLowerInvariant())
            //     //                                   .InColumnValue("reason", "personal")
            //     //                );
            //     //            Log.Debug(string.Format("Write to template_unsubscribe {0}", userInfo.Email.ToLowerInvariant()));
            //     //        }
            //     //    }
            //     //    catch (Exception ex)
            //     //    {
            //     //        Log.Debug(string.Format("ERROR write to template_unsubscribe {0}, email:{1}", ex.Message, userInfo.Email.ToLowerInvariant()));
            //     //    }
            //     //}
            //
            //     await studioNotifyService.UserHasJoinAsync();
            //     await userHelpTourHelper.SetIsNewUser(true);
            // }

            return userInfo;
        }
        catch (Exception)
        {
            cookiesManager.ClearCookies(CookiesType.AuthKey);
            cookiesManager.ClearCookies(CookiesType.SocketIO);
            securityContext.Logout();
            throw;
        }
    }


    private async Task<(bool, Guid)> TryGetUserByHashAsync(string hashId)
    {
        var userId = Guid.Empty;
        if (string.IsNullOrEmpty(hashId))
        {
            return (false, userId);
        }

        var linkedProfiles = await accountLinker.GetLinkedObjectsByHashIdAsync(hashId);

        foreach (var profileId in linkedProfiles)
        {
            if (Guid.TryParse(profileId, out var tmp) && await userManager.UserExistsAsync(tmp))
            {
                return (true, tmp);
            }
        }

        return (false, userId);
    }
}

internal class UserInfoWrapper
{
    public UserInfo UserInfo { get; set; }
    public LoginType LoginType { get; set; }
    public string Provider { get; set; }
}

internal enum LoginType
{
    EmailAndPassword,
    EmailAndPasswordHash,
    ConfirmLink,
    SocialAccount
}
