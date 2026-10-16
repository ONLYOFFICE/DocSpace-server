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
/// A spending limit on a pay-as-you-go wallet service, set for the whole portal or for one of its users.
/// </summary>
public class ServiceLimitDto
{
    /// <summary>
    /// The service limit ID, used to read, change, switch off or delete the limit.
    /// </summary>
    /// <example>42</example>
    public int Id { get; init; }

    /// <summary>
    /// The limited wallet service, named the way the billing catalogue names it.
    /// </summary>
    /// <example>ai-tools</example>
    public string ServiceName { get; init; }

    /// <summary>
    /// The number of the billing account of the portal the limit belongs to.
    /// </summary>
    /// <example>12345</example>
    public int CustomerAccountNumber { get; init; }

    /// <summary>
    /// The ID of the portal user the limit is set for; null when the limit covers the portal as a whole.
    /// </summary>
    /// <example>3f2504e0-4f89-11d3-9a0c-0305e82c3301</example>
    public string Participant { get; init; }

    /// <summary>
    /// The most money the service may cost per period, in `currency`; null when the limit has no money threshold.
    /// </summary>
    /// <example>50</example>
    public decimal? AmountValue { get; init; }

    /// <summary>
    /// The currency of the service `amountValue` is expressed in; null when the limit has no money threshold.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; init; }

    /// <summary>
    /// The largest quantity of the service that may be consumed per period, in `serviceUnit`; null when the limit
    /// has no quantity threshold.
    /// </summary>
    /// <example>1000</example>
    public int? QuantityValue { get; init; }

    /// <summary>
    /// The unit of the service `quantityValue` is expressed in, such as pages, requests or gigabytes; null when the
    /// limit has no quantity threshold.
    /// </summary>
    /// <example>request</example>
    public string ServiceUnit { get; init; }

    /// <summary>
    /// The calendar period the thresholds apply to, counted in UTC.
    /// </summary>
    /// <example>Day</example>
    public ServiceLimitPeriod Period { get; init; }

    /// <summary>
    /// Whether the limit is in force. A switched-off limit no longer restricts the service and stays in the
    /// history.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; init; }

    /// <summary>
    /// The date and time when the limit was created, in UTC.
    /// </summary>
    /// <example>2026-09-01T10:30:00Z</example>
    public DateTime Created { get; init; }

    /// <summary>
    /// The date and time when the limit was last changed, in UTC.
    /// </summary>
    /// <example>2026-09-15T08:00:00Z</example>
    public DateTime Modified { get; init; }
}

/// <summary>
/// A page of the spending limits set on a wallet service for individual portal users.
/// </summary>
public class ServiceLimitReportDto
{
    /// <summary>
    /// The user limits of this page; empty when no user has a limit on the service or the page is past the end.
    /// </summary>
    /// <example>[{"id": 42, "serviceName": "ai-tools", "customerAccountNumber": 12345, "participant": "3f2504e0-4f89-11d3-9a0c-0305e82c3301", "amountValue": 40, "currency": "USD", "quantityValue": null, "serviceUnit": null, "period": "Day", "enabled": true, "created": "2026-09-01T10:30:00Z", "modified": "2026-09-15T08:00:00Z"}]</example>
    public List<ServiceLimitDto> Collection { get; init; }

    /// <summary>
    /// The number of limits skipped before this page.
    /// </summary>
    /// <example>0</example>
    public int Offset { get; init; }

    /// <summary>
    /// The page size the page was read with.
    /// </summary>
    /// <example>25</example>
    public int Limit { get; init; }

    /// <summary>
    /// The number of pages the whole list takes at this page size.
    /// </summary>
    /// <example>3</example>
    public int TotalPage { get; init; }

    /// <summary>
    /// The number of this page within the whole list.
    /// </summary>
    /// <example>1</example>
    public int CurrentPage { get; init; }

    /// <summary>
    /// The number of user limits on the service across all pages.
    /// </summary>
    /// <example>60</example>
    public int TotalQuantity { get; init; }
}

/// <summary>
/// What has been spent against a service limit in its current calendar period, and what is left.
/// </summary>
public class ServiceLimitUsageDto
{
    /// <summary>
    /// The ID of the service limit the usage is counted for.
    /// </summary>
    /// <example>42</example>
    public int ServiceLimitId { get; init; }

    /// <summary>
    /// The calendar period the limit is reset with.
    /// </summary>
    /// <example>Day</example>
    public ServiceLimitPeriod Period { get; init; }

    /// <summary>
    /// The start of the current period, in UTC.
    /// </summary>
    /// <example>2026-10-01T00:00:00Z</example>
    public DateTime PeriodStart { get; init; }

    /// <summary>
    /// The end of the current period, in UTC: the moment the consumed figures reset to zero.
    /// </summary>
    /// <example>2026-10-02T00:00:00Z</example>
    public DateTime PeriodEnd { get; init; }

    /// <summary>
    /// The money threshold of the limit, in `currency`; null when the limit has no money threshold.
    /// </summary>
    /// <example>50</example>
    public decimal? AmountValue { get; init; }

    /// <summary>
    /// The money charged for the service in the current period, in `currency`. Operations that have started but
    /// not finished yet count with their expected cost.
    /// </summary>
    /// <example>12.5</example>
    public decimal AmountConsumed { get; init; }

    /// <summary>
    /// The money still available in the current period, in `currency`; zero once the threshold is reached, and
    /// null when the limit has no money threshold.
    /// </summary>
    /// <example>37.5</example>
    public decimal? AmountAvailable { get; init; }

    /// <summary>
    /// The currency of the amounts; null when the limit has no money threshold.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; init; }

    /// <summary>
    /// The quantity threshold of the limit, in `serviceUnit`; null when the limit has no quantity threshold.
    /// </summary>
    /// <example>1000</example>
    public int? QuantityValue { get; init; }

    /// <summary>
    /// The quantity of the service consumed in the current period, in `serviceUnit`.
    /// </summary>
    /// <example>250</example>
    public int QuantityConsumed { get; init; }

    /// <summary>
    /// The quantity still available in the current period, in `serviceUnit`; zero once the threshold is reached,
    /// and null when the limit has no quantity threshold.
    /// </summary>
    /// <example>750</example>
    public int? QuantityAvailable { get; init; }

    /// <summary>
    /// The unit of the quantities, such as pages, requests or gigabytes; null when the limit has no quantity
    /// threshold.
    /// </summary>
    /// <example>request</example>
    public string ServiceUnit { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class ServiceLimitDtoMapper
{
    public static partial ServiceLimitDto Map(this ServiceLimit source);

    public static partial ServiceLimitReportDto Map(this ServiceLimitReport source);

    public static partial ServiceLimitUsageDto Map(this ServiceLimitUsage source);
}
