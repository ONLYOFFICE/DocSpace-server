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
/// Cross-user access to drafts. An attachment id is not a bearer token: a draft belonging to someone else resolves on
/// neither <c>get</c> nor <c>get-many</c> (a refused id reads exactly like an unknown one), and a delete of somebody else's
/// draft answers <c>200 {"success":true}</c> without deleting it — from the caller's side a draft it may not see is a draft
/// that does not exist. A Guest is handled by a different mechanism and both halves of it are the intended contract: the read
/// routes answer a Guest 403 for any id, an unknown one included.
/// </summary>
/// <remarks>
/// Every test that concludes "the other user got nothing" carries a positive control that the caller can read, or delete, a
/// draft of its own in the same portal: without it a dead endpoint or a lost session produces the same nulls as a working
/// access check. Each client signs in as its own identity, so unlike the TypeScript suite (one shared session cookie) no
/// test can silently act as the wrong user; the owner's setup still happens before the member exists.
/// </remarks>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Attachments")]
public class AttachmentCrossUserTests(AspireAppFixture fixture) : AttachmentTestBase(fixture)
{
    private const int Rounds = 3;

    [Fact]
    [Trait("Bug", "82765")]
    public async Task GetMany_User_CannotReadAnotherUsersFileDraft()
    {
        var secret = $"OWNER-SECRET-{Suffix()}";
        var ownerDraft = await SaveDraftAsync(Owner, "Autotest owner-secret.docx", secret);
        var member = await AddMemberAsync(EmployeeType.User);

        // Positive control first: the member's own draft resolves through the very same call.
        var memberDraft = await SaveDraftAsync(member, "Autotest member-own.docx", "the caller's own");
        (await ExpectStoredAsync(memberDraft, "the member's own draft")).Content.Should().Be("the caller's own", "the member reads its own payload");

        for (var attempt = 0; attempt < Rounds; attempt++)
        {
            var leaked = await ReadOnceAsync(ownerDraft);

            leaked.Should().BeNull($"another user's draft must not be readable (attempt {attempt + 1}); it leaked {leaked?.Content}");
        }
    }

    [Fact]
    [Trait("Bug", "82768")]
    public async Task Delete_User_CannotDeleteAnotherUsersDraft()
    {
        var ownerDraft = await SaveDraftAsync(Owner, "Autotest owner-victim.docx");
        await ExpectStoredAsync(ownerDraft, "the owner's draft before the member exists");
        var member = await AddMemberAsync(EmployeeType.User);
        await ActAsAsync(member);

        var statuses = new List<int>();

        for (var round = 0; round < Rounds; round++)
        {
            statuses.Add((await DeleteAsync(ownerDraft)).Status);
        }

        // The refusal is silent — `200 {"success":true}`, the same answer an unknown id gets. Pinned because a client
        // cannot tell the two apart, and because a future 403 here would be a change worth noticing.
        statuses.Should().OnlyContain(status => status == 200, "a refused delete still reports success on every round");

        // Positive control: the member's delete does remove records, so the survivor below is a refusal and not a dead route.
        var memberDraft = await SaveDraftAsync(member, "Autotest member-victim.docx");
        await ExpectStoredAsync(memberDraft, "the member's own draft");
        (await DeleteAsync(memberDraft)).Status.Should().Be(200);
        await ExpectAbsentAsync(memberDraft, "the member's own draft after its own delete");

        await ActAsAsync(Owner);
        var survivor = await FindAsync(ownerDraft);

        survivor.Should().NotBeNull($"the draft must survive deletion by another user (delete answered {string.Join(",", statuses)})");
    }

    [Fact]
    [Trait("Bug", "82765")]
    public async Task GetMany_Guest_CannotReadThePortalOwnersFileDraft()
    {
        // The sharpest form of the leak this closed: a Guest is the least privileged principal there is, holds Read on a
        // single AI room, and used to resolve the owner's draft through it. How it is closed for a Guest is not how it is
        // closed for a User: the read routes answer a Guest 403 for ANY id, an id that exists nowhere included, while the
        // owner gets `200 [null]` for the same unknown id. It is a gate on the user type, not a decision about this draft.
        var roomId = await CreateAiRoomAsync();
        var secret = $"OWNER-SECRET-{Suffix()}";
        var ownerDraft = await SaveDraftAsync(Owner, "Autotest guest-target.docx", secret, roomId.ToString());

        var guest = await AddMemberAsync(EmployeeType.Guest);
        // Read is the highest access an AI room grants a Guest.
        await ShareRoomAsync(roomId, guest, DocSpace.API.SDK.Model.FileShare.Read);
        await ActAsAsync(guest);

        var asked = await GetManyAsync(ownerDraft);
        asked.Status.Should().Be(403, "a Guest asking for the owner's draft");
        asked.Data.Should().BeNull();
        var askedUnknown = await GetManyAsync(MissingId);
        askedUnknown.Status.Should().Be(403, "a Guest asking for an id that exists nowhere — same answer");

        // Positive control: the draft is alive and readable for its owner at this very moment, so the Guest's refusal is
        // not a draft that was never stored. Measured as the owner deliberately — a Guest cannot create a draft of their
        // own to control with.
        await ActAsAsync(Owner);
        (await ExpectStoredAsync(ownerDraft, "the owner's draft, read by the owner")).Content.Should().Be(secret);
    }

