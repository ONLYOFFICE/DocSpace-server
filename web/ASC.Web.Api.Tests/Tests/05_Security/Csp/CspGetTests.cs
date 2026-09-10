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

namespace ASC.Web.Api.Tests.Tests._05_Security.Csp;

/// <summary>
/// GET /api/2.0/security/csp — reads the portal's Content Security Policy settings. Unlike
/// configuring it, reading is open to every role, including an anonymous caller: the CSP header
/// itself is sent on every response and is not a secret.
/// </summary>
[Trait("Category", "Security")]
public class CspGetTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetCspSettings_Owner_ReturnsConfiguredDomains()
    {
        // Arrange
        var domain = $"https://{Guid.NewGuid():N}.example.com";
        await _webApiClient.Authenticate(Owner);
        await _cspApi.ConfigureCspAsync(new CspRequestsDto([domain]), TestContext.Current.CancellationToken);

        // Act
        var response = await _cspApi.GetCspSettingsWithHttpInfoAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Response.Domains.Should().Contain(domain);
        response.Data.Response.Header.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetCspSettings_ByRole_ReturnsConfiguredDomains(EmployeeType employeeType)
    {
        // Arrange
        var domain = $"https://{Guid.NewGuid():N}.example.com";
        await _webApiClient.Authenticate(Owner);
        await _cspApi.ConfigureCspAsync(new CspRequestsDto([domain]), TestContext.Current.CancellationToken);

        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var response = await _cspApi.GetCspSettingsWithHttpInfoAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Response.Domains.Should().Contain(domain);
        response.Data.Response.Header.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetCspSettings_Anonymous_ReturnsConfiguredDomains()
    {
        // Arrange
        var domain = $"https://{Guid.NewGuid():N}.example.com";
        await _webApiClient.Authenticate(Owner);
        await _cspApi.ConfigureCspAsync(new CspRequestsDto([domain]), TestContext.Current.CancellationToken);

        await _webApiClient.Authenticate(null);

        // Act
        var response = await _cspApi.GetCspSettingsWithHttpInfoAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Response.Domains.Should().Contain(domain);
        response.Data.Response.Header.Should().NotBeNullOrEmpty();
    }
}
