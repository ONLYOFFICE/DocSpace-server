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

using ASC.Web.Files.Utils;

namespace ASC.People.Api;

[ConstraintRoute("int")]
public class AccountsControllerInternal(
    IDaoFactory daoFactory,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    GroupFullDtoHelper groupFullDtoHelper,
    ApiContext apiContext,
    FileSecurity fileSecurity,
    FileSharing fileSharing,
    AuthContext authContext,
    UserManager userManager)
    : AccountsController<int>(daoFactory, employeeFullDtoHelper, groupFullDtoHelper, apiContext, fileSecurity, fileSharing, authContext, userManager);

public class AccountsControllerThirdParty(
    IDaoFactory daoFactory,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    GroupFullDtoHelper groupFullDtoHelper,
    ApiContext apiContext,
    FileSecurity fileSecurity,
    FileSharing fileSharing,
    AuthContext authContext,
    UserManager userManager)
    : AccountsController<string>(daoFactory, employeeFullDtoHelper, groupFullDtoHelper, apiContext, fileSecurity, fileSharing, authContext, userManager);

[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("accounts")]
public class AccountsController<T>(
    IDaoFactory daoFactory,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    GroupFullDtoHelper groupFullDtoHelper,
    ApiContext apiContext,
    FileSecurity fileSecurity,
    FileSharing fileSharing,
    AuthContext authContext,
    UserManager userManager) : ControllerBase
{
    /// <remarks>
    /// Returns the account entries with their sharing settings in a room with the ID specified in request.
    /// </remarks>
    /// <summary>Get account entries</summary>
    /// <path>api/2.0/accounts/room/{id}/search</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "Ok", typeof(IAsyncEnumerable<object>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("room/{id}/search")]
    public async IAsyncEnumerable<object> GetAccountsEntriesWithRoomsShared(AccountsEntriesRequestDto<T> inDto)
    {
        var room = (await daoFactory.GetFolderDao<T>().GetFolderAsync(inDto.Id)).NotFoundIfNull();

        await foreach (var p in GetAccounts(inDto, room))
        {
            yield return p;
        }
    }
    /// <remarks>
    /// Returns the account entries with their sharing settings in a folder with the ID specified in request.
    /// </remarks>
    /// <summary>Get account entries with folder sharing settings</summary>
    /// <path>api/2.0/accounts/folder/{id}/search</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "Ok", typeof(IAsyncEnumerable<object>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("folder/{id}/search")]
    public async IAsyncEnumerable<object> GetAccountsEntriesWithFoldersShared(AccountsEntriesRequestDto<T> inDto)
    {
        var room = (await daoFactory.GetFolderDao<T>().GetFolderAsync(inDto.Id)).NotFoundIfNull();

        await foreach (var p in GetAccounts(inDto, room))
        {
            yield return p;
        }
    }
    /// <remarks>
    /// Returns the account entries with their sharing settings for a file with the ID specified in request.
    /// </remarks>
    /// <summary>Get account entries with file sharing settings</summary>
    /// <path>api/2.0/accounts/file/{id}/search</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "Ok", typeof(IAsyncEnumerable<object>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("file/{id}/search")]
    public async IAsyncEnumerable<object> GetAccountsEntriesWithFilesShared(AccountsEntriesRequestDto<T> inDto)
    {
        var room = (await daoFactory.GetFileDao<T>().GetFileAsync(inDto.Id)).NotFoundIfNull();

        await foreach (var p in GetAccounts(inDto, room))
        {
            yield return p;
        }
    }

    private async IAsyncEnumerable<object> GetAccounts(AccountsEntriesRequestDto<T> inDto, FileEntry<T> fileEntry)
    {
        if (!await fileSecurity.CanEditAccessAsync(fileEntry))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var offset = inDto.StartIndex;
        var count = inDto.Count;
        var text = inDto.Text;
        var separator = inDto.FilterSeparator;

        var securityDao = daoFactory.GetSecurityDao<T>();
        var includeStrangers = await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID);
        var parentUserIds = await fileSharing.GetPureSharesAsync(fileEntry, ShareFilterType.UserOrGroup, inDto.ActivationStatus, inDto.Text, 0, int.MaxValue).Select(r=> r.Id).ToListAsync();

        if (string.IsNullOrEmpty(text))
        {
            apiContext.SetCount(0).SetTotalCount(0);
            yield break;
        }


        var totalGroups = await securityDao.GetGroupsWithSharedCountAsync(fileEntry, text, inDto.ExcludeShared ?? false, parentUserIds);
        var totalUsers = await securityDao.GetUsersWithSharedCountAsync(fileEntry,
            text,
            inDto.EmployeeStatus,
            inDto.ActivationStatus,
            inDto.ExcludeShared ?? false,
            inDto.IncludeShared ?? false,
            separator,
            includeStrangers,
            inDto.Area,
            inDto.InvitedByMe,
            inDto.InviterId,
            inDto.EmployeeTypes,
            parentUserIds);

        var total = totalGroups + totalUsers;

        apiContext.SetCount(Math.Min(Math.Max(total - offset, 0), count)).SetTotalCount(total);

        var groupsCount = 0;

        await foreach (var item in securityDao.GetGroupsWithSharedAsync(fileEntry, text, inDto.ExcludeShared ?? false, offset, count, parentUserIds))
        {
            groupsCount++;
            yield return await groupFullDtoHelper.Get(item.GroupInfo, false, item.Shared);
        }

        var usersCount = count - groupsCount;
        var usersOffset = Math.Max(groupsCount > 0 ? 0 : offset - totalGroups, 0);

        await foreach (var item in securityDao.GetUsersWithSharedAsync(fileEntry,
                           text,
                           inDto.EmployeeStatus,
                           inDto.ActivationStatus,
                           inDto.ExcludeShared ?? false,
                           inDto.IncludeShared ?? false,
                           separator,
                           includeStrangers,
                           inDto.Area,
                           inDto.InvitedByMe,
                           inDto.InviterId,
                           inDto.EmployeeTypes,
                           parentUserIds,
                           usersOffset,
                           usersCount))
        {
            yield return await employeeFullDtoHelper.GetFullAsync(item.UserInfo, item.Shared);
        }
    }
}