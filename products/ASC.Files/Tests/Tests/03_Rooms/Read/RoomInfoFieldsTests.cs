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

namespace ASC.Files.Tests.Tests._03_Rooms.Read;

/// <summary>
/// GET /files/rooms/:id - fields that only apply to some room types (VDR flags, lifetime,
/// watermark) and fields set at create time that must come back unchanged (cover, color, tags).
/// </summary>
[Trait("Category", "Rooms")]
public class RoomInfoFieldsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetRoomInfo_VdrCreatedWithoutLifetime_HasUnsetLifetime()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom("Autotest VDR No Lifetime");

        // Act
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        (info.Lifetime?.Value ?? 0).Should().Be(0);
    }

    [Fact]
    public async Task GetRoomInfo_CoverSetAtCreate_IsReflected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();

        var created = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Cover On Create", roomType: RoomType.CustomRoom, cover: coverId),
            TestContext.Current.CancellationToken)).Response;

        // Act
        var info = (await _roomsApi.GetRoomInfoAsync(created.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Logo?.Cover?.Id.Should().Be(coverId);
    }

    [Fact]
    public async Task GetRoomInfo_ColorSetAtCreate_IsReflected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var created = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Color On Create", roomType: RoomType.CustomRoom, color: "FF5733"),
            TestContext.Current.CancellationToken)).Response;

        // Act
        var info = (await _roomsApi.GetRoomInfoAsync(created.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Logo?.Color.Should().Be("FF5733");
    }

    [Fact]
    public async Task GetRoomInfo_TagsAddedAfterCreate_AppearInResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var tagA = "AutotestGetInfoTagA" + Guid.NewGuid().ToString()[..8];
        var tagB = "AutotestGetInfoTagB" + Guid.NewGuid().ToString()[..8];
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagA), TestContext.Current.CancellationToken);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagB), TestContext.Current.CancellationToken);

        var room = await CreateCustomRoom("Autotest Tags On GetInfo");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagA, tagB]), TestContext.Current.CancellationToken);

        // Act
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Tags.Should().Contain([tagA, tagB]);
    }

    [Fact]
    public async Task GetRoomInfo_CustomRoom_DoesNotExposeVdrOnlyFlags()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest GetInfo Custom");

        // Act
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.RoomType.Should().Be(RoomType.CustomRoom);
        info.Indexing.Should().NotBe(true);
        info.DenyDownload.Should().NotBe(true);
        (info.Lifetime?.Value ?? 0).Should().Be(0);
        (info.Watermark?.Text ?? "").Should().BeEmpty();
    }

    [Fact]
    public async Task GetRoomInfo_Vdr_ExposesVdrSpecificFlagsAfterCreate()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var created = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest GetInfo VDR", roomType: RoomType.VirtualDataRoom, indexing: true, denyDownload: true),
            TestContext.Current.CancellationToken)).Response;

        // Act
        var info = (await _roomsApi.GetRoomInfoAsync(created.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.RoomType.Should().Be(RoomType.VirtualDataRoom);
        info.Indexing.Should().BeTrue();
        info.DenyDownload.Should().BeTrue();
    }

    [Fact]
    public async Task GetRoomInfo_PublicRoom_HasNoVdrFlags()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest GetInfo Public");

        // Act
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.RoomType.Should().Be(RoomType.PublicRoom);
        info.Indexing.Should().NotBe(true);
        info.DenyDownload.Should().NotBe(true);
    }

    [Fact]
    public async Task GetRoomInfo_FillingFormsRoom_HasCorrectType()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest GetInfo FormFilling");

        // Act
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.RoomType.Should().Be(RoomType.FillingFormsRoom);
        info.Indexing.Should().NotBe(true);
        info.DenyDownload.Should().NotBe(true);
    }

    [Fact]
    public async Task GetRoomInfo_EditingRoom_HasCorrectType()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCollaborationRoom("Autotest GetInfo Editing");

        // Act
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.RoomType.Should().Be(RoomType.EditingRoom);
        info.Indexing.Should().NotBe(true);
        info.DenyDownload.Should().NotBe(true);
    }
}
