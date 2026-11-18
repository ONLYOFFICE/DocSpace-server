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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Common;

/// <summary>
/// GET /api/2.0/settings/timezones — the content of the list (<c>SettingsController.GetTimeZones</c>: UTC added
/// when the host lacks it, ordered by the base UTC offset) and GET /api/2.0/settings/machine, which needs the
/// Wizard role of a setup confirmation link that an ordinary session never holds. The services run on the same
/// machine as the tests, so the offsets are looked up in this process's time zone database.
/// </summary>
[Trait("Category", "Settings")]
public class TimeZonesRulesTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetTimeZones_Owner_ListHoldsUtc()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var zones = (await _commonSettingsApi.GetTimeZonesAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        zones.Should().Contain(z => z.Id == "UTC" || z.Id == "Etc/UTC");
    }

    [Fact]
    public async Task GetTimeZones_Owner_ListIsOrderedByBaseUtcOffset()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var zones = (await _commonSettingsApi.GetTimeZonesAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        var offsets = zones
            .Select(z => TimeZoneInfo.TryFindSystemTimeZoneById(z.Id, out var info) ? info.BaseUtcOffset : (TimeSpan?)null)
            .Where(o => o.HasValue)
            .Select(o => o!.Value)
            .ToList();
        offsets.Count.Should().BeGreaterThan(zones.Count / 2);
        offsets.Should().BeInAscendingOrder();
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task GetPortalHostname_OrdinarySession_ThrowsForbidden(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.GetPortalHostnameAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetPortalHostname_OwnerOrdinarySession_ThrowsForbidden()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.GetPortalHostnameAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetPortalHostname_SetupWizardLink_ReturnsTheRequestHost()
    {
        // Arrange: a portal registered without a password hands its Wizard link to an anonymous caller
        using var portal = await Fixture.CreatePortalAsync(model => model.Password = null, TestContext.Current.CancellationToken);
        var token = (await portal.CommonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.WizardToken;
        token.Should().NotBeNullOrEmpty();
        portal.WebApiHttpClient.DefaultRequestHeaders.Add("confirm", token);

        // Act
        var hostname = await portal.CommonSettingsApi.GetPortalHostnameAsync(TestContext.Current.CancellationToken);

        // Assert
        hostname.StatusCode.Should().Be(200);
        hostname.Response.Should().Be(portal.WebApiHttpClient.BaseAddress!.Authority);
    }

    [Fact]
    public async Task GetTimeZones_SetupWizardLinkWithoutSession_ReturnsList()
    {
        // Arrange
        using var portal = await Fixture.CreatePortalAsync(model => model.Password = null, TestContext.Current.CancellationToken);
        var token = (await portal.CommonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.WizardToken;
        token.Should().NotBeNullOrEmpty();
        portal.WebApiHttpClient.DefaultRequestHeaders.Add("confirm", token);

        // Act
        var zones = await portal.CommonSettingsApi.GetTimeZonesAsync(TestContext.Current.CancellationToken);

        // Assert
        zones.StatusCode.Should().Be(200);
        zones.Response.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPortalHostname_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.GetPortalHostnameAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
