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

namespace ASC.Files.Tests.Tests._02_Folders.Upload;

/// <summary>
/// A form-filling room only takes PDF forms. A single-request upload of anything else used to reach the same refusal
/// as the creation endpoints and was answered with 500; it is now a 403, and nothing stays in the room.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "Upload")]
public class FormsRoomNonPdfUploadTests(
    AspireAppFixture fixture)
    : UploadTestBase(fixture)
{
    private const string FileName = "Autotest Non-PDF Upload.txt";

    private static readonly byte[] _content = "Autotest non-PDF content"u8.ToArray();

    private async Task<int> CreateRoomAsOwner()
    {
        await _filesClient.Authenticate(Owner);

        return (await CreateFillingFormsRoom("Autotest Non-PDF Upload " + Guid.NewGuid().ToString()[..8])).Id;
    }

    [Fact]
    [Trait("Bug", "84840")]
    public async Task UploadFile_NonPdf_FormFillingRoom_Returns403AndStoresNothing()
    {
        var roomId = await CreateRoomAsOwner();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadToFolderAsync(roomId, _content, FileName));

        exception.ErrorCode.Should().Be(403);
        (await GetFolderFilesAsync(roomId)).Should().BeEmpty();
    }

    [Fact]
    [Trait("Bug", "84840")]
    public async Task InsertFile_NonPdf_FormFillingRoom_Returns403AndStoresNothing()
    {
        var roomId = await CreateRoomAsOwner();
        using var stream = new MemoryStream(_content);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.InsertFileAsync(
                roomId,
                new FileParameter(FileName, "text/plain", stream),
                FileName,
                cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        (await GetFolderFilesAsync(roomId)).Should().BeEmpty();
    }
}
