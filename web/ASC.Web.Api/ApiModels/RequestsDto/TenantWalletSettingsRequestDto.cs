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
/// The body of an automatic top-up settings change.
/// </summary>
public class TenantWalletSettingsRequestDto
{
    /// <summary>
    /// The settings to store. They replace the stored ones as a whole, and a body without them resets automatic
    /// top-up to its defaults.
    /// </summary>
    /// <example>{"enabled": true, "minBalance": 10, "upToBalance": 100, "currency": "USD"}</example>
    public WalletTopUpSettingsRequestDto Settings { get; set; }
}

/// <summary>
/// The part of the automatic top-up settings a payer chooses. The low-balance warning state is kept by the portal
/// itself and cannot be set here.
/// </summary>
public class WalletTopUpSettingsRequestDto
{
    /// <summary>
    /// Whether the payment method on file is charged automatically when the wallet balance runs low.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; set; }

    /// <summary>
    /// The balance below which a top-up is charged, in `currency`.
    /// </summary>
    /// <example>10</example>
    [Range(5, 1000)]
    public int MinBalance { get; set; }

    /// <summary>
    /// The balance a top-up brings the wallet up to, in `currency`.
    /// </summary>
    /// <example>100</example>
    [Range(6, 5000)]
    public int UpToBalance { get; set; }

    /// <summary>
    /// The three-letter ISO 4217 code both amounts are expressed in; it has to be the currency of the wallet.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; set; }
}
