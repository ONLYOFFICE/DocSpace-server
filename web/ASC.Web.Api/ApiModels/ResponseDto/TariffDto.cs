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
/// The subscription this portal runs on: its state, the end of the current period, and the quotas it is made of.
/// </summary>
/// <example>
/// {
///   "openSource": false,
///   "enterprise": true,
///   "developer": false,
///   "id": 1,
///   "state": "Paid",
///   "dueDate": "2024-01-15T10:30:00Z",
///   "delayDueDate": "2024-01-15T10:30:00Z",
///   "licenseDate": "2024-01-15T10:30:00Z",
///   "customerId": "00000000-0000-0000-0000-000000000001",
///   "quotas": [{"id": 1, "quantity": 500}]
/// }
/// </example>
public class TariffDto
{
    /// <summary>
    /// Whether the installation runs the open-source build, which has no paid plan at all. This flag and the two
    /// below describe the build rather than the subscription, and all three are left empty for a caller without
    /// the portal-settings right.
    /// </summary>
    /// <example>false</example>
    public bool? OpenSource { get; set; }

    /// <summary>
    /// Whether the installation runs on an Enterprise licence file, which is what makes the licence operations
    /// under `api/2.0/settings/license` usable.
    /// </summary>
    /// <example>true</example>
    public bool? Enterprise { get; set; }

    /// <summary>
    /// Whether the installation runs on a Developer licence, an Enterprise licence meant for embedding rather
    /// than for production use.
    /// </summary>
    /// <example>false</example>
    public bool? Developer { get; set; }

    /// <summary>
    /// The identifier of the subscription record itself, for quoting when a charge has to be traced. It is filled
    /// in for a caller with the portal-settings right only, and nothing accepts it as an argument.
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }

    /// <summary>
    /// How the subscription stands: on trial, paid, inside the grace period that follows the due date, or unpaid.
    /// It is the one field every caller gets, whatever their role, so a client can warn about payment without
    /// needing administrator rights.
    /// </summary>
    /// <example>Paid</example>
    public TariffState State { get; set; }

    /// <summary>
    /// When the current period ends, in the portal time zone. It is filled in for a room or DocSpace
    /// administrator only, and set to the largest value a date can hold for a subscription that never ends.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime DueDate { get; set; }

    /// <summary>
    /// When the grace period after `dueDate` runs out and the portal is cut off, in the portal time zone. Filled
    /// in under the same conditions as `dueDate`, and equal to it when the plan grants no grace period.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime DelayDueDate { get; set; }

    /// <summary>
    /// When the licence file behind the subscription was issued, in the portal time zone. It is meaningful on a
    /// server installation and filled in for a caller with the portal-settings right only.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime LicenseDate { get; set; }

    /// <summary>
    /// The account in the billing system the subscription is charged to, empty for a portal that has never been
    /// billed. Filled in for a caller with the portal-settings right only.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    public string CustomerId { get; set; }

    /// <summary>
    /// The quotas the subscription is made of - the plan itself and its add-ons - with the overdue ones listed
    /// alongside the current ones, so an entry here is not proof that it is still being paid for; read each
    /// entry's own `state` for that. Filled in for a caller with the portal-settings right only.
    /// </summary>
    /// <example>[{"id": 1, "quantity": 500}]</example>
    public List<TariffQuotaDto> Quotas { get; set; }
}

/// <summary>
/// One quota the subscription is made of - the plan itself or an add-on - with its quantity and its own deadline.
/// </summary>
/// <example>
/// {
///   "id": -11,
///   "quantity": 500,
///   "wallet": true,
///   "additional": false,
///   "dueDate": "2024-01-15T10:30:00Z",
///   "nextQuantity": 100,
///   "state": "Active"
/// }
/// </example>
public class TariffQuotaDto(Quota quota, DateTime tariffDueDate, ApiDateTimeHelper apiDateTimeHelper)
{
    /// <summary>
    /// The quota this entry stands for. `GET api/2.0/portal/payment/quotas` describes the quota behind the ID,
    /// including what its `quantity` counts; a negative ID belongs to a built-in quota rather than a purchased
    /// one.
    /// </summary>
    /// <example>-11</example>
    public int Id { get; set; } = quota.Id;

