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

namespace ASC.Files.Tests.Tests._05_Features.ExternalDbSync;

/// <summary>
/// Access control of <c>/files/rooms/{id}/externaldbsync</c>: only a room manager of the room may
/// use it, including for a portal administrator.
/// </summary>
[Trait("Category", "Rooms")]
public class ExternalDbSyncPermissionsTests(
    AspireAppFixture fixture)
    : ExternalDbSyncTestBase(fixture)
{
    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    public async Task OutsideTheRoom_BothCallsForbiddenAndNothingQueued(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom($"Autotest DbSync {employeeType}");
        var member = await InviteMember(employeeType);

        // Act
        await _filesClient.Authenticate(member);
        var get = await ExpectGetRefused(room.Id);
        var start = await ExpectStartRefused(room.Id);

        // Assert
        get.ErrorCode.Should().Be(403);
        start.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        await AssertNoJob(room.Id);
    }

    /// <remarks>
    /// Editing is not a valid level for a form filling room, so the only lower level the room
    /// accepts (FillForms) is used.
    /// </remarks>
    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    public async Task InvitedAsFormFiller_BothCallsForbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom($"Autotest DbSync {employeeType} FormFiller");
        var member = await InviteMember(employeeType);
        await InviteAndConfirm(room.Id, member, FileShare.FillForms);

        // Act
        await _filesClient.Authenticate(member);
        var get = await ExpectGetRefused(room.Id);
        var start = await ExpectStartRefused(room.Id);

        // Assert
        get.ErrorCode.Should().Be(403);
        start.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        await AssertNoJob(room.Id);
    }

    /// <remarks>
    /// RoomManager can only be granted to a RoomAdmin or above. The member passes the permission
    /// gate: the status call is 200 with no job, and the start is refused for the missing external
    /// database (bug 84473 territory), not for authorisation.
    /// </remarks>
    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    public async Task InvitedAsRoomManager_PassesPermissionGate(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom($"Autotest DbSync {employeeType} Manager");
        var member = await InviteMember(employeeType);
        await InviteAndConfirm(room.Id, member, FileShare.RoomManager);

        // Act
        await _filesClient.Authenticate(member);
        var status = await _roomsApi.GetExternalDbSyncStatusAsync(room.Id, TestContext.Current.CancellationToken);
        var start = await ExpectStartRefused(room.Id);

        // Assert
        status?.Response.Should().BeNull();
        start.ErrorCode.Should().NotBe(403).And.NotBe(401);

        await _filesClient.Authenticate(Owner);
        await AssertNoJob(room.Id);
    }
}
