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

namespace ASC.Files.Tests.Tests._02_Folders.News;

/// <summary>
/// Shared helpers for the folder-level "new items" suites
/// (<c>GET /api/2.0/files/{folderId}/news</c>). Reuses the room/invite helpers already defined for
/// the room-level "new items" endpoint (<see cref="RoomsPermissionsTestBase"/>), since a folder's
/// news badge is exercised through the same rooms.
/// </summary>
public abstract class FolderNewItemsTestBase(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    /// <summary>
    /// Opening the folder marks everything currently in it as read, which is what makes anything
    /// created afterwards count as "new" for that member.
    /// </summary>
    protected async Task VisitRoom(int folderId)
    {
        // Reading the folder's *content* is what establishes the last-read baseline; the subfolder
        // listing (GetFoldersAsync) does not clear the "new" badges.
        await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Creates a room, invites a member with the given access and lets them open it, so that
    /// anything created afterwards counts as new for that member. Leaves the client authenticated
    /// as the owner.
    /// </summary>
    protected async Task<(FolderDtoInteger Room, User Member)> CreateRoomWithVisitor(string title, FileShare access)
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom(title);

        var member = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, member, access);

        await _filesClient.Authenticate(member);
        await VisitRoom(room.Id);

        await _filesClient.Authenticate(Owner);

        return (room, member);
    }

    /// <summary>
    /// Waits until the folder news of the currently authenticated user satisfy <paramref name="until"/>.
    /// The "new" badges are written asynchronously, so a bare read right after the change races with
    /// the badge being created or cleared.
    /// </summary>
    protected async Task<List<string>> PollFolderNewsTitles(int folderId, Func<List<string>, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (true)
        {
            var titles = (await _foldersApi.GetNewFolderItemsAsync(folderId, TestContext.Current.CancellationToken)).Response.ConvertAll(e => e.Title);

            if (until(titles) || DateTime.UtcNow >= deadline)
            {
                return titles;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}
