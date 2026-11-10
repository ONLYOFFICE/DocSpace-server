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

/// <summary>
/// <c>PUT /api/2.0/files/fileops/deleteversion</c> — what is left of the history. The listed versions
/// are removed for good; the current version is refused before anything is queued, versions that no
/// longer exist are passed over, and an EMPTY or null list is not an empty request: it deletes the
/// whole file. The tests upload known bytes per version so that "the others keep their own content"
/// can be checked, not only their numbers.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class DeleteVersionContentTests(
    AspireAppFixture fixture)
    : DeleteVersionTestBase(fixture)
{
    private async Task<(int FileId, byte[][] Contents)> CreateHistory(int folderId, string name, int versions, int seed)
    {
        var contents = Enumerable.Range(0, versions).Select(i => OperationFileBytes.Make(20 + i, seed + i)).ToArray();
        var id = await OperationFileBytes.UploadVersionsAsync(
            _filesOperationsApi, folderId, name, contents, TestContext.Current.CancellationToken);

        return (id, contents);
    }

    private Task<byte[]> Download(int fileId, int? version = null)
    {
        return OperationFileBytes.DownloadAsync(_filesClient, fileId, version, TestContext.Current.CancellationToken);
    }

    private async Task AssertNoOperationQueued()
    {
        var statuses = (await _filesOperationsApi.GetOperationStatusesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        statuses.Should().BeEmpty("a refused request must not queue anything");
    }

    [Theory]
    [InlineData(new[] { 2 }, new[] { 1, 3, 4 })]
    [InlineData(new[] { 1, 3 }, new[] { 2, 4 })]
    public async Task DeleteVersion_OldVersions_LeaveTheOthersWithTheirOwnContentAndTheFileInPlace(int[] deleted, int[] remaining)
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder($"Autotest Versions Old {string.Join('-', deleted)}", myDocsFolderId);
        var (id, contents) = await CreateHistory(dir.Id, "history.bin", 4, 201);
        (await GetVersionNumbers(id)).Order().Should().Equal(1, 2, 3, 4);

        // Act
        await DeleteVersionsAndWait(id, [.. deleted]);

        // Assert
        (await GetVersionNumbers(id)).Order().Should().Equal(remaining);

        foreach (var version in remaining)
        {
            (await Download(id, version)).Should().Equal(contents[version - 1]);
        }

        (await Download(id)).Should().Equal(contents[3]);
        (await GetFile(id)).FolderId.Should().Be(dir.Id);
    }

    [Theory]
    [InlineData(new[] { 3 })]
    [InlineData(new[] { 1, 3 })]
    public async Task DeleteVersion_CurrentVersion_IsRefusedBeforeAnythingIsQueuedAndTheHistoryIsUntouched(int[] versions)
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder($"Autotest Versions Current {string.Join('-', versions)}", myDocsFolderId);
        var (id, contents) = await CreateHistory(dir.Id, "current.bin", 3, 221);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await DeleteVersionsAndWait(id, [.. versions]));

        // Assert
        exception.ErrorCode.Should().BeInRange(400, 499);
        await AssertNoOperationQueued();
        (await GetVersionNumbers(id)).Order().Should().Equal(1, 2, 3);

        for (var version = 1; version <= 3; version++)
        {
            (await Download(id, version)).Should().Equal(contents[version - 1]);
        }
    }

    [Fact]
    public async Task DeleteVersion_TheOnlyVersion_IsTheCurrentOneAndCannotBeDeleted()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var content = OperationFileBytes.Make(33, 231);
        var upload = await OperationFileBytes.UploadAsync(
            _filesOperationsApi, myDocsFolderId, "Autotest Versions Only.bin", content, true, TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await DeleteVersionsAndWait(upload.File.Id, [1]));

        // Assert
        exception.ErrorCode.Should().BeInRange(400, 499);
        (await GetVersionNumbers(upload.File.Id)).Should().Equal(1);
        (await Download(upload.File.Id)).Should().Equal(content);
    }

    [Theory]
    [InlineData(new[] { 1 })]
    [InlineData(new[] { 99 })]
    [InlineData(new[] { 1, 99 })]
    public async Task DeleteVersion_VersionsThatDoNotExistOrWereAlreadyDeleted_ArePassedOverWithoutAComplaint(int[] versions)
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder($"Autotest Versions Twice {string.Join('-', versions)}", myDocsFolderId);
        var (id, contents) = await CreateHistory(dir.Id, "twice.bin", 3, 241);
        await DeleteVersionsAndWait(id, [1]);
        (await GetVersionNumbers(id)).Order().Should().Equal(2, 3);

        // Act
        await DeleteVersionsAndWait(id, [.. versions]);

        // Assert
        (await GetVersionNumbers(id)).Order().Should().Equal(2, 3);
        (await Download(id, 2)).Should().Equal(contents[1]);
        (await Download(id)).Should().Equal(contents[2]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteVersion_EmptyOrNullVersionList_DeletesTheWholeFileAndOnlyThatFile(bool nullList)
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder($"Autotest Versions Trap {nullList}", myDocsFolderId);
        var bystander = OperationFileBytes.Make(55, 251);
        var bystanderId = (await OperationFileBytes.UploadAsync(
            _filesOperationsApi, dir.Id, "bystander.bin", bystander, true, TestContext.Current.CancellationToken)).File.Id;
        var (victim, _) = await CreateHistory(dir.Id, "victim.bin", 2, 252);

        // Act
        if (nullList)
        {
            using var response = await DeleteVersionsWithNullVersionsRaw(victim);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            await WaitLongOperation();
        }
        else
        {
            await DeleteVersionsAndWait(victim, []);
        }

        // Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileInfoAsync(victim, cancellationToken: TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(404);

        var entries = await GetFolderFileTitles(dir.Id);
        entries.Should().Equal("bystander.bin");
        (await Download(bystanderId)).Should().Equal(bystander);
    }

    [Fact]
    public async Task DeleteVersion_VersionsOfOneFile_DoNotTouchTheVersionsOrContentOfAnother()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Versions Neighbour", myDocsFolderId);
        var (target, _) = await CreateHistory(dir.Id, "target.bin", 2, 261);
        var (neighbour, neighbourContents) = await CreateHistory(dir.Id, "neighbour.bin", 2, 271);

        // Act
        await DeleteVersionsAndWait(target, [1]);

        // Assert
        (await GetVersionNumbers(target)).Should().Equal(2);
        (await GetVersionNumbers(neighbour)).Order().Should().Equal(1, 2);
        (await Download(neighbour, 1)).Should().Equal(neighbourContents[0]);
        (await Download(neighbour, 2)).Should().Equal(neighbourContents[1]);
    }

    private async Task<List<string>> GetFolderFileTitles(int folderId)
    {
        var content = (await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return [.. (content.Files ?? []).Select(f => f.Title).Order()];
    }
}
