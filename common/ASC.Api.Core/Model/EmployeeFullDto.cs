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

namespace ASC.Web.Api.Models;

public class EmployeeFullDto : EmployeeDto
{
    [SwaggerSchemaCustom (Example = "some text", Description = "First name")]
    public string FirstName { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Last name")]
    public string LastName { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Username")]
    public string UserName { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Email")]
    public string Email { get; set; }

    [SwaggerSchemaCustom(Description = "List of contacts")]
    public List<Contact> Contacts { get; set; }

    [SwaggerSchemaCustom(Example = "2008-04-10T06-30-00.000Z", Description = "Birthday")]
    public ApiDateTime Birthday { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Sex")]
    public string Sex { get; set; }

    [SwaggerSchemaCustom(Example = "Active", Description = "Employee status")]
    public EmployeeStatus Status { get; set; }

    [SwaggerSchemaCustom(Example = "Activated", Description = "Employee activation status")]
    public EmployeeActivationStatus ActivationStatus { get; set; }

    [SwaggerSchemaCustom(Example = "2008-04-10T06-30-00.000Z", Description = "The date when the user account was terminated")]
    public ApiDateTime Terminated { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Department")]
    public string Department { get; set; }

    [SwaggerSchemaCustom(Example = "2008-04-10T06-30-00.000Z", Description = "Registration date")]
    public ApiDateTime WorkFrom { get; set; }

    [SwaggerSchemaCustom(Description = "List of groups")]
    public List<GroupSummaryDto> Groups { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Location")]
    public string Location { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Notes")]
    public string Notes { get; set; }

    [SwaggerSchemaCustom(Example = "false", Description = "Specifies if the user is an administrator or not")]
    public bool IsAdmin { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Specifies if the user is a room administrator or not")]
    public bool IsRoomAdmin { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Specifies if the LDAP settings are enabled for the user or not")]
    public bool IsLDAP { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "List of administrator modules")]
    public List<string> ListAdminModules { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Specifies if the user is a portal owner or not")]
    public bool IsOwner { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Specifies if the user is a portal visitor or not")]
    public bool IsVisitor { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Specifies if the user is a portal collaborator or not")]
    public bool IsCollaborator { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Language")]
    public string CultureName { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Mobile phone number")]
    public string MobilePhone { get; set; }

    [SwaggerSchemaCustom(Example = "NotActivated", Description = "ile phone activation status")]
    public MobilePhoneActivationStatus MobilePhoneActivationStatus { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Specifies if the SSO settings are enabled for the user or not")]
    public bool IsSSO { get; set; }

    [SwaggerSchemaCustom(Example = "Base", Description = "Theme", Nullable = true)]
    public DarkThemeSettingsType? Theme { get; set; }

    [SwaggerSchemaCustom(Example = "1234", Description = "Quota limit", Nullable = true, Format = "int64")]
    public long? QuotaLimit { get; set; }

    [SwaggerSchemaCustom(Example = "12345", Description = "Portal used space", Nullable = true)]
    public double? UsedSpace { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Shared", Nullable = true)]
    public bool? Shared { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Specifies if the user has a custom quota or not", Nullable = true)]
    public bool? IsCustomQuota { get; set; }

    public static new EmployeeFullDto GetSample()
    {
        return new EmployeeFullDto
        {
            Avatar = "url to big avatar",
            AvatarSmall = "url to small avatar",
            AvatarMax = "url to max avatar",
            AvatarOriginal = "url to original avatar",
            Contacts = [Contact.GetSample()],
            Email = "my@gmail.com",
            FirstName = "Mike",
            Id = Guid.Empty,
            IsAdmin = false,
            ListAdminModules = ["projects", "crm"],
            UserName = "Mike.Zanyatski",
            LastName = "Zanyatski",
            Title = "Manager",
            Groups = [GroupSummaryDto.GetSample()],
            AvatarMedium = "url to medium avatar",
            Birthday = ApiDateTime.GetSample(),
            Department = "Marketing",
            Location = "Palo Alto",
            Notes = "Notes to worker",
            Sex = "male",
            Status = EmployeeStatus.Active,
            WorkFrom = ApiDateTime.GetSample(),
            Terminated = ApiDateTime.GetSample(),
            CultureName = "en-EN",
            IsLDAP = false,
            IsSSO = false
        };
    }
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
        ILogger<EmployeeDtoHelper> logger)
    : EmployeeDtoHelper(httpContext, displayUserSettingsHelper, userPhotoManager, commonLinkUtility, userManager, authContext, logger)
{
    public static Expression<Func<User, UserInfo>> GetExpression(ApiContext apiContext)
    {
        if (apiContext?.Fields == null)
        {
            return null;
        }

        var newExpr = Expression.New(typeof(UserInfo));

        //i => new UserInfo { ID = i.id } 
        var parameter = Expression.Parameter(typeof(User), "i");
        var bindExprs = new List<MemberAssignment>();

        //foreach (var field in apiContext.Fields)
        //{
        //    var userInfoProp = typeof(UserInfo).GetProperty(field);
        //    var userProp = typeof(User).GetProperty(field);
        //    if (userInfoProp != null && userProp != null)
        //    {
        //        bindExprs.Add(Expression.Bind(userInfoProp, Expression.Property(parameter, userProp)));
        //    }
        //}

        if (apiContext.Check("Id"))
        {
            bindExprs.Add(Expression.Bind(typeof(UserInfo).GetProperty("Id"),
                Expression.Property(parameter, typeof(User).GetProperty("Id"))));
        }

        var body = Expression.MemberInit(newExpr, bindExprs);
        var lambda = Expression.Lambda<Func<User, UserInfo>>(body, parameter);

        return lambda;
    }
    
    public async Task<EmployeeFullDto> GetSimple(UserInfo userInfo)
    {
        var result = new EmployeeFullDto
        {
            FirstName = userInfo.FirstName,
            LastName = userInfo.LastName
        };

        await FillGroupsAsync(result, userInfo);

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
        result.DisplayName = _displayUserSettingsHelper.GetFullUserName(userInfo);
        result.Email = userInfo.Email;
        return result;
    }

    public async Task<EmployeeFullDto> GetFullAsync(UserInfo userInfo, bool? shared = null)
    {
        var currentType = await _userManager.GetUserTypeAsync(userInfo.Id);
        var tenant = await tenantManager.GetCurrentTenantAsync();

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
            IsVisitor = await _userManager.IsUserAsync(userInfo),
            IsAdmin = currentType is EmployeeType.DocSpaceAdmin,
            IsRoomAdmin = currentType is EmployeeType.RoomAdmin,
            IsOwner = userInfo.IsOwner(tenant),
            IsCollaborator = currentType is EmployeeType.Collaborator,
            IsLDAP = userInfo.IsLDAP(),
            IsSSO = userInfo.IsSSO(),
            Shared = shared
        };

        await InitAsync(result, userInfo);

        if ((coreBaseSettings.Standalone || (await tenantManager.GetCurrentTenantQuotaAsync()).Statistic) && (await _userManager.IsDocSpaceAdminAsync(_authContext.CurrentAccount.ID) || userInfo.Id == _authContext.CurrentAccount.ID))
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

        if (_httpContext.Check("avatarOriginal"))
        {
            result.AvatarOriginal = await _userPhotoManager.GetPhotoAbsoluteWebPath(userInfo.Id) + $"?hash={cacheKey}";
        }

        if (_httpContext.Check("avatarMax"))
        {
            result.AvatarMax = await _userPhotoManager.GetMaxPhotoURL(userInfo.Id) + $"?hash={cacheKey}";
        }

        if (_httpContext.Check("avatarMedium"))
        {
            result.AvatarMedium = await _userPhotoManager.GetMediumPhotoURL(userInfo.Id) + $"?hash={cacheKey}";
        }

        if (_httpContext.Check("avatar"))
        {
            result.Avatar = await _userPhotoManager.GetBigPhotoURL(userInfo.Id) + $"?hash={cacheKey}";
        }

        if (_httpContext.Check("listAdminModules"))
        {
            var listAdminModules = await userInfo.GetListAdminModulesAsync(webItemSecurity, webItemManager);
            if (listAdminModules.Count > 0)
            {
                result.ListAdminModules = listAdminModules;
            }
        }

        return result;
    }

    private async Task FillGroupsAsync(EmployeeFullDto result, UserInfo userInfo)
    {
        if (!_httpContext.Check("groups") && !_httpContext.Check("department"))
        {
            return;
        }

        var groupsFromDb = (await _userManager.GetUserGroupsAsync(userInfo.Id));
        List<GroupSummaryDto> groups = new();

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