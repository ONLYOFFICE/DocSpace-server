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

namespace ASC.Web.Api.Tests.Tests._05_Security.LoginHistory;

/// <summary>
/// POST /api/2.0/security/audit/login/report — starts generating the login history report (as a
/// Document Builder task) and saves it to "My documents". Only an Owner or a DocSpaceAdmin may call
/// it. The test portal runs in Standalone mode (base domain "localhost"), whose default quota
/// already grants the "audit" feature, so no payment/tariff setup is needed to exercise this
/// endpoint — unlike the TypeScript suite, which calls <c>paymentsApi.setupPayment()</c> for a real
/// SaaS deployment (and is a no-op against a local environment anyway).
/// </summary>
[Trait("Category", "Security")]
public class LoginHistoryReportTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task CreateLoginHistoryReport_Owner_ReturnsTask()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _loginHistoryApi.CreateLoginHistoryReportAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeNull();
        result.Response.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateLoginHistoryReport_DocSpaceAdmin_ReturnsTask()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var result = await _loginHistoryApi.CreateLoginHistoryReportAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeNull();
        result.Response.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateLoginHistoryReport_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _loginHistoryApi.CreateLoginHistoryReportAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task CreateLoginHistoryReport_ByRole_ReturnsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _loginHistoryApi.CreateLoginHistoryReportAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task CreateLoginHistoryReport_WithPeriod_StartsReportGeneration()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var now = DateTime.UtcNow;

        // Act
        var result = await _loginHistoryApi.CreateLoginHistoryReportAsync(from: now.AddDays(-7), to: now, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeNull();
        result.Response.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateLoginHistoryReport_FromBeforeLifetime_StartsReportGeneration()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var now = DateTime.UtcNow;

        // Act
        var result = await _loginHistoryApi.CreateLoginHistoryReportAsync(from: now.AddYears(-10), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeNull();
        result.Response.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateLoginHistoryReport_PeriodEndsBeforeStart_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var now = DateTime.UtcNow;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _loginHistoryApi.CreateLoginHistoryReportAsync(from: now.AddDays(-1), to: now.AddDays(-2), cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateLoginHistoryReport_PeriodOutsideLifetime_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var now = DateTime.UtcNow;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _loginHistoryApi.CreateLoginHistoryReportAsync(to: now.AddYears(-10), cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }
}
