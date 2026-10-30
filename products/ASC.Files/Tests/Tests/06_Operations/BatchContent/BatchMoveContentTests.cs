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
/// <c>PUT /api/2.0/files/fileops/move</c> at content level: a moved entry keeps its id, versions and
/// bytes; name clashes settle as requested; a set the caller may not fully move is refused as a whole.
/// Access matrices live in the Move folder.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class BatchMoveContentTests(
    AspireAppFixture fixture)
    : BatchContentTestBase(fixture)
{
    [Theory]
    [InlineData("itself")]
    [InlineData("child")]
    [InlineData("grandchild")]
    public async Task MoveBatchItems_FolderIntoItselfOrItsDescendant_IsClientErrorWithNoJobAndNothingChanges(string target)
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var (root, child, grandchild) = await NestedFolders(myDocs, "Autotest Batch MoveLoop");
        var dest = target switch { "itself" => root, "child" => child, _ => grandchild };

        async Task<string> Snapshot() =>
            string.Join('|', await Listing(myDocs)) + "#" + string.Join('|', await Listing(root)) +
            "#" + string.Join('|', await Listing(child)) + "#" + string.Join('|', await Listing(grandchild));

        var before = await Snapshot();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(BatchRequest(dest, folderIds: [root]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().BeInRange(400, 499);
        await ExpectNoOperationQueued();
        (await Snapshot()).Should().Be(before);
    }

    [Fact]
    public async Task MoveBatchItems_File_KeepsIdVersionsAndContentAndLivesOnlyInDestination()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Batch MoveFile", myDocs);
        var v1 = MakeBytes(300, 71);
        var v2 = MakeBytes(400, 72);
        var file = await PutFile(dir.Id, "Moved.bin", v1);
        await PutFile(dir.Id, "Moved.bin", v2, createNewIfExist: false);
        var room = await CreateCustomRoom("Autotest Batch MoveFile Room");

        // Act
        await MoveItems(room.Id, fileIds: [file.Id]);

        // Assert
        (await Listing(dir.Id)).Should().BeEmpty();
        (await Listing(room.Id)).Should().Equal($"{file.Id}:Moved.bin");
        (await GetFile(file.Id)).FolderId.Should().Be(room.Id);
        (await VersionsOf(file.Id)).Should().Equal(1, 2);
        await ExpectContent(file.Id, v2);
    }

    [Fact]
    public async Task MoveBatchItems_FolderTree_KeepsIdsOfFoldersAndFilesAndEveryFileByteForByte()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var root = await CreateFolder("Autotest Batch MoveTree", myDocs);
        var inner = await CreateFolder("Inner", root.Id);
        var file = await PutFile(inner.Id, "inner.bin", MakeBytes(120, 81));
        var treeBefore = await ReadTree(root.Id);
        var room = await CreateCustomRoom("Autotest Batch MoveTree Room");

        // Act
        await MoveItems(room.Id, folderIds: [root.Id]);

        // Assert
        (await Listing(room.Id)).Should().Equal($"{root.Id}:Autotest Batch MoveTree");
        (await Listing(myDocs)).Should().NotContain($"{root.Id}:Autotest Batch MoveTree");
        (await Listing(root.Id)).Should().Equal($"{inner.Id}:Inner");
        (await Listing(inner.Id)).Should().Equal($"{file.Id}:inner.bin");
        (await ReadTree(root.Id)).Should().Equal(treeBefore);
    }

    [Theory]
    [InlineData(FileConflictResolveType.Skip)]
    [InlineData(FileConflictResolveType.Overwrite)]
    [InlineData(FileConflictResolveType.Duplicate)]
    public async Task MoveBatchItems_NameClash_SettlesAsRequested(FileConflictResolveType mode)
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var source = await CreateFolder($"Autotest Batch MoveClash Source {mode}", myDocs);
        var dest = await CreateFolder($"Autotest Batch MoveClash Dest {mode}", myDocs);
        var fromSource = MakeBytes(200, 1);
        var atDest = MakeBytes(300, 2);
        var src = await PutFile(source.Id, "same.bin", fromSource);
        var existing = await PutFile(dest.Id, "same.bin", atDest);

        // Act
        await MoveItems(dest.Id, fileIds: [src.Id], resolve: mode);

        // Assert
        var atDestAfter = await Listing(dest.Id);
        var atSourceAfter = await Listing(source.Id);

        switch (mode)
        {
            case FileConflictResolveType.Skip:
                atDestAfter.Should().Equal($"{existing.Id}:same.bin");
                atSourceAfter.Should().Equal($"{src.Id}:same.bin");
                await ExpectContent(existing.Id, atDest);
                await ExpectContent(src.Id, fromSource);
                break;
            case FileConflictResolveType.Overwrite:
                atSourceAfter.Should().BeEmpty();
                atDestAfter.Should().Equal($"{existing.Id}:same.bin");
                await ExpectContent(existing.Id, fromSource);
                break;
            default:
                atSourceAfter.Should().BeEmpty();
                atDestAfter.Should().HaveCount(2).And.Contain($"{existing.Id}:same.bin");

                // the moved file keeps its id and takes a numbered name
                atDestAfter.Should().Contain($"{src.Id}:same (1).bin");
                await ExpectContent(existing.Id, atDest);
                await ExpectContent(src.Id, fromSource);
                break;
        }
    }

    [Fact]
    public async Task MoveBatchItems_RepeatedId_MovesFileOnceAndLeavesNothingBehind()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Batch MoveRepeat", myDocs);
        var content = MakeBytes(64, 91);
        var file = await PutFile(dir.Id, "Once.bin", content);
        var room = await CreateCustomRoom("Autotest Batch MoveRepeat Room");

        // Act
        await MoveItems(room.Id, fileIds: [file.Id, file.Id]);

        // Assert
        (await Listing(dir.Id)).Should().BeEmpty();
        (await Listing(room.Id)).Should().Equal($"{file.Id}:Once.bin");
        await ExpectContent(file.Id, content);
    }

    [Fact]
    public async Task MoveBatchItems_SetMixingOwnFileAndUntouchableFile_IsRefusedAsAWholeAndNothingMoves()
    {
        // Arrange
        var ownerDocs = await GetUserFolderIdAsync(Owner);
        var privateFile = await PutFile(ownerDocs, "Autotest Batch MovePrivate.bin", MakeBytes(80, 95));
        var room = await CreateCustomRoom("Autotest Batch MoveMixed Room");
        var user = await InviteContentCreator(room.Id);

        var userDocs = await GetUserFolderIdAsync(user);
        var ownFile = await PutFile(userDocs, "Autotest Batch MoveOwn.bin", MakeBytes(90, 96));

        // positive control: the same user may move the own file alone
        await MoveItems(room.Id, fileIds: [ownFile.Id]);
        (await Listing(room.Id)).Should().Equal($"{ownFile.Id}:Autotest Batch MoveOwn.bin");
        var second = await PutFile(userDocs, "Autotest Batch MoveOwn Two.bin", MakeBytes(91, 97));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(
                BatchRequest(room.Id, fileIds: [second.Id, privateFile.Id]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        (await Listing(userDocs)).Should().Contain($"{second.Id}:Autotest Batch MoveOwn Two.bin");
        (await Listing(room.Id)).Should().Equal($"{ownFile.Id}:Autotest Batch MoveOwn.bin");

        await _filesClient.Authenticate(Owner);
        (await Listing(ownerDocs)).Should().Contain($"{privateFile.Id}:Autotest Batch MovePrivate.bin");
    }
}
