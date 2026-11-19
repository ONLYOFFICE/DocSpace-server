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

namespace ASC.Core.Billing;

/// <summary>
/// Hands the tariff event and the quota values of a tariff write to the socket service, portal by portal, so that a
/// unit test reaches the fan-out: the writes that start it need a database.
/// </summary>
public static class TariffChangeNotification
{
    /// <summary>
    /// Sends the tariff event and then each quota value to every portal. A portal whose sends fail does not keep the
    /// others from being told; its remaining values are skipped.
    /// </summary>
    /// <param name="portals">The portals whose clients are told.</param>
    /// <param name="features">The quota values to send, by feature name; empty when there are none.</param>
    /// <param name="sendTariffChanged">Sends the tariff event to a portal.</param>
    /// <param name="sendQuotaValue">Sends one quota value to a portal.</param>
    /// <returns>How many portals could not be told, and the first failure.</returns>
    public static async Task<(int FailedPortals, Exception FirstFailure)> SendAsync(
        IEnumerable<int> portals,
        IReadOnlyCollection<(string Feature, object Value)> features,
        Func<int, Task> sendTariffChanged,
        Func<int, string, object, Task> sendQuotaValue)
    {
        var failedPortals = 0;
        Exception firstFailure = null;

        foreach (var portal in portals)
        {
            try
            {
                await sendTariffChanged(portal);

                foreach (var (feature, value) in features)
                {
                    await sendQuotaValue(portal, feature, value);
                }
            }
            catch (Exception ex)
            {
                failedPortals++;
                firstFailure ??= ex;
            }
        }

        return (failedPortals, firstFailure);
    }
}
