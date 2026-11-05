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

namespace ASC.Files.Tests.Tests._03_Rooms.Instructions;

[Trait("Category", "Rooms")]
[Trait("Feature", "Instructions")]
public class SkillMoveCopyTests(AspireAppFixture fixture) : InstructionsRoomTestBase(fixture)
{
    [Fact]
    public async Task CopyFile_ValidSkill_LandsInRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Copy Valid");
        var myId = await GetUserFolderIdAsync(Owner);
        var source = await UploadAsync(myId, Skill("pdf-review"), "pdf-review.md");

        // Act
        await CopyAsync(roomId, source.Id);

        // Assert
        (await FileTitlesAsync(roomId)).Should().Equal("pdf-review.md");
        (await GetFile(source.Id)).FolderId.Should().Be(myId);
    }

    [Fact]
    public async Task CopyFile_InvalidSkill_DoesNotLandInRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Copy Invalid");
        var myId = await GetUserFolderIdAsync(Owner);
        var source = await UploadAsync(myId, Text("# No frontmatter\n"), "plain.md");

        // Act
        await CopyAsync(roomId, source.Id);

        // Assert
        (await FileTitlesAsync(roomId)).Should().BeEmpty();
    }

    [Fact]
    public async Task MoveFile_ValidSkill_LandsInRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Move Valid");
        var myId = await GetUserFolderIdAsync(Owner);
        var source = await UploadAsync(myId, Skill("pdf-review"), "pdf-review.md");

        // Act
        await MoveAsync(roomId, source.Id);

        // Assert
        (await GetFile(source.Id)).FolderId.Should().Be(roomId);
    }

    [Fact]
    public async Task MoveFile_InvalidSkill_StaysWhereItWas()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Move Invalid");
        var myId = await GetUserFolderIdAsync(Owner);
        var source = await UploadAsync(myId, Skill("pdf-review", ""), "pdf-review.md");

        // Act
        await MoveAsync(roomId, source.Id);

        // Assert
        (await GetFile(source.Id)).FolderId.Should().Be(myId);
        (await FileTitlesAsync(roomId)).Should().BeEmpty();
    }

    [Fact]
    public async Task MoveFile_NotMarkdown_StaysWhereItWas()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Move Not Markdown");
        var myId = await GetUserFolderIdAsync(Owner);
        var source = await UploadAsync(myId, Skill("pdf-review"), "pdf-review.txt");

        // Act
        await MoveAsync(roomId, source.Id);

        // Assert
        (await GetFile(source.Id)).FolderId.Should().Be(myId);
    }

    [Fact]
    public async Task MoveFile_BetweenInstructionsRooms_LandsInTarget()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var fromId = await CreateInstructionsRoomAsync("Autotest Skills Move From");
        var toId = await CreateInstructionsRoomAsync("Autotest Skills Move To");
        var file = await UploadAsync(fromId, Skill("pdf-review"), "pdf-review.md");

        // Act
        await MoveAsync(toId, file.Id);

        // Assert
        (await GetFile(file.Id)).FolderId.Should().Be(toId);
        (await FileTitlesAsync(fromId)).Should().BeEmpty();
    }

    [Fact]
    public async Task MoveFile_OutOfRoom_LandsInMyDocuments()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Move Out");
        var myId = await GetUserFolderIdAsync(Owner);
        var file = await UploadAsync(roomId, Skill("pdf-review"), "pdf-review.md");

        // Act
        await MoveAsync(myId, file.Id);

        // Assert
        (await GetFile(file.Id)).FolderId.Should().Be(myId);
        (await FileTitlesAsync(roomId)).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteFile_ThenRestore_ReturnsToRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Trash Restore");
        var trashId = await GetTrashFolderIdAsync(Owner);
        var file = await UploadAsync(roomId, Skill("pdf-review"), "pdf-review.md");
        await DeleteToTrashAsync(file.Id);

        // Act
        await MoveAsync(roomId, file.Id);

        // Assert
        (await GetFile(file.Id)).FolderId.Should().Be(roomId);
        (await FileTitlesAsync(trashId)).Should().NotContain("pdf-review.md");
    }
}
