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

namespace ASC.AI.Tests.Tests.MessageStorageTests;

/// <summary>
/// Which model a regenerate runs on when the button sends no <c>profileId</c> of its own.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Messages")]
public class MessageRegenerateModelTests(AspireAppFixture fixture) : MessagePart1TestBase(fixture)
{
    /// <summary>
    /// The portal-wide Chat binding points at the OTHER profile — otherwise "the regenerate stayed on the thread's
    /// model" and "it re-resolved the binding and landed on the same one" are the same observation. The regenerate
    /// counterpart of BUG 82860 on send-with-stream. Each profile sits on a provider of its own, so which model
    /// served the regenerate is observed directly, not only what the thread is left on.
    /// </summary>
    [Fact]
    public async Task RegenerateStream_WithoutProfileId_KeepsTheThreadsOwnModel()
    {
        using var boundProvider = new StubModelProvider("bound model answer");
        using var pickedProvider = new StubModelProvider("picked model answer");

        var bound = await CreateChatProfileOnAsync(boundProvider);
        var picked = await CreateChatProfileOnAsync(pickedProvider);

        var assigned = await _aiAssignments.AiAssignmentsAssignAsync(
            new AiAssignmentsAssignRequest(AiActionType.Chat, bound.Id),
            TestContext.Current.CancellationToken);
        assigned.Success.Should().BeTrue($"assign rejected Chat: {assigned.Error?.Message}");

        var resolved = await _aiAssignments.AiAssignmentsResolveForActionAsync("Chat", cancellationToken: TestContext.Current.CancellationToken);
        resolved.ProfileId.Should().Be(bound.Id, "the portal resolves the other profile");

        var thread = await CreateThreadAsync(profileId: picked.Id);
        var sent = await SendWithStreamAsync(thread.ThreadId, "Reply with the single word OK.", picked.Id);
        sent.StatusCode.Should().Be(HttpStatusCode.OK, sent.ToString());
        sent.StreamError.Should().BeNull(sent.ToString());

        var before = await WaitForMessagesAsync(thread.ThreadId, m => Replies(m).Any(r => TextOf(r) == "picked model answer"));
        var firstReply = Replies(before).Should().ContainSingle().Subject;
        (await _aiThreads.AiThreadsGetByIdAsync(thread.ThreadId, TestContext.Current.CancellationToken)).ProfileId
            .Should().Be(picked.Id, "the thread is on the picked model before the regenerate");

        var boundRequests = StreamedRequests(boundProvider);
        var pickedRequests = StreamedRequests(pickedProvider);

        // What the button sends: the thread, and nothing about the model.
        var round = await RegenerateAsync(thread.ThreadId);
        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull(round.ToString());

        var after = await WaitForReplacedReplyAsync(thread.ThreadId, firstReply.Id);
        Replies(after).Select(TextOf).Should().ContainSingle().Which.Should().Be("picked model answer", "the regenerate ran on the thread's model");

        StreamedRequests(pickedProvider).Should().Be(pickedRequests + 1, pickedProvider.Describe());
        StreamedRequests(boundProvider).Should().Be(boundRequests, $"the bound model was not asked\n{boundProvider.Describe()}");
        (await _aiThreads.AiThreadsGetByIdAsync(thread.ThreadId, TestContext.Current.CancellationToken)).ProfileId
            .Should().Be(picked.Id, "regenerating does not move the conversation onto the bound model");
    }
}
