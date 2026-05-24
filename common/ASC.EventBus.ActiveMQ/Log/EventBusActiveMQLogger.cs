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
internal static partial class EventBusActiveMQLogger
{
    [LoggerMessage(LogLevel.Warning, "Could not publish event: {eventId} after {timeout}s")]
    public static partial void WarningCouldNotPublishEvent(this ILogger<EventBusActiveMQ> logger, Guid eventId, double timeout, Exception exception);

    [LoggerMessage(LogLevel.Trace, "Creating ActiveMQ session to publish event: {eventId} ({eventName})")]
    public static partial void TraceCreatingActiveMQSession(this ILogger<EventBusActiveMQ> logger, Guid eventId, string eventName);

    [LoggerMessage(LogLevel.Trace, "Declaring ActiveMQ exchange to publish event: {eventId}")]
    public static partial void TraceDeclaringActiveMQSession(this ILogger<EventBusActiveMQ> logger, Guid eventId);

    [LoggerMessage(LogLevel.Trace, "Publishing event to ActiveMQ: {eventId}")]
    public static partial void TracePublishingEvent(this ILogger<EventBusActiveMQ> logger, Guid eventId);

    [LoggerMessage(LogLevel.Information, "Subscribing to dynamic event {eventName} with {eventHandler}")]
    public static partial void InformationSubscribingDynamic(this ILogger<EventBusActiveMQ> logger, string eventName, string eventHandler);

    [LoggerMessage(LogLevel.Information, "Subscribing to event {eventName} with {eventHandler}")]
    public static partial void InformationSubscribing(this ILogger<EventBusActiveMQ> logger, string eventName, string eventHandler);

    [LoggerMessage(LogLevel.Information, "Unsubscribing from event {eventName}")]
    public static partial void InformationUnsubscribing(this ILogger<EventBusActiveMQ> logger, string eventName);

    [LoggerMessage(LogLevel.Trace, "Starting ActiveMQ basic consume")]
    public static partial void TraceStartingBasicConsume(this ILogger<EventBusActiveMQ> logger);

    [LoggerMessage(LogLevel.Trace, "Consumer tag {consumerTag} already exist. Cancelled BasicConsume again")]
    public static partial void TraceConsumerTagExist(this ILogger<EventBusActiveMQ> logger, string consumerTag);

    [LoggerMessage(LogLevel.Error, "StartBasicConsume can't call on _consumerSession == null")]
    public static partial void ErrorStartBasicConsumeCantCall(this ILogger<EventBusActiveMQ> logger);

    [LoggerMessage(LogLevel.Warning, "----- ERROR Processing message \"{message}\"")]
    public static partial void WarningProcessingMessage(this ILogger<EventBusActiveMQ> logger, string message, Exception exception);

    [LoggerMessage(LogLevel.Trace, "Creating ActiveMQ consumer session")]
    public static partial void TraceCreatingConsumerSession(this ILogger<EventBusActiveMQ> logger);

    [LoggerMessage(LogLevel.Warning, "Recreating ActiveMQ consumer session")]
    public static partial void WarningRecreatingConsumerSession(this ILogger<EventBusActiveMQ> logger, Exception exception);

    [LoggerMessage(LogLevel.Trace, "Processing ActiveMQ event: {eventName}")]
    public static partial void TraceProcessingEvent(this ILogger<EventBusActiveMQ> logger, string eventName);

    [LoggerMessage(LogLevel.Warning, "No subscription for ActiveMQ event: {eventName}")]
    public static partial void WarningNoSubscription(this ILogger<EventBusActiveMQ> logger, string eventName);
}