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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// The bonus a redeemed promo code credits to the portal.
/// </summary>
/// <example>
/// {
///   "status": "Completed",
///   "amount": 10,
///   "currency": "USD",
///   "expiresAt": "2027-02-07T12:00:00Z"
/// }
/// </example>
public class PromoCodeDto
{
    /// <summary>
    /// Whether the bonus is on the bonus balance already or is credited later.
    /// </summary>
    /// <example>Completed</example>
    public PromoCodeStatus Status { get; init; }

    /// <summary>
    /// How much bonus money the code credits, in `currency`. It lands on the bonus balance, read with
    /// `GET api/2.0/portal/payment/customer/goodwill`, not on the money paid into the wallet.
    /// </summary>
    /// <example>10</example>
    public decimal Amount { get; init; }

    /// <summary>
    /// The three-letter ISO 4217 code of `amount`, in upper case.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; init; }

    /// <summary>
    /// The moment the bonus expires, in UTC; whatever is left of it can no longer be spent after that. Null for a bonus
    /// that never expires.
    /// </summary>
    /// <example>2027-02-07T12:00:00Z</example>
    public DateTime? ExpiresAt { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class PromoCodeDtoMapper
{
    [MapProperty(nameof(PromoCodeResult.Currency), nameof(PromoCodeDto.Currency), Use = nameof(ToUpperCurrency))]
    public static partial PromoCodeDto Map(this PromoCodeResult source);

    // The billing service may spell the currency in lower case, as it does for a subscription moved to the wallet.
    private static string ToUpperCurrency(string currency)
    {
        return currency?.ToUpperInvariant();
    }
}
