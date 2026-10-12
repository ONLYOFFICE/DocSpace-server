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

namespace ASC.People.Tests.Tests._14_GroupSearch;

/// <summary>
/// <c>GET /api/2.0/group/room/{id}</c> - functional behaviour, pagination and filtering.
/// Validation/edge cases live in <see cref="GroupRoomSharedValidationTests"/> (split to stay
/// under the ~24-case class limit).
///
/// Rooms are shared through <c>RoomsApi.SetRoomSecurityAsync</c> (<c>BaseTest.InviteToRoom</c>),
/// not <c>SharingApi</c> - a room's sharing settings are a distinct endpoint from a plain
/// file/folder's. <c>FileShare.Read</c> and <c>FileShare.Editing</c> are both legal for
/// <c>CustomRoom</c> per <c>FileSecurity.AvailableRoomAccesses</c>.
/// </summary>
public class GroupRoomSharedTests(AspireAppFixture fixture) : GroupSearchTestBase(fixture)
{
    [Fact]
    public async Task GetGroupsWithRoomsShared_ReturnsGroupSharedWithRoom()
    {
        var roomId = await CreateCustomRoomAsync();
        var group = await CreateGroupAsync();
        await InviteToRoom(roomId, group.Id, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Should().Contain(g => g.Id == group.Id && g.Shared == true);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_ReturnsMultipleGroupsSharedWithRoom()
    {
        var roomId = await CreateCustomRoomAsync();
        var group1 = await CreateGroupAsync();
        var group2 = await CreateGroupAsync();

        await _filesClient.Authenticate(Owner);
        await _roomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest
            {
                Invitations = [new RoomInvitation { Id = group1.Id, Access = FileShare.Read }, new RoomInvitation { Id = group2.Id, Access = FileShare.Read }],
                Notify = false
            },
            cancellationToken: TestContext.Current.CancellationToken);

        var result = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedIds = result.Where(g => g.Shared == true).Select(g => g.Id).ToList();

        sharedIds.Should().Contain(group1.Id);
        sharedIds.Should().Contain(group2.Id);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_GroupMarkedShared_AfterReadAccess()
    {
        var roomId = await CreateCustomRoomAsync();
        var group = await CreateGroupAsync();
        await InviteToRoom(roomId, group.Id, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.First(g => g.Id == group.Id).Shared.Should().Be(true);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_GroupStaysShared_AfterAccessChangeFromReadToEditing()
    {
        var roomId = await CreateCustomRoomAsync();
        var group = await CreateGroupAsync();
        await InviteToRoom(roomId, group.Id, FileShare.Read);
        await InviteToRoom(roomId, group.Id, FileShare.Editing);

        var result = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.First(g => g.Id == group.Id).Shared.Should().Be(true);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_GroupWithoutAccess_IsNotMarkedShared()
    {
        var roomId = await CreateCustomRoomAsync();
        var sharedGroup = await CreateGroupAsync();
        var unsharedGroup = await CreateGroupAsync();
        await InviteToRoom(roomId, sharedGroup.Id, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.First(g => g.Id == sharedGroup.Id).Shared.Should().Be(true);
        result.First(g => g.Id == unsharedGroup.Id).Shared.Should().NotBe(true);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_NoGroupsMarkedShared_WhenRoomNotSharedWithAnyGroup()
    {
        var roomId = await CreateCustomRoomAsync();
        await CreateGroupAsync();

        var result = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Where(g => g.Shared == true).Should().BeEmpty();
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_GroupStopsBeingShared_AfterSharingIsRemoved()
    {
        var roomId = await CreateCustomRoomAsync();
        var group = await CreateGroupAsync();
        await InviteToRoom(roomId, group.Id, FileShare.Read);
        await InviteToRoom(roomId, group.Id, FileShare.None);

        var result = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.FirstOrDefault(g => g.Id == group.Id)?.Shared.Should().NotBe(true);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_RepeatedCalls_ReturnTheSameSharedGroup()
    {
        var roomId = await CreateCustomRoomAsync();
        var group = await CreateGroupAsync();
        await InviteToRoom(roomId, group.Id, FileShare.Read);

        var first = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var second = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        first.First(g => g.Id == group.Id).Shared.Should().Be(true);
        second.First(g => g.Id == group.Id).Shared.Should().Be(true);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_ExcludeSharedTrue_ExcludesAlreadySharedGroups()
    {
        var roomId = await CreateCustomRoomAsync();
        var sharedGroup = await CreateGroupAsync();
        var unsharedGroup = await CreateGroupAsync();
        await InviteToRoom(roomId, sharedGroup.Id, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, excludeShared: true, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var ids = result.Select(g => g.Id).ToList();

        ids.Should().NotContain(sharedGroup.Id);
        ids.Should().Contain(unsharedGroup.Id);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_ExcludeSharedFalse_ReturnsAlreadySharedGroups()
    {
        var roomId = await CreateCustomRoomAsync();
        var group = await CreateGroupAsync();
        await InviteToRoom(roomId, group.Id, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, excludeShared: false, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Select(g => g.Id).Should().Contain(group.Id);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_FilterValue_ReturnsOnlyMatchingGroups()
    {
        var roomId = await CreateCustomRoomAsync();
        var uniqueToken = Guid.NewGuid().ToString()[..8];
        var matchingGroup = await CreateGroupAsync(name: $"match-{uniqueToken}");
        var nonMatchingGroup = await CreateGroupAsync(name: $"other-{Guid.NewGuid():N}");

        var result = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, filterValue: uniqueToken, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var ids = result.Select(g => g.Id).ToList();

        ids.Should().Contain(matchingGroup.Id);
        ids.Should().NotContain(nonMatchingGroup.Id);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_FilterValue_WithNoMatch_ReturnsEmptyList()
    {
        var roomId = await CreateCustomRoomAsync();

        var result = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(
            roomId,
            filterValue: $"nomatch-{Guid.NewGuid():N}",
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_EmptyFilterValue_BehavesLikeNoFilter()
    {
        var roomId = await CreateCustomRoomAsync();
        for (var i = 0; i < 2; i++)
        {
            await CreateGroupAsync();
        }

        var noFilter = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var emptyFilter = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, filterValue: "", cancellationToken: TestContext.Current.CancellationToken)).Response;

        emptyFilter.Should().HaveCount(noFilter.Count);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_FilterValueWithSpecialCharacters_DoesNotError()
    {
        var roomId = await CreateCustomRoomAsync();

        var result = await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, filterValue: "test %_$ #@!", cancellationToken: TestContext.Current.CancellationToken);

        result.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_Count_LimitsNumberOfGroupsReturned()
    {
        var roomId = await CreateCustomRoomAsync();
        for (var i = 0; i < 3; i++)
        {
            await CreateGroupAsync();
        }

        var result = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, count: 1, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_StartIndex_OffsetsResultList()
    {
        var roomId = await CreateCustomRoomAsync();
        for (var i = 0; i < 3; i++)
        {
            await CreateGroupAsync();
        }

        var full = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var offset = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, startIndex: 1, cancellationToken: TestContext.Current.CancellationToken)).Response;

        offset.Should().HaveCount(full.Count - 1);
        offset[0].Id.Should().Be(full[1].Id);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_CountAndStartIndex_WorkTogether()
    {
        var roomId = await CreateCustomRoomAsync();
        for (var i = 0; i < 3; i++)
        {
            await CreateGroupAsync();
        }

        var full = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var page = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, count: 1, startIndex: 1, cancellationToken: TestContext.Current.CancellationToken)).Response;

        page.Should().HaveCount(1);
        page[0].Id.Should().Be(full[1].Id);
    }
}
