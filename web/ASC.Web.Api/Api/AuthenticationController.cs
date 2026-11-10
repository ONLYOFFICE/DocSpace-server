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
    /// Returns whether the request carries a valid sign-in to the current portal. The call checks the authentication
    /// cookie or the token in the `Authorization` header. Anyone can call it without signing in. It is read-only and
    /// also works while the portal is unpaid. It returns `true` for a signed-in user and `false` in every other case.
    /// A `false` result gives no reason. The token may be missing, malformed, expired, or revoked. To sign in again,
    /// use `POST api/2.0/authentication`. To learn who the caller is, use `GET api/2.0/people/@self`.
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
    /// Finishes a two-factor sign-in with a one-time code and returns the authentication token. Call it after
    /// `POST api/2.0/authentication` returns `sms` or `tfa`. Send the same credentials again, with the code in `code`.
    /// With SMS codes on, the call checks an SMS code. Otherwise it checks an authenticator app code or a backup code.
    /// Anyone can call it without signing in. The call changes data, and each code works only once. A repeated call
    /// with the same code returns 401. An accepted SMS code cancels the other codes and activates the phone number.
    /// The first accepted app code connects the app and creates backup codes. A new `culture` also becomes the user's
    /// language. The answer has `token`, and `sms` or `tfa` shows the checked factor. To get a new SMS code, use
    /// `POST api/2.0/authentication/sendsms`. The call returns 401 for a wrong, empty, or expired code. It also
    /// returns 401 if the user has no second factor. After too many wrong codes, even a right code returns 401 for up
    /// to 10 minutes. A wrong password or an unknown user also returns 401. The call returns 403 for a disabled user
    /// or too many failed password attempts. A rejected key in `confirmData` returns 404.
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
        if (!string.IsNullOrEmpty(inDto.Culture))
        {
            var culture = coreBaseSettings.EnabledCultures.Find(c => string.Equals(c.Name, inDto.Culture, StringComparison.InvariantCultureIgnoreCase));
            if (culture != null)
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }
        }
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
    /// Signs a user in to the current portal and returns an authentication token. If the user has a second factor,
    /// the call returns what that factor needs instead. Send `userName` with `password` or `passwordHash`, a
    /// confirmation link in `confirmData`, or a third-party account. Anyone can call it without signing in. The call
    /// changes data, and each successful call opens a new session. You can repeat it with the same body. A new
    /// `culture` also becomes the user's language. With SMS codes on, the answer has `sms` and no `token`. Without an
    /// activated phone number, `confirmUrl` holds a phone activation link for `POST api/2.0/authentication/setphone`.
    /// Otherwise the call sends an SMS code. It sends none while an earlier code is still valid. With an
    /// authenticator app on, the answer has `tfa` and no `token`. Before the app is connected, the answer also has
    /// its setup key in `tfaKey`. To finish such a sign-in, send the code to `POST api/2.0/authentication/{code}`.
    /// The call returns 401 for a wrong password, an unknown user, or a rejected third-party account. It also returns
    /// 401 if the portal does not allow third-party sign-in. A rejected `confirmData` key or its unknown user
    /// returns 404. The call returns 403 for a disabled user, a wrong CAPTCHA, or too many failed attempts. Attempts
    /// count per user name and IP address. During a block, a solved CAPTCHA in `recaptchaResponse` lets the call through.
    /// The portal's login settings set the attempt limit and the block time.
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
        if (!string.IsNullOrEmpty(inDto.Culture))
        {
            var culture = coreBaseSettings.EnabledCultures.Find(c => string.Equals(c.Name, inDto.Culture, StringComparison.InvariantCultureIgnoreCase));
            if (culture != null)
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }
        }

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
    /// Ends the session of the authentication cookie that comes with the request. The token of that session stops
    /// working, and the call clears the portal cookies. A token sent only in the `Authorization` header stays valid.
    /// To end such a session, use `PUT api/2.0/security/activeconnections/logout/{loginEventId}`. Anyone can call it
    /// without signing in. Without a session cookie, the call ends nothing. You can repeat it. Other sessions of the
    /// user stay open. To end them, use `PUT api/2.0/security/activeconnections/logoutallexceptthis`. For a user who
    /// signed in through SSO, the call can return a single logout URL. It does so when SSO is on and has an SLO
    /// address. Open this URL to end the session at the identity provider too. For other users, the result is empty.
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
    /// Checks the key of a confirmation link and returns whether the link can still be used. Links include
    /// invitations, phone activation, password changes, and portal deletion. Take `key`, `type`, and the other values
    /// from the query string of the link. If `key` is empty, the call uses the confirmation cookie of this `type`.
    /// Anyone can call it without signing in, even on an unpaid, suspended, or blocked portal. For a signed-out
    /// caller, the call is read-only. It does not accept an invitation or sign anyone in. A signed-in caller with a
    /// room's shared invitation link joins that room with the link's role. The call also counts one use of that link.
    /// A repeated call changes nothing. For a signed-in caller, a valid invitation link returns `UserExisted` instead
    /// of `Ok`. Follow only an `Ok` result with the action of the link. For an invitation, use `POST api/2.0/people`
    /// with `fromInviteLink`. For a sign-in link, use `POST api/2.0/authentication` with `confirmData`. If the
    /// portal's IP restrictions block the caller, an invitation link returns 403. The call returns 400 if `email` is
    /// not a valid email address.
    /// </remarks>
    /// <summary>
    /// Check a confirmation link
    /// </summary>
    /// <path>api/2.0/authentication/confirm</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Authentication")]
    [SwaggerResponse(200, "Whether the confirmation link may be used, with the room and the email it was issued for when it is an invitation", typeof(ConfirmDto))]
    [SwaggerResponse(400, "The request body cannot be read, or `email` is sent but empty or not a valid email address")]
    [SwaggerResponse(403, "The portal's IP restrictions do not allow this address to check an invitation link")]
    [AllowNotPayment, AllowSuspended, AllowBlocked, AllowAnonymous]
    [HttpPost("confirm")]
    public async Task<ConfirmDto> CheckConfirm(CheckConfirmRequestDto inDto)
    {
        if (string.IsNullOrEmpty(inDto.Key))
        {
            inDto.Key = cookiesManager.GetCookies(CookiesType.ConfirmKey, $"_{inDto.Type}");
        }

        if (inDto.Type != ConfirmType.LinkInvite)
        {
            var (validationResult, validationEmail) = await emailValidationKeyModelHelper.ValidateAsync(inDto.Map());
            return new ConfirmDto { Result = validationResult, Email = validationEmail };
        }

        var email = string.IsNullOrEmpty(inDto.Email) && !string.IsNullOrEmpty(inDto.EncEmail)
            ? emailValidationKeyModelHelper.DecryptEmail(inDto.EncEmail)
            : inDto.Email;

        var result = await invitationService.ConfirmAsync(inDto.Key, email, inDto.EmplType ?? default, inDto.RoomId, inDto.UiD);

        return result.Map();
    }

    /// <remarks>
    /// Saves the phone number of a user and sends an SMS code to it. Use it when `POST api/2.0/authentication`
    /// returns `sms` with a phone activation link in `confirmUrl`. Sign the request with the confirmation token from
    /// that link. Other callers get 401. An authentication token alone does not work here. The call saves the number as not activated. The number becomes activated when
    /// `POST api/2.0/authentication/{code}` accepts a code sent to it. If a code sent to this number is still valid,
    /// the call sends no new SMS. You can repeat it to change a number that is not activated yet. The answer has no
    /// token. To send another code, use `POST api/2.0/authentication/sendsms`. If the user already has an activated
    /// number, the call returns 500. An empty number also returns 500. If SMS codes are on for the user but the
    /// portal cannot send them, the call returns 405. It still saves the number but sends no SMS.
    /// </remarks>
    /// <summary>
    /// Set a mobile phone
    /// </summary>
    /// <path>api/2.0/authentication/setphone</path>
    [Tags("Authentication")]
    [SwaggerResponse(200, "The masked phone number the code was sent to and the moment that code expires - no authentication token yet", typeof(AuthenticationTokenDto))]
    [SwaggerResponse(405, "SMS two-factor is switched on for the user but the portal cannot send SMS codes, for example because its plan does not include them; the number is stored and no code is sent")]
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
    /// Sends a new SMS code to the stored phone number of a user. Use it to resend the code after
    /// `POST api/2.0/authentication` returns `sms`. The call checks the credentials in the body as
    /// `POST api/2.0/authentication` does. Anyone can call it without signing in. Each call sends another SMS and
    /// counts toward the SMS usage of the portal. Earlier codes stay valid until they expire, by default after 10
    /// minutes. The first accepted code cancels every code of the number. By default, a number gets at most 6 codes
    /// in 10 minutes. You can repeat the call up to this limit. More calls return 500 until the codes expire. The
    /// answer has no token. To finish the sign-in, send the code to `POST api/2.0/authentication/{code}`. The call
    /// returns 401 for a wrong password or an unknown user. It returns 403 for a disabled user or after too many
    /// failed attempts. A rejected key in `confirmData` returns 404. If the user has no stored phone number, the call
    /// returns 400. If SMS codes are off on the portal or for the user, it returns 405.
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
    [SwaggerResponse(405, "SMS two-factor is not available on the portal or not required for this user, so there is no code to send")]
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
