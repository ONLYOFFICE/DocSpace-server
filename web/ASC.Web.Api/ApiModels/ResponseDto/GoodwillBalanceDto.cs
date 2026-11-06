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
/// The goodwill balance of the portal - the bonus money credited to it, held on an account of its own rather than on the
/// wallet - with the bonus credits that can still be spent.
/// </summary>
/// <example>
/// {
///   "accountNumber": 12345,
///   "subAccountNumber": 12346,
///   "accountName": "goodwill",
///   "accountCurrency": "USD",
///   "subAccounts": [{"currency": "USD", "amount": 35.5}],
///   "lastCredit": {"date": "2026-11-01T09:00:00Z", "currency": "USD", "amount": 50},
///   "lots": [{"operationId": 987654, "amount": 50, "remaining": 35.5, "currency": "USD", "date": "2026-11-01T09:00:00Z", "expiresAt": "2026-12-01T09:00:00Z"}]
/// }
/// </example>
public class GoodwillBalanceDto : BalanceDto
{
    /// <summary>
    /// The bonus credits that have neither expired nor been spent in full, in the order they are spent. An empty list
    /// means no bonus is left to spend.
    /// </summary>
    /// <example>[{"operationId": 987654, "amount": 50, "remaining": 35.5, "currency": "USD", "date": "2026-11-01T09:00:00Z", "expiresAt": "2026-12-01T09:00:00Z"}]</example>
    public List<GoodwillLotDto> Lots { get; init; }
}

/// <summary>
/// One bonus credit of the portal that can still be spent: how much was credited, how much is left, and until when.
/// </summary>
public class GoodwillLotDto
{
    /// <summary>
    /// The ID of the accounting operation that credited the bonus; it tells apart two credits of the same amount and
    /// date.
    /// </summary>
    /// <example>987654</example>
    public long OperationId { get; init; }

    /// <summary>
    /// The amount credited, in `currency`. It does not fall as the bonus is spent; `remaining` does.
    /// </summary>
    /// <example>50</example>
    public decimal Amount { get; init; }

    /// <summary>
    /// The part of `amount` that is left to spend, in `currency`.
    /// </summary>
    /// <example>35.5</example>
    public decimal Remaining { get; init; }

    /// <summary>
    /// The three-character ISO 4217 currency symbol of `amount` and `remaining`.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; init; }

    /// <summary>
    /// The date and time the bonus was credited, in UTC.
    /// </summary>
    /// <example>2026-11-01T09:00:00Z</example>
    public DateTime Date { get; init; }

    /// <summary>
    /// The date and time after which whatever is left of the bonus can no longer be spent, in UTC; null for a bonus that
    /// never expires.
    /// </summary>
    /// <example>2026-12-01T09:00:00Z</example>
    public DateTime? ExpiresAt { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class GoodwillBalanceDtoMapper
{
    public static partial GoodwillBalanceDto Map(this GoodwillBalance source);
}
