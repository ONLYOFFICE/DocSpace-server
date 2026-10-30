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

namespace ASC.Files.Tests.Tests._06_Operations.CheckPreflight;

/// <summary>
/// The preflight endpoints together with the operation they announce: what a check reports has to
/// be what the following copy or move then does, and a check itself changes nothing.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class CheckThenActTests(
    AspireAppFixture fixture)
    : CheckPreflightTestBase(fixture)
{
    [Fact]
    public async Task CheckMove_ReportedClash_DecidesTheOutcome_DuplicateKeepsTheExistingFileAndAddsACopy()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var source = await CreateFile("Autotest Flow Clash.docx", myDocsFolderId);
        var room = await CreateCustomRoom("Autotest Flow Clash Room");
        var existing = await CreateFile("Autotest Flow Clash.docx", room.Id);

        var check = await Preflight(PreflightKind.Move, fileIds: [source.Id], destFolderId: room.Id);
        IdsOf(check).Should().Equal(source.Id);

        // Act
        var copied = (await _filesOperationsApi.CopyBatchItemsAsync(
            BuildRequest(room.Id, FileConflictResolveType.Duplicate, fileIds: [source.Id]),
            TestContext.Current.CancellationToken)).Response;
        await WaitLongOperation(copied.FirstOrDefault()?.Id);

        // Assert
        var after = await Snapshot(room.Id, myDocsFolderId);
        after[room.Id].Should().HaveCount(2);
        after[room.Id].Should().Contain(e => e.StartsWith($"{existing.Id}:"));
        after[myDocsFolderId].Should().Contain(e => e.StartsWith($"{source.Id}:"));
    }

    [Fact]
    public async Task CheckDestFolder_AcceptedDestination_TakesTheFile_AndTheOriginalFolderThenReportsNoClashForIt()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var sourceFolder = await CreateFolder("Autotest Flow Move Source", myDocsFolderId);
        var file = await CreateFile("Autotest Flow Move.docx", sourceFolder.Id);
        var room = await CreateCustomRoom("Autotest Flow Move Room");

        var verdict = await Preflight(PreflightKind.CheckDestFolder, fileIds: [file.Id], destFolderId: room.Id);
        verdict.Result.Should().Be(CheckDestFolderResult.AllAllowed);

        // Act
        var moved = (await _filesOperationsApi.MoveBatchItemsAsync(
            BuildRequest(room.Id, FileConflictResolveType.Skip, fileIds: [file.Id]),
            TestContext.Current.CancellationToken)).Response;
        await WaitLongOperation(moved.FirstOrDefault()?.Id);

        // Assert
        var state = await Snapshot(sourceFolder.Id, room.Id);
        state[sourceFolder.Id].Should().BeEmpty();
        state[room.Id].Should().ContainSingle().Which.Should().EndWith("Autotest Flow Move.docx");

        // The file now lives in the room; a clash check against the old folder is empty.
        var movedId = int.Parse(state[room.Id][0].Split(':')[0]);
        var back = await Preflight(PreflightKind.Move, fileIds: [movedId], destFolderId: sourceFolder.Id);
        back.Status.Should().Be(HttpStatusCode.OK);
        back.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckDestFolder_FormFillingRoomThatRefusesADocument_SaysSoBeforeAnyCopyIsStarted()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest Flow Refused.docx", myDocsFolderId);
        var room = await CreateFillingFormsRoom("Autotest Flow Refused Room");

        // Act
        var verdict = await Preflight(PreflightKind.CheckDestFolder, fileIds: [file.Id], destFolderId: room.Id);

        // Assert
        verdict.Result.Should().Be(CheckDestFolderResult.NoneAllowed);

        // Checking is not acting: the room is still empty and no operation exists.
        (await Snapshot(room.Id))[room.Id].Should().BeEmpty();
        await AssertNoOperationQueued();
    }

    private static BatchRequestDto BuildRequest(int destFolderId, FileConflictResolveType conflictResolveType, int[] fileIds)
    {
        return new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(destFolderId),
            ConflictResolveType = conflictResolveType,
            FileIds = [.. fileIds.Select(id => new BatchRequestDtoAllOfFileIds(id))],
            FolderIds = [],
            DeleteAfter = false,
            ReturnSingleOperation = true
        };
    }
}
