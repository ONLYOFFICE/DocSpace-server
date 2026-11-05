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

namespace ASC.Files.Tests.Tests._06_Operations.BatchContent;

/// <summary>
/// <c>PUT /api/2.0/files/fileops/delete</c> and <c>PUT /api/2.0/files/fileops/emptytrash</c> at content
/// level: what ends up in the Trash, what restoring brings back, and what emptying removes for good.
/// A file is restored by moving it out of the Trash.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class BatchTrashContentTests(
    AspireAppFixture fixture)
    : BatchContentTestBase(fixture)
{
    [Fact]
    public async Task DeleteBatchItems_SetWithExistingAndNonExistentFile_ReturnsNotFoundAndDeletesNothing()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Batch DeleteMixed", myDocs);
        var content = MakeBytes(100, 101);
        var real = await PutFile(dir.Id, "Real.bin", content);
        var trashBefore = await TrashFileIds();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DeleteBatchItemsAsync(
                DeleteRequest(false, fileIds: [real.Id, 99999999]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        await ExpectNoOperationQueued();
        (await Listing(dir.Id)).Should().Equal($"{real.Id}:Real.bin");
        (await TrashFileIds()).Should().Equal(trashBefore);
        await ExpectContent(real.Id, content);
    }

    [Fact]
    public async Task DeleteBatchItems_RepeatedId_DeletesFileOnceIntoTrash()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Batch DeleteRepeat", myDocs);
        var file = await PutFile(dir.Id, "Twice.bin", MakeBytes(100, 102));
        var other = await PutFile(dir.Id, "Stays.bin", MakeBytes(100, 103));

        // Act
        await DeleteItems(false, fileIds: [file.Id, file.Id]);

        // Assert
        (await Listing(dir.Id)).Should().Equal($"{other.Id}:Stays.bin");
        (await TrashFileIds()).Count(id => id == file.Id).Should().Be(1);
        await ExpectContent(other.Id, MakeBytes(100, 103));
    }

    [Fact]
    public async Task DeleteBatchItems_FileWithTwoVersions_GoesToTrashAndRestoringBringsBackIdVersionsAndContent()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Batch Restore", myDocs);
        var v2 = MakeBytes(222, 112);
        var file = await PutFile(dir.Id, "Restore.bin", MakeBytes(111, 111));
        await PutFile(dir.Id, "Restore.bin", v2, createNewIfExist: false);

        // Act
        await DeleteItems(false, fileIds: [file.Id]);

        // Assert: in the Trash, and its content is not handed out
        (await Listing(dir.Id)).Should().BeEmpty();
        (await TrashFileIds()).Should().Contain(file.Id);

        var inTrash = await Record.ExceptionAsync(async () => await ReadContent(file.Id));
        inTrash.Should().NotBeNull("the content of a trashed file is not handed out");

        // restoring is moving the item out of the Trash
        await MoveItems(dir.Id, fileIds: [file.Id]);

        (await TrashFileIds()).Should().NotContain(file.Id);
        (await Listing(dir.Id)).Should().Equal($"{file.Id}:Restore.bin");
        (await VersionsOf(file.Id)).Should().Equal(1, 2);
        await ExpectContent(file.Id, v2);
    }

    [Fact]
    public async Task DeleteBatchItems_RepeatedCallWithSameId_DeletesWhatWasRestoredInTheMeantime()
    {
        // Arrange: documented, not idempotent
        var myDocs = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Batch DeleteAgain", myDocs);
        var file = await PutFile(dir.Id, "Again.bin", MakeBytes(50, 121));

        await DeleteItems(false, fileIds: [file.Id]);
        await MoveItems(dir.Id, fileIds: [file.Id]);
        (await Listing(dir.Id)).Should().Equal($"{file.Id}:Again.bin");

        // Act
        await DeleteItems(false, fileIds: [file.Id]);

        // Assert
        (await Listing(dir.Id)).Should().BeEmpty();
        (await TrashFileIds()).Should().Contain(file.Id);
    }

    [Fact]
    public async Task DeleteBatchItems_SetMixingOwnFileAndFileCallerCannotDelete_IsRefusedAsAWholeAndNothingIsDeleted()
    {
        // Arrange
        var ownerDocs = await GetUserFolderIdAsync(Owner);
        var content = MakeBytes(80, 131);
        var foreign = await PutFile(ownerDocs, "Autotest Batch DeleteForeign.bin", content);
        var user = await InviteMember(EmployeeType.User);
        var userDocs = await GetUserFolderIdAsync(user);
        var ownBytes = MakeBytes(90, 132);
        var ownFile = await PutFile(userDocs, "Autotest Batch DeleteOwn.bin", ownBytes);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DeleteBatchItemsAsync(
                DeleteRequest(true, fileIds: [ownFile.Id, foreign.Id]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        await ExpectNoOperationQueued();
        (await Listing(userDocs)).Should().Contain($"{ownFile.Id}:Autotest Batch DeleteOwn.bin");
        await ExpectContent(ownFile.Id, ownBytes);

        await _filesClient.Authenticate(Owner);
        (await Listing(ownerDocs)).Should().Contain($"{foreign.Id}:Autotest Batch DeleteForeign.bin");
        await ExpectContent(foreign.Id, content);
    }

    [Fact]
    public async Task DeleteBatchItems_AnotherUserDeletingFromOwnersTrash_IsClientErrorAndFileStaysRestorable()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var content = MakeBytes(40, 141);
        var file = await PutFile(myDocs, "Autotest Batch TrashGuard.bin", content);
        await DeleteItems(false, fileIds: [file.Id]);
        (await TrashFileIds()).Should().Contain(file.Id);

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DeleteBatchItemsAsync(
                DeleteRequest(true, fileIds: [file.Id]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().BeInRange(400, 499);

        await _filesClient.Authenticate(Owner);
        (await TrashFileIds()).Should().Contain(file.Id);
        await MoveItems(myDocs, fileIds: [file.Id]);
        await ExpectContent(file.Id, content);
    }

    [Fact]
    public async Task EmptyTrash_TrashedFilesAndFolders_AreGoneForGoodFromTrashLookupsAndRestore()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Batch EmptyGone", myDocs);
        var file = await PutFile(dir.Id, "Gone.bin", MakeBytes(60, 151));
        var folder = await CreateFolder("Gone Folder", dir.Id);
        var nested = await PutFile(folder.Id, "Nested.bin", MakeBytes(61, 152));
        await DeleteItems(false, fileIds: [file.Id], folderIds: [folder.Id]);
        (await TrashFileIds()).Should().Contain(file.Id);
        (await TrashFolderIds()).Should().Contain(folder.Id);

        // Act
        await EmptyTrash();

        // Assert
        (await TrashFileIds()).Should().BeEmpty();
        (await TrashFolderIds()).Should().BeEmpty();

        var fileLookup = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));
        var nestedLookup = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.GetFileInfoAsync(nested.Id, cancellationToken: TestContext.Current.CancellationToken));
        var folderLookup = await Assert.ThrowsAsync<ApiException>(async () => await _foldersApi.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken));
        fileLookup.ErrorCode.Should().Be(404);
        nestedLookup.ErrorCode.Should().Be(404);
        folderLookup.ErrorCode.Should().Be(404);

        (await Record.ExceptionAsync(async () => await ReadContent(file.Id))).Should().NotBeNull();

        var restore = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(
                BatchRequest(dir.Id, fileIds: [file.Id]), TestContext.Current.CancellationToken));
        restore.ErrorCode.Should().BeInRange(400, 499);
        (await Listing(dir.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task EmptyTrash_FilesOutsideTrashInDocumentsAndRooms_AreUntouchedVersionsIncluded()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Batch EmptyOutside", myDocs);
        var room = await CreateCustomRoom("Autotest Batch EmptyOutside Room");
        var v2 = MakeBytes(71, 162);
        var inRoomBytes = MakeBytes(72, 163);
        var keep = await PutFile(dir.Id, "Keep.bin", MakeBytes(70, 161));
        await PutFile(dir.Id, "Keep.bin", v2, createNewIfExist: false);
        var inRoom = await PutFile(room.Id, "InRoom.bin", inRoomBytes);
        var doomed = await PutFile(dir.Id, "Doomed.bin", MakeBytes(73, 164));
        await DeleteItems(false, fileIds: [doomed.Id]);
        var dirBefore = await Listing(dir.Id);
        var roomBefore = await Listing(room.Id);

        // Act
        await EmptyTrash();

        // Assert
        (await TrashFileIds()).Should().BeEmpty();
        (await Listing(dir.Id)).Should().Equal(dirBefore);
        (await Listing(room.Id)).Should().Equal(roomBefore);
        (await VersionsOf(keep.Id)).Should().Equal(1, 2);
        await ExpectContent(keep.Id, v2);
        await ExpectContent(inRoom.Id, inRoomBytes);
    }

    [Fact]
    public async Task EmptyTrash_AlreadyEmptyTrash_ChangesNothing()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var file = await PutFile(myDocs, "Autotest Batch EmptyTwice.bin", MakeBytes(30, 171));
        await DeleteItems(false, fileIds: [file.Id]);
        var keptBytes = MakeBytes(31, 172);
        var kept = await PutFile(myDocs, "Autotest Batch EmptyTwice Keep.bin", keptBytes);
        await EmptyTrash();
        (await TrashFileIds()).Should().BeEmpty();
        var before = await Listing(myDocs);

        // Act
        await EmptyTrash();

        // Assert
        (await TrashFileIds()).Should().BeEmpty();
        (await Listing(myDocs)).Should().Equal(before);
        await ExpectContent(kept.Id, keptBytes);
    }

    [Fact]
    public async Task EmptyTrash_OneAccountEmptying_LeavesAnotherAccountsTrashAndRestorableFilesAlone()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var v2 = MakeBytes(81, 182);
        var ownerFile = await PutFile(myDocs, "Autotest Batch TrashIso.bin", MakeBytes(80, 181));
        await PutFile(myDocs, "Autotest Batch TrashIso.bin", v2, createNewIfExist: false);
        await DeleteItems(false, fileIds: [ownerFile.Id]);

        var user = await InviteMember(EmployeeType.User);
        var userDocs = await GetUserFolderIdAsync(user);
        var userFile = await PutFile(userDocs, "Autotest Batch UserTrash.bin", MakeBytes(82, 183));
        await DeleteItems(false, fileIds: [userFile.Id]);
        (await TrashFileIds()).Should().Contain(userFile.Id);

        // Act
        await EmptyTrash();

        // Assert
        (await TrashFileIds()).Should().BeEmpty();

        await _filesClient.Authenticate(Owner);
        (await TrashFileIds()).Should().Equal(ownerFile.Id);
        await MoveItems(myDocs, fileIds: [ownerFile.Id]);
        (await VersionsOf(ownerFile.Id)).Should().Equal(1, 2);
        await ExpectContent(ownerFile.Id, v2);
    }

    [Fact]
    public async Task BatchOperations_FileGoesThroughCopyMoveDuplicateDeleteAndEmptyTrash_BytesStayIntactAndSurvivorsUntouched()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var dirA = await CreateFolder("Autotest Batch Flow A", myDocs);
        var dirB = await CreateFolder("Autotest Batch Flow B", myDocs);
        var room = await CreateCustomRoom("Autotest Batch Flow Room");
        var content = MakeBytes(256, 191);
        var file = await PutFile(dirA.Id, "Flow.bin", content);

        // copy into the room: a new file with the same bytes
        await CopyItems(room.Id, fileIds: [file.Id]);
        var copyId = IdOf((await Listing(room.Id)).Single());
        copyId.Should().NotBe(file.Id);
        await ExpectContent(copyId, content);

        // move the original to the other folder: same id, same bytes
        await MoveItems(dirB.Id, fileIds: [file.Id]);
        (await Listing(dirA.Id)).Should().BeEmpty();
        (await Listing(dirB.Id)).Should().Equal($"{file.Id}:Flow.bin");
        await ExpectContent(file.Id, content);

        // duplicate it in place: two files, both with the same bytes
        var duplicated = (await _filesOperationsApi.DuplicateBatchItemsAsync(
            new DuplicateRequestDto(fileIds: [new DuplicateRequestDtoAllOfFileIds(file.Id)]) { ReturnSingleOperation = true },
            TestContext.Current.CancellationToken)).Response;
        await Complete(duplicated, FileOperationType.Duplicate);

        var inB = await Listing(dirB.Id);
        inB.Should().HaveCount(2);

        foreach (var entry in inB)
        {
            await ExpectContent(IdOf(entry), content);
        }

        // delete both into the Trash, then empty it
        var ids = inB.Select(IdOf).ToList();
        await DeleteItems(false, fileIds: ids);
        (await Listing(dirB.Id)).Should().BeEmpty();
        (await TrashFileIds()).Should().BeEquivalentTo(ids);

        await EmptyTrash();
        (await TrashFileIds()).Should().BeEmpty();

        // the copy in the room survived the purge
        var inRoom = await Listing(room.Id);
        inRoom.Should().ContainSingle();
        await ExpectContent(IdOf(inRoom[0]), content);
    }
}
