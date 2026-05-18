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

namespace ASC.Core.Common.Log;
internal static partial class TariffServiceLogger
{
    [LoggerMessage(LogLevel.Debug, "Payment tenant {tenantId} not found: {message}")]
    public static partial void DebugPaymentTenant(this ILogger<TariffService> logger, string tenantId, string message);

    [LoggerMessage(LogLevel.Debug, "Billing tenant {tenantId} not configured: {message}")]
    public static partial void DebugBillingTenant(this ILogger<TariffService> logger, string tenantId, string message);

    [LoggerMessage(LogLevel.Error, "GetShoppingUri")]
    public static partial void ErrorGetShoppingUri(this ILogger<TariffService> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "LoaderExceptions: {text}")]
    public static partial void ErrorLoaderExceptions(this ILogger<TariffService> logger, string text, Exception exception);

    [LoggerMessage(LogLevel.Error, "Billing tenant {tenantId}")]
    public static partial void ErrorBillingWithException(this ILogger<TariffService> logger, string tenantId, Exception exception);

    [LoggerMessage(LogLevel.Error, "Billing tenant {tenantId}: {message}")]
    public static partial void ErrorBilling(this ILogger<TariffService> logger, string tenantId, string message);
}