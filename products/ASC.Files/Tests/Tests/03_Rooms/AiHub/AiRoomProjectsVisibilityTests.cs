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

namespace ASC.Files.Tests.Tests._03_Rooms.AiHub;

/// <summary>
/// Who is listed by <c>GET api/2.0/files/rooms/{id}/projects</c>: a project only to a caller who may read it, a
/// DocSpace admin, the creator of the project or a member with a role of their own in it, while
/// <c>linkedProjectsCount</c> of the room counts every project. The pinned SDK has neither the operation nor the
/// field yet, so both are read over raw HTTP. A Knowledge room needs vectorization, which the test environment does
/// not have, so the room here is an Instructions room.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AiHub")]
public class AiRoomProjectsVisibilityTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    [Fact]
    public async Task GetRoomProjects_ViewerOfTheRoomAndMemberOfOneProject_ListsThatProjectOnly()
    {
        var (roomId, projectIds) = await CreateRoomWithProjectsAsync("Autotest Project One", "Autotest Project Two");
        var member = await InviteContact(EmployeeType.User);
        await ShareRoomAsync(roomId, member.Id, FileShare.Read);
        await ShareRoomAsync(projectIds[0], member.Id, FileShare.ContentCreator);
        var all = await GetRoomProjectsAsync(roomId);
        await _filesClient.Authenticate(member);

        var page = await GetRoomProjectsAsync(roomId);
        var room = await GetRoomAsync(roomId);

        all.Ids.Should().Equal(projectIds);
        page.Ids.Should().Equal(projectIds[0]);
        page.Total.Should().Be(1);
        LinkedProjectsCountOf(room).Should().Be(2);
    }

    [Fact]
    public async Task GetRoomProjects_ViewerOfTheRoomOutsideTheProjects_EmptyPageAndTotal0()
    {
        var (roomId, projectIds) = await CreateRoomWithProjectsAsync("Autotest Project One", "Autotest Project Two");
        var viewer = await InviteContact(EmployeeType.User);
        await ShareRoomAsync(roomId, viewer.Id, FileShare.Read);
        var all = await GetRoomProjectsAsync(roomId);
        await _filesClient.Authenticate(viewer);

        var page = await GetRoomProjectsAsync(roomId);

        all.Ids.Should().Equal(projectIds);
        all.Total.Should().Be(2);
        page.Projects.Should().BeEmpty();
        page.Total.Should().Be(0);
    }

    [Fact]
    public async Task GetRoomProjects_ProjectMemberWithNoRoleInTheRoom_Returns200WithOwnProjectOnly()
    {
        var (roomId, projectIds) = await CreateRoomWithProjectsAsync("Autotest Project One", "Autotest Project Two");
        var member = await InviteContact(EmployeeType.User);
        await ShareRoomAsync(projectIds[1], member.Id, FileShare.ContentCreator);
        var all = await GetRoomProjectsAsync(roomId);
        await _filesClient.Authenticate(member);

        using var response = await GetRoomProjectsResponseAsync(roomId);
        var projects = await Files.ReadAsync<List<RoomProject>>(response, TestContext.Current.CancellationToken);

        all.Ids.Should().Equal(projectIds);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        projects.Select(p => p.Id).Should().Equal(projectIds[1]);
    }

    [Fact]
    public async Task GetRoomProjects_DocSpaceAdminOutsideTheProjects_ListsBoth()
    {
        var (roomId, projectIds) = await CreateRoomWithProjectsAsync("Autotest Project One", "Autotest Project Two");
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        var page = await GetRoomProjectsAsync(roomId);

        page.Ids.Should().Equal(projectIds);
        page.Total.Should().Be(2);
    }

    [Fact]
    public async Task GetRoomProjects_RoomAdminWhoCreatedTheProject_ListsItAndNotTheProjectOfAnother()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var anotherRoomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);
        var ownProject = await CreateProjectAsync("Autotest Own Project");
        var roomId = await CreateInstructionsRoomAsync();
        await LinkAsync(ownProject, roomId);
        await ShareRoomAsync(roomId, anotherRoomAdmin.Id, FileShare.RoomManager);
        await _filesClient.Authenticate(anotherRoomAdmin);
        var otherProject = await CreateProjectAsync("Autotest Other Project");
        await LinkAsync(otherProject, roomId);
        await _filesClient.Authenticate(Owner);
        var all = await GetRoomProjectsAsync(roomId);
        await _filesClient.Authenticate(roomAdmin);

        var page = await GetRoomProjectsAsync(roomId);

        all.Ids.Should().BeEquivalentTo([ownProject, otherProject]);
        page.Ids.Should().Equal(ownProject);
        page.Total.Should().Be(1);
    }
}
