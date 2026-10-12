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
/// The information about the current subscription and its unused balance.
/// </summary>
public class SubscriptionBalanceInfo
{
    /// <summary>
    /// The total cost of the current billing period (the sum across all subscription items).
    /// </summary>
    /// <example>120.00</example>
    [JsonPropertyName("totalCost")]
    public decimal TotalCost { get; set; }

    /// <summary>
    /// The three-character ISO 4217 currency symbol of the subscription.
    /// </summary>
    /// <example>USD</example>
    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    /// <summary>
    /// The start of the current billing period.
    /// </summary>
    /// <example>2026-06-01T00:00:00Z</example>
    [JsonPropertyName("periodStart")]
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// The end of the current billing period.
    /// </summary>
    /// <example>2026-07-01T00:00:00Z</example>
    [JsonPropertyName("periodEnd")]
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// The boundary of the used part of the period (the moment of the request).
    /// </summary>
    /// <example>2026-06-23T14:35:00Z</example>
    [JsonPropertyName("periodUsedUntil")]
    public DateTime PeriodUsedUntil { get; set; }

    /// <summary>
    /// The number of days elapsed since the start of the period (inclusive).
    /// </summary>
    /// <example>23</example>
    [JsonPropertyName("daysElapsed")]
    public int DaysElapsed { get; set; }

    /// <summary>
    /// The unused balance of the subscription, in the subscription currency.
    /// </summary>
    /// <example>87.74</example>
    [JsonPropertyName("remainingBalance")]
    public decimal RemainingBalance { get; set; }

    /// <summary>
    /// The unused balance of the subscription, converted to the wallet currency.
    /// </summary>
    /// <example>87.74</example>
    [JsonPropertyName("remainingBalanceInWalletCurrency")]
    public decimal RemainingBalanceInWalletCurrency { get; set; }

    /// <summary>
    /// The three-character ISO 4217 currency symbol of the wallet.
    /// </summary>
    /// <example>USD</example>
    [JsonPropertyName("walletCurrency")]
    public string WalletCurrency { get; set; }
}
