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
/// Covers how <c>POST /files/@my/file</c> and <c>POST /files/:folderId/file</c> normalize the
/// requested title into a final file name.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class FileCreateVariantsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task CreateFileInMyDocuments_TitleWithoutExtension_GetsDocxExtension()
    {
        await _filesClient.Authenticate(Owner);

        var result = (await _filesApi.CreateFileInMyDocumentsAsync(
            new CreateFileJsonElement("Autotest Document"), TestContext.Current.CancellationToken)).Response;

        result.Title.Should().Be("Autotest Document.docx");
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateFileInMyDocuments_TitleWithDocxExtension_StaysDocx()
    {
        await _filesClient.Authenticate(Owner);

        var result = (await _filesApi.CreateFileInMyDocumentsAsync(
            new CreateFileJsonElement("Autotest Document.docx"), TestContext.Current.CancellationToken)).Response;

        result.Title.Should().Be("Autotest Document.docx");
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateFileInMyDocuments_TitleWithTxtExtension_ConvertedToDocx()
    {
        await _filesClient.Authenticate(Owner);

        var result = (await _filesApi.CreateFileInMyDocumentsAsync(
            new CreateFileJsonElement("Autotest Document.txt"), TestContext.Current.CancellationToken)).Response;

        result.Title.Should().Be("Autotest Document.docx");
        result.Id.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// BUG 80324: creating a file with an unknown extension (<c>.md</c>) did not keep it — the
    /// server rewrote the title to <c>.docx</c> (and could throw a <c>NullReferenceException</c>
    /// mapped to 403). Fixed by making <c>CreateNewFileAsync</c> keep unknown extensions and the
    /// <c>FileUtility.ExtsKeepOnCreate</c> list (.md/.markdown) verbatim.
    /// </summary>
    [Fact]
    [Trait("Bug", "80324")]
    public async Task CreateFileInMyDocuments_UnknownExtension_KeepsOriginalExtension()
    {
        await _filesClient.Authenticate(Owner);

        var result = (await _filesApi.CreateFileInMyDocumentsAsync(
            new CreateFileJsonElement("Autotest Document.md", enableExternalExt: false), TestContext.Current.CancellationToken)).Response;

        result.Title.Should().Be("Autotest Document.md");
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateFile_InRoom_ReturnsFileWithMatchingFolder()
    {
        await _filesClient.Authenticate(Owner);

        var room = await CreateCustomRoom("Autotest Room For File Creation " + Guid.NewGuid().ToString()[..8]);

        var result = (await _filesApi.CreateFileAsync(
            room.Id, new CreateFileJsonElement("Autotest Document"), TestContext.Current.CancellationToken)).Response;

        result.Title.Should().Be("Autotest Document.docx");
        result.FolderId.Should().Be(room.Id);
        result.Id.Should().BeGreaterThan(0);
    }
}
