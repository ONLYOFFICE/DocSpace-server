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
/// The number of AI projects that use a room, <c>linkedProjectsCount</c> of a Knowledge or an Instructions room. A
/// room whose answer has no such field reads as null through the SDK. A Knowledge room needs vectorization, which the
/// test environment does not have, so the room here is an Instructions room.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AiHub")]
public class AiRoomLinkedProjectsCountTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    [Fact]
    public async Task LinkedProjectsCount_RoomLinkedToTwoProjects_Is2InTheSectionAndInTheRoom()
    {
        var (roomId, _) = await CreateRoomWithProjectsAsync("Autotest Project One", "Autotest Project Two");

        var section = await GetAiHubFoldersAsync();
        var room = await GetRoomInfoAsync(roomId);

        section.Single(f => f.Id == roomId).LinkedProjectsCount.Should().Be(2);
        room.LinkedProjectsCount.Should().Be(2);
    }

    [Fact]
    public async Task LinkedProjectsCount_RoomLinkedToNoProject_Is0OnCreateAndOnRead()
    {
        var created = await CreateSectionRoomAsync(RoomType.InstructionsRoom, "Autotest Unused Instructions");

        var room = await GetRoomInfoAsync(created.Id);

        created.LinkedProjectsCount.Should().Be(0);
        room.LinkedProjectsCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateRoom_RoomLinkedToOneProject_AnswerCarries1()
    {
        var (roomId, _) = await CreateRoomWithProjectsAsync("Autotest Project");

        var updated = (await _roomsApi.UpdateRoomAsync(roomId, new UpdateRoomRequest(title: "Autotest Renamed Instructions"), TestContext.Current.CancellationToken)).Response;

        updated.Title.Should().Be("Autotest Renamed Instructions");
        updated.LinkedProjectsCount.Should().Be(1);
    }

    [Fact]
    public async Task LinkedProjectsCount_ProjectAndCustomRoom_CarryNoField()
    {
        var (roomId, projectIds) = await CreateRoomWithProjectsAsync("Autotest Project");
        var customRoom = await CreateRoom(new CreateRoomRequestDto("Autotest Custom Room", roomType: RoomType.CustomRoom));

        var project = await GetRoomInfoAsync(projectIds[0]);
        var custom = await GetRoomInfoAsync(customRoom.Id);
        var section = await GetAiHubFoldersAsync();

        section.Single(f => f.Id == roomId).LinkedProjectsCount.Should().Be(1);
        section.Single(f => f.Id == projectIds[0]).LinkedProjectsCount.Should().BeNull();
        project.LinkedProjectsCount.Should().BeNull();
        custom.LinkedProjectsCount.Should().BeNull();
    }

    [Fact]
    public async Task LinkedProjectsCount_ViewerOfTheRoomOutsideTheProjects_Is2()
    {
        var (roomId, _) = await CreateRoomWithProjectsAsync("Autotest Project One", "Autotest Project Two");
        var viewer = await InviteContact(EmployeeType.User);
        await ShareRoomAsync(roomId, viewer.Id, FileShare.Read);
        await _filesClient.Authenticate(viewer);

        var room = await GetRoomInfoAsync(roomId);

        room.LinkedProjectsCount.Should().Be(2);
    }

    [Fact]
    public async Task LinkedProjectsCount_AfterUnlinkAndProjectDeletion_FollowsTheLinks()
    {
        var (roomId, projectIds) = await CreateRoomWithProjectsAsync("Autotest Project One", "Autotest Project Two");
        var manager = await InviteContact(EmployeeType.RoomAdmin);
        await ShareRoomAsync(projectIds[0], manager.Id, FileShare.RoomManager);
        var linked = (await GetRoomInfoAsync(roomId)).LinkedProjectsCount;

        await _filesClient.Authenticate(manager);
        await UnlinkAsync(projectIds[0], roomId);
        await _filesClient.Authenticate(Owner);
        var afterUnlink = (await GetRoomInfoAsync(roomId)).LinkedProjectsCount;

        await DeleteRoomAndWaitAsync(projectIds[1]);
        var afterDeletion = await PollLinkedProjectsCountAsync(roomId, 0);

        linked.Should().Be(2);
        afterUnlink.Should().Be(1);
        afterDeletion.Should().Be(0);
    }

    private async Task<int?> PollLinkedProjectsCountAsync(int roomId, int expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var count = (await GetRoomInfoAsync(roomId)).LinkedProjectsCount;

            if (count == expected || DateTime.UtcNow >= deadline)
            {
                return count;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}
