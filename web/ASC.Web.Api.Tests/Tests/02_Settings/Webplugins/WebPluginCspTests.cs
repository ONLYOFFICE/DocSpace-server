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
/// The <c>cspDomains</c> of a plugin manifest: added to the portal Content Security Policy on upload and on
/// enable, removed on disable and delete, read back through GET /api/2.0/security/csp.
/// </summary>
[Trait("Category", "Settings")]
public class WebPluginCspTests(
    AspireAppFixture fixture)
    : WebPluginTestBase(fixture)
{
    private const string FirstDomain = "https://first.plugin-csp.example.com";
    private const string SecondDomain = "https://second.plugin-csp.example.com";

    private async Task<List<string>> CspDomainsAsync()
    {
        return (await _cspApi.GetCspSettingsAsync(TestContext.Current.CancellationToken)).Response.Domains ?? [];
    }

    private static Dictionary<string, object?> ManifestWithDomains(string name, string domains)
    {
        var manifest = WebPluginsTestData.Manifest(name);
        manifest["cspDomains"] = domains;
        return manifest;
    }

    [Fact]
    public async Task CspDomains_FollowUploadDisableEnableAndDelete()
    {
        // Act + Assert: upload adds both domains
        await UploadAsOwnerAsync(ManifestWithDomains("csp-cycle", $"{FirstDomain},{SecondDomain}"));
        (await CspDomainsAsync()).Should().Contain([FirstDomain, SecondDomain]);

        // disable removes them
        await UpdatePluginAsync("csp-cycle", false);
        (await CspDomainsAsync()).Should().NotContain(FirstDomain).And.NotContain(SecondDomain);

        // enable adds them again
        await UpdatePluginAsync("csp-cycle", true);
        (await CspDomainsAsync()).Should().Contain([FirstDomain, SecondDomain]);

        // delete removes them
        await _webpluginsApi.DeleteWebPluginWithHttpInfoAsync("csp-cycle", TestContext.Current.CancellationToken);
        (await CspDomainsAsync()).Should().NotContain(FirstDomain).And.NotContain(SecondDomain);
    }

    [Fact]
    public async Task CspDomains_DisablingOnePlugin_KeepsTheDomainsOfTheOther()
    {
        // Arrange
        await UploadAsOwnerAsync(ManifestWithDomains("csp-first", FirstDomain));
        await UploadAsOwnerAsync(ManifestWithDomains("csp-second", SecondDomain));

        // Act
        await UpdatePluginAsync("csp-first", false);

        // Assert
        var domains = await CspDomainsAsync();
        domains.Should().Contain(SecondDomain).And.NotContain(FirstDomain);
    }

    [Fact]
    public async Task Upload_DomainsOverTheHeaderLimit_ReturnsForbidden()
    {
        // Arrange: 600 domains of ~40 bytes are well over the 15 KB csp:default.maxSize
        var domains = string.Join(',', Enumerable.Range(0, 600).Select(i => $"https://d{i:000}.plugin-csp-limit.example.com"));

        // Act + Assert
        await AssertUploadRefusedAsync(WebPluginsTestData.CreatePluginZip(ManifestWithDomains("csp-limit", domains)), 403, "CSP header size exceeds");
    }

    // P2 of the plugins spec: the plugin is stored and switched on before the policy is saved, so a refused
    // policy leaves the plugin installed although the caller was told 403.
    [Trait("Category", "Bug")]
    [Fact]
    public async Task Upload_DomainsOverTheHeaderLimit_LeavesNoPluginInstalled()
    {
        // Arrange
        var domains = string.Join(',', Enumerable.Range(0, 600).Select(i => $"https://d{i:000}.plugin-csp-limit.example.com"));
        await AssertUploadRefusedAsync(WebPluginsTestData.CreatePluginZip(ManifestWithDomains("csp-atomic", domains)), 403, "CSP header size exceeds");

        // Act
        var plugins = await ListPluginsAsync();

        // Assert
        plugins.Select(p => p.Name).Should().NotContain("csp-atomic");
    }
}
