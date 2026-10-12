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

namespace ASC.Files.Tests.Tests._03_Rooms.ThirdParty;

/// <summary>
/// DELETE /files/thirdparty/{providerId} — open bugs in what happens to a room, and to anything
/// that still references it, once the connection behind it is disconnected.
/// </summary>
[Trait("Category", "Rooms")]
public class ThirdPartyProviderDeletionBugTests(
    AspireAppFixture fixture)
    : ThirdPartyTestBase(fixture)
{
    /// <remarks>
    /// Bug 83305: <c>deleteThirdParty</c> succeeded (200) even though a room still used the
    /// connection. The room then silently disappeared from <c>getRoomsFolder</c>, and
    /// <c>getRoomInfo</c> on it failed with a server error. <c>FileStorageService.DeleteThirdPartyAsync</c>
    /// now refuses (403) to remove a connection a room stands on: the room is deleted as a room,
    /// and that deletion disconnects the storage.
    /// </remarks>
    [Fact]
    [Trait("Bug", "83305")]
    public async Task DeleteThirdParty_ProviderBehindActiveRoom_ShouldBeRefusedAndKeepRoom()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (providerId, _, roomId) = await CreateNextcloudRoom("Autotest TP Room Then Delete Provider");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.DeleteThirdPartyAsync(providerId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403,
            "a connection that a room still stands on must not be disconnected behind the room's back");

        var room = await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken);
        room.Response.Id.Should().Be(roomId, "the refused disconnect must leave the room working");

        var accounts = await _thirdPartyApi.GetThirdPartyAccountsAsync(TestContext.Current.CancellationToken);
        accounts.Response.Should().Contain(a => a.ProviderId == providerId,
            "the refused disconnect must leave the connection in place");
    }

    /// <remarks>
    /// Bug 83264: connect Nextcloud, create a room on it, add that room to a room group, then
    /// delete the room — removing the provider account behind it did not remove the room's group
    /// membership, so the group kept a dangling reference to the room and <c>getRoomGroups</c>
    /// failed on the missing provider, taking down the entire group list instead of just the
    /// affected room. <c>ProviderAccountDao.RemoveProviderInfoAsync</c> now deletes the group links
    /// of the connection's rooms, and <c>ProviderFolderDao.GetFoldersAsync</c> leaves out the ids of
    /// a removed connection, so links that are already dangling no longer break the listing.
    /// </remarks>
    [Fact]
    [Trait("Bug", "83264")]
    public async Task GetRoomGroups_DanglingThirdPartyRoomReference_ShouldNotFail()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (_, _, roomId) = await CreateNextcloudRoom("Autotest Dangling TP Group Room");

        var group = await _roomGroupsApi.AddRoomGroupAsync(
            new RoomGroupRequestDto("Autotest Dangling TP Group", "star", [new DuplicateRequestDtoAllOfFileIds(roomId)]),
            cancellationToken: TestContext.Current.CancellationToken);
        group.Response.Rooms.Select(r => r.Title).Should().Contain("Autotest Dangling TP Group Room");

        // Disconnecting the account directly is refused while the room exists (bug 83305), so the
        // room is deleted as a room: that deletion removes the account behind it.
        await _roomsApi.DeleteRoomAsync(roomId, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();
        operations.Should().OnlyContain(o => o.Finished && o.Error == "", "the room deletion must have succeeded");
        (await WaitForFolderGone(roomId)).Should().NotBe(200, "the room deletion must have finished");

        // Act
        var exception = await Record.ExceptionAsync(
            async () => await _roomGroupsApi.GetRoomGroupsAsync(includeMembers: false, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.Should().BeNull(
            "a dangling third-party room reference in one group must not take down the entire group list");
    }
}
