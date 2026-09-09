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

namespace ASC.Files.Tests.Tests._03_Rooms.Covers;

/// <summary>
/// PUT /files/rooms/{id}/cover — applying a cover from the gallery. Access control for the same
/// endpoint lives in <c>Permissions/RoomCoverPermissionsTests</c>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomCoverChangeTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task ChangeCover_WithCoverAndColor_Applied()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom("Autotest Cover Room");

        // Act
        var updated = (await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto("FF5733", coverId),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Id.Should().Be(room.Id);
        updated.Logo.Cover.Id.Should().Be(coverId);
        updated.Logo.Color.Should().Be("FF5733");
    }

    [Fact]
    public async Task ChangeCover_ReturnsFullUpdatedRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom("Autotest Cover Full Response Room");

        // Act
        var updated = (await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto("AB12CD", coverId),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Id.Should().Be(room.Id);
        updated.Title.Should().Be("Autotest Cover Full Response Room");
        updated.RoomType.Should().Be(RoomType.CustomRoom);
        updated.Logo.Cover.Id.Should().Be(coverId);
        updated.Logo.Cover.Data.Should().NotBeNullOrEmpty();
        updated.Logo.Color.Should().Be("AB12CD");
    }

    [Fact]
    public async Task ChangeCover_ColorOnly_Applied()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Color Only Room");

        // Act
        var updated = (await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto("AABBCC"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Logo.Color.Should().Be("AABBCC");
    }

    [Fact]
    public async Task ChangeCover_CoverOnly_Applied()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom("Autotest Cover No Color Room");

        // Act
        var updated = (await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto(cover: coverId),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Logo.Cover.Id.Should().Be(coverId);
    }

    [Fact]
    public async Task ChangeCover_ReflectsInRoomInfo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom("Autotest Cover Verify Room");

        // Act
        await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto("CC3300", coverId),
            TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Logo.Cover.Id.Should().Be(coverId);
        info.Logo.Color.Should().Be("CC3300");
    }

    [Fact]
    public async Task ChangeCover_CoverDataPayload_IsNotEmpty()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom("Autotest Cover Data Payload Room");

        // Act
        var updated = (await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto("FF5733", coverId),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Logo.Cover.Data.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ChangeCover_Twice_LastOneWins()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var covers = (await _roomsApi.GetRoomCoversAsync(TestContext.Current.CancellationToken)).Response;
        var room = await CreateCustomRoom("Autotest Cover Multiple Changes Room");

        // Act
        var first = (await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto("111111", covers[0].Id),
            TestContext.Current.CancellationToken)).Response;

        var second = (await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto("222222", covers[1].Id),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        first.Logo.Cover.Id.Should().Be(covers[0].Id);
        first.Logo.Color.Should().Be("111111");
        second.Logo.Cover.Id.Should().Be(covers[1].Id);
        second.Logo.Color.Should().Be("222222");
    }

    [Fact]
    public async Task ChangeCover_SamePayloadTwice_IsIdempotent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom("Autotest Cover Idempotent Room");

        // Act
        var first = (await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto("ABCDEF", coverId),
            TestContext.Current.CancellationToken)).Response;

        var second = (await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto("ABCDEF", coverId),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        first.Logo.Cover.Id.Should().Be(coverId);
        second.Logo.Cover.Id.Should().Be(coverId);
        second.Logo.Color.Should().Be("ABCDEF");
    }

    [Fact]
    public async Task ChangeCover_EveryGalleryCover_CanBeApplied()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var covers = (await _roomsApi.GetRoomCoversAsync(TestContext.Current.CancellationToken)).Response;
        var room = await CreateCustomRoom("Autotest Cover Multi Apply Room");

        // Act & Assert
        foreach (var cover in covers.Take(3))
        {
            var updated = (await _roomsApi.ChangeRoomCoverAsync(
                room.Id,
                new CoverRequestDto(cover: cover.Id),
                TestContext.Current.CancellationToken)).Response;

            updated.Logo.Cover.Id.Should().Be(cover.Id);
        }
    }

    /// <summary>A room saved as a template keeps its own cover, changeable the same way.</summary>
    [Fact]
    public async Task ChangeCover_RoomTemplate_Applied()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom("Autotest Cover Template Room");

        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Cover Template"),
            TestContext.Current.CancellationToken);

        var templateId = await WaitForRoomTemplate();

        // Act
        var updated = (await _roomsApi.ChangeRoomCoverAsync(
            templateId,
            new CoverRequestDto("FF5733", coverId),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Logo.Cover.Id.Should().Be(coverId);
        updated.Logo.Color.Should().Be("FF5733");
    }

    [Theory]
    [InlineData(RoomType.CustomRoom)]
    [InlineData(RoomType.EditingRoom)]
    [InlineData(RoomType.PublicRoom)]
    [InlineData(RoomType.FillingFormsRoom)]
    public async Task ChangeCover_EveryRoomType_Applied(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();

        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto($"Autotest Cover {roomType}", roomType: roomType),
            TestContext.Current.CancellationToken)).Response;

        // Act
        var updated = (await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto("FF5733", coverId),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Logo.Cover.Id.Should().Be(coverId);
        updated.Logo.Color.Should().Be("FF5733");
    }

    [Fact]
    public async Task ChangeCover_PrivateRoom_Applied()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();

        var room = await CreatePrivateRoom("Autotest Cover Private Room", RoomType.CustomRoom);

        // Act
        var updated = (await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto("FF5733", coverId),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Private.Should().BeTrue();
        updated.Logo.Cover.Id.Should().Be(coverId);
        updated.Logo.Color.Should().Be("FF5733");
    }

    [Fact]
    public async Task ChangeCover_NonExistentRoom_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ChangeRoomCoverAsync(
                999999999,
                new CoverRequestDto("FF0000"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task ChangeCover_DeletedRoom_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Cover Deleted Room");

        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ChangeRoomCoverAsync(
                room.Id,
                new CoverRequestDto("FF0000"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task ChangeCover_ArchivedRoom_ForbiddenAndCoverKept()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom("Autotest Cover Archived Room");

        await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto("ABCDEF", coverId),
            TestContext.Current.CancellationToken);

        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ChangeRoomCoverAsync(
                room.Id,
                new CoverRequestDto("FF0000"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Logo.Cover.Id.Should().Be(coverId);
        info.Logo.Color.Should().Be("ABCDEF");
    }

    [Fact]
    public async Task ChangeCover_SurvivesArchiveUnarchiveCycle()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom("Autotest Cover Archive Cycle Room");

        await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto("1A2B3C", coverId),
            TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Logo.Cover.Id.Should().Be(coverId);
        info.Logo.Color.Should().Be("1A2B3C");
    }

}
