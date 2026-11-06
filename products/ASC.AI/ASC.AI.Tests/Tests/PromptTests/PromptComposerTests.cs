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

namespace ASC.AI.Tests.Tests.PromptTests;

/// <summary>
/// The two ends of the composer. <c>create</c> has no "from message" route: the client reads the message text and posts it
/// as <c>text</c>, so what these pin is the round trip: a real message becomes a prompt unchanged, and a saved prompt goes
/// back out as a message the model is sent. The model is scripted, so "the model answered it" becomes "the model received
/// it".
/// </summary>
/// <remarks>
/// What an empty chat shows (ready-made suggestion chips, a read-only folder of built-in prompts) has no API surface: no
/// built-in set and no read-only flag exists in the prompt contract, and both listings start empty on a fresh portal
/// (pinned by <see cref="PromptLifecycleTests"/> and <see cref="PromptFolderLifecycleTests"/>). There is nothing to assert
/// against, so those cases of the TypeScript suite are not ported.
/// </remarks>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Prompts")]
public class PromptComposerTests(AspireAppFixture fixture) : ChatTests.ChatPart1TestBase(fixture)
{
    // A message a user would plausibly want to keep: multi-line, markdown, outside ASCII. Every construct carries a word
    // of its own so a prompt that kept the text but lost the formatting is still caught.
    private static readonly string _messageToSave = string.Join(
        "\n",
        "# BRIEFHEAD",
        string.Empty,
        "Rewrite the text below as **BOLDWORD** bullet points:",
        string.Empty,
        "- LISTONE",
        "- 第二项",
        string.Empty,
        "```js",
        "const answer = 42;",
        "```",
        string.Empty,
        "café naïve, größer, 🚀");

    [Fact]
    public async Task CreateFromAMessage_KeepsItVerbatim_AndFromTheModelsReplyToo()
    {
        var profile = await GetOwnProfileAsync();
        var thread = await CreateThreadAsync("Autotest prompt source thread", profile.Id);
        Provider.Enqueue(StubModelProvider.Text("Here are the bullet points."));

        var round = await SendWithStreamAsync(thread.ThreadId, _messageToSave, profile.Id);
        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull(round.ToString());

        var messages = await WaitForAssistantRepliesAsync(thread.ThreadId);
        AssertHealthyReplies(messages, "Here are the bullet points.");

        // The text the composer would save comes off the stored message, not off the constant above: a store that
        // mangled the message would otherwise be invisible and the round trip would be measured against nothing.
        var userMessages = UserMessages(messages);
        userMessages.Should().ContainSingle();
        var messageText = TextOf(userMessages[0])!;
        messageText.Should().Be(_messageToSave, "the message survived the send verbatim");

        var created = await _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput("Autotest from my message", messageText), TestContext.Current.CancellationToken);
        created.Success.Should().BeTrue();

        var read = await _aiPrompts.AiPromptsGetByIdAsync(created.Prompt.Id, TestContext.Current.CancellationToken);
        read.Text.Should().Be(_messageToSave, "the saved prompt is the message, unchanged");

        // Spelt out separately: comparing the whole blob says "different" without saying what a truncating or
        // newline-normalising store dropped.
        read.Text.Split('\n').Should().HaveCount(_messageToSave.Split('\n').Length);
        read.Text.Should().ContainAll("BRIEFHEAD", "**BOLDWORD**", "第二项", "🚀");

        // And it is in the library the composer lists, not just readable by id.
        (await _aiPrompts.AiPromptsListAsync(cancellationToken: TestContext.Current.CancellationToken)).Select(p => p.Id).Should().Contain(created.Prompt.Id);

        // The other half of the same gesture: keeping the model's answer. Same route, but a text the test did not author.
        var replyText = TextOf(AssistantMessages(messages)[0])!;
        var fromReply = await _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput("Autotest from the reply", replyText), TestContext.Current.CancellationToken);
        fromReply.Success.Should().BeTrue();
        (await _aiPrompts.AiPromptsGetByIdAsync(fromReply.Prompt.Id, TestContext.Current.CancellationToken)).Text.Should().Be(replyText);
    }

    [Fact]
    public async Task ASavedPrompt_IsSentFromTheComposer_AndReachesTheModel()
    {
        const string promptText = "Reply with exactly the word PROMPTPLUTO.";

        var created = await _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput("Autotest composer preset", promptText), TestContext.Current.CancellationToken);
        created.Success.Should().BeTrue();

        // What the composer inserts is what get-by-id hands back, so the message is built from the response.
        var stored = await _aiPrompts.AiPromptsGetByIdAsync(created.Prompt.Id, TestContext.Current.CancellationToken);
        stored.Text.Should().Be(promptText);

        var profile = await GetOwnProfileAsync();
        var thread = await CreateThreadAsync("Autotest preset thread", profile.Id);
        Provider.Enqueue(StubModelProvider.Text("PROMPTPLUTO"));

        var round = await SendWithStreamAsync(thread.ThreadId, stored.Text, profile.Id);
        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull(round.ToString());

        var messages = await WaitForAssistantRepliesAsync(thread.ThreadId);
        AssertHealthyReplies(messages, "PROMPTPLUTO");

        // The prompt arrived as the user's message, and the model was sent it: a healthy reply on its own would pass
        // even if the text had been dropped on the way.
        TextOf(UserMessages(messages)[0]).Should().Be(promptText);
        ChatRequestsCarrying(promptText).Should().ContainSingle(Provider.Describe());

        // Using a preset does not consume it: it is still in the library.
        (await _aiPrompts.AiPromptsListAsync(cancellationToken: TestContext.Current.CancellationToken)).Select(p => p.Id).Should().Contain(created.Prompt.Id);
    }
}
