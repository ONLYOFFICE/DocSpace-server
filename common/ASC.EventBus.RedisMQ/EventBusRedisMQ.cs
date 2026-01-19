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

using ASC.Api.Core.Core;
using ASC.EventBus.RedisMQ.Log;

namespace ASC.EventBus.RedisMQ;

public class EventBusRedisMQ : IEventBus, IAsyncDisposable
{
    private const string StreamPrefix = "eventbus:stream:";
    private const string ConsumerGroupName = "eventbus-consumers";
    private const string EventDataField = "data";
    private const string EventTypeField = "type";
    private const string DeadLetterStreamSuffix = ":dlq";
    private const int MaxRetryAttempts = 3;
    private const int PendingMessageIdleTimeMs = 60000;

    private readonly RedisPersistentConnection _persistentConnection;
    private readonly IEventBusSubscriptionsManager _subscriptionsManager;
    private readonly IIntegrationEventSerializer _serializer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventBusRedisMQ> _logger;
    private readonly string _consumerName;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _consumerCancellationTokens = new();
    private readonly int _retryCount;

    public EventBusRedisMQ(
        RedisPersistentConnection persistentConnection,
        ILogger<EventBusRedisMQ> logger,
        IEventBusSubscriptionsManager subscriptionsManager,
        IIntegrationEventSerializer serializer,
        IServiceProvider serviceProvider,
        int retryCount = 5)
    {
        _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
        _subscriptionsManager = subscriptionsManager ?? throw new ArgumentNullException(nameof(subscriptionsManager));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryCount = retryCount;
        _consumerName = $"{Environment.MachineName}-{Guid.NewGuid():N}";

        _subscriptionsManager.OnEventRemoved += OnEventRemoved;
    }

