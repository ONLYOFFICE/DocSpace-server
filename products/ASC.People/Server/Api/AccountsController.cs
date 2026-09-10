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

/// <remarks>
/// Accounts API.
/// </remarks>
[Scope]
[ApiEndpoint("accounts")]
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
    /// Searches the portal users and groups that can be given access to the room with the ID given in the route, and
    /// reports for each of them whether it already has access to that room.
    /// The caller has to be allowed to manage the access of that room, and the ID has to belong to an existing room,
    /// so the operation answers 403 for a room the caller cannot share and 404 for an ID that matches nothing.
    /// The search is read-only and needs `filterValue`: while it is empty the operation returns an empty list and a
    /// total of 0 instead of every account, so it cannot be used to enumerate the portal.
    /// `filterValue` is matched case-insensitively against the first name, the last name and the email; without
    /// `filterSeparator` it is split on spaces and every term has to match, and with a separator it is split on that
    /// separator and any term may match.
    /// Matching groups are streamed first and users after them, both paged together by `count` and `startIndex`,
    /// while the number of matches is reported in the total count of the response.
    /// Pass `excludeShared` to keep only the accounts that have no access yet, `includeShared` to keep only those
    /// that already have it, and neither to get both kinds with the `shared` field telling them apart.
    /// </remarks>
    /// <summary>Search accounts for a room</summary>
    /// <path>api/2.0/accounts/room/{id}/search</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "The matching users and groups, each with its access state for the room", typeof(IAsyncEnumerable<IAccountEntryDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No room has the specified ID")]
    [HttpGet("room/{id}/search")]
    public async IAsyncEnumerable<IAccountEntryDto> GetAccountsEntriesWithRoomsShared(AccountsEntriesRequestDto<T> inDto)
    {
        var room = (await daoFactory.GetFolderDao<T>().GetFolderAsync(inDto.Id)).NotFoundIfNull();

        await foreach (var p in GetAccounts(inDto, room))
        {
            yield return p;
        }
    }
    /// <remarks>
    /// Searches the portal users and groups that can be given access to the folder with the ID given in the route,
    /// and reports for each of them whether it already has access to that folder.
    /// The caller has to be allowed to manage the access of that folder, and the ID has to belong to an existing
    /// folder, so the operation answers 403 for a folder the caller cannot share and 404 for an ID that matches
    /// nothing.
    /// The search is read-only and needs `filterValue`: while it is empty the operation returns an empty list and a
    /// total of 0 instead of every account, so it cannot be used to enumerate the portal.
    /// `filterValue` is matched case-insensitively against the first name, the last name and the email; without
    /// `filterSeparator` it is split on spaces and every term has to match, and with a separator it is split on that
    /// separator and any term may match.
    /// Matching groups are streamed first and users after them, both paged together by `count` and `startIndex`,
    /// while the number of matches is reported in the total count of the response.
    /// Pass `excludeShared` to keep only the accounts that have no access yet, `includeShared` to keep only those
    /// that already have it, and neither to get both kinds with the `shared` field telling them apart.
    /// </remarks>
    /// <summary>Search accounts for a folder</summary>
    /// <path>api/2.0/accounts/folder/{id}/search</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "The matching users and groups, each with its access state for the folder", typeof(IAsyncEnumerable<IAccountEntryDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No folder has the specified ID")]
    [HttpGet("folder/{id}/search")]
    public async IAsyncEnumerable<IAccountEntryDto> GetAccountsEntriesWithFoldersShared(AccountsEntriesRequestDto<T> inDto)
    {
        var room = (await daoFactory.GetFolderDao<T>().GetFolderAsync(inDto.Id)).NotFoundIfNull();

        await foreach (var p in GetAccounts(inDto, room))
        {
            yield return p;
        }
    }
    /// <remarks>
    /// Searches the portal users and groups that can be given access to the file with the ID given in the route, and
    /// reports for each of them whether it already has access to that file.
    /// The caller has to be allowed to manage the access of that file, and the ID has to belong to an existing file,
    /// so the operation answers 403 for a file the caller cannot share and 404 for an ID that matches nothing.
    /// The search is read-only and needs `filterValue`: while it is empty the operation returns an empty list and a
    /// total of 0 instead of every account, so it cannot be used to enumerate the portal.
    /// `filterValue` is matched case-insensitively against the first name, the last name and the email; without
    /// `filterSeparator` it is split on spaces and every term has to match, and with a separator it is split on that
    /// separator and any term may match.
    /// Matching groups are streamed first and users after them, both paged together by `count` and `startIndex`,
    /// while the number of matches is reported in the total count of the response.
    /// Pass `excludeShared` to keep only the accounts that have no access yet, `includeShared` to keep only those
    /// that already have it, and neither to get both kinds with the `shared` field telling them apart.
    /// </remarks>
    /// <summary>Search accounts for a file</summary>
    /// <path>api/2.0/accounts/file/{id}/search</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "The matching users and groups, each with its access state for the file", typeof(IAsyncEnumerable<IAccountEntryDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No file has the specified ID")]
    [HttpGet("file/{id}/search")]
    public async IAsyncEnumerable<IAccountEntryDto> GetAccountsEntriesWithFilesShared(AccountsEntriesRequestDto<T> inDto)
    {
        var room = (await daoFactory.GetFileDao<T>().GetFileAsync(inDto.Id)).NotFoundIfNull();

        await foreach (var p in GetAccounts(inDto, room))
        {
            yield return p;
        }
    }

    private async IAsyncEnumerable<IAccountEntryDto> GetAccounts(AccountsEntriesRequestDto<T> inDto, FileEntry<T> fileEntry)
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