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
    StorageFactory storageFactory,
    SetupInfo setupInfo,
    ExternalResourceSettings externalResourceSettings,
    ExternalResourceSettingsHelper externalResourceSettingsHelper,
    ConsumerFactory consumerFactory,
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
    QuotaSocketManager quotaSocketManager,
    ExternalDatabaseClient externalDatabaseClient)
    : BaseSettingsController(fusionCache, webItemManager)
{
    [GeneratedRegex("^[a-z0-9]([a-z0-9-.]){1,253}[a-z0-9]$")]
    private static partial Regex EmailDomainRegex();

    /// <remarks>
    /// Returns the current portal's general configuration: branding, culture, feature flags, and DocSpace/Standalone
    /// mode, everything the client needs to render its shell before or after login. No permission is required, but
    /// the response shape depends on the caller's identity. An anonymous caller receives only the public subset
    /// (culture, branding, DocSpace/Standalone flags, deep link data, setup-wizard and join-by-domain hints); once
    /// authenticated, the response also includes tenant-specific fields such as the owner ID, time zone, invitation
    /// limit, AI/banner/dev-tools flags, and, for a DocSpace administrator, the tenant wallet's low-balance flag.
    /// This is a read-only, idempotent call. Pass `withPassword=true` to also receive the parameters (`salt`,
    /// iteration count, hash size) used to hash the password client-side before it is sent to the authentication
    /// endpoints; these are only added for an anonymous caller or when explicitly requested, never as part of the
    /// default authenticated response.
    /// </remarks>
    /// <summary>
    /// Get the portal settings
    /// </summary>
    /// <path>api/2.0/settings</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Current portal settings, tailored to the caller's authentication state", typeof(SettingsDto))]
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
            var timeZone = TimeZoneConverter.GetTimeZone(tenant.TimeZone);
            settings.Timezone = TimeZoneConverter.GetIanaTimeZoneId(timeZone);
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
            settings.AiEnabled = (await settingsManager.LoadAsync<TenantAiAccessSettings>()).Enabled;

            if (await userManager.IsDocSpaceAdminAsync(securityContext.CurrentAccount.ID))
            {
                settings.WalletLowBalance = (await settingsManager.LoadAsync<TenantWalletSettings>()).LowBalanceNotified;
            }

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
            settings.ExternalDbEnabled = externalDatabaseClient.IsEnabled();
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

    /// <remarks>
    /// Overwrites the portal's trusted mail domain configuration, which controls which email domains are treated as
    /// already verified when a user is invited or self-registers. Requires Owner or DocSpaceAdmin (the
    /// EditPortalSettings permission). When the requested mode is a custom domain list, every domain is normalized to
    /// lowercase and checked against the expected hostname format; a domain that fails the check, or an empty custom
    /// list, causes the whole call to be rejected without saving anything. For the other modes the domain list in the
    /// request is ignored. The `inviteUsersAsVisitors` flag controls whether users who join through a trusted domain
    /// are added as full members or as visitors, and takes effect on the next join rather than retroactively. This is
    /// a mutating, idempotent call: repeating it with the same body leaves the portal in the same state. On success
    /// it returns a confirmation message, not the saved settings themselves; read them back from
    /// `GET api/2.0/settings`.
    /// </remarks>
    /// <summary>
    /// Save the mail domain settings
    /// </summary>
    /// <path>api/2.0/settings/maildomainsettings</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Confirmation message that the trusted mail domain settings were saved", typeof(string))]
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


    /// <remarks>
    /// Saves the user quota settings specified in the request to the current portal.
    /// </remarks>
    /// <summary>
    /// Save the user quota settings
    /// </summary>
    /// <path>api/2.0/settings/userquotasettings</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Settings / Quota")]
    [SwaggerResponse(200, "Message about the result of saving the user quota settings", typeof(TenantUserQuotaSettings))]
    [SwaggerResponse(400, "The entered quota value is invalid or greater than the total storage size")]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPost("userquotasettings")]
    public async Task<TenantUserQuotaSettings> SaveUserQuotaSettings(QuotaSettingsRequestsDto inDto)
    {
        await DemandStatisticPermissionAsync();

        if (!inDto.DefaultQuota.TryGetInt64(out var quota))
        {
            throw new ArgumentException(Resource.UserQuotaGreaterPortalError);
        }

        var tenant = tenantManager.GetCurrentTenant();
        var tenantSpaceQuota = await tenantManager.GetTenantQuotaAsync(tenant.Id);
        var maxTotalSize = tenantSpaceQuota?.MaxTotalSize ?? -1;

        if (maxTotalSize < quota)
        {
            throw new ArgumentException(Resource.UserQuotaGreaterPortalError);
        }
        var tenantQuotaSetting = await settingsManager.LoadAsync<TenantQuotaSettings>();
        if (coreBaseSettings.Standalone)
        {
            if (tenantQuotaSetting.EnableQuota)
            {
                if (tenantQuotaSetting.Quota < quota)
                {
                    throw new ArgumentException(Resource.UserQuotaGreaterPortalError);
                }
            }
        }
        var quotaSettings = await settingsManager.LoadAsync<TenantUserQuotaSettings>();
        quotaSettings.EnableQuota = inDto.EnableQuota;
        quotaSettings.DefaultQuota = quota > 0 ? quota : 0;

        await settingsManager.SaveAsync(quotaSettings);

        var usedSize = (await tenantManager.FindTenantQuotaRowsAsync(tenant.Id))
          .Where(r => !string.IsNullOrEmpty(r.Tag) && new Guid(r.Tag) != Guid.Empty)
          .Sum(r => r.Counter);
        var admins = (await userManager.GetUsersByGroupAsync(ASC.Core.Users.Constants.GroupAdmin.ID)).Select(u => u.Id).ToList();

        _ = quotaSocketManager.ChangeCustomQuotaUsedValueAsync(tenant.Id, customQuota.GetFeature<TenantCustomQuotaFeature>().Name, tenantQuotaSetting.EnableQuota, usedSize, tenantQuotaSetting.Quota, admins);

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

    /// <remarks>
    /// Returns the portal's per-user default storage quota: whether it is enabled and, if so, its size in bytes.
    /// Requires Owner or DocSpaceAdmin (the EditPortalSettings permission); every other authenticated role, and an
    /// anonymous caller, is refused. This is a read-only, idempotent call. When `enableQuota` is false, the size
    /// value is not enforced and users get unlimited personal storage regardless of what it holds. The response
    /// supports conditional requests: send the standard If-Modified-Since header with the previous `lastModified`
    /// value, and an unchanged response comes back empty instead of resending the settings.
    /// </remarks>
    /// <summary>
    /// Get the user quota settings
    /// </summary>
    /// <path>api/2.0/settings/userquotasettings</path>
    [Tags("Settings / Quota")]
    [SwaggerResponse(200, "Current per-user default storage quota settings", typeof(TenantUserQuotaSettings))]
    [HttpGet("userquotasettings")]
    public async Task<TenantUserQuotaSettings> GetUserQuotaSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var result = await settingsManager.LoadAsync<TenantUserQuotaSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(result.LastModified) ? null : result;
    }

    /// <remarks>
    /// Sets the portal's default per-room storage quota, applied to newly created rooms as their starting limit.
    /// Requires Owner or DocSpaceAdmin (the EditPortalSettings permission), and on a paid SaaS tenant the portal's
    /// plan must include the statistics feature, or the call is rejected as not covered by the plan. The requested
    /// size cannot exceed the portal's own total storage quota, nor, on a Standalone install with a portal-wide quota
    /// enabled, that quota's size. Disable enforcement by passing `enableQuota=false`; the size is then ignored for
    /// new rooms. This is a mutating, idempotent call: sending the same body again leaves the quota unchanged. It
    /// returns the saved settings, not the individual rooms' current usage.
    /// </remarks>
    /// <summary>
    /// Save the room quota settings
    /// </summary>
    /// <path>api/2.0/settings/roomquotasettings</path>
    [Tags("Settings / Quota")]
    [SwaggerResponse(200, "Saved default per-room storage quota settings", typeof(TenantRoomQuotaSettings))]
    [SwaggerResponse(402, "The portal's pricing plan does not include the statistics feature required for room quotas")]
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

    /// <remarks>
    /// Sets the portal's default storage quota for AI agents, applied as the starting limit for newly created agents.
    /// Requires Owner or DocSpaceAdmin (the EditPortalSettings permission), and on a paid SaaS tenant the portal's
    /// plan must include the statistics feature, or the call is rejected as not covered by the plan. The requested
    /// size cannot exceed the portal's own total storage quota, nor, on a Standalone install with a portal-wide quota
    /// enabled, that quota's size. Disable enforcement by passing `enableQuota=false`; the size is then ignored for
    /// new agents. This is a mutating, idempotent call: sending the same body again leaves the quota unchanged. It
    /// returns the saved settings, not any agent's current usage.
    /// </remarks>
    /// <summary>
    /// Save the AI Agent quota settings
    /// </summary>
    /// <path>api/2.0/settings/aiagentquotasettings</path>
    [Tags("Settings / Quota")]
    [SwaggerResponse(200, "Saved default AI agent storage quota settings", typeof(TenantAiAgentQuotaSettings))]
    [SwaggerResponse(402, "The portal's pricing plan does not include the statistics feature required for AI agent quotas")]
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

    /// <remarks>
    /// Sets how the portal responds when a client opens a DocSpace link on a mobile device: always in the browser,
    /// always in the native app, or asking the user to choose each time. Requires Owner or DocSpaceAdmin (the
    /// EditPortalSettings permission). The handling mode must be one of the documented enum values; anything else is
    /// rejected without being saved. This is a mutating, idempotent call: sending the same mode again leaves the
    /// setting unchanged. It returns the saved deep link settings, including the timestamp of the last change; read
    /// the current value at any time, including anonymously, from `GET api/2.0/settings/deeplink`.
    /// </remarks>
    /// <summary>
    /// Configure the deep link settings
    /// </summary>
    /// <path>api/2.0/settings/deeplink</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Saved deep link handling settings", typeof(TenantDeepLinkSettings))]
    [SwaggerResponse(400, "The handling mode is not one of the supported deep link handling values")]
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

    /// <remarks>
    /// Returns how the portal currently responds when a client opens a DocSpace link on a mobile device: always in
    /// the browser, always in the native app, or asking the user to choose. No permission is required; anonymous
    /// callers can read it too. This is a read-only, idempotent call. The response supports conditional requests:
    /// send the standard If-Modified-Since header with the previous `lastModified` value, and an unchanged response
    /// comes back empty instead of resending the settings. Change the mode with `POST api/2.0/settings/deeplink`,
    /// which requires the EditPortalSettings permission.
    /// </remarks>
    /// <summary>
    /// Get the deep link settings
    /// </summary>
    /// <path>api/2.0/settings/deeplink</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Current deep link handling settings", typeof(TenantDeepLinkSettings))]
    [HttpGet("deeplink")]
    [AllowAnonymous]
    public async Task<TenantDeepLinkSettings> GetDeepLinkSettings()
    {
        var result = await settingsManager.LoadAsync<TenantDeepLinkSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(result.LastModified) ? null : result;
    }

    /// <remarks>
    /// Sets or removes the storage quota for a given tenant. Available only on a Standalone (self-hosted)
    /// installation; on SaaS the call is always refused. Requires a DocSpace administrator, and the portal's plan
    /// must include the statistics feature or the call is rejected as not covered by the plan. Pass a non-negative
    /// `quota` in bytes to enable the limit for the tenant identified by `tenantId`, or a negative value to remove
    /// any limit. This is a mutating, idempotent call: sending the same body again leaves the quota unchanged. It
    /// returns the saved quota settings for that tenant, not its current usage.
    /// </remarks>
    /// <summary>
    /// Save the tenant quota settings
    /// </summary>
    /// <path>api/2.0/settings/tenantquotasettings</path>
    [Tags("Settings / Quota")]
    [SwaggerResponse(200, "Saved tenant storage quota settings", typeof(TenantQuotaSettings))]
    [SwaggerResponse(402, "The portal's pricing plan does not include the statistics feature required for tenant quotas")]
    [SwaggerResponse(405, "The caller is not a DocSpace administrator, or the portal is not a Standalone installation")]
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

    /// <remarks>
    /// Returns the two- or four-letter language codes of every culture currently enabled on the portal (for example
    /// `en-US`), used to populate a language picker before or after login. No permission is required; anonymous
    /// callers can read it too. This is a read-only, idempotent call, and the list is not paginated. The response
    /// supports conditional requests: an unchanged result is signaled instead of resending the same list. The set of
    /// enabled cultures is a portal-wide configuration value, not a per-user preference.
    /// </remarks>
    /// <summary>Get supported languages</summary>
    /// <path>api/2.0/settings/cultures</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <collection>list</collection>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Language codes of every culture currently enabled on the portal", typeof(IEnumerable<string>))]
    [AllowAnonymous]
    [AllowNotPayment]
    [HttpGet("cultures")]
    public async Task<IEnumerable<string>> GetSupportedCultures()
    {
        var result = coreBaseSettings.EnabledCultures.Select(r => r.Name).ToList();
        return HttpContext.TryGetFromCache(await HttpContextExtension.CalculateEtagAsync(result)) ? null : result;
    }

    /// <remarks>
    /// Returns every time zone known to the host machine, each with its IANA identifier and a human-readable display
    /// name, ordered from the most negative to the most positive UTC offset. This call is not for a normal logged-in
    /// session: it requires a confirmation link bearing the Wizard or Administrators claim, of the kind generated
    /// during initial portal setup or issued by an administrator, and the link is consumed as part of authenticating
    /// the request. This is a read-only, idempotent call, and the list is not paginated. Use the returned `id` values
    /// wherever the portal expects a time zone identifier; an unrecognized value is rejected there, not here.
    /// </remarks>
    /// <summary>Get time zones</summary>
    /// <path>api/2.0/settings/timezones</path>
    /// <collection>list</collection>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Every time zone known to the host, with its IANA ID and display name", typeof(List<TimezonesRequestsDto>))]
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
                Id = TimeZoneConverter.GetIanaTimeZoneId(tz),
                DisplayName = TimeZoneConverter.GetTimeZoneDisplayName(tz)
            });
        }

        return listOfTimezones;
    }

    /// <remarks>
    /// Returns the hostname the current request arrived on, exactly as sent in the HTTP Host header, so a client
    /// mid-setup can learn the address the portal is actually reachable at. This call is not for a normal logged-in
    /// session: it requires a confirmation link bearing the Wizard claim, of the kind generated during initial portal
    /// setup, and the link is consumed as part of authenticating the request. This is a read-only, idempotent call.
    /// The value reflects whatever the caller connected through, including a reverse proxy's public name, and is not
    /// necessarily the tenant's configured alias or mapped domain.
    /// </remarks>
    /// <summary>Get the portal hostname</summary>
    /// <path>api/2.0/settings/machine</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Hostname the current request arrived on", typeof(string))]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "Wizard")]
    [HttpGet("machine")]
    [AllowNotPayment]
    public string GetPortalHostname()
    {
        return Request.Host.Value;
    }

    /// <remarks>
    /// Maps a custom domain name onto the current tenant, or clears the mapping, so the portal becomes reachable
    /// under the caller's own DNS name instead of only its default alias. Available only on a Standalone
    /// (self-hosted) installation; on SaaS the call is always refused. Requires Owner or DocSpaceAdmin (the
    /// EditPortalSettings permission). Disable the mapping by passing `enable=false`, in which case the domain name
    /// in the request is ignored. A domain that collides with the portal's reserved base domain, or otherwise fails
    /// validation, is rejected without changing the current mapping. This is a mutating, idempotent call. On success
    /// the previous domain also stops answering, and any CSP configuration referencing it is updated to the new one.
    /// </remarks>
    /// <summary>Save the DNS settings</summary>
    /// <path>api/2.0/settings/dns</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Confirmation that the DNS mapping was updated", typeof(string))]
    [SwaggerResponse(400, "The domain name is invalid, or collides with the portal's reserved base domain")]
    [SwaggerResponse(402, "This option is not available under the portal's current pricing plan")]
    [SwaggerResponse(405, "The portal is not a Standalone installation, so a custom domain cannot be mapped")]
    [HttpPut("dns")]
    public async Task<string> SaveDnsSettings(DnsSettingsRequestsDto inDto)
    {
        return await dnsSettings.SaveDnsSettingsAsync(inDto.DnsName, inDto.Enable);
    }

    /// <remarks>
    /// Starts the process of the quota recalculation.
    /// </remarks>
    /// <summary>
    /// Recalculate the quota
    /// </summary>
    /// <path>api/2.0/settings/recalculatequota</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Settings / Quota")]
    [HttpGet("recalculatequota")]
    public async Task RecalculateQuota()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await usersQuotaSyncOperation.RecalculateQuota(tenantManager.GetCurrentTenant());
    }

    /// <remarks>
    /// Checks the process of the quota recalculation.
    /// </remarks>
    /// <summary>
    /// Check the quota recalculation
    /// </summary>
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

    /// <remarks>
    /// Returns the absolute URL of the portal's current logo image, already resolved against the active white-label
    /// branding. Requires an authenticated session; every role, including Guest, can read it. This is a read-only,
    /// idempotent call. The response supports conditional requests: send the standard If-Modified-Since header with
    /// the previous `lastModified` value, and an unchanged response comes back empty instead of resending the same
    /// URL. The URL points at whatever image is currently configured, including the default DocSpace logo when no
    /// custom branding has been set.
    /// </remarks>
    /// <summary>
    /// Get a portal logo
    /// </summary>
    /// <path>api/2.0/settings/logo</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Absolute URL of the portal's current logo image", typeof(string))]
    [HttpGet("logo")]
    public async Task<string> GetPortalLogo()
    {
        var result = await settingsManager.LoadAsync<TenantInfoSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(result.LastModified) ? null : await tenantInfoSettingsHelper.GetAbsoluteCompanyLogoPathAsync(result);
    }

    /// <remarks>
    /// Finishes the initial portal setup wizard: sets the owner's password and locale, applies the supplied license
    /// if one is required, and marks the wizard as completed so it is not shown again. This call is not for a normal
    /// logged-in session: it requires a confirmation link bearing the Wizard claim, of the kind issued when a new
    /// portal is created, and the link is consumed as part of authenticating the request; the caller must also hold
    /// the EditPortalSettings permission. An empty password or a malformed email address is rejected without
    /// completing the wizard, and so is a missing, invalid, or expired license, or a license whose user quota does
    /// not cover the portal. This call is meant to run once per portal; running it again is accepted but has no
    /// further effect once the wizard is already completed. It returns the resulting wizard settings, including the
    /// completed flag.
    /// </remarks>
    /// <summary>Complete the Wizard settings</summary>
    /// <path>api/2.0/settings/wizard/complete</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Resulting wizard settings, including the completed flag", typeof(WizardSettings))]
    [SwaggerResponse(400, "The email address is malformed, or the password is empty")]
    [SwaggerResponse(402, "The supplied license is missing, invalid, expired, or its user quota does not cover the portal")]
    [AllowNotPayment]
    [HttpPut("wizard/complete")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "Wizard")]
    public async Task<WizardSettings> CompleteWizard(WizardRequestsDto inDto)
    {
        await securityContext.AuthByClaimAsync();

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await firstTimeTenantSettings.SaveDataAsync(inDto);
    }

    /// <remarks>
    /// Closes the welcome pop-up notification.
    /// </remarks>
    /// <summary>Close the welcome pop-up notification</summary>
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

    /// <remarks>
    /// Returns the portal's color theme configuration: every saved custom theme, which one is currently selected, and
    /// how many custom themes the plan still allows. No permission is required; anonymous callers can read it too.
    /// This is a read-only, idempotent call. The response supports conditional requests: send the standard
    /// If-Modified-Since header with the previous `lastModified` value, and an unchanged response comes back empty
    /// instead of resending the same settings. A `limit` of `0` means the plan does not cap the number of custom
    /// themes.
    /// </remarks>
    /// <summary>Get a color theme</summary>
    /// <path>api/2.0/settings/colortheme</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Current color theme configuration: saved themes, selected theme, and plan limit", typeof(CustomColorThemesSettingsDto))]
    [AllowAnonymous, AllowNotPayment, AllowSuspended]
    [HttpGet("colortheme")]
    public async Task<CustomColorThemesSettingsDto> GetPortalColorTheme()
    {
        var settings = await settingsManager.LoadAsync<CustomColorThemesSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(settings.LastModified) ? null : new CustomColorThemesSettingsDto(settings, customColorThemesSettingsHelper.Limit);
    }

    /// <remarks>
    /// Adds or updates a custom color theme, or changes which theme is selected, for the whole portal. Requires Owner
    /// or DocSpaceAdmin (the EditPortalSettings permission). Pass `theme` to create or edit one: an existing theme is
    /// matched and updated by its ID, a new one is appended, and an ID that collides with a built-in default theme is
    /// treated as a request to create a new custom theme instead of overwriting the default. Once the plan's
    /// custom-theme limit is reached, a new theme is silently not added rather than rejected with an error, so check
    /// the returned `themes` count against `limit` before assuming it was saved. Pass `selected` to switch the active
    /// theme; an ID that does not match any existing theme is ignored. This is a mutating call, not strictly
    /// idempotent once the limit has been reached. It returns the full updated theme configuration.
    /// </remarks>
    /// <summary>Save a color theme</summary>
    /// <path>api/2.0/settings/colortheme</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Updated color theme configuration: saved themes, selected theme, and plan limit", typeof(CustomColorThemesSettingsDto))]
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

    /// <remarks>
    /// Removes a custom color theme from the portal by its ID. Requires Owner or DocSpaceAdmin (the
    /// EditPortalSettings permission). An ID belonging to one of the built-in default themes is not removable; the
    /// call succeeds but leaves the theme list unchanged. If the deleted theme was the currently selected one, the
    /// theme with the lowest remaining ID is selected automatically. This is a mutating, idempotent call: deleting an
    /// ID that is already gone succeeds without error and again leaves nothing changed. It returns the full updated
    /// theme configuration, including the (possibly new) selected theme.
    /// </remarks>
    /// <summary>Delete a color theme</summary>
    /// <path>api/2.0/settings/colortheme</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Updated color theme configuration: saved themes, selected theme, and plan limit", typeof(CustomColorThemesSettingsDto))]
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

    /// <remarks>
    /// Dismisses the administrator helper tip for the caller, so it is not shown again on this account. Available
    /// only to a DocSpace administrator, which includes the portal Owner, on a Standalone (self-hosted) installation
    /// running outside white-label custom mode; every other caller is refused. This is a mutating, idempotent call
    /// scoped to the calling account only; it never affects other administrators. It returns no data on success.
    /// </remarks>
    /// <summary>Close the admin helper</summary>
    /// <path>api/2.0/settings/closeadminhelper</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "The admin helper tip was dismissed for the caller")]
    [SwaggerResponse(405, "The caller is not a DocSpace administrator, or the portal is on SaaS, custom mode, or not Standalone")]
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

    /// <remarks>
    /// Sets the portal time zone and language specified in the request.
    /// </remarks>
    /// <summary>Set time zone and language</summary>
    /// <path>api/2.0/settings/timeandlanguage</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Message about saving settings successfully", typeof(object))]
    [HttpPut("timeandlanguage")]
    public async Task<string> SetTimeAndLanguage(TimeZoneRequestDto inDto)
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

        tenant.TimeZone = TimeZoneConverter.GetIanaTimeZoneId(inDto.TimeZoneID);

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

    /// <remarks>
    /// Sets which folder the current user's account opens into by default, such as My Documents, the rooms list, or
    /// favorites. Requires an authenticated session; every role may set its own default, and the change never affects
    /// any other user. Only folder types the client actually offers as a landing page are accepted; picking My
    /// Documents (`USER`) as a Guest is rejected too, since guests have no personal storage. This is a mutating,
    /// idempotent call. It returns the saved setting.
    /// </remarks>
    /// <summary>Set the default folder</summary>
    /// <path>api/2.0/settings/defaultFolder</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Saved default folder setting for the current user", typeof(StudioDefaultPageSettings))]
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
            FolderType.Recent,
            FolderType.Forms,
            FolderType.DEFAULT
        ];

        if (!allowedFolderTypes.Contains(inDto.DefaultFolderType) ||
            await userManager.IsGuestAsync(authContext.CurrentAccount.ID) && inDto.DefaultFolderType == FolderType.USER)
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

    /// <remarks>
    /// Updates the current user's own preference for whether the email confirmation prompt is displayed on their
    /// account. Requires an authenticated session; every role may change its own setting, and the change never
    /// affects any other user. This is a mutating, idempotent call. It returns the settings exactly as submitted,
    /// without validating them against the account's actual email confirmation state, so `show` can be set to `true`
    /// even after the address is already confirmed.
    /// </remarks>
    /// <summary>Update the email activation settings</summary>
    /// <path>api/2.0/settings/emailactivation</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Email activation settings exactly as submitted", typeof(EmailActivationSettings))]
    [HttpPut("emailactivation")]
    public async Task<EmailActivationSettings> UpdateEmailActivationSettings(EmailActivationSettings inDto)
    {
        await settingsManager.SaveForCurrentUserAsync(inDto);
        return inDto;
    }

    /// <remarks>
    /// Returns the storage space used by one portal module, broken down per data category the module tracks (for
    /// example per room type), together with a human-readable size and whether the category is disabled. Requires
    /// Owner or DocSpaceAdmin (the EditPortalSettings permission). `id` identifies the module by the same GUID the
    /// portal's module catalog uses; a module that does not exist, or one that does not report space usage at all,
    /// returns an empty list rather than an error. This is a read-only, idempotent call, and the list is not
    /// paginated. Sizes are already formatted as display strings (for example `1.5 GB`), not raw byte counts.
    /// </remarks>
    /// <summary>Get the space usage statistics</summary>
    /// <path>api/2.0/settings/statistics/spaceusage/{id}</path>
    /// <collection>list</collection>
    [Tags("Settings / Statistics")]
    [SwaggerResponse(200, "Per-category space usage statistics for the requested module", typeof(List<UsageSpaceStatItemDto>))]
    [HttpGet("statistics/spaceusage/{id:guid}")]
    public async Task<List<UsageSpaceStatItemDto>> GetSpaceUsageStatistics(IdRequestDto<Guid> inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var webitem = (await webItemManagerSecurity.GetItemsAsync(WebZoneType.All, ItemAvailableState.All))
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

    /// <remarks>
    /// Returns the base URL of the portal's real-time notification hub (Socket.IO), which the client connects to for
    /// live updates such as file changes, presence, or quota alerts. Requires an authenticated session; every role
    /// can read it. This is a read-only, idempotent call. The value comes from server-side configuration and cannot
    /// be changed through this API; an empty `url` means the portal has no notification hub configured and the client
    /// should not attempt to connect.
    /// </remarks>
    /// <summary>Get the socket settings</summary>
    /// <path>api/2.0/settings/socket</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Base URL of the portal's real-time notification hub", typeof(SocketSettingsDto))]
    [HttpGet("socket")]
    public SocketSettingsDto GetSocketSettings()
    {
        var hubUrl = configuration["web:hub"] ?? string.Empty;
        if (hubUrl.Length != 0)
        {
            if (!hubUrl.EndsWith('/'))
            {
                hubUrl += "/";
            }
        }

        return new SocketSettingsDto { Url = hubUrl };
    }

    /// <remarks>
    /// Returns the catalogue of third-party storage and authorization providers DocSpace can integrate with (for
    /// example Amazon S3, Dropbox, Google, or Telegram), including whichever keys were last saved for each one that
    /// currently has any configured. Requires Owner or DocSpaceAdmin (the EditPortalSettings permission). This is a
    /// read-only, idempotent call, and the list is not paginated; entries are ordered by the provider's configured
    /// display order. Only providers that expose at least one manageable key are included, so a provider with nothing
    /// to configure is omitted entirely. Save or change a provider's keys with `POST api/2.0/settings/authservice`.
    /// </remarks>
    /// <summary>Get the authorization services</summary>
    /// <path>api/2.0/settings/authservice</path>
    /// <collection>list</collection>
    [Tags("Settings / Authorization")]
    [SwaggerResponse(200, "Third-party providers with a manageable key, and their last-saved key values", typeof(IEnumerable<AuthServiceRequestsDto>))]
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

    /// <remarks>
    /// Saves the authorization keys for one third-party storage or authorization provider, identified by name, or
    /// clears them when every submitted key is left empty. Requires Owner or DocSpaceAdmin (the EditPortalSettings
    /// permission); a provider that does not allow its keys to be changed from the API rejects the call outright. A
    /// provider that is only available on a paid plan additionally requires the portal's tariff to include
    /// third-party storage, or Standalone licensing, before the call is accepted. Keys that fail the provider's own
    /// validation are cleared and the call is rejected rather than left partially applied. This is a mutating,
    /// idempotent call: resaving identical keys succeeds and reports no change. It returns whether the keys actually
    /// changed, not the keys themselves; connecting Telegram or an external database through this call also triggers
    /// the matching real-time connection update.
    /// </remarks>
    /// <summary>Save the authorization keys</summary>
    /// <path>api/2.0/settings/authservice</path>
    [Tags("Settings / Authorization")]
    [SwaggerResponse(200, "Whether the provider's keys actually changed", typeof(bool))]
    [SwaggerResponse(400, "The submitted keys failed the provider's own validation")]
    [SwaggerResponse(402, "The provider is a paid option not covered by the portal's current pricing plan")]
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

            if (consumer is ExternalDatabaseProvider externalDbProvider)
            {
                await userSocketManager.UpdateExternalDbSettingsAsync(tenantId, externalDbProvider.IsEnabled());
            }
        }

        return changed;
    }

    /// <remarks>
    /// Probes connectivity to an external database using the settings supplied in the request, without saving them or
    /// affecting the portal's own configuration. Requires Owner or DocSpaceAdmin (the EditPortalSettings permission).
    /// SQLite is only accepted as a target on a Standalone (self-hosted) installation; requesting it on SaaS is
    /// reported as a failed connection rather than an error. This is a read-only call, safe to retry. A failed
    /// connection is not an HTTP error: the response always comes back as a normal success with `success=false` and
    /// an `error` message describing what went wrong.
    /// </remarks>
    /// <summary>Test external database connection</summary>
    /// <path>api/2.0/settings/authservice/externaldb/test</path>
    [Tags("Settings / Authorization")]
    [SwaggerResponse(200, "Connection test result: a success flag and, on failure, an error message", typeof(ConnectionTestResult))]
    [HttpPost("authservice/externaldb/test")]
    public async Task<ConnectionTestResult> TestExternalDatabaseConnection(ExternalDatabaseSettings inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (inDto.DatabaseTypeEnum == ExternalDatabaseType.Sqlite && !coreBaseSettings.Standalone)
        {
            return ConnectionTestResult.Failure(Resource.ConsumersExternalDbSqliteStandaloneOnly);
        }

        return await ExternalDatabaseProvider.TestConnectionAsync(inDto, storageFactory, tenantManager.GetCurrentTenantId());
    }

    /// <remarks>
    /// Returns the portal's payment-related configuration: the sales contact email, the URL to buy or extend a
    /// subscription, whether the portal is Standalone, the current license's trial status and expiration date, and
    /// the maximum quota quantity that can be purchased at once. Requires Owner or DocSpaceAdmin (the
    /// EditPortalSettings permission). This is a read-only, idempotent call. It remains reachable even while the
    /// portal's own subscription payment is overdue, since this is how the caller finds the link to resolve it.
    /// </remarks>
    /// <summary>Get the payment settings</summary>
    /// <path>api/2.0/settings/payment</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Payment-related settings: sales contact, buy URL, Standalone flag, license, and quota cap", typeof(PaymentSettingsDto))]
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

    /// <remarks>
    /// Returns whether the portal currently restricts the `User` role from using the developer tools (API keys, OAuth
    /// apps, webhooks). Requires an authenticated session; every role can read the restriction, even though it only
    /// limits what a `User` may do, not what a `RoomAdmin` or `DocSpaceAdmin` may do. This is a read-only, idempotent
    /// call. Change the restriction with `POST api/2.0/security/devtoolsaccess`, which requires the
    /// EditPortalSettings permission.
    /// </remarks>
    /// <summary>
    /// Get the Developer Tools access settings
    /// </summary>
    /// <path>api/2.0/settings/devtoolsaccess</path>
    [Tags("Settings / Access to DevTools")]
    [SwaggerResponse(200, "Whether the `User` role is currently restricted from using the developer tools", typeof(TenantDevToolsAccessSettings))]
    [HttpGet("devtoolsaccess")]
    public async Task<TenantDevToolsAccessSettings> GetTenantAccessDevToolsSettings()
    {
        return await settingsManager.LoadAsync<TenantDevToolsAccessSettings>();
    }

    /// <remarks>
    /// Sets whether the portal restricts the `User` role from using the developer tools (API keys, OAuth apps,
    /// webhooks); `RoomAdmin` and `DocSpaceAdmin` are never affected by this setting. Requires Owner or DocSpaceAdmin
    /// (the EditPortalSettings permission). This is a mutating, idempotent, portal-wide call: it applies to every
    /// `User` on the tenant immediately. It returns the saved setting; read the current value at any time from
    /// `GET api/2.0/settings/devtoolsaccess`.
    /// </remarks>
    /// <summary>
    /// Set the Developer Tools access settings
    /// </summary>
    /// <path>api/2.0/security/devtoolsaccess</path>
    [Tags("Security / Access to DevTools")]
    [SwaggerResponse(200, "Saved developer tools access restriction for the `User` role", typeof(TenantDevToolsAccessSettings))]
    [HttpPost("devtoolsaccess")]
    public async Task<TenantDevToolsAccessSettings> SetTenantDevToolsAccessSettings(TenantDevToolsAccessSettingsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = new TenantDevToolsAccessSettings { LimitedAccessForUsers = inDto.LimitedAccessForUsers };

        await settingsManager.SaveAsync(settings);

        messageService.Send(MessageAction.DevToolsAccessSettingsChanged);

        return settings;
    }

    /// <remarks>
    /// Returns whether the portal's promotional banners are currently hidden from every user's interface. Requires an
    /// authenticated session; every role can read it, since the flag affects what they see regardless of their own
    /// permissions. This is a read-only, idempotent call. The flag only takes effect on a Standalone (self-hosted)
    /// installation; on SaaS, banners are always shown no matter what is saved here. Change the setting with
    /// `POST api/2.0/settings/banner`, which additionally requires an Enterprise license.
    /// </remarks>
    /// <summary>
    /// Get the banners visibility
    /// </summary>
    /// <path>api/2.0/settings/banner</path>
    [Tags("Settings / Banners visibility")]
    [SwaggerResponse(200, "Whether the portal's promotional banners are currently hidden", typeof(TenantBannerSettings))]
    [HttpGet("banner")]
    public async Task<TenantBannerSettings> GetTenantBannerSettings()
    {
        return await settingsManager.LoadAsync<TenantBannerSettings>();
    }

    /// <remarks>
    /// Sets whether the portal's promotional banners are hidden for every user. Available only on an Enterprise
    /// license; every other plan is refused regardless of the caller's role. Requires Owner or DocSpaceAdmin (the
    /// EditPortalSettings permission). The flag only takes effect on a Standalone (self-hosted) installation; on
    /// SaaS, banners are always shown no matter what is saved here. This is a mutating, idempotent, portal-wide call:
    /// it applies to every user on the tenant immediately. It returns the saved setting; read the current value at
    /// any time from `GET api/2.0/settings/banner`.
    /// </remarks>
    /// <summary>
    /// Set the banners visibility
    /// </summary>
    /// <path>api/2.0/settings/banner</path>
    [Tags("Security / Banners visibility")]
    [SwaggerResponse(200, "Saved promotional banners visibility setting", typeof(TenantBannerSettings))]
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

    /// <remarks>
    /// Returns whether AI functionality (chat, agents, vectorization) is currently available on the portal at all; AI
    /// is enabled by default. Requires an authenticated session; every role can read it. This is a read-only,
    /// idempotent call. When the setting is disabled, every AI-specific endpoint and folder is unavailable regardless
    /// of the caller's own permissions; this call only reports the portal-wide switch, not any per-user entitlement.
    /// </remarks>
    /// <summary>
    /// Get the AI access settings
    /// </summary>
    /// <path>api/2.0/settings/ai-access</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Whether AI functionality is currently enabled for the portal", typeof(TenantAiAccessSettings))]
    [HttpGet("ai-access")]
    public async Task<TenantAiAccessSettings> GetTenantAiAccessSettings()
    {
        return await settingsManager.LoadAsync<TenantAiAccessSettings>();
    }

    /// <remarks>
    /// Turns AI functionality (chat, agents, vectorization) on or off for the whole portal; AI is enabled by default.
    /// Requires Owner or DocSpaceAdmin (the EditPortalSettings permission); every other caller is refused. Disabling
    /// it immediately hides the AI Agents folder from root folder listings, makes AI status checks report disabled,
    /// and makes AI chat endpoints unreachable for every user on the tenant, not only the caller. This is a mutating,
    /// idempotent, portal-wide call, and the change is pushed to already-connected clients over the real-time
    /// notification hub rather than waiting for their next request. It returns the saved setting.
    /// </remarks>
    /// <summary>
    /// Set the AI access settings
    /// </summary>
    /// <path>api/2.0/settings/ai-access</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Saved AI access setting for the portal", typeof(TenantAiAccessSettings))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, so the AI access setting cannot be changed")]
    [HttpPost("ai-access")]
    public async Task<TenantAiAccessSettings> SetTenantAiAccessSettings(TenantAiAccessSettingsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = new TenantAiAccessSettings { Enabled = inDto.Enabled };

        await settingsManager.SaveAsync(settings);

        await quotaSocketManager.ChangeAiAccessSettingsAsync(inDto.Enabled);

        messageService.Send(inDto.Enabled ? MessageAction.AIAccessEnabled : MessageAction.AIAccessDisabled);

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


    /// <remarks>
    /// Returns whether the portal currently allows inviting new members and new guests at all. No permission is
    /// required; anonymous callers can read it too, since the invitation flow itself may run before the caller has
    /// signed in. This is a read-only, idempotent call. The response supports conditional requests: send the standard
    /// If-Modified-Since header with the previous `lastModified` value, and an unchanged response comes back empty
    /// instead of resending the same settings.
    /// </remarks>
    /// <summary>Get the user invitation settings</summary>
    /// <path>api/2.0/settings/invitationsettings</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Whether inviting new members and new guests is currently allowed", typeof(TenantUserInvitationSettingsDto))]
    [HttpGet("invitationsettings")]
    [AllowAnonymous]
    public async Task<TenantUserInvitationSettingsDto> GetTenantUserInvitationSettings()
    {
        var settings = await settingsManager.LoadAsync<TenantUserInvitationSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(settings.LastModified)
            ? null
            : settings.Map();
    }


    /// <remarks>
    /// Sets whether the portal allows inviting new members and new guests. Requires Owner or DocSpaceAdmin (the
    /// EditPortalSettings permission). Disabling member or guest invitations only blocks creating new invitations
    /// going forward; it does not revoke links already issued or remove members already invited. This is a mutating,
    /// idempotent, portal-wide call. It returns the saved setting; read the current value at any time, including
    /// anonymously, from `GET api/2.0/settings/invitationsettings`.
    /// </remarks>
    /// <summary>Update the user invitation settings</summary>
    /// <path>api/2.0/settings/invitationsettings</path>
    [Tags("Settings / Common settings")]
    [SwaggerResponse(200, "Saved user invitation settings", typeof(TenantUserInvitationSettingsDto))]
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

        messageService.Send(MessageAction.InvitationSettingsUpdated);

        return settings.Map();
    }
}
