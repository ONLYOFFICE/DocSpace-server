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

namespace ASC.Files.Tests.Tests._03_Rooms;

/// <summary>
/// Verifies that form filling rooms are surfaced exclusively in the dedicated "Forms" section
/// and no longer appear in the "Rooms" (Virtual Rooms) section.
///
/// Form filling rooms physically live under the VirtualRooms tree, but they are split by the room's
/// own folder type at query time: excluded from the Active rooms listing and shown only when the
/// "Forms" section is requested (SearchArea.Forms / the @forms endpoint). Because the split is based
/// purely on <see cref="FolderType.FillingFormsRoom"/>, this behavior applies equally to rooms created
/// before the "Forms" section was introduced and to newly created ones.
/// </summary>
[Collection("Test Collection")]
[Trait("Category", "Rooms")]
public class FormFillingRoomSectionTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task CreateFillingFormsRoom_DoesNotAppearInVirtualRoomsSection()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var roomTitle = "Form Room " + Guid.NewGuid().ToString()[..8];

        // Act
        var formRoom = await CreateFillingFormsRoom(roomTitle);

        var virtualRooms = (await _roomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.Active,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        formRoom.Should().NotBeNull();
        virtualRooms.Folders.Should().NotContain(r => r.Title == formRoom.Title,
            "form filling rooms must no longer be listed in the Virtual Rooms section");
    }

    [Fact]
    public async Task CreateFillingFormsRoom_AppearsInFormsSection()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var roomTitle = "Form Room " + Guid.NewGuid().ToString()[..8];

        // Act
        var formRoom = await CreateFillingFormsRoom(roomTitle);

        var formsSection = (await _roomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.Forms,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        formRoom.Should().NotBeNull();
        formsSection.Folders.Should().Contain(r => r.Title == formRoom.Title,
            "form filling rooms must be listed in the dedicated Forms section");
    }

    [Fact]
    public async Task GetFormsFolder_ReturnsOnlyFormFillingRooms()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);

        var formRoom = await CreateFillingFormsRoom("Form Room " + Guid.NewGuid().ToString()[..8]);
        var customRoom = await CreateCustomRoom("Custom Room " + Guid.NewGuid().ToString()[..8]);

        // Act - query the "Forms" root section (api/2.0/files/@forms)
        var formsFolder = (await _foldersApi.GetFormsFolderAsync(
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        formsFolder.Should().NotBeNull();
        formsFolder.Current.RootFolderType.Should().Be(FolderType.Forms);
        formsFolder.Folders.Should().Contain(r => r.Title == formRoom.Title,
            "the Forms section must contain form filling rooms");
        formsFolder.Folders.Should().NotContain(r => r.Title == customRoom.Title,
            "the Forms section must not contain non-form rooms");
    }

    [Fact]
    public async Task NonFormRoom_AppearsInVirtualRooms_ButNotInFormsSection()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var roomTitle = "Custom Room " + Guid.NewGuid().ToString()[..8];

        // Act
        var customRoom = await CreateCustomRoom(roomTitle);

        var virtualRooms = (await _roomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.Active,
            cancellationToken: TestContext.Current.CancellationToken)).Response;
        var formsSection = (await _roomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.Forms,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        virtualRooms.Folders.Should().Contain(r => r.Title == customRoom.Title,
            "regular rooms remain visible in the Virtual Rooms section");
        formsSection.Folders.Should().NotContain(r => r.Title == customRoom.Title,
            "regular rooms must not leak into the Forms section");
    }

    [Fact]
    public async Task FormAndRegularRooms_AreSplitBetweenSections()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);

        var formRoom = await CreateFillingFormsRoom("Form Room " + Guid.NewGuid().ToString()[..8]);
        var customRoom = await CreateCustomRoom("Custom Room " + Guid.NewGuid().ToString()[..8]);
        var publicRoom = await CreatePublicRoom("Public Room " + Guid.NewGuid().ToString()[..8]);

        // Act
        var virtualRooms = (await _roomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.Active,
            cancellationToken: TestContext.Current.CancellationToken)).Response;
        var formsSection = (await _roomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.Forms,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert - the form room is only in Forms
        formsSection.Folders.Should().Contain(r => r.Title == formRoom.Title);
        virtualRooms.Folders.Should().NotContain(r => r.Title == formRoom.Title);

        // Assert - the non-form rooms are only in Virtual Rooms
        virtualRooms.Folders.Should().Contain(r => r.Title == customRoom.Title);
        virtualRooms.Folders.Should().Contain(r => r.Title == publicRoom.Title);
        formsSection.Folders.Should().NotContain(r => r.Title == customRoom.Title);
        formsSection.Folders.Should().NotContain(r => r.Title == publicRoom.Title);
    }

    [Fact]
    public async Task FilteringRoomsByFillingFormsRoomType_InVirtualRooms_ReturnsNothing()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var formRoom = await CreateFillingFormsRoom("Form Room " + Guid.NewGuid().ToString()[..8]);

        // Act - explicitly asking for form filling rooms within the Virtual Rooms section
        var virtualRooms = (await _roomsApi.GetRoomsFolderAsync(
            type: [RoomType.FillingFormsRoom],
            searchArea: SearchArea.Active,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        virtualRooms.Folders.Should().NotContain(r => r.Title == formRoom.Title,
            "form filling rooms must not be reachable through the Virtual Rooms section even when filtered by their type");
    }

    [Fact]
    public async Task RootFolders_ContainDedicatedFormsSection()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);

        // Act
        var rootFolders = (await _foldersApi.GetRootFoldersAsync(
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        rootFolders.Should().Contain(
            r => r.Current.RootFolderType.HasValue && r.Current.RootFolderType.Value == FolderType.Forms,
            "the Forms section is exposed as a dedicated root folder");
    }
}
