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

namespace ASC.AI.Tests.Tests.PreferenceTests;

/// <summary>
/// What the deep-mode switch does to an answer. The model is a stand-in of the test's own, so the reply is scripted:
/// with the switch on the reply is a healthy answer with no raw reasoning trace mixed into its text, and the reasoning
/// the model streams separately has to reach the client as a part of its own (Bug 83050).
/// </summary>
/// <remarks>
/// The TypeScript suite also pinned the reasoning flag of the gateway's model catalogue; the gateway is not part of
/// the standalone test stand, so that case is not ported.
/// </remarks>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Preferences")]
public class PreferenceDeepModeChatTests(AspireAppFixture fixture) : PreferenceTestBase(fixture)
{
    private const string ReasoningQuestion =
        "A bat and a ball cost $1.10 together. The bat costs $1.00 more than the ball. How much does the ball cost? Reply with the amount only.";

    private static readonly string[] _thinkMarkers = ["<think", "</think", "<thinking", "</thinking"];

    private readonly StubModelProvider _provider = new();

    public override async ValueTask DisposeAsync()
    {
        _provider.Dispose();
        await base.DisposeAsync();
    }

    private async Task<AiProfile> CreateReasoningProfileAsync()
    {
        var input = BuildProfileInput();
        input.BaseUrl = _provider.BaseUrl;
        input.Reasoning = true;
        input.ReasoningSupport = new AiReasoningSupport(
            thinks: true,
            canDisable: true,
            depths: [AiReasoningDepth.Low, AiReasoningDepth.Medium, AiReasoningDepth.High],
            defaultDepth: AiReasoningDepth.Medium);

        var created = await _aiProfiles.AiProfilesCreateAsync(input, TestContext.Current.CancellationToken);
        created.Success.Should().BeTrue($"profiles/create rejected the profile: {created.Error}\n{_provider.Describe()}");

        return created.Profile;
    }

    [Fact]
    public async Task SendWithStream_DeepModeOn_RepliesHealthyWithNoReasoningTraceInTheAnswer()
    {
        var profile = await CreateReasoningProfileAsync();
        await SetLevelAsync(AiReasoningLevel.High);
        (await ReadStateAsync()).Deep.Should().BeTrue("the switch is on before the send");
        var thread = await CreateThreadAsync("Autotest deep mode thread", profile.Id);
        _provider.Enqueue(StubModelProvider.Text("0.05"));

        var round = await SendWithStreamAsync(thread.ThreadId, ReasoningQuestion, profile.Id);

        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull(round.ToString());
        round.AssistantText.Should().Be("0.05", round.ToString());

        var stored = (await _aiThreads.AiThreadsReadMessagesAsync(thread.ThreadId, cancellationToken: TestContext.Current.CancellationToken))
            .Where(m => m.Role == AiThreadMessageLike.RoleEnum.Assistant)
            .ToList();
        stored.Should().ContainSingle();
        // A completed reply is stored without a status (measured 2026-11-03), so a missing one reads as complete; only an
        // explicit incomplete status fails the round.
        (stored[0].Status?.Type ?? "complete").Should().NotBe("incomplete", _provider.Describe());

        // Neither the stream nor the stored reply carries a raw `<think>` trace.
        foreach (var marker in _thinkMarkers)
        {
            round.Raw.ToLowerInvariant().Should().NotContain(marker);
            TextOf(stored[0])!.ToLowerInvariant().Should().NotContain(marker);
        }
    }

    [Fact]
    [Trait("Bug", "83050")]
    public async Task SendWithStream_DeepModeOn_ReasoningReachesTheClientAsAPartOfItsOwn()
    {
        var profile = await CreateReasoningProfileAsync();
        await SetLevelAsync(AiReasoningLevel.High);
        var thread = await CreateThreadAsync("Autotest deep mode thread", profile.Id);
        _provider.Enqueue(ReasoningThenText("The ball costs five cents because the difference is a dollar.", "0.05"));

        var round = await SendWithStreamAsync(thread.ThreadId, ReasoningQuestion, profile.Id);

        // Control first: the model really answered. A refused reply carries no reasoning either, and that is not the
        // defect under test.
        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.AssistantText.Should().Be("0.05", round.ToString());

        var signals = new List<string>();
        foreach (var frame in round.Frames)
        {
            if (!frame.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            // A part of a type other than the answer's text, the way a tool call is a part of its own ...
            if (message.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            {
                signals.AddRange(content.EnumerateArray()
                    .Where(part => part.ValueKind == JsonValueKind.Object && part.TryGetProperty("type", out _))
                    .Select(part => part.GetProperty("type").GetString() ?? string.Empty)
                    .Where(type => type != "text"));
            }

            // ... or a field on the message the client could hang the block off.
            signals.AddRange(message.EnumerateObject()
                .Select(property => property.Name)
                .Where(name => name.Contains("reason", StringComparison.OrdinalIgnoreCase) || name.Contains("think", StringComparison.OrdinalIgnoreCase)));
        }

        signals.Should().NotBeEmpty($"with deep mode on, the reasoning has to reach the client somewhere: {round}");
    }

    /// <summary>A streamed reply whose first chunks carry <c>reasoning_content</c> and whose later ones carry the answer.</summary>
    private static ScriptedAnswer ReasoningThenText(string reasoning, string answer)
    {
        return new ScriptedAnswer(200, StubModelProvider.Sse(
            StubModelProvider.Chunk(new { role = "assistant", content = "" }),
            StubModelProvider.Chunk(new { reasoning_content = reasoning }),
            StubModelProvider.Chunk(new { content = answer }),
            StubModelProvider.Chunk(new { }, "stop")));
    }
}
