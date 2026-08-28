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

namespace ASC.Files.Tests.Tests._06_Operations.CheckMoveCopy;

/// <remarks>
/// <c>GET /api/2.0/files/fileops/move</c> is bound by <c>BatchModelBinder</c>
/// (<c>products/ASC.Files/Core/ApiModels/Binders.cs:215</c>), which reads flat query parameters —
/// <c>fileIds</c>, <c>folderIds</c>, <c>destFolderId</c>, <c>conflictResolveType</c>,
/// <c>deleteAfter</c>, <c>content</c>. The generated
/// <c>OperationsApi.CheckMoveOrCopyBatchItemsAsync</c> instead sends the whole
/// <c>BatchRequestDto</c> under a single <c>inDto</c> query key
/// (<c>ClientUtils.ParameterToMultiMap("", "inDto", inDto)</c>), so the binder never populates
/// <c>DestFolderId</c> — its <c>ValueKind</c> stays <c>Undefined</c> — and
/// <c>OperationController.CheckMoveOrCopyBatchItems</c> throws server-side. That is an
/// SDK/OpenAPI-generation defect, not a product bug, and it is exactly the "route the typed
/// signature cannot produce" carve-out in <c>tests.md</c>. Every test in this folder therefore goes
/// through <see cref="CheckMoveOrCopy"/> below, which issues the request over raw HTTP with a
/// hand-built query string.
/// </remarks>
public abstract class CheckMoveCopyTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// The parsed result of a <c>GET /api/2.0/files/fileops/move</c> call. <c>FileEntryBaseDto</c>
    /// (what the endpoint's response array actually deserializes to) carries a <c>Title</c> but no
    /// <c>Id</c>, so conflicting items are identified by title here as well.
    /// </summary>
    protected sealed record CheckMoveOrCopyResult(HttpStatusCode StatusCode, List<string> Titles, string RawBody);

    private sealed class ResponseEntry
    {
        public string? Title { get; set; }
    }

    private sealed class ResponseBody
    {
        public List<ResponseEntry>? Response { get; set; }
    }

    protected async Task<CheckMoveOrCopyResult> CheckMoveOrCopy(
        int? destFolderId = null,
        IEnumerable<int>? fileIds = null,
        IEnumerable<int>? folderIds = null,
        FileConflictResolveType? conflictResolveType = null,
        bool? deleteAfter = null,
        bool? content = null)
    {
        var query = new List<string>();

        if (destFolderId.HasValue)
        {
            query.Add($"destFolderId={destFolderId.Value}");
        }

        if (fileIds != null)
        {
            query.AddRange(fileIds.Select(id => $"fileIds={id}"));
        }

        if (folderIds != null)
        {
            query.AddRange(folderIds.Select(id => $"folderIds={id}"));
        }

        if (conflictResolveType.HasValue)
        {
            query.Add($"conflictResolveType={conflictResolveType.Value}");
        }

        if (deleteAfter.HasValue)
        {
            query.Add($"deleteAfter={(deleteAfter.Value ? "true" : "false")}");
        }

        if (content.HasValue)
        {
            query.Add($"content={(content.Value ? "true" : "false")}");
        }

        var path = "api/2.0/files/fileops/move" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

        using var response = await _filesClient.GetAsync(path, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        var titles = new List<string>();

        if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(body))
        {
            var parsed = JsonSerializer.Deserialize<ResponseBody>(body, _jsonOptions);
            titles = parsed?.Response?.Select(entry => entry.Title ?? string.Empty).ToList() ?? [];
        }

        return new CheckMoveOrCopyResult(response.StatusCode, titles, body);
    }

    /// <summary>
    /// The entry the endpoint reports for a conflicting item. Only the title survives the response
    /// model (see the class remarks), so that is all a test can identify an entry by.
    /// </summary>
    protected sealed record CheckedEntry(string Title);

    /// <summary>The parsed response body, shaped like the SDK wrapper the tests were written against.</summary>
    protected sealed record CheckBatchResult(List<CheckedEntry> Response);

    /// <summary>
    /// SDK-shaped adapter over <see cref="CheckMoveOrCopy"/>: takes the same <see cref="BatchRequestDto"/>
    /// the generated client takes, sends it as the flat query string the server actually binds, throws
    /// <see cref="ApiException"/> on a non-2xx exactly like the generated client would, and hands back
    /// the response list under <c>.Response</c>.
    /// </summary>
    protected async Task<CheckBatchResult> CheckMoveOrCopyBatch(BatchRequestDto request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await CheckMoveOrCopy(
            destFolderId: request.DestFolderId?.ActualInstance is int id ? id : null,
            fileIds: request.FileIds?.Select(f => Convert.ToInt32(f.ActualInstance)),
            folderIds: request.FolderIds?.Select(f => Convert.ToInt32(f.ActualInstance)),
            conflictResolveType: request.ConflictResolveType,
            deleteAfter: request.DeleteAfter,
            content: request.Content ? true : null);

        if ((int)result.StatusCode is < 200 or >= 300)
        {
            throw new ApiException((int)result.StatusCode, $"Error calling CheckMoveOrCopyBatchItems: {result.RawBody}");
        }

        return new CheckBatchResult(result.Titles.ConvertAll(t => new CheckedEntry(t)));
    }
}
