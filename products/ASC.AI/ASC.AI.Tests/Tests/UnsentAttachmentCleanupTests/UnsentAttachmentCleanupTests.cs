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

namespace ASC.AI.Tests.Tests.UnsentAttachmentCleanupTests;

[Trait("Category", "Storage")]
[Trait("Feature", "AI/Attachments")]
public class UnsentAttachmentCleanupTests(AspireAppFixture fixture) : ThreadFilesTestBase(fixture)
{
    private const decimal DocumentType = 7m;

    private static readonly TimeSpan _pastLifetime = TimeSpan.FromDays(1);
    private static readonly TimeSpan _beyondRetryWindow = TimeSpan.FromDays(40);

    private User? _actor;

    private sealed record RemovalState(AiAttachment? Draft, int FileStatus, long UsedSpace);

    [Fact]
    public async Task Delete_DraftOfAFileInAProject_RemovesTheFileAndFreesTheProjectSpace()
    {
        var projectId = await CreateProjectAsync();
        var member = await InviteMemberAsync(projectId);
        await ActAsAsync(member);

        var spaceBefore = await ProjectSpaceAsync(projectId);
        var file = await UploadAsync(projectId, "notes.txt");
        var spaceWithFile = await ProjectSpaceAsync(projectId);
        var draft = await AttachStoredFileAsync(file.Id);

        (await _aiAttachments.AiAttachmentsDeleteAsync(draft.Id, TestContext.Current.CancellationToken)).Success.Should().BeTrue();

        var state = await WaitForRemovalAsync(projectId, draft.Id, file.Id, 404, spaceBefore);

        spaceWithFile.Should().BeGreaterThan(spaceBefore);
        state.Draft.Should().BeNull();
        state.FileStatus.Should().Be(404);
        state.UsedSpace.Should().Be(spaceBefore);
    }

    [Fact]
    public async Task SaveFilesMany_DeletedDraftOfAProjectFileAttachedAgain_Answers404AndStoresNothing()
    {
        var projectId = await CreateProjectAsync();
        var member = await InviteMemberAsync(projectId);
        await ActAsAsync(member);

        var spaceBefore = await ProjectSpaceAsync(projectId);
        var file = await UploadAsync(projectId, "notes.txt");
        var other = await UploadAsync(await MyDocumentsIdAsync(), "other.txt");
        var draft = await AttachStoredFileAsync(file.Id);

        (await _aiAttachments.AiAttachmentsDeleteAsync(draft.Id, TestContext.Current.CancellationToken)).Success.Should().BeTrue();

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _aiAttachments.AiAttachmentsSaveFilesManyAsync(
            new AiAttachmentsSaveFilesManyRequest(
            [
                new AiAttachmentsSaveFileRequestInput(file.Id.ToString(), string.Empty, DocumentType),
                new AiAttachmentsSaveFileRequestInput(other.Id.ToString(), string.Empty, DocumentType)
            ]),
            TestContext.Current.CancellationToken));

        var otherDrafts = await CountDraftsAsync(member.Id, other.Id);
        var state = await WaitForRemovalAsync(projectId, draft.Id, file.Id, 404, spaceBefore);

