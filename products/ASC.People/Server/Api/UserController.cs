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

using System.Globalization;

using ASC.Core.Common.Identity;

namespace ASC.People.Api;

///<summary>
/// User API.
///</summary>
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
    TenantUtil tenantUtil,
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
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    SettingsManager settingsManager,
    InstanceCrypto instanceCrypto,
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
    IdentityClient client,
    GroupFullDtoHelper groupFullDtoHelper)
    : PeopleControllerBase(userManager, permissionContext, apiContext, userPhotoManager, httpClientFactory, httpContextAccessor)
{
    /// <summary>
    /// Returns the user claims.
    /// </summary>
    /// <path>api/2.0/people/tokendiagnostics</path>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "Claims", typeof(object))]
    [HttpGet("tokendiagnostics")]
    public object GetClaims()
    {
        var result = new
        {
            Name = User.Identity?.Name ?? "Unknown Name",
            Claims = (from c in User.Claims select c.Type + ":" + c.Value).ToList()
        };

        return result;
    }


    /// <summary>
    /// Adds an activated portal user with the first name, last name, email address, and several optional parameters specified in the request.
    /// </summary>
    /// <short>
    /// Add an activated user
    /// </short>
    /// <path>api/2.0/people/active</path>
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
        user.Title = inDto.Title;
        user.Location = inDto.Location;
        user.Notes = inDto.Comment;

        if (inDto.Sex.HasValue)
        {
            user.Sex = inDto.Sex.Value == SexEnum.Male;
        }
        
        user.Spam = inDto.Spam;
        user.BirthDate = inDto.Birthday != null ? tenantUtil.DateTimeFromUtc(inDto.Birthday) : null;
        user.WorkFromDate = inDto.Worksfrom != null ? tenantUtil.DateTimeFromUtc(inDto.Worksfrom) : DateTime.UtcNow.Date;

        await UpdateContactsAsync(inDto.Contacts, user);

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

        if (inDto.Files != _userPhotoManager.GetDefaultPhotoAbsoluteWebPath())
        {
            await UpdatePhotoUrlAsync(inDto.Files, user);
        }

        await webhookManager.PublishAsync(WebhookTrigger.UserCreated, user);

        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <summary>
    /// Adds a new portal user with the first name, last name, email address, and several optional parameters specified in the request.
    /// </summary>
    /// <short>
    /// Add a user
    /// </short>
    /// <path>api/2.0/people</path>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "Newly added user with the detailed information", typeof(EmployeeFullDto))]
    [SwaggerResponse(403, "The invitation link is invalid or its validity has expired")]
    [HttpPost]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "LinkInvite,Everyone")]
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
            await _permissionContext.DemandPermissionsAsync(Constants.Action_AddRemoveUser);
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
            user.CreatedBy = model?.UiD;
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
        user.Title = inDto.Title;
        user.Location = inDto.Location;
        user.Notes = inDto.Comment;

        if (inDto.Sex.HasValue)
        {
            user.Sex = inDto.Sex.Value == SexEnum.Male;
        }

        user.Spam = inDto.Spam;
        user.BirthDate = inDto.Birthday != null && inDto.Birthday != DateTime.MinValue ? tenantUtil.DateTimeFromUtc(inDto.Birthday) : null;
        user.WorkFromDate = inDto.Worksfrom != null && inDto.Worksfrom != DateTime.MinValue ? tenantUtil.DateTimeFromUtc(inDto.Worksfrom) : DateTime.UtcNow.Date;
        user.Status = EmployeeStatus.Active;
        
        await UpdateContactsAsync(inDto.Contacts, user, !inDto.FromInviteLink);

        cache.Insert("REWRITE_URL" + tenantManager.GetCurrentTenantId(), HttpContext.Request.GetDisplayUrl(), TimeSpan.FromMinutes(5));

        var quotaLimit = false;
        
        try
        {
            user = await userManagerWrapper.AddUserAsync(user, inDto.PasswordHash, inDto.FromInviteLink, true, inDto.Type,
                inDto.FromInviteLink && linkData is { IsCorrect: true, ConfirmType: not ConfirmType.EmpInvite }, true, true, byEmail);
            if(inDto.Type is EmployeeType.Guest)
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

        if (inDto.Files != _userPhotoManager.GetDefaultPhotoAbsoluteWebPath())
        {
            await UpdatePhotoUrlAsync(inDto.Files, user);
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

    /// <summary>
    /// Invites users specified in the request to the current portal.
    /// </summary>
    /// <short>
    /// Invite users
    /// </short>
    /// <path>api/2.0/people/invite</path>
    /// <collection>list</collection>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "List of users", typeof(List<EmployeeDto>))]
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
               continue;
            }
            
            switch (invite.Type)
            {
                case EmployeeType.Guest:
                case EmployeeType.RoomAdmin when currentUserType is not EmployeeType.DocSpaceAdmin:
                case EmployeeType.DocSpaceAdmin when !currentUser.IsOwner(tenant):
                    continue;
            }

            var user = await _userManager.GetUserByEmailAsync(invite.Email);
            if (!user.Equals(Constants.LostUser))
            {
                if (user.Status == EmployeeStatus.Terminated)
                {
                    continue;
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
            var link = commonLinkUtility.GetInvitationLink(user.Email, invite.Type, authContext.CurrentAccount.ID, inDto.Culture);
            var shortenLink = await urlShortener.GetShortenLinkAsync(link);

            await studioNotifyService.SendDocSpaceInviteAsync(user.Email, shortenLink, inDto.Culture, true);
            messageService.Send(MessageAction.SendJoinInvite, MessageTarget.Create(user.Id), currentUser.DisplayUserName(displayUserSettingsHelper), user.Email);
            await socketManager.AddUserAsync(user);

            await webhookManager.PublishAsync(WebhookTrigger.UserInvited, user);
        }

        var result = new List<EmployeeDto>();

        var users = (await _userManager.GetUsersAsync()).Where(u => u.ActivationStatus == EmployeeActivationStatus.Pending);

        foreach (var user in users)
        {
            if (await _userManager.CanUserViewAnotherUserAsync(currentUser, user))
            {
                result.Add(await employeeDtoHelper.GetAsync(user));
            }
        }

        return result;
    }

    /// <summary>
    /// Sets a new password to the user with the ID specified in the request.
    /// </summary>
    /// <short>Change a user password</short>
    /// <path>api/2.0/people/{userid}/password</path>
    [Tags("People / Password")]
    [SwaggerResponse(200, "Detailed user information", typeof(EmployeeFullDto))]
    [SwaggerResponse(400, "Incorrect email")]
    [SwaggerResponse(403, "The invitation link is invalid or its validity has expired")]
    [SwaggerResponse(404, "User not found")]
    [AllowNotPayment]
    [HttpPut("{userid:guid}/password")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "PasswordChange,EmailChange,Activation,EmailActivation,Everyone")]
    public async Task<EmployeeFullDto> ChangeUserPassword(MemberBaseByIdRequestDto inDto)
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
            return null;
        }

        if (_userManager.IsSystemUser(user.Id) || user.Status == EmployeeStatus.Terminated)
        {
            throw new SecurityException();
        }

        var viewer = await _userManager.GetUsersAsync(securityContext.CurrentAccount.ID);

        var tenant = tenantManager.GetCurrentTenant();
        if (user.IsOwner(tenant) && viewer.Id != user.Id)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var email = string.IsNullOrEmpty(inDto.MemberBase.Email) && !string.IsNullOrEmpty(inDto.MemberBase.EncEmail)
            ? emailValidationKeyModelHelper.DecryptEmail(inDto.MemberBase.EncEmail)
            : inDto.MemberBase.Email;

        if (!string.IsNullOrEmpty(email))
        {
            email = email.Trim();

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
        }

        inDto.MemberBase.PasswordHash = (inDto.MemberBase.PasswordHash ?? "").Trim();

        if (string.IsNullOrEmpty(inDto.MemberBase.PasswordHash))
        {
            inDto.MemberBase.Password = (inDto.MemberBase.Password ?? "").Trim();

            if (!string.IsNullOrEmpty(inDto.MemberBase.Password))
            {
                await userManagerWrapper.CheckPasswordPolicyAsync(inDto.MemberBase.Password);
                inDto.MemberBase.PasswordHash = passwordHasher.GetClientPassword(inDto.MemberBase.Password);
            }
        }

        if (!string.IsNullOrEmpty(inDto.MemberBase.PasswordHash))
        {
            try
            {
                await securityContext.SetUserPasswordHashAsync(inDto.UserId, inDto.MemberBase.PasswordHash);

                var messageTarget = MessageTarget.Create(inDto.UserId);
                messageService.Send(MessageAction.UserUpdatedPassword, messageTarget);

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
        }

        return await employeeFullDtoHelper.GetFullAsync(await GetUserInfoAsync(inDto.UserId.ToString()));
    }

    /// <summary>
    /// Deletes a user with the ID specified in the request from the portal.
    /// </summary>
    /// <short>
    /// Delete a user
    /// </short>
    /// <path>api/2.0/people/{userid}</path>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "Deleted user detailed information", typeof(EmployeeFullDto))]
    [SwaggerResponse(400, "The user is not suspended")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "User not found")]
    [HttpDelete("{userid}")]
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
            throw new Exception("The user is not suspended");
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

        await client.DeleteClientsAsync(user.Id);
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

        await webhookManager.PublishAsync(WebhookTrigger.UserDeleted, user);

        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <summary>
    /// Deletes the current user profile.
    /// </summary>
    /// <short>
    /// Delete my profile
    /// </short>
    /// <path>api/2.0/people/@self</path>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "Detailed information about my profile", typeof(EmployeeFullDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "User not found")]
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

    /// <summary>
    /// Deletes guests from the list and excludes them from rooms to which they were invited.
    /// </summary>
    /// <short>
    /// Delete guests
    /// </short>
    /// <path>api/2.0/people/guests</path>
    [SwaggerResponse(200, "Request parameters for deleting guests")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [Tags("People / Guests")]
    [HttpDelete("guests")]
    public async Task DeleteGuests(UpdateMembersRequestDto inDto)
    {
        var currentUser = await _userManager.GetUsersAsync(authContext.CurrentAccount.ID);
        
        var type = await _userManager.GetUserTypeAsync(currentUser.Id);
        if (type != EmployeeType.RoomAdmin)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }
        
        var relations = await _userManager.GetUserRelationsAsync(currentUser.Id);

        foreach (var userId in inDto.UserIds)
        {
            var user = await _userManager.GetUsersAsync(userId);
            if (user.Equals(Constants.LostUser) || 
                user.Status == EmployeeStatus.Terminated || 
                !await _userManager.IsGuestAsync(user) ||       
                !relations.ContainsKey(user.Id))
            {
                continue;
            }
        
            var t1 = _userManager.DeleteUserRelationAsync(currentUser.Id, user.Id);
            var t2 = fileSecurity.RemoveSecuritiesAsync(user.Id, currentUser.Id, SubjectType.User);
            
            await Task.WhenAll(t1, t2).ContinueWith(async _ => await socketManager.DeleteGuestAsync(currentUser.Id, user.Id));
        }
    }

    /// <summary>
    /// Returns a link to share a guest with another user.
    /// </summary>
    /// <short>
    /// Get a guest sharing link
    /// </short>
    /// <path>api/2.0/people/guests/{userid}/share</path>
    [Tags("Portal / Guests")]
    [SwaggerResponse(200, "User share link", typeof(string))]
    [SwaggerResponse(404, "User not found")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("guests/{userid:guid}/share")]
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

        if (targetUserType is not EmployeeType.Guest || 
            await _userManager.GetUserTypeAsync(currentUser) is EmployeeType.Guest || 
            !await _userManager.CanUserViewAnotherUserAsync(currentUser, targetUser))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var link = commonLinkUtility.GetConfirmationEmailUrl(targetUser.Email, ConfirmType.GuestShareLink, $"{currentUserId}{inDto.UserId}", currentUserId);

        return await urlShortener.GetShortenLinkAsync(link);
    }

    /// <summary>
    /// Approves a guest sharing link and returns the detailed information about a guest.
    /// </summary>
    /// <short>
    /// Approve a guest sharing link
    /// </short>
    /// <path>api/2.0/people/guests/share/approve</path>
    [Tags("People / Guests")]
    [SwaggerResponse(200, "Detailed profile information", typeof(EmployeeFullDto))]
    [SwaggerResponse(404, "User not found")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [AllowNotPayment]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "GuestShareLink")]
    [HttpPost("guests/share/approve")]
    public async Task<EmployeeFullDto> ApproveGuestShareLink(EmailMemberRequestDto inDto)
    {
        await securityContext.AuthByClaimAsync();

        var targetUser = await _userManager.GetUserByEmailAsync(inDto.Email);

        if (Equals(targetUser, Constants.LostUser))
        {
            throw new ItemNotFoundException("User not found");
        }

        var targetUserType = await _userManager.GetUserTypeAsync(targetUser);

        if (targetUserType is not EmployeeType.Guest)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var currentUser = await _userManager.GetUsersAsync(authContext.CurrentAccount.ID);
        var currentUserType = await _userManager.GetUserTypeAsync(currentUser);

        if (currentUserType is EmployeeType.Guest or EmployeeType.User)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        if (!await _userManager.CanUserViewAnotherUserAsync(currentUser, targetUser))
        {
            await _userManager.AddUserRelationAsync(currentUser.Id, targetUser.Id);
        }

        return await employeeFullDtoHelper.GetFullAsync(targetUser);
    }

    /// <summary>
    /// Returns a list of users matching the status filter and search query.
    /// </summary>
    /// <short>
    /// Search users by status filter
    /// </short>
    /// <path>api/2.0/people/status/{status}/search</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "List of users with the detailed information", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
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
            list = list.WhereAwait(async x => await _userManager.IsUserInGroupAsync(x.Id, groupId));
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

    /// <summary>
    /// Returns a list of profiles for all the portal users.
    /// </summary>
    /// <short>
    /// Get profiles
    /// </short>
    /// <path>api/2.0/people</path>
    /// <collection>list</collection>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "List of users with the detailed information", typeof(IAsyncEnumerable<EmployeeFullDto>))]
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

    /// <summary>
    /// Returns the detailed information about a profile of the user with the email specified in the request.
    /// </summary>
    /// <short>
    /// Get a profile by user email
    /// </short>
    /// <path>api/2.0/people/email</path>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "Detailed profile information", typeof(EmployeeFullDto))]
    [SwaggerResponse(404, "User not found")]
    [AllowNotPayment]
    [HttpGet("email")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "LinkInvite,GuestShareLink,Everyone")]
    public async Task<EmployeeFullDto> GetProfileByEmail(GetMemberByEmailRequestDto inDto)
    {
        var cultureInfo = string.IsNullOrEmpty(inDto.Culture) ? null : new CultureInfo(inDto.Culture);

        var user = await _userManager.GetUserByEmailAsync(inDto.Email);

        var isConfirmLink = _httpContextAccessor.HttpContext!.User.Claims
            .Any(role => role.Type == ClaimTypes.Role &&
                ConfirmTypeExtensions.TryParse(role.Value, out var confirmType) &&
                (confirmType == ConfirmType.LinkInvite || confirmType == ConfirmType.GuestShareLink));

        if (user.Id == Constants.LostUser.Id)
        {
            throw new ItemNotFoundException(Resource.ResourceManager.GetString("ErrorUserNotFound", cultureInfo));
        }

        if (isConfirmLink)
        {
            return await employeeFullDtoHelper.GetSimple(user, false);
        }

        var currentUser = await _userManager.GetUsersAsync(authContext.CurrentAccount.ID);

        if (!await _userManager.CanUserViewAnotherUserAsync(currentUser, user))
        {
            throw new SecurityException(Resource.ResourceManager.GetString("ErrorAccessDenied", cultureInfo));
        }

        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <summary>
    /// Returns the detailed information about a profile of the user with the name specified in the request.
    /// </summary>
    /// <short>
    /// Get a profile by user name
    /// </short>
    /// <path>api/2.0/people/{username}</path>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "Detailed profile information", typeof(EmployeeFullDto))]
    [SwaggerResponse(400, "Incorect UserId")]
    [SwaggerResponse(404, "User not found")]
    [AllowNotPayment]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "LinkInvite,Everyone")]
    [HttpGet("{userid}", Order = 1)]
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

        var currentUser = await _userManager.GetUsersAsync(authContext.CurrentAccount.ID);

        if (!await _userManager.CanUserViewAnotherUserAsync(currentUser, user))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <summary>
    /// Returns a list of profiles filtered by the user status.
    /// </summary>
    /// <short>
    /// Get profiles by status
    /// </short>
    /// <path>api/2.0/people/status/{status}</path>
    /// <collection>list</collection>
    [Tags("People / User status")]
    [SwaggerResponse(200, "List of users with the detailed information", typeof(IAsyncEnumerable<EmployeeFullDto>))]
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

    /// <summary>
    /// Returns a list of users with full information about them matching the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Search users with detaailed information by extended filter
    /// </short>
    /// <path>api/2.0/people/filter</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "List of users with the detailed information", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
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

    /// <summary>
    /// Returns the information about the "People" module.
    /// </summary>
    /// <short>Get the People information</short>
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

    /// <summary>
    /// Returns a list of users matching the search query. This method uses the query parameters.
    /// </summary>
    /// <short>Search users (using query parameters)</short>
    /// <path>api/2.0/people/search</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "List of users", typeof(IAsyncEnumerable<EmployeeDto>))]
    [HttpGet("search")]
    public IAsyncEnumerable<EmployeeDto> SearchUsersByQuery(GetPeopleByQueryRequestDto inDto)
    {
        var query = new GetMemberByQueryRequestDto { Query = inDto.Query };
        return GetSearch(query);
    }

    /// <summary>
    /// Returns a list of users matching the search query.
    /// </summary>
    /// <short>Search users</short>
    /// <path>api/2.0/people/@search/{query}</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "List of users with the detailed information", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
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

    /// <summary>
    /// Returns a list of users matching the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Search users by extended filter
    /// </short>
    /// <path>api/2.0/people/simple/filter</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "List of users", typeof(IAsyncEnumerable<EmployeeDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
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

    /// <summary>
    /// Deletes a list of the users with the IDs specified in the request.
    /// </summary>
    /// <short>
    /// Delete users
    /// </short>
    /// <path>api/2.0/people/delete</path>
    /// <collection>list</collection>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "List of users with the detailed information", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(409, "Data reassign process is not complete")]
    [HttpPut("delete", Order = -1)]
    public async IAsyncEnumerable<EmployeeFullDto> RemoveUsers(UpdateMembersRequestDto inDto)
    {
        await _permissionContext.DemandPermissionsAsync(Constants.Action_AddRemoveUser);

        await CheckReassignProcessAsync(inDto.UserIds);

        var users = await inDto.UserIds.ToAsyncEnumerable().SelectAwait(async userId => await _userManager.GetUsersAsync(userId))
            .Where(u => !_userManager.IsSystemUser(u.Id) && !u.IsLDAP()).ToListAsync();

        var userNames = users.Select(x => x.DisplayUserName(false, displayUserSettingsHelper)).ToList();
        var tenant = tenantManager.GetCurrentTenant();
        var currentUser = await _userManager.GetUsersAsync(authContext.CurrentAccount.ID);
        var currentUserType = await _userManager.GetUserTypeAsync(currentUser.Id); 
        
        foreach (var user in users)
        {
            if (user.Status != EmployeeStatus.Terminated)
            {
                continue;
            }
            
            var userType = await _userManager.GetUserTypeAsync(user.Id);
            switch (userType)
            {
                case EmployeeType.RoomAdmin when currentUserType is not EmployeeType.DocSpaceAdmin:
                case EmployeeType.DocSpaceAdmin when !currentUser.IsOwner(tenant):
                    continue;
            }
            
            var isGuest = userType == EmployeeType.Guest;

            await client.DeleteClientsAsync(user.Id);
            await _userPhotoManager.RemovePhotoAsync(user.Id);
            await _userManager.DeleteUserAsync(user.Id);
            await fileSecurity.RemoveSubjectAsync(user.Id, true);
            await queueWorkerRemove.StartAsync(tenant.Id, user, securityContext.CurrentAccount.ID, false, false, isGuest);

            await webhookManager.PublishAsync(WebhookTrigger.UserDeleted, user);
        }

        messageService.Send(MessageAction.UsersDeleted, MessageTarget.Create(users.Select(x => x.Id)), userNames);

        foreach (var user in users)
        {
            yield return await employeeFullDtoHelper.GetFullAsync(user);
        }
    }

    /// <summary>
    /// Resends emails to the users who have not activated their emails.
    /// </summary>
    /// <short>
    /// Resend activation emails
    /// </short>
    /// <path>api/2.0/people/invite</path>
    /// <collection>list</collection>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "List of users with the detailed information", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [AllowNotPayment]
    [HttpPut("invite")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async IAsyncEnumerable<EmployeeFullDto> ResendUserInvites(UpdateMembersRequestDto inDto)
    {
        List<UserInfo> users;

        var currentUser = await _userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        if (currentUser == null || currentUser.Equals(Constants.LostUser))
        {
            throw new Exception(Resource.ErrorAccessDenied);
        }

        var currentUserType = await _userManager.GetUserTypeAsync(currentUser);

        var tenant = tenantManager.GetCurrentTenant();

        if (inDto.ResendAll)
        {
            if (currentUserType is EmployeeType.User or EmployeeType.Guest)
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }
            
            users = (await _userManager.GetUsersAsync())
                .Where(u => u.ActivationStatus == EmployeeActivationStatus.Pending)
                .ToList();
        }
        else
        {
            users = await inDto.UserIds.ToAsyncEnumerable()
                .Where(userId => !_userManager.IsSystemUser(userId))
                .SelectAwait(async userId => await _userManager.GetUsersAsync(userId))
                .ToListAsync();
        }

        Dictionary<Guid, UserRelation> userRelations = null;

        foreach (var user in users)
        {
            if (user.IsActive)
            {
                continue;
            }

            if (currentUserType == EmployeeType.DocSpaceAdmin || currentUser.Id == user.Id)
            {
                if (user.ActivationStatus == EmployeeActivationStatus.Activated)
                {
                    user.ActivationStatus = EmployeeActivationStatus.NotActivated;
                }
                if (user.ActivationStatus == (EmployeeActivationStatus.AutoGenerated | EmployeeActivationStatus.Activated))
                {
                    user.ActivationStatus = EmployeeActivationStatus.AutoGenerated;
                }

                await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);
            }

            if (user.ActivationStatus == EmployeeActivationStatus.Pending)
            {
                var type = await _userManager.GetUserTypeAsync(user.Id);
                if (currentUser.Id != user.Id && !await HasAccessInviteAsync(type, currentUser, user))
                {
                    continue;
                }

                var culture = user.GetCulture()?.Name;
                var link = commonLinkUtility.GetInvitationLink(user.Email, type, authContext.CurrentAccount.ID, culture);
                var shortenLink = await urlShortener.GetShortenLinkAsync(link);
                messageService.Send(MessageAction.SendJoinInvite, MessageTarget.Create(user.Id), currentUser.DisplayUserName(displayUserSettingsHelper), user.Email);
                await studioNotifyService.SendDocSpaceInviteAsync(user.Email, shortenLink, culture);
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

                await studioNotifyService.SendEmailActivationInstructionsAsync(user, user.Email);
            }
        }

        messageService.Send(MessageAction.UsersSentActivationInstructions, MessageTarget.Create(users.Select(x => x.Id)), 
            users.Select(x => x.DisplayUserName(false, displayUserSettingsHelper)));

        foreach (var user in users)
        {
            if (await _userManager.CanUserViewAnotherUserAsync(currentUser, user))
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

    /// <summary>
    /// Returns a theme which is set to the current portal.
    /// </summary>
    /// <short>
    /// Get the portal theme
    /// </short>
    /// <path>api/2.0/people/theme</path>
    [Tags("People / Theme")]
    [SwaggerResponse(200, "Theme", typeof(DarkThemeSettings))]
    [HttpGet("theme")]
    public async Task<DarkThemeSettings> GetPortalTheme()
    {
        return await settingsManager.LoadForCurrentUserAsync<DarkThemeSettings>();
    }

    /// <summary>
    /// Changes the current portal theme.
    /// </summary>
    /// <short>
    /// Change the portal theme
    /// </short>
    /// <path>api/2.0/people/theme</path>
    [Tags("People / Theme")]
    [SwaggerResponse(200, "Theme", typeof(DarkThemeSettings))]
    [HttpPut("theme")]
    public async Task<DarkThemeSettings> ChangePortalTheme(DarkThemeSettingsRequestDto inDto)
    {
        var darkThemeSettings = new DarkThemeSettings
        {
            Theme = inDto.Theme
        };

        await settingsManager.SaveForCurrentUserAsync(darkThemeSettings);

        return darkThemeSettings;
    }

    /// <summary>
    /// Returns the detailed information about the current user profile.
    /// </summary>
    /// <short>
    /// Get my profile
    /// </short>
    /// <path>api/2.0/people/@self</path>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "Detailed information about my profile", typeof(EmployeeFullDto))]
    [AllowNotPayment]
    [HttpGet("@self")]
    public async Task<EmployeeFullDto> GetSelfProfile()
    {
        var user = await _userManager.GetUserAsync(securityContext.CurrentAccount.ID, null);

        var result = await employeeFullDtoHelper.GetFullAsync(user);

        result.Theme = (await settingsManager.LoadForCurrentUserAsync<DarkThemeSettings>()).Theme;

        result.LoginEventId = cookieStorage.GetLoginEventIdFromCookie(cookiesManager.GetCookies(CookiesType.AuthKey));

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

    /// <summary>
    /// Sends a message to the user email with the instructions to change the email address connected to the portal.
    /// </summary>
    /// <short>
    /// Send instructions to change email
    /// </short>
    /// <path>api/2.0/people/email</path>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "Message text", typeof(string))]
    [SwaggerResponse(400, "Incorrect userId or email")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "User not found")]
    [AllowNotPayment]
    [HttpPost("email")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task<string> SendEmailChangeInstructions(UpdateMemberRequestDto inDto)
    {
        Guid.TryParse(inDto.UserId, out var userid);

        if (userid == Guid.Empty)
        {
            throw new ArgumentNullException(inDto.UserId);
        }

        var email = (inDto.Email ?? "").Trim();

        if (string.IsNullOrEmpty(email))
        {
            throw new Exception(Resource.ErrorEmailEmpty);
        }

        if (!email.TestEmailRegex())
        {
            throw new Exception(Resource.ErrorNotCorrectEmail);
        }

        var viewer = await _userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var viewerIsAdmin = await _userManager.IsDocSpaceAdminAsync(viewer);
        var user = await _userManager.GetUsersAsync(userid);

        if (_userManager.IsSystemUser(user.Id) || user.Status == EmployeeStatus.Terminated || user.Status ==  EmployeeStatus.Pending)
        {
            throw new Exception(Resource.ErrorUserNotFound);
        }

        if (!viewerIsAdmin && viewer.Id != user.Id)
        {
            throw new Exception(Resource.ErrorAccessDenied);
        }

        var tenant = tenantManager.GetCurrentTenant();
        if (user.IsOwner(tenant) && viewer.Id != user.Id)
        {
            throw new Exception(Resource.ErrorAccessDenied);
        }

        if (!viewer.IsOwner(tenant) && await _userManager.IsDocSpaceAdminAsync(user) && viewer.Id != user.Id)
        {
            throw new Exception(Resource.ErrorAccessDenied);
        }

        var existentUser = await _userManager.GetUserByEmailAsync(email);

        if (existentUser.Id != Constants.LostUser.Id)
        {
            throw new Exception(await customNamingPeople.Substitute<Resource>("ErrorEmailAlreadyExists"));
        }

        if (!viewerIsAdmin)
        {
            await studioNotifyService.SendEmailChangeInstructionsAsync(user, email);
        }
        else
        {
            if (email == user.Email)
            {
                throw new Exception(Resource.ErrorEmailsAreTheSame);
            }

            user.Email = email;
            user.ActivationStatus = EmployeeActivationStatus.NotActivated;
            await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);
            await cookiesManager.ResetUserCookieAsync(user.Id);
            await studioNotifyService.SendEmailActivationInstructionsAsync(user, email);
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

    /// <summary>
    /// Reminds a password to the user using the email address specified in the request.
    /// </summary>
    /// <short>
    /// Remind a user password
    /// </short>
    /// <path>api/2.0/people/password</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("People / Password")]
    [SwaggerResponse(200, "Email with the password", typeof(string))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [AllowNotPayment]
    [AllowAnonymous]
    [HttpPost("password")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task<string> SendUserPassword(EmailMemberRequestDto inDto)
    {
        if (authContext.IsAuthenticated)
        {
            var currentUser = await _userManager.GetUserByEmailAsync(inDto.Email);
            if (currentUser.Id != authContext.CurrentAccount.ID && !(await _userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID)))
            {
                throw new Exception(Resource.ErrorAccessDenied);
            }
        }

        var error = await userManagerWrapper.SendUserPasswordAsync(inDto.Email);
        if (!string.IsNullOrEmpty(error))
        {
            logger.ErrorPasswordRecovery(inDto.Email, error);
        }

        var pattern = authContext.IsAuthenticated ? Resource.MessagePasswordSendedToEmail : Resource.MessageYourPasswordSendedToEmail;
        return string.Format(pattern, inDto.Email);
    }

    /// <summary>
    /// Sets the required activation status to the list of users with the IDs specified in the request.
    /// </summary>
    /// <short>
    /// Set an activation status to the users
    /// </short>
    /// <path>api/2.0/people/activationstatus/{activationstatus}</path>
    /// <collection>list</collection>
    [Tags("People / User status")]
    [SwaggerResponse(200, "List of users with the detailed information", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [AllowNotPayment]
    [HttpPut("activationstatus/{activationstatus}")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "Activation,Everyone")]
    public async IAsyncEnumerable<EmployeeFullDto> UpdateUserActivationStatus(UpdateMemberActivationStatusRequestDto inDto)
    {
        await securityContext.AuthByClaimAsync();
        
        var tenant = tenantManager.GetCurrentTenant();
        var currentUser = await _userManager.GetUsersAsync(authContext.CurrentAccount.ID);
        var currentUserType = await _userManager.GetUserTypeAsync(currentUser.Id); 
        
        foreach (var id in inDto.UpdateMembers.UserIds.Where(userId => !_userManager.IsSystemUser(userId)))
        {
            await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(id), Constants.Action_EditUser);
            var u = await _userManager.GetUsersAsync(id);
            if (u.Id == Constants.LostUser.Id)
            {
                continue;
            }

            if (currentUser.Id != u.Id)
            {
                var userType = await _userManager.GetUserTypeAsync(u.Id);

                switch (userType)
                {
                    case EmployeeType.RoomAdmin when currentUserType is not EmployeeType.DocSpaceAdmin:
                    case EmployeeType.DocSpaceAdmin when !currentUser.IsOwner(tenant):
                        continue;
                }
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

    /// <summary>
    /// Updates the user culture code with the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Update a user culture code
    /// </short>
    /// <path>api/2.0/people/{userid}/culture</path>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "Detailed user information", typeof(EmployeeFullDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "User not found")]
    [HttpPut("{userid}/culture")]
    public async Task<EmployeeFullDto> UpdateMemberCulture(UpdateMemberCultureByIdRequestDto inDto)
    {
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

    /// <summary>
    /// Updates the data for the selected portal user with the first name, last name, email address, and/or optional parameters specified in the request.
    /// </summary>
    /// <short>
    /// Update a user
    /// </short>
    /// <path>api/2.0/people/{userid}</path>
    [Tags("People / Profiles")]
    [SwaggerResponse(200, "Updated user with the detailed information", typeof(EmployeeFullDto))]
    [SwaggerResponse(400, "Incorrect user name")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "User not found")]
    [HttpPut("{userid}", Order = 1)]
    public async Task<EmployeeFullDto> UpdateMember(UpdateMemberByIdRequestDto inDto)
    {
        var user = await GetUserInfoAsync(inDto.UserId);

        if (_userManager.IsSystemUser(user.Id))
        {
            throw new SecurityException();
        }

        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser);

        var changed = false;
        var self = securityContext.CurrentAccount.ID.Equals(user.Id);
        var currentUserIsDocSpaceAdmin = await _userManager.IsDocSpaceAdminAsync(securityContext.CurrentAccount.ID);
        
        //Update it
        if (self)
        {
            var isLdap = user.IsLDAP();
            var isSso = user.IsSSO();

            if (!isLdap && !isSso)
            {
                //Set common fields

                var firstName = inDto.UpdateMember.FirstName ?? user.FirstName;
                var lastName = inDto.UpdateMember.LastName ?? user.LastName;

                if (!userFormatter.IsValidUserName(firstName, lastName))
                {
                    throw new ArgumentException(Resource.ErrorIncorrectUserName);
                }

                user.FirstName = firstName;
                user.LastName = lastName;
                user.Location = inDto.UpdateMember.Location ?? user.Location;

                if (currentUserIsDocSpaceAdmin)
                {
                    user.Title = inDto.UpdateMember.Title ?? user.Title;
                }
            }

            user.Notes = inDto.UpdateMember.Comment ?? user.Notes;

            user.Sex = inDto.UpdateMember.Sex switch
            {
                SexEnum.Male => true,
                SexEnum.Female => false,
                _ => user.Sex
            };

            user.Spam = inDto.UpdateMember.Spam;

            user.BirthDate = inDto.UpdateMember.Birthday != null ? tenantUtil.DateTimeFromUtc(inDto.UpdateMember.Birthday) : user.BirthDate;

            var resetDate = new DateTime(1900, 01, 01);
            if (user.BirthDate == resetDate)
            {
                user.BirthDate = null;
            }

            user.WorkFromDate = inDto.UpdateMember.Worksfrom != null
                ? tenantUtil.DateTimeFromUtc(inDto.UpdateMember.Worksfrom)
                : user.WorkFromDate;

            if (user.WorkFromDate == resetDate)
            {
                user.WorkFromDate = null;
            }

            //Update contacts
            await UpdateContactsAsync(inDto.UpdateMember.Contacts, user);
            await UpdateDepartmentsAsync(inDto.UpdateMember.Department, user);

            if (inDto.UpdateMember.Files != await _userPhotoManager.GetPhotoAbsoluteWebPath(user.Id))
            {
                await UpdatePhotoUrlAsync(inDto.UpdateMember.Files, user);
            }

            changed = true;
        }
        
        var tenant = tenantManager.GetCurrentTenant();
        var userIsOwner = user.IsOwner(tenant);
        var currentUserIsOwner = securityContext.CurrentAccount.ID.IsOwner(tenant);
        var userType = await _userManager.GetUserTypeAsync(user.Id); 
        var statusChanged = false;
        
        if ((self || currentUserIsOwner || currentUserIsDocSpaceAdmin && !userIsOwner && userType != EmployeeType.DocSpaceAdmin) && inDto.UpdateMember.Disable.HasValue)
        {
            user.Status = inDto.UpdateMember.Disable.Value ? EmployeeStatus.Terminated : EmployeeStatus.Active;
            user.TerminatedDate = inDto.UpdateMember.Disable.Value ? DateTime.UtcNow : null;
            changed = true;
            statusChanged = true;
        }


        // change user type
        var canBeGuestFlag = !userIsOwner && 
                             !await _userManager.IsDocSpaceAdminAsync(user) && 
                             (await user.GetListAdminModulesAsync(webItemSecurity, webItemManager)).Count == 0 && 
                             !self;

        if (inDto.UpdateMember.IsUser.HasValue)
        {
            var isGuest = inDto.UpdateMember.IsUser.Value;
            
            if (isGuest && canBeGuestFlag && !await _userManager.IsGuestAsync(user))
            {
                await using (await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetUsersCountCheckKey(tenant.Id)))
                {
                    await activeUsersChecker.CheckAppend();
                    await _userManager.AddUserIntoGroupAsync(user.Id, Constants.GroupGuest.ID);
                    await webItemSecurityCache.ClearCacheAsync(tenant.Id);
                    changed = true;
                }
            }
            else if (!self && !isGuest && await _userManager.IsGuestAsync(user))
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

    /// <summary>
    /// Changes a status of the users with the IDs specified in the request.
    /// </summary>
    /// <short>
    /// Change a user status
    /// </short>
    /// <path>api/2.0/people/status/{status}</path>
    /// <collection>list</collection>
    [Tags("People / User status")]
    [SwaggerResponse(200, "List of users with the detailed information", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [HttpPut("status/{status}")]
    public async IAsyncEnumerable<EmployeeFullDto> UpdateUserStatus(UpdateMemberStatusRequestDto inDto)
    {
        await _permissionContext.DemandPermissionsAsync(Constants.Action_EditUser);

        var tenant = tenantManager.GetCurrentTenant();
        var users = await inDto.UpdateMembers.UserIds.ToAsyncEnumerable().SelectAwait(async userId => await _userManager.GetUsersAsync(userId))
            .Where(u => !_userManager.IsSystemUser(u.Id) && !u.IsLDAP()).ToListAsync();

        foreach (var user in users)
        {
            if (user.IsOwner(tenant) || authContext.CurrentAccount.ID != tenant.OwnerId && await _userManager.IsDocSpaceAdminAsync(user) || user.IsMe(authContext))
            {
                continue;
            }

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

    /// <summary>
    /// Changes a type of the users with the IDs specified in the request.
    /// </summary>
    /// <short>
    /// Change a user type
    /// </short>
    /// <path>api/2.0/people/type/{type}</path>
    /// <collection>list</collection>
    [Tags("People / User type")]
    [SwaggerResponse(200, "List of users with the detailed information", typeof(IAsyncEnumerable<EmployeeFullDto>))]
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

        var users = await inDto.UpdateMembers.UserIds
            .ToAsyncEnumerable()
            .Where(userId => !_userManager.IsSystemUser(userId))
            .SelectAwait(async userId => await _userManager.GetUsersAsync(userId))
            .Where(r => r.Status != EmployeeStatus.Terminated)
            .ToListAsync();

        foreach (var user in users)
        {
            var isGuest = await _userManager.IsGuestAsync(user);
            await userManagerWrapper.UpdateUserTypeAsync(user, inDto.Type);
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
            await studioNotifyService.SendMsgUserTypeChangedAsync(user, FilesCommonResource.ResourceManager.GetString("RoleEnum_" + inDto.Type.ToStringFast()));
        }

        messageService.Send(MessageAction.UsersUpdatedType, MessageTarget.Create(users.Select(x => x.Id)),
        users.Select(x => x.DisplayUserName(false, displayUserSettingsHelper)), users.Select(x => x.Id).ToList(), inDto.Type);

        foreach (var user in users)
        {
            await webhookManager.PublishAsync(WebhookTrigger.UserUpdated, user);

            yield return await employeeFullDtoHelper.GetFullAsync(user);
        }
    }

    /// <summary>
    /// Starts updating the type of the user or guest when reassigning rooms and shared files.
    /// </summary>
    /// <short>Update user type</short>
    /// <path>api/2.0/people/type</path>
    [Tags("People / User type")]
    [SwaggerResponse(200, "Update type progress", typeof(TaskProgressResponseDto))]
    [SwaggerResponse(400, "Can not update user type")]
    [HttpPost("type")]
    public async Task<TaskProgressResponseDto> StarUserTypetUpdate(StartUpdateUserTypeDto inDto)
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

        if (inDto.Type is EmployeeType.RoomAdmin or EmployeeType.DocSpaceAdmin)
        {
            throw new ArgumentException($"Can not update to {inDto.Type}");
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

    /// <summary>
    /// Returns the progress of updating the user type.
    /// </summary>
    /// <short>Get the progress of updating user type</short>
    /// <path>api/2.0/people/type/progress/{userid}</path>
    [Tags("People / User type")]
    [SwaggerResponse(200, "Update type progress", typeof(TaskProgressResponseDto))]
    [HttpGet("type/progress/{userid:guid}")]
    public async Task<TaskProgressResponseDto> GetUserTypeUpdateProgress(UserIdRequestDto inDto)
    {
        await _permissionContext.DemandPermissionsAsync(Constants.Action_AddRemoveUser);

        var tenant = tenantManager.GetCurrentTenant();
        var progressItem = await queueWorkerUpdateUserType.GetProgressItemStatus(tenant.Id, inDto.UserId);

        return TaskProgressResponseDto.Get(progressItem);
    }

    /// <summary>
    /// Terminates the process of updating the type of the user or guest.
    /// </summary>
    /// <short>Terminate update user type</short>
    /// <path>api/2.0/people/type/terminate</path>
    [Tags("People / User type")]
    [SwaggerResponse(200, "Update type progress", typeof(TaskProgressResponseDto))]
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

    ///<summary>
    /// Starts the process of recalculating a quota.
    /// </summary>
    /// <short>
    /// Recalculate a quota 
    /// </short>
    /// <path>api/2.0/people/recalculatequota</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("People / Quota")]
    [HttpGet("recalculatequota")]
    public async Task RecalculateQuota()
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        await usersQuotaSyncOperation.RecalculateQuota(tenantManager.GetCurrentTenant());
    }

    /// <summary>
    /// Checks the process of recalculating a quota.
    /// </summary>
    /// <short>
    /// Check the quota recalculation
    /// </short>
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

    /// <summary>
    /// Changes a quota limit for the users with the IDs specified in the request.
    /// </summary>
    /// <short>
    /// Change a user quota limit
    /// </short>
    /// <path>api/2.0/people/userquota</path>
    /// <collection>list</collection>
    [Tags("People / Quota")]
    [SwaggerResponse(200, "List of users with the detailed information", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(402, "Failed to set quota per user. The entered value is greater than the total DocSpace storage")]
    [HttpPut("userquota")]
    public async IAsyncEnumerable<EmployeeFullDto> UpdateUserQuota(UpdateMembersQuotaRequestDto inDto)
    {
        if (!inDto.Quota.TryGetInt64(out var quota))
        {
            throw new Exception(Resource.UserQuotaGreaterPortalError);
        }

        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var users = await inDto.UserIds.ToAsyncEnumerable()
            .Where(userId => !_userManager.IsSystemUser(userId))
            .SelectAwait(async userId => await _userManager.GetUsersAsync(userId))
            .ToListAsync();

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

        foreach (var user in users)
        {
            await settingsManager.SaveAsync(new UserQuotaSettings { UserQuota = quota }, user);

            var userUsedSpace = Math.Max(0, (await quotaService.FindUserQuotaRowsAsync(tenant.Id, user.Id)).Where(r => !string.IsNullOrEmpty(r.Tag) && !string.Equals(r.Tag, Guid.Empty.ToString())).Sum(r => r.Counter));
            var quotaUserSettings = await settingsManager.LoadAsync<TenantUserQuotaSettings>();
            _ = quotaSocketManager.ChangeCustomQuotaUsedValueAsync(tenant.Id, customQuota.GetFeature<UserCustomQuotaFeature>().Name, quotaUserSettings.EnableQuota, userUsedSpace, quota, [user.Id]);
            await socketManager.UpdateUserAsync(user);
            yield return await employeeFullDtoHelper.GetFullAsync(user);
        }

        if(quota >= 0)
        {
            messageService.Send(MessageAction.CustomQuotaPerUserChanged, inDto.Quota.ToString(),
                        users.Select(x => HttpUtility.HtmlDecode(displayUserSettingsHelper.GetFullUserName(x))));
        }
        else
        {
            messageService.Send(MessageAction.CustomQuotaPerUserDisabled, MessageTarget.Create(users.Select(x => x.Id)), users.Select(x => HttpUtility.HtmlDecode(displayUserSettingsHelper.GetFullUserName(x))));
        }
        

    }

    /// <summary>
    /// Resets a quota limit of users with the IDs specified in the request.
    /// </summary>
    /// <short>
    /// Reset a user quota limit
    /// </short>
    /// <path>api/2.0/people/resetquota</path>
    /// <collection>list</collection>
    [Tags("People / Quota")]
    [SwaggerResponse(200, "User detailed information", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [SwaggerResponse(403, "The invitation link is invalid or its validity has expired")]
    [SwaggerResponse(409, "Conflict - system user quota cannot be reset")]
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
            .SelectAwait(async userId => await _userManager.GetUsersAsync(userId))
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
        foreach (var groupInfoId in groups.Select(r=> r.ID))
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
        
        var isDocSpaceAdmin = (await _userManager.IsDocSpaceAdminAsync(securityContext.CurrentAccount.ID)) ||
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

    ///// <summary>
    ///// Imports the new portal users with the first name, last name, and email address.
    ///// </summary>
    ///// <short>
    ///// Import users
    ///// </short>
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

    // <summary>
    // Returns a status of the current user.
    // </summary>
    // <short>
    // Get a user status
    // </short>
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
    EmployeeFullDtoHelper employeeFullDtoHelper, 
    FileSecurity fileSecurity, 
    ApiContext apiContext, 
    IDaoFactory daoFactory,
    AuthContext authContext,
    UserManager userManager) 
    : UserControllerAdditional<int>(employeeFullDtoHelper, fileSecurity, apiContext, daoFactory, authContext, userManager);
        
public class UserControllerAdditionalThirdParty(
    EmployeeFullDtoHelper employeeFullDtoHelper, 
    FileSecurity fileSecurity, 
    ApiContext apiContext, 
    IDaoFactory daoFactory,
    AuthContext authContext,
    UserManager userManager) 
    : UserControllerAdditional<string>(employeeFullDtoHelper, fileSecurity, apiContext, daoFactory, authContext, userManager);
        
public class UserControllerAdditional<T>(
    EmployeeFullDtoHelper employeeFullDtoHelper, 
    FileSecurity fileSecurity, 
    ApiContext apiContext, 
    IDaoFactory daoFactory,
    AuthContext authContext,
    UserManager userManager) 
    : ApiControllerBase 
{
    /// <summary>
    /// Returns the users with the sharing settings in a room with the ID specified in request.
    /// </summary>
    /// <short>
    /// Get users with room sharing settings
    /// </short>
    /// <path>api/2.0/people/room/{id}</path>
    [Tags("People / Search")]
    [SwaggerResponse(200, "Ok", typeof(IAsyncEnumerable<EmployeeFullDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("room/{id}")]
    public async IAsyncEnumerable<EmployeeFullDto> GetUsersWithRoomShared(UsersWithRoomSharedRequestDto<T> inDto)
    {
        var room = (await daoFactory.GetFolderDao<T>().GetFolderAsync(inDto.Id)).NotFoundIfNull();

        if (!await fileSecurity.CanReadAsync(room))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var includeStrangers = await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID);
        
        var offset = inDto.StartIndex;
        var count = inDto.Count;
        var filterValue = inDto.Text;
        var filterSeparator = inDto.FilterSeparator;

        var securityDao = daoFactory.GetSecurityDao<T>();

        var totalUsers = await securityDao.GetUsersWithSharedCountAsync(room,
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
            inDto.EmployeeTypes);

        apiContext.SetCount(Math.Min(Math.Max(totalUsers - offset, 0), count)).SetTotalCount(totalUsers);

        await foreach (var u in securityDao.GetUsersWithSharedAsync(room, 
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
                           offset,
                           count))
        {
            yield return await employeeFullDtoHelper.GetFullAsync(u.UserInfo, u.Shared);
        }
    }
}