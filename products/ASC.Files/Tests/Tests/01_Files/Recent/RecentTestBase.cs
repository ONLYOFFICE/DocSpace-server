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

namespace ASC.Files.Tests.Tests._01_Files.Recent;

/// <summary>
/// Shared setup for the Recent suites. Inherits <c>RoomsPermissionsTestBase</c> (namespace
/// <c>ASC.Files.Tests.Tests._03_Rooms</c>, already brought in through the project's
/// <c>GlobalUsings.cs</c>) purely to reuse its <c>InviteMember</c> / <c>InviteToRoom</c> helpers,
/// the same way <c>PrivacyRoomTestBase</c> under <c>08_Private</c> does.
/// </summary>
public abstract class RecentTestBase(AspireAppFixture fixture) : RoomsPermissionsTestBase(fixture)
{
    /// <summary>
    /// Reads the Recent section for the currently authenticated user, optionally filtered by folder
    /// type(s), the same way <c>FolderType?includeType</c> is used elsewhere in the suite.
    /// </summary>
    protected async Task<FolderContentDtoInteger> GetRecentAsync(List<FolderType>? folderType = null)
    {
        var recentId = (await _foldersApi.GetRecentFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;

        return (await _foldersApi.GetFolderByFolderIdAsync(
            recentId,
            folderType: folderType?.Select(r => (int)r).ToList(),
            cancellationToken: TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>
    /// Polls the Recent section on a deadline until <paramref name="until"/> is satisfied, returning
    /// the last observed listing either way. Adding to Recent and deleting from it are both applied
    /// asynchronously, so a bare read right after the request races with the write.
    /// </summary>
    protected async Task<FolderContentDtoInteger> PollRecentUntil(Func<FolderContentDtoInteger, bool> until, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(10));

        while (true)
        {
            var recent = await GetRecentAsync();

            if (until(recent) || DateTime.UtcNow >= deadline)
            {
                return recent;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}
