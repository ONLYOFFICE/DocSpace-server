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

namespace ASC.AI.Tests.Tests.ChatTests;

/// <summary>
/// A chat opened against an AI room — what the composer's agent picker sends: <c>entityId</c> is the only channel, so
/// picking an agent is <c>entityId = &lt;room id&gt;</c>. The agent routes are gone (the agent factory, its list and
/// its model), so what is left is the room as a chat location and the instructions it stores.
/// </summary>
/// <remarks>
/// Which system prompt won is read off the request the provider received, through two marker tokens, instead of off
/// a real model's reply as the TypeScript suite did.
/// </remarks>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Chat")]
public class ChatComposerAgentTests(AspireAppFixture fixture) : ChatPart3TestBase(fixture)
{
    private const string AgentMarker = "ZZPICKEDAGENTZZ";
    private const string RequestMarker = "ZZREQUESTPROMPTZZ";

    [Fact]
    public async Task OpenOrCreate_ThreadStartedInAnAiRoomWithNoModelPicked_ReopensWithItsConversation()
    {
        var provider = NewProvider();
        var profile = await CreateProfileOnAsync(provider);
        var roomId = (await CreateRoomOfTypeAsync(RoomType.AiRoom)).Id.ToString();
        var question = $"picked-{Guid.NewGuid():N}";

        // A composer whose picker is hidden behind the room sends no profileId: the portal's model answers.
        var thread = await _aiThreads.AiThreadsCreateAsync(
            new AiThreadsCreateRequest(title: "picked-agent thread", profileId: null!, entityId: roomId),
            TestContext.Current.CancellationToken);
        provider.Enqueue(StubModelProvider.Text("OK"));

        var round = await SendWithStreamAsync(thread.ThreadId, question, entityId: roomId);

        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull(round.ToString());
        round.AssistantText.Should().Be("OK", round.ToString());
        StreamedRequestsCarrying(provider, question).Should().NotBeEmpty($"the portal's model answered\n{provider.Describe()}");

        // Reopening: the same thread comes back with the conversation held in it.
        var reopened = await _aiThreads.AiThreadsOpenOrCreateAsync(
            new AiThreadsOpenOrCreateRequest(
                threadId: thread.ThreadId,
                profile: profile,
                profileId: profile.Id,
                firstMessage: BuildUserMessage("hello"),
                entityId: roomId),
            TestContext.Current.CancellationToken);

        reopened.ThreadId.Should().Be(thread.ThreadId, "the same thread is reopened");
        reopened.PriorMessages.Should().HaveCount(2, "with the question and the reply held in it");
    }

    [Fact]
    public async Task SendWithStream_RequestPromptOnARoomWithoutInstructions_ReachesTheModel()
    {
        // The control for the two tests below: an entity with no instructions of its own, so "the request's prompt
        // goes nowhere at all" is ruled out before anything is said about which prompt wins.
        var provider = NewProvider();
        var profile = await CreateProfileOnAsync(provider);
        var roomId = (await CreateRoomOfTypeAsync(RoomType.CustomRoom)).Id.ToString();
        var thread = await CreateThreadAsync(profileId: profile.Id, entityId: roomId);
        var question = $"instructed-{Guid.NewGuid():N}";
        provider.Enqueue(StubModelProvider.Text($"Hi {RequestMarker}"));

        var round = await SendWithStreamAsync(thread.ThreadId, question, profile.Id, roomId, ReplacePrompt(InstructionsEndingWith(RequestMarker)));

        round.StreamError.Should().BeNull(round.ToString());
        StreamedRequestsCarrying(provider, question).Should().ContainSingle(provider.Describe())
            .Which.Should().Contain(RequestMarker, "the prompt sent with the request reached the model");
    }

    [Fact]
    [Trait("Bug", "83236")]
    public async Task SendWithStream_RequestPromptInAnAiRoomWithInstructions_ReplacesTheRoomInstructions()
    {
        // The documented per-request override has to replace what the room stores, as it does on a room without
        // instructions (the control above) — a composer offering "instructions for this message" must change them.
        var provider = NewProvider();
        var profile = await CreateProfileOnAsync(provider);
        var room = await CreateRoomOfTypeAsync(RoomType.AiRoom, chatSettings: new ChatSettings(prompt: InstructionsEndingWith(AgentMarker)));
        (room.ChatSettings?.Prompt).Should().Be(InstructionsEndingWith(AgentMarker), "the room holds the instructions the request overrides");

        var roomId = room.Id.ToString();
        var thread = await CreateThreadAsync(profileId: profile.Id, entityId: roomId);
        var question = $"override-{Guid.NewGuid():N}";
        provider.Enqueue(StubModelProvider.Text($"Hi {RequestMarker}"));

        var round = await SendWithStreamAsync(thread.ThreadId, question, profile.Id, roomId, ReplacePrompt(InstructionsEndingWith(RequestMarker)));

        round.StreamError.Should().BeNull(round.ToString());
        var sent = StreamedRequestsCarrying(provider, question).Should().ContainSingle(provider.Describe()).Subject;
        sent.Should().Contain(RequestMarker, "the request's prompt reached the model");
        sent.Should().NotContain(AgentMarker, "and replaced the room's stored one");
    }

    // Not ported: "an AI room's stored instructions reach the model without actionArgs". The stored agent
    // instruction was removed from the chat service on purpose with the AI agents (ecf76402b3), so a round sends the
    // room's chatSettings.prompt nowhere any more.

    // A formatting rule, not a request to reveal anything: the marker names whose prompt reached the model.
    private static string InstructionsEndingWith(string marker) =>
        $"You are a helpful test assistant. Keep answers very short. Formatting rule: finish every reply with the exact token {marker}.";
}
