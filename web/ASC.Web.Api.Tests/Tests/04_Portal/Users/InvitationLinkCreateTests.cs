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
/// POST /api/2.0/portal/users/invitationlink — creating an invitation link for a given
/// <see cref="EmployeeType"/>. A <c>null</c> acting role means the portal owner.
/// </summary>
[Trait("Category", "Portal")]
public class InvitationLinkCreateTests(
    AspireAppFixture fixture)
    : UsersTestBase(fixture)
{
    [Theory]
    [InlineData(null, EmployeeType.User)]
    [InlineData(null, EmployeeType.DocSpaceAdmin)]
    [InlineData(null, EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.DocSpaceAdmin, EmployeeType.User)]
    [InlineData(EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.RoomAdmin, EmployeeType.User)]
    public async Task CreateInvitationLink_ByRole_CreatesLinkForTarget(EmployeeType? actingRole, EmployeeType targetType)
    {
        // Arrange
        await ActAsAsync(actingRole);

        // Act
        var result = await _portalUsersApi.CreateInvitationLinkAsync(
            new InvitationLinkCreateRequestDto(targetType),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeNull();
        result.Response.Id.Should().NotBeEmpty();
        result.Response.EmployeeType.Should().Be(targetType);
        result.Response.Url.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateInvitationLink_Owner_WithFutureExpiration_IsNotExpired()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(7);

        // Act
        var result = await _portalUsersApi.CreateInvitationLinkAsync(
            new InvitationLinkCreateRequestDto(EmployeeType.User, futureDate),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeNull();
        result.Response.IsExpired.Should().BeFalse();
        result.Response.Url.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateInvitationLink_Owner_WithMaxUseCount1000_IsAccepted()
    {
        // Act
        var result = await _portalUsersApi.CreateInvitationLinkAsync(
            new InvitationLinkCreateRequestDto(EmployeeType.User, maxUseCount: 1000),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeNull();
        result.Response.MaxUseCount.Should().Be(1000);
        result.Response.CurrentUseCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateInvitationLink_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.CreateInvitationLinkAsync(
                new InvitationLinkCreateRequestDto(EmployeeType.User),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(null, EmployeeType.Guest)]
    [InlineData(EmployeeType.DocSpaceAdmin, EmployeeType.Guest)]
    [InlineData(EmployeeType.RoomAdmin, EmployeeType.Guest)]
    [InlineData(EmployeeType.User, EmployeeType.Guest)]
    public async Task CreateInvitationLink_ForGuestType_ReturnsBadRequest(EmployeeType? actingRole, EmployeeType targetType)
    {
        // Arrange
        await ActAsAsync(actingRole);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.CreateInvitationLinkAsync(
                new InvitationLinkCreateRequestDto(targetType),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("EmployeeType");
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin, EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin, EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin, EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User, EmployeeType.User)]
    [InlineData(EmployeeType.Guest, EmployeeType.User)]
    [InlineData(EmployeeType.Guest, EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.Guest, EmployeeType.DocSpaceAdmin)]
    public async Task CreateInvitationLink_ByRole_ReturnsAccessDenied(EmployeeType actingRole, EmployeeType targetType)
    {
        // Arrange
        await ActAsAsync(actingRole);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.CreateInvitationLinkAsync(
                new InvitationLinkCreateRequestDto(targetType),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task CreateInvitationLink_Owner_MaxUseCountAbove1000_ReturnsBadRequest()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.CreateInvitationLinkAsync(
                new InvitationLinkCreateRequestDto(EmployeeType.User, maxUseCount: 1001),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("The field MaxUseCount must be between 1 and 1000.");
    }

    [Fact]
    public async Task CreateInvitationLink_Owner_PastExpiration_ReturnsBadRequest()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.CreateInvitationLinkAsync(
                new InvitationLinkCreateRequestDto(EmployeeType.User, pastDate),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Expiration");
    }
}
