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
/// <c>GET /api/2.0/files/fileops/move</c> (<c>checkMoveOrCopyBatchItems</c>) — what exactly is reported.
/// The answer is the part of the request that clashes by name with an entry of the destination;
/// <c>[]</c> means no clash, <c>conflictResolveType</c> is ignored, unknown source ids are passed over
/// and nothing is changed. The coarser scenarios (a clash by title, an empty answer, 400/403/404)
/// live in <c>CheckMoveCopy</c>; these assert the reported ids and folder, which that folder's
/// title-only adapter cannot see.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class CheckMoveOrCopyReportTests(
    AspireAppFixture fixture)
    : CheckPreflightTestBase(fixture)
{
    [Fact]
    public async Task CheckMove_ClashingFile_IsReportedWithIdTitleAndFolderAndNothingChanges()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var source = await CreateFolder("Autotest Check Source", myDocsFolderId);
        var dest = await CreateFolder("Autotest Check Dest", myDocsFolderId);
        var clashing = await CreateFile("Report.docx", source.Id);
        await CreateFile("Report.docx", dest.Id);
        var before = await Snapshot(source.Id, dest.Id);

        // Act
        var response = await Preflight(PreflightKind.Move, fileIds: [clashing.Id], destFolderId: dest.Id);

        // Assert
        response.Status.Should().Be(HttpStatusCode.OK);
        response.Entries.Should().ContainSingle();
        response.Entries[0].Id.Should().Be(clashing.Id);
        response.Entries[0].Title.Should().Be("Report.docx");
        response.Entries[0].FolderId.Should().Be(source.Id);
        (await Snapshot(source.Id, dest.Id)).Should().BeEquivalentTo(before);
        await AssertNoOperationQueued();
    }

    [Fact]
    public async Task CheckMove_ClashingFolder_IsReportedByIdAndTitle()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var source = await CreateFolder("Autotest Check Folder Source", myDocsFolderId);
        var dest = await CreateFolder("Autotest Check Folder Dest", myDocsFolderId);
        var clashing = await CreateFolder("Archive 2026", source.Id);
        await CreateFolder("Archive 2026", dest.Id);

        // Act
        var response = await Preflight(PreflightKind.Move, folderIds: [clashing.Id], destFolderId: dest.Id);

        // Assert
        response.Status.Should().Be(HttpStatusCode.OK);
        IdsOf(response).Should().Equal(clashing.Id);
        TitlesOf(response).Should().Equal("Archive 2026");
    }

    [Fact]
    public async Task CheckMove_MixedSelection_ReportsOnlyTheClashingItemsEachOnce()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var source = await CreateFolder("Autotest Check Mixed Source", myDocsFolderId);
        var dest = await CreateFolder("Autotest Check Mixed Dest", myDocsFolderId);
        var clashFile = await CreateFile("Clash.docx", source.Id);
        var cleanFile = await CreateFile("Clean.docx", source.Id);
        var clashFolder = await CreateFolder("ClashFolder", source.Id);
        var cleanFolder = await CreateFolder("CleanFolder", source.Id);
        await CreateFile("Clash.docx", dest.Id);
        await CreateFolder("ClashFolder", dest.Id);

        // Act
        var response = await Preflight(
            PreflightKind.Move,
            fileIds: [clashFile.Id, cleanFile.Id],
            folderIds: [clashFolder.Id, cleanFolder.Id],
            destFolderId: dest.Id);

        // Assert
        response.Status.Should().Be(HttpStatusCode.OK);
        IdsOf(response).Should().Equal(new[] { clashFile.Id, clashFolder.Id }.Order());
        TitlesOf(response).Should().Equal("Clash.docx", "ClashFolder");
    }

    [Fact]
    public async Task CheckMove_NamesDifferingOnlyByCaseOrUsingNonLatinLetters_StillClash()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var source = await CreateFolder("Autotest Check Case Source", myDocsFolderId);
        var dest = await CreateFolder("Autotest Check Case Dest", myDocsFolderId);
        var lower = await CreateFile("budget.docx", source.Id);
        var unicode = await CreateFile("Отчёт Ünï.docx", source.Id);
        await CreateFile("BUDGET.DOCX", dest.Id);
        await CreateFile("Отчёт Ünï.docx", dest.Id);

        // Act
        var response = await Preflight(PreflightKind.Move, fileIds: [lower.Id, unicode.Id], destFolderId: dest.Id);

        // Assert
        response.Status.Should().Be(HttpStatusCode.OK);
        IdsOf(response).Should().Equal(new[] { lower.Id, unicode.Id }.Order());
    }

    [Fact]
    public async Task CheckMove_ClashInRoomSubfolder_IsReportedForTheSubfolderOnlyNotForTheRoomRoot()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest Check Nested.docx", myDocsFolderId);
        var room = await CreateCustomRoom("Autotest Check Nested Room");
        var sub = await CreateFolder("Inner", room.Id);
        await CreateFile("Autotest Check Nested.docx", sub.Id);

        // Act
        var inSub = await Preflight(PreflightKind.Move, fileIds: [file.Id], destFolderId: sub.Id);
        var inRoot = await Preflight(PreflightKind.Move, fileIds: [file.Id], destFolderId: room.Id);

        // Assert
        inSub.Status.Should().Be(HttpStatusCode.OK);
        IdsOf(inSub).Should().Equal(file.Id);
        inRoot.Status.Should().Be(HttpStatusCode.OK);
        inRoot.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckMove_RepeatedId_ReportsTheItemOnce()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var source = await CreateFolder("Autotest Check Dup Source", myDocsFolderId);
        var dest = await CreateFolder("Autotest Check Dup Dest", myDocsFolderId);
        var file = await CreateFile("Twice.docx", source.Id);
        await CreateFile("Twice.docx", dest.Id);

        // Act
        var response = await Preflight(PreflightKind.Move, fileIds: [file.Id, file.Id], destFolderId: dest.Id);

        // Assert
        response.Status.Should().Be(HttpStatusCode.OK);
        IdsOf(response).Should().Equal(file.Id);
    }

    [Fact]
    public async Task CheckMove_UnknownSourceId_IsPassedOverAndTheRealClashIsStillReported()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var source = await CreateFolder("Autotest Check Unknown Source", myDocsFolderId);
        var dest = await CreateFolder("Autotest Check Unknown Dest", myDocsFolderId);
        var file = await CreateFile("Real.docx", source.Id);
        await CreateFile("Real.docx", dest.Id);

        // Act
        var onlyUnknown = await Preflight(PreflightKind.Move, fileIds: [99999999], destFolderId: dest.Id);
        var withReal = await Preflight(PreflightKind.Move, fileIds: [99999999, file.Id], destFolderId: dest.Id);

        // Assert
        onlyUnknown.Status.Should().Be(HttpStatusCode.OK);
        onlyUnknown.Entries.Should().BeEmpty();
        withReal.Status.Should().Be(HttpStatusCode.OK);
        IdsOf(withReal).Should().Equal(file.Id);
    }

    [Theory]
    [InlineData(FileConflictResolveType.Skip)]
    [InlineData(FileConflictResolveType.Overwrite)]
    [InlineData(FileConflictResolveType.Duplicate)]
    public async Task CheckMove_ConflictResolveType_DoesNotChangeWhatIsReported(FileConflictResolveType conflictResolveType)
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var source = await CreateFolder($"Autotest Check Resolve Source {conflictResolveType}", myDocsFolderId);
        var dest = await CreateFolder($"Autotest Check Resolve Dest {conflictResolveType}", myDocsFolderId);
        var file = await CreateFile("Resolve.docx", source.Id);
        await CreateFile("Resolve.docx", dest.Id);

        // Act
        var response = await Preflight(
            PreflightKind.Move, fileIds: [file.Id], destFolderId: dest.Id, conflictResolveType: conflictResolveType);

        // Assert
        response.Status.Should().Be(HttpStatusCode.OK);
        IdsOf(response).Should().Equal(file.Id);
    }
}
