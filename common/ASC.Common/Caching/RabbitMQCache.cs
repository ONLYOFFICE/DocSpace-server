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

namespace ASC.Common.Caching;

[Singleton]
public class RabbitMQCache<T> : IDisposable, ICacheNotify<T> where T : new()
{
    private IConnection _connection;
    private readonly ConnectionFactory _factory;

    private IChannel _consumerChannel;
    private readonly string _exchangeName;
    private readonly string _queueName;

    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, List<Action<T>>> _actions;
    private readonly ConcurrentDictionary<string, List<Func<T, Task>>> _funcs = new();
    private bool _disposed;

    private readonly Task _initializeTask;

    public RabbitMQCache(IConfiguration configuration, ILogger<RabbitMQCache<T>> logger)
    {
        _logger = logger;
        var instanceId = Guid.NewGuid();
        _exchangeName = $"asc:cache_notify:event_bus:{typeof(T).FullName}";
        _queueName = $"asc:cache_notify:queue:{typeof(T).FullName}:{instanceId}";
        _actions = new ConcurrentDictionary<string, List<Action<T>>>();

        var rabbitMQConfiguration = configuration.GetSection("rabbitmq").Get<RabbitMQSettings>();
        _factory = rabbitMQConfiguration.GetConnectionFactory();
        _initializeTask = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (_connection is not null)
        {
            return;
        }

        _connection = await _factory.CreateConnectionAsync();
        _consumerChannel = await CreateConsumerChannelAsync();

        await StartBasicConsumeAsync();

        //diligently initializing
        await Task.Delay(100);
    }

    private async Task<IChannel> CreateConsumerChannelAsync()
    {
        await TryConnect();

        _logger.TraceCreatingRabbitMQ();

        var channel = await _connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Fanout);
        await channel.QueueDeclareAsync(queue: _queueName,
                                        durable: false,
                                        exclusive: false,
                                        autoDelete: true,
                                        arguments: null);

        await channel.QueueBindAsync(_queueName, _exchangeName, string.Empty);

        channel.CallbackExceptionAsync += async (_, ea) =>
        {
            _logger.WarningRecreatingRabbitMQ(ea.Exception);

            _consumerChannel.Dispose();
            _consumerChannel = await CreateConsumerChannelAsync();

            await StartBasicConsumeAsync();

        };

        return channel;
    }

    private async Task StartBasicConsumeAsync()
    {
        _logger.TraceStartingRabbitMQ();

        if (_consumerChannel != null)
        {
            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

            consumer.ReceivedAsync += AsyncConsumerOnReceived;

            await _consumerChannel.BasicConsumeAsync(queue: _queueName, autoAck: true, consumer: consumer);
        }
        else
        {
            _logger.ErrorStartBasicConsumeCanNotCall();
        }
    }

    private async Task TryConnect()
    {
        if (IsConnected)
        {
            return;
        }

        _connection = await _factory.CreateConnectionAsync();
        _connection.ConnectionShutdownAsync += async (_, _) => await TryConnect();
        _connection.CallbackExceptionAsync += async (_, _) => await TryConnect();
        _connection.ConnectionBlockedAsync += async (_, _) => await TryConnect();

    }


    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    private async Task AsyncConsumerOnReceived(object sender, BasicDeliverEventArgs e)
    {
        var body = e.Body.Span.ToArray();

        var data = body.Take(body.Length - 1);

        var obj = BaseProtobufSerializer.Deserialize<T>(data.ToArray());

        var action = (CacheNotifyAction)body[^1];

        if (_actions.TryGetValue(GetKey(action), out var onchange) && onchange != null)
        {
            Parallel.ForEach(onchange, a => a(obj));
        }
        
        if (_funcs.TryGetValue(GetKey(action), out var onchangeFunc) && onchangeFunc != null)
        {
            await Task.WhenAll(onchangeFunc.Select(a => a(obj)));
        }
        
        await Task.CompletedTask;
    }

    public async Task PublishAsync(T obj, CacheNotifyAction action)
    {
        await _initializeTask;

        var objAsByteArray = BaseProtobufSerializer.Serialize(obj);

        var body = new byte[objAsByteArray.Length + 1];

        objAsByteArray.CopyTo(body, 0);

        body[^1] = (byte)action;

        await _consumerChannel.BasicPublishAsync(
                             exchange: _exchangeName,
                             routingKey: string.Empty,
                             mandatory: true,
                             basicProperties: new BasicProperties(),
                             body: body);
    }


    public void Subscribe(Action<T> onchange, CacheNotifyAction action)
    {
        _actions.GetOrAdd(GetKey(action), []).Add(onchange);
    }

    public void Subscribe(Func<T, Task> onchange, CacheNotifyAction action)
    {
        _funcs.GetOrAdd(GetKey(action), []).Add(onchange);
    }

    public void Unsubscribe(CacheNotifyAction action)
    {
        _actions.TryRemove(GetKey(action), out _);
        _funcs.TryRemove(GetKey(action), out _);
    }

    private string GetKey(CacheNotifyAction cacheNotifyAction)
    {
        return $"asc:channel:{cacheNotifyAction}:{typeof(T).FullName}".ToLower();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _consumerChannel?.Dispose();
        _connection.Dispose();

        _disposed = true;
    }
}