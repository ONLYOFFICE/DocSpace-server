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

namespace ASC.Core.Tenants;

/// <summary>
/// The wrapper for the tenant AI auto top up settings.
/// </summary>
public class TenantAiAutoTopUpSettingsWrapper
{
    /// <summary>
    /// The tenant AI auto top up settings.
    /// </summary>
    /// <example>{"enabled": true, "amount": 50.00, "period": 0, "currency": "USD"}</example>
    public TenantAiAutoTopUpSettings Settings { get; set; }
}

/// <summary>
/// Defines the period between automatic AI sub-account top ups.
/// </summary>
public enum AiAutoTopUpPeriod
{
    /// <summary>Top up at most once per day.</summary>
    Daily,
    /// <summary>Top up at most once per week.</summary>
    Weekly,
    /// <summary>Top up at most once per month.</summary>
    Monthly
}

/// <summary>
/// The tenant AI auto top up settings.
/// </summary>
[Scope]
[Serializable]
public class TenantAiAutoTopUpSettings : ISettings<TenantAiAutoTopUpSettings>
{
    /// <summary>
    /// Specifies whether automatic top-up of the AI sub-account is enabled.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; set; }

    /// <summary>
    /// The target AI sub-account balance. The service tops up to this amount when the current balance falls below it.
    /// Must be between 0.01 and 999999.
    /// </summary>
    /// <example>50.00</example>
    [Range(0.01, 999999)]
    public decimal Amount { get; set; }

    /// <summary>
    /// The period between automatic top ups (Daily, Weekly, or Monthly).
    /// </summary>
    /// <example>0</example>
    public AiAutoTopUpPeriod Period { get; set; }

    /// <summary>
    /// The date and time (UTC) of the last successful automatic top-up.
    /// </summary>
    /// <example>1990-01-01T00:00:00Z</example>
    public DateTime LastTopUp { get; set; }

    /// <summary>
    /// The three-character ISO 4217 currency symbol.
    /// </summary>
    /// <example>USD</example>
    [StringLength(3)]
    public string Currency { get; set; }

    /// <summary>
    /// The date and time when the settings were last modified.
    /// </summary>
    /// <example>1990-01-01T00:00:00Z</example>
    public DateTime LastModified { get; set; }

    public static Guid ID => new("{A3F2B8C1-7E4D-4F9A-B021-6C8D3E5F1A2B}");

    public TenantAiAutoTopUpSettings GetDefault() => new()
    {
        Currency = "USD"
    };

    public static DateTime GetNextTopUpDate(DateTime lastTopUp, AiAutoTopUpPeriod period) =>
        period switch
        {
            AiAutoTopUpPeriod.Daily   => lastTopUp.AddDays(1),
            AiAutoTopUpPeriod.Weekly  => lastTopUp.AddDays(7),
            AiAutoTopUpPeriod.Monthly => lastTopUp.AddMonths(1),
            _                         => lastTopUp.AddDays(1)
        };
}
