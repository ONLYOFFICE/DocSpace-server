// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.People.Api;

[Scope]
[DefaultRoute]
[ApiController]
public class AccountsController(
    CoreBaseSettings coreBaseSettings,
    ApiContext apiContext,
    SecurityContext securityContext,
    UserManager userManager,
    WebItemSecurity webItemSecurity,
    GroupFullDtoHelper groupFullDtoHelper,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    WebItemManager webItemManager) : ControllerBase
{
    [HttpGet]
    public async IAsyncEnumerable<object> GetEntriesAsync(EmployeeStatus? employeeStatus,
        EmployeeActivationStatus? activationStatus,
        EmployeeType? employeeType,
        Payments? payments,
        AccountLoginType? accountLoginType,
        [FromQuery] EmployeeType[] employeeTypes,
        [FromQuery] Guid[] groupsIds,
        bool? isAdministrator,
        bool? withoutGroup)
    {
        if (coreBaseSettings.Personal)
        {
            throw new MethodAccessException("Method not available");
        }
        
        var isDocSpaceAdmin = (await userManager.IsDocSpaceAdminAsync(securityContext.CurrentAccount.ID)) ||
                              await webItemSecurity.IsProductAdministratorAsync(WebItemManager.PeopleProductID, securityContext.CurrentAccount.ID);
        var filter = GroupBasedFilter.Create(groupsIds, employeeType, employeeTypes, isAdministrator, payments, withoutGroup, webItemManager);

        var totalUsersCountTask = userManager.GetUsersCountAsync(isDocSpaceAdmin, employeeStatus, filter.IncludeGroups, filter.ExcludeGroups, filter.CombinedGroups, activationStatus, accountLoginType,
            apiContext.FilterValue, withoutGroup ?? false);

        var onlyUsers = employeeStatus.HasValue || (groupsIds != null && groupsIds.Length != 0) || activationStatus.HasValue || employeeType.HasValue || 
                        (employeeTypes != null && employeeTypes.Length != 0) || isAdministrator.HasValue || payments.HasValue || accountLoginType.HasValue;

        var groups = onlyUsers 
            ? Enumerable.Empty<GroupInfo>() 
            : (await userManager.GetDepartmentsAsync()).Select(r => r);
        
        if (!string.IsNullOrEmpty(apiContext.FilterValue))
        {
            groups = groups.Where(r => r.Name!.Contains(apiContext.FilterValue, StringComparison.InvariantCultureIgnoreCase));
        }

        var totalGroupsCount = groups.Count();

        groups = groups.Skip((int)apiContext.StartIndex).Take((int)apiContext.Count);
        
        var groupsCount = groups.Count();

        var usersLimit = apiContext.Count - groupsCount;
        var usersOffset =  Math.Max(groupsCount > 0 ? 0 : apiContext.StartIndex - totalGroupsCount, 0);

        var users = userManager.GetUsers(isDocSpaceAdmin, employeeStatus, filter.IncludeGroups, filter.ExcludeGroups, filter.CombinedGroups, activationStatus, accountLoginType,
            apiContext.FilterValue, withoutGroup ?? false, apiContext.SortBy, !apiContext.SortDescending, usersLimit, usersOffset);

        var usersCount = 0;
        
        foreach (var g in groups)
        {
            yield return await groupFullDtoHelper.Get(g, false);
        }

        await foreach (var user in users)
        {
            usersCount++;

            yield return await employeeFullDtoHelper.GetFullAsync(user);
        }

        apiContext.SetCount(groupsCount + usersCount).SetTotalCount(totalGroupsCount + await totalUsersCountTask);
    }
}