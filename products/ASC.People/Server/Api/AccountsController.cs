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

using SearchArea = ASC.People.Utils.SearchArea;

namespace ASC.People.Api;

[Scope]
[DefaultRoute]
[ApiController]
public class AccountsController(
    ApiContext apiContext,
    SecurityContext securityContext,
    UserManager userManager,
    WebItemSecurity webItemSecurity,
    GroupFullDtoHelper groupFullDtoHelper,
    EmployeeFullDtoHelper employeeFullDtoHelper) : ControllerBase
{
    /// <summary>
    /// Returns a list of users or groups with full information about them matching the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Search users and groups by extended filter
    /// </short>
    /// <category>accounts</category>
    /// <path>api/2.0/accounts</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("search")]
    public async IAsyncEnumerable<object> GetAccountsEntriesAsync(EmployeeStatus? employeeStatus, EmployeeActivationStatus? activationStatus)
    {
        var offset = Convert.ToInt32(apiContext.StartIndex);
        var count = Convert.ToInt32(apiContext.Count);
        var text = apiContext.FilterValue;

        if (string.IsNullOrEmpty(text))
        {
            apiContext.SetCount(0).SetTotalCount(0);
            yield break;
        }
        
        var isDocSpaceAdmin = (await userManager.IsDocSpaceAdminAsync(securityContext.CurrentAccount.ID)) ||
                              await webItemSecurity.IsProductAdministratorAsync(WebItemManager.PeopleProductID, securityContext.CurrentAccount.ID);

        var totalGroups = await userManager.GetGroupsCountAsync(text);
        var totalUsers = await userManager.GetUsersCountAsync(isDocSpaceAdmin, employeeStatus, [], [], [], activationStatus, null, text, false);
        var total = totalGroups + totalUsers;

        apiContext.SetCount(Math.Min(Math.Max(total - offset, 0), count)).SetTotalCount(total);

        var groupsCount = 0;

        await foreach (var group in userManager.GetGroupsAsync(text, true, offset, count))
        {
            groupsCount++;
            yield return await groupFullDtoHelper.Get(group, false);
        }

        var usersCount = count - groupsCount;
        var usersOffset = Math.Max(groupsCount > 0 ? 0 : offset - totalGroups, 0);

        var users = userManager.GetUsers(isDocSpaceAdmin, employeeStatus, [], [], [], activationStatus, null, text, false,
            "firstname", true, usersCount, usersOffset);

        await foreach (var user in users)
        {
            yield return await employeeFullDtoHelper.GetFullAsync(user);
        }
    }
}


[ConstraintRoute("int")]
public class AccountsControllerAdditionalInternal(
    IDaoFactory daoFactory,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    GroupFullDtoHelper groupFullDtoHelper,
    ApiContext apiContext,
    FileSecurity fileSecurity)
    : AccountsControllerAdditional<int>(daoFactory, employeeFullDtoHelper, groupFullDtoHelper, apiContext, fileSecurity);

public class AccountsControllerAdditionalThirdParty(
    IDaoFactory daoFactory,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    GroupFullDtoHelper groupFullDtoHelper,
    ApiContext apiContext,
    FileSecurity fileSecurity)
    : AccountsControllerAdditional<string>(daoFactory, employeeFullDtoHelper, groupFullDtoHelper, apiContext, fileSecurity);

[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("accounts")]
public class AccountsControllerAdditional<T>(
    IDaoFactory daoFactory,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    GroupFullDtoHelper groupFullDtoHelper,
    ApiContext apiContext,
    FileSecurity fileSecurity)
{
    [HttpGet("room/{id}")]
    public async IAsyncEnumerable<object> GetEntriesWithSharedAsync(T id,
        EmployeeStatus? employeeStatus,
        EmployeeActivationStatus? activationStatus,
        bool? excludeShared,
        SearchArea searchArea = SearchArea.Any)
    {
        var offset = Convert.ToInt32(apiContext.StartIndex);
        var requestCount = Convert.ToInt32(apiContext.Count);
        
        var room = (await daoFactory.GetFolderDao<T>().GetFolderAsync(id)).NotFoundIfNull();

        var totalGroupsTask = searchArea != SearchArea.Users 
            ? fileSecurity.GetGroupsWithSharedCountAsync(room, apiContext.FilterValue, excludeShared ?? false)
            : Task.FromResult(0);

        var totalUsersTask = searchArea != SearchArea.Groups
            ? fileSecurity.GetUsersWithSharedCountAsync(room, apiContext.FilterValue, employeeStatus, activationStatus, excludeShared ?? false)
            : Task.FromResult(0);

        var groups = searchArea != SearchArea.Users
            ? await fileSecurity.GetGroupInfoWithSharedAsync(room, apiContext.FilterValue, excludeShared ?? false, offset, requestCount).ToListAsync()
            : [];

        var totalGroups = await totalGroupsTask;

        var usersCount = requestCount - groups.Count;
        var usersOffset = Math.Max(groups.Count > 0 ? 0 : offset - totalGroups, 0);

        var usersWithShared = searchArea != SearchArea.Groups
            ? fileSecurity.GetUsersWithSharedAsync(room, apiContext.FilterValue, employeeStatus, activationStatus, excludeShared ?? false, usersOffset, usersCount)
            : AsyncEnumerable.Empty<UserInfoWithShared>();

        var totalUsers = await totalUsersTask;
        var total = totalGroups + totalUsers;

        apiContext.SetCount(Math.Min(Math.Max(total - offset, 0), requestCount)).SetTotalCount(total);

        await foreach (var item in groups.ToAsyncEnumerable())
        {
            yield return await groupFullDtoHelper.Get(item.GroupInfo, false, item.Shared);
        }

        await foreach (var item in usersWithShared)
        {
            yield return await employeeFullDtoHelper.GetFullAsync(item.UserInfo, item.Shared);
        }
    }
}