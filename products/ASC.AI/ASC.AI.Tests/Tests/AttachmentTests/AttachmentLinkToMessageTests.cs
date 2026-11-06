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
/// <c>POST /api/2.0/ai/attachments/link-to-message</c>. Linking writes nothing onto the message or the record (by design,
/// BUG 82770 was closed as such): it marks the draft as the message's own, which shows only when the message or the thread
/// is deleted — a linked draft goes with it, an unlinked one stays. A link that cannot be made is refused with a status
/// that says why.
/// </summary>
[Trait("Category", "Attachments")]
[Trait("Feature", "AI/Attachments")]
public class AttachmentLinkToMessageTests(AspireAppFixture fixture) : AttachmentTestBase(fixture)
{
    private sealed record LinkTargets(string FirstThreadId, string FirstMessageId, string SecondThreadId, string DraftId);

    /// <summary>
    /// Two threads with one stored message each, and one draft linked to the first message once. Linking is the control
    /// for the refusals that follow: if the real attachment into the real message is not accepted, a refusal measured
    /// afterwards says nothing about the target.
    /// </summary>
    private async Task<LinkTargets> PrepareLinkTargetsAsync()
    {
        var first = await ThreadWithMessageAsync("Autotest Thread A");
        var second = await ThreadWithMessageAsync("Autotest Thread B");
        var id = await SaveDraftAsync(Owner, "Autotest target.docx");
        await ExpectStoredAsync(id, "draft before linking");

        var control = await LinkAsync([id], first.MessageId, first.ThreadId);
        control.Status.Should().Be(200, $"the attachment into its own message: {control.Error}");

        return new LinkTargets(first.ThreadId, first.MessageId, second.ThreadId, id);
    }

    [Fact]
    [Trait("Bug", "82770")]
    public async Task LinkToMessage_LinkedDraft_IsRemovedWithItsMessageAnUnlinkedOneIsNot()
    {
        var (threadId, messageId) = await ThreadWithMessageAsync();
        var linked = await SaveDraftAsync(Owner, "Autotest linkable.docx", "linkable payload");
        var loose = await SaveDraftAsync(Owner, "Autotest loose.docx", "loose payload");
        await ExpectStoredAsync(linked, "draft before linking");
        await ExpectStoredAsync(loose, "unlinked draft");

        var link = await LinkAsync([linked], messageId, threadId);
        link.Status.Should().Be(200, link.Error);
        link.Data!.Success.Should().BeTrue("link-to-message reports success");

        (await _aiThreads.AiThreadsDeleteMessageAsync(messageId, TestContext.Current.CancellationToken)).Success.Should().BeTrue();

        await ExpectAbsentAsync(linked, "linked draft");
        // Positive control: the same wait did not remove a draft that was not linked.
        await ExpectStoredAsync(loose, "unlinked draft");
    }

    [Fact]
    [Trait("Bug", "82770")]
    public async Task LinkToMessage_Linking_WritesNothingOntoTheMessageOrTheRecord()
    {
        // Pins the documented design so nobody reads the empty list as a failure: the message's attachments are written
        // when the message is created, not here.
        var (threadId, messageId) = await ThreadWithMessageAsync();
        var id = await SaveDraftAsync(Owner, "Autotest bound.docx");
        await ExpectStoredAsync(id, "draft before linking");

        var link = await LinkAsync([id], messageId, threadId);
        link.Status.Should().Be(200, link.Error);

        var stored = await ExpectStoredAsync(id, "draft after linking");
        stored.MessageId.Should().BeNull();
        stored.ThreadId.Should().BeNull();

        var message = (await ReadStoredMessagesAsync(threadId)).Find(m => Str(m, "id") == messageId);
        message.ValueKind.Should().Be(JsonValueKind.Object, "the message is readable");
        (message.TryGetProperty("attachments", out var attachments) ? attachments.GetArrayLength() : 0).Should().Be(0);
    }

    [Fact]
    [Trait("Bug", "82771")]
    public async Task LinkToMessage_MessageIdFromAnotherThread_Is400()
    {
        var targets = await PrepareLinkTargetsAsync();

        var refused = await LinkAsync([targets.DraftId], targets.FirstMessageId, targets.SecondThreadId);

        refused.Status.Should().Be(400);
    }

    [Fact]
    [Trait("Bug", "82771")]
    public async Task LinkToMessage_UnknownMessageId_Is404()
    {
        var targets = await PrepareLinkTargetsAsync();

        var refused = await LinkAsync([targets.DraftId], MissingId, targets.FirstThreadId);

        refused.Status.Should().Be(404);
    }

