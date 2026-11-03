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
/// What <c>approve-tool-call</c> and <c>deny-tool-call</c> do with a confirmation that refers to nothing, or comes
/// from someone who may not decide: refused with 409 (the <c>(messageId, idx)</c> pair is the identity), 401, or 403.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Mcp")]
public class McpToolCallDecisionTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    private const string UnknownMessageId = "019fcc1d-478e-749f-85df-7427ca64566b";

    public static TheoryData<string> Decisions() => new() { "approve-tool-call", "deny-tool-call" };

    /// <summary>A decision body the way the dialog sends it, for a message and index of the caller's choosing.</summary>
    private async Task<(HttpStatusCode Status, string Text)> DecideAsync(string route, McpChat chat, string messageId, int idx)
    {
        var message = new AiThreadMessageLike(
            role: AiThreadMessageLike.RoleEnum.Assistant,
            content: new AiMessageContent([new AiMessageContentBlock(type: "text", text: string.Empty)]));
        var json = route == "approve-tool-call"
            ? new AiApproveToolCallRequest(new Newtonsoft.Json.Linq.JObject(), false, chat.ThreadId, messageId, idx, message, null!, chat.RoomId.ToString(), chat.Profile.Id).ToJson()
            : new AiToolCallData(chat.ThreadId, messageId, idx, message, null!, chat.RoomId.ToString(), chat.Profile.Id).ToJson();

        // Raw response on purpose — an SDK defect: the decision routes answer an ndjson stream (or a refusal) the typed
        // call cannot read. The request is the SDK model.
        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(
            $"{AiChatPath}/{route}",
            json,
            TestContext.Current.CancellationToken);

        return (response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [MemberData(nameof(Decisions))]
    public async Task Decision_ForAMessageWithNoToolCall_Is409_AndInventsNothing(string route)
    {
        var chat = await StartChatAsync();
        var messageId = await AppendUserMessageAsync(chat.ThreadId, "Autotest question");

        var (status, text) = await DecideAsync(route, chat, messageId, 0);

        status.Should().Be(HttpStatusCode.Conflict, text);
        text.Should().Contain(messageId, "the refusal names the message").And.Contain("has no tool call at index 0");

        // Neither decision invented a tool result or a reply in the thread.
        var messages = await ReadStoredMessagesAsync(chat.ThreadId);
        messages.Select(m => Str(m, "id")).Should().Equal(messageId);
        AssistantMessages(messages).Should().BeEmpty();
    }

    /// <summary>There is no request id in this API: the (messageId, idx) pair is the identity, and the message half is checked first.</summary>
    [Theory]
    [InlineData(UnknownMessageId, 0)]
    [InlineData(UnknownMessageId, 999)]
    [InlineData(UnknownMessageId, -1)]
    public async Task Approve_AnUnknownMessageIdAndAnOutOfRangeIndex_AreRefusedWith409(string messageId, int idx)
    {
        var chat = await StartChatAsync();

        var (status, text) = await DecideAsync("approve-tool-call", chat, messageId, idx);

        status.Should().Be(HttpStatusCode.Conflict, text);
        (await ReadStoredMessagesAsync(chat.ThreadId)).Should().BeEmpty("nothing was created");
    }

    [Theory]
    [MemberData(nameof(Decisions))]
    public async Task Decision_Anonymous_Gets401(string route)
    {
        var chat = await StartChatAsync();
        await ActAsAsync(null);

        var (status, text) = await DecideAsync(route, chat, UnknownMessageId, 0);

        status.Should().Be(HttpStatusCode.Unauthorized, text);
    }

    /// <summary>
    /// The confirmation routes carry the same ownership check as every neighbouring route: a user cannot answer the
    /// model's permission prompt in a thread they cannot even read. They used to answer 200 here.
    /// </summary>
    [Theory]
    [Trait("Bug", "82837")]
    [MemberData(nameof(Decisions))]
    public async Task Decision_ByAUserWhoCannotReadTheThread_Is403(string route)
    {
        var chat = await StartChatAsync();
        var messageId = await AppendUserMessageAsync(chat.ThreadId, "Autotest question");
        var outsider = await InviteRoleAsync(McpRole.User);
        await ActAsAsync(outsider);

        var thread = await Assert.ThrowsAsync<ApiException>(async () => await _aiThreads.AiThreadsGetByIdAsync(chat.ThreadId, TestContext.Current.CancellationToken));
        thread.ErrorCode.Should().Be(403, "the thread itself is closed to them");
        var message = await Assert.ThrowsAsync<ApiException>(async () => await _aiThreads.AiThreadsGetMessageByIdAsync(messageId, TestContext.Current.CancellationToken));
        message.ErrorCode.Should().Be(403);

        var (status, text) = await DecideAsync(route, chat, messageId, 0);
        await ActAsAsync(Owner);

        status.Should().Be(HttpStatusCode.Forbidden, text);
        (await ReadStoredMessagesAsync(chat.ThreadId)).Select(m => Str(m, "id")).Should().Equal(messageId);
    }

    /// <summary>The switch is meant to stop "an MCP tool cannot be invoked"; the decision half used to be accepted with it off.</summary>
    [Theory]
    [Trait("Bug", "82838")]
    [MemberData(nameof(Decisions))]
    public async Task Decision_WithAiAccessDisabled_Is403(string route)
    {
        var chat = await StartChatAsync();

        await SetPortalAiAccessAsync(false);

        // The rest of the thread surface is gated, so the switch did take effect.
        var thread = await Assert.ThrowsAsync<ApiException>(async () => await _aiThreads.AiThreadsGetByIdAsync(chat.ThreadId, TestContext.Current.CancellationToken));
        thread.ErrorCode.Should().Be(403, "reading the thread is gated");

        var (status, text) = await DecideAsync(route, chat, UnknownMessageId, 0);

        status.Should().Be(HttpStatusCode.Forbidden, text);
    }
}
