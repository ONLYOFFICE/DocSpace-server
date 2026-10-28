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

namespace ASC.ClearEvents.Tests.Infrastructure;

/// <summary>
/// The tariff of each test portal, as the cleanup reads it. A portal no test has set is on a trial, so it gets
/// the free retention.
/// </summary>
/// <remarks>
/// The cleanup asks <see cref="ITariffService"/> exactly two things — the tariff of a tenant and whether it is a
/// free one — so <see cref="Service"/> answers those two and rejects anything else, rather than standing in for
/// the fifty other members of the interface. The pass logs and skips a tenant it fails on, so a rejected call is
/// also recorded in <see cref="UnexpectedCalls"/>, which every pass a test runs is checked against. <see cref="Tariff.Id"/> carries the tenant id from the
/// first call to the second: the real service identifies a tariff by its quotas, which this one has none of.
/// </remarks>
public static class TestTariffs
{
    private static readonly ConcurrentDictionary<int, (TariffState State, bool FreeQuota)> _tariffs = new();
    private static readonly ConcurrentDictionary<int, int> _lookups = new();

    public static ITariffService Service { get; } = DispatchProxy.Create<ITariffService, TariffServiceProxy>();

    /// <summary>The members of <see cref="ITariffService"/> a pass called that <see cref="Service"/> does not answer.</summary>
    public static ConcurrentQueue<string> UnexpectedCalls { get; } = new();

    /// <summary>How many times a pass has asked for the tariff of <paramref name="tenantId"/>.</summary>
    public static int LookupsOf(int tenantId)
    {
        return _lookups.GetValueOrDefault(tenantId);
    }

    /// <summary>
    /// Sets the tariff of <paramref name="tenantId"/>: its <paramref name="state"/>, and whether its quota is a
    /// free one (free, trial, non-profit or custom) whatever the state says.
    /// </summary>
    public static void Set(int tenantId, TariffState state, bool freeQuota = false)
    {
        _tariffs[tenantId] = (state, freeQuota);
    }

    private static (TariffState State, bool FreeQuota) Get(int tenantId)
    {
        return _tariffs.TryGetValue(tenantId, out var tariff) ? tariff : (TariffState.Trial, true);
    }

    /// <summary>Public and unsealed: <see cref="DispatchProxy"/> derives the proxy type from it.</summary>
    public class TariffServiceProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod?.Name)
            {
                case nameof(ITariffService.GetTariffAsync):
                    var tenantId = (int)args![0]!;

                    _lookups.AddOrUpdate(tenantId, 1, (_, count) => count + 1);

                    return Task.FromResult(new Tariff { Id = tenantId, State = Get(tenantId).State, DueDate = DateTime.MaxValue, Quotas = [] });

                case nameof(ITariffService.IsFreeTariffAsync):
                    var tariff = (Tariff)args![0]!;

                    return Task.FromResult(Get(tariff.Id).FreeQuota);

                default:
                    UnexpectedCalls.Enqueue(targetMethod?.Name ?? "?");

                    throw new NotSupportedException(
                        $"The audit cleanup was not expected to call ITariffService.{targetMethod?.Name}; teach {nameof(TestTariffs)} to answer it.");
            }
        }
    }
}
