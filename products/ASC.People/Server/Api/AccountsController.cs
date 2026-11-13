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
    /// Searches the users and groups that can be given access to the room given in the route, marking those that
    /// already hold an access record.
    /// It answers 403 without the right to manage that access and 404 when no room has the ID.
    /// The search is read-only and needs `filterValue`, matched case-insensitively against names and email - all
    /// space-separated terms, or any term with `filterSeparator`; an empty value returns nothing.
    /// Groups come before users, paged together by `count` and `startIndex`.
    /// `shared` marks an account with an access record, even a denying one, on the room; `excludeShared` and
    /// `includeShared` filter by it.
    /// A DocSpace administrator finds everyone; anybody else a guest only if related to it or holding such a record.
    /// A room admin finds every member and group.
    /// `inviterId` naming another account works only for a DocSpace administrator and is ignored otherwise.
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
    /// Searches the users and groups that can be given access to the folder given in the route, marking those that
    /// already hold an access record.
    /// It answers 403 without the right to manage that access and 404 when no folder has the ID.
    /// The search is read-only and needs `filterValue`, matched case-insensitively against names and email - all
    /// space-separated terms, or any term with `filterSeparator`; an empty value returns nothing.
    /// Groups come before users, paged together by `count` and `startIndex`.
    /// `shared` marks an account with an access record, even a denying one, on the folder or a parent folder;
    /// `excludeShared` and `includeShared` filter by it.
    /// A DocSpace administrator finds everyone; anybody else a guest only if related to it or holding such a record.
    /// A room admin finds every member and group; a user or a guest sharing its own folder only the Everyone group and
    /// the accounts it works with: owners and members of its rooms, directly or through a group, and holders of such a
    /// record in person.
    /// Of another account they get only the id, names, email, avatars, type flags, status and `shared`.
    /// `inviterId` naming another account works only for a DocSpace administrator and is ignored otherwise.
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
    /// Searches the users and groups that can be given access to the file given in the route, marking those that
    /// already hold an access record.
    /// It answers 403 without the right to manage that access and 404 when no file has the ID.
    /// The search is read-only and needs `filterValue`, matched case-insensitively against names and email - all
    /// space-separated terms, or any term with `filterSeparator`; an empty value returns nothing.
    /// Groups come before users, paged together by `count` and `startIndex`.
    /// `shared` marks an account with an access record, even a denying one, on the file or a parent folder;
    /// `excludeShared` and `includeShared` filter by it.
    /// A DocSpace administrator finds everyone; anybody else a guest only if related to it or holding such a record.
    /// A room admin finds every member and group; a user or a guest sharing its own file only the Everyone group and
    /// the accounts it works with: owners and members of its rooms, directly or through a group, and holders of such a
    /// record in person.
    /// Of another account they get only the id, names, email, avatars, type flags, status and `shared`.
    /// `inviterId` naming another account works only for a DocSpace administrator and is ignored otherwise.
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