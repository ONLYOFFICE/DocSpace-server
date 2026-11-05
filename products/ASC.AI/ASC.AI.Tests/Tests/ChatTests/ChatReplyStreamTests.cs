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
/// The contract of one streamed reply of <c>ai/send-with-stream</c>: NDJSON with no length announced, the stored
/// question acknowledged first, one opening and one terminal frame with content frames between them, every later frame
/// tied to one assistant message, cumulative snapshots, and the assembled text equal to what the thread stores.
/// </summary>
/// <remarks>
/// Two details shape the assertions: the first frame carries the id of the stored <i>user</i> message and every frame
/// after it the assistant's; and a <c>message-delta</c> is a snapshot of the whole reply so far, not the added piece.
/// </remarks>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Chat")]
public class ChatReplyStreamTests(AspireAppFixture fixture) : ChatPart1TestBase(fixture)
{
    private const string LongAnswerQuestion = "Count from 1 to 60, separated by commas. Output nothing else.";

    private static readonly string[] _contentFrameTypes = ["message-start", "message-delta", "message-end"];

    [Fact]
    public async Task SendWithStream_Reply_IsStreamedAsNdjsonFramesThatMatchTheStoredMessage()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var profile = await GetOwnProfileAsync();
        var thread = await CreateThreadAsync("Autotest stream thread", profile.Id);

        var pieces = Enumerable.Range(0, 6).Select(i => string.Join(", ", Enumerable.Range((i * 10) + 1, 10)) + (i < 5 ? ", " : "")).ToArray();
        Provider.Enqueue(TextInPieces(pieces));

        // Act
        // Raw on purpose — an SDK defect: the response is an ndjson stream of AiChatEvent frames, but the SDK declares it
        // as one AiChatEvent, so AiSendWithStreamAsync throws on the second frame. Read here rather than through
        // SendWithStreamAsync because the headers are part of the contract. The request body is the SDK model.
        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(
            SendWithStreamPath,
            new AiSendStreamBody(threadId: thread.ThreadId, userMessage: BuildUserMessage(LongAnswerQuestion), profileId: profile.Id).ToJson(),
            cancellationToken);

        // Read before anything touches Content.Headers.ContentLength, which computes and stores a length of its own
        // once the body is buffered.
        var announcesLength = response.Content.Headers.NonValidated.Contains("Content-Length");
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        var round = ChatRound.Parse(response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));

        // Assert
        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull(round.ToString());
        mediaType.Should().Be("application/x-ndjson");
        announcesLength.Should().BeFalse("a streamed body cannot announce its length up front");

        var frames = round.Frames;
        var types = frames.Select(FrameType).ToList();
        var where = $"frames were {string.Join(", ", types)}\n{Provider.Describe()}";

        // The question is acknowledged first, before the model is asked anything.
        types[0].Should().Be("user-message-stored", where);
        var userFrame = frames[0];
        FrameText(userFrame).Should().Be(LongAnswerQuestion);

        // Then the reply opens and finishes, once each and in that order.
        var startIndex = types.IndexOf("message-start");
        var endIndex = types.IndexOf("message-end");
        startIndex.Should().BeGreaterThan(0, where);
        endIndex.Should().BeGreaterThan(startIndex, where);
        types.Count(t => t == "message-start").Should().Be(1, "one reply, one opening frame");
        types.Count(t => t == "message-end").Should().Be(1, "one reply, one terminal frame");

        // A healthy reply never carries the failure terminal, nor a tool-call pause.
        types.Should().NotContain("message-incomplete", where).And.NotContain("tool-call-pending", where);

        // The answer arrives in pieces rather than in one frame at the end.
        types.Skip(startIndex + 1).Take(endIndex - startIndex - 1).Count(t => t == "message-delta")
            .Should().BeGreaterThan(0, $"no content frame between start and end; {where}");

        // Nothing carrying reply content follows the terminal frame.
        types.Skip(endIndex + 1).Where(t => _contentFrameTypes.Contains(t)).Should().BeEmpty(where);

        // Every frame after the acknowledgement belongs to one assistant message.
        var streamedMessageId = FrameString(frames[startIndex], "messageId");
        streamedMessageId.Should().NotBeNullOrEmpty(where);

        foreach (var frame in frames.Skip(1))
        {
            FrameString(frame, "messageId").Should().Be(streamedMessageId, $"{FrameType(frame)} names another message");

            var messageId = FrameMessageId(frame);
            if (messageId is not null)
            {
                messageId.Should().Be(streamedMessageId);
            }

            // threadId is optional on this protocol, so it is checked where it exists.
            var threadId = FrameString(frame, "threadId");
            if (threadId is not null)
            {
                threadId.Should().Be(thread.ThreadId);
            }
        }

        // The snapshots grow: each one extends the one before it.
        var snapshots = frames.Where(f => _contentFrameTypes.Contains(FrameType(f))).Select(FrameText).ToList();
        for (var index = 1; index < snapshots.Count; index++)
        {
            snapshots[index].Should().StartWith(snapshots[index - 1], $"snapshot {index} extends the one before it");
        }

        var streamedText = snapshots[^1];
        streamedText.Should().Be(string.Concat(pieces), "the assembled reply is what the model sent");

        // What the client assembled is what the thread kept.
        var stored = await WaitForAssistantRepliesAsync(thread.ThreadId);
        AssertHealthyReplies(stored, streamedText);

        var asked = UserMessages(stored);
        asked.Should().ContainSingle("the question is stored once");
        asked[0].Id.Should().Be(FrameString(userFrame, "messageId"));

        var replies = AssistantMessages(stored);
        replies.Should().ContainSingle("the reply is stored once");
        replies[0].Id.Should().Be(streamedMessageId);
    }
}
