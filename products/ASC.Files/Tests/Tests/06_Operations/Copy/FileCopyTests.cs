// (c) Copyright Ascensio System SIA 2009-2026
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using ASC.Files.Tests.ApiFactories;

namespace ASC.Files.Tests.Tests._06_Operations.Copy;

[Collection("Test Collection")]
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class FileCopyTests(
    FilesApiFactory filesFactory, 
    WepApiFactory apiFactory, 
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    [Fact]
    public async Task CopyFile_BetweenUserFolders_ReturnsValidFile()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a source file
        var sourceFile = await CreateFileInMy("source_document.docx", Initializer.Owner);
        
        // Create a target folder
        var targetFolder = await CreateFolderInMy("target_folder", Initializer.Owner);
        
        // Act
        var copyParams = new CopyAsJsonElement(
            destTitle: sourceFile.Title,
            destFolderId: new CopyAsJsonElementDestFolderId(targetFolder.Id)
        );
        
        var copiedFile = (await _filesApi.CopyFileAsAsync(sourceFile.Id, copyParams, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        copiedFile.Should().NotBeNull();
        // copiedFile.Id.Should().NotBe(sourceFile.Id);
        // copiedFile.Title.Should().Be(sourceFile.Title);
        // copiedFile.FolderId.Should().Be(targetFolderId);
        
        // Verify the copied file exists in the destination folder
        var folderContent = (await _foldersApi.GetFolderByFolderIdAsync(targetFolder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        folderContent.Files.Should().Contain(f => f.Title == sourceFile.Title);
    }
    
    [Fact]
    public async Task DuplicateFile_InsideUserFolder_ReturnsValidFileWithIndex()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a source file
        var sourceFile = await CreateFileInMy("source_document.docx", Initializer.Owner);
        
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
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a source file
        var sourceFile = await CreateFileInMy("rename_source.docx", Initializer.Owner);
        var newFileName = "renamed_copy.docx";
        
        // Get root folders to find a target folder
        var targetFolderId = await GetUserFolderIdAsync( Initializer.Owner);
        
        // Act
        var copyParams = new CopyAsJsonElement(
            destTitle: newFileName,
            destFolderId: new CopyAsJsonElementDestFolderId(targetFolderId)
        );
        
        var copiedFile = (await _filesApi.CopyFileAsAsync(sourceFile.Id, copyParams, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        copiedFile.Should().NotBeNull();

        // copiedFile.Id.Should().NotBe(sourceFile.Id);
        // copiedFile.Title.Should().Be(newFileName);
        // copiedFile.FolderId.Should().Be(targetFolderId);
    }
     
    [Fact]
    public async Task CopyFile_FileNotFound_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var targetFolderId = await GetUserFolderIdAsync(Initializer.Owner);
        var resultFileId = 999999999;

        // Act
        var copyParams = new CopyAsJsonElement(
            destTitle: "resultFile.docx",
            destFolderId: new CopyAsJsonElementDestFolderId(targetFolderId)
        );

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CopyFileAsAsync(
                resultFileId,
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFile_NoPermissions_ReturnError()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var sourceFile = await CreateFileInMy("source_document.docx", Initializer.Owner);

        var user = await Initializer.InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var targetFolder = await CreateFolderInMy("folder_no_permissions", user);

        // Act
        var copyParams = new CopyAsJsonElement(
            destTitle: sourceFile.Title,
            destFolderId: new CopyAsJsonElementDestFolderId(targetFolder.Id)
        );

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CopyFileAsAsync(
                sourceFile.Id,
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFile_AnotherUsersFile_ReturnError()
    {
        // Arrange
        var fileOwner = await Initializer.InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(fileOwner);
        var sourceFile = await CreateFile("source_file.docx", FolderType.USER, fileOwner);

        var folderOwner = await Initializer.InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(folderOwner);
        var targetFolder = await CreateFolder("folder_no_permissions", FolderType.USER, folderOwner);
        
        // Act
        var copyParams = new CopyAsJsonElement(
            destTitle: sourceFile.Title,
            destFolderId: new CopyAsJsonElementDestFolderId(targetFolder.Id)
        );

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CopyFileAsAsync(
                sourceFile.Id,
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFiles_ToFormFillingRoom_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var firstSourceFile = await CreateFile("first_document.docx", FolderType.USER, Initializer.Owner);
        var secondSourceFile = await CreateFile("second_document.docx", FolderType.USER, Initializer.Owner);
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
        await _filesClient.Authenticate(Initializer.Owner);
        var targetFolder = await CreateFolderInMy("target_folder", Initializer.Owner);

        var user = await Initializer.InviteContact(EmployeeType.User);
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
    public async Task CopeFile_SecurityException_ReturnsErrror()
    {
        // Assert
        var targetFolder = await CreateFolderInMy("target_folder", Initializer.Owner);
        var sourceFile = await CreateFileInMy("source_file.docx", Initializer.Owner);
        var user = await Initializer.InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var copyParams = new CopyAsJsonElement(
            destTitle: sourceFile.Title,
            destFolderId: new CopyAsJsonElementDestFolderId(targetFolder.Id)
        );

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CopyFileAsAsync(
                sourceFile.Id,
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFile_NotSupportedFormat_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var sourceFile = await CreateFileInMy("source_file.docx", Initializer.Owner);
        sourceFile.FileExst = "unsupported";

        var targetFolderId = await GetUserFolderIdAsync(Initializer.Owner);

        // Act
        var copyParams = new CopyAsJsonElement(
            destTitle: "result_file.unsupported",
            destFolderId: new CopyAsJsonElementDestFolderId(targetFolderId)
        );

        var a = (await _filesApi.CopyFileAsAsync(
                sourceFile.Id,
                copyParams,
                TestContext.Current.CancellationToken)).Response;

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CopyFileAsAsync(
                sourceFile.Id,
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(500);
    }

    [Fact]
    public async Task CopyFile_FormToFillingFormsRoom_ReturnsError()
    {
        // Assert
        await _filesClient.Authenticate(Initializer.Owner);
        var sourceFile = await CreateFileInMy("source_file.docx", Initializer.Owner);
        sourceFile.IsForm = true;

        var parentFolder = await CreateFillingFormsRoom("parent_folder");
        var targetFolder = await CreateFolder("target_folder", parentFolder.Id);
        targetFolder.Type = FolderType.VirtualRooms;

        // Act
        var copyParams = new CopyAsJsonElement(
           destTitle: sourceFile.Title,
           destFolderId: new CopyAsJsonElementDestFolderId(targetFolder.Id)
        );
 
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CopyFileAsAsync(
                sourceFile.Id,
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFile_LockedFile_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);

        var sourceFile = await CreateFileInMy("locked_file.docx", Initializer.Owner);
        await _filesApi.LockFileAsync(sourceFile.Id, new LockFileParameters(true), TestContext.Current.CancellationToken);

        var targetFolderId = await GetUserFolderIdAsync(Initializer.Owner);

        // Act
        var copyParams = new CopyAsJsonElement(
            destTitle: sourceFile.Title,
            destFolderId: new CopyAsJsonElementDestFolderId(targetFolderId)
        );

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CopyFileAsAsync(
                sourceFile.Id,
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(500);
    }

    [Fact]
    public async Task CopyFile_EditingFile_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);

        var sourceFile = await CreateFileInMy("editing_file.docx", Initializer.Owner);
        await _filesApi.StartEditFileAsync(sourceFile.Id, new StartEdit(), TestContext.Current.CancellationToken);

        var targetFolderId = await GetUserFolderIdAsync(Initializer.Owner);

        // Act
        var copyParams = new CopyAsJsonElement(
            destTitle: sourceFile.Title,
            destFolderId: new CopyAsJsonElementDestFolderId(targetFolderId)
        );

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CopyFileAsAsync(
                sourceFile.Id,
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(500);
    }
}
