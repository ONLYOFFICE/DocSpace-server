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
public class ThreadFilesAiHubRootListingTests(AspireAppFixture fixture) : ThreadFilesAccessTestBase(fixture)
{
    public enum Viewer
    {
        ProjectMember,
        ProjectOwner,
        PortalAdmin
    }

    private sealed record ProjectContent(int ProjectId, int ArtifactId, int ThreadFolderId, int UnsentFileId);

    [Theory]
    [InlineData(Viewer.ProjectMember)]
    [InlineData(Viewer.ProjectOwner)]
    [InlineData(Viewer.PortalAdmin)]
    public async Task ListAiHubRoot_ShowsNoContentOfSomeoneElsesThread(Viewer viewer)
    {
        var project = await CreateProjectContentAsync();

        await AuthenticateAsync(viewer, project.ProjectId);
        var content = (await FoldersApi.GetFolderByFolderIdAsync(
            await AiHubRootIdAsync(),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        AssertOnlyRoomsListed(content, project, expectProject: true);
    }

    [Theory]
    [InlineData(Viewer.ProjectMember)]
    [InlineData(Viewer.ProjectOwner)]
    [InlineData(Viewer.PortalAdmin)]
    public async Task FilterAiHubRootByThreadFolderTitle_FindsNothing(Viewer viewer)
    {
        var project = await CreateProjectContentAsync();

        await AuthenticateAsync(viewer, project.ProjectId);
        var content = (await FoldersApi.GetFolderByFolderIdAsync(
            await AiHubRootIdAsync(),
            filterValue: "artifacts",
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        AssertOnlyRoomsListed(content, project, expectProject: false);
    }

    [Theory]
    [InlineData(Viewer.ProjectMember)]
    [InlineData(Viewer.ProjectOwner)]
    [InlineData(Viewer.PortalAdmin)]
    public async Task SearchAiHubRoot_ShowsNoContentOfSomeoneElsesThread(Viewer viewer)
    {
        var project = await CreateProjectContentAsync();

        await AuthenticateAsync(viewer, project.ProjectId);
        var content = (await FoldersApi.SearchFolderAsync(
            await AiHubRootIdAsync(),
            new FolderMetadataSearch(count: 100),
            TestContext.Current.CancellationToken)).Response;

        AssertOnlyRoomsListed(content, project, expectProject: true);
    }

    [Theory]
    [InlineData(Viewer.ProjectMember)]
    [InlineData(Viewer.ProjectOwner)]
    [InlineData(Viewer.PortalAdmin)]
    public async Task ListRoomsRootAsAiHubArea_ShowsNoContentOfSomeoneElsesThread(Viewer viewer)
    {
        var project = await CreateProjectContentAsync();

        await AuthenticateAsync(viewer, project.ProjectId);
        var roomsRootId = (await RoomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.Active,
            cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;
        var content = (await FoldersApi.GetFolderByFolderIdAsync(
            roomsRootId,
            searchArea: SearchArea.AiHub,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        AssertOnlyRoomsListed(content, project, expectProject: true);
    }

    [Theory]
    [InlineData(Viewer.ProjectMember, null)]
    [InlineData(Viewer.ProjectOwner, null)]
    [InlineData(Viewer.PortalAdmin, null)]
    [InlineData(Viewer.ProjectMember, "artifacts")]
    [InlineData(Viewer.ProjectOwner, "artifacts")]
    [InlineData(Viewer.PortalAdmin, "artifacts")]
    public async Task ListRoomsRootAsAnyArea_ShowsNoContentOfSomeoneElsesThread(Viewer viewer, string? filterValue)
    {
        var project = await CreateProjectContentAsync();

        await AuthenticateAsync(viewer, project.ProjectId);
        var roomsRootId = (await RoomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.Active,
            cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;
        var content = (await FoldersApi.GetFolderByFolderIdAsync(
            roomsRootId,
            searchArea: SearchArea.Any,
            filterValue: filterValue,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        var folderIds = (content.Folders ?? []).OfType<FileEntryDto>().Select(f => f.Id).ToList();
        var fileIds = (content.Files ?? []).OfType<FileEntryDto>().Select(f => f.Id).ToList();

        if (viewer == Viewer.ProjectMember && filterValue == null)
        {
            folderIds.Should().Contain(project.ProjectId);
        }

        folderIds.Should().NotContain(project.ThreadFolderId);
        fileIds.Should().NotContain([project.ArtifactId, project.UnsentFileId]);
        content.Total.Should().Be(folderIds.Count + fileIds.Count);
    }

    private async Task<ProjectContent> CreateProjectContentAsync()
    {
        var artifact = await CreateArtifactAsync();

        await FilesHttpClient.Authenticate(artifact.Author);
        var unsent = await UploadAsync(artifact.ProjectId, "notes.txt");
        var threadFolder = (await FoldersApi.GetFolderInfoAsync(artifact.File.FolderId, TestContext.Current.CancellationToken)).Response;
        await FilesHttpClient.Authenticate(Owner);

        threadFolder.ParentId.Should().Be(artifact.ProjectId);

        return new ProjectContent(artifact.ProjectId, artifact.File.Id, artifact.File.FolderId, unsent.Id);
    }

    private async Task<int> AiHubRootIdAsync()
    {
        return (await RoomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.AiHub,
            cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;
    }

    private static void AssertOnlyRoomsListed(FolderContentDto content, ProjectContent project, bool expectProject)
    {
        var folderIds = (content.Folders ?? []).OfType<FileEntryDto>().Select(f => f.Id).ToList();
        var fileIds = (content.Files ?? []).OfType<FileEntryDto>().Select(f => f.Id).ToList();

        if (expectProject)
        {
            folderIds.Should().Contain(project.ProjectId);
        }

        folderIds.Should().NotContain(project.ThreadFolderId);
        fileIds.Should().NotContain([project.ArtifactId, project.UnsentFileId]);
        (content.Folders ?? []).OfType<FolderDto>().Should().NotContain(f => f.RoomType == null);
        content.Files.Should().BeNullOrEmpty();
        content.Total.Should().Be(folderIds.Count);
    }

    private async Task AuthenticateAsync(Viewer viewer, int projectId)
    {
        var user = viewer switch
        {
            Viewer.ProjectMember => await InviteMemberAsync(projectId),
            Viewer.ProjectOwner => Owner,
            Viewer.PortalAdmin => await InviteContact(EmployeeType.DocSpaceAdmin, TestContext.Current.CancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(viewer))
        };

        await FilesHttpClient.Authenticate(user);
    }
}