        exception.ErrorCode.Should().Be(404);
        otherDrafts.Should().Be(0);
        state.Draft.Should().BeNull();
        state.FileStatus.Should().Be(404);
        state.UsedSpace.Should().Be(spaceBefore);
    }

    [Fact]
    public async Task Cleanup_AbandonedDraftOfAFileInAProject_RemovesTheFileAndFreesTheProjectSpace()
    {
        var projectId = await CreateProjectAsync();
        var member = await InviteMemberAsync(projectId);
        await ActAsAsync(member);

        var spaceBefore = await ProjectSpaceAsync(projectId);
        var file = await UploadAsync(projectId, "notes.txt");
        var spaceWithFile = await ProjectSpaceAsync(projectId);
        var draft = await AttachStoredFileAsync(file.Id);

        await AgeDraftAsync(draft.Id, _pastLifetime);

        var state = await WaitForRemovalAsync(projectId, draft.Id, file.Id, 404, spaceBefore);

        spaceWithFile.Should().BeGreaterThan(spaceBefore);
        state.Draft.Should().BeNull();
        state.FileStatus.Should().Be(404);
        state.UsedSpace.Should().Be(spaceBefore);
    }

    [Fact]
    public async Task Cleanup_AbandonedDraftOfAFileInMyDocuments_LeavesTheFile()
    {
        await ActAsAsync(Owner);
        var file = await UploadAsync(await MyDocumentsIdAsync(), "notes.txt");
        var draft = await AttachStoredFileAsync(file.Id);

        await AgeDraftAsync(draft.Id, _pastLifetime);

        var gone = await WaitForDraftGoneAsync(draft.Id);
        var fileStatus = await StatusOfAsync(() => FilesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        gone.Should().BeTrue("the abandoned draft is removed by the cleanup");
        fileStatus.Should().Be(200);
    }

    [Fact]
    public async Task Cleanup_SentAttachmentOlderThanTheLifetime_StaysWithItsThread()
    {
        var projectId = await CreateProjectAsync();
        var member = await InviteMemberAsync(projectId);
        await ActAsAsync(member);

        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var message = await CreateMessageInternalAsync(thread.Id);
        var sent = await UploadAsync(projectId, "sent.txt");
        var sentDraft = await AttachStoredFileAsync(sent.Id);
        var control = await AttachStoredFileAsync((await UploadAsync(projectId, "control.txt")).Id);

        (await _aiAttachments.AiAttachmentsLinkToMessageAsync(
            new AiAttachmentsLinkToMessageRequest([sentDraft.Id], message.Id.ToString(), thread.Id.ToString()),
            TestContext.Current.CancellationToken)).Success.Should().BeTrue();

        await AgeDraftAsync(sentDraft.Id, _pastLifetime);
        await AgeDraftAsync(control.Id, _pastLifetime);

        var controlGone = await WaitForDraftGoneAsync(control.Id);
        var page = await ListAttachmentsAsync(projectId, thread.Id);

        controlGone.Should().BeTrue("the aged control draft shows that a cleanup pass ran");
        page.Files.Should().ContainSingle().Which.Id.Should().Be(sent.Id);
    }

    [Fact]
    public async Task Cleanup_DraftsBeyondTheRetryWindow_AreLeftAlone()
    {
        await ActAsAsync(Owner);
        var folderId = await MyDocumentsIdAsync();
        var deletedLongAgo = await AttachStoredFileAsync((await UploadAsync(folderId, "deleted-long-ago.txt")).Id);
        var abandonedLongAgo = await AttachStoredFileAsync((await UploadAsync(folderId, "abandoned-long-ago.txt")).Id);
        var control = await AttachStoredFileAsync((await UploadAsync(folderId, "control.txt")).Id);

        await MarkDraftDeletedAsync(deletedLongAgo.Id, _beyondRetryWindow);
        await AgeDraftAsync(abandonedLongAgo.Id, _beyondRetryWindow);
        await AgeDraftAsync(control.Id, _pastLifetime);

        var controlGone = await WaitForDraftGoneAsync(control.Id);
        var deletedKept = await ReadDraftAsync(deletedLongAgo.Id);
        var abandonedKept = await ReadDraftAsync(abandonedLongAgo.Id);

        controlGone.Should().BeTrue("the aged control draft shows that a cleanup pass ran");
        deletedKept.Should().NotBeNull();
        abandonedKept.Should().NotBeNull();
    }

    private async Task ActAsAsync(User user)
    {
        _actor = user;

        await FilesHttpClient.Authenticate(user);
        await _aiClient.Authenticate(user);
        await _aiChatClient.Authenticate(user);
    }

    private async Task<long> ProjectSpaceAsync(int projectId)
    {
        await FilesHttpClient.Authenticate(Owner);

        try
        {
            return await UsedSpaceAsync(projectId);
        }
        finally
        {
            await FilesHttpClient.Authenticate(_actor ?? Owner);
        }
    }

    private async Task<AiAttachment> AttachStoredFileAsync(int fileId)
    {
        var saved = await _aiAttachments.AiAttachmentsSaveFilesManyAsync(
            new AiAttachmentsSaveFilesManyRequest([new AiAttachmentsSaveFileRequestInput(fileId.ToString(), string.Empty, DocumentType)]),
            TestContext.Current.CancellationToken);

        return saved.Should().ContainSingle().Subject;
    }

    private async Task<long> CountDraftsAsync(Guid userId, int fileId)
    {
        await using var db = await CreateDbContextAsync<AiIntegrationContext>();

        return await db.Attachments.LongCountAsync(attachment => attachment.CreatedBy == userId && attachment.EntryId == fileId, TestContext.Current.CancellationToken);
    }

    private async Task<AiAttachment?> ReadDraftAsync(string id)
    {
        var read = await _aiAttachments.AiAttachmentsGetManyAsync([id], TestContext.Current.CancellationToken);

        return read is { Count: > 0 } ? read[0] : null;
    }

    private async Task<bool> WaitForDraftGoneAsync(string id)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);

        while (true)
        {
            if (await ReadDraftAsync(id) is null)
            {
                return true;
            }

            if (DateTime.UtcNow >= deadline)
            {
                return false;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    private async Task<RemovalState> WaitForRemovalAsync(int projectId, string draftId, int fileId, int expectedFileStatus, long expectedSpace)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);

        while (true)
        {
            var state = new RemovalState(
                await ReadDraftAsync(draftId),
                await StatusOfAsync(() => FilesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)),
                await ProjectSpaceAsync(projectId));

            if ((state.Draft is null && state.FileStatus == expectedFileStatus && state.UsedSpace == expectedSpace) || DateTime.UtcNow >= deadline)
            {
                return state;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}
