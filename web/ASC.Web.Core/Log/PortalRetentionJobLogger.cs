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
internal static partial class PortalRetentionJobLogger
{
    [LoggerMessage(LogLevel.Information, "Retention policy counts from {startedOn:yyyy-MM-dd}")]
    public static partial void InformationPolicyStart(this ILogger<PortalRetentionJob> logger, DateTime startedOn);

    [LoggerMessage(LogLevel.Information, "Retention run sends the letters of the days after {coveredFrom:yyyy-MM-dd} up to {today:yyyy-MM-dd}")]
    public static partial void InformationRunCovers(this ILogger<PortalRetentionJob> logger, DateTime coveredFrom, DateTime today);

    [LoggerMessage(LogLevel.Information, "Retention: tenant {tenantId} {tenantDomain}, {category}: {step} {letter}, block on {blockOn:yyyy-MM-dd}, delete on {deleteOn:yyyy-MM-dd}")]
    public static partial void InformationDecision(this ILogger<PortalRetentionJob> logger, int tenantId, string tenantDomain, PortalRetentionCategory category, PortalRetentionStep step, PortalRetentionLetter? letter, DateTime blockOn, DateTime deleteOn);

    [LoggerMessage(LogLevel.Warning, "Retention skipped for tenant {tenantId} today: the wallet balance could not be read")]
    public static partial void WarningBalanceUnknown(this ILogger<PortalRetentionJob> logger, int tenantId);

    [LoggerMessage(LogLevel.Warning, "Retention: {failures} balance requests in a row failed; the accounting service is not asked again in this run, and the portals that need it wait for the next one")]
    public static partial void WarningAccountingUnavailable(this ILogger<PortalRetentionJob> logger, int failures);

    [LoggerMessage(LogLevel.Information, "Retention skipped for tenant {tenantId} {tenantDomain}: the domain is kept on purpose")]
    public static partial void InformationForbiddenDomain(this ILogger<PortalRetentionJob> logger, int tenantId, string tenantDomain);
}
