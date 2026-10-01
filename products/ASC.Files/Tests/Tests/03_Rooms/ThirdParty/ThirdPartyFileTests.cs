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

namespace ASC.Files.Tests.Tests._03_Rooms.ThirdParty;

/// <summary>
/// Files inside a third-party (Nextcloud) room through the string overloads of the SDK:
/// <c>CreateFileAsync(string)</c>, <c>CreateTextFileAsync(string)</c>, <c>UploadFileAsync(string)</c>,
/// <c>GetFileInfoAsync(string)</c>, <c>UpdateFileAsync(string)</c>, <c>DeleteFileAsync(string)</c>, and the
/// batch copy/move operations between the portal and the storage with string ids. Skipped unless
/// Nextcloud is configured in the environment.
/// </summary>
/// <remarks>
/// Every test works inside its own uniquely named folder of the room, which the base class removes
/// from the storage afterwards.
/// </remarks>
[Trait("Category", "Rooms")]
[Trait("Requires", "Nextcloud")]
[Collection("Nextcloud")]
public class ThirdPartyFileTests(
    AspireAppFixture fixture)
    : ThirdPartyTestBase(fixture)
{
    [Fact]
    public async Task CreateFile_InThirdPartyFolder_ReturnsThirdPartyFile()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (_, work) = await CreateWorkFolder("TP File Create");
        var title = $"{UniqueTitle("TP File Create")}.docx";

        // Act
        var file = await CreateThirdPartyFile(work.Id, title);

        // Assert
        file.Id.Should().NotBeNullOrEmpty();
        file.Title.Should().Be(title);
        file.FileExst.Should().Be(".docx");
        file.FolderId.Should().Be(work.Id);
        file.RootFolderType.Should().Be(FolderType.VirtualRooms);
        file.ProviderKey.Should().Be(NextcloudProviderKey);
    }

    [Fact]
    public async Task GetFileInfo_StringId_ReturnsCreatedFile()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (_, work) = await CreateWorkFolder("TP File Info");
        var created = await CreateThirdPartyFile(work.Id, $"{UniqueTitle("TP File Info")}.docx");

        // Act
        var file = await GetFile(created.Id);

        // Assert
        file.Id.Should().Be(created.Id);
        file.Title.Should().Be(created.Title);
        file.FolderId.Should().Be(work.Id);
        file.PureContentLength.Should().BeGreaterThan(0, "a new document is created from a template, not empty");
    }

    [Fact]
    public async Task GetFolderByFolderId_StringId_ListsCreatedFile()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (_, work) = await CreateWorkFolder("TP File Listed");
        var file = await CreateThirdPartyFile(work.Id, $"{UniqueTitle("TP File Listed")}.docx");

        // Act
        var (files, folders) = await ListThirdPartyFolder(work.Id);

        // Assert
        files.Should().Equal(file.Title);
        folders.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateFile_StringId_RenamesFile()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (_, work) = await CreateWorkFolder("TP File Rename");
        var file = await CreateThirdPartyFile(work.Id, $"{UniqueTitle("TP File Rename")}.docx");
        var newTitle = $"{UniqueTitle("TP File Renamed")}.docx";

        // Act
        var renamed = (await _filesApi.UpdateFileAsync(
            file.Id, new UpdateFile(newTitle), TestContext.Current.CancellationToken)).Response;

        // Assert
        renamed.Title.Should().Be(newTitle);

        // A WebDAV id is path based, so the renamed file is read back by the id the rename returned.
        var reread = await GetFile(renamed.Id);
        reread.Title.Should().Be(newTitle);

        var (files, _) = await ListThirdPartyFolder(work.Id);
        files.Should().Equal(newTitle);
    }

    [Fact]
    public async Task CreateTextFile_InThirdPartyFolder_StoresContent()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (_, work) = await CreateWorkFolder("TP File Text");
        var title = $"{UniqueTitle("TP File Text")}.txt";
        const string content = "third-party text file content";

        // Act
        var file = (await _filesApi.CreateTextFileAsync(
            work.Id, new CreateTextOrHtmlFile(title, content), TestContext.Current.CancellationToken)).Response;

        // Assert
        file.Title.Should().Be(title);
        file.FileExst.Should().Be(".txt");
        file.FolderId.Should().Be(work.Id);

        var reread = await GetFile(file.Id);
        reread.PureContentLength.Should().Be(content.Length);
    }

    [Fact]
    public async Task UploadFile_ToThirdPartyFolder_StoresFile()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (_, work) = await CreateWorkFolder("TP File Upload");
        var title = $"{UniqueTitle("TP File Upload")}.txt";
        var bytes = Encoding.UTF8.GetBytes("uploaded to third-party storage");
        await using var stream = new MemoryStream(bytes);

        // Act
        var uploaded = (await _foldersApi.UploadFileAsync(
            work.Id,
            file: new FileParameter(title, "text/plain", stream),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var file = uploaded.Should().ContainSingle().Subject;
        file.Title.Should().Be(title);
        file.FolderId.Should().Be(work.Id);

        var reread = await GetFile(file.Id);
        reread.PureContentLength.Should().Be(bytes.Length);
    }

    [Fact]
    public async Task DeleteFile_StringId_RemovesFileFromStorage()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (_, work) = await CreateWorkFolder("TP File Delete");
        var file = await CreateThirdPartyFile(work.Id, $"{UniqueTitle("TP File Delete")}.docx");

        // Act
        var results = (await _filesApi.DeleteFileAsync(
            file.Id, new Delete(false, true), cancellationToken: TestContext.Current.CancellationToken)).Response;
        var finished = await WaitLongOperation(results.FirstOrDefault()?.Id);

        // Assert
        finished.Should().NotBeNull();
        finished!.Should().OnlyContain(r => r.Finished && string.IsNullOrEmpty(r.Error));

        var (files, _) = await ListThirdPartyFolder(work.Id);
        files.Should().BeEmpty();

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await GetFile(file.Id));
        exception.ErrorCode.Should().Be(404, "a third-party file has no trash: once deleted it is gone");
    }

    [Fact]
    public async Task CopyBatchItems_FromMyDocumentsToThirdPartyFolder_CopiesFile()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (_, work) = await CreateWorkFolder("TP File Copy In");
        var source = await CreateFileInMy($"{UniqueTitle("TP File Copy In")}.docx", Owner);

        // Act
        var results = (await _filesOperationsApi.CopyBatchItemsAsync(new BatchRequestDto
        {
            FileIds = [new(source.Id)],
            DestFolderId = new(work.Id),
            ConflictResolveType = FileConflictResolveType.Overwrite,
            DeleteAfter = false
        }, TestContext.Current.CancellationToken)).Response;
        var finished = await WaitLongOperation(results.FirstOrDefault()?.Id);

        // Assert
        finished.Should().NotBeNull();
        finished!.Should().OnlyContain(r => r.Finished && string.IsNullOrEmpty(r.Error));

        var (files, _) = await ListThirdPartyFolder(work.Id);
        files.Should().Equal(source.Title);

        var myDocuments = (await _foldersApi.GetFolderByFolderIdAsync(
            await GetUserFolderIdAsync(Owner), cancellationToken: TestContext.Current.CancellationToken)).Response;
        myDocuments.Files.Select(f => f.Title).Should().Contain(source.Title, "copying leaves the source in place");
    }

    [Fact]
    public async Task CopyBatchItems_FromThirdPartyFolderToMyDocuments_CopiesFile()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (_, work) = await CreateWorkFolder("TP File Copy Out");
        var source = await CreateThirdPartyFile(work.Id, $"{UniqueTitle("TP File Copy Out")}.docx");
        var myDocumentsId = await GetUserFolderIdAsync(Owner);

        // Act
        var results = (await _filesOperationsApi.CopyBatchItemsAsync(new BatchRequestDto
        {
            FileIds = [new(source.Id)],
            DestFolderId = new(myDocumentsId),
            ConflictResolveType = FileConflictResolveType.Overwrite,
            DeleteAfter = false
        }, TestContext.Current.CancellationToken)).Response;
        var finished = await WaitLongOperation(results.FirstOrDefault()?.Id);

        // Assert
        finished.Should().NotBeNull();
        finished!.Should().OnlyContain(r => r.Finished && string.IsNullOrEmpty(r.Error));

        var myDocuments = (await _foldersApi.GetFolderByFolderIdAsync(
            myDocumentsId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        myDocuments.Files.Select(f => f.Title).Should().Contain(source.Title);

        var (files, _) = await ListThirdPartyFolder(work.Id);
        files.Should().ContainSingle(t => t == source.Title, "copying leaves the source in place");
    }

    [Fact]
    public async Task MoveBatchItems_BetweenThirdPartyFolders_MovesFile()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (_, work) = await CreateWorkFolder("TP File Move");
        var source = await CreateThirdPartyFolder(work.Id, UniqueTitle("TP File Move Source"));
        var destination = await CreateThirdPartyFolder(work.Id, UniqueTitle("TP File Move Dest"));
        var file = await CreateThirdPartyFile(source.Id, $"{UniqueTitle("TP File Move")}.docx");

        // Act
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            FileIds = [new(file.Id)],
            DestFolderId = new(destination.Id),
            ConflictResolveType = FileConflictResolveType.Overwrite,
            DeleteAfter = false
        }, TestContext.Current.CancellationToken)).Response;
        var finished = await WaitLongOperation(results.FirstOrDefault()?.Id);

        // Assert
        finished.Should().NotBeNull();
        finished!.Should().OnlyContain(r => r.Finished && string.IsNullOrEmpty(r.Error));

        var (sourceFiles, _) = await ListThirdPartyFolder(source.Id);
        sourceFiles.Should().BeEmpty();

        var (destinationFiles, _) = await ListThirdPartyFolder(destination.Id);
        destinationFiles.Should().Equal(file.Title);
    }
}
