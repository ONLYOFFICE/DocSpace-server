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

namespace ASC.Files.Tests.Tests._02_Folders.Upload;

/// <summary>
/// Shared setup for the single-request upload suites
/// (<c>POST /api/2.0/files/{folderId}/upload</c>, <c>POST /api/2.0/files/@my/upload</c> and
/// <c>POST /api/2.0/files/@my/insert</c>). Inherits <c>RoomsPermissionsTestBase</c> purely to reuse
/// its <c>InviteMember</c> / <c>InviteToRoom</c> / <c>ArchiveRoom</c> helpers, the same way
/// <c>RecentTestBase</c> under <c>01_Files/Recent</c> does.
/// </summary>
/// <remarks>
/// The TypeScript suite calls these endpoints through a hand-rolled multipart helper
/// (<c>uploadFileToFolder</c>) because the generated TypeScript SDK client serialises the file as
/// <c>{}</c> under a JSON content type, so the server sees no input file. The generated .NET SDK
/// builds a genuine <c>multipart/form-data</c> request itself (see <c>UploadFileWithHttpInfoAsync</c>),
/// so that workaround does not apply here — every test below goes through the typed SDK call
/// directly, per the "prefer the typed SDK" rule. See the porting report for the two TS "via SDK"
/// tests (BUG 81536, BUG 81538) that exist specifically to demonstrate that TS-side defect.
/// </remarks>
public abstract class UploadTestBase(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    /// <summary>
    /// Uploads a single in-memory file to the given folder through
    /// <c>POST /api/2.0/files/{folderId}/upload</c>, returning the response array's file entries.
    /// </summary>
    protected async Task<List<FileDtoInteger>> UploadToFolderAsync(
        int folderId,
        byte[]? content,
        string fileName,
        string contentType = "application/octet-stream",
        bool? createNewIfExist = null,
        bool? storeOriginalFile = null)
    {
        var file = content is null ? null : new FileParameter(fileName, contentType, new MemoryStream(content));

        var wrapper = await _foldersApi.UploadFileAsync(
            folderId,
            createNewIfExist: createNewIfExist,
            storeOriginalFile: storeOriginalFile,
            file: file,
            cancellationToken: TestContext.Current.CancellationToken);

        return wrapper.Response;
    }

    /// <summary>
    /// Uploads a single in-memory file to the My Documents section through
    /// <c>POST /api/2.0/files/@my/upload</c>, returning the response array's file entries.
    /// </summary>
    protected async Task<List<FileDtoInteger>> UploadToMyAsync(
        byte[]? content,
        string fileName,
        string contentType = "application/octet-stream",
        bool? createNewIfExist = null,
        bool? storeOriginalFile = null)
    {
        var file = content is null ? null : new FileParameter(fileName, contentType, new MemoryStream(content));

        var wrapper = await _foldersApi.UploadFileToMyAsync(
            createNewIfExist: createNewIfExist,
            storeOriginalFile: storeOriginalFile,
            file: file,
            cancellationToken: TestContext.Current.CancellationToken);

        return wrapper.Response;
    }

    /// <summary>
    /// Inserts a single in-memory file into the My Documents section through
    /// <c>POST /api/2.0/files/@my/insert</c>, returning the inserted file entry.
    /// </summary>
    protected async Task<FileDtoInteger> InsertToMyAsync(
        byte[]? content,
        string fileName,
        string? title = null,
        string contentType = "application/octet-stream",
        bool? createNewIfExist = null)
    {
        var file = content is null ? null : new FileParameter(fileName, contentType, new MemoryStream(content));

        var wrapper = await _foldersApi.InsertFileToMyFromBodyAsync(
            file: file,
            title: title,
            createNewIfExist: createNewIfExist,
            cancellationToken: TestContext.Current.CancellationToken);

        return wrapper.Response;
    }

    /// <summary>Reads the files of a folder through its content listing, as used by "appears in listing" checks.</summary>
    protected async Task<List<FileEntryBaseDto>> GetFolderFilesAsync(int folderId)
    {
        var content = await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken);

        return content.Response.Files;
    }
}
