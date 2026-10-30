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

namespace ASC.Files.Tests.Tests._06_Operations.CheckPreflight;

/// <summary>
/// Shared helpers for the two preflight endpoints, <c>GET /api/2.0/files/fileops/move</c>
/// (<c>checkMoveOrCopyBatchItems</c>) and <c>GET /api/2.0/files/fileops/checkdestfolder</c>
/// (<c>checkMoveOrCopyDestFolder</c>).
/// </summary>
/// <remarks>
/// The request goes through the typed client; the response is read from its raw JSON, because
/// <c>FileEntryBaseDto</c>, the element type of both responses, has a <c>Title</c> but no <c>Id</c> and no
/// <c>FolderId</c>, and the tests identify the reported entries by id. A failure comes back as a status and a
/// body, not as an exception, so a test can assert on either.
/// </remarks>
public abstract class CheckPreflightTestBase(
    AspireAppFixture fixture)
    : CheckDestFolder.CheckDestFolderTestBase(fixture)
{
    private static readonly JsonSerializerOptions _preflightJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public enum PreflightKind
    {
        Move,
        CheckDestFolder
    }

    /// <summary>An entry of a preflight answer, as far as the tests need it.</summary>
    protected sealed record PreflightEntry(int Id, string? Title, int? FolderId);

    private sealed class MoveBody
    {
        public List<PreflightEntry>? Response { get; set; }
    }

    private sealed class DestBody
    {
        public DestPayload? Response { get; set; }
    }

    private sealed class DestPayload
    {
        public CheckDestFolderResult? Result { get; set; }

        public List<PreflightEntry>? Files { get; set; }
    }

    /// <summary>The status, the body and the parsed answer of a preflight call.</summary>
    /// <param name="Status">The HTTP status.</param>
    /// <param name="Body">The body as sent, for "this text must not appear" checks.</param>
    /// <param name="Entries">
    /// <c>move</c>: the clashing items. <c>checkdestfolder</c>: the files named as accepted. Empty on a failure.
    /// </param>
    /// <param name="Result">The verdict of <c>checkdestfolder</c>; <c>null</c> for <c>move</c> and on a failure.</param>
    protected sealed record PreflightResponse(HttpStatusCode Status, string Body, List<PreflightEntry> Entries, CheckDestFolderResult? Result);

    protected async Task<PreflightResponse> Preflight(
        PreflightKind kind,
        IEnumerable<int>? fileIds = null,
        IEnumerable<int>? folderIds = null,
        int? destFolderId = null,
        FileConflictResolveType? conflictResolveType = null,
        bool? content = null)
    {
        var queryFileIds = AsQueryIds(fileIds);
        var queryFolderIds = AsQueryIds(folderIds);
        var queryDestFolderId = destFolderId?.ToString(CultureInfo.InvariantCulture);

        try
        {
            if (kind == PreflightKind.Move)
            {
                var moveResponse = await _filesOperationsApi.CheckMoveOrCopyBatchItemsWithHttpInfoAsync(
                    fileIds: queryFileIds,
                    folderIds: queryFolderIds,
                    destFolderId: queryDestFolderId,
                    conflictResolveType: conflictResolveType,
                    content: content,
                    cancellationToken: TestContext.Current.CancellationToken);

                var parsed = JsonSerializer.Deserialize<MoveBody>(moveResponse.RawContent, _preflightJsonOptions);

                return new PreflightResponse(moveResponse.StatusCode, moveResponse.RawContent, parsed?.Response ?? [], null);
            }

            var destResponse = await _filesOperationsApi.CheckMoveOrCopyDestFolderWithHttpInfoAsync(
                fileIds: queryFileIds,
                folderIds: queryFolderIds,
                destFolderId: queryDestFolderId,
                conflictResolveType: conflictResolveType,
                content: content,
                cancellationToken: TestContext.Current.CancellationToken);

            var payload = JsonSerializer.Deserialize<DestBody>(destResponse.RawContent, _preflightJsonOptions)?.Response;

            return new PreflightResponse(destResponse.StatusCode, destResponse.RawContent, payload?.Files ?? [], payload?.Result);
        }
        catch (ApiException exception)
        {
            return new PreflightResponse((HttpStatusCode)exception.ErrorCode, exception.ErrorContent?.ToString() ?? string.Empty, [], null);
        }
    }

    /// <summary>
    /// The files and folders of every listed folder as <c>id:title</c>, sorted, for "nothing was
    /// changed" comparisons.
    /// </summary>
    protected async Task<Dictionary<int, List<string>>> Snapshot(params int[] folderIds)
    {
        var result = new Dictionary<int, List<string>>();

        foreach (var folderId in folderIds)
        {
            var raw = await _foldersApi.GetFolderByFolderIdWithHttpInfoAsync(folderId, cancellationToken: TestContext.Current.CancellationToken);
            var listing = JsonSerializer.Deserialize<ListingBody>(raw.RawContent, _preflightJsonOptions)?.Response;

            result[folderId] =
            [
                .. (listing?.Files ?? []).Concat(listing?.Folders ?? []).Select(e => $"{e.Id}:{e.Title}").Order()
            ];
        }

        return result;
    }

    private sealed class ListingBody
    {
        public ListingPayload? Response { get; set; }
    }

    private sealed class ListingPayload
    {
        public List<PreflightEntry>? Files { get; set; }

        public List<PreflightEntry>? Folders { get; set; }
    }

    /// <summary>A preflight must not start work: the caller has no operation on record afterwards.</summary>
    protected async Task AssertNoOperationQueued()
    {
        var statuses = (await _filesOperationsApi.GetOperationStatusesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        statuses.Should().BeEmpty("a preflight check must not start an operation");
    }

    protected static List<int> IdsOf(PreflightResponse response)
    {
        return [.. response.Entries.Select(e => e.Id).Order()];
    }

    protected static List<string?> TitlesOf(PreflightResponse response)
    {
        return [.. response.Entries.Select(e => e.Title).Order()];
    }
}
