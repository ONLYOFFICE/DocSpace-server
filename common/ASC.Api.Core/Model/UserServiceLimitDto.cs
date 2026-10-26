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
/// The spending limit on the AI services in force for one user: the user's personal limit, or else the limit for every
/// user, together with the money the user spent against it in the current period.
/// </summary>
public class UserServiceLimitDto
{
    /// <summary>
    /// The service limit ID, used to read, change, switch off or delete the limit through the
    /// `api/2.0/portal/payment/servicelimit` operations. For the limit for every user it is the same for all users.
    /// </summary>
    /// <example>42</example>
    public int Id { get; init; }

    /// <summary>
    /// Whether the limit is the user's own (`Custom`) or the limit for every user (`Global`) the user falls back to.
    /// </summary>
    /// <example>Custom</example>
    public ServiceLimitScope Scope { get; init; }

    /// <summary>
    /// The most money the AI services may cost the user per period, in `currency`. It is the only threshold a limit on
    /// the AI services has: their services count in different units, so a threshold on a quantity is refused for them.
    /// </summary>
    /// <example>40</example>
    public decimal? AmountValue { get; init; }

    /// <summary>
    /// The money the AI services cost the user in the current period, in `currency`; 0 when nothing was spent yet,
    /// including a user who has never used the AI services.
    /// </summary>
    /// <example>12.5</example>
    public decimal AmountConsumed { get; init; }

    /// <summary>
    /// The currency `amountValue` and `amountConsumed` are expressed in; null when the limit has no money threshold.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; init; }

    /// <summary>
    /// The window the threshold applies to. It resets at the start of the next day, week or month in UTC (a week on
    /// Monday), not a fixed time after the first charge; a lifetime limit never resets.
    /// </summary>
    /// <example>Day</example>
    public ServiceLimitPeriod Period { get; init; }

    /// <summary>
    /// The moment the current period ends and the spending starts over from zero, in UTC. Null for a lifetime limit,
    /// which never resets.
    /// </summary>
    /// <example>2026-10-20T23:59:59.9999999Z</example>
    public DateTime? PeriodEnd { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class UserServiceLimitDtoMapper
{
    public static partial UserServiceLimitDto ToUserServiceLimitDto(this UserServiceLimit source);
}
