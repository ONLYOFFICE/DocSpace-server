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

namespace ASC.Web.Api.Tests.Tests._05_Security.AuditTrail;

/// <summary>
/// GET /api/2.0/security/audit/settings/lifetime — the currently configured login history and
/// audit trail retention lifetimes. Only an Owner or a DocSpaceAdmin may call it.
///
/// The TS suite calls <c>paymentsApi.setupPayment()</c> first; see
/// <see cref="AuditTrailReportTests"/> for why that is not needed on this Aspire-hosted
/// (<c>Standalone</c>) integration portal.
/// </summary>
[Trait("Category", "Security")]
public class AuditSettingsGetTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetAuditSettings_Owner_ReturnsSettings()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var settings = (await _auditTrailDataApi.GetAuditSettingsAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        settings.LoginHistoryLifeTime.Should().BeGreaterThan(0);
        settings.AuditTrailLifeTime.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAuditSettings_DocSpaceAdmin_ReturnsSettings()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var settings = (await _auditTrailDataApi.GetAuditSettingsAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        settings.LoginHistoryLifeTime.Should().BeGreaterThan(0);
        settings.AuditTrailLifeTime.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAuditSettings_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _auditTrailDataApi.GetAuditSettingsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetAuditSettings_NonAdminMember_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _auditTrailDataApi.GetAuditSettingsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}
