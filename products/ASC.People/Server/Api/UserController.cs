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

using System.Globalization;

using ASC.Core.Common.Identity;
using ASC.MessagingSystem;
using ASC.Web.Files.Utils;

namespace ASC.People.Api;

///<remarks>
/// User API.
///</remarks>
public class UserController(
    CommonLinkUtility commonLinkUtility,
    ICache cache,
    TenantManager tenantManager,
    CookiesManager cookiesManager,
    CookieStorage cookieStorage,
    CustomNamingPeople customNamingPeople,
    EmployeeDtoHelper employeeDtoHelper,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    ILogger<UserController> logger,
    PasswordHasher passwordHasher,
    QueueWorkerReassign queueWorkerReassign,
    QueueWorkerUpdateUserType queueWorkerUpdateUserType,
    QueueWorkerRemove queueWorkerRemove,
    UserFormatter userFormatter,
    UserManagerWrapper userManagerWrapper,
    WebItemManager webItemManager,
    WebItemSecurity webItemSecurity,
    WebItemSecurityCache webItemSecurityCache,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    UserInvitationLimitHelper userInvitationLimitHelper,
    SecurityContext securityContext,
    StudioNotifyService studioNotifyService,
    MessageService messageService,
    AuthContext authContext,
    UserManager userManager,
    PermissionContext permissionContext,
    CoreBaseSettings coreBaseSettings,
    ApiContext apiContext,
    UserPhotoManager userPhotoManager,
    IHttpContextAccessor httpContextAccessor,
    SettingsManager settingsManager,
    InvitationService invitationService,
    FileSecurity fileSecurity,
    UsersQuotaSyncOperation usersQuotaSyncOperation,
    CountPaidUserChecker countPaidUserChecker,
    CountUserChecker activeUsersChecker,
    IUrlShortener urlShortener,
    FileSecurityCommon fileSecurityCommon,
    IDistributedLockProvider distributedLockProvider,
    QuotaSocketManager quotaSocketManager,
    IQuotaService quotaService,
    CustomQuota customQuota,
    AuditEventsRepository auditEventsRepository,
    EmailValidationKeyModelHelper emailValidationKeyModelHelper,
    CountPaidUserStatistic countPaidUserStatistic,
    UserSocketManager socketManager,
    GlobalFolderHelper globalFolderHelper,
    UserWebhookManager webhookManager,
    BruteForceLoginManager bruteForceLoginManager,
    SetupInfo setupInfo,
    IdentityClient client,
    GroupFullDtoHelper groupFullDtoHelper,
    IUrlValidator urlValidator,
    IHttpClientFactory httpClientFactory)
    : PeopleControllerBase(userManager, permissionContext, apiContext, userPhotoManager, httpContextAccessor, urlValidator, setupInfo, httpClientFactory)
{
    /// <remarks>
    /// Returns the account name and all claims of the token or cookie that authenticated the current request. Use it to
    /// find out which account a token belongs to and which scopes and roles it has. Any signed-in user can call it,
    /// including a guest. It reports only on the caller. The call is read-only. Each claim comes as one `type:value`
    /// string, in the order of authentication. The name `Unknown Name` means that the identity has no name claim. To
    /// read the profile of the caller, use `GET api/2.0/people/@self`.
    /// </remarks>
    /// <summary>
    /// Get user claims
    /// </summary>
    /// <path>api/2.0/people/tokendiagnostics</path>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "The account name and the claims of the current identity", typeof(TokenDiagnosticsDto))]
    [HttpGet("tokendiagnostics")]
    public TokenDiagnosticsDto GetClaims()
    {
        var result = new TokenDiagnosticsDto
        {
            Name = User.Identity?.Name ?? "Unknown Name",
            Claims = (from c in User.Claims select c.Type + ":" + c.Value).ToList()
        };

        return result;
    }


    /// <remarks>
    /// Adds an activated portal user with the first name, last name, email address, and several optional parameters specified in the request.
    /// </remarks>
    /// <summary>
    /// Add an activated user
    /// </summary>
    /// <path>api/2.0/people/active</path>
    [RaisesWebhook(WebhookTrigger.UserCreated, When = "once the avatar named in `files`, if any, is saved: when that download fails, the profile exists but the webhook is not sent")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "Newly added user with the detailed information", typeof(EmployeeFullDto))]
    [HttpPost("active")]
    public async Task<EmployeeFullDto> AddMemberAsActivated(MemberRequestDto inDto)
    {
        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(inDto.Type), Constants.Action_AddRemoveUser);

        var invitationSettings = await settingsManager.LoadAsync<TenantUserInvitationSettings>();

        if (inDto.Type is EmployeeType.Guest)
        {
            if (!invitationSettings.AllowInvitingGuests)
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }
        }
        else
        {
            if (!invitationSettings.AllowInvitingMembers)
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }
        }

        var user = new UserInfo();

        inDto.PasswordHash = (inDto.PasswordHash ?? "").Trim();
        if (string.IsNullOrEmpty(inDto.PasswordHash))
        {
            inDto.Password = (inDto.Password ?? "").Trim();

            if (string.IsNullOrEmpty(inDto.Password))
            {
                inDto.Password = UserManagerWrapper.GeneratePassword();
            }
            else
            {
                await userManagerWrapper.CheckPasswordPolicyAsync(inDto.Password);
            }

            inDto.PasswordHash = passwordHasher.GetClientPassword(inDto.Password);
        }

        //Validate email
        var address = new MailAddress(inDto.Email);
        user.Email = address.Address;
        //Set common fields
        user.FirstName = inDto.FirstName;
        user.LastName = inDto.LastName;
        //user.Title = inDto.Title;
        user.Location = inDto.Location;
        user.Notes = inDto.Comment;

        // if (inDto.Sex.HasValue)
        // {
        //     user.Sex = inDto.Sex.Value == SexEnum.Male;
        // }

        user.Spam = inDto.Spam;
        user.CreatedBy = securityContext.CurrentAccount.ID;
        // user.BirthDate = inDto.Birthday != null ? tenantUtil.DateTimeFromUtc(inDto.Birthday) : null;
        // user.WorkFromDate = inDto.Worksfrom != null ? tenantUtil.DateTimeFromUtc(inDto.Worksfrom) : DateTime.UtcNow.Date;

        await UpdateContactsAsync(inDto.Contacts, user, false);

        UrlValidationResult photoValidation = null;
        if (inDto.Files != _userPhotoManager.GetDefaultPhotoAbsoluteWebPath())
        {
            photoValidation = await ValidatePhotoUrlAsync(inDto.Files);
        }

        cache.Insert("REWRITE_URL" + tenantManager.GetCurrentTenantId(), HttpContext.Request.GetDisplayUrl(), TimeSpan.FromMinutes(5));
        user = await userManagerWrapper.AddUserAsync(user, inDto.PasswordHash, false, false, inDto.Type,
            false, true, true);
        if (inDto.Type is EmployeeType.Guest)
        {
            await socketManager.AddGuestAsync(user);
        }
        else
        {
            await socketManager.AddUserAsync(user);
        }

        await UpdateDepartmentsAsync(inDto.Department, user);

        if (photoValidation != null)
        {
            await DownloadAndSavePhotoAsync(photoValidation, user);
        }

        await webhookManager.PublishAsync(WebhookTrigger.UserCreated, user);

        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <remarks>
    /// Creates an account in the portal. An admin can add a person directly, or a person can accept an invitation link.
    /// To accept a link, set `fromInviteLink` to true and pass the `key` of the link. The link then sets the account
    /// type, and the call ignores `type`. If the link is invalid or expired, the call returns 403. Without a link, a
    /// room admin can create a user, and a DocSpace admin can also create a room admin. Only the portal owner can
    /// create a DocSpace admin. Other callers get 403. Without a link, the `Guest` type also returns 403. The call also
    /// returns 403 if the portal does not allow inviting members, or guests for a guest link. The call changes data. A
    /// repeated call with the same email returns 400. With an invitation link that `POST api/2.0/people/invite`
    /// emailed, a repeat returns 403. If `password` breaks the portal password policy, the call returns 400. It also
    /// returns 400 if the first and last names are not valid. Without `password` and `passwordHash`, the portal sets a
    /// random password that nobody knows. If the portal has no free paid seat, the call still creates the account, as a
    /// user. So check the type flags of the returned profile, such as `isRoomAdmin`. The call also downloads the avatar
    /// from `files`. If the portal refuses that URL, the call returns 403 and creates no account. If its server returns
    /// an error, the call also returns 403. Any failed download keeps the created account. To invite several people by
    /// email, use `POST api/2.0/people/invite`.
    /// </remarks>
    /// <summary>
    /// Add a user
    /// </summary>
    /// <path>api/2.0/people</path>
    [RaisesWebhook(WebhookTrigger.UserCreated, When = "after the portal saves the avatar from `files`, if any, but not if the download fails")]
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "The new profile with its detailed information", typeof(EmployeeFullDto))]
    [SwaggerResponse(400, "The password does not meet the portal password policy")]
    [SwaggerResponse(403, "The invitation link is invalid or has expired, the portal does not allow inviting this kind of account, the caller may not create an account of the requested type, the `files` URL was refused, or the server behind it answered with an error status after the account had been created")]
    [HttpPost]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "LinkInvite,Authenticated")]
    public async Task<EmployeeFullDto> AddMember(MemberRequestDto inDto)
    {
        await securityContext.AuthByClaimAsync();
        var model = emailValidationKeyModelHelper.GetModel();
        var linkData = inDto.FromInviteLink ? await invitationService.GetLinkDataAsync(inDto.Key, inDto.Email, model.Type, inDto.Type, model.UiD) : null;
        if (linkData is { IsCorrect: false })
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_InvintationLink);
        }

        if (linkData != null)
        {
            await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(Guid.Empty, linkData.EmployeeType), Constants.Action_AddRemoveUser);
        }
        else
        {
            await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(Guid.Empty, inDto.Type), Constants.Action_AddRemoveUser);
            var tenant = tenantManager.GetCurrentTenant();
            var currentUser = await _userManager.GetUsersAsync(authContext.CurrentAccount.ID);
            var currentUserType = await _userManager.GetUserTypeAsync(currentUser.Id);

            switch (inDto.Type)
            {
                case EmployeeType.Guest:
                case EmployeeType.RoomAdmin when currentUserType is not EmployeeType.DocSpaceAdmin:
                case EmployeeType.DocSpaceAdmin when !currentUser.IsOwner(tenant):
                    throw new SecurityException(Resource.ErrorAccessDenied);
            }
        }

        inDto.Type = linkData?.EmployeeType ?? inDto.Type;

        var invitationSettings = await settingsManager.LoadAsync<TenantUserInvitationSettings>();

        if (inDto.Type is EmployeeType.Guest)
        {
            if (!invitationSettings.AllowInvitingGuests)
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }
        }
        else
        {
            if (!invitationSettings.AllowInvitingMembers)
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }
        }

        var user = new UserInfo();

        var byEmail = linkData is { LinkType: InvitationLinkType.Individual, ConfirmType: not ConfirmType.EmpInvite };
        if (byEmail)
        {
            user = await _userManager.GetUserByEmailAsync(inDto.Email);

            if (user.Equals(Constants.LostUser) || user.ActivationStatus != EmployeeActivationStatus.Pending)
            {
                throw new SecurityException(FilesCommonResource.ErrorMessage_InvintationLink);
            }
        }

        if (byEmail || linkData?.ConfirmType is ConfirmType.EmpInvite)
        {
            await userInvitationLimitHelper.IncreaseLimit();
        }

        if (!byEmail)
        {
            user.CreatedBy = linkData != null ? model?.UiD : securityContext.CurrentAccount.ID;
        }

        inDto.PasswordHash = (inDto.PasswordHash ?? "").Trim();
        if (string.IsNullOrEmpty(inDto.PasswordHash))
        {
            inDto.Password = (inDto.Password ?? "").Trim();

            if (string.IsNullOrEmpty(inDto.Password))
            {
                inDto.Password = UserManagerWrapper.GeneratePassword();
            }
            else
            {
                await userManagerWrapper.CheckPasswordPolicyAsync(inDto.Password);
            }
            inDto.PasswordHash = passwordHasher.GetClientPassword(inDto.Password);
        }

        //Validate email
        var address = new MailAddress(inDto.Email);
        user.Email = address.Address;
        //Set common fields
        user.CultureName = inDto.CultureName;
        user.FirstName = inDto.FirstName;
        user.LastName = inDto.LastName;
        //user.Title = inDto.Title;
        user.Location = inDto.Location;
        user.Notes = inDto.Comment;

        // if (inDto.Sex.HasValue)
        // {
        //     user.Sex = inDto.Sex.Value == SexEnum.Male;
        // }

        user.Spam = inDto.Spam;
        //user.BirthDate = inDto.Birthday != null && inDto.Birthday != DateTime.MinValue ? tenantUtil.DateTimeFromUtc(inDto.Birthday) : null;
        //user.WorkFromDate = inDto.Worksfrom != null && inDto.Worksfrom != DateTime.MinValue ? tenantUtil.DateTimeFromUtc(inDto.Worksfrom) : DateTime.UtcNow.Date;
        user.Status = EmployeeStatus.Active;

        await UpdateContactsAsync(inDto.Contacts, user, false);

        UrlValidationResult photoValidation = null;
        if (inDto.Files != _userPhotoManager.GetDefaultPhotoAbsoluteWebPath())
        {
            photoValidation = await ValidatePhotoUrlAsync(inDto.Files);
        }

        cache.Insert("REWRITE_URL" + tenantManager.GetCurrentTenantId(), HttpContext.Request.GetDisplayUrl(), TimeSpan.FromMinutes(5));

        var quotaLimit = false;

        try
        {
            user = await userManagerWrapper.AddUserAsync(user, inDto.PasswordHash, inDto.FromInviteLink, true, inDto.Type,
                inDto.FromInviteLink && linkData is { IsCorrect: true, ConfirmType: not ConfirmType.EmpInvite }, true, true, byEmail);

            if (linkData is { IsCorrect: true, ConfirmType: ConfirmType.LinkInvite } && linkData.LinkId != Guid.Empty)
            {
                await _userManager.IncreaseInvitationLinkUsageAsync(linkData.LinkId);
            }

            if (inDto.Type is EmployeeType.Guest)
            {
                await socketManager.AddGuestAsync(user);
            }
            else
            {
                await socketManager.AddUserAsync(user);
            }
        }
        catch (TenantQuotaException)
        {
            quotaLimit = true;
            user = await userManagerWrapper.AddUserAsync(user, inDto.PasswordHash, inDto.FromInviteLink, true, EmployeeType.User,
                inDto.FromInviteLink && linkData is { IsCorrect: true, ConfirmType: not ConfirmType.EmpInvite }, true, true, byEmail);
            await socketManager.AddUserAsync(user);
        }

        await UpdateDepartmentsAsync(inDto.Department, user);

        if (photoValidation != null)
        {
            await DownloadAndSavePhotoAsync(photoValidation, user);
        }

        if (inDto.IsUser.GetValueOrDefault(false))
        {
            messageService.Send(MessageAction.GuestCreated, MessageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper));
        }
        else
        {
            messageService.Send(MessageAction.UserCreated, MessageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper), user.Id);
        }

        await webhookManager.PublishAsync(WebhookTrigger.UserCreated, user);

        if (linkData is { LinkType: InvitationLinkType.CommonToRoom })
        {
            await invitationService.AddUserToRoomByInviteAsync(linkData, user, quotaLimit);
        }

        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <remarks>
    /// Invites people to the portal by email. For each new address, the call creates a pending account and emails it
    /// an invitation link. Requires a room admin or a DocSpace admin. Other callers get 403. Only a DocSpace admin
    /// can invite a room admin, and only the portal owner can invite a DocSpace admin. The `Guest` type and a portal
    /// that does not allow inviting members also return 403. For an address that already has an account, the call
    /// sends no email. It links that account to the caller and raises its type if needed. The call changes data. A
    /// repeated call sends no new emails, since the accounts then exist. Its seat check still counts each room admin
    /// and DocSpace admin in the body. So a repeat can return 402. If the pending account was deleted, a new invitation
    /// to its address soon after the first sends no email. The first link still works. If the invitations need more
    /// paid seats than the tariff has left, the call returns 402 and sends nothing. The call returns 400 for a
    /// malformed or punycode address, or for the address of a disabled account. The call handles the addresses in
    /// order. So a 400 or 403 in the middle leaves the earlier invitations sent. The call returns every pending account
    /// that the caller can see, not only the new ones. The portal limits the rate of these calls. To send an invitation
    /// again, use `PUT api/2.0/people/invite`.
    /// </remarks>
    /// <summary>
    /// Invite users
    /// </summary>
    /// <path>api/2.0/people/invite</path>
    /// <collection>list</collection>
    [RaisesWebhook(WebhookTrigger.UserInvited, When = "for every address that has no account yet")]
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "Every pending profile the caller may see, not only the ones just invited", typeof(List<EmployeeDto>))]
    [SwaggerResponse(400, "An address is malformed or written in punycode, or it belongs to a disabled account")]
    [SwaggerResponse(402, "The invitations would need more paid seats than the tariff has left")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPost("invite")]
    [EnableRateLimiting(RateLimiterPolicy.EmailInvitationApi)]
    public async Task<List<EmployeeDto>> InviteUsers(InviteUsersRequestDto inDto)
    {
        ArgumentNullException.ThrowIfNull(inDto);

        var invitationSettings = await settingsManager.LoadAsync<TenantUserInvitationSettings>();

        if (!invitationSettings.AllowInvitingMembers)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var currentUser = await _userManager.GetUsersAsync(authContext.CurrentAccount.ID);
        var currentUserType = await _userManager.GetUserTypeAsync(currentUser);

        var tenant = tenantManager.GetCurrentTenant();

        if (currentUserType is EmployeeType.User or EmployeeType.Guest)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var quotaIncreaseBy = inDto.Invitations.Count(x => x.Type is EmployeeType.DocSpaceAdmin or EmployeeType.RoomAdmin);
        if (quotaIncreaseBy > 0)
        {
            var tenantId = tenantManager.GetCurrentTenantId();
            var quota = await tenantManager.GetTenantQuotaAsync(tenantId);
            var maxCount = quota.GetFeature<CountPaidUserFeature>().Value;
            var currentCount = await countPaidUserStatistic.GetValueAsync();

            if (maxCount < currentCount + quotaIncreaseBy)
            {
                throw new TenantQuotaException(string.Format(Resource.TariffsFeature_usersQuotaExceeds_exception, quotaIncreaseBy, maxCount - currentCount));
            }
        }

        foreach (var invite in inDto.Invitations)
        {
            if (!invite.Email.TestEmailRegex() || invite.Email.TestEmailPunyCode())
            {
                throw new ArgumentException(Resource.ErrorNotCorrectEmail);
            }

            switch (invite.Type)
            {
                case EmployeeType.Guest:
                case EmployeeType.RoomAdmin when currentUserType is not EmployeeType.DocSpaceAdmin:
                case EmployeeType.DocSpaceAdmin when !currentUser.IsOwner(tenant):
                    throw new SecurityException(Resource.ErrorAccessDenied);
            }

            var user = await _userManager.GetUserByEmailAsync(invite.Email);
            if (!user.Equals(Constants.LostUser))
            {
                if (user.Status == EmployeeStatus.Terminated)
                {
                    throw new ArgumentException(Resource.ErrorUserDisabled + ": " + invite.Email);
                }

                var type = await _userManager.GetUserTypeAsync(user.Id);

                var comparer = EmployeeTypeComparer.Instance;
                if (comparer.Compare(type, invite.Type) < 0)
                {
                    if (!await userManagerWrapper.UpdateUserTypeAsync(user, invite.Type))
                    {
                        continue;
                    }
                }

                await _userManager.AddUserRelationAsync(currentUser.Id, user.Id);
                continue;
            }

            user = await userManagerWrapper.AddInvitedUserAsync(invite.Email, invite.Type, inDto.Culture, false);

            // the same invitation already went out within the period: no new link, no record, no spent invitation
            await using var acceptance = await studioNotifyService.TryAcceptDocSpaceInviteAsync(user.Email, invite.Type);
            if (acceptance.Accepted)
            {
                var link = commonLinkUtility.GetInvitationLink(user.Email, invite.Type, authContext.CurrentAccount.ID, inDto.Culture);
                var shortenLink = await urlShortener.GetShortenLinkAsync(link);

                await studioNotifyService.SendDocSpaceInviteAsync(user.Email, shortenLink, invite.Type, inDto.Culture, true, acceptance);
                messageService.Send(MessageAction.SendJoinInvite, MessageTarget.Create(user.Id), currentUser.DisplayUserName(displayUserSettingsHelper), user.Email);
            }

            await socketManager.AddUserAsync(user);

            await webhookManager.PublishAsync(WebhookTrigger.UserInvited, user);
        }

        var result = new List<EmployeeDto>();

        var users = (await _userManager.GetUsersAsync()).Where(u => u.ActivationStatus == EmployeeActivationStatus.Pending);

        foreach (var user in users)
        {
            if (await _userManager.CanUserViewAnotherUserAsync(currentUser.Id, user.Id))
            {
                result.Add(await employeeDtoHelper.GetAsync(user));
            }
        }

        return result;
    }

    /// <remarks>
    /// Sets a new password for an account. This is the last step of a password change or a password recovery. Sign the
    /// request with the confirmation token from the emailed link. An expired or used token returns 401. The account
    /// must be `Active`. Otherwise the call returns 403. Only the portal owner can set the password of the portal
    /// owner. If no account has the ID, the call returns 404. Send `passwordHash`, or a plain `password` that the call
    /// checks against the password policy. The call returns 400 if both are missing or the policy rejects the password.
    /// The call changes data. A repeated call with the token of a password change link returns 401. With the token of
    /// an activation link, a repeat with the same password returns 400. It ends the other sessions of the account and
    /// emails it a notice about the change. To get the recovery link, use `POST api/2.0/people/password`.
    /// </remarks>
    /// <summary>Change a user password</summary>
    /// <path>api/2.0/people/{userId}/password</path>
    [Tags("People / Password")]
    [SwaggerResponse(200, "The profile whose password was changed", typeof(EmployeeFullDto))]
    [SwaggerResponse(400, "The user ID is empty, no password was sent, or the password does not meet the portal policy")]
    [SwaggerResponse(403, "The account is not active, or only its owner may change this password")]
    [SwaggerResponse(404, "No account has the specified ID")]
    [AllowNotPayment]
    [HttpPut("{userId:guid}/password")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "PasswordChange,Activation")]
    public async Task<EmployeeFullDto> ChangeUserPassword(ChangePasswordByIdRequestDto inDto)
    {
        await securityContext.AuthByClaimAsync();
        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(inDto.UserId), Constants.Action_EditUser);
        if (inDto.UserId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(inDto.UserId));
        }

        var user = await _userManager.GetUsersAsync(inDto.UserId);
        if (!_userManager.UserExists(user))
        {
            throw new ItemNotFoundException(Resource.ErrorUserNotFound);
        }

        if (_userManager.IsSystemUser(user.Id) || user.Status != EmployeeStatus.Active)
        {
            throw new SecurityException();
        }

        var viewer = await _userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var tenant = tenantManager.GetCurrentTenant();
        if (user.IsOwner(tenant) && viewer.Id != user.Id)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        if (string.IsNullOrEmpty(inDto.ChangePasswordData.PasswordHash) && string.IsNullOrEmpty(inDto.ChangePasswordData.Password))
        {
            throw new ArgumentException(Resource.ErrorPasswordEmpty);
        }

        inDto.ChangePasswordData.PasswordHash = (inDto.ChangePasswordData.PasswordHash ?? "").Trim();
        if (string.IsNullOrEmpty(inDto.ChangePasswordData.PasswordHash))
        {
            inDto.ChangePasswordData.Password = (inDto.ChangePasswordData.Password ?? "").Trim();
            await userManagerWrapper.CheckPasswordPolicyAsync(inDto.ChangePasswordData.Password);
            inDto.ChangePasswordData.PasswordHash = passwordHasher.GetClientPassword(inDto.ChangePasswordData.Password);
        }

        try
        {
            await securityContext.SetUserPasswordHashAsync(inDto.UserId, inDto.ChangePasswordData.PasswordHash);

            var messageTarget = MessageTarget.Create(inDto.UserId);
            await messageService.SendAsync(MessageAction.UserUpdatedPassword, messageTarget);

            var passwordChangeEvent = (await auditEventsRepository.GetByFilterAsync(
                userId: securityContext.CurrentAccount.ID,
                action: MessageAction.UserUpdatedPassword,
                target: messageTarget.ToString(),
                limit: 1))
                .FirstOrDefault();

            await studioNotifyService.SendUserPasswordChangedAsync(user, passwordChangeEvent);

            await cookiesManager.ResetUserCookieAsync(inDto.UserId, false);
            messageService.Send(MessageAction.CookieSettingsUpdated);
        }
        catch (SecurityContext.PasswordException ex)
        {
            throw new ArgumentException(ex.Message);
        }

        return await employeeFullDtoHelper.GetFullAsync(await GetUserInfoAsync(inDto.UserId.ToString()));
    }

    /// <remarks>
    /// Sets a new email address for an account. This is the last step of an email change. Sign the request with the
    /// confirmation token from the emailed link. An expired token, or the token of an earlier link, returns 401. The
    /// account must be `Active`. Otherwise the call returns 403. Only the portal owner can change the address of the portal owner. If no account
    /// has the ID, the call returns 404. Pass the address in `email`, or encrypted in `encEmail` as the link has it.
    /// The call returns 400 if the address is empty or malformed. The call changes data. You can repeat it with the
    /// same body and token. It saves a new address in lowercase and marks the account `Activated`. An address equal to
    /// the current one changes nothing. To send the link, use `POST api/2.0/people/email`.
    /// </remarks>
    /// <summary>Change a user email</summary>
    /// <path>api/2.0/people/{userId}/email</path>
    [Tags("People / Email")]
    [SwaggerResponse(200, "The profile with its new address", typeof(EmployeeFullDto))]
    [SwaggerResponse(400, "The user ID is empty, or the address is missing or malformed")]
    [SwaggerResponse(403, "The account is not active, or only its owner may change this address")]
    [SwaggerResponse(404, "No account has the specified ID")]
    [AllowNotPayment]
    [HttpPut("{userId:guid}/email")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "EmailChange")]
    public async Task<EmployeeFullDto> ChangeUserEmail(ChangeEmailByIdRequestDto inDto)
    {
        await securityContext.AuthByClaimAsync();
        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(inDto.UserId), Constants.Action_EditUser);
        if (inDto.UserId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(inDto.UserId));
        }

        var user = await _userManager.GetUsersAsync(inDto.UserId);
        if (!_userManager.UserExists(user))
        {
            throw new ItemNotFoundException(Resource.ErrorUserNotFound);
        }

        if (_userManager.IsSystemUser(user.Id) || user.Status != EmployeeStatus.Active)
        {
            throw new SecurityException();
        }

        var viewer = await _userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var tenant = tenantManager.GetCurrentTenant();
        if (user.IsOwner(tenant) && viewer.Id != user.Id)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var email = string.IsNullOrEmpty(inDto.ChangeEmailData.Email) && !string.IsNullOrEmpty(inDto.ChangeEmailData.EncEmail)
            ? emailValidationKeyModelHelper.DecryptEmail(inDto.ChangeEmailData.EncEmail)
            : inDto.ChangeEmailData.Email;

        if (string.IsNullOrEmpty(email))
        {
            throw new ArgumentException(Resource.ErrorEmailEmpty);
        }

        if (!email.TestEmailRegex())
        {
            throw new ArgumentException(Resource.ErrorNotCorrectEmail);
        }

        var address = new MailAddress(email);
        if (!string.Equals(address.Address, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = address.Address.ToLowerInvariant();
            user.ActivationStatus = EmployeeActivationStatus.Activated;
            await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);
        }

        return await employeeFullDtoHelper.GetFullAsync(await GetUserInfoAsync(inDto.UserId.ToString()));
    }

    /// <remarks>
    /// Deletes an account and queues the deletion of its data. First disable the account with the `Terminated` status
    /// of `PUT api/2.0/people/status/{status}`. Otherwise the call returns 403. Requires a DocSpace admin. Other
    /// callers get 403. Only the portal owner can delete a DocSpace admin. The call also returns 403 for a system
    /// account or an LDAP account. If no account has the ID, the call returns 404. The call cannot be undone, and a
    /// repeated call returns 404. The account disappears immediately, with its avatar, group memberships, file shares,
    /// and OAuth apps. A queued job then deletes its data. To track the job, use
    /// `GET api/2.0/people/remove/progress/{userId}`. If the account shared files from its own section, the job moves
    /// them to a new folder of the caller. To move rooms and shared files to another user first, use
    /// `POST api/2.0/people/reassign/start`. The call returns the profile as it was before the deletion. To delete
    /// several accounts, use `PUT api/2.0/people/delete`.
    /// </remarks>
    /// <summary>
    /// Delete a user
    /// </summary>
    /// <path>api/2.0/people/{userId}</path>
    [RaisesWebhook(WebhookTrigger.UserDeleted)]
    [RaisesWebhook(WebhookTrigger.FolderCreated, When = "once the data deletion job creates a folder for the files that the account shared")]
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "The profile as it was just before it was deleted", typeof(EmployeeFullDto))]
    [SwaggerResponse(403, "The account is not disabled, is a system or an LDAP account, the caller may not delete a DocSpace administrator, or the OAuth service refused to remove the OAuth clients of the account")]
    [SwaggerResponse(404, "No user has the specified ID")]
    [HttpDelete("{userId}")]
    public async Task<EmployeeFullDto> DeleteMember(GetMemberByIdRequestDto inDto)
    {
        await _permissionContext.DemandPermissionsAsync(Constants.Action_AddRemoveUser);

        var user = await GetUserInfoAsync(inDto.UserId);

        if (_userManager.IsSystemUser(user.Id) || user.IsLDAP())
        {
            throw new SecurityException();
        }

        if (user.Status != EmployeeStatus.Terminated)
        {
            throw new InvalidOperationException("The user is not suspended");
        }

        var currentUser = await _userManager.GetUsersAsync(authContext.CurrentAccount.ID);

        if (await fileSecurityCommon.IsDocSpaceAdministratorAsync(user.Id) && !currentUser.IsOwner(tenantManager.GetCurrentTenant()))
        {
            throw new SecurityException();
        }

        var isGuest = await _userManager.IsGuestAsync(user);

        await CheckReassignProcessAsync([user.Id]);

        var userName = user.DisplayUserName(false, displayUserSettingsHelper);
        var groups = await _userManager.GetUserGroupsAsync(user.Id);

        await webhookManager.PublishAfterAsync(WebhookTrigger.UserDeleted, user, async () =>
        {
            if (!isGuest)
            {
                await client.DeleteClientsAsync(user.Id);
            }

            await _userPhotoManager.RemovePhotoAsync(user.Id);
            await _userManager.DeleteUserAsync(user.Id);
            await fileSecurity.RemoveSubjectAsync(user.Id, true);
            var tenant = tenantManager.GetCurrentTenant();
            await queueWorkerRemove.StartAsync(tenant.Id, user, securityContext.CurrentAccount.ID, false, false, isGuest);

            messageService.Send(MessageAction.UserDeleted, MessageTarget.Create(user.Id), userName);
            if (isGuest)
            {
                await socketManager.DeleteGuestAsync(user.Id);
            }
            else
            {
                await socketManager.DeleteUserAsync(user.Id);
                foreach (var group in groups)
                {
                    var groupInfo = await _userManager.GetGroupInfoAsync(group.ID);
                    var groupDto = await groupFullDtoHelper.Get(groupInfo, true);
                    await socketManager.UpdateGroupAsync(groupDto);
                }
            }
        });

        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <remarks>
    /// Disables the account of the caller at the request of its owner. This is the second step of self-removal. The
    /// first step, `PUT api/2.0/people/self/delete`, emails the confirmation link. Sign the request with the
    /// confirmation token from that link. The portal owner and an LDAP account get 403. If the account no longer
    /// exists, the call returns 404. The call changes data, and a repeated call with the same token returns 401. It
    /// sets the `Terminated` status, ends all sessions of the account, and notifies the DocSpace admins. The account
    /// then cannot sign in. The call keeps its rooms, files, and group memberships. A DocSpace admin can then delete
    /// the account with `DELETE api/2.0/people/{userId}`. Until then, `PUT api/2.0/people/status/{status}` can enable
    /// it again. The call returns the profile with the new status.
    /// </remarks>
    /// <summary>
    /// Close my own profile
    /// </summary>
    /// <path>api/2.0/people/@self</path>
    [RaisesWebhook(WebhookTrigger.UserUpdated, When = "and not `user.deleted`")]
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "The profile of the caller with the Terminated status", typeof(EmployeeFullDto))]
    [SwaggerResponse(403, "The caller is the portal owner, an LDAP account or a system account")]
    [SwaggerResponse(404, "The calling account no longer exists")]
    [HttpDelete("@self")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "ProfileRemove")]
    public async Task<EmployeeFullDto> DeleteProfile()
    {
        await securityContext.AuthByClaimAsync();

        if (_userManager.IsSystemUser(securityContext.CurrentAccount.ID))
        {
            throw new SecurityException();
        }

        var user = await GetUserInfoAsync(securityContext.CurrentAccount.ID.ToString());

        if (!_userManager.UserExists(user))
        {
            throw new Exception(Resource.ErrorUserNotFound);
        }

        var tenant = tenantManager.GetCurrentTenant();
        if (user.IsLDAP() || user.IsOwner(tenant))
        {
            throw new SecurityException();
        }

        await securityContext.AuthenticateMeWithoutCookieAsync(Core.Configuration.Constants.CoreSystem);
        user.Status = EmployeeStatus.Terminated;

        await _userManager.UpdateUserInfoAsync(user);
        await socketManager.UpdateUserAsync(user);
        var userName = user.DisplayUserName(false, displayUserSettingsHelper);
        messageService.Send(MessageAction.UsersUpdatedStatus, MessageTarget.Create(user.Id), userName);

        await cookiesManager.ResetUserCookieAsync(user.Id);
        messageService.Send(MessageAction.CookieSettingsUpdated);

        await studioNotifyService.SendMsgProfileHasDeletedItselfAsync(user);

        await webhookManager.PublishAsync(WebhookTrigger.UserUpdated, user);

        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <remarks>
    /// Removes the listed guests from the guest list of the caller. The call also deletes the shares that the caller
    /// gave them. It does not delete the accounts. Each guest keeps its profile and the access that other users gave
    /// it. Requires a room admin or a DocSpace admin. Other callers get 403. Each listed account must exist, be an
    /// active guest, and be a guest of the caller. Otherwise the call returns 403 and changes nothing. The call changes
    /// data, and a repeated call returns 403. If `userIds` is missing, the call returns 400. It returns no body. To see
    /// the remaining guests, use `GET api/2.0/people/filter` with `area` set to `Guests`. To delete a guest account,
    /// disable it, then use `DELETE api/2.0/people/{userId}`.
    /// </remarks>
    /// <summary>
    /// Remove guest relations
    /// </summary>
    /// <path>api/2.0/people/guests</path>
    [Tags("People / Guests")]
    [SwaggerResponse(200, "The guests are no longer linked to the caller. No content is returned")]
    [SwaggerResponse(400, "The userIds field is missing")]
    [SwaggerResponse(403, "The caller is not an admin, or an entry is not an active guest of the caller")]
    [HttpDelete("guests")]
    public async Task DeleteGuests(UpdateMembersRequestDto inDto)
    {
        ArgumentNullException.ThrowIfNull(inDto?.UserIds);

        var currentUser = await _userManager.GetUsersAsync(authContext.CurrentAccount.ID);

        var type = await _userManager.GetUserTypeAsync(currentUser.Id);
        if (type is not (EmployeeType.DocSpaceAdmin or EmployeeType.RoomAdmin))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var relations = await _userManager.GetUserRelationsAsync(currentUser.Id);
        var users = await inDto.UserIds.ToAsyncEnumerable().Select(async (Guid x, CancellationToken _) => await _userManager.GetUsersAsync(x)).ToListAsync();


        foreach (var user in users)
        {
            if (user.Equals(Constants.LostUser) ||
                user.Status == EmployeeStatus.Terminated ||
                !await _userManager.IsGuestAsync(user) ||
                !relations.ContainsKey(user.Id))
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }
        }

        foreach (var user in users)
        {
            var t1 = _userManager.DeleteUserRelationAsync(currentUser.Id, user.Id);
            var t2 = fileSecurity.RemoveSecuritiesAsync(user.Id, currentUser.Id, SubjectType.User);

            await Task.WhenAll(t1, t2).ContinueWith(async _ => await socketManager.DeleteGuestAsync(currentUser.Id, user.Id));
        }
    }

    /// <remarks>
    /// Returns a link that lets another admin add a guest of the caller to their own guest list. Any signed-in user can
    /// call it, except a guest. A guest gets 403. If the caller cannot see the account, the call also returns 403. If
    /// the account is not a guest, the call returns 400. If no account has the ID, the call returns 404. The call is
    /// read-only. It returns a short confirmation link. Give it to the room admin or DocSpace admin who should get the
    /// guest. They accept it with `POST api/2.0/people/guests/share/approve`. The link has a token that expires, so
    /// create it right before use.
    /// </remarks>
    /// <summary>
    /// Get a guest sharing link
    /// </summary>
    /// <path>api/2.0/people/guests/{userId}/share</path>
    [Tags("Portal / Guests")]
    [SwaggerResponse(200, "The shortened confirmation link, as plain text", typeof(string))]
    [SwaggerResponse(400, "The account is not a guest")]
    [SwaggerResponse(403, "The caller is a guest, or is not allowed to see that account")]
    [SwaggerResponse(404, "No account has the specified ID")]
    [HttpGet("guests/{userId:guid}/share")]
    public async Task<string> GetGuestSharingLink(GuestShareRequestDto inDto)
    {
        var targetUser = await _userManager.GetUsersAsync(inDto.UserId);

        if (Equals(targetUser, Constants.LostUser))
        {
            throw new ItemNotFoundException("User not found");
        }

        var currentUserId = authContext.CurrentAccount.ID;
        var currentUser = await _userManager.GetUsersAsync(currentUserId);
        var targetUserType = await _userManager.GetUserTypeAsync(targetUser);

        if (targetUserType is not EmployeeType.Guest)
        {
            throw new ArgumentException("User is not a guest");
        }

        if (await _userManager.GetUserTypeAsync(currentUser) is EmployeeType.Guest ||
            !await _userManager.CanUserViewAnotherUserAsync(currentUserId, targetUser.Id))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var link = commonLinkUtility.GetConfirmationEmailUrl(targetUser.Email, ConfirmType.GuestShareLink, $"{currentUserId}{inDto.UserId}", currentUserId);

        return await urlShortener.GetShortenLinkAsync(link);
    }

    /// <remarks>
    /// Adds a shared guest to the guest list of the caller. Sign the request with the confirmation token from the link
    /// of `GET api/2.0/people/guests/{userId}/share`. An expired or used token returns 401. The call takes all data
    /// from the token and ignores the body. Requires a room admin or a DocSpace admin. Other callers get 403. If the
    /// account of the token no longer exists, the call returns 404. If it is no longer a guest, the call returns 400.
    /// The call changes data. You can repeat it, and the call returns the guest again. It returns the full profile of
    /// the guest.
    /// </remarks>
    /// <summary>
    /// Approve a guest sharing link
    /// </summary>
    /// <path>api/2.0/people/guests/share/approve</path>
    [Tags("People / Guests")]
    [SwaggerResponse(200, "The full profile of the guest now linked to the caller", typeof(EmployeeFullDto))]
    [SwaggerResponse(400, "The account named by the token is not a guest")]
    [SwaggerResponse(403, "The caller is a member or a guest")]
    [SwaggerResponse(404, "The account named by the token no longer exists")]
    [AllowNotPayment]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "GuestShareLink")]
    [HttpPost("guests/share/approve")]
    public async Task<EmployeeFullDto> ApproveGuestShareLink(EmailMemberRequestDto _)
    {
        await securityContext.AuthByClaimAsync();

        var model = emailValidationKeyModelHelper.GetModel();
        var targetEmail = emailValidationKeyModelHelper.DecryptEmail(model.EncEmail);
        var targetUser = await _userManager.GetUserByEmailAsync(targetEmail);

        if (Equals(targetUser, Constants.LostUser))
        {
            throw new ItemNotFoundException("User not found");
        }

        var targetUserType = await _userManager.GetUserTypeAsync(targetUser.Id);

        if (targetUserType is not EmployeeType.Guest)
        {
            throw new ArgumentException("User is not a guest");
        }

        var currentUserId = authContext.CurrentAccount.ID;
        var currentUserType = await _userManager.GetUserTypeAsync(currentUserId);

        if (currentUserType is EmployeeType.Guest or EmployeeType.User)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        if (!await _userManager.CanUserViewAnotherUserAsync(currentUserId, targetUser.Id))
        {
            await _userManager.AddUserRelationAsync(currentUserId, targetUser.Id);
        }

        return await employeeFullDtoHelper.GetFullAsync(targetUser);
    }

    /// <remarks>
    /// Searches the accounts with the status from the route. The term in `query` must be in the name, user name, email,
    /// or contacts. Requires a DocSpace admin. Other callers get 403, including a room admin. The call is read-only. It
    /// matches the term as a substring and ignores case. If `query` is missing, the call returns 400. With `filterBy`
    /// set to `group`, `filterValue` is a group ID. The call then keeps only the members of that group. The list has no
    /// paging, no total, and no fixed order. For a page with a total, use `GET api/2.0/people/filter`.
    /// </remarks>
    /// <summary>
    /// Search users by status filter
    /// </summary>
    /// <path>api/2.0/people/status/{status}/search</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "The full profiles of the matching accounts", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or `query` is missing")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
    [HttpGet("status/{status}/search")]
    public async IAsyncEnumerable<EmployeeFullDto> SearchUsersByStatus(AdvancedSearchDto inDto)
    {
        if (!await _userManager.IsDocSpaceAdminAsync(securityContext.CurrentAccount.ID))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var list = (await _userManager.GetUsersAsync(inDto.Status)).ToAsyncEnumerable();

        if ("group".Equals(inDto.FilterBy, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(inDto.Text))
        {
            var groupId = new Guid(inDto.Text);
            //Filter by group
            list = list.Where(async (x, _) => await _userManager.IsUserInGroupAsync(x.Id, groupId));
        }

        list = list.Where(x => x.FirstName != null && x.FirstName.Contains(inDto.Query, StringComparison.OrdinalIgnoreCase) ||
                               (x.LastName != null && x.LastName.Contains(inDto.Query, StringComparison.OrdinalIgnoreCase)) ||
                               (x.UserName != null && x.UserName.Contains(inDto.Query, StringComparison.OrdinalIgnoreCase)) ||
                               (x.Email != null && x.Email.Contains(inDto.Query, StringComparison.OrdinalIgnoreCase)) ||
                               (x.ContactsList != null && x.ContactsList.Exists(y => y.Contains(inDto.Query, StringComparison.OrdinalIgnoreCase))));

        await foreach (var item in list)
        {
            yield return await employeeFullDtoHelper.GetFullAsync(item);
        }
    }

    /// <remarks>
    /// Returns a page of the active accounts of the portal, with full profiles. The list has no disabled accounts and
    /// no pending invitations. For them, use `GET api/2.0/people/status/{status}`. Requires a room admin, a DocSpace
    /// admin, or an admin of the People module. Other callers get 403. The call is read-only. The response has the
    /// total number of matches. If `count` is out of range, the call returns 400. With `filterBy` set to `group`,
    /// `filterValue` is a group ID. The call then also matches that ID against names and emails, so it usually finds
    /// nothing. To filter by group, use `GET api/2.0/people/filter` with `groupId`.
    /// </remarks>
    /// <summary>
    /// Get the active profiles
    /// </summary>
    /// <path>api/2.0/people</path>
    /// <collection>list</collection>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "A page of active accounts, with their full profiles", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
    [SwaggerResponse(403, "The caller is a member or a guest")]
    [HttpGet]
    public IAsyncEnumerable<EmployeeFullDto> GetAllProfiles(GetAllProfilesRequestDto inDto)
    {
        var status = new GetByStatusRequestDto
        {
            Status = EmployeeStatus.Active,
            FilterBy = inDto.FilterBy,
            Count = inDto.Count,
            StartIndex = inDto.StartIndex,
            SortBy = inDto.SortBy,
            SortOrder = inDto.SortOrder,
            FilterSeparator = inDto.FilterSeparator,
            Text = inDto.Text
        };

        return GetByStatus(status);
    }

    /// <remarks>
    /// Returns whether an email address belongs to an account, and the status of that account. Pass the address in
    /// `email`, or encrypted in `encemail` as an invitation link has it. If both are missing, the call returns 400. It
    /// also returns 400 for a malformed address or one over 255 characters. Any signed-in user can call it, including a
    /// guest. The token of an invitation link or a guest sharing link also works. The call is read-only and works on an
    /// unpaid portal. If no account has the address, the response has no `status`. The call does not say who owns the
    /// address. For the profile, use `GET api/2.0/people/email`.
    /// </remarks>
    /// <summary>
    /// Check whether an email is taken
    /// </summary>
    /// <path>api/2.0/people/exists</path>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "Whether the address is taken, and the status of the profile that holds it", typeof(UserExistsResponseDto))]
    [SwaggerResponse(400, "Both email and encemail are missing, or the address is malformed or longer than 255 characters")]
    [AllowNotPayment]
    [HttpGet("exists")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "LinkInvite,GuestShareLink,Authenticated")]
    public async Task<UserExistsResponseDto> CheckUserExistsByEmail(GetMemberByEmailRequestDto inDto)
    {
        var email = string.IsNullOrEmpty(inDto.Email) && !string.IsNullOrEmpty(inDto.EncEmail)
            ? emailValidationKeyModelHelper.DecryptEmail(inDto.EncEmail)
            : inDto.Email;

        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var user = await _userManager.GetUserByEmailAsync(email.Trim());
        var exists = user.Id != Constants.LostUser.Id;

        return new UserExistsResponseDto
        {
            Exists = exists,
            Status = exists ? user.Status : null
        };
    }

    /// <remarks>
    /// Returns the full profile of the account with an email address. Pass the address in `email`, or encrypted in
    /// `encemail` as an invitation link has it. If both are missing, the call returns 400. It also returns 400 for a
    /// malformed address or one over 255 characters. Any signed-in user can call it. If the caller cannot see the
    /// account, the call returns 403. A guest, for example, sees only the accounts related to it. If no account has the
    /// address, the call returns 404. The call is read-only. The `culture` parameter only sets the language of the
    /// error message. To check an address without the right to see its owner, use `GET api/2.0/people/exists`.
    /// </remarks>
    /// <summary>
    /// Get a profile by user email
    /// </summary>
    /// <path>api/2.0/people/email</path>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "The full profile of the account that owns the address", typeof(EmployeeFullDto))]
    [SwaggerResponse(400, "Both email and encemail are missing, or the address is malformed or longer than 255 characters")]
    [SwaggerResponse(403, "The caller is not allowed to see that account")]
    [SwaggerResponse(404, "No account owns the specified address")]
    [HttpGet("email")]
    public async Task<EmployeeFullDto> GetProfileByEmail(GetMemberByEmailRequestDto inDto)
    {
        var cultureInfo = string.IsNullOrEmpty(inDto.Culture) ? null : new CultureInfo(inDto.Culture);

        var email = string.IsNullOrEmpty(inDto.Email) && !string.IsNullOrEmpty(inDto.EncEmail)
            ? emailValidationKeyModelHelper.DecryptEmail(inDto.EncEmail)
            : inDto.Email;

        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var user = await _userManager.GetUserByEmailAsync(email.Trim());

        if (user.Id == Constants.LostUser.Id)
        {
            throw new ItemNotFoundException(Resource.ResourceManager.GetString("ErrorUserNotFound", cultureInfo));
        }

        if (!await _userManager.CanUserViewAnotherUserAsync(authContext.CurrentAccount.ID, user.Id))
        {
            throw new SecurityException(Resource.ResourceManager.GetString("ErrorAccessDenied", cultureInfo));
        }

        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <remarks>
    /// Returns the profile of an account by its user name or ID. The call looks for a user name first, then for an ID.
    /// Any signed-in user can call it. If the caller cannot see the account, the call returns 403. A guest, for
    /// example, sees only the accounts related to it. If nothing matches, the call returns 404. A request with the
    /// token of an invitation link skips the visibility check. It gets a short profile with only the identifying
    /// fields. The call is read-only and works on an unpaid portal. For the profile of the caller, use
    /// `GET api/2.0/people/@self`.
    /// </remarks>
    /// <summary>
    /// Get a profile by user ID
    /// </summary>
    /// <path>api/2.0/people/{userId}</path>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "The full profile, or a reduced one for a request authenticated with an invitation link", typeof(EmployeeFullDto))]
    [SwaggerResponse(403, "The caller is not allowed to see that account")]
    [SwaggerResponse(404, "No account has the specified ID or user name")]
    [AllowNotPayment]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "LinkInvite,Authenticated")]
    [HttpGet("{userId}", Order = 1)]
    public async Task<EmployeeFullDto> GetProfileByUserId(GetMemberByIdRequestDto inDto)
    {
        var isInvite = _httpContextAccessor.HttpContext!.User.Claims
               .Any(role => role.Type == ClaimTypes.Role && ConfirmTypeExtensions.TryParse(role.Value, out var confirmType) && confirmType == ConfirmType.LinkInvite);

        await securityContext.AuthByClaimAsync();

        var user = await _userManager.GetUserByUserNameAsync(inDto.UserId);
        if (user.Id == Constants.LostUser.Id)
        {
            if (Guid.TryParse(inDto.UserId, out var userId))
            {
                user = await _userManager.GetUsersAsync(userId);
            }
            else
            {
                logger.ErrorCouldNotGetUserByName(securityContext.CurrentAccount.ID, inDto.UserId);
            }
        }

        if (user.Id == Constants.LostUser.Id)
        {
            throw new ItemNotFoundException(Resource.ErrorUserNotFound);
        }

        if (isInvite)
        {
            return await employeeFullDtoHelper.GetSimple(user, false);
        }


        if (!await _userManager.CanUserViewAnotherUserAsync(authContext.CurrentAccount.ID, user.Id))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <remarks>
    /// Returns a page of the accounts with the status from the route, with full profiles. Requires a room admin, a
    /// DocSpace admin, or an admin of the People module. Other callers get 403. The call is read-only. The response has
    /// the total number of matches. If `count` is out of range, the call returns 400. The call filters the accounts by
    /// name and email with `filterValue`. With `filterBy` set to `group`, `filterValue` is also a group ID. The call
    /// then matches that ID against names and emails too, so it usually finds nothing. To filter by group, use
    /// `GET api/2.0/people/filter` with `groupId`. For active accounts only, the same call is `GET api/2.0/people`.
    /// </remarks>
    /// <summary>
    /// Get profiles by status
    /// </summary>
    /// <path>api/2.0/people/status/{status}</path>
    /// <collection>list</collection>
    [Tags("People / User status")]
    [SwaggerResponse(200, "A page of accounts in the requested state, with their full profiles", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
    [SwaggerResponse(403, "The caller is a member or a guest")]
    [HttpGet("status/{status}")]
    public IAsyncEnumerable<EmployeeFullDto> GetByStatus(GetByStatusRequestDto inDto)
    {
        Guid? groupId = null;
        if ("group".Equals(inDto.FilterBy, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(inDto.Text))
        {
            groupId = new Guid(inDto.Text);
        }

        var filter = new SimpleByFilterRequestDto
        {
            EmployeeStatus = inDto.Status,
            GroupId = groupId,
            WithoutGroup = false,
            ExcludeGroup = false,
            InvitedByMe = false,
            InviterId = null,
            Count = inDto.Count,
            StartIndex = inDto.StartIndex,
            SortBy = inDto.SortBy,
            SortOrder = inDto.SortOrder,
            FilterSeparator = inDto.FilterSeparator,
            Text = inDto.Text
        };
        return SearchUsersByExtendedFilter(filter);
    }

    /// <remarks>
    /// Returns a page of the accounts that match all the given filters, with full profiles. Requires a room admin, a
    /// DocSpace admin, or an admin of the People module. Other callers get 403. A DocSpace admin sees more accounts
    /// than a room admin. The call is read-only and works on an unpaid portal. The response has the total number of
    /// matches. If `count` is out of range, the call returns 400. If both are set, `employeeType` wins over
    /// `employeeTypes`. With `withoutGroup`, the call ignores `groupId`. The `area` filter cancels the type filters
    /// that contradict it. For short profiles with the same filters, use `GET api/2.0/people/simple/filter`.
    /// </remarks>
    /// <summary>
    /// Filter users in detail
    /// </summary>
    /// <path>api/2.0/people/filter</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "A page of matching accounts, with their full profiles", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
    [SwaggerResponse(403, "The caller is a member or a guest")]
    [AllowNotPayment]
    [HttpGet("filter")]
    public async IAsyncEnumerable<EmployeeFullDto> SearchUsersByExtendedFilter(SimpleByFilterRequestDto inDto)
    {
        var filter = new UserFilter
        {
            EmployeeStatus = inDto.EmployeeStatus,
            GroupId = inDto.GroupId,
            ActivationStatus = inDto.ActivationStatus,
            EmployeeType = inDto.EmployeeType,
            EmployeeTypes = inDto.EmployeeTypes,
            IsDocSpaceAdministrator = inDto.IsAdministrator,
            Payments = inDto.Payments,
            AccountLoginType = inDto.AccountLoginType,
            QuotaFilter = inDto.QuotaFilter,
            WithoutGroup = inDto.WithoutGroup,
            ExcludeGroup = inDto.ExcludeGroup,
            Area = inDto.Area,
            InvitedByMe = inDto.InvitedByMe,
            InviterId = inDto.InviterId,
            Count = inDto.Count,
            StartIndex = inDto.StartIndex,
            SortBy = inDto.SortBy,
            SortOrder = inDto.SortOrder,
            FilterSeparator = inDto.FilterSeparator,
            Text = inDto.Text
        };

        var users = GetByFilterAsync(filter);

        await foreach (var user in users)
        {
            yield return await employeeFullDtoHelper.GetFullAsync(user);
        }
    }

    /// <remarks>
    /// Returns the information about the "People" module.
    /// </remarks>
    /// <summary>Get the People information</summary>
    /// <path>api/2.0/people/info</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("People / Module")]
    [SwaggerResponse(200, "Module information", typeof(Module))]
    [HttpGet("info")]
    public Module GetPeopleModule()
    {
        var product = new PeopleProduct();
        product.Init();

        return new Module(product);
    }

    /// <remarks>
    /// Searches the active accounts of the portal by the term in `query`. Requires a DocSpace admin. Other callers get
    /// 403, including a room admin. The call skips pending and disabled accounts. The call is read-only. It returns all
    /// matches with full profiles, without paging or a total. It has no group filter. To filter by group, use
    /// `GET api/2.0/people/@search/{query}`. To search in all states, use `GET api/2.0/people/filter`.
    /// </remarks>
    /// <summary>Search users by query</summary>
    /// <path>api/2.0/people/search</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "The full profiles of the matching active accounts", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
    [HttpGet("search")]
    public IAsyncEnumerable<EmployeeFullDto> SearchUsersByQuery(GetPeopleByQueryRequestDto inDto)
    {
        var query = new GetMemberByQueryRequestDto { Query = inDto.Query };
        return GetSearch(query);
    }

    /// <remarks>
    /// Searches the active accounts of the portal by the term in the path. Requires a DocSpace admin. Other callers get
    /// 403, including a room admin. The call skips pending and disabled accounts. The call is read-only. It returns all
    /// matches with full profiles, without paging or a total. With `filterBy` set to `group`, `filterValue` is a group
    /// ID. The call then keeps only the members of that group. To search in all states, use
    /// `GET api/2.0/people/filter`.
    /// </remarks>
    /// <summary>Search users</summary>
    /// <path>api/2.0/people/@search/{query}</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "The full profiles of the matching active accounts", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
    [HttpGet("@search/{query}")]
    public async IAsyncEnumerable<EmployeeFullDto> GetSearch(GetMemberByQueryRequestDto inDto)
    {
        if (!await _userManager.IsDocSpaceAdminAsync(securityContext.CurrentAccount.ID))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var groupId = Guid.Empty;
        if ("group".Equals(inDto.FilterBy, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(inDto.Text))
        {
            groupId = new Guid(inDto.Text);
        }

        var users = await _userManager.SearchAsync(inDto.Query, EmployeeStatus.Active, groupId);

        foreach (var user in users)
        {
            yield return await employeeFullDtoHelper.GetFullAsync(user);
        }
    }

    /// <remarks>
    /// Returns a page of the accounts that match all the given filters, with short profiles. Requires a room admin, a
    /// DocSpace admin, or an admin of the People module. Other callers get 403. A DocSpace admin sees more accounts
    /// than a room admin. The call is read-only. The response has the total number of matches. If `count` is out of
    /// range, the call returns 400. If both are set, `employeeType` wins over `employeeTypes`. With `withoutGroup`, the
    /// call ignores `groupId`. The `area` filter cancels the type filters that contradict it. For full profiles with
    /// the same filters, use `GET api/2.0/people/filter`.
    /// </remarks>
    /// <summary>
    /// Filter users in brief
    /// </summary>
    /// <path>api/2.0/people/simple/filter</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "A page of matching accounts, with their short profiles", typeof(IAsyncEnumerable<EmployeeDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
    [SwaggerResponse(403, "The caller is a member or a guest")]
    [HttpGet("simple/filter")]
    public async IAsyncEnumerable<EmployeeDto> GetSimpleByFilter(SimpleByFilterRequestDto inDto)
    {
        var filter = new UserFilter
        {
            EmployeeStatus = inDto.EmployeeStatus,
            GroupId = inDto.GroupId,
            ActivationStatus = inDto.ActivationStatus,
            EmployeeType = inDto.EmployeeType,
            EmployeeTypes = inDto.EmployeeTypes,
            IsDocSpaceAdministrator = inDto.IsAdministrator,
            Payments = inDto.Payments,
            AccountLoginType = inDto.AccountLoginType,
            QuotaFilter = inDto.QuotaFilter,
            WithoutGroup = inDto.WithoutGroup,
            ExcludeGroup = inDto.ExcludeGroup,
            Area = inDto.Area,
            InvitedByMe = inDto.InvitedByMe,
            InviterId = inDto.InviterId,
            Count = inDto.Count,
            StartIndex = inDto.StartIndex,
            SortBy = inDto.SortBy,
            SortOrder = inDto.SortOrder,
            FilterSeparator = inDto.FilterSeparator,
            Text = inDto.Text
        };

        var users = GetByFilterAsync(filter);

        await foreach (var user in users)
        {
            yield return await employeeDtoHelper.GetAsync(user);
        }
    }

    /// <remarks>
    /// Deletes several accounts and queues the deletion of their data. First disable each account with the `Terminated`
    /// status of `PUT api/2.0/people/status/{status}`. If any listed account is still active, the call returns 403 and
    /// deletes nothing. Requires a DocSpace admin. Other callers get 403. Only the portal owner can delete a DocSpace
    /// admin. The call skips the others without an error. It ignores system and LDAP accounts and leaves them out of
    /// the response. The call cannot be undone, and a repeated call returns 403. A deleted or unknown account ID also
    /// returns 403. If the call skipped every account, a repeat changes nothing. The call returns the listed accounts,
    /// including the skipped DocSpace admins. To check a deletion, read the account with `GET api/2.0/people/{userId}`.
    /// A queued job deletes the data of each account. To track it, use `GET api/2.0/people/remove/progress/{userId}`.
    /// If an account shared files from its own section, the job moves them to a new folder of the caller. To move rooms
    /// and shared files to another user first, use `POST api/2.0/people/reassign/start`. If `userIds` is missing, the
    /// call returns 400.
    /// </remarks>
    /// <summary>
    /// Delete users
    /// </summary>
    /// <path>api/2.0/people/delete</path>
    /// <collection>list</collection>
    [RaisesWebhook(WebhookTrigger.UserDeleted, When = "for every account it deletes, not for a skipped one")]
    [RaisesWebhook(WebhookTrigger.FolderCreated, When = "for every deleted account that shared files, once the job creates a folder for them")]
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "Every account that was asked for, including the ones that were skipped", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(400, "The userIds field is missing")]
    [SwaggerResponse(403, "No permissions to perform this action, one of the listed accounts is not disabled, or the OAuth service refused to remove the OAuth clients of an account")]
    [HttpPut("delete", Order = -1)]
    public async IAsyncEnumerable<EmployeeFullDto> RemoveUsers(UpdateMembersRequestDto inDto)
    {
        ArgumentNullException.ThrowIfNull(inDto?.UserIds);

        await _permissionContext.DemandPermissionsAsync(Constants.Action_AddRemoveUser);

        await CheckReassignProcessAsync(inDto.UserIds);

        var users = await inDto.UserIds
            .ToAsyncEnumerable()
            .Select(async (Guid userId, CancellationToken _) => await _userManager.GetUsersAsync(userId))
            .Where(u => !_userManager.IsSystemUser(u.Id) && !u.IsLDAP())
            .ToListAsync();

        if (users.Any(u => u.Status != EmployeeStatus.Terminated))
        {
            throw new InvalidOperationException("Users are not suspended");
        }

        var userNames = users.Select(x => x.DisplayUserName(false, displayUserSettingsHelper)).ToList();
        var tenant = tenantManager.GetCurrentTenant();
        var currentUser = await _userManager.GetUsersAsync(authContext.CurrentAccount.ID);
        var currentUserType = await _userManager.GetUserTypeAsync(currentUser.Id);

        foreach (var user in users)
        {
            var userType = await _userManager.GetUserTypeAsync(user.Id);
            switch (userType)
            {
                case EmployeeType.RoomAdmin when currentUserType is not EmployeeType.DocSpaceAdmin:
                case EmployeeType.DocSpaceAdmin when !currentUser.IsOwner(tenant):
                    continue;
            }

            var isGuest = userType == EmployeeType.Guest;

            await webhookManager.PublishAfterAsync(WebhookTrigger.UserDeleted, user, async () =>
            {
                if (!isGuest)
                {
                    await client.DeleteClientsAsync(user.Id);
                }

                await _userPhotoManager.RemovePhotoAsync(user.Id);
                await _userManager.DeleteUserAsync(user.Id);
                await fileSecurity.RemoveSubjectAsync(user.Id, true);
                await queueWorkerRemove.StartAsync(tenant.Id, user, securityContext.CurrentAccount.ID, false, false, isGuest);
            });
        }

        messageService.Send(MessageAction.UsersDeleted, MessageTarget.Create(users.Select(x => x.Id)), userNames);

        foreach (var user in users)
        {
            yield return await employeeFullDtoHelper.GetFullAsync(user);
        }
    }

    /// <remarks>
    /// Sends the invitation or activation email again to accounts that have not finished joining. With `resendAll`
    /// set to true, the call reaches every pending account and ignores `userIds`. Requires a room admin or a DocSpace
    /// admin for this mode. Other callers get 403. Without it, the call reaches only the listed accounts. A user or a
    /// guest can list only their own ID, and other IDs return 403. A pending invitation gets a new invitation link.
    /// An account that has not confirmed its email gets activation instructions. The call skips active and disabled
    /// accounts, and accounts the caller cannot invite, without an error. The call changes data and sends emails. By
    /// default, an account that got this email in the last 10 minutes gets no new one. Its earlier link stays valid. So
    /// you can repeat the call. A later repeat sends the emails again. The response lists only the targeted accounts
    /// that the caller can see. So it is not a delivery report. If `userIds` is missing and `resendAll` is false, the
    /// call returns 400. To invite a new address, use `POST api/2.0/people/invite`.
    /// </remarks>
    /// <summary>
    /// Resend activation emails
    /// </summary>
    /// <path>api/2.0/people/invite</path>
    /// <collection>list</collection>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "The targeted accounts the caller is allowed to see", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(400, "The request body cannot be read, or `userIds` is missing while `resendAll` is false")]
    [SwaggerResponse(403, "A member or a guest asked for resendAll, or listed an account other than their own")]
    [AllowNotPayment]
    [HttpPut("invite")]
    public async IAsyncEnumerable<EmployeeFullDto> ResendUserInvites(UpdateMembersRequestDto inDto)
    {
        List<UserInfo> users;

        var currentUser = await _userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        if (currentUser == null || currentUser.Equals(Constants.LostUser))
        {
            throw new Exception(Resource.ErrorAccessDenied);
        }

        var currentUserType = await _userManager.GetUserTypeAsync(currentUser);
        var currentUserIsNotAdmin = currentUserType is EmployeeType.User or EmployeeType.Guest;

        var tenant = tenantManager.GetCurrentTenant();

        if (inDto.ResendAll)
        {
            if (currentUserIsNotAdmin)
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }

            users = (await _userManager.GetUsersAsync())
                .Where(u => u.ActivationStatus == EmployeeActivationStatus.Pending)
                .ToList();
        }
        else
        {
            if (currentUserIsNotAdmin && inDto.UserIds.Any(x => x != currentUser.Id))
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }

            users = await inDto.UserIds.ToAsyncEnumerable()
                .Where(userId => !_userManager.IsSystemUser(userId))
                .Select(async (Guid userId, CancellationToken _) => await _userManager.GetUsersAsync(userId))
                .ToListAsync();
        }

        Dictionary<Guid, UserRelation> userRelations = null;
        List<UserInfo> sent = [];

        foreach (var user in users)
        {
            if (user.IsActive || user.Status == EmployeeStatus.Terminated)
            {
                continue;
            }

            if (currentUserType == EmployeeType.DocSpaceAdmin || currentUser.Id == user.Id)
            {
                var activationStatus = user.ActivationStatus;

                if (user.ActivationStatus == EmployeeActivationStatus.Activated)
                {
                    user.ActivationStatus = EmployeeActivationStatus.NotActivated;
                }
                if (user.ActivationStatus == (EmployeeActivationStatus.AutoGenerated | EmployeeActivationStatus.Activated))
                {
                    user.ActivationStatus = EmployeeActivationStatus.AutoGenerated;
                }

                // an account left as it was is not saved: the save would only move its modification date, once per
                // account on every call
                if (user.ActivationStatus != activationStatus)
                {
                    await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);
                }
            }

            if (user.ActivationStatus == EmployeeActivationStatus.Pending)
            {
                var type = await _userManager.GetUserTypeAsync(user.Id);
                if (currentUser.Id != user.Id && !await HasAccessInviteAsync(type, currentUser, user))
                {
                    continue;
                }

                await using var acceptance = await studioNotifyService.TryAcceptDocSpaceInviteAsync(user.Email, type);
                if (!acceptance.Accepted)
                {
                    continue;
                }

                var culture = user.GetCulture()?.Name;
                var link = commonLinkUtility.GetInvitationLink(user.Email, type, authContext.CurrentAccount.ID, culture);
                var shortenLink = await urlShortener.GetShortenLinkAsync(link);
                messageService.Send(MessageAction.SendJoinInvite, MessageTarget.Create(user.Id), currentUser.DisplayUserName(displayUserSettingsHelper), user.Email);
                await studioNotifyService.SendDocSpaceInviteAsync(user.Email, shortenLink, type, culture);
                acceptance.Complete();
                sent.Add(user);
            }
            else
            {
                if (currentUser.Id != user.Id)
                {
                    var type = await _userManager.GetUserTypeAsync(user.Id);
                    if (!await HasAccessInviteAsync(type, currentUser, user))
                    {
                        continue;
                    }
                }

                if (await studioNotifyService.SendEmailActivationInstructionsAsync(user, user.Email))
                {
                    sent.Add(user);
                }
            }
        }

        // only the accounts that got a letter: a repeat within the period is not mailed and not recorded
        if (sent.Count > 0)
        {
            messageService.Send(MessageAction.UsersSentActivationInstructions, MessageTarget.Create(sent.Select(x => x.Id)),
                sent.Select(x => x.DisplayUserName(false, displayUserSettingsHelper)));
        }

        foreach (var user in users)
        {
            if (await _userManager.CanUserViewAnotherUserAsync(currentUser.Id, user.Id))
            {
                yield return await employeeFullDtoHelper.GetFullAsync(user);
            }
        }

        yield break;

        async Task<bool> HasAccessInviteAsync(EmployeeType type, UserInfo currentUser, UserInfo user)
        {
            if (currentUserType == EmployeeType.Guest)
            {
                return false;
            }

            if (currentUserType != EmployeeType.DocSpaceAdmin &&
                type == EmployeeType.Guest &&
                user.CreatedBy.HasValue &&
                user.CreatedBy.Value != currentUser.Id)
            {
                userRelations ??= await _userManager.GetUserRelationsAsync(currentUser.Id);
                if (!userRelations.ContainsKey(user.Id))
                {
                    return false;
                }
            }

            switch (type)
            {
                case EmployeeType.DocSpaceAdmin when currentUser.IsOwner(tenant):
                case EmployeeType.RoomAdmin when currentUserType is EmployeeType.DocSpaceAdmin:
                case EmployeeType.User when currentUserType != EmployeeType.User:
                case EmployeeType.Guest:
                    return true;
            }

            return false;
        }
    }

    /// <remarks>
    /// Returns the interface theme of the caller, which is `Base`, `Dark`, or `System`. The theme belongs to the
    /// account, not to the portal. Any signed-in user can call it, including a guest. The call is read-only. If the
    /// caller has never chosen a theme, the call returns the portal default. The `theme` field of
    /// `GET api/2.0/people/@self` has the same value.
    /// </remarks>
    /// <summary>
    /// Get the portal theme
    /// </summary>
    /// <path>api/2.0/people/theme</path>
    [Tags("People / Theme")]
    [SwaggerResponse(200, "The interface theme of the calling account", typeof(DarkThemeSettingsDto))]
    [HttpGet("theme")]
    public async Task<DarkThemeSettingsDto> GetPortalTheme()
    {
        var settings = await settingsManager.LoadForCurrentUserAsync<DarkThemeSettings>();

        return settings.Map();
    }

    /// <remarks>
    /// Changes the interface theme of the caller to `Base`, `Dark`, or `System`. The theme belongs to the account, not
    /// to the portal. Any signed-in user can call it, including a guest. The call changes data. You can repeat it with
    /// the same body. It returns the saved theme. If the body has no `theme`, the call returns 400. The `theme` field
    /// of `GET api/2.0/people/@self` has the same value.
    /// </remarks>
    /// <summary>
    /// Change the portal theme
    /// </summary>
    /// <path>api/2.0/people/theme</path>
    [Tags("People / Theme")]
    [SwaggerResponse(200, "The interface theme that was stored", typeof(DarkThemeSettingsDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `theme`")]
    [HttpPut("theme")]
    public async Task<DarkThemeSettingsDto> ChangePortalTheme(DarkThemeSettingsRequestDto inDto)
    {
        var darkThemeSettings = new DarkThemeSettings
        {
            Theme = inDto.Theme
        };

        await settingsManager.SaveForCurrentUserAsync(darkThemeSettings);

        return darkThemeSettings.Map();
    }

    /// <remarks>
    /// Returns the profile of the caller, with session details that no other call returns. Any signed-in user can call
    /// it, including a guest. The call is read-only and works on an unpaid portal. This call alone fills four fields:
    /// `theme`, `loginEventId`, `hasPersonalFolder`, and `authCookieLifetime`. The `authCookieLifetime` field has the
    /// seconds left in the session. The call fills it only if less than a day is left or the portal has a custom
    /// session lifetime. So an empty value does not mean an endless session. To read another account, use
    /// `GET api/2.0/people/{userId}`.
    /// </remarks>
    /// <summary>
    /// Get my profile
    /// </summary>
    /// <path>api/2.0/people/@self</path>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "The profile of the caller, with the theme, the session and the personal folder details", typeof(EmployeeFullDto))]
    [AllowNotPayment]
    [HttpGet("@self")]
    public async Task<EmployeeFullDto> GetSelfProfile()
    {
        var user = await _userManager.GetUserAsync(securityContext.CurrentAccount.ID, null);

        var result = await employeeFullDtoHelper.GetFullAsync(user);

        result.Theme = (await settingsManager.LoadForCurrentUserAsync<DarkThemeSettings>()).Theme;

        var (loginEventId, expiration) = cookieStorage.GetLoginEventIdFromCookie(cookiesManager.GetCookies(CookiesType.AuthKey));

        result.LoginEventId = loginEventId;

        var lifetime = expiration - DateTime.UtcNow;

        if (lifetime < TimeSpan.FromDays(1) || (await settingsManager.LoadForCurrentUserAsync<TenantCookieSettings>()).Enabled)
        {
            result.AuthCookieLifetime = lifetime.TotalSeconds;
        }

        if (result.IsVisitor)
        {
            var my = await globalFolderHelper.FolderMyAsync;
            result.HasPersonalFolder = my != 0;
        }
        else
        {
            result.HasPersonalFolder = true;
        }

        return result;
    }

    /// <remarks>
    /// Changes the email address of an account, or sends a link that confirms the change. If the caller is not a
    /// DocSpace admin, the call emails a link to the new address. The address changes only when they follow the link
    /// to `PUT api/2.0/people/{userId}/email`. A DocSpace admin changes the address of any account immediately. The
    /// call then marks the account not activated, ends its sessions, and emails it activation instructions. If a
    /// DocSpace admin changes the address back to one that recently got activation instructions, the call sends
    /// nothing. The link of that email still works. Any signed-in user can call it for their own account. Another
    /// account requires a DocSpace admin, and other callers get 403. Only the portal owner can change the address of
    /// the portal owner. The same applies to another DocSpace admin. If the account does not exist, is disabled, or
    /// is a pending invitation, the call returns 404. The call returns 400 if the address is empty or malformed. It
    /// also returns 400 if any account has the address, even this one. The call changes data. A repeated call by a
    /// DocSpace admin returns 400. For other callers, each call emails a new link, and the earlier link stops working.
    /// The portal limits the rate of these calls. It returns a message for the user with the address.
    /// </remarks>
    /// <summary>
    /// Send instructions to change email
    /// </summary>
    /// <path>api/2.0/people/email</path>
    [Tags("People / Email")]
    [SwaggerResponse(200, "The message stating which address the letter was sent to", typeof(string))]
    [SwaggerResponse(400, "The user ID is empty, the address is missing, malformed, already taken, or equal to the current one")]
    [SwaggerResponse(403, "The caller may not change the address of that account")]
    [SwaggerResponse(404, "The account does not exist, is disabled, or is a pending invitation")]
    [AllowNotPayment]
    [HttpPost("email")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task<string> SendEmailChangeInstructions(UpdateMemberRequestDto inDto)
    {
        Guid.TryParse(inDto.UserId, out var userid);

        if (userid == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(inDto.UserId));
        }

        var email = (inDto.Email ?? "").Trim();

        if (string.IsNullOrEmpty(email))
        {
            throw new ArgumentException(Resource.ErrorEmailEmpty);
        }

        if (!email.TestEmailRegex())
        {
            throw new ArgumentException(Resource.ErrorNotCorrectEmail);
        }

        var viewer = await _userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var viewerIsAdmin = await _userManager.IsDocSpaceAdminAsync(viewer);
        var user = await _userManager.GetUsersAsync(userid);

        if (_userManager.IsSystemUser(user.Id) || user.Status == EmployeeStatus.Terminated || user.Status == EmployeeStatus.Pending)
        {
            throw new ItemNotFoundException(Resource.ErrorUserNotFound);
        }

        if (!viewerIsAdmin && viewer.Id != user.Id)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var tenant = tenantManager.GetCurrentTenant();
        if (user.IsOwner(tenant) && viewer.Id != user.Id)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        if (!viewer.IsOwner(tenant) && await _userManager.IsDocSpaceAdminAsync(user) && viewer.Id != user.Id)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var existentUser = await _userManager.GetUserByEmailAsync(email);

        if (existentUser.Id != Constants.LostUser.Id)
        {
            throw new ArgumentException(await customNamingPeople.Substitute<Resource>("ErrorEmailAlreadyExists"));
        }

        if (!viewerIsAdmin)
        {
            await studioNotifyService.SendEmailChangeInstructionsAsync(user, email);
        }
        else
        {
            if (email == user.Email)
            {
                throw new ArgumentException(Resource.ErrorEmailsAreTheSame);
            }

            user.Email = email;
            user.ActivationStatus = EmployeeActivationStatus.NotActivated;
            await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);
            await cookiesManager.ResetUserCookieAsync(user.Id);
            await studioNotifyService.SendEmailActivationInstructionsAsync(user, email);

            // recorded even when the letter is a repeat: the record is the only trace of the address change itself
            messageService.Send(MessageAction.UserSentEmailChangeInstructions, MessageTarget.Create(user.Id), DateTime.UtcNow, user.DisplayUserName(false, displayUserSettingsHelper));
        }

        if (await _userManager.IsGuestAsync(user))
        {
            await socketManager.UpdateGuestAsync(user);
        }
        else
        {
            await socketManager.UpdateUserAsync(user);
        }

        return string.Format(Resource.MessageEmailChangeInstuctionsSentOnEmail, email);
    }

    /// <remarks>
    /// Emails a password recovery link for an account. Anyone can call it without signing in. If the portal has a
    /// CAPTCHA, a request without sign-in must pass it. Otherwise the call returns 403. Without sign-in, the call
    /// returns the same message whether or not the address has an account. A signed-in caller can ask for their own
    /// account. A DocSpace admin can also ask for other accounts, except the portal owner. Only the portal owner can
    /// ask for another DocSpace admin. Other signed-in callers get 403. A signed-in caller also gets 403 if the account
    /// is missing or disabled. The same applies to an LDAP or SSO account, or one with a generated email. If `email` is
    /// missing, malformed, or too long, the call returns 400. The call changes data, but not the password. Do not
    /// repeat it. Each call sends the email again, and the earlier recovery link stops working. The portal limits the
    /// rate of these calls. The link leads to `PUT api/2.0/people/{userId}/password`, which sets the new password. The
    /// email never contains a password.
    /// </remarks>
    /// <summary>
    /// Remind a user password
    /// </summary>
    /// <path>api/2.0/people/password</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("People / Password")]
    [SwaggerResponse(200, "The message stating that the recovery link was sent to the address", typeof(string))]
    [SwaggerResponse(400, "The request body cannot be read or has no `email`, or the email is not a valid address or is longer than 255 characters")]
    [SwaggerResponse(403, "The CAPTCHA was not passed, an authenticated caller may not ask for that account, or, for an authenticated caller, the account does not exist, is disabled, comes from LDAP or SSO, or has an auto-generated email")]
    [AllowNotPayment]
    [AllowAnonymous]
    [HttpPost("password")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task<string> SendUserPassword(EmailMemberRequestDto inDto)
    {
        if (authContext.IsAuthenticated)
        {
            var targetUser = await _userManager.GetUserByEmailAsync(inDto.Email);
            var self = securityContext.CurrentAccount.ID.Equals(targetUser.Id);
            if (!self)
            {
                var currentUserIsDocSpaceAdmin = await _userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID);
                if (!currentUserIsDocSpaceAdmin)
                {
                    throw new SecurityException(Resource.ErrorAccessDenied);
                }

                var tenant = tenantManager.GetCurrentTenant();
                var targetUserIsOwner = targetUser.IsOwner(tenant);
                if (targetUserIsOwner)
                {
                    throw new SecurityException(Resource.ErrorAccessDenied);
                }

                var targetUserIsDocSpaceAdmin = await _userManager.IsDocSpaceAdminAsync(targetUser.Id);
                var currentUserIsOwner = securityContext.CurrentAccount.ID.IsOwner(tenant);
                if (targetUserIsDocSpaceAdmin && !currentUserIsOwner)
                {
                    throw new SecurityException(Resource.ErrorAccessDenied);
                }
            }
        }
        else if (!string.IsNullOrEmpty(_setupInfo.HcaptchaPublicKey) || !string.IsNullOrEmpty(_setupInfo.RecaptchaPublicKey))
        {
            var requestIp = MessageSettings.GetIP(_httpContextAccessor.HttpContext?.Request);
            var secretEmail = SetupInfo.IsSecretEmail(inDto.Email);

            var recaptchaPassed = secretEmail || await bruteForceLoginManager.CheckRecaptchaAsync(inDto.RecaptchaType, inDto.RecaptchaResponse, requestIp);

            if (!recaptchaPassed)
            {
                throw new RecaptchaException(Resource.RecaptchaInvalid);
            }
        }

        var error = await userManagerWrapper.SendUserPasswordAsync(inDto.Email);
        if (!string.IsNullOrEmpty(error))
        {
            logger.ErrorPasswordRecovery(inDto.Email, error);
            if (authContext.IsAuthenticated)
            {
                throw new InvalidOperationException(error);
            }
        }

        var pattern = authContext.IsAuthenticated ? Resource.MessagePasswordSendedToEmail : Resource.MessageYourPasswordSendedToEmail;
        return string.Format(pattern, inDto.Email);
    }

    /// <remarks>
    /// Sets the activation status of the account of the caller. A person uses it to confirm their email after following
    /// the emailed link. Sign the request with the confirmation token from that link. An invalid or expired token
    /// returns 401. A signed-in caller without this token gets 403. A token from another kind of link also returns
    /// 403. The `userIds` list must hold only the ID of the caller. Otherwise the call returns 400. The call changes
    /// data. With the activation link of a new account, a repeated call returns 401. With a link that confirms an email
    /// address, you can repeat it with the same body. If the portal owner gets `Activated`, the call sends the admin
    /// welcome email once per portal. If the account no longer exists, the response is empty. To enable or disable an
    /// account, use `PUT api/2.0/people/status/{status}`.
    /// </remarks>
    /// <summary>
    /// Set my activation status
    /// </summary>
    /// <path>api/2.0/people/activationstatus/{activationStatus}</path>
    /// <collection>list</collection>
    [RaisesWebhook(WebhookTrigger.UserUpdated)]
    [Tags("People / User status")]
    [SwaggerResponse(200, "The profile of the caller in its new activation state", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(400, "The list is empty, holds more than one ID, or names an account other than the caller")]
    [SwaggerResponse(403, "The caller is signed in but sent no activation link token, or sent a valid confirmation token of another link type")]
    [AllowNotPayment]
    [HttpPut("activationstatus/{activationStatus}")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "Activation,EmailActivation")]
    public async IAsyncEnumerable<EmployeeFullDto> UpdateUserActivationStatus(UpdateMemberActivationStatusRequestDto inDto)
    {
        await securityContext.AuthByClaimAsync();

        if (inDto?.UpdateMembers?.UserIds == null ||
            inDto.UpdateMembers.UserIds.Count() > 1 ||
            !inDto.UpdateMembers.UserIds.Contains(authContext.CurrentAccount.ID))
        {
            throw new ArgumentException();
        }

        foreach (var id in inDto.UpdateMembers.UserIds.Where(userId => !_userManager.IsSystemUser(userId)))
        {
            await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(id), Constants.Action_EditUser);
            var u = await _userManager.GetUsersAsync(id);
            if (u.Id == Constants.LostUser.Id)
            {
                continue;
            }

            u.ActivationStatus = inDto.ActivationStatus;
            await _userManager.UpdateUserInfoAsync(u);

            if (inDto.ActivationStatus == EmployeeActivationStatus.Activated && u.IsOwner(tenantManager.GetCurrentTenant()))
            {
                var settings = await settingsManager.LoadAsync<FirstEmailConfirmSettings>();

                if (settings.IsFirst)
                {
                    await studioNotifyService.SendAdminWelcomeAsync(u);

                    settings.IsFirst = false;
                    await settingsManager.SaveAsync(settings);
                }
            }

            await webhookManager.PublishAsync(WebhookTrigger.UserUpdated, u);

            yield return await employeeFullDtoHelper.GetFullAsync(u);
        }
    }

    /// <remarks>
    /// Changes the interface language of the caller. The language applies to the portal and to the emails of the
    /// account. The ID in the route must be the ID of the caller. Any signed-in user can call it for their own account.
    /// For another account, the call returns 403, even for an admin. The culture must be enabled on the portal.
    /// Otherwise the call returns 400. If no account has the ID, the call returns 404. The call changes data. You can
    /// repeat it with the same body. It returns the profile with the new `cultureName`. To change other fields, use
    /// `PUT api/2.0/people/{userId}`.
    /// </remarks>
    /// <summary>
    /// Update a user culture
    /// </summary>
    /// <path>api/2.0/people/{userId}/culture</path>
    [RaisesWebhook(WebhookTrigger.UserUpdated, When = "even when the culture does not change")]
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "The profile with its new culture", typeof(EmployeeFullDto))]
    [SwaggerResponse(400, "The specified culture is not enabled on the portal")]
    [SwaggerResponse(403, "The ID in the route is not the calling account, or the account may not edit its own profile")]
    [SwaggerResponse(404, "No user has the specified ID")]
    [HttpPut("{userId}/culture")]
    public async Task<EmployeeFullDto> UpdateMemberCulture(UpdateMemberCultureByIdRequestDto inDto)
    {
        if (!coreBaseSettings.EnabledCultures.Any(c =>
                string.Equals(c.Name, inDto.Culture.CultureName, StringComparison.InvariantCultureIgnoreCase)))
        {
            throw new ArgumentException("The specified culture is not in the list of available ones");
        }

        var user = await GetUserInfoAsync(inDto.UserId);

        if (_userManager.IsSystemUser(user.Id) || !user.Id.Equals(securityContext.CurrentAccount.ID))
        {
            throw new SecurityException();
        }

        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser);
        await _userManager.ChangeUserCulture(user, inDto.Culture.CultureName);
        messageService.Send(MessageAction.UserUpdatedLanguage, MessageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper));
        await webhookManager.PublishAsync(WebhookTrigger.UserUpdated, user);
        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <remarks>
    /// Updates an account. The fields it applies depend on whose account it is. On their own account, a caller can
    /// change the names, location, comment, `spam`, contacts, groups, and avatar. The call then ignores `disable` and
    /// `isUser`. On another account, the call applies only `disable` and `isUser` and ignores all other fields. Any
    /// signed-in user can call it for their own account. For another account, it requires a DocSpace admin, and other
    /// callers get 403. Only the portal owner can edit another DocSpace admin, and nobody else can edit the portal
    /// owner. The call changes groups only if the caller can edit groups. For an LDAP or SSO account, it keeps the
    /// names and location. If the names are not valid, the call returns 400. With `disable` set to true, the account
    /// gets the `Terminated` status, and its sessions end. Deletion with `DELETE api/2.0/people/{userId}` requires this
    /// status. With `isUser` set to true, the account becomes a guest, and false makes it a user again. If the tariff
    /// or quota has no free seat for this, the call returns 402. The call ignores a request to make the portal owner, a
    /// DocSpace admin, or a module admin a guest. If the portal refuses the `files` URL or its server returns an error,
    /// the call returns 403. If no account has the ID, the call returns 404. The call changes data. You can repeat it
    /// with the same body. It returns the profile after the update. To change the language, use
    /// `PUT api/2.0/people/{userId}/culture`. To change the type, use `PUT api/2.0/people/type/{type}`.
    /// </remarks>
    /// <summary>
    /// Update a user
    /// </summary>
    /// <path>api/2.0/people/{userId}</path>
    [RaisesWebhook(WebhookTrigger.UserUpdated, When = "when the call changes the account")]
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "The profile as it is after the update", typeof(EmployeeFullDto))]
    [SwaggerResponse(400, "The first and last name pair is not a valid user name")]
    [SwaggerResponse(402, "The tariff or the user quota does not allow the requested guest or member seat")]
    [SwaggerResponse(403, "The account is the portal owner or a system account, the caller may not edit it, only the portal owner may edit a DocSpace administrator, or, on the caller's own profile, the `files` URL was refused or the server behind it answered with an error status")]
    [SwaggerResponse(404, "No user has the specified ID")]
    [HttpPut("{userId}", Order = 1)]
    public async Task<EmployeeFullDto> UpdateMember(UpdateMemberByIdRequestDto inDto)
    {
        var user = await GetUserInfoAsync(inDto.UserId);

        if (_userManager.IsSystemUser(user.Id))
        {
            throw new SecurityException();
        }

        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser);

        var tenant = tenantManager.GetCurrentTenant();
        var self = securityContext.CurrentAccount.ID.Equals(user.Id);
        var currentUserIsOwner = securityContext.CurrentAccount.ID.IsOwner(tenant);
        //var currentUserIsDocSpaceAdmin = await _userManager.IsDocSpaceAdminAsync(securityContext.CurrentAccount.ID);
        var userIsOwner = user.IsOwner(tenant);
        var userType = await _userManager.GetUserTypeAsync(user.Id);

        if (!self)
        {
            if (userIsOwner || (!currentUserIsOwner && userType == EmployeeType.DocSpaceAdmin))
            {
                throw new SecurityException();
            }
        }

        var changed = false;
        var statusChanged = false;

        if (self)
        {
            var isLdap = user.IsLDAP();
            var isSso = user.IsSSO();

            if (!isLdap && !isSso)
            {
                var firstName = inDto.UpdateMember.FirstName ?? user.FirstName;
                var lastName = inDto.UpdateMember.LastName ?? user.LastName;

                if (!userFormatter.IsValidUserName(firstName, lastName))
                {
                    throw new ArgumentException(Resource.ErrorIncorrectUserName);
                }

                user.FirstName = firstName;
                user.LastName = lastName;
                user.Location = inDto.UpdateMember.Location ?? user.Location;

                // if (currentUserIsDocSpaceAdmin)
                // {
                //     user.Title = inDto.UpdateMember.Title ?? user.Title;
                //     user.WorkFromDate = inDto.UpdateMember.Worksfrom != null
                //         ? tenantUtil.DateTimeFromUtc(inDto.UpdateMember.Worksfrom)
                //         : user.WorkFromDate;
                // }
            }

            user.Notes = inDto.UpdateMember.Comment ?? user.Notes;

            // user.Sex = inDto.UpdateMember.Sex switch
            // {
            //     SexEnum.Male => true,
            //     SexEnum.Female => false,
            //     _ => user.Sex
            // };

            user.Spam = inDto.UpdateMember.Spam;

            //user.BirthDate = inDto.UpdateMember.Birthday != null ? tenantUtil.DateTimeFromUtc(inDto.UpdateMember.Birthday) : user.BirthDate;

            //var resetDate = new DateTime(1900, 01, 01);
            // if (user.BirthDate == resetDate)
            // {
            //     user.BirthDate = null;
            // }
            //
            // if (user.WorkFromDate == resetDate)
            // {
            //     user.WorkFromDate = null;
            // }

            UrlValidationResult photoValidation = null;
            if (inDto.UpdateMember.Files != await _userPhotoManager.GetPhotoAbsoluteWebPath(user.Id))
            {
                photoValidation = await ValidatePhotoUrlAsync(inDto.UpdateMember.Files);
            }

            await UpdateContactsAsync(inDto.UpdateMember.Contacts, user);
            await UpdateDepartmentsAsync(inDto.UpdateMember.Department, user);

            if (photoValidation != null)
            {
                await DownloadAndSavePhotoAsync(photoValidation, user);
            }

            changed = true;
        }

        if (!self && inDto.UpdateMember.Disable.HasValue)
        {
            user.Status = inDto.UpdateMember.Disable.Value ? EmployeeStatus.Terminated : EmployeeStatus.Active;
            user.TerminatedDate = inDto.UpdateMember.Disable.Value ? DateTime.UtcNow : null;
            changed = true;
            statusChanged = true;
        }

        if (!self && inDto.UpdateMember.IsUser.HasValue)
        {
            var setGuest = inDto.UpdateMember.IsUser.Value;
            var isGuest = await _userManager.IsGuestAsync(user);
            if (setGuest && !isGuest)
            {
                var canBeGuest = !userIsOwner &&
                                 !await _userManager.IsDocSpaceAdminAsync(user) &&
                                 (await user.GetListAdminModulesAsync(webItemSecurity, webItemManager)).Count == 0;
                if (canBeGuest)
                {
                    await using (await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetUsersCountCheckKey(tenant.Id)))
                    {
                        await activeUsersChecker.CheckAppend();
                        await _userManager.AddUserIntoGroupAsync(user.Id, Constants.GroupGuest.ID);
                        await webItemSecurityCache.ClearCacheAsync(tenant.Id);
                        changed = true;
                    }
                }
            }
            else if (!setGuest && isGuest)
            {
                await using (await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetPaidUsersCountCheckKey(tenant.Id)))
                {
                    await countPaidUserChecker.CheckAppend();
                    await _userManager.RemoveUserFromGroupAsync(user.Id, Constants.GroupGuest.ID);
                    await webItemSecurityCache.ClearCacheAsync(tenant.Id);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);
            if (await _userManager.IsGuestAsync(user))
            {
                await socketManager.UpdateGuestAsync(user);
            }
            else
            {
                await socketManager.UpdateUserAsync(user);
            }

            messageService.Send(MessageAction.UserUpdated, MessageTarget.Create(user.Id),
                user.DisplayUserName(false, displayUserSettingsHelper), user.Id);

            if (statusChanged && inDto.UpdateMember.Disable.HasValue && inDto.UpdateMember.Disable.Value)
            {
                await cookiesManager.ResetUserCookieAsync(user.Id);
                messageService.Send(MessageAction.CookieSettingsUpdated);
            }

            await webhookManager.PublishAsync(WebhookTrigger.UserUpdated, user);
        }

        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <remarks>
    /// Enables or disables several accounts. A disabled account keeps its data and can be enabled later. The route
    /// accepts only `Active` and `Terminated`. Any other status returns 400. Requires a DocSpace admin. Other callers
    /// get 403. The call checks the whole list before it changes anything. If the list has a system or LDAP account,
    /// the portal owner, or the caller, the call returns 403. Only the portal owner can change a DocSpace admin. The
    /// call changes data. You can repeat it with the same body. Disabling ends all sessions of the account and frees
    /// its seat. Enabling takes a seat again. If the tariff or quota has no free seat, the call returns 402. The call
    /// enables the accounts one by one, so a 402 leaves the earlier ones enabled. Enabling changes only disabled
    /// accounts. An account with no name and an unused invitation becomes `Pending`, not `Active`. So check `status` in
    /// the response. Deletion with `DELETE api/2.0/people/{userId}` requires the `Terminated` status.
    /// </remarks>
    /// <summary>
    /// Change a user status
    /// </summary>
    /// <path>api/2.0/people/status/{status}</path>
    /// <collection>list</collection>
    [RaisesWebhook(WebhookTrigger.UserUpdated, When = "for every listed account, whether or not its status changes")]
    [Tags("People / User status")]
    [SwaggerResponse(200, "The listed accounts with their statuses after the change", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(400, "The requested status is neither Active nor Terminated")]
    [SwaggerResponse(402, "The tariff or the user quota does not allow enabling one more account")]
    [SwaggerResponse(403, "No permissions to perform this action, or the list names a system, LDAP, owner, self or DocSpace admin account")]
    [HttpPut("status/{status}")]
    public async IAsyncEnumerable<EmployeeFullDto> UpdateUserStatus(UpdateMemberStatusRequestDto inDto)
    {
        if (inDto.Status is not (EmployeeStatus.Active or EmployeeStatus.Terminated))
        {
            throw new ArgumentException($"Incorrect status");
        }

        await _permissionContext.DemandPermissionsAsync(Constants.Action_EditUser);

        var tenant = tenantManager.GetCurrentTenant();
        var users = await inDto.UpdateMembers.UserIds
            .ToAsyncEnumerable()
            .Select(async (Guid userId, CancellationToken _) => await _userManager.GetUsersAsync(userId))
            .ToListAsync();

        var currentUserIsOwner = authContext.CurrentAccount.ID == tenant.OwnerId;
        foreach (var u in users)
        {
            if (_userManager.IsSystemUser(u.Id)
                || u.IsLDAP()
                || u.IsOwner(tenant)
                || u.IsMe(authContext)
                || !currentUserIsOwner && await _userManager.IsDocSpaceAdminAsync(u))
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }
        }

        foreach (var user in users)
        {
            switch (inDto.Status)
            {
                case EmployeeStatus.Active:
                    if (user.Status == EmployeeStatus.Terminated)
                    {
                        IDistributedLockHandle lockHandle = null;

                        var type = await _userManager.GetUserTypeAsync(user.Id);

                        try
                        {
                            if (type is EmployeeType.DocSpaceAdmin or EmployeeType.RoomAdmin)
                            {
                                lockHandle = await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetPaidUsersCountCheckKey(tenant.Id));

                                await countPaidUserChecker.CheckAppend();
                            }
                            else
                            {
                                lockHandle = await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetUsersCountCheckKey(tenant.Id));

                                await activeUsersChecker.CheckAppend();
                            }


                            if (user.Status == EmployeeStatus.Terminated && string.IsNullOrEmpty(user.FirstName) && string.IsNullOrEmpty(user.LastName))
                            {
                                var emailChangeEvent = (await auditEventsRepository.GetByFilterWithActionsAsync(actions: [MessageAction.SendJoinInvite, MessageAction.RoomInviteLinkUsed, MessageAction.RoomCreateUser], entry: EntryType.User, target: MessageTarget.Create(user.Id).ToString(), limit: 1)).FirstOrDefault() ??
                                                       (await auditEventsRepository.GetByFilterWithActionsAsync(actions: [MessageAction.RoomCreateUser], limit: 1, description: user.Email)).FirstOrDefault();

                                user.Status = emailChangeEvent != null ? EmployeeStatus.Pending : EmployeeStatus.Active;
                            }
                            else
                            {
                                user.Status = EmployeeStatus.Active;
                            }

                            await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);
                            if (await _userManager.IsGuestAsync(user))
                            {
                                await socketManager.UpdateGuestAsync(user);
                            }
                            else
                            {
                                await socketManager.UpdateUserAsync(user);
                            }
                        }
                        finally
                        {
                            if (lockHandle != null)
                            {
                                await lockHandle.ReleaseAsync();
                            }
                        }
                    }
                    break;
                case EmployeeStatus.Terminated:
                    user.Status = EmployeeStatus.Terminated;

                    await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);

                    await cookiesManager.ResetUserCookieAsync(user.Id);
                    messageService.Send(MessageAction.CookieSettingsUpdated);

                    await quotaSocketManager.LogoutSession(user.Id);

                    if (await _userManager.IsGuestAsync(user))
                    {
                        await socketManager.UpdateGuestAsync(user);
                    }
                    else
                    {
                        await socketManager.UpdateUserAsync(user);
                    }

                    break;
            }
        }

        messageService.Send(MessageAction.UsersUpdatedStatus, MessageTarget.Create(users.Select(x => x.Id)), users.Select(x => x.DisplayUserName(false, displayUserSettingsHelper)));

        foreach (var user in users)
        {
            await webhookManager.PublishAsync(WebhookTrigger.UserUpdated, user);

            yield return await employeeFullDtoHelper.GetFullAsync(user);
        }
    }

    /// <remarks>
    /// Changes the type of the listed accounts to the type in the route. Only these changes work: a guest to `User`,
    /// a user or guest to `RoomAdmin`, and any account to `DocSpaceAdmin`. The portal owner can also make a DocSpace
    /// admin a `RoomAdmin`. Any other change returns 403, including any change to `Guest`. A room admin can make a
    /// guest a `User`. Only a DocSpace admin can set `RoomAdmin`, and only the portal owner can set `DocSpaceAdmin`.
    /// Other callers get 403. Nobody can change their own type or the type of the portal owner. The caller must see
    /// each account, and no account can be disabled. Otherwise the call returns 403. The call changes data, and a
    /// repeated call returns 403. It emails each changed account. An account that got an email about the same type
    /// recently gets no new one. The call changes the accounts one by one, so an error in the middle leaves the
    /// earlier ones changed. A paid type takes a paid seat. If the tariff has no free paid seat, the call returns
    /// 402. If `userIds` is missing, the call returns 400. The call does not move rooms or shared files. To move them
    /// in the same step, use `POST api/2.0/people/type`.
    /// </remarks>
    /// <summary>
    /// Change a user type
    /// </summary>
    /// <path>api/2.0/people/type/{type}</path>
    /// <collection>list</collection>
    [RaisesWebhook(WebhookTrigger.UserUpdated, When = "for every changed account, one by one")]
    [Tags("People / User type")]
    [SwaggerResponse(200, "The converted users with their detailed information", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `userIds`")]
    [SwaggerResponse(402, "The tariff or the paid-user quota does not allow one more paid user")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPut("type/{type}")]
    public async IAsyncEnumerable<EmployeeFullDto> UpdateUserType(UpdateMemberTypeRequestDto inDto)
    {
        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(inDto.Type), Constants.Action_AddRemoveUser);

        if (inDto.Type is EmployeeType.Guest)
        {
            var invitationSettings = await settingsManager.LoadAsync<TenantUserInvitationSettings>();
            if (!invitationSettings.AllowInvitingGuests)
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }
        }

        var users = new List<UserInfo>(inDto.UpdateMembers.UserIds.Count());

        foreach (var userId in inDto.UpdateMembers.UserIds)
        {
            if (_userManager.IsSystemUser(userId) ||
                !await _userManager.CanUserViewAnotherUserAsync(authContext.CurrentAccount.ID, userId))
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }

            var user = await _userManager.GetUsersAsync(userId);
            users.Add(user);

            if (user.Status == EmployeeStatus.Terminated)
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }

            var isGuest = await _userManager.IsGuestAsync(user);

            if (!await userManagerWrapper.UpdateUserTypeAsync(user, inDto.Type))
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }

            if (isGuest && !await _userManager.IsGuestAsync(user))
            {
                await socketManager.AddUserAsync(user);
                await socketManager.DeleteGuestAsync(user.Id);
            }
            else
            {
                await socketManager.UpdateUserAsync(user);
            }

            await socketManager.ChangeUserTypeAsync(user, true);
            await studioNotifyService.SendMsgUserTypeChangedAsync(user, inDto.Type, FilesCommonResource.ResourceManager.GetString("RoleEnum_" + inDto.Type.ToStringFast()));
            messageService.Send(MessageAction.UsersUpdatedType, MessageTarget.Create([user.Id]), [user.DisplayUserName(false, displayUserSettingsHelper)], [user.Id], inDto.Type);
            await webhookManager.PublishAsync(WebhookTrigger.UserUpdated, user);
        }

        foreach (var user in users)
        {
            yield return await employeeFullDtoHelper.GetFullAsync(user);
        }
    }

    /// <remarks>
    /// Starts a job that changes an account to `Guest` or `User`. The same job moves the rooms and shared files of the
    /// account to another admin. For other types, use `PUT api/2.0/people/type/{type}`. Any other type returns 400.
    /// Requires a room admin or a DocSpace admin. Other callers get 403. The `Guest` type also returns 403 if the
    /// portal does not allow inviting guests. Only the portal owner can change a DocSpace admin. Otherwise the call
    /// returns 400. The call also returns 400 for a system or disabled account, or for the account of the caller. The
    /// recipient is `reassignUserId`, or the caller if it is empty. It must be an active room admin or DocSpace admin,
    /// other than the account. Otherwise the call returns 400. The call queues work. While the job runs, a repeated
    /// call returns its state and starts nothing. After the job ends, a repeat starts a new job, which emails the
    /// caller again. Poll `GET api/2.0/people/type/progress/{userId}` until `isCompleted` is true. To cancel the job,
    /// use `PUT api/2.0/people/type/terminate`. A failure inside the job shows in `error` of the progress, not as a
    /// status code.
    /// </remarks>
    /// <summary>Start updating user type</summary>
    /// <path>api/2.0/people/type</path>
    [RaisesWebhook(WebhookTrigger.UserUpdated, When = "once the queued type change succeeds, not when it is cancelled or fails")]
    [RaisesWebhook(WebhookTrigger.RoomUpdated, When = "for every room of the account, once the job moves it to the recipient")]
    [RaisesWebhook(WebhookTrigger.AgentUpdated, When = "instead for an AI agent the account owns")]
    [Tags("People / User type")]
    [SwaggerResponse(200, "The state of the queued user type change", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(400, "The requested type is neither Guest nor User, the account is a system account, disabled or the caller, the recipient is the same account or is not an active admin, or a non-owner tried to convert a DocSpace admin")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPost("type")]
    public async Task<TaskProgressResponseDto> StartUserTypeUpdate(StartUpdateUserTypeDto inDto)
    {
        if (inDto.Type is not (EmployeeType.Guest or EmployeeType.User))
        {
            throw new ArgumentException($"Can not update to {inDto.Type}");
        }

        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(inDto.Type), Constants.Action_AddRemoveUser);

        if (inDto.Type is EmployeeType.Guest)
        {
            var invitationSettings = await settingsManager.LoadAsync<TenantUserInvitationSettings>();
            if (!invitationSettings.AllowInvitingGuests)
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }
        }

        var tenant = tenantManager.GetCurrentTenant();

        var user = await _userManager.GetUsersAsync(inDto.UserId);
        var currentUser = await _userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var toUser = inDto.ReassignUserId.HasValue ? await _userManager.GetUsersAsync(inDto.ReassignUserId.Value) : currentUser;

        var userType = await _userManager.GetUserTypeAsync(user);
        var toUserType = await _userManager.GetUserTypeAsync(toUser);

        if (_userManager.IsSystemUser(user.Id)
            || user.Status == EmployeeStatus.Terminated
            || toUser.Status == EmployeeStatus.Terminated
            || user.Id == toUser.Id
            || user.Id == currentUser.Id)
        {
            throw new ArgumentException($"Can not update type");
        }

        if (!currentUser.IsOwner(tenant) && userType is EmployeeType.DocSpaceAdmin)
        {
            throw new ArgumentException($"Can not update type for admin");
        }

        if (toUserType != EmployeeType.DocSpaceAdmin && toUserType != EmployeeType.RoomAdmin)
        {
            throw new ArgumentException($"Can not reassign to user - {inDto.ReassignUserId}");
        }

        var progressItem = await queueWorkerUpdateUserType.StartAsync(tenant.Id, user.Id, toUser.Id, securityContext.CurrentAccount.ID, inDto.Type);

        return TaskProgressResponseDto.Get(progressItem);
    }

    /// <remarks>
    /// Returns the state of the type change job of an account. Start the job with `POST api/2.0/people/type`. Requires
    /// a DocSpace admin. Other callers get 403. A room admin also gets 403, even for its own job. The call is
    /// read-only. This is the call to poll. Repeat it until `isCompleted` is true. If no job exists for the account,
    /// the call returns 200 with an empty body. A failed job has its message in `error`. To cancel a running job, use
    /// `PUT api/2.0/people/type/terminate`.
    /// </remarks>
    /// <summary>Get the user type change progress</summary>
    /// <path>api/2.0/people/type/progress/{userId}</path>
    [Tags("People / User type")]
    [SwaggerResponse(200, "The state of the queued user type change, or an empty body when nothing is queued for the user", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("type/progress/{userId:guid}")]
    public async Task<TaskProgressResponseDto> GetUserTypeUpdateProgress(UserIdRequestDto inDto)
    {
        await _permissionContext.DemandPermissionsAsync(Constants.Action_AddRemoveUser);

        var tenant = tenantManager.GetCurrentTenant();
        var progressItem = await queueWorkerUpdateUserType.GetProgressItemStatus(tenant.Id, inDto.UserId);

        return TaskProgressResponseDto.Get(progressItem);
    }

    /// <remarks>
    /// Cancels the type change job of an account. Requires a DocSpace admin. Other callers get 403. The call changes
    /// data, and you can repeat it. If no job exists for the account, the call returns 200 with an empty body.
    /// Cancelling does not undo the type change or the moves that the job already made. You cannot resume a cancelled
    /// job. To try again, start a new job with `POST api/2.0/people/type`. The response has `status` set to `Canceled`
    /// and `isCompleted` set to true. If `userId` is missing, the call returns 400.
    /// </remarks>
    /// <summary>Terminate updating user type</summary>
    /// <path>api/2.0/people/type/terminate</path>
    [Tags("People / User type")]
    [SwaggerResponse(200, "The state of the cancelled user type change, or an empty body when nothing was queued for the user", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `userId`")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPut("type/terminate")]
    public async Task<TaskProgressResponseDto> TerminateUserTypeUpdate(TerminateRequestDto inDto)
    {
        await _permissionContext.DemandPermissionsAsync(Constants.Action_AddRemoveUser);

        var tenant = tenantManager.GetCurrentTenant();
        var progressItem = await queueWorkerUpdateUserType.GetProgressItemStatus(tenant.Id, inDto.UserId);

        if (progressItem != null)
        {
            await queueWorkerUpdateUserType.Terminate(tenant.Id, inDto.UserId);

            progressItem.Status = DistributedTaskStatus.Canceled;
            progressItem.IsCompleted = true;
        }

        return TaskProgressResponseDto.Get(progressItem);
    }

    ///<remarks>
    /// Starts the process of recalculating a quota.
    /// </remarks>
    /// <summary>
    /// Recalculate a quota
    /// </summary>
    /// <path>api/2.0/people/recalculatequota</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("People / Quota")]
    [HttpGet("recalculatequota")]
    public async Task RecalculateQuota()
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        await usersQuotaSyncOperation.RecalculateQuota(tenantManager.GetCurrentTenant());
    }

    /// <remarks>
    /// Checks the process of recalculating a quota.
    /// </remarks>
    /// <summary>
    /// Check the quota recalculation
    /// </summary>
    /// <path>api/2.0/people/checkrecalculatequota</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("People / Quota")]
    [SwaggerResponse(200, "Task progress", typeof(TaskProgressDto))]
    [HttpGet("checkrecalculatequota")]
    public async Task<TaskProgressDto> CheckRecalculateQuota()
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        return await usersQuotaSyncOperation.CheckRecalculateQuota(tenantManager.GetCurrentTenant());
    }

    /// <remarks>
    /// Sets a storage limit for each listed account, instead of the portal default. Pass `quota` as a whole number of
    /// bytes. The value -1 means no limit, and -2 returns the account to the portal default. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. The call
    /// returns 400 if `quota` is not a whole number or is over the storage the tariff allows. On a standalone
    /// installation, it also returns 400 for a value over the portal quota. The call changes data. You can repeat it
    /// with the same body. It skips system accounts without an error. It returns the changed accounts. A limit deletes
    /// nothing and frees no space. An account over its limit cannot add more files. To return accounts to the portal
    /// default, use `PUT api/2.0/people/resetquota`.
    /// </remarks>
    /// <summary>
    /// Change a user quota limit
    /// </summary>
    /// <path>api/2.0/people/userquota</path>
    /// <collection>list</collection>
    [Tags("People / Quota")]
    [SwaggerResponse(200, "The accounts whose limit was changed", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(400, "The value is not a whole number of bytes, or it exceeds the storage the portal allows")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPut("userquota")]
    public async IAsyncEnumerable<EmployeeFullDto> UpdateUserQuota(UpdateMembersQuotaRequestDto inDto)
    {
        if (!inDto.Quota.TryGetInt64(out var quota))
        {
            throw new ArgumentException(Resource.UserQuotaGreaterPortalError);
        }

        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var users = await inDto.UserIds.ToAsyncEnumerable()
            .Where(userId => !_userManager.IsSystemUser(userId))
            .Select(async (Guid userId, CancellationToken _) => await _userManager.GetUsersAsync(userId))
            .ToListAsync();

        var tenant = tenantManager.GetCurrentTenant();
        var tenantSpaceQuota = await tenantManager.GetTenantQuotaAsync(tenant.Id);
        var maxTotalSize = tenantSpaceQuota?.MaxTotalSize ?? -1;

        if (maxTotalSize < quota)
        {
            throw new ArgumentException(Resource.UserQuotaGreaterPortalError);
        }
        if (coreBaseSettings.Standalone)
        {
            var tenantQuotaSetting = await settingsManager.LoadAsync<TenantQuotaSettings>();
            if (tenantQuotaSetting.EnableQuota)
            {
                if (tenantQuotaSetting.Quota < quota)
                {
                    throw new ArgumentException(Resource.UserQuotaGreaterPortalError);
                }
            }
        }

        foreach (var user in users)
        {
            await settingsManager.SaveAsync(new UserQuotaSettings { UserQuota = quota }, user);

            var userUsedSpace = Math.Max(0, (await quotaService.FindUserQuotaRowsAsync(tenant.Id, user.Id)).Where(r => !string.IsNullOrEmpty(r.Tag) && !string.Equals(r.Tag, Guid.Empty.ToString())).Sum(r => r.Counter));
            var quotaUserSettings = await settingsManager.LoadAsync<TenantUserQuotaSettings>();
            _ = quotaSocketManager.ChangeCustomQuotaUsedValueAsync(tenant.Id, customQuota.GetFeature<UserCustomQuotaFeature>().Name, quotaUserSettings.EnableQuota, userUsedSpace, quota, [user.Id]);
            await socketManager.UpdateUserAsync(user);
            yield return await employeeFullDtoHelper.GetFullAsync(user);
        }

        if (quota >= 0)
        {
            messageService.Send(MessageAction.CustomQuotaPerUserChanged, inDto.Quota.ToString(),
                        users.Select(x => HttpUtility.HtmlDecode(displayUserSettingsHelper.GetFullUserName(x))));
        }
        else
        {
            messageService.Send(MessageAction.CustomQuotaPerUserDisabled, MessageTarget.Create(users.Select(x => x.Id)), users.Select(x => HttpUtility.HtmlDecode(displayUserSettingsHelper.GetFullUserName(x))));
        }


    }

    /// <remarks>
    /// Resets the storage limit of the listed accounts to the portal default. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. On a hosted portal, the
    /// tariff must include storage statistics. Otherwise the call returns 402. The call reads only `userIds` from the
    /// body. It skips system accounts without an error. The call changes data. You can repeat it with the same body. It
    /// returns the accounts with the portal default as their limit. The call deletes nothing and frees no space. To set
    /// a limit for an account, use `PUT api/2.0/people/userquota`.
    /// </remarks>
    /// <summary>
    /// Reset a user quota limit
    /// </summary>
    /// <path>api/2.0/people/resetquota</path>
    /// <collection>list</collection>
    [Tags("People / Quota")]
    [SwaggerResponse(200, "The accounts that now follow the portal default limit", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(402, "The tariff of a hosted portal does not include the storage statistics feature")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPut("resetquota")]
    public async IAsyncEnumerable<EmployeeFullDto> ResetUsersQuota(UpdateMembersQuotaRequestDto inDto)
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        if (!coreBaseSettings.Standalone
            && !(await tenantManager.GetCurrentTenantQuotaAsync()).Statistic)
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }

        var users = await inDto.UserIds.ToAsyncEnumerable()
            .Where(userId => !_userManager.IsSystemUser(userId))
            .Select(async (Guid userId, CancellationToken _) => await _userManager.GetUsersAsync(userId))
            .ToListAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var quotaUserSettings = await settingsManager.LoadAsync<TenantUserQuotaSettings>();
        foreach (var user in users)
        {
            if (_userManager.IsSystemUser(user.Id))
            {
                throw new SecurityException();
            }
            var defaultSettings = settingsManager.GetDefault<UserQuotaSettings>();
            await settingsManager.SaveAsync(defaultSettings, user);
            var userUsedSpace = Math.Max(0, (await quotaService.FindUserQuotaRowsAsync(tenant.Id, user.Id)).Where(r => !string.IsNullOrEmpty(r.Tag) && !string.Equals(r.Tag, Guid.Empty.ToString())).Sum(r => r.Counter));
            var userQuotaData = await settingsManager.LoadAsync<UserQuotaSettings>(user);
            var userQuotaLimit = userQuotaData.UserQuota == userQuotaData.GetDefault().UserQuota ? quotaUserSettings.DefaultQuota : userQuotaData.UserQuota;
            _ = quotaSocketManager.ChangeCustomQuotaUsedValueAsync(tenant.Id, customQuota.GetFeature<UserCustomQuotaFeature>().Name, quotaUserSettings.EnableQuota, userUsedSpace, userQuotaLimit, [user.Id]);
            await socketManager.UpdateUserAsync(user);
            yield return await employeeFullDtoHelper.GetFullAsync(user);
        }

        messageService.Send(MessageAction.CustomQuotaPerUserDefault, quotaUserSettings.DefaultQuota.ToString(),
                        users.Select(x => HttpUtility.HtmlDecode(displayUserSettingsHelper.GetFullUserName(x))));

    }

    private async Task UpdateDepartmentsAsync(IEnumerable<Guid> department, UserInfo user)
    {
        if (!await _permissionContext.CheckPermissionsAsync(Constants.Action_EditGroups))
        {
            return;
        }

        if (department == null)
        {
            return;
        }

        var groups = await _userManager.GetUserGroupsAsync(user.Id);
        var managerGroups = new List<Guid>();
        foreach (var groupInfoId in groups.Select(r => r.ID))
        {
            await _userManager.RemoveUserFromGroupAsync(user.Id, groupInfoId);
            var managerId = await _userManager.GetDepartmentManagerAsync(groupInfoId);
            if (managerId == user.Id)
            {
                managerGroups.Add(groupInfoId);
                await _userManager.SetDepartmentManagerAsync(groupInfoId, Guid.Empty);
            }
        }
        foreach (var guid in department)
        {
            var userDepartment = await _userManager.GetGroupInfoAsync(guid);
            if (!Equals(userDepartment, Constants.LostGroupInfo))
            {
                await _userManager.AddUserIntoGroupAsync(user.Id, guid);
                if (managerGroups.Contains(guid))
                {
                    await _userManager.SetDepartmentManagerAsync(guid, user.Id);
                }
            }
        }
    }

    private async Task CheckReassignProcessAsync(IEnumerable<Guid> userIds)
    {
        var tenant = tenantManager.GetCurrentTenant();

        foreach (var userId in userIds)
        {
            var reassignStatus = await queueWorkerReassign.GetProgressItemStatus(tenant.Id, userId);
            if (reassignStatus == null || reassignStatus.IsCompleted)
            {
                continue;
            }

            var userName = (await _userManager.GetUsersAsync(userId)).DisplayUserName(displayUserSettingsHelper);

            throw new Exception(string.Format(Resource.ReassignDataRemoveUserError, userName));
        }
    }

    private async IAsyncEnumerable<UserInfo> GetByFilterAsync(UserFilter filter)
    {
        if (await _userManager.IsGuestAsync(securityContext.CurrentAccount.ID) ||
            await _userManager.IsUserAsync(securityContext.CurrentAccount.ID))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var isDocSpaceAdmin = await _userManager.IsDocSpaceAdminAsync(securityContext.CurrentAccount.ID) ||
                      await webItemSecurity.IsProductAdministratorAsync(WebItemManager.PeopleProductID, securityContext.CurrentAccount.ID);

        var excludeGroups = new List<Guid>();
        var includeGroups = new List<List<Guid>>();
        var combinedGroups = new List<Tuple<List<List<Guid>>, List<Guid>>>();

        if (filter.GroupId.HasValue && (!filter.WithoutGroup.HasValue || !filter.WithoutGroup.Value))
        {
            if (filter.ExcludeGroup.HasValue && filter.ExcludeGroup.Value)
            {
                excludeGroups.Add(filter.GroupId.Value);
            }
            else
            {
                includeGroups.Add([filter.GroupId.Value]);
            }
        }

        if (filter.EmployeeType.HasValue)
        {
            FilterByUserType(filter.EmployeeType.Value, includeGroups, excludeGroups);
        }
        else if (filter.EmployeeTypes != null && filter.EmployeeTypes.Any())
        {
            foreach (var et in filter.EmployeeTypes)
            {
                var combinedIncludeGroups = new List<List<Guid>>();
                var combinedExcludeGroups = new List<Guid>();
                FilterByUserType(et, combinedIncludeGroups, combinedExcludeGroups);
                combinedGroups.Add(new Tuple<List<List<Guid>>, List<Guid>>(combinedIncludeGroups, combinedExcludeGroups));
            }
        }

        if (filter.Payments != null)
        {
            switch (filter.Payments)
            {
                case Payments.Paid:
                    excludeGroups.Add(Constants.GroupGuest.ID);
                    excludeGroups.Add(Constants.GroupUser.ID);
                    break;
                case Payments.Free:
                    includeGroups.Add([Constants.GroupGuest.ID, Constants.GroupUser.ID]);
                    break;
            }
        }

        if (filter.IsDocSpaceAdministrator.HasValue && filter.IsDocSpaceAdministrator.Value)
        {
            var adminGroups = new List<Guid>
            {
                Constants.GroupAdmin.ID
            };

            var products = webItemManager.GetItemsAll().Where(i => i is IProduct || i.ID == WebItemManager.MailProductID);
            adminGroups.AddRange(products.Select(r => r.ID));

            includeGroups.Add(adminGroups);
        }


        var queryFilter = new UserQueryFilter(
            isDocSpaceAdmin,
            filter.EmployeeStatus,
            includeGroups,
            excludeGroups,
            combinedGroups,
            filter.ActivationStatus,
            filter.AccountLoginType,
            filter.QuotaFilter,
            filter.Area,
            filter.InvitedByMe,
            filter.InviterId,
            filter.Text,
            filter.FilterSeparator,
            filter.WithoutGroup ?? false,
            filter.SortBy,
            filter.SortOrder == SortOrder.Ascending,
            isDocSpaceAdmin,
            filter.Count,
            filter.StartIndex);

        var totalCountTask = _userManager.GetUsersCountAsync(queryFilter);
        var users = _userManager.GetUsers(queryFilter);

        var counter = 0;

        await foreach (var user in users)
        {
            counter++;

            yield return user;
        }

        _apiContext.SetCount(counter).SetTotalCount(await totalCountTask);

        yield break;

        void FilterByUserType(EmployeeType eType, List<List<Guid>> iGroups, List<Guid> eGroups)
        {
            switch (eType)
            {
                case EmployeeType.DocSpaceAdmin when filter.Area is not Area.Guests:
                    iGroups.Add([Constants.GroupAdmin.ID]);
                    break;
                case EmployeeType.RoomAdmin when filter.Area is not Area.Guests:
                    eGroups.Add(Constants.GroupGuest.ID);
                    eGroups.Add(Constants.GroupAdmin.ID);
                    eGroups.Add(Constants.GroupUser.ID);
                    break;
                case EmployeeType.User when filter.Area is not Area.Guests:
                    iGroups.Add([Constants.GroupUser.ID]);
                    break;
                case EmployeeType.Guest when filter.Area is not Area.People:
                    iGroups.Add([Constants.GroupGuest.ID]);
                    break;
            }
        }
    }

    ///// <remarks>
    ///// Imports the new portal users with the first name, last name, and email address.
    ///// </remarks>
    ///// <summary>
    ///// Import users
    ///// </summary>
    ///// <param type="System.String, System" name="userList">List of users</param>
    ///// <param type="System.Boolean, System" name="importUsersAsCollaborators" optional="true">Specifies whether to import users as guests or not</param>
    ///// <path>api/2.0/people/import/save</path>
    //[HttpPost("import/save")]
    //public void SaveUsers(string userList, bool importUsersAsCollaborators)
    //{
    //    lock (progressQueue.SynchRoot)
    //    {
    //        var task = progressQueue.GetItems().OfType<ImportUsersTask>().FirstOrDefault(t => (int)t.Id == TenantProvider.CurrentTenantID);
    //var tenant = CoreContext.TenantManager.GetCurrentTenant();
    //Cache.Insert("REWRITE_URL" + tenant.TenantId, HttpContext.Current.Request.GetUrlRewriter().ToString(), TimeSpan.FromMinutes(5));
    //        if (task != null && task.IsCompleted)
    //        {
    //            progressQueue.Remove(task);
    //            task = null;
    //        }
    //        if (task == null)
    //        {
    //            progressQueue.Add(new ImportUsersTask(userList, importUsersAsCollaborators, GetHttpHeaders(HttpContext.Current.Request))
    //            {
    //                Id = TenantProvider.CurrentTenantID,
    //                UserId = SecurityContext.CurrentAccount.ID,
    //                Percentage = 0
    //            });
    //        }
    //    }
    //}

    // <remarks>
    // Returns a status of the current user.
    // </remarks>
    // <summary>
    // Get a user status
    // </summary>
    // <returns tye="System.Object, System">Current user information</returns>
    // <category>User status</category>
    // <path>api/2.0/people/import/status</path>
    // <httpMethod>GET</httpMethod>
    //[HttpGet("import/status")]
    //public object GetStatus()
    //{
    //    lock (progressQueue.SynchRoot)
    //    {
    //        var task = progressQueue.GetItems().OfType<ImportUsersTask>().FirstOrDefault(t => (int)t.Id == TenantProvider.CurrentTenantID);
    //        if (task == null) return null;

    //        return new
    //        {
    //            Completed = task.IsCompleted,
    //            Percents = (int)task.Percentage,
    //            UserCounter = task.GetUserCounter,
    //            Status = (int)task.Status,
    //            Error = (string)task.Error,
    //            task.Data
    //        };
    //    }
    //}
}

