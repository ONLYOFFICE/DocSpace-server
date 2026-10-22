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
/// The spending limit in force for one user, as the lists of users show it: only what they need of the limit and its
/// spending, since a list of them is cached for every user of the portal.
/// </summary>
public class UserServiceLimit
{
    /// <summary>
    /// The service limit ID.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Whether the limit is the user's own or the one for every user.
    /// </summary>
    public ServiceLimitScope Scope { get; init; }

    /// <summary>
    /// The money threshold per period.
    /// </summary>
    public decimal? AmountValue { get; init; }

    /// <summary>
    /// The currency of the money threshold and of the spending.
    /// </summary>
    public string Currency { get; init; }

    /// <summary>
    /// The window the threshold applies to.
    /// </summary>
    public ServiceLimitPeriod Period { get; init; }

    /// <summary>
    /// The money spent in the current period; 0 when nothing is counted for the user yet.
    /// </summary>
    public decimal AmountConsumed { get; init; }

    /// <summary>
    /// The end of the current period, in UTC; null for a lifetime limit and when nothing is counted for the user yet.
    /// </summary>
    public DateTime? PeriodEnd { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class UserServiceLimitMapper
{
    [MapPropertyFromSource(nameof(UserServiceLimit.AmountConsumed), Use = nameof(GetAmountConsumed))]
    [MapProperty(nameof(@ServiceLimit.Usage.PeriodEnd), nameof(UserServiceLimit.PeriodEnd))]
    public static partial UserServiceLimit ToUserServiceLimit(this ServiceLimit source);

    // The limit for every user comes without spending for a user the accounting service has not met yet; a nested path
    // to a non-nullable target would make the generated code throw there instead of reporting nothing spent.
    private static decimal GetAmountConsumed(ServiceLimit source)
    {
        return source.Usage?.AmountConsumed ?? 0;
    }
}
