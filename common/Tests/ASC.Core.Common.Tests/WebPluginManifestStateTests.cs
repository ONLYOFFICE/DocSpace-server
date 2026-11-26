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

namespace ASC.Core.Common.Tests;

/// <summary>
/// What a stored manifest may say about its plugin (<c>WebPluginManager.ClearManifestState</c> and
/// <c>WebPluginManager.ClearUnsafeLinks</c>, applied on upload and, through <c>PrepareStoredPlugin</c>, on every read
/// of the storage): an installation-wide plugin keeps the <c>enabled</c>, <c>settings</c> and <c>cssUrl</c> its
/// manifest declares, a portal or personal plugin takes none of them, so a manifest stored before uploads dropped
/// those fields cannot switch a plugin on or hand out settings; on every level <c>homePage</c> is kept only as an
/// http or https address and <c>image</c> only as a path inside the package.
/// </summary>
public class WebPluginManifestStateTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClearManifestState_ManifestDeclaringState_KeptForAnInstallationWidePluginOnly(bool system)
    {
        // Arrange
        var webPlugin = new WebPlugin { Name = "manifest-state", Enabled = true, Settings = "{\"key\":\"value\"}", CssUrl = "https://example.com/plugin.css" };

        // Act
        WebPluginManager.ClearManifestState(webPlugin, system);

        // Assert
        webPlugin.Enabled.Should().Be(system);
        (webPlugin.Settings != null).Should().Be(system);
        (webPlugin.CssUrl != null).Should().Be(system);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrepareStoredPlugin_PortalOrPersonalManifestDeclaringState_TakesNone(bool personal)
    {
        // Arrange
        var webPlugin = new WebPlugin { Name = "manifest-state", Enabled = true, Settings = "{\"key\":\"value\"}", CssUrl = "https://example.com/plugin.css" };

        // Act
        WebPluginManager.PrepareStoredPlugin(webPlugin, false, personal ? Guid.NewGuid() : null);

        // Assert
        webPlugin.Enabled.Should().BeFalse();
        webPlugin.Settings.Should().BeNull();
        webPlugin.CssUrl.Should().BeNull();
    }

    [Theory]
    [InlineData("https://example.com/plugin", "https://example.com/plugin")]
    [InlineData("http://example.com", "http://example.com")]
    [InlineData("javascript:fetch('/api/2.0/people')", null)]
    [InlineData("data:text/html,<script>alert(1)</script>", null)]
    [InlineData("//example.com", null)]
    [InlineData("example.com", null)]
    public void PrepareStoredPlugin_HomePage_KeptOnlyAsAnHttpAddress(string homePage, string? expected)
    {
        // Arrange
        var webPlugin = new WebPlugin { Name = "manifest-links", HomePage = homePage };

        // Act
        WebPluginManager.PrepareStoredPlugin(webPlugin, false, Guid.NewGuid());

        // Assert
        webPlugin.HomePage.Should().Be(expected);
    }

    [Theory]
    [InlineData("logo.svg", "logo.svg")]
    [InlineData("icons/logo.svg", "icons/logo.svg")]
    [InlineData("../plugin.js", null)]
    [InlineData("/logo.svg", null)]
    [InlineData("https://example.com/logo.svg", null)]
    [InlineData("logo.svg?x=1", null)]
    [InlineData("icons//logo.svg", null)]
    [InlineData("logo.svg#x", null)]
    [InlineData("logo%2Esvg", null)]
    [InlineData("\\logo.svg", null)]
    [InlineData("icons/./logo.svg", null)]
    [InlineData("\t../plugin.js", null)]
    [InlineData("icons/\n../logo.svg", null)]
    public void PrepareStoredPlugin_Image_KeptOnlyAsAPathInsideThePackage(string image, string? expected)
    {
        // Arrange
        var webPlugin = new WebPlugin { Name = "manifest-links", Image = image };

        // Act
        WebPluginManager.PrepareStoredPlugin(webPlugin, false, Guid.NewGuid());

        // Assert
        webPlugin.Image.Should().Be(expected);
    }
}
