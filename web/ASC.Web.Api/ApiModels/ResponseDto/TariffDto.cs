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
/// The tariff parameters.
/// </summary>
/// <example>
/// {
///   "openSource": true,
///   "enterprise": true,
///   "developer": true,
///   "id": 1,
///   "state": {},
///   "dueDate": "2024-01-15T10:30:00Z",
///   "delayDueDate": "2024-01-15T10:30:00Z",
///   "licenseDate": "2024-01-15T10:30:00Z",
///   "customerId": "example value",
///   "quotas": [{"id": 1, "title": "Basic Plan"}]
/// }
/// </example>
public class TariffDto
{
    /// <summary>
    /// Specifies whether the tariff is Community or not.
    /// </summary>
    /// <example>true</example>
    public bool? OpenSource { get; set; }

    /// <summary>
    /// Specifies whether the tariff is Enterprise or not.
    /// </summary>
    /// <example>true</example>
    public bool? Enterprise { get; set; }

    /// <summary>
    /// Specifies whether the tariff is Developer or not.
    /// </summary>
    /// <example>true</example>
    public bool? Developer { get; set; }

    /// <summary>
    /// The tariff ID.
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }

    /// <summary>
    /// The tariff state.
    /// </summary>
    /// <example>{}</example>
    public TariffState State { get; set; }

    /// <summary>
    /// The tariff due date.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime DueDate { get; set; }

    /// <summary>
    /// The tariff delay due date.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime DelayDueDate { get; set; }

    /// <summary>
    /// The tariff license date.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime LicenseDate { get; set; }

    /// <summary>
    /// The customer ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    public string CustomerId { get; set; }

    /// <summary>
    /// The list of quotas.
    /// </summary>
    /// <example>[{"id": 1, "title": "Basic Plan"}]</example>
    public List<TariffQuotaDto> Quotas { get; set; }
}

/// <summary>
/// The tariff quota parameters.
/// </summary>
/// <example>
/// {
///   "id": -11,
///   "quantity": 500,
///   "wallet": true,
///   "dueDate": "2024-01-15T10:30:00Z",
///   "nextQuantity": 100,
///   "state": "Active"
/// }
/// </example>
public class TariffQuotaDto(Quota quota, DateTime tariffDueDate, ApiDateTimeHelper apiDateTimeHelper)
{
    /// <summary>
    /// The quota ID.
    /// </summary>
    /// <example>-11</example>
    public int Id { get; set; } = quota.Id;

    /// <summary>
    /// The quota quantity.
    /// </summary>
    /// <example>500</example>
    public int Quantity { get; set; } = quota.Quantity;

    /// <summary>
    /// The quota applies to the wallet or not.
    /// </summary>
    /// <example>true</example>
    public bool Wallet { get; set; } = quota.Wallet;

    /// <summary>
    /// The quota due date in the portal time zone. Falls back to the tariff due date when the quota has none.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime DueDate { get; set; } = apiDateTimeHelper.Get(quota.DueDate ?? tariffDueDate);

    /// <summary>
    /// The quota next quantity.
    /// </summary>
    /// <example>100</example>
    public int? NextQuantity { get; set; } = quota.NextQuantity;

    /// <summary>
    /// The quota state.
    /// </summary>
    /// <example>Active</example>
    public QuotaState? State { get; set; } = quota.State;
}

/// <summary>
/// The upcoming payment parameters.
/// </summary>
/// <example>
/// {
///   "id": -11,
///   "name": "storage",
///   "quantity": 100,
///   "wallet": true,
///   "dueDate": "2026-07-08T11:39:43.0000000+03:00",
///   "amount": 14,
///   "currency": "USD"
/// }
/// </example>
public class UpcomingPaymentDto
{
    /// <summary>
    /// The quota ID.
    /// </summary>
    /// <example>-11</example>
    public int Id { get; set; }

    /// <summary>
    /// The quota name.
    /// </summary>
    /// <example>storage</example>
    public string Name { get; set; }

    /// <summary>
    /// The quota title.
    /// </summary>
    /// <example>Business plan</example>
    public string Title { get; set; }

    /// <summary>
    /// The quota unit of measure.
    /// </summary>
    /// <example>admins</example>
    public string UnitOfMeasure { get; set; }

    /// <summary>
    /// The quantity that will be charged (the next quantity if set, otherwise the current quantity).
    /// </summary>
    /// <example>100</example>
    public int Quantity { get; set; }

    /// <summary>
    /// The quota applies to the wallet or not.
    /// </summary>
    /// <example>true</example>
    public bool Wallet { get; set; }

    /// <summary>
    /// The due date of the upcoming payment in the portal time zone.
    /// </summary>
    /// <example>2026-07-08T11:39:43.0000000+03:00</example>
    public ApiDateTime DueDate { get; set; }

    /// <summary>
    /// The amount that will be charged (unit price multiplied by the quantity).
    /// </summary>
    /// <example>14</example>
    public decimal Amount { get; set; }

    /// <summary>
    /// The three-character ISO 4217 currency symbol of the amount.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; set; }
}
