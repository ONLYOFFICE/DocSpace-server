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

namespace ASC.AI.Tests.Tests.TextToDocxTests;

/// <summary>
/// The target folder of an export: required, of the right type, and reachable. Folder ids 0, -1 and a non-existent id
/// are in <see cref="TextToDocxPublishTests"/>. Ported from messages.spec.ts, "AI Messages - text-to-docx
/// validation".
/// </summary>
[Trait("Category", "API")]
[Trait("Feature", "AI/TextToDocx")]
public class TextToDocxFolderIdTests(AspireAppFixture fixture) : TextToDocxPortTestBase(fixture)
{
    /// <summary>
    /// The 400s prove what came back; this proves the queue was never touched — a usable title refused for empty
    /// content leaves the folder exactly as it was.
    /// </summary>
    [Fact]
    public async Task Export_RejectedBody_CreatesNoDocument()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();
        var before = (await WaitForStableFolderFilesAsync(folderId)).Select(file => file.Id).ToList();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _aiExport.AiExportTextToDocxAsync(BuildExport($"Exported {RandomSuffix()}", "", folderId), TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(400);

        await WaitForExportToSettleAsync();
        (await ListFolderFilesAsync(folderId)).Select(file => file.Id).Should().BeEquivalentTo(before);
    }

    /// <summary>A missing or null <c>folderId</c> — raw, since the SDK constructor rejects a null one client-side.</summary>
    [Theory]
    [InlineData("""{"title":"T","content":"hello"}""")]
    [InlineData("""{"title":"T","content":"hello","folderId":null}""")]
    public async Task Export_MissingOrNullFolderId_IsRejected(string body)
    {
        var result = await PostExportRawAsync(body);

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest, result.Body);
        result.Error.Should().Be(FolderIdRequiredError);
    }

    /// <summary>
    /// A valid folder id sent as a string used to crash the request with a 500 (Bug 82713). The crash is fixed: the
    /// route takes <c>folderId</c> as "int or string", and a string is a third-party folder id
    /// (<c>MdToDocxTaskPublisher.PublishAsync</c> reads only an int as a portal folder), so the numeric string names
    /// no third-party folder and is refused as not found — the TypeScript 400 predates that contract. (The 0 and -1
    /// shapes of the same bug are in <see cref="TextToDocxPublishTests"/>.)
    /// </summary>
    [Fact]
    [Trait("Bug", "82713")]
    public async Task Export_FolderIdSentAsString_IsAClientError()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();
        var request = new AiExportTextToDocxRequest(
            title: "T",
            content: "hello",
            folderId: new AiExportTextToDocxRequestFolderId(folderId.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        var result = await TryExportAsync(request);

        result.StatusCode.Should().Be(HttpStatusCode.NotFound, "a string folderId is looked up as a third-party folder, and there is none");
    }

    [Fact]
    public async Task Export_IntoDeletedFolder_IsNotFound()
    {
        var myFolderId = await GetMyDocumentsFolderIdAsync();
        var subFolderId = await CreateFolderAsync(myFolderId, "Autotest TextToDocx Deleted Folder");

        // The folder is reachable first, so the refusal below is about it being gone.
        await ExportAcceptedAsync($"Exported {RandomSuffix()}", "hello", subFolderId);

        await FoldersApi.DeleteFolderAsync(subFolderId, new DeleteFolderRequest(deleteAfter: false, immediately: true), TestContext.Current.CancellationToken);
        await WaitForFileOperationsAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _aiExport.AiExportTextToDocxAsync(BuildExport("T", "hello", subFolderId), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task Export_FileIdAsFolderId_IsNotFound()
    {
        await GetMyDocumentsFolderIdAsync();

        var fileId = (await FilesApi.CreateFileInMyDocumentsAsync(new CreateFileRequest("Autotest TextToDocx Target.docx"), TestContext.Current.CancellationToken)).Response.Id;

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _aiExport.AiExportTextToDocxAsync(BuildExport("T", "hello", fileId), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }
}
