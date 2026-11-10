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
/// Always-allow drives the pause. <c>autoAllow</c> on the <c>tool-call-pending</c> frame is the decision the UI acts on
/// — false means "show the approve dialog". It is computed per request from the persisted allow-always list, so it is
/// the observable side effect of <c>set-allow-always</c>. The dialog's own checkbox rides along with the decision as
/// <c>allowAlways</c> on <c>approve-tool-call</c>: a second write path to the same state.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Mcp")]
public class McpAllowAlwaysPauseTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    private static readonly HostTool[] _tools = [_weatherTool];

    private static bool HoldsGrantFor(IEnumerable<string> list, string toolName) =>
        list.Any(name => name == toolName || name.EndsWith($"_{toolName}", StringComparison.Ordinal));

    /// <summary>A fresh thread, one scripted call, and the pause it ends on.</summary>
    private async Task<JsonElement> AskAsync(McpChat chat, string title, IEnumerable<HostTool>? tools = null, string? threadId = null)
    {
        threadId ??= await NewThreadAsync(chat, title);
        chat.Provider.Enqueue(WeatherCall());

        return Pending(await SendAsync(chat, AskForWeather, tools ?? _tools, threadId), $"({title})");
    }

    /// <summary>
    /// The settings route files the grant as "host_get_weather". Until the bug was fixed the engine looked a client
    /// tool up under the bare name the model called it by, so the dialog still came up.
    /// </summary>
    [Fact]
    [Trait("Bug", "84292")]
    public async Task SetAllowAlways_AListedToolComesBackAutoAllow_AndDelistingItPromptsAgain()
    {
        var chat = await StartChatAsync();
        AutoAllowOf(await AskAsync(chat, "before")).Should().BeFalse("before allow-always");

        await SetAllowAlwaysAsync(HostServer, _weatherTool.Name);
        (await IsAllowedAsync(HostServer, _weatherTool.Name)).Should().BeTrue();
        AutoAllowOf(await AskAsync(chat, "after")).Should().BeTrue("after allow-always");

        await SetAllowAlwaysAsync(HostServer, _weatherTool.Name, false);
        AutoAllowOf(await AskAsync(chat, "revoked")).Should().BeFalse("after revoking it");
    }

    [Fact]
    public async Task Send_AToolThatOptsOutOfApproval_IsAutoAllowedWithoutAnyStoredSetting()
    {
        var chat = await StartChatAsync();

        var pending = await AskAsync(chat, "opted out", [_weatherTool with { RequireApproval = false }], chat.ThreadId);

        // The tool itself opted in, so no dialog — and nothing was persisted to make that happen.
        AutoAllowOf(pending).Should().BeTrue();
        (await AllowedAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Approve_AllowWithAllowAlways_PreApprovesTheToolForTheNextCall()
    {
        var chat = await StartChatAsync();
        var pending = await AskAsync(chat, "first", threadId: chat.ThreadId);
        chat.Provider.Enqueue(StubModelProvider.Text("It is 21C and sunny."));
        AutoAllowOf(pending).Should().BeFalse("the first call prompts");

        // Queued behind the call already waiting: the resume takes it.
        var approved = await ApproveAsync(chat, pending, "21C and sunny", allowAlways: true, tools: _tools);

        approved.StatusCode.Should().Be(HttpStatusCode.OK, approved.ToString());
        FrameTypes(approved).Should().Contain("message-end", "the decision still resumes the reply");

        // Persisted where the settings route keeps it, or the box was decoration.
        var stored = await AllowedAsync();
        HoldsGrantFor(stored, _weatherTool.Name).Should().BeTrue($"the pre-approval list holds {string.Join(", ", stored)}");

        // The requirement itself: next time the model reaches for the tool there is no dialog left. A fresh thread, so
        // nothing but the stored decision can carry it over.
        var again = await AskAsync(chat, "after always-allow");
        AutoAllowOf(again).Should().BeTrue("the tool was pre-approved from the dialog");
    }

    /// <summary>
    /// The mirror of the settings-route test: the dialog files the grant under the server-prefixed key
    /// ("host_get_weather"); the read only matched that spelling, so the settings screen showed the tool as not
    /// pre-approved while it demonstrably was.
    /// </summary>
    [Fact]
    [Trait("Bug", "84293")]
    public async Task IsAllowAlways_AClientToolPreApprovedThroughTheDialog_IsFoundUnderItsOwnKey()
    {
        var chat = await StartChatAsync();
        var pending = await AskAsync(chat, "dialog", threadId: chat.ThreadId);
        chat.Provider.Enqueue(StubModelProvider.Text("It is 21C and sunny."));
        (await ApproveAsync(chat, pending, "21C and sunny", allowAlways: true, tools: _tools)).StatusCode.Should().Be(HttpStatusCode.OK);

        var stored = await AllowedAsync();
        HoldsGrantFor(stored, _weatherTool.Name).Should().BeTrue($"the pre-approval list holds {string.Join(", ", stored)}");
        (await IsAllowedAsync(HostServer, _weatherTool.Name)).Should().BeTrue($"is-allow-always(host, {_weatherTool.Name}) - the list holds {string.Join(", ", stored)}");
    }

    /// <summary>The control for the dialog test: without it, an engine that pre-approves on every Allow would pass that one.</summary>
    [Fact]
    public async Task Approve_AOneTimeAllow_PreApprovesNothing()
    {
        var chat = await StartChatAsync();
        var pending = await AskAsync(chat, "once", threadId: chat.ThreadId);
        chat.Provider.Enqueue(StubModelProvider.Text("It is 21C and sunny."));

        var approved = await ApproveAsync(chat, pending, "21C and sunny", tools: _tools);

        approved.StatusCode.Should().Be(HttpStatusCode.OK, approved.ToString());
        (await AllowedAsync()).Should().BeEmpty();

        var again = await AskAsync(chat, "after a one-time allow");
        AutoAllowOf(again).Should().BeFalse("one Allow is one Allow - the dialog comes back");
    }
}
