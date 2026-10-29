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

namespace ASC.Webhooks.Core.Log;

internal static partial class WebhookPublisherLogger
{
    [LoggerMessage(LogLevel.Error, "The {trigger} webhook for {dataId} could not be published to subscription {configId}; it is dropped for that subscription only")]
    public static partial void ErrorPublishWebhook(this ILogger<WebhookPublisher> logger, WebhookTrigger trigger, string dataId, int configId, Exception exception);

    [LoggerMessage(LogLevel.Error, "The delivery record {logId} is written, but the event that wakes the sender could not be published; the sender's sweep sends it")]
    public static partial void ErrorWakeSender(this ILogger<WebhookPublisher> logger, int logId, Exception exception);

    [LoggerMessage(LogLevel.Error, "The manual retry {logId} is queued, but the event that wakes the sender could not be published; the sender's sweep sends it")]
    public static partial void ErrorRetryEventBus(this ILogger<WebhookPublisher> logger, int logId, Exception exception);
}
