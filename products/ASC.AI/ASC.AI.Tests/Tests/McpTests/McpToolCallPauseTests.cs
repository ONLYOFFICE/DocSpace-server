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
/// Client-supplied ("host") tools and the pause they cause — the end-to-end half of tool execution, driven through a
/// real conversation. A request may carry its own tools in <c>actionArgs.tools</c>; they live for that one request.
/// <c>tool-call-pending</c> is the engine's ONLY pause point: the stream stops there mid-reply and nothing resumes it
/// but <c>approve-tool-call</c> / <c>deny-tool-call</c>.
/// </summary>
/// <remarks>
/// The model is scripted: the tool call the TypeScript suite waited for a real model to make is queued on the stub, and
/// the follow-up text after a result is queued behind it. What is asserted is the server's part — the frames, the
/// stored message and what the provider was asked.
/// </remarks>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Mcp")]
public class McpToolCallPauseTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    private static readonly HostTool[] _tools = [_weatherTool];

    [Fact]
    public async Task Send_AClientSuppliedTool_PausesTheStreamAtToolCallPending()
    {
        var chat = await StartChatAsync();
        chat.Provider.Enqueue(WeatherCall());

        var sent = await SendAsync(chat, AskForWeather, _tools);

        sent.StatusCode.Should().Be(HttpStatusCode.OK, sent.ToString());
        sent.StreamError.Should().BeNull(sent.ToString());
        var pending = Pending(sent);

        // The pause is the end of the stream: nothing follows it, no message-end — the reply is deliberately unfinished.
        var frames = FrameTypes(sent);
        frames[0].Should().Be("user-message-stored");
        frames[^1].Should().Be("tool-call-pending");
        frames.Should().NotContain("message-end");

        // Everything the UI needs to resume, and the decision it has to make.
        pending.GetProperty("threadId").GetString().Should().Be(chat.ThreadId);
        pending.GetProperty("messageId").GetString().Should().NotBeNullOrEmpty();
        pending.GetProperty("idx").ValueKind.Should().Be(JsonValueKind.Number);
        AutoAllowOf(pending).Should().BeFalse("a requireApproval tool prompts");

        // The half-finished reply is already persisted, tool call included, with the arguments but no result yet.
        var reply = await OnlyReplyAsync(chat);
        Str(reply, "id").Should().Be(pending.GetProperty("messageId").GetString());
        var calls = ToolCallParts(reply);
        calls.Should().ContainSingle();
        ToolNameOf(calls[0]).Should().Be("get_weather");
        ArgText(calls[0], "city").Should().Be("Paris");
        Str(calls[0], "toolCallId").Should().NotBeNullOrEmpty();
        ResultOf(calls[0]).Should().BeNull("no result until it is approved");
    }

    [Fact]
    public async Task Approve_AStringResult_ResumesThePausedReply()
    {
        var chat = await StartChatAsync();
        chat.Provider.Enqueue(WeatherCall(), StubModelProvider.Text("It is 21C and sunny in Paris."));
        var pending = Pending(await SendAsync(chat, AskForWeather, _tools));

        var approved = await ApproveAsync(chat, pending, "21C and sunny", tools: _tools);

        approved.StatusCode.Should().Be(HttpStatusCode.OK, approved.ToString());
        // Resuming continues the SAME message rather than starting a new one, and this time it runs to completion.
        var frames = FrameTypes(approved);
        frames.Should().Contain("message-end");
        frames.Should().NotContain("message-incomplete");

        var reply = await OnlyReplyAsync(chat);
        Str(reply, "id").Should().Be(pending.GetProperty("messageId").GetString());
        StatusOf(reply).Reason.Should().NotBe("error", reply.ToString());
        var calls = ToolCallParts(reply);
        calls.Should().ContainSingle();
        ResultOf(calls[0]).Should().Be("21C and sunny");
        MessageText(reply).Should().Contain("21");

        // The result reached the model: it is in the continuation request the provider received.
        // Picked by content, not position: the title request of a new thread goes to the same provider.
        StreamedRequestsCarrying(chat.Provider, AskForWeather)
            .Should().Contain(body => body.Contains("21C and sunny", StringComparison.Ordinal), chat.Provider.Describe());
    }

    /// <summary>
    /// An MCP tool answers with JSON, so an object is the ordinary result shape. It used to be forwarded as an object
    /// and the continuation request failed to bind, leaving the reply abandoned half-written.
    /// </summary>
    [Fact]
    [Trait("Bug", "82862")]
    public async Task Approve_AStructuredResult_ResumesTheReply()
    {
        var chat = await StartChatAsync();
        chat.Provider.Enqueue(WeatherCall(), StubModelProvider.Text("It is 21C and sunny."));
        var pending = Pending(await SendAsync(chat, AskForWeather, _tools));

        var approved = await ApproveAsync(chat, pending, new { temperature = "21C", conditions = "sunny" }, tools: _tools);

        approved.StatusCode.Should().Be(HttpStatusCode.OK, approved.ToString());
        var reply = await OnlyReplyAsync(chat);
        // The object is serialised on the way in, which is what makes the continuation request bindable.
        ResultOf(ToolCallParts(reply)[0]).Should().Be("""{"temperature":"21C","conditions":"sunny"}""");
        StatusOf(reply).Reason.Should().NotBe("error", reply.ToString());
        FrameTypes(approved).Should().Contain("message-end");
    }

    [Fact]
    public async Task Deny_TheRefusalIsPassedToTheModel_AndTheReplyFinishes()
    {
        var chat = await StartChatAsync();
        chat.Provider.Enqueue(WeatherCall(), StubModelProvider.Text("I could not check the weather."));
        var pending = Pending(await SendAsync(chat, AskForWeather, _tools));

        var denied = await DenyAsync(chat, pending, _tools);

        denied.StatusCode.Should().Be(HttpStatusCode.OK, denied.ToString());
        FrameTypes(denied).Should().Contain("message-end");

        // A denial is not a dropped conversation: the refusal is written into the tool call as its result and the model answers around it.
        var reply = await OnlyReplyAsync(chat);
        StatusOf(reply).Reason.Should().NotBe("error", reply.ToString());
        ResultOf(ToolCallParts(reply)[0]).Should().Be("User deny tool call");
        MessageText(reply).Should().NotBeEmpty();
        StreamedRequestsCarrying(chat.Provider, AskForWeather)
            .Should().Contain(body => body.Contains("User deny tool call", StringComparison.Ordinal), chat.Provider.Describe());
    }

    /// <summary>
    /// Pending A -> approve A -> pending B -> approve B -> final: the second tool is genuinely a second hop, and both
    /// calls, their results and the final answer end up in ONE message, in order.
    /// </summary>
    [Fact]
    public async Task TwoHop_PendingApprovePendingApproveFinal_FoldsEverythingIntoOneMessage()
    {
        var chat = await StartChatAsync("Autotest Two-Hop thread");
        HostTool[] both = [_weatherTool, _recommendationTool];
        chat.Provider.Enqueue(WeatherCall(), RecommendationCall(14), StubModelProvider.Text("Wear a light jacket."));

        var pendingA = Pending(await SendAsync(chat, "Call get_weather, then get_recommendation with its temperature.", both));
        ToolNameOf(PendingPart(pendingA)).Should().Be("get_weather");

        var approveA = await ApproveAsync(chat, pendingA, "14", tools: both);
        approveA.StatusCode.Should().Be(HttpStatusCode.OK, approveA.ToString());

        // The resume paused again rather than finishing.
        var pendingB = Pending(approveA, "for the second hop");
        pendingB.GetProperty("messageId").GetString().Should().Be(pendingA.GetProperty("messageId").GetString());
        pendingB.GetProperty("idx").GetInt32().Should().NotBe(pendingA.GetProperty("idx").GetInt32());

        // Both calls live in ONE message's content array, at their own indices. A's result is filled in, B is still unresolved.
        var midway = pendingB.GetProperty("message").GetProperty("content");
        var partA = midway[pendingA.GetProperty("idx").GetInt32()];
        var partB = midway[pendingB.GetProperty("idx").GetInt32()];
        ToolNameOf(partA).Should().Be("get_weather");
        ResultOf(partA).Should().Be("14");
        ToolNameOf(partB).Should().Be("get_recommendation");
        ResultOf(partB).Should().BeNull("B is untouched until it is approved itself");
        ArgText(partB, "temperatureCelsius").Should().Be("14");

        var approveB = await ApproveAsync(chat, pendingB, "wear a light jacket", tools: both);
        approveB.StatusCode.Should().Be(HttpStatusCode.OK, approveB.ToString());
        FrameTypes(approveB).Should().Contain("message-end").And.NotContain("tool-call-pending");

        var messages = await WaitForAssistantRepliesAsync(chat.ThreadId);
        UserMessages(messages).Should().ContainSingle();
        var replies = AssistantMessages(messages);
        replies.Should().ContainSingle("still one assistant message, not two");
        Str(replies[0], "id").Should().Be(pendingA.GetProperty("messageId").GetString());
        StatusOf(replies[0]).Reason.Should().NotBe("error", replies[0].ToString());

        var content = replies[0].GetProperty("content");
        ToolNameOf(content[pendingA.GetProperty("idx").GetInt32()]).Should().Be("get_weather");
        ResultOf(content[pendingA.GetProperty("idx").GetInt32()]).Should().Be("14", "A's result from the first approve survived");
        ToolNameOf(content[pendingB.GetProperty("idx").GetInt32()]).Should().Be("get_recommendation");
        ResultOf(content[pendingB.GetProperty("idx").GetInt32()]).Should().Be("wear a light jacket");
        ToolCallParts(replies[0]).Should().HaveCount(2);
        MessageText(replies[0]).Should().NotBeEmpty("the wrap-up the model gives once both calls are resolved");
    }
}
