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

namespace ASC.Files.Tests.Tests._06_Operations.Terminate;

/// <summary>
/// <c>PUT /api/2.0/files/fileops/terminate/{id}</c> — access control. The endpoint is
/// <c>[AllowAnonymous]</c> and only reports which of the caller's own active operations it managed
/// to find and cancel, so every caller - including an anonymous one - gets 200; what varies is
/// whether the given id is actually found among that caller's own operations.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class TerminateTasksPermissionsTests(
    AspireAppFixture fixture)
    : OperationsStatusesTestBase(fixture)
{
    [Fact]
    public async Task TerminateTasks_Owner_CanTerminateOwnOperation()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest TerminateTasks Perm Owner.docx", Owner);
        var operationId = await StartDelete(file.Id);

        // Act & Assert
        await _filesOperationsApi.TerminateTasksAsync(operationId, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TerminateTasks_DocSpaceAdmin_CanTerminateOwnOperation()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        var file = await CreateFileInMy("Autotest TerminateTasks Perm Admin.docx", admin);
        var operationId = await StartDelete(file.Id);

        // Act & Assert
        await _filesOperationsApi.TerminateTasksAsync(operationId, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TerminateTasks_User_CanTerminateOwnOperation()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var file = await CreateFileInMy("Autotest TerminateTasks Perm User.docx", user);
        var operationId = await StartDelete(file.Id);

        // Act & Assert
        await _filesOperationsApi.TerminateTasksAsync(operationId, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TerminateTasks_User_CannotTerminateAnotherUsersOperation()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest TerminateTasks Perm CrossUser.docx", Owner);
        var operationId = await StartDelete(file.Id);

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var response = (await _filesOperationsApi.TerminateTasksAsync(operationId, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().BeEmpty("the operation belongs to the owner, not to this user");
    }

    [Fact]
    public async Task TerminateTasks_Guest_ReturnsEmptyForNonExistentOperation()
    {
        // Arrange
        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        // Act
        var response = (await _filesOperationsApi.TerminateTasksAsync(
            "00000000-0000-0000-0000-000000000000", TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().BeEmpty();
    }

    [Fact]
    public async Task TerminateTasks_Anonymous_ReturnsEmpty()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var response = (await _filesOperationsApi.TerminateTasksAsync(
            "00000000-0000-0000-0000-000000000000", TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().BeEmpty();
    }
}
