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

namespace ASC.Web.Api.Tests.Tests._09_Apps;

/// <summary>
/// PUT /api/2.0/apps/{id}/enabled tells the clients of the portal: <c>AppsSocketManager</c> queues
/// <c>change-app-enabled</c> for the room <c>{tenant}-apps</c>, and the socket service delivers it there as
/// <c>s:change-app-enabled</c> with <c>{id, enabled}</c>. Saving or dropping the settings document sends nothing.
/// </summary>
[Trait("Category", "Apps")]
[Trait("Category", "Socket")]
public class AppsSocketEventsTests(
    AspireAppFixture fixture)
    : AppsTestBase(fixture)
{
    private const string EventName = "s:change-app-enabled";

    [Fact]
    public async Task SetEnabled_ReachesTheAppsRoomWithIdAndFlag_AndSetSettingsSendsNothing()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await Fixture.WaitForRouterAsync().WaitAsync(Ct);
        await using var socket = await Clients.ConnectSocketAsync(Owner, Ct);
        await socket.SubscribeAsync(false, Ct, "apps");

        // Act — a settings save and a drop first, then two switches
        await SaveSettingsAsync(id, """{"socket":"probe"}""");
        await SaveSettingsAsync(id, "null");
        await SaveEnabledAsync(id, false);
        var disabled = await socket.WaitForAsync(EventName, payload => IsFor(payload, id, false), cancellationToken: Ct);
        await SaveEnabledAsync(id, true);
        var enabled = await socket.WaitForAsync(EventName, payload => IsFor(payload, id, true), cancellationToken: Ct);

        // Assert — the payload is exactly {id, enabled}
        disabled.Payload.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo("id", "enabled");
        enabled.Payload.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo("id", "enabled");

        // Assert — the two switches are the only events: the settings writes added none
        await Assert.ThrowsAsync<TimeoutException>(
            async () => await socket.WaitForCountAsync(EventName, _ => true, 3, TimeSpan.FromSeconds(3), Ct));
    }

    private static bool IsFor(JsonElement payload, string id, bool enabled)
    {
        return payload.TryGetProperty("id", out var appId) && appId.GetString() == id
            && payload.TryGetProperty("enabled", out var flag) && flag.ValueKind is JsonValueKind.True or JsonValueKind.False
            && flag.GetBoolean() == enabled;
    }
}
