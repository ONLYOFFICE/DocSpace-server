// (c) Copyright Ascensio System SIA 2009-2026
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

using ASC.Core.Common.Identity;

using Microsoft.AspNetCore.RateLimiting;

using Constants = ASC.Core.Users.Constants;

namespace ASC.Web.Api.Controllers;

///<remarks>
/// Portal information access.
///</remarks>
///<name>portal</name>
[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("portal")]
public class PortalController(
    ILogger<PortalController> logger,
    UserManager userManager,
    TenantManager tenantManager,
    TenantUtil tenantUtil,
    ITariffService tariffService,
    CommonLinkUtility commonLinkUtility,
    IUrlShortener urlShortener,
    AuthContext authContext,
    CookiesManager cookiesManager,
    SecurityContext securityContext,
    SettingsManager settingsManager,
    IDistributedLockProvider distributedLockProvider,
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
    ExternalResourceSettingsHelper externalResourceSettingsHelper,
    QuotaHelper quotaHelper,
    QuotaSocketManager quotaSocketManager,
    ApiDateTimeHelper apiDateTimeHelper,
    IEventBus eventBus,
    CspSettingsHelper cspSettingsHelper,
    IdentityClient client,
    InvitationLinkDtoHelper invitationLinkDtoHelper)
    : ControllerBase
{
    /// <remarks>
    /// Returns the current portal information.
    /// </remarks>
    /// <summary>
    /// Get a portal
    /// </summary>
    /// <path>api/2.0/portal</path>
    [Tags("Portal / Settings")]
    [SwaggerResponse(200, "Current portal information", typeof(TenantDto))]
    [AllowNotPayment]
    [HttpGet("")]
    public async Task<TenantDto> GetPortalInformation()
    {
        var tenant = tenantManager.GetCurrentTenant();

        if (!await permissionContext.CheckPermissionsAsync(SecurityConstants.EditPortalSettings))
        {
            return new TenantDto { TenantId = tenant.Id };
        }

        var dto = tenant.MapToDto();

        if (!coreBaseSettings.Standalone && apiSystemHelper.ApiCacheEnable)
        {
            dto.Region = await apiSystemHelper.GetTenantRegionAsync(dto.TenantAlias);
        }
        return dto;
    }

    /// <remarks>
    /// Returns a user with the ID specified in the request from the current portal.
    /// </remarks>
    /// <summary>
    /// Get a user by ID
    /// </summary>
    /// <path>api/2.0/portal/users/{userID}</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "User information", typeof(UserInfo))]
    [SwaggerResponse(404, "The user could not be found")]
    [HttpGet("users/{userID:guid}")]
    public async Task<UserInfo> GetUserById(UserIDRequestDto inDto)
    {
        if (!await userManager.CanUserViewAnotherUserAsync(authContext.CurrentAccount.ID, inDto.Id))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var user = await userManager.GetUsersAsync(inDto.Id);

        if (userManager.IsSystemUser(user.Id))
        {
            throw new ItemNotFoundException(Resource.ErrorUserNotFound);
        }

        return user;
    }

    /// <remarks>
    /// Returns an invitation link for joining the portal.
    /// </remarks>
    /// <summary>
    /// Get an invitation link
    /// </summary>
    /// <path>api/2.0/portal/users/invite/{employeeType}</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "Invitation link", typeof(string))]
    [HttpGet("users/invite/{employeeType}")]
    [Obsolete("Use CRUD /api/2.0/portal/users/invitationlink instead")]
    public async Task<string> GetInvitationLink(InvitationLinkRequestDto inDto)
    {
        var invitationSettings = await settingsManager.LoadAsync<TenantUserInvitationSettings>();

        if (!invitationSettings.AllowInvitingMembers)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var currentUser = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);

        if ((inDto.EmployeeType == EmployeeType.DocSpaceAdmin && !currentUser.IsOwner(tenantManager.GetCurrentTenant()))
            || !await permissionContext.CheckPermissionsAsync(new UserSecurityProvider(Guid.Empty, inDto.EmployeeType), Constants.Action_AddRemoveUser))
        {
            return string.Empty;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var link = commonLinkUtility.GetConfirmationEmailUrl(string.Empty, ConfirmType.LinkInvite,
                (int)inDto.EmployeeType + authContext.CurrentAccount.ID.ToString() + tenant.Alias,
                authContext.CurrentAccount.ID) + $"&emplType={inDto.EmployeeType:d}";

        return await urlShortener.GetShortenLinkAsync(link);
    }

    /// <remarks>
    /// Returns an invitation link for joining the portal.
    /// </remarks>
    /// <summary>
    /// Create an invitation link
    /// </summary>
    /// <path>api/2.0/portal/users/invitationlink</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "Invitation link", typeof(InvitationLinkDto))]
    [HttpPost("users/invitationlink")]
    public async Task<InvitationLinkDto> CreateInvitationLink(InvitationLinkCreateRequestDto inDto)
    {
        var invitationSettings = await settingsManager.LoadAsync<TenantUserInvitationSettings>();
        if (!invitationSettings.AllowInvitingMembers)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        if (inDto.EmployeeType is not (EmployeeType.DocSpaceAdmin or EmployeeType.RoomAdmin or EmployeeType.User))
        {
            throw new ArgumentException(nameof(inDto.EmployeeType));
        }

        var expiration = DateTime.MinValue;
        if (inDto.Expiration.HasValue)
        {
            expiration = tenantUtil.DateTimeToUtc(inDto.Expiration.Value);
            if (expiration != DateTime.MinValue && expiration < DateTime.UtcNow)
            {
                throw new ArgumentException(nameof(inDto.Expiration));
            }
        }

        var tenant = tenantManager.GetCurrentTenant();
        var currentUserId = authContext.CurrentAccount.ID;

        if ((inDto.EmployeeType == EmployeeType.DocSpaceAdmin && !currentUserId.IsOwner(tenant)) ||
            !await permissionContext.CheckPermissionsAsync(new UserSecurityProvider(Guid.Empty, inDto.EmployeeType), Constants.Action_AddRemoveUser))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        await using (await distributedLockProvider.TryAcquireFairLockAsync($"invitationlink_{tenant.Id}"))
        {
            var existedInvitationLink = await userManager.GetInvitationLinkAsync(inDto.EmployeeType);
            if (existedInvitationLink != null)
            {
                throw new ArgumentException("link with the same EmployeeType already exists");
            }

            var invitationLink = await userManager.CreateInvitationLinkAsync(inDto.EmployeeType, expiration, inDto.MaxUseCount);

            var result = await invitationLinkDtoHelper.GetAsync(invitationLink, tenant.Alias, currentUserId);

            return result;
        }
    }

    /// <remarks>
    /// Returns an invitation link for joining the portal.
    /// </remarks>
    /// <summary>
    /// Get an invitation link
    /// </summary>
    /// <path>api/2.0/portal/users/invitationlink/{employeeType}</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "Invitation link", typeof(InvitationLinkDto))]
    [HttpGet("users/invitationlink/{employeeType}")]
    public async Task<InvitationLinkDto> GetInvitationLinkByEmployeeType(InvitationLinkRequestDto inDto)
    {
        if (inDto.EmployeeType is not (EmployeeType.DocSpaceAdmin or EmployeeType.RoomAdmin or EmployeeType.User))
        {
            throw new ArgumentException(nameof(inDto.EmployeeType));
        }

        var invitationSettings = await settingsManager.LoadAsync<TenantUserInvitationSettings>();
        if (!invitationSettings.AllowInvitingMembers)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var tenant = tenantManager.GetCurrentTenant();
        var currentUserId = authContext.CurrentAccount.ID;

        if ((inDto.EmployeeType == EmployeeType.DocSpaceAdmin && !currentUserId.IsOwner(tenant)) ||
            !await permissionContext.CheckPermissionsAsync(new UserSecurityProvider(Guid.Empty, inDto.EmployeeType), Constants.Action_AddRemoveUser))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var invitationLink = await userManager.GetInvitationLinkAsync(inDto.EmployeeType);
        if (invitationLink == null)
        {
            return null;
        }

        var result = await invitationLinkDtoHelper.GetAsync(invitationLink, tenant.Alias, currentUserId);

        return result;
    }

    /// <remarks>
    /// Returns an invitation link for joining the portal.
    /// </remarks>
    /// <summary>
    /// Update an invitation link
    /// </summary>
    /// <path>api/2.0/portal/users/invitationlink</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "Invitation link", typeof(InvitationLinkDto))]
    [HttpPut("users/invitationlink")]
    public async Task<InvitationLinkDto> UpdateInvitationLink(InvitationLinkUpdateRequestDto inDto)
    {
        var invitationSettings = await settingsManager.LoadAsync<TenantUserInvitationSettings>();
        if (!invitationSettings.AllowInvitingMembers)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var invitationLink = await userManager.GetInvitationLinkAsync(inDto.Id);
        if (invitationLink == null)
        {
            throw new ItemNotFoundException();
        }

        if (inDto.MaxUseCount.HasValue && inDto.MaxUseCount.Value < invitationLink.CurrentUseCount)
        {
            throw new ArgumentException(nameof(inDto.MaxUseCount));
        }

        var expiration = DateTime.MinValue;
        if (inDto.Expiration.HasValue)
        {
            expiration = tenantUtil.DateTimeToUtc(inDto.Expiration.Value);
            if (expiration != DateTime.MinValue && expiration < DateTime.UtcNow)
            {
                throw new ArgumentException(nameof(inDto.Expiration));
            }
        }

        var tenant = tenantManager.GetCurrentTenant();
        var currentUserId = authContext.CurrentAccount.ID;

        if ((invitationLink.EmployeeType == EmployeeType.DocSpaceAdmin && !currentUserId.IsOwner(tenant)) ||
            !await permissionContext.CheckPermissionsAsync(new UserSecurityProvider(Guid.Empty, invitationLink.EmployeeType), Constants.Action_AddRemoveUser))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        invitationLink.Expiration = expiration;
        invitationLink.MaxUseCount = inDto.MaxUseCount;

        await userManager.UpdateInvitationLinkAsync(invitationLink.Id, invitationLink.Expiration, invitationLink.MaxUseCount);

        var result = await invitationLinkDtoHelper.GetAsync(invitationLink, tenant.Alias, currentUserId);

        return result;
    }

    /// <summary>
    /// Deletes an invitation link.
    /// </summary>
    /// <remarks>
    /// Ensures that the current user has permission to delete the specified invitation link.
    /// Throws security or not-found exceptions if required conditions are not met.
    /// </remarks>
    /// <path>api/2.0/portal/users/invitationlink</path>
    /// <param name="inDto">The data transfer object containing the details of the invitation link to be deleted.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "Invitation link", typeof(string))]
    [HttpDelete("users/invitationlink")]
    public async Task DeleteInvitationLink(InvitationLinkDeleteRequestDto inDto)
    {
        var invitationSettings = await settingsManager.LoadAsync<TenantUserInvitationSettings>();
        if (!invitationSettings.AllowInvitingMembers)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var invitationLink = await userManager.GetInvitationLinkAsync(inDto.Id);
        if (invitationLink == null)
        {
            throw new ItemNotFoundException();
        }

        var tenant = tenantManager.GetCurrentTenant();
        var currentUserId = authContext.CurrentAccount.ID;

        if ((invitationLink.EmployeeType == EmployeeType.DocSpaceAdmin && !currentUserId.IsOwner(tenant)) ||
            !await permissionContext.CheckPermissionsAsync(new UserSecurityProvider(Guid.Empty, invitationLink.EmployeeType), Constants.Action_AddRemoveUser))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        await userManager.DeleteInvitationLinkAsync(invitationLink.Id);
    }

    /// <remarks>
    /// Returns an extra tenant license for the portal.
    /// </remarks>
    /// <summary>
    /// Get an extra tenant license
    /// </summary>
    /// <path>api/2.0/portal/tenantextra</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Portal / Quota")]
    [SwaggerResponse(200, "Extra tenant license information", typeof(TenantExtraDto))]
    [AllowNotPayment]
    [HttpGet("tenantextra")]
    public async Task<TenantExtraDto> GetTenantExtra(PortalExtraTenantRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var quota = await quotaHelper.GetCurrentQuotaAsync(inDto.Refresh);
        var docServiceQuota = await documentServiceLicense.GetLicenseQuotaAsync();

        var result = new TenantExtraDto
        {
            CustomMode = coreBaseSettings.CustomMode,
            Opensource = tenantExtra.Opensource,
            Enterprise = tenantExtra.Enterprise,
            Developer = tenantExtra.Developer,
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


    /// <remarks>
    /// Returns the used space of the current portal.
    /// </remarks>
    /// <summary>
    /// Get the portal used space
    /// </summary>
    /// <path>api/2.0/portal/usedspace</path>
    [Tags("Portal / Quota")]
    [SwaggerResponse(200, "Used portal space", typeof(double))]
    [HttpGet("usedspace")]
    public async Task<double> GetPortalUsedSpace()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var tenant = tenantManager.GetCurrentTenant();
        return Math.Round(
            (await tenantManager.FindTenantQuotaRowsAsync(tenant.Id))
                        .Where(q => !string.IsNullOrEmpty(q.Tag) && new Guid(q.Tag) != Guid.Empty)
                        .Sum(q => q.Counter) / 1024f / 1024f / 1024f, 2);
    }


    /// <remarks>
    /// Returns a number of portal users.
    /// </remarks>
    /// <summary>
    /// Get a number of portal users
    /// </summary>
    /// <path>api/2.0/portal/userscount</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "Number of portal users", typeof(long))]
    [HttpGet("userscount")]
    public async Task<long> GetPortalUsersCount()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        return (await userManager.GetUserNamesAsync(EmployeeStatus.Active)).Length;
    }

    /// <remarks>
    /// Returns the current portal tariff.
    /// </remarks>
    /// <summary>
    /// Get a portal tariff
    /// </summary>
    /// <path>api/2.0/portal/tariff</path>
    [Tags("Portal / Quota")]
    [SwaggerResponse(200, "Current portal tariff", typeof(Tariff))]
    [AllowNotPayment]
    [HttpGet("tariff")]
    public async Task<TariffDto> GetPortalTariff(CurrentPortalTariffRequestDto inDto)
    {
        var tenant = tenantManager.GetCurrentTenant();
        var source = await tariffService.GetTariffAsync(tenant.Id, refresh: inDto.Refresh);

        var result = new TariffDto
        {
            State = source.State
        };

        var currentUserType = await userManager.GetUserTypeAsync(securityContext.CurrentAccount.ID);

        if (currentUserType is EmployeeType.RoomAdmin or EmployeeType.DocSpaceAdmin)
        {
            result.DueDate = apiDateTimeHelper.Get(source.DueDate);
            result.DelayDueDate = apiDateTimeHelper.Get(source.DelayDueDate);
        }

        if (await permissionContext.CheckPermissionsAsync(SecurityConstants.EditPortalSettings))
        {
            result.Id = source.Id;
            result.OpenSource = tenantExtra.Opensource;
            result.Enterprise = tenantExtra.Enterprise;
            result.Developer = tenantExtra.Developer;
            result.CustomerId = source.CustomerId;
            result.LicenseDate = apiDateTimeHelper.Get(source.LicenseDate);
            result.Quotas = source.Quotas.Concat(source.OverdueQuotas ?? []).ToList();
        }

        return result;
    }

    /// <remarks>
    /// Returns the current portal quota.
    /// </remarks>
    /// <summary>
    /// Get a portal quota
    /// </summary>
    /// <path>api/2.0/portal/quota</path>
    [Tags("Portal / Quota")]
    [SwaggerResponse(200, "Current portal quota", typeof(TenantQuota))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [AllowNotPayment]
    [HttpGet("quota")]
    public async Task<TenantQuota> GetPortalQuota()
    {
        if (await userManager.IsGuestAsync(securityContext.CurrentAccount.ID))
        {
            throw new SecurityException();
        }

        var tenant = tenantManager.GetCurrentTenant();
        var result = await tenantManager.GetTenantQuotaAsync(tenant.Id);

        if (await userManager.IsUserAsync(authContext.CurrentAccount.ID))
        {
            result.MaxTotalSize = 0;
        }

        return result;
    }

    /// <remarks>
    /// Returns the recommended quota for the current portal.
    /// </remarks>
    /// <summary>
    /// Get the recommended quota
    /// </summary>
    /// <path>api/2.0/portal/quota/right</path>
    [Tags("Portal / Quota")]
    [SwaggerResponse(200, "Recommended portal quota", typeof(TenantQuota))]
    [HttpGet("quota/right")]
    public async Task<TenantQuota> GetRightQuota()
    {
        var usedSpace = await GetPortalUsedSpace();
        var needUsersCount = await GetPortalUsersCount();

        return (await tenantManager.GetTenantQuotasAsync()).OrderBy(r => r.Price)
                            .FirstOrDefault(quota =>
                                            quota.CountUser > needUsersCount
                                            && quota.MaxTotalSize > usedSpace
                                            && !quota.Year);
    }


    /// <remarks>
    /// Returns the full absolute path to the current portal.
    /// </remarks>
    /// <summary>
    /// Get a path to the portal
    /// </summary>
    /// <path>api/2.0/portal/path</path>
    [Tags("Portal / Settings")]
    [SwaggerResponse(200, "Portal path", typeof(object))]
    [HttpGet("path")]
    public object GetPortalPath(PortalPathRequestDto inDto)
    {
        return commonLinkUtility.GetFullAbsolutePath(inDto.VirtualPath);
    }

    /// <remarks>
    /// Returns a thumbnail for the URL specified in the request.
    /// </remarks>
    /// <summary>
    /// Get a portal thumbnail
    /// </summary>
    /// <path>api/2.0/portal/thumb</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Portal / Settings")]
    [SwaggerResponse(200, "Thumbnail", typeof(FileResult))]
    [HttpGet("thumb")]
    public async Task<FileResult> GetThumb(PortalThumbnailRequestDto inDto)
    {
        if (!securityContext.IsAuthenticated || configuration["bookmarking:thumbnail-url"] == null)
        {
            return null;
        }

        inDto.Url = inDto.Url.Replace("&amp;", "&");
        inDto.Url = WebUtility.UrlEncode(inDto.Url);

        using var request = new HttpRequestMessage(HttpMethod.Get, string.Format(configuration["bookmarking:thumbnail-url"], inDto.Url));
#pragma warning disable CA2000
        var httpClient = clientFactory.CreateClient();
#pragma warning restore CA2000
        using var response = await httpClient.SendAsync(request);
        var bytes = await response.Content.ReadAsByteArrayAsync();

        var type = response.Headers.TryGetValues("Content-Type", out var values) ? values.First() : "image/png";
        return File(bytes, type);
    }

    /// <remarks>
    /// Marks a gift message as read.
    /// </remarks>
    /// <summary>
    /// Mark a gift message as read
    /// </summary>
    /// <path>api/2.0/portal/present/mark</path>
    [Tags("Portal / Users")]
    [HttpPost("present/mark")]
    public async Task MarkGiftMessageAsRead()
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

    /// <remarks>
    /// Registers the mobile application installation by its type.
    /// </remarks>
    /// <summary>
    /// Register the mobile app installation by its type
    /// </summary>
    /// <path>api/2.0/portal/mobile/registration</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Portal / Settings")]
    [HttpPost("mobile/registration")]
    public async Task RegisterMobileAppInstall(PortalMobileAppRequestDto inDto)
    {
        var currentUser = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        await mobileAppInstallRegistrator.RegisterInstallAsync(currentUser.Email, inDto.Type);
    }

    /// <remarks>
    /// Updates a portal name with a new one specified in the request.
    /// </remarks>
    /// <summary>Update a portal name</summary>
    /// <path>api/2.0/portal/portalrename</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Portal / Settings")]
    [SwaggerResponse(200, "Confirmation email about authentication to the portal with a new name", typeof(string))]
    [SwaggerResponse(400, "Alias is empty")]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpPut("portalrename")]
    public async Task<string> UpdatePortalName(PortalRenameRequestsDto inDto)
    {
        if (!SetupInfo.IsVisibleSettings(nameof(ManagementType.PortalSecurity)))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }

        if (!coreBaseSettings.Standalone && !(await tenantManager.GetCurrentTenantQuotaAsync()).Customization)
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var alias = inDto.Alias;
        if (string.IsNullOrEmpty(alias) || alias.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException(nameof(alias));
        }

        var tenant = tenantManager.GetCurrentTenant();
        var user = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);

        var localhost = coreSettings.BaseDomain == "localhost" || tenant.Alias == "localhost";

        var newAlias = alias.Trim().ToLowerInvariant();
        var oldAlias = tenant.Alias;
        var oldVirtualRootPath = commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/');

        var now = DateTime.UtcNow;
        var messageDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);
        if (!string.Equals(newAlias, oldAlias, StringComparison.InvariantCultureIgnoreCase))
        {
            try
            {
                if (!string.IsNullOrEmpty(apiSystemHelper.ApiSystemUrl))
                {
                    await apiSystemHelper.ValidatePortalNameAsync(newAlias, user.Id);
                }
                else
                {
                    await tenantManager.CheckTenantAddressAsync(newAlias.Trim());
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message, nameof(alias));
            }

            var oldDomain = tenant.GetTenantDomain(coreSettings);
            tenant.Alias = newAlias;
            tenant = await tenantManager.SaveTenantAsync(tenant);
            tenantManager.SetCurrentTenant(tenant);

            messageService.Send(MessageAction.PortalRenamed, MessageTarget.Create(tenant.Id), oldAlias, newAlias, dateTime: messageDate);
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

        var rewriter = HttpContext.Request.Url();

        var baseUrl = string.Format("{0}{1}{2}{3}",
                                rewriter?.Scheme ?? Uri.UriSchemeHttp,
                                Uri.SchemeDelimiter,
                                tenant.GetTenantDomain(coreSettings),
                                rewriter != null && !rewriter.IsDefaultPort ? $":{rewriter.Port}" : "");

        var confirmUrl = string.Format("{0}/{1}",
                                baseUrl,
                                commonLinkUtility.GetConfirmationUrlRelative(tenant.Id, user.Email, ConfirmType.Auth, messageDate.ToString(CultureInfo.InvariantCulture)));

        var users = (await userManager.GetUsersAsync(EmployeeStatus.Active))
                .Where(u => u.Id != user.Id);

        foreach (var u in users)
        {
            await quotaSocketManager.LogoutSession(u.Id, 0, baseUrl);
        }

        cookiesManager.ClearCookies(CookiesType.AuthKey);
        cookiesManager.ClearCookies(CookiesType.SocketIO);
        securityContext.Logout();

        return confirmUrl;
    }

    /// <remarks>
    /// Deletes the current portal immediately.
    /// </remarks>
    /// <summary>Delete a portal immediately</summary>
    /// <path>api/2.0/portal/deleteportalimmediately</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Portal / Settings")]
    [HttpDelete("deleteportalimmediately")]
    public async Task<string> DeletePortalImmediately()
    {
        var tenant = tenantManager.GetCurrentTenant();

        await DemandPermissionToDeleteTenantAsync(tenant);

        var user = await userManager.GetUsersAsync(tenant.OwnerId);

        if (!SetupInfo.IsSecretEmail(user.Email))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        return await DeletePortal();
    }

    /// <remarks>
    /// Sends the instructions to suspend the current portal.
    /// </remarks>
    /// <summary>Send suspension instructions</summary>
    /// <path>api/2.0/portal/suspend</path>
    [Tags("Portal / Settings")]
    [AllowNotPayment]
    [HttpPost("suspend")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task SendSuspendInstructions()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();

        await DemandPermissionToDeleteTenantAsync(tenant);

        var owner = await userManager.GetUsersAsync(tenant.OwnerId);
        var suspendUrl = commonLinkUtility.GetConfirmationEmailUrl(owner.Email, ConfirmType.PortalSuspend);
        var continueUrl = commonLinkUtility.GetConfirmationEmailUrl(owner.Email, ConfirmType.PortalContinue);

        await studioNotifyService.SendMsgPortalDeactivationAsync(tenant, await urlShortener.GetShortenLinkAsync(suspendUrl), await urlShortener.GetShortenLinkAsync(continueUrl));

        messageService.Send(MessageAction.OwnerSentPortalDeactivationInstructions, MessageTarget.Create(owner.Id), owner.DisplayUserName(false, displayUserSettingsHelper));
    }

    /// <remarks>
    /// Sends the instructions to remove the current portal.
    /// </remarks>
    /// <summary>Send removal instructions</summary>
    /// <path>api/2.0/portal/delete</path>
    [Tags("Portal / Settings")]
    [AllowNotPayment]
    [HttpPost("delete")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task SendDeleteInstructions()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();

        await DemandPermissionToDeleteTenantAsync(tenant);

        var owner = await userManager.GetUsersAsync(tenant.OwnerId);

        var showAutoRenewText = !coreBaseSettings.Standalone &&
                        (await tariffService.GetPaymentsAsync(tenant.Id)).Any() &&
                        !(await tenantManager.GetCurrentTenantQuotaAsync()).Trial;

        var confirmLink = commonLinkUtility.GetConfirmationEmailUrl(owner.Email, ConfirmType.PortalRemove);

        await studioNotifyService.SendMsgPortalDeletionAsync(tenant, await urlShortener.GetShortenLinkAsync(confirmLink), showAutoRenewText);
    }

    /// <remarks>
    /// Restores the current portal.
    /// </remarks>
    /// <summary>Restore a portal</summary>
    /// <path>api/2.0/portal/continue</path>
    [Tags("Portal / Settings")]
    [AllowSuspended]
    [HttpPut("continue")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "PortalContinue")]
    public async Task ContinuePortal()
    {
        var tenant = tenantManager.GetCurrentTenant();
        tenant.SetStatus(TenantStatus.Active);
        await tenantManager.SaveTenantAsync(tenant);

        await cspSettingsHelper.UpdateBaseDomain();
    }

    /// <remarks>
    /// Deactivates the current portal.
    /// </remarks>
    /// <summary>Deactivate a portal</summary>
    /// <path>api/2.0/portal/suspend</path>
    [Tags("Portal / Settings")]
    [HttpPut("suspend")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "PortalSuspend")]
    public async Task SuspendPortal()
    {
        var tenant = tenantManager.GetCurrentTenant();

        await DemandPermissionToDeleteTenantAsync(tenant);

        tenant.SetStatus(TenantStatus.Suspended);
        await tenantManager.SaveTenantAsync(tenant);
        messageService.Send(MessageAction.PortalDeactivated);

        await cspSettingsHelper.UpdateBaseDomain();
    }

    /// <remarks>
    /// Deletes the current portal.
    /// </remarks>
    /// <summary>Delete a portal</summary>
    /// <path>api/2.0/portal/delete</path>
    [Tags("Portal / Settings")]
    [SwaggerResponse(200, "URL to the feedback form about removing a portal", typeof(string))]
    [AllowNotPayment]
    [HttpDelete("delete")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "PortalRemove")]
    public async Task<string> DeletePortal()
    {
        var tenant = tenantManager.GetCurrentTenant();

        await DemandPermissionToDeleteTenantAsync(tenant);

        var tenantDomain = tenant.GetTenantDomain(coreSettings);

        var tariff = await tariffService.GetTariffAsync(tenant.Id);
        var quota = await tenantManager.GetTenantQuotaAsync(tenant.Id);

        await client.DeleteTenantClientsAsync();
        await tenantManager.RemoveTenantAsync(tenant);

        if (!coreBaseSettings.Standalone && apiSystemHelper.ApiCacheEnable)
        {
            await apiSystemHelper.RemoveTenantFromCacheAsync(tenantDomain);
        }

        var owner = await userManager.GetUsersAsync(tenant.OwnerId);

        var redirectLink = externalResourceSettingsHelper.Site.GetRegionalFullEntry("registrationcanceled");

        await studioNotifyService.SendMsgPortalDeletionSuccessAsync(owner, redirectLink);

        messageService.Send(MessageAction.PortalDeleted);

        await cspSettingsHelper.UpdateBaseDomain();

        if (!coreBaseSettings.Standalone && !quota.Free && tariff.State >= TariffState.Paid)
        {
            var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
            await studioNotifyService.SendMsgPaidPortalDeletedToSupportAsync(tenantDomain, owner, customerInfo);
        }

        await eventBus.PublishAsync(new RemovePortalIntegrationEvent(securityContext.CurrentAccount.ID, tenant.Id));

        return redirectLink;
    }

    /// <remarks>
    /// Sends congratulations to the user after registering a portal.
    /// </remarks>
    /// <summary>Send congratulations</summary>
    /// <path>api/2.0/portal/sendcongratulations</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [AllowAnonymous]
    [HttpPost("sendcongratulations")]
    public async Task SendCongratulations([FromQuery] SendCongratulationsDto inDto)
    {
        var authInterval = TimeSpan.FromHours(1);
        var checkKeyResult = emailValidationKeyProvider.ValidateEmailKey(inDto.Userid.ToString() + ConfirmType.Auth, inDto.Key, authInterval);

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

    /// <remarks>
    /// Sends the instructions to remove a portal of a user with the ID specified in the request.
    /// </remarks>
    /// <summary>Send removal instructions to the user</summary>
    /// <path>api/2.0/portal/sendremoveinstructions</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [AllowAnonymous]
    [HttpPost("sendremoveinstructions")]
    public async Task SendRemoveInstructions([FromQuery] SendRemoveInstructionsDto inDto)
    {
        var checkKeyResult = ValidationResult.Invalid;
        var tenant = tenantManager.GetCurrentTenant();
        var authInterval = TimeSpan.FromHours(1);

        if (coreBaseSettings.Standalone && tenant.OwnerId == inDto.Userid)
        {
            checkKeyResult = emailValidationKeyProvider.ValidateEmailKey(inDto.Userid.ToString() + ConfirmType.PortalRemove, inDto.Key, authInterval);
        }

        if (checkKeyResult != ValidationResult.Ok)
        {
            throw new SecurityException("Access Denied.");
        }

        var owner = await userManager.GetUsersAsync(tenant.OwnerId);
        var confirmLink = commonLinkUtility.GetConfirmationEmailUrl(owner.Email, ConfirmType.PortalRemove);

        await studioNotifyService.SendMsgPortalDeletionAsync(tenant, await urlShortener.GetShortenLinkAsync(confirmLink), false, false);
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
