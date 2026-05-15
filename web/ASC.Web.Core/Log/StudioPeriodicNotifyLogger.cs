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

namespace ASC.Web.Core.Log;
internal static partial class StudioPeriodicNotifyLogger
{
    [LoggerMessage(LogLevel.Error, "SendSaasLettersAsync {tenantId}")]
    public static partial void ErrorSendSaasLettersAsync(this ILogger logger, int tenantId, Exception exception);

    [LoggerMessage(LogLevel.Error, "SendEnterpriseLetters")]
    public static partial void ErrorSendEnterpriseLetters(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "SendOpensourceLetters")]
    public static partial void ErrorSendOpensourceLetters(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "SendMsgWhatsNew")]
    public static partial void ErrorSendMsgWhatsNew(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Information, "Start SendSaasTariffLetters")]
    public static partial void InformationStartSendSaasTariffLetters(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "End SendSaasTariffLetters")]
    public static partial void InformationEndSendSaasTariffLetters(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Start SendTariffEnterpriseLetters")]
    public static partial void InformationStartSendTariffEnterpriseLetters(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "End SendTariffEnterpriseLetters")]
    public static partial void InformationEndSendTariffEnterpriseLetters(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Start SendOpensourceTariffLetters")]
    public static partial void InformationStartSendOpensourceTariffLetters(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "End SendOpensourceTariffLetters")]
    public static partial void InformationEndSendOpensourceTariffLetters(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Current tenant: {tenantId}")]
    public static partial void InformationCurrentTenant(this ILogger logger, int tenantId);

    [LoggerMessage(LogLevel.Information, "Total send count: {sendCount}")]
    public static partial void InformationTotalSendCount(this ILogger logger, int sendCount);

    [LoggerMessage(LogLevel.Information, "Start removing unused free tenant: {tenantId} {tenantDomain}")]
    public static partial void InformationStartRemovingUnusedFreeTenant(this ILogger logger, int tenantId, string tenantDomain);

    [LoggerMessage(LogLevel.Information, "Start removing unused paid tenant: {tenantId} {tenantDomain}")]
    public static partial void InformationStartRemovingUnusedPaidTenant(this ILogger logger, int tenantId, string tenantDomain);
}