    [Fact]
    [Trait("Bug", "82774")]
    public async Task LinkToMessage_CallersAccessToTheTargetThreadIsChecked()
    {
        // Scope: the authorization decision only, because nothing is ever actually linked — there is no write to observe
        // either way. A caller with no access to the thread is refused, which used to be an unguarded 200. 403 rather than
        // 404 on purpose: an id that exists nowhere answers 404, so only the "exists, but not yours" case is the subject.
        await GetDefaultProfileAsync();
        var roomId = await CreateAiRoomAsync();
        var (threadId, messageId) = await ThreadWithMessageAsync("Autotest owner-only thread", roomId.ToString());
        var member = await AddMemberAsync(EmployeeType.User);
        await ActAsAsync(member);

        // Deliberately NOT invited to the room: the member cannot see the thread.
        var uninvited = await StatusOfAsync(async () => await _aiThreads.AiThreadsGetByIdAsync(threadId, TestContext.Current.CancellationToken));
        uninvited.Should().NotBe(200, "the member must not be able to read the thread it is about to link into");

        var memberDraft = await SaveDraftAsync(member, "Autotest intruder.docx");

        var linked = await LinkAsync([memberDraft], messageId, threadId);

        linked.Status.Should().Be(403, "linking into a thread the caller cannot read");
    }

    [Fact]
    [Trait("Bug", "82758")]
    public async Task Get_User_CannotReadAnotherUsersImageDraftEither()
    {
        // BUG 82758: an image draft leaked cross-user on `get` AND `get-many` while the file-draft leak was already closed.
        // That distinction is moot now that a picture is attached like any other file (upload, then save-file by id) and
        // inherits the isolation of a file draft; kept as its own test because "does an image-kind record get the same
        // isolation" is still worth pinning.
        var ownerDraft = await SaveImageDraftAsync(Owner, "owner-private.png");
        var member = await AddMemberAsync(EmployeeType.User);

        // Positive control: the member can read a draft of its own, so a null below is an access decision.
        var memberDraft = await SaveImageDraftAsync(member, "member-own.png");
        await ExpectStoredAsync(memberDraft, "the member's own image draft");

        var viaGet = await GetAsync(ownerDraft);
        var viaGetMany = await ReadOnceAsync(ownerDraft);

        viaGet.Status.Should().Be(200);
        viaGet.Data.Should().BeNull("another user's image draft must not be readable through get");
        viaGetMany.Should().BeNull("another user's image draft must not be readable through get-many");
    }

    [Fact]
    [Trait("Bug", "82765")]
    public async Task GetMany_MixedBatch_ReturnsTheCallersOwnDraftAndNullForAnotherUsers()
    {
        // The batch form, and the shape a real client would hit: the caller asks for its own id and someone else's in one
        // call. It used to get both; it now gets its own and a null in place, the batch still 200 with both positions kept —
        // a refused id is reported exactly like an unknown one.
        var secret = $"OWNER-SECRET-{Suffix()}";
        var ownerDraft = await SaveDraftAsync(Owner, "Autotest mixed-owner.docx", secret);
        var member = await AddMemberAsync(EmployeeType.User);
        var memberDraft = await SaveDraftAsync(member, "Autotest mixed-member.docx", "the caller's own");

        for (var attempt = 0; attempt < Rounds; attempt++)
        {
            var read = await GetManyAsync(memberDraft, ownerDraft);

            read.Status.Should().Be(200, read.Error);
            read.Data.Should().HaveCount(2, "the batch keeps both positions");
            read.Data![0].Should().NotBeNull("the caller's own draft resolves, so a null next to it is a decision about the id");
            read.Data[1].Should().BeNull($"the other user's draft must not come back (it carried {read.Data[1]?.Content})");
        }
    }

