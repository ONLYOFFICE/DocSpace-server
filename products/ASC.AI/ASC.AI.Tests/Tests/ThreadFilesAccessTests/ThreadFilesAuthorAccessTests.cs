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

namespace ASC.AI.Tests.Tests.ThreadFilesAccessTests;

[Trait("Category", "Security")]
[Trait("Feature", "AI/ThreadFiles")]
public class ThreadFilesAuthorAccessTests(AspireAppFixture fixture) : ThreadFilesAccessTestBase(fixture)
{
    [Fact]
    public async Task Read_OwnArtifact_Succeeds()
    {
        var artifact = await CreateArtifactAsync();

        await FilesHttpClient.Authenticate(artifact.Author);
        var file = (await FilesApi.GetFileInfoAsync(artifact.File.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        file.Title.Should().Be(artifact.File.Title);
    }

    [Fact]
    public async Task Download_OwnArtifact_Succeeds()
    {
        var artifact = await CreateArtifactAsync();

        await FilesHttpClient.Authenticate(artifact.Author);
        var link = (await FilesApi.GetPresignedFileUriAsync(artifact.File.Id, TestContext.Current.CancellationToken)).Response;

        link.Url.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Copy_OwnArtifactToMyDocuments_Succeeds()
    {
        var artifact = await CreateArtifactAsync();

        await FilesHttpClient.Authenticate(artifact.Author);
        var myDocumentsId = await MyDocumentsIdAsync();
        var operation = await CompleteAsync((await OperationsApi.CopyBatchItemsAsync(
            BuildBatch(myDocumentsId, fileIds: [artifact.File.Id]),
            TestContext.Current.CancellationToken)).Response);

        var copy = operation.Files.OfType<FileDto>().Should().ContainSingle().Subject;
        copy.FolderId.Should().Be(myDocumentsId);
    }

    [Fact]
    public async Task Rename_OwnArtifact_Returns403()
    {
        var artifact = await CreateArtifactAsync();

        await FilesHttpClient.Authenticate(artifact.Author);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await FilesApi.UpdateFileAsync(
            artifact.File.Id,
            new UpdateFileRequest { Title = "renamed.txt" },
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task Delete_OwnArtifact_Returns403()
    {
        var artifact = await CreateArtifactAsync();

        await FilesHttpClient.Authenticate(artifact.Author);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await FilesApi.DeleteFileAsync(
            artifact.File.Id,
            new DeleteFileRequest(immediately: true),
            true,
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task Move_OwnArtifactToMyDocuments_Returns403()
    {
        var artifact = await CreateArtifactAsync();

        await FilesHttpClient.Authenticate(artifact.Author);
        var myDocumentsId = await MyDocumentsIdAsync();
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await OperationsApi.MoveBatchItemsAsync(
            BuildBatch(myDocumentsId, fileIds: [artifact.File.Id]),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task OpenEdit_OwnArtifact_OpensForViewingOnly()
    {
        var artifact = await CreateArtifactAsync();

        await FilesHttpClient.Authenticate(artifact.Author);
        var config = (await FilesApi.OpenEditFileAsync(artifact.File.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        config.Document.Permissions.Edit.Should().BeFalse();
    }

    [Fact]
    public async Task Share_OwnArtifact_Returns403()
    {
        var artifact = await CreateArtifactAsync();

        await FilesHttpClient.Authenticate(artifact.Author);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await FilesApi.CreateFilePrimaryExternalLinkAsync(
            artifact.File.Id,
            new FileLinkRequest(access: FileShare.Read),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task Insert_IntoOwnThreadFolder_Returns403()
    {
        var artifact = await CreateArtifactAsync();

        await FilesHttpClient.Authenticate(artifact.Author);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(FileText));
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await FoldersApi.InsertFileAsync(
            artifact.File.FolderId,
            new FileParameter("inserted.txt", "text/plain", stream),
            "inserted.txt",
            cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task Read_AfterLeavingAndRejoiningTheProject_FollowsTheProjectAccess()
    {
        var artifact = await CreateArtifactAsync();

        await SetProjectAccessAsync(artifact, FileShare.None);
        await FilesHttpClient.Authenticate(artifact.Author);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await FilesApi.GetFileInfoAsync(
            artifact.File.Id,
            cancellationToken: TestContext.Current.CancellationToken));

        await SetProjectAccessAsync(artifact, FileShare.ContentCreator);
        await FilesHttpClient.Authenticate(artifact.Author);
        var file = (await FilesApi.GetFileInfoAsync(artifact.File.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        exception.ErrorCode.Should().Be(403);
        file.Id.Should().Be(artifact.File.Id);
    }

    private async Task SetProjectAccessAsync(Artifact artifact, FileShare access)
    {
        await FilesHttpClient.Authenticate(Owner);
        await RoomsApi.SetRoomSecurityAsync(
            artifact.ProjectId,
            new RoomInvitationRequest([new RoomInvitation(artifact.Author.Id, access)], notify: false),
            TestContext.Current.CancellationToken);
    }
}
