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

namespace ASC.Files.Tests.Tests._03_Rooms.AiHub;

[Trait("Category", "Rooms")]
[Trait("Feature", "AiHub")]
public class RoomsSkillsLifecycleTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    [Fact]
    public async Task NewVersion_ReplacesTheSkill()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();
        var fileId = await UploadSkillAsync(roomId, "pdf-review", "Reviews PDF documents");

        // Act
        var newVersionId = await UploadSkillAsync(roomId, "pdf-checker", "Checks PDF documents", "pdf-review.md");
        var result = await GetRoomsSkillsAsync(roomId);

        // Assert
        newVersionId.Should().Be(fileId);
        result.Rooms.Single().Skills.Select(s => (s.Id, s.Name, s.Description)).Should().Equal(
            (fileId.ToString(), "pdf-checker", "Checks PDF documents"));
    }

    [Fact]
    public async Task MovedIntoRoom_AppearsInCatalog()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();
        var fileId = await UploadSkillAsync(await GetUserFolderIdAsync(Owner), "pdf-review");

        // Act
        await MoveFileAsync(roomId, fileId);
        var result = await GetRoomsSkillsAsync(roomId);

        // Assert
        result.Rooms.Single().Skills.Select(s => s.Id).Should().Equal(fileId.ToString());
    }

    [Fact]
    public async Task MovedOutOfRoom_LeavesTheCatalog()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();
        var fileId = await UploadSkillAsync(roomId, "pdf-review");

        // Act
        await MoveFileAsync(await GetUserFolderIdAsync(Owner), fileId);
        var result = await GetRoomsSkillsAsync(roomId);

        // Assert
        result.Rooms.Single().Skills.Should().BeEmpty();
    }

    [Fact]
    public async Task MovedBetweenRooms_FollowsTheFile()
    {
        // Arrange
        var fromId = await CreateInstructionsRoomAsync();
        var toId = await CreateInstructionsRoomAsync();
        var fileId = await UploadSkillAsync(fromId, "pdf-review");

        // Act
        await MoveFileAsync(toId, fileId);
        var result = await GetRoomsSkillsAsync(fromId, toId);

        // Assert
        result.Rooms.Single(r => r.Id == fromId).Skills.Should().BeEmpty();
        result.Rooms.Single(r => r.Id == toId).Skills.Select(s => s.Id).Should().Equal(fileId.ToString());
    }

    [Fact]
    public async Task CopiedToAnotherRoom_AppearsInBothRooms()
    {
        // Arrange
        var fromId = await CreateInstructionsRoomAsync();
        var toId = await CreateInstructionsRoomAsync();
        var fileId = await UploadSkillAsync(fromId, "pdf-review");

        // Act
        await CopyFileAsync(toId, fileId);
        var result = await GetRoomsSkillsAsync(fromId, toId);

        // Assert
        result.Rooms.Single(r => r.Id == fromId).Skills.Select(s => s.Id).Should().Equal(fileId.ToString());
        var copy = result.Rooms.Single(r => r.Id == toId).Skills.Should().ContainSingle().Subject;
        copy.Id.Should().NotBe(fileId.ToString());
        copy.Name.Should().Be("pdf-review");
    }

    [Fact]
    public async Task MovedToTrash_LeavesTheCatalog()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();
        var fileId = await UploadSkillAsync(roomId, "pdf-review");

        // Act
        await DeleteFileAsync(fileId);
        var result = await GetRoomsSkillsAsync(roomId);

        // Assert
        result.Rooms.Single().Skills.Should().BeEmpty();
    }

    [Fact]
    public async Task RestoredFromTrash_ReturnsToTheCatalog()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();
        var fileId = await UploadSkillAsync(roomId, "pdf-review");
        await DeleteFileAsync(fileId);

        // Act
        await MoveFileAsync(roomId, fileId);
        var result = await GetRoomsSkillsAsync(roomId);

        // Assert
        result.Rooms.Single().Skills.Select(s => s.Id).Should().Equal(fileId.ToString());
    }

    [Fact]
    public async Task DeletedImmediately_LeavesTheCatalog()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();
        var fileId = await UploadSkillAsync(roomId, "pdf-review");

        // Act
        await DeleteFileAsync(fileId, immediately: true);
        var result = await GetRoomsSkillsAsync(roomId);

        // Assert
        result.Rooms.Single().Skills.Should().BeEmpty();
    }
}
