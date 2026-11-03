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
/// <c>GET /api/2.0/files/fileops/checkdestfolder</c> (<c>checkMoveOrCopyDestFolder</c>) — the verdict.
/// <c>result</c> is AllAllowed / PartAllowed / NoneAllowed and <c>files</c> names the accepted ones;
/// only <c>fileIds</c> and <c>destFolderId</c> are read, and a form-filling room accepts PDF forms
/// only. The plain cases (AllAllowed, NoneAllowed, the 400/403/404 family) live in
/// <c>CheckDestFolder</c>.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class CheckDestFolderVerdictTests(
    AspireAppFixture fixture)
    : CheckPreflightTestBase(fixture)
{
    [Fact]
    public async Task CheckDestFolder_FormAndTextDocumentTogether_IsPartAllowedNamingOnlyTheForm()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var formId = await UploadOoFormAsync(myDocsFolderId);
        var plain = await CreateFile("Autotest Dest Part Plain.docx", myDocsFolderId);
        var room = await CreateFillingFormsRoom("Autotest Dest Part Room");

        // Act
        var response = await Preflight(PreflightKind.CheckDestFolder, fileIds: [plain.Id, formId], destFolderId: room.Id);

        // Assert
        response.Status.Should().Be(HttpStatusCode.OK);
        response.Result.Should().Be(CheckDestFolderResult.PartAllowed);
        IdsOf(response).Should().Equal(formId);
    }

    [Fact]
    public async Task CheckDestFolder_FolderIdsAndCopyOptions_PlayNoPartInTheAnswer()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var plain = await CreateFile("Autotest Dest Ignored Plain.docx", myDocsFolderId);
        var folder = await CreateFolder("Autotest Dest Ignored Folder", myDocsFolderId);
        var room = await CreateFillingFormsRoom("Autotest Dest Ignored Room");

        // Act
        var onlyFile = await Preflight(PreflightKind.CheckDestFolder, fileIds: [plain.Id], destFolderId: room.Id);
        var withExtras = await Preflight(
            PreflightKind.CheckDestFolder,
            fileIds: [plain.Id],
            folderIds: [folder.Id],
            destFolderId: room.Id,
            conflictResolveType: FileConflictResolveType.Overwrite);

        // Assert
        onlyFile.Status.Should().Be(HttpStatusCode.OK);
        withExtras.Status.Should().Be(HttpStatusCode.OK);
        withExtras.Result.Should().Be(onlyFile.Result);
        withExtras.Result.Should().Be(CheckDestFolderResult.NoneAllowed);
        IdsOf(withExtras).Should().Equal(IdsOf(onlyFile));
    }

    [Fact]
    public async Task CheckDestFolder_SameNamedFileAtDestination_IsNotAReasonToRefuse()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest Dest Same Name.docx", myDocsFolderId);
        var room = await CreateCustomRoom("Autotest Dest Same Name Room");
        await CreateFile("Autotest Dest Same Name.docx", room.Id);

        // Positive control: the other preflight does see the clash.
        var clash = await Preflight(PreflightKind.Move, fileIds: [file.Id], destFolderId: room.Id);
        IdsOf(clash).Should().Equal(file.Id);

        // Act
        var response = await Preflight(PreflightKind.CheckDestFolder, fileIds: [file.Id], destFolderId: room.Id);

        // Assert
        response.Status.Should().Be(HttpStatusCode.OK);
        response.Result.Should().Be(CheckDestFolderResult.AllAllowed);
    }

    [Fact]
    public async Task CheckDestFolder_EmptySelection_Returns200AndNamesNoFiles()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Dest Empty Room");

        // Act
        var response = await Preflight(PreflightKind.CheckDestFolder, destFolderId: room.Id);

        // Assert
        response.Status.Should().Be(HttpStatusCode.OK);
        response.Entries.Should().BeEmpty();
    }
}
