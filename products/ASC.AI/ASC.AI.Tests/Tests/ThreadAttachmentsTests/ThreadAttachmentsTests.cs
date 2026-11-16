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

namespace ASC.AI.Tests.Tests.ThreadAttachmentsTests;

[Trait("Category", "Storage")]
[Trait("Feature", "AI/ThreadAttachments")]
public class ThreadAttachmentsTests(AspireAppFixture fixture) : ThreadFilesTestBase(fixture)
{
    private const string AttachmentsPath = "/internal/ai/attachments";
    private const string Text = "Autotest attachment. Line two.";

    private static readonly byte[] _png =
        Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8DwHwAFAAH/q842iQAAAABJRU5ErkJggg==");

    private sealed record Attachment(Guid Id, string Kind, string Title, string? Content, string? EntryId);

    private sealed record AttachedFile(int Id, string Title, int FolderId);

    private sealed record AttachedFilesPage(List<AttachedFile> Files, long Total);

    [Fact]
    public async Task Upload_ToTheProject_StoresTheFileInItsRoot()
    {
        var projectId = await CreateProjectAsync();

        var file = await UploadAsync(projectId, "notes.txt");

        file.FolderId.Should().Be(projectId);
    }

    [Fact]
    public async Task Upload_ByAProjectMember_StoresTheFileInItsRoot()
    {
        var projectId = await CreateProjectAsync();
        var member = await InviteMemberAsync(projectId);

        await FilesHttpClient.Authenticate(member);
        var file = await UploadAsync(projectId, "notes.txt");
        await FilesHttpClient.Authenticate(Owner);

        file.FolderId.Should().Be(projectId);
    }

    [Fact]
    public async Task Upload_ByAUserOutsideTheProject_Returns403()
    {
        var projectId = await CreateProjectAsync();
        var outsider = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);

