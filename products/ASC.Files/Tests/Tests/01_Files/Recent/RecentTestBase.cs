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

    // The equivalent helpers on ThirdPartyTestBase (ASC.Files.Tests.Tests._03_Rooms.ThirdParty)
    // are not reusable here: that class lives outside 01_Files, which this suite does not own,
    // and RecentTestBase already derives from RoomsPermissionsTestBase, so C# single inheritance
    // rules out pulling in a second base. The credential/skip logic is duplicated deliberately -
    // it is a handful of lines reading the same three environment variables.
    private static string NextcloudUrl => Environment.GetEnvironmentVariable("NEXTCLOUD_URL") ?? "";
    private static string NextcloudLogin => Environment.GetEnvironmentVariable("NEXTCLOUD_LOGIN") ?? "";
    private static string NextcloudPassword => Environment.GetEnvironmentVariable("NEXTCLOUD_PASSWORD") ?? "";

    /// <summary>
    /// Skips the current test unless a reachable Nextcloud is configured in the environment.
    /// </summary>
    protected static void RequireNextcloud()
    {
        Assert.SkipWhen(
            string.IsNullOrEmpty(NextcloudUrl) || string.IsNullOrEmpty(NextcloudLogin) || string.IsNullOrEmpty(NextcloudPassword),
            "Nextcloud is not configured: set NEXTCLOUD_URL, NEXTCLOUD_LOGIN and NEXTCLOUD_PASSWORD.");
    }

    /// <summary>
    /// Connects the configured Nextcloud account and creates a room backed by it, returning the
    /// room's id. The generated model types the id as <c>string</c>, but the value on the wire is
    /// the room's ordinary integer folder id, so it is parsed back for use with the rest of the
    /// (integer-keyed) Files API - the same cast the TypeScript suite makes explicitly.
    /// </summary>
    protected async Task<int> CreateThirdPartyRoomAsync(string customerTitle, string roomTitle)
    {
        var connection = await _thirdPartyApi.SaveThirdPartyAsync(
            new ThirdPartyRequestDto(
                url: NextcloudUrl,
                login: NextcloudLogin,
                password: NextcloudPassword,
                customerTitle: customerTitle,
                providerKey: "Nextcloud"),
            TestContext.Current.CancellationToken);

        var room = await _roomsApi.CreateRoomThirdPartyAsync(
            connection.Response.Id,
            new CreateThirdPartyRoom(title: roomTitle, roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken);

        return int.Parse(room.Response.Id);
    }
}
