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
/// The portal switch of personal web plugins (<c>POST api/2.0/settings/personalwebplugins</c>): a new portal has it
/// off, an administrator turns it on and off, and while it is off the personal plugins of every member are hidden
/// and kept.
/// </summary>
[Trait("Category", "Settings")]
public class PersonalWebPluginsSwitchTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task NewPortal_Member_PersonalPluginsOffAndUploadForbidden()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteMember(EmployeeType.User));

        // Act
        var plugins = await ReadPluginSwitchesAsync();
        using var uploadResponse = await WebPluginsTestData.UploadPluginAsync(_webApiClient, "personal-off", true, Ct);

        // Assert
        plugins.GetProperty("personal").GetBoolean().Should().BeFalse();
        plugins.GetProperty("personalAvailable").GetBoolean().Should().BeTrue();
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetPersonalWebPlugins_OffThenOn_HidesTheMemberPluginAndBringsItBack()
    {
        // Arrange
        const string pluginName = "personal-switch";
        var member = await InviteMember(EmployeeType.User);
        await SetAsync(true);
        await _webApiClient.Authenticate(member);
        using var uploadResponse = await WebPluginsTestData.UploadPluginAsync(_webApiClient, pluginName, true, Ct);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        await SetAsync(false);
        await _webApiClient.Authenticate(member);
        var whileOff = await ReadPluginSwitchesAsync();
        using var listWhileOff = await _webApiClient.GetAsync("api/2.0/settings/webplugins?personal=true", Ct);
        var availableWhileOff = await ListAvailableAsync();

        await SetAsync(true);
        await _webApiClient.Authenticate(member);
        var availableAfter = await ListAvailableAsync();

        // Assert
        whileOff.GetProperty("personal").GetBoolean().Should().BeFalse();
        listWhileOff.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        availableWhileOff.Should().NotContain(pluginName);
        availableAfter.Should().Contain(pluginName);
    }

    [Fact]
    public async Task SetPersonalWebPlugins_Member_Forbidden()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteMember(EmployeeType.User));

        // Act
        using var response = await _webApiClient.PostAsJsonAsync("api/2.0/settings/personalwebplugins", new { enabled = true }, Ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task SetAsync(bool enabled)
    {
        await _webApiClient.Authenticate(Owner);

        using var response = await _webApiClient.PostAsJsonAsync("api/2.0/settings/personalwebplugins", new { enabled }, Ct);
        var (status, body) = await WebPluginsTestData.ReadAsync(response, Ct);

        status.Should().Be(200, body);
    }

    private async Task<JsonElement> ReadPluginSwitchesAsync()
    {
        using var response = await _webApiClient.GetAsync("api/2.0/settings", Ct);
        var (status, body) = await WebPluginsTestData.ReadAsync(response, Ct);
        status.Should().Be(200, body);

        return JsonDocument.Parse(body).RootElement.GetProperty("response").GetProperty("plugins").Clone();
    }

    private async Task<List<string>> ListAvailableAsync()
    {
        using var response = await _webApiClient.GetAsync("api/2.0/settings/webplugins", Ct);
        var (status, body) = await WebPluginsTestData.ReadAsync(response, Ct);
        status.Should().Be(200, body);

        return JsonDocument.Parse(body).RootElement.GetProperty("response").EnumerateArray()
            .Select(x => x.GetProperty("name").GetString() ?? string.Empty)
            .ToList();
    }
}
