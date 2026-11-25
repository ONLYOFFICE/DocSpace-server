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
/// <c>GET api/2.0/settings/webplugins</c> when the stored settings of a plugin cannot be decrypted, as after a change
/// of the installation's key. The read is open to every member, a guest included, so it reports the plugin as
/// disabled in its answer and changes nothing that is stored.
/// </summary>
[Trait("Category", "Settings")]
public class WebPluginUndecryptableSettingsTests(
    AspireAppFixture fixture)
    : WebPluginTestBase(fixture)
{
    private static readonly Guid _webPluginSettingsId = new("B33CB1F2-1FE6-4BD5-83D0-0D9C217490F5");

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetWebPlugins_StoredSettingsCannotBeDecrypted_ShowsThePluginDisabledAndStoresNothing(EmployeeType employeeType)
    {
        // Arrange: an enabled plugin with settings, whose stored cipher text is then replaced in the database. The
        // API cannot store a value it cannot decrypt, and the settings cache was emptied by the save of the update.
        const string pluginName = "undecryptable";
        await UploadAsOwnerAsync(pluginName);
        await UpdatePluginAsync(pluginName, true, "{\"token\":\"secret\"}");
        var member = await InviteMember(employeeType);

        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        var stored = await ReplaceStoredSettingsAsync(connection, pluginName);

        // Act
        await _webApiClient.Authenticate(member);
        var plugins = await ListPluginsAsync();

        // Assert
        plugins.Should().ContainSingle(p => p.Name == pluginName).Which.Enabled.Should().BeFalse();
        var after = await SettingsAudit.ReadSettingsRowsAsync(connection, TenantId, _webPluginSettingsId);
        after.Should().ContainKey(Guid.Empty).WhoseValue.Should().Be(stored, "a read of the plugins stores nothing");
    }

    /// <summary>
    /// Puts 15 bytes in place of the cipher text of the plugin's settings: valid base64 that is no whole block of the
    /// cipher, so decrypting it always fails. Returns the stored row as it is after the change.
    /// </summary>
    private async Task<string> ReplaceStoredSettingsAsync(MySqlConnector.MySqlConnection connection, string pluginName)
    {
        var rows = await SettingsAudit.ReadSettingsRowsAsync(connection, TenantId, _webPluginSettingsId);
        rows.Should().ContainKey(Guid.Empty, "the update stores the settings of the portal");

        var data = JsonNode.Parse(rows[Guid.Empty])!.AsObject();
        var plugins = data.First(p => p.Key.Equals("EnabledPlugins", StringComparison.OrdinalIgnoreCase)).Value!.AsObject();
        var plugin = plugins[pluginName]!.AsObject();
        var settingsKey = plugin.First(p => p.Key.Equals("Settings", StringComparison.OrdinalIgnoreCase)).Key;
        plugin[settingsKey] = Convert.ToBase64String(new byte[15]);
        var changed = data.ToJsonString();

        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE webstudio_settings SET Data = @data WHERE TenantID = @tenant AND LOWER(ID) = @id AND UserID = @user";
        command.Parameters.AddWithValue("@data", changed);
        command.Parameters.AddWithValue("@tenant", TenantId);
        command.Parameters.AddWithValue("@id", _webPluginSettingsId.ToString().ToLowerInvariant());
        command.Parameters.AddWithValue("@user", Guid.Empty.ToString());
        (await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken)).Should().Be(1);

        return changed;
    }
}
