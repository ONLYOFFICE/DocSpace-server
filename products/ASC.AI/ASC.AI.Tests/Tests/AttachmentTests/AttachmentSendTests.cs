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

namespace ASC.AI.Tests.Tests.AttachmentTests;

/// <summary>
/// Sending a message with an attachment: the draft id travels inside the user message
/// (<c>ai/send-with-stream</c>, <c>threads/append-user-message</c>) and <c>link-to-message</c> is not involved at all. The
/// TypeScript suite asked a real model for the code word of the attached file; here the model is a stand-in, so what is
/// asserted is that the text or the picture really reached the provider, and what was stored on the message.
/// </summary>
[Trait("Category", "Attachments")]
[Trait("Feature", "AI/Attachments")]
public class AttachmentSendTests(AspireAppFixture fixture) : AttachmentTestBase(fixture)
{
    private const decimal VisionCapabilities = 257m + 128m;

    private static string NewMarker() => $"PINEAPPLE-{Guid.NewGuid():N}"[..16].ToUpperInvariant();

    private static List<object> Attach(params object[] attachments) => [.. attachments];

    [Fact]
    public async Task SendWithStream_UserMessageAttachments_AreStoredOnTheMessage()
    {
        // The real client flow: the draft id travels inside the user message.
        var (provider, profile, threadId) = await ArrangeChatAsync();
        var id = await SaveDraftAsync(Owner, "Autotest sent.docx", "sent payload");

        var round = await SendWithAttachmentsAsync(threadId, profile.Id, "Autotest attachment carrier", Attach(new { id }));

        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull($"{round}\n{provider.Describe()}");
        ExpectSameJson(await StoredUserAttachmentsAsync(threadId), new[] { new { id } }, "the user message carries the draft id");
    }

    [Fact]
    [Trait("Bug", "82773")]
    public async Task SendWithStream_DraftPassedAsAttachmentsIdOnly_ReachesTheModel()
    {
        // The draft holds a code word; a bare id is enough, the backend resolves it and gives the model the content.
        // Fixed 2026-08-20, reopened 2026-10-07 (the model answered "I don't see an attached file" again): the id was
        // resolved and the content dropped. The marker is random, so it cannot reach the provider any other way.
        var (provider, profile, threadId) = await ArrangeChatAsync();
        var marker = NewMarker();
        var id = await SaveDraftAsync(Owner, "code-word.txt", $"The code word is {marker}. Nothing else matters.");

        var round = await SendWithAttachmentsAsync(
            threadId,
            profile.Id,
            "The attached file contains one code word. Reply with that code word and nothing else.",
            Attach(new { id }));

        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull($"{round}\n{provider.Describe()}");

        StreamedRequestsCarrying(provider, marker).Should().NotBeEmpty($"the file's text reached the model\n{provider.Describe()}");
    }

    [Fact]
    [Trait("Bug", "82773")]
    public async Task SendWithStream_FullyInlinedAttachmentRecord_ReachesTheModelToo()
    {
        // The same contract from the other side, and the one the real client sends: the message carries the whole record
        // (kind, title and the text itself), so nothing has to be looked up. Kept beside the by-id test because the two
        // used to fail together and a fix could reach only one of the shapes. The inlined-attachment format also made
        // the stream hang indefinitely in CI (regression observed 2026-08-31).
        var (provider, profile, threadId) = await ArrangeChatAsync();
        var marker = NewMarker();
        var content = $"The code word is {marker}. Nothing else matters.";
        var id = await SaveDraftAsync(Owner, "code-word.txt", content);
        var inlined = new { id, kind = "file", title = "code-word.txt", content, type = 7 };

        var round = await SendWithAttachmentsAsync(
            threadId,
            profile.Id,
            "The attached file contains one code word. Reply with that code word and nothing else.",
            Attach(inlined));

        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull($"{round}\n{provider.Describe()}");
        // The record really did travel with the message and was stored whole...
        ExpectSameJson(await StoredUserAttachmentsAsync(threadId), new[] { inlined }, "the inlined record is stored whole");
        // ...and the model finished its turn normally, so a missing code word is about what it was given.
        AssertHealthyReply(AssistantMessages(await WaitForAssistantRepliesAsync(threadId))[^1], "the round finished with the model's text");
        StreamedRequestsCarrying(provider, marker).Should().NotBeEmpty($"the inlined text reached the model\n{provider.Describe()}");
    }

    [Fact]
    [Trait("Bug", "82773")]
    public async Task SendWithStream_ImageAttachment_ReachesAVisionCapableModel()
    {
        // An image travels a different route into the store than a file (it is stored as a reference to a picture and
        // served as a data URL), so "the model never sees it" has to be measured separately. Same picture, same
        // question; the profile advertises text + vision + tools, so the model's own capabilities are no explanation.
        var (provider, profile, threadId) = await ArrangeChatAsync(VisionCapabilities);
        var teal = CreatePng(64, 64, 0, 128, 128);
        var base64 = Convert.ToBase64String(teal);
        var id = await SaveImageDraftAsync(Owner, "autotest-solid.png", teal);
        var draft = await ExpectStoredAsync(id, "image draft");
        draft.Base64.Should().Be($"data:image/png;base64,{base64}", "the draft really holds the picture");
        var inlined = new { id, kind = "image", title = "autotest-solid.png", base64 = draft.Base64 };

        var round = await SendWithAttachmentsAsync(
            threadId,
            profile.Id,
            "The attached image is a single solid colour. Reply with the English name of that colour and nothing else.",
            Attach(inlined));

        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull($"{round}\n{provider.Describe()}");
        ExpectSameJson(await StoredUserAttachmentsAsync(threadId), new[] { inlined }, "the image travelled with the message and was stored whole");
        AssertHealthyReply(AssistantMessages(await WaitForAssistantRepliesAsync(threadId))[^1], "the turn finished normally");

        StreamedRequestsCarrying(provider, "solid colour")
            .Should().Contain(body => body.Contains(base64, StringComparison.Ordinal), $"the picture's bytes reached the model\n{provider.Describe()}");
    }

