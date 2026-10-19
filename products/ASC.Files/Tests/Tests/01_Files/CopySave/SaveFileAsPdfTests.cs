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
/// <c>POST /files/file/{id}/saveaspdf</c> — converting a file to PDF into a chosen folder.
/// </summary>
/// <remarks>
/// Only the cases that the TS suite (<c>files.spec.ts</c>, <c>conversion.spec.ts</c>) can decide
/// without a converted document are ported in full. Every other positive case of
/// <c>conversion.spec.ts</c> (the room-type matrix, the source-format matrix, the title rules, "every
/// call adds another PDF", the PDF magic-number check) needs a document server that can reach the
/// portal, and the integration-test AppHost does not provide one — see <see cref="SaveFileAsPdfTestBase"/>.
/// </remarks>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class SaveFileAsPdfTests(
    AspireAppFixture fixture)
    : SaveFileAsPdfTestBase(fixture)
{
    [Fact]
    public async Task SaveFileAsPdf_NonExistentSource_Returns404AndCreatesNothing()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Save PDF 404");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SaveFileAsPdfAsync(
                999999999,
                new SaveAsPdfRequest(room.Id, "X"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        (await GetFileTitles(room.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task SaveFileAsPdf_NonExistentDestinationFolder_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var source = await CreateFileInMy("Autotest Source File.docx", Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SaveFileAsPdfAsync(
                source.Id,
                new SaveAsPdfRequest(999999999, "X"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task SaveFileAsPdf_ArchivedDestinationRoom_Returns403AndCreatesNothing()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var source = await CreateFileInMy("Autotest Source File.docx", Owner);
        var room = await CreateCustomRoom("Autotest Save PDF Archived");
        await ArchiveRoom(room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SaveFileAsPdfAsync(
                source.Id,
                new SaveAsPdfRequest(room.Id, "X"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        (await GetFileTitles(room.Id)).Should().BeEmpty();
    }
}
