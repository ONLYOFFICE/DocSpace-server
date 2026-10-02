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

using Microsoft.Extensions.DependencyInjection;

namespace ASC.EventBus.RabbitMQ;

public class EventBusRabbitMQ : IEventBus, IDisposable, IAsyncDisposable
{
    const string EXCHANGE_NAME = "asc_event_bus";
    const string DEAD_LETTER_EXCHANGE_NAME = "asc_event_bus_dlx";

    private readonly IRabbitMQPersistentConnection _persistentConnection;
    private readonly ILogger<EventBusRabbitMQ> _logger;
    private readonly IEventBusSubscriptionsManager _subsManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly int _retryCount;
    private readonly ushort _prefetchCount;
    private readonly IIntegrationEventSerializer _serializer;

    private const int MaxPooledPublisherChannels = 16;

    private string _consumerTag;
    private IChannel _consumerChannel;
    private string _queueName;
    private readonly string _deadLetterQueueName;

    private readonly Task _initializeTask;
    private readonly SemaphoreSlim _consumeSemaphore = new(1, 1);
    private readonly SemaphoreSlim _recreateSemaphore = new(1, 1);
    private static readonly TimeSpan _initialRecreateDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan _maxRecreateDelay = TimeSpan.FromSeconds(30);
    private readonly ConcurrentBag<IChannel> _publisherChannelPool = [];
    private int _pooledPublisherChannelCount;
    private readonly ResiliencePipeline _publishPipeline;
    private volatile bool _disposing;

    private static readonly ResiliencePropertyKey<Guid> _eventIdPropertyKey = new("event-id");

    private static ConcurrentDictionary<Guid, byte[]> _rejectedEvents;

