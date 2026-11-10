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
/// A shared room does not share what each member asked the AI in it: threads are per user, a room the caller cannot
/// open is refused as a scope, and a member's own thread comes back with its history and its model.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Chat")]
public class ChatRoomAcrossUsersTests(AspireAppFixture fixture) : ChatPart2TestBase(fixture)
{
    private const string Question = "Remember the code word ORANGE. Reply with just: OK.";

    [Fact]
    public async Task ListAndRead_RoomMember_DoesNotSeeTheOwnersRoomThread()
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();
        var roomId = await CreateCustomRoomAsync("Autotest Shared Room");
        var entityId = roomId.ToString();

        var member = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        await InviteToRoomAsync(roomId, member);

        var ownerThread = await CreateThreadAsync("Autotest owner room thread", profile.Id, entityId);

        await _aiChatClient.Authenticate(member);

        // Act
        var listed = await ListThreadIdsAsync(entityId);
        var read = await Assert.ThrowsAsync<ApiException>(async () => await ReadMessagesAsync(ownerThread.ThreadId));
        var info = await Assert.ThrowsAsync<ApiException>(async () => await GetThreadAsync(ownerThread.ThreadId));

        // Assert
        listed.Should().NotContain(ownerThread.ThreadId, "sharing the room does not share the Owner's conversation");
        read.ErrorCode.Should().Be(403);
        info.ErrorCode.Should().Be(403);

        // Positive control: the member's own room thread lands in their own list.
        var memberThread = await CreateThreadAsync("Autotest member room thread", profile.Id, entityId);
        (await ListThreadIdsAsync(entityId)).Should().Equal(memberThread.ThreadId);
    }

    // Not ported: BUG 82858 "listing the threads of a room the caller cannot open is refused with 403". Threads are one
    // list per user since ecf76402b3 and threads/list ignores entityId (ThreadsStorage.readAll(_entityId, …)), so the
    // call answers the caller's own list and reveals nothing about the room; there is no access check left to assert.

    [Fact]
    public async Task OpenOrCreate_RoomMemberReopensOwnThread_GetsItsHistoryAndItsModel()
    {
        // The Owner's own thread in the same room runs on the other model: with one thread in the room, a backend
        // answering with the room's newest thread would pass.

        // Arrange
        var (first, second) = await TwoChatProfilesAsync();
        var roomId = await CreateCustomRoomAsync("Autotest Shared Room");
        var entityId = roomId.ToString();

        var member = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        await InviteToRoomAsync(roomId, member);

        var ownerThread = await CreateThreadAsync("Autotest owner room thread", first.Id, entityId);
        await AppendUserMessageAsync(ownerThread.ThreadId, "Autotest owner question");

        await _aiChatClient.Authenticate(member);

        var memberThread = await CreateThreadAsync("Autotest member room thread", second.Id, entityId);
        AssertHealthyReply(await SendWithStreamAsync(memberThread.ThreadId, Question, second.Id, entityId));
        var stored = await ReadMessagesAsync(memberThread.ThreadId);

        // Act
        var opened = await _aiThreads.AiThreadsOpenOrCreateAsync(
            new AiThreadsOpenOrCreateRequest(
                threadId: memberThread.ThreadId,
                profile: second,
                profileId: second.Id,
                firstMessage: BuildUserMessage("hello"),
                entityId: entityId),
            TestContext.Current.CancellationToken);

        // Assert
        opened.ThreadId.Should().Be(memberThread.ThreadId, "the member's own thread is reused");
        opened.PriorMessages.Select(m => m.Id).Should().Equal(stored.Select(m => m.Id));
        opened.PriorMessages.Select(m => m.Role)
            .Should().Equal(AiThreadMessageLike.RoleEnum.User, AiThreadMessageLike.RoleEnum.Assistant);
        TextOf(opened.PriorMessages[0]).Should().Be(Question, "the member's question, not the Owner's");

        (await ThreadProfileIdAsync(memberThread.ThreadId))
            .Should().Be(second.Id, "and the model the member picked, not the one the Owner's thread runs on");
        (await ListedProfileIdAsync(memberThread.ThreadId, entityId)).Should().Be(second.Id);
    }
}