        await FilesHttpClient.Authenticate(outsider);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await OpenSessionAsync(projectId, "notes.txt"));
        await FilesHttpClient.Authenticate(Owner);

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task Upload_WithARelativePath_Returns403()
    {
        var projectId = await CreateProjectAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await OpenSessionAsync(projectId, "notes.txt", "nested"));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task Upload_OfAnUnsupportedFormat_Returns400()
    {
        var projectId = await CreateProjectAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await OpenSessionAsync(projectId, "data.bin"));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task Upload_InOneRequest_Returns403()
    {
        var projectId = await CreateProjectAsync();

        using var form = new MultipartFormDataContent();
        using var content = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(Text));
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        form.Add(content, "file", "notes.txt");

        using var response = await FilesHttpClient.PostAsync($"/api/2.0/files/{projectId}/upload", form, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Copy_APortalFile_StoresTheCopyInTheProjectRoot()
    {
        var projectId = await CreateProjectAsync();
        var source = await UploadAsync(await MyDocumentsIdAsync(), "notes.txt");

        var copy = await CopyAsync(projectId, source.Id);

        copy.Id.Should().NotBe(source.Id);
        copy.FolderId.Should().Be(projectId);
    }

    [Fact]
    public async Task Copy_ByAProjectMember_StoresTheCopyInTheProjectRoot()
    {
        var projectId = await CreateProjectAsync();
        var member = await InviteMemberAsync(projectId);

        await FilesHttpClient.Authenticate(member);
        var source = await UploadAsync(await MyDocumentsIdAsync(), "notes.txt");
        var copy = await CopyAsync(projectId, source.Id);
        await FilesHttpClient.Authenticate(Owner);

        copy.FolderId.Should().Be(projectId);
    }

    [Fact]
    public async Task Copy_ByAUserOutsideTheProject_Returns403()
    {
        var projectId = await CreateProjectAsync();
        var outsider = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);

        await FilesHttpClient.Authenticate(outsider);
        var source = await UploadAsync(await MyDocumentsIdAsync(), "notes.txt");
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await OperationsApi.CopyBatchItemsAsync(
            BuildBatch(projectId, fileIds: [source.Id]),
            TestContext.Current.CancellationToken));
        await FilesHttpClient.Authenticate(Owner);

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task Copy_OfAFolder_Returns403()
    {
        var projectId = await CreateProjectAsync();
        var folder = (await FoldersApi.CreateFolderAsync(
            await MyDocumentsIdAsync(),
            new CreateFolderRequest("Autotest Attachments Folder"),
            TestContext.Current.CancellationToken)).Response;

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await OperationsApi.CopyBatchItemsAsync(
            BuildBatch(projectId, folderIds: [folder.Id]),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task Copy_OfAnUnsupportedFormat_Returns415()
    {
        var projectId = await CreateProjectAsync();
        var source = await UploadAsync(await MyDocumentsIdAsync(), "data.bin");

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await OperationsApi.CopyBatchItemsAsync(
            BuildBatch(projectId, fileIds: [source.Id]),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(415);
    }

    [Fact]
    public async Task Copy_TheSameFileTwice_KeepsBothCopies()
    {
        var projectId = await CreateProjectAsync();
        var source = await UploadAsync(await MyDocumentsIdAsync(), "notes.txt");

        var first = await CopyAsync(projectId, source.Id, FileConflictResolveType.Overwrite);
        var second = await CopyAsync(projectId, source.Id, FileConflictResolveType.Overwrite);

        second.Id.Should().NotBe(first.Id);
        second.Title.Should().NotBe(first.Title);
    }

    [Fact]
    public async Task Move_APortalFile_ToTheProject_Returns403()
    {
        var projectId = await CreateProjectAsync();
        var source = await UploadAsync(await MyDocumentsIdAsync(), "notes.txt");

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await OperationsApi.MoveBatchItemsAsync(
            BuildBatch(projectId, fileIds: [source.Id]),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task Attach_AnUploadedFile_ReturnsOneAttachmentWithItsText()
    {
        var projectId = await CreateProjectAsync();
        var file = await UploadAsync(projectId, "notes.txt");

        var first = await AttachAsync(file.Id);
        var second = await AttachAsync(file.Id);

        first.Kind.Should().Be("file");
        first.EntryId.Should().Be(file.Id.ToString());
        first.Content.Should().Contain("Autotest attachment");
        second.Id.Should().Be(first.Id);
        second.Content.Should().Be(first.Content);
    }

    [Fact]
    public async Task Attach_AnUploadedPicture_ReturnsOneImageAttachment()
    {
        var projectId = await CreateProjectAsync();
        var file = await UploadAsync(projectId, "picture.png", _png);

        var first = await AttachAsync(file.Id);
        var second = await AttachAsync(file.Id);

        first.Kind.Should().Be("image");
        first.EntryId.Should().Be(file.Id.ToString());
        second.Id.Should().Be(first.Id);
    }

    [Fact]
    public async Task Bind_ToAMessage_MovesTheFileToTheThreadAttachments()
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var file = await UploadAsync(projectId, "notes.txt");
        var attachment = await AttachAsync(file.Id);
        var message = await CreateMessageInternalAsync(thread.Id);

        using var response = await BindAsync(message.Id, attachment.Id);

        var page = await ListAttachmentsAsync(projectId, thread.Id);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        page.Total.Should().Be(1);
        page.Files.Should().ContainSingle().Which.Id.Should().Be(file.Id);
        page.Files[0].FolderId.Should().NotBe(projectId);
    }

    [Fact]
    public async Task Bind_ACopiedFile_KeepsItInTheThreadAfterTheOriginalIsDeleted()
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var source = await UploadAsync(await MyDocumentsIdAsync(), "notes.txt");
        var copy = await CopyAsync(projectId, source.Id);
        var attachment = await AttachAsync(copy.Id);
        var message = await CreateMessageInternalAsync(thread.Id);

        using var response = await BindAsync(message.Id, attachment.Id);
        await CompleteAsync((await FilesApi.DeleteFileAsync(
            source.Id,
            new DeleteFileRequest(deleteAfter: false, immediately: true),
            returnSingleOperation: true,
            TestContext.Current.CancellationToken)).Response);

        var page = await ListAttachmentsAsync(projectId, thread.Id);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        attachment.Content.Should().Contain("Autotest attachment");
        page.Files.Should().ContainSingle().Which.Id.Should().Be(copy.Id);
        page.Files[0].FolderId.Should().NotBe(projectId);
    }

    [Fact]
    public async Task Bind_FilesOfTwoMessages_KeepsThemInOneFolder()
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var first = await SendAsync(projectId, thread.Id, "first.txt");
        var second = await SendAsync(projectId, thread.Id, "second.txt");

        var page = await ListAttachmentsAsync(projectId, thread.Id);

        page.Total.Should().Be(2);
        page.Files.Select(f => f.Id).Should().BeEquivalentTo([first.Id, second.Id]);
        page.Files.Select(f => f.FolderId).Distinct().Should().ContainSingle();
    }

    [Fact]
    public async Task Bind_InThreadsOfTheSameProject_UsesAFolderPerThread()
    {
        var projectId = await CreateProjectAsync();
        var firstThread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var secondThread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var first = await SendAsync(projectId, firstThread.Id, "first.txt");
        var second = await SendAsync(projectId, secondThread.Id, "second.txt");

        var firstPage = await ListAttachmentsAsync(projectId, firstThread.Id);
        var secondPage = await ListAttachmentsAsync(projectId, secondThread.Id);

        firstPage.Files.Should().ContainSingle().Which.Id.Should().Be(first.Id);
        secondPage.Files.Should().ContainSingle().Which.Id.Should().Be(second.Id);
        secondPage.Files[0].FolderId.Should().NotBe(firstPage.Files[0].FolderId);
    }

    [Fact]
    public async Task Bind_InAThreadOutsideAProject_LeavesTheFileWhereItWas()
    {
        await FilesHttpClient.Authenticate(Owner);
        var myDocumentsId = await MyDocumentsIdAsync();
        var thread = await CreateThreadInternalAsync();
        var file = await UploadAsync(myDocumentsId, "notes.txt");
        var attachment = await AttachAsync(file.Id);
        var message = await CreateMessageInternalAsync(thread.Id);

        using var response = await BindAsync(message.Id, attachment.Id);

        var stored = (await FilesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        stored.FolderId.Should().Be(myDocumentsId);
    }

    [Fact]
    public async Task List_FileThatWasNotSent_IsNotInTheThread()
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var file = await UploadAsync(projectId, "notes.txt");
        await AttachAsync(file.Id);

        var page = await ListAttachmentsAsync(projectId, thread.Id);

        page.Files.Should().BeEmpty();
        page.Total.Should().Be(0);
    }

    [Fact]
    public async Task List_SomeoneElsesThread_Returns404EvenToTheProjectOwner()
    {
        var projectId = await CreateProjectAsync();
        var member = await InviteMemberAsync(projectId);

        await _aiClient.Authenticate(member);
        await FilesHttpClient.Authenticate(member);
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var file = await SendAsync(projectId, thread.Id, "notes.txt");
        var memberPage = await ListAttachmentsAsync(projectId, thread.Id);
        await _aiClient.Authenticate(Owner);

        await FilesHttpClient.Authenticate(Owner);
        using var ownerResponse = await GetAttachmentsAsync(projectId, thread.Id);

        memberPage.Files.Should().ContainSingle().Which.Id.Should().Be(file.Id);
        ownerResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private Task<ChunkedUploadSessionWrapper> OpenSessionAsync(int folderId, string name, string? relativePath = null)
    {
        return OperationsApi.CreateUploadSessionInFolderAsync(
            folderId,
            new SessionRequest(name, Text.Length, relativePath!, createNewIfExist: true),
            TestContext.Current.CancellationToken);
    }

    private async Task<AttachedFile> UploadAsync(int folderId, string name, byte[]? bytes = null)
    {
        bytes ??= System.Text.Encoding.UTF8.GetBytes(Text);

        var session = (await OperationsApi.CreateUploadSessionInFolderAsync(
            folderId,
            new SessionRequest(name, bytes.Length, createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        var uploaded = (await OperationsApi.UploadSessionAsync(
            folderId,
            session.Id,
            new FileParameter(name, "application/octet-stream", new MemoryStream(bytes)),
            TestContext.Current.CancellationToken)).Response.File;

        return new AttachedFile(uploaded.Id, uploaded.Title, uploaded.FolderId);
    }

    private async Task<int> MyDocumentsIdAsync()
    {
        return (await FoldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;
    }

    private static BatchRequestDto BuildBatch(
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

    private async Task<FileOperationDto> CompleteAsync(List<FileOperationDto> started)
    {
        var operation = started.Should().ContainSingle().Subject;

        if (!operation.Finished)
        {
            operation = (await WaitForOperationAsync(operation.Id)).Should().ContainSingle().Subject;
        }

        operation.Error.Should().BeNullOrEmpty();

        return operation;
    }

    private async Task<AttachedFile> CopyAsync(int projectId, int fileId, FileConflictResolveType conflictResolveType = FileConflictResolveType.Skip)
    {
        var operation = await CompleteAsync((await OperationsApi.CopyBatchItemsAsync(
            BuildBatch(projectId, fileIds: [fileId], conflictResolveType: conflictResolveType),
            TestContext.Current.CancellationToken)).Response);

        var copy = operation.Files.OfType<FileDto>().Should().ContainSingle().Subject;

        return new AttachedFile(copy.Id, copy.Title, copy.FolderId);
    }

    private async Task<Attachment> AttachAsync(int fileId)
    {
        using var response = await _ai.PostAsync(AttachmentsPath, new { entryIds = new[] { fileId.ToString() } }, TestContext.Current.CancellationToken);

        var attachments = await _ai.ReadAsync<List<Attachment>>(response, TestContext.Current.CancellationToken);

        return attachments.Should().ContainSingle().Subject;
    }

    private Task<HttpResponseMessage> BindAsync(Guid messageId, params Guid[] ids)
    {
        return _ai.PutAsync(AttachmentsPath, new { ids, messageId }, TestContext.Current.CancellationToken);
    }

    private async Task<AttachedFile> SendAsync(int projectId, Guid threadId, string name)
    {
        var file = await UploadAsync(projectId, name);
        var attachment = await AttachAsync(file.Id);
        var message = await CreateMessageInternalAsync(threadId);

        using var response = await BindAsync(message.Id, attachment.Id);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        return file;
    }

    private Task<HttpResponseMessage> GetAttachmentsAsync(int projectId, Guid threadId)
    {
        return FilesHttpClient.GetAsync($"/api/2.0/files/rooms/{projectId}/threads/{threadId}/attachments", TestContext.Current.CancellationToken);
    }

    private async Task<AttachedFilesPage> ListAttachmentsAsync(int projectId, Guid threadId)
    {
        using var response = await GetAttachmentsAsync(projectId, threadId);

        var envelope = await new RawApiClient(FilesHttpClient).ReadEnvelopeAsync<List<AttachedFile>>(response, TestContext.Current.CancellationToken);

        return new AttachedFilesPage(envelope.Response ?? [], envelope.Total ?? 0);
    }
}
