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
/// POST /api/2.0/settings/webplugins — what the archive and its manifest must contain. Every refusal raised
/// while the archive is read comes back as 400 "Wrong plugin archive. &lt;reason&gt;".
/// </summary>
[Trait("Category", "Settings")]
public class WebPluginPackageTests(
    AspireAppFixture fixture)
    : WebPluginTestBase(fixture)
{
    private static byte[] Png()
    {
        var bytes = new byte[256];
        Random.Shared.NextBytes(bytes);
        return bytes;
    }

    [Fact]
    public async Task Upload_PackageWithoutManifest_ReturnsConfigJsonNotFound()
    {
        var zip = WebPluginsTestData.CreatePluginZip(WebPluginsTestData.Manifest("pkg-noconfig"), withConfig: false);

        await AssertUploadRefusedAsync(zip, 400, "Wrong plugin archive. The required entry config.json not found");
    }

    [Fact]
    public async Task Upload_PackageWithoutEntryScript_ReturnsPluginJsNotFound()
    {
        var zip = WebPluginsTestData.CreatePluginZip(WebPluginsTestData.Manifest("pkg-noscript"), withScript: false);

        await AssertUploadRefusedAsync(zip, 400, "Wrong plugin archive. The required entry plugin.js not found");
    }

    [Fact]
    public async Task Upload_PackageWithStylesheetAndAssets_SetsCssUrlNextToTheScript()
    {
        // Arrange
        await UploadAsOwnerAsync(
            WebPluginsTestData.Manifest("pkg-css"),
            ("plugin.css", WebPluginsTestData.Text("body { color: red; }")),
            ("assets/", null),
            ("assets/logo.png", Png()),
            ("assets/readme.txt", WebPluginsTestData.Text("skipped")));

        // Act
        var plugin = await GetPluginAsync("pkg-css");

        // Assert
        plugin.CssUrl.Should().Be(plugin.Url!.Replace("plugin.js", "plugin.css"));
        plugin.CssUrl.Should().EndWith("/pkg-css/plugin.css?hash=1.0.0");
    }

    [Fact]
    public async Task Upload_MoreThanThirteenEntries_ReturnsTooManyEntries()
    {
        var assets = Enumerable.Range(0, 12).Select(i => ($"assets/{i}.png", (byte[]?)Png())).ToArray();
        var zip = WebPluginsTestData.CreatePluginZip(WebPluginsTestData.Manifest("pkg-many"), true, true, assets);

        await AssertUploadRefusedAsync(zip, 400, "Too many entries in zip file");
    }

    [Fact]
    public async Task Upload_EntryLargerThanFiveMegabytes_ReturnsEntryExceedsMaximumSize()
    {
        var zip = WebPluginsTestData.CreatePluginZip(WebPluginsTestData.Manifest("pkg-bigentry"), true, true, ("assets/big.png", new byte[6 * 1024 * 1024]));

        await AssertUploadRefusedAsync(zip, 400, "The entry assets/big.png exceeds the maximum size");
    }

    [Fact]
    public async Task Upload_EntryCompressedMoreThanHundredToOne_ReturnsCompressionRatioRefused()
    {
        var zip = WebPluginsTestData.CreatePluginZip(WebPluginsTestData.Manifest("pkg-ratio"), true, true, ("assets/zeros.png", new byte[1024 * 1024]));

        await AssertUploadRefusedAsync(zip, 400, "The entry assets/zeros.png exceeds the maximum compression ratio");
    }

    [Fact]
    public async Task Upload_EntryThreeLevelsDeep_ReturnsDirectoryDepthRefused()
    {
        var zip = WebPluginsTestData.CreatePluginZip(WebPluginsTestData.Manifest("pkg-deep"), true, true, ("assets/icons/logo.png", Png()));

        await AssertUploadRefusedAsync(zip, 400, "exceeds the maximum directory depth");
    }

    [Fact]
    public async Task Upload_EntryWithParentPath_ReturnsUnsafePath()
    {
        var zip = WebPluginsTestData.CreatePluginZip(WebPluginsTestData.Manifest("pkg-traversal"), true, true, ("../evil.png", Png()));

        await AssertUploadRefusedAsync(zip, 400, "Unsafe path detected");
    }

    [Fact]
    public async Task Upload_EmptyEntry_ReturnsUnknownSize()
    {
        var zip = WebPluginsTestData.CreatePluginZip(WebPluginsTestData.Manifest("pkg-empty"), true, true, ("plugin.css", []));

        await AssertUploadRefusedAsync(zip, 400, "The entry plugin.css has unknown size");
    }

    [Theory]
    [InlineData("Pkg-Upper")]
    [InlineData(".pkg-dot")]
    [InlineData("pkg space")]
    [InlineData("pkg/slash")]
    public async Task Upload_ManifestNameOutsideTheAllowedPattern_ReturnsWrongPluginName(string name)
    {
        var manifest = WebPluginsTestData.Manifest(name);
        manifest["pluginName"] = "pkgname";

        await AssertUploadRefusedAsync(WebPluginsTestData.CreatePluginZip(manifest), 400, "Wrong plugin name");
    }

    [Theory]
    [InlineData("pkg-js")]
    [InlineData("")]
    public async Task Upload_PluginNameThatIsNotAJavaScriptIdentifier_ReturnsWrongPluginName(string pluginName)
    {
        var manifest = WebPluginsTestData.Manifest("pkg-jsname");
        manifest["pluginName"] = pluginName;

        await AssertUploadRefusedAsync(WebPluginsTestData.CreatePluginZip(manifest), 400, "Wrong plugin name");
    }

    [Fact]
    public async Task Upload_PluginNameOfAnotherPortalPlugin_ReturnsPluginAlreadyExist()
    {
        // Arrange
        await UploadAsOwnerAsync("pkg-first");
        var manifest = WebPluginsTestData.Manifest("pkg-second");
        manifest["pluginName"] = WebPluginsTestData.JsName("pkg-first");

        // Act + Assert
        await AssertUploadRefusedAsync(WebPluginsTestData.CreatePluginZip(manifest), 400, "Plugin already exist");
        (await ListPluginsAsync()).Select(p => p.Name).Should().NotContain("pkg-second");
    }
}
