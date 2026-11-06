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
/// How the user's tool permission mode steers the approval pause. Seen on <c>autoAllow</c> of the
/// <c>tool-call-pending</c> frame: <c>ask</c> — every call waits; <c>auto</c> — the always-allow list and a tool that
/// opted out of approval (<c>requireApproval: false</c>) are let through, a missing flag asks; <c>allow</c> — nothing
/// waits. A registered server's tool is executed by the engine and follows the same mode.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Mcp")]
public class McpToolPermissionModeChatTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    private static readonly HostTool _requiresApproval = _weatherTool with { RequireApproval = true };
    private static readonly HostTool _optedOut = _weatherTool with { RequireApproval = false };
    private static readonly HostTool _flagUnset = _weatherTool with { RequireApproval = null };

    private static bool HoldsGrantFor(IEnumerable<string> list, string toolName) =>
        list.Any(name => name == toolName || name.EndsWith($"_{toolName}", StringComparison.Ordinal));

    /// <summary>One tool call in a thread of its own (or an existing one); returns the pause it ended on.</summary>
    private async Task<(JsonElement Pending, string ThreadId)> CallToolAsync(McpChat chat, HostTool tool, string label, string? existingThreadId = null)
    {
        var threadId = existingThreadId ?? await NewThreadAsync(chat, label);
        chat.Provider.Enqueue(WeatherCall());

        var sent = await SendAsync(chat, AskForWeather, [tool], threadId);

        var pending = Pending(sent, $"({label})");
        var frames = FrameTypes(sent);
        frames[^1].Should().Be("tool-call-pending", $"{label}: the pause is the last frame");
        frames.Should().NotContain("message-end", $"{label}: nothing resumed by itself");

        return (pending, threadId);
    }

    private async Task<bool> AutoAllowAsync(McpChat chat, HostTool tool, string label, string? existingThreadId = null) =>
        AutoAllowOf((await CallToolAsync(chat, tool, label, existingThreadId)).Pending);

    /// <summary>The user's decision, with the dialog's "always allow" box when asked for.</summary>
    private async Task AllowAlwaysFromDialogAsync(McpChat chat, HostTool tool, string label)
    {
        var (pending, threadId) = await CallToolAsync(chat, tool, label);
        chat.Provider.Enqueue(StubModelProvider.Text("It is 21C and sunny."));

        var approved = await ApproveAsync(chat, pending, "21C and sunny", allowAlways: true, tools: [tool], threadId: threadId);

        approved.StatusCode.Should().Be(HttpStatusCode.OK, $"{label}: approve");
        FrameTypes(approved).Should().Contain("message-end", $"{label}: the approval resumes the reply");
        HoldsGrantFor(await AllowedAsync(), _weatherTool.Name).Should().BeTrue("the grant is stored");
    }

    /// <summary>
    /// In <c>ask</c> every host tool waits, whatever its own flag says. The last step — a saved allow-always wins even
    /// here — is split out as a test of its own.
    /// </summary>
    [Fact]
    [Trait("Bug", "84298")]
    public async Task Ask_EveryHostToolWaitsForApproval_WhateverItsOwnFlagSays()
    {
        var chat = await StartChatAsync();
        await SetToolModeAsync(AiChatToolPermissionMode.Ask);
        (await AllowedAsync()).Should().BeEmpty("nothing is pre-approved");

        (await AutoAllowAsync(chat, _optedOut, "ask, requireApproval false")).Should().BeFalse("requireApproval: false still asks");
        (await AutoAllowAsync(chat, _flagUnset, "ask, flag unset")).Should().BeFalse("no requireApproval asks");
        (await AutoAllowAsync(chat, _requiresApproval, "ask, requireApproval true")).Should().BeFalse();

        await AllowAlwaysFromDialogAsync(chat, _requiresApproval, "ask, allow-always from the dialog");
    }

    /// <summary>
    /// What the TypeScript suite recorded as an open failure: with the grant saved, the tool is let through in ask.
    /// (The chat library's own contract for <c>ask</c> says the list is ignored — "the user asked to be asked" — so the
    /// expectation here is the TS suite's claim; see the report.)
    /// </summary>
    [Fact]
    [Trait("Bug", "84298")]
    public async Task Ask_ASavedAllowAlways_Wins()
    {
        var chat = await StartChatAsync();
        await SetToolModeAsync(AiChatToolPermissionMode.Ask);
        await SetAllowAlwaysAsync(HostServer, _weatherTool.Name);

        (await AutoAllowAsync(chat, _requiresApproval, "ask, allow-always saved")).Should().BeTrue();
    }

    [Fact]
    public async Task Auto_AHostToolFollowsItsOwnRequireApproval_AndAMissingFlagAsks()
    {
        var chat = await StartChatAsync();
        await SetToolModeAsync(AiChatToolPermissionMode.Auto);
        (await AllowedAsync()).Should().BeEmpty("nothing is pre-approved");

        (await AutoAllowAsync(chat, _optedOut, "auto, requireApproval false")).Should().BeTrue("requireApproval: false is let through");
        (await AutoAllowAsync(chat, _flagUnset, "auto, flag unset")).Should().BeFalse("no requireApproval asks");
        (await AutoAllowAsync(chat, _requiresApproval, "auto, requireApproval true")).Should().BeFalse();

        await AllowAlwaysFromDialogAsync(chat, _requiresApproval, "auto, allow-always from the dialog");

        (await AutoAllowAsync(chat, _requiresApproval, "auto, allow-always saved")).Should().BeTrue("a saved allow-always wins in auto");
    }

    [Fact]
    public async Task Allow_NothingWaits_WhateverTheToolsRequireApprovalSays()
    {
        var chat = await StartChatAsync();
        await SetToolModeAsync(AiChatToolPermissionMode.Allow);
        (await AllowedAsync()).Should().BeEmpty("nothing is pre-approved");

        (await AutoAllowAsync(chat, _requiresApproval, "allow, requireApproval true")).Should().BeTrue();
        (await AutoAllowAsync(chat, _optedOut, "allow, requireApproval false")).Should().BeTrue();
        (await AutoAllowAsync(chat, _flagUnset, "allow, flag unset")).Should().BeTrue();

        await AllowAlwaysFromDialogAsync(chat, _requiresApproval, "allow, allow-always from the dialog");

        (await AutoAllowAsync(chat, _requiresApproval, "allow, allow-always saved")).Should().BeTrue("an allow-always saved on top changes nothing");
    }

    [Fact]
    public async Task Mode_AChangeAppliesToBothExistingAndNewlyCreatedThreads()
    {
        var chat = await StartChatAsync();
        var before = await NewThreadAsync(chat, "Autotest thread created before the mode change");

        await SetToolModeAsync(AiChatToolPermissionMode.Allow);

        (await AutoAllowAsync(chat, _requiresApproval, "existing thread, allow", before)).Should().BeTrue("the thread that already existed follows the new mode");
        var after = await NewThreadAsync(chat, "Autotest thread created after the mode change");
        (await AutoAllowAsync(chat, _requiresApproval, "new thread, allow", after)).Should().BeTrue("a thread created afterwards follows it too");
    }

    [Fact]
    public async Task Mode_ANewModeAppliesToTheNextToolCall()
    {
        var chat = await StartChatAsync();
        (await AllowedAsync()).Should().BeEmpty("nothing is pre-approved");

        var steps = new (AiChatToolPermissionMode Mode, HostTool Tool, string Flag, bool AutoAllow)[]
        {
            (AiChatToolPermissionMode.Ask, _optedOut, "false", false),
            (AiChatToolPermissionMode.Auto, _optedOut, "false", true),
            (AiChatToolPermissionMode.Auto, _requiresApproval, "true", false),
            (AiChatToolPermissionMode.Allow, _requiresApproval, "true", true),
            (AiChatToolPermissionMode.Ask, _requiresApproval, "true", false)
        };

        foreach (var (index, step) in steps.Index())
        {
            var label = $"{index + 1}. {step.Mode}, requireApproval {step.Flag}";
            await SetToolModeAsync(step.Mode);

            (await AutoAllowAsync(chat, step.Tool, label)).Should().Be(step.AutoAllow, label);
        }

        (await AllowedAsync()).Should().BeEmpty("no allow-always masked a step");
    }

    /// <summary>
    /// A registered server's tool is executed by the engine (no client result), so it pauses with
    /// <c>serverExecuted</c> and is held back by <c>ask</c> and <c>auto</c> (an MCP tool with no annotations is
    /// destructive by default); <c>allow</c> runs it unasked.
    /// </summary>
    [Theory]
    [InlineData(AiChatToolPermissionMode.Ask)]
    [InlineData(AiChatToolPermissionMode.Auto)]
    [InlineData(AiChatToolPermissionMode.Allow)]
    public async Task CustomServerTool_WaitsInAskAndAuto_AndRunsUnaskedInAllow(AiChatToolPermissionMode mode)
    {
        using var mcp = new StubMcpServer();
        var chat = await StartChatAsync();
        await RegisterAsync("calculator", new Dictionary<string, object> { ["url"] = mcp.Url });
        await SetToolModeAsync(mode);
        ScriptToolOnOffer(chat.Provider, StubMcpServer.CalculateTool, """{"operation":"multiply","a":8231,"b":7,"aiChatIntent":"Multiplying"}""", "The result is 57617.");

        var sent = await SendAsync(chat, "Multiply 8231 by 7 with the calculate tool.");

        sent.StatusCode.Should().Be(HttpStatusCode.OK, sent.ToString());
        if (mode == AiChatToolPermissionMode.Allow)
        {
            PendingOf(sent).Should().BeNull($"the engine stopped for approval; frames were {string.Join(", ", FrameTypes(sent))}");
            FrameTypes(sent).Should().Contain("message-end", "the reply finished");
            mcp.CallsOf(StubMcpServer.CalculateTool).Should().Be(1, "the engine ran the tool without asking\n" + chat.Provider.Describe());
            ResultOf(ToolCallParts(await OnlyReplyAsync(chat))[0]).Should().Contain("57617");

            return;
        }

        var pending = Pending(sent, "for a tool that needs approval");
        ServerExecutedOf(pending).Should().BeTrue("serverExecuted");
        AutoAllowOf(pending).Should().BeFalse("autoAllow");
        FrameTypes(sent)[^1].Should().Be("tool-call-pending", "the pause ends the stream");
        FrameTypes(sent).Should().NotContain("message-end", "nothing resumed by itself");
        mcp.CallsOf(StubMcpServer.CalculateTool).Should().Be(0, "the tool has not run yet");
        ResultOf(ToolCallParts(pending.GetProperty("message"))[0]).Should().BeNull();

        // The user's "Allow" on a server-executed pause carries no result: the engine runs the tool.
        var approved = await ApproveAsync(chat, pending);

        approved.StatusCode.Should().Be(HttpStatusCode.OK, approved.ToString());
        FrameTypes(approved).Should().Contain("message-end", "the reply resumes");
        mcp.CallsOf(StubMcpServer.CalculateTool).Should().Be(1, "the tool ran once allowed");
        ResultOf(ToolCallParts(await OnlyReplyAsync(chat))[0]).Should().Contain("57617");
    }
}
