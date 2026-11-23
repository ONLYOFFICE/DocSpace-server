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
public class ThreadFilesDisclosureTests(AspireAppFixture fixture) : ThreadFilesAccessTestBase(fixture)
{
    public enum Viewer
    {
        ProjectMember,
        ProjectOwner,
        PortalAdmin
    }

    [Theory]
    [InlineData(Viewer.ProjectMember, false)]
    [InlineData(Viewer.ProjectOwner, true)]
    [InlineData(Viewer.PortalAdmin, false)]
    public async Task GetRoomInfo_OfAProject_ShowsTheUsedSpaceToWhoManagesIt(Viewer viewer, bool sees)
    {
        var projectId = await CreateProjectAsync();
        var uploader = await InviteMemberAsync(projectId);

        await FilesHttpClient.Authenticate(uploader);
        await UploadAsync(projectId, "notes.txt");
        await FilesHttpClient.Authenticate(Owner);
        (await UsedSpaceAsync(projectId)).Should().BePositive();

        await FilesHttpClient.Authenticate(viewer switch
        {
            Viewer.ProjectMember => await InviteMemberAsync(projectId),
            Viewer.ProjectOwner => Owner,
            Viewer.PortalAdmin => await InviteContact(EmployeeType.DocSpaceAdmin, TestContext.Current.CancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(viewer))
        });
        var room = (await RoomsApi.GetRoomInfoAsync(projectId, TestContext.Current.CancellationToken)).Response;
        await FilesHttpClient.Authenticate(Owner);

        if (sees)
        {
            room.UsedSpace.Should().BePositive();
        }
        else
        {
            room.UsedSpace.Should().BeNull();
        }
    }

    [Fact]
    public async Task ListAiHubRoot_SortedByUsedSpace_OrdersProjectsByTitle()
    {
        await FilesHttpClient.Authenticate(Owner);
        var empty = await AgentsTests.AiProjects.CreateAsync(RoomsApi, $"aaa-{Guid.NewGuid():N}");
        var filled = await AgentsTests.AiProjects.CreateAsync(RoomsApi, $"zzz-{Guid.NewGuid():N}");
        var member = await InviteMemberAsync(filled);
        await RoomsApi.SetRoomSecurityAsync(
            empty,
            new RoomInvitationRequest([new RoomInvitation(member.Id, FileShare.ContentCreator)], notify: false),
            TestContext.Current.CancellationToken);

        await UploadAsync(filled, "notes.txt");
        (await UsedSpaceAsync(filled)).Should().BeGreaterThan(await UsedSpaceAsync(empty));

        await FilesHttpClient.Authenticate(member);
        var aiHubRootId = (await RoomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.AiHub,
            cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;
        var content = (await FoldersApi.GetFolderByFolderIdAsync(
            aiHubRootId,
            sortBy: "UsedSpace",
            sortOrder: SortOrder.Descending,
            cancellationToken: TestContext.Current.CancellationToken)).Response;
        await FilesHttpClient.Authenticate(Owner);

        var ids = (content.Folders ?? []).OfType<FileEntryDto>().Select(f => f.Id).Where(id => id == empty || id == filled).ToList();
        ids.Should().Equal(empty, filled);
    }
}
