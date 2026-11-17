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

namespace ASC.Core.Common.Tests;

/// <summary>
/// What the tariff event hands to the socket service. The tariff service sends it from inside its own read of the
/// tariff, under the lock of that read and with the cached tariff just cleared; an event that read the tariff again
/// waited on that lock until it timed out, so the event must take the state it is given and never read the tariff.
/// </summary>
/// <remarks>
/// The tariff service is a stub that fails any call, and the channel is the one the socket service reads, so the test
/// sees the queued request without a socket service running.
/// </remarks>
public sealed class QuotaSocketManagerTests
{
    [Fact]
    public async Task ChangeTariffAsync_GivenTheState_QueuesTheEventWithoutReadingTheTariff()
    {
        var (manager, channel) = CreateManager();

        await manager.ChangeTariffAsync(5, TariffState.Trial);

        channel.Reader.TryRead(out var queued).Should().BeTrue();
        using var request = queued!.RequestMessage;
        request.RequestUri!.AbsolutePath.Should().Be("/controller/files/change-tariff");
        (await request.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("""{"room":"5-quota"}""");
        queued.TariffState.Should().Be(TariffState.Trial);
    }

    [Theory]
    [InlineData(1024L, """{"room":"7-quota","featureId":"total_size","value":1024}""")]
    [InlineData(true, """{"room":"7-quota","featureId":"total_size","value":true}""")]
    public async Task ChangeQuotaFeatureValueAsync_GivenThePortalAndTheState_QueuesTheEventWithoutReadingTheTariff(object value, string body)
    {
        // The quota values of a tariff write go to the portal the write names, with no current tenant (a background
        // job has none) and without reading the tariff. The tariff service hands the values over boxed, as objects.
        var (manager, channel) = CreateManager();

        await manager.ChangeQuotaFeatureValueAsync(7, "total_size", value, TariffState.Paid);

        channel.Reader.TryRead(out var queued).Should().BeTrue();
        using var request = queued!.RequestMessage;
        request.RequestUri!.AbsolutePath.Should().Be("/controller/files/change-quota-feature-value");
        (await request.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be(body);
        queued.TariffState.Should().Be(TariffState.Paid);
    }

    /// <summary>
    /// A notifier with no current tenant (the tenant manager is absent) and a tariff service that fails any call.
    /// </summary>
    private static (QuotaSocketManager Manager, System.Threading.Channels.Channel<SocketData> Channel) CreateManager()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["web:hub:internal"] = "http://localhost:9899/",
                ["core:machinekey"] = "test-machine-key"
            })
            .Build();
        var channel = System.Threading.Channels.Channel.CreateUnbounded<SocketData>();
        var manager = new QuotaSocketManager(
            StubProxy<ITariffService>.Create([]),
            null!,
            channel.Writer,
            new MachinePseudoKeys(configuration),
            configuration);

        return (manager, channel);
    }
}
