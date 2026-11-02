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
/// A cleared thread carries on. Clearing the history has to reach the model, not just the transcript a reader gets:
/// <c>read-messages</c> coming back empty is equally true of a backend that hid the transcript and kept feeding it to
/// the model. What the model is sent is read off the provider, so "the model forgot" needs no real model's recall.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Threads")]
public class ChatClearedThreadTests(AspireAppFixture fixture) : ChatPart3TestBase(fixture)
{
    private const string CodeWord = "ORANGE";
    private const string Teach = $"Remember the code word {CodeWord}. Reply with just: OK.";
    private const string Recall = "What code word did I ask you to remember earlier in this conversation? If no code word was ever mentioned here, reply with just: NONE.";

    [Fact]
    public async Task ClearMessages_ThenSend_TheThreadAnswersWithoutTheClearedHistory()
    {
        var provider = NewProvider();
        var profile = await CreateProfileOnAsync(provider);
        var thread = await CreateThreadAsync("cleared thread", profile.Id);
        provider.Enqueue(StubModelProvider.Text("OK"), StubModelProvider.Text(CodeWord), StubModelProvider.Text("NONE"));

        (await SendWithStreamAsync(thread.ThreadId, Teach, profile.Id)).StreamError.Should().BeNull();
        (await SendWithStreamAsync(thread.ThreadId, Recall, profile.Id)).StreamError.Should().BeNull();

        // The positive control: before the clear the history reaches the model with the round.
        StreamedRequestsCarrying(provider, Recall).Should().ContainSingle(provider.Describe())
            .Which.Should().Contain(Teach, "the history is sent while it is still there");

        var cleared = await _aiThreads.AiThreadsClearMessagesAsync(thread.ThreadId, TestContext.Current.CancellationToken);
        cleared.Success.Should().BeTrue();
        (await ReadMessagesRawAsync(thread.ThreadId)).Should().BeEmpty();

        // The same question again, in the same thread.
        var afterClear = await SendWithStreamAsync(thread.ThreadId, Recall, profile.Id);

        afterClear.StatusCode.Should().Be(HttpStatusCode.OK, afterClear.ToString());
        afterClear.StreamError.Should().BeNull(afterClear.ToString());
        StreamedRequestsCarrying(provider, Recall).Should().HaveCount(2, provider.Describe())
            .And.Subject.Last().Should().NotContain(CodeWord, "the cleared history is no longer sent to the model");

        // Exactly one turn is stored, and the cleared word is neither answered nor stored again.
        var withoutHistory = await WaitForAssistantRepliesAsync(thread.ThreadId);
        UserMessages(withoutHistory).Should().HaveCount(1);
        AssertHealthyReply(AssistantMessages(withoutHistory).Should().ContainSingle().Subject, "the cleared thread answers again");
        string.Join("\n", withoutHistory.Select(MessageText)).Should().NotContain(CodeWord);

        // Same thread, same name, same model, still listed.
        var stored = await _aiThreads.AiThreadsGetByIdAsync(thread.ThreadId, TestContext.Current.CancellationToken);
        stored.Title.Should().Be("cleared thread");
        stored.ProfileId.Should().Be(profile.Id);
        (await _aiThreads.AiThreadsListAsync(cancellationToken: TestContext.Current.CancellationToken)).Select(t => t.ThreadId)
            .Should().BeEquivalentTo(new[] { thread.ThreadId });
    }
}
