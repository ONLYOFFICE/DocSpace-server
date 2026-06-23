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
internal static partial class AWSSenderLogger
{
    [LoggerMessage(LogLevel.Debug, "Tenant: {tenantId}, To: {reciever}")]
    public static partial void DebugSendTo(this ILogger logger, int tenantId, string reciever);

    [LoggerMessage(LogLevel.Debug, "Amazon sending failed: {result}, fallback to smtp")]
    public static partial void DebugAmazonSendingFailed(this ILogger logger, NoticeSendResult result);

    [LoggerMessage(LogLevel.Debug, "Send rate doesn't fit in send window. sleeping for: {interval}")]
    public static partial void DebugSendRate(this ILogger logger, TimeSpan interval);

    [LoggerMessage(LogLevel.Debug, "refreshing quota. interval: {timeout} Last refresh was at: {refreshDate}")]
    public static partial void DebugRefreshingQuota(this ILogger logger, TimeSpan timeout, DateTime refreshDate);

    [LoggerMessage(LogLevel.Debug, "quota: {lastCount}/{maxCount} at {rate} mps. send window:{interval}")]
    public static partial void DebugQuota(this ILogger logger, double? lastCount, double? maxCount, double rate, TimeSpan interval);

    [LoggerMessage(LogLevel.Warning, "Quota limit reached. setting next check to: {lastRefresh}")]
    public static partial void WarningQuotaLimit(this ILogger logger, DateTime lastRefresh);

    [LoggerMessage(LogLevel.Error, "Tenant: {tenantId}, To: {reciever}")]
    public static partial void ErrorSend(this ILogger logger, int tenantId, string reciever, Exception exception);

    [LoggerMessage(LogLevel.Error, "error refreshing quota")]
    public static partial void ErrorRefreshingQuota(this ILogger logger, Exception exception);
}