    public async Task PublishAsync(IntegrationEvent @event)
    {
        var eventName = @event.GetType().Name;
        var streamName = GetStreamName(eventName);

        _logger.TracePublishingEvent(eventName, streamName);

        var builder = new ResiliencePipelineBuilder();

        var pipeline = builder.AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = _retryCount,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<RedisConnectionException>().Handle<SocketException>(),
            OnRetry = args =>
            {
                _logger.WarningCouldNotPublishEvent(@event.Id, args.Duration.TotalSeconds, args.Outcome.Exception!);
                return ValueTask.CompletedTask;
            }
        }).Build();

        var db = _persistentConnection.GetConnection().GetDatabase();
        var eventData = _serializer.Serialize(@event);

        await pipeline.ExecuteAsync(async _ =>
        {
            var entries = new NameValueEntry[]
            {
                new(EventTypeField, eventName),
                new(EventDataField, eventData)
            };

            await db.StreamAddAsync(streamName, entries, maxLength: 10000, useApproximateMaxLength: true);

            _logger.TraceEventPublished(eventName, streamName);
        });
    }

    public async Task SubscribeAsync<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>
    {
        var eventName = _subscriptionsManager.GetEventKey<T>();

        await DoInternalSubscriptionAsync(eventName);

        _logger.InformationSubscribing(eventName, typeof(TH).Name);

        _subscriptionsManager.AddSubscription<T, TH>();

        await StartConsumerAsync(eventName);
    }

    public async Task SubscribeDynamicAsync<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler
    {
        await DoInternalSubscriptionAsync(eventName);

        _logger.InformationSubscribingDynamic(eventName, typeof(TH).Name);

        _subscriptionsManager.AddDynamicSubscription<TH>(eventName);

        await StartConsumerAsync(eventName);
    }

    public void Unsubscribe<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>
    {
        var eventName = _subscriptionsManager.GetEventKey<T>();

        _logger.InformationUnsubscribing(eventName);

        _subscriptionsManager.RemoveSubscription<T, TH>();
    }

    public void UnsubscribeDynamic<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler
    {
        _logger.InformationUnsubscribingDynamic(eventName);

        _subscriptionsManager.RemoveDynamicSubscription<TH>(eventName);
    }

    private async Task DoInternalSubscriptionAsync(string eventName)
    {
        var streamName = GetStreamName(eventName);
        var db = _persistentConnection.GetConnection().GetDatabase();

        try
        {
            await db.StreamCreateConsumerGroupAsync(streamName, ConsumerGroupName, StreamPosition.Beginning, createStream: true);
            _logger.TraceCreatedConsumerGroup(ConsumerGroupName, streamName);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            _logger.TraceConsumerGroupExists(ConsumerGroupName, streamName);
        }
    }

    private async Task StartConsumerAsync(string eventName)
    {
        if (_consumerCancellationTokens.ContainsKey(eventName))
        {
            return;
        }

        var cts = new CancellationTokenSource();
        if (!_consumerCancellationTokens.TryAdd(eventName, cts))
        {
            cts.Dispose();
            return;
        }

        _ = Task.Run(async () => await ConsumeMessagesAsync(eventName, cts.Token), cts.Token);

        await Task.CompletedTask;
    }

    private async Task ConsumeMessagesAsync(string eventName, CancellationToken cancellationToken)
    {
        var streamName = GetStreamName(eventName);
        var db = _persistentConnection.GetConnection().GetDatabase();

        _logger.InformationStartingConsumer(streamName);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(streamName, db, eventName, cancellationToken);

                var entries = await db.StreamReadGroupAsync(
                    streamName,
                    ConsumerGroupName,
                    _consumerName,
                    ">",
                    count: 10,
                    noAck: false);

                if (entries.Length == 0)
                {
                    await Task.Delay(100, cancellationToken);
                    continue;
                }

                foreach (var entry in entries)
                {
                    try
                    {
                        await ProcessMessageWithRetryAsync(eventName, entry, db, streamName, deliveryCount: 1);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorProcessingMessage(entry.Id.ToString(), streamName, ex);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.ErrorConsumingMessages(streamName, ex);
                await Task.Delay(1000, cancellationToken);
            }
        }

        _logger.InformationConsumerStopped(streamName);
    }

    private async Task ProcessMessageWithRetryAsync(string eventName, StreamEntry entry, IDatabase db, string streamName, long deliveryCount)
    {
        var eventData = entry[EventDataField];

        if (eventData.IsNullOrEmpty)
        {
            _logger.WarningEmptyEventData(entry.Id.ToString());
            await db.StreamAcknowledgeAsync(streamName, ConsumerGroupName, entry.Id);
            return;
        }

        _logger.TraceProcessingMessage(entry.Id.ToString(), streamName);

        try
        {
            await ProcessEventAsync(eventName, (byte[])eventData!);
            await db.StreamAcknowledgeAsync(streamName, ConsumerGroupName, entry.Id);
            
            if (deliveryCount > 1)
            {
                _logger.InformationMessageProcessedAfterRetry(entry.Id.ToString(), deliveryCount);
            }
        }
        catch (IntegrationEventRejectExeption ex)
        {
            _logger.WarningEventRejected(eventName, entry.Id.ToString(), ex);
            await db.StreamAcknowledgeAsync(streamName, ConsumerGroupName, entry.Id);
        }
        catch (Exception ex)
        {
            _logger.ErrorProcessingEvent(eventName, entry.Id.ToString(), ex);

            if (deliveryCount >= MaxRetryAttempts)
            {
                _logger.WarningMessageMovedToDLQ(entry.Id.ToString(), MaxRetryAttempts);
                
                await MoveToDeadLetterQueueAsync(streamName, entry, db, ex.Message);
                await db.StreamAcknowledgeAsync(streamName, ConsumerGroupName, entry.Id);
            }
            else
            {
                _logger.InformationMessageWillRetry(entry.Id.ToString(), deliveryCount, MaxRetryAttempts);
            }
        }
    }

    private async Task ProcessEventAsync(string eventName, byte[] message)
    {
        _logger.TraceProcessingEvent(eventName);

        if (!_subscriptionsManager.HasSubscriptionsForEvent(eventName))
        {
            _logger.WarningNoSubscription(eventName);
            return;
        }

        await using var scope = _serviceProvider.CreateAsyncScope();
        var subscriptions = _subscriptionsManager.GetHandlersForEvent(eventName);

        foreach (var subscription in subscriptions)
        {
            if (subscription.IsDynamic)
            {
                if (scope.ServiceProvider.GetService(subscription.HandlerType) is not IDynamicIntegrationEventHandler handler)
                {
                    continue;
                }

                using dynamic eventData = JsonDocument.Parse(message);
                await handler.Handle(eventData.RootElement);
            }
            else
            {
                var handler = scope.ServiceProvider.GetService(subscription.HandlerType);
                if (handler == null)
                {
                    continue;
                }

                var eventType = _subscriptionsManager.GetEventTypeByName(eventName);
                var integrationEvent = _serializer.Deserialize(message, eventType);
                var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                var method = concreteType.GetMethod("Handle");

                if (method != null)
                {
                    await (Task)method.Invoke(handler, [integrationEvent])!;
                }
            }
        }
    }

    private async Task ProcessPendingMessagesAsync(string streamName, IDatabase db, string eventName, CancellationToken cancellationToken)
    {
        try
        {
            var pendingInfo = await db.StreamPendingMessagesAsync(
                streamName,
                ConsumerGroupName,
                count: 10,
                consumerName: RedisValue.Null);

            if (pendingInfo == null || pendingInfo.Length == 0)
            {
                return;
            }

            foreach (var pending in pendingInfo)
            {
                if (pending.IdleTimeInMilliseconds < PendingMessageIdleTimeMs)
                {
                    continue;
                }

                var claimedEntries = await db.StreamClaimAsync(
                    streamName,
                    ConsumerGroupName,
                    _consumerName,
                    minIdleTimeInMs: PendingMessageIdleTimeMs,
                    messageIds: new[] { pending.MessageId });

                if (claimedEntries == null || claimedEntries.Length == 0)
                {
                    continue;
                }

                foreach (var entry in claimedEntries)
                {
                    try
                    {
                        await ProcessMessageWithRetryAsync(eventName, entry, db, streamName, pending.DeliveryCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorProcessingMessage(entry.Id.ToString(), streamName, ex);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.ErrorProcessingPendingMessages(streamName, ex);
        }
    }

    private async Task MoveToDeadLetterQueueAsync(string streamName, StreamEntry entry, IDatabase db, string errorMessage)
    {
        try
        {
            var dlqStreamName = streamName + DeadLetterStreamSuffix;
            
            var entries = new List<NameValueEntry>();
            
            foreach (var field in entry.Values)
            {
                entries.Add(field);
            }
            
            entries.Add(new NameValueEntry("originalMessageId", entry.Id.ToString()));
            entries.Add(new NameValueEntry("errorMessage", errorMessage));
            entries.Add(new NameValueEntry("failedAt", DateTimeOffset.UtcNow.ToString("O")));
            
            await db.StreamAddAsync(dlqStreamName, entries.ToArray());
            
            _logger.InformationMessageMovedToDLQStream(entry.Id.ToString(), dlqStreamName);
        }
        catch (Exception ex)
        {
            _logger.ErrorFailedToMoveToDLQ(entry.Id.ToString(), ex);
        }
    }

    private void OnEventRemoved(object? sender, string eventName)
    {
        if (_consumerCancellationTokens.TryRemove(eventName, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    private static string GetStreamName(string eventName) => $"{StreamPrefix}{eventName}";

    public async ValueTask DisposeAsync()
    {
        foreach (var kvp in _consumerCancellationTokens)
        {
            kvp.Value.Cancel();
            kvp.Value.Dispose();
        }

        _consumerCancellationTokens.Clear();
        _subscriptionsManager.Clear();

        await Task.CompletedTask;
    }
}