    /// <summary>
    /// How much of the quota the portal holds, in whatever the quota itself is measured in - seats for a plan,
    /// gigabytes for storage. It is `1` for a quota that is simply on or off.
    /// </summary>
    /// <example>500</example>
    public int Quantity { get; set; } = quota.Quantity;

    /// <summary>
    /// Whether the quota is paid for out of the portal wallet as it is consumed, rather than being part of the
    /// subscription charged per period.
    /// </summary>
    /// <example>true</example>
    public bool Wallet { get; set; } = quota.Wallet;

    /// <summary>
    /// Whether this is an add-on bought on top of the plan rather than the plan itself. Exactly one entry of
    /// `quotas` is the plan, and the rest are add-ons.
    /// </summary>
    /// <example>true</example>
    public bool Additional { get; set; } = quota.Additional;

    /// <summary>
    /// When this quota runs out, in the portal time zone. An add-on can end earlier or later than the
    /// subscription; a quota with no deadline of its own reports the subscription's `dueDate` instead of an empty
    /// value.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime DueDate { get; set; } = apiDateTimeHelper.Get(quota.DueDate ?? tariffDueDate);

    /// <summary>
    /// The quantity the next period is going to be charged for, when a change has been scheduled. It is empty
    /// while `quantity` simply carries over.
    /// </summary>
    /// <example>100</example>
    public int? NextQuantity { get; set; } = quota.NextQuantity;

    /// <summary>
    /// The quota this one is scheduled to be replaced by at the start of the next period, empty when no such
    /// switch is planned. `GET api/2.0/portal/tariff/upcoming` already reports the charge for the replacement.
    /// </summary>
    /// <example>2</example>
    public int? NextQuota { get; set; } = quota.NextQuota;

    /// <summary>
    /// Whether the quota is still running or its deadline has passed. It is empty for a quota that has no
    /// deadline of its own, which means it lasts as long as the subscription does.
    /// </summary>
    /// <example>Active</example>
    public QuotaState? State { get; set; } = quota.State;
}

/// <summary>
/// One charge the portal is going to be billed for at the start of the next period.
/// </summary>
/// <example>
/// {
///   "id": -11,
///   "name": "storage",
///   "title": "Business plan",
///   "unitOfMeasure": "admins",
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
    /// The quota that is going to be charged. When a switch to another quota is scheduled, this is the quota
    /// being switched to, so it can differ from what `GET api/2.0/portal/tariff` reports for today.
    /// </summary>
    /// <example>-11</example>
    public int Id { get; set; }

    /// <summary>
    /// The quota's stable key, which is the same identifier the wallet operations use for a service.
    /// </summary>
    /// <example>storage</example>
    public string Name { get; set; }

    /// <summary>
    /// The quota name in the portal language, meant to be printed on an invoice preview.
    /// </summary>
    /// <example>Business plan</example>
    public string Title { get; set; }

    /// <summary>
    /// What `quantity` counts, in the portal language - seats, administrators, gigabytes. It is empty for a quota
    /// that is simply on or off.
    /// </summary>
    /// <example>admins</example>
    public string UnitOfMeasure { get; set; }

    /// <summary>
    /// How much is going to be charged for, which is the quantity scheduled for the next period when one has been
    /// scheduled and today's quantity otherwise.
    /// </summary>
    /// <example>100</example>
    public int Quantity { get; set; }

    /// <summary>
    /// Whether the charge is paid out of the portal wallet rather than from the subscription.
    /// </summary>
    /// <example>true</example>
    public bool Wallet { get; set; }

    /// <summary>
    /// When the charge falls due, in the portal time zone.
    /// </summary>
    /// <example>2026-07-08T11:39:43.0000000+03:00</example>
    public ApiDateTime DueDate { get; set; }

    /// <summary>
    /// What the charge comes to: the unit price of the quota multiplied by `quantity`. Taxes are not part of it,
    /// and a quota with no price of its own is not listed at all rather than listed with a zero.
    /// </summary>
    /// <example>14</example>
    public decimal Amount { get; set; }

    /// <summary>
    /// The currency `amount` is expressed in, as a three-letter ISO 4217 code. It follows the portal's billing
    /// account, so every entry of one answer carries the same code.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; set; }
}
