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

namespace ASC.Files.Tests.Tests._06_Operations.Favorites;

/// <summary>
/// <c>POST /api/2.0/files/favorites</c> and <c>DELETE /api/2.0/files/favorites</c> — favorites are
/// personal. Another member is unaffected, items the caller cannot read or that do not exist are
/// skipped with <c>true</c> as the answer, a marked item in the Trash keeps its mark and is hidden
/// until it is restored, and removing a mark deletes nothing.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class FavoritesPersonalTests(
    AspireAppFixture fixture)
    : FavoritesOperationsTestBase(fixture)
{
    private async Task<bool> IsFavorite(string title, bool expected)
    {
        var favorites = await PollFavorites(f => f.Files.Any(x => x.Title == title) == expected);

        return favorites.Files.Any(x => x.Title == title);
    }

    [Fact]
    public async Task Favorites_ArePersonal_EachMemberHasTheirOwnListAndRemovingAMarkLeavesTheOthers()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Favorites Personal Room");
        var first = await InviteMember(EmployeeType.User);
        var second = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, first, FileShare.Read);
        await InviteToRoom(room.Id, second, FileShare.Read);
        var content = OperationFileBytes.Make(90, 301);
        const string title = "Autotest Favorites Shared.bin";
        var file = (await OperationFileBytes.UploadAsync(
            _filesOperationsApi, room.Id, title, content, true, TestContext.Current.CancellationToken)).File;

        // Act & Assert
        await _filesClient.Authenticate(first);
        (await AddFilesToFavorites(file.Id)).Should().BeTrue();
        (await IsFavorite(title, true)).Should().BeTrue();

        await _filesClient.Authenticate(second);
        (await IsFavorite(title, false)).Should().BeFalse("the other member's mark is not mine");
        (await AddFilesToFavorites(file.Id)).Should().BeTrue();
        (await IsFavorite(title, true)).Should().BeTrue();

        await _filesClient.Authenticate(Owner);
        (await IsFavorite(title, false)).Should().BeFalse();

        await _filesClient.Authenticate(first);
        (await RemoveFilesFromFavorites(file.Id)).Should().BeTrue();
        (await IsFavorite(title, false)).Should().BeFalse();

        // Removing again, by the member who no longer has it, takes nothing from the other.
        (await RemoveFilesFromFavorites(file.Id)).Should().BeTrue();
        await _filesClient.Authenticate(second);
        (await IsFavorite(title, true)).Should().BeTrue();

        // The mark was never the file: it is where it was, with its content.
        await _filesClient.Authenticate(Owner);
        (await GetFile(file.Id)).FolderId.Should().Be(room.Id);
        (await OperationFileBytes.DownloadAsync(_filesClient, file.Id, null, TestContext.Current.CancellationToken)).Should().Equal(content);
    }

    [Fact]
    public async Task Favorites_FileTheCallerCannotReadAndAnIdThatDoesNotExist_AreSkippedWithATrueAnswer()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var room = await CreateCustomRoom("Autotest Favorites Mixed Room");
        var reader = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, reader, FileShare.Read);
        const string readableTitle = "Autotest Favorites Readable.bin";
        var readable = (await OperationFileBytes.UploadAsync(
            _filesOperationsApi, room.Id, readableTitle, OperationFileBytes.Make(10, 321), true, TestContext.Current.CancellationToken)).File;
        var secret = (await OperationFileBytes.UploadAsync(
            _filesOperationsApi, myDocsFolderId, "Autotest Favorites Secret.bin", OperationFileBytes.Make(11, 322), true, TestContext.Current.CancellationToken)).File;

        await _filesClient.Authenticate(reader);

        // Act
        var added = await AddFilesToFavorites(readable.Id, secret.Id, 99999999);

        // Assert
        added.Should().BeTrue();
        var favorites = await PollFavorites(f => f.Files.Any(x => x.Title == readableTitle));
        favorites.Files.Select(f => f.Title).Should().Equal(readableTitle);

        // And the other way round: removing the same mixed set is just as harmless.
        (await RemoveFilesFromFavorites(readable.Id, secret.Id, 99999999)).Should().BeTrue();
        (await PollFavorites(f => f.Files.Count == 0)).Files.Should().BeEmpty();
    }

    [Fact]
    public async Task Favorites_MarkedFileInTrash_IsLeftOutOfTheListAndComesBackWhenRestored()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        const string title = "Autotest Favorites Trash.bin";
        var content = OperationFileBytes.Make(50, 331);
        var file = (await OperationFileBytes.UploadAsync(
            _filesOperationsApi, myDocsFolderId, title, content, true, TestContext.Current.CancellationToken)).File;
        await AddFilesToFavorites(file.Id);
        (await IsFavorite(title, true)).Should().BeTrue();

        // Act
        await DeleteFileToTrash(file.Id);

        // Assert
        (await IsFavorite(title, false)).Should().BeFalse("a file in the Trash is hidden from the favorites");

        var restored = (await _filesOperationsApi.MoveBatchItemsAsync(
            new BatchRequestDto
            {
                FileIds = [new BatchRequestDtoAllOfFileIds(file.Id)],
                FolderIds = [],
                DestFolderId = new BatchRequestDtoAllOfDestFolderId(myDocsFolderId),
                DeleteAfter = false,
                ReturnSingleOperation = true
            },
            TestContext.Current.CancellationToken)).Response;
        await WaitLongOperation(restored.FirstOrDefault()?.Id);

        (await IsFavorite(title, true)).Should().BeTrue("the mark survives the Trash");
        (await OperationFileBytes.DownloadAsync(_filesClient, file.Id, null, TestContext.Current.CancellationToken)).Should().Equal(content);
    }
}
