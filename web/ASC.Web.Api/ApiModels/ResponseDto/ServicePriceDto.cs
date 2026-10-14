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
/// Represents a price of the service.
/// </summary>
public class ServicePriceDto
{
    /// <summary>
    /// The price unique identifier.
    /// </summary>
    /// <example>12345</example>
    public int Id { get; init; }

    /// <summary>
    /// The account number.
    /// </summary>
    /// <example>1010</example>
    public int AccountNumber { get; init; }

    /// <summary>
    /// The service ID.
    /// </summary>
    /// <example>12345</example>
    public int ServiceId { get; init; }

    /// <summary>
    /// The time unit the price is bound to.
    /// </summary>
    /// <example>None</example>
    public PriceTimeUnit TimeUnit { get; init; }

    /// <summary>
    /// The cost price.
    /// </summary>
    /// <example>1500.75</example>
    public decimal CostPrice { get; init; }

    /// <summary>
    /// The extra charge added to the cost price.
    /// </summary>
    /// <example>1500.75</example>
    public decimal ExtraCharge { get; init; }

    /// <summary>
    /// The resulting service price.
    /// </summary>
    /// <example>1500.75</example>
    public decimal ServicePrice { get; init; }

    /// <summary>
    /// The quota the price is set for.
    /// </summary>
    /// <example>100</example>
    public double? Quota { get; init; }

    /// <summary>
    /// The period the price is effective in.
    /// </summary>
    public TimeBoundDto TimeBound { get; init; }

    /// <summary>
    /// The price status.
    /// </summary>
    /// <example>Draft</example>
    public PriceStatus Status { get; init; }

    /// <summary>
    /// The date and time when the price was created.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime Created { get; init; }

    /// <summary>
    /// The discount category ID.
    /// </summary>
    /// <example>12345</example>
    public int? DiscountCategoryId { get; init; }

    /// <summary>
    /// The discount category.
    /// </summary>
    public DiscountCategoryDto DiscountCategory { get; init; }
}

/// <summary>
/// Represents the period the price is effective in.
/// </summary>
public class TimeBoundDto
{
    /// <summary>
    /// The date and time when the period starts.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime StartDate { get; init; }

    /// <summary>
    /// The date and time when the period ends.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime? EndDate { get; init; }
}

/// <summary>
/// Represents a discount category applied to the price.
/// </summary>
public class DiscountCategoryDto
{
    /// <summary>
    /// The discount category unique identifier.
    /// </summary>
    /// <example>12345</example>
    public int Id { get; init; }

    /// <summary>
    /// The discount value.
    /// </summary>
    /// <example>10.5</example>
    public decimal ValueDiscount { get; init; }

    /// <summary>
    /// The discount category description.
    /// </summary>
    /// <example>Annual subscription discount</example>
    public string Description { get; init; }

    /// <summary>
    /// The date and time when the discount category was created.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime Created { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class ServicePriceDtoMapper
{
    public static partial ServicePriceDto Map(this ServicePriceInfo source);
}
