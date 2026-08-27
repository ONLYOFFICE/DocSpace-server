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

namespace ASC.Files.Tests.Tests._06_Operations.MarkAsRead;

/// <summary>
/// Shared helpers for the <c>PUT /api/2.0/files/fileops/markasread</c> suite: reads the "new items"
/// badge of a room to check what markAsRead cleared, and builds the request bodies.
/// </summary>
public abstract class MarkAsReadTestBase(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    /// <summary>Flattens the date groups of <c>GET /files/rooms/{id}/news</c> into plain titles.</summary>
    protected static List<string> TitlesOf(List<NewItemsDtoFileEntryBaseDto> groups)
    {
        return [.. (groups ?? []).SelectMany(g => g.Items ?? []).Select(e => e.Title)];
    }

    /// <summary>
    /// Waits until the room news of the currently authenticated user satisfy <paramref name="until"/>.
    /// The "new" badges are written asynchronously, so a bare read right after the change races with
    /// the badge being created or cleared.
    /// </summary>
    protected async Task<List<string>> PollRoomNewsTitles(int roomId, Func<List<string>, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (true)
        {
            var titles = TitlesOf((await _roomsApi.GetNewRoomItemsAsync(roomId, TestContext.Current.CancellationToken)).Response);

            if (until(titles) || DateTime.UtcNow >= deadline)
            {
                return titles;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Creates a room, invites a member with Read access and lets them open it, so that anything
    /// created afterwards counts as new for that member. Leaves the client authenticated as the owner.
    /// </summary>
    protected async Task<(FolderDtoInteger Room, User Member)> CreateRoomWithReadVisitor(string title)
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom(title);

        var member = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.Read);

        await _filesClient.Authenticate(member);
        await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);

        return (room, member);
    }

    /// <summary>Builds a <c>markasread</c> request body carrying the given file ids.</summary>
    protected static BaseBatchRequestDto MarkAsReadFiles(params int[] fileIds)
    {
        return new BaseBatchRequestDto(fileIds: [.. fileIds.Select(id => new BaseBatchRequestDtoAllOfFileIds(id))]);
    }

    /// <summary>Builds a <c>markasread</c> request body carrying the given folder ids.</summary>
    protected static BaseBatchRequestDto MarkAsReadFolders(params int[] folderIds)
    {
        return new BaseBatchRequestDto(folderIds: [.. folderIds.Select(id => new BaseBatchRequestDtoAllOfFolderIds(id))]);
    }
}