    [Fact]
    public async Task SendWithStream_PictureSentToAModelWithoutVision_IsAcceptedWithNoWarningAtAll()
    {
        // "Works only with a model that supports images" is a limit the product states, and /ai/assignments enforces it
        // where it can. Chat has no such gate: a picture goes to a blind model exactly as to a seeing one and the user
        // is told nothing, at send time or afterwards. Not a bug: nothing specifies what should happen instead, so this
        // pins what is observable — the send is accepted, the picture is stored on the message, the turn completes
        // clean and no error names the model's capabilities.
        var (provider, profile, threadId) = await ArrangeChatAsync(ChatCapabilities);
        ((int)profile.Capabilities & 128).Should().Be(0, "the premise: this model cannot read pictures");

        var id = await SaveImageDraftAsync(Owner, "autotest-solid.png", CreatePng(64, 64, 0, 128, 128));
        var draft = await ExpectStoredAsync(id, "image draft");
        var inlined = new { id, kind = "image", title = "autotest-solid.png", base64 = draft.Base64 };

        var round = await SendWithAttachmentsAsync(
            threadId,
            profile.Id,
            "The attached image is a single solid colour. Reply with the English name of that colour and nothing else.",
            Attach(inlined));

        // Nothing is refused: not the send, and not the stream.
        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull($"{round}\n{provider.Describe()}");
        round.Raw.Should().NotMatchRegex("(?i)vision|not supported|capab", "no capability error came back on the stream");
        ExpectSameJson(await StoredUserAttachmentsAsync(threadId), new[] { inlined }, "the picture was stored on the message for a model that cannot read it");
        AssertHealthyReply(AssistantMessages(await WaitForAssistantRepliesAsync(threadId))[^1], "the turn looks like one on a vision model");
    }

    [Fact]
    public async Task AppendUserMessage_InlineAttachmentObject_IsStoredWithoutCheckingThatItExists()
    {
        // A contract test, not a finding: attachments on a user message are persisted exactly as the client sent them, so
        // an id that was never saved and a payload that never went through save-file are both stored and read back. Driven
        // through append-user-message rather than send-with-stream because this is about storage, not inference.
        var profile = await GetDefaultProfileAsync();
        var thread = await CreateThreadAsync(profileId: profile.Id);
        var inline = new { id = MissingId, kind = "file", title = "never-saved.txt", content = "content that was never stored as a draft", type = 7 };

        var status = await AppendWithAttachmentsAsync(thread.ThreadId, profile.Id, "Autotest inline attachment", Attach(inline));

        status.Should().Be(HttpStatusCode.OK);
        ExpectSameJson(await StoredUserAttachmentsAsync(thread.ThreadId), new[] { inline }, "stored exactly as sent");
        // And no draft with that id exists, before or after.
        await ExpectAbsentAsync(MissingId, "the inline id");
    }

    [Fact]
    [Trait("Bug", "82773")]
    public async Task SendWithStream_DeviceFileUploadedAttachedByIdAndCarriedOnTheMessage_ReachesTheModel()
    {
        // The whole path in one test: the file is a real DocSpace file in the AI room's Chat outputs, the attachment is
        // created the way the product creates it — by reference, with the server doing the extraction — and the code word
        // reaches the model. Fixed 2026-08-20, reopened 2026-10-07. Kept as the end-to-end guard: the tests above build their
        // draft from text the test invented, so only this one covers the real upload -> attach-by-reference -> send chain.
        var provider = NewProvider();
        var profile = await CreateCapableProfileAsync(provider, ChatCapabilities);
        var roomId = await CreateAiRoomAsync("Autotest End To End Room");
        var chatOutputsId = await RoomSubfolderIdAsync(Owner, roomId, FolderType.ChatOutputs);
        var marker = NewMarker();
        var uploaded = await UploadTextAsync(Owner, chatOutputsId.ToString(), $"autotest-device-{Suffix()}.txt", $"The code word is {marker}. Nothing else matters.");

        await ActAsAsync(Owner);
        var attached = await AttachByIdAsync(uploaded.Id, uploaded.Title, entityId: roomId.ToString());
        attached.Status.Should().Be(200, attached.Error);
        // The draft really carries the file's text, so a model that does not see the code word was not given something empty.
        attached.Data!.Content.Should().Contain(marker);
        attached.Data.Path.Should().Be($"{uploaded.Id}/{uploaded.Title}");

        var thread = await CreateThreadAsync("Autotest device upload thread", profile.Id, roomId.ToString());
        var round = await SendWithAttachmentsAsync(
            thread.ThreadId,
            profile.Id,
            "The attached file contains one code word. Reply with that code word and nothing else.",
            Attach(new { id = attached.Data.Id }),
            roomId.ToString());

        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull($"{round}\n{provider.Describe()}");
        ExpectSameJson(await StoredUserAttachmentsAsync(thread.ThreadId), new[] { new { id = attached.Data.Id } }, "the message carries the attachment");
        StreamedRequestsCarrying(provider, marker).Should().NotBeEmpty($"the uploaded file's text reached the model\n{provider.Describe()}");
    }
}
