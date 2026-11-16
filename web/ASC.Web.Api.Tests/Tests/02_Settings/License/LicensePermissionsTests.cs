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
/// Access control of the license endpoints. Upload and the trial activation are limited to DocSpace administrators;
/// activation (<c>settings/license/accept</c>) and refresh (<c>settings/license/refresh</c>) rewrite the
/// installation's license, quota and tariff and must be limited the same way, but demand no right at all (L8 of
/// the branding-and-licensing spec). On this open-source host both return before touching anything, so a member
/// calling them changes nothing.
/// </summary>
[Trait("Category", "Settings")]
public class LicensePermissionsTests(AspireAppFixture fixture) : LicenseTestBase(fixture)
{
    [Fact]
    public async Task AcceptLicense_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await LicenseClient.AcceptLicenseAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task RefreshLicense_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await LicenseClient.RefreshLicenseAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    /// <summary>
    /// L8: activation demands no right, so any member could put a staged license in force for the whole
    /// installation and write the "license key uploaded" audit event. Red while the defect is open.
    /// </summary>
    [Theory]
    [Trait("Category", "Bug")]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task AcceptLicense_NonAdminMember_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteMember(employeeType));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await LicenseClient.AcceptLicenseAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// L8: refresh demands no right, so any member could force a re-read of the installation's license and a
    /// rewrite of its quota and tariff. Red while the defect is open.
    /// </summary>
    [Theory]
    [Trait("Category", "Bug")]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task RefreshLicense_NonAdminMember_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteMember(employeeType));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await LicenseClient.RefreshLicenseAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task UploadLicense_NonAdminMember_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteMember(employeeType));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await LicenseClient.UploadLicenseAsync(NotALicenseFile(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// <c>POST settings/license/trial</c> is hidden from the OpenAPI document (L12), so it is called over raw HTTP.
    /// The administrator check comes before anything is read or written.
    /// </summary>
    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task ActivateTrialLicense_NonAdminMember_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteMember(employeeType));

        // Act
        using var response = await _webApi.PostAsync("api/2.0/settings/license/trial", null, TestContext.Current.CancellationToken);

        // Assert
        ((int)response.StatusCode).Should().Be(403);
    }
}
