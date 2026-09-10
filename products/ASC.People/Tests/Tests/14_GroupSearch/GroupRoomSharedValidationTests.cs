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
/// <c>GET /api/2.0/group/room/{id}</c> - validation and edge cases. Split from
/// <see cref="GroupRoomSharedTests"/> to stay under the ~24-case class limit.
///
/// Not ported: "Throws RequiredError for null/undefined room id" - see the remark on
/// <see cref="GroupFileSharedTests"/>.
/// </summary>
public class GroupRoomSharedValidationTests(AspireAppFixture fixture) : GroupSearchTestBase(fixture)
{
    [Fact]
    public async Task GetGroupsWithRoomsShared_Returns404_ForNonExistingRoomId()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _groupSearchApi.GetGroupsWithRoomsSharedAsync(999999999, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_Returns404_ForNegativeRoomId()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _groupSearchApi.GetGroupsWithRoomsSharedAsync(-1, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    // Raw HTTP: the SDK's `id` parameter is a non-nullable int - see GroupFileSharedTests.
    [Fact]
    public async Task GetGroupsWithRoomsShared_Returns404_ForInvalidRoomIdFormat()
    {
        using var response = await _peopleClient.GetAsync("api/2.0/group/room/not-a-number", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_Returns404_ForEmptyRoomId()
    {
        using var response = await _peopleClient.GetAsync("api/2.0/group/room/", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_GroupWithManager_IsReturnedWithManagerInfo()
    {
        var roomId = await CreateCustomRoomAsync();
        var group = await CreateGroupAsync();
        await InviteToRoom(roomId, group.Id, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.First(g => g.Id == group.Id).Manager.Id.Should().Be(Owner.Id);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_GroupWithDisabledMember_IsStillReturnedAsShared()
    {
        var roomId = await CreateCustomRoomAsync();
        var member = await InviteContact(EmployeeType.User);
        var group = await CreateGroupAsync(members: [member.Id]);
        await InviteToRoom(roomId, group.Id, FileShare.Read);
        await TerminateUser(member);

        var result = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.First(g => g.Id == group.Id).Shared.Should().Be(true);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_GroupAppearsOnlyOnce_AfterRepeatedSharingUpdates()
    {
        var roomId = await CreateCustomRoomAsync();
        var group = await CreateGroupAsync();
        await InviteToRoom(roomId, group.Id, FileShare.Read);
        await InviteToRoom(roomId, group.Id, FileShare.Editing);

        var result = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Count(g => g.Id == group.Id).Should().Be(1);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_ResponseItems_ContainExpectedGroupFields()
    {
        var roomId = await CreateCustomRoomAsync();
        var groupName = "Autotest Group " + Guid.NewGuid().ToString()[..8];
        var group = await CreateGroupAsync(name: groupName);
        await InviteToRoom(roomId, group.Id, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // TS also asserts `category` is defined; the .NET DTO types Category as a non-nullable
        // Guid, so "defined" has no separate meaning here.
        var found = result.First(g => g.Id == group.Id);
        found.Id.Should().Be(group.Id);
        found.Name.Should().Be(groupName);
    }
}
