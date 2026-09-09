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

namespace ASC.Files.Tests.Tests._01_Files.Recent;

[Trait("Category", "Features")]
[Trait("Feature", "Recent")]
public class AddFileToRecentTests(
    AspireAppFixture fixture)
    : RecentTestBase(fixture)
{
    [Fact]
    public async Task AddFileToRecent_OwnerFile_ReturnsFileWithCorrectMetadata()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Recent File.docx", Owner);

        // Act
        var wrapper = await _filesApi.AddFileToRecentAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        wrapper.Response.Id.Should().Be(file.Id);
        wrapper.Response.Title.Should().Be("Autotest Recent File.docx");
        wrapper.Response.FileExst.Should().Be(".docx");
        wrapper.Response.FolderId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddFileToRecent_SameFileTwice_IsIdempotent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Recent Idempotent File.docx", Owner);
        await _filesApi.AddFileToRecentAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var wrapper = await _filesApi.AddFileToRecentAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        wrapper.Response.Id.Should().Be(file.Id);
    }

    [Fact]
    public async Task AddFileToRecent_File_AppearsInRecentSection()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Recent Check File.docx", Owner);

        // Act
        await _filesApi.AddFileToRecentAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var recent = await PollRecentUntil(r => r.Files.Any(f => f.Title == file.Title));
        recent.Files.Should().Contain(f => f.Title == file.Title);
    }

    [Fact]
    public async Task AddFileToRecent_RoomFileByUserWithReadAccess_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var user = await InviteMember(EmployeeType.User);

        var room = await CreateCustomRoom("Autotest Recent Room");
        await InviteToRoom(room.Id, user, FileShare.Read);

        var file = await CreateFile("Autotest Recent Room File.docx", room.Id);

        // Act
        await _filesClient.Authenticate(user);
        var wrapper = await _filesApi.AddFileToRecentAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        wrapper.Response.Id.Should().Be(file.Id);
        wrapper.Response.Title.Should().Be("Autotest Recent Room File.docx");
    }

    [Fact]
    public async Task AddFileToRecent_FileInRoomSubfolder_ReturnsSubfolderAndRoomOriginFields()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string roomTitle = "Autotest Recent Subfolder Room";
        var room = await CreateCustomRoom(roomTitle);

        const string subfolderTitle = "Autotest Recent Subfolder";
        var subfolder = await CreateFolder(subfolderTitle, room.Id);

        var file = await CreateFile("Autotest Recent Subfolder File.docx", subfolder.Id);

        // Act
        var wrapper = await _filesApi.AddFileToRecentAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert - folderId is the immediate parent (the subfolder), not the room root
        wrapper.Response.Id.Should().Be(file.Id);
        wrapper.Response.Title.Should().Be("Autotest Recent Subfolder File.docx");
        wrapper.Response.FolderId.Should().Be(subfolder.Id);
        wrapper.Response.OriginId.Should().Be(subfolder.Id);
        wrapper.Response.OriginTitle.Should().Be(subfolderTitle);
        wrapper.Response.OriginRoomId.Should().Be(room.Id);
        wrapper.Response.OriginRoomTitle.Should().Be(roomTitle);
    }

    [Trait("Bug", "80795")]
    [Fact]
    public async Task AddFileToRecent_NonExistentFile_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.AddFileToRecentAsync(999999999, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent?.ToString().Should().Contain("The required file was not found");
    }
}
