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
/// The portal AI switch off: the chat surface refuses with 403. Everything runs against a real AI room, thread and
/// message created while AI was still on — an unknown thread produces 200/404/"stream error" depending on the route,
/// so a made-up id could not tell "the AI switch refused this" from "that id does not exist". A refused write is
/// checked with the switch back on: nothing changed.
/// </summary>
/// <remarks>
/// The wallet blocks of chat.ai-disabled.spec.ts (AI Tools unpaid, paid with no credit, low balance) are not ported:
/// they are about SaaS billing, which the standalone test stand does not have.
/// </remarks>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Chat")]
public class ChatAiDisabledTests(AspireAppFixture fixture) : ChatPermissionTestBase(fixture)
{
    public static TheoryData<string> GatedRoutes => new()
    {
        "threads/list",
        "threads/get-by-id",
        "threads/read-messages",
        "profiles/list",
        "threads/create",
        "threads/rename",
        "threads/delete",
        "threads/clear-messages",
        "threads/append-user-message",
        "threads/touch"
    };

    [Theory]
    [MemberData(nameof(GatedRoutes))]
    public async Task Route_AiDisabled_Returns403AndChangesNothing(string route)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedOwnerThreadAsync();
        var roomId = seed.RoomId.ToString();

        await SetPortalAiAccessAsync(false);

        Func<Task> act = route switch
        {
            "threads/list" => () => ListThreadsAsync(),
            "threads/get-by-id" => () => GetThreadAsync(seed.ThreadId),
            "threads/read-messages" => () => ReadAllAsync(seed.ThreadId),
            "profiles/list" => () => _aiProfiles.AiProfilesListAsync(cancellationToken),
            "threads/create" => () => CreateThreadAsync("Thread created while AI is off", seed.Profile.Id, roomId),
            "threads/rename" => () => _aiThreads.AiThreadsRenameAsync(
                new AiThreadsRenameRequest(seed.ThreadId, "Renamed while AI is off"),
                cancellationToken),
            "threads/delete" => () => _aiThreads.AiThreadsDeleteAsync(seed.ThreadId, cancellationToken),
            "threads/clear-messages" => () => _aiThreads.AiThreadsClearMessagesAsync(seed.ThreadId, cancellationToken),
            // The SDK call is fine here: its defect (the declared shape of the answer) only bites on a 200.
            "threads/append-user-message" => () => _aiThreads.AiThreadsAppendUserMessageAsync(
                new AiThreadsAppendUserMessageRequest(
                    threadId: seed.ThreadId,
                    message: BuildUserMessage("Appended while AI is off"),
                    profileId: seed.Profile.Id),
                cancellationToken),
            _ => () => _aiThreads.AiThreadsTouchAsync(new AiThreadsTouchRequest(seed.ThreadId), cancellationToken)
        };
        var (status, error) = await OutcomeOfAsync(act);

        // With the switch back on, the thread is exactly what it was: listed alone, same title, same date, the one
        // message.
        await SetPortalAiAccessAsync(true);
        await ExpectOwnerThreadIntactAsync(seed);
        (await ListIdsAsync()).Should().Equal(new[] { seed.ThreadId }, "no thread was created or deleted");

        status.Should().Be(403, error);
        error.Should().Contain("Forbidden");
    }

    /// <summary>
    /// The send used to answer HTTP 200 and put the refusal in the stream body as
    /// <c>{"type":"error","message":"stream error"}</c> — an opaque message under a status a client reads as success
    /// (Bug 82724). Briefly afterwards it answered 400 "unknown profileId", as if profile resolution ran before the
    /// AI-switch gate. It now refuses with 403 before anything reaches the model or the thread.
    /// </summary>
    [Fact]
    [Trait("Bug", "82724")]
    public async Task SendWithStream_AiDisabled_Returns403AndReachesNeitherTheModelNorTheThread()
    {
        var seed = await SeedOwnerThreadAsync();
        var question = $"What is 2+2? Answer in one word. {Guid.NewGuid():N}";

        await SetPortalAiAccessAsync(false);
        var round = await SendWithStreamAsync(seed.ThreadId, question, seed.Profile.Id, seed.RoomId.ToString());

        // The request neither reached the model nor touched the thread: no new user message, no reply.
        await SetPortalAiAccessAsync(true);
        await ExpectOwnerThreadIntactAsync(seed);
        ModelProvider.CompletionRequestBodies.Should().NotContain(b => b.Contains(question));

        round.StatusCode.Should().Be(HttpStatusCode.Forbidden, round.ToString());
        round.Raw.Should().Contain("Forbidden");
        round.StreamError.Should().BeNull("the refusal is the status, not a stream frame");
    }
}
