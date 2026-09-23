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
/// GET /api/2.0/portal/users/invitationlink/{employeeType} — reading the existing invitation link
/// for a given <see cref="EmployeeType"/>. A <c>null</c> acting role means the portal owner.
/// </summary>
[Trait("Category", "Portal")]
public class InvitationLinkGetTests(
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
    public async Task GetInvitationLinkByEmployeeType_ByRole_ReturnsLink(EmployeeType? actingRole, EmployeeType targetType)
    {
        // Arrange — a fresh portal has no default invitation links, so create one first.
        await CreateLinkAsOwnerAsync(targetType);
        await ActAsAsync(actingRole);

        // Act
        var result = await _portalUsersApi.GetInvitationLinkByEmployeeTypeAsync(targetType, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeNull();
        result.Response.Id.Should().NotBeEmpty();
        result.Response.EmployeeType.Should().Be(targetType);
        result.Response.Url.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetInvitationLinkByEmployeeType_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.GetInvitationLinkByEmployeeTypeAsync(EmployeeType.User, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetInvitationLinkByEmployeeType_ForGuestType_ReturnsBadRequest()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.GetInvitationLinkByEmployeeTypeAsync(EmployeeType.Guest, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("EmployeeType");
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin, EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin, EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.RoomAdmin, EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.User, EmployeeType.User)]
    [InlineData(EmployeeType.Guest, EmployeeType.User)]
    public async Task GetInvitationLinkByEmployeeType_ByRole_ReturnsAccessDenied(EmployeeType actingRole, EmployeeType targetType)
    {
        // Arrange
        await CreateLinkAsOwnerAsync(targetType);
        await ActAsAsync(actingRole);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.GetInvitationLinkByEmployeeTypeAsync(targetType, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    // On the Startup (free) plan the limit of paid users (DocSpaceAdmin + RoomAdmin) is 3. When it
    // is reached the UI blocks inviting paid roles, and the API rejects the read the same way
    // instead of returning 200 with a link that silently downgrades the invitee to User. The link
    // has to be created before the quota is filled, because creating one is gated by the same
    // check the read is.
    [Trait("Bug", "81564")]
    [Fact]
    public async Task GetInvitationLinkByEmployeeType_DocSpaceAdmin_QuotaReached_ReturnsPaymentRequired()
    {
        // Arrange
        await CreateLinkAsOwnerAsync(EmployeeType.DocSpaceAdmin);
        await FillPaidUserQuotaAsync(EmployeeType.DocSpaceAdmin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.GetInvitationLinkByEmployeeTypeAsync(EmployeeType.DocSpaceAdmin, TestContext.Current.CancellationToken));

        // Assert — should return a quota/payment error, not silently downgrade to User.
        exception.ErrorCode.Should().Be(402);
    }

    [Trait("Bug", "81564")]
    [Fact]
    public async Task GetInvitationLinkByEmployeeType_RoomAdmin_QuotaReached_ReturnsPaymentRequired()
    {
        // Arrange
        await CreateLinkAsOwnerAsync(EmployeeType.RoomAdmin);
        await FillPaidUserQuotaAsync(EmployeeType.RoomAdmin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.GetInvitationLinkByEmployeeTypeAsync(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
    }

    /// <summary>
    /// Invites paid members until the portal's <c>manager</c> quota is exhausted, and skips the
    /// calling test when the portal has no such limit: the integration AppHost registers portals
    /// with an unlimited <c>manager</c> feature (value <c>-1</c>), so the quota-reached branch does
    /// not exist there at all. The check being covered lives in
    /// <c>PortalController.GetInvitationLinkByEmployeeType</c> and fires on any limited plan.
    /// </summary>
    private async Task FillPaidUserQuotaAsync(EmployeeType employeeType)
    {
        await _webApiClient.Authenticate(Owner);

        var (limit, used) = await ReadPaidUserQuotaAsync();

        Assert.SkipWhen(limit < 0, "The portal's paid-user quota is unlimited, so it cannot be filled.");

        while (used < limit)
        {
            await InviteMember(employeeType);
            await _webApiClient.Authenticate(Owner);
            (_, used) = await ReadPaidUserQuotaAsync();
        }
    }

    /// <summary>Reads the portal's paid-user (<c>manager</c>) limit and current usage.</summary>
    private async Task<(int Limit, int Used)> ReadPaidUserQuotaAsync()
    {
        var quota = await _paymentApi.GetQuotaPaymentInformationAsync(cancellationToken: TestContext.Current.CancellationToken);
        var manager = quota.Response.Features.Find(f => f.Id == "manager");

        manager.Should().NotBeNull("the paid-user feature is part of every plan");

        return (Convert.ToInt32(manager.Value), Convert.ToInt32(manager.Used?.Value ?? 0));
    }
}
