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
/// A spending limit on the AI services: for the portal as a whole, for every user, or for one user.
/// </summary>
public class ServiceLimitDto
{
    /// <summary>
    /// The service limit ID, used to read, change, switch off or delete the limit. In the list of the users the same
    /// ID repeats for every user the limit for every user applies to.
    /// </summary>
    /// <example>42</example>
    public int Id { get; init; }

    /// <summary>
    /// The limited service group; the AI services are `ai-services`.
    /// </summary>
    /// <example>ai-services</example>
    public string ServiceGroupName { get; init; }

    /// <summary>
    /// The number of the billing account of the portal the limit belongs to.
    /// </summary>
    /// <example>12345</example>
    public int CustomerAccountNumber { get; init; }

    /// <summary>
    /// Whom the limit applies to.
    /// </summary>
    /// <example>Custom</example>
    public ServiceLimitScope Scope { get; init; }

    /// <summary>
    /// The ID of the portal user the limit applies to: the user of a personal limit, or, in the list of the users, the
    /// user a limit for every user applies to. Null for a limit of the portal as a whole, and for a limit for every
    /// user read on its own.
    /// </summary>
    /// <example>3f2504e0-4f89-11d3-9a0c-0305e82c3301</example>
    public Guid? UserId { get; init; }

    /// <summary>
    /// The most money the AI services may cost per period, in `currency`; null when the limit has no money threshold.
    /// </summary>
    /// <example>50</example>
    public decimal? AmountValue { get; init; }

    /// <summary>
    /// The currency `amountValue` is expressed in; null when the limit has no money threshold.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; init; }

    /// <summary>
    /// The largest quantity the AI services may consume per period, in `serviceUnit`; null when the limit has no
    /// quantity threshold.
    /// </summary>
    /// <example>1000</example>
    public int? QuantityValue { get; init; }

    /// <summary>
    /// The unit `quantityValue` is expressed in; null when the limit has no quantity threshold.
    /// </summary>
    /// <example>request</example>
    public string ServiceUnit { get; init; }

    /// <summary>
    /// The window the thresholds apply to. A day, week or month resets at the start of the next one in UTC (a week on
    /// Monday), not a fixed time after the first charge; a lifetime limit never resets.
    /// </summary>
    /// <example>Day</example>
    public ServiceLimitPeriod Period { get; init; }

    /// <summary>
    /// The moment a lifetime limit counts from, in UTC; null for the other periods.
    /// </summary>
    /// <example>2026-10-01T09:12:00Z</example>
    public DateTime? UsageFrom { get; init; }

    /// <summary>
    /// Whether the limit is in force. A switched-off limit no longer restricts the AI services and can be switched
    /// back on.
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

    /// <summary>
    /// What was spent against the limit in its current period and what is left. Filled for the limit of the portal as
    /// a whole and in the list of the users; null where the spending is not read, such as for a limit read by its ID
    /// or the limit for every user, whose spending is counted per user.
    /// </summary>
    public ServiceLimitUsageDto Usage { get; init; }
}

/// <summary>
/// A page of the spending limits in force for the portal users.
/// </summary>
public class ServiceLimitReportDto
{
    /// <summary>
    /// The limits of this page, one per user; empty when no limit applies to any user or the page is past the end.
    /// </summary>
    /// <example>[{"id": 42, "serviceGroupName": "ai-services", "customerAccountNumber": 12345, "scope": "Custom", "userId": "3f2504e0-4f89-11d3-9a0c-0305e82c3301", "amountValue": 40, "currency": "USD", "quantityValue": null, "serviceUnit": null, "period": "Day", "usageFrom": null, "enabled": true, "created": "2026-09-01T10:30:00Z", "modified": "2026-09-15T08:00:00Z", "usage": {"periodStart": "2026-10-18T00:00:00Z", "periodEnd": "2026-10-18T23:59:59Z", "amountConsumed": 12.5, "amountAvailable": 27.5, "quantityConsumed": 0, "quantityAvailable": null}}]</example>
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
    /// The number of this page, starting from 1.
    /// </summary>
    /// <example>1</example>
    public int CurrentPage { get; init; }

    /// <summary>
    /// The number of users with a limit in force across all pages.
    /// </summary>
    /// <example>60</example>
    public int TotalQuantity { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
[UseStaticMapper(typeof(ServiceLimitUsageDtoMapper))]
public static partial class ServiceLimitDtoMapper
{
    [MapProperty(nameof(ServiceLimit.Participant), nameof(ServiceLimitDto.UserId), Use = nameof(ToUserId))]
    public static partial ServiceLimitDto Map(this ServiceLimit source);

    public static partial ServiceLimitReportDto Map(this ServiceLimitReport source);

    // The portal names the participant of a limit by the user ID; the accounting service keeps it as text.
    private static Guid? ToUserId(string participant)
    {
        return Guid.TryParse(participant, out var userId) ? userId : null;
    }
}
