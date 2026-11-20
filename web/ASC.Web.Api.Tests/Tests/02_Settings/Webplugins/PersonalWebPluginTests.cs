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
/// Personal web plugins — installed by a member for themselves with <c>personal=true</c>, listed for that member
/// alone, and shadowed by a portal plugin that takes the same name.
/// </summary>
[Trait("Category", "Settings")]
public class PersonalWebPluginTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task UploadPersonalWebPlugin_User_ListedForOwnerOnly()
    {
        // Arrange
        const string pluginName = "personal-autotest";
        var pluginOwner = await InviteMember(EmployeeType.User);
        var otherUser = await InviteMember(EmployeeType.User);

        // Act
        await _webApiClient.Authenticate(pluginOwner);
        using var uploadResponse = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, pluginName, true, TestContext.Current.CancellationToken);
        var ownerPlugins = await GetPluginsAsync(false);

        await _webApiClient.Authenticate(otherUser);
        var otherPlugins = await GetPluginsAsync(false);
        var otherPersonalPlugins = await GetPluginsAsync(true);

        // Assert
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ownerPlugins.Should().ContainSingle(x => x.Name == pluginName && x.Personal);
        otherPlugins.Should().NotContain(x => x.Name == pluginName);
        otherPersonalPlugins.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadPersonalWebPlugin_NameOfPortalPlugin_Refused()
    {
        // Arrange
        const string pluginName = "portal-autotest";
        using var portalResponse = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, pluginName, TestContext.Current.CancellationToken);
        var user = await InviteMember(EmployeeType.User);

        // Act
        await _webApiClient.Authenticate(user);
        using var personalResponse = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, pluginName, true, TestContext.Current.CancellationToken);

        // Assert
        portalResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        personalResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadPortalWebPlugin_NameOfPersonalPlugin_ShadowsPersonalCopy()
    {
        // Arrange
        const string pluginName = "shadowed-autotest";
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);
        using var personalResponse = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, pluginName, true, TestContext.Current.CancellationToken);

        // Act
        await _webApiClient.Authenticate(Owner);
        using var portalResponse = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, pluginName, TestContext.Current.CancellationToken);

        await _webApiClient.Authenticate(user);
        var availablePlugins = await GetPluginsAsync(false);
        var personalPlugins = await GetPluginsAsync(true);

        // Assert
        personalResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        portalResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        availablePlugins.Should().ContainSingle(x => x.Name == pluginName)
            .Which.Personal.Should().BeFalse();
        personalPlugins.Should().ContainSingle(x => x.Name == pluginName && x.Personal);
    }

    [Fact]
    public async Task UploadPersonalWebPlugin_Guest_Forbidden()
    {
        // Arrange
        var guest = await InviteMember(EmployeeType.Guest);
        await _webApiClient.Authenticate(guest);

        // Act
        using var response = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, "guest-autotest", true, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<List<(string Name, bool Personal)>> GetPluginsAsync(bool personal)
    {
        var url = personal ? "api/2.0/settings/webplugins?personal=true" : "api/2.0/settings/webplugins";

        using var response = await _webApiClient.GetAsync(url, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(body);

        return json.RootElement.GetProperty("response").EnumerateArray()
            .Select(x => (
                x.GetProperty("name").GetString() ?? string.Empty,
                x.TryGetProperty("personal", out var personalProperty) && personalProperty.GetBoolean()))
            .ToList();
    }
}
