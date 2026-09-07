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

namespace ASC.Files.Tests.Tests._04_Security.Sharing;

/// <summary>
/// Shared helpers for the sharing/security-info suites: file/folder sharing shortcuts, member
/// termination and the one raw-HTTP carve-out this feature needs (an access level the typed
/// <see cref="FileShare"/> enum cannot represent).
/// </summary>
public abstract class SharingTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    /// <summary>Shares a file with a single user or group, acting as whoever is currently authenticated.</summary>
    protected async Task<List<FileShareDto>> ShareFile(int fileId, Guid shareTo, FileShare access, bool notify = false)
    {
        return (await _sharingApi.SetFileSecurityInfoAsync(
            fileId,
            new SecurityInfoSimpleRequestDto { Share = [new() { ShareTo = shareTo, Access = access }], Notify = notify },
            TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>Shares a folder with a single user or group, acting as whoever is currently authenticated.</summary>
    protected async Task<List<FileShareDto>> ShareFolder(int folderId, Guid shareTo, FileShare access, bool notify = false)
    {
        return (await _sharingApi.SetFolderSecurityInfoAsync(
            folderId,
            new SecurityInfoSimpleRequestDto { Share = [new() { ShareTo = shareTo, Access = access }], Notify = notify },
            TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>
    /// Sends a raw PUT to <c>api/2.0/files/share</c>, for the one payload the typed
    /// <see cref="SecurityInfoRequestDto"/> cannot express: an empty string in place of a
    /// <see cref="FileShare"/> value.
    /// </summary>
    protected async Task<(HttpStatusCode StatusCode, JsonDocument Body)> PutShareRaw(string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _filesClient.PutAsync("api/2.0/files/share", content, TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        return (response.StatusCode, JsonDocument.Parse(body));
    }
}
