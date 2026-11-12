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

namespace ASC.Web.Studio.Wallet;

internal static partial class RenewSubscriptionServiceLogger
{
    [LoggerMessage(LogLevel.Information, "Found {count} wallet quotas to renew")]
    public static partial void InfoRenewSubscriptionServiceFound(this ILogger<RenewSubscriptionService> logger, int count);

    [LoggerMessage(LogLevel.Information, "Subscription renewed: tenant {tenantId}, {description}")]
    public static partial void InfoRenewSubscriptionServiceDone(this ILogger<RenewSubscriptionService> logger, int tenantId, string description);

    [LoggerMessage(LogLevel.Error, "Subscription renewal failed: tenant {tenantId}, {description}")]
    public static partial void ErrorRenewSubscriptionServiceFail(this ILogger<RenewSubscriptionService> logger, int tenantId, string description);

    // an error, not a warning: money may have moved without the subscription being extended, and production logs
    // keep errors only - this is the one trace of the attempt until the re-checks settle it
    [LoggerMessage(LogLevel.Error, "Subscription renewal outcome is unknown, billing gave no readable answer, will re-check: tenant {tenantId}, {description}")]
    public static partial void ErrorRenewSubscriptionServiceOutcomeUnknown(this ILogger<RenewSubscriptionService> logger, int tenantId, string description, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Subscription renewal that timed out at {timedOutAt} is not confirmed yet: tenant {tenantId}, {description}: {reason}")]
    public static partial void WarningRenewSubscriptionServiceNotConfirmed(this ILogger<RenewSubscriptionService> logger, int tenantId, string description, DateTime timedOutAt, string reason, Exception exception);

    [LoggerMessage(LogLevel.Error, "Subscription renewal was interrupted at {stage} on attempt {attempt}, a process stopped half-way, resuming: tenant {tenantId}, {description}")]
    public static partial void ErrorRenewSubscriptionServiceInterrupted(this ILogger<RenewSubscriptionService> logger, int tenantId, string description, string stage, int attempt);

    // a warning: nothing was paid and the renewal is simply made by the next run - what a rollout leaves behind
    [LoggerMessage(LogLevel.Warning, "Subscription renewal was left queued by a process that stopped, starting it: tenant {tenantId}, {description}")]
    public static partial void WarningRenewSubscriptionServiceLeftQueued(this ILogger<RenewSubscriptionService> logger, int tenantId, string description);

    [LoggerMessage(LogLevel.Warning, "Subscription renewal is not started, its journal entry is gone, changed or could not be marked - another instance has it: tenant {tenantId}, {description}")]
    public static partial void WarningRenewSubscriptionServiceNotClaimed(this ILogger<RenewSubscriptionService> logger, int tenantId, string description);

    [LoggerMessage(LogLevel.Warning, "Subscription renewal left behind by a process that stopped is not made, the subscription has changed or been extended since: tenant {tenantId}, {description}")]
    public static partial void WarningRenewSubscriptionServiceOutdated(this ILogger<RenewSubscriptionService> logger, int tenantId, string description);

    [LoggerMessage(LogLevel.Warning, "Subscription renewal left behind by a process that stopped waits for billing to answer: tenant {tenantId}, {description}")]
    public static partial void WarningRenewSubscriptionServiceWaits(this ILogger<RenewSubscriptionService> logger, int tenantId, string description, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Subscription renewal is taken out of the journal, the re-check of its payment change settles it: tenant {tenantId}, quota {quota}")]
    public static partial void WarningRenewSubscriptionServiceLeftToRecheck(this ILogger<RenewSubscriptionService> logger, int tenantId, int quota);

    [LoggerMessage(LogLevel.Warning, "Subscription renewal is taken out of the journal, its quota is not a wallet quota any more: tenant {tenantId}, quota {quota}")]
    public static partial void WarningRenewSubscriptionServiceUnknownQuota(this ILogger<RenewSubscriptionService> logger, int tenantId, int quota);

    [LoggerMessage(LogLevel.Error, "RenewSubscriptionService: error processing tenant {tenantId}")]
    public static partial void ErrorRenewSubscriptionServiceTenant(this ILogger<RenewSubscriptionService> logger, int tenantId, Exception exception);

    [LoggerMessage(LogLevel.Error, "Subscription renewal for tenant {tenantId} scheduled a switch to unknown wallet quota {nextQuotaId}")]
    public static partial void ErrorRenewSubscriptionServiceUnknownNextQuota(this ILogger<RenewSubscriptionService> logger, int tenantId, int nextQuotaId);
}
