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
/// Covers <c>POST /files/@my/text</c> and <c>POST /files/:folderId/text</c>: content requirement,
/// the (inverted) <c>createNewIfExist</c> deduplication semantics, and title validation.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class TextFileCreateTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task CreateTextFileInMyDocuments_ValidContent_ReturnsNewTextFile()
    {
        await _filesClient.Authenticate(Owner);

        var result = (await _filesApi.CreateTextFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest Text My Docs File", "Hello world", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        result.Title.Should().Be("Autotest Text My Docs File.txt");
        result.Id.Should().BeGreaterThan(0);
        result.FolderId.Should().BeGreaterThan(0);
        result.FileExst.Should().Be(".txt");
    }

    // Note: content is optional in the SDK's DTO but required by the API - returns 400 without it.
    [Fact]
    public async Task CreateTextFileInMyDocuments_MissingContent_Returns400()
    {
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.CreateTextFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest Text My Docs No Content"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    // Note: createNewIfExist logic is inverted - true returns the existing file, false creates a new one with a suffix.
    [Fact]
    public async Task CreateTextFileInMyDocuments_CreateNewIfExistTrue_ReturnsExistingFile()
    {
        await _filesClient.Authenticate(Owner);

        var first = (await _filesApi.CreateTextFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest Text My Docs Dedup", "First", createNewIfExist: false),
            TestContext.Current.CancellationToken)).Response;

        var second = (await _filesApi.CreateTextFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest Text My Docs Dedup", "Second", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        second.Id.Should().Be(first.Id);
    }

    [Fact]
    public async Task CreateTextFileInMyDocuments_CreateNewIfExistFalse_CreatesNewFileWithSuffix()
    {
        await _filesClient.Authenticate(Owner);

        var first = (await _filesApi.CreateTextFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest Text My Docs Suffix", "First", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        var second = (await _filesApi.CreateTextFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest Text My Docs Suffix", "Second", createNewIfExist: false),
            TestContext.Current.CancellationToken)).Response;

        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public async Task CreateTextFileInMyDocuments_CreateNewIfExistOmitted_CreatesNewFileWithSuffix()
    {
        await _filesClient.Authenticate(Owner);

        var first = (await _filesApi.CreateTextFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest Text My Docs Default", "First"), TestContext.Current.CancellationToken)).Response;

        var second = (await _filesApi.CreateTextFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest Text My Docs Default", "Second"), TestContext.Current.CancellationToken)).Response;

        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public async Task CreateTextFile_InRoom_CreatesFileWithTitleAndContent()
    {
        await _filesClient.Authenticate(Owner);

        var room = await CreateCustomRoom("Autotest Room For Text File " + Guid.NewGuid().ToString()[..8]);

        var result = (await _filesApi.CreateTextFileAsync(
            room.Id,
            new CreateTextOrHtmlFile("Autotest Text File", "some text", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        result.Title.Should().Be("Autotest Text File.txt");
        result.FolderId.Should().Be(room.Id);
        result.Id.Should().BeGreaterThan(0);
    }

    // Note: createNewIfExist logic is inverted - true returns the existing file, false creates a new one with a suffix.
    [Fact]
    public async Task CreateTextFile_InRoom_CreateNewIfExistTrue_ReturnsExistingFile()
    {
        await _filesClient.Authenticate(Owner);

        var room = await CreateCustomRoom("Autotest Room For Text Dedup " + Guid.NewGuid().ToString()[..8]);

        var first = (await _filesApi.CreateTextFileAsync(
            room.Id,
            new CreateTextOrHtmlFile("Autotest Text Dedup", "some text", createNewIfExist: false),
            TestContext.Current.CancellationToken)).Response;

        var second = (await _filesApi.CreateTextFileAsync(
            room.Id,
            new CreateTextOrHtmlFile("Autotest Text Dedup", "some text", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        second.Id.Should().Be(first.Id);
    }

    [Fact]
    public async Task CreateTextFile_InRoom_CreateNewIfExistFalse_CreatesNewFileWithSuffix()
    {
        await _filesClient.Authenticate(Owner);

        var room = await CreateCustomRoom("Autotest Room For Text Suffix " + Guid.NewGuid().ToString()[..8]);

        var first = (await _filesApi.CreateTextFileAsync(
            room.Id,
            new CreateTextOrHtmlFile("Autotest Text Suffix", "First", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        var second = (await _filesApi.CreateTextFileAsync(
            room.Id,
            new CreateTextOrHtmlFile("Autotest Text Suffix", "Second", createNewIfExist: false),
            TestContext.Current.CancellationToken)).Response;

        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public async Task CreateTextFile_InRoom_MissingContent_Returns400()
    {
        await _filesClient.Authenticate(Owner);

        var room = await CreateCustomRoom("Autotest Room For Text No Content " + Guid.NewGuid().ToString()[..8]);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.CreateTextFileAsync(
            room.Id, new CreateTextOrHtmlFile("Autotest Text No Content"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateTextFile_NonExistentFolderId_Returns404()
    {
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.CreateTextFileAsync(
            999999999, new CreateTextOrHtmlFile("Autotest Text Bad Folder", "some text"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateTextFile_InRoom_CreateNewIfExistOmitted_CreatesNewFileWithSuffix()
    {
        await _filesClient.Authenticate(Owner);

        var room = await CreateCustomRoom("Autotest Room For Text Default " + Guid.NewGuid().ToString()[..8]);

        var first = (await _filesApi.CreateTextFileAsync(
            room.Id, new CreateTextOrHtmlFile("Autotest Text Default", "First"), TestContext.Current.CancellationToken)).Response;

        var second = (await _filesApi.CreateTextFileAsync(
            room.Id, new CreateTextOrHtmlFile("Autotest Text Default", "Second"), TestContext.Current.CancellationToken)).Response;

        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    [Trait("Bug", "81440")]
    public async Task CreateTextFile_InRoom_EmptyTitle_Returns400()
    {
        await _filesClient.Authenticate(Owner);

        var room = await CreateCustomRoom("Autotest Room For Text Empty Title " + Guid.NewGuid().ToString()[..8]);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.CreateTextFileAsync(
            room.Id, new CreateTextOrHtmlFile("", "some text"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateTextFile_InRoom_TitleWithDiacriticalCharacters_IsPreserved()
    {
        await _filesClient.Authenticate(Owner);

        var room = await CreateCustomRoom("Autotest Room For Text Unicode " + Guid.NewGuid().ToString()[..8]);
        const string title = "Ünïcödé Café résumé naïve";

        var result = (await _filesApi.CreateTextFileAsync(
            room.Id, new CreateTextOrHtmlFile(title, "some text", createNewIfExist: true), TestContext.Current.CancellationToken)).Response;

        result.Title.Should().Be(title + ".txt");
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateTextFile_InRoom_VeryLongTitle_Returns400()
    {
        await _filesClient.Authenticate(Owner);

        var room = await CreateCustomRoom("Autotest Room For Text Long Title " + Guid.NewGuid().ToString()[..8]);
        var title = new string('A', 300);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.CreateTextFileAsync(
            room.Id, new CreateTextOrHtmlFile(title, "some text"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }
}
