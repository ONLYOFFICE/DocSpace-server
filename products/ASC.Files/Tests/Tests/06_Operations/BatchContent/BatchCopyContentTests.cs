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
/// <c>PUT /api/2.0/files/fileops/copy</c> at content level: what the copy carries (latest content, whole
/// trees), that it is independent of the original, how name clashes settle, and that a set the caller may
/// not fully copy is refused as a whole. Access matrices live in the Copy folder.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class BatchCopyContentTests(
    AspireAppFixture fixture)
    : BatchContentTestBase(fixture)
{
    [Theory]
    [InlineData("itself")]
    [InlineData("child")]
    [InlineData("grandchild")]
    public async Task CopyBatchItems_FolderIntoItselfOrItsDescendant_IsClientErrorWithNoJobAndNothingChanges(string target)
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var (root, child, grandchild) = await NestedFolders(myDocs, "Autotest Batch CopyLoop");
        var dest = target switch { "itself" => root, "child" => child, _ => grandchild };

        async Task<string> Snapshot() =>
            string.Join('|', await Listing(myDocs)) + "#" + string.Join('|', await Listing(root)) +
            "#" + string.Join('|', await Listing(child)) + "#" + string.Join('|', await Listing(grandchild));

        var before = await Snapshot();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(BatchRequest(dest, folderIds: [root]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().BeInRange(400, 499);
        await ExpectNoOperationQueued();
        (await Snapshot()).Should().Be(before);
    }

    [Fact]
    public async Task CopyBatchItems_FileWithTwoVersions_CopiesLatestContentAsNewFileAndLeavesOriginalUntouched()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Batch CopyVersions", myDocs);
        var v1 = MakeBytes(500, 11);
        var v2 = MakeBytes(700, 22);
        var original = await PutFile(dir.Id, "Versioned.bin", v1);
        await PutFile(dir.Id, "Versioned.bin", v2, createNewIfExist: false);
        (await VersionsOf(original.Id)).Should().Equal(1, 2);
        var room = await CreateCustomRoom("Autotest Batch CopyVersions Room");

        // Act
        await CopyItems(room.Id, fileIds: [original.Id]);

        // Assert: a copy carries the latest content only, not the version history (observed, not pinned)
        var copied = await Listing(room.Id);
        copied.Should().ContainSingle().Which.Should().EndWith(":Versioned.bin");
        IdOf(copied[0]).Should().NotBe(original.Id);
        await ExpectContent(IdOf(copied[0]), v2);
        await ExpectContent(original.Id, v2);
        (await VersionsOf(original.Id)).Should().Equal(1, 2);
    }

    [Fact]
    public async Task CopyBatchItems_Copy_IsIndependentOfOriginalNewVersionAndDeletion()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Batch CopyIndependent", myDocs);
        var content = MakeBytes(600, 31);
        var original = await PutFile(dir.Id, "Independent.bin", content);
        var room = await CreateCustomRoom("Autotest Batch CopyIndependent Room");
        await CopyItems(room.Id, fileIds: [original.Id]);
        var copiedId = IdOf((await Listing(room.Id)).Single());
        await ExpectContent(copiedId, content);

        // Act: a new version of the original, then its deletion
        var newer = MakeBytes(900, 32);
        await PutFile(dir.Id, "Independent.bin", newer, createNewIfExist: false);
        await ExpectContent(original.Id, newer);
        await ExpectContent(copiedId, content);

        await DeleteItems(immediately: true, fileIds: [original.Id]);

        // Assert
        (await Listing(dir.Id)).Should().BeEmpty();
        await ExpectContent(copiedId, content);
    }

    [Fact]
    public async Task CopyBatchItems_NestedFolderTree_CopiesEveryFileByteForByteAndSurvivesDeletionOfOriginal()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var root = await CreateFolder("Autotest Batch Tree", myDocs);
        var level1 = await CreateFolder("Level1", root.Id);
        var level2 = await CreateFolder("Level2", level1.Id);
        await PutFile(root.Id, "root.bin", MakeBytes(10, 41));
        await PutFile(level1.Id, "one.bin", MakeBytes(20, 42));
        await PutFile(level2.Id, "two.bin", MakeBytes(30, 43));
        var room = await CreateCustomRoom("Autotest Batch Tree Room");
        var originalTree = await ReadTree(root.Id);
        originalTree.Should().HaveCount(3);

        // Act
        await CopyItems(room.Id, folderIds: [root.Id]);

        // Assert
        var inRoom = await Listing(room.Id);
        inRoom.Should().ContainSingle().Which.Should().EndWith(":Autotest Batch Tree");
        var copiedRootId = IdOf(inRoom[0]);
        copiedRootId.Should().NotBe(root.Id);
        (await ReadTree(copiedRootId)).Should().Equal(originalTree);

        await DeleteItems(immediately: true, folderIds: [root.Id]);
        (await ReadTree(copiedRootId)).Should().Equal(originalTree);
    }

    [Theory]
    [InlineData(FileConflictResolveType.Skip)]
    [InlineData(FileConflictResolveType.Overwrite)]
    [InlineData(FileConflictResolveType.Duplicate)]
    public async Task CopyBatchItems_NameClash_SettlesAsRequestedAndSourceStays(FileConflictResolveType mode)
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var source = await CreateFolder($"Autotest Batch CopyClash Source {mode}", myDocs);
        var dest = await CreateFolder($"Autotest Batch CopyClash Dest {mode}", myDocs);
        var fromSource = MakeBytes(200, 1);
        var atDest = MakeBytes(300, 2);
        var src = await PutFile(source.Id, "same.bin", fromSource);
        var existing = await PutFile(dest.Id, "same.bin", atDest);

        // Act
        await CopyItems(dest.Id, fileIds: [src.Id], resolve: mode);

        // Assert: the source is never touched by a copy
        (await Listing(source.Id)).Should().Equal($"{src.Id}:same.bin");
        await ExpectContent(src.Id, fromSource);

        var atDestAfter = await Listing(dest.Id);

        switch (mode)
        {
            case FileConflictResolveType.Skip:
                atDestAfter.Should().Equal($"{existing.Id}:same.bin");
                await ExpectContent(existing.Id, atDest);
                break;
            case FileConflictResolveType.Overwrite:
                atDestAfter.Should().Equal($"{existing.Id}:same.bin");
                await ExpectContent(existing.Id, fromSource);
                break;
            default:
                atDestAfter.Should().HaveCount(2).And.Contain($"{existing.Id}:same.bin");
                var added = atDestAfter.Single(e => IdOf(e) != existing.Id);
                added.Should().EndWith(":same (1).bin");
                await ExpectContent(existing.Id, atDest);
                await ExpectContent(IdOf(added), fromSource);
                break;
        }
    }

    [Fact]
    public async Task CopyBatchItems_RepeatedId_NeverHarmsOriginalAndEveryCopyMatchesIt()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Batch CopyRepeat", myDocs);
        var content = MakeBytes(64, 51);
        var original = await PutFile(dir.Id, "Repeated.bin", content);
        var room = await CreateCustomRoom("Autotest Batch CopyRepeat Room");

        // Act
        await CopyItems(room.Id, fileIds: [original.Id, original.Id], resolve: FileConflictResolveType.Duplicate);

        // Assert
        (await Listing(dir.Id)).Should().Equal($"{original.Id}:Repeated.bin");
        await ExpectContent(original.Id, content);

        var copies = await Listing(room.Id);
        copies.Should().NotBeEmpty();

        foreach (var entry in copies)
        {
            await ExpectContent(IdOf(entry), content);
        }
    }

    [Fact]
    public async Task CopyBatchItems_SetMixingOwnFileAndUnreadableFile_IsRefusedAsAWholeAndNothingIsCopied()
    {
        // Arrange
        var ownerDocs = await GetUserFolderIdAsync(Owner);
        var privateFile = await PutFile(ownerDocs, "Autotest Batch Private.bin", MakeBytes(80, 61));
        var room = await CreateCustomRoom("Autotest Batch CopyMixed Room");
        var user = await InviteContentCreator(room.Id);

        var userDocs = await GetUserFolderIdAsync(user);
        var ownFile = await PutFile(userDocs, "Autotest Batch Own.bin", MakeBytes(90, 62));
        var roomBefore = await Listing(room.Id);

        // positive control: the user may copy the own file alone into this room
        await CopyItems(room.Id, fileIds: [ownFile.Id]);
        var afterControl = await Listing(room.Id);
        afterControl.Should().HaveCount(roomBefore.Count + 1);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                BatchRequest(room.Id, fileIds: [ownFile.Id, privateFile.Id], resolve: FileConflictResolveType.Duplicate),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        (await Listing(room.Id)).Should().Equal(afterControl);

        await _filesClient.Authenticate(Owner);
        (await Listing(ownerDocs)).Should().Contain($"{privateFile.Id}:Autotest Batch Private.bin");
    }
}
