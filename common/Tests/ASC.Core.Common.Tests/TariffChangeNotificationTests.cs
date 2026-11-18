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
/// How a tariff write tells the clients of its portals: the tariff event, then each quota value, portal by portal. The
/// writes that start it need a database, so the fan-out is tested here with senders that record or fail.
/// </summary>
public sealed class TariffChangeNotificationTests
{
    private readonly List<string> _sent = [];

    [Fact]
    public async Task SendAsync_OnePortalFails_TellsTheOthersAndCountsIt()
    {
        var failure = new InvalidOperationException("failed by the test");

        var (failedPortals, firstFailure) = await TariffChangeNotification.SendAsync(
            [1, 2, 3],
            [("total_size", 1024L)],
            portal => portal == 2 ? throw failure : Record($"{portal}:tariff"),
            (portal, feature, value) => Record($"{portal}:{feature}={value}"));

        _sent.Should().Equal("1:tariff", "1:total_size=1024", "3:tariff", "3:total_size=1024");
        failedPortals.Should().Be(1);
        firstFailure.Should().BeSameAs(failure);
    }

    [Fact]
    public async Task SendAsync_AValueFails_SkipsTheRestOfThatPortalOnly()
    {
        var (failedPortals, _) = await TariffChangeNotification.SendAsync(
            [1, 2],
            [("total_size", 1024L), ("room", 5)],
            portal => Record($"{portal}:tariff"),
            (portal, feature, value) => portal == 1 && feature == "total_size"
                ? throw new InvalidOperationException("failed by the test")
                : Record($"{portal}:{feature}={value}"));

        _sent.Should().Equal("1:tariff", "2:tariff", "2:total_size=1024", "2:room=5");
        failedPortals.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_NoValues_SendsTheTariffEventToEachPortal()
    {
        // A deleted tariff has no quota until the next read creates one: only the tariff event goes out.
        var (failedPortals, firstFailure) = await TariffChangeNotification.SendAsync(
            [1, 2],
            [],
            portal => Record($"{portal}:tariff"),
            (portal, feature, value) => Record($"{portal}:{feature}={value}"));

        _sent.Should().Equal("1:tariff", "2:tariff");
        failedPortals.Should().Be(0);
        firstFailure.Should().BeNull();
    }

    private Task Record(string send)
    {
        _sent.Add(send);
        return Task.CompletedTask;
    }
}
