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
/// Every test in this folder goes through <see cref="CheckMoveOrCopy"/>, which calls
/// <c>GET /api/2.0/files/fileops/move</c> through the typed client and hands back the status and the body whether
/// the call succeeded or not. The ids travel as text: the query carries no type, and the server reads a numeric
/// text as a portal id (BUG 84727 was the generated client sending its id wrapper instead).
/// </remarks>
public abstract class CheckMoveCopyTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// The parsed result of a <c>GET /api/2.0/files/fileops/move</c> call. The tests of this folder name the
    /// conflicting items by title, which every entry carries whatever its kind.
    /// </summary>
    protected sealed record CheckMoveOrCopyResult(HttpStatusCode StatusCode, List<string> Titles, string RawBody);

    protected async Task<CheckMoveOrCopyResult> CheckMoveOrCopy(
        int? destFolderId = null,
        IEnumerable<int>? fileIds = null,
        IEnumerable<int>? folderIds = null,
        FileConflictResolveType? conflictResolveType = null,
        bool? deleteAfter = null,
        bool? content = null)
    {
        try
        {
            var response = await _filesOperationsApi.CheckMoveOrCopyBatchItemsWithHttpInfoAsync(
                fileIds: fileIds?.Select(id => id.ToString(CultureInfo.InvariantCulture)).ToList(),
                folderIds: folderIds?.Select(id => id.ToString(CultureInfo.InvariantCulture)).ToList(),
                destFolderId: destFolderId?.ToString(CultureInfo.InvariantCulture),
                conflictResolveType: conflictResolveType,
                deleteAfter: deleteAfter,
                content: content,
                cancellationToken: TestContext.Current.CancellationToken);

            var titles = response.Data.Response?.Select(entry => entry.Title ?? string.Empty).ToList() ?? [];

            return new CheckMoveOrCopyResult(response.StatusCode, titles, response.RawContent);
        }
        catch (ApiException exception)
        {
            return new CheckMoveOrCopyResult((HttpStatusCode)exception.ErrorCode, [], exception.ErrorContent?.ToString() ?? string.Empty);
        }
    }

    /// <summary>
    /// The entry the endpoint reports for a conflicting item. Only the title survives the response
    /// model (see the class remarks), so that is all a test can identify an entry by.
    /// </summary>
    protected sealed record CheckedEntry(string Title);

    /// <summary>The parsed response body, shaped like the SDK wrapper the tests were written against.</summary>
    protected sealed record CheckBatchResult(List<CheckedEntry> Response);

    /// <summary>
    /// Adapter over <see cref="CheckMoveOrCopy"/> for the tests written against the body model: takes a
    /// <see cref="BatchRequestDto"/>, sends its fields as the query parameters of the preflight, throws
    /// <see cref="ApiException"/> on a non-2xx and hands back the response list under <c>.Response</c>.
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
