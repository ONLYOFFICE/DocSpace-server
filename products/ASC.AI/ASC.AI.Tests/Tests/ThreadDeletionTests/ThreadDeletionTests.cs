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

namespace ASC.AI.Tests.Tests.ThreadDeletionTests;

[Trait("Category", "Storage")]
[Trait("Feature", "AI/ThreadDeletion")]
public class ThreadDeletionTests(AspireAppFixture fixture) : ThreadFilesTestBase(fixture)
{
    private sealed record CleanupState(int ArtifactStatus, int AttachmentStatus, long UsedSpace);

    [Fact]
    public async Task Delete_ProjectThread_LeavesTheListAndTheSearchAtOnce()
    {
        var projectId = await CreateProjectAsync();
        var title = $"autotest-deleted-{Guid.NewGuid():N}";
        var thread = await CreateThreadInternalAsync(title, entityId: projectId.ToString());
        await UploadArtifactAsync(thread.Id, "report.txt");

        using var deleted = await DeleteThreadAsync(thread.Id);

        var listed = await ReadAllThreadsInternalAsync(projectId.ToString());
        var found = await ReadThreadsPageInternalAsync(DefaultPageCount, projectId.ToString(), query: title);
        using var read = await _ai.GetAsync($"{ThreadsPath}/{thread.Id}", TestContext.Current.CancellationToken);

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        listed.Should().NotContain(t => t.Id == thread.Id);
        found.Items.Should().BeEmpty();
        read.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ProjectThread_ClosesItsMessagesAndFilesAtOnce()
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var message = await CreateMessageInternalAsync(thread.Id);
        await UploadArtifactAsync(thread.Id, "report.txt");

        using var deleted = await DeleteThreadAsync(thread.Id);

        using var renamed = await _ai.PutAsync($"{ThreadsPath}/{thread.Id}", new { title = "renamed" }, TestContext.Current.CancellationToken);
        using var readMessage = await _ai.GetAsync($"{MessagesPath}/{message.Id}", TestContext.Current.CancellationToken);
        using var appended = await _ai.PostAsync(
            $"{ThreadsPath}/{thread.Id}/messages",
            new { contents = BuildMessageContents() },
            TestContext.Current.CancellationToken);
        using var uploaded = await PostArtifactAsync(thread.Id, "late.txt");
        using var artifacts = await GetArtifactsAsync(projectId, thread.Id);
        using var attachments = await GetAttachmentsAsync(projectId, thread.Id);

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        renamed.StatusCode.Should().Be(HttpStatusCode.NotFound);
        readMessage.StatusCode.Should().Be(HttpStatusCode.NotFound);
        appended.StatusCode.Should().Be(HttpStatusCode.NotFound);
        uploaded.StatusCode.Should().Be(HttpStatusCode.NotFound);
        artifacts.StatusCode.Should().Be(HttpStatusCode.NotFound);
        attachments.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ProjectThread_CleanerDeletesItsFilesAndFreesTheProjectSpace()
    {
        var projectId = await CreateProjectAsync();
        var spaceBefore = await UsedSpaceAsync(projectId);
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var artifact = await UploadArtifactAsync(thread.Id, "report.txt");
        var attachment = await SendAsync(projectId, thread.Id, "notes.txt");
        var spaceWithFiles = await UsedSpaceAsync(projectId);

        using var deleted = await DeleteThreadAsync(thread.Id);

        var state = await WaitForCleanupAsync(projectId, artifact.Id, attachment.Id, spaceBefore);

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        spaceWithFiles.Should().BeGreaterThan(spaceBefore);
        state.ArtifactStatus.Should().Be(404);
        state.AttachmentStatus.Should().Be(404);
        state.UsedSpace.Should().Be(spaceBefore);
    }

    [Fact]
    public async Task Delete_ProjectThread_KeepsTheOtherThreadsOfTheProject()
    {
        var projectId = await CreateProjectAsync();
        var kept = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var keptArtifact = await UploadArtifactAsync(kept.Id, "kept.txt");
        var spaceAfterKept = await UsedSpaceAsync(projectId);
        var doomed = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var doomedArtifact = await UploadArtifactAsync(doomed.Id, "doomed.txt");
        var doomedAttachment = await SendAsync(projectId, doomed.Id, "doomed-notes.txt");

        using var deleted = await DeleteThreadAsync(doomed.Id);

        var state = await WaitForCleanupAsync(projectId, doomedArtifact.Id, doomedAttachment.Id, spaceAfterKept);
        var keptPage = await ListArtifactsAsync(projectId, kept.Id);
        var keptThread = await ReadThreadInternalAsync(kept.Id);

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        state.ArtifactStatus.Should().Be(404);
        state.UsedSpace.Should().Be(spaceAfterKept);
        keptThread.Id.Should().Be(kept.Id);
        keptPage.Files.Should().ContainSingle().Which.Id.Should().Be(keptArtifact.Id);
    }

    [Fact]
    public async Task DeleteProject_WithADeletedThread_Finishes()
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        await UploadArtifactAsync(thread.Id, "report.txt");
        using var deleted = await DeleteThreadAsync(thread.Id);

        var deletion = (await RoomsApi.DeleteRoomAsync(
            projectId,
            new DeleteRoomRequest(deleteAfter: false),
            TestContext.Current.CancellationToken)).Response;
        var statuses = await WaitForOperationAsync(deletion.Id);

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        statuses.Should().OnlyContain(s => s.Finished && string.IsNullOrEmpty(s.Error));
    }

    private Task<HttpResponseMessage> DeleteThreadAsync(Guid threadId)
    {
        return _ai.DeleteAsync($"{ThreadsPath}/{threadId}", TestContext.Current.CancellationToken);
    }

    private async Task<long> UsedSpaceAsync(int projectId)
    {
        var room = (await RoomsApi.GetRoomInfoAsync(projectId, TestContext.Current.CancellationToken)).Response;

        return room.UsedSpace ?? 0;
    }

    private async Task<CleanupState> WaitForCleanupAsync(int projectId, int artifactId, int attachmentId, long expectedSpace)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var state = new CleanupState(
                await StatusOfAsync(() => FilesApi.GetFileInfoAsync(artifactId, cancellationToken: TestContext.Current.CancellationToken)),
                await StatusOfAsync(() => FilesApi.GetFileInfoAsync(attachmentId, cancellationToken: TestContext.Current.CancellationToken)),
                await UsedSpaceAsync(projectId));

            if ((state.ArtifactStatus == 404 && state.AttachmentStatus == 404 && state.UsedSpace == expectedSpace) || DateTime.UtcNow >= deadline)
            {
                return state;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}
