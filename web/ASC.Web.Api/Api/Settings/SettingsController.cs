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

namespace ASC.Web.Api.Controllers.Settings;

public partial class SettingsController(MessageService messageService,
        ApiContext apiContext,
        UserManager userManager,
        TenantManager tenantManager,
        TenantExtra tenantExtra,
        AuthContext authContext,
        PermissionContext permissionContext,
        SettingsManager settingsManager,
        WebItemManager webItemManager,
        WebItemManagerSecurity webItemManagerSecurity,
        TenantInfoSettingsHelper tenantInfoSettingsHelper,
        TenantUtil tenantUtil,
        CoreSettings coreSettings,
        CoreBaseSettings coreBaseSettings,
        CommonLinkUtility commonLinkUtility,
        IConfiguration configuration,
        SetupInfo setupInfo,
        GeolocationHelper geolocationHelper,
        StatisticManager statisticManager,
        ConsumerFactory consumerFactory,
        TimeZoneConverter timeZoneConverter,
        CustomNamingPeople customNamingPeople,
        IMemoryCache memoryCache,
        ProviderManager providerManager,
        FirstTimeTenantSettings firstTimeTenantSettings,
        TelegramHelper telegramHelper,
        PasswordHasher passwordHasher,
        IHttpContextAccessor httpContextAccessor,
        DnsSettings dnsSettings,
        CustomColorThemesSettingsHelper customColorThemesSettingsHelper,
        UserInvitationLimitHelper userInvitationLimitHelper,
        QuotaUsageManager quotaUsageManager,
        TenantDomainValidator tenantDomainValidator,
        ExternalShare externalShare,
        IMapper mapper,
        UserFormatter userFormatter,
        IDistributedLockProvider distributedLockProvider,
        UsersQuotaSyncOperation usersQuotaSyncOperation,
        CustomQuota customQuota,
        QuotaSocketManager quotaSocketManager)
    : BaseSettingsController(apiContext, memoryCache, webItemManager, httpContextAccessor)
{
    [GeneratedRegex("^[a-z0-9]([a-z0-9-.]){1,253}[a-z0-9]$")]
    private static partial Regex EmailDomainRegex();

    /// <summary>
    /// Returns a list of all the available portal settings with the current values for each parameter.
    /// </summary>
    /// <short>
    /// Get the portal settings
    /// </short>
    /// <category>Common settings</category>
    /// <param type="System.Boolean, System" name="withpassword">Specifies if the password hasher settings will be returned or not</param>
    /// <returns type="ASC.Web.Api.ApiModel.ResponseDto.SettingsDto, ASC.Web.Api">Settings</returns>
    /// <path>api/2.0/settings</path>
    /// <httpMethod>GET</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    [HttpGet("")]
    [AllowNotPayment, AllowSuspended, AllowAnonymous]
    public async Task<SettingsDto> GetSettingsAsync(bool? withpassword)
    {
        var studioAdminMessageSettings = await settingsManager.LoadAsync<StudioAdminMessageSettings>();
        var tenantCookieSettings = await settingsManager.LoadAsync<TenantCookieSettings>();
        var tenant = await tenantManager.GetCurrentTenantAsync();

        var settings = new SettingsDto
        {
            Culture = coreBaseSettings.GetRightCultureName(tenant.GetCulture()),
            GreetingSettings = tenant.Name == "" ? Resource.PortalName : tenant.Name,
            DocSpace = true,
            Standalone = coreBaseSettings.Standalone,
            BaseDomain = coreBaseSettings.Standalone ? await coreSettings.GetSettingAsync("BaseDomain") ?? coreBaseSettings.Basedomain : coreBaseSettings.Basedomain,
            Version = configuration["version:number"] ?? "",
            TenantStatus = tenant.Status,
            TenantAlias = tenant.Alias,
            EnableAdmMess = studioAdminMessageSettings.Enable || await tenantExtra.IsNotPaidAsync(),
            LegalTerms = setupInfo.LegalTerms,
            CookieSettingsEnabled = tenantCookieSettings.Enabled,
            UserNameRegex = userFormatter.UserNameRegex.ToString(),
            ForumLink = await commonLinkUtility.GetUserForumLinkAsync(settingsManager)
        };

        if (!authContext.IsAuthenticated && await externalShare.GetLinkIdAsync() != default)
        {
            settings.SocketUrl = configuration["web:hub:url"] ?? "";
        }

        if (authContext.IsAuthenticated)
        {
            settings.TrustedDomains = tenant.TrustedDomains;
            settings.TrustedDomainsType = tenant.TrustedDomainsType;
            var timeZone = timeZoneConverter.GetTimeZone(tenant.TimeZone);
            settings.Timezone = timeZoneConverter.GetIanaTimeZoneId(timeZone);
            settings.UtcOffset = timeZone.GetUtcOffset(DateTime.UtcNow);
            settings.UtcHoursOffset = settings.UtcOffset.TotalHours;
            settings.OwnerId = tenant.OwnerId;
            settings.NameSchemaId = (await customNamingPeople.GetCurrent()).Id;
            settings.DomainValidator = tenantDomainValidator;
            settings.ZendeskKey = setupInfo.ZendeskKey;
            settings.TagManagerId = setupInfo.TagManagerId;
            settings.BookTrainingEmail = setupInfo.BookTrainingEmail;
            settings.DocumentationEmail = setupInfo.DocumentationEmail;
            settings.SocketUrl = configuration["web:hub:url"] ?? "";
            settings.LimitedAccessSpace = (await settingsManager.LoadAsync<TenantAccessSpaceSettings>()).LimitedAccessSpace;

            settings.Firebase = new FirebaseDto
            {
                ApiKey = configuration["firebase:apiKey"] ?? "",
                AuthDomain = configuration["firebase:authDomain"] ?? "",
                ProjectId = configuration["firebase:projectId"] ?? "",
                StorageBucket = configuration["firebase:storageBucket"] ?? "",
                MessagingSenderId = configuration["firebase:messagingSenderId"] ?? "",
                AppId = configuration["firebase:appId"] ?? "",
                MeasurementId = configuration["firebase:measurementId"] ?? "",
                DatabaseURL = configuration["firebase:databaseURL"] ?? ""
            };

            settings.DeepLink = new DeepLinkDto
            {
                AndroidPackageName = configuration["deeplink:androidpackagename"] ?? "",
                Url = configuration["deeplink:url"] ?? "",
                IosPackageId = configuration["deeplink:iospackageid"] ?? ""
            };

            settings.HelpLink = await commonLinkUtility.GetHelpLinkAsync(settingsManager);
            settings.ApiDocsLink = configuration["web:api-docs"];

            if (bool.TryParse(configuration["debug-info:enabled"], out var debugInfo))
            {
                settings.DebugInfo = debugInfo;
            }

            settings.Plugins = new PluginsDto();

            if (bool.TryParse(configuration["plugins:enabled"], out var pluginsEnabled))
            {
                settings.Plugins.Enabled = pluginsEnabled;
            }

            if (bool.TryParse(configuration["plugins:upload"], out var pluginsUpload))
            {
                settings.Plugins.Upload = pluginsUpload;
            }

            if (bool.TryParse(configuration["plugins:delete"], out var pluginsDelete))
            {
                settings.Plugins.Delete = pluginsDelete;
            }

            var formGallerySettings = configuration.GetSection("files:oform").Get<OFormSettings>();
            settings.FormGallery = mapper.Map<FormGalleryDto>(formGallerySettings);

            settings.InvitationLimit = await userInvitationLimitHelper.GetLimit();
            settings.MaxImageUploadSize = setupInfo.MaxImageUploadSize;
        }
        else
        {
            if (!(await settingsManager.LoadAsync<WizardSettings>()).Completed)
            {
                settings.WizardToken = commonLinkUtility.GetToken(tenant.Id, "", ConfirmType.Wizard, userId: tenant.OwnerId);
            }

            settings.EnabledJoin =
                (tenant.TrustedDomainsType == TenantTrustedDomainsType.Custom &&
                tenant.TrustedDomains.Count > 0) ||
                tenant.TrustedDomainsType == TenantTrustedDomainsType.All;

            if (settings.EnabledJoin.GetValueOrDefault(false))
            {
                settings.TrustedDomainsType = tenant.TrustedDomainsType;
                settings.TrustedDomains = tenant.TrustedDomains;
            }

            settings.ThirdpartyEnable = setupInfo.ThirdPartyAuthEnabled && providerManager.IsNotEmpty;

            var country = (await geolocationHelper.GetIPGeolocationFromHttpContextAsync()).Key;

            settings.RecaptchaType = country == "CN" ? RecaptchaType.hCaptcha : RecaptchaType.Default;

            settings.RecaptchaPublicKey = settings.RecaptchaType is RecaptchaType.hCaptcha ? setupInfo.HcaptchaPublicKey : setupInfo.RecaptchaPublicKey;
        }

        if (!authContext.IsAuthenticated || (withpassword.HasValue && withpassword.Value))
        {
            settings.PasswordHash = passwordHasher;
        }

        return settings;
    }

    /// <summary>
    /// Saves the mail domain settings specified in the request to the portal.
    /// </summary>
    /// <short>
    /// Save the mail domain settings
    /// </short>
    /// <category>Common settings</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.MailDomainSettingsRequestsDto, ASC.Web.Api" name="inDto">Request parameters for mail domain settings</param>
    /// <returns type="System.Object, System">Message about the result of saving the mail domain settings</returns>
    /// <path>api/2.0/settings/maildomainsettings</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("maildomainsettings")]
    public async Task<object> SaveMailDomainSettingsAsync(MailDomainSettingsRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = await tenantManager.GetCurrentTenantAsync();

        if (inDto.Type == TenantTrustedDomainsType.Custom)
        {
            tenant.TrustedDomainsRaw = "";
            tenant.TrustedDomains.Clear();
            foreach (var d in inDto.Domains.Select(domain => (domain ?? "").Trim().ToLower()))
            {
                if (!(!string.IsNullOrEmpty(d) && EmailDomainRegex().IsMatch(d)))
                {
                    return Resource.ErrorNotCorrectTrustedDomain;
                }

                tenant.TrustedDomains.Add(d);
            }

            if (tenant.TrustedDomains.Count == 0)
            {
                inDto.Type = TenantTrustedDomainsType.None;
            }
        }

        tenant.TrustedDomainsType = inDto.Type;

        await settingsManager.SaveAsync(new StudioTrustedDomainSettings { InviteAsUsers = inDto.InviteUsersAsVisitors });

        await tenantManager.SaveTenantAsync(tenant);

        await messageService.SendAsync(MessageAction.TrustedMailDomainSettingsUpdated);

        return Resource.SuccessfullySaveSettingsMessage;
    }

    /// <summary>
    /// Returns the space usage quota for the portal.
    /// </summary>
    /// <short>
    /// Get the space usage
    /// </short>
    /// <category>Quota</category>
    /// <returns type="ASC.Web.Api.ApiModel.ResponseDto.QuotaUsageDto, ASC.Web.Api">Space usage and limits for upload</returns>
    /// <path>api/2.0/settings/quota</path>
    /// <httpMethod>GET</httpMethod>
    /// <visible>false</visible>
    [HttpGet("quota")]
    public async Task<QuotaUsageDto> GetQuotaUsed()
    {
        return await quotaUsageManager.Get();
    }

    /// <summary>
    /// Saves the user quota settings specified in the request to the current portal.
    /// </summary>
    /// <short>
    /// Save the user quota settings
    /// </short>
    /// <category>Quota</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.UserQuotaSettingsRequestsDto, ASC.Web.Api" name="inDto">Request parameters for the user quota settings</param>
    /// <returns type="System.Object, System">Message about the result of saving the user quota settings</returns>
    /// <path>api/2.0/settings/userquotasettings</path>
    /// <httpMethod>POST</httpMethod>
    /// <visible>false</visible>
    [HttpPost("userquotasettings")]
    public async Task<TenantUserQuotaSettings> SaveUserQuotaSettingsAsync(QuotaSettingsRequestsDto inDto)
    {
        await DemandStatisticPermissionAsync();

        if (!inDto.DefaultQuota.TryGetInt64(out var quota))
        {
            throw new Exception(Resource.QuotaGreaterPortalError);
        }

        var tenant = await tenantManager.GetCurrentTenantAsync();
        var tenantSpaceQuota = await tenantManager.GetTenantQuotaAsync(tenant.Id);
        var maxTotalSize = tenantSpaceQuota?.MaxTotalSize ?? -1;

        if (maxTotalSize < quota)
        {
            throw new Exception(Resource.QuotaGreaterPortalError);
        }

        if (coreBaseSettings.Standalone)
        {
            var tenantQuotaSetting = await settingsManager.LoadAsync<TenantQuotaSettings>();
            if (tenantQuotaSetting.EnableQuota)
            {
                if (tenantQuotaSetting.Quota < quota)
                {
                    throw new Exception(Resource.QuotaGreaterPortalError);
                }
            }
        }
        var quotaSettings = await settingsManager.LoadAsync<TenantUserQuotaSettings>();
        quotaSettings.EnableQuota = inDto.EnableQuota;
        quotaSettings.DefaultQuota = quota > 0 ? quota : 0;

        await settingsManager.SaveAsync(quotaSettings);
        
        if (inDto.EnableQuota)
        {
            await messageService.SendAsync(MessageAction.QuotaPerUserChanged, quota.ToString());
        }
        else
        {
            await messageService.SendAsync(MessageAction.QuotaPerUserDisabled);
        }
        
        return quotaSettings;
    }

    [HttpGet("userquotasettings")]
    public async Task<object> GetUserQuotaSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await settingsManager.LoadAsync<TenantUserQuotaSettings>();
    }

    /// <summary>
    /// Saves the room quota settings specified in the request to the current portal.
    /// </summary>
    /// <short>
    /// Save the room quota settings
    /// </short>
    /// <category>Quota</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.QuotaSettingsRequestsDto, ASC.Web.Api" name="inDto">Request parameters for the quota settings</param>
    /// <returns type="ASC.Core.Tenants.TenantRoomQuotaSettings, ASC.Core.Common">Tenant room quota settings</returns>
    /// <path>api/2.0/settings/roomquotasettings</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("roomquotasettings")]
    public async Task<TenantRoomQuotaSettings> SaveRoomQuotaSettingsAsync(QuotaSettingsRequestsDto inDto)
    {
        await DemandStatisticPermissionAsync();

        if (!inDto.DefaultQuota.TryGetInt64(out var quota))
        {
            throw new Exception(Resource.QuotaGreaterPortalError);
        }

        var tenant = await tenantManager.GetCurrentTenantAsync();
        var tenantSpaceQuota = await tenantManager.GetTenantQuotaAsync(tenant.Id);
        var maxTotalSize = tenantSpaceQuota?.MaxTotalSize ?? -1;

        if (maxTotalSize < quota)
        {
            throw new Exception(Resource.QuotaGreaterPortalError);
        }
        if (coreBaseSettings.Standalone)
        {
            var tenantQuotaSetting = await settingsManager.LoadAsync<TenantQuotaSettings>();
            if (tenantQuotaSetting.EnableQuota)
            {
                if (tenantQuotaSetting.Quota < quota)
                {
                    throw new Exception(Resource.QuotaGreaterPortalError);
                }
            }
        }

        var quotaSettings = await settingsManager.LoadAsync<TenantRoomQuotaSettings>();
        quotaSettings.EnableQuota = inDto.EnableQuota;
        quotaSettings.DefaultQuota = quota > 0 ? quota : 0;

        await settingsManager.SaveAsync(quotaSettings);
        
        if (inDto.EnableQuota)
        {
            await messageService.SendAsync(MessageAction.QuotaPerRoomChanged, quota.ToString());
        }
        else
        {
            await messageService.SendAsync(MessageAction.QuotaPerRoomDisabled);
        }

        return quotaSettings;
    }

    /// <summary>
    /// Saves the tenant quota settings specified in the request to the current portal.
    /// </summary>
    /// <short>
    /// Save the tenant quota settings
    /// </short>
    /// <category>Quota</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.TenantQuotaSettingsRequestsDto, ASC.Web.Api" name="inDto">Request parameters for the tenant quota settings</param>
    /// <returns type="ASC.Core.Tenants.TenantQuotaSettings, ASC.Core.Common">Tenant quota settings</returns>
    /// <path>api/2.0/settings/tenantquotasettings</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("tenantquotasettings")]
    public async Task<TenantQuotaSettings> SetTenantQuotaSettingsAsync(TenantQuotaSettingsRequestsDto inDto)
    {
        await DemandStatisticPermissionAsync();

        if (!await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID) || !coreBaseSettings.Standalone)
        {
            throw new NotSupportedException("Not available.");
        }
        var tenantQuotaSetting = await settingsManager.LoadAsync<TenantQuotaSettings>();

        if (inDto.Quota >= 0)
        {
            tenantQuotaSetting.EnableQuota = true;
            tenantQuotaSetting.Quota = inDto.Quota;
        }
        else
        {
            tenantQuotaSetting.EnableQuota = false;
            tenantQuotaSetting.Quota = -1;
        }
        await settingsManager.SaveAsync(tenantQuotaSetting, inDto.TenantId);

        var usedSize = (await tenantManager.FindTenantQuotaRowsAsync(inDto.TenantId))
           .Where(r => !string.IsNullOrEmpty(r.Tag) && new Guid(r.Tag) != Guid.Empty)
           .Sum(r => r.Counter);
        var admins = (await userManager.GetUsersByGroupAsync(ASC.Core.Users.Constants.GroupAdmin.ID)).Select(u => u.Id).ToList();

        _ = quotaSocketManager.ChangeCustomQuotaUsedValueAsync(inDto.TenantId, customQuota.GetFeature<TenantCustomQuotaFeature>().Name, tenantQuotaSetting.EnableQuota, usedSize, tenantQuotaSetting.Quota, admins);
        
        if (tenantQuotaSetting.EnableQuota)
        {
            await messageService.SendAsync(MessageAction.QuotaPerPortalChanged, tenantQuotaSetting.Quota.ToString());
        }
        else
        {
            await messageService.SendAsync(MessageAction.QuotaPerPortalDisabled);
        }
        
        return tenantQuotaSetting;
    }

    /// <summary>
    /// Returns a list of all the available portal languages in the format of a two-letter or four-letter language code (e.g. "de", "en-US", etc.).
    /// </summary>
    /// <short>Get supported languages</short>
    /// <category>Common settings</category>
    /// <returns type="System.Object, System">List of all the available portal languages</returns>
    /// <path>api/2.0/settings/cultures</path>
    /// <httpMethod>GET</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <collection>list</collection>
    [AllowAnonymous]
    [AllowNotPayment]
    [HttpGet("cultures")]
    public IEnumerable<string> GetSupportedCultures()
    {
        return coreBaseSettings.EnabledCultures.Select(r => r.Name).ToList();
    }

    /// <summary>
    /// Returns a list of all the available portal time zones.
    /// </summary>
    /// <short>Get time zones</short>
    /// <category>Common settings</category>
    /// <returns type="ASC.Web.Api.ApiModel.RequestsDto.TimezonesRequestsDto, ASC.Web.Api">List of all the available time zones with their IDs and display names</returns>
    /// <path>api/2.0/settings/timezones</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [Authorize(AuthenticationSchemes = "confirm", Roles = "Wizard,Administrators")]
    [HttpGet("timezones")]
    [AllowNotPayment]
    public async Task<List<TimezonesRequestsDto>> GetTimeZonesAsyncAsync()
    {
        await ApiContext.AuthByClaimAsync();
        var timeZones = TimeZoneInfo.GetSystemTimeZones().ToList();

        if (timeZones.All(tz => tz.Id != "UTC"))
        {
            timeZones.Add(TimeZoneInfo.Utc);
        }

        var listOfTimezones = new List<TimezonesRequestsDto>();

        foreach (var tz in timeZones.OrderBy(z => z.BaseUtcOffset))
        {
            listOfTimezones.Add(new TimezonesRequestsDto
            {
                Id = timeZoneConverter.GetIanaTimeZoneId(tz),
                DisplayName = timeZoneConverter.GetTimeZoneDisplayName(tz)
            });
        }

        return listOfTimezones;
    }

    /// <summary>
    /// Returns the portal hostname.
    /// </summary>
    /// <short>Get hostname</short>
    /// <category>Common settings</category>
    /// <returns type="System.Object, System">Portal hostname</returns>
    /// <path>api/2.0/settings/machine</path>
    /// <httpMethod>GET</httpMethod>
    [Authorize(AuthenticationSchemes = "confirm", Roles = "Wizard")]
    [HttpGet("machine")]
    [AllowNotPayment]
    public object GetMachineName()
    {
        return _httpContextAccessor.HttpContext.Request.Host.Value;
    }

    /// <summary>
    /// Saves the DNS settings specified in the request to the current portal.
    /// </summary>
    /// <short>Save the DNS settings</short>
    /// <category>Common settings</category>
    /// <param type="ASC.Web.Api.Models.DnsSettingsRequestsDto, ASC.Web.Api" name="inDto">DNS settings request parameters</param>
    /// <returns type="System.Object, System">Message about changing DNS</returns>
    /// <path>api/2.0/settings/dns</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("dns")]
    public async Task<object> SaveDnsSettingsAsync(DnsSettingsRequestsDto inDto)
    {
        return await dnsSettings.SaveDnsSettingsAsync(inDto.DnsName, inDto.Enable);
    }

    /// <summary>
    /// Starts the process of quota recalculation.
    /// </summary>
    /// <short>
    /// Recalculate quota 
    /// </short>
    /// <category>Quota</category>
    /// <path>api/2.0/settings/recalculatequota</path>
    /// <httpMethod>GET</httpMethod>
    /// <returns></returns>
    /// <visible>false</visible>
    [HttpGet("recalculatequota")]
    public async Task RecalculateQuotaAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await usersQuotaSyncOperation.RecalculateQuota(await tenantManager.GetCurrentTenantAsync());
    }

    /// <summary>
    /// Checks the process of quota recalculation.
    /// </summary>
    /// <short>
    /// Check quota recalculation
    /// </short>
    /// <category>Quota</category>
    /// <returns type="System.Boolean, System">Boolean value: true - quota recalculation process is enabled, false - quota recalculation process is disabled</returns>
    /// <path>api/2.0/settings/checkrecalculatequota</path>
    /// <httpMethod>GET</httpMethod>
    /// <visible>false</visible>
    [HttpGet("checkrecalculatequota")]
    public async Task<bool> CheckRecalculateQuotaAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var result = await usersQuotaSyncOperation.CheckRecalculateQuota(await tenantManager.GetCurrentTenantAsync());
        return !result.IsCompleted;
    }

    /// <summary>
    /// Returns the portal logo image URL.
    /// </summary>
    /// <short>
    /// Get a portal logo
    /// </short>
    /// <category>Common settings</category>
    /// <returns type="System.Object, System">Portal logo image URL</returns>
    /// <path>api/2.0/settings/logo</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("logo")]
    public async Task<object> GetLogoAsync()
    {
        return await tenantInfoSettingsHelper.GetAbsoluteCompanyLogoPathAsync(await settingsManager.LoadAsync<TenantInfoSettings>());
    }

    /// <summary>
    /// Completes the Wizard settings.
    /// </summary>
    /// <short>Complete the Wizard settings</short>
    /// <category>Common settings</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.WizardRequestsDto, ASC.Web.Api" name="inDto">Wizard settings request parameters</param>
    /// <returns type="ASC.Web.Core.Utility.Settings.WizardSettings, ASC.Web.Core">Wizard settings</returns>
    /// <path>api/2.0/settings/wizard/complete</path>
    /// <httpMethod>PUT</httpMethod>
    [AllowNotPayment]
    [HttpPut("wizard/complete")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "Wizard")]
    public async Task<WizardSettings> CompleteWizardAsync(WizardRequestsDto inDto)
    {
        await ApiContext.AuthByClaimAsync();

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await firstTimeTenantSettings.SaveDataAsync(inDto);
    }

    /// <summary>
    /// Closes the welcome pop-up notification.
    /// </summary>
    /// <short>Close the welcome pop-up notification</short>
    /// <category>Common settings</category>
    /// <returns></returns>
    /// <path>api/2.0/settings/welcome/close</path>
    /// <httpMethod>PUT</httpMethod>
    ///<visible>false</visible>
    [HttpPut("welcome/close")]
    public async Task CloseWelcomePopupAsync()
    {
        var currentUser = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);

        var collaboratorPopupSettings = await settingsManager.LoadForCurrentUserAsync<CollaboratorSettings>();

        if (!(await userManager.IsUserAsync(currentUser) && collaboratorPopupSettings.FirstVisit && !await userManager.IsOutsiderAsync(currentUser)))
        {
            throw new NotSupportedException("Not available.");
        }

        collaboratorPopupSettings.FirstVisit = false;
        await settingsManager.SaveForCurrentUserAsync(collaboratorPopupSettings);
    }

    /// <summary>
    /// Returns the portal color theme.
    /// </summary>
    /// <short>Get a color theme</short>
    /// <category>Common settings</category>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.CustomColorThemesSettingsDto, ASC.Web.Api">Settings of the portal themes</returns>
    /// <path>api/2.0/settings/colortheme</path>
    /// <httpMethod>GET</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowAnonymous, AllowNotPayment, AllowSuspended]
    [HttpGet("colortheme")]
    public async Task<CustomColorThemesSettingsDto> GetColorThemeAsync()
    {
        return new CustomColorThemesSettingsDto(await settingsManager.LoadAsync<CustomColorThemesSettings>(), customColorThemesSettingsHelper.Limit);
    }

    /// <summary>
    /// Saves the portal color theme specified in the request.
    /// </summary>
    /// <short>Save a color theme</short>
    /// <category>Common settings</category>
    /// <param type="ASC.Web.Api.ApiModels.RequestsDto.CustomColorThemesSettingsRequestsDto, ASC.Web.Api" name="inDto">Portal theme settings</param>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.CustomColorThemesSettingsDto, ASC.Web.Api">Portal theme settings</returns>
    /// <path>api/2.0/settings/colortheme</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("colortheme")]
    public async Task<CustomColorThemesSettingsDto> SaveColorThemeAsync(CustomColorThemesSettingsRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var settings = await settingsManager.LoadAsync<CustomColorThemesSettings>();

        if (inDto.Theme != null)
        {
            await using (await distributedLockProvider.TryAcquireFairLockAsync("save_color_theme"))
            {
                var theme = inDto.Theme;

                if (CustomColorThemesSettingsItem.Default.Exists(r => r.Id == theme.Id))
                {
                    theme.Id = 0;
                }

                var settingItem = settings.Themes.SingleOrDefault(r => r.Id == theme.Id);
                if (settingItem != null)
                {
                    if (theme.Main != null)
                    {
                        settingItem.Main = new CustomColorThemesSettingsColorItem
                        {
                            Accent = theme.Main.Accent,
                            Buttons = theme.Main.Buttons
                        };
                    }
                    if (theme.Text != null)
                    {
                        settingItem.Text = new CustomColorThemesSettingsColorItem
                        {
                            Accent = theme.Text.Accent,
                            Buttons = theme.Text.Buttons
                        };
                    }
                }
                else
                {
                    if (customColorThemesSettingsHelper.Limit == 0 || settings.Themes.Count < customColorThemesSettingsHelper.Limit)
                    {
                        if (theme.Id == 0)
                        {
                            theme.Id = settings.Themes.Max(r => r.Id) + 1;
                        }

                        theme.Name = "";
                        settings.Themes = settings.Themes.Append(theme).ToList();
                    }
                }

                await settingsManager.SaveAsync(settings);
            }
        }

        if (inDto.Selected.HasValue && settings.Themes.Exists(r => r.Id == inDto.Selected.Value))
        {
            settings.Selected = inDto.Selected.Value;
            await settingsManager.SaveAsync(settings);
            await messageService.SendAsync(MessageAction.ColorThemeChanged);
        }

        return new CustomColorThemesSettingsDto(settings, customColorThemesSettingsHelper.Limit);
    }

    /// <summary>
    /// Deletes the portal color theme with the ID specified in the request.
    /// </summary>
    /// <short>Delete a color theme</short>
    /// <category>Common settings</category>
    /// <param ype="System.Int32, System" name="id">Portal theme ID</param>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.CustomColorThemesSettingsDto, ASC.Web.Api">Portal theme settings: custom color theme settings, selected or not, limit</returns>
    /// <path>api/2.0/settings/colortheme</path>
    /// <httpMethod>DELETE</httpMethod>
    [HttpDelete("colortheme")]
    public async Task<CustomColorThemesSettingsDto> DeleteColorThemeAsync(int id)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = await settingsManager.LoadAsync<CustomColorThemesSettings>();

        if (CustomColorThemesSettingsItem.Default.Any(r => r.Id == id))
        {
            return new CustomColorThemesSettingsDto(settings, customColorThemesSettingsHelper.Limit);
        }

        settings.Themes = settings.Themes.Where(r => r.Id != id).ToList();

        if (settings.Selected == id)
        {
            settings.Selected = settings.Themes.Min(r => r.Id);
            await messageService.SendAsync(MessageAction.ColorThemeChanged);
        }

        await settingsManager.SaveAsync(settings);

        return new CustomColorThemesSettingsDto(settings, customColorThemesSettingsHelper.Limit);
    }

    /// <summary>
    /// Closes the admin helper notification.
    /// </summary>
    /// <short>Close the admin helper notification</short>
    /// <category>Common settings</category>
    /// <returns></returns>
    /// <path>api/2.0/settings/closeadminhelper</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("closeadminhelper")]
    public async Task CloseAdminHelperAsync()
    {
        if (!await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID) || coreBaseSettings.CustomMode || !coreBaseSettings.Standalone)
        {
            throw new NotSupportedException("Not available.");
        }

        var adminHelperSettings = await settingsManager.LoadForCurrentUserAsync<AdminHelperSettings>();
        adminHelperSettings.Viewed = true;
        await settingsManager.SaveForCurrentUserAsync(adminHelperSettings);
    }

    /// <summary>
    /// Sets the portal time zone and language specified in the request.
    /// </summary>
    /// <short>Set time zone and language</short>
    /// <category>Common settings</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.SettingsRequestsDto, ASC.Web.Api" name="inDto">Settings request parameters</param>
    /// <returns type="System.Object, System">Message about saving settings successfully</returns>
    /// <path>api/2.0/settings/timeandlanguage</path>
    /// <httpMethod>PUT</httpMethod>
    ///<visible>false</visible>
    [HttpPut("timeandlanguage")]
    public async Task<object> TimaAndLanguageAsync(SettingsRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var culture = CultureInfo.GetCultureInfo(inDto.Lng);
        var tenant = await tenantManager.GetCurrentTenantAsync();

        var changelng = false;
        if (coreBaseSettings.EnabledCultures.Find(c => string.Equals(c.Name, culture.Name, StringComparison.InvariantCultureIgnoreCase)) != null && !string.Equals(tenant.Language, culture.Name, StringComparison.InvariantCultureIgnoreCase))
        {
            tenant.Language = culture.Name;
            changelng = true;
        }

        var oldTimeZone = tenant.TimeZone;
        var timeZones = TimeZoneInfo.GetSystemTimeZones().ToList();
        if (timeZones.All(tz => tz.Id != "UTC"))
        {
            timeZones.Add(TimeZoneInfo.Utc);
        }
        tenant.TimeZone = timeZones.FirstOrDefault(tz => tz.Id == inDto.TimeZoneID)?.Id ?? TimeZoneInfo.Utc.Id;

        await tenantManager.SaveTenantAsync(tenant);

        if (!tenant.TimeZone.Equals(oldTimeZone) || changelng)
        {
            if (!tenant.TimeZone.Equals(oldTimeZone))
            {
                await messageService.SendAsync(MessageAction.TimeZoneSettingsUpdated);
            }
            if (changelng)
            {
                await messageService.SendAsync(MessageAction.LanguageSettingsUpdated);
            }
        }

        return Resource.SuccessfullySaveSettingsMessage;
    }

    /// <summary>
    /// Sets the default product page.
    /// </summary>
    /// <short>Set the default product page</short>
    /// <category>Common settings</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.SettingsRequestsDto, ASC.Web.Api" name="inDto">Settings request parameters</param>
    /// <returns type="System.Object, System">Message about saving settings successfully</returns>
    /// <path>api/2.0/settings/defaultpage</path>
    /// <httpMethod>PUT</httpMethod>
    ///<visible>false</visible>
    [HttpPut("defaultpage")]
    public async Task<object> SaveDefaultPageSettingAsync(SettingsRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await settingsManager.SaveAsync(new StudioDefaultPageSettings { DefaultProductID = inDto.DefaultProductID });

        await messageService.SendAsync(MessageAction.DefaultStartPageSettingsUpdated);

        return Resource.SuccessfullySaveSettingsMessage;
    }

    /// <summary>
    /// Updates the email activation settings.
    /// </summary>
    /// <short>Update the email activation settings</short>
    /// <category>Common settings</category>
    /// <param type="ASC.Web.Studio.Core.EmailActivationSettings, ASC.Web.Studio.Core" name="inDto">Email activation settings</param>
    /// <returns type="ASC.Web.Studio.Core.EmailActivationSettings, ASC.Web.Studio.Core">Updated email activation settings</returns>
    /// <path>api/2.0/settings/emailactivation</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("emailactivation")]
    public async Task<EmailActivationSettings> UpdateEmailActivationSettingsAsync(EmailActivationSettings inDto)
    {
        await settingsManager.SaveForCurrentUserAsync(inDto);
        return inDto;
    }

    /// <summary>
    /// Returns the space usage statistics of the module with the ID specified in the request.
    /// </summary>
    /// <category>Statistics</category>
    /// <short>Get the space usage statistics</short>
    /// <param ype="System.Guid, System" method="url" name="id">Module ID</param>
    /// <returns type="ASC.Web.Api.ApiModel.ResponseDto.UsageSpaceStatItemDto, ASC.Web.Api">Module space usage statistics</returns>
    /// <path>api/2.0/settings/statistics/spaceusage/{id}</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("statistics/spaceusage/{id:guid}")]
    public async Task<List<UsageSpaceStatItemDto>> GetSpaceUsageStatistics(Guid id)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var webitem = webItemManagerSecurity.GetItems(WebZoneType.All, ItemAvailableState.All)
                                   .FirstOrDefault(item =>
                                                   item != null &&
                                                   item.ID == id &&
                                                   item.Context is { SpaceUsageStatManager: not null });

        if (webitem == null)
        {
            return new List<UsageSpaceStatItemDto>();
        }

        var statData = await webitem.Context.SpaceUsageStatManager.GetStatDataAsync();

        return statData.ConvertAll(it => new UsageSpaceStatItemDto
        {
            Name = it.Name.HtmlEncode(),
            Icon = it.ImgUrl,
            Disabled = it.Disabled,
            Size = FileSizeComment.FilesSizeToString(it.SpaceUsage),
            Url = it.Url
        });
    }

    /// <summary>
    /// Returns the user visit statistics for the period specified in the request.
    /// </summary>
    /// <category>Statistics</category>
    /// <short>Get the visit statistics</short>
    /// <param type="ASC.Api.Core.ApiDateTime, ASC.Api.Core" name="fromDate">Start period date</param>
    /// <param type="ASC.Api.Core.ApiDateTime, ASC.Api.Core" name="toDate">End period date</param>
    /// <returns type="ASC.Web.Api.ApiModel.ResponseDto.ChartPointDto, ASC.Web.Api">List of point charts</returns>
    /// <path>api/2.0/settings/statistics/visit</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("statistics/visit")]
    public async Task<List<ChartPointDto>> GetVisitStatisticsAsync(ApiDateTime fromDate, ApiDateTime toDate)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var from = tenantUtil.DateTimeFromUtc(fromDate);
        var to = tenantUtil.DateTimeFromUtc(toDate);

        var points = new List<ChartPointDto>();

        if (from.CompareTo(to) >= 0)
        {
            return points;
        }

        for (var d = new DateTime(from.Ticks); d.Date.CompareTo(to.Date) <= 0; d = d.AddDays(1))
        {
            points.Add(new ChartPointDto
            {
                DisplayDate = d.Date.ToShortDateString(),
                Date = d.Date,
                Hosts = 0,
                Hits = 0
            });
        }

        var tenant = await tenantManager.GetCurrentTenantAsync();
        var hits = await statisticManager.GetHitsByPeriodAsync(tenant.Id, from, to);
        var hosts = await statisticManager.GetHostsByPeriodAsync(tenant.Id, from, to);

        if (hits.Count == 0 || hosts.Count == 0)
        {
            return points;
        }

        hits.Sort((x, y) => x.VisitDate.CompareTo(y.VisitDate));
        hosts.Sort((x, y) => x.VisitDate.CompareTo(y.VisitDate));

        for (int i = 0, n = points.Count, hitsNum = 0, hostsNum = 0; i < n; i++)
        {
            while (hitsNum < hits.Count && points[i].Date.CompareTo(hits[hitsNum].VisitDate.Date) == 0)
            {
                points[i].Hits += hits[hitsNum].VisitCount;
                hitsNum++;
            }
            while (hostsNum < hosts.Count && points[i].Date.CompareTo(hosts[hostsNum].VisitDate.Date) == 0)
            {
                points[i].Hosts++;
                hostsNum++;
            }
        }

        return points;
    }

    /// <summary>
    /// Returns the socket settings.
    /// </summary>
    /// <category>Common settings</category>
    /// <short>Get the socket settings</short>
    /// <path>api/2.0/settings/socket</path>
    /// <httpMethod>GET</httpMethod>
    /// <returns type="System.Object, System">Socket settings: hub URL</returns>
    [HttpGet("socket")]
    public object GetSocketSettings()
    {
        var hubUrl = configuration["web:hub"] ?? string.Empty;
        if (hubUrl.Length != 0)
        {
            if (!hubUrl.EndsWith('/'))
            {
                hubUrl += "/";
            }
        }

        return new { Url = hubUrl };
    }

    /*/// <summary>
    /// Returns the tenant Control Panel settings.
    /// </summary>
    /// <category>Common settings</category>
    /// <short>Get the tenant Control Panel settings</short>
    /// <returns type="ASC.Core.Tenants.TenantControlPanelSettings, ASC.Core.Common">Tenant Control Panel settings</returns>
    /// <path>api/2.0/settings/controlpanel</path>
    /// <httpMethod>GET</httpMethod>
    ///<visible>false</visible>
    [HttpGet("controlpanel")]
    public TenantControlPanelSettings GetTenantControlPanelSettings()
    {
        return _settingsManager.Load<TenantControlPanelSettings>();
    }*/

    /// <summary>
    /// Returns the authorization services.
    /// </summary>
    /// <category>Authorization</category>
    /// <short>Get the authorization services</short>
    /// <path>api/2.0/settings/authservice</path>
    /// <httpMethod>GET</httpMethod>
    /// <returns type="ASC.Web.Api.ApiModel.RequestsDto.AuthServiceRequestsDto, ASC.Web.Api">Authorization services</returns>
    /// <collection>list</collection>
    [HttpGet("authservice")]
    public async Task<IEnumerable<AuthServiceRequestsDto>> GetAuthServices()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await consumerFactory.GetAll<Consumer>()
            .Where(consumer => consumer.ManagedKeys.Any())
            .OrderBy(services => services.Order)
            .ToAsyncEnumerable()
            .SelectAwait(async r => await AuthServiceRequestsDto.From(r))
            .ToListAsync();
    }

    /// <summary>
    /// Saves the authorization keys.
    /// </summary>
    /// <category>Authorization</category>
    /// <short>Save the authorization keys</short>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.AuthServiceRequestsDto, ASC.Web.Api" name="inDto">Request parameters for authorization service</param>
    /// <path>api/2.0/settings/authservice</path>
    /// <httpMethod>POST</httpMethod>
    /// <returns type="System.Boolean, System">Boolean value: true if the authorization keys are changed</returns>
    [HttpPost("authservice")]
    public async Task<bool> SaveAuthKeys(AuthServiceRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var saveAvailable = coreBaseSettings.Standalone || (await tenantManager.GetTenantQuotaAsync(await tenantManager.GetCurrentTenantIdAsync())).ThirdParty;
        if (!SetupInfo.IsVisibleSettings(nameof(ManagementType.ThirdPartyAuthorization))
            || !saveAvailable)
        {
            throw new BillingException(Resource.ErrorNotAllowedOption, "ThirdPartyAuthorization");
        }

        var changed = false;
        var consumer = consumerFactory.GetByKey<Consumer>(inDto.Name);

        var validateKeyProvider = consumer as IValidateKeysProvider;

        if (inDto.Props.All(r => string.IsNullOrEmpty(r.Value)))
        {
            await consumer.ClearAsync();
            changed = true;
        }
        else
        {
            foreach (var authKey in inDto.Props)
            {
                if (await consumer.GetAsync(authKey.Name) != authKey.Value)
                {
                    await consumer.SetAsync(authKey.Name, authKey.Value);
                    changed = true;
                }
            }
        }

        //TODO: Consumer implementation required (Bug 50606)
        var allPropsIsEmpty = consumer.GetType() == typeof(SmscProvider)
            ? consumer.ManagedKeys.All(key => string.IsNullOrEmpty(consumer[key]))
            : consumer.All(r => string.IsNullOrEmpty(r.Value));

        if (validateKeyProvider != null && !await validateKeyProvider.ValidateKeysAsync() && !allPropsIsEmpty)
        {
            await consumer.ClearAsync();
            throw new ArgumentException(Resource.ErrorBadKeys);
        }

        if (changed)
        {
            await messageService.SendAsync(MessageAction.AuthorizationKeysSetting);
        }

        return changed;
    }

    /// <summary>
    /// Returns the portal payment settings.
    /// </summary>
    /// <category>Common settings</category>
    /// <short>Get the payment settings</short>
    /// <path>api/2.0/settings/payment</path>
    /// <httpMethod>GET</httpMethod>
    /// <returns type="System.Object, System">Payment settings: sales email, feedback and support URL, link to pay for a portal, Standalone or not, current license, maximum quota quantity</returns>
    [AllowNotPayment]
    [HttpGet("payment")]
    public async Task<object> PaymentSettingsAsync()
    {
        var settings = await settingsManager.LoadForDefaultTenantAsync<AdditionalWhiteLabelSettings>();
        var currentQuota = await tenantManager.GetCurrentTenantQuotaAsync();
        var currentTariff = await tenantExtra.GetCurrentTariffAsync();

        if (!int.TryParse(configuration["core:payment:max-quantity"], out var maxQuotaQuantity))
        {
            maxQuotaQuantity = 999;
        }

        return
            new
            {
                settings.SalesEmail,
                settings.FeedbackAndSupportUrl,
                settings.BuyUrl,
                coreBaseSettings.Standalone,
                currentLicense = new
                {
                    currentQuota.Trial,
                    currentTariff.DueDate.Date
                },
                max = maxQuotaQuantity
            };
    }

    /// <summary>
    /// Returns a link that will connect TelegramBot to your account.
    /// </summary>
    /// <category>Telegram</category>
    /// <short>Get the Telegram link</short>
    /// <path>api/2.0/settings/telegramlink</path>
    /// <httpMethod>GET</httpMethod>
    /// <returns type="System.Object, System">Telegram link</returns>
    /// <visible>false</visible>
    [HttpGet("telegramlink")]
    public async Task<object> TelegramLink()
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();
        var currentLink = telegramHelper.CurrentRegistrationLink(authContext.CurrentAccount.ID, tenant.Id);

        if (string.IsNullOrEmpty(currentLink))
        {
            var url = await telegramHelper.RegisterUserAsync(authContext.CurrentAccount.ID, tenant.Id);
            return url;
        }

        return currentLink;
    }

    /// <summary>
    /// Checks if the user has connected to TelegramBot.
    /// </summary>
    /// <category>Telegram</category>
    /// <short>Check the Telegram connection</short>
    /// <path>api/2.0/settings/telegramisconnected</path>
    /// <httpMethod>GET</httpMethod>
    /// <returns type="System.Object, System">Operation result: 0 - not connected, 1 - connected, 2 - awaiting confirmation</returns>
    /// <visible>false</visible>
    [HttpGet("telegramisconnected")]
    public async Task<object> TelegramIsConnectedAsync()
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();
        return (int)await telegramHelper.UserIsConnectedAsync(authContext.CurrentAccount.ID, tenant.Id);
    }

    /// <summary>
    /// Unlinks TelegramBot from your account.
    /// </summary>
    /// <category>Telegram</category>
    /// <short>Unlink Telegram</short>
    /// <path>api/2.0/settings/telegramdisconnect</path>
    /// <httpMethod>DELETE</httpMethod>
    /// <returns></returns>
    /// <visible>false</visible>
    [HttpDelete("telegramdisconnect")]
    public async Task TelegramDisconnectAsync()
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();
        await telegramHelper.DisconnectAsync(authContext.CurrentAccount.ID, tenant.Id);
    }

    private async Task DemandStatisticPermissionAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!coreBaseSettings.Standalone
            && !(await tenantManager.GetCurrentTenantQuotaAsync()).Statistic)
        {
            throw new BillingException(Resource.ErrorNotAllowedOption, "Statistic");
        }
    }
}