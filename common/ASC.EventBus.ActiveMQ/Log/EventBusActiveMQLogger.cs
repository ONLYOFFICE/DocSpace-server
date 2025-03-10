// (c) Copyright Ascensio System SIA 2009-2024
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
