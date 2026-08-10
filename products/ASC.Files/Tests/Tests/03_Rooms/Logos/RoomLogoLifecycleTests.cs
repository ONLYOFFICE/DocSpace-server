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

/// <summary>File-lifecycle and cross-room consistency of a room logo created via <c>POST /files/rooms/{id}/logo</c>.</summary>
[Trait("Category", "Rooms")]
public class RoomLogoLifecycleTests(
    AspireAppFixture fixture)
    : RoomLogoTestBase(fixture)
{
    [Fact]
    public async Task CreateLogo_TmpFileReusedForAnotherRoom_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomA = await CreateCustomRoom("Autotest Logo Reuse Room A");
        var roomB = await CreateCustomRoom("Autotest Logo Reuse Room B");
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        await CreateLogo(roomA.Id, tmpFile);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateLogo(roomB.Id, tmpFile));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    [Trait("Bug", "81679")]
    public async Task CreateLogo_NonImageContentAsTmpFile_ShouldBeForbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Non Image Room");

        var result = (await _roomsApi.UploadRoomLogoAsync(
            new FileParameter("logo.png", "image/png", new MemoryStream(Encoding.UTF8.GetBytes("this is not a valid image"))),
            TestContext.Current.CancellationToken)).Response;
        var tmpFile = result.Data?.ToString() ?? string.Empty;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateLogo(room.Id, tmpFile));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateLogo_DoesNotModifyOtherRoomMetadata()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Metadata Room");
        var before = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        await CreateLogo(room.Id, tmpFile);

        // Assert
        var after = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        after.Title.Should().Be(before.Title);
        after.RoomType.Should().Be(before.RoomType);
        after.Access.Should().Be(before.Access);
        (after.Tags ?? []).Should().BeEquivalentTo(before.Tags ?? []);
    }

    [Fact]
    public async Task CreateLogo_SurvivesArchiveUnarchiveCycle()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Archive Cycle Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());
        var created = await CreateLogo(room.Id, tmpFile);
        var originalPath = created.Logo.Original.Split('?')[0];

        // Act
        await ArchiveRoom(room.Id);
        await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        var after = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        after.Logo.Original.Should().Contain("/storage/room_logos/");
        after.Logo.Original.Split('?')[0].Should().Be(originalPath);
    }

    [Fact]
    public async Task CreateLogo_UrlsStableAcrossRepeatedGetRoomInfoCalls()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Stable URLs Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());
        await CreateLogo(room.Id, tmpFile);

        // Act
        var first = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        var second = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        var third = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        first.Logo.Original.Should().NotBeNullOrEmpty();
        second.Logo.Original.Should().Be(first.Logo.Original);
        third.Logo.Original.Should().Be(first.Logo.Original);
        second.Logo.Large.Should().Be(first.Logo.Large);
        second.Logo.Medium.Should().Be(first.Logo.Medium);
        second.Logo.Small.Should().Be(first.Logo.Small);
    }
}
