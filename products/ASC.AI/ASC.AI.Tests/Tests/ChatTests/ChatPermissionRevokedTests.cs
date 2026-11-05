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
/// A member who chatted in a room and was then taken out of it. The thread is the user's own writing, so it stays
/// theirs; naming the room they can no longer open, and reaching the model through it, does not.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Chat")]
public class ChatPermissionRevokedTests(AspireAppFixture fixture) : ChatPermissionTestBase(fixture)
{
    private const string WrittenWhileMember = "written while still a member";

    /// <summary>
    /// The TypeScript case also expected the thread list scoped to the room to be refused. The list ignores its
    /// entityId by design (Bug 82719), so that half cannot pass by construction and only the create is asserted.
    /// </summary>
    [Fact]
    [Trait("Bug", "82858")]
    public async Task Create_InARoomTheMemberWasRemovedFrom_Returns403()
    {
        var (roomId, member, _) = await ChattedThenRemovedAsync();
        var profile = await GetProfileAsync();

        await _aiChatClient.Authenticate(member);
        var refused = await OutcomeOfAsync(() => CreateThreadAsync("Thread from after they were removed", profile.Id, roomId.ToString()));

        // Control: the route still works for this caller, so the refusal is about the room and not a broken session.
        (await ListThreadsAsync()).Should().ContainSingle("the thread from when they were a member is still theirs");

        refused.Status.Should().Be(403, refused.Error);
    }

    [Fact]
    public async Task Thread_WrittenInARoomTheMemberWasRemovedFrom_StaysTheirs()
    {
        var (_, member, threadId) = await ChattedThenRemovedAsync();

        await _aiChatClient.Authenticate(member);

        (await GetThreadAsync(threadId)).ThreadId.Should().Be(threadId);
        (await ReadTextsAsync(threadId)).Should().Equal(WrittenWhileMember);

        (await _aiThreads.AiThreadsRenameAsync(new AiThreadsRenameRequest(threadId, "Renamed after leaving"), TestContext.Current.CancellationToken))
            .Success.Should().BeTrue();
        (await GetThreadAsync(threadId)).Title.Should().Be("Renamed after leaving");

        await AppendUserMessageAsync(threadId, "written after leaving");
        (await ReadAllAsync(threadId)).Should().HaveCount(2);
    }

    /// <summary>
    /// The send used to answer HTTP 200 with <c>{"type":"error","message":"stream error"}</c> in the body; a removed
    /// member's send now refuses with 403. The model is never reached.
    /// </summary>
    [Fact]
    [Trait("Bug", "82717")]
    public async Task SendWithStream_InARoomTheMemberWasRemovedFrom_Returns403()
    {
        var (roomId, member, threadId) = await ChattedThenRemovedAsync();
        var profile = await GetProfileAsync();
        var question = $"Reply with the single word OK. {Guid.NewGuid():N}";

        await _aiChatClient.Authenticate(member);
        var round = await SendWithStreamAsync(threadId, question, profile.Id, roomId.ToString());

        (await AssistantMessagesAsync(threadId)).Should().BeEmpty("the model was never called for a user who left the room");
        ModelProvider.CompletionRequestBodies.Should().NotContain(b => b.Contains(question));

        round.StatusCode.Should().Be(HttpStatusCode.Forbidden, round.ToString());
        round.Raw.Should().Contain("Forbidden");
        round.StreamError.Should().BeNull("the refusal is the status, not a stream frame");
    }

    /// <summary>
    /// An ordinary room and a RoomAdmin who chatted in it as a ContentCreator, then was removed. Every step is
    /// asserted: a revoke that silently failed would make the class describe a membership that never ended.
    /// </summary>
    private async Task<(int RoomId, User Member, string ThreadId)> ChattedThenRemovedAsync()
    {
        var profile = await GetProfileAsync();
        var roomId = await CreateCustomRoomAsync("Autotest Revoked Room");
        var member = await InviteMemberAsync(EmployeeType.RoomAdmin);
        await ShareRoomAsync(roomId, member.Id, FileShare.ContentCreator);

        await _aiChatClient.Authenticate(member);
        var thread = await CreateThreadAsync("Thread from when they were a member", profile.Id, roomId.ToString());
        await AppendUserMessageAsync(thread.ThreadId, WrittenWhileMember);
        await _aiChatClient.Authenticate(Owner);

        await ShareRoomAsync(roomId, member.Id, FileShare.None);

        // The room itself is untouched — what changed is only this user's access.
        (await RoomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response.Id.Should().Be(roomId);

        return (roomId, member, thread.ThreadId);
    }
}
