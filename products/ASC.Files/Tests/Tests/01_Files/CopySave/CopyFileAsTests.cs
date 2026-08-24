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

namespace ASC.Files.Tests.Tests._01_Files.CopySave;

/// <summary>
/// <c>POST /files/file/{fileId}/copyas</c> — copying a file under a new title into another folder.
/// </summary>
/// <remarks>
/// Only the same-extension case is covered here. <c>FilesControllerHelper.CopyFileAsAsync</c> takes
/// that fast path (<c>FileStorageService.CreateNewFileAsync</c>, a template-based copy) only when
/// the source and destination extensions match; any mismatch — <c>toForm</c>, or a non-standard
/// extension such as <c>.md</c> — falls through to <c>FileConverter.ExecAsync</c>, which asks the
/// ONLYOFFICE document server for a real conversion. The integration-test AppHost provisions no
/// document-server resource, so those cases cannot pass here; see the porting report for the two
/// TS cases dropped for this reason (and one already <c>test.skip</c>'d in the source suite).
/// </remarks>
/// <remarks>
/// <see cref="FileEntryBaseDto"/> — the type <see cref="FileEntryBaseWrapper.Response"/> is typed
/// as — carries <c>Title</c> but neither <c>Id</c> nor <c>FolderId</c>, so the copy's destination
/// is verified by listing the destination folder's content instead of reading the copy response
/// directly. This is an SDK model gap, not a preference.
/// </remarks>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class CopyFileAsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task CopyFileAs_SameExtension_CopiesToSpecifiedTitleAndDestination()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceFile = await CreateFileInMy("Autotest Source File.docx", Owner);
        var destRoom = await CreateCustomRoom("Autotest Room For Copy");

        var copyRequest = new CopyAsJsonElement(
            destTitle: "Autotest Copied File.docx",
            destFolderId: new CopyAsJsonElementDestFolderId(destRoom.Id));

        // Act
        var copied = (await _filesApi.CopyFileAsAsync(sourceFile.Id, copyRequest, TestContext.Current.CancellationToken)).Response;

        // Assert
        copied.Title.Should().Be("Autotest Copied File.docx");

        var destContent = (await _foldersApi.GetFolderByFolderIdAsync(destRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        destContent.Files.Should().Contain(f => f.Title == "Autotest Copied File.docx");
    }
}
