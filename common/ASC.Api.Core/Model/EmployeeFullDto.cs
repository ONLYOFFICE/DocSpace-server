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

using ASC.Web.Studio.Core.TFA;

namespace ASC.Web.Api.Models;

/// <summary>
/// The full list of user parameters.
/// </summary>
public class EmployeeFullDto : EmployeeDto
{
    /// <summary>
    /// The user first name.
    /// </summary>
    [SwaggerSchemaCustom(Example = "Mike")]
    public string FirstName { get; set; }

    /// <summary>
    /// The user last name.
    /// </summary>
    [SwaggerSchemaCustom(Example = "Zanyatski")]
    public string LastName { get; set; }

    /// <summary>
    /// The user username.
    /// </summary>
    [SwaggerSchemaCustom(Example = "Mike.Zanyatski")]
    public string UserName { get; set; }

    /// <summary>
    /// The user email.
    /// </summary>
    [SwaggerSchemaCustom(Example = "my@gmail.com")]
    [EmailAddress]
    public string Email { get; set; }

    /// <summary>
    /// The list of user contacts.
    /// </summary>
    public List<Contact> Contacts { get; set; }

    /// <summary>
    /// The user birthday.
    /// </summary>
    public ApiDateTime Birthday { get; set; }

    /// <summary>
    /// The user sex.
    /// </summary>
    [SwaggerSchemaCustom(Example = "male")]
    public string Sex { get; set; }

    /// <summary>
    /// The user status.
    /// </summary>
    public EmployeeStatus Status { get; set; }

    /// <summary>
    /// The user activation status.
    /// </summary>
    public EmployeeActivationStatus ActivationStatus { get; set; }

    /// <summary>
    /// The date when the user account was terminated.
    /// </summary>
    public ApiDateTime Terminated { get; set; }

    /// <summary>
    /// The user department.
    /// </summary>
    [SwaggerSchemaCustom(Example = "Marketing")]
    public string Department { get; set; }

    /// <summary>
    /// The user registration date.
    /// </summary>
    public ApiDateTime WorkFrom { get; set; }

    /// <summary>
    /// The list of user groups.
    /// </summary>
    public List<GroupSummaryDto> Groups { get; set; }

    /// <summary>
    /// The user location.
    /// </summary>
    [SwaggerSchemaCustom(Example = "Palo Alto")]
    public string Location { get; set; }

    /// <summary>
    /// The user notes.
    /// </summary>
    [SwaggerSchemaCustom(Example = "Notes to worker")]
    public string Notes { get; set; }

    /// <summary>
    /// Specifies if the user is an administrator or not.
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool IsAdmin { get; set; }

    /// <summary>
    /// Specifies if the user is a room administrator or not.
    /// </summary>
    public bool IsRoomAdmin { get; set; }

    /// <summary>
    /// Specifies if the LDAP settings are enabled for the user or not.
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool IsLDAP { get; set; }

    /// <summary>
    /// The list of the administrator modules.
    /// </summary>
    [SwaggerSchemaCustom(Example = "[\"projects\", \"crm\"]")]
    public List<string> ListAdminModules { get; set; }

    /// <summary>
    /// Specifies if the user is a portal owner or not.
    /// </summary>
    public bool IsOwner { get; set; }

    /// <summary>
    /// Specifies if the user is a portal visitor or not.
    /// </summary>
    public bool IsVisitor { get; set; }

    /// <summary>
    /// Specifies if the user is a portal collaborator or not.
    /// </summary>
    public bool IsCollaborator { get; set; }

    /// <summary>
    /// The user culture code.
    /// </summary>
    [SwaggerSchemaCustom(Example = "en-EN")]
    public string CultureName { get; set; }

    /// <summary>
    /// The user mobile phone number.
    /// </summary>
    public string MobilePhone { get; set; }

    /// <summary>
    /// The mobile phone activation status.
    /// </summary>
    public MobilePhoneActivationStatus MobilePhoneActivationStatus { get; set; }

    /// <summary>
    /// Specifies if the SSO settings are enabled for the user or not.
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool IsSSO { get; set; }

    /// <summary>
    /// The user theme settings.
    /// </summary>
    public DarkThemeSettingsType? Theme { get; set; }

    /// <summary>
    /// The user quota limit.
    /// </summary>
    public long? QuotaLimit { get; set; }

    /// <summary>
    /// The portal used space of the user.
    /// </summary>
    [SwaggerSchemaCustom(Example = 12345)]
    public double? UsedSpace { get; set; }

    /// <summary>
    /// Specifies if the user has access rights.
    /// </summary>
    public bool? Shared { get; set; }

    /// <summary>
    /// Specifies if the user has a custom quota or not.
    /// </summary>
    public bool? IsCustomQuota { get; set; }

    /// <summary>
    /// The current login event ID.
    /// </summary>
    public int? LoginEventId { get; set; }

    /// <summary>
    /// The user who created the current user.
    /// </summary>
    public EmployeeDto CreatedBy { get; set; }

