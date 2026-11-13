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
    /// Returns the users and groups that match a search, with whether each one has access to the room. Requires the right
    /// to change the access of the room. Other callers get 403. If no room has the ID, the call returns 404. The call is
    /// read-only. If `filterValue` is empty, it returns an empty list and a total of 0. The call matches users by first
    /// name, last name, or email, and groups by name, ignoring case. Without `filterSeparator`, a user must match every
    /// term between spaces. With `filterSeparator`, a user may match any one term. Groups always match the whole text,
    /// and they include the system group Everyone. The call returns groups first, sorted by name, and then users. The
    /// `count` and `startIndex` parameters page over both together, and the total counts both. The `excludeShared` flag
    /// keeps only accounts without access and wins over `includeShared`. The `includeShared` flag filters only users, so
    /// groups without access still appear. Groups ignore the user filters, such as `employeeStatus`, `area`, and
    /// `employeeTypes`. A caller who is not a DocSpace admin sees only the guests related to them. With `area` set to
    /// `All`, they also see guests with access. The `shared` field is also true for the owner and for an account with a
    /// record that denies access. Only a DocSpace admin can filter by the `inviterId` of another account. For other
    /// callers, the call ignores such an `inviterId`. If `count` is out of range, the call returns 400.
    /// </remarks>
    /// <summary>Search accounts for a room</summary>
    /// <path>api/2.0/accounts/room/{id}/search</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "The matching users and groups, each with its access state for the room", typeof(IAsyncEnumerable<IAccountEntryDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
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
    /// Returns the users and groups that match a search, with whether each one has access to the folder. Requires the
    /// right to change the access of the folder. Other callers get 403. If no folder has the ID, the call returns 404.
    /// The call is read-only. If `filterValue` is empty, it returns an empty list and a total of 0. The call matches
    /// users by first name, last name, or email, and groups by name, ignoring case. Without `filterSeparator`, a user
    /// must match every term between spaces. With `filterSeparator`, a user may match any one term. Groups always match
    /// the whole text, and they include the system group Everyone. The call returns groups first, sorted by name, and
    /// then users. The `count` and `startIndex` parameters page over both together, and the total counts both. The
    /// `excludeShared` flag keeps only accounts without access and wins over `includeShared`. The `includeShared` flag
    /// filters only users, so groups without access still appear. Groups ignore the user filters, such as
    /// `employeeStatus`, `area`, and `employeeTypes`. A caller who is not a DocSpace admin sees only the guests related
    /// to them. With `area` set to `All`, they also see guests with access. The `shared` field is also true for the owner
    /// and for an account with a record that denies access. A user or a guest can call it for their own folder. They find
    /// only the group Everyone and the accounts they work with. These are the owners and members of their rooms, directly
    /// or through a group. Accounts with their own record that gives access also count. A record on a parent folder
    /// counts too. For another account, they get only the id, names, email, avatars, type flags, status, and `shared`.
    /// Only a DocSpace admin can filter by the `inviterId` of another account. For other callers, the call ignores such
    /// an `inviterId`. If `count` is out of range, the call returns 400.
    /// </remarks>
    /// <summary>Search accounts for a folder</summary>
    /// <path>api/2.0/accounts/folder/{id}/search</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "The matching users and groups, each with its access state for the folder", typeof(IAsyncEnumerable<IAccountEntryDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
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
    /// Returns the users and groups that match a search, with whether each one has access to the file. Requires the right
    /// to change the access of the file. Other callers get 403. If no file has the ID, the call returns 404. The call is
    /// read-only. If `filterValue` is empty, it returns an empty list and a total of 0. The call matches users by first
    /// name, last name, or email, and groups by name, ignoring case. Without `filterSeparator`, a user must match every
    /// term between spaces. With `filterSeparator`, a user may match any one term. Groups always match the whole text,
    /// and they include the system group Everyone. The call returns groups first, sorted by name, and then users. The
    /// `count` and `startIndex` parameters page over both together, and the total counts both. The `excludeShared` flag
    /// keeps only accounts without access and wins over `includeShared`. The `includeShared` flag filters only users, so
    /// groups without access still appear. Groups ignore the user filters, such as `employeeStatus`, `area`, and
    /// `employeeTypes`. A caller who is not a DocSpace admin sees only the guests related to them. With `area` set to
    /// `All`, they also see guests with access. The `shared` field is also true for the owner and for an account with a
    /// record that denies access. A user or a guest can call it for their own file. They find only the group Everyone and
    /// the accounts they work with. These are the owners and members of their rooms, directly or through a group.
    /// Accounts with their own record that gives access also count. A record on a parent folder counts too. For another
    /// account, they get only the id, names, email, avatars, type flags, status, and `shared`. Only a DocSpace admin can
    /// filter by the `inviterId` of another account. For other callers, the call ignores such an `inviterId`. If `count`
    /// is out of range, the call returns 400.
    /// </remarks>
    /// <summary>Search accounts for a file</summary>
    /// <path>api/2.0/accounts/file/{id}/search</path>
    /// <collection>list</collection>
    [Tags("People / Search")]
    [SwaggerResponse(200, "The matching users and groups, each with its access state for the file", typeof(IAsyncEnumerable<IAccountEntryDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
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
        var knownToSubjects = await fileSharing.GetKnownToSubjectsAsync();
        var everyoneOnly = !await fileSharing.CanPickAnyGroupAsync();

        if (string.IsNullOrEmpty(text))
        {
            apiContext.SetCount(0).SetTotalCount(0);
            yield break;
        }

        // The access list as the caller sees it: every record marks an account as already having one (`shared`), but a
        // Restrict record denies access and makes nobody known.
        // Read without the search text: these are sets of ids, and the query below matches the text word by word,
        // while the access list would match it as one string and drop "Ann Lee" from both sets. Only the ids and
        // the access are read: the full entries of the list cost a lookup per record on every request.
        var shares = await fileSharing.GetPureShareSubjectsAsync(fileEntry, ShareFilterType.UserOrGroup, inDto.ActivationStatus);
        var parentUserIds = shares.Select(r => r.Id).ToList();
        var knownFromShares = shares.Where(r => r.Access != ASC.Files.Core.Security.FileShare.Restrict).Select(r => r.Id).ToList();


        var totalGroups = await securityDao.GetGroupsWithSharedCountAsync(fileEntry, text, inDto.ExcludeShared ?? false, everyoneOnly, parentUserIds);
        var totalUsers = await securityDao.GetUsersWithSharedCountAsync(fileEntry,
            text,
            inDto.EmployeeStatus,
            inDto.ActivationStatus,
            inDto.ExcludeShared ?? false,
            inDto.IncludeShared ?? false,
            sharedVisible: true,
            separator,
            includeStrangers,
            knownToSubjects,
            knownFromShares,
            inDto.Area,
            inDto.InvitedByMe,
            inDto.InviterId,
            inDto.EmployeeTypes,
            parentUserIds);

        var total = totalGroups + totalUsers;

        apiContext.SetCount(Math.Min(Math.Max(total - offset, 0), count)).SetTotalCount(total);

        var groupsCount = 0;

        await foreach (var item in securityDao.GetGroupsWithSharedAsync(fileEntry, text, inDto.ExcludeShared ?? false, everyoneOnly, offset, count, parentUserIds))
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
                           sharedVisible: true,
                           separator,
                           includeStrangers,
                           knownToSubjects,
                           knownFromShares,
                           inDto.Area,
                           inDto.InvitedByMe,
                           inDto.InviterId,
                           inDto.EmployeeTypes,
                           parentUserIds,
                           usersOffset,
                           usersCount))
        {
            yield return await employeeFullDtoHelper.GetShareCandidateAsync(item.UserInfo, item.Shared);
        }
    }
}