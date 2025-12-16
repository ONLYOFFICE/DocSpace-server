// (c) Copyright Ascensio System SIA 2009-2025
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

using ASC.Files.Core;

namespace ASC.Web.Api.Controllers.Settings;

public partial class SettingsController(
    MessageService messageService,
    SecurityContext securityContext,
    UserManager userManager,
    TenantManager tenantManager,
    TenantExtra tenantExtra,
    AuthContext authContext,
    PermissionContext permissionContext,
    SettingsManager settingsManager,
    WebItemManager webItemManager,
    WebItemManagerSecurity webItemManagerSecurity,
    TenantInfoSettingsHelper tenantInfoSettingsHelper,
    CoreSettings coreSettings,
    CoreBaseSettings coreBaseSettings,
    CommonLinkUtility commonLinkUtility,
    IConfiguration configuration,
    SetupInfo setupInfo,
    ExternalResourceSettings externalResourceSettings,
    ExternalResourceSettingsHelper externalResourceSettingsHelper,
    ConsumerFactory consumerFactory,
    TimeZoneConverter timeZoneConverter,
    CustomNamingPeople customNamingPeople,
    IFusionCache fusionCache,
    ProviderManager providerManager,
    FirstTimeTenantSettings firstTimeTenantSettings,
    PasswordHasher passwordHasher,
    DnsSettings dnsSettings,
    CustomColorThemesSettingsHelper customColorThemesSettingsHelper,
    UserInvitationLimitHelper userInvitationLimitHelper,
    TenantDomainValidator tenantDomainValidator,
    TenantLogoManager tenantLogoManager,
    ExternalShare externalShare,
    UserFormatter userFormatter,
    IDistributedLockProvider distributedLockProvider,
    UsersQuotaSyncOperation usersQuotaSyncOperation,
    CustomQuota customQuota,
    UserSocketManager userSocketManager,
    QuotaSocketManager quotaSocketManager)
    : BaseSettingsController(fusionCache, webItemManager)
{
    [GeneratedRegex("^[a-z0-9]([a-z0-9-.]){1,253}[a-z0-9]$")]
    private static partial Regex EmailDomainRegex();

    /// <summary>
    /// Returns a list of all the available portal settings with the current values for each parameter.
    /// </summary>
    /// <short>
    /// Get the portal settings
    /// </short>
    /// <path>api/2.0/settings</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Settings", typeof(SettingsDto))]
    [HttpGet("")]
    [AllowNotPayment, AllowSuspended, AllowAnonymous]
    public async Task<SettingsDto> GetPortalSettings(PortalSettingsRequestDto inDto)
    {
        var studioAdminMessageSettings = await settingsManager.LoadAsync<StudioAdminMessageSettings>();
        var tenantCookieSettings = await settingsManager.LoadAsync<TenantCookieSettings>();
        var additionalWhiteLabelSettings = await settingsManager.LoadForDefaultTenantAsync<AdditionalWhiteLabelSettings>();
        var companyWhiteLabelSettings = await settingsManager.LoadForDefaultTenantAsync<CompanyWhiteLabelSettings>();

        var tenant = tenantManager.GetCurrentTenant();
        var quota = await tenantManager.GetCurrentTenantQuotaAsync();

        var settings = new SettingsDto
        {
            Culture = coreBaseSettings.GetRightCultureName(tenant.GetCulture()),
            GreetingSettings = tenant.Name == "" ? Resource.PortalName : tenant.Name,
            DocSpace = true,
            Standalone = coreBaseSettings.Standalone,
            IsAmi = coreBaseSettings.Standalone && !string.IsNullOrEmpty(setupInfo.AmiMetaUrl),
            BaseDomain = coreBaseSettings.Standalone ? await coreSettings.GetSettingAsync("BaseDomain") ?? coreBaseSettings.Basedomain : coreBaseSettings.Basedomain,
            Version = configuration["version:number"] ?? "",
            TenantStatus = tenant.Status,
            TenantAlias = tenant.Alias,
            EnableAdmMess = studioAdminMessageSettings.Enable || await tenantExtra.IsNotPaidAsync(),
            CookieSettingsEnabled = tenantCookieSettings.Enabled,
            UserNameRegex = userFormatter.UserNameRegex.ToString(),
            DisplayAbout = (!coreBaseSettings.Standalone && !coreBaseSettings.CustomMode) || !quota.Branding || !companyWhiteLabelSettings.HideAbout,
            DeepLink = new DeepLinkDto
            {
                AndroidPackageName = configuration["deeplink:androidpackagename"] ?? "",
                Url = configuration["deeplink:url"] ?? "",
                IosPackageId = configuration["deeplink:iospackageid"] ?? ""
            },
            LogoText = await tenantLogoManager.GetLogoTextAsync(),
            ExternalResources = externalResourceSettings.GetCultureSpecificExternalResources(whiteLabelSettings: additionalWhiteLabelSettings)
        };

        if (!authContext.IsAuthenticated && await externalShare.GetLinkIdAsync() != Guid.Empty)
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
            settings.SocketUrl = configuration["web:hub:url"] ?? "";
            settings.LimitedAccessSpace = (await settingsManager.LoadAsync<TenantAccessSpaceSettings>()).LimitedAccessSpace;
            settings.LimitedAccessDevToolsForUsers = (await settingsManager.LoadAsync<TenantDevToolsAccessSettings>()).LimitedAccessForUsers;
            settings.DisplayBanners = coreBaseSettings.Standalone ? !(await settingsManager.LoadAsync<TenantBannerSettings>()).Hidden : true;

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
            settings.FormGallery = formGallerySettings.Map();

            settings.InvitationLimit = await userInvitationLimitHelper.GetLimit();
            settings.MaxImageUploadSize = setupInfo.MaxImageUploadSize;
            settings.DefaultFolderType = (await settingsManager.LoadForCurrentUserAsync<StudioDefaultPageSettings>()).DefaultFolderType;
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

            settings.RecaptchaType = !string.IsNullOrEmpty(setupInfo.HcaptchaPublicKey) ? RecaptchaType.hCaptcha : RecaptchaType.Default;

            settings.RecaptchaPublicKey = settings.RecaptchaType is RecaptchaType.hCaptcha ? setupInfo.HcaptchaPublicKey : setupInfo.RecaptchaPublicKey;
        }

        if (!authContext.IsAuthenticated || (inDto.WithPassword.HasValue && inDto.WithPassword.Value))
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
    /// <path>api/2.0/settings/maildomainsettings</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Message about the result of saving the mail domain settings", typeof(string))]
    [HttpPost("maildomainsettings")]
    public async Task<string> SaveMailDomainSettings(MailDomainSettingsRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();

        if (inDto.Type == TenantTrustedDomainsType.Custom)
        {
            tenant.TrustedDomainsRaw = "";
            tenant.TrustedDomains.Clear();
            foreach (var d in inDto.Domains.Select(domain => (domain ?? "").Trim().ToLower()))
            {
                if (!(!string.IsNullOrEmpty(d) && EmailDomainRegex().IsMatch(d)))
                {
                    throw new ArgumentException(Resource.ErrorNotCorrectTrustedDomain);
                }

                tenant.TrustedDomains.Add(d);
            }

            if (tenant.TrustedDomains.Count == 0)
            {
                throw new ArgumentException(Resource.ErrorTrustedMailDomain);
            }
        }

        tenant.TrustedDomainsType = inDto.Type;

        await settingsManager.SaveAsync(new StudioTrustedDomainSettings { InviteAsUsers = inDto.InviteUsersAsVisitors });

        await tenantManager.SaveTenantAsync(tenant);

        messageService.Send(MessageAction.TrustedMailDomainSettingsUpdated);

        return Resource.SuccessfullySaveSettingsMessage;
    }


    /// <summary>
    /// Saves the user quota settings specified in the request to the current portal.
    /// </summary>
    /// <short>
    /// Save the user quota settings
    /// </short>
    /// <path>api/2.0/settings/userquotasettings</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Settings / Quota")]
    [SwaggerResponse(200, "Message about the result of saving the user quota settings", typeof(TenantUserQuotaSettings))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpPost("userquotasettings")]
    public async Task<TenantUserQuotaSettings> SaveUserQuotaSettings(QuotaSettingsRequestsDto inDto)
    {
        await DemandStatisticPermissionAsync();

        if (!inDto.DefaultQuota.TryGetInt64(out var quota))
        {
            throw new Exception(Resource.UserQuotaGreaterPortalError);
        }

        var tenant = tenantManager.GetCurrentTenant();
        var tenantSpaceQuota = await tenantManager.GetTenantQuotaAsync(tenant.Id);
        var maxTotalSize = tenantSpaceQuota?.MaxTotalSize ?? -1;

        if (maxTotalSize < quota)
        {
            throw new Exception(Resource.UserQuotaGreaterPortalError);
        }

        if (coreBaseSettings.Standalone)
        {
            var tenantQuotaSetting = await settingsManager.LoadAsync<TenantQuotaSettings>();
            if (tenantQuotaSetting.EnableQuota)
            {
                if (tenantQuotaSetting.Quota < quota)
                {
                    throw new Exception(Resource.UserQuotaGreaterPortalError);
                }
            }
        }
        var quotaSettings = await settingsManager.LoadAsync<TenantUserQuotaSettings>();
        quotaSettings.EnableQuota = inDto.EnableQuota;
        quotaSettings.DefaultQuota = quota > 0 ? quota : 0;

        await settingsManager.SaveAsync(quotaSettings);

        if (inDto.EnableQuota)
        {
            messageService.Send(MessageAction.QuotaPerUserChanged, quota.ToString());
        }
        else
        {
            messageService.Send(MessageAction.QuotaPerUserDisabled);
        }

        return quotaSettings;
    }

    /// <summary>
    /// Returns the user quota settings.
    /// </summary>
    /// <short>
    /// Get the user quota settings
    /// </short>
    /// <path>api/2.0/settings/userquotasettings</path>
    [Tags("Settings / Quota")]
    [SwaggerResponse(200, "Ok", typeof(TenantUserQuotaSettings))]
    [HttpGet("userquotasettings")]
    public async Task<TenantUserQuotaSettings> GetUserQuotaSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var result = await settingsManager.LoadAsync<TenantUserQuotaSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(result.LastModified) ? null : result;
    }

    /// <summary>
    /// Saves the room quota settings specified in the request to the current portal.
    /// </summary>
    /// <short>
    /// Save the room quota settings
    /// </short>
    /// <path>api/2.0/settings/roomquotasettings</path>
    [Tags("Settings / Quota")]
    [SwaggerResponse(200, "Tenant room quota settings", typeof(TenantRoomQuotaSettings))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpPost("roomquotasettings")]
    public async Task<TenantRoomQuotaSettings> SaveRoomQuotaSettings(QuotaSettingsRequestsDto inDto)
    {
        await DemandStatisticPermissionAsync();

        if (!inDto.DefaultQuota.TryGetInt64(out var quota))
        {
            throw new Exception(Resource.RoomQuotaGreaterPortalError);
        }

        var tenant = tenantManager.GetCurrentTenant();
        var tenantSpaceQuota = await tenantManager.GetTenantQuotaAsync(tenant.Id);
        var maxTotalSize = tenantSpaceQuota?.MaxTotalSize ?? -1;

        if (maxTotalSize < quota)
        {
            throw new Exception(Resource.RoomQuotaGreaterPortalError);
        }
        if (coreBaseSettings.Standalone)
        {
            var tenantQuotaSetting = await settingsManager.LoadAsync<TenantQuotaSettings>();
            if (tenantQuotaSetting.EnableQuota)
            {
                if (tenantQuotaSetting.Quota < quota)
                {
                    throw new Exception(Resource.RoomQuotaGreaterPortalError);
                }
            }
        }

        var quotaSettings = await settingsManager.LoadAsync<TenantRoomQuotaSettings>();
        quotaSettings.EnableQuota = inDto.EnableQuota;
        quotaSettings.DefaultQuota = quota > 0 ? quota : 0;

        await settingsManager.SaveAsync(quotaSettings);

        if (inDto.EnableQuota)
        {
            messageService.Send(MessageAction.QuotaPerRoomChanged, quota.ToString());
        }
        else
        {
            messageService.Send(MessageAction.QuotaPerRoomDisabled);
        }

        return quotaSettings;
    }

    /// <summary>
    /// Saves the AI Agent quota settings specified in the request to the current portal.
    /// </summary>
    /// <short>
    /// Save the AI Agent quota settings
    /// </short>
    /// <path>api/2.0/settings/aiagentquotasettings</path>
    [Tags("Settings / Quota")]
    [SwaggerResponse(200, "Tenant AI Agent quota settings", typeof(TenantAiAgentQuotaSettings))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpPost("aiagentquotasettings")]
    public async Task<TenantAiAgentQuotaSettings> SaveAiAgentQuotaSettings(QuotaSettingsRequestsDto inDto)
    {
        await DemandStatisticPermissionAsync();

        if (!inDto.DefaultQuota.TryGetInt64(out var quota))
        {
            throw new Exception(Resource.AiAgentQuotaGreaterPortalError);
        }

        var tenant = tenantManager.GetCurrentTenant();
        var tenantSpaceQuota = await tenantManager.GetTenantQuotaAsync(tenant.Id);
        var maxTotalSize = tenantSpaceQuota?.MaxTotalSize ?? -1;

        if (maxTotalSize < quota)
        {
            throw new Exception(Resource.AiAgentQuotaGreaterPortalError);
        }

        if (coreBaseSettings.Standalone)
        {
            var tenantQuotaSetting = await settingsManager.LoadAsync<TenantQuotaSettings>();
            if (tenantQuotaSetting.EnableQuota && tenantQuotaSetting.Quota < quota)
            {
                throw new Exception(Resource.AiAgentQuotaGreaterPortalError);
            }
        }

        var quotaSettings = await settingsManager.LoadAsync<TenantAiAgentQuotaSettings>();
        quotaSettings.EnableQuota = inDto.EnableQuota;
        quotaSettings.DefaultQuota = quota > 0 ? quota : 0;

        await settingsManager.SaveAsync(quotaSettings);

        if (inDto.EnableQuota)
        {
            messageService.Send(MessageAction.QuotaPerAiAgentChanged, quota.ToString());
        }
        else
        {
            messageService.Send(MessageAction.QuotaPerAiAgentDisabled);
        }

        return quotaSettings;
    }

    /// <summary>
    /// Saves the deep link configuration settings for the portal.
    /// </summary>
    /// <short>
    /// Configure the deep link settings
    /// </short>
    /// <path>api/2.0/settings/deeplink</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Deep link configuration updated", typeof(TenantDeepLinkSettings))]
    [SwaggerResponse(400, "Invalid deep link configuration")]
    [HttpPost("deeplink")]
    public async Task<TenantDeepLinkSettings> ConfigureDeepLink(DeepLinkConfigurationRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        if (!Enum.IsDefined(inDto.DeepLinkSettings.HandlingMode))
        {
            throw new ArgumentException(nameof(inDto.DeepLinkSettings.HandlingMode));
        }
        var tenant = tenantManager.GetCurrentTenant();
        var tenantDeepLinkSettings = await settingsManager.LoadAsync<TenantDeepLinkSettings>();

        tenantDeepLinkSettings.HandlingMode = inDto.DeepLinkSettings.HandlingMode;
        await settingsManager.SaveAsync(tenantDeepLinkSettings, tenant.Id);

        return tenantDeepLinkSettings;
    }

    /// <summary>
    /// Returns the deep link settings.
    /// </summary>
    /// <short>
    /// Get the deep link settings
    /// </short>
    /// <path>api/2.0/settings/deeplink</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Ok", typeof(TenantDeepLinkSettings))]
    [HttpGet("deeplink")]
    [AllowAnonymous]
    public async Task<TenantDeepLinkSettings> GetDeepLinkSettings()
    {
        var result = await settingsManager.LoadAsync<TenantDeepLinkSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(result.LastModified) ? null : result;
    }

    /// <summary>
    /// Saves the tenant quota settings specified in the request to the current portal.
    /// </summary>
    /// <short>
    /// Save the tenant quota settings
    /// </short>
    /// <path>api/2.0/settings/tenantquotasettings</path>
    [Tags("Settings / Quota")]
    [SwaggerResponse(200, "Tenant quota settings", typeof(TenantQuotaSettings))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [SwaggerResponse(405, "Not available")]
    [HttpPut("tenantquotasettings")]
    public async Task<TenantQuotaSettings> SetTenantQuotaSettings(TenantQuotaSettingsRequestsDto inDto)
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
            messageService.Send(MessageAction.QuotaPerPortalChanged, tenantQuotaSetting.Quota.ToString());
        }
        else
        {
            messageService.Send(MessageAction.QuotaPerPortalDisabled);
        }

        return tenantQuotaSetting;
    }

    /// <summary>
    /// Returns a list of all the available portal languages in the format of a two-letter or four-letter language code (e.g. "de", "en-US", etc.).
    /// </summary>
    /// <short>Get supported languages</short>
    /// <path>api/2.0/settings/cultures</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <collection>list</collection>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "List of all the available portal languages", typeof(IEnumerable<string>))]
    [AllowAnonymous]
    [AllowNotPayment]
    [HttpGet("cultures")]
    public async Task<IEnumerable<string>> GetSupportedCultures()
    {
        var result = coreBaseSettings.EnabledCultures.Select(r => r.Name).ToList();
        return HttpContext.TryGetFromCache(await HttpContextExtension.CalculateEtagAsync(result)) ? null : result;
    }

    /// <summary>
    /// Returns a list of all the available portal time zones.
    /// </summary>
    /// <short>Get time zones</short>
    /// <path>api/2.0/settings/timezones</path>
    /// <collection>list</collection>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "List of all the available time zones with their IDs and display names", typeof(List<TimezonesRequestsDto>))]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "Wizard,Administrators")]
    [HttpGet("timezones")]
    [AllowNotPayment]
    public async Task<List<TimezonesRequestsDto>> GetTimeZones()
    {
        await securityContext.AuthByClaimAsync();
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
    /// <path>api/2.0/settings/machine</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Portal hostname", typeof(object))]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "Wizard")]
    [HttpGet("machine")]
    [AllowNotPayment]
    public object GetPortalHostname()
    {
        return Request.Host.Value;
    }

    /// <summary>
    /// Saves the DNS settings specified in the request to the current portal.
    /// </summary>
    /// <short>Save the DNS settings</short>
    /// <path>api/2.0/settings/dns</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Message about changing DNS", typeof(string))]
    [SwaggerResponse(400, "Invalid domain name/incorrect length of doman name")]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [SwaggerResponse(405, "Method not allowed")]
    [HttpPut("dns")]
    public async Task<string> SaveDnsSettings(DnsSettingsRequestsDto inDto)
    {
        return await dnsSettings.SaveDnsSettingsAsync(inDto.DnsName, inDto.Enable);
    }

    /// <summary>
    /// Starts the process of the quota recalculation.
    /// </summary>
    /// <short>
    /// Recalculate the quota
    /// </short>
    /// <path>api/2.0/settings/recalculatequota</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Settings / Quota")]
    [HttpGet("recalculatequota")]
    public async Task RecalculateQuota()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await usersQuotaSyncOperation.RecalculateQuota(tenantManager.GetCurrentTenant());
    }

    /// <summary>
    /// Checks the process of the quota recalculation.
    /// </summary>
    /// <short>
    /// Check the quota recalculation
    /// </short>
    /// <path>api/2.0/settings/checkrecalculatequota</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Settings / Quota")]
    [SwaggerResponse(200, "Boolean value: true - quota recalculation process is enabled, false - quota recalculation process is disabled", typeof(bool))]
    [HttpGet("checkrecalculatequota")]
    public async Task<bool> CheckRecalculateQuota()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var result = await usersQuotaSyncOperation.CheckRecalculateQuota(tenantManager.GetCurrentTenant());
        return !result.IsCompleted;
    }

    /// <summary>
    /// Returns the portal logo image URL.
    /// </summary>
    /// <short>
    /// Get a portal logo
    /// </short>
    /// <path>api/2.0/settings/logo</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Portal logo image URL", typeof(string))]
    [HttpGet("logo")]
    public async Task<string> GetPortalLogo()
    {
        var result = await settingsManager.LoadAsync<TenantInfoSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(result.LastModified) ? null : await tenantInfoSettingsHelper.GetAbsoluteCompanyLogoPathAsync(result);
    }

    /// <summary>
    /// Completes the Wizard settings.
    /// </summary>
    /// <short>Complete the Wizard settings</short>
    /// <path>api/2.0/settings/wizard/complete</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Wizard settings", typeof(WizardSettings))]
    [SwaggerResponse(400, "Incorrect email address/The password is empty")]
    [SwaggerResponse(402, "You must enter a license key or license key is not correct or license expired or user quota does not match the license")]
    [AllowNotPayment]
    [HttpPut("wizard/complete")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "Wizard")]
    public async Task<WizardSettings> CompleteWizard(WizardRequestsDto inDto)
    {
        await securityContext.AuthByClaimAsync();

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await firstTimeTenantSettings.SaveDataAsync(inDto);
    }

    /// <summary>
    /// Closes the welcome pop-up notification.
    /// </summary>
    /// <short>Close the welcome pop-up notification</short>
    /// <path>api/2.0/settings/welcome/close</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Settings / Common settings")]
    [SwaggerResponse(405, "Not available")]
    [HttpPut("welcome/close")]
    public async Task CloseWelcomePopup()
    {
        var collaboratorPopupSettings = await settingsManager.LoadForCurrentUserAsync<CollaboratorSettings>();

        if (!(await userManager.IsGuestAsync(authContext.CurrentAccount.ID) && 
              collaboratorPopupSettings.FirstVisit && 
              !await userManager.IsOutsiderAsync(authContext.CurrentAccount.ID)))
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
    /// <path>api/2.0/settings/colortheme</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Settings of the portal themes", typeof(CustomColorThemesSettingsDto))]
    [AllowAnonymous, AllowNotPayment, AllowSuspended]
    [HttpGet("colortheme")]
    public async Task<CustomColorThemesSettingsDto> GetPortalColorTheme()
    {
        var settings = await settingsManager.LoadAsync<CustomColorThemesSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(settings.LastModified) ? null : new CustomColorThemesSettingsDto(settings, customColorThemesSettingsHelper.Limit);
    }

    /// <summary>
    /// Saves the portal color theme specified in the request.
    /// </summary>
    /// <short>Save a color theme</short>
    /// <path>api/2.0/settings/colortheme</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Portal theme settings", typeof(CustomColorThemesSettingsDto))]
    [HttpPut("colortheme")]
    public async Task<CustomColorThemesSettingsDto> SavePortalColorTheme(CustomColorThemesSettingsRequestsDto inDto)
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
            messageService.Send(MessageAction.ColorThemeChanged);
        }

        return new CustomColorThemesSettingsDto(settings, customColorThemesSettingsHelper.Limit);
    }

    /// <summary>
    /// Deletes the portal color theme with the ID specified in the request.
    /// </summary>
    /// <short>Delete a color theme</short>
    /// <path>api/2.0/settings/colortheme</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Portal theme settings: custom color theme settings, selected or not, limit", typeof(CustomColorThemesSettingsDto))]
    [HttpDelete("colortheme")]
    public async Task<CustomColorThemesSettingsDto> DeletePortalColorTheme(DeleteColorThemeRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = await settingsManager.LoadAsync<CustomColorThemesSettings>();

        if (CustomColorThemesSettingsItem.Default.Any(r => r.Id == inDto.Id))
        {
            return new CustomColorThemesSettingsDto(settings, customColorThemesSettingsHelper.Limit);
        }

        settings.Themes = settings.Themes.Where(r => r.Id != inDto.Id).ToList();

        if (settings.Selected == inDto.Id)
        {
            settings.Selected = settings.Themes.Min(r => r.Id);
            messageService.Send(MessageAction.ColorThemeChanged);
        }

        await settingsManager.SaveAsync(settings);

        return new CustomColorThemesSettingsDto(settings, customColorThemesSettingsHelper.Limit);
    }

    /// <summary>
    /// Closes the administrator helper notification.
    /// </summary>
    /// <short>Close the admin helper</short>
    /// <path>api/2.0/settings/closeadminhelper</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(405, "Not available")]
    [HttpPut("closeadminhelper")]
    public async Task CloseAdminHelper()
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
    /// <path>api/2.0/settings/timeandlanguage</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Message about saving settings successfully", typeof(object))]
    [HttpPut("timeandlanguage")]
    public async Task<string> SetTimaAndLanguage(TimeZoneRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var culture = CultureInfo.GetCultureInfo(inDto.Lng);
        var tenant = tenantManager.GetCurrentTenant();

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
                messageService.Send(MessageAction.TimeZoneSettingsUpdated);
            }
            if (changelng)
            {
                messageService.Send(MessageAction.LanguageSettingsUpdated);
            }
        }

        return Resource.SuccessfullySaveSettingsMessage;
    }

    /// <summary>
    /// Sets the default folder.
    /// </summary>
    /// <short>Set the default folder</short>
    /// <path>api/2.0/settings/defaultFolder</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Message about saving settings successfully", typeof(StudioDefaultPageSettings))]
    [HttpPut("defaultfolder")]
    public async Task<StudioDefaultPageSettings> SaveDefaultFolder(DefaultProductRequestDto inDto)
    {
        List<FolderType> allowedFolderTypes =
        [
            FolderType.AiAgents,
            FolderType.USER,
            FolderType.VirtualRooms,
            FolderType.SHARE,
            FolderType.Favorites,
            FolderType.Recent
        ];

        if (!allowedFolderTypes.Contains(inDto.DefaultFolderType))
        {
            throw new ArgumentException(nameof(inDto.DefaultFolderType));
        }

        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID) && inDto.DefaultFolderType == FolderType.USER)
        {
            throw new ArgumentException(nameof(inDto.DefaultFolderType));
        }
        
        var defaultPageSettings = new StudioDefaultPageSettings
        {
            DefaultFolderType = inDto.DefaultFolderType
        };
        
        await settingsManager.SaveForCurrentUserAsync(defaultPageSettings);

        messageService.Send(MessageAction.DefaultStartPageSettingsUpdated);

        return defaultPageSettings;
    }

    /// <summary>
    /// Updates the email activation settings.
    /// </summary>
    /// <short>Update the email activation settings</short>
    /// <path>api/2.0/settings/emailactivation</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Updated email activation settings", typeof(EmailActivationSettings))]
    [HttpPut("emailactivation")]
    public async Task<EmailActivationSettings> UpdateEmailActivationSettings(EmailActivationSettings inDto)
    {
        await settingsManager.SaveForCurrentUserAsync(inDto);
        return inDto;
    }

    /// <summary>
    /// Returns the space usage statistics for the module with the ID specified in the request.
    /// </summary>
    /// <short>Get the space usage statistics</short>
    /// <path>api/2.0/settings/statistics/spaceusage/{id}</path>
    /// <collection>list</collection>
    [Tags("Settings / Statistics")]
    [SwaggerResponse(200, "Module space usage statistics", typeof(List<UsageSpaceStatItemDto>))]
    [HttpGet("statistics/spaceusage/{id:guid}")]
    public async Task<List<UsageSpaceStatItemDto>> GetSpaceUsageStatistics(IdRequestDto<Guid> inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var webitem = webItemManagerSecurity.GetItems(WebZoneType.All, ItemAvailableState.All)
                                   .FirstOrDefault(item =>
                                                   item != null &&
                                                   item.ID == inDto.Id &&
                                                   item.Context is { SpaceUsageStatManager: not null });

        if (webitem == null)
        {
            return [];
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
    /// Returns the socket settings.
    /// </summary>
    /// <short>Get the socket settings</short>
    /// <path>api/2.0/settings/socket</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Socket settings: hub URL", typeof(object))]
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

    /// <summary>
    /// Returns the authorization services.
    /// </summary>
    /// <short>Get the authorization services</short>
    /// <path>api/2.0/settings/authservice</path>
    /// <collection>list</collection>
    [Tags("Settings / Authorization")]
    [SwaggerResponse(200, "Authorization services", typeof(IEnumerable<AuthServiceRequestsDto>))]
    [HttpGet("authservice")]
    public async Task<IEnumerable<AuthServiceRequestsDto>> GetAuthServices()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var logoText = await tenantLogoManager.GetLogoTextAsync();

        return await consumerFactory.GetAll<Consumer>()
            .Where(consumer => consumer.ManagedKeys.Any())
            .OrderBy(services => services.Order)
            .ToAsyncEnumerable()
            .Select(async (Consumer r, CancellationToken _) => await AuthServiceRequestsDto.From(r, logoText))
            .ToListAsync();
    }

    /// <summary>
    /// Saves the authorization keys.
    /// </summary>
    /// <short>Save the authorization keys</short>
    /// <path>api/2.0/settings/authservice</path>
    [Tags("Settings / Authorization")]
    [SwaggerResponse(200, "Boolean value: true if the authorization keys are changed", typeof(bool))]
    [SwaggerResponse(400, "Bad keys")]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpPost("authservice")]
    public async Task<bool> SaveAuthKeys(AuthServiceRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var consumer = consumerFactory.GetByKey<Consumer>(inDto.Name);

        if (!consumer.CanSet)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var tenantId = tenantManager.GetCurrentTenantId();
        var saveAvailable = !consumer.Paid || coreBaseSettings.Standalone || (await tenantManager.GetTenantQuotaAsync(tenantId)).ThirdParty;
        if (!SetupInfo.IsVisibleSettings(nameof(ManagementType.ThirdPartyAuthorization))
            || !saveAvailable)
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }

        var changed = false;

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
            messageService.Send(MessageAction.AuthorizationKeysSetting);

            if (consumer is TelegramLoginProvider)
            {
                await userSocketManager.ConnectTelegram(tenantId, authContext.CurrentAccount.ID);
            }
        }

        return changed;
    }

    /// <summary>
    /// Returns the portal payment settings.
    /// </summary>
    /// <short>Get the payment settings</short>
    /// <path>api/2.0/settings/payment</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Payment settings: sales email, feedback and support URL, link to pay for a portal, Standalone or not, current license, maximum quota quantity", typeof(PaymentSettingsDto))]
    [AllowNotPayment]
    [HttpGet("payment")]
    public async Task<PaymentSettingsDto> GetPaymentSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var currentQuota = await tenantManager.GetCurrentTenantQuotaAsync();
        var currentTariff = await tenantExtra.GetCurrentTariffAsync();

        if (!int.TryParse(configuration["core:payment:max-quantity"], out var maxQuotaQuantity))
        {
            maxQuotaQuantity = 999;
        }

        return new PaymentSettingsDto
        {
            SalesEmail = externalResourceSettingsHelper.Common.GetDefaultRegionalFullEntry("paymentemail"),
            BuyUrl = externalResourceSettingsHelper.Site.GetDefaultRegionalFullEntry("buy" + (configuration["license:type"] ?? "enterprise")),
            Standalone = coreBaseSettings.Standalone,
            CurrentLicense = new CurrentLicenseInfo { Trial = currentQuota.Trial, DueDate = currentTariff.DueDate.Date },
            Max = maxQuotaQuantity
        };
    }

    /// <summary>
    /// Returns the Developer Tools access settings for the portal.
    /// </summary>
    /// <short>
    /// Get the Developer Tools access settings
    /// </short>
    /// <path>api/2.0/settings/devtoolsaccess</path>
    [Tags("Settings / Access to DevTools")]
    [SwaggerResponse(200, "Developer Tools access settings", typeof(TenantDevToolsAccessSettings))]
    [HttpGet("devtoolsaccess")]
    public async Task<TenantDevToolsAccessSettings> GetTenantAccessDevToolsSettings()
    {
        return await settingsManager.LoadAsync<TenantDevToolsAccessSettings>();
    }

    /// <summary>
    /// Sets the Developer Tools access settings for the portal.
    /// </summary>
    /// <short>
    /// Set the Developer Tools access settings
    /// </short>
    /// <path>api/2.0/security/devtoolsaccess</path>
    [Tags("Security / Access to DevTools")]
    [SwaggerResponse(200, "Developer Tools access settings", typeof(TenantDevToolsAccessSettings))]
    [HttpPost("devtoolsaccess")]
    public async Task<TenantDevToolsAccessSettings> SetTenantDevToolsAccessSettings(TenantDevToolsAccessSettingsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = new TenantDevToolsAccessSettings { LimitedAccessForUsers = inDto.LimitedAccessForUsers };

        await settingsManager.SaveAsync(settings);

        messageService.Send(MessageAction.DevToolsAccessSettingsChanged);

        return settings;
    }

    /// <summary>
    /// Returns the visibility settings of the promotional banners in the portal.
    /// </summary>
    /// <short>
    /// Get the banners visibility
    /// </short>
    /// <path>api/2.0/settings/banner</path>
    [Tags("Settings / Banners visibility")]
    [SwaggerResponse(200, "Promotional banners visibility settings", typeof(TenantBannerSettings))]
    [HttpGet("banner")]
    public async Task<TenantBannerSettings> GetTenantBannerSettings()
    {
        return await settingsManager.LoadAsync<TenantBannerSettings>();
    }

    /// <summary>
    /// Sets the visibility settings of the promotional banners in the portal.
    /// </summary>
    /// <short>
    /// Set the banners visibility
    /// </short>
    /// <path>api/2.0/settings/banner</path>
    [Tags("Security / Banners visibility")]
    [SwaggerResponse(200, "Promotional banners visibility settings", typeof(TenantBannerSettings))]
    [HttpPost("banner")]
    public async Task<TenantBannerSettings> SetTenantBannerSettings(TenantBannerSettingsDto inDto)
    {
        if (!tenantExtra.Enterprise)
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = new TenantBannerSettings { Hidden = inDto.Hidden };

        await settingsManager.SaveAsync(settings);

        messageService.Send(MessageAction.BannerSettingsChanged);

        return settings;
    }

    private async Task DemandStatisticPermissionAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!coreBaseSettings.Standalone
            && !(await tenantManager.GetCurrentTenantQuotaAsync()).Statistic)
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }
    }


    /// <summary>
    /// Returns the portal user invitation settings.
    /// </summary>
    /// <short>Get the user invitation settings</short>
    /// <path>api/2.0/settings/invitationsettings</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "portal user invitation settings", typeof(TenantUserInvitationSettingsDto))]
    [HttpGet("invitationsettings")]
    [AllowAnonymous]
    public async Task<TenantUserInvitationSettingsDto> GetTenantUserInvitationSettings()
    {
        var settings = await settingsManager.LoadAsync<TenantUserInvitationSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(settings.LastModified)
            ? null
            : settings.Map();
    }


    /// <summary>
    /// Updates the portal user invitation settings.
    /// </summary>
    /// <short>Update user invitation settings</short>
    /// <path>api/2.0/settings/invitationsettings</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Updated user invitation settings", typeof(TenantUserInvitationSettingsDto))]
    [HttpPut("invitationsettings")]
    public async Task<TenantUserInvitationSettingsDto> UpdateInvitationSettings(TenantUserInvitationSettingsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = new TenantUserInvitationSettings
        {
            AllowInvitingMembers = inDto.AllowInvitingMembers,
            AllowInvitingGuests = inDto.AllowInvitingGuests
        };

        _ = await settingsManager.SaveAsync(settings);

        return settings.Map();
    }
}