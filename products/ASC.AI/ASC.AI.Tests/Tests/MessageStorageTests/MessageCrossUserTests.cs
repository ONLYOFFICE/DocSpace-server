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

namespace ASC.AI.Tests.Tests.MessageStorageTests;

/// <summary>
/// The per-message routes (<c>get-message-by-id</c>, <c>update-message</c>, <c>delete-message</c>) called by someone
/// whose thread it is not. A message id is not a bearer token: threads are per user, and membership in the room a
/// thread lives in buys the room, not another member's messages.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Messages")]
public class MessageCrossUserTests(AspireAppFixture fixture) : MessagePart1TestBase(fixture)
{
    [Fact]
    public async Task PerMessageRoutes_NonMember_AreRefusedAndTheMessageIsKept()
    {
        var threadId = await CreateMessageThreadAsync();
        var messageId = await AppendAsync(threadId, "owner's question");

        var member = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await _aiChatClient.Authenticate(member);

        await AssertPerMessageRoutesRefusedAsync(messageId);

        // The thread around it is closed too, so the refusals are not a fluke of the per-message routes alone.
        var thread = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiThreads.AiThreadsGetByIdAsync(threadId, TestContext.Current.CancellationToken));
        thread.ErrorCode.Should().Be(403);

        await _aiChatClient.Authenticate(Owner);
        await AssertOwnerMessageIntactAsync(threadId, messageId);
    }

    [Fact]
    public async Task PerMessageRoutes_MemberOfTheRoom_AreRefusedAndTheMessageIsKept()
    {
        var roomId = await CreateRoomAsync();
        var profile = await GetDefaultProfileAsync();
        var thread = await CreateThreadAsync(profileId: profile.Id, entityId: roomId.ToString());
        var messageId = await AppendAsync(thread.ThreadId, "owner's question");

        var member = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        await InviteToRoomAsync(roomId, member);
        await _aiChatClient.Authenticate(member);

        await AssertPerMessageRoutesRefusedAsync(messageId);

        await _aiChatClient.Authenticate(Owner);
        await AssertOwnerMessageIntactAsync(thread.ThreadId, messageId);
    }

    private async Task AssertPerMessageRoutesRefusedAsync(string messageId)
    {
        var read = await Assert.ThrowsAsync<ApiException>(async () => await GetMessageAsync(messageId));
        read.ErrorCode.Should().Be(403, "reading another user's message");

        var update = await Assert.ThrowsAsync<ApiException>(async () => await _aiThreads.AiThreadsUpdateMessageAsync(
            new AiThreadsUpdateMessageRequest(messageId: messageId, message: BuildUserMessage("hijacked")),
            TestContext.Current.CancellationToken));
        update.ErrorCode.Should().Be(403, "rewriting another user's message");

        var delete = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiThreads.AiThreadsDeleteMessageAsync(messageId, TestContext.Current.CancellationToken));
        delete.ErrorCode.Should().Be(403, "deleting another user's message");
    }

    private async Task AssertOwnerMessageIntactAsync(string threadId, string messageId)
    {
        TextOf(await GetMessageAsync(messageId)).Should().Be("owner's question", "the owner's message content");
        (await ReadMessagesAsync(threadId)).Should().ContainSingle("the owner's thread still holds the message");
    }
}
