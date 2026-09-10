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

namespace ASC.People.Tests.Tests._11_Guests;

/// <summary>
/// <c>DELETE /people/guests</c> — a room admin's own guest list.
///
/// <c>approveGuestShareLink</c> from the TypeScript suite is not ported: it requires a browser
/// session context (a cookie set by the front end when opening the external share-link page),
/// which cannot be driven through the API alone.
/// </summary>
public class GuestsApiTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task DeleteGuests_RoomAdminRemovesGuests_ExcludedFromRoom()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        var guest1 = await InviteGuest(roomAdmin);
        var guest2 = await InviteGuest(roomAdmin);

        await _filesClient.Authenticate(roomAdmin);
        var room = await CreateCustomRoom("Autotest Guests Room");

        await InviteToRoom(room.Id, guest1.Id, FileShare.Read);
        await InviteToRoom(room.Id, guest2.Id, FileShare.Read);

        await _peopleClient.Authenticate(roomAdmin);
        await _guestsApi.DeleteGuestsAsync(
            new UpdateMembersRequestDto([guest1.Id, guest2.Id]),
            TestContext.Current.CancellationToken);

        var shares = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var memberIds = shares.Select(s => s.SharedToUser.Id).ToList();

        memberIds.Should().NotContain(guest1.Id);
        memberIds.Should().NotContain(guest2.Id);
    }

    [Fact]
    public async Task DeleteGuests_RoomAdminRemovesSingleGuest_Succeeds()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        var guest = await InviteGuest(roomAdmin);

        await _guestsApi.DeleteGuestsAsync(
            new UpdateMembersRequestDto([guest.Id]),
            TestContext.Current.CancellationToken);
    }
}
