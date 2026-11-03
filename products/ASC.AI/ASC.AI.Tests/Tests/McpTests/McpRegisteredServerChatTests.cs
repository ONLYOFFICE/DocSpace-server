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
/// A registered MCP server and the conversation: what the model is offered from a server in the registry, how its
/// tools are approved, and that a dead or vanished server never costs a turn. The server is a live
/// <see cref="StubMcpServer"/>; the TypeScript suite used a hosted calculator and skipped when it did not answer.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Mcp")]
public class McpRegisteredServerChatTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    private const string CalculatorArguments = """{"operation":"multiply","a":8231,"b":7,"aiChatIntent":"Multiplying"}""";
    private const string AskCalculator = "Call the tool named calculate (it belongs to the \"calculator\" MCP server) with the operation multiply, a=8231 and b=7, then report the result.";

    private static readonly Dictionary<string, object> _deadServer = new() { ["type"] = "http", ["url"] = "http://127.0.0.1:1/mcp" };

    /// <summary>Registers the live server (for the chat's room, or portal-wide) and drives the calculator call to its end.</summary>
    private async Task AssertTheServersToolsReachTheModelAsync(bool scopedToRoom)
    {
        using var mcp = new StubMcpServer();
        var chat = await StartChatAsync();
        var config = new Dictionary<string, object> { ["url"] = mcp.Url };
        await RegisterAsync("calculator", config, scopedToRoom ? chat.RoomId.ToString() : null);
        ScriptToolOnOffer(chat.Provider, StubMcpServer.CalculateTool, CalculatorArguments, "The result is 57617.");

        var sent = await SendAsync(chat, AskCalculator);

        sent.StatusCode.Should().Be(HttpStatusCode.OK, sent.ToString());
        sent.StreamError.Should().BeNull(sent.ToString());
        OfferedToolNames(chat.Provider).Should().Contain(t => t.EndsWith(StubMcpServer.CalculateTool, StringComparison.Ordinal),
            "the agent is offered the tools of its registered server\n" + chat.Provider.Describe());
        var pending = Pending(sent, "for the registered server's tool");
        ServerExecutedOf(pending).Should().BeTrue("the engine runs a registered server's tool itself");

        var approved = await ApproveAsync(chat, pending);

        FrameTypes(approved).Should().Contain("message-end");
        mcp.CallsOf(StubMcpServer.CalculateTool).Should().Be(1);
        var reply = await OnlyReplyAsync(chat);
        ResultOf(ToolCallParts(reply)[0]).Should().Contain("57617", "the server's answer is the tool call's result");
        MessageText(reply).Should().Contain("57617");
    }

    /// <summary>A registered server's tools never reached the model: it answered that it had no such tool.</summary>
    [Fact]
    [Trait("Bug", "82989")]
    public Task Send_ARegisteredServersToolsReachTheModel() => AssertTheServersToolsReachTheModelAsync(true);

    /// <summary>Rules out the per-agent explanation of the test above: registered with no entity at all it applies to every chat.</summary>
    [Fact]
    [Trait("Bug", "82990")]
    public async Task Send_APortalWideServersToolsReachTheModelToo()
    {
        await AssertTheServersToolsReachTheModelAsync(false);
    }

    /// <summary>
    /// A dead endpoint sitting in the registry costs the next ordinary message nothing — no stall, no stream error,
    /// no failed reply. (The test above shows a working server is consulted; this one only says a dead one is harmless.)
    /// </summary>
    [Fact]
    public async Task Send_AnUnreachableRegisteredServer_LeavesAnOrdinaryReplyUnaffected()
    {
        var chat = await StartChatAsync();
        await RegisterAsync("autotest-dead-server", _deadServer);
        chat.Provider.Enqueue(StubModelProvider.Text("pong"));

        var sent = await SendAsync(chat, "Reply with the single word: pong");

        sent.StatusCode.Should().Be(HttpStatusCode.OK, sent.ToString());
        sent.StreamError.Should().BeNull(sent.ToString());
        FrameTypes(sent).Should().Contain("message-end");
        AssertHealthyReply(await OnlyReplyAsync(chat), "the dead server did not hurt the turn");
    }

    /// <summary>
    /// The dialog's checkbox on a registered server's own tool. The model calls the tool by the bare name the server
    /// advertises, the checkbox persists it, and the settings screen reads it as (serverType: the server, toolName):
    /// the read must find what the dialog wrote.
    /// </summary>
    [Fact]
    [Trait("Bug", "84009")]
    public async Task IsAllowAlways_ACustomServersToolPreApprovedThroughTheDialog_IsFoundUnderItsOwnKey()
    {
        using var mcp = new StubMcpServer();
        var chat = await StartChatAsync();
        await RegisterAsync("calculator", new Dictionary<string, object> { ["url"] = mcp.Url });
        ScriptToolOnOffer(chat.Provider, StubMcpServer.GetTimeTool, "{}", "It is midnight.");
        var sent = await SendAsync(chat, "Call the tool named get_time (it belongs to the \"calculator\" MCP server) to get the current time.");
        var pending = Pending(sent, "for the registered server's tool");
        var toolName = ToolNameOf(PendingPart(pending))!;

        var approved = await ApproveAsync(chat, pending, allowAlways: true);

        approved.StatusCode.Should().Be(HttpStatusCode.OK, approved.ToString());
        // The checkbox's own write landed somewhere, or there is nothing left to look up at all.
        var stored = await AllowedAsync();
        stored.Where(name => name == toolName || name.EndsWith($"_{StubMcpServer.GetTimeTool}", StringComparison.Ordinal)).Should().NotBeEmpty($"the pre-approval list holds {string.Join(", ", stored)}");
        (await IsAllowedAsync("calculator", StubMcpServer.GetTimeTool)).Should().BeTrue(
            $"is-allow-always(serverType=calculator, toolName={StubMcpServer.GetTimeTool}) - the pre-approval list holds {string.Join(", ", stored)}");
    }

    [Fact]
    public async Task Send_AServerRemovedBetweenTwoMessagesOfOneThread_CostsTheNextOneNothing()
    {
        var chat = await StartChatAsync();
        await RegisterAsync("autotest-vanishing-server", _deadServer);
        chat.Provider.Enqueue(StubModelProvider.Text("pong"), StubModelProvider.Text("OK"));
        var before = await SendAsync(chat, "Reply with the single word: pong");
        before.StreamError.Should().BeNull(before.ToString());
        AssertHealthyReply(await OnlyReplyAsync(chat), "before the removal");

        (await RemoveServerAsync("autotest-vanishing-server")).Success.Should().BeTrue();
        (await ListServersAsync()).Should().BeEmpty();
        var after = await SendAsync(chat, "Forget the weather. Reply with the single word: OK.");

        after.StreamError.Should().BeNull(after.ToString());
        after.StatusCode.Should().Be(HttpStatusCode.OK);
        FrameTypes(after).Should().Contain("message-end", "the thread answers with the server gone");
        var replies = AssistantMessages(await WaitForAssistantRepliesAsync(chat.ThreadId, 2));
        replies.Should().HaveCount(2);
        replies.Should().OnlyContain(r => MessageText(r).Length > 0);
    }

    /// <summary>A pause is decided after the scope was rewritten under it: the decision still resumes.</summary>
    [Fact]
    public async Task Approve_APauseDecidedAfterTheServerLeftTheRegistry_StillResumes()
    {
        var chat = await StartChatAsync();
        await RegisterAsync("autotest-removed-under-pause", _deadServer);
        chat.Provider.Enqueue(WeatherCall(), StubModelProvider.Text("It is 21C and sunny."));
        var pending = Pending(await SendAsync(chat, AskForWeather, [_weatherTool]));

        (await RemoveServerAsync("autotest-removed-under-pause")).Success.Should().BeTrue();
        (await ListServersAsync()).Should().BeEmpty("the registry really was rewritten under the open pause");
        var approved = await ApproveAsync(chat, pending, "21C and sunny", tools: [_weatherTool]);

        approved.StreamError.Should().BeNull(approved.ToString());
        FrameTypes(approved).Should().Contain("message-end", "the decision still resumes the reply");
        approved.StatusCode.Should().Be(HttpStatusCode.OK);
        var reply = await OnlyReplyAsync(chat);
        ResultOf(ToolCallParts(reply)[0]).Should().Be("21C and sunny", "the result of the decision landed on the call");
        MessageText(reply).Should().NotBeEmpty();
    }

    /// <summary>An unreachable server carries no status or log field: a registration reads back as its config and nothing else.</summary>
    [Fact]
    public async Task Get_AnUnreachableServerCarriesNoStatusOrLogField()
    {
        var chat = await StartChatAsync();
        await RegisterAsync("autotest-failed-server", _deadServer);
        ShouldBeConfig(await GetServerAsync("autotest-failed-server"), _deadServer, "a freshly registered server reads back as its config and nothing else");
        chat.Provider.Enqueue(StubModelProvider.Text("pong"));

        var sent = await SendAsync(chat, "Reply with the single word: pong");

        sent.StreamError.Should().BeNull(sent.ToString());
        AssertHealthyReply(await OnlyReplyAsync(chat), "a turn ran with the server in the registry");
        ShouldBeConfig(await GetServerAsync("autotest-failed-server"), _deadServer, "and it still reads back as its config after a turn ran with it in scope");
        ShouldHold(await ListServersAsync(), new Dictionary<string, object> { ["autotest-failed-server"] = _deadServer }, "the listing carries no per-server state either");
    }
}
