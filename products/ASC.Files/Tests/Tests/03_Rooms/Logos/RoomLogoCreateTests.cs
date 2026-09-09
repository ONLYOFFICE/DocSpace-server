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

namespace ASC.Files.Tests.Tests._03_Rooms.Logos;

/// <summary>
/// <c>POST /files/rooms/{id}/logo</c> — applying an uploaded temporary image as a room's logo.
/// Access control lives in <see cref="RoomLogoCreatePermissionsTests"/>; tmpFile/id/crop validation
/// in <see cref="RoomLogoCreateValidationTests"/>; consistency across the room lifecycle in
/// <see cref="RoomLogoLifecycleTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomLogoCreateTests(
    AspireAppFixture fixture)
    : RoomLogoTestBase(fixture)
{
    [Fact]
    public async Task CreateLogo_FromUploadedImage_Applied()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Create Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var updated = await CreateLogo(room.Id, tmpFile);

        // Assert
        updated.Id.Should().Be(room.Id);
        updated.Logo.Original.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateLogo_PrivateRoom_Applied()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePrivateRoom("Autotest Logo Private Room", RoomType.CustomRoom);
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var updated = await CreateLogo(room.Id, tmpFile);

        // Assert
        updated.Id.Should().Be(room.Id);
        updated.Private.Should().BeTrue();
        updated.Logo.Original.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateLogo_HasAllSizeUrls()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Sizes Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var updated = await CreateLogo(room.Id, tmpFile);

        // Assert
        updated.Logo.Original.Should().NotBeNullOrEmpty();
        updated.Logo.Large.Should().NotBeNullOrEmpty();
        updated.Logo.Medium.Should().NotBeNullOrEmpty();
        updated.Logo.Small.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateLogo_WithCropParameters_Applied()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Crop Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var updated = await CreateLogo(room.Id, tmpFile, x: 0, y: 0, width: 1, height: 1);

        // Assert
        updated.Logo.Original.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateLogo_NonExistentRoom_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateLogo(999999999, tmpFile));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateLogo_ArchivedRoom_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Archived Room");
        await ArchiveRoom(room.Id);
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateLogo(room.Id, tmpFile));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateLogo_InvalidTmpFile_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Invalid TmpFile Room");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateLogo(room.Id, "/non/existent/path/fake.png"));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateLogo_RoomTemplate_Applied()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Template Room");

        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, "Autotest Logo Template"),
            TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var updated = await CreateLogo(templateId, tmpFile);

        // Assert
        updated.Logo.Original.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateLogo_VisibleInRoomInfoWithSameUrls()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo GetInfo Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());
        var created = await CreateLogo(room.Id, tmpFile);

        // Act
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Logo.Original.Should().Contain("/storage/room_logos/");
        PathOf(info.Logo.Original).Should().Be(PathOf(created.Logo.Original));
        PathOf(info.Logo.Large).Should().Be(PathOf(created.Logo.Large));
        PathOf(info.Logo.Medium).Should().Be(PathOf(created.Logo.Medium));
        PathOf(info.Logo.Small).Should().Be(PathOf(created.Logo.Small));
    }

    [Fact]
    public async Task CreateLogo_Replace_ChangesOriginalUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Replace Room");

        var firstTmpFile = await UploadLogo(CreateTestImageBytes());
        var first = await CreateLogo(room.Id, firstTmpFile);

        var secondTmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var second = await CreateLogo(room.Id, secondTmpFile);

        // Assert
        second.Logo.Original.Should().NotBeNullOrEmpty();
        second.Logo.Original.Should().NotBe(first.Logo.Original);
    }

    private static string? PathOf(string? url) => url?.Split('?')[0];
}
