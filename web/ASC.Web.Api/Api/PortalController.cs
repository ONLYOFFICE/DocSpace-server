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

using Microsoft.AspNetCore.RateLimiting;

using Constants = ASC.Core.Users.Constants;

namespace ASC.Web.Api.Controllers;

///<summary>
/// Portal information access.
///</summary>
///<name>portal</name>
[Scope]
[DefaultRoute]
[ApiController]
public class PortalController(
    ILogger<PortalController> logger,
    UserManager userManager,
    TenantManager tenantManager,
    ITariffService tariffService,
    CommonLinkUtility commonLinkUtility,
    IUrlShortener urlShortener,
    AuthContext authContext,
        CookiesManager cookiesManager,
    SecurityContext securityContext,
    SettingsManager settingsManager,
    IMobileAppInstallRegistrator mobileAppInstallRegistrator,
    TenantExtra tenantExtra,
    IConfiguration configuration,
    CoreBaseSettings coreBaseSettings,
    LicenseReader licenseReader,
    SetupInfo setupInfo,
    DocumentServiceLicense documentServiceLicense,
    IHttpClientFactory clientFactory,
    ApiSystemHelper apiSystemHelper,
    CoreSettings coreSettings,
    PermissionContext permissionContext,
    StudioNotifyService studioNotifyService,
    MessageService messageService,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    EmailValidationKeyProvider emailValidationKeyProvider,
    StudioSmsNotificationSettingsHelper studioSmsNotificationSettingsHelper,
    TfaAppAuthSettingsHelper tfaAppAuthSettingsHelper,
    IMapper mapper,
    IHttpContextAccessor httpContextAccessor,
    QuotaHelper quotaHelper,
    IEventBus eventBus,
    CspSettingsHelper cspSettingsHelper)
    : ControllerBase
{
    /// <summary>
    /// Returns the current portal.
    /// </summary>
    /// <short>
    /// Get a portal
    /// </short>
    /// <category>Settings</category>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.TenantDto, ASC.Web.Api">Current portal information</returns>
    /// <path>api/2.0/portal</path>
    /// <httpMethod>GET</httpMethod>
    [AllowNotPayment]
    [HttpGet("")]
    public async Task<TenantDto> Get()
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();   
        return mapper.Map<TenantDto>(tenant);
    }

    /// <summary>
    /// Returns a user with the ID specified in the request from the current portal.
    /// </summary>
    /// <short>
    /// Get a user by ID
    /// </short>
    /// <category>Users</category>
    /// <param type="System.Guid, System" method="url" name="userID">User ID</param>
    /// <returns type="ASC.Core.Users.UserInfo, ASC.Core.Common">User information</returns>
    /// <path>api/2.0/portal/users/{userID}</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("users/{userID:guid}")]
    public async Task<UserInfo> GetUserAsync(Guid userID)
    {
        return await userManager.GetUsersAsync(userID);
    }

    /// <summary>
    /// Returns an invitation link for joining the portal.
    /// </summary>
    /// <short>
    /// Get an invitation link
    /// </short>
    /// <param type="ASC.Core.Users.EmployeeType, ASC.Core.Common" method="url" name="employeeType">Employee type (All, RoomAdmin, User, DocSpaceAdmin)</param>
    /// <category>Users</category>
    /// <returns type="System.Object, System">Invitation link</returns>
    /// <path>api/2.0/portal/users/invite/{employeeType}</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("users/invite/{employeeType}")]
    public async Task<object> GeInviteLinkAsync(EmployeeType employeeType)
    {
        var currentUser = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);

        if ((employeeType == EmployeeType.DocSpaceAdmin && !currentUser.IsOwner(await tenantManager.GetCurrentTenantAsync()))
            || !await permissionContext.CheckPermissionsAsync(new UserSecurityProvider(Guid.Empty, employeeType), Constants.Action_AddRemoveUser))
        {
            return string.Empty;
        }

        var link = await commonLinkUtility.GetConfirmationEmailUrlAsync(string.Empty, ConfirmType.LinkInvite, (int)employeeType + authContext.CurrentAccount.ID.ToString(), authContext.CurrentAccount.ID)
                + $"&emplType={employeeType:d}";

        return await urlShortener.GetShortenLinkAsync(link);
    }

    /// <summary>
    /// Returns a link specified in the request in the shortened format.
    /// </summary>
    /// <short>Get a shortened link</short>
    /// <category>Settings</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.ShortenLinkRequestsDto, ASC.Web.Api" name="inDto">Shortened link request parameters</param>
    /// <returns type="System.Object, System">Shortened link</returns>
    /// <path>api/2.0/portal/getshortenlink</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("getshortenlink")]
    public async Task<object> GetShortenLinkAsync(ShortenLinkRequestsDto inDto)
    {
        try
        {
            return await urlShortener.GetShortenLinkAsync(inDto.Link);
        }
        catch (Exception ex)
        {
            logger.ErrorGetShortenLink(ex);
            return inDto.Link;
        }
    }

    /// <summary>
    /// Returns an extra tenant license for the portal.
    /// </summary>
    /// <short>
    /// Get an extra tenant license
    /// </short>
    /// <category>Quota</category>
    /// <param type="System.Boolean, System" name="refresh">Specifies whether the tariff will be refreshed</param>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto, ASC.Web.Api">Extra tenant license information</returns>
    /// <path>api/2.0/portal/tenantextra</path>
    /// <httpMethod>GET</httpMethod>
    /// <visible>false</visible>
    [AllowNotPayment]
    [HttpGet("tenantextra")]
    public async Task<TenantExtraDto> GetTenantExtra(bool refresh)
    {
        //await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var quota = await quotaHelper.GetCurrentQuotaAsync(refresh);
        var docServiceQuota = await documentServiceLicense.GetLicenseQuotaAsync();
        
        var result = new TenantExtraDto
        {
            CustomMode = coreBaseSettings.CustomMode,
            Opensource = tenantExtra.Opensource,
            Enterprise = tenantExtra.Enterprise,
            EnableTariffPage = 
                (!coreBaseSettings.Standalone || !string.IsNullOrEmpty(licenseReader.LicensePath))
                && string.IsNullOrEmpty(setupInfo.AmiMetaUrl)
                && !coreBaseSettings.CustomMode,
            Tariff = await tenantExtra.GetCurrentTariffAsync(),
            Quota = quota,
            NotPaid = await tenantExtra.IsNotPaidAsync(),
            LicenseAccept = (await settingsManager.LoadForDefaultTenantAsync<TariffSettings>()).LicenseAcceptSetting,
            DocServerUserQuota = docServiceQuota.Item1,
            DocServerLicense = docServiceQuota.Item2
        };

        return result;
    }


    /// <summary>
    /// Returns the used space of the current portal.
    /// </summary>
    /// <short>
    /// Get the used portal space
    /// </short>
    /// <category>Quota</category>
    /// <returns type="System.Double, System">Used portal space</returns>
    /// <path>api/2.0/portal/usedspace</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("usedspace")]
    public async Task<double> GetUsedSpaceAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var tenant = await tenantManager.GetCurrentTenantAsync();
        return Math.Round(
            (await tenantManager.FindTenantQuotaRowsAsync(tenant.Id))
                        .Where(q => !string.IsNullOrEmpty(q.Tag) && new Guid(q.Tag) != Guid.Empty)
                        .Sum(q => q.Counter) / 1024f / 1024f / 1024f, 2);
    }


    /// <summary>
    /// Returns a number of portal users.
    /// </summary>
    /// <short>
    /// Get a number of portal users
    /// </short>
    /// <category>Users</category>
    /// <returns type="System.Int64, System">Number of portal users</returns>
    /// <path>api/2.0/portal/userscount</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("userscount")]
    public async Task<long> GetUsersCountAsync()
    {
        return (await userManager.GetUserNamesAsync(EmployeeStatus.Active)).Length;
    }

    /// <summary>
    /// Returns the current portal tariff.
    /// </summary>
    /// <short>
    /// Get a portal tariff
    /// </short>
    /// <category>Quota</category>
    /// <param type="System.Boolean, System" name="refresh">Specifies whether the tariff will be refreshed</param>
    /// <returns type="ASC.Core.Billing.Tariff, ASC.Core.Common">Current portal tariff</returns>
    /// <path>api/2.0/portal/tariff</path>
    /// <httpMethod>GET</httpMethod>
    [AllowNotPayment]
    [HttpGet("tariff")]
    public async Task<Tariff> GetTariffAsync(bool refresh)
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();
        return await tariffService.GetTariffAsync(tenant.Id, refresh: refresh);
    }

    /// <summary>
    /// Returns the current portal quota.
    /// </summary>
    /// <short>
    /// Get a portal quota
    /// </short>
    /// <category>Quota</category>
    /// <returns type="ASC.Core.Tenants.TenantQuota, ASC.Core.Common">Current portal quota</returns>
    /// <path>api/2.0/portal/quota</path>
    /// <httpMethod>GET</httpMethod>
    [AllowNotPayment]
    [HttpGet("quota")]
    public async Task<TenantQuota> GetQuotaAsync()
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();
        return await tenantManager.GetTenantQuotaAsync(tenant.Id);
    }

    /// <summary>
    /// Returns the recommended quota for the current portal.
    /// </summary>
    /// <short>
    /// Get the recommended quota
    /// </short>
    /// <category>Quota</category>
    /// <returns type="ASC.Core.Tenants.TenantQuota, ASC.Core.Common">Recommended portal quota</returns>
    /// <path>api/2.0/portal/quota/right</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("quota/right")]
    public async Task<TenantQuota> GetRightQuotaAsync()
    {
        var usedSpace = await GetUsedSpaceAsync();
        var needUsersCount = await GetUsersCountAsync();

        return (await tenantManager.GetTenantQuotasAsync()).OrderBy(r => r.Price)
                            .FirstOrDefault(quota =>
                                            quota.CountUser > needUsersCount
                                            && quota.MaxTotalSize > usedSpace);
    }


    /// <summary>
    /// Returns the full absolute path to the current portal.
    /// </summary>
    /// <short>
    /// Get a path to the portal
    /// </short>
    /// <category>Settings</category>
    /// <param type="System.String, System" name="virtualPath">Portal virtual path</param>
    /// <returns type="System.Object, System">Portal path</returns>
    /// <path>api/2.0/portal/path</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("path")]
    public object GetFullAbsolutePath(string virtualPath)
    {
        return commonLinkUtility.GetFullAbsolutePath(virtualPath);
    }

    /// <summary>
    /// Returns a thumbnail of the bookmark URL specified in the request.
    /// </summary>
    /// <short>
    /// Get a bookmark thumbnail
    /// </short>
    /// <category>Settings</category>
    /// <param type="System.String, System" name="url">Bookmark URL</param>
    /// <returns type="Microsoft.AspNetCore.Mvc.FileResult, Microsoft.AspNetCore.Mvc">Thumbnail</returns>
    /// <path>api/2.0/portal/thumb</path>
    /// <httpMethod>GET</httpMethod>
    /// <visible>false</visible>
    [HttpGet("thumb")]
    public async Task<FileResult> GetThumb(string url)
    {
        if (!securityContext.IsAuthenticated || configuration["bookmarking:thumbnail-url"] == null)
        {
            return null;
        }

        url = url.Replace("&amp;", "&");
        url = WebUtility.UrlEncode(url);

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(string.Format(configuration["bookmarking:thumbnail-url"], url))
        };

        var httpClient = clientFactory.CreateClient();
        using var response = await httpClient.SendAsync(request);
        var bytes = await response.Content.ReadAsByteArrayAsync();

        var type = response.Headers.TryGetValues("Content-Type", out var values) ? values.First() : "image/png";
        return File(bytes, type);
    }

    /// <summary>
    /// Marks a gift message as read.
    /// </summary>
    /// <short>
    /// Mark a gift message as read
    /// </short>
    /// <category>Users</category>
    /// <returns></returns>
    /// <path>api/2.0/portal/present/mark</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("present/mark")]
    public async Task MarkPresentAsReadedAsync()
    {
        try
        {
            var settings = await settingsManager.LoadForCurrentUserAsync<OpensourceGiftSettings>();
            settings.Readed = true;
            await settingsManager.SaveForCurrentUserAsync(settings);
        }
        catch (Exception ex)
        {
            logger.ErrorMarkPresentAsReaded(ex);
        }
    }

    /// <summary>
    /// Registers the mobile app installation.
    /// </summary>
    /// <short>
    /// Register the mobile app installation
    /// </short>
    /// <category>Settings</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.MobileAppRequestsDto, ASC.Web.Api" name="inDto">Mobile app request parameters</param>
    /// <returns></returns>
    /// <path>api/2.0/portal/mobile/registration</path>
    /// <httpMethod>POST</httpMethod>
    /// <visible>false</visible>
    [HttpPost("mobile/registration")]
    public async Task RegisterMobileAppInstallAsync(MobileAppRequestsDto inDto)
    {
        var currentUser = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        await mobileAppInstallRegistrator.RegisterInstallAsync(currentUser.Email, inDto.Type);
    }

    /// <summary>
    /// Registers the mobile app installation by mobile app type.
    /// </summary>
    /// <short>
    /// Register the mobile app installation by mobile app type
    /// </short>
    /// <category>Settings</category>
    /// <param type="ASC.Core.Common.Notify.Push.MobileAppType, ASC.Core.Common" name="type">Mobile app type (IosProjects, AndroidProjects, IosDocuments, AndroidDocuments, or DesktopEditor)</param>
    /// <returns></returns>
    /// <path>api/2.0/portal/mobile/registration</path>
    /// <httpMethod>POST</httpMethod>
    /// <visible>false</visible>
    [HttpPost("mobile/registration")]
    public async Task RegisterMobileAppInstallAsync(MobileAppType type)
    {
        var currentUser = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        await mobileAppInstallRegistrator.RegisterInstallAsync(currentUser.Email, type);
    }

    /// <summary>
    /// Updates a portal name with a new one specified in the request.
    /// </summary>
    /// <short>Update a portal name</short>
    /// <category>Settings</category>
    /// <param type="ASC.Web.Api.ApiModels.RequestsDto.PortalRenameRequestsDto, ASC.Web.Api" name="inDto">Request parameters for portal renaming</param>
    /// <returns type="System.Object, System">Confirmation email about authentication to the portal with a new name</returns>
    /// <path>api/2.0/portal/portalrename</path>
    /// <httpMethod>PUT</httpMethod>
    /// <visible>false</visible>
    [HttpPut("portalrename")]
    public async Task<object> UpdatePortalName(PortalRenameRequestsDto inDto)
    {
        if (!SetupInfo.IsVisibleSettings(nameof(ManagementType.PortalSecurity)))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption, "PortalRename");
        }

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var alias = inDto.Alias;
        if (string.IsNullOrEmpty(alias))
        {
            throw new ArgumentException(nameof(alias));
        }

        var tenant = await tenantManager.GetCurrentTenantAsync();
        var user = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);

        var localhost = coreSettings.BaseDomain == "localhost" || tenant.Alias == "localhost";

        var newAlias = string.Concat(alias.Where(c => !Char.IsWhiteSpace(c))).ToLowerInvariant();
        var oldAlias = tenant.Alias;
        var oldVirtualRootPath = commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/');

        var massageDate = DateTime.UtcNow;
        if (!string.Equals(newAlias, oldAlias, StringComparison.InvariantCultureIgnoreCase))
        {
            if (!string.IsNullOrEmpty(apiSystemHelper.ApiSystemUrl))
            {
                await apiSystemHelper.ValidatePortalNameAsync(newAlias, user.Id);
            }
            else
            {
                await tenantManager.CheckTenantAddressAsync(newAlias.Trim());
            }

            var oldDomain = tenant.GetTenantDomain(coreSettings);
            tenant.Alias = alias;
            tenant = await tenantManager.SaveTenantAsync(tenant);
            tenantManager.SetCurrentTenant(tenant);
            
            await messageService.SendAsync(MessageAction.PortalRenamed, MessageTarget.Create(tenant.Id), oldAlias, newAlias, dateTime: massageDate);
            await cspSettingsHelper.RenameDomain(oldDomain, tenant.GetTenantDomain(coreSettings));

            if (!coreBaseSettings.Standalone && apiSystemHelper.ApiCacheEnable)
            {
                await apiSystemHelper.UpdateTenantToCacheAsync(oldDomain, tenant.GetTenantDomain(coreSettings));
            }

            if (!localhost || string.IsNullOrEmpty(tenant.MappedDomain))
            {
                await studioNotifyService.PortalRenameNotifyAsync(tenant, oldVirtualRootPath, oldAlias);
            }
        }
        else
        {
            return string.Empty;
        }

        var rewriter = httpContextAccessor.HttpContext.Request.Url();
        var confirmUrl = string.Format("{0}{1}{2}{3}/{4}",
                                rewriter?.Scheme ?? Uri.UriSchemeHttp,
                                Uri.SchemeDelimiter,
                                tenant.GetTenantDomain(coreSettings),
                                rewriter != null && !rewriter.IsDefaultPort ? $":{rewriter.Port}" : "",
                                commonLinkUtility.GetConfirmationUrlRelative(tenant.Id, user.Email, ConfirmType.Auth, massageDate.ToString(CultureInfo.InvariantCulture))
               );

        cookiesManager.ClearCookies(CookiesType.AuthKey);
        cookiesManager.ClearCookies(CookiesType.SocketIO);
        securityContext.Logout();

        return confirmUrl;
    }

    /// <summary>
    /// Deletes the current portal immediately.
    /// </summary>
    /// <short>Delete a portal immediately</short>
    /// <category>Settings</category>
    /// <returns></returns>
    /// <path>api/2.0/portal/deleteportalimmediately</path>
    /// <httpMethod>DELETE</httpMethod>
    /// <visible>false</visible>
    [HttpDelete("deleteportalimmediately")]
    public async Task DeletePortalImmediatelyAsync()
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();

        if (securityContext.CurrentAccount.ID != tenant.OwnerId)
        {
            throw new Exception(Resource.ErrorAccessDenied);
        }

        await tenantManager.RemoveTenantAsync(tenant.Id);

        if (!coreBaseSettings.Standalone)
        {
            await apiSystemHelper.RemoveTenantFromCacheAsync(tenant.GetTenantDomain(coreSettings));
        }

        try
        {
            if (!securityContext.IsAuthenticated)
            {
                await securityContext.AuthenticateMeWithoutCookieAsync(ASC.Core.Configuration.Constants.CoreSystem);
            }
        }
        finally
        {
            await eventBus.PublishAsync(new RemovePortalIntegrationEvent(securityContext.CurrentAccount.ID, tenant.Id));
            securityContext.Logout();
        }
    }

    /// <summary>
    /// Sends the instructions to suspend the current portal.
    /// </summary>
    /// <short>Send suspension instructions</short>
    /// <category>Settings</category>
    /// <returns></returns>
    /// <path>api/2.0/portal/suspend</path>
    /// <httpMethod>POST</httpMethod>
    [AllowNotPayment]
    [HttpPost("suspend")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task SendSuspendInstructionsAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = await tenantManager.GetCurrentTenantAsync();

        await DemandPermissionToDeleteTenantAsync(tenant);

        var owner = await userManager.GetUsersAsync(tenant.OwnerId);
        var suspendUrl = await commonLinkUtility.GetConfirmationEmailUrlAsync(owner.Email, ConfirmType.PortalSuspend);
        var continueUrl = await commonLinkUtility.GetConfirmationEmailUrlAsync(owner.Email, ConfirmType.PortalContinue);

        await studioNotifyService.SendMsgPortalDeactivationAsync(tenant, await urlShortener.GetShortenLinkAsync(suspendUrl), await urlShortener.GetShortenLinkAsync(continueUrl));

        await messageService.SendAsync(MessageAction.OwnerSentPortalDeactivationInstructions, MessageTarget.Create(owner.Id), owner.DisplayUserName(false, displayUserSettingsHelper));
    }

    /// <summary>
    /// Sends the instructions to remove the current portal.
    /// </summary>
    /// <short>Send removal instructions</short>
    /// <category>Settings</category>
    /// <returns></returns>
    /// <path>api/2.0/portal/delete</path>
    /// <httpMethod>POST</httpMethod>
    [AllowNotPayment]
    [HttpPost("delete")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task SendDeleteInstructionsAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = await tenantManager.GetCurrentTenantAsync();

        await DemandPermissionToDeleteTenantAsync(tenant);

        var owner = await userManager.GetUsersAsync(tenant.OwnerId);

        var showAutoRenewText = !coreBaseSettings.Standalone &&
                        (await tariffService.GetPaymentsAsync(tenant.Id)).Any() &&
                        !(await tenantManager.GetCurrentTenantQuotaAsync()).Trial;

        var confirmLink = await commonLinkUtility.GetConfirmationEmailUrlAsync(owner.Email, ConfirmType.PortalRemove);
            
        await studioNotifyService.SendMsgPortalDeletionAsync(tenant, await urlShortener.GetShortenLinkAsync(confirmLink), showAutoRenewText);
    }

    /// <summary>
    /// Restores the current portal.
    /// </summary>
    /// <short>Restore a portal</short>
    /// <category>Settings</category>
    /// <returns></returns>
    /// <path>api/2.0/portal/continue</path>
    /// <httpMethod>PUT</httpMethod>
    [AllowSuspended]
    [HttpPut("continue")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "PortalContinue")]
    public async Task ContinuePortalAsync()
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();
        tenant.SetStatus(TenantStatus.Active);
        await tenantManager.SaveTenantAsync(tenant);

        await cspSettingsHelper.UpdateBaseDomain();
    }

    /// <summary>
    /// Deactivates the current portal.
    /// </summary>
    /// <short>Deactivate a portal</short>
    /// <category>Settings</category>
    /// <returns></returns>
    /// <path>api/2.0/portal/suspend</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("suspend")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "PortalSuspend")]
    public async Task SuspendPortalAsync()
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();

        await DemandPermissionToDeleteTenantAsync(tenant);

        tenant.SetStatus(TenantStatus.Suspended);
        await tenantManager.SaveTenantAsync(tenant);
        await messageService.SendAsync(MessageAction.PortalDeactivated);

        await cspSettingsHelper.UpdateBaseDomain();
    }

    /// <summary>
    /// Deletes the current portal.
    /// </summary>
    /// <short>Delete a portal</short>
    /// <category>Settings</category>
    /// <returns type="System.Object, System">URL to the feedback form about removing a portal</returns>
    /// <path>api/2.0/portal/delete</path>
    /// <httpMethod>DELETE</httpMethod>
    [AllowNotPayment]
    [HttpDelete("delete")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "PortalRemove")]
    public async Task<object> DeletePortalAsync()
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();

        await DemandPermissionToDeleteTenantAsync(tenant);

        await tenantManager.RemoveTenantAsync(tenant.Id);

        if (!coreBaseSettings.Standalone)
        {
            await apiSystemHelper.RemoveTenantFromCacheAsync(tenant.GetTenantDomain(coreSettings));
        }

        var owner = await userManager.GetUsersAsync(tenant.OwnerId);
        var redirectLink = setupInfo.TeamlabSiteRedirect + "/remove-portal-feedback-form.aspx#";
        var parameters = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"firstname\":\"" + owner.FirstName +
                                                                                "\",\"lastname\":\"" + owner.LastName +
                                                                                "\",\"alias\":\"" + tenant.Alias +
                                                                                "\",\"email\":\"" + owner.Email + "\"}"));

        redirectLink += HttpUtility.UrlEncode(parameters);

        await studioNotifyService.SendMsgPortalDeletionSuccessAsync(owner, redirectLink);

        await messageService.SendAsync(MessageAction.PortalDeleted);

        await cspSettingsHelper.UpdateBaseDomain();
        await eventBus.PublishAsync(new RemovePortalIntegrationEvent(securityContext.CurrentAccount.ID, tenant.Id));

        return redirectLink;
    }

    /// <summary>
    /// Sends congratulations to the user after registering the portal.
    /// </summary>
    /// <short>Send congratulations</short>
    /// <category>Users</category>
    /// <param type="ASC.Web.Api.ApiModels.RequestsDto.SendCongratulationsDto, ASC.Web.Api" name="inDto">Congratulations request parameters</param>
    /// <returns></returns>
    /// <path>api/2.0/portal/sendcongratulations</path>
    /// <httpMethod>POST</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowAnonymous]
    [HttpPost("sendcongratulations")]
    public async Task SendCongratulationsAsync([FromQuery] SendCongratulationsDto inDto)
    {
        var authInterval = TimeSpan.FromHours(1);
        var checkKeyResult = await emailValidationKeyProvider.ValidateEmailKeyAsync(inDto.Userid.ToString() + ConfirmType.Auth, inDto.Key, authInterval);

        switch (checkKeyResult)
        {
            case ValidationResult.Ok:
                var currentUser = await userManager.GetUsersAsync(inDto.Userid);

                await studioNotifyService.SendCongratulationsAsync(currentUser);
                await studioNotifyService.SendRegDataAsync(currentUser);

                if (!SetupInfo.IsSecretEmail(currentUser.Email))
                {
                    if (setupInfo.TfaRegistration == "sms")
                    {
                        await studioSmsNotificationSettingsHelper.SetEnable(true);
                    }
                    else if (setupInfo.TfaRegistration == "code")
                    {
                        await tfaAppAuthSettingsHelper.SetEnable(true);
                    }
                }
                break;
            default:
                throw new SecurityException("Access Denied.");
        }
    }

    private async Task DemandPermissionToDeleteTenantAsync(Tenant tenant)
    {
        if (securityContext.CurrentAccount.ID != tenant.OwnerId)
        {
            throw new Exception(Resource.ErrorAccessDenied);
        }

        if (!coreBaseSettings.Standalone)
        {
            return;
        }

        var activeTenants = await tenantManager.GetTenantsAsync();
        foreach (var t in activeTenants.Where(t => t.Id != tenant.Id))
        {
            var settings = await settingsManager.LoadAsync<TenantAccessSpaceSettings>(t.Id);
            if (!settings.LimitedAccessSpace)
            {
                return;
            }
        }

        throw new Exception(Resource.ErrorCannotDeleteLastSpace);
    }
}