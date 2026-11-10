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
/// <c>ai/regenerate-stream</c> by someone whose thread it is not, and with the portal AI switch off. The side effect
/// is checked first in each: a refusal that had already regenerated the reply would be the worse bug, and the
/// status alone does not tell a refusal from a silent regenerate.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Messages")]
public class MessageRegenerateAccessTests(AspireAppFixture fixture) : MessagePart1TestBase(fixture)
{
    private const string Question = "Reply with the single word OK.";
    private const string Answer = "OK";

    /// <summary>
    /// The block was always real — nothing generated, the owner's reply untouched — but the refusal used to arrive
    /// as HTTP 200 with <c>{"type":"error","message":"stream error"}</c> in the body. It now refuses like every
    /// non-streaming route into someone else's thread.
    /// </summary>
    [Fact]
    [Trait("Bug", "82717")]
    public async Task RegenerateStream_NonMember_IsRefusedAndTheReplyIsKept()
    {
        var (profile, threadId) = await CreateScriptedThreadAsync();
        var reply = Replies(await SendAnsweredAsync(threadId, Question, Answer, profile.Id)).Single();
        var requestsBefore = StreamedRequests(Provider);

        var member = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await _aiChatClient.Authenticate(member);

        var round = await RegenerateAsync(threadId, profile.Id);

        await _aiChatClient.Authenticate(Owner);
        await AssertReplyKeptAsync(threadId, reply, requestsBefore);

        round.StatusCode.Should().Be(HttpStatusCode.Forbidden, round.ToString());
        round.StreamError.Should().BeNull("the refusal is the status, not a stream frame");
    }

    /// <summary>
    /// Membership buys the room, not other people's conversations — threads are per user. The <c>entityId</c> in
    /// the body IS a room the caller belongs to, so a check that only looked at the entity would let it through.
    /// </summary>
    [Fact]
    [Trait("Bug", "82717")]
    public async Task RegenerateStream_MemberOfTheRoom_IsRefusedAndTheReplyIsKept()
    {
        var room = await CreateRoomAsync();
        var roomId = room.ToString();
        var (profile, threadId) = await CreateScriptedThreadAsync(roomId);
        var reply = Replies(await SendAnsweredAsync(threadId, Question, Answer, profile.Id, roomId)).Single();
        var requestsBefore = StreamedRequests(Provider);

        var member = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        await InviteToRoomAsync(room, member);
        await _aiChatClient.Authenticate(member);

        var round = await RegenerateAsync(threadId, profile.Id, roomId);

        await _aiChatClient.Authenticate(Owner);
        await AssertReplyKeptAsync(threadId, reply, requestsBefore);

        round.StatusCode.Should().Be(HttpStatusCode.Forbidden, round.ToString());
        round.StreamError.Should().BeNull("the refusal is the status, not a stream frame");
    }

    /// <summary>
    /// An enabled-to-disabled transition rather than an end state: the regenerate that worked before the flip is
    /// what makes the one after it mean something. Inference was stopped either way, but the refusal used to come
    /// back as HTTP 200 with an opaque "stream error".
    /// </summary>
    [Fact]
    [Trait("Bug", "82724")]
    public async Task RegenerateStream_AiDisabled_Returns403AndGeneratesNothing()
    {
        await SetPortalAiAccessAsync(true);

        var (profile, threadId) = await CreateScriptedThreadAsync();
        var firstReply = Replies(await SendAnsweredAsync(threadId, Question, Answer, profile.Id)).Single();

        Provider.Enqueue(StubModelProvider.Text("regenerated"));
        var working = await RegenerateAsync(threadId, profile.Id);
        working.StatusCode.Should().Be(HttpStatusCode.OK, working.ToString());
        working.StreamError.Should().BeNull(working.ToString());

        var regenerated = await WaitForReplacedReplyAsync(threadId, firstReply.Id);
        var survivor = Replies(regenerated).Should().ContainSingle().Subject;
        TextOf(survivor).Should().Be("regenerated");
        var requestsBefore = StreamedRequests(Provider);

        await SetPortalAiAccessAsync(false);

        var refused = await RegenerateAsync(threadId, profile.Id);

        // read-messages is 403 while the switch is off, so the thread is inspected after turning AI back on —
        // which doubles as the proof that the reply is readable at all.
        await SetPortalAiAccessAsync(true);
        await AssertReplyKeptAsync(threadId, survivor, requestsBefore);

        refused.StatusCode.Should().Be(HttpStatusCode.Forbidden, refused.ToString());
        refused.StreamError.Should().BeNull("the refusal is the status, not a stream frame");
    }

    /// <summary>Watches the thread for <see cref="MessagePart1TestBase.SettleTime"/>: the one reply must stay the same object.</summary>
    private async Task AssertReplyKeptAsync(string threadId, AiThreadMessageLike reply, int requestsBefore)
    {
        var messages = await WaitForMessagesAsync(threadId, m => Replies(m).Any(r => r.Id != reply.Id), SettleTime);

        var kept = Replies(messages).Should().ContainSingle("the thread still holds one reply").Subject;
        kept.Id.Should().Be(reply.Id, "and it is the same reply");
        TextOf(kept).Should().Be(TextOf(reply));
        StreamedRequests(Provider).Should().Be(requestsBefore, $"the model was not asked\n{Provider.Describe()}");
    }
}
