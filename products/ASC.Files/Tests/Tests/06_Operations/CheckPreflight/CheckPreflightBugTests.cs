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
/// Open defects of the preflight endpoints that have no bug number yet (the TypeScript suite marks
/// them <c>BUG-PENDING</c>): the expected behaviour is that a preflight agrees with the operation it
/// announces. Both were reproduced with the real <c>PUT /fileops/copy</c> on the same inputs (copy
/// only, so nothing is removed), so the contract is the operation's own answer.
/// </summary>
[Trait("Category", "Bug")]
[Trait("Feature", "Files")]
public class CheckPreflightBugTests(
    AspireAppFixture fixture)
    : CheckPreflightTestBase(fixture)
{
    public static TheoryData<PreflightKind, FolderType> SectionRoots
    {
        get
        {
            var data = new TheoryData<PreflightKind, FolderType>();

            foreach (var kind in new[] { PreflightKind.Move, PreflightKind.CheckDestFolder })
            {
                data.Add(kind, FolderType.VirtualRooms);
                data.Add(kind, FolderType.Forms);
                data.Add(kind, FolderType.AiAgents);
            }

            return data;
        }
    }

    /// <summary>
    /// <c>checkdestfolder</c> counts the repeated id as a refused file: the verdict is PartAllowed
    /// although the one real file is accepted.
    /// </summary>
    [Fact]
    public async Task CheckDestFolder_RepeatedFileId_DoesNotTurnAllAllowedIntoPartAllowed()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest Dest Repeated.docx", myDocsFolderId);
        var room = await CreateCustomRoom("Autotest Dest Repeated Room");

        // Premise: the same file listed once is fully accepted.
        var once = await Preflight(PreflightKind.CheckDestFolder, fileIds: [file.Id], destFolderId: room.Id);
        once.Status.Should().Be(HttpStatusCode.OK);
        once.Result.Should().Be(CheckDestFolderResult.AllAllowed);

        // Act
        var twice = await Preflight(PreflightKind.CheckDestFolder, fileIds: [file.Id, file.Id], destFolderId: room.Id);

        // Assert
        twice.Status.Should().Be(HttpStatusCode.OK);
        twice.Result.Should().Be(CheckDestFolderResult.AllAllowed);
        IdsOf(twice).Should().Equal(file.Id);
    }

    /// <summary>
    /// <c>content=true</c> is not taken into account by <c>move</c>: the preflight answers <c>[]</c>
    /// although both files of the folder clash with the destination, which the real copy of the folder
    /// content resolves as clashes.
    /// </summary>
    [Fact]
    public async Task CheckMove_ContentTrue_ReportsTheClashesThatTheRealCopyResolves()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var source = await CreateFolder("Autotest Content Source", myDocsFolderId);
        var dest = await CreateFolder("Autotest Content Dest", myDocsFolderId);
        var sourceA = await CreateFile("a.docx", source.Id);
        var sourceB = await CreateFile("b.docx", source.Id);
        var destA = await CreateFile("a.docx", dest.Id);
        var destB = await CreateFile("b.docx", dest.Id);
        var untouched = await Snapshot(dest.Id);

        // Premise 1: addressing the same files by id, the preflight does see both clashes.
        var byId = await Preflight(PreflightKind.Move, fileIds: [sourceA.Id, sourceB.Id], destFolderId: dest.Id);
        IdsOf(byId).Should().Equal(new[] { sourceA.Id, sourceB.Id }.Order());

        // Premise 2: the real copy of the folder content treats them as clashes too: with Skip it
        // leaves the destination exactly as it was.
        var copy = (await _filesOperationsApi.CopyBatchItemsAsync(
            new BatchRequestDto
            {
                FolderIds = [new BatchRequestDtoAllOfFolderIds(source.Id)],
                FileIds = [],
                Content = true,
                DestFolderId = new BatchRequestDtoAllOfDestFolderId(dest.Id),
                ConflictResolveType = FileConflictResolveType.Skip,
                DeleteAfter = false,
                ReturnSingleOperation = true
            },
            TestContext.Current.CancellationToken)).Response;
        await WaitLongOperation(copy.FirstOrDefault()?.Id);

        var afterCopy = await Snapshot(dest.Id);
        afterCopy.Should().BeEquivalentTo(untouched);
        untouched[dest.Id].Should().HaveCount(2);
        untouched[dest.Id].Should().Contain(e => e.StartsWith($"{destA.Id}:"));
        untouched[dest.Id].Should().Contain(e => e.StartsWith($"{destB.Id}:"));

        // Act
        var response = await Preflight(PreflightKind.Move, folderIds: [source.Id], destFolderId: dest.Id, content: true);

        // Assert
        response.Status.Should().Be(HttpStatusCode.OK);
        TitlesOf(response).Should().Equal("a.docx", "b.docx");
    }

    /// <summary>
    /// The real copy refuses a section root (Rooms, Forms, AI agents) as a destination with 403 and
    /// creates nothing; the preflights answer something else for the same destination.
    /// </summary>
    [Theory]
    [MemberData(nameof(SectionRoots))]
    public async Task Preflight_SectionRootAsDestination_IsRefusedWith403AsTheRealCopyRefusesIt(PreflightKind kind, FolderType rootType)
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest Root Probe.docx", myDocsFolderId);
        var rootId = await GetFolderIdAsync(rootType, Owner);
        rootId.Should().BeGreaterThan(0);
        var rootBefore = await Snapshot(rootId);

        // Premise: the real operation refuses this destination and creates nothing.
        var refused = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                new BatchRequestDto
                {
                    FileIds = [new BatchRequestDtoAllOfFileIds(file.Id)],
                    FolderIds = [],
                    DestFolderId = new BatchRequestDtoAllOfDestFolderId(rootId),
                    ConflictResolveType = FileConflictResolveType.Skip,
                    DeleteAfter = false,
                    ReturnSingleOperation = true
                },
                TestContext.Current.CancellationToken));
        refused.ErrorCode.Should().Be(403);
        (await Snapshot(rootId)).Should().BeEquivalentTo(rootBefore);

        // Act
        var response = await Preflight(kind, fileIds: [file.Id], destFolderId: rootId);

        // Assert
        response.Status.Should().Be(HttpStatusCode.Forbidden);
    }
}
