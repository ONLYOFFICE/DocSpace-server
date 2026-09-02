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

namespace ASC.Web.Api.Tests.Tests._02_Settings.IpRestrictions;

/// <summary>
/// GET /api/2.0/settings/iprestrictions and GET /api/2.0/settings/iprestrictions/settings —
/// reading the portal's IP restrictions and their toggle. Readable by the owner and a
/// DocSpaceAdmin.
///
/// NOTE: no test here ever saves a restriction with <c>enable: true</c> — doing so without also
/// adding the test runner's own IP would lock this portal's client out mid-suite.
/// </summary>
[Trait("Category", "Settings")]
public class IpRestrictionsGetTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetIpRestrictions_Owner_ReturnsRestrictions()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _ipRestrictionsApi.GetIpRestrictionsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetIpRestrictions_DocSpaceAdmin_ReturnsRestrictions()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var result = await _ipRestrictionsApi.GetIpRestrictionsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task ReadIpRestrictionsSettings_Owner_ReturnsSettings()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _ipRestrictionsApi.ReadIpRestrictionsSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().NotBeNull();
        result.Response.LastModified.Should().NotBe(default);
    }

    [Fact]
    public async Task ReadIpRestrictionsSettings_DocSpaceAdmin_ReturnsSettings()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var result = await _ipRestrictionsApi.ReadIpRestrictionsSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().NotBeNull();
    }
}
