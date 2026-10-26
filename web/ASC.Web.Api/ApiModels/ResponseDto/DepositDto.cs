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
/// One page of the portal wallet's top-ups, with the paging figures needed to walk the rest.
/// </summary>
/// <example>
/// {
///   "collection": [{"id": 15342, "date": "2025-03-14T10:30:00Z", "amount": 100.0, "currency": "USD", "status": "Completed", "hasInvoice": true}],
///   "offset": 0,
///   "limit": 25,
///   "totalQuantity": 12,
///   "totalPage": 1,
///   "currentPage": 1
/// }
/// </example>
public class DepositReportDto
{
    /// <summary>
    /// The top-ups on this page. It is empty for a page past the end of the list as well as for a period in which
    /// nothing was paid into the wallet.
    /// </summary>
    /// <example>[{"id": 15342, "date": "2025-03-14T10:30:00Z", "amount": 100.0, "currency": "USD", "status": "Completed", "hasInvoice": true}]</example>
    public List<DepositDto> Collection { get; init; }

    /// <summary>
    /// How many top-ups were skipped before this page, echoed from the request so a client need not remember what it
    /// asked for.
    /// </summary>
    /// <example>0</example>
    public int Offset { get; set; }

    /// <summary>
    /// How many top-ups one page may hold, echoed from the request; it is 25 unless another value was asked for. A
    /// full page is not proof that more exist - compare `currentPage` with `totalPage`.
    /// </summary>
    /// <example>25</example>
    public int Limit { get; set; }

    /// <summary>
    /// How many top-ups match the filters in total, across every page.
    /// </summary>
    /// <example>12</example>
    public int TotalQuantity { get; init; }

    /// <summary>
    /// How many pages those top-ups come to at the current `limit`.
    /// </summary>
    /// <example>1</example>
    public int TotalPage { get; init; }

    /// <summary>
    /// Which of those pages this one is, as the billing service numbers them. Page through by advancing `offset`
    /// rather than this value, which nothing accepts as an argument.
    /// </summary>
    /// <example>1</example>
    public int CurrentPage { get; init; }
}

/// <summary>
/// One payment into the portal wallet: when it was made, how much it brought in, how it ended, and whether an
/// invoice stands behind it.
/// </summary>
public class DepositDto
{
    /// <summary>
    /// The identifier of the top-up, which is what `GET api/2.0/portal/payment/customer/deposits/{id}/invoice` takes
    /// to read its invoice.
    /// </summary>
    /// <example>15342</example>
    public long Id { get; init; }

    /// <summary>
    /// When the top-up was booked, as an ISO 8601 timestamp that carries its own offset: that of the portal's time
    /// zone, written as `Z` for a portal in UTC - the zone in which `startDate` and `endDate` without an offset are
    /// read.
    /// </summary>
    /// <example>2025-03-14T10:30:00Z</example>
    public ApiDateTime Date { get; init; }

    /// <summary>
    /// The amount that went into the wallet, in `currency`.
    /// </summary>
    /// <example>100.0</example>
    public decimal Amount { get; init; }

    /// <summary>
    /// The currency `amount` is expressed in, as a three-letter ISO 4217 code - the accounting currency of the
    /// wallet.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; init; }

    /// <summary>
    /// How the top-up ended. A pending one is still being settled and may change later, so only a completed one has
    /// actually raised the balance.
    /// </summary>
    /// <example>Completed</example>
    public OperationStatus Status { get; init; }

    /// <summary>
    /// Whether the billing provider holds an invoice for the payment behind the top-up, which is when
    /// `GET api/2.0/portal/payment/customer/deposits/{id}/invoice` can open it. It is `false` for a top-up whose
    /// payment came without one, and asking for its invoice then fails with 404.
    /// </summary>
    /// <example>true</example>
    public bool HasInvoice { get; init; }
}

[Scope]
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class DepositDtoMapper(ApiDateTimeHelper apiDateTimeHelper)
{
    public DepositReportDto Map(DepositReport source, DepositFilter filter)
    {
        var result = Map(source);

        // The accounting service answers a filter nothing matches with a zero window, so the window asked for is
        // echoed instead; a page with items keeps the one the accounting service applied.
        if (result.TotalQuantity == 0)
        {
            result.Offset = filter.Offset ?? 0;
            result.Limit = filter.Limit ?? 0;
        }

        return result;
    }

    private partial DepositReportDto Map(DepositReport source);

    [MapProperty(nameof(Deposit.Sum), nameof(DepositDto.Amount))]
    private partial DepositDto Map(Deposit source);

    private ApiDateTime MapDate(DateTime source) => apiDateTimeHelper.Get(source);
}
