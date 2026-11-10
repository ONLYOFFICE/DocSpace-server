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

namespace ASC.Web.Api.Tests.Tests._02_Settings.License;

/// <summary>
/// The license endpoints and the edition flags on the open-source edition of the integration host: a standalone
/// installation without a license path. None of these calls can change the installation's license.
/// </summary>
[Trait("Category", "Settings")]
public class LicenseOpenSourceTests(AspireAppFixture fixture) : LicenseTestBase(fixture)
{
    [Fact]
    public async Task AcceptLicense_OwnerOnOpenSource_AnswersNotAllowedOptionWith200()
    {
        // Act
        var response = await LicenseClient.AcceptLicenseAsync(TestContext.Current.CancellationToken);

        // Assert: the outcome is in the body, not in the status
        response.StatusCode.Should().Be(200);
        response.Response.Should().Be(NotAllowedOption);
    }

    [Fact]
    public async Task RefreshLicense_OwnerOnOpenSource_ReturnsFalse()
    {
        // Act
        var response = await LicenseClient.RefreshLicenseAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
        response.Response.Should().BeFalse();
    }

    [Fact]
    public async Task UploadLicense_OwnerOnOpenSource_ThrowsUnsupportedBeforeReadingTheFile()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await LicenseClient.UploadLicenseAsync(NotALicenseFile(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(415);
        exception.ErrorContent!.ToString().Should().Contain(NotAllowedOption);
    }

    [Fact]
    public async Task GetIsLicenseRequired_AnonymousOnOpenSource_ReturnsFalse()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var response = await LicenseClient.GetIsLicenseRequiredAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
        response.Response.Should().BeFalse();
    }

    [Fact]
    public async Task GetPortalSettings_Anonymous_ReportsStandalone()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var response = await _commonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.Response.Standalone.Should().BeTrue();
    }

    [Fact]
    public async Task GetPortalTariff_Owner_ReportsOpenSourceEditionAndPaidState()
    {
        // Act
        var tariff = (await _portalQuotaApi.GetPortalTariffAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        tariff.State.Should().Be(TariffState.Paid);
        tariff.OpenSource.Should().BeTrue();
        tariff.Enterprise.Should().BeFalse();
        tariff.Developer.Should().BeFalse();
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task GetPortalTariff_MemberWithoutPortalSettingsRight_GetsNoEditionFlags(EmployeeType employeeType)
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteMember(employeeType));

        // Act
        var tariff = (await _portalQuotaApi.GetPortalTariffAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        tariff.OpenSource.Should().BeNull();
        tariff.Enterprise.Should().BeNull();
        tariff.Developer.Should().BeNull();
    }

    [Fact]
    public async Task GetPaymentSettings_Owner_ReturnsEnterpriseBuyUrlAndDefaultQuantityCap()
    {
        // Act
        var payment = (await _commonSettingsApi.GetPaymentSettingsAsync(TestContext.Current.CancellationToken)).Response;

        // Assert: no license type configured → the "buyenterprise" site entry; no max-quantity configured → 999
        payment.Standalone.Should().BeTrue();
        payment.BuyUrl.Should().EndWith("type=buydocspaceenterprise");
        payment.Max.Should().Be(999);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task GetPaymentSettings_MemberWithoutPortalSettingsRight_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteMember(employeeType));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.GetPaymentSettingsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
