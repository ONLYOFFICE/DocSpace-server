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
/// Changing a thread while the model is still writing into it — the race a user creates by renaming, clearing or
/// deleting a chat mid-reply. The window is held open by a provider that answers only after a delay; the round is
/// started and not awaited, the change is made once the question is stored, and the round is awaited afterwards.
/// </summary>
/// <remarks>
/// Every test checks the round had not finished when the change landed: a change is only a mid-reply change if the
/// send was still in flight, and without that check these tests would keep passing against a build that answers at
/// once.
/// </remarks>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Chat")]
public class ChatMutatedWhileWritingTests(AspireAppFixture fixture) : ChatPart3TestBase(fixture)
{
    private const string Reply = "A detailed essay about the history of typography.";

    private static readonly TimeSpan _writingTime = TimeSpan.FromSeconds(6);

    private static readonly TimeSpan _lateWriteWindow = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task Rename_MidReply_SurvivesTheReply()
    {
        var (provider, profile) = await ArrangeSlowModelAsync();
        var thread = await CreateThreadAsync("before rename", profile.Id);

        var inFlight = await StartReplyAsync(thread.ThreadId, $"race-{Guid.NewGuid():N}", profile.Id);
        var renamed = await _aiThreads.AiThreadsRenameAsync(
            new AiThreadsRenameRequest(thread.ThreadId, "renamed mid-reply"),
            TestContext.Current.CancellationToken);

        inFlight.IsCompleted.Should().BeFalse("the reply was still being written when the rename landed");
        renamed.Success.Should().BeTrue();

        // The reply lands after the rename; neither write loses to the other.
        var round = await inFlight;
        round.StreamError.Should().BeNull(round.ToString());

        var messages = await WaitForAssistantRepliesAsync(thread.ThreadId);
        AssertHealthyReply(AssistantMessages(messages).Should().ContainSingle(provider.Describe()).Subject, "the reply still arrived");

        (await _aiThreads.AiThreadsGetByIdAsync(thread.ThreadId, TestContext.Current.CancellationToken)).Title
            .Should().Be("renamed mid-reply");
        (await ListAllThreadsAsync()).Should().ContainSingle(t => t.ThreadId == thread.ThreadId)
            .Which.Title.Should().Be("renamed mid-reply");
    }

