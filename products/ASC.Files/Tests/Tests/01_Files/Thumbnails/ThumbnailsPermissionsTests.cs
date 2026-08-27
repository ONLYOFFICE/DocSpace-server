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

namespace ASC.Files.Tests.Tests._01_Files.Thumbnails;

/// <summary>
/// POST /files/thumbnails — who is allowed to queue thumbnail generation.
/// </summary>
[Trait("Category", "Files")]
public class ThumbnailsPermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <remarks>
    /// BUG 81268: an unauthenticated caller should get 401 Unauthorized, like every other
    /// endpoint under <c>files/</c> that mutates state. The controller action currently carries
    /// <c>[AllowAnonymous]</c>, so the request succeeds instead of being rejected.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81268")]
    public async Task CreateThumbnails_Anonymous_ShouldBeUnauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Thumbnails Anon Room");
        var file = await CreateFile("Autotest Thumbnails Anon File", room.Id);
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CreateThumbnailsAsync(
                new BaseBatchRequestDto(fileIds: [new BaseBatchRequestDtoAllOfFileIds(file.Id)]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task CreateThumbnails_GuestWithReadAccess_CanCreateThumbnails()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Thumbnails Guest Room");
        var file = await CreateFile("Autotest Thumbnails Guest File", room.Id);

        var guest = await InviteGuest();
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = guest.Id, Access = FileShare.Read }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(guest);

        // Act
        var result = (await _filesApi.CreateThumbnailsWithHttpInfoAsync(
            new BaseBatchRequestDto(fileIds: [new BaseBatchRequestDtoAllOfFileIds(file.Id)]),
            TestContext.Current.CancellationToken)).Data;

        // Assert
        Ids(result).Should().Contain(file.Id);
    }

    [Fact]
    public async Task CreateThumbnails_UserWithReadAccess_CanCreateThumbnails()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Thumbnails Read Room");
        var file = await CreateFile("Autotest Thumbnails Read File", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.Read }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);

        // Act
        var result = (await _filesApi.CreateThumbnailsWithHttpInfoAsync(
            new BaseBatchRequestDto(fileIds: [new BaseBatchRequestDtoAllOfFileIds(file.Id)]),
            TestContext.Current.CancellationToken)).Data;

        // Assert
        Ids(result).Should().Contain(file.Id);
    }

    /// <summary>
    /// The TS source grants <c>RoomManager</c> to a plain <c>User</c> invitation, which
    /// <c>FileSecurity.AvailableRoomAccesses</c> rejects — only a <c>RoomAdmin</c> can be granted
    /// <c>RoomManager</c>. Ported with a <c>RoomAdmin</c> invitee instead, keeping the intent: a
    /// room manager can create thumbnails for files in their room.
    /// </summary>
    [Fact]
    public async Task CreateThumbnails_RoomAdminWithRoomManagerAccess_CanCreateThumbnails()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Thumbnails Manager Room");

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = roomAdmin.Id, Access = FileShare.RoomManager }]
        }, TestContext.Current.CancellationToken);

        var file = await CreateFile("Autotest Thumbnails Manager File", room.Id);

        await _filesClient.Authenticate(roomAdmin);

        // Act
        var result = (await _filesApi.CreateThumbnailsWithHttpInfoAsync(
            new BaseBatchRequestDto(fileIds: [new BaseBatchRequestDtoAllOfFileIds(file.Id)]),
            TestContext.Current.CancellationToken)).Data;

        // Assert
        Ids(result).Should().Contain(file.Id);
    }

    [Fact]
    public async Task CreateThumbnails_DocSpaceAdmin_CanCreateThumbnails()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        var room = await CreateCustomRoom("Autotest Thumbnails Admin Room");
        var file = await CreateFile("Autotest Thumbnails Admin File", room.Id);

        // Act
        var result = (await _filesApi.CreateThumbnailsWithHttpInfoAsync(
            new BaseBatchRequestDto(fileIds: [new BaseBatchRequestDtoAllOfFileIds(file.Id)]),
            TestContext.Current.CancellationToken)).Data;

        // Assert
        Ids(result).Should().Contain(file.Id);
    }

    /// <summary>
    /// <see cref="ObjectArrayWrapper.Response" /> is untyped (<c>List&lt;object&gt;</c>); the file
    /// IDs in it deserialize as boxed <see cref="long" />, not <see cref="int" />, so callers must
    /// normalize before comparing against an <see cref="int" /> file ID.
    /// </summary>
    private static List<long> Ids(ObjectArrayWrapper result) =>
        result.Response?.Select(Convert.ToInt64).ToList() ?? [];
}
