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

using ASC.Web.Studio.Core.TFA;

namespace ASC.Web.Api.Models;

/// <summary>
/// The full list of user parameters.
/// </summary>
public class EmployeeFullDto : EmployeeDto, IAccountEntryDto
{
    /// <summary>
    /// The user first name.
    /// </summary>
    /// <example>Mike</example>
    public string FirstName { get; set; }

    /// <summary>
    /// The user last name.
    /// </summary>
    /// <example>Zanyatski</example>
    public string LastName { get; set; }

    /// <summary>
    /// The user username.
    /// </summary>
    /// <example>Mike.Zanyatski</example>
    public string UserName { get; set; }

    /// <summary>
    /// The user email.
    /// </summary>
    /// <example>my@gmail.com</example>
    [EmailAddress]
    public string Email { get; set; }

    /// <summary>
    /// The list of user contacts.
    /// </summary>
    /// <example>[{"type": "email", "value": "user@example.com"}]</example>
    public List<Contact> Contacts { get; set; }

    // /// <summary>
    // /// The user birthday.
    // /// </summary>
    // /// <example>1990-06-07T00:00:00.0000000Z</example>
    // public ApiDateTime Birthday { get; set; }

    // /// <summary>
    // /// The user sex.
    // /// </summary>
    // /// <example>male</example>
    // public string Sex { get; set; }

    /// <summary>
    /// The user status.
    /// </summary>
    /// <example>Active</example>
    public EmployeeStatus Status { get; set; }

    /// <summary>
    /// The user activation status.
    /// </summary>
    /// <example>Activated</example>
    public EmployeeActivationStatus ActivationStatus { get; set; }

    /// <summary>
    /// The date when the user account was terminated.
    /// </summary>
    /// <example>2025-06-01T00:00:00.0000000Z</example>
    public ApiDateTime Terminated { get; set; }

    /// <summary>
    /// The user department.
    /// </summary>
    /// <example>Marketing</example>
    public string Department { get; set; }

    // /// <summary>
    // /// The user registration date.
    // /// </summary>
    // /// <example>2020-01-01T00:00:00.0000000Z</example>
    // public ApiDateTime WorkFrom { get; set; }

    /// <summary>
    /// The list of user groups.
    /// </summary>
    /// <example>[{"id": "00000000-0000-0000-0000-000000000000", "name": "Marketing"}]</example>
    public List<GroupSummaryDto> Groups { get; set; }

    /// <summary>
    /// The user location.
    /// </summary>
    /// <example>Palo Alto</example>
    public string Location { get; set; }

    /// <summary>
    /// The user notes.
    /// </summary>
    /// <example>Notes to worker</example>
    public string Notes { get; set; }

    /// <summary>
    /// Specifies if the user is an administrator or not.
    /// </summary>
    /// <example>false</example>
    public bool IsAdmin { get; set; }

    /// <summary>
    /// Specifies if the user is a room administrator or not.
    /// </summary>
    /// <example>false</example>
    public bool IsRoomAdmin { get; set; }

    /// <summary>
    /// Specifies if the LDAP settings are enabled for the user or not.
    /// </summary>
    /// <example>false</example>
    public bool IsLDAP { get; set; }

    /// <summary>
    /// The list of the administrator modules.
    /// </summary>
    /// <example>["projects", "crm"]</example>
    public List<string> ListAdminModules { get; set; }

    /// <summary>
    /// Specifies if the user is a portal owner or not.
    /// </summary>
    /// <example>false</example>
    public bool IsOwner { get; set; }

    /// <summary>
    /// Specifies if the user is a portal visitor or not.
    /// </summary>
    /// <example>false</example>
    public bool IsVisitor { get; set; }

    /// <summary>
    /// Specifies if the user is a portal collaborator or not.
    /// </summary>
    /// <example>false</example>
    public bool IsCollaborator { get; set; }

    /// <summary>
    /// The user culture code.
    /// </summary>
    /// <example>en-EN</example>
    public string CultureName { get; set; }

    /// <summary>
    /// The user mobile phone number.
    /// </summary>
    /// <example>+1 (555) 123-4567</example>
    public string MobilePhone { get; set; }

    /// <summary>
    /// The mobile phone activation status.
    /// </summary>
    /// <example>NotActivated</example>
    public MobilePhoneActivationStatus MobilePhoneActivationStatus { get; set; }

    /// <summary>
    /// Specifies if the SSO settings are enabled for the user or not.
    /// </summary>
    /// <example>false</example>
    public bool IsSSO { get; set; }

    /// <summary>
    /// The user theme settings.
    /// </summary>
    /// <example>Base</example>
    public DarkThemeSettingsType? Theme { get; set; }

    /// <summary>
    /// The user quota limit.
    /// </summary>
    /// <example>1073741824</example>
    public long? QuotaLimit { get; set; }

    /// <summary>
    /// The portal used space of the user.
    /// </summary>
    /// <example>12345</example>
    public double? UsedSpace { get; set; }

    /// <summary>
    /// Specifies if the user has access rights.
    /// </summary>
    /// <example>false</example>
    public bool? Shared { get; set; }

    /// <summary>
    /// Specifies if the user has a custom quota or not.
    /// </summary>
    /// <example>false</example>
    public bool? IsCustomQuota { get; set; }

