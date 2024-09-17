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

namespace ASC.People.Api;

[ConstraintRoute("int")]
public class AccountsControllerInternal(
    IDaoFactory daoFactory,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    GroupFullDtoHelper groupFullDtoHelper,
    ApiContext apiContext,
    FileSecurity fileSecurity)
    : AccountsController<int>(daoFactory, employeeFullDtoHelper, groupFullDtoHelper, apiContext, fileSecurity);

public class AccountsControllerThirdParty(
    IDaoFactory daoFactory,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    GroupFullDtoHelper groupFullDtoHelper,
    ApiContext apiContext,
    FileSecurity fileSecurity)
    : AccountsController<string>(daoFactory, employeeFullDtoHelper, groupFullDtoHelper, apiContext, fileSecurity);

[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("accounts")]
public class AccountsController<T>(
    IDaoFactory daoFactory,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    GroupFullDtoHelper groupFullDtoHelper,
    ApiContext apiContext,
    FileSecurity fileSecurity) : ControllerBase
{
    [HttpGet("room/{id}/search")]
    public async IAsyncEnumerable<object> GetAccountsEntriesWithSharedAsync(T id,
        EmployeeStatus? employeeStatus,
        EmployeeActivationStatus? activationStatus,
        bool? excludeShared)
    {
        var room = (await daoFactory.GetFolderDao<T>().GetFolderAsync(id)).NotFoundIfNull();

        if (!await fileSecurity.CanEditAccessAsync(room))
        {
            throw new SecurityException();
        }
        
        var offset = Convert.ToInt32(apiContext.StartIndex);
        var count = Convert.ToInt32(apiContext.Count);
        var text = apiContext.FilterValue;
        var separator = apiContext.FilterSeparator;

        if (string.IsNullOrEmpty(text))
        {
            apiContext.SetCount(0).SetTotalCount(0);
            yield break;
        }

        var securityDao = daoFactory.GetSecurityDao<T>();

        var totalGroups = await securityDao.GetGroupsWithSharedCountAsync(room, text, excludeShared ?? false);
        var totalUsers = await securityDao.GetUsersWithSharedCountAsync(room, text, employeeStatus, activationStatus, excludeShared ?? false, separator);
        var total = totalGroups + totalUsers;
        
        apiContext.SetCount(Math.Min(Math.Max(total - offset, 0), count)).SetTotalCount(total);

        var groupsCount = 0;

        await foreach (var item in securityDao.GetGroupsWithSharedAsync(room, text, excludeShared ?? false, offset, count))
        {
            groupsCount++;
            yield return await groupFullDtoHelper.Get(item.GroupInfo, false, item.Shared);
        }
        
        var usersCount = count - groupsCount;
        var usersOffset = Math.Max(groupsCount > 0 ? 0 : offset - totalGroups, 0);

        await foreach (var item in securityDao.GetUsersWithSharedAsync(room, text, employeeStatus, activationStatus, excludeShared ?? false, 
                           separator, usersOffset, usersCount))
        {
            yield return await employeeFullDtoHelper.GetFullAsync(item.UserInfo, item.Shared);
        }
    }
}