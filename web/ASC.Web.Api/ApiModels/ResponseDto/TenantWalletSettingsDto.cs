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
/// The automatic wallet top-up settings of the portal, together with the low-balance warning state the portal keeps
/// for them.
/// </summary>
public class TenantWalletSettingsDto
{
    /// <summary>
    /// Whether the payment method on file is charged automatically when the wallet balance runs low.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; init; }

    /// <summary>
    /// The balance below which a top-up is charged, in `currency`; 0 while top-up has never been configured.
    /// </summary>
    /// <example>10</example>
    public int MinBalance { get; init; }

    /// <summary>
    /// The balance a top-up brings the wallet up to, in `currency`; 0 while top-up has never been configured.
    /// </summary>
    /// <example>100</example>
    public int UpToBalance { get; init; }

    /// <summary>
    /// The three-letter ISO 4217 code both amounts are expressed in, or `null` while top-up has never been configured.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; init; }

    /// <summary>
    /// The wallet balance below which the portal sends its low-balance warning. The portal maintains it; it cannot be
    /// set by a request.
    /// </summary>
    /// <example>1</example>
    public int LowBalanceThreshold { get; init; }

    /// <summary>
    /// Whether the low-balance warning has already been sent for the current dip below `lowBalanceThreshold`. The
    /// portal maintains it, and switching top-up on re-arms it.
    /// </summary>
    /// <example>false</example>
    public bool LowBalanceNotified { get; init; }

    /// <summary>
    /// When the settings were last stored; when they were never stored, the moment they were read instead.
    /// </summary>
    /// <example>2026-01-01T00:00:00Z</example>
    public DateTime LastModified { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class TenantWalletSettingsDtoMapper
{
    public static partial TenantWalletSettingsDto Map(this TenantWalletSettings source);
}
