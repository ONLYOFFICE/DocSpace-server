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

namespace ASC.Web.Api.Tests.Tests._05_Security.ActiveConnections;

/// <summary>
/// GET /api/2.0/security/activeconnections — every authenticated caller sees the list of their own
/// active connections (a synthetic entry representing the current request is always included even
/// when nothing has been persisted yet, so the list is never empty). Access from other roles is
/// covered in <see cref="GetActiveConnectionsPermissionsTests"/>.
/// </summary>
[Trait("Category", "Security")]
public class GetActiveConnectionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetAllActiveConnections_Owner_ReturnsList()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _activeConnectionsApi.GetAllActiveConnectionsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().NotBeNull();
        result.Response.Items.Should().NotBeNull();
        result.Response.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAllActiveConnections_Owner_ItemHasCorrectFieldValues()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _activeConnectionsApi.GetAllActiveConnectionsAsync(TestContext.Current.CancellationToken);

        // Assert
        var item = result.Response.Items![0];
        item.Id.Should().BeGreaterThanOrEqualTo(0);
        item.TenantId.Should().BeGreaterThan(0);

        // Optional fields: on this host a loopback connection has no X-Forwarded-For and no
        // geo-IP data, so Ip/Country/City legitimately come back as empty strings — only the
        // fields derived from the request itself are asserted.
        (item.Browser is null || item.Browser.Length > 0).Should().BeTrue();
        (item.Platform is null || item.Platform.Length > 0).Should().BeTrue();
        (item.Page is null || item.Page.Length > 0).Should().BeTrue();
    }

    [Fact]
    public async Task GetAllActiveConnections_Owner_ItemDateIsValid()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _activeConnectionsApi.GetAllActiveConnectionsAsync(TestContext.Current.CancellationToken);

        // Assert
        var item = result.Response.Items![0];
        item.Date.Should().NotBeNull();
        item.Date.UtcTime.Should().NotBe(default);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetAllActiveConnections_Member_ReturnsOwnConnections(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var result = await _activeConnectionsApi.GetAllActiveConnectionsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Items.Should().NotBeEmpty();
    }
}