    [Fact]
    public async Task Delete_MidReply_TheThreadDoesNotComeBack()
    {
        // The failure this rules out: the in-flight reply is written after the delete and recreates the row, leaving
        // a chat the user thought was gone.
        var (_, profile) = await ArrangeSlowModelAsync();
        var doomed = await CreateThreadAsync("deleted mid-reply", profile.Id);
        var keeper = await CreateThreadAsync("keeper", profile.Id);

        var inFlight = await StartReplyAsync(doomed.ThreadId, $"race-{Guid.NewGuid():N}", profile.Id);
        var deleted = await _aiThreads.AiThreadsDeleteAsync(doomed.ThreadId, TestContext.Current.CancellationToken);

        inFlight.IsCompleted.Should().BeFalse("the reply was still being written when the thread was deleted");
        deleted.Success.Should().BeTrue();

        // Losing the thread under it ends the stream: the reply on its way comes back as an error frame.
        var round = await inFlight;
        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.OfType("error").Should().NotBeEmpty($"the round reports it lost its thread: {round}");

        // A late write could only show as the row coming back: watch for it over the window, stop at the first sight.
        var threadIds = await WatchAsync(
            async () => (await ListAllThreadsAsync()).Select(t => t.ThreadId).ToList(),
            ids => ids.Contains(doomed.ThreadId));

        threadIds.Should().BeEquivalentTo(new[] { keeper.ThreadId }, "the deleted thread must not be resurrected by the late reply");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiThreads.AiThreadsGetByIdAsync(doomed.ThreadId, TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(404);

        (await TryReadStoredMessagesAsync(doomed.ThreadId)).Messages.Should().BeEmpty("and it holds no messages either");
    }

    [Fact]
    public async Task ClearMessages_MidReply_DoesNotBringTheQuestionBack()
    {
        var (_, profile) = await ArrangeSlowModelAsync();
        var thread = await CreateThreadAsync("cleared mid-reply", profile.Id);

        var inFlight = await StartReplyAsync(thread.ThreadId, $"race-{Guid.NewGuid():N}", profile.Id);
        var cleared = await _aiThreads.AiThreadsClearMessagesAsync(thread.ThreadId, TestContext.Current.CancellationToken);

        inFlight.IsCompleted.Should().BeFalse("the reply was still being written when the thread was cleared");
        cleared.Success.Should().BeTrue();

        await inFlight;

        // Whether the late reply is dropped or lands in the emptied thread is the backend's choice; the cleared
        // question coming back is not — that is history the user asked to be gone. A late write would bring it back
        // after the round: watch for it over the window, stop at the first sight.
        var messages = await WatchAsync(() => ReadStoredMessagesAsync(thread.ThreadId), m => UserMessages(m).Count > 0);
        UserMessages(messages).Should().BeEmpty($"the cleared question must stay cleared: {string.Join("\n", messages)}");

        (await _aiThreads.AiThreadsGetByIdAsync(thread.ThreadId, TestContext.Current.CancellationToken)).Title
            .Should().Be("cleared mid-reply", "the thread itself survives clearing, with its title");
    }

    [Fact]
    public async Task SendWithStream_TwoThreadsAtTheSameTime_EachAnswerGoesToItsOwnThread()
    {
        // Two replies in flight for the same user must not be serialised into one another's thread. Each question is
        // answered by its own word after a short wait, so the two rounds overlap.
        var provider = NewProvider();
        var profile = await CreateProfileOnAsync(provider);
        var alphaQuestion = $"alpha-{Guid.NewGuid():N}";
        var betaQuestion = $"beta-{Guid.NewGuid():N}";
        AnswerSlowly(provider, TimeSpan.FromSeconds(2), "ALPHA", body => body.Contains(alphaQuestion, StringComparison.Ordinal));
        AnswerSlowly(provider, TimeSpan.FromSeconds(2), "BETA", body => body.Contains(betaQuestion, StringComparison.Ordinal));

        var first = await CreateThreadAsync("concurrent A", profile.Id);
        var second = await CreateThreadAsync("concurrent B", profile.Id);

        var rounds = await Task.WhenAll(
            SendWithStreamAsync(first.ThreadId, alphaQuestion, profile.Id),
            SendWithStreamAsync(second.ThreadId, betaQuestion, profile.Id));

        rounds[0].StreamError.Should().BeNull(rounds[0].ToString());
        rounds[1].StreamError.Should().BeNull(rounds[1].ToString());

        var messagesA = await WaitForAssistantRepliesAsync(first.ThreadId);
        var messagesB = await WaitForAssistantRepliesAsync(second.ThreadId);
        var textA = string.Concat(AssistantMessages(messagesA).Select(MessageText));
        var textB = string.Concat(AssistantMessages(messagesB).Select(MessageText));

        textA.Should().Contain("ALPHA").And.NotContain("BETA");
        textB.Should().Contain("BETA").And.NotContain("ALPHA");
        MessageText(UserMessages(messagesA).Should().ContainSingle().Subject).Should().Be(alphaQuestion);
        MessageText(UserMessages(messagesB).Should().ContainSingle().Subject).Should().Be(betaQuestion);
    }

    /// <summary>A profile on a provider of the test's own that takes <see cref="_writingTime"/> over every reply.</summary>
    private async Task<(StubModelProvider Provider, AiProfile Profile)> ArrangeSlowModelAsync()
    {
        var provider = NewProvider();
        var profile = await CreateProfileOnAsync(provider);

        // After the create: the profile's own tool probe is streamed too and would be held up as well.
        AnswerSlowly(provider, _writingTime, Reply);

        return (provider, profile);
    }

    /// <summary>
    /// Re-reads <paramref name="read"/> for <see cref="_lateWriteWindow"/> after the round has ended, the time a late
    /// write of the engine would need to land, and answers the first state <paramref name="until"/> accepts (the
    /// failure seen) or the last one observed.
    /// </summary>
    private static async Task<T> WatchAsync<T>(Func<Task<T>> read, Func<T, bool> until)
    {
        var deadline = DateTime.UtcNow + _lateWriteWindow;

        while (true)
        {
            var observed = await read();

            if (until(observed) || DateTime.UtcNow >= deadline)
            {
                return observed;
            }

            await Task.Delay(250, TestContext.Current.CancellationToken);
        }
    }

    private Task<List<AiThread>> ListAllThreadsAsync() =>
        _aiThreads.AiThreadsListAsync(cancellationToken: TestContext.Current.CancellationToken);
}
