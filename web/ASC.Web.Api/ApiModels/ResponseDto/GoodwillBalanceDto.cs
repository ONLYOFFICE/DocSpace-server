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
/// The bonus balance of the portal - the bonus money credited to it, which the accounting service holds on a goodwill
/// account of its own rather than on the wallet - with the bonus credits not spent in full. The amount of a
/// sub-account may still include a credit that has ended, until the accounting service writes it off.
/// </summary>
/// <example>
/// {
///   "accountNumber": 100238,
///   "subAccountNumber": 53,
///   "accountName": "goodwill",
///   "accountCurrency": "USD",
///   "subAccounts": [{"currency": "USD", "amount": 35.5}],
///   "lastCredit": {"date": "2026-11-01T12:00:00.0000000+03:00", "currency": "USD", "amount": 50},
///   "lots": [{"operationId": 987654, "amount": 50, "remaining": 35.5, "currency": "USD", "date": "2026-11-01T12:00:00.0000000+03:00", "expiresAt": "2026-12-01T12:00:00.0000000+03:00"}]
/// }
/// </example>
public class GoodwillBalanceDto : BalanceDto
{
    /// <summary>
    /// The bonus credits not spent in full, in the order they are spent. A credit may stay here after its `expiresAt`
    /// until the accounting service writes it off, and is no longer spent then. An empty list means no bonus is left;
    /// null means the accounting service sent no list of credits, and only the sub-account amounts are known.
    /// </summary>
    /// <example>[{"operationId": 987654, "amount": 50, "remaining": 35.5, "currency": "USD", "date": "2026-11-01T12:00:00.0000000+03:00", "expiresAt": "2026-12-01T12:00:00.0000000+03:00"}]</example>
    public List<GoodwillLotDto> Lots { get; init; }
}

/// <summary>
/// One bonus credit of the portal not spent in full: how much was credited, how much is left, and until when.
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
    /// The date and time the bonus was credited. It carries its own offset.
    /// </summary>
    /// <example>2026-11-01T12:00:00.0000000+03:00</example>
    public ApiDateTime Date { get; init; }

    /// <summary>
    /// The date and time after which whatever is left of the bonus can no longer be spent. It carries its own offset.
    /// Null for a bonus that never expires.
    /// </summary>
    /// <example>2026-12-01T12:00:00.0000000+03:00</example>
    public ApiDateTime ExpiresAt { get; init; }
}

[Scope]
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class GoodwillBalanceDtoMapper(ApiDateTimeHelper apiDateTimeHelper)
{
    public partial GoodwillBalanceDto Map(GoodwillBalance source);

    // The accounting service gives its times in UTC; the client sees them with the offset of the portal.
    private ApiDateTime MapDate(DateTime source) => apiDateTimeHelper.Get(BillingTime.AsUtc(source));

    private ApiDateTime MapNullableDate(DateTime? source) => apiDateTimeHelper.Get(BillingTime.AsUtc(source));
}
