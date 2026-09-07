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
/// Covers <c>POST /files/:folderId/html</c> and <c>POST /files/@my/html</c>: content requirement
/// and the (inverted) <c>createNewIfExist</c> deduplication semantics.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class HtmlFileCreateTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task CreateHtmlFile_InRoom_CreatesFileWithTitleAndContent()
    {
        await _filesClient.Authenticate(Owner);

        var room = await CreateCustomRoom("Autotest Room For HTML File " + Guid.NewGuid().ToString()[..8]);

        var result = (await _filesApi.CreateHtmlFileAsync(
            room.Id,
            new CreateTextOrHtmlFile("Autotest HTML File", "some text", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        result.Title.Should().Be("Autotest HTML File.html");
        result.FolderId.Should().Be(room.Id);
        result.Id.Should().BeGreaterThan(0);
    }

    // Note: createNewIfExist logic is inverted - true returns the existing file, false creates a new one with a suffix.
    [Fact]
    public async Task CreateHtmlFile_InRoom_CreateNewIfExistTrue_ReturnsExistingFile()
    {
        await _filesClient.Authenticate(Owner);

        var room = await CreateCustomRoom("Autotest Room For HTML Dedup " + Guid.NewGuid().ToString()[..8]);

        var first = (await _filesApi.CreateHtmlFileAsync(
            room.Id,
            new CreateTextOrHtmlFile("Autotest HTML Dedup", "some text", createNewIfExist: false),
            TestContext.Current.CancellationToken)).Response;

        var second = (await _filesApi.CreateHtmlFileAsync(
            room.Id,
            new CreateTextOrHtmlFile("Autotest HTML Dedup", "some text", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        second.Id.Should().Be(first.Id);
    }

    [Fact]
    public async Task CreateHtmlFileInMyDocuments_ValidContent_ReturnsNewHtmlFile()
    {
        await _filesClient.Authenticate(Owner);

        var result = (await _filesApi.CreateHtmlFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest HTML My Docs File", "<p>Hello world</p>", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        result.Title.Should().Be("Autotest HTML My Docs File.html");
        result.Id.Should().BeGreaterThan(0);
        result.FolderId.Should().BeGreaterThan(0);
        result.FileExst.Should().Be(".html");
    }

    // Note: content is optional in the SDK's DTO but required by the API - returns 400 without it.
    [Fact]
    public async Task CreateHtmlFileInMyDocuments_MissingContent_Returns400()
    {
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.CreateHtmlFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest HTML My Docs No Content"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateHtmlFileInMyDocuments_CreateNewIfExistTrue_ReturnsExistingFile()
    {
        await _filesClient.Authenticate(Owner);

        var first = (await _filesApi.CreateHtmlFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest HTML My Docs Dedup", "<p>First</p>", createNewIfExist: false),
            TestContext.Current.CancellationToken)).Response;

        var second = (await _filesApi.CreateHtmlFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest HTML My Docs Dedup", "<p>Second</p>", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        second.Id.Should().Be(first.Id);
    }

    [Fact]
    public async Task CreateHtmlFileInMyDocuments_CreateNewIfExistFalse_CreatesNewFileWithSuffix()
    {
        await _filesClient.Authenticate(Owner);

        var first = (await _filesApi.CreateHtmlFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest HTML My Docs Suffix", "<p>First</p>", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        var second = (await _filesApi.CreateHtmlFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest HTML My Docs Suffix", "<p>Second</p>", createNewIfExist: false),
            TestContext.Current.CancellationToken)).Response;

        second.Id.Should().NotBe(first.Id);
    }

    // Default behavior when createNewIfExist is omitted: same as false, a new file with a suffix.
    [Fact]
    public async Task CreateHtmlFileInMyDocuments_CreateNewIfExistOmitted_CreatesNewFileWithSuffix()
    {
        await _filesClient.Authenticate(Owner);

        var first = (await _filesApi.CreateHtmlFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest HTML My Docs Default", "<p>First</p>"), TestContext.Current.CancellationToken)).Response;

        var second = (await _filesApi.CreateHtmlFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest HTML My Docs Default", "<p>Second</p>"), TestContext.Current.CancellationToken)).Response;

        second.Id.Should().NotBe(first.Id);
    }
}