    public EventBusRabbitMQ(IRabbitMQPersistentConnection persistentConnection,
                            ILogger<EventBusRabbitMQ> logger,
                            IServiceProvider serviceProvider,
                            IEventBusSubscriptionsManager subsManager,
                            IIntegrationEventSerializer serializer,
                            string queueName = null,
                            int retryCount = 5,
                            ushort prefetchCount = 10)
    {
        _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _subsManager = subsManager ?? new InMemoryEventBusSubscriptionsManager();
        _queueName = queueName;
        _deadLetterQueueName = $"{_queueName}_dlx";
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        _retryCount = retryCount;
        _prefetchCount = prefetchCount;
        _subsManager.OnEventRemoved += async (s, e) =>
                                                    {
                                                        await SubsManager_OnEventRemovedAsync(s, e);
                                                    };

        _serializer = serializer;
        _rejectedEvents = new ConcurrentDictionary<Guid, byte[]>();

        _publishPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = _retryCount,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder()
                    .Handle<BrokerUnreachableException>()
                    .Handle<SocketException>(),
                OnRetry = args =>
                {
                    args.Context.Properties.TryGetValue(_eventIdPropertyKey, out var eventId);
                    _logger.WarningCouldNotPublishEvent(eventId, args.Duration.TotalSeconds, args.Outcome.Exception);

                    return ValueTask.CompletedTask;
                }
            })
            .AddRetry(new RetryStrategyOptions
            {
                // a dead pooled channel is replaced on the spot, so a single immediate retry is enough
                MaxRetryAttempts = 1,
                Delay = TimeSpan.Zero,
                ShouldHandle = new PredicateBuilder().Handle<AlreadyClosedException>(),
                OnRetry = args =>
                {
                    args.Context.Properties.TryGetValue(_eventIdPropertyKey, out var eventId);
                    _logger.WarningCouldNotPublishEvent(eventId, args.Duration.TotalSeconds, args.Outcome.Exception);

                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        _initializeTask = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (_consumerChannel is not null)
        {
            return;
        }

        _consumerChannel = await CreateConsumerChannelAsync();
    }

    private async Task SubsManager_OnEventRemovedAsync(object sender, string eventName)
    {
        if (!_persistentConnection.IsConnected)
        {
            await _persistentConnection.TryConnectAsync();
        }

        await using var channel = await _persistentConnection.CreateModelAsync();

        await channel.QueueUnbindAsync(queue: _queueName,
            exchange: EXCHANGE_NAME,
            routingKey: eventName);

        if (_subsManager.IsEmpty)
        {
            _queueName = string.Empty;

            await _consumerChannel.CloseAsync();
        }
    }

    public async Task PublishAsync(IntegrationEvent @event)
    {
        await _initializeTask;

        var eventName = @event.GetType().Name;
        var body = _serializer.Serialize(@event);

        var context = ResilienceContextPool.Shared.Get();
        context.Properties.Set(_eventIdPropertyKey, @event.Id);

        try
        {
            await _publishPipeline.ExecuteAsync(async _ =>
            {
                var channel = await RentPublisherChannelAsync();

                try
                {
                    var properties = new BasicProperties
                    {
                        DeliveryMode = DeliveryModes.Persistent,
                        MessageId = Guid.NewGuid().ToString()
                    };

                    _logger.TracePublishingEvent(@event.Id);

                    await channel.BasicPublishAsync(
                        exchange: EXCHANGE_NAME,
                        routingKey: eventName,
                        mandatory: true,
                        basicProperties: properties,
                        body: body);
                }
                catch
                {
                    channel.Dispose();

                    throw;
                }

                ReturnPublisherChannel(channel);
            }, context);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    private async Task<IChannel> RentPublisherChannelAsync()
    {
        // never hand out (or worse, reconnect for) a channel once shutdown has started
        ObjectDisposedException.ThrowIf(_disposing, this);

        while (_publisherChannelPool.TryTake(out var channel))
        {
            Interlocked.Decrement(ref _pooledPublisherChannelCount);

            if (channel.IsOpen)
            {
                return channel;
            }

            channel.Dispose();
        }

        if (!_persistentConnection.IsConnected)
        {
            await _persistentConnection.TryConnectAsync();
        }

        var newChannel = await _persistentConnection.CreateModelAsync();

        await newChannel.ExchangeDeclareAsync(exchange: EXCHANGE_NAME, type: "direct");

        return newChannel;
    }

    private void ReturnPublisherChannel(IChannel channel)
    {
        if (_disposing || !channel.IsOpen)
        {
            channel.Dispose();

            return;
        }

        if (Interlocked.Increment(ref _pooledPublisherChannelCount) > MaxPooledPublisherChannels)
        {
            Interlocked.Decrement(ref _pooledPublisherChannelCount);
            channel.Dispose();

            return;
        }

        _publisherChannelPool.Add(channel);
    }

    public async Task SubscribeDynamicAsync<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler
    {
        _logger.InformationSubscribingDynamic(eventName, typeof(TH).GetGenericTypeName());

        await DoInternalSubscriptionAsync(eventName);

        _subsManager.AddDynamicSubscription<TH>(eventName);

        await StartBasicConsumeAsync();
    }

    public async Task SubscribeAsync<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>
    {
        var eventName = _subsManager.GetEventKey<T>();

        await DoInternalSubscriptionAsync(eventName);

        _logger.InformationSubscribing(eventName, typeof(TH).GetGenericTypeName());

        _subsManager.AddSubscription<T, TH>();

        await StartBasicConsumeAsync();
    }

    private async Task DoInternalSubscriptionAsync(string eventName)
    {
        await _initializeTask;

        var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);

        if (!containsKey)
        {
            if (!_persistentConnection.IsConnected)
            {
                await _persistentConnection.TryConnectAsync();
            }

            await _consumerChannel.QueueBindAsync(queue: _deadLetterQueueName,
                                exchange: DEAD_LETTER_EXCHANGE_NAME,
                                routingKey: eventName);

            await _consumerChannel.QueueBindAsync(queue: _queueName,
                                exchange: EXCHANGE_NAME,
                                routingKey: eventName);
        }
    }

    public void Unsubscribe<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>
    {
        var eventName = _subsManager.GetEventKey<T>();

        _logger.InformationUnsubscribing(eventName);

        _subsManager.RemoveSubscription<T, TH>();
    }

    public void UnsubscribeDynamic<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler
    {
        _subsManager.RemoveDynamicSubscription<TH>(eventName);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        _disposing = true;

        await CloseChannelAsync(_consumerChannel);

        // an in-flight publication may still return a channel to the pool after this drain,
        // but a leaked channel at process shutdown is harmless, so a single pass is enough
        while (_publisherChannelPool.TryTake(out var channel))
        {
            await CloseChannelAsync(channel);
        }

        Interlocked.Exchange(ref _pooledPublisherChannelCount, 0);

        _subsManager.Clear();
    }

    private static async Task CloseChannelAsync(IChannel channel)
    {
        if (channel == null)
        {
            return;
        }

        try
        {
            // abort: close locally without waiting for the broker reply — it may already be gone on shutdown
            await channel.CloseAsync(Constants.ReplySuccess, "Goodbye", abort: true);
        }
        catch
        {
            // the channel is being thrown away, a failed close must not break shutdown
        }

        channel.Dispose();
    }

    private async Task StartBasicConsumeAsync()
    {
        _logger.TraceStartingBasicConsume();

        if (_consumerChannel == null)
        {
            _logger.ErrorStartBasicConsumeCantCall();
            return;
        }

        if (!string.IsNullOrEmpty(_consumerTag))
        {
            _logger.TraceConsumerTagExist(_consumerTag);
            return;
        }

        await _consumeSemaphore.WaitAsync();
        try
        {
            if (!string.IsNullOrEmpty(_consumerTag))
            {
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

            consumer.ReceivedAsync += Consumer_Received;
            consumer.ShutdownAsync += Consumer_Shutdown;
            _consumerTag = await _consumerChannel.BasicConsumeAsync(
                queue: _queueName,
                autoAck: false,
                consumer: consumer);
        }
        finally
        {
            _consumeSemaphore.Release();
        }
    }

    private Task Consumer_Shutdown(object sender, ShutdownEventArgs @event)
    {
        // closed by us: DisposeAsync, the last subscription removed, or a channel being replaced
        if (_disposing || @event.Initiator == ShutdownInitiator.Application)
        {
            return Task.CompletedTask;
        }

        _logger.WarningModelIsShutdown(@event.Cause?.ToString(), @event.Exception);

        // any other reason took the connection down with the channel: automatic recovery reopens
        // this very channel together with its consumer, so a consumer of our own would be a duplicate.
        // IsConnected cannot tell the two apart: the connection's shutdown handler waits for the
        // recovery, and the client holds this notification back until it is done
        if (!IsChannelLevelError(@event))
        {
            return Task.CompletedTask;
        }

        var channel = ((AsyncEventingBasicConsumer)sender).Channel;

        // the client's frame-reading loop waits for this handler, and opening a new channel
        // needs that loop to read the broker's reply, so the recreation must not be awaited here
        _ = Task.Run(() => RecreateConsumerAsync(channel));

        return Task.CompletedTask;
    }

    // AMQP soft errors: the broker closes the channel and keeps the connection
    private static bool IsChannelLevelError(ShutdownEventArgs @event)
    {
        return @event.Initiator == ShutdownInitiator.Peer
            && @event.ReplyCode is Constants.ContentTooLarge or Constants.NoRoute or Constants.NoConsumers
                or Constants.AccessRefused or Constants.NotFound or Constants.ResourceLocked or Constants.PreconditionFailed;
    }

    private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
    {
        // a delivery tag is only valid on the channel that delivered the message
        var channel = ((AsyncEventingBasicConsumer)sender).Channel;
        var deliveryTag = eventArgs.DeliveryTag;

        var eventName = eventArgs.RoutingKey;

        if (!channel.IsOpen)
        {
            // the broker requeues every unsettled delivery of a closed channel,
            // so handling this one now would only run it a second time
            _logger.DebugSkipDeliveryOnClosedChannel(eventName, deliveryTag);

            return;
        }

        IntegrationEvent @event;

        try
        {
            @event = GetEvent(eventName, eventArgs.Body.Span.ToArray());
        }
        catch (Exception ex)
        {
            // an exception leaving this handler would keep the message unacknowledged
            // until the broker's consumer timeout closes the whole channel
            _logger.ErrorDeserializingEvent(eventName, ex);

            await SettleAsync(() => channel.BasicRejectAsync(deliveryTag, requeue: false), deliveryTag);

            return;
        }

        if (@event == null)
        {
            await SettleAsync(() => channel.BasicRejectAsync(deliveryTag, requeue: false), deliveryTag);

            _logger.WarningUnknownEvent(eventName);

            return;
        }

        var message = @event.ToString();

        try
        {
            if (!_subsManager.HasSubscriptionsForEvent(eventName))
            {
                _logger.WarningNoSubscription(eventName);

                Guid.TryParse(eventArgs.BasicProperties.MessageId, out var messageId);

                if (_rejectedEvents.ContainsKey(messageId) || messageId == Guid.Empty)
                {
                    _rejectedEvents.TryRemove(messageId, out _);

                    _logger.DebugBeforeRejectEvent(eventName, message);

                    await SettleAsync(() => channel.BasicRejectAsync(deliveryTag, requeue: false), deliveryTag);

                    _logger.DebugRejectEvent(eventName);
                }
                else
                {
                    _rejectedEvents.TryAdd(messageId, eventArgs.Body.Span.ToArray());

                    _logger.DebugBeforeNackEvent(eventName, message);

                    // anti-pattern https://github.com/LeanKit-Labs/wascally/issues/36
                    await SettleAsync(() => channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true), deliveryTag);

                    _logger.DebugNackEvent(eventName);
                }

                return;
            }

            if (message.ToLowerInvariant().Contains("throw-fake-exception"))
            {
                throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
            }

            await ProcessEvent(eventName, @event);
        }
        catch (IntegrationEventRejectExeption ex)
        {
            _logger.ErrorProcessingMessage(message, ex);

            if (_rejectedEvents.ContainsKey(ex.EventId))
            {
                _rejectedEvents.TryRemove(ex.EventId, out _);
                await SettleAsync(() => channel.BasicRejectAsync(deliveryTag, requeue: false), deliveryTag);

                _logger.DebugRejectEvent(eventName);
            }
            else
            {
                _rejectedEvents.TryAdd(ex.EventId, eventArgs.Body.Span.ToArray());
                await SettleAsync(() => channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true), deliveryTag);

                _logger.DebugNackEvent(eventName);
            }

            return;
        }
        catch (Exception ex)
        {
            _logger.ErrorProcessingMessage(message, ex);
        }

        await SettleAsync(() => channel.BasicAckAsync(deliveryTag, multiple: false), deliveryTag);
    }

    private async Task SettleAsync(Func<ValueTask> settle, ulong deliveryTag)
    {
        try
        {
            await settle();
        }
        catch (AlreadyClosedException ex)
        {
            // the broker closed the channel while the message was handled and will redeliver it;
            // letting this escape would raise a callback exception for nothing
            _logger.WarningSettleOnClosedChannel(deliveryTag, ex);
        }
    }

    private async Task<IChannel> CreateConsumerChannelAsync()
    {
        if (!_persistentConnection.IsConnected)
        {
            await _persistentConnection.TryConnectAsync();
        }

        _logger.TraceCreatingConsumerChannel();

        var channel = await _persistentConnection.CreateModelAsync();

        // without a prefetch limit the broker pushes the whole queue at once, and its consumer
        // timeout runs from delivery, so the tail of a large backlog times out while still waiting
        // for the sequential handler; global: false limits each consumer, not the whole channel
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: _prefetchCount, global: false);

        await channel.ExchangeDeclareAsync(exchange: EXCHANGE_NAME,
                                type: "direct");

        await channel.ExchangeDeclareAsync(exchange: DEAD_LETTER_EXCHANGE_NAME,
                                type: "direct");

        await channel.QueueDeclareAsync(queue: _deadLetterQueueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);


        var arguments = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", DEAD_LETTER_EXCHANGE_NAME }
        };

        await channel.QueueDeclareAsync(queue: _queueName,
                                durable: true,
                                exclusive: false,
                                autoDelete: false,
                                arguments: arguments);

        channel.CallbackExceptionAsync += Channel_CallbackException;

        return channel;
    }

    private Task Channel_CallbackException(object sender, CallbackExceptionEventArgs e)
    {
        // a failed callback leaves the channel open; when the channel is closed instead,
        // Consumer_Shutdown brings the consumer back
        _logger.WarningCallbackException(e.Exception);

        return Task.CompletedTask;
    }

    private async Task RecreateConsumerAsync(IChannel closedChannel)
    {
        await _recreateSemaphore.WaitAsync();

        try
        {
            // already replaced by an earlier call, reopened by automatic recovery,
            // or no subscriptions left to consume for
            if (_disposing || !ReferenceEquals(closedChannel, _consumerChannel) || closedChannel.IsOpen || string.IsNullOrEmpty(_queueName))
            {
                return;
            }

            _logger.WarningRecreatingChannel();

            // a channel closed by the broker stays registered for automatic recovery until it is
            // closed locally (Dispose alone skips that), and the next connection recovery would
            // revive it together with its consumer
            await CloseChannelAsync(closedChannel);

            _consumerTag = string.Empty;

            var delay = _initialRecreateDelay;

            while (!_disposing)
            {
                IChannel channel = null;

                try
                {
                    channel = await CreateConsumerChannelAsync();

                    _consumerChannel = channel;

                    await StartBasicConsumeAsync();

                    _logger.InfoCreatedConsumerChannel();

                    return;
                }
                catch (Exception ex)
                {
                    _logger.WarningCouldNotRecreateChannel(delay.TotalSeconds, ex);

                    await CloseChannelAsync(channel);

                    _consumerTag = string.Empty;
                }

                await Task.Delay(delay);

                delay = delay * 2 < _maxRecreateDelay ? delay * 2 : _maxRecreateDelay;
            }
        }
        finally
        {
            _recreateSemaphore.Release();
        }
    }

    private IntegrationEvent GetEvent(string eventName, byte[] serializedMessage)
    {
        var eventType = _subsManager.GetEventTypeByName(eventName);

        if (eventType == null)
        {
            return null;
        }

        var integrationEvent = (IntegrationEvent)_serializer.Deserialize(serializedMessage, eventType);

        return integrationEvent;
    }

    private void PreProcessEvent(IntegrationEvent @event)
    {
        if (_rejectedEvents.IsEmpty)
        {
            return;
        }

        if (_rejectedEvents.ContainsKey(@event.Id))
        {
            @event.Redelivered = true;
        }
    }

    private async Task ProcessEvent(string eventName, IntegrationEvent @event)
    {
        _logger.TraceProcessingEvent(eventName);

        PreProcessEvent(@event);

        await using var scope = _serviceProvider.CreateAsyncScope();
        var subscriptions = _subsManager.GetHandlersForEvent(eventName);

        foreach (var subscription in subscriptions)
        {
            if (subscription.IsDynamic)
            {
                
                if (scope.ServiceProvider.GetService(subscription.HandlerType) is not IDynamicIntegrationEventHandler handler)
                {
                    continue;
                }

                using dynamic eventData = @event;
                await Task.Yield();
                await handler.Handle(eventData);
            }
            else
            {
                var handler = scope.ServiceProvider.GetService(subscription.HandlerType);
                if (handler == null)
                {
                    continue;
                }

                var eventType = _subsManager.GetEventTypeByName(eventName);
                var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

                await Task.Yield();
                await (Task)concreteType.GetMethod("Handle").Invoke(handler, [@event]);
            }
        }
    }
}