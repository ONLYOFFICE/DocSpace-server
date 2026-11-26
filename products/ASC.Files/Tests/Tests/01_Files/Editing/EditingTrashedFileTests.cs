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

namespace ASC.Files.Tests.Tests._01_Files.Editing;

/// <summary>
/// An editing session on a file in Trash - started with <c>editingAlone</c>, or refreshed through
/// <c>trackeditfile</c> - is refused with 403. The 403 comes from the edit right, checked by <c>CanEditAsync</c>:
/// Trash grants no edit right. These are characterization tests of that answer, not guards of the Trash check of
/// <c>EntryManager.TrackEditingAsync</c>, which raised a plain exception (a 500) and now raises a 403 of its own: a
/// caller never reaches it, so the tests pass with or without it (not proven red: guard unreachable behind
/// CanEditAsync).
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class EditingTrashedFileTests(AspireAppFixture fixture) : EditingTestBase(fixture)
{
    [Fact]
    public async Task StartEditFile_EditingAloneOnFileInTrash_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFile("Autotest Start Edit Trashed.docx", FolderType.USER, Owner);
        await MoveToTrash(file.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.StartEditFileAsync(file.Id, new StartEditRequest(editingAlone: true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task TrackEditFile_FileMovedToTrash_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFile("Autotest Track Edit Trashed.docx", FolderType.USER, Owner);
        var docKey = await GetDocKey(file.Id);
        await MoveToTrash(file.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.TrackEditFileAsync(file.Id, docKeyForTrack: docKey, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    private async Task MoveToTrash(int fileId)
    {
        var results = (await _filesApi.DeleteFileAsync(fileId, new DeleteFileRequest { Immediately = false }, true, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.First().Id) ?? [];
        }

        results.Should().NotContain(r => !string.IsNullOrEmpty(r.Error));
        (await _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response.FolderId
            .Should().Be(await GetTrashFolderIdAsync(Owner));
    }
}
