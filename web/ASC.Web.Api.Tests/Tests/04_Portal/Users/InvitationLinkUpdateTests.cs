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
/// PUT /api/2.0/portal/users/invitationlink — updating an existing invitation link's
/// <c>maxUseCount</c> or <c>expiration</c>.
/// </summary>
[Trait("Category", "Portal")]
public class InvitationLinkUpdateTests(
    AspireAppFixture fixture)
    : UsersTestBase(fixture)
{
    [Fact]
    public async Task UpdateInvitationLink_Owner_UpdatesMaxUseCount()
    {
        // Arrange
        var created = await CreateLinkAsOwnerAsync(EmployeeType.User, maxUseCount: 5);

        // Act
        var result = await _portalUsersApi.UpdateInvitationLinkAsync(
            new InvitationLinkUpdateRequestDto(created.Id, maxUseCount: 1000),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeNull();
        result.Response.Id.Should().Be(created.Id);
        result.Response.MaxUseCount.Should().Be(1000);
    }

    [Fact]
    public async Task UpdateInvitationLink_Owner_UpdatesExpiration()
    {
        // Arrange
        var created = await CreateLinkAsOwnerAsync(EmployeeType.User);
        var futureDate = DateTime.UtcNow.AddDays(30);

        // Act
        var result = await _portalUsersApi.UpdateInvitationLinkAsync(
            new InvitationLinkUpdateRequestDto(created.Id, futureDate),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeNull();
        result.Response.Id.Should().Be(created.Id);
        result.Response.IsExpired.Should().BeFalse();
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task UpdateInvitationLink_ByRole_UpdatesOwnLink(EmployeeType actingRole)
    {
        // Arrange
        await ActAsAsync(actingRole);
        var created = await _portalUsersApi.CreateInvitationLinkAsync(
            new InvitationLinkCreateRequestDto(EmployeeType.User, maxUseCount: 5),
            TestContext.Current.CancellationToken);

        // Act
        var result = await _portalUsersApi.UpdateInvitationLinkAsync(
            new InvitationLinkUpdateRequestDto(created.Response.Id, maxUseCount: 10),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeNull();
        result.Response.MaxUseCount.Should().Be(10);
    }

    [Fact]
    public async Task UpdateInvitationLink_Owner_MaxUseCountAbove1000_ReturnsBadRequest()
    {
        // Arrange
        var created = await CreateLinkAsOwnerAsync(EmployeeType.User, maxUseCount: 5);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.UpdateInvitationLinkAsync(
                new InvitationLinkUpdateRequestDto(created.Id, maxUseCount: 1001),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task UpdateInvitationLink_Owner_NonExistentLink_ReturnsNotFound()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.UpdateInvitationLinkAsync(
                new InvitationLinkUpdateRequestDto(Guid.Empty, maxUseCount: 10),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateInvitationLink_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.UpdateInvitationLinkAsync(
                new InvitationLinkUpdateRequestDto(Guid.Empty, maxUseCount: 10),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task UpdateInvitationLink_ByRole_ReturnsAccessDenied(EmployeeType actingRole)
    {
        // Arrange
        var created = await CreateLinkAsOwnerAsync(EmployeeType.User);
        await ActAsAsync(actingRole);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.UpdateInvitationLinkAsync(
                new InvitationLinkUpdateRequestDto(created.Id, maxUseCount: 10),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}
