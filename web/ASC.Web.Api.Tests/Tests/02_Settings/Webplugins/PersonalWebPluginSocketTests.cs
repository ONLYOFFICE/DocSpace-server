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
/// Who hears about personal web plugins. A change of one plugin goes as <c>s:change-personal-web-plugin</c> with
/// <c>{webPluginName, enabled}</c> to the private room of its member alone, and the portal switch goes as
/// <c>s:change-personal-web-plugins</c> with <c>{enabled}</c> to the quota room every client of the portal joins.
/// </summary>
[Trait("Category", "Settings")]
[Trait("Category", "Socket")]
public class PersonalWebPluginSocketTests(
    AspireAppFixture fixture)
    : WebPluginTestBase(fixture)
{
    private const string PluginEvent = "s:change-personal-web-plugin";
    private const string SwitchEvent = "s:change-personal-web-plugins";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task UploadPersonalWebPlugin_OwnerAndAnotherMember_OnlyTheOwnerIsTold()
    {
        // Arrange
        const string pluginName = "personal-socket";
        var pluginOwner = await InviteMember(EmployeeType.User);
        var otherUser = await InviteMember(EmployeeType.User);
        await Fixture.WaitForRouterAsync().WaitAsync(Ct);
        await using var ownerSocket = await Clients.ConnectSocketAsync(pluginOwner, Ct);
        await using var otherSocket = await Clients.ConnectSocketAsync(otherUser, Ct);

        // Act
        await UploadPersonalAsync(pluginOwner, pluginName);
        var told = await ownerSocket.WaitForAsync(PluginEvent, payload => IsFor(payload, pluginName), cancellationToken: Ct);
        var otherTold = await otherSocket.ArrivesWithinAsync(PluginEvent, payload => IsFor(payload, pluginName), TimeSpan.FromSeconds(3), Ct);

        // Assert
        told.Payload.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo("webPluginName", "enabled");
        told.Payload.GetProperty("enabled").GetBoolean().Should().BeTrue();
        otherTold.Should().BeFalse();
    }

    [Fact]
    public async Task SetPersonalWebPlugins_Off_AMemberOfThePortalIsTold()
    {
        // Arrange
        var member = await InviteMember(EmployeeType.User);
        await Fixture.WaitForRouterAsync().WaitAsync(Ct);
        await using var socket = await Clients.ConnectSocketAsync(member, Ct);
        await socket.SubscribeAsync(false, Ct, "quota");

        // Act
        await SetPersonalWebPluginsAsync(false);
        var told = await socket.WaitForAsync(
            SwitchEvent, payload => payload.TryGetProperty("enabled", out var enabled) && enabled.ValueKind == JsonValueKind.False, cancellationToken: Ct);

        // Assert
        told.Payload.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo("enabled");
    }

    private static bool IsFor(JsonElement payload, string pluginName)
    {
        return payload.TryGetProperty("webPluginName", out var name) && name.GetString() == pluginName;
    }
}
