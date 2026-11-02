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

namespace ASC.AI.Tests.Tests.ChatTests;

/// <summary>
/// A location is not permanent: a room gets archived, comes back, or is deleted outright. The conversations the user
/// held in it have to stay theirs — listed, readable, usable, and removable.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Chat")]
public class ChatRoomGoneTests(AspireAppFixture fixture) : ChatPart2TestBase(fixture)
{
    [Fact]
    public async Task ArchiveAndUnarchive_RoomOfAThread_ThreadIsStillListedReadableAndAnswered()
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();
        var roomId = await CreateCustomRoomAsync("Autotest Archived Room");
        var entityId = roomId.ToString();

        var thread = await CreateThreadAsync("Autotest thread before archiving", profile.Id, entityId);
        await AppendUserMessageAsync(thread.ThreadId, "written before archiving");

        var archive = (await RoomsApi.ArchiveRoomAsync(
            roomId,
            new ArchiveRoomRequest(deleteAfter: false),
            TestContext.Current.CancellationToken)).Response;
        (await WaitForOperationAsync(archive.Id)).Should().OnlyContain(s => s.Finished && string.IsNullOrEmpty(s.Error));
        (await RoomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response.RootFolderType
            .Should().Be(DocSpace.API.SDK.Model.FolderType.Archive, "the room is in Archive");

        // Act
        // An archived room is read-only for its content, but a chat is not its content.
        var listed = await ListThreadIdsAsync(entityId);
        var read = await ReadMessagesAsync(thread.ThreadId);
        var round = await SendWithStreamAsync(thread.ThreadId, ReplyOk, profile.Id, entityId);

        // Assert
        listed.Should().Contain(thread.ThreadId);
        UserTexts(read).Should().Equal("written before archiving");
        AssertHealthyReply(round);

        // And bringing the room back does not disturb any of it.
        var unarchive = (await RoomsApi.UnarchiveRoomAsync(
            roomId,
            new ArchiveRoomRequest(deleteAfter: false),
            TestContext.Current.CancellationToken)).Response;
        (await WaitForOperationAsync(unarchive.Id)).Should().OnlyContain(s => s.Finished && string.IsNullOrEmpty(s.Error));

        (await ListThreadIdsAsync(entityId)).Should().Contain(thread.ThreadId);
        (await ReadMessagesAsync(thread.ThreadId)).Should().HaveCount(read.Count + 2);
    }

    [Fact]
    public async Task DeleteRoom_RoomOfAThread_ConversationStaysReadableAndDeletable()
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();
        var roomId = await CreateCustomRoomAsync("Autotest Doomed Room");

        var thread = await CreateThreadAsync("Autotest thread in a doomed room", profile.Id, roomId.ToString());
        await AppendUserMessageAsync(thread.ThreadId, "written before the room was deleted");

        var deletion = (await RoomsApi.DeleteRoomAsync(
            roomId,
            new DeleteRoomRequest(deleteAfter: false),
            TestContext.Current.CancellationToken)).Response;
        (await WaitForOperationAsync(deletion.Id)).Should().OnlyContain(s => s.Finished && string.IsNullOrEmpty(s.Error));

        var roomInfo = await Assert.ThrowsAsync<ApiException>(
            async () => await RoomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken));
        roomInfo.ErrorCode.Should().Be(404, "deleting a room is permanent");

        // Act
        // The room is gone for good; what the user wrote is theirs and stays.
        var info = await GetThreadAsync(thread.ThreadId);
        var read = await ReadMessagesAsync(thread.ThreadId);
        var deleted = await _aiThreads.AiThreadsDeleteAsync(thread.ThreadId, TestContext.Current.CancellationToken);

        // Assert
        info.ThreadId.Should().Be(thread.ThreadId);
        UserTexts(read).Should().Equal("written before the room was deleted");
        deleted.Success.Should().BeTrue("and it can still be cleaned up");

        var gone = await Assert.ThrowsAsync<ApiException>(async () => await GetThreadAsync(thread.ThreadId));
        gone.ErrorCode.Should().Be(404);
    }
}
