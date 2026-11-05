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
/// The announcement that comes with a tool call — the API-side half of "before calling a tool the model writes a short
/// phrase about what it is about to do, and the technical call card is hidden so the two do not duplicate each other".
/// Only the first half is an API contract, and it is not chat text: the engine injects an <c>aiChatIntent</c> parameter
/// into every tool's schema, so the phrase arrives as one of the arguments the model chose. The sentence is attached
/// to the call rather than written next to it.
/// </summary>
/// <remarks>
/// The stub model supplies the phrase the way a real one does — as an argument of the call — and the provider's
/// request shows the engine offered the parameter at all.
/// </remarks>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Mcp")]
public class McpToolCallAnnouncementTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    private static readonly HostTool[] _tools = [_weatherTool];

    private static string? IntentOf(JsonElement part) => ArgText(part, "aiChatIntent");

    /// <summary>The announcement is prose, so the assertions are on its shape: not blank, not the bare tool name, not serialised arguments.</summary>
    private static void ShouldBeAnAnnouncement(string? intent, string label)
    {
        intent.Should().NotBeNull($"{label}: aiChatIntent is a string");
        var phrase = intent!.Trim();
        phrase.Should().NotBeEmpty($"{label}: the phrase is not blank");
        phrase.Should().NotBe(_weatherTool.Name, $"{label}: the phrase is not the bare tool name");
        phrase.Should().NotStartWith("[", $"{label}: the phrase is not serialised arguments").And.NotStartWith("{");
    }

    /// <summary>Whether the provider was offered the tool with an <c>aiChatIntent</c> parameter in its schema.</summary>
    private static bool OfferedWithIntent(string requestBody, string tool)
    {
        using var document = JsonDocument.Parse(requestBody);

        return document.RootElement.TryGetProperty("tools", out var tools)
            && tools.ValueKind == JsonValueKind.Array
            && tools.EnumerateArray().Any(t =>
                t.TryGetProperty("function", out var function)
                && Str(function, "name") == tool
                && function.TryGetProperty("parameters", out var parameters)
                && parameters.TryGetProperty("properties", out var properties)
                && properties.TryGetProperty("aiChatIntent", out _));
    }

    [Fact]
    public async Task Pending_CarriesTheModelsAiChatIntentPhrase_AndKeepsItInTheThread()
    {
        var chat = await StartChatAsync();
        chat.Provider.Enqueue(WeatherCall());

        var sent = await SendAsync(chat, AskForWeather, _tools);

        sent.StatusCode.Should().Be(HttpStatusCode.OK, sent.ToString());
        sent.StreamError.Should().BeNull(sent.ToString());
        var pending = Pending(sent);

        // The engine offered the parameter: it is part of the schema the model saw.
        chat.Provider.CompletionRequestBodies.Any(b => OfferedWithIntent(b, _weatherTool.Name)).Should().BeTrue(
            "the engine injects aiChatIntent into every tool's schema\n" + chat.Provider.Describe());

        // On the frame, not only in the thread: the client draws the pause from what the stream just handed it.
        var streamed = IntentOf(PendingPart(pending));
        ShouldBeAnAnnouncement(streamed, "on the tool-call-pending frame");

        // And it is stored, so reopening the thread shows the same sentence.
        var reply = await OnlyReplyAsync(chat);
        Str(reply, "id").Should().Be(pending.GetProperty("messageId").GetString());
        var call = ToolCallParts(reply)[0];
        ShouldBeAnAnnouncement(IntentOf(call), "in the stored reply");
        IntentOf(call).Should().Be(streamed, "the stored phrase is the streamed one");

        // The phrase rides along with the real arguments — it does not replace them.
        ArgText(call, "city").Should().Be("Paris");
    }

    [Fact]
    public async Task Pending_TheAnnouncementIsNotDuplicatedAsChatTextNextToTheCall()
    {
        var chat = await StartChatAsync();
        chat.Provider.Enqueue(WeatherCall());
        Pending(await SendAsync(chat, AskForWeather, _tools));

        var reply = await OnlyReplyAsync(chat);

        reply.GetProperty("content").ValueKind.Should().Be(JsonValueKind.Array, "a paused reply has content blocks, not the failed reply's empty string");
        var parts = reply.GetProperty("content").EnumerateArray().ToList();
        var callIndex = parts.FindIndex(p => Str(p, "type") == "tool-call");
        callIndex.Should().BeGreaterThan(-1, "the reply has a tool-call part");
        var intent = IntentOf(parts[callIndex]);
        ShouldBeAnAnnouncement(intent, "at the pause");

        // Whether the model adds prose of its own is its decision; a reply that both explains itself in text AND carries
        // the same sentence in the call is the duplication the requirement rules out.
        var texts = parts.Where(p => Str(p, "type") != "tool-call").Select(p => Str(p, "text")?.Trim() ?? string.Empty).Where(t => t.Length > 0);
        texts.Where(t => t.Contains(intent!, StringComparison.Ordinal)).Should().BeEmpty("the announcement is carried once, by the tool call");

        // Content order is render order, and the pause is the end of the reply.
        parts.Skip(callIndex + 1).Where(p => !string.IsNullOrWhiteSpace(Str(p, "text"))).Should().BeEmpty("no text trails the paused call");
    }

    /// <summary>Resuming rewrites the tool call to add the result; the phrase is part of the same args, so this is where it would be lost.</summary>
    [Fact]
    public async Task Approve_TheAnnouncementSurvivesTheApprovalThatFillsInTheResult()
    {
        var chat = await StartChatAsync();
        chat.Provider.Enqueue(WeatherCall(), StubModelProvider.Text("It is 21C and sunny."));
        var pending = Pending(await SendAsync(chat, AskForWeather, _tools));
        var announced = IntentOf(PendingPart(pending));
        ShouldBeAnAnnouncement(announced, "at the pause");

        var approved = await ApproveAsync(chat, pending, "21C and sunny", tools: _tools);

        approved.StatusCode.Should().Be(HttpStatusCode.OK, approved.ToString());
        var call = ToolCallParts(await OnlyReplyAsync(chat))[0];
        ResultOf(call).Should().Be("21C and sunny", "the approval landed");
        IntentOf(call).Should().Be(announced, "the phrase is still there");
    }

    /// <summary>`requireApproval: false` is the case with no dialog to put a sentence in: the phrase belongs to the call itself.</summary>
    [Fact]
    public async Task Pending_AToolThatNeedsNoApprovalDialog_IsAnnouncedToo()
    {
        var chat = await StartChatAsync();
        chat.Provider.Enqueue(WeatherCall());

        var sent = await SendAsync(chat, AskForWeather, [_weatherTool with { RequireApproval = false }]);

        var pending = Pending(sent);
        AutoAllowOf(pending).Should().BeTrue("no dialog for this one");
        ShouldBeAnAnnouncement(IntentOf(PendingPart(pending)), "on an auto-allowed call");
    }
}
