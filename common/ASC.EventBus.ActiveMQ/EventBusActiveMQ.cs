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

namespace ASC.EventBus.ActiveMQ;

public class EventBusActiveMQ : IEventBus, IDisposable
{
    const string AUTOFAC_SCOPE_NAME = "asc_event_bus";

    private readonly ILogger<EventBusActiveMQ> _logger;
    private readonly IEventBusSubscriptionsManager _subsManager;
    private readonly ILifetimeScope _autofac;

    private static ConcurrentQueue<Guid> _rejectedEvents;
    private readonly IActiveMQPersistentConnection _persistentConnection;
    private readonly IIntegrationEventSerializer _serializer;
    private ISession _consumerSession;

    private readonly List<IMessageConsumer> _consumers;

    private readonly int _retryCount;
    private string _queueName;
    private readonly Task _initializeTask;

    public EventBusActiveMQ(IActiveMQPersistentConnection persistentConnection,
                            ILogger<EventBusActiveMQ> logger,
                            ILifetimeScope autofac,
                            IEventBusSubscriptionsManager subsManager,
                            IIntegrationEventSerializer serializer,
                            string queueName = null,
                            int retryCount = 5)
    {
        _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _subsManager = subsManager ?? new InMemoryEventBusSubscriptionsManager();
        _serializer = serializer;
        _queueName = queueName;
        _autofac = autofac;
        _retryCount = retryCount;
        _rejectedEvents = new ConcurrentQueue<Guid>();
        _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
        _consumers = [];
        _initializeTask = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (_consumerSession is not null)
        {
            return;
        }

        _consumerSession = await CreateConsumerSessionAsync();
    }

    private async void SubsManager_OnEventRemoved(object sender, string eventName)
    {
        if (!_persistentConnection.IsConnected)
        {
            await _persistentConnection.TryConnectAsync();
        }

        using var session = await _persistentConnection.CreateSessionAsync(AcknowledgementMode.ClientAcknowledge);

        var messageSelector = $"eventName='{eventName}'";

        var findedConsumer = _consumers.Find(x => x.MessageSelector == messageSelector);

        if (findedConsumer != null)
        {
            await findedConsumer.CloseAsync();

            _consumers.Remove(findedConsumer);
        }

        if (_subsManager.IsEmpty)
        {
            _queueName = string.Empty;
            await _consumerSession.CloseAsync();
        }

    }

