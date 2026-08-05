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

namespace ASC.Files.Tests.Tests._03_Rooms.Permissions;

[Trait("Category", "Rooms")]
public class RoomIndexExportTerminatePermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task TerminateIndexExport_OwnTask_Terminated(EmployeeType? employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // A RoomAdmin owns the VDR room they create, so this also exercises a non-DocSpaceAdmin
        // manager terminating their own task.
        if (employeeType == EmployeeType.RoomAdmin)
        {
            var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
            await _filesClient.Authenticate(roomAdmin);
        }

        var room = await CreateVirtualRoom($"Autotest IdxTerminate AC {employeeType?.ToString() ?? "Owner"}");

        if (employeeType == EmployeeType.DocSpaceAdmin)
        {
            var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
            await _filesClient.Authenticate(admin);
        }

        await _roomsApi.StartRoomIndexExportAsync(room.Id, TestContext.Current.CancellationToken);

        // Act & Assert - the call completes without throwing
        await _roomsApi.TerminateRoomIndexExportAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Terminate takes no room id and is scoped to the caller's own task, so a low-access member has
    /// nothing to cancel: the call is a per-user no-op (200), not a room-permission 403 like start.
    /// The owner's running task must stay untouched.
    /// </summary>
    [Theory]
    [MemberData(nameof(RoomAccessData.NonManagerInvitedMemberAccesses), MemberType = typeof(RoomAccessData))]
    public async Task TerminateIndexExport_InvitedMember_IsNoOp(EmployeeType employeeType, FileShare access)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom($"Autotest IdxTerminate AC {employeeType} {access}");

        var member = await InviteMember(employeeType);
        await InviteToRoom(room.Id, member, access);

        // The owner starts an export; the member terminating must not affect it.
        await _roomsApi.StartRoomIndexExportAsync(room.Id, TestContext.Current.CancellationToken);
        var ownerBefore = (await _roomsApi.GetRoomIndexExportAsync(TestContext.Current.CancellationToken)).Response;

        // Act
        await _filesClient.Authenticate(member);
        // Act & Assert - the call completes without throwing
        await _roomsApi.TerminateRoomIndexExportAsync(TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
        var ownerAfter = (await _roomsApi.GetRoomIndexExportAsync(TestContext.Current.CancellationToken)).Response;
        ownerAfter.Id.Should().Be(ownerBefore.Id);

        await _roomsApi.TerminateRoomIndexExportAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TerminateIndexExport_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.TerminateRoomIndexExportAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
