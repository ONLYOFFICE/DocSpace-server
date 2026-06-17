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

namespace ASC.Core.Billing;

/// <summary>
/// The tariff parameters.
/// </summary>
/// <example>
/// {
///   "id": 1,
///   "state": "Trial",
///   "dueDate": "2026-03-31T00:00:00Z",
///   "delayDueDate": "2026-04-07T00:00:00Z",
///   "licenseDate": "2026-03-01T00:00:00Z",
///   "customerId": "cus_123",
///   "quotas": [
///     {
///       "id": 1,
///       "quantity": 50,
///       "wallet": false
///     }
///   ],
///   "overdueQuotas": []
/// }
/// </example>
[DebuggerDisplay("{State} before {DueDate}")]
[ProtoContract]
public class Tariff
{
    /// <summary>
    /// The tariff ID.
    /// </summary>
    /// <example>1</example>
    [ProtoMember(1)]
    public int Id { get; set; }

    /// <summary>
    /// The tariff state.
    /// </summary>
    /// <example>Trial</example>
    [ProtoMember(2)]
    public TariffState State { get; set; }

    /// <summary>
    /// The tariff due date.
    /// </summary>
    /// <example>2026-03-31T00:00:00Z</example>
    [ProtoMember(3)]
    public required DateTime DueDate { get; set; }

    /// <summary>
    /// The tariff delay due date.
    /// </summary>
    /// <example>2026-04-07T00:00:00Z</example>
    [ProtoMember(4)]
    public DateTime DelayDueDate { get; set; }

    /// <summary>
    /// The tariff license date.
    /// </summary>
    /// <example>2026-03-01T00:00:00Z</example>
    [ProtoMember(5)]
    public DateTime LicenseDate { get; set; }

    /// <summary>
    /// The tariff customer ID.
    /// </summary>
    /// <example>cus_123</example>
    [ProtoMember(6)]
    public string CustomerId { get; set; }

    /// <summary>
    /// The list of tariff quotas.
    /// </summary>
    /// <example>
    /// {
    ///   "quotas": [
    ///     {
    ///       "id": 1,
    ///       "quantity": 50,
    ///       "wallet": false
    ///     }
    ///   ]
    /// }
    /// </example>
    [ProtoMember(7)]
    public required List<Quota> Quotas { get; set; }

    /// <summary>
    /// The list of overdue tariff quotas.
    /// </summary>
    /// <example>[]</example>
    [ProtoMember(8)]
    public List<Quota> OverdueQuotas { get; set; }

    public override int GetHashCode()
    {
        return DueDate.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return obj is Tariff t && t.DueDate == DueDate;
    }

    public bool EqualsByParams(Tariff t)
    {
        return t != null
            && t.DueDate == DueDate
            && t.Quotas.Count == Quotas.Count
            && t.Quotas.TrueForAll(Quotas.Contains)
            && t.CustomerId == CustomerId
            && OverdueQuotasEqual(t.OverdueQuotas, OverdueQuotas);
    }

    private static bool OverdueQuotasEqual(List<Quota> left, List<Quota> right)
    {
        left ??= [];
        right ??= [];

        return left.Count == right.Count && left.TrueForAll(right.Contains);
    }
}

/// <summary>
/// The quota parameters.
/// <example>
/// {
///   "id": 1,
///   "quantity": 50,
///   "wallet": false,
///   "additional": false,
///   "dueDate": "2026-03-31T00:00:00Z",
///   "nextQuantity": 100,
///   "state": "Active"
/// }
/// </example>
/// </summary>
[ProtoContract]
public class Quota : IEquatable<Quota>
{
    /// <summary>
    /// The quota ID.
    /// </summary>
    /// <example></example>
    [ProtoMember(1)]
    public int Id { get; set; }

    /// <summary>
    /// The quota quantity.
    /// </summary>
    /// <example>50</example>
    [ProtoMember(2)]
    public int Quantity { get; set; }

    /// <summary>
    /// The quota applies to the wallet or not
    /// </summary>
    /// <example>false</example>
    [ProtoMember(3)]
    public bool Wallet { get; set; }

    /// <summary>
    /// The quota due date.
    /// </summary>
    /// <example>2026-03-31T00:00:00Z</example>
    [ProtoMember(4)]
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// The quota next quantity.
    /// </summary>
    /// <example>100</example>
    [ProtoMember(5)]
    public int? NextQuantity { get; set; }

    /// <summary>
    /// Indicates whether the quota is primary or additional.
    /// </summary>
    /// <example>false</example>
    [ProtoMember(6)]
    public bool Additional { get; set; }

    /// <summary>
    /// The quota state.
    /// </summary>
    /// <example>Active</example>
    public QuotaState? State => DueDate.HasValue ? DueDate.Value < DateTime.UtcNow ? QuotaState.Overdue : QuotaState.Active : null;

    public Quota()
    {
    }

    public Quota(int id, int quantity)
    {
        Id = id;
        Quantity = quantity;
    }

    public Quota(int id, int quantity, bool additional, bool wallet, DateTime? dueDate, int? nextQuantity)
    {
        Id = id;
        Quantity = quantity;
        Additional = additional;
        Wallet = wallet;
        DueDate = dueDate;
        NextQuantity = nextQuantity;
    }

    public bool Equals(Quota other)
    {
        return other != null && other.Id == Id && other.Quantity == Quantity && other.Wallet == Wallet && other.DueDate == DueDate && other.NextQuantity == NextQuantity;
    }
}

/// <summary>
/// The quota state.
/// </summary>
public enum QuotaState
{
    [Description("Active")]
    Active,

    [Description("Overdue")]
    Overdue
}
