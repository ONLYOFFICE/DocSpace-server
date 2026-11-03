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

namespace ASC.Files.Tests.Tests._01_Files.States;

/// <summary>
/// Shared setup for the file-state suites: a file held by someone else (locked, open in an editing
/// session) and the observable state a refused call must leave untouched.
/// </summary>
public abstract class StatesTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>What a refused call on a file must not change.</summary>
    protected sealed record FileState(string? Title, int Version, bool Locked, bool Editing, string Bytes);

    /// <summary>Creates a custom room and an HTML file in it, as the owner.</summary>
    protected async Task<(int RoomId, int FileId)> CreateRoomWithHtml(string content = "<p>original</p>")
    {
        await _filesClient.Authenticate(Owner);

        var room = await CreateCustomRoom("Autotest File States Room " + Guid.NewGuid().ToString()[..8]);
        var file = (await _filesApi.CreateHtmlFileAsync(
            room.Id,
            new CreateTextOrHtmlFileRequest("Autotest State", content),
            TestContext.Current.CancellationToken)).Response;

        return (room.Id, file.Id);
    }

    /// <summary>Invites a new portal user into the room, as the owner, and returns it.</summary>
    protected async Task<User> AddMember(int roomId, FileShare access)
    {
        var member = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        await InviteToRoom(roomId, member, access);

        return member;
    }

    /// <summary>The file as the owner sees it, content included.</summary>
    protected async Task<FileState> ReadState(int fileId)
    {
        await _filesClient.Authenticate(Owner);

        var file = await GetFile(fileId);
        var bytes = await _06_Operations.OperationFileBytes.DownloadAsync(_filesClient, fileId, null, TestContext.Current.CancellationToken);

        return new FileState(
            file.Title,
            file.Version,
            file.Locked == true,
            file.FileStatus.HasValue && (file.FileStatus.Value & FileStatus.IsEditing) == FileStatus.IsEditing,
            Convert.ToBase64String(bytes));
    }

    /// <summary>Runs a call and returns its HTTP status: 200 on success, the error code when it was refused.</summary>
    protected static async Task<int> StatusOf(Func<Task> call)
    {
        try
        {
            await call();

            return 200;
        }
        catch (ApiException e)
        {
            return e.ErrorCode;
        }
    }

    /// <summary>The number of stored versions of the file, as the owner sees it.</summary>
    protected async Task<int> VersionCount(int fileId)
    {
        await _filesClient.Authenticate(Owner);

        return (await _filesApi.GetFileVersionInfoAsync(fileId, TestContext.Current.CancellationToken)).Response.Count;
    }
}
