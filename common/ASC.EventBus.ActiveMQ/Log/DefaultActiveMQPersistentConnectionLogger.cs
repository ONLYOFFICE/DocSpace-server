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

namespace ASC.EventBus.ActiveMQ.Log;
internal static partial class DefaultActiveMQPersistentConnectionLogger
{
    [LoggerMessage(LogLevel.Critical, "DefaultActiveMQPersistentConnection")]
    public static partial void CriticalDefaultActiveMQPersistentConnection(this ILogger<DefaultActiveMQPersistentConnection> logger, Exception exception);

    [LoggerMessage(LogLevel.Information, "ActiveMQ Client is trying to connect")]
    public static partial void InformationActiveMQTryingConnect(this ILogger<DefaultActiveMQPersistentConnection> logger);

    [LoggerMessage(LogLevel.Warning, "ActiveMQ Client could not connect after {timeOut}s")]
    public static partial void WarningActiveMQCouldNotConnect(this ILogger<DefaultActiveMQPersistentConnection> logger, double timeOut, Exception exception);

    [LoggerMessage(LogLevel.Information, "ActiveMQ Client acquired a persistent connection to '{hostName}' and is subscribed to failure events")]
    public static partial void InformationActiveMQAcquiredPersistentConnection(this ILogger<DefaultActiveMQPersistentConnection> logger, string hostName);

    [LoggerMessage(LogLevel.Critical, "FATAL ERROR: ActiveMQ connections could not be created and opened")]
    public static partial void CriticalActiveMQCouldNotBeCreated(this ILogger<DefaultActiveMQPersistentConnection> logger);

    [LoggerMessage(LogLevel.Warning, "A ActiveMQ connection is shutdown. Trying to re-connect...")]
    public static partial void WarningActiveMQConnectionShutdown(this ILogger<DefaultActiveMQPersistentConnection> logger);

    [LoggerMessage(LogLevel.Warning, "A ActiveMQ connection throw exception. Trying to re-connect...")]
    public static partial void WarningActiveMQConnectionThrowException(this ILogger<DefaultActiveMQPersistentConnection> logger);

    [LoggerMessage(LogLevel.Warning, "A ActiveMQ connection is on shutdown. Trying to re-connect...")]
    public static partial void WarningActiveMQConnectionIsOnShutDown(this ILogger<DefaultActiveMQPersistentConnection> logger);
}