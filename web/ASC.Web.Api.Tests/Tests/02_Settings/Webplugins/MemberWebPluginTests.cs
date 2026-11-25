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
/// An administrator's view of the personal web plugins of members (<c>api/2.0/settings/personalwebplugins/plugins</c>):
/// the list with the member and without the member's settings, switching a plugin off for its member and deleting it.
/// </summary>
[Trait("Category", "Settings")]
public class MemberWebPluginTests(
    AspireAppFixture fixture)
    : WebPluginTestBase(fixture)
{
    private const string PluginSettings = "{\"apiKey\":\"member-secret\"}";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetMembersWebPlugins_Owner_ListsThePluginWithItsMemberAndWithoutSettings()
    {
        // Arrange
        const string pluginName = "member-listed";
        var member = await InviteMember(EmployeeType.User);
        await UploadWithSettingsAsync(member, pluginName);

        // Act
        await _webApiClient.Authenticate(Owner);
        var plugins = await ReadMembersPluginsAsync();

        // Assert
        var plugin = plugins.Single(x => x.GetProperty("name").GetString() == pluginName);
        plugin.GetProperty("createBy").GetProperty("id").GetGuid().Should().Be(member.Id);
        plugin.GetProperty("createByType").GetString().Should().Be(nameof(EmployeeType.User));
        plugin.GetProperty("createByIsOwner").GetBoolean().Should().BeFalse();
        plugin.GetProperty("enabled").GetBoolean().Should().BeTrue();
        plugin.GetProperty("personal").GetBoolean().Should().BeTrue();
        (plugin.TryGetProperty("settings", out var settings) ? settings.GetString() : null).Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GetMembersWebPlugins_OwnersOwnPlugin_MarksTheOwner()
    {
        // Arrange
        const string pluginName = "owner-listed";
        await UploadPersonalAsync(Owner, pluginName);

        // Act
        await _webApiClient.Authenticate(Owner);
        var plugins = await ReadMembersPluginsAsync();

        // Assert
        var plugin = plugins.Single(x => x.GetProperty("name").GetString() == pluginName);
        plugin.GetProperty("createBy").GetProperty("id").GetGuid().Should().Be(Owner.Id);
        plugin.GetProperty("createByType").GetString().Should().Be(nameof(EmployeeType.DocSpaceAdmin));
        plugin.GetProperty("createByIsOwner").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task SetMemberWebPlugin_Off_MemberGetsItOffWithItsSettings()
    {
        // Arrange
        const string pluginName = "member-switched";
        var member = await InviteMember(EmployeeType.User);
        await UploadWithSettingsAsync(member, pluginName);

        // Act
        await _webApiClient.Authenticate(Owner);
        using var response = await _webApiClient.PutAsJsonAsync(
            $"api/2.0/settings/personalwebplugins/plugins/{member.Id}/{pluginName}", new { enabled = false }, Ct);
        var (status, body) = await WebPluginsTestData.ReadAsync(response, Ct);
        var plugin = await GetPluginAsMemberAsync(member, pluginName);

        // Assert
        status.Should().Be(200, body);
        plugin.Enabled.Should().BeFalse();
        plugin.Settings.Should().Be(PluginSettings);
    }

    [Fact]
    public async Task DeleteMemberWebPlugin_Owner_RemovesItFromTheMemberOnce()
    {
        // Arrange
        const string pluginName = "member-deleted";
        var member = await InviteMember(EmployeeType.User);
        await UploadWithSettingsAsync(member, pluginName);

        // Act
        await _webApiClient.Authenticate(Owner);
        using var first = await _webApiClient.DeleteAsync($"api/2.0/settings/personalwebplugins/plugins/{member.Id}/{pluginName}", Ct);
        using var second = await _webApiClient.DeleteAsync($"api/2.0/settings/personalwebplugins/plugins/{member.Id}/{pluginName}", Ct);
        await _webApiClient.Authenticate(member);
        var memberPlugins = await ListPluginsAsync(personal: true);

        // Assert
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.NotFound);
        memberPlugins.Should().NotContain(x => x.Name == pluginName);
    }

    [Fact]
    public async Task MembersWebPluginsCalls_Member_Forbidden()
    {
        // Arrange
        const string pluginName = "member-guarded";
        var member = await InviteMember(EmployeeType.User);
        await UploadWithSettingsAsync(member, pluginName);

        // Act
        using var list = await _webApiClient.GetAsync("api/2.0/settings/personalwebplugins/plugins", Ct);
        using var update = await _webApiClient.PutAsJsonAsync(
            $"api/2.0/settings/personalwebplugins/plugins/{member.Id}/{pluginName}", new { enabled = false }, Ct);
        using var delete = await _webApiClient.DeleteAsync($"api/2.0/settings/personalwebplugins/plugins/{member.Id}/{pluginName}", Ct);

        // Assert
        list.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        update.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        delete.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>Uploads a personal plugin as the member and saves settings for it, leaving the client on the member.</summary>
    private async Task UploadWithSettingsAsync(User member, string pluginName)
    {
        await UploadPersonalAsync(member, pluginName);

        using var response = await _webApiClient.PutAsJsonAsync(
            $"api/2.0/settings/webplugins/{pluginName}?personal=true", new { enabled = true, settings = PluginSettings }, Ct);
        var (status, body) = await WebPluginsTestData.ReadAsync(response, Ct);

        status.Should().Be(200, body);
    }

    private async Task<WebPluginView> GetPluginAsMemberAsync(User member, string pluginName)
    {
        await _webApiClient.Authenticate(member);

        return await GetPluginAsync(pluginName, personal: true);
    }

    private async Task<List<JsonElement>> ReadMembersPluginsAsync()
    {
        using var response = await _webApiClient.GetAsync("api/2.0/settings/personalwebplugins/plugins", Ct);
        var (status, body) = await WebPluginsTestData.ReadAsync(response, Ct);
        status.Should().Be(200, body);

        return JsonDocument.Parse(body).RootElement.GetProperty("response").EnumerateArray().Select(x => x.Clone()).ToList();
    }
}
