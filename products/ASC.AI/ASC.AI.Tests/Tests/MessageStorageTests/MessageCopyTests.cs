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
/// Copying a message. The clipboard belongs to the browser; what the API decides is what the copy button puts into
/// it: the text of a message as the portal returns it. So the reply has to be served as its own markdown — not
/// rendered, not escaped —, the text copied off the stream has to be the text copied after a reload, and the user's
/// own message has to come back exactly as typed, edits included.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Messages")]
public class MessageCopyTests(AspireAppFixture fixture) : MessagePart2TestBase(fixture)
{
    [Fact]
    public async Task ReadMessages_TheAssistantReply_IsServedAsRawMarkdown()
    {
        using var provider = new StubModelProvider();

        var reply = (await ReplyWithAsync(provider, MarkdownAnswer)).Text;

        // Every construct arrives with its syntax intact — this is what the copy button puts on the clipboard.
        foreach (var construct in new[]
                 {
                     "# COPYCHECK", "**BOLDWORD**", "*ITALICWORD*", "- LISTONE", "- LISTTWO", "```js", "const answer = 42;",
                     "| CELLA | CELLB |", "| --- |"
                 })
        {
            reply.Should().Contain(construct, $"the markdown construct {JsonSerializer.Serialize(construct)} did not survive");
        }

        // ...and it is still markdown: not rendered to HTML on the way out, and not escaped either.
        foreach (var tell in HtmlTells)
        {
            reply.Should().NotContain(tell, $"the reply arrived rendered as HTML: {tell}");
        }

        foreach (var tell in EscapeTells)
        {
            reply.Should().NotContain(tell, $"the reply arrived escaped: {JsonSerializer.Serialize(tell)}");
        }

        reply.Trim().Should().Be(MarkdownAnswer.Trim(), "the stored reply is the model's answer, unchanged");
    }

    [Fact]
    public async Task SendWithStream_TheStreamedTextAndTheStoredText_AreTheSameCopy()
    {
        using var provider = new StubModelProvider();

        // Markdown in both directions, so the comparison is not made on a plain one-liner no formatting step could disturb.
        const string question = "Format check: **bold**, _under_, `code`, 🙂 — dash.";

        var scripted = await ReplyWithAsync(provider, MarkdownAnswer, question);
        var round = scripted.Round;

        var streamed = round.AssistantText;
        streamed.Should().NotBeNullOrEmpty($"the stream carried a reply: {round}");

        var echoed = round.OfType("user-message-stored").ToList();
        echoed.Should().NotBeEmpty($"the stream echoes the question back: {round}");

        var messages = await ReadMessagesAsync(scripted.ThreadId);

        // Trimmed: a trailing newline is a difference no user could perceive in a paste.
        scripted.Text.Trim().Should().Be(streamed!.Trim(), "the reply copied off the stream differs from the reply copied after a reload");

        FrameText(echoed[0]).Should().Be(question, "the echoed question is what was typed");
        TextOf(UserMessages(messages).Single()).Should().Be(question, "the question is stored as it was typed");
    }

    [Fact]
    public async Task AppendUserMessage_TheUsersOwnMessage_ComesBackExactlyAsTyped()
    {
        // Markdown, an emoji, a fenced block, and the two trailing spaces markdown reads as a hard line break — the one
        // piece of formatting a careless trim destroys without leaving a trace.
        var own = string.Join("\n",
            "# My note",
            "",
            "**BOLDWORD**, _under_, `code`, 2 * 3 = 6",
            "",
            "- item one",
            "- item two 🙂",
            "",
            "```sql",
            "select 1;",
            "```",
            "",
            "hard break follows  ",
            "last line");

        var threadId = await CreateMessageThreadAsync();
        var messageId = await AppendAsync(threadId, own);

        TextOf(await GetMessageAsync(messageId)).Should().Be(own, "get-message-by-id changed the text the user typed");

        var history = await ReadMessagesAsync(threadId);
        history.Should().ContainSingle();
        TextOf(history[0]).Should().Be(own, "read-messages changed the text the user typed");

        // An edited message is copied the same way, so the rewrite must not normalise anything either.
        var edited = own.Replace("# My note", "# My edited note ✅", StringComparison.Ordinal);
        var update = await _aiThreads.AiThreadsUpdateMessageAsync(
            new AiThreadsUpdateMessageRequest(messageId: messageId, message: BuildUserMessage(edited)),
            TestContext.Current.CancellationToken);
        update.Success.Should().BeTrue();

        TextOf(await GetMessageAsync(messageId)).Should().Be(edited);

        var historyAfterEdit = await ReadMessagesAsync(threadId);
        historyAfterEdit.Should().ContainSingle();
        TextOf(historyAfterEdit[0]).Should().Be(edited);
    }
}
