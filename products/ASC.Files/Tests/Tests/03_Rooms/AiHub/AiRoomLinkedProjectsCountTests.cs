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
/// The number of AI projects that use a room, <c>linkedProjectsCount</c> of a Knowledge or an Instructions room. The
/// pinned SDK model of a room does not have the field yet, so it is read from the raw JSON of the answers. A Knowledge
/// room needs vectorization, which the test environment does not have, so the room here is an Instructions room.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AiHub")]
public class AiRoomLinkedProjectsCountTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    [Fact]
    public async Task LinkedProjectsCount_RoomLinkedToTwoProjects_Is2InTheSectionAndInTheRoom()
    {
        var (roomId, _) = await CreateRoomWithProjectsAsync("Autotest Project One", "Autotest Project Two");

        var section = await GetAiHubSectionAsync();
        var room = await GetRoomAsync(roomId);

        LinkedProjectsCountOf(FolderOf(section, roomId)).Should().Be(2);
        LinkedProjectsCountOf(room).Should().Be(2);
    }

    [Fact]
    public async Task LinkedProjectsCount_RoomLinkedToNoProject_Is0OnCreateAndOnRead()
    {
        var created = await CreateHubRoomAsync(InstructionsRoomType, "Autotest Unused Instructions");

        var room = await GetRoomAsync(created.GetProperty("id").GetInt32());

        LinkedProjectsCountOf(created).Should().Be(0);
        LinkedProjectsCountOf(room).Should().Be(0);
    }

    [Fact]
    public async Task UpdateRoom_RoomLinkedToOneProject_AnswerCarries1()
    {
        var (roomId, _) = await CreateRoomWithProjectsAsync("Autotest Project");

        using var response = await UpdateRoomResponseAsync(roomId, new { title = "Autotest Renamed Instructions" });
        var updated = await Files.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);

        updated.GetProperty("title").GetString().Should().Be("Autotest Renamed Instructions");
        LinkedProjectsCountOf(updated).Should().Be(1);
    }

    [Fact]
    public async Task LinkedProjectsCount_ProjectAndCustomRoom_CarryNoField()
    {
        var (roomId, projectIds) = await CreateRoomWithProjectsAsync("Autotest Project");
        var customRoom = await CreateRoom(new CreateRoomRequestDto("Autotest Custom Room", roomType: RoomType.CustomRoom));

        var project = await GetRoomAsync(projectIds[0]);
        var custom = await GetRoomAsync(customRoom.Id);
        var section = await GetAiHubSectionAsync();

        LinkedProjectsCountOf(FolderOf(section, roomId)).Should().Be(1);
        FolderOf(section, projectIds[0]).TryGetProperty("linkedProjectsCount", out _).Should().BeFalse();
        project.TryGetProperty("linkedProjectsCount", out _).Should().BeFalse();
        custom.TryGetProperty("linkedProjectsCount", out _).Should().BeFalse();
    }

    [Fact]
    public async Task LinkedProjectsCount_ViewerOfTheRoomOutsideTheProjects_Is2()
    {
        var (roomId, _) = await CreateRoomWithProjectsAsync("Autotest Project One", "Autotest Project Two");
        var viewer = await InviteContact(EmployeeType.User);
        await ShareRoomAsync(roomId, viewer.Id, FileShare.Read);
        await _filesClient.Authenticate(viewer);

        var room = await GetRoomAsync(roomId);

        LinkedProjectsCountOf(room).Should().Be(2);
    }

    [Fact]
    public async Task LinkedProjectsCount_AfterUnlinkAndProjectDeletion_FollowsTheLinks()
    {
        var (roomId, projectIds) = await CreateRoomWithProjectsAsync("Autotest Project One", "Autotest Project Two");
        var manager = await InviteContact(EmployeeType.RoomAdmin);
        await ShareRoomAsync(projectIds[0], manager.Id, FileShare.RoomManager);
        var linked = LinkedProjectsCountOf(await GetRoomAsync(roomId));

        await _filesClient.Authenticate(manager);
        using var unlinked = await UnlinkSourcesAsync(projectIds[0], roomId);
        await _filesClient.Authenticate(Owner);
        var afterUnlink = LinkedProjectsCountOf(await GetRoomAsync(roomId));

        await DeleteRoomAndWaitAsync(projectIds[1]);
        var afterDeletion = await PollLinkedProjectsCountAsync(roomId, 0);

        linked.Should().Be(2);
        unlinked.StatusCode.Should().Be(HttpStatusCode.NoContent);
        afterUnlink.Should().Be(1);
        afterDeletion.Should().Be(0);
    }

    private static JsonElement FolderOf(JsonElement section, int folderId)
    {
        return section.GetProperty("folders").EnumerateArray().Single(f => f.GetProperty("id").GetInt32() == folderId);
    }

    private async Task<int?> PollLinkedProjectsCountAsync(int roomId, int expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var count = LinkedProjectsCountOf(await GetRoomAsync(roomId));

            if (count == expected || DateTime.UtcNow >= deadline)
            {
                return count;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}
