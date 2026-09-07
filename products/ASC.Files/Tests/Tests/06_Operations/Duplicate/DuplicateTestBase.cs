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

namespace ASC.Files.Tests.Tests._06_Operations.Duplicate;

/// <summary>
/// Shared helpers for the <c>PUT /api/2.0/files/fileops/duplicate</c> suites: request builders,
/// operation polling and the raw-JSON lookups the generated models cannot carry.
///
/// Derives from <see cref="RoomsPermissionsTestBase"/> (not <see cref="BaseTest"/> directly) to
/// reuse its <c>InviteMember</c>/<c>InviteToRoom</c>/<c>ArchiveRoom</c> helpers instead of
/// duplicating them here (see <c>CopyTestBase</c> in the sibling <c>Copy/</c> folder, which does
/// the same for the copy endpoint).
/// </summary>
public abstract class DuplicateTestBase(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    protected static DuplicateRequestDto BuildDuplicateRequest(
        IEnumerable<int>? fileIds = null,
        IEnumerable<int>? folderIds = null)
    {
        return new DuplicateRequestDto(
            folderIds: (folderIds ?? []).Select(id => new DuplicateRequestDtoAllOfFolderIds(id)).ToList(),
            fileIds: (fileIds ?? []).Select(id => new DuplicateRequestDtoAllOfFileIds(id)).ToList())
        {
            ReturnSingleOperation = true
        };
    }

    /// <summary>
    /// Triggers a duplicate batch operation and waits for it to finish. A fast operation can come
    /// back with an already-empty result array (see tests.md), so the caller must never rely on the
    /// returned list being non-empty and should instead assert the outcome (the duplicate entry at
    /// the destination) directly. <see cref="DocSpace.API.SDK.Api.Files.OperationsApi.DuplicateBatchItemsAsync"/>
    /// returns <c>FileOperationArrayWrapper</c> directly (not wrapped in <c>ApiResponse&lt;T&gt;</c>).
    /// </summary>
    protected async Task DuplicateAndWait(DuplicateRequestDto request)
    {
        var result = await _filesOperationsApi.DuplicateBatchItemsAsync(request, TestContext.Current.CancellationToken);
        var operationId = result.Response.FirstOrDefault()?.Id;
        await WaitLongOperation(operationId);
    }

    protected async Task<FolderContentDtoInteger> GetFolderContent(int folderId)
    {
        return (await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;
    }

    protected static List<string> FileTitles(FolderContentDtoInteger content)
    {
        return (content.Files ?? []).Select(f => f.Title).ToList();
    }

    protected static List<string> FolderTitles(FolderContentDtoInteger content)
    {
        return (content.Folders ?? []).Select(f => f.Title).ToList();
    }

    protected static int CountTitlesContaining(IEnumerable<string> titles, string substring)
    {
        return titles.Count(t => t.Contains(substring, StringComparison.Ordinal));
    }

    /// <summary>
    /// <see cref="FolderContentDtoInteger.Folders"/> and <see cref="FolderContentDtoInteger.Files"/>
    /// are typed <c>List&lt;FileEntryBaseDto&gt;</c>, which carries a <c>Title</c> but no <c>Id</c>
    /// (the same SDK narrowness tests.md calls out for room reads through a folder listing). Locating
    /// a duplicated entry's id therefore has to go through the raw JSON.
    /// </summary>
    protected async Task<List<(int Id, string Title)>> GetRawEntries(int parentFolderId, bool inFolders)
    {
        var raw = await _foldersApi.GetFolderByFolderIdWithHttpInfoAsync(parentFolderId, cancellationToken: TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(raw.RawContent);
        var array = json.RootElement.GetProperty("response").GetProperty(inFolders ? "folders" : "files");

        var result = new List<(int, string)>();

        foreach (var entry in array.EnumerateArray())
        {
            result.Add((entry.GetProperty("id").GetInt32(), entry.GetProperty("title").GetString()!));
        }

        return result;
    }
}
