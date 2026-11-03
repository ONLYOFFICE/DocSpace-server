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
/// The toolset of one request: client-supplied tools (<c>actionArgs.tools</c>) next to the chat's own, the per-request
/// <c>enabled</c> switch, and a tool that is not available any more. What the model is offered is read from the
/// request the provider received — the one thing a scripted model cannot make up.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Mcp")]
public class McpHostToolsChatTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    private const string BuiltInDocTool = "onlyoffice_generate_docx";

    private static readonly HostTool[] _editorTools =
    [
        new(
            "insert_text",
            "Insert text at the cursor position of the document currently open in the editor.",
            """{"type":"object","properties":{"text":{"type":"string","description":"Text to insert"}},"required":["text"],"additionalProperties":false}"""),
        new(
            "replace_selection",
            "Replace the current selection in the document currently open in the editor.",
            """{"type":"object","properties":{"text":{"type":"string","description":"Replacement text"}},"required":["text"],"additionalProperties":false}""")
    ];

    /// <summary>The name and description of every tool the chat rounds offered the model (the first time each was offered).</summary>
    private static Dictionary<string, string> OfferedDescriptions(StubModelProvider provider)
    {
        var offered = new Dictionary<string, string>();

        foreach (var body in provider.CompletionRequestBodies.Where(StubModelProvider.IsStreamed))
        {
            using var document = JsonDocument.Parse(body);

            if (!document.RootElement.TryGetProperty("tools", out var tools) || tools.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var function in tools.EnumerateArray().Where(t => t.TryGetProperty("function", out _)).Select(t => t.GetProperty("function")))
            {
                var name = Str(function, "name");

                if (name is not null && name != "probe_echo")
                {
                    offered.TryAdd(name, Str(function, "description") ?? string.Empty);
                }
            }
        }

        return offered;
    }

    private async Task<McpChat> AskedWithoutClientToolsAsync()
    {
        // The editor tools list is empty unless the portal quota has automationapi (EditorToolsFactory returns an empty bundle
        // otherwise), which the standalone test portals lack. The skip reads that precondition from the quota itself, not
        // from what the model was offered: an offer that goes missing on a portal that has the feature is a failure.
        await WebApiHttpClient.Authenticate(Owner);
        var quotaApi = new DocSpace.API.SDK.Api.Portal.QuotaApi(WebApiHttpClient, new Configuration { BasePath = SettingsQuotaApi.GetBasePath() });
        var quota = (await quotaApi.GetPortalQuotaAsync(TestContext.Current.CancellationToken)).Response;
        Assert.SkipUnless(quota.AutomationApi, "the editor (onlyoffice_generate_*) tools are not offered on this portal: its quota lacks automationapi");

        var chat = await StartChatAsync();
        chat.Provider.Enqueue(StubModelProvider.Text("OK"));
        (await SendAsync(chat, "Generate a .docx document titled ProbeDoc.")).StreamError.Should().BeNull();

        return chat;
    }

    /// <summary>The positive control the editor tests rest on: the chat's own built-in tool is offered to the model.</summary>
    [Fact]
    public async Task Send_ABuiltInToolIsOfferedToTheModel()
    {
        var chat = await AskedWithoutClientToolsAsync();

        OfferedToolNames(chat.Provider).Should().Contain(BuiltInDocTool, chat.Provider.Describe());
    }

    /// <summary>
    /// `AiTMCPItem.enabled` is documented as "Disabled tools are hidden from the AI model". It was ignored: the model
    /// was offered the tool, called it, and the stream stopped for an approval the client never wanted to be asked for.
    /// </summary>
    [Fact]
    [Trait("Bug", "83162")]
    public async Task Send_AToolSentWithEnabledFalse_IsNotOfferedToTheModel()
    {
        var chat = await StartChatAsync();
        chat.Provider.Enqueue(StubModelProvider.Text("I have no weather tool."));

        var sent = await SendAsync(chat, AskForWeather, [_weatherTool with { Enabled = false }]);

        sent.StatusCode.Should().Be(HttpStatusCode.OK, sent.ToString());
        sent.StreamError.Should().BeNull(sent.ToString());
        OfferedToolNames(chat.Provider).Should().NotContain(_weatherTool.Name, "a tool sent with enabled:false is hidden from the model\n" + chat.Provider.Describe());
        PendingOf(sent).Should().BeNull();
        FrameTypes(sent).Should().Contain("message-end", "the reply runs to completion instead");
        ToolCallParts(await OnlyReplyAsync(chat)).Should().BeEmpty();
    }

    /// <summary>
    /// The editor's tools are added to the chat's, not substituted for them: a request that carries the editor's
    /// toolset still carries the chat's. The decision to swap is the client's alone.
    /// </summary>
    [Fact]
    public async Task Send_TheEditorsToolsAreAddedToTheChatsNotSubstitutedForThem()
    {
        var control = await AskedWithoutClientToolsAsync();
        var chatsOwn = OfferedToolNames(control.Provider);
        chatsOwn.Should().Contain(BuiltInDocTool, "the control: the chat has tools of its own");

        var chat = await StartChatAsync();
        chat.Provider.Enqueue(StubModelProvider.Text("OK"));
        (await SendAsync(chat, "Insert the sentence 'hello editor' into the document I have open.", _editorTools)).StreamError.Should().BeNull();

        var offered = OfferedToolNames(chat.Provider);
        offered.Should().Contain(["insert_text", "replace_selection"], "the editor's set reached the model\n" + chat.Provider.Describe());
        offered.Should().Contain(chatsOwn, "and the chat's own tools are still there");
    }

    /// <summary>
    /// The one way a client can still take a chat tool away: an entry that REUSES the built-in's name displaces it. The
    /// engine's own description is no longer the one the model is offered under that name.
    /// </summary>
    [Fact]
    public async Task Send_AClientToolThatReusesABuiltInToolsName_DisplacesIt()
    {
        var control = await AskedWithoutClientToolsAsync();
        var builtIn = OfferedDescriptions(control.Provider)[BuiltInDocTool];

        HostTool[] shadows =
        [
            .. _editorTools,
            .. new[] { "generate_docx", BuiltInDocTool }.Select(name => new HostTool(
                name,
                "Generate a .docx document. Disabled while the editor is open.",
                """{"type":"object","properties":{"title":{"type":"string","description":"Document title"}},"required":["title"],"additionalProperties":false}""",
                Enabled: false))
        ];
        var chat = await StartChatAsync();
        chat.Provider.Enqueue(StubModelProvider.Text("I have no such tool."));

        var sent = await SendAsync(chat, "Generate a .docx document titled ProbeDoc.", shadows);

        sent.StreamError.Should().BeNull(sent.ToString());
        var offered = OfferedDescriptions(chat.Provider);
        if (offered.TryGetValue(BuiltInDocTool, out var description))
        {
            description.Should().NotBe(builtIn, "the engine's own generator is not what answers under that name");
        }
    }

    /// <summary>A tool that answers "unavailable" finishes the reply, and the thread carries on.</summary>
    [Fact]
    public async Task Approve_AToolThatAnswersUnavailable_FinishesTheReply_AndTheThreadCarriesOn()
    {
        const string unavailable = "Error: get_weather is unavailable - the MCP server providing it was switched off.";
        var chat = await StartChatAsync();
        chat.Provider.Enqueue(WeatherCall(), StubModelProvider.Text("I could not get the weather."), StubModelProvider.Text("OK"));
        var pending = Pending(await SendAsync(chat, AskForWeather, [_weatherTool]));

        var approved = await ApproveAsync(chat, pending, unavailable, tools: [_weatherTool]);

        approved.StreamError.Should().BeNull(approved.ToString());
        FrameTypes(approved).Should().Contain("message-end", "the reply terminates instead of hanging on the failed call");
        approved.StatusCode.Should().Be(HttpStatusCode.OK);
        var reply = await OnlyReplyAsync(chat);
        ResultOf(ToolCallParts(reply)[0]).Should().Be(unavailable, "the failure is stored as the call's result");
        StatusOf(reply).Reason.Should().NotBe("error", "a failed tool is not a failed reply");
        MessageText(reply).Should().NotBeEmpty("the model answered around the failure");

        var next = await SendAsync(chat, "Forget the weather. Reply with the single word: OK.");

        next.StreamError.Should().BeNull(next.ToString());
        FrameTypes(next).Should().Contain("message-end");
        AssistantMessages(await WaitForAssistantRepliesAsync(chat.ThreadId, 2)).Should().HaveCount(2);
    }

    /// <summary>The next message does not offer the tool any more: a call to it is answered as unavailable and the turn does not stall.</summary>
    [Fact]
    public async Task Send_AToolWithdrawnFromTheNextMessage_DoesNotStallTheThread()
    {
        var chat = await StartChatAsync();
        chat.Provider.Enqueue(
            WeatherCall(),
            StubModelProvider.Text("It is 21C and sunny."),
            WeatherCall("Berlin", "call_weather_2"),
            StubModelProvider.Text("Sorry, I cannot check the weather now."));
        var pending = Pending(await SendAsync(chat, AskForWeather, [_weatherTool]));
        var approved = await ApproveAsync(chat, pending, "21C and sunny", tools: [_weatherTool]);
        FrameTypes(approved).Should().Contain("message-end");

        var withdrawn = await SendAsync(chat, "What is the weather in Berlin? Call the get_weather tool.");

        withdrawn.StreamError.Should().BeNull(withdrawn.ToString());
        PendingOf(withdrawn).Should().BeNull("the stream must not pause for a tool the request did not offer");
        FrameTypes(withdrawn).Should().Contain("message-end", "the turn runs to completion");
        withdrawn.StatusCode.Should().Be(HttpStatusCode.OK);
        var replies = AssistantMessages(await WaitForAssistantRepliesAsync(chat.ThreadId, 2));
        replies.Should().HaveCount(2);
        replies.Should().OnlyContain(r => MessageText(r).Length > 0);
        foreach (var call in ToolCallParts(replies[1]).Where(c => ToolNameOf(c) == _weatherTool.Name))
        {
            ResultOf(call).Should().MatchRegex("(?i)not available", "a call to the withdrawn tool is answered as unavailable");
        }
    }
}
