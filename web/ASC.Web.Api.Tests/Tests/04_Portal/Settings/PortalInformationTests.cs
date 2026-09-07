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

namespace ASC.Web.Api.Tests.Tests._04_Portal.Settings;

/// <summary>
/// GET /api/2.0/portal — the current portal information. The controller never denies the
/// request; instead, it demands <c>EditPortalSettings</c> internally and, when the caller lacks
/// it, silently returns a stripped-down <see cref="TenantDto"/> that only carries the tenant ID.
/// </summary>
[Trait("Category", "Portal")]
public class PortalInformationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetPortalInformation_DocSpaceAdmin_ReturnsFullInformation()
    {
        // Arrange
        var member = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(member);

        // Act
        var portal = await _portalSettingsApi.GetPortalInformationAsync(TestContext.Current.CancellationToken);

        // Assert
        portal.StatusCode.Should().Be(200);
        portal.Response.TenantId.Should().BeGreaterThan(0);
        portal.Response.OwnerId.Should().NotBe(Guid.Empty);
        portal.Response.Language.Should().NotBeNullOrEmpty();
        portal.Response.TimeZone.Should().NotBeNullOrEmpty();
        portal.Response.CreationDateTime.Should().NotBe(default);
        portal.Response.LastModified.Should().NotBe(default);
    }

    [Fact]
    public async Task GetPortalInformation_Owner_ReturnsFullInformation()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var portal = await _portalSettingsApi.GetPortalInformationAsync(TestContext.Current.CancellationToken);

        // Assert
        portal.StatusCode.Should().Be(200);
        portal.Response.TenantId.Should().BeGreaterThan(0);
        portal.Response.OwnerId.Should().NotBe(Guid.Empty);
        portal.Response.Language.Should().NotBeNullOrEmpty();
        portal.Response.TimeZone.Should().NotBeNullOrEmpty();
        portal.Response.CreationDateTime.Should().NotBe(default);
        portal.Response.LastModified.Should().NotBe(default);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetPortalInformation_LowerRoles_ReturnsOnlyTenantId(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var portal = await _portalSettingsApi.GetPortalInformationAsync(TestContext.Current.CancellationToken);

        // Assert
        portal.StatusCode.Should().Be(200);
        portal.Response.TenantId.Should().BeGreaterThan(0);
        portal.Response.Calls.Should().BeFalse();
        portal.Response.OwnerId.Should().Be(Guid.Empty);
        portal.Response.CreationDateTime.Should().Be(default);
        portal.Response.LastModified.Should().Be(default);
    }
}
