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

namespace ASC.Web.Api.Tests.Tests._08_OAuth2.ClientManagement;

/// <summary>
/// DELETE /api/2.0/clients/tenant — only an owner or DocSpaceAdmin may wipe the whole tenant's
/// OAuth2 clients; every lesser role, including a signed-in RoomAdmin, is refused.
/// </summary>
[Trait("Category", "OAuth2")]
public class DeleteTenantClientsPermissionsTests(
    AspireAppFixture fixture)
    : ClientManagementTestBase(fixture)
{
    [Fact]
    public async Task DeleteTenantClients_Anonymous_ThrowsForbidden()
    {
        // Arrange
        ApplySignature(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.DeleteTenantClientsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteTenantClients_Guest_ThrowsForbidden()
    {
        // Arrange
        var guest = await InviteGuest();
        await _webApiClient.Authenticate(guest);
        ApplySignature(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.DeleteTenantClientsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteTenantClients_RoomAdmin_ThrowsForbidden()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.RoomAdmin);
        await ApplySignatureAsync(admin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.DeleteTenantClientsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteTenantClients_User_ThrowsForbidden()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await ApplySignatureAsync(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.DeleteTenantClientsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
