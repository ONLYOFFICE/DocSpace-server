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
/// The list of the AI projects that use a room, <c>GET api/2.0/files/rooms/{id}/projects</c>. A Knowledge room needs
/// vectorization, which the test environment does not have, so the room here is an Instructions room.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AiHub")]
public class AiRoomProjectsListTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    [Fact]
    public async Task GetRoomProjects_ThreeProjects_SortedByTitleWithIdTitleAndLogo()
    {
        var (roomId, projectIds) = await CreateRoomWithProjectsAsync("Gamma", "Alpha", "Beta");

        var page = await GetRoomProjectsAsync(roomId);

        page.Response.Select(p => p.Title).Should().Equal("Alpha", "Beta", "Gamma");
        IdsOf(page).Should().Equal(projectIds[1], projectIds[2], projectIds[0]);
        page.Response.Should().OnlyContain(p => p.Logo != null);
        page.Total.Should().Be(3);
    }

    [Fact]
    public async Task GetRoomProjects_TwoProjectsWithOneTitle_LowerIdFirst()
    {
        var (roomId, projectIds) = await CreateRoomWithProjectsAsync("Twin", "Twin");

        var page = await GetRoomProjectsAsync(roomId);

        projectIds[0].Should().BeLessThan(projectIds[1]);
        IdsOf(page).Should().Equal(projectIds);
    }

    [Fact]
    public async Task GetRoomProjects_RoomLinkedToNoProject_EmptyPageAndTotal0()
    {
        var room = await CreateSectionRoomAsync(RoomType.InstructionsRoom);
        await CreateSectionRoomAsync(RoomType.AiProjectRoom);

        var page = await GetRoomProjectsAsync(room.Id);

        page.Response.Should().BeEmpty();
        page.Total.Should().Be(0);
    }

    [Theory]
    [InlineData("project")]
    [InlineData("custom room")]
    [InlineData("unknown")]
    public async Task GetRoomProjects_IdThatIsNoKnowledgeOrInstructionsRoom_Returns404(string kind)
    {
        var (roomId, projectIds) = await CreateRoomWithProjectsAsync("Autotest Project");
        var id = kind switch
        {
            "project" => projectIds[0],
            "custom room" => (await CreateRoom(new CreateRoomRequestDto("Autotest Custom Room", roomType: RoomType.CustomRoom))).Id,
            _ => int.MaxValue
        };

        var room = await GetRoomProjectsAsync(roomId);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await GetRoomProjectsAsync(id));

        IdsOf(room).Should().Equal(projectIds);
        exception.ErrorCode.Should().Be(404, exception.ErrorContent!.ToString());
    }

    [Fact]
    public async Task GetRoomProjects_UserWhoCannotReadTheRoom_Returns403()
    {
        var (roomId, _) = await CreateRoomWithProjectsAsync("Autotest Project");
        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await GetRoomProjectsAsync(roomId));

        exception.ErrorCode.Should().Be(403, exception.ErrorContent!.ToString());
    }
}
