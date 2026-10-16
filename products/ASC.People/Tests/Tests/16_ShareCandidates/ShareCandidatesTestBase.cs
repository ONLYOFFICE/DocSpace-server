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

namespace ASC.People.Tests.Tests._16_ShareCandidates;

/// <summary>
/// Shared setup for the share candidates: who a caller is offered when it shares an entry of its own
/// through <c>GET /people/{file|folder}/{id}</c>, <c>GET /group/{file|folder}/{id}</c> and
/// <c>GET /accounts/{file|folder}/{id}/search</c>. A user or a guest has no access to the contacts
/// of the portal, so it is offered only the accounts it already works with and the Everyone group;
/// administrators and room admins are offered the portal as before. The lists are read with plain
/// SQL, so a read right after the write that should change them is safe.
/// </summary>
public abstract class ShareCandidatesTestBase(AspireAppFixture fixture) : SharingTestBase(fixture)
{
    /// <summary>
    /// A custom room created by the owner with the given subjects - users or groups - invited at
    /// <see cref="FileShare.Editing"/>, the access every member type accepts in a custom room.
    /// </summary>
    protected async Task<int> CreateRoomWithMembersAsync(params Guid[] subjects)
    {
        var roomId = await CreateCustomRoomAsync();

        foreach (var subject in subjects)
        {
            await InviteToRoom(roomId, subject, FileShare.Editing);
        }

        return roomId;
    }

    /// <summary>
    /// The users the caller is offered for its file, read as the share dialog reads them: every
    /// active account of the area, without a search term.
    /// </summary>
    protected async Task<List<EmployeeFullDto>> UsersOfferedForFileAsync(User caller, int fileId, AccountSearchArea area = AccountSearchArea.People)
    {
        await _peopleClient.Authenticate(caller);

        return (await _peopleSearchApi.GetUsersWithFilesSharedAsync(
            fileId,
            employeeStatus: EmployeeStatus.Active,
            area: area,
            count: 100,
            cancellationToken: TestContext.Current.CancellationToken)).Response;
    }
}
