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

namespace ASC.AI.Tests.Tests.McpTests;

/// <summary>
/// The lifecycle of one pending call once it has already been resolved. <c>(threadId, messageId, idx)</c> still
/// points at a REAL, already-resolved tool call, and a second decision on it must neither be a replay that overwrites
/// the stored result and regenerates what followed, nor reach back into a message that is no longer the thread's last.
/// </summary>
/// <remarks>
/// The assertions are about absence of mutation, not a status code. A replay reaches the provider again, so each test
/// queues a different answer behind the expected ones: a replay then shows up as different stored text.
/// </remarks>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Mcp")]
public class McpToolCallReplayTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    private static readonly HostTool[] _tools = [_weatherTool];

    private async Task<(McpChat Chat, JsonElement Pending)> PausedAsync(params ScriptedAnswer[] behindTheCall)
    {
        var chat = await StartChatAsync();
        chat.Provider.Enqueue([WeatherCall(), .. behindTheCall]);

        return (chat, Pending(await SendAsync(chat, AskForWeather, _tools)));
    }

    private async Task<JsonElement> SettledReplyAsync(McpChat chat) => await OnlyReplyAsync(chat);

    [Fact]
    [Trait("Bug", "84283")]
    public async Task Approve_ASecondTimeWithADifferentResult_DoesNotOverwriteTheFirst()
    {
        var (chat, pending) = await PausedAsync(StubModelProvider.Text("First follow-up."), StubModelProvider.Text("Replayed follow-up."));
        (await ApproveAsync(chat, pending, "14C and sunny", tools: _tools)).StatusCode.Should().Be(HttpStatusCode.OK);
        ResultOf(ToolCallParts(await SettledReplyAsync(chat))[0]).Should().Be("14C and sunny");

        await ApproveAsync(chat, pending, "DIFFERENT RESULT SECOND CALL", tools: _tools);

        ResultOf(ToolCallParts(await SettledReplyAsync(chat))[0]).Should().Be("14C and sunny", "the first decision's result must survive a stale second approve");
    }

    [Fact]
    [Trait("Bug", "84285")]
    public async Task Deny_OnAnAlreadyApprovedCall_DoesNotOverwriteItsResult()
    {
        var (chat, pending) = await PausedAsync(StubModelProvider.Text("First follow-up."), StubModelProvider.Text("Replayed follow-up."));
        await ApproveAsync(chat, pending, "14C and sunny", tools: _tools);
        ResultOf(ToolCallParts(await SettledReplyAsync(chat))[0]).Should().Be("14C and sunny");

        await DenyAsync(chat, pending, _tools);

        ResultOf(ToolCallParts(await SettledReplyAsync(chat))[0]).Should().Be("14C and sunny", "an approved result must survive a stale deny");
    }

    [Fact]
    [Trait("Bug", "84287")]
    public async Task Approve_OnAnAlreadyDeniedCall_DoesNotOverwriteItsResult()
    {
        var (chat, pending) = await PausedAsync(StubModelProvider.Text("First follow-up."), StubModelProvider.Text("Replayed follow-up."));
        await DenyAsync(chat, pending, _tools);
        ResultOf(ToolCallParts(await SettledReplyAsync(chat))[0]).Should().Be("User deny tool call");

        await ApproveAsync(chat, pending, "14C and sunny", tools: _tools);

        ResultOf(ToolCallParts(await SettledReplyAsync(chat))[0]).Should().Be("User deny tool call", "a denial must survive a stale approve");
    }

    /// <summary>
    /// The canned deny result reads the same both times, so it cannot tell a replay apart from a true no-op here — the
    /// generated prose around it can: a repeat deny used to re-answer with differently-worded text.
    /// </summary>
    [Fact]
    [Trait("Bug", "84288")]
    public async Task Deny_ASecondTime_DoesNotRegenerateTheFollowUpText()
    {
        var (chat, pending) = await PausedAsync(StubModelProvider.Text("Follow-up one."), StubModelProvider.Text("Follow-up two."));
        await DenyAsync(chat, pending, _tools);
        var settledText = MessageText(await SettledReplyAsync(chat));

        await DenyAsync(chat, pending, _tools);

        MessageText(await SettledReplyAsync(chat)).Should().Be(settledText, "an already-denied call must not trigger a fresh generation on a repeat deny");
    }

    /// <summary>
    /// The escalated version: the stale call is no longer even the thread's last message — a real later turn exists.
    /// The later turn must survive, and the earlier message must not be reopened.
    /// </summary>
    [Fact]
    [Trait("Bug", "84291")]
    public async Task Approve_AStaleOneDoesNotReachBackIntoAnEarlierAlreadySupersededMessage()
    {
        var (chat, pending) = await PausedAsync(
            StubModelProvider.Text("First follow-up."),
            StubModelProvider.Text("Giraffes have long necks."),
            StubModelProvider.Text("Garbled replay."));
        await ApproveAsync(chat, pending, "14C and sunny", tools: _tools);
        var firstReply = await SettledReplyAsync(chat);

        // A whole new, unrelated turn on top of it.
        var second = await SendAsync(chat, "Now tell me a fun fact about giraffes, in one sentence.");
        second.StatusCode.Should().Be(HttpStatusCode.OK, second.ToString());
        var afterSecondTurn = await WaitForAssistantRepliesAsync(chat.ThreadId, 2);
        afterSecondTurn.Should().HaveCount(4, "user1, assistant1(tool), user2, assistant2");
        var secondReply = AssistantMessages(afterSecondTurn)[1];
        StatusOf(secondReply).Reason.Should().NotBe("error", secondReply.ToString());
        MessageText(secondReply).Should().NotBeEmpty();

        // The stale decision, made against a message that is no longer the thread's last one at all.
        await ApproveAsync(chat, pending, "STALE REPLAY RESULT", tools: _tools);

        var final = await ReadMessagesRawAsync(chat.ThreadId);
        final.Should().HaveCount(4, "the later turn was not deleted by the stale decision");
        var finalReplies = AssistantMessages(final);
        Str(finalReplies[1], "id").Should().Be(Str(secondReply, "id"));
        MessageText(finalReplies[1]).Should().Be(MessageText(secondReply));
        ResultOf(ToolCallParts(finalReplies[0])[0]).Should().Be(
            ResultOf(ToolCallParts(firstReply)[0]),
            "a message that is no longer the thread's last turn must not be reopened by a stale decision on it");
    }
}
