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
/// PUT /api/2.0/settings/iprestrictions and PUT /api/2.0/settings/iprestrictions/settings —
/// saving the IP restriction list and toggling it. Writable by the owner and a DocSpaceAdmin.
///
/// NOTE: every save here uses <c>enable: false</c>. Saving a restriction with <c>enable: true</c>
/// without also adding the test runner's own IP would lock this portal's client out mid-suite —
/// each test owns its own portal, so there is nothing to clean up either way.
/// </summary>
[Trait("Category", "Settings")]
public class IpRestrictionsSaveTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SaveIpRestrictions_Owner_SavesDisabledRestrictions()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var dto = new IpRestrictionsDto([new IpRestrictionBase("192.168.1.1", false)], false);

        // Act
        var result = await _ipRestrictionsApi.SaveIpRestrictionsAsync(dto, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Enable.Should().BeFalse();
        result.Response.IpRestrictions.Should().ContainSingle(r => r.Ip == "192.168.1.1" && r.ForAdmin == false);
    }

    [Fact]
    public async Task SaveIpRestrictions_DocSpaceAdmin_SavesDisabledRestrictions()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);
        var dto = new IpRestrictionsDto([new IpRestrictionBase("192.168.1.1", false)], false);

        // Act
        var result = await _ipRestrictionsApi.SaveIpRestrictionsAsync(dto, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Enable.Should().BeFalse();
    }

    // IpRestrictionsDto's only public constructor throws client-side when ipRestrictions is null
    // ("ipRestrictions is a required property"), but disabling the settings this way sends
    // ipRestrictions: null — a value the typed constructor cannot carry — so this goes raw.
    // Verbatim JSON, because RawApiClient's serializer omits null properties and the server-side
    // DTO marks ipRestrictions `required`: an absent property is 400, an explicit null binds.
    [Fact]
    public async Task UpdateIpRestrictionsSettings_Owner_DisablesRestrictions()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        using var response = await _webApi.PutRawAsync(
            "api/2.0/settings/iprestrictions/settings",
            """{"ipRestrictions":null,"enable":false}""",
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(body);
        var settingsResponse = json.RootElement.GetProperty("response");
        settingsResponse.GetProperty("enable").GetBoolean().Should().BeFalse();
        settingsResponse.GetProperty("ipRestrictions").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task UpdateIpRestrictionsSettings_DocSpaceAdmin_DisablesRestrictions()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        using var response = await _webApi.PutRawAsync(
            "api/2.0/settings/iprestrictions/settings",
            """{"ipRestrictions":null,"enable":false}""",
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("response").GetProperty("enable").GetBoolean().Should().BeFalse();
    }
}
