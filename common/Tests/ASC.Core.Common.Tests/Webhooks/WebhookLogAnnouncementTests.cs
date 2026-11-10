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

namespace ASC.Core.Common.Tests.Webhooks;

/// <summary>
/// <see cref="WebhookSocketManager.AddWebhookLogAsync"/> is called by the publisher for every journal row it writes,
/// and a bulk action writes one per entry and subscription. The event only tells the open journals to re-read, so
/// within one scope a subscription's journal is announced at most once per window; the update-webhook-log of every
/// attempt is never coalesced. The manager is built without its services: the send is counted, not made.
/// </summary>
[Trait("Category", "Webhooks")]
public class WebhookLogAnnouncementTests
{
    [Fact]
    public async Task AddWebhookLog_ManyRowsOfOneSubscription_AnnouncedOncePerWindow()
    {
        var manager = new CountingSocketManager();
        var config = Config(1);

        for (var id = 1; id <= 1000; id++)
        {
            await manager.AddWebhookLogAsync(config, id);
        }

        manager.Sent.Should().ContainSingle().Which.Should().Be(("add-webhook-log", 1));
    }

    [Fact]
    public async Task AddWebhookLog_EachSubscription_IsAnnouncedOnItsOwn()
    {
        var manager = new CountingSocketManager();

        for (var id = 1; id <= 10; id++)
        {
            await manager.AddWebhookLogAsync(Config(1), id);
            await manager.AddWebhookLogAsync(Config(2), id);
            await manager.AddWebhookLogAsync(Config(3), id);
        }

        manager.Sent.Should().BeEquivalentTo([("add-webhook-log", 1), ("add-webhook-log", 2), ("add-webhook-log", 3)]);
    }

    [Fact]
    public async Task AddWebhookLog_AfterTheWindow_IsAnnouncedAgain()
    {
        var manager = new CountingSocketManager();
        var config = Config(1);

        await manager.AddWebhookLogAsync(config, 1);
        manager.Clock.Advance(WebhookSocketManager.AddWebhookLogWindow - TimeSpan.FromMilliseconds(1));
        await manager.AddWebhookLogAsync(config, 2);
        manager.Clock.Advance(TimeSpan.FromMilliseconds(1));
        await manager.AddWebhookLogAsync(config, 3);

        manager.Sent.Should().Equal(("add-webhook-log", 1), ("add-webhook-log", 1));
    }

    [Fact]
    public async Task UpdateWebhookLog_EveryAttempt_IsAnnounced()
    {
        var manager = new CountingSocketManager();
        var config = Config(1);

        await manager.AddWebhookLogAsync(config, 1);

        for (var id = 1; id <= 5; id++)
        {
            await manager.UpdateWebhookLogAsync(config, id);
        }

        manager.Sent.Should().HaveCount(6);
        manager.Sent.Count(s => s.Method == "update-webhook-log").Should().Be(5);
    }

    private static ASC.Webhooks.Core.EF.Model.DbWebhooksConfig Config(int id) => new() { Id = id, TenantId = 1 };

    private sealed class ManualClock : TimeProvider
    {
        private long _timestamp = 1;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _timestamp;

        public void Advance(TimeSpan by) => _timestamp += by.Ticks;
    }

    private sealed class CountingSocketManager() : WebhookSocketManager(null!, null!, null!, null!, null!, null!, null!, NullLogger<WebhookSocketManager>.Instance)
    {
        public ManualClock Clock { get; } = new();

        public List<(string Method, int ConfigId)> Sent { get; } = [];

        protected override TimeProvider TimeProvider => Clock;

        protected override Task SendAsync(string method, ASC.Webhooks.Core.EF.Model.DbWebhooksConfig config, object data)
        {
            Sent.Add((method, config.Id));

            return Task.CompletedTask;
        }
    }
}
