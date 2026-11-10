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

namespace ASC.Files.Tests.Tests._01_Files.Create;

/// <summary>
/// A form-filling room only takes PDF forms. Creating anything else in it must be refused with a
/// client error.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "Files")]
public class FormsRoomNonPdfCreateTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private async Task<int> StatusOfCreate(Func<int, Task> create)
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest Non-PDF Forms " + Guid.NewGuid().ToString()[..8]);

        try
        {
            await create(room.Id);

            return 200;
        }
        catch (ApiException e)
        {
            return e.ErrorCode;
        }
    }

    /// <summary>
    /// The portal used to report the rejection as an unhandled exception, a 500, for every non-PDF creation. The
    /// refusal is now an InvalidOperationException, answered with 403 as the copy and move checks do.
    /// </summary>
    [Fact]
    [Trait("Bug", "84840")]
    public async Task CreateHtmlFile_FormFillingRoom_Returns403()
    {
        var status = await StatusOfCreate(async roomId => await _filesApi.CreateHtmlFileAsync(
            roomId, new CreateTextOrHtmlFileRequest("X", "<p/>"), TestContext.Current.CancellationToken));

        status.Should().Be(403);
    }

    [Fact]
    [Trait("Bug", "84840")]
    public async Task CreateTextFile_FormFillingRoom_Returns403()
    {
        var status = await StatusOfCreate(async roomId => await _filesApi.CreateTextFileAsync(
            roomId, new CreateTextOrHtmlFileRequest("X", "x"), TestContext.Current.CancellationToken));

        status.Should().Be(403);
    }

    [Fact]
    [Trait("Bug", "84840")]
    public async Task CreateFile_Docx_FormFillingRoom_Returns403()
    {
        var status = await StatusOfCreate(async roomId => await _filesApi.CreateFileAsync(
            roomId, new CreateFileRequest("X.docx"), TestContext.Current.CancellationToken));

        status.Should().Be(403);
    }
}
