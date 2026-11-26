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
/// <c>regenerate-stream</c> over a resolved tool-call history. The operation's contract: the previous reply, tool-call
/// hops included, is replaced by a fresh reply streamed against the unchanged prompt; no title is generated. The chat
/// library keeps a finished replaced reply as an earlier version in <c>metadata.custom.replyVersions.others</c> of the
/// new one, so the user can switch back to it; only the message's own content is the active reply and the history the
/// model gets on the next round. A caller controls whether the model has a tool to call again by re-sending
/// <c>tools</c> on the regenerate call itself.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Mcp")]
public class McpToolCallRegenerateTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    private static readonly HostTool[] _tools = [_weatherTool];

    /// <summary>
    /// Waits for the old reply to be gone and a replacement to exist, and returns what it last saw: the regenerated
    /// reply is stored by the round, so a bare read right after the request can race it.
    /// </summary>
    private async Task<List<JsonElement>> WaitForReplacementAsync(McpChat chat, string oldReplyId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var messages = await ReadStoredMessagesAsync(chat.ThreadId);
            var replies = AssistantMessages(messages);

            if ((replies.Count >= 1 && replies.All(r => Str(r, "id") != oldReplyId)) || DateTime.UtcNow >= deadline)
            {
                return messages;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>The earlier versions a regenerated reply keeps in <c>metadata.custom.replyVersions.others</c>, oldest first.</summary>
    private static List<JsonElement> EarlierVersionsOf(JsonElement reply)
    {
        if (reply.TryGetProperty("metadata", out var metadata) && metadata.ValueKind == JsonValueKind.Object
            && metadata.TryGetProperty("custom", out var custom) && custom.ValueKind == JsonValueKind.Object
            && custom.TryGetProperty("replyVersions", out var versions) && versions.ValueKind == JsonValueKind.Object
            && versions.TryGetProperty("others", out var others) && others.ValueKind == JsonValueKind.Array)
        {
            return [.. others.EnumerateArray()];
        }

        return [];
    }

    /// <summary>The stored thread with the earlier reply versions taken out: what the user sees as the thread.</summary>
    private static string WithoutVersions(IEnumerable<JsonElement> messages) =>
        string.Join("\n", messages.Select(message =>
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse(message.GetRawText())!;
            (node["metadata"]?["custom"] as System.Text.Json.Nodes.JsonObject)?.Remove("replyVersions");

            return node.ToJsonString();
        }));

    /// <summary>The streamed chat requests the model received after the first <paramref name="skip"/> requests.</summary>
    private static List<string> StreamedRequestsAfter(StubModelProvider provider, int skip) =>
        [.. provider.CompletionRequestBodies.Skip(skip).Where(StubModelProvider.IsStreamed)];

    [Fact]
    public async Task Regenerate_TheOldToolCallAndItsResultAreReplacedWhole_NotOrphanedOrDuplicated()
    {
        var chat = await StartChatAsync();
        chat.Provider.Enqueue(WeatherCall(), StubModelProvider.Text("It is sunny."), StubModelProvider.Text("A fresh reply without any tool."));
        var pending = Pending(await SendAsync(chat, AskForWeather, _tools));
        await ApproveAsync(chat, pending, "14C and sunny", tools: _tools);

        var before = await ReadStoredMessagesAsync(chat.ThreadId);
        var question = UserMessages(before)[0];
        var oldReply = AssistantMessages(before)[0];
        var oldToolCallId = Str(ToolCallParts(oldReply)[0], "toolCallId");
        oldToolCallId.Should().NotBeNullOrEmpty();

        // No tools re-offered: the model cannot redo the call at all, the cleanest way to see whether the OLD call and
        // result survive anywhere they should not.
        var requestsBefore = chat.Provider.CompletionRequestBodies.Count();
        var regen = await RegenerateAsync(chat);

        regen.StatusCode.Should().Be(HttpStatusCode.OK, regen.ToString());
        regen.StreamError.Should().BeNull(regen.ToString());
        FrameTypes(regen).Should().NotContain("thread-title", "regenerate never generates a title");

        var after = await WaitForReplacementAsync(chat, Str(oldReply, "id")!);
        UserMessages(after).Select(m => Str(m, "id")).Should().BeEquivalentTo([Str(question, "id")], "the question survives untouched");
        var replies = AssistantMessages(after);
        replies.Should().ContainSingle("replaced rather than appended to");
        Str(replies[0], "id").Should().NotBe(Str(oldReply, "id"));
        StatusOf(replies[0]).Reason.Should().NotBe("error", replies[0].ToString());
        ToolCallParts(replies[0]).Should().BeEmpty();

        var shown = WithoutVersions(after);
        shown.Should().NotContain(oldToolCallId!, "the old tool call's id is not part of the thread any more");
        shown.Should().NotContain("14C and sunny", "the old tool call's result is not part of the thread any more");
        var toModel = StreamedRequestsAfter(chat.Provider, requestsBefore);
        toModel.Should().NotBeEmpty("the regenerate asked the model");
        toModel.Should().OnlyContain(
            body => !body.Contains(oldToolCallId!, StringComparison.Ordinal) && !body.Contains("14C and sunny", StringComparison.Ordinal),
            "the model's history does not carry the replaced chain");

        var earlier = EarlierVersionsOf(replies[0]);
        earlier.Should().ContainSingle("the replaced reply is kept as the one earlier version: {0}", replies[0]);
        var keptCall = ToolCallParts(earlier[0]).Should().ContainSingle().Which;
        Str(keptCall, "toolCallId").Should().Be(oldToolCallId);
        ResultOf(keptCall).Should().Be("14C and sunny");
    }

    /// <summary>This is the case that would catch a regenerate that only rewinds as far as the LAST tool call.</summary>
    [Fact]
    public async Task Regenerate_ATwoHopToolChain_IsReplacedWhole_NotJustItsLastCall()
    {
        var chat = await StartChatAsync();
        HostTool[] both = [_weatherTool, _recommendationTool];
        chat.Provider.Enqueue(
            WeatherCall(),
            RecommendationCall(14),
            StubModelProvider.Text("Wear a light jacket."),
            StubModelProvider.Text("A fresh reply without any tool."));
        var pendingA = Pending(await SendAsync(chat, "Call get_weather, then get_recommendation.", both));
        var pendingB = Pending(await ApproveAsync(chat, pendingA, "14", tools: both), "for the second hop");
        await ApproveAsync(chat, pendingB, "wear a light jacket", tools: both);

        var oldReply = AssistantMessages(await ReadStoredMessagesAsync(chat.ThreadId))[0];
        var oldCalls = ToolCallParts(oldReply);
        oldCalls.Should().HaveCount(2, "both hops resolved before the regenerate");
        var oldIds = oldCalls.Select(c => Str(c, "toolCallId")!).ToList();

        var requestsBefore = chat.Provider.CompletionRequestBodies.Count();
        var regen = await RegenerateAsync(chat);

        regen.StatusCode.Should().Be(HttpStatusCode.OK, regen.ToString());
        regen.StreamError.Should().BeNull(regen.ToString());
        var after = await WaitForReplacementAsync(chat, Str(oldReply, "id")!);
        var replies = AssistantMessages(after);
        replies.Should().ContainSingle();
        Str(replies[0], "id").Should().NotBe(Str(oldReply, "id"));
        ToolCallParts(replies[0]).Should().BeEmpty("neither hop survives");
        var shown = WithoutVersions(after);
        oldIds.Should().OnlyContain(id => !shown.Contains(id, StringComparison.Ordinal));
        var toModel = StreamedRequestsAfter(chat.Provider, requestsBefore);
        toModel.Should().NotBeEmpty("the regenerate asked the model");
        toModel.Should().OnlyContain(body => oldIds.All(id => !body.Contains(id, StringComparison.Ordinal)), "neither hop reaches the model's history");

        var earlier = EarlierVersionsOf(replies[0]);
        earlier.Should().ContainSingle("the replaced reply is kept as the one earlier version: {0}", replies[0]);
        ToolCallParts(earlier[0]).Select(c => Str(c, "toolCallId")).Should().Equal(oldIds, "the earlier version keeps both hops in order");
    }

    [Fact]
    public async Task Regenerate_AReplyThatItselfCallsATool_StartsACleanNewChain()
    {
        var chat = await StartChatAsync();
        chat.Provider.Enqueue(
            WeatherCall(callId: "call_weather_old"),
            StubModelProvider.Text("It is sunny."),
            WeatherCall(callId: "call_weather_new"),
            StubModelProvider.Text("It is windy."));
        var pending = Pending(await SendAsync(chat, AskForWeather, _tools));
        await ApproveAsync(chat, pending, "OLD RESULT 14C and sunny", tools: _tools);
        var oldReply = AssistantMessages(await ReadStoredMessagesAsync(chat.ThreadId))[0];
        var oldToolCallId = Str(ToolCallParts(oldReply)[0], "toolCallId");

        // `tools` IS re-offered this time, so the model can redo the call.
        var requestsBefore = chat.Provider.CompletionRequestBodies.Count();
        var regen = await RegenerateAsync(chat, _tools);

        regen.StatusCode.Should().Be(HttpStatusCode.OK, regen.ToString());
        var newPending = Pending(regen, "for the regenerated reply");
        newPending.GetProperty("messageId").GetString().Should().NotBe(Str(oldReply, "id"));
        var newToolCallId = Str(PendingPart(newPending), "toolCallId");
        newToolCallId.Should().NotBe(oldToolCallId, "the regenerated chain gets its own tool-call id");

        var approved = await ApproveAsync(chat, newPending, "NEW RESULT 20C and windy", tools: _tools);

        approved.StatusCode.Should().Be(HttpStatusCode.OK, approved.ToString());
        FrameTypes(approved).Should().Contain("message-end");

        var final = await ReadStoredMessagesAsync(chat.ThreadId);
        var replies = AssistantMessages(final);
        replies.Should().ContainSingle("one message, the regenerated one");
        Str(replies[0], "id").Should().Be(newPending.GetProperty("messageId").GetString());
        var calls = ToolCallParts(replies[0]);
        calls.Should().ContainSingle("only the new chain's call, not both");
        Str(calls[0], "toolCallId").Should().Be(newToolCallId);
        ResultOf(calls[0]).Should().Be("NEW RESULT 20C and windy");

        var shown = WithoutVersions(final);
        shown.Should().NotContain(oldToolCallId!, "the old chain's call id does not leak into the new one");
        shown.Should().NotContain("OLD RESULT 14C and sunny", "the old chain's result does not leak into the new one");
        var toModel = StreamedRequestsAfter(chat.Provider, requestsBefore);
        toModel.Should().HaveCountGreaterThanOrEqualTo(2, "the regenerate and the approval each asked the model");
        toModel.Should().OnlyContain(
            body => !body.Contains(oldToolCallId!, StringComparison.Ordinal) && !body.Contains("OLD RESULT 14C and sunny", StringComparison.Ordinal),
            "the old chain does not reach the model while the new one runs");

        var earlier = EarlierVersionsOf(replies[0]);
        earlier.Should().ContainSingle("the replaced reply is kept as the one earlier version through the new chain's approval: {0}", replies[0]);
        var keptCall = ToolCallParts(earlier[0]).Should().ContainSingle().Which;
        Str(keptCall, "toolCallId").Should().Be(oldToolCallId);
        ResultOf(keptCall).Should().Be("OLD RESULT 14C and sunny");
    }

    /// <summary>
    /// <c>actionArgs.prompt</c> in replace mode applies to the regenerated round only; the next ordinary turn is not
    /// affected. Seen from the provider: the marker is in the regenerate request and not in the next turn's.
    /// </summary>
    [Fact]
    public async Task Regenerate_APromptReplace_AppliesToTheRegenerate_AndDoesNotStickToTheNextTurn()
    {
        const string marker = "ZZREGENPROMPTZZ";
        var chat = await StartChatAsync();
        chat.Provider.Enqueue(StubModelProvider.Text("Hi there!"), StubModelProvider.Text("Hello, short and sweet."), StubModelProvider.Text("Hi again."));
        (await SendAsync(chat, "Hi there!")).StatusCode.Should().Be(HttpStatusCode.OK);
        var first = AssistantMessages(await WaitForAssistantRepliesAsync(chat.ThreadId))[0];

        var regen = await RegenerateAsync(chat, replacePrompt: $"You are a helpful test assistant. Keep answers very short. Formatting rule: finish every reply with the exact token {marker}.");

        regen.StatusCode.Should().Be(HttpStatusCode.OK, regen.ToString());
        regen.StreamError.Should().BeNull(regen.ToString());
        await WaitForReplacementAsync(chat, Str(first, "id")!);
        StreamedRequestsCarrying(chat.Provider, marker).Should().NotBeEmpty("the per-request prompt reached the regenerate");

        var second = await SendAsync(chat, "Say hi again.");

        second.StatusCode.Should().Be(HttpStatusCode.OK, second.ToString());
        var nextTurn = StreamedRequestsCarrying(chat.Provider, "Say hi again.");
        nextTurn.Should().NotBeEmpty();
        nextTurn.Should().OnlyContain(body => !body.Contains(marker, StringComparison.Ordinal), "the override does not persist into the next ordinary turn");
    }
}
