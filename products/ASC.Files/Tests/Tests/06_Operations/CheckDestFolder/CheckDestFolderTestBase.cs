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

namespace ASC.Files.Tests.Tests._06_Operations.CheckDestFolder;

/// <summary>
/// Shared setup for the <c>GET /api/2.0/files/fileops/checkdestfolder</c> suites
/// (<c>checkMoveOrCopyDestFolder</c>).
/// </summary>
public abstract class CheckDestFolderTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>Calls checkMoveOrCopyDestFolder for the given source files/folders and destination.</summary>
    /// <summary>A checked file as the tests see it: only the title survives the response model.</summary>
    protected sealed record CheckedFile(string Title);

    /// <summary>The parsed response of <c>GET /files/fileops/checkdestfolder</c>.</summary>
    protected sealed record DestFolderCheck(CheckDestFolderResult Result, List<CheckedFile> Files);

    /// <remarks>
    /// Sent over raw HTTP, not through the generated client: the endpoint is bound by
    /// <c>BatchModelBinder</c> (<c>products/ASC.Files/Core/ApiModels/Binders.cs:215</c>), which reads
    /// flat query parameters (<c>fileIds</c>, <c>destFolderId</c>, ...), while the generated
    /// <c>OperationsApi.CheckMoveOrCopyDestFolderAsync</c> sends the whole DTO under a single
    /// <c>inDto</c> query key. The binder then never sees <c>destFolderId</c> and the controller dies
    /// calling <c>GetString()</c> on an undefined <c>JsonElement</c>. An SDK/OpenAPI generation
    /// defect — the same one <c>CheckMoveCopy</c> works around — not a preference.
    /// </remarks>
    protected async Task<DestFolderCheck> CheckDestFolder(
        int[]? fileIds = null,
        int[]? folderIds = null,
        int? destFolderId = null,
        FileConflictResolveType conflictResolveType = FileConflictResolveType.Skip,
        bool deleteAfter = true)
    {
        var query = new List<string>();

        if (destFolderId != null)
        {
            query.Add($"destFolderId={destFolderId.Value}");
        }

        query.AddRange((fileIds ?? []).Select(id => $"fileIds={id}"));
        query.AddRange((folderIds ?? []).Select(id => $"folderIds={id}"));
        query.Add($"conflictResolveType={conflictResolveType}");
        query.Add($"deleteAfter={(deleteAfter ? "true" : "false")}");

        var path = "api/2.0/files/fileops/checkdestfolder?" + string.Join("&", query);

        using var response = await _filesClient.GetAsync(path, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException((int)response.StatusCode, $"Error calling CheckMoveOrCopyDestFolder: {body}");
        }

        using var json = JsonDocument.Parse(body);
        var payload = json.RootElement.GetProperty("response");

        var result = payload.TryGetProperty("result", out var resultElement)
            ? resultElement.ValueKind == JsonValueKind.Number
                ? (CheckDestFolderResult)resultElement.GetInt32()
                : Enum.Parse<CheckDestFolderResult>(resultElement.GetString()!, ignoreCase: true)
            : CheckDestFolderResult.AllAllowed;

        var files = new List<CheckedFile>();

        if (payload.TryGetProperty("files", out var filesElement) && filesElement.ValueKind == JsonValueKind.Array)
        {
            files.AddRange(filesElement.EnumerateArray()
                .Select(f => new CheckedFile(f.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "")));
        }

        return new DestFolderCheck(result, files);
    }

    /// <summary>
    /// Uploads the shared <c>new.pdf</c> asset — a real ONLYOFFICE PDF form — into the given folder
    /// through a chunked upload session, which is how the product's <c>FileChecker</c> gets to
    /// inspect real content and tag the file <c>FilterType.PdfForm</c> on finalize. This is the same
    /// technique <c>FormsTestBase.UploadOoFormAsync</c> uses; it is duplicated here (rather than
    /// reused across a namespace boundary) because usings may only live in <c>GlobalUsings.cs</c>,
    /// which this task is not permitted to touch.
    /// </summary>
    protected async Task<int> UploadOoFormAsync(int folderId, string fileName = "Autotest CheckDestFolder Form.pdf")
    {
        using var content = new MemoryStream();

        await using (var stream = typeof(CheckDestFolderTestBase).Assembly.GetManifestResourceStream("ASC.Files.Tests.Data.new.pdf")!)
        {
            await stream.CopyToAsync(content, TestContext.Current.CancellationToken);
        }

        var bytes = content.ToArray();

        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken)).Response;

        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            folderId,
            new SessionRequest(fileName, bytes.Length),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        var chunkSize = (int)settings.ChunkUploadSize;
        var chunkNumber = 1;

        for (var offset = 0; offset < bytes.Length; offset += chunkSize)
        {
            var length = Math.Min(chunkSize, bytes.Length - offset);
            await using var chunkStream = new MemoryStream(bytes, offset, length);

            await _filesOperationsApi.UploadAsyncSessionAsync(
                folderId,
                session.Id,
                chunkNumber,
                new FileParameter(chunkStream),
                TestContext.Current.CancellationToken);

            chunkNumber++;
        }

        var uploaded = (await _filesOperationsApi.FinalizeSessionAsync(
            folderId,
            session.Id,
            TestContext.Current.CancellationToken)).Response;

        return uploaded.File.Id;
    }
}
