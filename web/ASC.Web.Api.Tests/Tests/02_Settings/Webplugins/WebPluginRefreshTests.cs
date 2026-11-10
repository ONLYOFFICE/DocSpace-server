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
/// PUT /api/2.0/settings/webplugins/{name}/refresh. The pinned SDK package has no operation for it (P7 of the
/// plugins spec), so the call is raw HTTP.
/// </summary>
[Trait("Category", "Settings")]
public class WebPluginRefreshTests(
    AspireAppFixture fixture)
    : WebPluginTestBase(fixture)
{
    private async Task<(int Status, string Body)> RefreshAsync(string name)
    {
        using var response = await _webApiClient.PutAsync($"api/2.0/settings/webplugins/{name}/refresh", null, TestContext.Current.CancellationToken);

        return await WebPluginsTestData.ReadAsync(response, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task Refresh_MemberWithoutPortalSettingsRight_ReturnsForbidden(EmployeeType employeeType)
    {
        // Arrange
        await UploadAsOwnerAsync("ref-denied");
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var (status, body) = await RefreshAsync("ref-denied");

        // Assert
        status.Should().Be(403, body);
        body.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Refresh_NameInAnotherCase_ReturnsTheServedPlugin()
    {
        // Arrange
        await UploadAsOwnerAsync("ref-served", "1.4.0");

        // Act
        var (status, body) = await RefreshAsync("REF-Served");

        // Assert
        status.Should().Be(200, body);
        using var json = JsonDocument.Parse(body);
        var plugin = json.RootElement.GetProperty("response");
        plugin.GetProperty("name").GetString().Should().Be("ref-served");
        plugin.GetProperty("version").GetString().Should().Be("1.4.0");
        plugin.GetProperty("url").GetString().Should().EndWith("/ref-served/plugin.js?hash=1.4.0");
    }

    [Theory]
    [InlineData("ref-unknown")]
    [InlineData(".ref-dot")]
    public async Task Refresh_UnknownOrInvalidName_ReturnsNotFound(string name)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var (status, body) = await RefreshAsync(name);

        // Assert
        status.Should().Be(404, body);
        body.Should().Contain("Plugin not found");
    }

    [Fact]
    public async Task Refresh_LeavesTheStoredStateSettingsAndCspDomainsUntouched()
    {
        // Arrange
        const string domain = "https://refresh.plugin-csp.example.com";
        var manifest = WebPluginsTestData.Manifest("ref-state");
        manifest["cspDomains"] = domain;
        await UploadAsOwnerAsync(manifest);
        await UpdatePluginAsync("ref-state", false, "kept");
        var domainsBefore = (await _cspApi.GetCspSettingsAsync(TestContext.Current.CancellationToken)).Response.Domains ?? [];

        // Act
        var (status, body) = await RefreshAsync("ref-state");

        // Assert
        status.Should().Be(200, body);
        var plugin = await GetPluginAsync("ref-state");
        plugin.Enabled.Should().BeFalse();
        plugin.Settings.Should().Be("kept");
        var domainsAfter = (await _cspApi.GetCspSettingsAsync(TestContext.Current.CancellationToken)).Response.Domains ?? [];
        domainsAfter.Should().BeEquivalentTo(domainsBefore);
        domainsAfter.Should().NotContain(domain);
    }
}
