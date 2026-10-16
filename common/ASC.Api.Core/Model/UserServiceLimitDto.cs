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

namespace ASC.Web.Api.Models;

/// <summary>
/// A spending limit a portal administrator set on a pay-as-you-go wallet service for one user.
/// </summary>
public class UserServiceLimitDto
{
    /// <summary>
    /// The service limit ID, used to read, change, switch off or delete the limit through the
    /// `api/2.0/portal/payment/servicelimit` operations.
    /// </summary>
    /// <example>42</example>
    public int Id { get; init; }

    /// <summary>
    /// The most money the service may cost the user per period, in `currency`; null when the limit has no money
    /// threshold.
    /// </summary>
    /// <example>40</example>
    public decimal? AmountValue { get; init; }

    /// <summary>
    /// The currency of the service `amountValue` is expressed in; null when the limit has no money threshold.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; init; }

    /// <summary>
    /// The largest quantity of the service the user may consume per period, in `serviceUnit`; null when the limit
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
    /// Whether the limit is in force. A switched-off limit no longer restricts the user and can be switched back on.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class UserServiceLimitDtoMapper
{
    public static partial UserServiceLimitDto ToUserServiceLimitDto(this ServiceLimit source);
}
