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

namespace ASC.Files.Tests.Tests._03_Rooms.Update;

/// <summary>
/// PUT /files/rooms/{id} - core update behavior: single-field updates, the multi-field case, and
/// the guarantees that make the endpoint safe to call repeatedly (idempotency, last-write-wins,
/// response shape, and that unrelated room state - members, pin, owner - is left untouched).
/// </summary>
[Trait("Category", "Rooms")]
public class RoomUpdateTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task UpdateRoom_Title_UpdatesAndPersists()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room Before Update");

        // Act
        var updated = (await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest("Autotest Room After Update"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Title.Should().Be("Autotest Room After Update");
        updated.Id.Should().Be(room.Id);

        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be("Autotest Room After Update");
    }

    [Fact]
    public async Task UpdateRoom_AllAllowedFieldsForVdrRoom_UpdatesAndPersists()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom("Autotest VDR Room");

        // Act
        var updated = (await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(
                title: "Updated VDR Room",
                indexing: true,
                denyDownload: true,
                lifetime: new RoomDataLifetimeDto(deletePermanently: true, period: RoomDataLifetimePeriod.Day, value: 30, enabled: true),
                watermark: new WatermarkRequestDto(enabled: true, additions: WatermarkAdditions.UserName, text: "Confidential", rotate: 0, imageScale: 100),
                color: "FF5733"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Title.Should().Be("Updated VDR Room");
        updated.Indexing.Should().BeTrue();
        updated.DenyDownload.Should().BeTrue();
        updated.Logo.Color.Should().Be("FF5733");
        updated.Lifetime.Period.Should().Be(RoomDataLifetimePeriod.Day);
        updated.Lifetime.Value.Should().Be(30);
        updated.Lifetime.DeletePermanently.Should().BeTrue();
        updated.Watermark.Additions.Should().Be(WatermarkAdditions.UserName);
        updated.Watermark.Text.Should().Be("Confidential");
        updated.Watermark.Rotate.Should().Be(0);
        updated.Watermark.ImageScale.Should().Be(100);

        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be("Updated VDR Room");
        info.Indexing.Should().BeTrue();
        info.DenyDownload.Should().BeTrue();
        info.Logo.Color.Should().Be("FF5733");
        info.Lifetime.Period.Should().Be(RoomDataLifetimePeriod.Day);
        info.Lifetime.Value.Should().Be(30);
        info.Watermark.Additions.Should().Be(WatermarkAdditions.UserName);
        info.Watermark.Text.Should().Be("Confidential");
    }

    [Fact]
    public async Task UpdateRoom_EmptyTitle_KeepsOriginalTitle()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room");

        // Act
        var updated = (await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(""),
            TestContext.Current.CancellationToken)).Response;

        // Assert - the API ignores an empty title and keeps the original value
        updated.Title.Should().Be("Autotest Room");
    }

    [Fact]
    public async Task UpdateRoom_NonExistentRoom_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert - room ids are globally unique, so the API returns 403 instead of 404 to
        // avoid letting a caller enumerate which ids exist.
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(
                999999999,
                new UpdateRoomRequest("Does Not Exist"),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateRoom_MultipleFieldsInOneRequest_UpdatesAll()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom("Autotest Multi Field");

        // Act
        var updated = (await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(
                title: "Autotest Multi Field Updated",
                denyDownload: true,
                tags: ["AutotestMultiTag"],
                color: "123ABC",
                cover: coverId),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Title.Should().Be("Autotest Multi Field Updated");

        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be("Autotest Multi Field Updated");
        info.Tags.Should().Contain("AutotestMultiTag");
        info.Logo.Color.Should().Be("123ABC");
        info.Logo.Cover.Id.Should().Be(coverId);
        info.DenyDownload.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateRoom_ReapplyingSameValues_IsIdempotent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Idempotent");
        var body = new UpdateRoomRequest(
            title: "Autotest Idempotent Final",
            tags: ["AutotestIdemTag"],
            color: "ABCDEF");

        // Act
        var first = await _roomsApi.UpdateRoomAsync(room.Id, body, TestContext.Current.CancellationToken);
        var second = await _roomsApi.UpdateRoomAsync(room.Id, body, TestContext.Current.CancellationToken);

        // Assert
        first.Response.Title.Should().Be("Autotest Idempotent Final");
        second.Response.Title.Should().Be("Autotest Idempotent Final");

        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be("Autotest Idempotent Final");
        info.Tags.Should().Equal("AutotestIdemTag");
        info.Logo.Color.Should().Be("ABCDEF");
    }

    [Fact]
    public async Task UpdateRoom_SequentialUpdates_LastWriteWins()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Sequential");

        // Act
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest("Autotest Sequential First"), TestContext.Current.CancellationToken);
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest("Autotest Sequential Second"), TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be("Autotest Sequential Second");
    }

    [Fact]
    public async Task UpdateRoom_SuccessfulResponse_MatchesFolderIntegerShape()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Shape");

        // Act
        var updated = (await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest("Autotest Shape Updated"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Should().NotBeNull();
        updated.Id.Should().Be(room.Id);
        updated.Title.Should().Be("Autotest Shape Updated");
    }

    [Fact]
    public async Task UpdateRoom_DoesNotResetMembersPinOrOwner()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Side Effects");

        var member = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = member.Id, Access = FileShare.Read }], Notify = false },
            TestContext.Current.CancellationToken);
        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        var before = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        var ownerId = before.CreatedBy.Id;

        // Act
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest("Autotest Side Effects Updated"), TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Id.Should().Be(room.Id);
        info.Pinned.Should().BeTrue();
        info.CreatedBy.Id.Should().Be(ownerId);

        var security = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        security.Should().Contain(s => s.SharedToUser.Id == member.Id);
    }
}
