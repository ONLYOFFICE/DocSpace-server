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

namespace ASC.Files.Tests.Tests._06_Operations.Copy;

[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class FileCopyTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task CopyFile_BetweenUserFolders_ReturnsValidFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Create a source file
        var sourceFile = await CreateFileInMy("source_document.docx", Owner);

        // Create a target folder
        var targetFolder = await CreateFolderInMy("target_folder", Owner);

        // Act
        var copyParams = new CopyAsJsonElement(
            destTitle: sourceFile.Title,
            destFolderId: new CopyAsJsonElementDestFolderId(targetFolder.Id)
        );

        var copiedFile = (await _filesApi.CopyFileAsAsync(sourceFile.Id, copyParams, TestContext.Current.CancellationToken)).Response;

        // Assert
        copiedFile.Should().NotBeNull();

        // Verify the copied file exists in the destination folder
        var folderContent = (await _foldersApi.GetFolderByFolderIdAsync(targetFolder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        folderContent.Files.Should().Contain(f => f.Title == sourceFile.Title);
    }

    [Fact]
    public async Task DuplicateFile_InsideUserFolder_ReturnsValidFileWithIndex()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Create a source file
        var sourceFile = await CreateFileInMy("source_document.docx", Owner);

        // Act
        var results = (await _filesOperationsApi.DuplicateBatchItemsAsync(new DuplicateRequestDto
        {
            FileIds = [new(sourceFile.Id)],
            ReturnSingleOperation =  true
        }, TestContext.Current.CancellationToken)).Response;

        var operationId = results.FirstOrDefault()?.Id;

        // Assert
        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(operationId);
        }

        // Assert
        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));
        var newFile = results.OfType<FileOperationDto>().FirstOrDefault();
        newFile.Should().NotBeNull();
        newFile.Files.Should().Contain(r=> Path.GetFileNameWithoutExtension(r.Title) == Path.GetFileNameWithoutExtension(sourceFile.Title) + " (1)");
    }

    [Fact]
    public async Task CopyFile_WithRename_ReturnsRenamedFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Create a source file
        var sourceFile = await CreateFileInMy("rename_source.docx", Owner);
        var newFileName = "renamed_copy.docx";

        // Get root folders to find a target folder
        var targetFolderId = await GetUserFolderIdAsync( Owner);

        // Act
        var copyParams = new CopyAsJsonElement(
            destTitle: newFileName,
            destFolderId: new CopyAsJsonElementDestFolderId(targetFolderId)
        );

        var copiedFile = (await _filesApi.CopyFileAsAsync(sourceFile.Id, copyParams, TestContext.Current.CancellationToken)).Response;

        // Assert
        copiedFile.Should().NotBeNull();
    }

    [Fact]
    public async Task CopyFile_FileNotFound_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var targetFolderId = await GetUserFolderIdAsync(Owner);
        var resultFileId = 999999999;

        // Act
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targetFolderId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(resultFileId)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFile_NoPermissions_ReturnError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceFile = await CreateFileInMy("source_document.docx", Owner);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var targetFolder = await CreateFolderInMy("folder_no_permissions", user);

        // Act
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targetFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(sourceFile.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFile_AnotherUsersFile_ReturnError()
    {
        // Arrange
        var fileOwner = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(fileOwner);
        var sourceFile = await CreateFile("source_file.docx", FolderType.USER, fileOwner);

        var folderOwner = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(folderOwner);
        var targetFolder = await CreateFolder("folder_no_permissions", FolderType.USER, folderOwner);

        // Act
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targetFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(sourceFile.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFiles_ToFormFillingRoom_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var firstSourceFile = await CreateFile("first_document.docx", FolderType.USER, Owner);
        var secondSourceFile = await CreateFile("second_document.docx", FolderType.USER, Owner);
        var targertRoom = await CreateFillingFormsRoom("target_room");

        // Act
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targertRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(firstSourceFile.Id), new(secondSourceFile.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFile_ToAnotherUsersFolder_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var targetFolder = await CreateFolderInMy("target_folder", Owner);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var sourceFile = await CreateFileInMy("source_file.docx", user);

        // Act
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targetFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(sourceFile.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFile_SecurityException_ReturnsErrror()
    {
        // Assert
        var targetFolder = await CreateFolderInMy("target_folder", Owner);
        var sourceFile = await CreateFileInMy("source_file.docx", Owner);
        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targetFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(sourceFile.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFile_FormToFillingFormsRoom_ReturnsError()
    {
        // Assert
        await _filesClient.Authenticate(Owner);
        var sourceFile = await CreateFileInMy("source_file.docx", Owner);
        sourceFile.IsForm = true;

        var parentFolder = await CreateFillingFormsRoom("parent_folder");
        var targetFolder = await CreateFolder("target_folder", parentFolder.Id);
        targetFolder.Type = FolderType.VirtualRooms;

        // Act
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targetFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(sourceFile.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFile_LockedFile_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var createdRoom = await CreateVirtualRoom("room_to_lock");
        var sourceFile = await CreateFile("file_to_lock.docx", createdRoom.Id);
        var lockedFile = (await _filesApi.LockFileAsync(sourceFile.Id, new LockFileParameters(true), TestContext.Current.CancellationToken)).Response;

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var targetFolderId = await GetUserFolderIdAsync(user);

        // Act
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targetFolderId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(sourceFile.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFile_EditingFile_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var createdRoom = await CreateVirtualRoom("room");
        var sourceFile = await CreateFile("file_to_edit.docx", createdRoom.Id);
        await _filesApi.StartEditFileAsync(sourceFile.Id, new StartEdit(true), TestContext.Current.CancellationToken);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var targetFolderId = await GetUserFolderIdAsync(user);

        // Act
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targetFolderId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(sourceFile.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFile_NotSupportedFormat_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var roomRequest = new CreateRoomRequestDto("ai_room", indexing: true, roomType: RoomType.AiRoom);

        var aiRoom = (await _roomsApi.CreateRoomAsync(
            roomRequest,
            TestContext.Current.CancellationToken)).Response;

        var subFoldersResponse = await _foldersApi.GetFoldersWithHttpInfoAsync(aiRoom.Id, TestContext.Current.CancellationToken);

        using var payload = JsonDocument.Parse(subFoldersResponse.RawContent);
        var response = payload.RootElement.GetProperty("response");

        int? knowledgeFolderId = null;

        foreach (var folder in response.EnumerateArray())
        {
            var isKnowledgeByType = folder.TryGetProperty("type", out var rawType) &&
                                    rawType.ValueKind == JsonValueKind.Number &&
                                    rawType.TryGetInt32(out var folderTypeValue) &&
                                    folderTypeValue == (int)FolderType.Knowledge;

            if (!isKnowledgeByType)
            {
                continue;
            }

            if (folder.TryGetProperty("id", out var idProp) &&
                idProp.ValueKind == JsonValueKind.Number &&
                idProp.TryGetInt32(out var id))
            {
                knowledgeFolderId = id;
                break;
            }
        }

        knowledgeFolderId.Should().HaveValue();


        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken)).Response;

        var unsupportedExtension = settings.ExtsUploadable
            .FirstOrDefault(ext => !settings.ExtsFilesVectorized.Contains(ext, StringComparer.OrdinalIgnoreCase));

        unsupportedExtension.Should().NotBeNullOrWhiteSpace();

        var extension = unsupportedExtension!.StartsWith('.') ? unsupportedExtension : $".{unsupportedExtension}";

        var myFolder = await GetUserFolderIdAsync(Owner);
        var fileName = $"new{extension}";

        await using var stream = new MemoryStream([1, 2, 3, 4]);
        var contentLength = stream.Length;

        var createdSession = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(myFolder, new SessionRequest(fileName, contentLength), cancellationToken: TestContext.Current.CancellationToken)).Response;
        createdSession.Should().NotBeNull();
        createdSession.Id.Should().NotBeEmpty();

        var chunkSize = (int)settings.ChunkUploadSize;
        var buffer = new byte[chunkSize];
        var chunkNumber = 1;
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, chunkSize), TestContext.Current.CancellationToken)) > 0)
        {
            var chunkStream = new MemoryStream(buffer, 0, bytesRead);
            var fileParameter = new FileParameter(chunkStream);

            await _filesOperationsApi.UploadAsyncSessionAsync(myFolder, createdSession.Id, chunkNumber, fileParameter, TestContext.Current.CancellationToken);
            chunkNumber++;
        }

        var resultFile = (await _filesOperationsApi.FinalizeSessionAsync(myFolder, createdSession.Id, TestContext.Current.CancellationToken)).Response;

        // Act
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(knowledgeFolderId!.Value),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(resultFile.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        var attemptedExtension = Path.GetExtension(resultFile.Title);
        attemptedExtension.Should().NotBeNullOrWhiteSpace();
        settings.ExtsFilesVectorized.Should().NotContain(
            ext => string.Equals(ext, attemptedExtension, StringComparison.OrdinalIgnoreCase),
            $"copied file '{resultFile.Title}' has unsupported extraction format");
    }
}