    public async Task PublishAsync(IntegrationEvent @event)
    {
        await _initializeTask;

        if (!_persistentConnection.IsConnected)
        {
            await _persistentConnection.TryConnectAsync();
        }

        Policy.Handle<SocketException>()
            .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _logger.WarningCouldNotPublishEvent(@event.Id, time.TotalSeconds, ex);
            });

        using var session = await _persistentConnection.CreateSessionAsync(AcknowledgementMode.ClientAcknowledge);
        var destination = await session.GetQueueAsync(_queueName);

        using var producer = await session.CreateProducerAsync(destination);
        producer.DeliveryMode = MsgDeliveryMode.Persistent;

        var body = _serializer.Serialize(@event);

        var request = await session.CreateStreamMessageAsync();
        var eventName = @event.GetType().Name;

        request.Properties["eventName"] = eventName;

        request.WriteBytes(body);

        await producer.SendAsync(request);
    }

    public async Task SubscribeAsync<T, TH>()
    where T : IntegrationEvent
    where TH : IIntegrationEventHandler<T>
    {
        await _initializeTask;

        var eventName = _subsManager.GetEventKey<T>();

        _logger.InformationSubscribing(eventName, typeof(TH).GetGenericTypeName());

        _subsManager.AddSubscription<T, TH>();

        await StartBasicConsumeAsync(eventName);
    }

    public async Task SubscribeDynamicAsync<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
    {
        await _initializeTask;

        _logger.InformationSubscribingDynamic(eventName, typeof(TH).GetGenericTypeName());

        _subsManager.AddDynamicSubscription<TH>(eventName);

        await StartBasicConsumeAsync(eventName);
    }

    private async Task<ISession> CreateConsumerSessionAsync()
    {
        if (!_persistentConnection.IsConnected)
        {
            await _persistentConnection.TryConnectAsync();
        }

        _logger.TraceCreatingConsumerSession();

        _consumerSession = await _persistentConnection.CreateSessionAsync(AcknowledgementMode.ClientAcknowledge);

        return _consumerSession;
    }

    private async Task StartBasicConsumeAsync(string eventName)
    {
        _logger.TraceStartingBasicConsume();

        if (!_persistentConnection.IsConnected)
        {
           await  _persistentConnection.TryConnectAsync();
        }

        var destination = await _consumerSession.GetQueueAsync(_queueName);

        var messageSelector = $"eventName='{eventName}'";

        var consumer = await _consumerSession.CreateConsumerAsync(destination, messageSelector);

        _consumers.Add(consumer);

        if (_consumerSession != null)
        {
            consumer.Listener += Consumer_Listener;
        }
        else
        {
            _logger.ErrorStartBasicConsumeCantCall();
        }
    }

    private async void Consumer_Listener(IMessage objMessage)
    {
        var streamMessage = objMessage as IStreamMessage;

        var eventName = streamMessage.Properties["eventName"].ToString();

        var buffer = new byte[4 * 1024];

        byte[] serializedMessage;

        using (var ms = new MemoryStream())
        {
            int read;

            while ((read = streamMessage.ReadBytes(buffer)) > 0)
            {
                ms.Write(buffer, 0, read);

                if (read < buffer.Length)
                {
                    break;
                }
            }

            serializedMessage = ms.ToArray();
        }

        var @event = GetEvent(eventName, serializedMessage);
        var message = @event.ToString();

        try
        {
            if (message.ToLowerInvariant().Contains("throw-fake-exception"))
            {
                throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
            }

            await ProcessEventAsync(eventName, @event);
               
            await streamMessage.AcknowledgeAsync();
        }
        catch (IntegrationEventRejectExeption ex)
        {
            _logger.WarningProcessingMessage(message, ex);

            if (_rejectedEvents.TryPeek(out var result) && result.Equals(ex.EventId))
            {
                _rejectedEvents.TryDequeue(out _);
                await streamMessage.AcknowledgeAsync();
            }
            else
            {
                _rejectedEvents.Enqueue(ex.EventId);
            }

        }
        catch (Exception ex)
        {
            _logger.WarningProcessingMessage(message, ex);

            await streamMessage.AcknowledgeAsync();
        }
    }

    private IntegrationEvent GetEvent(string eventName, byte[] serializedMessage)
    {
        var eventType = _subsManager.GetEventTypeByName(eventName);

        var integrationEvent = (IntegrationEvent)_serializer.Deserialize(serializedMessage, eventType);

        return integrationEvent;
    }


    public void Unsubscribe<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>
    {
        var eventName = _subsManager.GetEventKey<T>();

        _logger.InformationUnsubscribing(eventName);

        _subsManager.RemoveSubscription<T, TH>();
    }

    public void UnsubscribeDynamic<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
    {
        _subsManager.RemoveDynamicSubscription<TH>(eventName);
    }

    private static void PreProcessEvent(IntegrationEvent @event)
    {
        if (_rejectedEvents.Count == 0)
        {
            return;
        }

        if (_rejectedEvents.TryPeek(out var result) && result.Equals(@event.Id))
        {
            @event.Redelivered = true;
        }
    }

    private async Task ProcessEventAsync(string eventName, IntegrationEvent @event)
    {
        _logger.TraceProcessingEvent(eventName);

        PreProcessEvent(@event);

        if (_subsManager.HasSubscriptionsForEvent(eventName))
        {
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
        else
        {
            _logger.WarningNoSubscription(eventName);
        }
    }

    public void Dispose()
    {
        foreach (var consumer in _consumers)
        {
            consumer.Dispose();
        }

        _consumerSession?.Dispose();

        _subsManager.Clear();
    }
}