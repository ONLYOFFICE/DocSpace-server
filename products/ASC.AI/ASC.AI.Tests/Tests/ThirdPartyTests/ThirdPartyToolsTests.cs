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

namespace ASC.AI.Tests.Tests.ThirdPartyTests;

/// <summary>
/// MCP tool scoping and client tools in a third-party room (thirdparty.spec.ts, "MCP tool scoping"). Needs a Nextcloud
/// account; see <see cref="ThirdPartyTestBase"/>.
/// </summary>
/// <remarks>
/// The custom MCP server registry is one map per portal: a supplied <c>entityId</c> must name a reachable room
/// (Bug 82975) but is otherwise ignored (Bug 82863). The two TypeScript tests that expected a server scoped to a room to
/// be hidden from the portal-wide list and from a sibling room were written against the opposite reading and marked
/// <c>test.fail</c>; the chat service documents the portal-wide registry as the design, so they assert it here.
/// </remarks>
[Trait("Category", "ThirdParty")]
[Trait("Requires", "Nextcloud")]
[Trait("Feature", "AI/ThirdParty")]
public class ThirdPartyToolsTests(AspireAppFixture fixture) : ThirdPartyTestBase(fixture)
{
    [Fact]
    public async Task CustomServer_ScopedToAThirdPartyRoom_IsRegisteredAndReadableByName()
    {
        var roomId = await CreateNextcloudRoomAsync("TP MCP Scope Room");

        await AddCustomServerAsync("tp-room-server", entityId: roomId);

        var forRoom = await ListCustomServersForAsync(roomId);
        var single = await _aiTools.AiToolsGetCustomServerAsync("tp-room-server", roomId, TestContext.Current.CancellationToken);

        forRoom.Should().ContainKey("tp-room-server");
        ConfigEquals(forRoom["tp-room-server"]).Should().BeTrue();
        ConfigEquals(single).Should().BeTrue();
    }

    [Fact]
    [Trait("Bug", "82975")]
    public async Task CustomServer_ScopedToAThirdPartyRoom_IsOnThePortalWideRegistry()
    {
        var roomId = await CreateNextcloudRoomAsync("TP MCP Portal Room");
        await AddCustomServerAsync("tp-room-portal-server", entityId: roomId);

        var portalWide = await ListCustomServersAsync();

        portalWide.Should().ContainKey("tp-room-portal-server", "the registry is one map per portal; the room only has to be reachable");
    }

    [Fact]
    [Trait("Bug", "82975")]
    public async Task CustomServer_ScopedToAThirdPartyRoom_IsVisibleFromAnUnrelatedInternalRoom()
    {
        var roomId = await CreateNextcloudRoomAsync("TP MCP Leak Room");
        var internalRoomId = await CreateCustomRoomAsync("MCP Sibling Room");
        await AddCustomServerAsync("tp-room-sibling-server", entityId: roomId);

        var forSibling = await ListCustomServersForAsync(internalRoomId.ToString());

        forSibling.Should().ContainKey("tp-room-sibling-server", "the scope is not a distinct key of the registry");
    }

    [Fact]
    public async Task AddCustomServer_NonAdminMemberOfAThirdPartyRoom_IsRefused()
    {
        var roomId = await CreateNextcloudRoomAsync("TP MCP Perm Room");
        var user = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await ShareThirdPartyRoomAsync(roomId, user.Id, FileShare.Read);

        await _aiChatClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiTools.AiToolsAddCustomServerAsync(
                new AiToolsAddCustomServerRequest(name: "tp-room-should-fail", config: McpConfig(), entityId: roomId),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ClientTool_PausesAndResumes_InAThreadScopedToAThirdPartyRoom()
    {
        var roomId = await CreateNextcloudRoomAsync("TP Tool Pause Room");
        using var provider = new StubModelProvider();
        var profile = await CreateProfileOnAsync(provider);
        var thread = await CreateThreadAsync("Autotest TP tool thread", profile.Id, roomId);

        var weather = new AiMCPItem(
            name: "get_weather",
            description: "Get the current weather for a city.",
            inputSchema: new
            {
                type = "object",
                properties = new { city = new { type = "string", description = "City name" } },
                required = new[] { "city" },
                additionalProperties = false
            },
            enabled: true,
            requireApproval: true);
        var actionArgs = new AiActionArgs(tools: [weather]);

        provider.Enqueue(
            StubModelProvider.ToolCall("get_weather", """{"city":"Paris"}"""),
            StubModelProvider.Text("It is sunny in Paris."));

        var sent = await SendWithStreamAsync(thread.ThreadId, "What is the weather in Paris? Call the get_weather tool.", profile.Id, roomId, actionArgs);

        var context = $"{sent}\n{provider.Describe()}";
        sent.StatusCode.Should().Be(HttpStatusCode.OK, context);
        sent.StreamError.Should().BeNull(context);
        var pending = sent.OfType("tool-call-pending").ToList();
        pending.Should().ContainSingle($"the model asked for the tool; {context}");
        // The response is raw on purpose — an SDK defect: approve-tool-call answers an ndjson stream of chat frames, which
        // the SDK declares as one AiChatEvent. The request is the SDK model; only its `message` is spliced in from the
        // pending frame, because the SDK message model drops the tool-call parts (toolName, args, toolCallId) the engine needs.
        var frame = pending[0];
        var approve = new AiApproveToolCallRequest(
            result: "21C and sunny",
            threadId: thread.ThreadId,
            messageId: frame.GetProperty("messageId").GetString()!,
            idx: frame.TryGetProperty("idx", out var idx) ? idx.GetInt32() : 0,
            actionArgs: actionArgs,
            entityId: roomId,
            profileId: profile.Id);
        var approveBody = Newtonsoft.Json.Linq.JObject.Parse(approve.ToJson());
        approveBody["message"] = Newtonsoft.Json.Linq.JToken.Parse(frame.GetProperty("message").GetRawText());

        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(
            "/api/2.0/ai/ai/approve-tool-call",
            approveBody.ToString(Newtonsoft.Json.Formatting.None),
            TestContext.Current.CancellationToken);
        var resumed = ChatRound.Parse(response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        resumed.StatusCode.Should().Be(HttpStatusCode.OK, $"{resumed}\n{provider.Describe()}");
        resumed.OfType("message-end").Should().NotBeEmpty($"the resumed round finished; {resumed}");

        // The stored tool call carries the result the client supplied. Read raw: the SDK cannot deserialize a stored
        // tool-call block (AiThreadMessageLike has no schema for it).
        using var messages = await new RawApiClient(_aiChatClient).GetAsync(
            $"/api/2.0/ai/threads/read-messages?threadId={Uri.EscapeDataString(thread.ThreadId)}",
            TestContext.Current.CancellationToken);
        var body = await messages.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        messages.IsSuccessStatusCode.Should().BeTrue(body);

        using var document = JsonDocument.Parse(body);
        var results = document.RootElement.EnumerateArray()
            .Where(m => m.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            .SelectMany(m => m.GetProperty("content").EnumerateArray())
            .Where(block => block.TryGetProperty("type", out var type) && type.GetString() == "tool-call" && block.TryGetProperty("result", out _))
            .Select(block => block.GetProperty("result").ToString())
            .ToList();
        results.Should().Contain("21C and sunny", body);
    }
}
