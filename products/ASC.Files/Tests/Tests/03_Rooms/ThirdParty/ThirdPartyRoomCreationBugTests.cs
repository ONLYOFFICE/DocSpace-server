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
/// PUT /files/rooms/thirdparty/{id} — open bugs in error handling and access control when turning
/// a third-party connection into a room.
/// </summary>
[Trait("Category", "Rooms")]
public class ThirdPartyRoomCreationBugTests(
    AspireAppFixture fixture)
    : ThirdPartyTestBase(fixture)
{
    /// <remarks>
    /// Bug 83301: <c>createRoomThirdParty</c> throws <c>System.NullReferenceException</c> (500)
    /// when given a plain internal folder id instead of a <c>sbox-*</c> third-party selector,
    /// instead of the 400/404 an unrecognized-shape id should get.
    /// </remarks>
    [Fact]
    [Trait("Bug", "83301")]
    public async Task CreateRoomThirdParty_InternalFolderId_ShouldReturnControlledError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var internalFolderId = await GetUserFolderIdAsync(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomThirdPartyAsync(
                internalFolderId.ToString(),
                new CreateThirdPartyRoom(title: "Autotest Internal Id As TP Id", roomType: RoomType.CustomRoom),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().BeLessThan(500,
            "a plain internal folder id passed as a third-party selector must be rejected with a controlled 4xx");
    }

    /// <remarks>
    /// Bug 83301: <c>createRoomThirdParty</c> throws <c>System.InvalidOperationException</c>
    /// ("Sequence contains no elements", 500) for an id that matches the <c>sbox-&lt;n&gt;</c>
    /// selector pattern but doesn't correspond to any connected provider, instead of the clean 404
    /// a completely unrecognized id gets.
    /// </remarks>
    [Fact]
    [Trait("Bug", "83301")]
    public async Task CreateRoomThirdParty_NonExistentSboxId_ShouldReturnControlledError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomThirdPartyAsync(
                "sbox-999999999",
                new CreateThirdPartyRoom(title: "Autotest Bad Sbox Id", roomType: RoomType.CustomRoom),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().BeLessThan(500,
            "a well-formed but non-existent sbox-* id must get the same clean 404 an unrecognized id gets");
    }

    /// <remarks>
    /// Bug 83306: <c>createRoomThirdParty</c> has no ownership/role check — a plain <c>User</c>
    /// (who gets 403 from both <c>saveThirdParty</c> and the regular <c>POST /files/rooms</c>) can
    /// successfully (200) convert an Owner's still-unused third-party connection into a room they
    /// don't even have access to afterwards. <c>deleteThirdParty</c> correctly checks ownership
    /// (403) for the same actor; <c>createRoomThirdParty</c> does not.
    /// </remarks>
    [Fact]
    [Trait("Bug", "83306")]
    public async Task CreateRoomThirdParty_UserFromAnotherUsersConnection_ShouldBeForbidden()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var connection = await ConnectNextcloud("Autotest User TP Room IDOR");

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomThirdPartyAsync(
                connection.Id,
                new CreateThirdPartyRoom(title: "Autotest User TP Room IDOR", roomType: RoomType.CustomRoom),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403,
            "a plain User must not be able to turn someone else's unused connection into a room");
    }

    /// <remarks>
    /// Bug 83306: same missing ownership/role check as the <c>User</c> case above — a
    /// <c>Guest</c> (who gets 403 from <c>saveThirdParty</c> and from the regular
    /// <c>POST /files/rooms</c>) can still successfully (200) turn someone else's unused
    /// third-party connection into a room.
    /// </remarks>
    [Fact]
    [Trait("Bug", "83306")]
    public async Task CreateRoomThirdParty_GuestFromAnotherUsersConnection_ShouldBeForbidden()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var connection = await ConnectNextcloud("Autotest Guest TP Room Attempt");

        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomThirdPartyAsync(
                connection.Id,
                new CreateThirdPartyRoom(title: "Autotest Guest TP Room", roomType: RoomType.CustomRoom),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403,
            "a Guest must not be able to turn someone else's unused connection into a room");
    }
}