    [Fact]
    [Trait("Bug", "82771")]
    public async Task LinkToMessage_UnknownThreadIdWithARealMessage_Is400()
    {
        // 400 and not 404: the message exists, it just does not belong to that thread.
        var targets = await PrepareLinkTargetsAsync();

        var refused = await LinkAsync([targets.DraftId], targets.FirstMessageId, MissingId);

        refused.Status.Should().Be(400);
    }

    [Fact]
    [Trait("Bug", "82771")]
    public async Task LinkToMessage_UnknownAttachmentId_Is404()
    {
        var targets = await PrepareLinkTargetsAsync();

        var refused = await LinkAsync([MissingId], targets.FirstMessageId, targets.FirstThreadId);

        refused.Status.Should().Be(404);
    }

    [Fact]
    [Trait("Bug", "82771")]
    public async Task LinkToMessage_EmptyIdsArray_Is400()
    {
        var targets = await PrepareLinkTargetsAsync();

        var refused = await LinkAsync([], targets.FirstMessageId, targets.FirstThreadId);

        refused.Status.Should().Be(400);
    }

    [Fact]
    public async Task LinkToMessage_MalformedAttachmentId_Is400()
    {
        var targets = await PrepareLinkTargetsAsync();

        var refused = await LinkAsync(["not-a-uuid"], targets.FirstMessageId, targets.FirstThreadId);

        refused.Status.Should().Be(400);
    }

    [Fact]
    [Trait("Bug", "82771")]
    public async Task LinkToMessage_EmptyBody_IsRefused()
    {
        // All three used to answer 200 {success:true}. Raw: no body, an empty object and a null `ids` are bodies the SDK
        // request, which requires all three fields, cannot send.
        var noBody = await PostRawAsync("link-to-message", null);
        var emptyObject = await PostRawAsync("link-to-message", "{}");
        var nullIds = await PostRawAsync("link-to-message", Json(new { ids = (string[]?)null, messageId = MissingId, threadId = MissingId }));

        new[] { noBody.Status, emptyObject.Status, nullIds.Status }.Should().OnlyContain(status => status == 400);
        noBody.Error.Should().Be("ids (non-empty array), messageId (string) and threadId (string) are required");
    }

    [Fact]
    public async Task LinkToMessage_NonArrayIds_IsRejected()
    {
        // The type of `ids` is the one input checked before anything is looked up. Raw: the SDK types `ids` as a list.
        var refused = await PostRawAsync("link-to-message", Json(new { ids = "not-an-array", messageId = MissingId, threadId = MissingId }));

        refused.Status.Should().Be(400, refused.Body);
    }

    [Fact]
    public async Task LinkToMessage_DraftsLinkedInOneCall_AreRemovedWithTheThreadAnUnlinkedOneIsNot()
    {
        var targets = await PrepareLinkTargetsAsync();
        var second = await SaveDraftAsync(Owner, "Autotest multi-2.docx", "two");
        var loose = await SaveDraftAsync(Owner, "Autotest loose.docx", "loose");
        await ExpectStoredAsync(second, "second draft");
        await ExpectStoredAsync(loose, "unlinked draft");

        var link = await LinkAsync([targets.DraftId, second], targets.FirstMessageId, targets.FirstThreadId);
        link.Status.Should().Be(200, link.Error);

        (await _aiThreads.AiThreadsDeleteAsync(targets.FirstThreadId, TestContext.Current.CancellationToken)).Success.Should().BeTrue();

        await ExpectAbsentAsync(targets.DraftId, "first linked draft");
        await ExpectAbsentAsync(second, "second linked draft");
        await ExpectStoredAsync(loose, "unlinked draft");
    }

    [Fact]
    public async Task LinkToMessage_TheSameDraftTwice_IsAcceptedAndStillRemovedWithTheMessage()
    {
        var targets = await PrepareLinkTargetsAsync();

        // `PrepareLinkTargetsAsync` already linked the draft once; this is the second link.
        var link = await LinkAsync([targets.DraftId], targets.FirstMessageId, targets.FirstThreadId);
        link.Status.Should().Be(200, link.Error);

        (await _aiThreads.AiThreadsDeleteMessageAsync(targets.FirstMessageId, TestContext.Current.CancellationToken)).Success.Should().BeTrue();

        await ExpectAbsentAsync(targets.DraftId, "twice-linked draft");
    }
}
