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

using ASC.Core.Common.Identity;

using Microsoft.AspNetCore.RateLimiting;

using Constants = ASC.Core.Users.Constants;

namespace ASC.Web.Api.Controllers;

/// <remarks>
/// Portal-wide information and lifecycle: what this portal is - its name, alias, owner, status, time zone and
/// creation date - how much space it has used, which tariff and quota it runs on and what the next payments for them
/// will be, the invitation links that let new members join it, and the operations that deactivate, restore or remove
/// the portal itself. Most reading operations here need the portal-settings right, held by a DocSpace administrator:
/// `GET api/2.0/portal` answers a caller without it with the portal ID alone, the used-space, user-count and quota
/// operations refuse such a caller, and `GET api/2.0/portal/tariff` answers everyone but fills the fewer fields the
/// fewer rights the caller has. Invitation links live under `api/2.0/portal/users/invitationlink`, one link per
/// invited role, and work only while inviting members is allowed by the portal setting that
/// `GET api/2.0/settings/invitationsettings` reports. Deactivating and removing the portal is a two-step flow: an
/// operation here mails a confirmation link to the portal owner, and that link, not an authentication token,
/// authorizes the operation that changes the portal status. Every other portal setting lives under
/// `api/2.0/settings`.
/// </remarks>
///<name>portal</name>
[Scope]
[ApiEndpoint("portal")]
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
    InvitationLinkDtoHelper invitationLinkDtoHelper,
    CountPaidUserChecker countPaidUserChecker)
    : ControllerBase
{
    /// <remarks>
    /// Returns the portal the request was addressed to - the tenant behind the current domain - with its name, alias,
    /// owner, language, time zone, industry, trusted-domain rules, version and creation date. Nothing has to be
    /// called first, the call is read-only and idempotent, and it keeps answering while the portal's payment has
    /// lapsed. What comes back depends on the caller's rights: a caller with the portal-settings right gets the whole
    /// record, while every other user gets an object in which only `tenantId` is filled and no error is raised - so
    /// check `tenantAlias` for null before reading the rest. `status` says whether the portal is active, suspended or
    /// pending removal, and `creationDateTime`, `statusChangeDate`, `lastModified` and `versionChanged` are UTC.
    /// `region` names the data-center region a hosted portal is served from and stays empty on a server installation
    /// and when the portal cache is off, while `hostedRegion` is the region written on the record itself. The
    /// settings of the same portal are read with `GET api/2.0/settings`, its tariff with `GET api/2.0/portal/tariff`
    /// and its quota with `GET api/2.0/portal/quota`.
    /// </remarks>
    /// <summary>
    /// Get portal information
    /// </summary>
    /// <path>api/2.0/portal</path>
    [Tags("Portal / Settings")]
    [SwaggerResponse(200, "The portal record, or an object in which only `tenantId` is filled when the caller has no portal-settings right", typeof(TenantDto))]
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
    /// Returns one user of this portal, addressed by ID, in the shape the portal stores the account: display name,
    /// e-mail, contacts, role and status flags, and the dates of the profile. Nothing has to be called first, and the
    /// call is read-only and idempotent. Who may be read is decided per pair of accounts: a caller always reads their
    /// own profile, a DocSpace administrator reads anyone, a room administrator reads anyone except a guest they have
    /// no relation with, and a user or a guest reads nobody but themselves - a pair that is not allowed is refused.
    /// An ID that belongs to no account of this portal and an ID of a system account are both answered as not found,
    /// so a 404 does not tell the two apart. `userID` in the path has to be a GUID; the calling user's own profile is
    /// easier to fetch with `GET api/2.0/people/@self`. This operation hands back the internal user record - use
    /// `GET api/2.0/people/{userid}` for the same user in the People format, with the group, quota and access
    /// information a client usually needs.
    /// </remarks>
    /// <summary>
    /// Get a portal user
    /// </summary>
    /// <path>api/2.0/portal/users/{userID}</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "The account of this portal, in the internal user format", typeof(UserInfo))]
    [SwaggerResponse(404, "No account with this ID exists on the portal, or the ID belongs to a system account")]
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
    /// Deprecated - use `POST api/2.0/portal/users/invitationlink` and the neighbouring operations under that path,
    /// which store the link and let it be read, changed and revoked. Builds a shortened URL that lets whoever opens
    /// it join this portal with the role given in the path, and returns it as a bare string; nothing is stored, so
    /// the link can afterwards be neither listed nor withdrawn. Inviting members has to be enabled for the portal -
    /// `GET api/2.0/settings/invitationsettings` reports that - otherwise the call is refused. The caller needs the
    /// right to add users of the requested role and only the portal owner may ask for a DocSpace administrator link;
    /// a caller without that right gets an empty string instead of an error, so treat an empty answer as a refusal.
    /// The call changes nothing on the portal and may be repeated, each time returning an equivalent link. The URL
    /// carries a confirmation key bound to the calling account and the portal alias; it has no use limit and stops
    /// being accepted once the portal's e-mail key lifetime has passed, seven days by default - neither of the two
    /// can be set per link, which is what the replacement operations add.
    /// </remarks>
    /// <summary>
    /// Get a legacy invitation link
    /// </summary>
    /// <path>api/2.0/portal/users/invite/{employeeType}</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "The invitation URL to hand to the invited person, or an empty string when the caller may not invite that role", typeof(string))]
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
    /// Creates the portal's invitation link for one role and returns it together with the URL to share. A portal
    /// keeps at most one link per role, so a call for a role that already has one is refused - read the existing link
    /// with `GET api/2.0/portal/users/invitationlink/{employeeType}` and change it with
    /// `PUT api/2.0/portal/users/invitationlink` instead. Inviting members has to be enabled for the portal
    /// (`GET api/2.0/settings/invitationsettings`), `employeeType` has to be `DocSpaceAdmin`, `RoomAdmin` or `User`,
    /// and `expiration`, when given, has to lie in the future and is read in the portal time zone. The caller needs
    /// the right to add users of that role, only the portal owner may create the DocSpace administrator link, and a
    /// link for a paying role additionally needs a free paid seat in the portal quota. The call is mutating and not
    /// idempotent. The answer carries the `id` needed to update or delete the link, the shortened `url`,
    /// `maxUseCount` and `currentUseCount`, `expiration` in the portal time zone - empty for a link that never
    /// expires - and `isExpired`.
    /// </remarks>
    /// <summary>
    /// Create an invitation link
    /// </summary>
    /// <path>api/2.0/portal/users/invitationlink</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "The invitation link as it was created, with the `id` to address it later and the `url` to share", typeof(InvitationLinkDto))]
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

        if (inDto.EmployeeType is EmployeeType.RoomAdmin or EmployeeType.DocSpaceAdmin)
        {
            await countPaidUserChecker.CheckAppend();
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
    /// Returns the portal's invitation link for one role - the URL to share, how long it lasts and how often it has
    /// already been used. Inviting members has to be enabled for the portal
    /// (`GET api/2.0/settings/invitationsettings`) and `employeeType` has to be `DocSpaceAdmin`, `RoomAdmin` or
    /// `User`; the caller needs the right to add users of that role, only the portal owner may read the DocSpace
    /// administrator link, and a link for a paying role is shown only while the portal quota still has a free paid
    /// seat. The call is read-only and idempotent, but the `url` it returns is signed for the calling account, so two
    /// administrators are handed two different URLs for one and the same link. A role that has no link yet is
    /// answered with an empty body and 200 rather than a 404 - create the link with
    /// `POST api/2.0/portal/users/invitationlink`. `expiration` is in the portal time zone and empty for a link
    /// without a deadline, `isExpired` says whether that deadline has passed, and `currentUseCount` counts how many
    /// accounts have already joined through the link.
    /// </remarks>
    /// <summary>
    /// Get an invitation link by role
    /// </summary>
    /// <path>api/2.0/portal/users/invitationlink/{employeeType}</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "The invitation link of that role, or an empty body when the portal has no link for it", typeof(InvitationLinkDto))]
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

        if (inDto.EmployeeType is EmployeeType.RoomAdmin or EmployeeType.DocSpaceAdmin)
        {
            await countPaidUserChecker.CheckAppend();
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
    /// Changes the deadline and the use limit of an existing invitation link, addressed by its `id`. The role of a
    /// link cannot be changed - delete it and create a link for the other role instead. Inviting members has to be
    /// enabled for the portal (`GET api/2.0/settings/invitationsettings`), the link has to exist, and `maxUseCount`
    /// may not be lower than the number of uses the link already has, which
    /// `GET api/2.0/portal/users/invitationlink/{employeeType}` reports as `currentUseCount`. An `expiration` in the
    /// past is refused; the body is applied as a whole, so omitting `expiration` clears the deadline and omitting
    /// `maxUseCount` removes the use limit. The caller needs the right to add users of the link's role and only the
    /// portal owner may change the DocSpace administrator link. The call is mutating, and repeating it with the same
    /// body leaves the link as it is. The whole link comes back as it now stands, with `url` signed for the calling
    /// account - the URL therefore differs between administrators while the link behind it is the same.
    /// </remarks>
    /// <summary>
    /// Update an invitation link
    /// </summary>
    /// <path>api/2.0/portal/users/invitationlink</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "The invitation link as it now stands, with the deadline and the use limit that were applied", typeof(InvitationLinkDto))]
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

    /// <remarks>
    /// Deletes the portal's invitation link with the given `id`, so the URL shared from it stops letting anyone in;
    /// accounts that already joined through it are not touched. Inviting members has to be enabled for the portal
    /// (`GET api/2.0/settings/invitationsettings`) and the link has to exist - a second call with the same `id` is
    /// answered as not found. The caller needs the right to add users of the link's role, and only the portal owner
    /// may delete the DocSpace administrator link. The call is destructive and cannot be undone: a link for the same
    /// role has to be created again with `POST api/2.0/portal/users/invitationlink`, and it gets a new `id`, a new
    /// URL and a `currentUseCount` that starts from zero. Nothing is returned in the body. To stop invitations
    /// without losing the links, switch inviting members off for the whole portal with
    /// `PUT api/2.0/settings/invitationsettings` - the links then stay stored but are refused until it is switched on
    /// again.
    /// </remarks>
    /// <summary>
    /// Delete an invitation link
    /// </summary>
    /// <path>api/2.0/portal/users/invitationlink</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "The invitation link is deleted and its URL no longer lets anyone join the portal", typeof(string))]
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
    /// Returns how much space the content of this portal occupies, in gigabytes rounded to two decimals, so a client
    /// can show the storage bar next to the allowance. The caller needs the portal-settings right and is refused
    /// without it; the call is read-only and idempotent. The number is added up from the storage counters the portal
    /// keeps per owner, which means content that belongs to no account - system data - is not part of it, and it is a
    /// plain number, not an object. The counters are maintained as files are written and removed, so the value is
    /// current but may lag a large operation that is still running. The allowance to compare it with is
    /// `maxTotalSize` from `GET api/2.0/portal/quota`, in bytes rather than gigabytes, and the smallest quota that
    /// would still fit the portal is suggested by `GET api/2.0/portal/quota/right`. This operation says nothing about
    /// which room or user the space belongs to - the per-user figures come from the People API.
    /// </remarks>
    /// <summary>
    /// Get the portal used space
    /// </summary>
    /// <path>api/2.0/portal/usedspace</path>
    [Tags("Portal / Quota")]
    [SwaggerResponse(200, "The space the portal content occupies, in gigabytes rounded to two decimals", typeof(double))]
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
    /// Returns how many accounts this portal currently has in the active state, whatever their role, so a client can
    /// show the seat usage next to the allowance. Accounts that were invited but have not joined yet and accounts
    /// that were disabled or removed are not counted. The caller needs the portal-settings right and is refused
    /// without it; the call is read-only and idempotent, and the number moves as soon as an account joins, is
    /// disabled or is deleted. The answer is a plain number, not an object. Compare it with `countUser` and
    /// `countPaidUser` from `GET api/2.0/portal/quota` to see how much of the allowance is left, and with
    /// `GET api/2.0/portal/quota/right` for the smallest quota that would still hold everyone. When the accounts
    /// themselves are needed, and not only how many there are, list them with the People API instead - this operation
    /// cannot filter by role, group or status.
    /// </remarks>
    /// <summary>
    /// Get a number of portal users
    /// </summary>
    /// <path>api/2.0/portal/userscount</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "The number of accounts of this portal that are in the active state", typeof(long))]
    [HttpGet("userscount")]
    public async Task<long> GetPortalUsersCount()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        return (await userManager.GetUserNamesAsync(EmployeeStatus.Active)).Length;
    }

    /// <remarks>
    /// Returns the tariff this portal runs on: its state, the end of the current period and the quotas - the plan and
    /// its add-ons - it is made of. Nothing has to be called first, the call is read-only and idempotent, and it
    /// keeps answering while the portal's payment has lapsed, which is what a client needs in order to show a payment
    /// warning. How much of it is filled depends on the caller: every user gets `state`, which is `Trial`, `Paid`,
    /// `Delay` for the grace period after the due date, or `NotPaid`; a room or DocSpace administrator also gets
    /// `dueDate` and `delayDueDate`; and a caller with the portal-settings right additionally gets `id`,
    /// `customerId`, `licenseDate`, the `openSource`, `enterprise` and `developer` flags and `quotas`, each entry
    /// naming the quota, its quantity, its own due date and the quota it switches to next period. Dates are in the
    /// portal time zone. Pass `refresh=true` to re-read the tariff from the billing system instead of the portal
    /// cache - it is slower, so use it after a payment, not on every page. What the next period will cost is listed
    /// by `GET api/2.0/portal/tariff/upcoming`.
    /// </remarks>
    /// <summary>
    /// Get the portal tariff
    /// </summary>
    /// <path>api/2.0/portal/tariff</path>
    [Tags("Portal / Quota")]
    [SwaggerResponse(200, "The tariff of this portal, filled as far as the rights of the caller allow", typeof(TariffDto))]
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
            result.Quotas = source.Quotas.Concat(source.OverdueQuotas ?? [])
                .Select(q => new TariffQuotaDto(q, source.DueDate, apiDateTimeHelper)).ToList();
        }

        return result;
    }

    /// <remarks>
    /// Lists what this portal will be charged next for the quotas of its current tariff - one entry per quota that is
    /// going to be billed, with the amount, the currency and the due date. The caller needs the portal-settings right
    /// and gets 403 without it; the call is read-only and idempotent and keeps answering while the portal's payment
    /// has lapsed. Only quotas that are really charged appear: an overdue quota is skipped, and so is a quota that
    /// has no price of its own, such as a trial or a free plan - which is why the list can come back empty on a
    /// portal that does have a tariff. When a switch to another quota is scheduled for the next period, the entry
    /// describes that next quota and its quantity, so `id` and `name` may differ from what
    /// `GET api/2.0/portal/tariff` reports for today. `amount` is the unit price multiplied by `quantity`, in the
    /// currency named by `currency` as an ISO 4217 code, `dueDate` is in the portal time zone, and `wallet` marks a
    /// service paid from the portal wallet instead of the subscription.
    /// </remarks>
    /// <summary>
    /// Get upcoming payments
    /// </summary>
    /// <path>api/2.0/portal/tariff/upcoming</path>
    /// <collection>list</collection>
    [Tags("Portal / Quota")]
    [SwaggerResponse(200, "The charges the portal is going to be billed next, one entry per quota, empty when nothing is due", typeof(IEnumerable<UpcomingPaymentDto>))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [AllowNotPayment]
    [HttpGet("tariff/upcoming")]
    public async Task<List<UpcomingPaymentDto>> GetUpcomingPayments(CurrentPortalTariffRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();
        var source = await tariffService.GetTariffAsync(tenant.Id, refresh: inDto.Refresh);

        var result = new List<UpcomingPaymentDto>();

        foreach (var quota in source.Quotas)
        {
            if (quota.State == QuotaState.Overdue)
            {
                continue;
            }

            // when a switch to a different quota is scheduled, the upcoming payment is for that quota, not the current one
            var quotaId = quota.NextQuota ?? quota.Id;

            var definition = await tenantManager.GetTenantQuotaAsync(quotaId);
            if (definition == null || definition.TenantId != quotaId || definition.Price <= 0)
            {
                continue;
            }

            var quantity = quota.NextQuantity ?? quota.Quantity;

            var (_, title, unitOfMeasure) = WalletServiceDescriptionManager.GetServiceTitleAndUom(definition.ServiceName ?? definition.Name, []);

            result.Add(new UpcomingPaymentDto
            {
                Id = quotaId,
                Name = definition.Name,
                Title = title,
                UnitOfMeasure = unitOfMeasure,
                Quantity = quantity,
                Wallet = quota.Wallet,
                DueDate = apiDateTimeHelper.Get(quota.DueDate ?? source.DueDate),
                Amount = definition.Price * quantity,
                Currency = definition.PriceISOCurrencySymbol
            });
        }

        return result;
    }

    /// <remarks>
    /// Returns the quota this portal runs on - the allowance its tariff grants: how many users and paid users it may
    /// have, how many rooms, the largest total and single-file size, the price of the quota and the feature flags
    /// that go with it. The caller needs the portal-settings right and gets 403 without it; the call is read-only and
    /// idempotent. Sizes are in bytes, and `maxTotalSize` comes back as `0` when the calling account's own role is
    /// user, rather than as the real allowance. This is what the portal is allowed, not what it consumes: the
    /// consumption is reported by `GET api/2.0/portal/usedspace` in gigabytes and by `GET api/2.0/portal/userscount`.
    /// The quotas the portal could move to are listed by `GET api/2.0/portal/payment/quotas`, and
    /// `GET api/2.0/portal/quota/right` picks the smallest of them that would still fit. A free or trial quota
    /// carries no price, and the billing state that goes with the quota - paid, in grace period or not paid - is read
    /// from `GET api/2.0/portal/tariff`.
    /// </remarks>
    /// <summary>
    /// Get the portal quota
    /// </summary>
    /// <path>api/2.0/portal/quota</path>
    [Tags("Portal / Quota")]
    [SwaggerResponse(200, "The allowance the current tariff grants this portal, with sizes in bytes", typeof(TenantQuota))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [AllowNotPayment]
    [HttpGet("quota")]
    public async Task<TenantQuota> GetPortalQuota()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();
        var result = await tenantManager.GetTenantQuotaAsync(tenant.Id);

        if (await userManager.IsUserAsync(authContext.CurrentAccount.ID))
        {
            result.MaxTotalSize = 0;
        }

        return result;
    }

    /// <remarks>
    /// Recommends the cheapest quota this portal could run on and still fit: the lowest-priced quota that is not
    /// billed yearly, whose user allowance is above the number of active accounts and whose storage allowance is
    /// above the space already used. The caller needs the portal-settings right and gets 403 without it. The call is
    /// read-only, idempotent and buys nothing - it only picks one quota out of those the portal may switch to,
    /// comparing them with the figures that `GET api/2.0/portal/userscount` and `GET api/2.0/portal/usedspace`
    /// report. The answer is a single quota in the same shape as `GET api/2.0/portal/quota`, with sizes in bytes;
    /// when no quota is large enough the answer is an empty body with 200 and not an error, so handle the empty
    /// result as nothing to recommend. Yearly quotas are left out by design, so the recommendation is always a
    /// monthly one - the full list to choose from comes from `GET api/2.0/portal/payment/quotas`, and the purchase
    /// itself is started with `PUT api/2.0/portal/payment/url`.
    /// </remarks>
    /// <summary>
    /// Get the recommended quota
    /// </summary>
    /// <path>api/2.0/portal/quota/right</path>
    [Tags("Portal / Quota")]
    [SwaggerResponse(200, "The cheapest monthly quota that would still fit this portal, or an empty body when none does", typeof(TenantQuota))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("quota/right")]
    public async Task<TenantQuota> GetRightQuota()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var usedSpace = await GetPortalUsedSpace();
        var needUsersCount = await GetPortalUsersCount();

        return (await tenantManager.GetTenantQuotasAsync()).OrderBy(r => r.Price)
                            .FirstOrDefault(quota =>
                                            quota.CountUser > needUsersCount
                                            && quota.MaxTotalSize > usedSpace
                                            && !quota.Year);
    }


    /// <remarks>
    /// Turns a portal-relative path into the absolute URL a client can open, filling in the scheme, the current
    /// portal domain and the virtual root the portal is hosted on. Any signed-in user may call it, nothing has to be
    /// called first, and the call is read-only and idempotent - it neither checks that the path exists nor that the
    /// caller is allowed to open it. `virtualPath` is taken as it is: an omitted or empty value yields the portal
    /// root, a value starting with `/` is appended to that root, a value starting with `~/` is resolved against the
    /// virtual root, and a value that already starts with `http://`, `https://` or `mailto:` is handed back
    /// unchanged. The answer is a bare JSON string. The domain in the result is the one the portal answers on right
    /// now, so a renamed portal starts returning the new domain without any change on the client. Use it to build
    /// links that have to survive a rename; the portal's own addresses and settings are read from
    /// `GET api/2.0/settings` instead.
    /// </remarks>
    /// <summary>
    /// Get a path to the portal
    /// </summary>
    /// <path>api/2.0/portal/path</path>
    [Tags("Portal / Settings")]
    [SwaggerResponse(200, "The absolute URL that the given portal-relative path resolves to", typeof(string))]
    [HttpGet("path")]
    public string GetPortalPath(PortalPathRequestDto inDto)
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
    /// Marks the open-source gift message - the notice a server installation shows about its free edition - as read
    /// for the calling user, so the client stops displaying it. Any signed-in user may call it and nothing has to be
    /// called first. The flag is stored per user, so marking it read for one account leaves it unread for everybody
    /// else on the portal. The call is mutating but idempotent: repeating it changes nothing. It never fails on the
    /// caller's behalf - a storage error is written to the portal log and the operation still answers with a success,
    /// so the answer is no proof that the flag was saved. Nothing is returned in the body, and no operation reads the
    /// flag back or clears it again, which makes the change effectively permanent for that user. It touches only this
    /// one notice: portal-wide announcements and the letters the portal sends are unaffected, and other per-user
    /// settings are stored through the operations under `api/2.0/settings`.
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
            await cspSettingsHelper.RenameDomainAsync(oldDomain, tenant.GetTenantDomain(coreSettings));

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
    /// Mails the portal owner the two confirmation links that deactivate this portal and bring it back again, and
    /// records the request in the audit trail; the portal itself is not changed here. The caller has to be the portal
    /// owner and hold the portal-settings right, and on a server installation the last remaining space cannot be
    /// deactivated - the call is refused when every other space has limited access. The letter always goes to the
    /// owner's own address, and the operation keeps working while the portal's payment has lapsed. It is mutating
    /// only in that it sends a message, and it is rate-limited to five requests per fifteen minutes per user and path
    /// by default, answering 429 above that. Nothing is returned in the body, so a client cannot tell from the answer
    /// whether the mail was delivered. The first link in the letter authorizes `PUT api/2.0/portal/suspend`, which
    /// suspends the portal, and the second one authorizes `PUT api/2.0/portal/continue`, which makes it active again.
    /// To remove the portal instead of pausing it, use `POST api/2.0/portal/delete`.
    /// </remarks>
    /// <summary>
    /// Send suspension instructions
    /// </summary>
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
    /// Mails the portal owner the confirmation link that removes this portal; nothing about the portal changes until
    /// that link is used. The caller has to be the portal owner and hold the portal-settings right, and on a server
    /// installation the last remaining space cannot be removed - the call is refused when every other space has
    /// limited access. The letter goes to the owner's own address whoever asked for it, and it warns about the
    /// subscription that will stop renewing when the portal is on a paid plan. The operation keeps working while the
    /// portal's payment has lapsed, is mutating only in that it sends a message, and is rate-limited to five requests
    /// per fifteen minutes per user and path by default, answering 429 above that. Nothing is returned in the body.
    /// The link in the letter authorizes `DELETE api/2.0/portal/delete`, which deletes the portal with all of its
    /// rooms, files and accounts and cannot be undone. To pause the portal instead of deleting it, send the
    /// deactivation letter with `POST api/2.0/portal/suspend`.
    /// </remarks>
    /// <summary>
    /// Send removal instructions
    /// </summary>
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
    /// Brings a deactivated portal back to the active state, so its users can sign in again and its domain serves the
    /// portal as before. It is reached only with the reactivation link that `POST api/2.0/portal/suspend` mails to
    /// the portal owner: that link authorizes the call in place of an authentication token, and no ordinary token is
    /// accepted here. The call is mutating and idempotent - it sets the status to active, re-applies the portal's
    /// Content Security Policy and refreshes its base domain, and a portal that is already active is simply left
    /// active. Nothing is returned in the body; read the result from `status` in `GET api/2.0/portal`. Deactivating
    /// the portal again means asking for a fresh letter with `POST api/2.0/portal/suspend`, because each link is
    /// issued for one operation. This operation cannot bring back a removed portal: the deletion behind
    /// `DELETE api/2.0/portal/delete` is final, and a removed portal has to be restored from a backup instead.
    /// </remarks>
    /// <summary>
    /// Restore a portal
    /// </summary>
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

        var current = await settingsManager.LoadAsync<CspSettings>();
        await cspSettingsHelper.SaveAsync(current.Domains, false);
        await cspSettingsHelper.UpdateBaseDomainAsync();
    }

    /// <remarks>
    /// Deactivates this portal: its status becomes suspended and its users can no longer work in it, while all of its
    /// rooms, files and accounts stay untouched. It is reached only with the deactivation link that
    /// `POST api/2.0/portal/suspend` mails to the portal owner - that link authorizes the call instead of an
    /// authentication token - and the owner is checked again here, so a link issued for another account is refused.
    /// On a server installation the last remaining space cannot be deactivated. The call is mutating and idempotent:
    /// it sets the status, records the deactivation in the audit trail and refreshes the portal's base domain, and
    /// repeating it leaves the portal suspended. Nothing is returned in the body; the new state is read from `status`
    /// in `GET api/2.0/portal`. Bring the portal back with `PUT api/2.0/portal/continue`, using the second link from
    /// the same letter. To remove the portal and its content for good, use `DELETE api/2.0/portal/delete` instead -
    /// that cannot be undone.
    /// </remarks>
    /// <summary>
    /// Deactivate a portal
    /// </summary>
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

        await cspSettingsHelper.UpdateBaseDomainAsync();
    }

    /// <remarks>
    /// Removes this portal for good: its rooms, files, accounts, settings and OAuth clients go with it and its domain
    /// stops serving the portal. It is reached only with the removal link that `POST api/2.0/portal/delete` mails to
    /// the portal owner - that link authorizes the call instead of an authentication token - and the owner is checked
    /// again here; on a server installation the last remaining space cannot be removed. The call is destructive and
    /// cannot be undone, and there is no restore operation, so take a backup with `POST api/2.0/backup/startbackup`
    /// first when the content still matters. It keeps working while the portal's payment has lapsed. Along the way
    /// the portal is dropped from the hosting cache, the owner is mailed a confirmation, the removal is written to
    /// the audit trail and, for a portal that was paying, the support team is notified as well. The answer is the
    /// absolute URL of the feedback form to send the departing owner to. To pause the portal instead of erasing it,
    /// use `PUT api/2.0/portal/suspend`.
    /// </remarks>
    /// <summary>
    /// Delete a portal
    /// </summary>
    /// <path>api/2.0/portal/delete</path>
    [Tags("Portal / Settings")]
    [SwaggerResponse(200, "The absolute URL of the feedback form to send the owner of the removed portal to", typeof(string))]
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

        await cspSettingsHelper.RemoveFromCacheAsync(tenantDomain);
        await cspSettingsHelper.UpdateBaseDomainAsync();

        if (!coreBaseSettings.Standalone && !quota.Free && tariff.State >= TariffState.Paid)
        {
            var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
            await studioNotifyService.SendMsgPaidPortalDeletedToSupportAsync(tenantDomain, owner, customerInfo);
        }

        await eventBus.PublishAsync(new RemovePortalIntegrationEvent(securityContext.CurrentAccount.ID, tenant.Id));

        return redirectLink;
    }

    /// <remarks>
    /// Sends the welcome letter that follows the registration of a new portal to the account named by `userid` and
    /// switches on the second authentication factor the installation is configured to require after registration; on
    /// a hosted portal in custom mode the registration data is mailed to the sales address as well. Open to
    /// unauthenticated callers: in place of a token it needs `key`, the confirmation key of the sign-in link the
    /// portal issued for that account, and that key is accepted for one hour after it was created - a wrong, foreign
    /// or expired key answers 403 and sends nothing. Both parameters go in the query string. The call is meant to be
    /// made once, right after registration; it is not idempotent, and every call within that hour sends the letters
    /// again. When the installation asks for SMS or an authenticator app after registration, this call is what
    /// enables that method for the whole portal, unless the new account is an internal test address. Nothing is
    /// returned in the body and there is no operation that reports afterwards whether the letters were delivered.
    /// </remarks>
    /// <summary>
    /// Send congratulations
    /// </summary>
    /// <path>api/2.0/portal/sendcongratulations</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "The welcome letters were sent and the configured second factor was switched on for the portal")]
    [SwaggerResponse(403, "The confirmation key does not match the user or is older than one hour")]
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
            throw new SecurityException(Resource.ErrorAccessDenied);
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
