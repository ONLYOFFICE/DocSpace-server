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

namespace ASC.Web.Api.Tests.Tests._05_Security.ActiveConnections;

/// <summary>
/// PUT /api/2.0/security/activeconnections/logout/{loginEventId} — logging out a single connection,
/// either the caller's own or (for a DocSpaceAdmin/Owner) another user's. See
/// <see cref="LogOutActiveConnectionPermissionsTests"/> for the cases where the target belongs to
/// someone else and the caller is not a DocSpaceAdmin.
/// </summary>
[Trait("Category", "Security")]
public class LogOutActiveConnectionTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.Guest)]
    public async Task LogOutActiveConnection_Member_LogsOutOwnConnection(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);
        var connections = await _activeConnectionsApi.GetAllActiveConnectionsAsync(TestContext.Current.CancellationToken);
        var loginEventId = connections.Response.Items![0].Id;

        // Act
        var result = await _activeConnectionsApi.LogOutActiveConnectionAsync(loginEventId, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().BeTrue();
    }

    [Fact]
    public async Task LogOutActiveConnection_User_LogsOutOwnConnection_InvalidatesToken()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);
        var connections = await _activeConnectionsApi.GetAllActiveConnectionsAsync(TestContext.Current.CancellationToken);
        var loginEventId = connections.Response.Items![0].Id;

        // Act
        var result = await _activeConnectionsApi.LogOutActiveConnectionAsync(loginEventId, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeTrue();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _activeConnectionsApi.GetAllActiveConnectionsAsync(TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task LogOutActiveConnection_Owner_LogsOutAnotherUsersConnection()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);
        var connections = await _activeConnectionsApi.GetAllActiveConnectionsAsync(TestContext.Current.CancellationToken);
        var loginEventId = connections.Response.Items![0].Id;

        // Act
        await _webApiClient.Authenticate(Owner);
        var result = await _activeConnectionsApi.LogOutActiveConnectionAsync(loginEventId, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().BeTrue();
    }

    [Fact]
    public async Task LogOutActiveConnection_DocSpaceAdmin_LogsOutAnotherUsersConnection()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);

        await _webApiClient.Authenticate(Owner);
        var connections = await _activeConnectionsApi.GetAllActiveConnectionsAsync(TestContext.Current.CancellationToken);
        var loginEventId = connections.Response.Items![0].Id;

        // Act
        await _webApiClient.Authenticate(admin);
        var result = await _activeConnectionsApi.LogOutActiveConnectionAsync(loginEventId, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().BeTrue();
        result.Count.Should().Be(1);
    }
}
