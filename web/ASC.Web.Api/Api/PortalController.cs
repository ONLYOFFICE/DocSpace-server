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
    CspSettingsHelper cspSettingsHelper,
    PortalRemovalService portalRemovalService,
    InvitationLinkDtoHelper invitationLinkDtoHelper,
    CountPaidUserChecker countPaidUserChecker)
    : ControllerBase
{
    /// <remarks>
    /// Returns the record of the current portal, with its name, alias, owner, language, time zone, status, and dates.
    /// Any signed-in user can call it, including a guest. A caller with the EditPortalSettings permission, which the
    /// portal owner and DocSpace admins have, gets the full record. Other callers get an object with only `tenantId`
    /// filled. So check `tenantAlias` for null first. The call is read-only and also works while the portal is
    /// unpaid. To read the portal settings, use `GET api/2.0/settings`. To read the tariff and the quota, use
    /// `GET api/2.0/portal/tariff` and `GET api/2.0/portal/quota`.
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
    /// Returns an account of the current portal by its ID, in the internal user format. Any signed-in user can read
    /// their own profile. A DocSpace admin can read any account. A room admin can read any account except a guest
    /// they have no relation with. A user or a guest can read only their own profile. Other callers get 403, even for
    /// an ID that does not exist. The call is read-only. It returns 404 for an unknown ID or a system account. To
    /// read your own profile, use `GET api/2.0/people/@self`. To get the account in the People format, with groups,
    /// quota, and access data, use `GET api/2.0/people/{userId}`.
    /// </remarks>
    /// <summary>
    /// Get a portal user
    /// </summary>
    /// <path>api/2.0/portal/users/{userId}</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "The account of this portal, in the internal user format", typeof(PortalUserDto))]
    [SwaggerResponse(403, "The caller is not allowed to view this account: a user or a guest gets it for any ID but their own, whether the account exists or not, and a room administrator gets it for a guest they have no relation with")]
    [SwaggerResponse(404, "No account with this ID exists on the portal, or the ID belongs to a system account")]
    [HttpGet("users/{userId:guid}")]
    public async Task<PortalUserDto> GetUserById(UserIDRequestDto inDto)
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

        return user.ToPortalUserDto();
    }

    /// <remarks>
    /// Returns an invitation URL for the role in `employeeType`, and is deprecated. Use
    /// `POST api/2.0/portal/users/invitationlink` and the other operations under that path instead. Anyone who opens
    /// the URL can join the portal with that role. The portal does not store the link, so you cannot list or revoke
    /// it later. If inviting members is turned off in `GET api/2.0/settings/invitationsettings`, the call
    /// returns 403. The caller needs the right to add users of that role. Only the portal owner gets a DocSpace admin
    /// link. Other callers get an empty string instead of an error. The call is read-only, and you can repeat it.
    /// Each call returns an equivalent link as a bare string. The link is signed for the calling account and has no
    /// use limit. It expires with the email key lifetime of the portal, by default after 7 days.
    /// </remarks>
    /// <summary>
    /// Get a legacy invitation link
    /// </summary>
    /// <path>api/2.0/portal/users/invite/{employeeType}</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "The invitation URL to hand to the invited person, or an empty string when the caller may not invite that role", typeof(string))]
    [SwaggerResponse(403, "Inviting members is disabled for the portal")]
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
    /// Creates the invitation link of the portal for one role and returns it with the URL to share. If inviting
    /// members is turned off in `GET api/2.0/settings/invitationsettings`, the call returns 403. The portal owner can
    /// create a link for any role. A DocSpace admin can create the `RoomAdmin` and `User` links, and a room admin
    /// only the `User` link. Other callers get 403. The call changes data. A role has at most one link, so a repeated
    /// call returns 400. To change an existing link, use `PUT api/2.0/portal/users/invitationlink`. Without
    /// `expiration`, the link never expires. Without `maxUseCount`, it has no use limit. The answer has the `id` that
    /// the update and delete calls need. The call returns 400 for another role, an `expiration` in the past, or a
    /// `maxUseCount` out of range. For a `RoomAdmin` or `DocSpaceAdmin` link, it returns 402 if the payment is
    /// overdue or no paid seat is free.
    /// </remarks>
    /// <summary>
    /// Create an invitation link
    /// </summary>
    /// <path>api/2.0/portal/users/invitationlink</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "The invitation link as it was created, with the `id` to address it later and the `url` to share", typeof(InvitationLinkDto))]
    [SwaggerResponse(400, "The request body cannot be read, the role is not `DocSpaceAdmin`, `RoomAdmin` or `User`, the use limit is outside 1-1000, the deadline is in the past, or the role already has a link")]
    [SwaggerResponse(402, "The link is for a paying role, and the portal payment is overdue or the portal quota has no free paid seat left")]
    [SwaggerResponse(403, "Inviting members is disabled for the portal, the caller has no right to add users of that role, or a non-owner asks for the DocSpace administrator link")]
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
    /// Returns the invitation link of the portal for one role, with its URL, deadline, and use count. If inviting
    /// members is turned off in `GET api/2.0/settings/invitationsettings`, the call returns 403. The portal owner can
    /// read every link. A DocSpace admin can read the `RoomAdmin` and `User` links, and a room admin only the `User`
    /// link. Other callers get 403. The call is read-only. If the role has no link yet, the call returns 200 with no
    /// link. To create one, use `POST api/2.0/portal/users/invitationlink`. The call returns 400 for a role other
    /// than `DocSpaceAdmin`, `RoomAdmin`, or `User`. For a `RoomAdmin` or `DocSpaceAdmin` link, it returns 402 if the
    /// payment is overdue or no paid seat is free. It returns 402 even when the role has no link.
    /// </remarks>
    /// <summary>
    /// Get an invitation link by role
    /// </summary>
    /// <path>api/2.0/portal/users/invitationlink/{employeeType}</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "The invitation link of that role, or an empty body when the portal has no link for it", typeof(InvitationLinkDto))]
    [SwaggerResponse(400, "The role is not `DocSpaceAdmin`, `RoomAdmin` or `User`")]
    [SwaggerResponse(402, "The role is a paying one, and the portal payment is overdue or the portal quota has no free paid seat left, whether or not the role has a link")]
    [SwaggerResponse(403, "Inviting members is disabled for the portal, the caller has no right to add users of that role, or a non-owner asks for the DocSpace administrator link")]
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
    /// Changes the deadline and the use limit of an invitation link by its `id`. The call applies the whole body. So
    /// a missing `expiration` removes the deadline, and a missing `maxUseCount` removes the use limit. You cannot
    /// change the role of a link. To do this, delete the link and create a new one. If inviting members is turned off
    /// in `GET api/2.0/settings/invitationsettings`, the call returns 403. The portal owner can change every link. A
    /// DocSpace admin can change the `RoomAdmin` and `User` links, and a room admin only the `User` link. Other
    /// callers get 403. The call changes data, and you can repeat it with the same body. It returns the whole link as
    /// it now stands. If no link has this `id`, the call returns 404. The call returns 400 for an `expiration` in the
    /// past or a `maxUseCount` out of range. It also returns 400 if `maxUseCount` is lower than the `currentUseCount`
    /// of the link.
    /// </remarks>
    /// <summary>
    /// Update an invitation link
    /// </summary>
    /// <path>api/2.0/portal/users/invitationlink</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "The invitation link as it now stands, with the deadline and the use limit that were applied", typeof(InvitationLinkDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `id`, the use limit is outside 1-1000 or lower than the number of uses the link already has, or the deadline is in the past")]
    [SwaggerResponse(403, "Inviting members is disabled for the portal, the caller has no right to add users of the link's role, or a non-owner tries to change the DocSpace administrator link")]
    [SwaggerResponse(404, "No invitation link with this ID exists on the portal")]
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
    /// Deletes an invitation link of the portal by its `id`. Its URL then stops letting anyone join, and the accounts
    /// that joined through it stay. If inviting members is turned off in `GET api/2.0/settings/invitationsettings`,
    /// the call returns 403. The portal owner can delete every link. A DocSpace admin can delete the `RoomAdmin` and
    /// `User` links, and a room admin only the `User` link. Other callers get 403. The call cannot be undone. A
    /// repeated call or an unknown `id` returns 404. A body without `id` returns 400. A new link for the role gets a
    /// new `id` and URL, and its use count starts from zero. To create it, use
    /// `POST api/2.0/portal/users/invitationlink`. To stop invitations and keep the links, turn inviting off with
    /// `PUT api/2.0/settings/invitationsettings`. The links then stop working until you turn it on again.
    /// </remarks>
    /// <summary>
    /// Delete an invitation link
    /// </summary>
    /// <path>api/2.0/portal/users/invitationlink</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "The invitation link is deleted and its URL no longer lets anyone join the portal", typeof(string))]
    [SwaggerResponse(400, "The request body cannot be read or has no `id`")]
    [SwaggerResponse(403, "Inviting members is disabled for the portal, the caller has no right to add users of the link's role, or a non-owner tries to delete the DocSpace administrator link")]
    [SwaggerResponse(404, "No invitation link with this ID exists on the portal")]
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
    /// Returns the space that the content of the portal uses, in gigabytes rounded to two decimals. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. The
    /// call is read-only. The answer is a number, not an object. The portal adds up the storage counters of its
    /// accounts. So system data that belongs to no account is not counted. To compare the result with the allowance,
    /// use `maxTotalSize` of `GET api/2.0/portal/quota`, which is in bytes. The call does not split the space by room
    /// or user.
    /// </remarks>
    /// <summary>
    /// Get the portal used space
    /// </summary>
    /// <path>api/2.0/portal/usedspace</path>
    [Tags("Portal / Quota")]
    [SwaggerResponse(200, "The space the portal content occupies, in gigabytes rounded to two decimals", typeof(double))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
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
    /// Returns how many accounts of the portal have the active status, of every user type. Disabled and deleted
    /// accounts are not counted. Requires the EditPortalSettings permission, which the portal owner and DocSpace
    /// admins have. Other callers get 403. The call is read-only. The answer is a number, not an object. To compare
    /// it with the allowance, use `countUser` and `countPaidUser` of `GET api/2.0/portal/quota`. To list the accounts
    /// or filter them by type, group, or status, use `GET api/2.0/people/filter`.
    /// </remarks>
    /// <summary>
    /// Get a number of portal users
    /// </summary>
    /// <path>api/2.0/portal/userscount</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "The number of accounts of this portal that are in the active state", typeof(long))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("userscount")]
    public async Task<long> GetPortalUsersCount()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        return (await userManager.GetUserNamesAsync(EmployeeStatus.Active)).Length;
    }

    /// <remarks>
    /// Returns the tariff of the portal, with its state, the end of the current period, and its quotas. Any signed-in
    /// user can call it, including a guest. Every caller gets `state`. A room admin or a DocSpace admin also gets
    /// `dueDate` and `delayDueDate`. Only a caller with the EditPortalSettings permission gets the other fields. The
    /// portal owner and DocSpace admins have this permission. The call is read-only and also works while the portal
    /// is unpaid. With `refresh` set to `true`, the call reads the tariff from the billing system instead of the
    /// cache. To see what the next period costs, use `GET api/2.0/portal/tariff/upcoming`.
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
    /// Returns the next charges of the portal for the quotas of its tariff. The list has one entry per quota.
    /// Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers
    /// get 403. The call is read-only and also works while the portal is unpaid. The list skips overdue quotas. It
    /// also skips quotas without a price, such as a trial or a free plan. So the list can be empty on a portal with a
    /// tariff. If a switch to another quota is planned, the entry shows the next quota and its quantity. With
    /// `refresh` set to `true`, the call reads the tariff from the billing system instead of the cache.
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
    /// Returns the quota of the portal, which is the allowance its tariff gives. It has the limits on users, paid
    /// users, rooms, total size, and file size, and the features. Requires the EditPortalSettings permission, which
    /// the portal owner and DocSpace admins have. Other callers get 403. The call is read-only and also works while
    /// the portal is unpaid. Sizes are in bytes. A free or trial quota has no price. The quota shows what the portal
    /// may use, not what it uses. The usage comes from `GET api/2.0/portal/usedspace` and
    /// `GET api/2.0/portal/userscount`. To list the quotas the portal can switch to, use
    /// `GET api/2.0/portal/payment/quotas`. To read the billing state, use `GET api/2.0/portal/tariff`.
    /// </remarks>
    /// <summary>
    /// Get the portal quota
    /// </summary>
    /// <path>api/2.0/portal/quota</path>
    [Tags("Portal / Quota")]
    [SwaggerResponse(200, "The allowance the current tariff grants this portal, with sizes in bytes", typeof(TenantQuotaDto))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [AllowNotPayment]
    [HttpGet("quota")]
    public async Task<TenantQuotaDto> GetPortalQuota()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();
        var result = await tenantManager.GetTenantQuotaAsync(tenant.Id);

        if (await userManager.IsUserAsync(authContext.CurrentAccount.ID))
        {
            result.MaxTotalSize = 0;
        }

        return result.Map();
    }

    /// <remarks>
    /// Returns the cheapest quota that is not yearly and allows more users than the portal has active accounts.
    /// Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers
    /// get 403. The call is read-only and buys nothing. The answer has the shape of `GET api/2.0/portal/quota`, with
    /// sizes in bytes. If no quota fits, the call returns 200 with no quota. To list all quotas, use
    /// `GET api/2.0/portal/payment/quotas`. To buy one, use `PUT api/2.0/portal/payment/url`.
    /// </remarks>
    /// <summary>
    /// Get the recommended quota
    /// </summary>
    /// <path>api/2.0/portal/quota/right</path>
    [Tags("Portal / Quota")]
    [SwaggerResponse(200, "The cheapest monthly quota that would still fit this portal, or an empty body when none does", typeof(TenantQuotaDto))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("quota/right")]
    public async Task<TenantQuotaDto> GetRightQuota()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var usedSpace = await GetPortalUsedSpace();
        var needUsersCount = await GetPortalUsersCount();

        return (await tenantManager.GetTenantQuotasAsync()).OrderBy(r => r.Price)
                            .FirstOrDefault(quota =>
                                            quota.CountUser > needUsersCount
                                            && quota.MaxTotalSize > usedSpace
                                            && !quota.Year)?.Map();
    }


    /// <remarks>
    /// Returns the absolute URL of a path on the portal, with the scheme, the current domain, and the virtual root.
    /// Any signed-in user can call it, including a guest. The call is read-only. It does not check the path or the
    /// access of the caller to it. An empty `virtualPath` returns the portal root. The call adds a path that starts
    /// with `/` to the root and resolves `~/` against the virtual root. It returns a value that starts with
    /// `http://`, `https://`, or `mailto:` unchanged. The answer is a JSON string. It has the current domain, so
    /// after a rename the call returns the new domain.
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
    /// Marks the open-source gift message as read for the calling user, so the app stops showing it. Any signed-in
    /// user can call it. The portal saves the flag per user, so other accounts still see the message. The call
    /// changes data, and you can repeat it. It returns success even if saving fails. In that case, it only writes the
    /// error to the portal log. So a success does not prove that the portal saved the flag. No operation reads the
    /// flag or clears it again.
    /// </remarks>
    /// <summary>
    /// Mark a gift message as read
    /// </summary>
    /// <path>api/2.0/portal/present/mark</path>
    [Tags("Portal / Users")]
    [SwaggerResponse(200, "The gift message is marked as read for the calling user; the same answer is returned when saving the flag failed, because the error is only written to the portal log. The response carries no content")]
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
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
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
    /// Sends the portal owner an email with the links that deactivate the portal and make it active again. This call
    /// does not change the portal. Only the portal owner can call it. Other callers get 403, including a DocSpace
    /// admin. The call also works while the portal is unpaid. A repeated call shortly after the email went out does
    /// nothing, and the links of the first email stay valid. The first link signs `PUT api/2.0/portal/suspend`, and
    /// the second signs `PUT api/2.0/portal/continue`. To delete the portal instead, use
    /// `POST api/2.0/portal/delete`. On a server installation, the call returns 500 if every other space has limited
    /// access. So you cannot deactivate the last space.
    /// </remarks>
    /// <summary>
    /// Send suspension instructions
    /// </summary>
    /// <path>api/2.0/portal/suspend</path>
    [Tags("Portal / Settings")]
    [SwaggerResponse(200, "The request is accepted: the deactivation letter is queued and the request recorded in the audit trail, unless the same letter already went out within the repeat period; the portal itself is still active and the response carries no content")]
    [SwaggerResponse(403, "The caller is not the portal owner or has no portal-settings right")]
    [SwaggerResponse(500, "On a server installation every other space has limited access, so the last remaining space cannot be deactivated")]
    [AllowNotPayment]
    [HttpPost("suspend")]
    public async Task SendSuspendInstructions()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();

        await DemandPermissionToDeleteTenantAsync(tenant);

        await using var acceptance = await studioNotifyService.TryAcceptPortalDeactivationAsync(tenant);
        if (!acceptance.Accepted)
        {
            return;
        }

        var owner = await userManager.GetUsersAsync(tenant.OwnerId);
        var suspendUrl = commonLinkUtility.GetConfirmationEmailUrl(owner.Email, ConfirmType.PortalSuspend);
        var continueUrl = commonLinkUtility.GetConfirmationEmailUrl(owner.Email, ConfirmType.PortalContinue);

        await studioNotifyService.SendMsgPortalDeactivationAsync(tenant, await urlShortener.GetShortenLinkAsync(suspendUrl), await urlShortener.GetShortenLinkAsync(continueUrl));
        acceptance.Complete();

        messageService.Send(MessageAction.OwnerSentPortalDeactivationInstructions, MessageTarget.Create(owner.Id), owner.DisplayUserName(false, displayUserSettingsHelper));
    }

    /// <remarks>
    /// Sends the portal owner an email with the link that deletes the portal. This call does not change the portal.
    /// Only the portal owner can call it. Other callers get 403, including a DocSpace admin. The call also works
    /// while the portal is unpaid. A repeated call shortly after the email went out does nothing, and the link of the
    /// first email stays valid. If the portal has payments and is not on trial, the email also warns that the
    /// subscription stops renewing. The link signs `DELETE api/2.0/portal/delete`, which deletes the portal and
    /// cannot be undone. To deactivate the portal instead, use `POST api/2.0/portal/suspend`. On a server
    /// installation, the call returns 500 if every other space has limited access. So you cannot delete the last
    /// space.
    /// </remarks>
    /// <summary>
    /// Send removal instructions
    /// </summary>
    /// <path>api/2.0/portal/delete</path>
    [Tags("Portal / Settings")]
    [SwaggerResponse(200, "The request is accepted, and the removal letter is queued unless one already went out within the repeat period; nothing about the portal has changed yet and the response carries no content")]
    [SwaggerResponse(403, "The caller is not the portal owner or has no portal-settings right")]
    [SwaggerResponse(500, "On a server installation every other space has limited access, so the last remaining space cannot be removed")]
    [AllowNotPayment]
    [HttpPost("delete")]
    public async Task SendDeleteInstructions()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();

        await DemandPermissionToDeleteTenantAsync(tenant);

        await using var acceptance = await studioNotifyService.TryAcceptPortalDeletionAsync(tenant);
        if (!acceptance.Accepted)
        {
            return;
        }

        var owner = await userManager.GetUsersAsync(tenant.OwnerId);

        var showAutoRenewText = !coreBaseSettings.Standalone &&
                        (await tariffService.GetPaymentsAsync(tenant.Id)).Any() &&
                        !(await tenantManager.GetCurrentTenantQuotaAsync()).Trial;

        var confirmLink = commonLinkUtility.GetConfirmationEmailUrl(owner.Email, ConfirmType.PortalRemove);

        await studioNotifyService.SendMsgPortalDeletionAsync(tenant, await urlShortener.GetShortenLinkAsync(confirmLink), showAutoRenewText);
        acceptance.Complete();
    }

    /// <remarks>
    /// Makes a deactivated portal active again, so its users can sign in. Sign the request with the confirmation
    /// token from the second link of the email that `POST api/2.0/portal/suspend` sends. An ordinary sign-in token
    /// does not work here. The call changes data, and you can repeat it. An active portal stays active. The call
    /// returns no data. To check the result, read `status` from `GET api/2.0/portal`. The call cannot bring back a
    /// deleted portal.
    /// </remarks>
    /// <summary>
    /// Restore a portal
    /// </summary>
    /// <path>api/2.0/portal/continue</path>
    [Tags("Portal / Settings")]
    [SwaggerResponse(200, "The portal is active again and its users can sign in; the response carries no content")]
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
    /// Unblocks the portal after the retention policy blocked it for a long time without use. Its users can then sign
    /// in again. Sign the request with the confirmation token from the unblocking link in the retention email to the
    /// owner. An ordinary sign-in token gets 403. The call changes data and makes the portal active. It also restarts
    /// the retention count. A repeated call returns 401. It also returns 401 for an invalid link, an earlier block's
    /// link, or a portal that is not blocked. The call returns no data. To check the status, read `tenantStatus` from
    /// `GET api/2.0/settings`, which a blocked portal still answers. A lapsed paid tariff stays unpaid, so renew it
    /// next. The emails of a free portal have no such link, and support unblocks it. The call does not bring back a
    /// deleted portal or one that its owner deactivated. To make a deactivated portal active, use
    /// `PUT api/2.0/portal/continue`.
    /// </remarks>
    /// <summary>
    /// Unblock a portal
    /// </summary>
    /// <path>api/2.0/portal/unblock</path>
    [Tags("Portal / Settings")]
    [SwaggerResponse(200, "The portal is active again and its users can sign in; the response carries no content")]
    [SwaggerResponse(401, "The unblocking link is invalid, was mailed for an earlier block, or the portal is not blocked")]
    [SwaggerResponse(403, "The call carries an ordinary sign-in token instead of the unblocking link")]
    [AllowBlocked]
    [AllowNotPayment]
    [HttpPut("unblock")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "PortalUnblock")]
    public async Task UnblockPortal()
    {
        var tenant = tenantManager.GetCurrentTenant();

        // The link is refused for a portal that is not blocked before it gets here; a call that raced another one
        // which has just unblocked the portal gets the same answer, not a second unblocking.
        if (tenant.Status != TenantStatus.Blocked)
        {
            throw new AuthenticationException("The portal is not blocked");
        }

        // The new status date is also where the retention count starts again.
        tenant.SetStatus(TenantStatus.Active);
        await tenantManager.SaveTenantAsync(tenant);
        messageService.Send(MessageAction.PortalUnblocked);

        // The category of the block is over with it; the next block records its own.
        await settingsManager.SaveAsync(new PortalRetentionSettings());

        var current = await settingsManager.LoadAsync<CspSettings>();
        await cspSettingsHelper.SaveAsync(current.Domains, false);
        await cspSettingsHelper.UpdateBaseDomainAsync();
    }

    /// <remarks>
    /// Deactivates the portal. Its users can no longer work in it, and its rooms, files, and accounts stay. Sign the
    /// request with the confirmation token from the first link of the email that `POST api/2.0/portal/suspend` sends.
    /// The call also checks that the token belongs to the portal owner. Other callers get 403, including an ordinary
    /// sign-in token. The call changes data. After it, the portal refuses most calls, so a repeated call fails. To
    /// make the portal active again, use `PUT api/2.0/portal/continue` with the second link of the email. To delete
    /// the portal for good, use `DELETE api/2.0/portal/delete`. On a server installation, the call returns 500 if
    /// every other space has limited access. So you cannot deactivate the last space.
    /// </remarks>
    /// <summary>
    /// Deactivate a portal
    /// </summary>
    /// <path>api/2.0/portal/suspend</path>
    [Tags("Portal / Settings")]
    [SwaggerResponse(200, "The portal is now suspended, its users can no longer work in it and its content is kept; the response carries no content")]
    [SwaggerResponse(403, "The account the confirmation link was issued for is not the portal owner")]
    [SwaggerResponse(500, "On a server installation every other space has limited access, so the last remaining space cannot be deactivated")]
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
    /// Deletes the portal with its rooms, files, accounts, settings, and OAuth apps. Its domain then stops serving
    /// the portal. Sign the request with the confirmation token from the email that `POST api/2.0/portal/delete`
    /// sends. The call also checks that the token belongs to the portal owner. Other callers get 403, including an
    /// ordinary sign-in token. The call cannot be undone, and no operation restores the portal. To keep the content,
    /// first start a backup with `POST api/2.0/backup/startbackup`. The call also works while the portal is unpaid.
    /// After it, the portal refuses all calls, so a repeated call fails. The portal emails the owner a confirmation.
    /// The call returns the absolute URL of a feedback form to send the owner to. To deactivate the portal instead,
    /// use `POST api/2.0/portal/suspend`. On a server installation, the call returns 500 if every other space has
    /// limited access. So you cannot delete the last space.
    /// </remarks>
    /// <summary>
    /// Delete a portal
    /// </summary>
    /// <path>api/2.0/portal/delete</path>
    [Tags("Portal / Settings")]
    [SwaggerResponse(200, "The absolute URL of the feedback form to send the owner of the removed portal to", typeof(string))]
    [SwaggerResponse(403, "The account the confirmation link was issued for is not the portal owner")]
    [SwaggerResponse(500, "On a server installation every other space has limited access, so the last remaining space cannot be removed")]
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

        var owner = await userManager.GetUsersAsync(tenant.OwnerId);

        var redirectLink = externalResourceSettingsHelper.Site.GetRegionalFullEntry("registrationcanceled");

        await portalRemovalService.RemoveAsync(tenant, securityContext.CurrentAccount.ID, auto: false, async () =>
        {
            await studioNotifyService.SendMsgPortalDeletionSuccessAsync(owner, redirectLink);

            messageService.Send(MessageAction.PortalDeleted);

            await cspSettingsHelper.RemoveFromCacheAsync(tenantDomain);
            await cspSettingsHelper.UpdateBaseDomainAsync();

            if (!coreBaseSettings.Standalone && !quota.Free && tariff.State >= TariffState.Paid)
            {
                var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
                await studioNotifyService.SendMsgPaidPortalDeletedToSupportAsync(tenantDomain, owner, customerInfo);
            }
        });

        return redirectLink;
    }

    /// <remarks>
    /// Sends the welcome email of a newly registered portal to the account in `userid`. Anyone can call it without
    /// signing in. Instead of a token, `key` must be the confirmation key of the sign-in link for that account. The
    /// key works for one hour. The call returns 403 for a wrong, foreign, or expired key, and then sends nothing. The
    /// call changes data. Call it once, right after registration. Each repeated call within the hour sends the emails
    /// again. If the installation requires SMS or an authenticator app after registration, the call turns that method
    /// on for the portal. It skips this step for an internal test address. On a hosted portal in custom mode, the
    /// call also sends the registration data to the sales address. The call returns no data.
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
    /// Mails the portal owner the confirmation link that removes this portal; nothing about the portal changes until
    /// that link is used. The call needs no authentication token: it works only on a server installation, for the
    /// portal owner's ID, with a key issued for that ID and the removal, valid for an hour - anything else answers
    /// 403. The letter goes to the owner's address as it is, without waiting for the address to be activated. A repeat
    /// shortly after the letter went out sends nothing, and the link of that letter stays valid.
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

        await using var acceptance = await studioNotifyService.TryAcceptPortalDeletionAsync(tenant, false);
        if (!acceptance.Accepted)
        {
            return;
        }

        var owner = await userManager.GetUsersAsync(tenant.OwnerId);
        var confirmLink = commonLinkUtility.GetConfirmationEmailUrl(owner.Email, ConfirmType.PortalRemove);

        await studioNotifyService.SendMsgPortalDeletionAsync(tenant, await urlShortener.GetShortenLinkAsync(confirmLink), false, false);
        acceptance.Complete();
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
