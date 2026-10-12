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

namespace ASC.Files.Tests.Tests._01_Files.Read;

[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class FileInfoTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetFileInfo_FileInMyDocuments_ReturnsCorrectMetadata()
    {
        var created = await CreateFileInMy("Autotest Get File Info", Owner);

        var file = await GetFile(created.Id);

        file.Id.Should().Be(created.Id);
        file.Title.Should().Be("Autotest Get File Info.docx");
        file.FileExst.Should().Be(".docx");
        file.Version.Should().Be(1);
        file.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetFileInfo_FileInRoom_ReturnsCorrectFolderId()
    {
        var room = await CreateCustomRoom("Autotest Room For Get File Info");

        var created = await CreateFile("Autotest Room File Info", room.Id);

        var file = await GetFile(created.Id);

        file.Id.Should().Be(created.Id);
        file.Title.Should().Be("Autotest Room File Info.docx");
        file.FolderId.Should().Be(room.Id);
    }

    [Fact]
    public async Task GetFileInfo_NonExistentFile_Returns404()
    {
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileInfoAsync(999999999, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }
}
