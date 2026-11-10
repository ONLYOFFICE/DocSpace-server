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
/// GET /api/2.0/settings/webplugins and GET /api/2.0/settings/webplugins/{name}, plus the installation-wide
/// switches the portal settings report.
/// </summary>
[Trait("Category", "Settings")]
public class WebPluginReadTests(
    AspireAppFixture fixture)
    : WebPluginTestBase(fixture)
{
    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetPortalSettings_SignedInMember_ReportsAllThreePluginSwitchesOn(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var settings = (await _commonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert: the stand runs with plugins:enabled, upload and delete all "true"
        settings.Plugins.Should().NotBeNull();
        settings.Plugins.Enabled.Should().BeTrue();
        settings.Plugins.Upload.Should().BeTrue();
        settings.Plugins.Delete.Should().BeTrue();
    }

    [Fact]
    public async Task GetPortalSettings_Anonymous_HasNoPluginSwitches()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var settings = (await _commonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        settings.Plugins.Should().BeNull();
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetWebPlugins_AnyMember_SeesTheUploadedPlugin(EmployeeType employeeType)
    {
        // Arrange
        await UploadAsOwnerAsync("read-list");
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var plugins = await ListPluginsAsync();

        // Assert
        plugins.Should().ContainSingle(p => p.Name == "read-list");
    }

    [Fact]
    public async Task GetWebPlugins_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webpluginsApi.GetWebPluginsAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetWebPlugins_EnabledFilter_ReturnsOnlyPluginsInThatState()
    {
        // Arrange
        await UploadAsOwnerAsync("read-on");
        await UploadAsOwnerAsync("read-off");
        await UpdatePluginAsync("read-off", false);

        // Act
        var enabled = await ListPluginsAsync(true);
        var disabled = await ListPluginsAsync(false);

        // Assert
        enabled.Select(p => p.Name).Should().Contain("read-on").And.NotContain("read-off");
        enabled.Should().OnlyContain(p => p.Enabled);
        disabled.Select(p => p.Name).Should().Contain("read-off").And.NotContain("read-on");
        disabled.Should().OnlyContain(p => !p.Enabled);
    }

    [Fact]
    public async Task GetWebPlugin_UploadedPlugin_ReturnsManifestFieldsAndServerFields()
    {
        // Arrange
        var manifest = WebPluginsTestData.Manifest("read-fields", "1.2.3");
        manifest["minDocSpaceVersion"] = "3.0.0";
        manifest["image"] = "logo.png";
        manifest["homePage"] = "https://example.com/plugin";
        manifest["nameLocale"] = new Dictionary<string, string> { ["en"] = "Fields plugin" };
        manifest["descriptionLocale"] = new Dictionary<string, string> { ["en"] = "Fields description" };
        var before = DateTime.UtcNow.AddMinutes(-5);
        await UploadAsOwnerAsync(manifest);

        // Act
        var plugin = await GetPluginAsync("read-fields");

        // Assert
        plugin.Name.Should().Be("read-fields");
        plugin.Version.Should().Be("1.2.3");
        plugin.MinDocSpaceVersion.Should().Be("3.0.0");
        plugin.Description.Should().Be("Autotest plugin");
        plugin.License.Should().Be("MIT");
        plugin.Author.Should().Be("Autotest");
        plugin.HomePage.Should().Be("https://example.com/plugin");
        plugin.PluginName.Should().Be("readfields");
        plugin.Scopes.Should().Be("rooms");
        plugin.Image.Should().Be("logo.png");
        plugin.NameLocale.Should().Contain("en", "Fields plugin");
        plugin.DescriptionLocale.Should().Contain("en", "Fields description");
        plugin.CreateBy!.Id.Should().Be(Owner.Id);
        plugin.CreateOn.ToUniversalTime().Should().BeAfter(before);
        plugin.Enabled.Should().BeTrue();
        plugin.System.Should().BeFalse();
        plugin.Settings.Should().BeNull();
        plugin.CssUrl.Should().BeNull();
        plugin.Url.Should().Contain("webplugins").And.EndWith("/read-fields/plugin.js?hash=1.2.3");
    }

    [Fact]
    public async Task GetWebPlugin_NameInAnotherCase_ReturnsThePlugin()
    {
        // Arrange
        await UploadAsOwnerAsync("read-case");

        // Act
        var plugin = await GetPluginAsync("READ-Case");

        // Assert
        plugin.Name.Should().Be("read-case");
    }

    // P8 of the plugins spec: the published WebPluginDto lists image, cssUrl, settings and the other optional
    // fields as required, the server omits them when null, so the generated client cannot read such a plugin.
    [Trait("Category", "Bug")]
    [Fact]
    public async Task GetWebPlugin_GeneratedClient_ReadsAPluginWithoutOptionalFields()
    {
        // Arrange
        await UploadAsOwnerAsync("read-sdk");

        // Act
        var plugin = (await _webpluginsApi.GetWebPluginAsync("read-sdk", TestContext.Current.CancellationToken)).Response;
        var plugins = (await _webpluginsApi.GetWebPluginsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        plugin.Name.Should().Be("read-sdk");
        plugins.Should().Contain(p => p.Name == "read-sdk");
    }

    [Fact]
    public async Task GetWebPlugin_UnknownName_ThrowsNotFound()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webpluginsApi.GetWebPluginAsync("read-missing", TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent!.ToString().Should().Contain("Plugin not found");
    }
}