[ConstraintRoute("int")]
public class UserControllerAdditionalInternal(
    FileSharing fileSharing,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    FileSecurity fileSecurity,
    ApiContext apiContext,
    IDaoFactory daoFactory,
    AuthContext authContext,
    UserManager userManager)
    : UserControllerAdditional<int>(fileSharing, employeeFullDtoHelper, fileSecurity, apiContext, daoFactory, authContext, userManager);

public class UserControllerAdditionalThirdParty(
    FileSharing fileSharing,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    FileSecurity fileSecurity,
    ApiContext apiContext,
    IDaoFactory daoFactory,
    AuthContext authContext,
    UserManager userManager)
    : UserControllerAdditional<string>(fileSharing, employeeFullDtoHelper, fileSecurity, apiContext, daoFactory, authContext, userManager);

public class UserControllerAdditional<T>(
    FileSharing fileSharing,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    FileSecurity fileSecurity,
    ApiContext apiContext,
    IDaoFactory daoFactory,
    AuthContext authContext,
    UserManager userManager)
    : ApiControllerBase
{
    /// <remarks>
    /// Returns the portal accounts that match a search, with whether each one has access to the room. Any signed-in user
    /// with read access to the room can call it, except a guest. Other callers get 403. If no room has the ID, the call
    /// returns 404. The call is read-only. If `filterValue` is empty, it returns every account that matches the other
    /// filters. With `excludeShared`, the call returns only accounts without access. With `includeShared`, it returns
    /// only accounts with access. If both are set, `excludeShared` wins. Without them, the `shared` field of each profile
    /// shows the access. The call does not sort the list. The `count` and `startIndex` parameters page over it, and
    /// `total` counts all matches. A caller who is not a DocSpace admin sees only the guests related to them. With `area`
    /// set to `All`, they also see guests with access. If `count` is out of range, the call returns 400. To search users
    /// and groups together, use `GET api/2.0/accounts/room/{id}/search`.
    /// </remarks>
    /// <summary>
    /// Search users for a room
    /// </summary>
    /// <path>api/2.0/people/room/{id}</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "The matching accounts, each with its access state for the room", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
    [SwaggerResponse(403, "The caller is a guest or cannot read the room")]
    [SwaggerResponse(404, "No room has the specified ID")]
    [HttpGet("room/{id}")]
    public async IAsyncEnumerable<EmployeeFullDto> GetUsersWithRoomShared(UsersWithFileEntitySharedRequestDto<T> inDto)
    {
        var room = (await daoFactory.GetFolderDao<T>().GetFolderAsync(inDto.Id)).NotFoundIfNull();

        await foreach (var p in GetUsers(inDto, room))
        {
            yield return p;
        }
    }
    /// <remarks>
    /// Returns the portal accounts that match a search, with whether each one has access to the folder. Any signed-in
    /// user with read access to the folder can call it, except a guest. Other callers get 403. If no folder has the ID,
    /// the call returns 404. The call is read-only. If `filterValue` is empty, it returns every account that matches the
    /// other filters. With `excludeShared`, the call returns only accounts without access. With `includeShared`, it
    /// returns only accounts with access. If both are set, `excludeShared` wins. Without them, the `shared` field of each
    /// profile shows the access. The call does not sort the list. The `count` and `startIndex` parameters page over it,
    /// and `total` counts all matches. A caller who is not a DocSpace admin sees only the guests related to them. With
    /// `area` set to `All`, they also see guests with access. If `count` is out of range, the call returns 400. To search
    /// users and groups together, use `GET api/2.0/accounts/folder/{id}/search`.
    /// </remarks>
    /// <summary>
    /// Search users for a folder
    /// </summary>
    /// <path>api/2.0/people/folder/{id}</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "The matching accounts, each with its access state for the folder", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
    [SwaggerResponse(403, "The caller is a guest or cannot read the folder")]
    [SwaggerResponse(404, "No folder has the specified ID")]
    [HttpGet("folder/{id}")]
    public async IAsyncEnumerable<EmployeeFullDto> GetUsersWithFoldersShared(UsersWithFileEntitySharedRequestDto<T> inDto)
    {
        var folder = (await daoFactory.GetFolderDao<T>().GetFolderAsync(inDto.Id)).NotFoundIfNull();

        await foreach (var p in GetUsers(inDto, folder))
        {
            yield return p;
        }
    }
    /// <remarks>
    /// Returns the portal accounts that match a search, with whether each one has access to the file. Any signed-in user
    /// with read access to the file can call it, except a guest. Other callers get 403. If no file has the ID, the call
    /// returns 404. The call is read-only. If `filterValue` is empty, it returns every account that matches the other
    /// filters. With `excludeShared`, the call returns only accounts without access. With `includeShared`, it returns
    /// only accounts with access. If both are set, `excludeShared` wins. Without them, the `shared` field of each profile
    /// shows the access. The call does not sort the list. The `count` and `startIndex` parameters page over it, and
    /// `total` counts all matches. A caller who is not a DocSpace admin sees only the guests related to them. With `area`
    /// set to `All`, they also see guests with access. If `count` is out of range, the call returns 400. To search users
    /// and groups together, use `GET api/2.0/accounts/file/{id}/search`.
    /// </remarks>
    /// <summary>
    /// Search users for a file
    /// </summary>
    /// <path>api/2.0/people/file/{id}</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "The matching accounts, each with its access state for the file", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
    [SwaggerResponse(403, "The caller is a guest or cannot read the file")]
    [SwaggerResponse(404, "No file has the specified ID")]
    [HttpGet("file/{id}")]
    public async IAsyncEnumerable<EmployeeFullDto> GetUsersWithFilesShared(UsersWithFileEntitySharedRequestDto<T> inDto)
    {
        var file = (await daoFactory.GetFileDao<T>().GetFileAsync(inDto.Id)).NotFoundIfNull();

        await foreach (var p in GetUsers(inDto, file))
        {
            yield return p;
        }
    }

    private async IAsyncEnumerable<EmployeeFullDto> GetUsers(UsersWithFileEntitySharedRequestDto<T> inDto, FileEntry<T> fileEntry)
    {
        var currentUser = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);

        if (!await fileSecurity.CanReadAsync(fileEntry) || await userManager.IsGuestAsync(currentUser.Id))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var includeStrangers = await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID);
        var securityDao = daoFactory.GetSecurityDao<T>();
        var parentUserIds = await fileSharing.GetPureSharesAsync(fileEntry, ShareFilterType.UserOrGroup, inDto.ActivationStatus, inDto.Text, 0, int.MaxValue).Select(r => r.Id).ToListAsync();

        var offset = inDto.StartIndex;
        var count = inDto.Count;
        var filterValue = inDto.Text;
        var filterSeparator = inDto.FilterSeparator;


        var totalUsers = await securityDao.GetUsersWithSharedCountAsync(fileEntry,
            filterValue,
            inDto.EmployeeStatus,
            inDto.ActivationStatus,
            inDto.ExcludeShared ?? false,
            inDto.IncludeShared ?? false,
            filterSeparator,
            includeStrangers,
            inDto.Area,
            inDto.InvitedByMe,
            inDto.InviterId,
            inDto.EmployeeTypes,
            parentUserIds);

        apiContext.SetCount(Math.Min(Math.Max(totalUsers - offset, 0), count)).SetTotalCount(totalUsers);

        await foreach (var u in securityDao.GetUsersWithSharedAsync(fileEntry,
                           filterValue,
                           inDto.EmployeeStatus,
                           inDto.ActivationStatus,
                           inDto.ExcludeShared ?? false,
                           inDto.IncludeShared ?? false,
                           filterSeparator,
                           includeStrangers,
                           inDto.Area,
                           inDto.InvitedByMe,
                           inDto.InviterId,
                           inDto.EmployeeTypes,
                           parentUserIds,
                           offset,
                           count))
        {
            yield return await employeeFullDtoHelper.GetFullAsync(u.UserInfo, u.Shared);
        }
    }
}
