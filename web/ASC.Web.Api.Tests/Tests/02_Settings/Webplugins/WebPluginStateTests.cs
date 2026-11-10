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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Webplugins;

/// <summary>
/// PUT /api/2.0/settings/webplugins/{name} — switching a plugin on or off and storing its settings string.
/// </summary>
[Trait("Category", "Settings")]
public class WebPluginStateTests(
    AspireAppFixture fixture)
    : WebPluginTestBase(fixture)
{
    private static string StatePath(string name)
    {
        return $"api/2.0/settings/webplugins/{name}";
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task Update_MemberWithoutPortalSettingsRight_ThrowsAccessDeniedAndKeepsTheState(EmployeeType employeeType)
    {
        // Arrange
        await UploadAsOwnerAsync("state-denied");
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webpluginsApi.UpdateWebPluginWithHttpInfoAsync("state-denied", new WebPluginRequest(false, ""), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain("Access denied");
        (await GetPluginAsync("state-denied")).Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task Update_DocSpaceAdmin_SwitchesThePluginOff()
    {
        // Arrange
        await UploadAsOwnerAsync("state-admin");
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var result = await _webpluginsApi.UpdateWebPluginWithHttpInfoAsync("state-admin", new WebPluginRequest(false, ""), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetPluginAsync("state-admin")).Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task Update_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await UploadAsOwnerAsync("state-anonymous");
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webpluginsApi.UpdateWebPluginWithHttpInfoAsync("state-anonymous", new WebPluginRequest(false, ""), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task Update_Settings_AreReturnedVerbatimByBothReads()
    {
        // Arrange
        await UploadAsOwnerAsync("state-settings");
        const string settings = "{\"apiKey\":\"value with spaces\",\"n\":1}";

        // Act
        await UpdatePluginAsync("state-settings", true, settings);

        // Assert
        var member = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(member);
        (await GetPluginAsync("state-settings")).Settings.Should().Be(settings);
        (await ListPluginsAsync()).Single(p => p.Name == "state-settings").Settings.Should().Be(settings);
    }

    [Fact]
    public async Task Update_DisabledWithoutSettings_ReportsThePluginOffWithNoSettings()
    {
        // Arrange
        await UploadAsOwnerAsync("state-off");
        await UpdatePluginAsync("state-off", true, "old");

        // Act
        await UpdatePluginAsync("state-off", false, "");

        // Assert
        var plugin = await GetPluginAsync("state-off");
        plugin.Enabled.Should().BeFalse();
        plugin.Settings.Should().BeNull();
    }

    [Fact]
    public async Task Update_EmptySettings_ReadsBackAsNull()
    {
        // Arrange
        await UploadAsOwnerAsync("state-empty");
        await UpdatePluginAsync("state-empty", true, "old");

        // Act
        await UpdatePluginAsync("state-empty", true, "");

        // Assert
        var plugin = await GetPluginAsync("state-empty");
        plugin.Enabled.Should().BeTrue();
        plugin.Settings.Should().BeNull();
    }

    [Fact]
    public async Task Update_SettingsOf255Characters_AreStored()
    {
        // Arrange
        await UploadAsOwnerAsync("state-255");
        var settings = new string('s', 255);

        // Act
        await UpdatePluginAsync("state-255", true, settings);

        // Assert
        (await GetPluginAsync("state-255")).Settings.Should().Be(settings);
    }

    [Fact]
    public async Task Update_SettingsLongerThan255Characters_ThrowsBadRequest()
    {
        // Arrange
        await UploadAsOwnerAsync("state-256");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webpluginsApi.UpdateWebPluginWithHttpInfoAsync("state-256", new WebPluginRequest(true, new string('s', 256)), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        (await GetPluginAsync("state-256")).Settings.Should().BeNull();
    }

    [Fact]
    public async Task Update_NullSettings_AreAcceptedAsNoSettings()
    {
        // Arrange: the generated WebPluginRequest refuses a null settings client-side, so the body is raw
        await UploadAsOwnerAsync("state-null");
        await UpdatePluginAsync("state-null", true, "old");

        // Act
        using var response = await _webApi.PutRawAsync(StatePath("state-null"), "{\"enabled\":true,\"settings\":null}", TestContext.Current.CancellationToken);

        // Assert
        ((int)response.StatusCode).Should().Be(200, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        (await GetPluginAsync("state-null")).Settings.Should().BeNull();
    }

    [Fact]
    public async Task Update_BodyWithoutSettings_ReturnsBadRequest()
    {
        // Arrange: a body missing a required member cannot be built with the generated model
        await UploadAsOwnerAsync("state-missing");

        // Act
        using var response = await _webApi.PutRawAsync(StatePath("state-missing"), "{\"enabled\":false}", TestContext.Current.CancellationToken);

        // Assert
        ((int)response.StatusCode).Should().Be(400);
        (await GetPluginAsync("state-missing")).Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task Update_UnknownName_ThrowsNotFound()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webpluginsApi.UpdateWebPluginWithHttpInfoAsync("state-unknown", new WebPluginRequest(true, ""), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent!.ToString().Should().Contain("Plugin not found");
    }

    // P1 of the plugins spec: the manifest is read into the object that carries the portal state, so a
    // config.json declaring "enabled": true reports the plugin on whenever the portal has no stored row, and
    // switching it off without settings removes that row.
    [Trait("Category", "Bug")]
    [Fact]
    public async Task Update_ManifestDeclaringEnabled_PluginCanStillBeSwitchedOff()
    {
        // Arrange
        var manifest = WebPluginsTestData.Manifest("state-manifest");
        manifest["enabled"] = true;
        await UploadAsOwnerAsync(manifest);

        // Act
        await UpdatePluginAsync("state-manifest", false, "");

        // Assert
        (await GetPluginAsync("state-manifest")).Enabled.Should().BeFalse();
    }
}
