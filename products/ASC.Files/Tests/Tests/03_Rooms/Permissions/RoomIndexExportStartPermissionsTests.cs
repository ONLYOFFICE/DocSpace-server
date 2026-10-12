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
public class RoomIndexExportStartPermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    // Index export is a manager-level operation on an indexed VDR room: the owner and DocSpace admins
    // can start it, while members invited with any non-manager access level, non-members and anonymous
    // callers cannot. The VDR + RoomManager invitation path is intentionally not asserted — it is
    // non-deterministic at invitation time; the positive manager case is covered deterministically
    // through the owner and the DocSpace admin.

    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    public async Task StartIndexExport_OwnerOrAdmin_Started(EmployeeType? employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom($"Autotest IdxExport AC {employeeType?.ToString() ?? "Owner"}");

        if (employeeType.HasValue)
        {
            var member = await InviteMember(employeeType.Value);
            await _filesClient.Authenticate(member);
        }

        // Act
        var export = (await _roomsApi.StartRoomIndexExportAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        export.Error.Should().BeNullOrEmpty();

        await _roomsApi.TerminateRoomIndexExportAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// No non-manager access level lets an invited member start the export, and neither does simply
    /// being a portal member without an invitation.
    /// </summary>
    [Theory]
    [MemberData(nameof(RoomAccessData.VdrNonManagerInvitedMemberAccesses), MemberType = typeof(RoomAccessData))]
    public async Task StartIndexExport_InvitedMember_Forbidden(EmployeeType employeeType, FileShare access)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom($"Autotest IdxExport AC {employeeType} {access}");

        var member = await InviteMember(employeeType);
        await InviteToRoom(room.Id, member, access);

        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.StartRoomIndexExportAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task StartIndexExport_NonMember_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest IdxExport AC NonMember");

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.StartRoomIndexExportAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task StartIndexExport_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest IdxExport AC Anon");

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.StartRoomIndexExportAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
