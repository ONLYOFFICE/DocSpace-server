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
/// PUT /api/2.0/security/activeconnections/logoutall/{userId} — a DocSpaceAdmin (or the Owner) logs
/// out all of another user's connections. Returns no body
/// (<c>ConnectionsController.LogOutAllActiveConnectionsForUser</c> is <c>void</c>), so the positive
/// cases only assert the status code. See <see cref="LogOutAllForUserPermissionsTests"/> for the
/// cases where the caller is not entitled to act on someone else's connections.
/// </summary>
[Trait("Category", "Security")]
public class LogOutAllForUserTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task LogOutAllActiveConnectionsForUser_Owner_Succeeds()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _activeConnectionsApi.LogOutAllActiveConnectionsForUserWithHttpInfoAsync(
            user.Id, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogOutAllActiveConnectionsForUser_DocSpaceAdmin_Succeeds()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        var user = await InviteContact(EmployeeType.User);

        await _webApiClient.Authenticate(admin);

        // Act
        var result = await _activeConnectionsApi.LogOutAllActiveConnectionsForUserWithHttpInfoAsync(
            user.Id, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogOutAllActiveConnectionsForUser_Owner_InvalidatesUsersToken()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);
        await _webApiClient.Authenticate(Owner);

        // Act
        await _activeConnectionsApi.LogOutAllActiveConnectionsForUserAsync(user.Id, TestContext.Current.CancellationToken);

        // Assert — the user's cached token is now stale, so signing back in as them reuses it as-is.
        await _webApiClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _activeConnectionsApi.GetAllActiveConnectionsAsync(TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(401);
    }
}
