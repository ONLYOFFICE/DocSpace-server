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

namespace ASC.Files.Tests.Tests._06_Operations.Duplicate;

/// <summary>
/// <c>PUT /api/2.0/files/fileops/duplicate</c> — what the copies hold. Each item is copied next to
/// itself under a numbered name, every repetition adds a copy, a folder comes with its content, and
/// the caller needs the right to create in that folder. These tests upload known bytes and compare
/// them, which the title-only tests in <see cref="DuplicateBatchItemsTests"/> cannot tell apart from
/// "a copy of an empty document".
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class DuplicateContentTests(
    AspireAppFixture fixture)
    : DuplicateTestBase(fixture)
{
    private async Task<int> Put(int folderId, string name, byte[] bytes, bool createNewIfExist = true)
    {
        return (await OperationFileBytes.UploadAsync(
            _filesOperationsApi, folderId, name, bytes, createNewIfExist, TestContext.Current.CancellationToken)).File.Id;
    }

    private Task<byte[]> Download(int fileId, int? version = null)
    {
        return OperationFileBytes.DownloadAsync(_filesClient, fileId, version, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DuplicateFile_Repeatedly_AddsNumberedCopiesWithTheSameBytesAndLeavesTheOriginal()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Duplicate One", myDocsFolderId);
        var content = OperationFileBytes.Make(200, 101);
        var originalId = await Put(dir.Id, "dup.bin", content);

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(fileIds: [originalId]));
        await DuplicateAndWait(BuildDuplicateRequest(fileIds: [originalId]));

        // Assert
        var entries = await GetRawEntries(dir.Id, inFolders: false);
        entries.Should().HaveCount(3);
        var copies = entries.Where(e => e.Id != originalId).ToList();
        copies.Select(c => c.Title).Distinct().Should().HaveCount(2);

        foreach (var copy in copies)
        {
            copy.Title.Should().StartWith("dup").And.NotBe("dup.bin");
            (await Download(copy.Id)).Should().Equal(content);
        }

        (await Download(originalId)).Should().Equal(content);
        (await GetVersionsOf(originalId)).Should().Equal(1);
    }

    [Fact]
    public async Task DuplicateFile_Copy_IsIndependentOfTheOriginal()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Duplicate Independent", myDocsFolderId);
        var content = OperationFileBytes.Make(300, 111);
        var originalId = await Put(dir.Id, "indep.bin", content);
        await DuplicateAndWait(BuildDuplicateRequest(fileIds: [originalId]));
        var copyId = (await GetRawEntries(dir.Id, inFolders: false)).Single(e => e.Id != originalId).Id;

        // Act - a new version of the original, then its deletion
        var newer = OperationFileBytes.Make(310, 112);
        await Put(dir.Id, "indep.bin", newer, createNewIfExist: false);

        // Assert
        (await Download(originalId)).Should().Equal(newer);
        (await Download(copyId)).Should().Equal(content);

        var deleted = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FileIds = [new(originalId)], Immediately = true, ReturnSingleOperation = true },
            TestContext.Current.CancellationToken)).Response;
        await WaitLongOperation(deleted.FirstOrDefault()?.Id);

        (await Download(copyId)).Should().Equal(content);
    }

    [Fact]
    public async Task DuplicateFile_WithTwoVersions_CopiesTheLatestContentAndTheOriginalKeepsBoth()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Duplicate Versions", myDocsFolderId);
        var v1 = OperationFileBytes.Make(50, 121);
        var v2 = OperationFileBytes.Make(60, 122);
        var id = await OperationFileBytes.UploadVersionsAsync(
            _filesOperationsApi, dir.Id, "versioned.bin", [v1, v2], TestContext.Current.CancellationToken);

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(fileIds: [id]));

        // Assert
        var copyId = (await GetRawEntries(dir.Id, inFolders: false)).Single(e => e.Id != id).Id;
        (await Download(copyId)).Should().Equal(v2);
        (await GetVersionsOf(id)).Should().Equal(1, 2);
        (await Download(id, 1)).Should().Equal(v1);
        (await Download(id, 2)).Should().Equal(v2);
    }

    [Fact]
    public async Task DuplicateFiles_SeveralInOneCall_EachGetsACopyWithTheRightBytes()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Duplicate Several", myDocsFolderId);
        var contents = new[] { OperationFileBytes.Make(30, 131), OperationFileBytes.Make(40, 132), OperationFileBytes.Make(50, 133) };
        var ids = new List<int>();

        for (var i = 0; i < contents.Length; i++)
        {
            ids.Add(await Put(dir.Id, $"item{i}.bin", contents[i]));
        }

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(fileIds: ids));

        // Assert
        var entries = await GetRawEntries(dir.Id, inFolders: false);
        entries.Should().HaveCount(6);

        for (var i = 0; i < contents.Length; i++)
        {
            var copy = entries.Single(e => !ids.Contains(e.Id) && e.Title.StartsWith($"item{i}"));
            (await Download(copy.Id)).Should().Equal(contents[i]);
        }
    }

    [Fact]
    public async Task DuplicateFolder_Tree_IsCopiedByteForByteAndTheCopySurvivesDeletionOfTheOriginal()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var root = await CreateFolder("Autotest Duplicate Tree", myDocsFolderId);
        var level = await CreateFolder("Level", root.Id);
        var rootBytes = OperationFileBytes.Make(10, 141);
        var levelBytes = OperationFileBytes.Make(20, 142);
        await Put(root.Id, "root.bin", rootBytes);
        await Put(level.Id, "level.bin", levelBytes);

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(folderIds: [root.Id]));

        // Assert
        var copyRoot = (await GetRawEntries(myDocsFolderId, inFolders: true))
            .Single(e => e.Id != root.Id && e.Title.StartsWith("Autotest Duplicate Tree"));
        var copyLevel = (await GetRawEntries(copyRoot.Id, inFolders: true)).Single();
        copyLevel.Title.Should().Be("Level");

        async Task AssertCopyIntact()
        {
            var rootFile = (await GetRawEntries(copyRoot.Id, inFolders: false)).Single();
            var levelFile = (await GetRawEntries(copyLevel.Id, inFolders: false)).Single();
            (await Download(rootFile.Id)).Should().Equal(rootBytes);
            (await Download(levelFile.Id)).Should().Equal(levelBytes);
        }

        await AssertCopyIntact();

        var deleted = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FolderIds = [new(root.Id)], Immediately = true, ReturnSingleOperation = true },
            TestContext.Current.CancellationToken)).Response;
        await WaitLongOperation(deleted.FirstOrDefault()?.Id);

        await AssertCopyIntact();
    }

    [Fact]
    public async Task DuplicateFile_RepeatedId_AddsOneMoreCopyPerOccurrence()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Duplicate Repeat", myDocsFolderId);
        var content = OperationFileBytes.Make(64, 151);
        var id = await Put(dir.Id, "rep.bin", content);

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(fileIds: [id, id]));

        // Assert
        var copies = (await GetRawEntries(dir.Id, inFolders: false)).Where(e => e.Id != id).ToList();
        copies.Should().HaveCount(2);
        copies.Select(c => c.Title).Distinct().Should().HaveCount(2);

        foreach (var copy in copies)
        {
            (await Download(copy.Id)).Should().Equal(content);
        }
    }

    [Fact]
    public async Task DuplicateFile_NumberedNameAlreadyTaken_IsSkippedAndNothingIsOverwritten()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Duplicate Taken", myDocsFolderId);
        var content = OperationFileBytes.Make(80, 161);
        var taken = OperationFileBytes.Make(90, 162);
        var id = await Put(dir.Id, "taken.bin", content);
        var squatterId = await Put(dir.Id, "taken (1).bin", taken);

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(fileIds: [id]));

        // Assert
        var entries = await GetRawEntries(dir.Id, inFolders: false);
        entries.Should().HaveCount(3);
        var copy = entries.Single(e => e.Id != id && e.Id != squatterId);
        copy.Title.Should().NotBe("taken (1).bin");
        (await Download(copy.Id)).Should().Equal(content);
        (await Download(squatterId)).Should().Equal(taken);
    }

    [Fact]
    public async Task DuplicateBatch_MixingAFileTheCallerMayDuplicateAndOneTheyMayNot_IsRefusedAsAWholeAndNothingIsCopied()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var room = await CreateCustomRoom("Autotest Duplicate Mixed Room");
        var creator = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, creator, FileShare.ContentCreator);
        var own = await Put(room.Id, "own.bin", OperationFileBytes.Make(30, 171));
        var privateFile = await Put(myDocsFolderId, "Autotest Duplicate Private.bin", OperationFileBytes.Make(30, 172));

        await _filesClient.Authenticate(creator);

        // Positive control: the same user may duplicate the room file alone.
        await DuplicateAndWait(BuildDuplicateRequest(fileIds: [own]));
        var afterControl = await GetRawEntries(room.Id, inFolders: false);
        afterControl.Should().HaveCount(2);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DuplicateBatchItemsAsync(
                BuildDuplicateRequest(fileIds: [own, privateFile]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        (await GetRawEntries(room.Id, inFolders: false)).Should().BeEquivalentTo(afterControl);

        await _filesClient.Authenticate(Owner);
        (await GetRawEntries(myDocsFolderId, inFolders: false)).Should().ContainSingle(e => e.Title.StartsWith("Autotest Duplicate Private"));
    }

    [Fact]
    public async Task DuplicateFile_Users_AreIsolated_ACopyLandsInTheirOwnDocumentsAndTheOwnersPrivateFileIsOutOfReach()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var ownerFile = await Put(myDocsFolderId, "Autotest Duplicate Isolation Owner.bin", OperationFileBytes.Make(30, 181));
        var ownerBefore = await GetRawEntries(myDocsFolderId, inFolders: false);

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var userDocsFolderId = await GetUserFolderIdAsync(user);
        var content = OperationFileBytes.Make(40, 182);
        var userFile = await Put(userDocsFolderId, "Autotest Duplicate Isolation User.bin", content);

        // Act
        var stolen = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DuplicateBatchItemsAsync(
                BuildDuplicateRequest(fileIds: [ownerFile]), TestContext.Current.CancellationToken));
        await DuplicateAndWait(BuildDuplicateRequest(fileIds: [userFile]));

        // Assert
        stolen.ErrorCode.Should().Be(403);
        var copy = (await GetRawEntries(userDocsFolderId, inFolders: false)).Single(e => e.Id != userFile);
        (await Download(copy.Id)).Should().Equal(content);

        await _filesClient.Authenticate(Owner);
        (await GetRawEntries(myDocsFolderId, inFolders: false)).Should().BeEquivalentTo(ownerBefore);
    }

    private async Task<List<int>> GetVersionsOf(int fileId)
    {
        var versions = (await _filesApi.GetFileVersionInfoAsync(fileId, TestContext.Current.CancellationToken)).Response;

        return [.. versions.Select(v => v.Version).Order()];
    }
}
