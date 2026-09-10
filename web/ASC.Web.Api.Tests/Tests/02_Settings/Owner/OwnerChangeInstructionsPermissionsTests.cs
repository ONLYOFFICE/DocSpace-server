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
/// POST /api/2.0/settings/owner — sends the owner-change confirmation email. Guarded by
/// <c>SecurityConstants.EditPortalSettings</c>, which only the portal owner and a DocSpaceAdmin
/// hold. The positive path (Owner/DocSpaceAdmin) cannot be verified here: the portals this suite
/// creates have no activated owner email (no mail server in the integration environment), so the
/// controller always throws 400 "Owner's email is not activated" before anything else happens —
/// see <c>OwnerController.SendOwnerChangeInstructions</c>. Only the negative, role-based paths are
/// verifiable, which is what this class covers.
/// </summary>
[Trait("Category", "Settings")]
public class OwnerChangeInstructionsPermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SendOwnerChangeInstructions_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        var newOwner = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _ownerApi.SendOwnerChangeInstructionsAsync(
                new OwnerIdSettingsRequestDto(newOwner.Id), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task SendOwnerChangeInstructions_RoomAdmin_ThrowsForbidden()
    {
        // Arrange
        var newOwner = await InviteContact(EmployeeType.User);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _webApiClient.Authenticate(roomAdmin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _ownerApi.SendOwnerChangeInstructionsAsync(
                new OwnerIdSettingsRequestDto(newOwner.Id), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SendOwnerChangeInstructions_User_ThrowsForbidden()
    {
        // Arrange
        var newOwner = await InviteContact(EmployeeType.User);
        var user = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _ownerApi.SendOwnerChangeInstructionsAsync(
                new OwnerIdSettingsRequestDto(newOwner.Id), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SendOwnerChangeInstructions_Guest_ThrowsForbidden()
    {
        // Arrange
        var newOwner = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();
        await _webApiClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _ownerApi.SendOwnerChangeInstructionsAsync(
                new OwnerIdSettingsRequestDto(newOwner.Id), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
