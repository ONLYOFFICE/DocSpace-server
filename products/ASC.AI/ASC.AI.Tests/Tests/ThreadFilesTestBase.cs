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

namespace ASC.AI.Tests.Tests;

public abstract class ThreadFilesTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    protected const string FileText = "Autotest attachment. Line two.";
    protected const string AttachmentsPath = "/internal/ai/attachments";

    protected sealed record ThreadFile(int Id, string Title, int FolderId);

    protected sealed record ThreadFilesPage(List<ThreadFile> Files, long Total);

    protected sealed record CleanupState(int ArtifactStatus, int AttachmentStatus, long UsedSpace);

    protected sealed record Attachment(Guid Id, string Kind, string Title, string? Content, string? EntryId);

    protected async Task<int> CreateProjectAsync()
    {
        await FilesHttpClient.Authenticate(Owner);

        return await AgentsTests.AiProjects.CreateAsync(RoomsApi);
    }

    protected async Task<User> InviteMemberAsync(int projectId)
    {
        var member = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);

        await FilesHttpClient.Authenticate(Owner);
        await RoomsApi.SetRoomSecurityAsync(
            projectId,
            new RoomInvitationRequest([new RoomInvitation(member.Id, FileShare.ContentCreator)], notify: false),
            TestContext.Current.CancellationToken);

        return member;
    }

    protected async Task<HttpResponseMessage> PostArtifactAsync(Guid threadId, string name, byte[]? bytes = null)
    {
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent(bytes ?? System.Text.Encoding.UTF8.GetBytes($"Autotest artifact {name}"));
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        form.Add(file, "file", name);

        return await _aiClient.PostAsync($"{ThreadsPath}/{threadId}/artifacts", form, TestContext.Current.CancellationToken);
    }

    protected async Task<ThreadFile> UploadArtifactAsync(Guid threadId, string name)
    {
        using var response = await PostArtifactAsync(threadId, name);

        return await _ai.ReadAsync<ThreadFile>(response, TestContext.Current.CancellationToken);
    }

    protected Task<int> ArtifactsStatusAsync(int projectId, Guid threadId)
    {
        return StatusOfAsync(() => RoomsAiApi.GetThreadArtifactsAsync(projectId, threadId, cancellationToken: TestContext.Current.CancellationToken));
    }

    protected async Task<ThreadFilesPage> ListArtifactsAsync(int projectId, Guid threadId, int? count = null, int? startIndex = null)
    {
        var page = await RoomsAiApi.GetThreadArtifactsAsync(projectId, threadId, count, startIndex, cancellationToken: TestContext.Current.CancellationToken);

        return ToPage(page);
    }

    protected Task<int> AttachmentsStatusAsync(int projectId, Guid threadId)
    {
        return StatusOfAsync(() => RoomsAiApi.GetThreadAttachmentsAsync(projectId, threadId, cancellationToken: TestContext.Current.CancellationToken));
    }

    protected async Task<ThreadFilesPage> ListAttachmentsAsync(int projectId, Guid threadId)
    {
        var page = await RoomsAiApi.GetThreadAttachmentsAsync(projectId, threadId, cancellationToken: TestContext.Current.CancellationToken);

        return ToPage(page);
    }

    protected async Task<Attachment> AttachAsync(int fileId)
    {
        using var response = await _ai.PostAsync(AttachmentsPath, new { entryIds = new[] { fileId.ToString() } }, TestContext.Current.CancellationToken);

        var attachments = await _ai.ReadAsync<List<Attachment>>(response, TestContext.Current.CancellationToken);

        return attachments.Should().ContainSingle().Subject;
    }

    protected Task<HttpResponseMessage> BindAsync(Guid messageId, params Guid[] ids)
    {
        return _ai.PutAsync(AttachmentsPath, new { ids, messageId }, TestContext.Current.CancellationToken);
    }

    protected async Task<ThreadFile> SendAsync(int projectId, Guid threadId, string name)
    {
        var file = await UploadAsync(projectId, name);
        var attachment = await AttachAsync(file.Id);
        var message = await CreateMessageInternalAsync(threadId);

        using var response = await BindAsync(message.Id, attachment.Id);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        return file;
    }

    protected async Task<ThreadFile> UploadAsync(int folderId, string name, byte[]? bytes = null)
    {
        bytes ??= System.Text.Encoding.UTF8.GetBytes(FileText);

        var session = (await OperationsApi.CreateUploadSessionInFolderAsync(
            folderId,
            new SessionRequest(name, bytes.Length, createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        var uploaded = (await OperationsApi.UploadSessionAsync(
            folderId,
            session.Id,
            new FileParameter(name, "application/octet-stream", new MemoryStream(bytes)),
            TestContext.Current.CancellationToken)).Response.File;

        return new ThreadFile(uploaded.Id, uploaded.Title, uploaded.FolderId);
    }

    protected async Task<int> MyDocumentsIdAsync()
    {
        return (await FoldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;
    }

    protected static BatchRequestDto BuildBatch(
        int destFolderId,
        IEnumerable<int>? fileIds = null,
        IEnumerable<int>? folderIds = null,
        FileConflictResolveType conflictResolveType = FileConflictResolveType.Skip)
    {
        return new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(destFolderId),
            ConflictResolveType = conflictResolveType,
            FileIds = (fileIds ?? []).Select(id => new BatchRequestDtoAllOfFileIds(id)).ToList(),
            FolderIds = (folderIds ?? []).Select(id => new BatchRequestDtoAllOfFolderIds(id)).ToList(),
            ReturnSingleOperation = true
        };
    }

    protected async Task<FileOperationDto> CompleteAsync(List<FileOperationDto> started)
    {
        var operation = started.Should().ContainSingle().Subject;

        if (!operation.Finished)
        {
            operation = (await WaitForOperationAsync(operation.Id)).Should().ContainSingle().Subject;
        }

        operation.Error.Should().BeNullOrEmpty();

        return operation;
    }

    protected async Task<long> UsedSpaceAsync(int projectId)
    {
        var room = (await RoomsApi.GetRoomInfoAsync(projectId, TestContext.Current.CancellationToken)).Response;

        return room.UsedSpace ?? 0;
    }

    protected async Task<CleanupState> WaitForCleanupAsync(int projectId, int artifactId, int attachmentId, long expectedSpace)
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

    protected async Task AgeDraftAsync(string attachmentId, TimeSpan age)
    {
        await using var connection = await OpenDatabaseAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = "UPDATE ai_integration_attachments SET created_at = @createdAt WHERE id = @id";
        command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow - age);
        command.Parameters.AddWithValue("@id", attachmentId);

        (await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    private static ThreadFilesPage ToPage(FileArrayWrapper page)
    {
        return new ThreadFilesPage([.. page.Response.Select(f => new ThreadFile(f.Id, f.Title, f.FolderId))], page.Total);
    }
}
