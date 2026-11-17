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
/// POST /api/2.0/settings/webplugins with a name the portal already has, and the per-portal count limit.
/// </summary>
[Trait("Category", "Settings")]
public class WebPluginReuploadTests(
    AspireAppFixture fixture)
    : WebPluginTestBase(fixture)
{
    // plugins:maxCount of the stand configuration
    private const int MaxCount = 100;

    [Fact]
    public async Task Reupload_NewVersion_ReplacesThePackageKeepsTheSettingsAndSwitchesItOn()
    {
        // Arrange
        await UploadAsOwnerAsync("re-version");
        await UpdatePluginAsync("re-version", false, "kept-settings");

        // Act
        await UploadAsOwnerAsync("re-version", "2.0.0");

        // Assert
        var plugin = await GetPluginAsync("re-version");
        plugin.Version.Should().Be("2.0.0");
        plugin.Url.Should().EndWith("/re-version/plugin.js?hash=2.0.0");
        plugin.Enabled.Should().BeTrue();
        plugin.Settings.Should().Be("kept-settings");
        (await ListPluginsAsync()).Should().ContainSingle(p => p.Name == "re-version");
    }

    [Fact]
    public async Task Reupload_SameVersion_KeepsTheScriptUrl()
    {
        // Arrange
        await UploadAsOwnerAsync("re-same");
        var before = (await GetPluginAsync("re-same")).Url;

        // Act
        await UploadAsOwnerAsync("re-same");

        // Assert
        (await GetPluginAsync("re-same")).Url.Should().Be(before);
    }

    // Fills the portal up to the limit; installation-wide plugins count too.
    private async Task FillToTheLimitAsync()
    {
        await _webApiClient.Authenticate(Owner);
        var installed = (await ListPluginsAsync()).Count;

        for (var i = installed; i < MaxCount; i++)
        {
            await UploadAsOwnerAsync($"limit-{i:000}");
        }
    }

    [Fact]
    public async Task Upload_PortalAtThePluginLimit_RefusesANewNameButAcceptsAnInstalledOne()
    {
        // Arrange
        await FillToTheLimitAsync();

        // Act + Assert
        await AssertUploadRefusedAsync(WebPluginsTestData.CreatePluginZip("limit-over"), 400, "Wrong plugin archive.");
        await UploadAsOwnerAsync($"limit-{MaxCount - 1:000}", "2.0.0");
        var plugins = await ListPluginsAsync();
        plugins.Should().HaveCount(MaxCount);
        plugins.Select(p => p.Name).Should().NotContain("limit-over");
        plugins.Single(p => p.Name == $"limit-{MaxCount - 1:000}").Version.Should().Be("2.0.0");
    }

    // P9 of the plugins spec: the limit check looks the new name up with GetWebPluginByNameAsync, which throws
    // "Plugin not found" instead of returning null, so the caller reads "Wrong plugin archive. Plugin not found"
    // and the "number of plugins exceeds the limit" text is never sent.
    [Trait("Category", "Bug")]
    [Fact]
    public async Task Upload_PortalAtThePluginLimit_SaysTheLimitIsReached()
    {
        // Arrange
        await FillToTheLimitAsync();

        // Act + Assert
        await AssertUploadRefusedAsync(WebPluginsTestData.CreatePluginZip("limit-over"), 400, "Wrong plugin archive. The number of plugins exceeds the limit");
    }
}