    /// <summary>
    /// The user registration date.
    /// </summary>
    public ApiDateTime RegistrationDate { get; set; }

    /// <summary>
    /// Specifies if the user has a personal folder or not.
    /// </summary>
    public bool HasPersonalFolder { get; set; }

    /// <summary>
    /// Indicates whether the user has enabled two-factor authentication (TFA) using an authentication app.
    /// </summary>
    public bool? TfaAppEnabled { get; set; }
}
[Scope]
public class EmployeeFullDtoHelper(
        ApiContext httpContext,
        UserManager userManager,
        AuthContext authContext,
        UserPhotoManager userPhotoManager,
        WebItemSecurity webItemSecurity,
        CommonLinkUtility commonLinkUtility,
        DisplayUserSettingsHelper displayUserSettingsHelper,
        ApiDateTimeHelper apiDateTimeHelper,
        WebItemManager webItemManager,
        SettingsManager settingsManager,
        IQuotaService quotaService,
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        GroupSummaryDtoHelper groupSummaryDtoHelper,
        TfaAppAuthSettingsHelper tfaAppAuthSettingsHelper,
        ILogger<EmployeeDtoHelper> logger)
    : EmployeeDtoHelper(httpContext, displayUserSettingsHelper, userPhotoManager, commonLinkUtility, userManager, authContext, logger)
{
    // public static Expression<Func<User, UserInfo>> GetExpression(ApiContext apiContext)
    // {
    //     if (apiContext?.Fields == null)
    //     {
    //         return null;
    //     }
    //
    //     var newExpr = Expression.New(typeof(UserInfo));
    //
    //     //i => new UserInfo { ID = i.id } 
    //     var parameter = Expression.Parameter(typeof(User), "i");
    //     var bindExprs = new List<MemberAssignment>();
    //
    //     //foreach (var field in apiContext.Fields)
    //     //{
    //     //    var userInfoProp = typeof(UserInfo).GetProperty(field);
    //     //    var userProp = typeof(User).GetProperty(field);
    //     //    if (userInfoProp != null && userProp != null)
    //     //    {
    //     //        bindExprs.Add(Expression.Bind(userInfoProp, Expression.Property(parameter, userProp)));
    //     //    }
    //     //}
    //
    //     if (apiContext.Check("Id"))
    //     {
    //         bindExprs.Add(Expression.Bind(typeof(UserInfo).GetProperty("Id"),
    //             Expression.Property(parameter, typeof(User).GetProperty("Id"))));
    //     }
    //
    //     var body = Expression.MemberInit(newExpr, bindExprs);
    //     var lambda = Expression.Lambda<Func<User, UserInfo>>(body, parameter);
    //
    //     return lambda;
    // }
    
    public async Task<EmployeeFullDto> GetSimple(UserInfo userInfo, bool withGroups = true)
    {
        var result = new EmployeeFullDto
        {
            FirstName = userInfo.FirstName,
            LastName = userInfo.LastName,
            DisplayName = _displayUserSettingsHelper.GetFullUserName(userInfo)
        };

        if (withGroups)
        {
            await FillGroupsAsync(result, userInfo);
        }

        var photoData = await _userPhotoManager.GetUserPhotoData(userInfo.Id, UserPhotoManager.BigFotoSize);

        if (photoData != null)
        {
            result.Avatar = "data:image/png;base64," + Convert.ToBase64String(photoData);
        }

        result.HasAvatar = await _userPhotoManager.UserHasAvatar(userInfo.Id);

        return result;
    }

    public async Task<EmployeeFullDto> GetSimpleWithEmail(UserInfo userInfo)
    {
        var result = await GetSimple(userInfo);
        result.Email = userInfo.Email;
        return result;
    }

    public async Task<EmployeeFullDto> GetFullAsync(UserInfo userInfo, bool? shared = null)
    {
        var currentType = await _userManager.GetUserTypeAsync(userInfo.Id);
        var tenant = tenantManager.GetCurrentTenant();

        var result = new EmployeeFullDto
        {
            UserName = userInfo.UserName,
            FirstName = userInfo.FirstName,
            LastName = userInfo.LastName,
            Birthday = apiDateTimeHelper.Get(userInfo.BirthDate),
            Status = userInfo.Status,
            ActivationStatus = userInfo.ActivationStatus & ~EmployeeActivationStatus.AutoGenerated,
            Terminated = apiDateTimeHelper.Get(userInfo.TerminatedDate),
            WorkFrom = apiDateTimeHelper.Get(userInfo.WorkFromDate),
            Email = userInfo.Email,
            IsVisitor = await _userManager.IsGuestAsync(userInfo),
            IsAdmin = currentType is EmployeeType.DocSpaceAdmin,
            IsRoomAdmin = currentType is EmployeeType.RoomAdmin,
            IsOwner = userInfo.IsOwner(tenant),
            IsCollaborator = currentType is EmployeeType.User,
            IsLDAP = userInfo.IsLDAP(),
            IsSSO = userInfo.IsSSO(),
            Shared = shared
        };

        await InitAsync(result, userInfo);

        var isDocSpaceAdmin = await _userManager.IsDocSpaceAdminAsync(_authContext.CurrentAccount.ID);

        if ((coreBaseSettings.Standalone || (await tenantManager.GetCurrentTenantQuotaAsync()).Statistic) && (isDocSpaceAdmin || userInfo.Id == _authContext.CurrentAccount.ID))
        {
            var quotaSettings = await settingsManager.LoadAsync<TenantUserQuotaSettings>();
            result.UsedSpace = Math.Max(0, (await quotaService.FindUserQuotaRowsAsync(tenant.Id, userInfo.Id)).Where(r => !string.IsNullOrEmpty(r.Tag) && !string.Equals(r.Tag, Guid.Empty.ToString())).Sum(r => r.Counter));
            if (quotaSettings.EnableQuota)
            {
                var userQuotaSettings = await settingsManager.LoadAsync<UserQuotaSettings>(userInfo);

                result.IsCustomQuota = userQuotaSettings != null && userQuotaSettings.UserQuota != userQuotaSettings.GetDefault().UserQuota;

                result.QuotaLimit = userQuotaSettings != null ?
                                    userQuotaSettings.UserQuota != userQuotaSettings.GetDefault().UserQuota ? userQuotaSettings.UserQuota : quotaSettings.DefaultQuota
                                    : quotaSettings.DefaultQuota;
            }
        }

        if (userInfo.Sex.HasValue)
        {
            result.Sex = userInfo.Sex.Value ? "male" : "female";
        }

        if (!string.IsNullOrEmpty(userInfo.Location))
        {
            result.Location = userInfo.Location;
        }

        if (!string.IsNullOrEmpty(userInfo.Notes))
        {
            result.Notes = userInfo.Notes;
        }

        if (!string.IsNullOrEmpty(userInfo.MobilePhone))
        {
            result.MobilePhone = userInfo.MobilePhone;
        }

        result.MobilePhoneActivationStatus = userInfo.MobilePhoneActivationStatus;

        if (!string.IsNullOrEmpty(userInfo.CultureName))
        {
            result.CultureName = coreBaseSettings.GetRightCultureName(userInfo.GetCulture());
        }

        FillConacts(result, userInfo);
        await FillGroupsAsync(result, userInfo);

        var cacheKey = Math.Abs(userInfo.LastModified.GetHashCode());
        
        result.AvatarOriginal = await _userPhotoManager.GetPhotoAbsoluteWebPath(userInfo.Id) + $"?hash={cacheKey}";
        result.AvatarMax = await _userPhotoManager.GetMaxPhotoURL(userInfo.Id) + $"?hash={cacheKey}";
        result.AvatarMedium = await _userPhotoManager.GetMediumPhotoURL(userInfo.Id) + $"?hash={cacheKey}";
        result.Avatar = await _userPhotoManager.GetBigPhotoURL(userInfo.Id) + $"?hash={cacheKey}";
        var listAdminModules = await userInfo.GetListAdminModulesAsync(webItemSecurity, webItemManager);
        if (listAdminModules.Count > 0)
        {
            result.ListAdminModules = listAdminModules;
        }
        

        if (!isDocSpaceAdmin)
        {
            return result;
        }

        if (userInfo.CreatedBy.HasValue)
        {
            result.CreatedBy = await GetAsync(await _userManager.GetUsersAsync(userInfo.CreatedBy.Value));
        }
            
        result.RegistrationDate = apiDateTimeHelper.Get(userInfo.CreateDate);

        if (await tfaAppAuthSettingsHelper.GetEnable())
        {
            result.TfaAppEnabled = await TfaAppUserSettings.EnableForUserAsync(settingsManager, userInfo.Id);
        }

        return result;
    }

    private async Task FillGroupsAsync(EmployeeFullDto result, UserInfo userInfo)
    {
        var groupsFromDb = (await _userManager.GetUserGroupsAsync(userInfo.Id));
        List<GroupSummaryDto> groups = [];

        foreach (var g in groupsFromDb)
        {
            groups.Add(await groupSummaryDtoHelper.GetAsync(g));
        }
        

        if (groups.Count > 0)
        {
            result.Groups = groups;
            result.Department = string.Join(", ", result.Groups.Select(d => d.Name.HtmlEncode()));
        }
        else
        {
            result.Department = "";
        }
    }

    private void FillConacts(EmployeeFullDto employeeWraperFull, UserInfo userInfo)
    {
        if (userInfo.ContactsList == null)
        {
            return;
        }

        var contacts = new List<Contact>();

        for (var i = 0; i < userInfo.ContactsList.Count; i += 2)
        {
            if (i + 1 < userInfo.ContactsList.Count)
            {
                contacts.Add(new Contact(userInfo.ContactsList[i], userInfo.ContactsList[i + 1]));
            }
        }

        if (contacts.Count > 0)
        {
            employeeWraperFull.Contacts = contacts;
        }
    }
}