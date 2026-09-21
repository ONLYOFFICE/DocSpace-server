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

namespace ASC.Web.Api.Tests.Tests._04_Portal.Users;

/// <summary>
/// DELETE /api/2.0/portal/users/invitationlink — deleting an invitation link. The link is portal-wide
/// (not owned per-creator), so any role allowed to manage invitation links for a given
/// <see cref="EmployeeType"/> can delete it, regardless of who created it — except when the target
/// role is above what the acting role may itself invite.
/// </summary>
[Trait("Category", "Portal")]
public class InvitationLinkDeleteTests(
    AspireAppFixture fixture)
    : UsersTestBase(fixture)
{
    [Fact]
    public async Task DeleteInvitationLink_Owner_DeletesAndLinkIsGone()
    {
        // Arrange
        var created = await CreateLinkAsOwnerAsync(EmployeeType.User);

        // Act
        var result = await _portalUsersApi.DeleteInvitationLinkAsync(
            new InvitationLinkDeleteRequestDto(created.Id),
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();

        // Deleting the same link again fails, proving it is gone.
        var reDelete = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.DeleteInvitationLinkAsync(
                new InvitationLinkDeleteRequestDto(created.Id),
                TestContext.Current.CancellationToken));
        reDelete.ErrorCode.Should().Be(404);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    public async Task DeleteInvitationLink_ByRole_DeletesLinkForUser(EmployeeType actingRole)
    {
        // Arrange
        var created = await CreateLinkAsOwnerAsync(EmployeeType.User);
        await ActAsAsync(actingRole);

        // Act
        var result = await _portalUsersApi.DeleteInvitationLinkAsync(
            new InvitationLinkDeleteRequestDto(created.Id),
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteInvitationLink_Owner_NonExistentLink_ReturnsNotFound()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.DeleteInvitationLinkAsync(
                new InvitationLinkDeleteRequestDto(Guid.Empty),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteInvitationLink_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.DeleteInvitationLinkAsync(
                new InvitationLinkDeleteRequestDto(Guid.Empty),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin, EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin, EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.DocSpaceAdmin, EmployeeType.DocSpaceAdmin)]
    public async Task DeleteInvitationLink_ByRole_ReturnsAccessDeniedForHigherTarget(EmployeeType actingRole, EmployeeType targetType)
    {
        // Arrange
        var created = await CreateLinkAsOwnerAsync(targetType);
        await ActAsAsync(actingRole);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.DeleteInvitationLinkAsync(
                new InvitationLinkDeleteRequestDto(created.Id),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task DeleteInvitationLink_ByRole_ReturnsAccessDenied(EmployeeType actingRole)
    {
        // Arrange
        var created = await CreateLinkAsOwnerAsync(EmployeeType.User);
        await ActAsAsync(actingRole);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.DeleteInvitationLinkAsync(
                new InvitationLinkDeleteRequestDto(created.Id),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}
