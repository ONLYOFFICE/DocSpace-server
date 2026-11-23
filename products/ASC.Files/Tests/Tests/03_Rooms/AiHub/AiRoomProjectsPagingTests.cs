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
/// Paging of <c>GET api/2.0/files/rooms/{id}/projects</c>: <c>count</c> from 1 to 100 and <c>startIndex</c> from 0.
/// The typed client cannot send an empty <c>count</c>, so that one case goes over raw HTTP. A Knowledge room needs
/// vectorization, which the test environment does not have, so the room here is an Instructions room.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AiHub")]
public class AiRoomProjectsPagingTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    [Fact]
    public async Task GetRoomProjects_Count1StartIndex1_SecondProjectByTitleAndTotal3()
    {
        var (roomId, projectIds) = await CreateRoomWithProjectsAsync("Gamma", "Alpha", "Beta");

        var page = await GetRoomProjectsAsync(roomId, count: 1, startIndex: 1);

        IdsOf(page).Should().Equal(projectIds[2]);
        page.Response.Single().Title.Should().Be("Beta");
        page.Total.Should().Be(3);
    }

    [Fact]
    public async Task GetRoomProjects_StartIndexPastTheEnd_EmptyPageAndTotal3()
    {
        var (roomId, _) = await CreateRoomWithProjectsAsync("Gamma", "Alpha", "Beta");

        var page = await GetRoomProjectsAsync(roomId, startIndex: 3);

        page.Response.Should().BeEmpty();
        page.Total.Should().Be(3);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(-1, null)]
    [InlineData(101, null)]
    [InlineData(null, -1)]
    public async Task GetRoomProjects_CountOrStartIndexOutOfRange_Returns400(int? count, int? startIndex)
    {
        var (roomId, projectIds) = await CreateRoomWithProjectsAsync("Autotest Project");

        var valid = await GetRoomProjectsAsync(roomId);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await GetRoomProjectsAsync(roomId, count, startIndex));

        IdsOf(valid).Should().Equal(projectIds);
        exception.ErrorCode.Should().Be(400, exception.ErrorContent!.ToString());
    }

    [Fact]
    public async Task GetRoomProjects_EmptyCount_Returns400()
    {
        var (roomId, projectIds) = await CreateRoomWithProjectsAsync("Autotest Project");

        var valid = await GetRoomProjectsAsync(roomId);
        using var response = await Files.GetAsync($"api/2.0/files/rooms/{roomId}/projects?count=", TestContext.Current.CancellationToken);

        IdsOf(valid).Should().Equal(projectIds);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetRoomProjects_Count100StartIndex0_Returns200WithTheThreeProjects()
    {
        var (roomId, projectIds) = await CreateRoomWithProjectsAsync("Gamma", "Alpha", "Beta");

        var page = await GetRoomProjectsAsync(roomId, count: 100, startIndex: 0);

        IdsOf(page).Should().Equal(projectIds[1], projectIds[2], projectIds[0]);
    }
}
