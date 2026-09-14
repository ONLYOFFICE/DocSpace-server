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

namespace ASC.People.Tests.Tests._08_Search;

/// <summary>
/// Shared setup for the people-search suite: creating a room with a file/folder inside it to
/// search sharing settings against, resolving a member's display name, and polling reads that
/// come from the asynchronously-written search index.
/// </summary>
public abstract class SearchTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    /// <summary>
    /// The invite helpers only return id/email; the search assertions also want the display name
    /// the index stores, so it is fetched once, right after invitation.
    /// </summary>
    /// <remarks>
    /// The decode is not cosmetic. The API returns <c>DisplayName</c> HTML-encoded while
    /// <c>FirstName</c>, <c>LastName</c> and the stored values are not, so a name like
    /// <c>O'Hara</c> comes back as <c>O&amp;#39;Hara</c>. Feeding that straight back as a search
    /// term matches nothing — the search is a plain SQL <c>LIKE</c> over the raw columns
    /// (<c>UserQueryHelper.FilterByText</c>) — which showed up as one arbitrary search test going
    /// red per full run, whichever one the faker happened to give an apostrophe to.
    /// </remarks>
    protected async Task<string> DisplayNameOf(User user)
    {
        var profile = await _profilesApi.GetProfileByEmailAsync(user.Email, cancellationToken: TestContext.Current.CancellationToken);
        return WebUtility.HtmlDecode(profile.Response.DisplayName);
    }

    protected async Task<(int RoomId, int FileId)> CreateRoomWithFileAsync(string roomTitle, string fileTitle)
    {
        var room = await CreateCustomRoom(roomTitle);
        var file = await _filesApi.CreateFileAsync(room.Id, new CreateFileJsonElement(fileTitle), TestContext.Current.CancellationToken);
        return (room.Id, file.Response.Id);
    }

    protected async Task<(int RoomId, int FolderId)> CreateRoomWithFolderAsync(string roomTitle, string folderTitle)
    {
        var room = await CreateCustomRoom(roomTitle);
        var folder = await _foldersApi.CreateFolderAsync(room.Id, new CreateFolder(folderTitle), TestContext.Current.CancellationToken);
        return (room.Id, folder.Response.Id);
    }

    /// <summary>
    /// Every search/filter endpoint here is served from an index written asynchronously after the
    /// change that is supposed to make it visible. Polls on a deadline and returns the last
    /// observed result, so a failing assertion shows what was actually there instead of a bare
    /// timeout.
    /// </summary>
    protected async Task<T> PollUntilAsync<T>(Func<Task<T>> read, Func<T, bool> until)
    {
        // The people search is a plain SQL LIKE, not an index read, so a miss is normally a real
        // miss. The poll is kept because an invite's group membership is written on its own path,
        // and the deadline only bounds the wait — a green run does not spend it.
        var deadline = DateTime.UtcNow.AddSeconds(30);
        var last = await read();

        while (!until(last) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(1000, TestContext.Current.CancellationToken);
            last = await read();
        }

        return last;
    }
}
