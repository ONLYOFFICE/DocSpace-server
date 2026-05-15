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

namespace ASC.EventBus.RedisMQ.Log;

internal static partial class EventBusRedisMQLogger
{
    [LoggerMessage(LogLevel.Warning, "Could not publish event: {eventId} after {timeout}s")]
    public static partial void WarningCouldNotPublishEvent(this ILogger<EventBusRedisMQ> logger, Guid eventId, double timeout, Exception exception);

    [LoggerMessage(LogLevel.Trace, "Publishing event {eventName} to Redis Stream {streamName}")]
    public static partial void TracePublishingEvent(this ILogger<EventBusRedisMQ> logger, string eventName, string streamName);

    [LoggerMessage(LogLevel.Trace, "Event {eventName} published to Redis Stream {streamName}")]
    public static partial void TraceEventPublished(this ILogger<EventBusRedisMQ> logger, string eventName, string streamName);

    [LoggerMessage(LogLevel.Information, "Subscribing to event {eventName} with handler {handlerName}")]
    public static partial void InformationSubscribing(this ILogger<EventBusRedisMQ> logger, string eventName, string handlerName);

    [LoggerMessage(LogLevel.Information, "Subscribing to dynamic event {eventName} with handler {handlerName}")]
    public static partial void InformationSubscribingDynamic(this ILogger<EventBusRedisMQ> logger, string eventName, string handlerName);

    [LoggerMessage(LogLevel.Information, "Unsubscribing from event {eventName}")]
    public static partial void InformationUnsubscribing(this ILogger<EventBusRedisMQ> logger, string eventName);

    [LoggerMessage(LogLevel.Information, "Unsubscribing from dynamic event {eventName}")]
    public static partial void InformationUnsubscribingDynamic(this ILogger<EventBusRedisMQ> logger, string eventName);

    [LoggerMessage(LogLevel.Trace, "Created consumer group {groupName} for stream {streamName}")]
    public static partial void TraceCreatedConsumerGroup(this ILogger<EventBusRedisMQ> logger, string groupName, string streamName);

    [LoggerMessage(LogLevel.Trace, "Consumer group {groupName} already exists for stream {streamName}")]
    public static partial void TraceConsumerGroupExists(this ILogger<EventBusRedisMQ> logger, string groupName, string streamName);

    [LoggerMessage(LogLevel.Information, "Starting consumer for stream {streamName}")]
    public static partial void InformationStartingConsumer(this ILogger<EventBusRedisMQ> logger, string streamName);

    [LoggerMessage(LogLevel.Information, "Consumer for stream {streamName} stopped")]
    public static partial void InformationConsumerStopped(this ILogger<EventBusRedisMQ> logger, string streamName);

    [LoggerMessage(LogLevel.Error, "Error processing message {messageId} from stream {streamName}")]
    public static partial void ErrorProcessingMessage(this ILogger<EventBusRedisMQ> logger, string messageId, string streamName, Exception exception);

    [LoggerMessage(LogLevel.Error, "Error consuming messages from stream {streamName}")]
    public static partial void ErrorConsumingMessages(this ILogger<EventBusRedisMQ> logger, string streamName, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Received empty event data for message {messageId}")]
    public static partial void WarningEmptyEventData(this ILogger<EventBusRedisMQ> logger, string messageId);

    [LoggerMessage(LogLevel.Trace, "Processing message {messageId} from stream {streamName}")]
    public static partial void TraceProcessingMessage(this ILogger<EventBusRedisMQ> logger, string messageId, string streamName);

    [LoggerMessage(LogLevel.Warning, "Event {eventName} rejected, message {messageId}")]
    public static partial void WarningEventRejected(this ILogger<EventBusRedisMQ> logger, string eventName, string messageId, Exception exception);

    [LoggerMessage(LogLevel.Error, "Error processing event {eventName}, message {messageId}")]
    public static partial void ErrorProcessingEvent(this ILogger<EventBusRedisMQ> logger, string eventName, string messageId, Exception exception);

    [LoggerMessage(LogLevel.Trace, "Processing event {eventName}")]
    public static partial void TraceProcessingEvent(this ILogger<EventBusRedisMQ> logger, string eventName);

    [LoggerMessage(LogLevel.Warning, "No subscription for event {eventName}")]
    public static partial void WarningNoSubscription(this ILogger<EventBusRedisMQ> logger, string eventName);

    [LoggerMessage(LogLevel.Information, "Message {messageId} processed successfully after {deliveryCount} attempts")]
    public static partial void InformationMessageProcessedAfterRetry(this ILogger<EventBusRedisMQ> logger, string messageId, long deliveryCount);

    [LoggerMessage(LogLevel.Warning, "Message {messageId} exceeded max retry attempts ({maxRetries}), moving to DLQ")]
    public static partial void WarningMessageMovedToDLQ(this ILogger<EventBusRedisMQ> logger, string messageId, int maxRetries);

    [LoggerMessage(LogLevel.Information, "Message {messageId} will be retried (attempt {deliveryCount}/{maxRetries})")]
    public static partial void InformationMessageWillRetry(this ILogger<EventBusRedisMQ> logger, string messageId, long deliveryCount, int maxRetries);

    [LoggerMessage(LogLevel.Information, "Message {messageId} moved to DLQ stream {dlqStream}")]
    public static partial void InformationMessageMovedToDLQStream(this ILogger<EventBusRedisMQ> logger, string messageId, string dlqStream);

    [LoggerMessage(LogLevel.Error, "Error processing pending messages for stream {streamName}")]
    public static partial void ErrorProcessingPendingMessages(this ILogger<EventBusRedisMQ> logger, string streamName, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed to move message {messageId} to DLQ")]
    public static partial void ErrorFailedToMoveToDLQ(this ILogger<EventBusRedisMQ> logger, string messageId, Exception exception);
}
