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
public class ThreadFilesStrangerAccessTests(AspireAppFixture fixture) : ThreadFilesAccessTestBase(fixture)
{
    public enum Stranger
    {
        ProjectMember,
        ProjectOwner,
        PortalAdmin
    }

    [Theory]
    [InlineData(Stranger.ProjectMember)]
    [InlineData(Stranger.ProjectOwner)]
    [InlineData(Stranger.PortalAdmin)]
    public async Task Read_SomeoneElsesArtifact_Returns403(Stranger stranger)
    {
        var artifact = await CreateArtifactAsync();

        await AuthenticateAsync(stranger, artifact.ProjectId);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await FilesApi.GetFileInfoAsync(
            artifact.File.Id,
            cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(Stranger.ProjectMember)]
    [InlineData(Stranger.ProjectOwner)]
    [InlineData(Stranger.PortalAdmin)]
    public async Task Download_SomeoneElsesArtifact_Returns403(Stranger stranger)
    {
        var artifact = await CreateArtifactAsync();

        await AuthenticateAsync(stranger, artifact.ProjectId);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await FilesApi.GetPresignedFileUriAsync(
            artifact.File.Id,
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(Stranger.ProjectMember)]
    [InlineData(Stranger.ProjectOwner)]
    [InlineData(Stranger.PortalAdmin)]
    public async Task VersionInfo_SomeoneElsesArtifact_Returns403(Stranger stranger)
    {
        var artifact = await CreateArtifactAsync();

        await AuthenticateAsync(stranger, artifact.ProjectId);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await FilesApi.GetFileVersionInfoAsync(
            artifact.File.Id,
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(Stranger.ProjectMember)]
    [InlineData(Stranger.ProjectOwner)]
    [InlineData(Stranger.PortalAdmin)]
    public async Task Copy_SomeoneElsesArtifact_Returns403(Stranger stranger)
    {
        var artifact = await CreateArtifactAsync();

        await AuthenticateAsync(stranger, artifact.ProjectId);
        var myDocumentsId = await MyDocumentsIdAsync();
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await OperationsApi.CopyBatchItemsAsync(
            BuildBatch(myDocumentsId, fileIds: [artifact.File.Id]),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(Stranger.ProjectMember)]
    [InlineData(Stranger.ProjectOwner)]
    [InlineData(Stranger.PortalAdmin)]
    public async Task DownloadAsArchive_SomeoneElsesArtifact_Returns403(Stranger stranger)
    {
        var artifact = await CreateArtifactAsync();

        await AuthenticateAsync(stranger, artifact.ProjectId);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await OperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(fileIds: [new(artifact.File.Id)], folderIds: [], fileConvertIds: []) { ReturnSingleOperation = true },
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(Stranger.ProjectMember)]
    [InlineData(Stranger.ProjectOwner)]
    [InlineData(Stranger.PortalAdmin)]
    public async Task Read_SomeoneElsesThreadFolder_Returns403(Stranger stranger)
    {
        var artifact = await CreateArtifactAsync();

        await AuthenticateAsync(stranger, artifact.ProjectId);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await FoldersApi.GetFolderInfoAsync(
            artifact.File.FolderId,
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(Stranger.ProjectMember)]
    [InlineData(Stranger.ProjectOwner)]
    [InlineData(Stranger.PortalAdmin)]
    public async Task Read_SomeoneElsesUnsentAttachment_Returns403(Stranger stranger)
    {
        var projectId = await CreateProjectAsync();
        var author = await InviteMemberAsync(projectId);

        await FilesHttpClient.Authenticate(author);
        var file = await UploadAsync(projectId, "notes.txt");

        await AuthenticateAsync(stranger, projectId);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await FilesApi.GetFileInfoAsync(
            file.Id,
            cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    private async Task AuthenticateAsync(Stranger stranger, int projectId)
    {
        var user = stranger switch
        {
            Stranger.ProjectMember => await InviteMemberAsync(projectId),
            Stranger.ProjectOwner => Owner,
            Stranger.PortalAdmin => await InviteContact(EmployeeType.DocSpaceAdmin, TestContext.Current.CancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(stranger))
        };

        await FilesHttpClient.Authenticate(user);
    }
}