    /// <summary>
    /// The spending limit on the AI services in force for this user - the personal one, or else the limit for every
    /// user - with what the user spent against it. Null when no such limit applies to the user, when the portal has no
    /// billing service, or when the caller is neither a DocSpace administrator nor the user; the limit of the portal as
    /// a whole is not reflected here. The value, and the spending in particular, can lag by a few minutes.
    /// </summary>
    public UserServiceLimitDto AiServicesLimit { get; set; }

    /// <summary>
    /// The current login event ID.
    /// </summary>
    /// <example>123</example>
    public int? LoginEventId { get; set; }

    /// <summary>
    /// The auth cookie lifetime in seconds.
    /// </summary>
    /// <example>3600</example>
    public double? AuthCookieLifetime { get; set; }

    /// <summary>
    /// The user who created the current user.
    /// </summary>
    /// <example>{"displayName": "John Doe"}</example>
    public EmployeeDto CreatedBy { get; set; }

    /// <summary>
    /// The user registration date.
    /// </summary>
    /// <example>2020-01-01T00:00:00.0000000Z</example>
    public ApiDateTime RegistrationDate { get; set; }

    /// <summary>
    /// Specifies if the user has a personal folder or not.
    /// </summary>
    /// <example>true</example>
    public bool? HasPersonalFolder { get; set; }

    /// <summary>
    /// Indicates whether the user has enabled two-factor authentication (TFA) using an authentication app.
    /// </summary>
    /// <example>false</example>
    public bool? TfaAppEnabled { get; set; }
}
[Scope]
public class EmployeeFullDtoHelper(
        UserManager userManager,
        AuthContext authContext,
        UserPhotoManager userPhotoManager,
        WebItemSecurity webItemSecurity,
        UserFieldsHelper userFieldsHelper,
        DisplayUserSettingsHelper displayUserSettingsHelper,
        ApiDateTimeHelper apiDateTimeHelper,
        WebItemManager webItemManager,
        SettingsManager settingsManager,
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        GroupSummaryDtoHelper groupSummaryDtoHelper,
        TfaAppAuthSettingsHelper tfaAppAuthSettingsHelper,
        ITariffService tariffService,
        ILogger<EmployeeDtoHelper> logger)
    : EmployeeDtoHelper(displayUserSettingsHelper, userPhotoManager, userFieldsHelper, userManager, authContext, logger)
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
        var tenant = tenantManager.GetCurrentTenant();
        var roles = await _userFieldsHelper.GetRoleFlagsAsync(userInfo, tenant);

        var result = new EmployeeFullDto
        {
            UserName = userInfo.UserName,
            FirstName = userInfo.FirstName,
            LastName = userInfo.LastName,
            //Birthday = apiDateTimeHelper.Get(userInfo.BirthDate),
            Status = userInfo.Status,
            ActivationStatus = userInfo.ActivationStatus & ~EmployeeActivationStatus.AutoGenerated,
            Terminated = apiDateTimeHelper.Get(userInfo.TerminatedDate),
            //WorkFrom = apiDateTimeHelper.Get(userInfo.WorkFromDate),
            Email = userInfo.Email,
            IsVisitor = roles.IsVisitor,
            IsAdmin = roles.IsAdmin,
            IsRoomAdmin = roles.IsRoomAdmin,
            IsOwner = roles.IsOwner,
            IsCollaborator = roles.IsCollaborator,
            IsLDAP = userInfo.IsLDAP(),
            IsSSO = userInfo.IsSSO(),
            Shared = shared
        };

        await InitAsync(result, userInfo);

        var isDocSpaceAdmin = await _userManager.IsDocSpaceAdminAsync(_authContext.CurrentAccount.ID);

        if (isDocSpaceAdmin || userInfo.Id == _authContext.CurrentAccount.ID)
        {
            var quota = await _userFieldsHelper.GetQuotaAsync(userInfo, tenant);

            if (quota != null)
            {
                result.UsedSpace = quota.UsedSpace;
                result.QuotaLimit = quota.QuotaLimit;
                result.IsCustomQuota = quota.IsCustomQuota;
            }
        }

        if (isDocSpaceAdmin || userInfo.Id == _authContext.CurrentAccount.ID)
        {
            result.AiServicesLimit = (await tariffService.GetCachedUserServiceLimitAsync(tenant.Id, ServiceLimitGroups.AiServices, userInfo.Id))?.ToUserServiceLimitDto();
        }

        // if (userInfo.Sex.HasValue)
        // {
        //     result.Sex = userInfo.Sex.Value ? "male" : "female";
        // }

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

        var listAdminModules = await userInfo.GetListAdminModulesAsync(webItemSecurity, webItemManager);
        if (listAdminModules.Count > 0)
        {
            result.ListAdminModules = listAdminModules;
        }

        result.RegistrationDate = apiDateTimeHelper.Get(userInfo.CreateDate);

        if (!isDocSpaceAdmin)
        {
            return result;
        }

        if (userInfo.CreatedBy.HasValue)
        {
            result.CreatedBy = await GetAsync(await _userManager.GetUsersAsync(userInfo.CreatedBy.Value));
        }

        if (await tfaAppAuthSettingsHelper.GetEnable())
        {
            result.TfaAppEnabled = await TfaAppUserSettings.EnableForUserAsync(settingsManager, userInfo.Id);
        }

        return result;
    }

    private async Task FillGroupsAsync(EmployeeFullDto result, UserInfo userInfo)
    {
        // A User or a Guest may only see their own group membership; anyone else's is not theirs to know.
        if (await IsWithoutContactsAccessAsync(userInfo.Id))
        {
            return;
        }

        var groupsFromDb = await _userManager.GetUserGroupsAsync(userInfo.Id);
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
