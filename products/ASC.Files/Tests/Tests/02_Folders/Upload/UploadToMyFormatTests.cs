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
/// <c>POST /api/2.0/files/@my/upload</c> - the response's <c>fileExst</c> matches the extension of
/// the uploaded file name, across a spread of common formats.
/// </summary>
[Trait("Category", "Functional")]
[Trait("Feature", "Upload")]
public class UploadToMyFormatTests(
    AspireAppFixture fixture)
    : UploadTestBase(fixture)
{
    [Theory]
    [InlineData("autotest-my-format.pdf", "application/pdf", ".pdf")]
    [InlineData("autotest-my-format.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx")]
    [InlineData("autotest-my-format.pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation", ".pptx")]
    [InlineData("autotest-my-format.png", "image/png", ".png")]
    [InlineData("autotest-my-format.jpg", "image/jpeg", ".jpg")]
    [InlineData("autotest-my-format.zip", "application/zip", ".zip")]
    [InlineData("autotest-my-format.csv", "text/csv", ".csv")]
    [InlineData("autotest-my-format.md", "text/markdown", ".md")]
    public async Task UploadToMy_VariousFormats_ReturnsCorrectFileExst(string fileName, string contentType, string expectedExst)
    {
        var uploaded = (await UploadToMyAsync("fake file content"u8.ToArray(), fileName, contentType))[0];

        uploaded.FileExst.Should().Be(expectedExst);
    }
}
