// (c) Copyright Ascensio System SIA 2009-2025
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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
