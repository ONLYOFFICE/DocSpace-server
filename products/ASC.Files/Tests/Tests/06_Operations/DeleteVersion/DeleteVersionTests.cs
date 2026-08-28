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

namespace ASC.Files.Tests.Tests._06_Operations.DeleteVersion;

/// <summary>PUT /api/2.0/files/fileops/deleteversion - deleteFileVersions.</summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class DeleteVersionTests(
    AspireAppFixture fixture)
    : DeleteVersionTestBase(fixture)
{
    [Fact]
    public async Task DeleteVersion_SingleVersion_RemovesOnlyThatVersion()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest DelVer Single File");

        // Act
        var operation = await DeleteVersionsAndWait(file.Id, [1]);

        // Assert
        operation.Should().NotBeNull();

        var versionNumbers = await GetVersionNumbers(file.Id);
        versionNumbers.Should().NotContain(1);
        versionNumbers.Should().Contain(2);
    }

    [Fact]
    public async Task DeleteVersion_MultipleVersionsAtOnce_OnlyRemainingVersionStays()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest DelVer Multi File");
        await _filesApi.UpdateFileAsync(file.Id, new UpdateFile { LastVersion = 3 }, TestContext.Current.CancellationToken);

        // Act
        var operation = await DeleteVersionsAndWait(file.Id, [1, 2]);

        // Assert
        operation.Should().NotBeNull();

        var versionNumbers = await GetVersionNumbers(file.Id);
        versionNumbers.Should().NotContain(1);
        versionNumbers.Should().NotContain(2);
        versionNumbers.Should().Contain(3);
    }

    [Fact]
    public async Task DeleteVersion_StillAccessibleInSourceFolder_AfterDeletion()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFileWithSecondVersion("Autotest DelVer Accessible");

        // Act
        await DeleteVersionsAndWait(file.Id, [1]);

        // Assert
        var folderContent = (await _foldersApi.GetFolderByFolderIdAsync(myDocsFolderId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var titles = (folderContent.Files ?? []).Select(f => f.Title).ToList();
        titles.Should().Contain(file.Title);
    }

    [Fact]
    public async Task DeleteVersion_NonExistentVersionNumber_SilentlyIgnored()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest DelVer NonExistVer", Owner);

        // Act
        var operation = await DeleteVersionsAndWait(file.Id, [999]);

        // Assert
        operation.Should().NotBeNull();

        var versionNumbers = await GetVersionNumbers(file.Id);
        versionNumbers.Should().ContainSingle().Which.Should().Be(1);
    }

    [Fact]
    public async Task DeleteVersion_NonExistentFile_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DeleteFileVersionsAsync(
                new DeleteVersionBatchRequestDto(fileId: 999999999, versions: [1]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteVersion_FileInCustomRoom_OnlyRemainingVersionStays()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest DelVer CustomRoom");
        var file = await CreateFile("Autotest DelVer Room File", room.Id);
        await _filesApi.UpdateFileAsync(file.Id, new UpdateFile { LastVersion = 2 }, TestContext.Current.CancellationToken);

        // Act
        var operation = await DeleteVersionsAndWait(file.Id, [1]);

        // Assert
        operation.Should().NotBeNull();

        var versionNumbers = await GetVersionNumbers(file.Id);
        versionNumbers.Should().NotContain(1);
        versionNumbers.Should().Contain(2);
    }

    [Fact]
    public async Task DeleteVersion_FileInArchivedRoom_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest DelVer ArchivedRoom");
        var file = await CreateFile("Autotest DelVer Archived File", room.Id);
        await _filesApi.UpdateFileAsync(file.Id, new UpdateFile { LastVersion = 2 }, TestContext.Current.CancellationToken);

        await ArchiveRoom(room.Id);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DeleteFileVersionsAsync(
                new DeleteVersionBatchRequestDto(fileId: file.Id, versions: [1]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteVersion_FileInTrash_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest DelVer Trash File");

        var deleteResults = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FileIds = [new(file.Id)], Immediately = false },
            TestContext.Current.CancellationToken)).Response;
        await WaitLongOperation(deleteResults.FirstOrDefault()?.Id);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DeleteFileVersionsAsync(
                new DeleteVersionBatchRequestDto(fileId: file.Id, versions: [1]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteVersion_NullVersions_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest DelVer Null Versions");

        // Act
        using var response = await DeleteVersionsWithNullVersionsRaw(file.Id);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteVersion_ReturnSingleOperationTrue_VersionIsDeleted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest DelVer SingleOp File");

        // Act
        var operation = await DeleteVersionsAndWait(file.Id, [1], returnSingleOperation: true);

        // Assert
        operation.Should().NotBeNull();

        var versionNumbers = await GetVersionNumbers(file.Id);
        versionNumbers.Should().NotContain(1);
        versionNumbers.Should().Contain(2);
    }
}
