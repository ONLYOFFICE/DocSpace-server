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
/// The wrapper for the tenant wallet settings.
/// </summary>
public class TenantWalletSettingsWrapper
{
    /// <summary>
    /// The tenant wallet settings.
    /// </summary>
    /// <example>{"enabled": true, "minBalance": 10, "upToBalance": 100, "currency": "USD"}</example>
    public TenantWalletSettings Settings { get; set; }
}

/// <summary>
/// The tenant wallet settings.
/// </summary>
[Scope]
[Serializable]
public class TenantWalletSettings : ISettings<TenantWalletSettings>
{
    /// <summary>
    /// Specifies whether automatic top-up for the tenant wallet is enabled.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; set; }

    /// <summary>
    /// The minimum wallet balance at which automatic top-up will be triggered. Must be between 5 and 1000.
    /// </summary>
    /// <example>10</example>
    [Range(5, 1000)]
    public int MinBalance { get; set; }

    /// <summary>
    /// The maximum wallet balance at which automatic top-up will be triggered. Must be between 6 and 5000.
    /// </summary>
    /// <example>100</example>
    [Range(6, 5000)]
    public int UpToBalance { get; set; }

    /// <summary>
    /// The three-character ISO 4217 currency symbol.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; set; }


    public static Guid ID => new("{40069709-492A-4F41-988C-F1A053A8A560}");

    public TenantWalletSettings GetDefault()
    {
        return new TenantWalletSettings();
    }

    /// <summary>
    /// The date and time when the tenant wallet settings were last modified.
    /// </summary>
    /// <example>1990-01-01T00:00:00Z</example>
    public DateTime LastModified { get; set; }
}