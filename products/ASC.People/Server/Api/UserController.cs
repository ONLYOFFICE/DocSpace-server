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

using ASC.Api.Core.Core;

namespace ASC.People.Api;

public class UserController(
    CommonLinkUtility commonLinkUtility,
    ICache cache,
    TenantManager tenantManager,
    CookiesManager cookiesManager,
    CustomNamingPeople customNamingPeople,
    EmployeeDtoHelper employeeDtoHelper,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    ILogger<UserController> logger,
    PasswordHasher passwordHasher,
    QueueWorkerReassign queueWorkerReassign,
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
    InvitationService invitationService,
    FileSecurity fileSecurity,
    UsersQuotaSyncOperation usersQuotaSyncOperation,
    CountPaidUserChecker countPaidUserChecker,
    CountUserChecker activeUsersChecker,
    UsersInRoomChecker usersInRoomChecker,
    IUrlShortener urlShortener,
    FileSecurityCommon fileSecurityCommon, 
    IDistributedLockProvider distributedLockProvider,
    QuotaSocketManager quotaSocketManager,
    IQuotaService quotaService,
    CustomQuota customQuota)
    : PeopleControllerBase(userManager, permissionContext, apiContext, userPhotoManager, httpClientFactory, httpContextAccessor)
{
    

    /// <summary>
    /// Adds an activated portal user with the first name, last name, email address, and several optional parameters specified in the request.
    /// </summary>
    /// <short>
    /// Add an activated user
    /// </short>
    /// <category>Profiles</category>
    /// <param type="ASC.People.ApiModels.RequestDto.MemberRequestDto, ASC.People" name="inDto">Member request parameters</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">Newly added user with the detailed information</returns>
    /// <path>api/2.0/people/active</path>
    /// <httpMethod>POST</httpMethod>
    /// <visible>false</visible>
    [HttpPost("active")]
    public async Task<EmployeeFullDto> AddMemberAsActivatedAsync(MemberRequestDto inDto)
    {
        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(inDto.Type), Constants.Action_AddRemoveUser);

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
        user.FirstName = inDto.Firstname;
        user.LastName = inDto.Lastname;
        user.Title = inDto.Title;
        user.Location = inDto.Location;
        user.Notes = inDto.Comment;

        if ("male".Equals(inDto.Sex, StringComparison.OrdinalIgnoreCase))
        {
            user.Sex = true;
        }
        else if ("female".Equals(inDto.Sex, StringComparison.OrdinalIgnoreCase))
        {
            user.Sex =  false;
        }
        
        user.BirthDate = inDto.Birthday != null ? tenantUtil.DateTimeFromUtc(inDto.Birthday) : null;
        user.WorkFromDate = inDto.Worksfrom != null ? tenantUtil.DateTimeFromUtc(inDto.Worksfrom) : DateTime.UtcNow.Date;

        await UpdateContactsAsync(inDto.Contacts, user);

        cache.Insert("REWRITE_URL" + await tenantManager.GetCurrentTenantIdAsync(), HttpContext.Request.GetDisplayUrl(), TimeSpan.FromMinutes(5));
        user = await userManagerWrapper.AddUserAsync(user, inDto.PasswordHash, false, false, inDto.Type,
            false, true, true);

        await UpdateDepartmentsAsync(inDto.Department, user);

        if (inDto.Files != _userPhotoManager.GetDefaultPhotoAbsoluteWebPath())
        {
            await UpdatePhotoUrlAsync(inDto.Files, user);
        }

        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <summary>
    /// Adds a new portal user with the first name, last name, email address, and several optional parameters specified in the request.
    /// </summary>
    /// <short>
    /// Add a user
    /// </short>
    /// <category>Profiles</category>
    /// <param type="ASC.People.ApiModels.RequestDto.MemberRequestDto, ASC.People" name="inDto">Member request parameters</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">Newly added user with the detailed information</returns>
    /// <path>api/2.0/people</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "LinkInvite,Everyone")]
    public async Task<EmployeeFullDto> AddMember(MemberRequestDto inDto)
    {
        await _apiContext.AuthByClaimAsync();

        var linkData = inDto.FromInviteLink ? await invitationService.GetInvitationDataAsync(inDto.Key, inDto.Email, inDto.Type) : null;
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
        }

        inDto.Type = linkData?.EmployeeType ?? inDto.Type;

        var user = new UserInfo();

        var byEmail = linkData?.LinkType == InvitationLinkType.Individual;

        if (byEmail)
        {
            user = await _userManager.GetUserByEmailAsync(inDto.Email);

            if (user.Equals(Constants.LostUser) || user.ActivationStatus != EmployeeActivationStatus.Pending)
            {
                throw new SecurityException(FilesCommonResource.ErrorMessage_InvintationLink);
            }

            await userInvitationLimitHelper.IncreaseLimit();
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
        user.FirstName = inDto.Firstname;
        user.LastName = inDto.Lastname;
        user.Title = inDto.Title;
        user.Location = inDto.Location;
        user.Notes = inDto.Comment;

        if ("male".Equals(inDto.Sex, StringComparison.OrdinalIgnoreCase))
        {
            user.Sex = true;
        }
        else if ("female".Equals(inDto.Sex, StringComparison.OrdinalIgnoreCase))
        {
            user.Sex =  false;
        }
        
        user.BirthDate = inDto.Birthday != null && inDto.Birthday != DateTime.MinValue ? tenantUtil.DateTimeFromUtc(inDto.Birthday) : null;
        user.WorkFromDate = inDto.Worksfrom != null && inDto.Worksfrom != DateTime.MinValue ? tenantUtil.DateTimeFromUtc(inDto.Worksfrom) : DateTime.UtcNow.Date;
        user.Status = EmployeeStatus.Active;
        
        await UpdateContactsAsync(inDto.Contacts, user, !inDto.FromInviteLink);

        cache.Insert("REWRITE_URL" + await tenantManager.GetCurrentTenantIdAsync(), HttpContext.Request.GetDisplayUrl(), TimeSpan.FromMinutes(5));

        user = await userManagerWrapper.AddUserAsync(user, inDto.PasswordHash, inDto.FromInviteLink, true, inDto.Type,
            inDto.FromInviteLink && linkData is { IsCorrect: true, ConfirmType: not ConfirmType.EmpInvite }, true, true, byEmail);

        await UpdateDepartmentsAsync(inDto.Department, user);

        if (inDto.Files != _userPhotoManager.GetDefaultPhotoAbsoluteWebPath())
        {
            await UpdatePhotoUrlAsync(inDto.Files, user);
        }

        if (linkData is { LinkType: InvitationLinkType.CommonToRoom })
        {
            var success = int.TryParse(linkData.RoomId, out var id);
            var tenantId = await tenantManager.GetCurrentTenantIdAsync();

            await using (await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetUsersInRoomCountCheckKey(tenantId)))
            {
                if (success)
                {
                    await usersInRoomChecker.CheckAppend();
                    await fileSecurity.ShareAsync(id, FileEntryType.Folder, user.Id, linkData.Share);
                }
                else
                {
                    await usersInRoomChecker.CheckAppend();
                    await fileSecurity.ShareAsync(linkData.RoomId, FileEntryType.Folder, user.Id, linkData.Share);
                }
            }
        }

        if (inDto.IsUser.GetValueOrDefault(false))
        {
            await messageService.SendAsync(MessageAction.GuestCreated, MessageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper));
        }
        else
        {
            await messageService.SendAsync(MessageAction.UserCreated, MessageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper), user.Id);
        }

        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <summary>
    /// Invites users specified in the request to the current portal.
    /// </summary>
    /// <short>
    /// Invite users
    /// </short>
    /// <category>Profiles</category>
    /// <param type="ASC.People.ApiModels.RequestDto.InviteUsersRequestDto, ASC.People" name="inDto">Request parameters for inviting users</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeDto, ASC.Api.Core">List of users</returns>
    /// <path>api/2.0/people/invite</path>
    /// <httpMethod>POST</httpMethod>
    /// <collection>list</collection>
    [HttpPost("invite")]
    [EnableRateLimiting(RateLimiterPolicy.EmailInvitationApi)]
    public async Task<List<EmployeeDto>> InviteUsersAsync(InviteUsersRequestDto inDto)
    {
        ArgumentNullException.ThrowIfNull(inDto);
        ArgumentNullException.ThrowIfNull(inDto.Invitations);

        var currentUser = await _userManager.GetUsersAsync(authContext.CurrentAccount.ID);

        var tenant = await tenantManager.GetCurrentTenantAsync();

        foreach (var invite in inDto.Invitations)
        {
            if ((invite.Type == EmployeeType.DocSpaceAdmin && !currentUser.IsOwner(tenant)) ||
                !await _permissionContext.CheckPermissionsAsync(new UserSecurityProvider(Guid.Empty, invite.Type), Constants.Action_AddRemoveUser))
            {
                continue;
            }

            var user = await userManagerWrapper.AddInvitedUserAsync(invite.Email, invite.Type, inDto.Culture);
            var link = await commonLinkUtility.GetInvitationLinkAsync(user.Email, invite.Type, authContext.CurrentAccount.ID, inDto.Culture);
            var shortenLink = await urlShortener.GetShortenLinkAsync(link);

            await studioNotifyService.SendDocSpaceInviteAsync(user.Email, shortenLink, inDto.Culture, true);
            await messageService.SendAsync(MessageAction.SendJoinInvite, MessageTarget.Create(user.Id), currentUser.DisplayUserName(displayUserSettingsHelper), user.Email);
        }

        var result = new List<EmployeeDto>();

        var users = (await _userManager.GetUsersAsync()).Where(u => u.ActivationStatus == EmployeeActivationStatus.Pending);

        foreach (var user in users)
        {
            result.Add(await employeeDtoHelper.GetAsync(user));
        }

        return result;
    }

    /// <summary>
    /// Sets a new password to the user with the ID specified in the request.
    /// </summary>
    /// <short>Change a user password</short>
    /// <category>Password</category>
    /// <param type="System.Guid, System" method="url" name="userid">User ID</param>
    /// <param type="ASC.People.ApiModels.RequestDto.MemberRequestDto, ASC.People" name="inDto">Request parameters for setting new password</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">Detailed user information</returns>
    /// <path>api/2.0/people/{userid}/password</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{userid:guid}/password")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "PasswordChange,EmailChange,Activation,EmailActivation,Everyone")]
    public async Task<EmployeeFullDto> ChangeUserPassword(Guid userid, MemberRequestDto inDto)
    {
        await _apiContext.AuthByClaimAsync();
        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(userid), Constants.Action_EditUser);

        var user = await _userManager.GetUsersAsync(userid);

        if (!_userManager.UserExists(user))
        {
            return null;
        }

        if (_userManager.IsSystemUser(user.Id))
        {
            throw new SecurityException();
        }

        if (!string.IsNullOrEmpty(inDto.Email))
        {
            var address = new MailAddress(inDto.Email);
            if (!string.Equals(address.Address, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = address.Address.ToLowerInvariant();
                user.ActivationStatus = EmployeeActivationStatus.Activated;
                await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);
            }
        }

        inDto.PasswordHash = (inDto.PasswordHash ?? "").Trim();

        if (string.IsNullOrEmpty(inDto.PasswordHash))
        {
            inDto.Password = (inDto.Password ?? "").Trim();

            if (!string.IsNullOrEmpty(inDto.Password))
            {
                await userManagerWrapper.CheckPasswordPolicyAsync(inDto.Password);
                inDto.PasswordHash = passwordHasher.GetClientPassword(inDto.Password);
            }
        }

        if (!string.IsNullOrEmpty(inDto.PasswordHash))
        {
            await securityContext.SetUserPasswordHashAsync(userid, inDto.PasswordHash);
            await messageService.SendAsync(MessageAction.UserUpdatedPassword);

            await cookiesManager.ResetUserCookieAsync(userid, false);
            await messageService.SendAsync(MessageAction.CookieSettingsUpdated);
        }

        return await employeeFullDtoHelper.GetFullAsync(await GetUserInfoAsync(userid.ToString()));
    }

    /// <summary>
    /// Deletes a user with the ID specified in the request from the portal.
    /// </summary>
    /// <short>
    /// Delete a user
    /// </short>
    /// <category>Profiles</category>
    /// <param type="System.String, System" method="url" name="userid">User ID</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">Deleted user detailed information</returns>
    /// <path>api/2.0/people/{userid}</path>
    /// <httpMethod>DELETE</httpMethod>
    [HttpDelete("{userid}")]
    public async Task<EmployeeFullDto> DeleteMemberAsync(string userid)
    {
        await _permissionContext.DemandPermissionsAsync(Constants.Action_AddRemoveUser);

        var user = await GetUserInfoAsync(userid);

        if (_userManager.IsSystemUser(user.Id) || user.IsLDAP())
        {
            throw new SecurityException();
        }

        if (user.Status != EmployeeStatus.Terminated)
        {
            throw new Exception("The user is not suspended");
        }

        var currentUser = await _userManager.GetUsersAsync(authContext.CurrentAccount.ID);

        if (await fileSecurityCommon.IsDocSpaceAdministratorAsync(user.Id) && !currentUser.IsOwner(await tenantManager.GetCurrentTenantAsync()))
        {
            throw new SecurityException();
        }
        
        await CheckReassignProcessAsync(new[] { user.Id });

        var userName = user.DisplayUserName(false, displayUserSettingsHelper);
        await _userPhotoManager.RemovePhotoAsync(user.Id);
        await _userManager.DeleteUserAsync(user.Id);
        await fileSecurity.RemoveSubjectAsync(user.Id, true);
        var tenant = await tenantManager.GetCurrentTenantAsync();
        await queueWorkerRemove.StartAsync(tenant.Id, user, securityContext.CurrentAccount.ID, false, false);

        await messageService.SendAsync(MessageAction.UserDeleted, MessageTarget.Create(user.Id), userName);

        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <summary>
    /// Deletes the current user profile.
    /// </summary>
    /// <short>
    /// Delete my profile
    /// </short>
    /// <category>Profiles</category>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">Detailed information about my profile</returns>
    /// <path>api/2.0/people/@self</path>
    /// <httpMethod>DELETE</httpMethod>
    [HttpDelete("@self")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "ProfileRemove")]
    public async Task<EmployeeFullDto> DeleteProfile()
    {
        await _apiContext.AuthByClaimAsync();

        if (_userManager.IsSystemUser(securityContext.CurrentAccount.ID))
        {
            throw new SecurityException();
        }

        var user = await GetUserInfoAsync(securityContext.CurrentAccount.ID.ToString());

        if (!_userManager.UserExists(user))
        {
            throw new Exception(Resource.ErrorUserNotFound);
        }
        
        var tenant = await tenantManager.GetCurrentTenantAsync();
        if (user.IsLDAP() || user.IsOwner(tenant))
        {
            throw new SecurityException();
        }

        await securityContext.AuthenticateMeWithoutCookieAsync(Core.Configuration.Constants.CoreSystem);
        user.Status = EmployeeStatus.Terminated;

        await _userManager.UpdateUserInfoAsync(user);
        var userName = user.DisplayUserName(false, displayUserSettingsHelper);
        await messageService.SendAsync(MessageAction.UsersUpdatedStatus, MessageTarget.Create(user.Id), userName);

        await cookiesManager.ResetUserCookieAsync(user.Id);
        await messageService.SendAsync(MessageAction.CookieSettingsUpdated);

        await studioNotifyService.SendMsgProfileHasDeletedItselfAsync(user);

        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <summary>
    /// Returns a list of users matching the status filter and search query.
    /// </summary>
    /// <short>
    /// Search users by status filter
    /// </short>
    /// <category>Search</category>
    /// <param type="ASC.Core.Users.EmployeeStatus, ASC.Core.Common" method="url" name="status">User status</param>
    /// <param type="System.String, System" name="query">Search query</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">List of users with the detailed information</returns>
    /// <path>api/2.0/people/status/{status}/search</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("status/{status}/search")]
    public async IAsyncEnumerable<EmployeeFullDto> GetAdvanced(EmployeeStatus status, [FromQuery] string query)
    {
        var list = (await _userManager.GetUsersAsync(status)).ToAsyncEnumerable();

        if ("group".Equals(_apiContext.FilterBy, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(_apiContext.FilterValue))
        {
            var groupId = new Guid(_apiContext.FilterValue);
            //Filter by group
            list = list.WhereAwait(async x => await _userManager.IsUserInGroupAsync(x.Id, groupId));
            _apiContext.SetDataFiltered();
        }

        list = list.Where(x => x.FirstName != null && x.FirstName.IndexOf(query, StringComparison.OrdinalIgnoreCase) > -1 || (x.LastName != null && x.LastName.IndexOf(query, StringComparison.OrdinalIgnoreCase) != -1) ||
                                (x.UserName != null && x.UserName.IndexOf(query, StringComparison.OrdinalIgnoreCase) != -1) || (x.Email != null && x.Email.IndexOf(query, StringComparison.OrdinalIgnoreCase) != -1) || (x.ContactsList != null && x.ContactsList.Exists(y => y.IndexOf(query, StringComparison.OrdinalIgnoreCase) != -1)));

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
    /// <category>Profiles</category>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">List of users with the detailed information</returns>
    /// <path>api/2.0/people</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet]
    public IAsyncEnumerable<EmployeeFullDto> GetAll()
    {
        return GetByStatus(EmployeeStatus.Active);
    }

    /// <summary>
    /// Returns the detailed information about a profile of the user with the email specified in the request.
    /// </summary>
    /// <short>
    /// Get a profile by user email
    /// </short>
    /// <category>Profiles</category>
    /// <param type="System.String, System" method="url" name="email">User email address</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">Detailed profile information</returns>
    /// <path>api/2.0/people/email</path>
    /// <httpMethod>GET</httpMethod>
    [AllowNotPayment]
    [HttpGet("email")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "LinkInvite,Everyone")]
    public async Task<EmployeeFullDto> GetByEmailAsync([FromQuery] string email)
    {
        var user = await _userManager.GetUserByEmailAsync(email);
        if (user.Id == Constants.LostUser.Id)
        {
            throw new ItemNotFoundException("User not found");
        }

        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <summary>
    /// Returns the detailed information about a profile of the user with the name specified in the request.
    /// </summary>
    /// <short>
    /// Get a profile by user name
    /// </short>
    /// <category>Profiles</category>
    /// <param type="System.String, System" method="url" name="username">User name</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">Detailed profile information</returns>
    /// <path>api/2.0/people/{username}</path>
    /// <httpMethod>GET</httpMethod>
    [AllowNotPayment]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "LinkInvite,Everyone")]
    [HttpGet("{username}", Order = 1)]
    public async Task<EmployeeFullDto> GetById(string username)
    {
        var isInvite = _httpContextAccessor.HttpContext!.User.Claims
               .Any(role => role.Type == ClaimTypes.Role && ConfirmTypeExtensions.TryParse(role.Value, out var confirmType) && confirmType == ConfirmType.LinkInvite);

        await _apiContext.AuthByClaimAsync();

        var user = await _userManager.GetUserByUserNameAsync(username);
        if (user.Id == Constants.LostUser.Id)
        {
            if (Guid.TryParse(username, out var userId))
            {
                user = await _userManager.GetUsersAsync(userId);
            }
            else
            {
                logger.ErrorCouldNotGetUserByName(securityContext.CurrentAccount.ID, username);
            }
        }

        if (user.Id == Constants.LostUser.Id)
        {
            throw new ItemNotFoundException("User not found");
        }

        if (isInvite)
        {
            return await employeeFullDtoHelper.GetSimple(user);
        }

        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <summary>
    /// Returns a list of profiles filtered by user status.
    /// </summary>
    /// <short>
    /// Get profiles by status
    /// </short>
    /// <param type="ASC.Core.Users.EmployeeStatus, ASC.Core.Common" method="url" name="status">User status</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">List of users with the detailed information</returns>
    /// <category>User status</category>
    /// <path>api/2.0/people/status/{status}</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("status/{status}")]
    public IAsyncEnumerable<EmployeeFullDto> GetByStatus(EmployeeStatus status)
    {
        Guid? groupId = null;
        if ("group".Equals(_apiContext.FilterBy, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(_apiContext.FilterValue))
        {
            groupId = new Guid(_apiContext.FilterValue);
            _apiContext.SetDataFiltered();
        }

        return GetFullByFilter(status, groupId, null, null, null, null, null, null, null, false, false);
    }

    /// <summary>
    /// Returns a list of users with full information about them matching the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Search users and their information by extended filter
    /// </short>
    /// <category>Search</category>
    /// <param type="System.Nullable{ASC.Core.Users.EmployeeStatus}, System" name="employeeStatus">User status</param>
    /// <param type="System.Nullable{System.Guid}, System" name="groupId">Group ID</param>
    /// <param type="System.Nullable{ASC.Core.Users.EmployeeActivationStatus}, System" name="activationStatus">Activation status</param>
    /// <param type="System.Nullable{ASC.Core.Users.EmployeeType}, System" name="employeeType">User type</param>
    /// <param type="ASC.Core.Users.EmployeeType[], ASC.Core.Common" name="employeeTypes">List of user types</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="isAdministrator">Specifies if the user is an administrator or not</param>
    /// <param type="System.Nullable{ASC.Core.Payments}, System" name="payments">User payment status</param>
    /// <param type="System.Nullable{ASC.Core.AccountLoginType}, System" name="accountLoginType">Account login type</param>
    /// <param type="System.Nullable{ASC.Core.QuotaFilter}, System" name="quotaFilter">Filter by quota (Default - 1, Custom - 2)</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="withoutGroup">Specifies whether the user should be a member of a group or not</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="excludeGroup">Specifies whether or not the user should be a member of the group with the specified ID</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">List of users with the detailed information</returns>
    /// <path>api/2.0/people/filter</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("filter")]
    public async IAsyncEnumerable<EmployeeFullDto> GetFullByFilter(EmployeeStatus? employeeStatus,
        Guid? groupId,
        EmployeeActivationStatus? activationStatus,
        EmployeeType? employeeType,
        [FromQuery] EmployeeType[] employeeTypes,
        bool? isAdministrator,
        Payments? payments,
        AccountLoginType? accountLoginType,
        QuotaFilter? quotaFilter,
        bool? withoutGroup,
        bool? excludeGroup)
    {
        var users = GetByFilterAsync(employeeStatus, groupId, activationStatus, employeeType, employeeTypes, isAdministrator, payments, accountLoginType, quotaFilter, withoutGroup, excludeGroup);

        await foreach (var user in users)
        {
            yield return await employeeFullDtoHelper.GetFullAsync(user);
        }
    }

    /// <summary>
    /// Returns the information about the People module.
    /// </summary>
    /// <short>Get the People information</short>
    /// <category>Module</category>
    /// <returns type="ASC.Api.Core.Module, ASC.Api.Core">Module information</returns>
    /// <path>api/2.0/people/info</path>
    /// <httpMethod>GET</httpMethod>
    /// <visible>false</visible>
    [HttpGet("info")]
    public Module GetModule()
    {
        var product = new PeopleProduct();
        product.Init();

        return new Module(product);
    }

    /// <summary>
    /// Returns a list of users matching the search query. This method uses the query parameters.
    /// </summary>
    /// <short>Search users (using query parameters)</short>
    /// <category>Search</category>
    /// <param type="System.String, System" name="query">Search query</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeDto, ASC.Api.Core">List of users</returns>
    /// <path>api/2.0/people/search</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("search")]
    public IAsyncEnumerable<EmployeeDto> GetPeopleSearch([FromQuery] string query)
    {
        return GetSearch(query);
    }

    /// <summary>
    /// Returns a list of users matching the search query.
    /// </summary>
    /// <short>Search users</short>
    /// <category>Search</category>
    /// <param type="System.String, System" method="url" name="query">Search query</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">List of users with the detailed information</returns>
    /// <path>api/2.0/people/@search/{query}</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("@search/{query}")]
    public async IAsyncEnumerable<EmployeeFullDto> GetSearch(string query)
    {
        var groupId = Guid.Empty;
        if ("group".Equals(_apiContext.FilterBy, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(_apiContext.FilterValue))
        {
            groupId = new Guid(_apiContext.FilterValue);
        }

        var users = await _userManager.SearchAsync(query, EmployeeStatus.Active, groupId);

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
    /// <category>Search</category>
    /// <param type="System.Nullable{ASC.Core.Users.EmployeeStatus}, System" name="employeeStatus">User status</param>
    /// <param type="System.Nullable{System.Guid}, System" name="groupId">Group ID</param>
    /// <param type="System.Nullable{ASC.Core.Users.EmployeeActivationStatus}, System" name="activationStatus">Activation status</param>
    /// <param type="System.Nullable{ASC.Core.Users.EmployeeType}, System" name="employeeType">User type</param>
    /// <param type="ASC.Core.Users.EmployeeType[], ASC.Core.Common" name="employeeTypes">List of user types</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="isAdministrator">Specifies if the user is an administrator or not</param>
    /// <param type="System.Nullable{ASC.Core.Payments}, System" name="payments">User payment status</param>
    /// <param type="System.Nullable{ASC.Core.AccountLoginType}, System" name="accountLoginType">Account login type</param>
    /// <param type="System.Nullable{ASC.Core.QuotaFilter}, System" name="quotaFilter">Filter by quota (Default - 1, Custom - 2)</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="withoutGroup">Specifies whether the user should be a member of a group or not</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="excludeGroup">Specifies whether or not the user should be a member of the group with the specified ID</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeDto, ASC.Api.Core">List of users</returns>
    /// <path>api/2.0/people/simple/filter</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("simple/filter")]
    public async IAsyncEnumerable<EmployeeDto> GetSimpleByFilter(EmployeeStatus? employeeStatus,
        Guid? groupId,
        EmployeeActivationStatus? activationStatus,
        EmployeeType? employeeType,
        [FromQuery] EmployeeType[] employeeTypes,
        bool? isAdministrator,
        Payments? payments,
        AccountLoginType? accountLoginType,
        QuotaFilter? quotaFilter,
        bool? withoutGroup,
        bool? excludeGroup)
    {
        var users = GetByFilterAsync(employeeStatus, groupId, activationStatus, employeeType, employeeTypes, isAdministrator, payments, accountLoginType, quotaFilter, withoutGroup, excludeGroup);

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
    /// <category>Profiles</category>
    /// <param type="ASC.People.ApiModels.RequestDto.UpdateMembersRequestDto, ASC.People" name="inDto">Request parameters for updating portal users</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">List of users with the detailed information</returns>
    /// <path>api/2.0/people/delete</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    [HttpPut("delete", Order = -1)]
    public async IAsyncEnumerable<EmployeeFullDto> RemoveUsers(UpdateMembersRequestDto inDto)
    {
        await _permissionContext.DemandPermissionsAsync(Constants.Action_AddRemoveUser);

        await CheckReassignProcessAsync(inDto.UserIds);

        var users = await inDto.UserIds.ToAsyncEnumerable().SelectAwait(async userId => await _userManager.GetUsersAsync(userId))
            .Where(u => !_userManager.IsSystemUser(u.Id) && !u.IsLDAP()).ToListAsync();

        var userNames = users.Select(x => x.DisplayUserName(false, displayUserSettingsHelper)).ToList();
        var tenant = await tenantManager.GetCurrentTenantAsync();
        
        foreach (var user in users)
        {
            if (user.Status != EmployeeStatus.Terminated)
            {
                continue;
            }

            await _userPhotoManager.RemovePhotoAsync(user.Id);
            await _userManager.DeleteUserAsync(user.Id);
            await fileSecurity.RemoveSubjectAsync(user.Id, true);
            await queueWorkerRemove.StartAsync(tenant.Id, user, securityContext.CurrentAccount.ID, false, false);
        }

        await messageService.SendAsync(MessageAction.UsersDeleted, MessageTarget.Create(users.Select(x => x.Id)), userNames);

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
    /// <category>Profiles</category>
    /// <param type="ASC.People.ApiModels.RequestDto.UpdateMembersRequestDto, ASC.People" name="inDto">Request parameters for updating portal users</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">List of users with the detailed information</returns>
    /// <path>api/2.0/people/invite</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    [AllowNotPayment]
    [HttpPut("invite")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async IAsyncEnumerable<EmployeeFullDto> ResendUserInvitesAsync(UpdateMembersRequestDto inDto)
    {
        List<UserInfo> users;

        if (inDto.ResendAll)
        {
            await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(Guid.Empty, EmployeeType.User), Constants.Action_AddRemoveUser);
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

        foreach (var user in users)
        {
            if (user.IsActive)
            {
                continue;
            }

            var viewer = await _userManager.GetUsersAsync(securityContext.CurrentAccount.ID);

            if (viewer == null)
            {
                throw new Exception(Resource.ErrorAccessDenied);
            }

            if (await _userManager.IsDocSpaceAdminAsync(viewer) || viewer.Id == user.Id)
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

                if (!await _permissionContext.CheckPermissionsAsync(new UserSecurityProvider(type), Constants.Action_AddRemoveUser))
                {
                    continue;
                }

                var link = await commonLinkUtility.GetInvitationLinkAsync(user.Email, type, authContext.CurrentAccount.ID, user.GetCulture()?.Name);
                var shortenLink = await urlShortener.GetShortenLinkAsync(link);
                await messageService.SendAsync(MessageAction.SendJoinInvite, MessageTarget.Create(user.Id), viewer.DisplayUserName(displayUserSettingsHelper), user.Email);
                await studioNotifyService.SendDocSpaceInviteAsync(user.Email, shortenLink);
            }
            else
            {
                if (viewer.Id != user.Id)
                {
                    var type = await _userManager.GetUserTypeAsync(user.Id);

                    if (!await _permissionContext.CheckPermissionsAsync(new UserSecurityProvider(type), Constants.Action_AddRemoveUser))
                    {
                        continue;
                    }
                }

                await studioNotifyService.SendEmailActivationInstructionsAsync(user, user.Email);
            }
        }

        await messageService.SendAsync(MessageAction.UsersSentActivationInstructions, MessageTarget.Create(users.Select(x => x.Id)), users.Select(x => x.DisplayUserName(false, displayUserSettingsHelper)));

        foreach (var user in users)
        {
            yield return await employeeFullDtoHelper.GetFullAsync(user);
        }
    }

    /// <summary>
    /// Returns a theme which is set to the current portal.
    /// </summary>
    /// <short>
    /// Get portal theme
    /// </short>
    /// <category>Theme</category>
    /// <returns type="ASC.Web.Core.Users.DarkThemeSettings, ASC.Web.Core">Theme</returns>
    /// <path>api/2.0/people/theme</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("theme")]
    public async Task<DarkThemeSettings> GetThemeAsync()
    {
        return await settingsManager.LoadForCurrentUserAsync<DarkThemeSettings>();
    }

    /// <summary>
    /// Changes the current portal theme.
    /// </summary>
    /// <short>
    /// Change portal theme
    /// </short>
    /// <category>Theme</category>
    /// <param type="ASC.People.ApiModels.RequestDto.DarkThemeSettingsRequestDto, ASC.People" name="inDto">Theme settings request parameters</param>
    /// <returns type="ASC.Web.Core.Users.DarkThemeSettings, ASC.Web.Core">Theme</returns>
    /// <path>api/2.0/people/theme</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("theme")]
    public async Task<DarkThemeSettings> ChangeThemeAsync(DarkThemeSettingsRequestDto inDto)
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
    /// <category>Profiles</category>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">Detailed information about my profile</returns>
    /// <path>api/2.0/people/@self</path>
    /// <httpMethod>GET</httpMethod>
    [AllowNotPayment]
    [HttpGet("@self")]
    public async Task<EmployeeFullDto> SelfAsync()
    {
        var user = await _userManager.GetUserAsync(securityContext.CurrentAccount.ID, EmployeeFullDtoHelper.GetExpression(_apiContext));

        var result = await employeeFullDtoHelper.GetFullAsync(user);

        result.Theme = (await settingsManager.LoadForCurrentUserAsync<DarkThemeSettings>()).Theme;

        return result;
    }

    /// <summary>
    /// Sends a message to the user email with the instructions to change the email address connected to the portal.
    /// </summary>
    /// <short>
    /// Send instructions to change email
    /// </short>
    /// <category>Profiles</category>
    /// <param type="ASC.People.ApiModels.RequestDto.UpdateMemberRequestDto, ASC.People" name="inDto">Request parameters for updating user information</param>
    /// <returns type="System.Object, System">Message text</returns>
    /// <path>api/2.0/people/email</path>
    /// <httpMethod>POST</httpMethod>
    [AllowNotPayment]
    [HttpPost("email")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task<object> SendEmailChangeInstructionsAsync(UpdateMemberRequestDto inDto)
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

        if (_userManager.IsSystemUser(user.Id))
        {
            throw new Exception(Resource.ErrorUserNotFound);
        }

        if (!viewerIsAdmin && viewer.Id != user.Id)
        {
            throw new Exception(Resource.ErrorAccessDenied);
        }

        var tenant = await tenantManager.GetCurrentTenantAsync();
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
            await messageService.SendAsync(MessageAction.UserSentEmailChangeInstructions, MessageTarget.Create(user.Id), DateTime.UtcNow, user.DisplayUserName(false, displayUserSettingsHelper));
        }

        return string.Format(Resource.MessageEmailChangeInstuctionsSentOnEmail, email);
    }

    /// <summary>
    /// Reminds a password to the user using the email address specified in the request.
    /// </summary>
    /// <short>
    /// Remind a user password
    /// </short>
    /// <category>Password</category>
    /// <param type="ASC.People.ApiModels.RequestDto.MemberRequestDto, ASC.People" name="inDto">Member request parameters</param>
    /// <returns type="System.Object, System">Email with the password</returns>
    /// <path>api/2.0/people/password</path>
    /// <httpMethod>POST</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowNotPayment]
    [AllowAnonymous]
    [HttpPost("password")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task<object> SendUserPasswordAsync(MemberRequestDto inDto)
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
    /// <category>User status</category>
    /// <param type="ASC.Core.Users.EmployeeActivationStatus, ASC.Core.Common" method="url" name="activationstatus">Activation status</param>
    /// <param type="ASC.People.ApiModels.RequestDto.UpdateMembersRequestDto, ASC.People" name="inDto">Request parameters for updating user information</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">List of users with the detailed information</returns>
    /// <path>api/2.0/people/activationstatus/{activationstatus}</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    [AllowNotPayment]
    [HttpPut("activationstatus/{activationstatus}")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "Activation,Everyone")]
    public async IAsyncEnumerable<EmployeeFullDto> UpdateEmployeeActivationStatus(EmployeeActivationStatus activationstatus, UpdateMembersRequestDto inDto)
    {
        await _apiContext.AuthByClaimAsync();

        foreach (var id in inDto.UserIds.Where(userId => !_userManager.IsSystemUser(userId)))
        {
            await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(id), Constants.Action_EditUser);
            var u = await _userManager.GetUsersAsync(id);
            if (u.Id == Constants.LostUser.Id || u.IsLDAP())
            {
                continue;
            }

            u.ActivationStatus = activationstatus;
            await _userManager.UpdateUserInfoAsync(u);

            if (activationstatus == EmployeeActivationStatus.Activated && u.IsOwner(await tenantManager.GetCurrentTenantAsync()))
            {
                var settings = await settingsManager.LoadAsync<FirstEmailConfirmSettings>();

                if (settings.IsFirst)
                {
                    await studioNotifyService.SendAdminWelcomeAsync(u);

                    settings.IsFirst = false;
                    await settingsManager.SaveAsync(settings);
                }
            }

            yield return await employeeFullDtoHelper.GetFullAsync(u);
        }
    }

    /// <summary>
    /// Updates the user language with the parameter specified in the request.
    /// </summary>
    /// <short>
    /// Update user language
    /// </short>
    /// <category>Profiles</category>
    /// <param type="System.String, System" method="url" name="userid">User ID</param>
    /// <param type="ASC.People.ApiModels.RequestDto.UpdateMemberRequestDto, ASC.People" name="inDto">Request parameters for updating user information</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">Detailed user information</returns>
    /// <path>api/2.0/people/{userid}/culture</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{userid}/culture")]
    public async Task<EmployeeFullDto> UpdateMemberCulture(string userid, UpdateMemberRequestDto inDto)
    {
        var user = await GetUserInfoAsync(userid);

        if (_userManager.IsSystemUser(user.Id) || !Equals(user.Id, securityContext.CurrentAccount.ID))
        {
            throw new SecurityException();
        }

        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser);
        await _userManager.ChangeUserCulture(user, inDto.CultureName);
            await messageService.SendAsync(MessageAction.UserUpdatedLanguage, MessageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper));
        
        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <summary>
    /// Updates the data for the selected portal user with the first name, last name, email address, and/or optional parameters specified in the request.
    /// </summary>
    /// <short>
    /// Update a user
    /// </short>
    /// <category>Profiles</category>
    /// <param type="System.String, System" method="url" name="userid">User ID</param>
    /// <param type="ASC.People.ApiModels.RequestDto.UpdateMemberRequestDto, ASC.People" name="inDto">Member request parameters</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">Updated user with the detailed information</returns>
    /// <path>api/2.0/people/{userid}</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{userid}", Order = 1)]
    public async Task<EmployeeFullDto> UpdateMember(string userid, UpdateMemberRequestDto inDto)
    {
        var user = await GetUserInfoAsync(userid);

        if (_userManager.IsSystemUser(user.Id))
        {
            throw new SecurityException();
        }

        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser);

        var changed = false;
        var self = securityContext.CurrentAccount.ID.Equals(user.Id);
        var isDocSpaceAdmin = await _userManager.IsDocSpaceAdminAsync(securityContext.CurrentAccount.ID);
        
        //Update it
        if (self)
        {
            var isLdap = user.IsLDAP();
            var isSso = user.IsSSO();

            if (!isLdap && !isSso)
            {
                //Set common fields

                var firstName = inDto.Firstname ?? user.FirstName;
                var lastName = inDto.Lastname ?? user.LastName;

                if (!userFormatter.IsValidUserName(firstName, lastName))
                {
                    throw new Exception(Resource.ErrorIncorrectUserName);
                }

                user.FirstName = firstName;
                user.LastName = lastName;
                user.Location = inDto.Location ?? user.Location;

                if (isDocSpaceAdmin)
                {
                    user.Title = inDto.Title ?? user.Title;
                }
            }

            user.Notes = inDto.Comment ?? user.Notes;

            user.Sex = inDto.Sex switch
            {
                "male" => true,
                "female" => false,
                _ => user.Sex
            };


            user.BirthDate = inDto.Birthday != null ? tenantUtil.DateTimeFromUtc(inDto.Birthday) : user.BirthDate;

            var resetDate = new DateTime(1900, 01, 01);
            if (user.BirthDate == resetDate)
            {
                user.BirthDate = null;
            }

            user.WorkFromDate = inDto.Worksfrom != null
                ? tenantUtil.DateTimeFromUtc(inDto.Worksfrom)
                : user.WorkFromDate;

            if (user.WorkFromDate == resetDate)
            {
                user.WorkFromDate = null;
            }

            //Update contacts
            await UpdateContactsAsync(inDto.Contacts, user);
            await UpdateDepartmentsAsync(inDto.Department, user);

            if (inDto.Files != await _userPhotoManager.GetPhotoAbsoluteWebPath(user.Id))
            {
                await UpdatePhotoUrlAsync(inDto.Files, user);
            }

            changed = true;
        }
        
        var tenant = await tenantManager.GetCurrentTenantAsync();
        var owner = user.IsOwner(tenant);
        var statusChanged = false;
        
        if ((self || isDocSpaceAdmin && !owner) && inDto.Disable.HasValue)
        {
            user.Status = inDto.Disable.Value ? EmployeeStatus.Terminated : EmployeeStatus.Active;
            user.TerminatedDate = inDto.Disable.Value ? DateTime.UtcNow : null;
            changed = true;
            statusChanged = true;
        }


        // change user type
        var canBeGuestFlag = !owner && 
                             !await _userManager.IsDocSpaceAdminAsync(user) && 
                             (await user.GetListAdminModulesAsync(webItemSecurity, webItemManager)).Count == 0 && 
                             !self;

        if (inDto.IsUser.HasValue)
        {
            var isUser = inDto.IsUser.Value;
            
            if (isUser && canBeGuestFlag && !await _userManager.IsUserAsync(user))
            {
                await using (await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetUsersCountCheckKey(tenant.Id)))
                {
                    await activeUsersChecker.CheckAppend();
                    await _userManager.AddUserIntoGroupAsync(user.Id, Constants.GroupUser.ID);
                    webItemSecurityCache.ClearCache(tenant.Id);
                    changed = true;
                }
            }
            else if (!self && !isUser && await _userManager.IsUserAsync(user))
            {
                await using (await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetPaidUsersCountCheckKey(tenant.Id)))
                {
                    await countPaidUserChecker.CheckAppend();
                    await _userManager.RemoveUserFromGroupAsync(user.Id, Constants.GroupUser.ID);
                    webItemSecurityCache.ClearCache(tenant.Id);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);

            await messageService.SendAsync(MessageAction.UserUpdated, MessageTarget.Create(user.Id),
                user.DisplayUserName(false, displayUserSettingsHelper), user.Id);

            if (statusChanged && inDto.Disable.HasValue && inDto.Disable.Value)
            {
                await cookiesManager.ResetUserCookieAsync(user.Id);
                await messageService.SendAsync(MessageAction.CookieSettingsUpdated);
            }
        }

        return await employeeFullDtoHelper.GetFullAsync(user);
    }

    /// <summary>
    /// Changes a status for the users with the IDs specified in the request.
    /// </summary>
    /// <short>
    /// Change a user status
    /// </short>
    /// <category>User status</category>
    /// <param type="ASC.Core.Users.EmployeeStatus, ASC.Core.Common" method="url" name="status">New user status</param>
    /// <param type="ASC.People.ApiModels.RequestDto.UpdateMembersRequestDto, ASC.People" name="inDto">Request parameters for updating user information</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">List of users with the detailed information</returns>
    /// <path>api/2.0/people/status/{status}</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    [HttpPut("status/{status}")]
    public async IAsyncEnumerable<EmployeeFullDto> UpdateUserStatus(EmployeeStatus status, UpdateMembersRequestDto inDto)
    {
        await _permissionContext.DemandPermissionsAsync(Constants.Action_EditUser);

        var tenant = await tenantManager.GetCurrentTenantAsync();
        var users = await inDto.UserIds.ToAsyncEnumerable().SelectAwait(async userId => await _userManager.GetUsersAsync(userId))
            .Where(u => !_userManager.IsSystemUser(u.Id) && !u.IsLDAP()).ToListAsync();

        foreach (var user in users)
        {
            if (user.IsOwner(tenant) || authContext.CurrentAccount.ID != tenant.OwnerId && await _userManager.IsDocSpaceAdminAsync(user) || user.IsMe(authContext))
            {
                continue;
            }

            switch (status)
            {
                case EmployeeStatus.Active:
                    if (user.Status == EmployeeStatus.Terminated)
                    {
                        IDistributedLockHandle lockHandle = null;
                        
                        try
                        {
                            if (!await _userManager.IsUserAsync(user))
                            {
                                lockHandle = await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetPaidUsersCountCheckKey(tenant.Id));
                                
                                await countPaidUserChecker.CheckAppend();
                            }
                            else
                            {
                                lockHandle = await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetUsersCountCheckKey(tenant.Id));
                                
                                await activeUsersChecker.CheckAppend();
                            }

                            user.Status = EmployeeStatus.Active;

                            await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);
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
                    await messageService.SendAsync(MessageAction.CookieSettingsUpdated);
                    break;
            }
        }

        await messageService.SendAsync(MessageAction.UsersUpdatedStatus, MessageTarget.Create(users.Select(x => x.Id)), users.Select(x => x.DisplayUserName(false, displayUserSettingsHelper)));

        foreach (var user in users)
        {
            yield return await employeeFullDtoHelper.GetFullAsync(user);
        }
    }

    /// <summary>
    /// Changes a type for the users with the IDs specified in the request.
    /// </summary>
    /// <short>
    /// Change a user type
    /// </short>
    /// <category>User type</category>
    /// <param type="ASC.Core.Users.EmployeeType, ASC.Core.Common" method="url" name="type">New user type</param>
    /// <param type="ASC.People.ApiModels.RequestDto.UpdateMembersRequestDto, ASC.People" name="inDto">Request parameters for updating user information</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">List of users with the detailed information</returns>
    /// <path>api/2.0/people/type/{type}</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    [HttpPut("type/{type}")]
    public async IAsyncEnumerable<EmployeeFullDto> UpdateUserTypeAsync(EmployeeType type, UpdateMembersRequestDto inDto)
    {
        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(type), Constants.Action_AddRemoveUser);

        var users = await inDto.UserIds
            .ToAsyncEnumerable()
            .Where(userId => !_userManager.IsSystemUser(userId))
            .SelectAwait(async userId => await _userManager.GetUsersAsync(userId))
            .Where(r => r.Status == EmployeeStatus.Active)
            .ToListAsync();

        foreach (var user in users)
        {
            await userManagerWrapper.UpdateUserTypeAsync(user, type);
        }

        await messageService.SendAsync(MessageAction.UsersUpdatedType, MessageTarget.Create(users.Select(x => x.Id)),
        users.Select(x => x.DisplayUserName(false, displayUserSettingsHelper)), users.Select(x => x.Id).ToList(), type);

        foreach (var user in users)
        {
            yield return await employeeFullDtoHelper.GetFullAsync(user);
        }
    }

    ///<summary>
    /// Starts the process of recalculating quota.
    /// </summary>
    /// <short>
    /// Recalculate quota 
    /// </short>
    /// <category>Quota</category>
    /// <path>api/2.0/people/recalculatequota</path>
    /// <httpMethod>GET</httpMethod>
    /// <returns></returns>
    /// <visible>false</visible>
    [HttpGet("recalculatequota")]
    public async Task RecalculateQuotaAsync()
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        await usersQuotaSyncOperation.RecalculateQuota(await tenantManager.GetCurrentTenantAsync());
    }

    /// <summary>
    /// Checks the process of recalculating quota.
    /// </summary>
    /// <short>
    /// Check quota recalculation
    /// </short>
    /// <category>Quota</category>
    /// <returns type="ASC.Api.Core.Model.TaskProgressDto, ASC.Api.Core.Model">Task progress</returns>
    /// <path>api/2.0/people/checkrecalculatequota</path>
    /// <httpMethod>GET</httpMethod>
    /// <visible>false</visible>
    [HttpGet("checkrecalculatequota")]
    public async Task<TaskProgressDto> CheckRecalculateQuotaAsync()
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        return await usersQuotaSyncOperation.CheckRecalculateQuota(await tenantManager.GetCurrentTenantAsync());
    }

    /// <summary>
    /// Changes a quota limit for the users with the IDs specified in the request.
    /// </summary>
    /// <short>
    /// Change a user quota limit
    /// </short>
    /// <category>Quota</category>
    /// <param type="ASC.People.ApiModels.RequestDto.UpdateMembersRequestDto, ASC.People" name="inDto">Request parameters for updating user information</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">List of users with the detailed information</returns>
    /// <path>api/2.0/people/userquota</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    [HttpPut("userquota")]
    public async IAsyncEnumerable<EmployeeFullDto> UpdateUserQuotaAsync(UpdateMembersQuotaRequestDto inDto)
    {
        if (!inDto.Quota.TryGetInt64(out var quota))
        {
            throw new Exception(Resource.QuotaGreaterPortalError);
        }

        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var users = await inDto.UserIds.ToAsyncEnumerable()
            .Where(userId => !_userManager.IsSystemUser(userId))
            .SelectAwait(async userId => await _userManager.GetUsersAsync(userId))
            .ToListAsync();

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

        foreach (var user in users)
        {
            await settingsManager.SaveAsync(new UserQuotaSettings { UserQuota = quota }, user);

            var userUsedSpace = Math.Max(0, (await quotaService.FindUserQuotaRowsAsync(tenant.Id, user.Id)).Where(r => !string.IsNullOrEmpty(r.Tag) && !string.Equals(r.Tag, Guid.Empty.ToString())).Sum(r => r.Counter));
            var quotaUserSettings = await settingsManager.LoadAsync<TenantUserQuotaSettings>();
            _ = quotaSocketManager.ChangeCustomQuotaUsedValueAsync(tenant.Id, customQuota.GetFeature<UserCustomQuotaFeature>().Name, quotaUserSettings.EnableQuota, userUsedSpace, quota, [user.Id]);

            yield return await employeeFullDtoHelper.GetFullAsync(user);
        }
    }

    /// <summary>
    /// Resets a user quota limit with the ID specified in the request from the portal.
    /// </summary>
    /// <short>
    /// Reset a user quota limit
    /// </short>
    /// <category>Quota</category>
    /// <param type="ASC.People.ApiModels.RequestDto.UpdateMembersQuotaRequestDto, ASC.People" name="inDto">Request parameters for updating user information</param>
    /// <returns type="ASC.Web.Api.Models.EmployeeFullDto, ASC.Api.Core">User detailed information</returns>
    /// <path>api/2.0/people/resetquota</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    [HttpPut("resetquota")]
    public async IAsyncEnumerable<EmployeeFullDto> ResetUsersQuota(UpdateMembersQuotaRequestDto inDto)
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        if (!coreBaseSettings.Standalone
            && !(await tenantManager.GetCurrentTenantQuotaAsync()).Statistic)
        {
            throw new BillingException(Resource.ErrorNotAllowedOption, "Statistic");
        }

        var users = await inDto.UserIds.ToAsyncEnumerable()
            .Where(userId => !_userManager.IsSystemUser(userId))
            .SelectAwait(async userId => await _userManager.GetUsersAsync(userId))
            .ToListAsync();

        var tenant = await tenantManager.GetCurrentTenantAsync();
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
            var quotaUserSettings = await settingsManager.LoadAsync<TenantUserQuotaSettings>();
            var userQuotaLimit = userQuotaData.UserQuota == userQuotaData.GetDefault().UserQuota ? quotaUserSettings.DefaultQuota : userQuotaData.UserQuota;
            _ = quotaSocketManager.ChangeCustomQuotaUsedValueAsync(tenant.Id, customQuota.GetFeature<UserCustomQuotaFeature>().Name, quotaUserSettings.EnableQuota, userUsedSpace, userQuotaLimit, [user.Id]);

            yield return await employeeFullDtoHelper.GetFullAsync(user);
        }

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
        var tenant = await tenantManager.GetCurrentTenantAsync();
        
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

    private async IAsyncEnumerable<UserInfo> GetByFilterAsync(
        EmployeeStatus? employeeStatus,
        Guid? groupId,
        EmployeeActivationStatus? activationStatus,
        EmployeeType? employeeType,
        IEnumerable<EmployeeType> employeeTypes,
        bool? isDocSpaceAdministrator,
        Payments? payments,
        AccountLoginType? accountLoginType,
        QuotaFilter? quotaFilter,
        bool? withoutGroup,
        bool? excludeGroup)
    {
        var isDocSpaceAdmin = (await _userManager.IsDocSpaceAdminAsync(securityContext.CurrentAccount.ID)) ||
                      await webItemSecurity.IsProductAdministratorAsync(WebItemManager.PeopleProductID, securityContext.CurrentAccount.ID);

        var excludeGroups = new List<Guid>();
        var includeGroups = new List<List<Guid>>();
        var combinedGroups = new List<Tuple<List<List<Guid>>, List<Guid>>>();

        if (groupId.HasValue && (!withoutGroup.HasValue || !withoutGroup.Value))
        {
            if (excludeGroup.HasValue && excludeGroup.Value)
            {
                excludeGroups.Add(groupId.Value);
            }
            else
            {
            includeGroups.Add([groupId.Value]);
        }
        }

        if (employeeType.HasValue)
        {
            FilterByUserType(employeeType.Value, includeGroups, excludeGroups);
        }
        else if (employeeTypes != null && employeeTypes.Any())
        {
            foreach (var et in employeeTypes)
            {
                var combinedIncludeGroups = new List<List<Guid>>();
                var combinedExcludeGroups = new List<Guid>();
                FilterByUserType(et, combinedIncludeGroups, combinedExcludeGroups);
                combinedGroups.Add(new Tuple<List<List<Guid>>, List<Guid>>(combinedIncludeGroups, combinedExcludeGroups));
            }
        }

        if (payments != null)
        {
            switch (payments)
            {
                case Payments.Paid:
                    excludeGroups.Add(Constants.GroupUser.ID);
                    break;
                case Payments.Free:
                    includeGroups.Add([Constants.GroupUser.ID]);
                    break;
            }
        }

        if (isDocSpaceAdministrator.HasValue && isDocSpaceAdministrator.Value)
        {
            var adminGroups = new List<Guid>
            {
                    Constants.GroupAdmin.ID
            };
            var products = webItemManager.GetItemsAll().Where(i => i is IProduct || i.ID == WebItemManager.MailProductID);
            adminGroups.AddRange(products.Select(r => r.ID));

            includeGroups.Add(adminGroups);
        }

        var totalCountTask = _userManager.GetUsersCountAsync(isDocSpaceAdmin, employeeStatus, includeGroups, excludeGroups, combinedGroups, activationStatus, accountLoginType, quotaFilter,
            _apiContext.FilterValue, withoutGroup ?? false);

        var users = _userManager.GetUsers(isDocSpaceAdmin, employeeStatus, includeGroups, excludeGroups, combinedGroups, activationStatus, accountLoginType, quotaFilter,
            _apiContext.FilterValue, withoutGroup ?? false, _apiContext.SortBy, !_apiContext.SortDescending, _apiContext.Count, _apiContext.StartIndex);

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
                case EmployeeType.DocSpaceAdmin:
                    iGroups.Add([Constants.GroupAdmin.ID]);
                    break;
                case EmployeeType.RoomAdmin:
                    eGroups.Add(Constants.GroupUser.ID);
                    eGroups.Add(Constants.GroupAdmin.ID);
                    eGroups.Add(Constants.GroupCollaborator.ID);
                    break;
                case EmployeeType.Collaborator:
                    iGroups.Add([Constants.GroupCollaborator.ID]);
                    break;
                case EmployeeType.User:
                    iGroups.Add([Constants.GroupUser.ID]);
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
    ///// <category>Profiles</category>
    ///// <param type="System.String, System" name="userList">List of users</param>
    ///// <param type="System.Boolean, System" name="importUsersAsCollaborators" optional="true">Specifies whether to import users as guests or not</param>
    ///// <returns></returns>
    ///// <path>api/2.0/people/import/save</path>
    ///// <httpMethod>POST</httpMethod>
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
public class UserControllerAdditionalInternal(EmployeeFullDtoHelper employeeFullDtoHelper,
        FileSecurity fileSecurity, 
        ApiContext apiContext, 
        IDaoFactory daoFactory) 
    : UserControllerAdditional<int>(employeeFullDtoHelper, fileSecurity, apiContext, daoFactory);
        
public class UserControllerAdditionalThirdParty(EmployeeFullDtoHelper employeeFullDtoHelper,
        FileSecurity fileSecurity, 
        ApiContext apiContext, 
        IDaoFactory daoFactory) 
    : UserControllerAdditional<string>(employeeFullDtoHelper, fileSecurity, apiContext, daoFactory);
        
public class UserControllerAdditional<T>(EmployeeFullDtoHelper employeeFullDtoHelper,
        FileSecurity fileSecurity, 
        ApiContext apiContext, 
        IDaoFactory daoFactory) : ApiControllerBase
    {
    [HttpGet("room/{id}")]
    public async IAsyncEnumerable<EmployeeFullDto> GetUsersWithRoomSharedAsync(T id, EmployeeStatus? employeeStatus, EmployeeActivationStatus? activationStatus, bool? excludeShared)
    {
        var room = (await daoFactory.GetFolderDao<T>().GetFolderAsync(id)).NotFoundIfNull();

        if (!await fileSecurity.CanEditAccessAsync(room))
        {
            throw new SecurityException();
        }
        
        var offset = Convert.ToInt32(apiContext.StartIndex);
        var count = Convert.ToInt32(apiContext.Count);

        var securityDao = daoFactory.GetSecurityDao<T>();

        var totalUsers = await securityDao.GetUsersWithSharedCountAsync(room, apiContext.FilterValue, employeeStatus, activationStatus, excludeShared ?? false);

        apiContext.SetCount(Math.Min(Math.Max(totalUsers - offset, 0), count)).SetTotalCount(totalUsers);

        await foreach (var u in securityDao.GetUsersWithSharedAsync(room, apiContext.FilterValue, employeeStatus, activationStatus, excludeShared ?? false, offset, 
                           count))
        {
            yield return await employeeFullDtoHelper.GetFullAsync(u.UserInfo, u.Shared);
        }
    }
}