    [Fact]
    [Trait("Bug", "82769")]
    public async Task DeleteMany_MixedBatch_DeletesOnlyTheCallersOwnDraft()
    {
        // delete-many is a separate route from delete, and this is the form that used to do real damage: one batch
        // containing somebody else's id. The batch is still accepted whole and reports success — what changed is that the
        // foreign id in it is a no-op while the caller's own id in the same batch is deleted (the positive control).
        var ownerDraft = await SaveDraftAsync(Owner, "Autotest batch-victim.docx");
        await ExpectStoredAsync(ownerDraft, "the owner's draft before the member exists");
        var member = await AddMemberAsync(EmployeeType.User);
        var memberDraft = await SaveDraftAsync(member, "Autotest batch-own.docx");

        var statuses = new List<int>();

        for (var round = 0; round < Rounds; round++)
        {
            statuses.Add((await DeleteManyAsync(memberDraft, ownerDraft)).Status);
        }

        statuses.Should().OnlyContain(status => status == 200, "the mixed batch is accepted on every round");
        await ExpectAbsentAsync(memberDraft, "the caller's own draft in the mixed batch");

        await ActAsAsync(Owner);
        var survivor = await FindAsync(ownerDraft);

        survivor.Should().NotBeNull($"the draft must survive another user's batch delete (which answered {string.Join(",", statuses)})");
    }

    [Fact]
    public async Task SaveFilesMany_BatchHoldingAFileTheCallerCannotRead_Is403AndReturnsNoneOfIt()
    {
        // Status and leak only: a failed batch returns no ids, so whether the caller's own element was stored anyway
        // cannot be observed. Both orders, so the refusal does not depend on where the forbidden element sits.
        var secret = $"OWNER-SECRET-{Suffix()}";
        var privateFile = await UploadTextAsync(Owner, "@my", "autotest-batch-private.txt", $"Confidential. {secret}");
        var member = await AddMemberAsync(EmployeeType.User);
        await ActAsAsync(member);

        // Control: the member really has no access to the owner's file.
        var direct = await Assert.ThrowsAsync<ApiException>(
            async () => await FilesApi.GetFileInfoAsync(privateFile.Id, cancellationToken: TestContext.Current.CancellationToken));
        direct.ErrorCode.Should().Be(403, "the member cannot read the owner's file directly");

        var ownFile = await UploadTextAsync(member, "@my", "autotest-batch-own.txt", "the member's own file");
        await ActAsAsync(member);

        // Positive control: the member's own element is accepted on its own.
        (await SaveFilesManyAsync([ownFile.Id])).Status.Should().Be(200, "the member's own file on its own");

        var forbiddenLast = await SaveFilesManyAsync([ownFile.Id, privateFile.Id]);
        var forbiddenFirst = await SaveFilesManyAsync([privateFile.Id, ownFile.Id]);

        forbiddenLast.Status.Should().Be(403);
        forbiddenFirst.Status.Should().Be(403);
        JsonSerializer.Serialize(new[] { forbiddenLast, forbiddenFirst }).Should().NotContain(secret, "and no part of the forbidden file came back");
    }

    [Fact]
    public async Task LinkToMessage_User_CannotLinkAnotherUsersDraftToTheirOwnMessage()
    {
        // The thread is the member's own and readable by them, so what is measured is the attachment: it belongs to
        // somebody else and has to be refused. A link leaves no trace on the message or the record, so only the answer is
        // asserted.
        await GetDefaultProfileAsync();
        var ownerDraft = await SaveDraftAsync(Owner, "Autotest foreign-link.docx", "the owner's draft");
        await ExpectStoredAsync(ownerDraft, "the owner's draft");
        var member = await AddMemberAsync(EmployeeType.User);
        await ActAsAsync(member);

        var memberThread = await ThreadWithMessageAsync("Autotest member thread");
        var memberDraft = await SaveDraftAsync(member, "Autotest member-link.docx", "the member's draft");
        await ExpectStoredAsync(memberDraft, "the member's draft");

        // Positive control: the member's own draft goes into the member's own message, so the refusal is about whose draft it is.
        var control = await LinkAsync([memberDraft], memberThread.MessageId, memberThread.ThreadId);
        control.Status.Should().Be(200, $"the member's own draft into their own message: {control.Error}");

        var refused = await LinkAsync([ownerDraft], memberThread.MessageId, memberThread.ThreadId);

        refused.Status.Should().Be(404, "another user's draft");
    }
}
