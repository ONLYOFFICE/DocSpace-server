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

using AuthenticationException = System.Security.Authentication.AuthenticationException;
using Constants = ASC.Core.Users.Constants;

namespace ASC.Web.Api.Controllers;

/// <summary>
/// Authorization API.
/// </summary>
/// <name>authentication</name>
[Scope]
[DefaultRoute]
[ApiController]
[AllowAnonymous]
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
    UserManagerWrapper userManagerWrapper,
    Signature signature,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    StudioSmsNotificationSettingsHelper studioSmsNotificationSettingsHelper,
    SettingsManager settingsManager,
    SmsManager smsManager,
    TfaManager tfaManager,
    TimeZoneConverter timeZoneConverter,
    SmsKeyStorage smsKeyStorage,
    CommonLinkUtility commonLinkUtility,
    ApiContext apiContext,
    AuthContext authContext,
    CookieStorage cookieStorage,
    QuotaSocketManager quotaSocketManager,
    DbLoginEventsManager dbLoginEventsManager,
    BruteForceLoginManager bruteForceLoginManager,
    TfaAppAuthSettingsHelper tfaAppAuthSettingsHelper,
    ILogger<AuthenticationController> logger,
    InvitationService invitationService,
    LoginProfileTransport loginProfileTransport,
    IMapper mapper)
    : ControllerBase
{
    /// <summary>
    /// Checks if the current user is authenticated or not.
    /// </summary>
    /// <short>Check authentication</short>
    /// <httpMethod>GET</httpMethod>
    /// <path>api/2.0/authentication</path>
    /// <returns type="System.Boolean, System">Boolean value: true if the current user is authenticated</returns>
    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowNotPayment]
    [HttpGet]
    public bool GetIsAuthentificated()
    {
        return securityContext.IsAuthenticated;
    }

    /// <summary>
    /// Authenticates the current user by SMS or two-factor authentication code.
    /// </summary>
    /// <short>
    /// Authenticate a user by code
    /// </short>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.AuthRequestsDto, ASC.Web.Api" name="inDto">Authentication request parameters</param>
    /// <httpMethod>POST</httpMethod>
    /// <path>api/2.0/authentication/{code}</path>
    /// <returns type="ASC.Web.Api.ApiModel.ResponseDto.AuthenticationTokenDto, ASC.Web.Api">Authentication data</returns>
    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowNotPayment]
    [HttpPost("{code}", Order = 1)]
    public async Task<AuthenticationTokenDto> AuthenticateMeFromBodyWithCode(AuthRequestsDto inDto)
    {
        var tenant = (await tenantManager.GetCurrentTenantAsync()).Id;
        var user = (await GetUserAsync(inDto)).UserInfo;
        var sms = false;

        try
        {
            if (await studioSmsNotificationSettingsHelper.IsVisibleAndAvailableSettingsAsync() && await studioSmsNotificationSettingsHelper.TfaEnabledForUserAsync(user.Id))
            {
                sms = true;
                await smsManager.ValidateSmsCodeAsync(user, inDto.Code, true);
            }
            else if (tfaAppAuthSettingsHelper.IsVisibleSettings && await tfaAppAuthSettingsHelper.TfaEnabledForUserAsync(user.Id))
            {
                if (await tfaManager.ValidateAuthCodeAsync(user, inDto.Code, true, true))
                {
                    await messageService.SendAsync(MessageAction.UserConnectedTfaApp, MessageTarget.Create(user.Id));
                }
            }
            else
            {
                throw new SecurityException("Auth code is not available");
            }

            var token = await cookiesManager.AuthenticateMeAndSetCookiesAsync(user.Id);
            var expires = await tenantCookieSettingsHelper.GetExpiresTimeAsync(tenant);

            var result = new AuthenticationTokenDto
            {
                Token = token,
                Expires = new ApiDateTime(tenantManager, timeZoneConverter, expires)
            };

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
            await messageService.SendAsync(user.DisplayUserName(false, displayUserSettingsHelper), sms
                                                                          ? MessageAction.LoginFailViaApiSms
                                                                          : MessageAction.LoginFailViaApiTfa,
                                MessageTarget.Create(user.Id));
            logger.ErrorWithException(ex);
            throw new AuthenticationException("User authentication failed");
        }
        finally
        {
            securityContext.Logout();
        }
    }

    /// <summary>
    /// Authenticates the current user by SMS, authenticator app, or without two-factor authentication.
    /// </summary>
    /// <short>
    /// Authenticate a user
    /// </short>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.AuthRequestsDto, ASC.Web.Api" name="inDto">Authentication request parameters</param>
    /// <httpMethod>POST</httpMethod>
    /// <path>api/2.0/authentication</path>
    /// <returns type="ASC.Web.Api.ApiModel.ResponseDto.AuthenticationTokenDto, ASC.Web.Api">Authentication data</returns>
    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowNotPayment]
    [HttpPost]
    public async Task<AuthenticationTokenDto> AuthenticateMeAsync(AuthRequestsDto inDto)
    {
        var wrapper = await GetUserAsync(inDto);
        var viaEmail = wrapper.ViaEmail;
        var user = wrapper.UserInfo;
        var session = inDto.Session;

        if (user == null || Equals(user, Constants.LostUser))
        {
            throw new Exception(Resource.ErrorUserNotFound);
        }

        if (user.Status != EmployeeStatus.Active)
        {
            throw new Exception(Resource.ErrorUserDisabled);
        }

        if (await studioSmsNotificationSettingsHelper.IsVisibleAndAvailableSettingsAsync() && await studioSmsNotificationSettingsHelper.TfaEnabledForUserAsync(user.Id))
        {
            if (string.IsNullOrEmpty(user.MobilePhone) || user.MobilePhoneActivationStatus == MobilePhoneActivationStatus.NotActivated)
            {
                return new AuthenticationTokenDto
                {
                    Sms = true,
                    ConfirmUrl = await commonLinkUtility.GetConfirmationEmailUrlAsync(user.Email, ConfirmType.PhoneActivation)
                };
            }

            await smsManager.PutAuthCodeAsync(user, false);

            return new AuthenticationTokenDto
            {
                Sms = true,
                PhoneNoise = SmsSender.BuildPhoneNoise(user.MobilePhone),
                Expires = new ApiDateTime(tenantManager, timeZoneConverter, DateTime.UtcNow.Add(smsKeyStorage.StoreInterval)),
                ConfirmUrl = await commonLinkUtility.GetConfirmationEmailUrlAsync(user.Email, ConfirmType.PhoneAuth)
            };
        }

        if (tfaAppAuthSettingsHelper.IsVisibleSettings && await tfaAppAuthSettingsHelper.TfaEnabledForUserAsync(user.Id))
        {
            if (!await TfaAppUserSettings.EnableForUserAsync(settingsManager, user.Id))
            {
                var (urlActivation, keyActivation) = await commonLinkUtility.GetConfirmationUrlAndKeyAsync(user.Email, ConfirmType.TfaActivation);
                await cookiesManager.SetCookiesAsync(CookiesType.ConfirmKey, keyActivation, true, $"_{ConfirmType.TfaActivation}");
                return new AuthenticationTokenDto
                {
                    Tfa = true,
                    TfaKey = (await tfaManager.GenerateSetupCodeAsync(user)).ManualEntryKey,
                    ConfirmUrl = urlActivation
                };
            }

            var (urlAuth, keyAuth) = await commonLinkUtility.GetConfirmationUrlAndKeyAsync(user.Email, ConfirmType.TfaAuth);
            await cookiesManager.SetCookiesAsync(CookiesType.ConfirmKey, keyAuth, true, $"_{ConfirmType.TfaAuth}");
            return new AuthenticationTokenDto
            {
                Tfa = true,
                ConfirmUrl = urlAuth
            };
        }

        try
        {
            var action = viaEmail ? MessageAction.LoginSuccessViaApi : MessageAction.LoginSuccessViaApiSocialAccount;
            var token = await cookiesManager.AuthenticateMeAndSetCookiesAsync(user.Id, action, session);

            if (!string.IsNullOrEmpty(inDto.Culture))
            {
                await userManager.ChangeUserCulture(user, inDto.Culture);
                await messageService.SendAsync(MessageAction.UserUpdatedLanguage, MessageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper));
            }

            var outDto = new AuthenticationTokenDto
            {
                Token = token
            };

            if (!session)
            {
                var tenant = await tenantManager.GetCurrentTenantIdAsync();
                var expires = await tenantCookieSettingsHelper.GetExpiresTimeAsync(tenant);

                outDto.Expires = new ApiDateTime(tenantManager, timeZoneConverter, expires);
            }

            return outDto;
        }
        catch (Exception ex)
        {
            await messageService.SendAsync(user.DisplayUserName(false, displayUserSettingsHelper), viaEmail ? MessageAction.LoginFailViaApi : MessageAction.LoginFailViaApiSocialAccount);
            logger.ErrorWithException(ex);
            throw new AuthenticationException("User authentication failed");
        }
        finally
        {
            securityContext.Logout();
        }
    }

    /// <summary>
    /// Logs out of the current user account.
    /// </summary>
    /// <short>
    /// Log out
    /// </short>
    /// <httpMethod>POST</httpMethod>
    /// <path>api/2.0/authentication/logout</path>
    /// <returns></returns>
    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowNotPayment]
    [HttpPost("logout")]
    [HttpGet("logout")]// temp fix
    public async Task<object> LogoutAsync()
    {
        var cookie = cookiesManager.GetCookies(CookiesType.AuthKey);
        var loginEventId = cookieStorage.GetLoginEventIdFromCookie(cookie);
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        await dbLoginEventsManager.LogOutEventAsync(tenantId, loginEventId);
        await quotaSocketManager.LogoutSession(securityContext.CurrentAccount.ID, loginEventId);

        var user = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var loginName = user.DisplayUserName(false, displayUserSettingsHelper);
        await messageService.SendAsync(loginName, MessageAction.Logout);

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

    /// <summary>
    /// Opens a confirmation email URL to validate a certain action (employee invitation, portal removal, phone activation, etc.).
    /// </summary>
    /// <short>
    /// Open confirmation email URL
    /// </short>
    /// <param type="ASC.Security.Cryptography.EmailValidationKeyModel, ASC.Core.Common" name="inDto">Confirmation email parameters</param>
    /// <httpMethod>POST</httpMethod>
    /// <path>api/2.0/authentication/confirm</path>
    /// <returns type="ASC.Security.Cryptography.EmailValidationKeyProvider.ValidationResult, ASC.Security.Cryptography">Validation result: Ok, Invalid, or Expired</returns>
    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowNotPayment, AllowSuspended]
    [HttpPost("confirm")]
    public async Task<ConfirmDto> CheckConfirm(EmailValidationKeyModel inDto)
    {
        if (string.IsNullOrEmpty(inDto.Key))
        {
            inDto.Key = cookiesManager.GetCookies(CookiesType.ConfirmKey, $"_{inDto.Type}");
        }

        if (inDto.Type != ConfirmType.LinkInvite)
        {
            return new ConfirmDto { Result = await emailValidationKeyModelHelper.ValidateAsync(inDto)};
        }

        var result = await invitationService.ConfirmAsync(inDto.Key, inDto.Email, inDto.EmplType ?? default, inDto.RoomId, inDto.UiD);

        return mapper.Map<Validation, ConfirmDto>(result);
    }

    /// <summary>
    /// Sets a mobile phone for the current user.
    /// </summary>
    /// <short>
    /// Set a mobile phone
    /// </short>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.MobileRequestsDto, ASC.Web.Api" name="inDto">Mobile phone request parameters</param>
    /// <httpMethod>POST</httpMethod>
    /// <path>api/2.0/authentication/setphone</path>
    /// <returns type="ASC.Web.Api.ApiModel.ResponseDto.AuthenticationTokenDto, ASC.Web.Api">Authentication data</returns>
    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowNotPayment]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "PhoneActivation")]
    [HttpPost("setphone")]
    public async Task<AuthenticationTokenDto> SaveMobilePhoneAsync(MobileRequestsDto inDto)
    {
        await apiContext.AuthByClaimAsync();
        var user = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);
        inDto.MobilePhone = await smsManager.SaveMobilePhoneAsync(user, inDto.MobilePhone);
        await messageService.SendAsync(MessageAction.UserUpdatedMobileNumber, MessageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper), inDto.MobilePhone);

        return new AuthenticationTokenDto
        {
            Sms = true,
            PhoneNoise = SmsSender.BuildPhoneNoise(inDto.MobilePhone),
            Expires = new ApiDateTime(tenantManager, timeZoneConverter, DateTime.UtcNow.Add(smsKeyStorage.StoreInterval))
        };
    }

    /// <summary>
    /// Sends SMS with an authentication code.
    /// </summary>
    /// <short>
    /// Send SMS code
    /// </short>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.AuthRequestsDto, ASC.Web.Api" name="inDto">Authentication request parameters</param>
    /// <httpMethod>POST</httpMethod>
    /// <path>api/2.0/authentication/sendsms</path>
    /// <returns type="ASC.Web.Api.ApiModel.ResponseDto.AuthenticationTokenDto, ASC.Web.Api">Authentication data</returns>
    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowNotPayment]
    [HttpPost("sendsms")]
    public async Task<AuthenticationTokenDto> SendSmsCodeAsync(AuthRequestsDto inDto)
    {
        var user = (await GetUserAsync(inDto)).UserInfo;
        await smsManager.PutAuthCodeAsync(user, true);

        return new AuthenticationTokenDto
        {
            Sms = true,
            PhoneNoise = SmsSender.BuildPhoneNoise(user.MobilePhone),
            Expires = new ApiDateTime(tenantManager, timeZoneConverter, DateTime.UtcNow.Add(smsKeyStorage.StoreInterval))
        };
    }

    private async Task<UserInfoWrapper> GetUserAsync(AuthRequestsDto inDto)
    {
        var wrapper = new UserInfoWrapper
        {
            ViaEmail = true
        };

        var action = MessageAction.LoginFailViaApi;
        UserInfo user = null;

        try
        {
            if (inDto.ConfirmData != null)
            {
                var email = inDto.ConfirmData.Email;
                    
                var checkKeyResult = await emailValidationKeyModelHelper.ValidateAsync(new EmailValidationKeyModel { Key = inDto.ConfirmData.Key, Email = email, Type = ConfirmType.Auth, First = inDto.ConfirmData.First.ToString() });

                if (checkKeyResult == ValidationResult.Ok)
                {
                    user = email.Contains("@")
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

                if(user == null || Equals(user, Constants.LostUser))
                {
                    user = await userManager.GetUsersByPasswordHashAsync(await tenantManager.GetCurrentTenantIdAsync(), inDto.UserName, inDto.PasswordHash);
                }

                user = await bruteForceLoginManager.AttemptAsync(inDto.UserName, inDto.RecaptchaType, inDto.RecaptchaResponse, user);
            }
            else
            {
                if (!(coreBaseSettings.Standalone || (await tenantManager.GetTenantQuotaAsync(await tenantManager.GetCurrentTenantIdAsync())).Oauth))
                {
                    throw new Exception(Resource.ErrorNotAllowedOption);
                }
                wrapper.ViaEmail = false;
                action = MessageAction.LoginFailViaApiSocialAccount;
                var thirdPartyProfile = !string.IsNullOrEmpty(inDto.SerializedProfile) ? 
                    await loginProfileTransport.FromTransport(inDto.SerializedProfile) : 
                    providerManager.GetLoginProfile(inDto.Provider, inDto.AccessToken, inDto.CodeOAuth);

                inDto.UserName = thirdPartyProfile.EMail;
                
                user = await bruteForceLoginManager.AttemptAsync(inDto.UserName, inDto.RecaptchaType, inDto.RecaptchaResponse, await GetUserByThirdParty(thirdPartyProfile));
            }
        }
        catch (BruteForceCredentialException)
        {
            await messageService.SendAsync(!string.IsNullOrEmpty(inDto.UserName) ? inDto.UserName : AuditResource.EmailNotSpecified, MessageAction.LoginFailBruteForce);
            throw new BruteForceCredentialException(Resource.ErrorTooManyLoginAttempts);
        }
        catch (RecaptchaException)
        {
            await messageService.SendAsync(!string.IsNullOrEmpty(inDto.UserName) ? inDto.UserName : AuditResource.EmailNotSpecified, MessageAction.LoginFailRecaptcha);
            throw new RecaptchaException(Resource.RecaptchaInvalid);
        }
        catch (Exception ex)
        {
            await messageService.SendAsync(!string.IsNullOrEmpty(inDto.UserName) ? inDto.UserName : AuditResource.EmailNotSpecified, action);
            logger.ErrorWithException(ex);
            throw new AuthenticationException("User authentication failed");
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

        private async Task<UserInfo> JoinByThirdPartyAccount(LoginProfile loginProfile)
    {
        if (string.IsNullOrEmpty(loginProfile.EMail))
        {
            throw new Exception(Resource.ErrorNotCorrectEmail);
        }

        var userInfo = await userManager.GetUserByEmailAsync(loginProfile.EMail);
        if (!await userManager.UserExistsAsync(userInfo.Id))
        {
            var newUserInfo = ProfileToUserInfo(loginProfile);

            try
            {
                await securityContext.AuthenticateMeWithoutCookieAsync(ASC.Core.Configuration.Constants.CoreSystem);
                userInfo = await userManagerWrapper.AddUserAsync(newUserInfo, UserManagerWrapper.GeneratePassword());
            }
            finally
            {
                securityContext.Logout();
            }
        }

        await accountLinker.AddLinkAsync(userInfo.Id, loginProfile);

        return userInfo;
    }

    private UserInfo ProfileToUserInfo(LoginProfile loginProfile)
    {
        if (string.IsNullOrEmpty(loginProfile.EMail))
        {
            throw new Exception(Resource.ErrorNotCorrectEmail);
        }

        var firstName = loginProfile.FirstName;
        if (string.IsNullOrEmpty(firstName))
        {
            firstName = loginProfile.DisplayName;
        }

        var userInfo = new UserInfo
        {
            FirstName = string.IsNullOrEmpty(firstName) ? UserControlsCommonResource.UnknownFirstName : firstName,
            LastName = string.IsNullOrEmpty(loginProfile.LastName) ? UserControlsCommonResource.UnknownLastName : loginProfile.LastName,
            Email = loginProfile.EMail,
            Title = string.Empty,
            Location = string.Empty,
            CultureName = coreBaseSettings.CustomMode ? "ru-RU" : Thread.CurrentThread.CurrentUICulture.Name,
            ActivationStatus = EmployeeActivationStatus.Activated
        };

        var gender = loginProfile.Gender;
        if (!string.IsNullOrEmpty(gender))
        {
            userInfo.Sex = gender == "male";
        }

        return userInfo;
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
                userId = tmp;
                break;
            }
        }

        return (true, userId);
    }
}

class UserInfoWrapper
{
    public UserInfo UserInfo { get; set; }
    public bool ViaEmail { get; set; }
}