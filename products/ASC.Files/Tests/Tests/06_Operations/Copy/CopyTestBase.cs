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

namespace ASC.Files.Tests.Tests._06_Operations.Copy;

/// <summary>
/// Shared helpers for the <c>PUT /api/2.0/files/fileops/copy</c> suites: request builders,
/// operation polling and the raw-JSON lookups the generated models cannot carry.
///
/// Derives from <see cref="RoomsPermissionsTestBase"/> (not <see cref="BaseTest"/> directly) to
/// reuse its <c>InviteMember</c>/<c>InviteToRoom</c>/<c>ArchiveRoom</c> helpers instead of
/// duplicating them here.
/// </summary>
public abstract class CopyTestBase(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    protected static BatchRequestDto BuildCopyRequest(
        int destFolderId,
        FileConflictResolveType conflictResolveType = FileConflictResolveType.Skip,
        bool deleteAfter = false,
        bool content = false,
        bool toFillOut = false,
        IEnumerable<int>? fileIds = null,
        IEnumerable<int>? folderIds = null)
    {
        return new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(destFolderId),
            ConflictResolveType = conflictResolveType,
            FileIds = (fileIds ?? []).Select(id => new BatchRequestDtoAllOfFileIds(id)).ToList(),
            FolderIds = (folderIds ?? []).Select(id => new BatchRequestDtoAllOfFolderIds(id)).ToList(),
            DeleteAfter = deleteAfter,
            Content = content,
            ToFillOut = toFillOut,
            ReturnSingleOperation = true
        };
    }

    /// <summary>
    /// Triggers a copy batch operation and waits for it to finish. A fast operation can come back
    /// with an already-empty result array (see tests.md), so the caller must never rely on the
    /// returned list being non-empty and should instead assert the outcome (the copy at the
    /// destination) directly.
    /// </summary>
    protected async Task CopyAndWait(BatchRequestDto request)
    {
        var results = (await _filesOperationsApi.CopyBatchItemsAsync(request, TestContext.Current.CancellationToken)).Response;
        var operationId = results.FirstOrDefault()?.Id;
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

    /// <summary>
    /// <see cref="FolderContentDtoInteger.Folders"/> and <see cref="FolderContentDtoInteger.Files"/>
    /// are typed <c>List&lt;FileEntryBaseDto&gt;</c>, which carries a <c>Title</c> but no <c>Id</c>
    /// (the same SDK narrowness tests.md calls out for room reads through a folder listing). Locating
    /// a copied entry's id therefore has to go through the raw JSON.
    /// </summary>
    protected async Task<int> FindEntryIdByTitle(int parentFolderId, string title, bool inFolders)
    {
        var raw = await _foldersApi.GetFolderByFolderIdWithHttpInfoAsync(parentFolderId, cancellationToken: TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(raw.RawContent);
        var array = json.RootElement.GetProperty("response").GetProperty(inFolders ? "folders" : "files");

        foreach (var entry in array.EnumerateArray())
        {
            if (entry.TryGetProperty("title", out var titleProp) && titleProp.GetString() == title)
            {
                return entry.GetProperty("id").GetInt32();
            }
        }

        throw new InvalidOperationException($"No entry titled '{title}' found under folder {parentFolderId}.");
    }

    /// <summary>
    /// Creates a genuine ONLYOFFICE PDF form in the given folder. No document-server conversion is
    /// involved: a new .pdf goes through <c>FileStorageService.CreateNewFileAsync</c>, which copies
    /// the built-in blank-PDF template, and that template itself carries the <c>ONLYOFFICEFORM</c>
    /// signature (the same fact the <c>01_Files/Forms</c> and <c>01_Files/FormFilling</c> suites rely
    /// on). The original TypeScript helper converted .docx → .docxf → .pdf through <c>copyas</c>,
    /// which needs a live document server the integration-test AppHost does not run.
    /// </summary>
    protected async Task<int> CreateOoForm(int folderId)
    {
        var form = await CreateFile("Autotest OO Form.pdf", folderId);

        return form.Id;
    }
}
