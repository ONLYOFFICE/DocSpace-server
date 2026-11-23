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

[Trait("Category", "Storage")]
[Trait("Feature", "AI/ThreadFiles")]
public class ThreadFilesProjectListingTests(AspireAppFixture fixture) : ThreadFilesAccessTestBase(fixture)
{
    public enum Viewer
    {
        Author,
        ProjectOwner,
        PortalAdmin
    }

    [Theory]
    [InlineData(Viewer.Author)]
    [InlineData(Viewer.ProjectOwner)]
    [InlineData(Viewer.PortalAdmin)]
    public async Task List_ProjectWithThreadFiles_IsEmpty(Viewer viewer)
    {
        var artifact = await CreateProjectWithThreadFilesAsync();

        await AuthenticateAsync(viewer, artifact);
        var content = (await FoldersApi.GetFolderByFolderIdAsync(
            artifact.ProjectId,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        content.Files.Should().BeEmpty();
        content.Folders.Should().BeEmpty();
        content.Total.Should().Be(0);
    }

    [Theory]
    [InlineData(Viewer.Author)]
    [InlineData(Viewer.ProjectOwner)]
    [InlineData(Viewer.PortalAdmin)]
    public async Task ListFolders_ProjectWithThreadFiles_IsEmpty(Viewer viewer)
    {
        var artifact = await CreateProjectWithThreadFilesAsync();

        await AuthenticateAsync(viewer, artifact);
        var folders = (await FoldersApi.GetFoldersAsync(artifact.ProjectId, TestContext.Current.CancellationToken)).Response;

        folders.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_InsideTheProject_FindsNothing()
    {
        var artifact = await CreateProjectWithThreadFilesAsync();

        await FilesHttpClient.Authenticate(artifact.Author);
        var content = (await FoldersApi.GetFolderByFolderIdAsync(
            artifact.ProjectId,
            filterValue: "report",
            withSubFolders: true,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        content.Files.Should().BeEmpty();
        content.Total.Should().Be(0);
    }

    [Fact]
    public async Task RoomInfo_ProjectWithThreadFiles_ReportsNoFilesOrFolders()
    {
        var artifact = await CreateProjectWithThreadFilesAsync();

        var project = (await RoomsApi.GetRoomInfoAsync(artifact.ProjectId, TestContext.Current.CancellationToken)).Response;

        project.FilesCount.Should().Be(0);
        project.FoldersCount.Should().Be(0);
    }

    [Fact]
    public async Task Sources_ProjectWithThreadFiles_StillListsTheLinkedRooms()
    {
        var artifact = await CreateProjectWithThreadFilesAsync();
        var instructions = (await RoomsApi.CreateRoomAsync(
            new CreateRoomRequestDto($"instructions-{Guid.NewGuid():N}", roomType: RoomType.InstructionsRoom),
            TestContext.Current.CancellationToken)).Response;

        await RoomsAiApi.LinkProjectSourcesAsync(
            artifact.ProjectId,
            new LinkAiProjectSourcesRequest([instructions.Id]),
            TestContext.Current.CancellationToken);

        var sources = (await RoomsAiApi.GetProjectSourcesAsync(artifact.ProjectId, TestContext.Current.CancellationToken)).Response;
        var skills = (await RoomsAiApi.GetRoomSkillsAsync(artifact.ProjectId, TestContext.Current.CancellationToken)).Response;

        sources.InstructionsRooms.Should().ContainSingle().Which.Id.Should().Be(instructions.Id);
        skills.Rooms.Should().NotBeNull();
    }

    private async Task<Artifact> CreateProjectWithThreadFilesAsync()
    {
        var artifact = await CreateArtifactAsync();

        await FilesHttpClient.Authenticate(artifact.Author);
        await UploadAsync(artifact.ProjectId, "notes.txt");
        await FilesHttpClient.Authenticate(Owner);

        return artifact;
    }

    private async Task AuthenticateAsync(Viewer viewer, Artifact artifact)
    {
        var user = viewer switch
        {
            Viewer.Author => artifact.Author,
            Viewer.ProjectOwner => Owner,
            Viewer.PortalAdmin => await InviteContact(EmployeeType.DocSpaceAdmin, TestContext.Current.CancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(viewer))
        };

        await FilesHttpClient.Authenticate(user);
    }
}
