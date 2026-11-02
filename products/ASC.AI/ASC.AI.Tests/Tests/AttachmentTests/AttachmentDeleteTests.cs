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
/// <c>DELETE /api/2.0/ai/attachments/delete</c> and <c>delete-many</c>. An attachment is a reference to a stored file, not
/// a copy of it, so removing the reference never touches the file; the routes report success for an id that does not
/// exist, so a caller cannot tell "removed" from "never existed".
/// </summary>
[Trait("Category", "Attachments")]
[Trait("Feature", "AI/Attachments")]
public class AttachmentDeleteTests(AspireAppFixture fixture) : AttachmentTestBase(fixture)
{
    [Fact]
    public async Task Delete_FileDraft_ReportsSuccess()
    {
        var id = await SaveDraftAsync(Owner, "Autotest delete.docx");
        await ExpectStoredAsync(id, "draft before deletion");

        var deleted = await DeleteAsync(id);

        deleted.Status.Should().Be(200, deleted.Error);
        deleted.Data!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ImageDraft_WithABareJsonStringLiteralBody_ReportsSuccess()
    {
        var id = await SaveImageDraftAsync(Owner, "delete.png");
        await ExpectStoredAsync(id, "image draft before deletion");

        // The string-literal body is the shape the SDK documents (the typed call sends it); `{ id }` works too.
        var deleted = await DeleteAsync(id);

        deleted.Status.Should().Be(200, deleted.Error);
        deleted.Data!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_TwiceAndAnUnknownId_BothReportSuccess()
    {
        // Idempotent as far as the response goes: there is no 404, so a caller cannot tell "removed" from "never existed".
        var id = await SaveDraftAsync(Owner, "Autotest twice.docx");

        var first = await DeleteAsync(id);
        var second = await DeleteAsync(id);
        var unknown = await DeleteAsync(MissingId);

        new[] { first.Status, second.Status, unknown.Status }.Should().OnlyContain(status => status == 200);
        new[] { first.Data!.Success, second.Data!.Success, unknown.Data!.Success }.Should().OnlyContain(success => success);
    }

    [Fact]
    public async Task Delete_Draft_LeavesTheDocSpaceFileItWasMadeFrom()
    {
        // An attachment is a reference to a stored file, not a copy of it, so removing the reference must not touch the file.
        var fileId = await BackingFileIdAsync(Owner, "Autotest delete-source.docx", "keep me");
        await ActAsAsync(Owner);
        var saved = await AttachByIdAsync(fileId);
        saved.Status.Should().Be(200, saved.Error);
        var id = saved.Data!.Id;
        await ExpectStoredAsync(id, "draft before deletion");

        (await DeleteAsync(id)).Status.Should().Be(200);
        await ExpectAbsentAsync(id, "the deleted draft");

        var file = await FilesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken);
        file.Response.Id.Should().Be(fileId, "the source file after its draft is gone");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("""{"id":"not-a-uuid"}""")]
    [InlineData("""["not-a-uuid"]""")]
    public async Task Delete_BodyWithNoUsableId_IsRejectedAndNothingIsDeleted(string? body)
    {
        // The route reads an id out of whatever JSON it is given and only 400s when no value in the body is a uuid.
        // Raw: no body at all, and a wrongly shaped one.
        var id = await SaveDraftAsync(Owner, "Autotest survivor.docx");

        var refused = await SendRawAsync(HttpMethod.Delete, $"{AttachmentsPath}/delete", body);

        refused.Status.Should().Be(400, refused.Body);
        await ExpectStoredAsync(id, "the draft after a rejected delete");
    }

    [Fact]
    [Trait("Bug", "83289")]
    public async Task DeleteMany_AnUnknownIdInTheBatch_DoesNotStopTheRealOnesBeingDeleted()
    {
        // The atomicity question for delete-many, in the one form that is answerable: does one id the server cannot find
        // abort the batch? It does not — both real ids end up gone.
        var fileId = await SaveDraftAsync(Owner, "Autotest batch-delete.docx");
        var imageId = await SaveImageDraftAsync(Owner, "batch-delete.png");
        await ExpectStoredAsync(fileId, "file draft");
        await ExpectStoredAsync(imageId, "image draft");

        var deleted = await DeleteManyAsync(fileId, MissingId, imageId);

        deleted.Status.Should().Be(200, deleted.Error);
        deleted.Data!.Success.Should().BeTrue();
        await ExpectAbsentAsync(fileId, "deleted file draft");
        await ExpectAbsentAsync(imageId, "deleted image draft");
    }

    [Fact]
    public async Task Delete_OneDraftOutOfAComposersSet_LeavesTheOthersAlone()
    {
        // What the cross on one preview does: five staged drafts, one removed, and the remaining four have to still be
        // readable — so a delete reaching further than its id shows up.
        var drafts = await StageComposerDraftsAsync(files: 3, images: 2, tag: "one-of-many");

        foreach (var draft in drafts)
        {
            await ExpectStoredAsync(draft.Id, "staged draft");
        }

        // A file draft from the middle of the set, so an off-by-one in either direction shows up below.
        var removed = drafts[1].Id;
        (await DeleteAsync(removed)).Status.Should().Be(200);
        await ExpectAbsentAsync(removed, "the removed draft");

        foreach (var draft in drafts.Where(candidate => candidate.Id != removed))
        {
            await ExpectStoredAsync(draft.Id, "draft left in the composer");
        }
    }

    [Fact]
    public async Task DeleteMany_Drafts_LeavesTheDocSpaceFilesTheyWereMadeFrom()
    {
        string[] names = ["Autotest many-source-1.docx", "Autotest many-source-2.docx"];
        var fileIds = new List<int>();
        var ids = new List<string>();

        foreach (var name in names)
        {
            var fileId = await BackingFileIdAsync(Owner, name, name);
            await ActAsAsync(Owner);
            var saved = await AttachByIdAsync(fileId);
            saved.Status.Should().Be(200, saved.Error);
            fileIds.Add(fileId);
            ids.Add(saved.Data!.Id);
        }

        foreach (var id in ids)
        {
            await ExpectStoredAsync(id, "draft before deletion");
        }

        (await DeleteManyAsync([.. ids])).Status.Should().Be(200);

        foreach (var id in ids)
        {
            await ExpectAbsentAsync(id, "the deleted draft");
        }

        foreach (var fileId in fileIds)
        {
            var file = await FilesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken);
            file.Response.Id.Should().Be(fileId, $"source file {fileId} after its draft is gone");
        }
    }

    [Theory]
    [InlineData("[]", 200)]
    [InlineData("null", 200)]
    [InlineData("""{"ids":["3fa85f64-5717-4562-b3fc-2c963f66afa6"]}""", 200)]
    [InlineData("""["not-a-uuid"]""", 400)]
    [InlineData("""{"ids":["not-a-uuid"]}""", 400)]
    [InlineData("\"not-a-uuid\"", 400)]
    public async Task DeleteMany_AcceptsSeveralBodyShapes_AndRejectsOnlyUnusableIds(string body, int expectedStatus)
    {
        // Raw: a null body, a wrapped list and a bare string are shapes the typed list of strings cannot send.
        var outcome = await SendRawAsync(HttpMethod.Delete, $"{AttachmentsPath}/delete-many", body);

        outcome.Status.Should().Be(expectedStatus, outcome.Body);
    }
}
