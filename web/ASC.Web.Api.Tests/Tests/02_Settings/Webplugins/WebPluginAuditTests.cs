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
/// The audit trail entries of the plugin operations, read through GET /api/2.0/security/audit/events/filter.
/// Audit events are written after the request returns, so the read is polled.
/// </summary>
[Trait("Category", "Settings")]
public class WebPluginAuditTests(
    AspireAppFixture fixture)
    : WebPluginTestBase(fixture)
{
    private static readonly MessageAction[] _pluginActions =
        [MessageAction.WebpluginUploaded, MessageAction.WebpluginUpdated, MessageAction.WebpluginDeleted];

    private async Task<List<AuditEventDto>> PollPluginEventsAsync(int expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);

        while (true)
        {
            var events = (await _auditTrailDataApi.GetAuditEventsByFilterAsync(count: 100, cancellationToken: TestContext.Current.CancellationToken)).Response
                .Where(e => e.ActionId.HasValue && _pluginActions.Contains(e.ActionId.Value))
                .ToList();

            if (events.Count >= expected || DateTime.UtcNow >= deadline)
            {
                return events;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task PluginLifecycle_WritesOneEntryPerChange_WithNameAndVersionAsTarget_AndNoneForReads()
    {
        // Arrange
        await UploadAsOwnerAsync("audit-plugin", "3.1.0");
        var target = $"{WebPluginsTestData.JsName("audit-plugin")}3.1.0";

        // Act: reads and a refresh in between must not be audited
        await GetPluginAsync("audit-plugin");
        await ListPluginsAsync();
        using (var refresh = await _webApiClient.PutAsync("api/2.0/settings/webplugins/audit-plugin/refresh", null, TestContext.Current.CancellationToken))
        {
            refresh.IsSuccessStatusCode.Should().BeTrue();
        }

        await UpdatePluginAsync("audit-plugin", false);
        await _webpluginsApi.DeleteWebPluginWithHttpInfoAsync("audit-plugin", TestContext.Current.CancellationToken);

        // Assert
        var events = await PollPluginEventsAsync(3);
        await Task.Delay(1000, TestContext.Current.CancellationToken);
        events = await PollPluginEventsAsync(3);

        events.Select(e => e.ActionId!.Value).Should().BeEquivalentTo(_pluginActions);
        events.Should().OnlyContain(e => e.Target != null && e.Target.Contains(target));
        events.Should().OnlyContain(e => e.UserId == Owner.Id);
    }
}
