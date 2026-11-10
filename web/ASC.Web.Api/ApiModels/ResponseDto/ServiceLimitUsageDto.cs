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
/// What has been spent against a spending limit in its current period, and what is left.
/// </summary>
public class ServiceLimitUsageDto
{
    /// <summary>
    /// The start of the current period, in UTC.
    /// </summary>
    /// <example>2026-10-18T00:00:00Z</example>
    public DateTime PeriodStart { get; init; }

    /// <summary>
    /// The end of the current period, in UTC: the moment the spending starts again from zero. Null for a lifetime
    /// limit, which never resets.
    /// </summary>
    /// <example>2026-10-18T23:59:59Z</example>
    public DateTime? PeriodEnd { get; init; }

    /// <summary>
    /// The money charged in the current period, in the currency of the limit.
    /// </summary>
    /// <example>12.5</example>
    public decimal AmountConsumed { get; init; }

    /// <summary>
    /// The money still available in the current period: the threshold less what was charged and what is reserved for
    /// operations in progress, never below zero. Null when the limit has no money threshold.
    /// </summary>
    /// <example>27.5</example>
    public decimal? AmountAvailable { get; init; }

    /// <summary>
    /// The quantity consumed in the current period, in the unit of the limit.
    /// </summary>
    /// <example>250</example>
    public int QuantityConsumed { get; init; }

    /// <summary>
    /// The quantity still available in the current period, never below zero; null when the limit has no quantity
    /// threshold.
    /// </summary>
    /// <example>750</example>
    public int? QuantityAvailable { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class ServiceLimitUsageDtoMapper
{
    public static partial ServiceLimitUsageDto Map(this ServiceLimitUsage source);
}
