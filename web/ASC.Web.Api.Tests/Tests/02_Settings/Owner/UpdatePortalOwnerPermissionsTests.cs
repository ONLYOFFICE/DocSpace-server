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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Owner;

/// <summary>
/// PUT /api/2.0/settings/owner — completes an owner-change transfer. It is guarded by the
/// <c>confirm</c> authentication scheme with role <c>PortalOwnerChange</c>
/// (<c>OwnerController.UpdatePortalOwner</c>), which is a confirmation-email token, not the
/// portal's normal session. Getting a real token requires a mail server this environment does
/// not have, so only the access-control shape is verifiable here: a caller with no session at
/// all is unauthenticated, and a caller with an ordinary (non-confirm) session is authenticated
/// but never holds the <c>PortalOwnerChange</c> role, so it is forbidden regardless of the
/// caller's DocSpace role.
/// </summary>
[Trait("Category", "Settings")]
public class UpdatePortalOwnerPermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task UpdatePortalOwner_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _ownerApi.UpdatePortalOwnerAsync(
                new OwnerIdSettingsRequestDto(Guid.NewGuid()), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task UpdatePortalOwner_RoomAdmin_ThrowsForbidden()
    {
        // Arrange
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _webApiClient.Authenticate(roomAdmin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _ownerApi.UpdatePortalOwnerAsync(
                new OwnerIdSettingsRequestDto(Guid.NewGuid()), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdatePortalOwner_User_ThrowsForbidden()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _ownerApi.UpdatePortalOwnerAsync(
                new OwnerIdSettingsRequestDto(Guid.NewGuid()), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdatePortalOwner_Guest_ThrowsForbidden()
    {
        // Arrange
        var guest = await InviteGuest();
        await _webApiClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _ownerApi.UpdatePortalOwnerAsync(
                new OwnerIdSettingsRequestDto(Guid.NewGuid()), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
