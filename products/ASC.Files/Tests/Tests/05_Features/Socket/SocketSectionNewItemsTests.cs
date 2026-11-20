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


namespace ASC.Files.Tests.Tests._05_Features.Socket;

/// <summary>
/// The Forms badge after Mark as read: the client refreshes it only on an event for the Forms root, which was never
/// sent, so the badge kept its old count over an empty panel.
/// </summary>
[Trait("Category", "Socket")]
public class SocketSectionNewItemsTests(AspireAppFixture fixture) : SocketTestBase(fixture)
{
    private const string MarkAsNewFolder = "s:markasnew-folder";

    [Fact]
    public async Task MarkAsReadOfAFormRoom_SendsTheClearedFormsBadge()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (room, member) = await CreateFormRoomWithMember("Socket Forms Badge Read");

        await _filesClient.Authenticate(Owner);
        await UploadFormAsync(room.Id, "Socket Forms Badge Read.pdf");

        await _filesClient.Authenticate(member);
        var formsId = await GetFolderIdAsync(FolderType.Forms, member);
        (await PollRoomNewsCount(room.Id, count => count > 0)).Should().BeGreaterThan(0, "the upload has to be new for the member first");

        await using var socket = await ConnectAndWatch(member, formsId);

        var operation = (await _filesOperationsApi.MarkAsReadAsync(
            new BaseBatchRequestDto(folderIds: [new BaseBatchRequestDtoAllOfFolderIds(room.Id)]) { ReturnSingleOperation = true },
            cancellationToken)).Response.Single();

        // The operation runs in Files.Worker: a missing event is a finding only once the operation has finished.
        var statuses = await WaitLongOperation(operation.Id);
        statuses.Should().AllSatisfy(s => s.Finished.Should().BeTrue("Mark as read has to finish before its event can be expected"));

        await socket.WaitForAsync(MarkAsNewFolder, Badge(formsId, 0), cancellationToken: cancellationToken);
    }

    /// <summary>A form room with a content creator, who gets the marks of what the owner adds to it.</summary>
    private async Task<(FolderDto Room, User Member)> CreateFormRoomWithMember(string title)
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom(title);
        var member = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.ContentCreator);

        return (room, member);
    }

    /// <summary><c>{ folderId, count }</c> of the section root.</summary>
    private static Func<JsonElement, bool> Badge(int folderId, int count)
    {
        return payload => payload.GetProperty("folderId").GetRawText() == folderId.ToString(CultureInfo.InvariantCulture)
                          && payload.GetProperty("count").GetInt32() == count;
    }

    /// <summary>Uploads the shared <c>new.pdf</c> asset, a real PDF form, as the authenticated user.</summary>
    private async Task UploadFormAsync(int roomId, string title)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var stream = typeof(SocketSectionNewItemsTests).Assembly.GetManifestResourceStream("ASC.Files.Tests.Data.new.pdf")!;

        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            roomId, new SessionRequest(title, stream.Length), cancellationToken: cancellationToken)).Response;

        await _filesOperationsApi.UploadAsyncSessionAsync(roomId, session.Id, 1, new FileParameter(stream), cancellationToken);
        await _filesOperationsApi.FinalizeSessionAsync(roomId, session.Id, cancellationToken);
    }

    /// <summary>The marks are written asynchronously: waits until the room news of the authenticated user satisfy the check.</summary>
    private async Task<int> PollRoomNewsCount(int roomId, Func<int, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (true)
        {
            var groups = (await _roomsApi.GetNewRoomItemsAsync(roomId, TestContext.Current.CancellationToken)).Response ?? [];
            var count = groups.Sum(g => g.Items?.Count ?? 0);

            if (until(count) || DateTime.UtcNow >= deadline)
            {
                return count;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}
