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

namespace ASC.EventBus.RabbitMQ;

public class EventBusRabbitMQ : IEventBus, IDisposable
{
    const string EXCHANGE_NAME = "asc_event_bus";
    const string DEAD_LETTER_EXCHANGE_NAME = "asc_event_bus_dlx";
    const string AUTOFAC_SCOPE_NAME = "asc_event_bus";

    private readonly IRabbitMQPersistentConnection _persistentConnection;
    private readonly ILogger<EventBusRabbitMQ> _logger;
    private readonly IEventBusSubscriptionsManager _subsManager;
    private readonly ILifetimeScope _autofac;
    private readonly int _retryCount;
    private readonly IIntegrationEventSerializer _serializer;

    private string _consumerTag;
    private IChannel _consumerChannel;
    private string _queueName;
    private readonly string _deadLetterQueueName;

    private readonly Task _initializeTask;

    private static ConcurrentDictionary<Guid, byte[]> _rejectedEvents;

    public EventBusRabbitMQ(IRabbitMQPersistentConnection persistentConnection,
                            ILogger<EventBusRabbitMQ> logger,
                            ILifetimeScope autofac,
                            IEventBusSubscriptionsManager subsManager,
                            IIntegrationEventSerializer serializer,
                            string queueName = null,
                            int retryCount = 5)
    {
        _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _subsManager = subsManager ?? new InMemoryEventBusSubscriptionsManager();
        _queueName = queueName;
        _deadLetterQueueName = $"{_queueName}_dlx";
        _autofac = autofac;
        _retryCount = retryCount;
        _subsManager.OnEventRemoved += async (s, e) =>
                                                    {
                                                        await SubsManager_OnEventRemovedAsync(s, e);
                                                    };

        _serializer = serializer;
        _rejectedEvents = new ConcurrentDictionary<Guid, byte[]>();
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

        using var channel = await _persistentConnection.CreateModelAsync();

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

        if (!_persistentConnection.IsConnected)
        {
            await _persistentConnection.TryConnectAsync();
        }

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _logger.WarningCouldNotPublishEvent(@event.Id, time.TotalSeconds, ex);
            });

        var eventName = @event.GetType().Name;

        _logger.TraceCreatingRabbitMQChannel(@event.Id, eventName);

        using var channel = await _persistentConnection.CreateModelAsync();

        _logger.TraceDeclaringRabbitMQChannel(@event.Id);

        await channel.ExchangeDeclareAsync(exchange: EXCHANGE_NAME, type: "direct");

        var body = _serializer.Serialize(@event);

        await policy.Execute(async () =>
        {
            // TODO: check this method
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
        });
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
        _consumerChannel?.Dispose();

        _subsManager.Clear();
    }

    private async Task StartBasicConsumeAsync()
    {
        _logger.TraceStartingBasicConsume();

        if (_consumerChannel != null)
        {
            if (!String.IsNullOrEmpty(_consumerTag))
            {
                _logger.TraceConsumerTagExist(_consumerTag);

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
        else
        {
            _logger.ErrorStartBasicConsumeCantCall();
        }
    }

    private async Task Consumer_Shutdown(object sender, ShutdownEventArgs @event)
    {
        _logger.WarningModelIsShutdown(@event.Cause?.ToString(), @event.Exception);

        await Task.CompletedTask;
    }

    private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
    {
        var eventName = eventArgs.RoutingKey;

        var @event = GetEvent(eventName, eventArgs.Body.Span.ToArray());

        if (@event == null)
        {
            // anti-pattern https://github.com/LeanKit-Labs/wascally/issues/36
            await _consumerChannel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);

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

                if (_rejectedEvents.ContainsKey(messageId) || messageId == default)
                {
                    _rejectedEvents.TryRemove(messageId, out _);

                    _logger.DebugBeforeRejectEvent(eventName, message);

                    await _consumerChannel.BasicRejectAsync(eventArgs.DeliveryTag, requeue: false);

                    _logger.DebugRejectEvent(eventName);
                }
                else
                {
                    _rejectedEvents.TryAdd(messageId, eventArgs.Body.Span.ToArray());

                    _logger.DebugBeforeNackEvent(eventName, message);

                    // anti-pattern https://github.com/LeanKit-Labs/wascally/issues/36
                    await _consumerChannel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);

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
                await _consumerChannel.BasicRejectAsync(eventArgs.DeliveryTag, requeue: false);

                _logger.DebugRejectEvent(eventName);
            }
            else
            {
                _rejectedEvents.TryAdd(ex.EventId, eventArgs.Body.Span.ToArray());
                await _consumerChannel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);

                _logger.DebugNackEvent(eventName);
            }

            return;
        }
        catch (Exception ex)
        {
            _logger.ErrorProcessingMessage(message, ex);
        }

        await _consumerChannel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
    }



    private async Task<IChannel> CreateConsumerChannelAsync()
    {
        if (!_persistentConnection.IsConnected)
        {
            await _persistentConnection.TryConnectAsync();
        }

        _logger.TraceCreatingConsumerChannel();

        var channel = await _persistentConnection.CreateModelAsync();

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

        channel.CallbackExceptionAsync += RecreateChannel;

        return channel;
    }

    private async Task RecreateChannel(object sender, CallbackExceptionEventArgs e)
    {
        _logger.WarningCallbackException(e.Exception);

        _logger.WarningRecreatingChannel();

        _consumerChannel.Dispose();

        _consumerChannel = await CreateConsumerChannelAsync();
        _consumerTag = String.Empty;

        await StartBasicConsumeAsync();

        _logger.InfoCreatedConsumerChannel();

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

        await using var scope = _autofac.BeginLifetimeScope(AUTOFAC_SCOPE_NAME);
        var subscriptions = _subsManager.GetHandlersForEvent(eventName);

        foreach (var subscription in subscriptions)
        {
            if (subscription.IsDynamic)
            {
                if (scope.ResolveOptional(subscription.HandlerType) is not IDynamicIntegrationEventHandler handler)
                {
                    continue;
                }

                using dynamic eventData = @event;
                await Task.Yield();
                await handler.Handle(eventData);
            }
            else
            {
                var handler = scope.ResolveOptional(subscription.HandlerType);
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
