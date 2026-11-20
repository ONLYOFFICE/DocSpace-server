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


namespace ASC.Tests.Common.ApiFactories;

/// <summary>
/// Listens on the stack's event bus for one kind of event, for a test about what an API call hands over to another
/// service - a letter on its way to the notify service, above all. It binds a queue of its own to the exchange the
/// services publish on, so it sees only what was published after it started, and decodes each message with the
/// services' own serializer.
/// </summary>
/// <remarks>
/// A letter is published inside the request (<c>StudioNotifyServiceHelper</c> awaits the bus), so by the time a call
/// returns its letters have left. The services publish without broker confirms, though, so a message may reach this
/// queue a few milliseconds after the call returned: <see cref="ReadAsync{T}"/> reads until the first match and then
/// a moment longer.
/// </remarks>
public sealed class EventBusProbe : IAsyncDisposable
{
    // EventBusRabbitMQ publishes every event to this exchange, with the event's type name as the routing key.
    private const string Exchange = "asc_event_bus";

    private static readonly TimeSpan _settle = TimeSpan.FromMilliseconds(300);

    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _queue;
    // The serializer registers the subtypes of the events declared in the assemblies loaded when it is built, and the
    // model takes no new subtype after its first use, so it is built once, after the assemblies that declare the events
    // the suites read or publish are loaded: ASC.Common (the letters) and ASC.Core.Common (a disabled account), which a
    // suite that references no service project may not have.
    private static readonly Lazy<ProtobufSerializer> _serializer = new(() =>
    {
        _ = typeof(ASC.Common.IntegrationEvents.Events.NotifyItemIntegrationEvent).Assembly;

        try
        {
            System.Reflection.Assembly.Load("ASC.Core.Common");
        }
        catch (FileNotFoundException)
        {
            // not deployed with this suite; its events are then not read here
        }

        return new ProtobufSerializer();
    });

    private EventBusProbe(IConnection connection, IChannel channel, string queue)
    {
        _connection = connection;
        _channel = channel;
        _queue = queue;
    }

    /// <summary>
    /// Publishes an event the way <c>EventBusRabbitMQ</c> does: to the services' exchange, with the event's type name as
    /// the routing key, serialized as an <see cref="IntegrationEvent"/>. For a test of what a handler does with an
    /// event whose publisher the stack cannot reach (an LDAP synchronization).
    /// </summary>
    internal static async Task PublishAsync(string connectionString, IntegrationEvent @event, CancellationToken cancellationToken)
    {
        var body = _serializer.Value.Serialize(@event);

        await using var connection = await new ConnectionFactory { Uri = new Uri(connectionString) }.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Direct, cancellationToken: cancellationToken);
        await channel.BasicPublishAsync(
            Exchange,
            @event.GetType().Name,
            mandatory: true,
            new BasicProperties { DeliveryMode = DeliveryModes.Persistent, MessageId = Guid.NewGuid().ToString() },
            body,
            cancellationToken);
    }

    internal static async Task<EventBusProbe> StartAsync(string connectionString, string eventName, CancellationToken cancellationToken)
    {
        var connection = await new ConnectionFactory { Uri = new Uri(connectionString) }.CreateConnectionAsync(cancellationToken);
        IChannel? channel = null;

        try
        {
            channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            // declared the way the services declare it, so the call is a no-op once they have
            await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Direct, cancellationToken: cancellationToken);

            var queue = await channel.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true, cancellationToken: cancellationToken);
            await channel.QueueBindAsync(queue.QueueName, Exchange, eventName, cancellationToken: cancellationToken);

            return new EventBusProbe(connection, channel, queue.QueueName);
        }
        catch
        {
            channel?.Dispose();
            connection.Dispose();

            throw;
        }
    }

    /// <summary>
    /// The events published since the probe started that <paramref name="match"/> accepts. It reads until the first
    /// one arrives and then a moment longer, or gives up after ten seconds and returns what it has.
    /// </summary>
    public async Task<List<T>> ReadAsync<T>(Func<T, bool> match, CancellationToken cancellationToken) where T : IntegrationEvent
    {
        var found = new List<T>();
        var deadline = DateTime.UtcNow.AddSeconds(10);
        DateTime? settled = null;

        while (true)
        {
            while (await _channel.BasicGetAsync(_queue, autoAck: true, cancellationToken) is { } message)
            {
                var @event = (T)_serializer.Value.Deserialize(message.Body.ToArray(), typeof(T));

                if (match(@event))
                {
                    found.Add(@event);
                }
            }

            if (found.Count > 0)
            {
                settled ??= DateTime.UtcNow.Add(_settle);
            }

            var now = DateTime.UtcNow;
            if (now >= (settled ?? deadline) || now >= deadline)
            {
                return found;
            }

            await Task.Delay(50, cancellationToken);
        }
    }

    // Closing is best effort: a channel the broker already closed must not replace the failure of the test with its own.
    public async ValueTask DisposeAsync()
    {
        try
        {
            await _channel.CloseAsync();
            await _connection.CloseAsync();
        }
        catch (Exception)
        {
            // the queue is exclusive, so it goes with the connection anyway
        }
        finally
        {
            _channel.Dispose();
            _connection.Dispose();
        }
    }
}
